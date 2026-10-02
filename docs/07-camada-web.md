# Camada Web

A Web utiliza Blazor com Interactive Server.

Sua responsabilidade é:

```text
mostrar informações
receber dados do usuário
executar Commands e Queries
apresentar resultados
```

Ela não contém regra de negócio.

## Organização

```text
Components/
├── Layouts/
└── Pages/

Configurations/

Middleware/

wwwroot/
```

As páginas relacionadas a uma feature ficam próximas.

## Code-behind

Páginas com comportamento utilizam:

```text
Pagina.razor
Pagina.razor.cs
Pagina.razor.css
```

Responsabilidades:

```text
.razor
→ markup

.razor.cs
→ comportamento de apresentação

.razor.css
→ estilo
```

O `.razor.cs` pode controlar loading, tratar eventos, enviar Commands/Queries, exibir mensagens e interagir com JavaScript.

Ele não deve acessar Repository diretamente ou implementar regra de negócio.

## Página de presença

Rota:

```text
/presenca/{ActivityId}
```

Responsável por carregar a atividade, capturar Nome/RA, obter DeviceId, enviar o Command e mostrar o resultado.

## Página de QR Code

Rota:

```text
/qrcode/{ActivityId}
```

Responsável por carregar a atividade, montar a URL pública, gerar o QR Code, exibir tela cheia e permitir impressão.

## Configurations

A configuração da Web fica em:

```text
Configurations/
```

A ideia é manter o `Program.cs` pequeno.

## Rotas de erro

404:

```text
/not-found
```

Erro interno:

```text
/error
```

A página de erro não deve mostrar stack trace, credenciais ou detalhes internos.
