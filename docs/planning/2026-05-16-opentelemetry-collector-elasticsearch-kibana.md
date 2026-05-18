# Plano - OpenTelemetry Collector + Elasticsearch + Kibana

## Objetivo

Implementar uma stack local de observabilidade para centralizar logs estruturados dos serviços .NET no Elasticsearch via OpenTelemetry Collector, mantendo logs no console e permitindo consulta no Kibana por `CorrelationId`, `ServiceName`, ambiente, requests HTTP e eventos RabbitMQ.

## Estado Atual Relevante

Baseado no código inspecionado:

- O `docker-compose.yml` atual sobe `rabbitmq`, `mongo`, `account-service`, `transaction-service` e `api-gateway`; ainda não há Elasticsearch, Kibana nem Collector.
- Os três serviços usam Serilog com `.Enrich.FromLogContext()`, `.Enrich.WithProperty("ServiceName", builder.Environment.ApplicationName)` e console sink.
- Os três serviços têm OpenTelemetry básico para tracing/métricas com `AddAspNetCoreInstrumentation()`, `AddHttpClientInstrumentation()`, `AddRuntimeInstrumentation()` e `AddConsoleExporter()`.
- Prometheus já existe via `UseHttpMetrics()` e `MapMetrics()`. Não deve ser removido.
- CorrelationId distribuído já foi implementado em `BuildingBlocks.Correlation`:
  - HTTP: `X-Correlation-Id`
  - RabbitMQ: `x-correlation-id`
  - log property: `CorrelationId`
  - activity tag: `correlation.id`
- ApiGateway e AccountService já registram `CorrelationIdMiddleware`.
- ApiGateway propaga `X-Correlation-Id` para AccountService via `CorrelationIdDelegatingHandler`.
- ApiGateway publica eventos RabbitMQ com headers opcionais no `IMessagePublisher`.
- `RabbitMqPublisher` já preenche `ContentType`, `MessageId`, `CorrelationId` nativo e header `x-correlation-id`.
- `TransactionConsumer` já lê/gera CorrelationId, usa `LogContext.PushProperty("CorrelationId", correlationId)` e preserva headers em retry/DLQ.
- Logs de request HTTP ainda dependem dos logs padrão do ASP.NET Core, mas `Microsoft.AspNetCore` está em `Warning`, então requests normais não ficam bem registrados.
- Logs do consumer RabbitMQ existem, mas devem ser enriquecidos com campos consistentes para Kibana: `QueueName`, `RoutingKey`, `RetryCount`, `DeadLetterReason`.

## Decisões Técnicas

- **Elasticsearch**: usar `docker.elastic.co/elasticsearch/elasticsearch:9.4.0`.
- **Kibana**: usar `docker.elastic.co/kibana/kibana:9.4.0`.
- **OpenTelemetry Collector**: usar `otel/opentelemetry-collector-contrib:0.152.0`.
- **Segurança local Elastic**: usar `xpack.security.enabled=false` e `xpack.security.autoconfiguration.enabled=false`, apenas para ambiente local de aprendizado. Isso evita setup de certificados, senhas e enrollment tokens. Não usar essa configuração fora de dev local.
- **Portas**:
  - Elasticsearch: `9200`
  - Kibana: `5601`
  - OTLP gRPC: `4317`
  - OTLP HTTP: `4318`
- **Índice Elasticsearch**: usar índice único `financialplatform-logs` nesta primeira versão.
  - Justificativa: facilita criar um único Data View no Kibana e consultar por campos (`ServiceName`, `Environment`, `CorrelationId`) sem gerenciar múltiplos índices.
  - Não criar índice por serviço nesta etapa.
- **Collector**: criar `otel-collector-config.yml` na raiz do projeto.
- **Receiver do Collector**: `otlp` com `grpc` e `http`.
- **Processors do Collector**: `memory_limiter` e `batch`.
- **Exporter do Collector**: `elasticsearch` com `logs_index: financialplatform-logs`.
- **Envio dos serviços .NET**: usar `Serilog.Sinks.OpenTelemetry`.
  - Justificativa: o projeto já usa Serilog e `LogContext`; o sink preserva propriedades Serilog como atributos OTLP, incluindo `CorrelationId`, `ServiceName`, `TransactionId`, `RetryCount`.
  - Evita migrar para outro modelo de logging agora.
