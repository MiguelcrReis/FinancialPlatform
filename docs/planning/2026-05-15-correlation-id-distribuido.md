# Plano - CorrelationId Distribuído

## Objetivo

Implementar correlação distribuída simples e ponta a ponta para rastrear uma transação desde o `POST /api/transactions` no ApiGateway, passando pela validação HTTP no AccountService, publicação/consumo via RabbitMQ e processamento no TransactionService, usando logs estruturados e headers HTTP/RabbitMQ.

## Estado Atual Relevante

Baseado na inspeção do código real:

- `docker-compose.yml` sobe `api-gateway`, `account-service`, `transaction-service`, `rabbitmq` e `mongo`. As URLs internas já usam nomes de serviço Docker, e debug local depende dos fallbacks em `Program.cs`.
- `src/ApiGateway/Program.cs` configura Serilog com `Enrich.FromLogContext()`, OpenTelemetry básico, Swagger, Prometheus, `HttpClient` nomeado para `AccountService` com Polly e registra `RabbitMqPublisher` como singleton.
- `src/ApiGateway/Controllers/TransactionsController.cs` recebe `POST /api/transactions`, chama `AccountServiceClient.ValidateAsync`, cria `TransactionCreatedEvent` e publica no RabbitMQ.
- `src/ApiGateway/Services/AccountServiceClient.cs` usa `IHttpClientFactory` e `PostAsJsonAsync`, sem propagar headers.
- `src/ApiGateway/Messaging/Publishers/RabbitMqPublisher.cs` publica JSON em `transactions-exchange`, `Persistent = true`, mas não define `Headers`, `ContentType`, `MessageId` ou `CorrelationId`.
- `src/AccountService/Program.cs` também usa Serilog com `Enrich.FromLogContext()`, mas não tem middleware de correlação.
- `src/AccountService/Controllers/AccountsController.cs` valida contas sem ler headers explicitamente.
- `src/TransactionService/Program.cs` configura Serilog, OpenTelemetry, Mongo, RabbitMQ e registra `TransactionConsumer` como hosted service.
- `src/TransactionService/Messaging/Consumers/TransactionConsumer.cs` já usa `x-retry-count`, DLQ e `CreateForwardProperties`, que copia `source.Headers` para retry/DLQ. Isso facilita preservar `x-correlation-id`.
- `src/TransactionService/Application/Services/TransactionProcessorService.cs` mapeia o evento para `Transaction` e persiste, mas usa `Console.WriteLine`, que não participa do contexto Serilog.
- `src/TransactionService/Domain/Transaction.cs` não possui `CorrelationId`.
- `src/BuildingBlocks/Messaging/Contracts/TransactionCreatedEvent.cs` não possui `CorrelationId` no payload.

## Decisões Técnicas

- Header HTTP: usar `X-Correlation-Id`.
- Header RabbitMQ: usar `x-correlation-id`.
- Formato canônico: `Guid` em string, preferencialmente formato `D` (`xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`).
- Geração: o ApiGateway reutiliza `X-Correlation-Id` quando for um `Guid` válido; se ausente, vazio ou inválido, gera novo `Guid`.
- Validação: não retornar `400` por CorrelationId inválido. Tratar como ausente para não quebrar clientes; registrar warning com o valor recebido.
- Armazenamento por request: usar middleware + serviço scoped `ICorrelationContext`; também enriquecer logs com `Serilog.Context.LogContext`.
- Resposta HTTP: o middleware deve incluir `X-Correlation-Id` na resposta para facilitar teste via Swagger/curl.
- Propagação HTTP: usar `DelegatingHandler` no `HttpClient` do `AccountService`. É mais idiomático que alterar cada chamada em `AccountServiceClient` e mantém a propagação centralizada.
- Propagação RabbitMQ: alterar `IMessagePublisher` para aceitar headers opcionais e fazer o controller passar `x-correlation-id`. O publisher deve preencher `IBasicProperties.Headers`.
- Contrato do evento: não adicionar `CorrelationId` ao `TransactionCreatedEvent` nesta etapa. O CorrelationId é metadado de transporte/observabilidade, não dado de negócio. Alterar o record quebraria ou forçaria adaptação de produtores/consumidores sem necessidade.
- Persistência MongoDB: não persistir `CorrelationId` na entidade `Transaction` nesta etapa. Logs e headers atendem ao rastreamento desejado. Persistir teria valor para auditoria/consulta posterior, mas acoplaria domínio a metadado técnico e exigiria decidir exposição no DTO/API.
- OpenTelemetry: não fazer refatoração grande. Apenas adicionar `Activity.Current?.SetTag("correlation.id", correlationId)` no middleware HTTP e no consumidor quando houver activity ativa. Não criar tracing manual completo de RabbitMQ nesta etapa.
- Retry/DLQ: preservar `x-correlation-id` copiando headers existentes em `CreateForwardProperties`, e garantir que headers novos não sobrescrevam esse valor.

