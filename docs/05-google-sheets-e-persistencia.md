# Google Sheets e persistência

O Google Sheets é utilizado como armazenamento externo do sistema.

## Aba de atividades

Por padrão:

```text
Activities
```

A configuração pode ser alterada em `GoogleSheets:ActivitiesSheetName`.

A leitura considera as colunas de `A` até `G`.

| Coluna | Conteúdo |
|---|---|
| A | ActivityId |
| B | Name |
| C | Date |
| D | EntryStart |
| E | EntryEnd |
| F | ExitStart |
| G | ExitEnd |

Exemplo:

| ActivityId | Name | Date | EntryStart | EntryEnd | ExitStart | ExitEnd |
|---|---|---|---|---|---|---|
| capela-001 | Capela Geral | 30/09/2026 | 08:00 | 09:00 | 11:00 | 12:00 |

## Abas de presença

As presenças são separadas por:

```text
Nome da atividade - Data
```

Exemplo:

```text
Capela Geral - 30-09-2026
```

Caracteres incompatíveis são normalizados antes da criação da aba.

## Atividades com mesmo nome e data

Duas atividades com o mesmo nome e mesma data podem compartilhar a mesma aba.

A identificação continua sendo feita pelo `ActivityId`.

## Estrutura da aba de presença

| Coluna | Conteúdo |
|---|---|
| A | Data |
| B | Hora |
| C | ActivityId |
| D | Atividade |
| E | Nome |
| F | RA |
| G | TipoRegistro |
| H | DeviceId |

## Idempotência

O Repository mantém chaves em memória para RA e DeviceId e protege o acesso à mesma aba com `SemaphoreSlim`.

## Falha ambígua

Pode acontecer:

```text
Sistema envia linha
↓
Google grava
↓
conexão cai antes da resposta
```

Neste caso o cache da aba é invalidado.

Na próxima tentativa, a planilha é lida novamente. Se a linha já existir, o resultado será `Duplicate`.