- **Protocolo dos serviços para Collector**: usar OTLP gRPC em `4317`.
  - Docker: `http://otel-collector:4317`
  - Debug local: `http://localhost:4317`
- **Console logs**: manter `.WriteTo.Console(...)` como hoje.
- **Falha do Collector/Elastic**: não pode impedir startup nem processamento dos serviços. Observabilidade deve ser best-effort.
- **Prometheus**: manter como está.
- **Tracing completo/APM**: fora do escopo. Apenas manter TraceId/SpanId nos logs quando houver `Activity.Current`.

## Arquitetura Proposta

```text
ApiGateway
  -> Serilog Console
  -> Serilog OpenTelemetry Sink
      -> OTLP/gRPC
          -> OpenTelemetry Collector
              -> Elasticsearch index financialplatform-logs
                  -> Kibana Data View financialplatform-logs*

AccountService
  -> Serilog Console
  -> Serilog OpenTelemetry Sink
      -> OTLP/gRPC
          -> OpenTelemetry Collector
              -> Elasticsearch
                  -> Kibana

TransactionService
  -> HTTP logs + RabbitMQ consumer logs
  -> Serilog Console
  -> Serilog OpenTelemetry Sink
      -> OTLP/gRPC
          -> OpenTelemetry Collector
              -> Elasticsearch
                  -> Kibana
```

## Infraestrutura Docker

Adicionar ao `docker-compose.yml`:

- `elasticsearch`
  - image: `docker.elastic.co/elasticsearch/elasticsearch:9.4.0`
  - container: `elasticsearch`
  - port: `9200:9200`
  - environment:
    - `discovery.type=single-node`
    - `xpack.security.enabled=false`
    - `xpack.security.autoconfiguration.enabled=false`
    - `ES_JAVA_OPTS=-Xms512m -Xmx512m`
  - volume: `elastic-data:/usr/share/elasticsearch/data`
  - healthcheck em `http://localhost:9200/_cluster/health`

- `kibana`
  - image: `docker.elastic.co/kibana/kibana:9.4.0`
  - container: `kibana`
  - port: `5601:5601`
  - environment:
    - `ELASTICSEARCH_HOSTS=http://elasticsearch:9200`
  - depends_on `elasticsearch` healthy

- `otel-collector`
  - image: `otel/opentelemetry-collector-contrib:0.152.0`
  - container: `otel-collector`
  - command: `--config=/etc/otelcol-contrib/config.yml`
  - ports:
    - `4317:4317`
    - `4318:4318`
  - volume:
    - `./otel-collector-config.yml:/etc/otelcol-contrib/config.yml:ro`
  - depends_on `elasticsearch` healthy

Atualizar os serviços .NET no Compose com:

```text
Observability__OtlpEndpoint=http://otel-collector:4317
```

Não tornar `api-gateway`, `account-service` ou `transaction-service` dependentes do Collector. Se a observabilidade cair, a aplicação deve continuar funcionando.

Adicionar volume:

```text
elastic-data:
```

## Configuração do Collector

Criar `otel-collector-config.yml` na raiz com esta intenção:

```yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318

processors:
  memory_limiter:
    check_interval: 1s
    limit_mib: 256
    spike_limit_mib: 64
  batch:
    timeout: 5s
    send_batch_size: 512

exporters:
  elasticsearch:
    endpoints:
      - http://elasticsearch:9200
    logs_index: financialplatform-logs

service:
  pipelines:
    logs:
      receivers: [otlp]
      processors: [memory_limiter, batch]
      exporters: [elasticsearch]
```

Não configurar pipeline de metrics/traces nesta etapa. O foco inicial é logs estruturados.

## Configuração dos Serviços .NET

Em `ApiGateway`, `AccountService` e `TransactionService`:

- Adicionar NuGet:
  - `Serilog.Sinks.OpenTelemetry` versão `4.2.0`

- Atualizar o setup do Serilog para manter console e adicionar OTLP:

```text
.Enrich.FromLogContext()
.Enrich.WithProperty("ServiceName", builder.Environment.ApplicationName)
.Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
.WriteTo.Console(...)
.WriteTo.OpenTelemetry(...)
```

- Configurar o sink OTLP com:
  - endpoint vindo de `Observability:OtlpEndpoint`
  - default local: `http://localhost:4317`
  - protocolo: gRPC
  - resource attributes:
    - `service.name`
    - `deployment.environment`
    - opcionalmente `service.namespace = FinancialPlatform`