## Desenho da Solução

### Fluxo HTTP

1. Cliente chama `POST /api/transactions`.
2. Middleware no ApiGateway lê `X-Correlation-Id`.
3. Se o valor for `Guid` válido, normaliza e reutiliza.
4. Se não existir ou for inválido, gera novo `Guid`.
5. Middleware grava no `ICorrelationContext`, `HttpContext.Items["CorrelationId"]`, `LogContext` e header de resposta.
6. `AccountServiceClient` chama AccountService.
7. `DelegatingHandler` adiciona `X-Correlation-Id` na chamada HTTP.
8. Middleware equivalente no AccountService lê o header, valida, coloca em `LogContext` e devolve o mesmo header na resposta.

### Fluxo RabbitMQ

1. `TransactionsController` cria `TransactionCreatedEvent` sem alterar payload.
2. Controller obtém o correlation id do `ICorrelationContext`.
3. Chama `PublishAsync("transaction.created", @event, headers)` com `x-correlation-id`.
4. `RabbitMqPublisher` cria `IBasicProperties`, define:
   - `Persistent = true`
   - `ContentType = "application/json"`
   - `Headers["x-correlation-id"] = Encoding.UTF8.GetBytes(correlationId)` ou string normalizada
   - opcionalmente `CorrelationId = correlationId`, aproveitando propriedade nativa do RabbitMQ sem depender dela como fonte principal.
5. `TransactionConsumer` lê `x-correlation-id` de `ea.BasicProperties.Headers`.
6. Durante desserialização, processamento, retry e DLQ, o consumidor usa `LogContext.PushProperty("CorrelationId", correlationId)`.
7. `CreateForwardProperties` já copia headers. A implementação deve manter isso e só adicionar/atualizar `x-retry-count`, `x-error-reason` e `x-original-routing-key`.
8. Retry e DLQ preservam o mesmo `x-correlation-id`.

### Logs

- Manter o nome da propriedade como `CorrelationId` em todos os serviços.
- Como os três `Program.cs` já usam `Enrich.FromLogContext()`, `LogContext.PushProperty("CorrelationId", correlationId)` passa a aparecer no `{Properties:j}` do console.
- Substituir `Console.WriteLine` no `TransactionProcessorService` por `ILogger<TransactionProcessorService>` para que o processamento também apareça com `CorrelationId`.

## Arquivos Prováveis

Alterar/criar:

- `src/BuildingBlocks`: criar componentes compartilhados de correlação:
  - constantes `X-Correlation-Id`, `x-correlation-id`, `CorrelationId`
  - `ICorrelationContext`
  - `CorrelationContext`
  - middleware HTTP
  - `CorrelationIdDelegatingHandler`
  - helpers para ler/escrever headers RabbitMQ, incluindo conversão de `byte[]`
- `src/ApiGateway/Program.cs`: registrar contexto, middleware, `IHttpContextAccessor` se necessário e handler do `HttpClient`.
- `src/ApiGateway/Controllers/TransactionsController.cs`: passar `x-correlation-id` ao publisher.
- `src/ApiGateway/Messaging/Publishers/RabbitMqPublisher.cs`: aceitar headers opcionais e preencher `IBasicProperties.Headers`.
- `src/BuildingBlocks/Messaging/Interfaces/IMessagePublisher.cs`: alterar assinatura para headers opcionais.
- `src/AccountService/AccountService.csproj`: adicionar referência ao `BuildingBlocks`, se os componentes compartilhados ficarem lá.
- `src/AccountService/Program.cs`: registrar contexto e middleware.
- `src/TransactionService/Program.cs`: registrar contexto se o consumidor/processador precisar de scoped context; caso contrário, só manter dependências existentes.
- `src/TransactionService/Messaging/Consumers/TransactionConsumer.cs`: ler header, enriquecer logs, preservar em retry/DLQ.
- `src/TransactionService/Application/Services/TransactionProcessorService.cs`: trocar `Console.WriteLine` por `ILogger`.

