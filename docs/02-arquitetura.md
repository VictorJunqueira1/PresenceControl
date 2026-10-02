# Arquitetura

## Estrutura

O projeto está dividido em quatro camadas principais:

```text
src/
├── ControlePresenca.Domain
├── ControlePresenca.Application
├── ControlePresenca.Infrastructure
└── ControlePresenca.Web
```

A direção esperada das dependências é:

```text
Web
 ├── Application
 └── Infrastructure

Infrastructure
 ├── Application
 └── Domain

Application
 └── Domain

Domain
 └── nenhuma outra camada
```

## Domain

Contém as regras de negócio.

Exemplos:

- `Activity`
- `Attendance`
- `AttendanceWindow`
- `AttendanceType`

O Domain não conhece Google Sheets, Blazor, JSON ou BackgroundService.

## Application

Contém os casos de uso.

Exemplos:

- `RegisterAttendanceCommand`
- `RegisterAttendanceCommandHandler`
- `ReprocessPendingAttendancesCommand`
- `ReprocessPendingAttendancesCommandHandler`
- `GetActivityByIdQuery`
- `GetActivityByIdQueryHandler`

A Application coordena o fluxo, mas evita implementar regras que pertencem ao Domain.

## Infrastructure

Implementa integrações externas.

Exemplos:

- `GoogleSheetsAttendanceRepository`
- `GoogleSheetsActivityRepository`
- `GoogleSheetsClient`
- `JsonPendingAttendanceStore`
- `PendingAttendanceBackgroundService`
- `SaoPauloDateTimeProvider`

## Web

Responsável pela interface.

A Web não acessa repositories diretamente.

```text
Página
  ↓
IMediator
  ↓
Command ou Query
  ↓
Handler
```

## CQRS

Leitura:

```text
Web
 ↓
Query
 ↓
QueryHandler
 ↓
QueryRepository
 ↓
Model
```

Alteração:

```text
Web
 ↓
Command
 ↓
CommandHandler
 ↓
Domain
 ↓
Repository
```

## Mediator

O projeto possui um Mediator simples próprio.

Exemplo:

```csharp
await Mediator.Send(command);
```

## DDD

As regras ficam próximas dos objetos que representam o negócio.

A decisão sobre entrada e saída pertence à `Activity`.

## Tell Don't Ask

Em vez de perguntar vários dados à atividade e tomar uma decisão fora dela, preferimos pedir que ela execute a responsabilidade:

```csharp
activity.CreateAttendance(...);
```

A própria `Activity` valida data, janela, tipo e cria a presença.
