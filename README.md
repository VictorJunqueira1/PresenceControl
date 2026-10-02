# Controle de Presença

Sistema para registro de presença em atividades por meio de QR Code.

A aplicação permite cadastrar atividades no Google Sheets, gerar um QR Code para cada atividade e registrar entradas e saídas dos participantes.

O projeto também possui tratamento para indisponibilidade do Google Sheets. Quando uma presença não consegue ser enviada, ela é armazenada localmente e reprocessada automaticamente.

## Documentação

A documentação completa está disponível em:

➡️ [docs/README.md](docs/README.md)

## Estrutura do projeto

```text
src/
├── ControlePresenca.Domain
├── ControlePresenca.Application
├── ControlePresenca.Infrastructure
└── ControlePresenca.Web

tests/
├── ControlePresenca.Tests
└── ControlePresenca.LoadTests

docs/
└── Documentação técnica
```