Não alterar nesta etapa:

- `src/BuildingBlocks/Messaging/Contracts/TransactionCreatedEvent.cs`
- `src/TransactionService/Domain/Transaction.cs`
- DTOs públicos de consulta do TransactionService

## Passo a Passo de Implementação

1. Criar componentes compartilhados em `BuildingBlocks`:
   - `CorrelationIdConstants` com `HttpHeaderName = "X-Correlation-Id"`, `RabbitMqHeaderName = "x-correlation-id"` e `LogPropertyName = "CorrelationId"`.
   - `ICorrelationContext` com propriedade `string? CorrelationId`.
   - `CorrelationContext` scoped.
   - `CorrelationIdMiddleware` para HTTP.
   - `CorrelationIdDelegatingHandler` para propagar HTTP.
   - Helper para RabbitMQ que leia header como `byte[]`, `string` ou outros tipos simples.

2. Middleware HTTP:
   - Ler o primeiro valor de `X-Correlation-Id`.
   - Se `Guid.TryParse` for válido, normalizar com `guid.ToString("D")`.
   - Se inválido/ausente, gerar `Guid.NewGuid().ToString("D")`.
   - Guardar no `ICorrelationContext`.
   - Guardar em `HttpContext.Items["CorrelationId"]`.
   - Adicionar `X-Correlation-Id` na resposta.
   - Usar `using (LogContext.PushProperty("CorrelationId", correlationId)) await next(context);`.
   - Adicionar `Activity.Current?.SetTag("correlation.id", correlationId)`.

3. Registrar no ApiGateway:
   - `AddScoped<ICorrelationContext, CorrelationContext>()`.
   - `AddTransient<CorrelationIdDelegatingHandler>()`.
   - Inserir `app.UseMiddleware<CorrelationIdMiddleware>()` após Swagger e antes de `UseRouting`.
   - Adicionar `.AddHttpMessageHandler<CorrelationIdDelegatingHandler>()` no `HttpClient("AccountService")`.

4. Registrar no AccountService:
   - Adicionar `ProjectReference` para `BuildingBlocks`.
   - Registrar `ICorrelationContext`.
   - Inserir o mesmo middleware após Swagger e antes de `UseRouting`.

5. Alterar publisher:
   - Mudar `IMessagePublisher.PublishAsync<T>` para aceitar `IReadOnlyDictionary<string, object>? headers = null`.
   - Em `RabbitMqPublisher`, inicializar `properties.Headers`.
   - Copiar headers recebidos.
   - Definir `properties.ContentType = "application/json"`.
   - Se existir `x-correlation-id`, também preencher `properties.CorrelationId` com a string normalizada.

6. Alterar controller do ApiGateway:
   - Injetar `ICorrelationContext`.
   - Ao publicar, passar `x-correlation-id` nos headers.
   - Manter o `TransactionCreatedEvent` intacto.

7. Alterar TransactionConsumer:
   - Criar constante `CorrelationIdHeader = "x-correlation-id"` ou usar a constante compartilhada.
   - Ler correlation id de `ea.BasicProperties.Headers`.
   - Se ausente ou inválido, usar fallback:
     - `ea.BasicProperties.CorrelationId` se for `Guid` válido.
     - caso contrário gerar novo `Guid` apenas para logs desse consumo e registrar warning.
   - Envolver todo o fluxo da mensagem em `LogContext.PushProperty("CorrelationId", correlationId)`, incluindo desserialização, processamento, retry e DLQ.
   - Preservar o header original em retry/DLQ; se o header estava ausente e foi gerado fallback, adicionar `x-correlation-id` nos headers encaminhados para não perder dali em diante.
   - Manter `x-retry-count`, `x-error-reason` e `x-original-routing-key`.

8. Alterar TransactionProcessorService:
   - Injetar `ILogger<TransactionProcessorService>`.
   - Substituir `Console.WriteLine($"Processed transaction {tx.Id}")` por `logger.LogInformation("Processed transaction {TransactionId}", tx.Id)`.
   - Não passar `correlationId` como parâmetro de domínio; confiar no `LogContext` ambient criado no consumidor.

