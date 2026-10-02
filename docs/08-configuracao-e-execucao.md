# Configuração e execução

## Requisitos

- .NET SDK 10
- Git
- acesso a uma planilha Google
- credencial de Service Account

## Configuração

Exemplo:

```json
{
  "GoogleSheets": {
    "SpreadsheetId": "ID_DA_PLANILHA",
    "CredentialsFilePath": "CAMINHO_DO_ARQUIVO_DE_CREDENCIAIS",
    "ActivitiesSheetName": "Activities"
  },
  "Application": {
    "PublicBaseUrl": "https://localhost:5001"
  },
  "PendingAttendance": {
    "FilePath": "App_Data/pending-attendances.json",
    "RetryIntervalSeconds": 30
  }
}
```

Não versionar credenciais reais.

## SpreadsheetId

Na URL:

```text
https://docs.google.com/spreadsheets/d/ID_DA_PLANILHA/edit
```

use apenas o trecho `ID_DA_PLANILHA`.

## CredentialsFilePath

Caminho para o JSON da Service Account.

Esse arquivo não deve ser enviado ao Git.

## PublicBaseUrl

URL usada para gerar o QR Code.

Se o QR Code for aberto em outro dispositivo, evite `localhost`.

## Restaurando dependências

```bash
dotnet restore ControlePresenca.slnx
```

## Compilando

```bash
dotnet build ControlePresenca.slnx
```

## Executando testes

```bash
dotnet test ControlePresenca.slnx
```

## Executando a aplicação

```bash
dotnet run --project src/ControlePresenca.Web/ControlePresenca.Web.csproj
```

## Executando a simulação de carga

```bash
dotnet run --project tests/ControlePresenca.LoadTests/ControlePresenca.LoadTests.csproj -- 1000
```
