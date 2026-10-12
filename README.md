# CorvuxIAM — Gestão Corporativa de Identidades e Acessos (IAM)

<div align="center">

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp&logoColor=white)](https://docs.microsoft.com/dotnet/csharp/)
[![React Native](https://img.shields.io/badge/React%20Native-Expo-61DAFB?logo=react&logoColor=black)](https://reactnative.dev/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC292B?logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![CI/CD](https://img.shields.io/badge/GitHub%20Actions-CI%2FCD-2088FF?logo=githubactions&logoColor=white)](https://github.com/Kaaiske/PROJETO_PIMIV_CORVUX/actions)
[![Tests](https://img.shields.io/badge/Tests-19%20Passed-brightgreen?logo=checkmarx&logoColor=white)](CorvusIAM.Tests/)
[![Production](https://img.shields.io/badge/Production-corvuxiam.tech-success?logo=googlechrome&logoColor=white)](https://corvuxiam.tech)

**Solução Corporativa Integrada para Transformação Digital, Governança e Segurança da Informação**

🌐 **Ambiente Oficial em Produção:** [https://corvuxiam.tech](https://corvuxiam.tech)  
📂 **Repositório Oficial:** [github.com/Kaaiske/PROJETO_PIMIV_CORVUX](https://github.com/Kaaiske/PROJETO_PIMIV_CORVUX)

</div>

---

## 📌 Apresentação Acadêmica — PIM IV (UNIP)

* **Instituição:** Universidade Paulista — UNIP
* **Curso:** Curso Superior de Tecnologia em Análise e Desenvolvimento de Sistemas (ADS)
* **Semestre:** 4º Semestre
* **Tema Oficial:** *Desenvolvimento e Implantação de uma Solução Tecnológica Corporativa Integrada para Apoiar a Transformação Digital de uma Empresa (Continuidade do PIM III)*
* **Equipe de Desenvolvimento:**
  * Caique Brito Mascarenhas Silva — RA: H74EHC0
  * Gabriel Matias Alves — RA: H584HF3
  * Jheferson Herrera Choque — RA: H751853
  * Nicolas Condorena dos Santos — RA: H712EG8
  * Raphael F. Martins da Silva — RA: F362051
  * Victor Balbino de O. Martins — RA: R575GA9

---

## 📖 Visão Geral do Projeto

O **CorvuxIAM** é uma plataforma corporativa completa de **Gestão de Identidades e Acessos (IAM - Identity & Access Management)** projetada para garantir segurança, rastreabilidade, governança digital e mobilidade para organizações modernas.

A plataforma resolve dores críticas de segurança empresarial:
- Centralização de credenciais e eliminação de privilégios excessivos.
- Autenticação e autorização robustas por papéis (*Role-Based Access Control* - RBAC) e políticas de menor privilégio.
- Rastreamento ininterrupto de ações via **Logs de Auditoria** gerados por aplicação e por **Triggers de Banco de Dados**.
- Atendimento e solicitações internas com um módulo ágil de **Chamados de Suporte**.
- Acesso multiplataforma via **Painel Web Administrativo**, **PWA Offline-First** e aplicativo móvel nativo em **React Native**.

---

## 🏛️ Matriz de Conformidade com o Manual do PIM IV

| Etapa / Disciplina | Implementação no Projeto | Status |
| :--- | :--- | :---: |
| **Etapa 1: Caracterização da Organização** | Modelo organizacional com departamentos (TI, RH, Contabilidade, HelpDesk) e fluxos de negócio mapeados. | 🟢 100% |
| **Etapa 2: Empreendedorismo em TI** | Proposta de valor centrada em redução de fraudes, conformidade com LGPD/ISO 27001 e baixo custo operacional. | 🟢 100% |
| **Etapa 3: Relações Étnico-Raciais e Diversidade** | Acessibilidade universal (WCAG 2.2, alto contraste, leitor de tela, rótulos semânticos) e regras de acesso justas sem viés discriminatório. | 🟢 100% |
| **Etapa 4: Solução Web (.NET Web)** | ASP.NET Core 8 MVC, Razor Views, autenticação híbrida (Cookies + JWT), Rate Limiting, Turnstile e APIs REST. | 🟢 100% |
| **Etapa 5: Solução Mobile** | App nativo em **React Native (Expo)** em `CorvusIAM.Mobile` e **PWA Offline-First** (`/Mobile`) com IndexedDB e Service Worker. | 🟢 100% |
| **Etapa 6: Arquitetura de Software (.NET)** | Organização em camadas desacopladas (Controllers, Services, Repositories, Entities, DTOs, Middlewares, DI). | 🟢 100% |
| **Etapa 7: Banco de Dados** | SQL Server 2022 / SQLite, modelo físico normalizado, Stored Procedure `sp_RegistrarLog` e Trigger `trg_AuditoriaUsuarios`. | 🟢 100% |
| **Etapa 8: Cloud Computing & DevOps** | Dockerfile, `docker-compose.yml`, pipeline automatizado no GitHub Actions, Health Checks (`/health`) e métricas Prometheus. | 🟢 100% |
| **Etapa 9: Gerenciamento Ágil de Projetos** | Planejamento em 4 Sprints, Product Backlog priorizado, Sprint Backlogs e Quadro Kanban integrado. | 🟢 100% |

---

## 🛠️ Tecnologias Utilizadas

### Backend & APIs
- **C# / .NET 8 (LTS)** — ASP.NET Core MVC e Web API RESTful
- **Entity Framework Core 8** — Mapeamento objeto-relacional (ORM) e migrations
- **Autenticação:** Cookie Authentication (Web) + JWT Bearer Tokens (Mobile/API)
- **Segurança:** BCrypt para hash de senhas, Cloudflare Turnstile CAPTCHA, HaveIBeenPwned API (verificação de senhas vazadas), MaxMind/IP Geolocation, Rate Limiter (5 req/min no login)
- **Documentação de API:** Swagger / OpenAPI 3.0 com suporte ao domínio `https://corvuxiam.tech`

### Frontend Web
- **Razor Views (ASP.NET Core)**
- **Bootstrap 5.3 & CSS Moderno** (Responsivo e Acessível)
- **JavaScript Moderno (ES6+)** com proteção Anti-Forgery Token automática

### Frontend Mobile
- **React Native 0.74 / Expo 51 (TypeScript)** — Aplicativo mobile com Axios, Expo SecureStore e React Navigation
- **Progressive Web App (PWA)** — Service Worker, Web App Manifest, Cache Storage e fila offline via IndexedDB

### Banco de Dados
- **Microsoft SQL Server 2022** (Ambiente de Produção e Docker)
- **SQLite** (Ambiente de Desenvolvimento Rápido e Testes Automatizados)
- **Recursos Avançados:**
  - Stored Procedure `sp_RegistrarLog`
  - Trigger `trg_AuditoriaUsuarios`

### DevOps & Nuvem
- **Docker & Docker Compose** (Multi-container: Web Application + SQL Server)
- **GitHub Actions** (CI/CD: Restore, Build, Unit Testing, Docker Image Build)
- **Health Checks** (`/health`) & **Prometheus Metrics** (`/metrics`)

---

## 📂 Estrutura da Solução

```text
PROJETO_PIMIV_CORVUX/
├── .github/
│   └── workflows/
│       └── ci.yml                     # Pipeline automatizado de CI/CD (GitHub Actions)
├── CorvusIAM/                         # Aplicação Principal (ASP.NET Core 8 MVC + REST API)
│   ├── Controllers/                   # Controllers Web (Admin, Auth, Home, Portal, Mobile)
│   │   └── Api/                       # Controllers de APIs REST (Usuarios, Perfis, Politicas, Chamados, etc.)
│   ├── Data/                          # Contextos do EF Core (ApplicationDbContext, SqlServer, Sqlite)
│   ├── Entities/                      # Entidades de Domínio (Usuario, Perfil, Permissao, Auditoria, Chamado)
│   ├── Interfaces/                    # Contratos de Serviços e Repositórios
│   ├── Middlewares/                   # Middleware Global de Tratamento de Exceções
│   ├── Migrations/                    # Migrations estruturadas para SQL Server e SQLite
│   ├── Models/DTOs/                   # Data Transfer Objects para APIs
│   ├── Repositories/                  # Camada de Repositório desacoplada
│   ├── Services/                      # Regras de Negócio (Auth, Auditoria com Stored Procedure, Senhas, etc.)
│   ├── Views/                         # Telas Razor MVC (Admin, Auth, Portal, Mobile)
│   ├── wwwroot/                       # Arquivos estáticos, CSS, JS e módulo PWA (`/mobile`)
│   ├── DatabaseScript.sql             # Script SQL físico completo (Tabelas, FKs, Procedure, Trigger, Seeds)
│   ├── Dockerfile                     # Construção da imagem Docker do backend
│   └── Program.cs                     # Inicialização da aplicação, DI, CORS, Swagger e Pipeline HTTP
├── CorvusIAM.Mobile/                  # Aplicativo Mobile Nativo (React Native / Expo / TypeScript)
│   ├── src/
│   │   ├── navigation/                # Navegação por Stack (AppNavigator)
│   │   ├── screens/                   # Telas Mobile (Login, Home, Access, Account, Policies, Support)
│   │   └── services/api.ts            # Cliente HTTP Axios configurado para https://corvuxiam.tech
│   ├── App.tsx                        # Ponto de entrada do aplicativo mobile
│   └── package.json                   # Dependências do app mobile
├── CorvusIAM.Tests/                   # Bateria de Testes Automatizados (xUnit / Moq)
│   ├── Controllers/                   # Testes unitários de controllers
│   ├── Integration/                   # Testes de integração de endpoints
│   ├── Repositories/                  # Testes da camada de dados
│   └── Services/                      # Testes de regras de negócio e autenticação
├── docker-compose.yml                 # Orquestração de containers (ASP.NET Core + SQL Server 2022)
├── .env.example                       # Modelo de variáveis de ambiente
├── PIM4_corrigido.docx                # Documento Acadêmico Oficial nos padrões ABNT
└── README.md                          # Documentação técnica central do repositório
```

---

## 🚀 Como Executar o Projeto Localmente

### Pré-requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) (para o módulo Mobile)
- [Docker & Docker Compose](https://www.docker.com/) (opcional, para execução containerizada)

---

### Opção 1: Execução com .NET CLI (Desenvolvimento Rápido)

1. Clone o repositório:
```bash
git clone https://github.com/Kaaiske/PROJETO_PIMIV_CORVUX.git
cd PROJETO_PIMIV_CORVUX
```

2. Restaure as dependências e compile a solução:
```bash
dotnet restore
dotnet build --configuration Release
```

3. Execute os testes automatizados:
```bash
dotnet test
```

4. Execute o servidor Web:
```bash
cd CorvusIAM
dotnet run
```
* A aplicação estará disponível em: `http://localhost:5052`
* Documentação Swagger: `http://localhost:5052/swagger`
* Health Check: `http://localhost:5052/health`

---

### Opção 2: Execução com Docker Compose (Produção / SQL Server)

1. Crie o arquivo `.env` a partir do modelo:
```bash
cp .env.example .env
```

2. Inicie os containers com auto-restart e migração resiliente:
```bash
docker compose up --build -d
```
* O backend (.NET 8) e o banco SQL Server 2022 iniciarão automaticamente.
* Aplicação Web no host: `http://localhost:8080` (redirecionada via Nginx para `https://corvuxiam.tech`).

---

### Opção 3: Deploy em Servidor Linux (VPS com Nginx e SSL)

Para expor a aplicação no domínio oficial [https://corvuxiam.tech](https://corvuxiam.tech):

1. Configure o Nginx como Proxy Reverso em `/etc/nginx/sites-available/corvuxiam.tech`:
```nginx
server {
    server_name corvuxiam.tech www.corvuxiam.tech;

    location / {
        proxy_pass http://localhost:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

2. Ative o site e emita o certificado HTTPS:
```bash
ln -s /etc/nginx/sites-available/corvuxiam.tech /etc/nginx/sites-enabled/
nginx -t && systemctl reload nginx
snap install --classic certbot && certbot --nginx -d corvuxiam.tech -d www.corvuxiam.tech
```

---

### Opção 3: Executar o Aplicativo Mobile (React Native / Expo)

1. Acesse o diretório do app móvel:
```bash
cd CorvusIAM.Mobile
```

2. Instale as dependências:
```bash
npm install
```

3. Inicie o bundler do Expo:
```bash
npx expo start
```
* Pressione `a` para rodar no emulador Android, `w` para abrir no navegador, ou escaneie o QR Code pelo aplicativo **Expo Go** no celular.

---

## 🔒 Segurança & Políticas de Acesso

- **CORS Configurado:** Habilitado para requisições originadas do domínio oficial `https://corvuxiam.tech` e ambientes de desenvolvimento autorizados.
- **Armazenamento de Senhas:** Hash irreversível via algoritmo BCrypt com verificação contra a base pública de senhas comprometidas (HaveIBeenPwned).
- **Proteção contra Força Bruta:** Rate Limiting com bloqueio temporário após 5 tentativas consecutivas de autenticação incorreta e bloqueio de IP.
- **Desafio Anti-Bot (Cloudflare Turnstile):** Validação resiliente de CAPTCHA inteligente na autenticação com chave de homologação e bypass automático para testes/desenvolvimento.
- **Prevenção contra Ações Acidentais:** Modais interativos de confirmação em duas etapas (SweetAlert2) com identificação visual do registro e consequências antes de qualquer exclusão.
- **Soft Delete & Conformidade LGPD:** Exclusão lógica com anonimização de dados cadastrais sensíveis (PII), preservando a integridade referencial dos registros de auditoria.
- **Rastreabilidade Obrigatória:** Cada ação administrativa, criação de usuário, deleção ou abertura de chamado gera um evento imutável com data UTC, IP de origem e identificador do usuário.

---

## 🗄️ Política de Backup, Recuperação de Desastre e Custos (RPO & RTO)

Em estrita conformidade com os requisitos de Governança e Continuidade de Negócios do PIM IV, o sistema conta com política documentada em [`CorvusIAM/docs/politica-backup-e-custos.md`](CorvusIAM/docs/politica-backup-e-custos.md):

- **RPO (Recovery Point Objective):** 24 Horas para backups completos automatizados / 0 Horas para logs persistidos.
- **RTO (Recovery Time Objective):** < 30 Minutos para restauração completa de desastre (*Disaster Recovery*).
- **Estratégia 3-2-1:** 3 cópias (Banco ativo, dump local `.bak` e cópia em nuvem), 2 mídias diferentes (SSD NVMe e Cloud Object Storage) e 1 cópia off-site remota.
- **Scripts Automatizados de Infraestrutura:**
  - `scripts/backup-db.sh` — Executa backup com compressão, verificação `CHECKSUM` e expurgo de retenção de 30 dias.
  - `scripts/restore-db.sh` — Procedimento seguro de restauração em modo de usuário único (`SINGLE_USER`).
  - `scripts/sync-backups-s3.sh` — Replicação off-site para Amazon S3 com criptografia SSE-S3 (AES-256).
- **Custo Operacional Total Estimado:** ~ R$ 44,85 / mês (Hostinger VPS + S3 Standard-IA + Domínio), gerando alta viabilidade econômica.

---

## 🧪 Testes Automatizados

A suíte de testes em `CorvusIAM.Tests` conta com **19 testes automatizados** cobrindo autenticação, autorização, CRUD de usuários, repositórios e endpoints de integração:

```bash
Passed!  - Failed: 0, Passed: 19, Skipped: 0, Total: 19 - CorvusIAM.Tests.dll (net8.0)
```

---

## 🌐 Endpoints Principais da API REST

| Método | Rota | Descrição |
| :---: | :--- | :--- |
| `POST` | `/api/Auth/login` | Autenticação de usuário e emissão de JWT Token |
| `GET` | `/api/Usuarios` | Listagem paginada de colaboradores cadastrados |
| `POST` | `/api/Usuarios` | Cadastro de novo usuário com perfil atribuído |
| `GET` | `/api/Perfis` | Consulta de perfis de acesso do sistema |
| `GET` | `/api/Politicas` | Consulta de políticas ativas de segurança |
| `GET` | `/api/Chamados` | Listagem de chamados do usuário logado |
| `POST` | `/api/Chamados` | Registro de novo chamado de suporte (Web ou Mobile) |
| `GET` | `/health` | Verificação de integridade e status da aplicação |
| `GET` | `/metrics` | Exposição de métricas para monitoramento Prometheus |

---

## 📄 Licença e Direitos

Projeto desenvolvido como parte dos requisitos acadêmicos da **Universidade Paulista (UNIP)** para a disciplina **Projeto Integrado Multidisciplinar IV (PIM IV)** do CST em Análise e Desenvolvimento de Sistemas.

© 2026 Grupo CorvuxIAM — Todos os direitos reservados.