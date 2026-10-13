# CorvuxIAM — Gestão Corporativa de Identidades e Acessos (IAM)

<div align="center">

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp&logoColor=white)](https://docs.microsoft.com/dotnet/csharp/)
[![React Native](https://img.shields.io/badge/React%20Native-Expo-61DAFB?logo=react&logoColor=black)](https://reactnative.dev/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC292B?logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![CI/CD](https://img.shields.io/badge/GitHub%20Actions-CI%2FCD-2088FF?logo=githubactions&logoColor=white)](https://github.com/Kaaiske/PROJETO_PIMIV_CORVUX/actions)
[![Tests](https://img.shields.io/badge/Tests-40%20Passed-brightgreen?logo=checkmarx&logoColor=white)](CorvusIAM.Tests/)
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
- **Infraestrutura em Nuvem (DevOps)** com telemetria e remediação remota de endpoints em tempo real.
- **Firewall WAF Ativo e Módulo SecOps** com bloqueio real de IPs maliciosos e modo de contingência Lock-Down.
- **Contabilidade e RH 100% Funcionais** com emissão real de notas fiscais (NFS-e), SPED Fiscal, folha de pagamento e espelho de ponto AFD.
- **Ecossistema de APIs Nacionais Integradas:** ViaCEP, BrasilAPI (CNPJ e Feriados), AwesomeAPI (Moedas em Tempo Real) e Validação Algorítmica de CPF (Módulo 11).
- **Acessibilidade Universal & Diversidade (WCAG 2.2):** Alto contraste, redimensionamento de fonte (A+/A-) e suporte a Nome Social.

---

## 🏛️ Matriz de Conformidade com o Manual do PIM IV

| Etapa / Disciplina | Implementação no Projeto | Status |
| :--- | :--- | :---: |
| **Etapa 1: Caracterização da Organização** | Modelo organizacional com departamentos (TI, RH, Contabilidade, HelpDesk) e fluxos de negócio mapeados. | 🟢 100% |
| **Etapa 2: Empreendedorismo em TI** | Proposta de valor centrada em redução de fraudes, conformidade com LGPD/ISO 27001 e baixo custo operacional (~R$ 44,85/mês). | 🟢 100% |
| **Etapa 3: Relações Étnico-Raciais e Diversidade** | Acessibilidade universal (WCAG 2.2, botões A+/A-, alto contraste, rótulos semânticos), campos inclusivos de Nome Social/Pronome e políticas sem viés discriminatório. | 🟢 100% |
| **Etapa 4: Solução Web (.NET Web)** | ASP.NET Core 8 MVC, Razor Views, autenticação híbrida (Cookies + JWT), Rate Limiting, Turnstile e APIs REST. | 🟢 100% |
| **Etapa 5: Solução Mobile** | App nativo em **React Native (Expo)** em `CorvusIAM.Mobile` e **PWA Offline-First** (`/Mobile`) com IndexedDB e Service Worker. | 🟢 100% |
| **Etapa 6: Arquitetura de Software (.NET)** | Organização em camadas desacopladas (Controllers, Services, Repositories, Entities, DTOs, Middlewares, DI). | 🟢 100% |
| **Etapa 7: Banco de Dados** | SQL Server 2022 / SQLite, modelo físico normalizado, Stored Procedures (`sp_RegistrarLog`, `sp_InativarColaborador`, `sp_ObterResumoAuditoria`) e Triggers (`trg_AuditoriaUsuarios`, `trg_AuditoriaChamados`). | 🟢 100% |
| **Etapa 8: Cloud Computing & DevOps** | Dockerfile, `docker-compose.yml`, pipeline automatizado no GitHub Actions, Health Checks (`/health`), métricas Prometheus, política de backup 3-2-1 e custos documentados. | 🟢 100% |
| **Etapa 9: Gerenciamento Ágil de Projetos** | Planejamento em 4 Sprints, Product Backlog priorizado, Sprint Backlogs e Quadro Kanban integrado. | 🟢 100% |

---

## 🛠️ Tecnologias Utilizadas

### Backend & APIs
- **C# / .NET 8 (LTS)** — ASP.NET Core MVC e Web API RESTful
- **Entity Framework Core 8** — Mapeamento objeto-relacional (ORM) e migrations
- **Resiliência e Null-Safety:** Mapeamento objeto-relacional com Nullable Reference Types (C# 12 / EF Core 8) e sanitização automática de valores nulos no startup
- **Autenticação:** Cookie Authentication (Web) + JWT Bearer Tokens (Mobile/API) + API Gateway Token (`X-API-Key`)
- **Segurança:** BCrypt para hash de senhas, Cloudflare Turnstile CAPTCHA, HaveIBeenPwned API (verificação de senhas vazadas), MaxMind/IP Geolocation, Rate Limiter (5 req/min no login) e WAF de Bloqueio Ativo de IPs
- **APIs Nacionais & Corporativas:** ViaCEP, BrasilAPI (CNPJ, Feriados Nacionais), AwesomeAPI (Câmbio USD/EUR), Módulo 11 da Receita Federal
- **Documentação de API:** Swagger / OpenAPI 3.0 com suporte ao domínio `https://corvuxiam.tech`

### Frontend Web
- **Razor Views (ASP.NET Core)**
- **Bootstrap 5.3 & Tailwind CSS** (Responsivo e Acessível WCAG 2.2)
- **Recursos Inclusivos:** Controles A+/A- de fonte, Modo Alto Contraste para pessoas com baixa visão, feedback com SweetAlert2

### Frontend Mobile
- **React Native 0.74 / Expo 51 (TypeScript)** — Aplicativo mobile com Axios, Expo SecureStore e React Navigation
- **Progressive Web App (PWA)** — Service Worker, Web App Manifest, Cache Storage e fila offline via IndexedDB

### Banco de Dados
- **Microsoft SQL Server 2022** (Ambiente de Produção e Docker)
- **SQLite** (Ambiente de Desenvolvimento Rápido e Testes Automatizados)
- **Recursos Avançados de Banco de Dados:**
  - **Stored Procedures:**
    - `sp_RegistrarLog`: Inserção atômica e padronizada de eventos na tabela `LogsAuditoria`.
    - `sp_InativarColaborador`: Inativação de usuário com invalidação de tokens e registro na auditoria (Soft Delete LGPD).
    - `sp_ObterResumoAuditoria`: Relatório consolidado de métricas analíticas e incidentes críticos por período.
  - **Triggers:**
    - `trg_AuditoriaUsuarios`: Auditoria automática em tempo real para comandos `INSERT`, `UPDATE` e `DELETE` em `Usuarios`.
    - `trg_AuditoriaChamados`: Rastreabilidade e auditoria automática de transições de status em `ChamadosSuporte`.
  - **Tabelas Corporativas Integradas:** `Usuarios`, `Perfis`, `Permissoes`, `LogsAuditoria`, `ChamadosSuporte`, `FirewallRegras`, `DispositivosRede`, `NotasFiscais`, `FechamentosMensais`, `RegistrosFerias`, `FolhasPagamento`.

### DevOps & Nuvem
- **Docker & Docker Compose** (Multi-container: Web Application + SQL Server)
- **GitHub Actions** (CI/CD: Restore, Build, Unit Testing, Docker Image Build)
- **Health Checks** (`/health`) & **Prometheus Metrics** (`/metrics`)
- **Política de Backup 3-2-1:** RPO de 24h, RTO < 30min, scripts em `scripts/` e armazenamento em nuvem no Amazon S3 com criptografia SSE-S3.

---

## 📂 Estrutura da Solução

```text
PROJETO_PIMIV_CORVUX/
├── .github/
│   └── workflows/
│       └── ci.yml                     # Pipeline automatizado de CI/CD (GitHub Actions)
├── CorvusIAM/                         # Aplicação Principal (ASP.NET Core 8 MVC + REST API)
│   ├── Controllers/                   # Controllers Web (Admin, Auth, Home, Portal, Mobile)
│   │   └── Api/                       # Controllers de APIs REST (Usuarios, Perfis, Politicas, V1Users, PublicIntegrations)
│   ├── Data/                          # Contextos do EF Core (ApplicationDbContext, DbDataSeeder)
│   ├── Entities/                      # Entidades de Domínio (Usuario, Perfil, Permissao, Auditoria, Chamado, etc.)
│   ├── Interfaces/                    # Contratos de Serviços e Repositórios
│   ├── Middlewares/                   # Middleware Global de Tratamento de Exceções
│   ├── Migrations/                    # Migrations estruturadas para SQL Server e SQLite
│   ├── Models/DTOs/                   # Data Transfer Objects e ViewModels departamentais
│   ├── Repositories/                  # Camada de Repositório desacoplada
│   ├── Services/                      # Regras de Negócio (Auth, Auditoria, Geolocation, Totp, etc.)
│   ├── Views/                         # Telas Razor MVC (Admin, Auth, Portal, Mobile)
│   ├── wwwroot/                       # Arquivos estáticos, CSS, JS e módulo PWA (`/mobile`)
│   ├── DatabaseScript.sql             # Script SQL físico completo (Tabelas, FKs, Procedures, Triggers, Seeds)
│   ├── Dockerfile                     # Construção da imagem Docker do backend
│   └── Program.cs                     # Inicialização da aplicação, DI, CORS, Swagger e Pipeline HTTP
├── CorvusIAM.Mobile/                  # Aplicativo Mobile Nativo (React Native / Expo / TypeScript)
│   ├── src/
│   │   ├── navigation/                # Navegação por Stack (AppNavigator)
│   │   ├── screens/                   # Telas Mobile (Login, Home, Access, Account, Policies, Support)
│   │   └── services/api.ts            # Cliente HTTP Axios configurado para https://corvuxiam.tech
│   ├── App.tsx                        # Ponto de entrada do aplicativo mobile
│   └── package.json                   # Dependências do app mobile
├── CorvusIAM.Tests/                   # Bateria de Testes Automatizados (xUnit / Moq / InMemory)
│   ├── Controllers/                   # Testes unitários (Admin, Portal, Cloud, Firewall, PublicIntegrations)
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
* O serviço do SQL Server está configurado com `user: root` no Docker Compose para evitar conflitos de permissão de escrita de volumes (`Access is denied`) no Linux/Ubuntu.

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

## 🔒 Segurança & Políticas de Acesso

- **CORS Configurado:** Habilitado para requisições originadas do domínio oficial `https://corvuxiam.tech` e ambientes de desenvolvimento autorizados.
- **Autenticação em Duas Etapas (MFA / 2FA com TOTP):** Padrão RFC 6238 nativo compatível com Google Authenticator, Microsoft Authenticator e apps TOTP.
- **Firewall WAF Ativo:** Bloqueio e liberação de IPs indesejados diretamente no banco de dados (`FirewallRegras`), com bloqueio preventivo antes da validação de senha.
- **Armazenamento de Senhas:** Hash irreversível via algoritmo BCrypt com verificação contra a base pública de senhas comprometidas (HaveIBeenPwned).
- **Proteção contra Força Bruta:** Rate Limiting com bloqueio temporário após 5 tentativas consecutivas de autenticação incorreta.
- **Desafio Anti-Bot (Cloudflare Turnstile):** Validação resiliente de CAPTCHA inteligente na autenticação com chave de homologação e bypass automático para testes/desenvolvimento.
- **Prevenção contra Ações Acidentais:** Modais interativos com alerta severo de **Ação Irreversível**, autoridade exclusiva de Administrador e anonimização LGPD perpétua via Stored Procedure `sp_InativarColaborador`.
- **Soft Delete Universal:** Exclusão lógica com anonimização LGPD para colaboradores e inativação segura para perfis de acesso.
- **Módulo de Anexos Seguros (Storage Local & S3):** Upload seguro de anexos em chamados de Help Desk com validação de tipos MIME e isolamento de arquivos.
- **Exportação de Relatórios Gerenciais:** Download instantâneo de colaboradores e trilhas de auditoria em formato CSV com UTF-8 BOM, além de SPED Fiscal e espelhos de ponto (AFD).
- **Rastreabilidade Obrigatória:** Cada ação administrativa, criação de usuário, deleção ou abertura de chamado gera um evento imutável com data UTC, IP de origem e identificador do usuário.

---

## 🧪 Testes Automatizados

A suíte de testes em `CorvusIAM.Tests` conta com **40 testes automatizados** cobrindo autenticação, MFA/TOTP, armazenamento seguro, autorização, CRUD de usuários, telemetria de rede DevOps, bloqueio de IP/WAF, emissão de NFS-e, cálculo de folha de pagamento, validação de CPF e endpoints de integração externa:

```bash
Passed!  - Failed: 0, Passed: 40, Skipped: 0, Total: 40, Duration: 10 s - CorvusIAM.Tests.dll (net8.0)
```

---

## 🌐 Endpoints Principais da API REST

| Método | Rota | Descrição |
| :---: | :--- | :--- |
| `POST` | `/api/Auth/login` | Autenticação de usuário e emissão de JWT Token |
| `GET` | `/api/public/cep/{cep}` | Consulta de endereço via webservice público ViaCEP |
| `GET` | `/api/public/cnpj/{cnpj}` | Consulta de dados fiscais e cadastrais via BrasilAPI |
| `GET` | `/api/public/cotacoes` | Cotação ao vivo de moedas (USD/BRL e EUR/BRL) via AwesomeAPI |
| `GET` | `/api/public/feriados/{ano}` | Consulta de feriados nacionais bancários via BrasilAPI |
| `POST` | `/api/public/validar-cpf` | Validação algorítmica de CPF pelo Módulo 11 oficial |
| `GET` | `/api/Usuarios` | Listagem paginada de colaboradores cadastrados |
| `POST` | `/api/Usuarios` | Cadastro de novo usuário com perfil atribuído |
| `GET` | `/api/v1/ping` | Gateway de status de integração externa autenticada |
| `GET` | `/api/v1/users` | Gateway de sincronização de identidades com terceiros |
| `GET` | `/Admin/ExportarUsuariosCsv` | Download de relatório de colaboradores em CSV |
| `GET` | `/Admin/ExportarLogsAuditoriaCsv` | Download de trilhas de auditoria em CSV |
| `GET` | `/Portal/ExportarSpedFiscal` | Download de arquivo SPED Fiscal consolidado |
| `GET` | `/Portal/ExportarEspelhoPonto` | Download de espelho de ponto em padrão AFD |
| `GET` | `/Portal/ObterMfaSetup` | Chave secreta e QR Code para configuração de 2FA |
| `POST` | `/Portal/ConfirmarAtivacaoMfa` | Validação de código TOTP e ativação do 2FA |
| `GET` | `/api/Perfis` | Consulta de perfis de acesso do sistema |
| `GET` | `/api/Politicas` | Consulta de políticas ativas de segurança |
| `GET` | `/api/Chamados` | Listagem de chamados do usuário logado |
| `POST` | `/api/Chamados` | Registro de novo chamado de suporte (Web ou Mobile) |
| `GET` | `/health` | Verificação de integridade e status da aplicação |
| `GET` | `/metrics` | Exposição de métricas para monitoramento Prometheus |

---

## 🚀 Arquitetura Funcional & Módulos 100% Reais

Todos os módulos do CorvuxIAM operam conectados a tabelas relacionais no banco de dados e possuem fluxos de ponta a ponta funcionais:

1. **Infraestrutura em Nuvem & Saúde de Endpoints (DevOps):**
   - Tabela `DispositivosRede` com 18 nós cadastrados (Workstations, Servidores Linux/Windows, Controladores AD).
   - Telemetrias reais de CPU, Memória, Disco e Agente EDR.
   - Ações operacionais remotas que alteram o banco em tempo real: **Escanear Agora**, **Aplicar Patches**, **Isolar da Rede / Liberar**, **Limpeza de Disco** e **Remediação em Lote**.

2. **Gestão de Custos de Nuvem & Backup AWS S3 (Etapa 8):**
   - Painel de transparência de custos corporativos na tela de Cloud (~R$ 44,85/mês).
   - RPO garantido de 24 horas e RTO inferior a 30 minutos.
   - Estratégia 3-2-1 com replicação automatizada em nuvem e integridade SHA-256.

3. **Firewall WAF & Defesa Ativa SecOps:**
   - Tabela `FirewallRegras` gravando regras manuais e automáticas de bloqueio.
   - O `AuthController.Login` consulta a tabela e rejeita conexões de IPs bloqueados antes mesmo do processamento da senha.
   - Desbloqueio e bloqueio dinâmico no painel `Inteligencia.cshtml`, com acionamento do **Modo Lock-Down**.
   - Radar de CVEs e vulnerabilidades críticas em conformidade com o catálogo CISA/NIST.

4. **Integrações de APIs Nacionais & Automações Cadastrais:**
   - **ViaCEP:** Preenchimento automático de logradouro, bairro e cidade ao digitar o CEP.
   - **BrasilAPI CNPJ:** Preenchimento automático de Razão Social, CNAE e regime tributário ao emitir notas fiscais.
   - **AwesomeAPI Moedas:** Cotação ao vivo de Dólar e Euro no painel contábil.
   - **BrasilAPI Feriados:** Verificação de dias úteis e feriados na programação de férias.
   - **Validação de CPF:** Algoritmo do Módulo 11 com feedback visual instantâneo.

5. **Acessibilidade Universal & Diversidade (Etapa 3 - PIM IV):**
   - Controles no header de redimensionamento de fonte (`A+` e `A-`).
   - Modo de **Alto Contraste** para pessoas com deficiência visual ou daltonismo.
   - Campo inclusivo de **Nome Social / Pronome** no cadastro de colaboradores.

6. **Contabilidade & Financeiro Corporativo:**
   - Módulo de NFS-e persistido na tabela `NotasFiscais` com geração de chave SEFAZ de 44 dígitos e alíquotas de ISS/PIS/COFINS.
   - Tabela `FechamentosMensais` com balanço consolidado de receitas e despesas.
   - Exportador do arquivo oficial do **SPED Fiscal** (`.txt`) formatado para envio à Receita.

7. **Recursos Humanos & Departamento Pessoal:**
   - Tabela `FolhasPagamento` com cálculo de proventos, desconto progressivo de INSS, FGTS e e-Social.
   - Tabela `RegistrosFerias` com aprovação gerencial e período aquisitivo.
   - Exportador de espelho de ponto eletrônico no padrão **Portaria AFD** (`.txt`).

8. **Help Desk & Abertura Rápida de Issues:**
   - Modal global de abertura de chamados integrado em todas as telas com categorias, prioridades e upload de evidências.

---

## 📄 Licença e Direitos

Projeto desenvolvido como parte dos requisitos acadêmicos da **Universidade Paulista (UNIP)** para a disciplina **Projeto Integrado Multidisciplinar IV (PIM IV)** do CST em Análise e Desenvolvimento de Sistemas.

© 2026 Grupo CorvuxIAM — Todos os direitos reservados.