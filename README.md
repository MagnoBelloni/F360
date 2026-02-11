# F360 - Orquestrador de Jobs

Construido com .NET 8, MongoDB, RabbitMQ com Outbox pattern e autenticação com API Key.

## Architecture

O projeto está organizado da seguinte forma:

- **F360.Domain**: Entidades, enums e exceptions
- **F360.Application**: Use cases, DTOs, interfaces, e validators
- **F360.Infrastructure**: Database, messaging, e HTTP clients
- **F360.Api**: API e middlewares
- **F360.Workers.OutboxWorker**: Background service para o outbox pattern
- **F360.Workers.JobConsumer**: RabbitMQ consumer (Filas de prioridade high e low)

## Pre requisitos

- Docker
- .NET 8 SDK

## Rodando a aplicação

### 1. Infra

```bash
docker-compose up -d
```

Iniciara:

- MongoDB na porta 27017
- RabbitMQ nas portas 5672 (AMQP) e 15672 (Management UI)
  - user: guest
  - password: guest

### 2. Aplicação

Rodando os 3 projetos:

**Terminal 1 - API:**

```bash
cd src/F360.Presentation.Api
dotnet run
```

**Terminal 2 - OutboxWorker:**

```bash
cd src/F360.Workers.OutboxWorker
dotnet run
```

**Terminal 3 - JobConsumer:**

```bash
cd src/F360.Workers.JobConsumer
dotnet run
```

A API ficara disponivel em: `http://localhost:5000`
Swagger: `http://localhost:5000/swagger`

## API Endpoints

Os endpoints tem o campo obrigatório `X-Api-Key` no header (exceto `/health`).

**Default API Key**: `my-super-secret-api-key-12345`

### Create Job

```bash
curl --location 'http://localhost:50418/api/jobs' \
--header 'X-Api-Key: api-key-12345' \
--header 'Content-Type: application/json' \
--data '{
    "cep": "01310-100",
    "priority": 1,
    "scheduledTime": "2026-02-10T10:00:00Z"
}'
```

**Priority Values:**

- `0` = Low
- `1` = High

**ScheduledTime**: Opcional. Se não enviado o job executara imediatamente.

**Response:** `201 Created`

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Id**: Identificador do Job

### Get Job

```bash
curl --location 'http://localhost:50418/api/jobs/25f7a2d9-ec7a-4ab1-acc0-b728265feaea' \
--header 'X-Api-Key: api-key-12345'
```

**Response:**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "cep": "01310-100",
  "priority": 1,
  "status": 2,
  "scheduledTime": "2026-02-10T10:00:00Z",
  "createdAt": "2026-02-09T12:00:00Z",
  "completedAt": "2026-02-09T12:05:00Z"
}
```

**Status Values:**

- `0` = Pending
- `1` = Processing
- `2` = Finished
- `3` = Error
- `4` = Cancelled

### Cancel Job

```bash
curl --location --request POST 'http://localhost:50418/api/jobs/469b1408-a2c3-4ef7-84e2-dedc775fbf45:cancel' \
--header 'X-Api-Key: api-key-12345'
```

**Response:** `204 No Content`

### Health Check (Public - No API Key Required)

```bash
curl --location 'http://localhost:50418/health'
```

## Pontos de melhoria

- CircuitBreaker com polly na chamada do ViaCEP
- TTL com base no campo SentAt após X tempo da collection outbox_messages
- Registro de idempotency_key relacionado com ApiKey
- ApiKey por usuário salvo no BD
- RateLimit com base na ApiKey
- ApiKey com roles
- Permissões por roles com base na ApiKey
- Projeto F360.Workers.JobConsumer, com deploy separado em 2 versões, cada um consumindo uma fila de prioridade
- Projeto F360.Workers.OutboxWorker, com configuração de quantas mensagens processar
- Retrys nos consumers antes de jogar a mensagem na DLQ
- Enviar CorrelationId na publicação das mensagens do consumer
