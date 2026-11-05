# Sales App — Venda de Produtos (Arquitetura em Microsserviços)

Projeto 2 da disciplina de **Arquitetura de Software** — Centro Universitário SATC.
Avaliação: Sistemas distribuídos. Professor: Eduardo Cizeski Meneghel.

**Equipe:**
- Éric Dagostim do Nascimento
- Gabriel Dagostim do Nascimento
- João Paulo Marques Votri

---

## 1. Propósito do sistema

O **Sales App** é o back-end de um sistema de venda de produtos que automatiza o fluxo
completo de uma venda: registro do pedido, confirmação do pagamento, baixa de estoque,
emissão do documento fiscal e criação da expedição.

O sistema foi modelado a partir do fluxo definido no mockup do projeto (`mockup.jpg`),
em que os módulos **pagamento**, **estoque**, **documento fiscal** e **expedição**
trabalham de forma integrada.

## 2. Usuários

| Usuário | Necessidades |
|---|---|
| **Vendedor / atendente** | Registrar vendas e confirmar pagamentos dos clientes. |
| **Equipe de estoque** | Cadastrar produtos, consultar saldos e acompanhar as baixas automáticas. |
| **Setor fiscal** | Consultar os documentos fiscais emitidos automaticamente para cada venda. |
| **Equipe de logística / expedição** | Consultar as expedições geradas e atualizar o status da entrega. |
| **Gestor** | Acompanhar o fluxo completo da venda (pagamento → estoque → NF → expedição). |

## 3. Requisitos funcionais

| Código | Requisito |
|---|---|
| RF01 | O sistema deve permitir o cadastro, consulta, atualização e inativação de produtos (nome, descrição, preço e quantidade). |
| RF02 | O sistema deve permitir o registro de uma venda com dados do cliente e itens do pedido. |
| RF03 | Ao registrar a venda, o sistema deve consultar os dados do produto no serviço de estoque para obter o preço atual e validar a disponibilidade. |
| RF04 | O sistema deve permitir a confirmação do pagamento de uma venda pendente. |
| RF05 | Ao confirmar o pagamento, o sistema deve dar baixa automática na quantidade dos produtos vendidos. |
| RF06 | A baixa de estoque deve disparar automaticamente a emissão de um documento fiscal para a venda. |
| RF07 | Ao emitir o documento fiscal, o sistema deve consultar os dados do produto no serviço de estoque para compor os itens do documento. |
| RF08 | Após a emissão do documento fiscal, o sistema deve criar automaticamente a expedição da venda. |
| RF09 | O sistema deve permitir consultar os documentos fiscais emitidos. |
| RF10 | O sistema deve permitir consultar as expedições e atualizar o status da entrega (Preparando, Enviada, Entregue, Cancelada). |
| RF11 | O sistema deve impedir vendas com estoque insuficiente e pagamentos duplicados, retornando mensagens de erro claras. |

## 4. Descritivo técnico — os microsserviços

A aplicação é composta por **4 microsserviços independentes**, cada um com seu próprio
banco SQLite, expostos via REST. Um microsserviço **nunca acessa o banco do outro**:
toda integração acontece por chamadas HTTP.

| Microsserviço | Porta local | Responsabilidade | Banco |
|---|---|---|---|
| **EstoqueService** | 5001 | Catálogo de produtos, consulta de saldos e baixa de estoque. Após a baixa, orquestra a emissão do documento fiscal e a criação da expedição. | `estoque.db` |
| **PagamentoService** | 5002 | Registro de vendas (cliente + itens + valor total) e confirmação do pagamento. | `pagamento.db` |
| **DocumentoFiscalService** | 5003 | Emissão e consulta de documentos fiscais numerados automaticamente (NF-AAAA-NNNNNN). | `documentos.db` |
| **ExpedicaoService** | 5004 | Criação de expedições e acompanhamento do status de entrega. | `expedicao.db` |

### Diagrama de arquitetura

```mermaid
flowchart LR
    subgraph Fluxo da venda
        V[Venda registrada] --> P[PagamentoService<br/>porta 5002]
        P -->|1. GET api/produtos/{id}<br/>busca dados do produto| E[EstoqueService<br/>porta 5001]
        P -->|3. POST api/estoque/baixa<br/>baixa de estoque| E
        E -->|4. POST api/documentos-fiscais/emitir<br/>emissão da NF| D[DocumentoFiscalService<br/>porta 5003]
        D -->|2. GET api/produtos/{id}<br/>busca dados do produto| E
        E -->|5. POST api/expedicoes<br/>criação da expedição| X[ExpedicaoService<br/>porta 5004]
    end
```

**Fluxo completo de uma venda:**

1. O vendedor registra a venda no **PagamentoService**;
2. O PagamentoService consulta o **EstoqueService** (busca simples 1);
3. O vendedor confirma o pagamento e o PagamentoService dispara a baixa no **EstoqueService** (alteração de dados);
4. O EstoqueService dá baixa e dispara a emissão do documento fiscal no **DocumentoFiscalService** (alteração de dados);
5. O DocumentoFiscalService consulta o **EstoqueService** para compor os itens (busca simples 2);
6. Com a NF emitida, o EstoqueService dispara a criação da expedição no **ExpedicaoService** (alteração de dados);
7. A equipe de logística acompanha e atualiza o status da entrega no **ExpedicaoService**.

