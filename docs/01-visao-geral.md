# Visão geral

## Objetivo

O Controle de Presença permite registrar entrada e saída de participantes em atividades.

Cada atividade possui:

- identificador;
- nome;
- data;
- horário permitido para entrada;
- horário permitido para saída.

As atividades são cadastradas no Google Sheets.

## Como funciona?

```text
Atividade
   ↓
QR Code
   ↓
Participante acessa o formulário
   ↓
Informa nome e RA
   ↓
Sistema identifica entrada ou saída
   ↓
Presença é registrada
```

O participante não escolhe manualmente se está registrando entrada ou saída.

O sistema decide isso utilizando o horário atual e as janelas configuradas para a atividade.

## Tecnologias principais

O projeto utiliza:

- .NET 10;
- ASP.NET Core;
- Blazor com Interactive Server;
- Google Sheets como armazenamento externo;
- xUnit para testes;
- BackgroundService para reprocessamento;
- JSON para armazenamento temporário de registros pendentes.

## Principais funcionalidades

### Registro de presença

O usuário acessa:

```text
/presenca/{ActivityId}
```

O sistema carrega a atividade e permite registrar a presença.

### QR Code

Existe uma página para exibir o QR Code da atividade:

```text
/qrcode/{ActivityId}
```

O QR Code direciona para a página de presença.

### Controle de duplicidade

O sistema evita registros duplicados considerando:

```text
ActivityId + RA + Tipo
```

Também existe proteção por dispositivo:

```text
ActivityId + DeviceId + Tipo
```

Entrada e saída são tratadas separadamente.

### Reprocessamento automático

Quando o Google Sheets está indisponível:

```text
Registro
   ↓
Google Sheets falha
   ↓
Presença salva localmente
   ↓
BackgroundService
   ↓
Google volta
   ↓
Presença enviada
```

O usuário não precisa permanecer na página esperando o Google retornar.
