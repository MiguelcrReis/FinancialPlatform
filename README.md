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

## Useful URLs

| Service | URL |
| --- | --- |
| ApiGateway Swagger | http://localhost:5240/swagger |
| TransactionService Swagger | http://localhost:5283/swagger |
| AccountService Swagger | http://localhost:5092/swagger |
| RabbitMQ Management | http://localhost:15672 |
| MongoDB | mongodb://localhost:27017 |

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

## URLs Uteis

| Servico | URL |
| --- | --- |
| ApiGateway Swagger | http://localhost:5240/swagger |
| TransactionService Swagger | http://localhost:5283/swagger |
| AccountService Swagger | http://localhost:5092/swagger |
| RabbitMQ Management | http://localhost:15672 |
| MongoDB | mongodb://localhost:27017 |

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
