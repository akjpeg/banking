# Banking API

Uma API bancária simples construída com .NET 10 para gerenciamento de contas e transferências de dinheiro.

## Funcionalidades

- **Gerenciamento de Contas** - Cadastro, login, visualização de detalhes da conta
- **Depósitos e Saques** - Adicionar ou retirar fundos das contas
- **Transferências** - Transferir dinheiro entre contas
- **Histórico de Transações** - Visualizar todas as transações da conta

## Tecnologias

- **.NET 10** - Web API
- **PostgreSQL** - Banco de dados
- **Entity Framework Core** - ORM
- **Docker** - Containerização
- **xUnit** - Testes unitários
- **Swagger** - Documentação da API

## Arquitetura

O projeto segue uma arquitetura de **monolito modular** com separação clara de responsabilidades:
```
src/
├── Api/                          # Camada de API (Controllers, DTOs)
├── Accounts/
│   ├── Accounts.Domain/          # Entidades e regras de domínio
│   ├── Accounts.Application/     # Serviços, lógica de negócio, interfaces de repositório
│   └── Accounts.Infrastructure/  # EF Core, implementações de repositório
├── AccountTransactions/
│   ├── AccountTransactions.Domain/
│   ├── AccountTransactions.Application/
│   └── AccountTransactions.Infrastructure/
├── Transfers/
│   ├── Transfers.Application/
│   └── Transfers.Infrastructure/
└── Shared/                       # Contratos compartilhados, exceções
```

---

### Diagrama de Entidades
```mermaid
erDiagram
    Account ||--o{ AccountTransaction : has
    Account {
        guid Id PK
        string AccountNumber UK
        string Name
        string Email UK
        string PasswordHash
        decimal Balance
        datetime CreatedAt
        datetime UpdatedAt
        uint Version
    }
    
    AccountTransaction {
        guid Id PK
        guid AccountId FK
        guid FromAccountId FK
        guid ToAccountId FK
        decimal Amount
        enum Type
        enum Status
        datetime CreatedAt
        datetime UpdatedAt
    }
```

### Invariantes

**Account:**
- Saldo não pode ser negativo
- Valor de crédito deve ser maior que zero
- Valor de débito deve ser maior que zero
- Não é possível debitar mais que o saldo disponível
- Email deve ser único
- Número da conta deve ser único

**AccountTransaction:**
- Valor deve ser maior que zero
- Não é possível transferir para a mesma conta
- Transferências e operações de depósito e saque são criadas com status `Pending`

### Arquitetura do Sistema
```mermaid
flowchart LR
    Client[Cliente] --> API[API Layer]
    API --> Accounts[Accounts Module]
    API --> Transactions[Transactions Module]
    API --> Transfers[Transfers Module]
    Accounts --> DB[(PostgreSQL)]
    Transactions --> DB
    Transfers --> Accounts
    Transfers --> Transactions
```
---

## Decisões de Design

### Monolito Modular
Foi escolhida uma arquitetura de monolito modular para manter a simplicidade de deployment enquanto mantemos uma separação clara de responsabilidades. Cada módulo (Accounts, AccountTransactions, Transfers) pode ser extraído para um microserviço no futuro, se necessário.

### Simplificação de Entidades
A decisão de manter informações de usuário (nome, email) junto das informações de conta foi proposital. Em um sistema bancário real, seria comum separar `User` e `Account` para permitir:
- Um usuário com múltiplas contas
- Contas conjuntas (múltiplos usuários, uma conta)
- Diferentes tipos de conta (corrente, poupança)

Para este projeto, a simplicidade foi priorizada, evitando complexidade desnecessária para o escopo proposto.

### Decimal para Valores Monetários
Utilização de `decimal` ao invés de `float` ou `double` para evitar erros de arredondamento em cálculos financeiros. O PostgreSQL armazena como `NUMERIC(18,2)`.

### Controle de Concorrência Otimista
Implementação de controle de concorrência usando um campo `Version` que é incrementado a cada atualização. Isso previne condições de corrida em operações simultâneas na mesma conta.

### Transações Duplicadas para Transferências
Cada transferência cria duas transações (TransferOut e TransferIn) para que cada conta tenha seu histórico completo de movimentações, facilitando auditoria e consultas.

## Melhorias Não Implementadas

Por simplicidade e limitação de tempo, algumas funcionalidades recomendadas para produção não foram implementadas:

### Cache

Não há cache implementado. **Redis** pode ajudar a melhorar performance em consultas de informações frequentes:
- Consulta de dados de conta (nome, email, número da conta)
- Histórico de transações (com invalidação a cada nova transação)
- Blacklist de tokens JWT

### Rate Limiting

Não há limitação de requisições, o que expõe a API a riscos:
- Endpoints públicos (`/login`, `/register`) vulneráveis a ataques de força bruta
- Possibilidade de sobrecarga do banco com requisições repetidas
- Sem proteção contra abuso por automação

### Idempotency Keys

Não há controle de idempotência. Endpoints de depósito, saque e transferência deveriam exigir um header `Idempotency-Key` para evitar operações duplicadas em caso de retry ou falha de rede.

