# FinancialPlatform

FinancialPlatform is a microservices-based financial system built with .NET for learning distributed systems, observability, messaging, resilience, Docker, and backend best practices with C#.

The project simulates a simple financial transaction flow using HTTP APIs, RabbitMQ events, MongoDB persistence, structured logging, metrics, and distributed tracing.

## Architecture

```text
Client
  -> ApiGateway
  -> AccountService
  -> RabbitMQ
  -> TransactionService
  -> MongoDB
```

### Services

- **ApiGateway**: receives external requests, validates accounts, and publishes transaction events.
- **AccountService**: simulates account validation.
- **TransactionService**: consumes transaction events, processes them, persists them in MongoDB, and exposes query endpoints.
- **RabbitMQ**: asynchronous messaging between services.
- **MongoDB**: transaction persistence.

## Main Features

- Microservices with ASP.NET Core
- Event-driven communication with RabbitMQ
- MongoDB persistence
- Idempotency using transaction external IDs
- Manual ACK/NACK message handling
- Retry and Dead Letter Queue for failed messages
- RabbitMQ connection retry during service startup
- Docker Compose local environment
- Swagger/OpenAPI
- Serilog structured logs
- OpenTelemetry tracing and metrics
- Prometheus metrics endpoint

## Tech Stack

- .NET 10
- ASP.NET Core
- C#
- RabbitMQ
- MongoDB
- Docker / Docker Compose
- OpenTelemetry
- Serilog
- Prometheus
- Swagger

## Running Locally With Docker

From the repository root:

```powershell
docker compose up -d --build
```

This starts the full local stack:

```text
ApiGateway, AccountService, TransactionService, RabbitMQ, MongoDB,
OpenTelemetry Collector, Elasticsearch, and Kibana.
```

Check running containers:

```powershell
docker compose ps
```

Stop containers:

```powershell
docker compose down
```

Stop containers and remove persisted MongoDB data:

```powershell
docker compose down -v
```

After startup, Elasticsearch and Kibana may take a few seconds to become ready. Check the stack with:

```powershell
docker compose ps
```

To validate Elasticsearch directly:

```powershell
Invoke-RestMethod -Uri "http://localhost:9200"
```

## Useful URLs

| Service | URL |
| --- | --- |
| ApiGateway Swagger | http://localhost:5240/swagger |
| TransactionService Swagger | http://localhost:5283/swagger |
| AccountService Swagger | http://localhost:5092/swagger |
| RabbitMQ Management | http://localhost:15672 |
| MongoDB | mongodb://localhost:27017 |
| Elasticsearch | http://localhost:9200 |
| Kibana | http://localhost:5601 |
| OTLP gRPC | localhost:4317 |
| OTLP HTTP | localhost:4318 |

RabbitMQ credentials:

```text
guest / guest
```

## Testing The Main Flow

1. Open ApiGateway Swagger:

```text
http://localhost:5240/swagger
```

2. Execute:

```text
POST /api/transactions
```

Example body:

```json
{
  "accountId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "amount": 125.5,
  "currency": "BRL",
  "description": "Swagger persistence test",
  "fromAccount": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "toAccount": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
}
```

Expected response:

```text
202 Accepted
```

3. Open TransactionService Swagger:

```text
http://localhost:5283/swagger
```

4. Execute:

```text
GET /api/transactions
```

The created transaction should be returned from MongoDB.

## MongoDB

Connect with MongoDB Compass:

```text
mongodb://localhost:27017
```

Database:

```text
financialdb
```

Collection:

```text
transactions
```

## RabbitMQ And DLQ

Transaction events are published to:

```text
Exchange: transactions-exchange
Queue: transactions
Routing key: transaction.created
```

Failed messages can be sent to:

```text
Dead Letter Exchange: transactions-dlx
Dead Letter Queue: transactions-dead-letter
Dead Letter Routing Key: transaction.failed
```

The consumer retries failed messages using the `x-retry-count` header. After the retry limit is exceeded, the message is sent to the DLQ.

To inspect RabbitMQ retries in logs:

```powershell
docker compose logs transaction-service | Select-String "RabbitMQ connection failed"
```

## Logs

Follow all logs:

```powershell
docker compose logs -f
```

Follow only TransactionService logs:

```powershell
docker compose logs -f transaction-service
```

## Observability With Kibana

Logs are exported through the OpenTelemetry Collector to Elasticsearch index:

```text
financialplatform-logs
```

In Kibana, create a Data View with:

```text
financialplatform-logs*
```

Use `@timestamp` as the timestamp field when available. Useful fields include `CorrelationId`, `ServiceName`, `Environment`, `TransactionId`, `RequestPath`, `RoutingKey`, `QueueName`, `RetryCount`, and `DeadLetterReason`.

### Viewing Logs

Open Kibana:

```text
http://localhost:5601
```

Go to:

```text
Analytics -> Discover
```

Select the `FinancialPlatform Logs` Data View, set the time range to `Last 24 hours` or `This week`, leave the KQL field empty, and click `Refresh`.

