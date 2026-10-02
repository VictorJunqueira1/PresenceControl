# Domínio e regras de negócio

## Activity

`Activity` representa uma atividade onde a presença pode ser registrada.

Possui:

```text
Id
Name
Date
EntryWindow
ExitWindow
```

Exemplo:

```text
Nome: Capela Geral
Data: 30/09/2026

Entrada:
08:00 até 09:00

Saída:
11:00 até 12:00
```

## Attendance

`Attendance` representa uma presença.

Possui:

```text
ActivityId
ActivityName
StudentName
RA
DeviceId
Type
RegisteredAt
```

`Type` pode ser:

```text
Entry
Exit
```

Na planilha esses valores são apresentados como:

```text
Entrada
Saída
```

## AttendanceWindow

`AttendanceWindow` representa um intervalo válido para registro.

Exemplo válido:

```text
08:00 → 09:00
```

Exemplos inválidos:

```text
09:00 → 08:00
09:00 → 09:00
```

## Decisão automática de entrada e saída

O usuário não informa o tipo da presença.

A `Activity` decide com base na data e no horário.

## Idempotência

O sistema evita que o mesmo aluno registre a mesma etapa mais de uma vez.

```text
ActivityId + RA + AttendanceType
```

Entrada e saída são independentes.

## Controle por dispositivo

Também existe proteção baseada em:

```text
ActivityId + DeviceId + AttendanceType
```

O objetivo é impedir que o mesmo dispositivo seja utilizado para registrar várias pessoas na mesma etapa da atividade.
