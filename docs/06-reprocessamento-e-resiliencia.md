# Reprocessamento e resiliência

O projeto possui um mecanismo de recuperação quando o Google Sheets está indisponível.

## Armazenamento de pendências

As presenças que não puderam ser enviadas são armazenadas em:

```text
App_Data/pending-attendances.json
```

Exemplo de configuração:

```json
"PendingAttendance": {
  "FilePath": "App_Data/pending-attendances.json",
  "RetryIntervalSeconds": 30
}
```

## Por que armazenar a Attendance completa?

O retry precisa preservar o registro original.

Exemplo:

```text
08:32
Aluno registra entrada

Google indisponível
```

A presença armazenada mantém:

```text
Type = Entry
RegisteredAt = 08:32
```

Se o reprocessamento ocorrer às 14:00, o sistema envia o registro original.

## BackgroundService

O serviço responsável é:

```text
PendingAttendanceBackgroundService
```

Fluxo:

```text
Aplicação inicia
        ↓
Reprocessa pendências
        ↓
Espera intervalo configurado
        ↓
Reprocessa novamente
```

## Reprocessamento

O BackgroundService envia:

```text
ReprocessPendingAttendancesCommand
```

Fluxo:

```text
BackgroundService
       ↓
IMediator
       ↓
Command
       ↓
Handler
       ↓
PendingStore + AttendanceRepository
```

## Resultado dos retries

### Registered

Remove da fila.

### Duplicate

Remove da fila.

### DeviceAlreadyUsed

É tratado como conflito terminal para evitar retry infinito.

### Unavailable

Permanece na fila.

## Restart da aplicação

Como as pendências ficam em arquivo, elas sobrevivem ao restart.

## Limitações atuais

Arquivo JSON, locks e caches são locais à instância.

Se a aplicação rodar em várias instâncias, será necessário reavaliar essa estratégia.