## 5. Integrações entre os microsserviços

| # | Origem → Destino | Tipo | Descrição | Critério do trabalho |
|---|---|---|---|---|
| 1 | Pagamento → Estoque | 🔍 Busca simples | Consulta nome, preço e disponibilidade do produto ao registrar a venda | 2.a (busca 1) |
| 2 | DocumentoFiscal → Estoque | 🔍 Busca simples | Consulta os dados do produto para compor os itens do documento fiscal | 2.a (busca 2) |
| 3 | Pagamento → Estoque | ✏️ Alteração de dados | Pagamento confirmado dispara a baixa de estoque | 2.b (alteração) |
| 4 | Estoque → DocumentoFiscal | ✏️ Alteração de dados | Baixa de estoque dispara a emissão automática do documento fiscal | Extra (mockup) |
| 5 | Estoque → Expedição | ✏️ Alteração de dados | Documento fiscal emitido dispara a criação da expedição | Extra (mockup) |

## 6. Tecnologias

- **.NET 10** (ASP.NET Core Web API + controllers)
- **Entity Framework Core 10** com **SQLite** (um banco por serviço)
- **HttpClient** (typed clients) para a comunicação **REST síncrona**
- **Swagger / OpenAPI** em todos os serviços (`/swagger`)

## 7. Como executar localmente

Pré-requisito: [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0).

Abra 4 terminais na raiz do projeto e execute:

```bash
# Terminal 1 — EstoqueService (porta 5001)
dotnet run --project src/EstoqueService

# Terminal 2 — PagamentoService (porta 5002)
dotnet run --project src/PagamentoService

# Terminal 3 — DocumentoFiscalService (porta 5003)
dotnet run --project src/DocumentoFiscalService

# Terminal 4 — ExpedicaoService (porta 5004)
dotnet run --project src/ExpedicaoService
```

O EstoqueService já inicia com 3 produtos cadastrados (seed).
Os bancos SQLite (`*.db`) são criados automaticamente na primeira execução.

Endpoints:

| Serviço | Swagger | API |
|---|---|---|
| EstoqueService | http://localhost:5001/swagger | http://localhost:5001/api/produtos e /api/estoque |
| PagamentoService | http://localhost:5002/swagger | http://localhost:5002/api/vendas |
| DocumentoFiscalService | http://localhost:5003/swagger | http://localhost:5003/api/documentos-fiscais |
| ExpedicaoService | http://localhost:5004/swagger | http://localhost:5004/api/expedicoes |

## 8. Testando o fluxo completo (cURL)

```bash
# 1) Registrar uma venda (PagamentoService busca os produtos no EstoqueService)
curl -X POST http://localhost:5002/api/vendas \
  -H "Content-Type: application/json" \
  -d '{
    "clienteNome": "João da Silva",
    "clienteEmail": "joao@email.com",
    "enderecoEntrega": "Rua das Flores, 123 - Criciúma/SC",
    "itens": [
      { "produtoId": 1, "quantidade": 2 },
      { "produtoId": 2, "quantidade": 1 }
    ]
  }'

# 2) Confirmar o pagamento (baixa estoque → emite NF → cria expedição)
curl -X POST http://localhost:5002/api/vendas/1/pagar

# 3) Conferir o resultado nos demais serviços
curl http://localhost:5001/api/estoque                    # quantidades reduzidas
curl http://localhost:5003/api/documentos-fiscais         # NF-2026-000001 emitida
curl http://localhost:5004/api/expedicoes                 # expedição criada

# 4) Atualizar o status da entrega
curl -X PUT http://localhost:5004/api/expedicoes/1/status \
  -H "Content-Type: application/json" \
  -d '{ "status": "Enviada" }'
```

## 9. Deploy no Render

Cada microsserviço é publicado como um **Web Service** separado no Render:

1. Crie 4 Web Services apontando para o mesmo repositório do GitHub;
2. Em cada um, informe o **Root Directory** da pasta do serviço (`src/EstoqueService`, etc.);
3. O Render fornece a variável `PORT` (os serviços já a utilizam automaticamente);
4. Configure as URLs de integração via variáveis de ambiente, por exemplo:
   - No PagamentoService: `Integracoes__EstoqueBaseUrl` = URL pública do EstoqueService;
   - No EstoqueService: `Integracoes__DocumentoFiscalBaseUrl` e `Integracoes__ExpedicaoBaseUrl`;
   - No DocumentoFiscalService: `Integracoes__EstoqueBaseUrl`.

> Observação: no Render os bancos SQLite são efêmeros (os dados são recriados a cada
> deploy). Para o escopo desta avaliação isso é suficiente.

## 10. Possíveis melhorias (extras)

- **Comunicação assíncrona** com fila de mensagens (RabbitMQ) no lugar da cadeia síncrona;
- **Observabilidade** com OpenTelemetry, Prometheus e Grafana;
- **Integração com IA** para sugestão de produtos ou análise de vendas;
- **Saga/compensação** para desfazer a baixa de estoque caso a emissão da NF falhe.