### Tratamento Global de Exceções

Não há implementação de middleware global para tratamento de exceções.
Existem exceções não tratadas no código, o que pode afetar a estabilidade da API.
A aplicação se beneficiaria de um ponto centralizado para capturar erros, padronizar respostas de erro e evitar a exposição de detalhes internos
(como stack traces) ao cliente.

### Paginações e filtros

Alguns endpoints, como de listagem de transações podem se beneficiar de paginações e filtros, evitando retorno de grande volume de dados em uma requisição.

### Logs

Não há implementações de logs das operações.
Para o escopo do projeto isso não é crítico, mas em um ambiente real seriam importantes para observabilidade, auditoria e troubleshooting.

Exemplos de logs relevantes:
- Criação de conta e login (sem dados sensíveis)
- Operações financeiras (depósito, saque, transferência)
- Falhas de autorização e validação
- Conflitos de concorrência

### Evolução para Microsserviços

A arquitetura modular facilita migração para microsserviços. Principais mudanças:

| Aspecto | Atual (Monolito) | Microsserviços |
|---------|------------------|----------------|
| Comunicação | In-process | HTTP/gRPC, mensageria (RabbitMQ) |
| Banco de dados | Compartilhado | Um banco por serviço |
| Transações | ACID única | Padrão Saga (consistência eventual) |
| Deploy | Artefato único | Deploy independente por serviço |

**Desafio principal:** A transferência entre contas atualmente usa uma transação única. Com microsserviços, seria necessário uma atenção para coordenar a transação de forma distribuída.


## Começando

### Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/get-started) (opcional)
- [PostgreSQL](https://www.postgresql.org/) (se não usar Docker)

### Executando com Docker

1. Clone o repositório:
```bash
   git clone https://github.com/akjpeg/banking-api.git
   cd banking-api
```

2. Crie o arquivo `.env` a partir do exemplo:
```bash
   cp .env.example .env
```

3. Inicie a aplicação:
```bash
   docker-compose up --build
```

4. Acesse a API:
    - API: http://localhost:5000
    - Swagger: http://localhost:5000/swagger

## Endpoints da API

### Autenticação

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| POST | `/api/accounts/register` | Cadastrar uma nova conta |
| POST | `/api/accounts/login` | Login e receber token JWT |

### Operações de Conta

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/api/accounts/me` | Obter detalhes da conta atual |
| GET | `/api/accounts/me/balance` | Obter saldo atual |
| GET | `/api/accounts/me/transactions` | Obter histórico de transações |
| POST | `/api/accounts/me/deposit` | Depositar fundos |
| POST | `/api/accounts/me/withdraw` | Sacar fundos |

### Transferências

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| POST | `/api/transfers` | Transferir dinheiro para outra conta |

### Administração

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/api/admin/accounts` | Obter todas as contas |
| GET | `/api/admin/accounts/{id}` | Obter conta por ID |
| DELETE | `/api/admin/accounts/{id}` | Deletar conta |

**Nota**: estes endpoints não possuem autorização e foram construídos para facilitar testes durante desenvolvimento

## Collection do Postman

Importe os arquivos na raiz do projeto para o Postman:
- `Banking.postman_collection.json` - Endpoints da API
- `Banking.postman_environment.json` - Variáveis de ambiente

Selecione o environment **Banking** no Postman antes de executar os requests.

## Executando Testes
```bash
# Executar todos os testes
dotnet test

# Executar com verbosidade
dotnet test --verbosity normal

# Executar projeto específico
dotnet test tests/Accounts.Domain.Tests
```
**Nota**: Os testes foram desenvolvidos com apoio de ferramentas de IA e
validados manualmente. Não houve adoção formal de TDD devido ao escopo
e ao tempo do desafio.

## Estrutura do Projeto
```
Banking/
├── src/
│   ├── Api/                              # Web API
│   ├── Accounts/                         # Módulo de contas
│   ├── AccountTransactions/              # Módulo de transações
│   ├── Transfers/                        # Módulo de transferências
│   └── Shared/                           # Componentes compartilhados
├── tests/
│   ├── Accounts.Domain.Tests/
│   ├── Accounts.Application.Tests/
│   ├── AccountTransactions.Domain.Tests/
│   ├── AccountTransactions.Application.Tests/
│   └── Api.Tests/
├── docker-compose.yml
├── .env.example
└── BankingApi.sln
```

## Configuração

### Variáveis de Ambiente

| Variável | Descrição | Padrão |
|----------|-----------|--------|
| `POSTGRES_HOST` | Host do banco de dados | `localhost` |
| `POSTGRES_USER` | Usuário do banco de dados | `postgres` |
| `POSTGRES_PASSWORD` | Senha do banco de dados | `postgres` |
| `JWT_KEY` | Chave de assinatura JWT | - |
| `JWT_ISSUER` | Emissor do JWT | `BankingApi` |
| `JWT_AUDIENCE` | Audiência do JWT | `BankingApi` |