9. Revisar Swagger/debug:
   - Swagger não precisa declarar explicitamente o header para funcionar.
   - Para facilitar testes manuais, opcionalmente adicionar operation filter no ApiGateway/AccountService para documentar `X-Correlation-Id`, mas isso fica fora do mínimo necessário.

## Testes e Validação

- Build:
  - Executar `dotnet build` na solução/projetos alterados.

- Request sem `X-Correlation-Id`:
  - Subir com `docker compose up --build`.
  - Enviar `POST http://localhost:5240/api/transactions` pelo Swagger ou curl sem header.
  - Verificar resposta `202 Accepted`.
  - Verificar que a resposta contém `X-Correlation-Id`.
  - Verificar logs de `api-gateway`, `account-service` e `transaction-service` com o mesmo `CorrelationId`.

- Request com `X-Correlation-Id` válido:
  - Enviar `POST /api/transactions` com `X-Correlation-Id: 11111111-1111-1111-1111-111111111111`.
  - Verificar que a resposta mantém esse valor normalizado.
  - Verificar que logs dos três serviços possuem `CorrelationId = 11111111-1111-1111-1111-111111111111`.

- Request com `X-Correlation-Id` inválido:
  - Enviar header com valor não-Guid.
  - Esperado: transação não falha por causa do header.
  - ApiGateway gera novo Guid, retorna no header de resposta e registra warning.

- Chamada ApiGateway -> AccountService:
  - Confirmar em logs do AccountService que a validação recebeu o mesmo `CorrelationId` do ApiGateway.
  - Em debug local, confirmar que o `HttpClient("AccountService")` continua usando `http://localhost:5092` quando não estiver em Docker.

- Evento ApiGateway -> RabbitMQ -> TransactionService:
  - Confirmar que o `TransactionConsumer` lê `x-correlation-id`.
  - Confirmar que logs de desserialização/processamento/ack usam o mesmo valor.

- Retry/DLQ:
  - Forçar erro de processamento, por exemplo com Mongo indisponível ou exceção controlada temporária em ambiente de teste.
  - Verificar que mensagens republicadas para retry mantêm `x-correlation-id`.
  - Após exceder `RabbitMq__MaxRetryCount`, verificar na fila `transactions-dead-letter` que os headers incluem:
    - `x-correlation-id`
    - `x-retry-count`
    - `x-error-reason`
    - `x-original-routing-key`
  - Lembrar que RabbitMQ pode representar headers string como `byte[]`; validar convertendo UTF-8 quando necessário.

- Logs via Docker:
  - Usar `docker compose logs api-gateway account-service transaction-service`.
  - Procurar pelo mesmo Guid nos três serviços.

## Riscos e Cuidados

- RabbitMQ headers podem chegar como `byte[]`, `string`, `int`, `long` ou outros tipos; implementar leitura defensiva.
- Não depender apenas de `IBasicProperties.CorrelationId`; a decisão principal é `x-correlation-id` em headers.
- `LogContext` usa contexto async; o `using` precisa envolver todo o processamento assíncrono da mensagem.
- Não usar `Console.WriteLine` em pontos onde o CorrelationId precisa aparecer.
- Não rejeitar request por header inválido evita quebra de compatibilidade.
- Não alterar `TransactionCreatedEvent` evita quebra de contrato entre produtor e consumidor.
- Não persistir `CorrelationId` no domínio evita acoplamento prematuro; se no futuro houver necessidade de busca/auditoria por correlation id, aí sim adicionar campo no Mongo e decidir exposição no DTO.
- Middleware deve ser registrado antes dos controllers para cobrir logs de request.
- Swagger continuará funcionando; para enviar header manualmente, usar curl/Postman ou adicionar header diretamente na UI quando possível.
- Docker usa URLs internas (`account-service`, `transaction-service`); debug local usa fallbacks. A solução por headers independe desses modos.

## Fora de Escopo

- Dashboards Grafana.
- Métricas customizadas.
- Refatoração completa de tracing distribuído.
- Instrumentação manual completa de RabbitMQ com spans OpenTelemetry.
- Novos serviços.
- Troca de mensageria.
- Persistência de `CorrelationId` no MongoDB nesta etapa.
- Alteração do payload de `TransactionCreatedEvent`.