Useful KQL filters:

```kql
ServiceName : "ApiGateway"
```

```kql
ServiceName : "TransactionService"
```

```kql
CorrelationId : "33333333-3333-3333-3333-333333333333"
```

```kql
SeverityText : "Warning" or SeverityText : "Error"
```

```kql
RequestPath : "/api/transactions"
```

### Importing Dashboards

The exported Kibana dashboard is versioned at:

```text
observability/kibana/financialplatform-dashboard.ndjson
```

To import it:

1. Open Kibana at `http://localhost:5601`.
2. Go to `Stack Management -> Saved Objects`.
3. Click `Import`.
4. Select `observability/kibana/financialplatform-dashboard.ndjson`.
5. Keep related objects enabled and confirm the import.

The `.ndjson` file contains only Kibana saved objects such as dashboards, visualizations, and the Data View. It does not contain logs or application data.

### Dashboard Panels

The recommended dashboard panels are:

| Panel | Purpose |
| --- | --- |
| Logs by service | Shows which services are generating logs in the selected period. |
| Logs by level | Groups logs by severity, such as Information, Warning, and Error. |
| Logs over time | Shows log volume trends and activity spikes. |
| Requests by route | Shows which HTTP routes are receiving traffic. |
| Warnings and errors | Focuses investigation on warning and error events. |
| Average response time by route | Shows average HTTP response time per route using the `Elapsed` field. |

## Development Notes

- Use Docker Compose for the full local stack.
- When changing code or Dockerfiles, run `docker compose up -d --build`.
- Use `docker compose down -v` only when you want to remove MongoDB data.
- When debugging a service locally, keep infrastructure services such as MongoDB and RabbitMQ running in Docker.

## Roadmap

- Correlation ID across HTTP, RabbitMQ, and logs
- Custom metrics for processed transactions, failures, retries, and DLQ messages
- Grafana dashboards
- Outbox Pattern
- Event versioning
- Additional services such as BalanceService, NotificationService, and FraudService

---

# FinancialPlatform - PT-BR

FinancialPlatform e um sistema financeiro baseado em microsservicos, desenvolvido com .NET para aprendizado de sistemas distribuidos, observabilidade, mensageria, resiliencia, Docker e boas praticas de backend com C#.

O projeto simula um fluxo simples de transacoes financeiras usando APIs HTTP, eventos com RabbitMQ, persistencia com MongoDB, logs estruturados, metricas e tracing distribuido.

## Arquitetura

```text
Client
  -> ApiGateway
  -> AccountService
  -> RabbitMQ
  -> TransactionService
  -> MongoDB
```

### Servicos

- **ApiGateway**: recebe requisicoes externas, valida contas e publica eventos de transacao.
- **AccountService**: simula a validacao de contas.
- **TransactionService**: consome eventos de transacao, processa, persiste no MongoDB e expoe endpoints de consulta.
- **RabbitMQ**: mensageria assincrona entre servicos.
- **MongoDB**: persistencia das transacoes.

## Principais Recursos

- Microsservicos com ASP.NET Core
- Comunicacao orientada a eventos com RabbitMQ
- Persistencia com MongoDB
- Idempotencia usando IDs externos de transacao
- Processamento de mensagens com ACK/NACK manual
- Retry e Dead Letter Queue para mensagens com falha
- Retry de conexao com RabbitMQ durante a inicializacao dos servicos
- Ambiente local com Docker Compose
- Swagger/OpenAPI
- Logs estruturados com Serilog
- Tracing e metricas com OpenTelemetry
- Endpoint de metricas Prometheus

## Stack

- .NET 10
- ASP.NET Core
- C#
- RabbitMQ
- MongoDB
- Docker / Docker Compose
- OpenTelemetry
- Serilog
- Prometheus
- Swagger

## Executando Localmente Com Docker

Na raiz do repositorio:

```powershell
docker compose up -d --build
```

Isso sobe a stack local completa:

```text
ApiGateway, AccountService, TransactionService, RabbitMQ, MongoDB,
OpenTelemetry Collector, Elasticsearch e Kibana.
```

Verificar containers em execucao:

```powershell
docker compose ps
```

Parar os containers:

```powershell
docker compose down
```

Parar os containers e remover os dados persistidos do MongoDB:

```powershell
docker compose down -v
```

Depois de subir, Elasticsearch e Kibana podem levar alguns segundos para ficarem prontos. Verifique a stack com:

```powershell
docker compose ps
```

Para validar o Elasticsearch diretamente:

```powershell
Invoke-RestMethod -Uri "http://localhost:9200"
```

## URLs Uteis

| Servico | URL |
| --- | --- |
| ApiGateway Swagger | http://localhost:5240/swagger |
| TransactionService Swagger | http://localhost:5283/swagger |
| AccountService Swagger | http://localhost:5092/swagger |
| RabbitMQ Management | http://localhost:15672 |
| MongoDB | mongodb://localhost:27017 |
| Elasticsearch | http://localhost:9200 |
| Kibana | http://localhost:5601 |
| OTLP gRPC | localhost:4317 |
| OTLP HTTP | localhost:4318 |

