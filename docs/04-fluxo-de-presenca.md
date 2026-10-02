# Fluxo de registro de presença

## Fluxo normal

```text
Usuário abre /presenca/{ActivityId}
                ↓
Web envia GetActivityByIdQuery
                ↓
Activity encontrada
                ↓
Usuário informa Nome + RA
                ↓
Web envia RegisterAttendanceCommand
                ↓
Handler carrega Activity
                ↓
Activity cria Attendance
                ↓
Repository tenta registrar
                ↓
Google Sheets
```

Se tudo funcionar:

```text
AttendancePersistenceStatus.Registered
                ↓
RegisterAttendanceStatus.Success
                ↓
Mensagem de sucesso
```

## Atividade não encontrada

A página informa ao usuário que a atividade não foi localizada.

## Janela fechada

Se o horário atual não estiver em nenhuma janela válida:

```text
AttendanceWindowClosed
```

Nenhuma presença é enviada ao Google Sheets.

## Registro duplicado

```text
AttendancePersistenceStatus.Duplicate
```

A Application retorna:

```text
AlreadyRegistered
```

## Dispositivo já utilizado

```text
AttendancePersistenceStatus.DeviceAlreadyUsed
```

A Web apresenta uma mensagem informando que o dispositivo já foi utilizado.

## Google Sheets indisponível

Caso ocorra falha de comunicação, o Repository retorna:

```text
Unavailable
```

A Application tenta salvar a presença localmente.

Se conseguir:

```text
StoredForRetry
```

Se até o armazenamento local falhar:

```text
RetryRequired
```