- Em `appsettings.json` dos três serviços, adicionar:

```json
"Observability": {
  "OtlpEndpoint": "http://localhost:4317"
}
```

- No Docker Compose, sobrescrever para:

```text
Observability__OtlpEndpoint=http://otel-collector:4317
```

- Adicionar `app.UseSerilogRequestLogging()` para logs estruturados de request HTTP.
  - Em ApiGateway e AccountService, registrar depois de `UseMiddleware<CorrelationIdMiddleware>()`.
  - Em TransactionService, adicionar `CorrelationIdMiddleware` também para endpoints HTTP GET e depois `UseSerilogRequestLogging()`.

- Não remover `AddConsoleExporter()` de OpenTelemetry nesta etapa, a menos que os logs fiquem ruidosos demais. O foco é não quebrar comportamento existente.

## Campos de Log Esperados

Campos esperados no Elasticsearch/Kibana:

- `ServiceName`
- `Environment`
- `CorrelationId`
- `TraceId`
- `SpanId`
- `TransactionId`
- `RoutingKey`
- `QueueName`
- `RetryCount`
- `MaxRetryCount`
- `DeadLetterReason`
- `OriginalRoutingKey`
- `RequestMethod`
- `RequestPath`
- `StatusCode`
- `Elapsed`
- `SourceContext`
- `service.name`
- `deployment.environment`

Observações:

- `CorrelationId` deve vir do `LogContext` já existente.
- `TraceId` e `SpanId` devem vir de `Activity.Current` quando houver activity ativa.
- No consumer RabbitMQ, se não houver activity ativa, ainda deve existir `CorrelationId`.
- Para logs RabbitMQ, enriquecer escopos ou chamadas de log no `TransactionConsumer` com `QueueName`, `RoutingKey`, `RetryCount` e `DeadLetterReason`.

## Passo a Passo de Implementação

1. Criar `otel-collector-config.yml` na raiz com receiver OTLP, processors `memory_limiter`/`batch`, exporter `elasticsearch` e pipeline `logs`.

2. Atualizar `docker-compose.yml`:
   - adicionar `elasticsearch`;
   - adicionar `kibana`;
   - adicionar `otel-collector`;
   - expor portas `9200`, `5601`, `4317`, `4318`;
   - adicionar volume `elastic-data`;
   - adicionar `Observability__OtlpEndpoint=http://otel-collector:4317` nos três serviços .NET.

3. Adicionar `Serilog.Sinks.OpenTelemetry` `4.2.0` em:
   - `src/ApiGateway/ApiGateway.csproj`
   - `src/AccountService/AccountService.csproj`
   - `src/TransactionService/TransactionService.csproj`

4. Atualizar `appsettings.json` dos três serviços com `Observability:OtlpEndpoint = http://localhost:4317`.

5. Atualizar configuração Serilog nos três `Program.cs`:
   - manter console;
   - adicionar `Environment`;
   - adicionar OpenTelemetry sink;
   - usar resource attributes `service.name`, `deployment.environment`, `service.namespace`.

6. Adicionar logs HTTP:
   - usar `app.UseSerilogRequestLogging()`;
   - garantir que venha depois do middleware de CorrelationId nos serviços que têm middleware;
   - adicionar `CorrelationIdMiddleware` ao TransactionService para seus endpoints HTTP.

7. Melhorar logs RabbitMQ no `TransactionConsumer`:
   - enriquecer escopo de consumo com `QueueName` e `RoutingKey`;
   - incluir `RetryCount` nos logs de retry;
   - incluir `DeadLetterReason` nos logs de DLQ;
   - manter preservação de `x-correlation-id` como já implementado.

8. Opcional mínimo no `RabbitMqPublisher`:
   - adicionar log estruturado ao publicar evento com `RoutingKey` e `CorrelationId`.
   - Não logar payload completo.

9. Atualizar `README.md`:
   - adicionar URLs:
     - Elasticsearch: `http://localhost:9200`
     - Kibana: `http://localhost:5601`
     - OTLP gRPC: `localhost:4317`
     - OTLP HTTP: `localhost:4318`
   - adicionar seção curta sobre criar Data View `financialplatform-logs*`.

## Testes e Validação

- **Stack sobe completa**
  - Rodar `docker compose up -d --build`.
  - Validar:
    - `docker compose ps`
    - `http://localhost:9200`
    - `http://localhost:5601`

