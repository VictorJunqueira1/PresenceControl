# Testes e simulação de carga

Existem dois projetos:

```text
tests/
├── ControlePresenca.Tests
└── ControlePresenca.LoadTests
```

## Testes automatizados

`ControlePresenca.Tests` valida comportamentos específicos.

Os testes cobrem principalmente:

- registro normal;
- janela de presença;
- indisponibilidade;
- armazenamento pendente;
- reprocessamento;
- duplicidade;
- DeviceId;
- persistência JSON;
- concorrência;
- falha ambígua após append.

Execute:

```bash
dotnet test ControlePresenca.slnx
```

## LoadTests

O projeto `ControlePresenca.LoadTests` simula concorrência e recuperação.

Ele utiliza o Repository real com um cliente do Google em memória.

Isso permite testar locks, cache, idempotência, retry, restart e reprocessamento sem escrever milhares de linhas em uma planilha real.

## Cenários

- vários alunos ao mesmo tempo;
- mesmo aluno simultaneamente;
- mesmo dispositivo simultaneamente;
- Google indisponível;
- acúmulo de pendências;
- restart;
- drenagem da fila;
- duplicidade durante retry;
- append remoto com resposta perdida.

## Resultados observados

### 100 operações

| Cenário | Tempo | Throughput |
|---|---:|---:|
| Alunos diferentes | 14 ms | 6.778 ops/s |
| Mesmo aluno | 1 ms | 98.270 ops/s |
| Mesmo dispositivo | < 1 ms | 100.000 ops/s |
| Indisponibilidade + restart | 287 ms | 347 ops/s |
| Duplicidade no retry | 4 ms | 223 ops/s |
| Append com resposta perdida | < 1 ms | 1.000 ops/s |

### 500 operações

| Cenário | Tempo | Throughput |
|---|---:|---:|
| Alunos diferentes | 16 ms | 30.206 ops/s |
| Mesmo aluno | 1 ms | 355.821 ops/s |
| Mesmo dispositivo | 1 ms | 427.936 ops/s |
| Indisponibilidade + restart | 886 ms | 282 ops/s |
| Duplicidade no retry | 4 ms | 222 ops/s |
| Append com resposta perdida | 1 ms | 965 ops/s |

### 1.000 operações

| Cenário | Tempo | Throughput |
|---|---:|---:|
| Alunos diferentes | 16 ms | 60.317 ops/s |
| Mesmo aluno | 1 ms | 521.295 ops/s |
| Mesmo dispositivo | 1 ms | 674.992 ops/s |
| Indisponibilidade + restart | 996 ms | 251 ops/s |
| Duplicidade no retry | 4 ms | 237 ops/s |
| Append com resposta perdida | 1 ms | 927 ops/s |

Em todas as execuções, nenhuma invariante esperada foi violada.

## Como interpretar

Esses números não representam a capacidade real do Google Sheets porque o cliente externo utilizado no teste é em memória.

O objetivo principal é validar concorrência, race conditions, idempotência, retry e recuperação.

## Evolução das métricas

O simulador deverá evoluir para apresentar:

```text
Total
Success
Duplicate
Conflict
Pending
Average
p95
p99
Throughput
```

Também devemos garantir que o throughput utilize exatamente a quantidade de operações executadas em cada cenário.