Credenciais do RabbitMQ:

```text
guest / guest
```

## Testando o Fluxo Principal

1. Abra o Swagger do ApiGateway:

```text
http://localhost:5240/swagger
```

2. Execute:

```text
POST /api/transactions
```

Body de exemplo:

```json
{
  "accountId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "amount": 125.5,
  "currency": "BRL",
  "description": "Swagger persistence test",
  "fromAccount": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "toAccount": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
}
```

Resposta esperada:

```text
202 Accepted
```

3. Abra o Swagger do TransactionService:

```text
http://localhost:5283/swagger
```

4. Execute:

```text
GET /api/transactions
```

A transacao criada deve ser retornada a partir do MongoDB.

## MongoDB

Conectar com MongoDB Compass:

```text
mongodb://localhost:27017
```

Database:

```text
financialdb
```

Collection:

```text
transactions
```

## RabbitMQ e DLQ

Eventos de transacao sao publicados em:

```text
Exchange: transactions-exchange
Queue: transactions
Routing key: transaction.created
```

Mensagens com falha podem ser enviadas para:

```text
Dead Letter Exchange: transactions-dlx
Dead Letter Queue: transactions-dead-letter
Dead Letter Routing Key: transaction.failed
```

O consumer faz retry usando o header `x-retry-count`. Quando o limite de tentativas e excedido, a mensagem e enviada para a DLQ.

Para inspecionar retries de conexao com RabbitMQ nos logs:

```powershell
docker compose logs transaction-service | Select-String "RabbitMQ connection failed"
```

## Logs

Acompanhar todos os logs:

```powershell
docker compose logs -f
```

Acompanhar apenas logs do TransactionService:

```powershell
docker compose logs -f transaction-service
```

## Observabilidade Com Kibana

Os logs sao exportados pelo OpenTelemetry Collector para o indice Elasticsearch:

```text
financialplatform-logs
```

No Kibana, crie um Data View com:

```text
financialplatform-logs*
```

Use `@timestamp` como campo de tempo quando estiver disponivel. Campos uteis incluem `CorrelationId`, `ServiceName`, `Environment`, `TransactionId`, `RequestPath`, `RoutingKey`, `QueueName`, `RetryCount` e `DeadLetterReason`.

### Visualizando Logs

Abra o Kibana:

```text
http://localhost:5601
```

Va para:

```text
Analytics -> Discover
```

Selecione o Data View `FinancialPlatform Logs`, ajuste o intervalo para `Last 24 hours` ou `This week`, deixe o campo KQL vazio e clique em `Refresh`.

Filtros KQL uteis:

```kql
ServiceName : "ApiGateway"
```

```kql
ServiceName : "TransactionService"
```

```kql
CorrelationId : "33333333-3333-3333-3333-333333333333"
```

```kql
SeverityText : "Warning" or SeverityText : "Error"
```

```kql
RequestPath : "/api/transactions"
```

### Importando Dashboards

O dashboard exportado do Kibana esta versionado em:

```text
observability/kibana/financialplatform-dashboard.ndjson
```

Para importar:

1. Abra o Kibana em `http://localhost:5601`.
2. Va para `Stack Management -> Saved Objects`.
3. Clique em `Import`.
4. Selecione `observability/kibana/financialplatform-dashboard.ndjson`.
5. Mantenha os objetos relacionados habilitados e confirme a importacao.

O arquivo `.ndjson` contem apenas objetos salvos do Kibana, como dashboards, visualizacoes e Data View. Ele nao contem logs nem dados da aplicacao.

### Paineis Do Dashboard

Os paineis recomendados do dashboard sao:

| Painel | Objetivo |
| --- | --- |
| Logs por servico | Mostra quais servicos estao gerando logs no periodo selecionado. |
| Logs por nivel | Agrupa logs por severidade, como Information, Warning e Error. |
| Logs ao longo do tempo | Mostra tendencia de volume de logs e picos de atividade. |
| Requests por rota | Mostra quais rotas HTTP estao recebendo trafego. |
| Warnings e erros | Foca a investigacao em eventos de warning e erro. |
| Tempo medio de resposta por rota | Mostra o tempo medio de resposta HTTP por rota usando o campo `Elapsed`. |

## Notas de Desenvolvimento

- Use Docker Compose para subir a stack local completa.
- Ao alterar codigo ou Dockerfiles, execute `docker compose up -d --build`.
- Use `docker compose down -v` apenas quando quiser remover os dados do MongoDB.
- Ao debugar um servico localmente, mantenha servicos de infraestrutura como MongoDB e RabbitMQ rodando no Docker.

## Roadmap

- Correlation ID entre HTTP, RabbitMQ e logs
- Metricas customizadas para transacoes processadas, falhas, retries e mensagens na DLQ
- Dashboards com Grafana
- Outbox Pattern
- Versionamento de eventos
- Novos servicos como BalanceService, NotificationService e FraudService
