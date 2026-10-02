# Convenções e troubleshooting

## Organização do código

Prefira organizar código por responsabilidade e feature.

Evite pastas genéricas como `Helpers`, `Utils`, `Services` e `Common` quando existir um nome mais específico.

## Domain

Regras de negócio ficam no Domain.

## Application

Casos de uso ficam na Application.

## Infrastructure

Detalhes externos ficam na Infrastructure.

## Web

A Web não deve acessar Repository diretamente.

Correto:

```text
Web
 ↓
IMediator
 ↓
Command / Query
```

## Razor

Para páginas com comportamento:

```text
Pagina.razor
Pagina.razor.cs
Pagina.razor.css
```

Não crie `.razor.cs` vazio.

## Commits

Padrão:

```text
[tipo] Verbo no gerúndio + descrição.
```

Exemplos:

```text
[feat] Adicionando reprocessamento automático de presenças.
[fix] Corrigindo duplicidade durante reprocessamento.
[refactor] Organizando componentes da camada Web.
[test] Adicionando cenários de concorrência.
[docs] Adicionando documentação técnica do projeto.
```

## DLL bloqueada durante build

Erros como `MSB3021` e `MSB3027` normalmente significam que a aplicação Web ainda está executando.

Pare a aplicação no Visual Studio com:

```text
Shift + F5
```

ou pelo PowerShell.

## ProjectReference não encontrado

Projetos de `src` são irmãos.

Projetos dentro de `tests` normalmente apontam para `../../src/...`.

## Google Sheets não conecta

Verifique:

- `SpreadsheetId`;
- `CredentialsFilePath`;
- permissão da Service Account;
- nome da aba `Activities`.

## QR Code abre endereço inválido

Confira `Application:PublicBaseUrl`.

Se estiver testando com celular, a URL precisa ser acessível pelo celular.

## Presenças acumuladas

Verifique os logs do `PendingAttendanceBackgroundService`.

Quando o Google voltar, os registros devem ser reprocessados automaticamente.

## Pontos de atenção atuais

### Escala horizontal

Locks, caches e JSON são locais à instância.

### Google Sheets

Google Sheets atende ao cenário atual, mas não é um banco de dados transacional.

### Credenciais

Nunca versionar credenciais, tokens ou secrets.