- **Logs continuam no console**
  - Rodar `docker compose logs -f api-gateway account-service transaction-service`.
  - Confirmar que logs continuam aparecendo como antes.

- **Logs chegam no Elasticsearch**
  - Consultar:
    - `GET http://localhost:9200/financialplatform-logs/_search`
  - Esperado: documentos de log dos serviços.

- **Kibana Data View**
  - Abrir `http://localhost:5601`.
  - Criar Data View:
    - pattern: `financialplatform-logs*`
    - timestamp field: `@timestamp`, se disponível.
  - Abrir Discover.

- **Busca por CorrelationId**
  - Fazer `POST /api/transactions` no Swagger do ApiGateway.
  - Capturar `X-Correlation-Id` da resposta.
  - Buscar no Kibana por esse valor.
  - Esperado: logs do ApiGateway, AccountService e TransactionService.

- **Request com CorrelationId manual**
  - Enviar `X-Correlation-Id: 11111111-1111-1111-1111-111111111111`.
  - Buscar esse valor no Kibana.
  - Esperado: logs dos três serviços com o mesmo id.

- **Logs HTTP**
  - Confirmar logs estruturados para:
    - `POST /api/transactions` no ApiGateway;
    - `POST /api/accounts/validate` no AccountService;
    - `GET /api/transactions` no TransactionService.

- **RabbitMQ retry/DLQ**
  - Forçar falha controlada de processamento em ambiente local.
  - Confirmar logs com:
    - `CorrelationId`
    - `TransactionId`
    - `RetryCount`
    - `MaxRetryCount`
    - `DeadLetterReason` quando for para DLQ.

- **Collector/Elastic indisponível**
  - Parar `otel-collector` ou `elasticsearch`.
  - Fazer request no ApiGateway.
  - Esperado:
    - aplicação continua funcionando;
    - logs continuam no console;
    - logs remotos podem ser perdidos até a stack voltar.

## Riscos e Cuidados

- O exporter Elasticsearch do Collector fica no `opentelemetry-collector-contrib`; por isso usar a imagem `contrib`, não a imagem core.
- Manter Elasticsearch e Kibana na mesma versão.
- Elasticsearch 9.x consome memória; Docker Desktop deve ter memória suficiente. Se necessário, ajustar `ES_JAVA_OPTS`.
- `xpack.security.enabled=false` é aceitável só para dev local.
- Não duplicar logs criando Serilog OTLP e OpenTelemetry logging provider ao mesmo tempo. A decisão é usar Serilog OTLP.
- OTLP gRPC usa `4317`; OTLP HTTP usa `4318`. Configurar os serviços para `4317`.
- Se `UseSerilogRequestLogging()` for registrado antes do CorrelationId middleware, logs HTTP podem sair sem `CorrelationId`.
- O formato dos documentos no Elasticsearch seguirá o mapeamento OTLP do exporter; no Kibana alguns campos podem aparecer sob atributos/resource fields.
- Se Collector ou Elasticsearch estiver fora, logs remotos podem ser perdidos. Console continua sendo a fonte local imediata.
- Evitar logar payloads financeiros completos. Usar identificadores e metadados.

## Fora de Escopo

- Dashboards avançados no Kibana.
- Alertas.
- Métricas customizadas.
- Substituir Prometheus.
- Tracing distribuído completo/APM.
- Instrumentação manual avançada de RabbitMQ com spans.
- Segurança avançada do Elastic.
- Autenticação TLS/usuários/senhas no Elasticsearch local.
- Deploy em Kubernetes.
- ILM, rollover e políticas de retenção.
- Índices separados por serviço/ambiente.

## Referências Consultadas

- [OpenTelemetry Collector exporters](https://opentelemetry.io/docs/collector/components/exporter/)
- [Elasticsearch exporter para OpenTelemetry Collector](https://www.elastic.co/docs/reference/edot-collector/components/elasticsearchexporter)
- [Serilog.Sinks.OpenTelemetry no NuGet](https://www.nuget.org/packages/Serilog.Sinks.OpenTelemetry)
- [Kibana com Docker](https://www.elastic.co/docs/deploy-manage/deploy/self-managed/install-kibana-with-docker)
- [Configuração de segurança do Elasticsearch](https://www.elastic.co/docs/reference/elasticsearch/configuration-reference/security-settings)
- [OpenTelemetry Collector releases](https://github.com/open-telemetry/opentelemetry-collector/releases)
