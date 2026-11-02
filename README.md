<div align="center">

<img src="CorvusIAM/wwwroot/icons/corvus-logo.png" alt="CorvuxIAM Logo" width="120" style="border-radius: 20px;" />

# CorvuxIAM (`CorvusIAM`) - Plataforma Corporativa de Gestão de Identidades, Acessos e Governança (IAM & SecOps)

[![.NET 8](https://img.shields.io/badge/.NET-8.0%20LTS-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![React Native](https://img.shields.io/badge/React%20Native-0.74%20%7C%20Expo%2051-61DAFB?logo=react&logoColor=black)](CorvusIAM.Mobile/)
[![PWA](https://img.shields.io/badge/PWA-Offline--First-5A0FC8?logo=pwa&logoColor=white)](CorvusIAM/Views/Mobile/Index.cshtml)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)](docker-compose.yml)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC292B?logo=microsoftsqlserver&logoColor=white)](CorvusIAM/DatabaseScript.sql)
[![CI/CD](https://img.shields.io/badge/GitHub%20Actions-CI%2FCD-2088FF?logo=githubactions&logoColor=white)](.github/workflows/ci.yml)
[![Tests](https://img.shields.io/badge/Tests-55%20Passed-brightgreen?logo=xunit&logoColor=white)](CorvusIAM.Tests/)
[![Production](https://img.shields.io/badge/Production-corvuxiam.tech-success?logo=googlechrome&logoColor=white)](https://corvuxiam.tech)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**Plataforma integrada de Gestão de Identidades e Controle de Acessos (IAM), Governança RBAC, Segurança Zero Trust, Acessibilidade Digital (WCAG 2.2 AA, VLibras e Web Speech API) e Solução Mobile Híbrida (PWA e React Native)**

**Ambiente de Produção:** [https://corvuxiam.tech](https://corvuxiam.tech)  
**Repositório Oficial:** [github.com/Kaaiske/PROJETO_PIMIV_CORVUX](https://github.com/Kaaiske/PROJETO_PIMIV_CORVUX)

</div>

---

## Apresentação Acadêmica - PIM IV (UNIP)

* **Instituição:** Universidade Paulista (UNIP)
* **Curso:** Curso Superior de Tecnologia em Análise e Desenvolvimento de Sistemas (ADS) - 4º Semestre
* **Projeto:** Continuidade prática do diagnóstico organizacional realizado no PIM III (estudo aplicado à empresa modelo *Corvux Tech Solutions Ltda.*), materializado em software corporativo funcional.

### Convenção de Nomenclatura (`CorvuxIAM` vs `CorvusIAM`)
Para manter rastreabilidade entre o plano de negócio e o código-fonte:
* **`CorvuxIAM`** designa o nome comercial do produto, a identidade visual da plataforma e o domínio público (`https://corvuxiam.tech`).
* **`CorvusIAM`** designa os identificadores técnicos da solução .NET (`CorvusIAM.sln`), projetos (`CorvusIAM/CorvusIAM.csproj`, `CorvusIAM.Tests/CorvusIAM.Tests.csproj`, `CorvusIAM.Mobile`), namespaces C# (`CorvusIAM.*`) e o banco de dados relacional (`CorvusIAMDb`).

---

## Visão Geral da Plataforma

O **CorvuxIAM** centraliza o ciclo de vida de identidades corporativas (admissão, movimentação de cargos, concessão granular de permissões e desligamento/anonimização LGPD), integrando portal administrativo Web em **Bento UI**, **API RESTful**, central **Service Desk (ITSM)**, **Centro de Operações de Segurança (SecOps / WAF L7)**, telemetria de endpoints e dois clientes móveis complementares (**PWA Touch-First** e **React Native / Expo**).

### Principais Recursos Implementados no Código
1. **Autenticação Híbrida Zero Trust (`CookiesAndJwt`):** Proteção anti-bot Cloudflare Turnstile com validação *fail-closed* em produção (`TurnstileService`), senhas com `BCrypt.Net-Next` + verificação k-Anonymity contra credenciais vazadas (`PasswordPwnedService` / Have I Been Pwned), segundo fator TOTP RFC 6238 (`TotpService`) com segredos criptografados em repouso via ASP.NET Core Data Protection (`MfaSecretProtector`), sessões efêmeras de desafio MFA com hash SHA-256 (`MfaSessoes`), rotação de *Refresh Token* de uso único comparado em tempo constante (`CryptographicOperations.FixedTimeEquals`) e bloqueio temporário (*lockout* de 15 minutos após 5 falhas).
2. **Controle de Acesso Baseado em Papéis (RBAC) e Políticas:** Cinco perfis nativos (`Admin`, `Recursos Humanos`, `Contabilidade`, `Help Desk`, `Colaborador`) e seis permissões atômicas (`usuarios.visualizar`, `usuarios.gerenciar`, `perfis.gerenciar`, `chamados.atender`, `logs.visualizar`, `politicas.gerenciar`) impostas no servidor por políticas declarativas (`RequireAdmin`, `RequireHR`, `RequireAccounting`, `RequireAdminOrHelpDesk`, `CanViewUsers`, `CanManageUsers`, `CanViewLogs`).
3. **Defesa em Profundidade e WAF L7 (`CorvusWafMiddleware`):** Inspeção de query strings, rotas e formulários contra SQL Injection, XSS e Path Traversal, respeito a cabeçalhos `X-Forwarded-For` restrito a proxies reversos confiáveis, auto-bloqueio de IPs na tabela `FirewallRegras`, Rate Limiting (`LoginPolicy`: 5 req/min; `PublicIntegrationPolicy`: 30 req/min), validação global de CSRF (`AutoValidateAntiforgeryTokenAttribute`) e filtro seguro de anexos (limite de 5 MB, assinatura binária *Magic Bytes* e armazenamento fora de `wwwroot`).
4. **Conformidade LGPD e Governança Étnico-Racial (IBGE):** Segregação dos metadados sensíveis de diversidade (`DIVERSIDADE_ETNICO_RACIAL_MAP_JSON`: raça/cor nas cinco categorias oficiais do IBGE, modalidade de ingresso, nome social e indicador PcD) em relação à tabela de autenticação e aos DTOs públicos de API, mascaramento automático de CPF/e-mail/tokens em logs (`SerilogMaskingEnricher`) e anonimização irreversível no desligamento (`DELETE /api/Usuarios/{id}` e `sp_AnonimizarTitularLGPD`).
5. **Acessibilidade Digital Multimodal (WCAG 2.2 Nível AA):** Contraste mínimo de 4,5:1, modo de alto contraste (`.high-contrast`), ajuste tipográfico (`A+`/`A-`), navegação por teclado, widget governamental **VLibras** (`vlibras-plugin.js`) e **Web Speech API** nativa para síntese de voz neural (TTS) e ditado por voz (STT) na abertura de chamados Web e Mobile.
6. **Arquitetura Mobile Dupla (PWA + React Native):**
   * **PWA Touch-First (`/Mobile`):** Manifesto `/mobile/manifest.webmanifest`, Service Workers (`/sw.js` e `/mobile/service-worker.js`), cache `offline.html` e fila offline persistida em `IndexedDB` (`CorvusOfflineDB`, store `syncQueue`) com chave de idempotência (`X-Idempotency-Key` / `ClientIdempotencyKey`).
   * **App Nativo (`CorvusIAM.Mobile`):** Construído em **React Native 0.74** e **Expo 51**, com armazenamento criptografado de tokens via `expo-secure-store`, fluxo completo de MFA (`/api/Auth/mfa-verify`), interceptor Axios com fila de concorrência para renovação de JWT (`/api/Auth/refresh-token`) e revogação explícita no logout (`/api/Auth/logout`).

---

## Estrutura dos Projetos no Repositório

```text
PROJETO_PIMIV_CORVUX/
├── CorvusIAM.sln                      # Solução .NET 8 principal
├── docker-compose.yml                 # Orquestração isolada (Web + SQL Server 2022)
├── .env.example                       # Modelo de variáveis de ambiente obrigatórias
├── scripts/                           # Automação 3-2-1 de Backup, Restore e Sync S3
│   ├── backup-db.sh
│   ├── restore-db.sh
│   └── sync-backups-s3.sh
├── .github/workflows/                 # Pipelines CI/CD (ci.yml, docker-publish.yml, release.yml)
├── CorvusIAM/                         # Aplicação Web MVC + API RESTful (.NET 8)
│   ├── Controllers/                   # Controladores MVC (Auth, Admin, Portal, Mobile, Home)
│   ├── Controllers/Api/               # Endpoints REST (/api/Auth, Usuarios, Chamados, v1/users...)
│   ├── Entities/                      # 16 entidades de domínio mapeadas no EF Core
│   ├── Data/                          # ApplicationDbContext, SqlServerDbContext, SqliteDbContext, Seeder
│   ├── Interfaces/ & Services/        # Contratos e regras de negócio desacopladas via DI
│   ├── Middlewares/                   # GlobalExceptionMiddleware e CorvusWafMiddleware
│   ├── Infrastructure/Logging/        # SerilogMaskingEnricher (proteção de PII em logs)
│   ├── Views/ & wwwroot/              # Interfaces Bento UI, PWA /Mobile, VLibras e TTS/STT
│   ├── DatabaseScript.sql             # Script Transact-SQL consolidado (clean-install completo)
│   ├── Dockerfile                     # Build multi-stage oficial (sdk:8.0 -> aspnet:8.0)
│   ├── Deploy.md                      # Guia operacional de implantação em VPS e nuvem
│   └── docs/                          # Documentação complementar (arquitetura, MER, ágil, segurança)
├── CorvusIAM.Mobile/                  # Aplicativo móvel React Native 0.74 / Expo 51 (TypeScript)
│   ├── App.tsx                        # Raiz do aplicativo e gerenciamento global de sessão
│   ├── src/screens/                   # LoginScreen, HomeScreen, SupportScreen, AccessScreen...
│   └── src/services/api.ts            # Cliente Axios com SecureStore e refreshPromise concorrente
└── CorvusIAM.Tests/                   # Suíte de 55 testes automatizados (xUnit + Moq + SQLite)
```

---

## Banco de Dados Relacional Consolidado (`CorvusIAM/DatabaseScript.sql`)

O script Transact-SQL único e idempotente em [`CorvusIAM/DatabaseScript.sql`](CorvusIAM/DatabaseScript.sql) provisiona a base `CorvusIAMDb` em **Terceira Forma Normal (3FN)** a partir de uma instância limpa do Microsoft SQL Server 2022:

* **16 Tabelas de Domínio:** `Perfis`, `Permissoes`, `Usuarios`, `MfaSessoes`, `UsuarioPerfis`, `PerfilPermissoes`, `PoliticasAcesso`, `ConfiguracoesSistema`, `FirewallRegras`, `DispositivosRede`, `LogsAuditoria`, `ChamadosSuporte` (com controle de concorrência otimista `ROWVERSION` e índice único filtrado de idempotência), `NotasFiscais`, `FechamentosMensais`, `RegistrosFerias` e `FolhasPagamento`.
* **Integridade e Desempenho:** Chaves primárias, **11 chaves estrangeiras** com regras explícitas de integridade referencial e **11 índices** (únicos, filtrados e de cobertura).
* **Programação Interna no SGBD:** **1 função escalar** (`fn_CalcularScoreRiscoUsuario`), **4 views analíticas** (`vw_UsuariosPerfisAtivos`, `vw_ChamadosSlaCritico`, `vw_AuditoriaSegurancaConsolidada`, `vw_ResumoFolhaDepartamento`), **5 triggers de auditoria e imutabilidade** (`trg_AuditoriaStatusUsuario`, `trg_AuditoriaFirewallWaf`, `trg_AuditoriaChamados`, `trg_AuditoriaConfig` e `trg_ProtegerLogsImutaveis` com `INSTEAD OF UPDATE, DELETE` bloqueando adulteração de logs) e **12 stored procedures** transacionais (`sp_RegistrarLog`, `sp_RegistrarLogAuditoria`, `sp_ProvisionarColaboradorRBAC`, `sp_Autenticar Metadados/Lockout`, `sp_InativarColaborador`, `sp_AnonimizarTitularLGPD`, `sp_ObterResumoAuditoria`, `sp_ProcessarChamadoSla`, `sp_BloquearIpFirewallWaf`, entre outras).

---

## Como Executar o Projeto

### Pré-requisitos
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [Docker e Docker Compose](https://docs.docker.com/compose/) (para execução containerizada com SQL Server 2022)
* [Node.js 20+](https://nodejs.org/) (opcional, apenas para o cliente nativo `CorvusIAM.Mobile` com Expo)

### Opção 1: Execução Containerizada com Docker Compose (Recomendada para Homologação/Produção)

```bash
git clone https://github.com/Kaaiske/PROJETO_PIMIV_CORVUX.git
cd PROJETO_PIMIV_CORVUX

# Copie o modelo de variáveis e preencha segredos fortes e exclusivos
cp .env.example .env

# Construa a imagem multi-stage e inicie os contêineres
docker compose up -d --build
docker compose ps
```

**Isolamento de Rede no Docker Compose:**
* O contêiner `sqlserver` opera exclusivamente na rede bridge interna `corvus-network`, **sem expor a porta 1433 no host**, eliminando vetores externos de conexão direta ao SGBD.
* O contêiner `corvusiam.web` faz bind apenas no loopback local (`127.0.0.1:8080:8080`), recebendo tráfego externo somente através do proxy reverso HTTPS (Nginx/Cloudflare TLS 1.3).
* O Swagger UI (`/swagger`) permanece habilitado apenas em `ASPNETCORE_ENVIRONMENT=Development`.

### Opção 2: Execução Local via .NET 8 CLI (Desenvolvimento com SQLite)

```bash
# Restaurar pacotes e compilar a solução
dotnet restore CorvusIAM.sln
dotnet build CorvusIAM.sln --configuration Release

# Definir senha inicial obrigatória do administrador para o seeder local
# Linux/macOS: export ADMIN_INITIAL_PASSWORD=" TroquePorSenhaForte!2026 "
# PowerShell:  $env:ADMIN_INITIAL_PASSWORD="TroquePorSenhaForte!2026"

# Executar a aplicação Web + API + PWA Mobile
dotnet run --project CorvusIAM/CorvusIAM.csproj
```

Rotas locais padrão (`Development`):
* **Portal Web (Login Corporativo):** `http://localhost:5052/Auth/Login`
* **Aplicativo Mobile PWA:** `http://localhost:5052/Mobile` (atalho de autenticação em `/Mobile/Login`)
* **Swagger OpenAPI (apenas Development):** `http://localhost:5052/swagger`
* **Health Check:** `http://localhost:5052/health`
* **Métricas Prometheus (restrito a `RequireAdmin`):** `http://localhost:5052/metrics`

### Opção 3: Execução do Aplicativo Nativo React Native / Expo (`CorvusIAM.Mobile`)

```bash
cd CorvusIAM.Mobile
npm install
# Opcional: apontar para servidor local ou domínio de produção
# PowerShell: $env:EXPO_PUBLIC_API_URL="http://10.0.2.2:5052"
npx expo start
```

---

## Suíte de Testes Automatizados e Cobertura Verificável (55 Testes)

A qualidade do código é aferida continuamente no GitHub Actions (`.github/workflows/ci.yml`) por **55 testes automatizados** no projeto [`CorvusIAM.Tests`](CorvusIAM.Tests/), combinando xUnit, Moq, Coverlet e `WebApplicationFactory<Program>`:

* **48 Testes Unitários e de Componente:** Autenticação, desafio MFA TOTP, criptografia de segredos MFA, rotação de Refresh Token, lockout, anonimização LGPD, bloqueio de autoexclusão, regras RBAC, WAF/IDS, cálculos de RH (folha/férias/AFD), contabilidade (NFS-e/SPED/DRE) e remediação segura de endpoints.
* **7 Testes de Integração Ponta a Ponta (`FullStackIntegrationAndAccessibilityTests`):** Ciclo HTTP completo de login JWT + rotação de Refresh Token + revogação no logout; renderização do PWA `/Mobile`, manifesto e prevenção de duplicidade offline via `X-Idempotency-Key` em `/api/Chamados`; Gateway `/api/v1/users` validando escopo de chave e não exposição de PII sensível; bloqueio L7 no `CorvusWafMiddleware` (SQLi, XSS e Path Traversal) com persistência em `LogsAuditoria`; e verificação de cabeçalhos de segurança (CSP, `X-Frame-Options`, `nosniff`) e marcação semântica WCAG 2.2 AA (`lang="pt-BR"`, `aria-label`, VLibras e TTS).

### Comando Reprodutível de Execução e Cobertura

```bash
dotnet test CorvusIAM.sln --collect:"XPlat Code Coverage"
```

```text
Passed!  - Failed:     0, Passed:    55, Skipped:     0, Total:    55 - CorvusIAM.Tests.dll (net8.0)
```

**Métricas Reais de Cobertura de Código (Coverlet Cobertura XML):**
* **Código C# de Aplicação** (excluindo artefatos autogerados de EF Core Migrations e templates Razor `.cshtml`): **42,22% de linhas cobertas** (`2.392 / 5.665` linhas executáveis), distribuídas por camada:
  * Persistência e Repositórios (`ApplicationDbContext` e `UsuarioRepository`): **92,44%** (`159 / 172` linhas)
  * Entidades de Domínio e Modelos/DTOs (`CorvusIAM.Entities` e `CorvusIAM.Models`): **71,00%** (`213 / 300` linhas)
  * Middlewares Defensivos (`CorvusWafMiddleware` e `GlobalExceptionMiddleware`): **65,82%** (`104 / 158` linhas)
  * Serviços de Negócio, Segurança e Infraestrutura: **42,41%** (`310 / 731` linhas)
  * Controladores Web MVC e API REST: **31,98%** (`955 / 2.986` linhas)
* **Assembly Total Compilado** (incluindo ~8.963 linhas autogeradas de Migrations e Views Razor): **16,49% de linhas** (`2.412 / 14.628`) e **16,96% de ramificações** (`470 / 2.770`).

---

## Catálogo Principal de Endpoints da API RESTful

| Método | Endpoint | Política / Autenticação | Módulo | Descrição |
| :---: | :--- | :--- | :--- | :--- |
| `POST` | `/api/Auth/login` | Público (`LoginPolicy` 5/min) | Autenticação | Autentica credenciais ou inicia desafio MFA efêmero |
| `POST` | `/api/Auth/mfa-verify` | Público (`LoginPolicy` 5/min) | Autenticação | Valida código TOTP RFC 6238 e emite JWT + Refresh Token |
| `POST` | `/api/Auth/refresh-token` | Refresh Token válido | Autenticação | Rotaciona par JWT + Refresh Token de uso único (SHA-256) |
| `POST` | `/api/Auth/logout` | `[Authorize]` (Bearer/Cookie) | Autenticação | Revoga imediatamente o Refresh Token ativo no servidor |
| `GET` | `/api/Usuarios` | `CanViewUsers` | IAM Core | Lista diretório de identidades (sem expor mapa étnico-racial) |
| `POST` | `/api/Usuarios` | `CanManageUsers` | IAM Core | Provisiona colaborador com validação HIBP e hash BCrypt |
| `PUT` | `/api/Usuarios/{id}` | `CanManageUsers` | IAM Core | Atualiza cadastro e vínculos de perfis RBAC |
| `DELETE` | `/api/Usuarios/{id}` | `CanManageUsers` | LGPD / IAM | Executa *soft delete* com anonimização irreversível de PII |
| `GET` | `/api/Perfis` | `RequireAdmin` | RBAC | Consulta perfis de acesso e permissões associadas |
| `GET/POST` | `/api/PoliticasAcesso` | `RequireAdmin` | Zero Trust | Consulta e gerencia políticas contextuais de acesso |
| `GET/PUT` | `/api/Configuracoes` | `RequireAdmin` | Governança | Gerencia parâmetros globais de segurança e sessão |
| `GET` | `/api/Chamados` | `[Authorize]` (Cookie/Bearer) | Service Desk | Lista chamados do colaborador autenticado |
| `POST` | `/api/Chamados` | `[Authorize]` + Idempotência | Service Desk | Registra chamado com SLA dinâmico e deduplicação offline |
| `POST` | `/api/chamados/anexos` | `[Authorize]` (Máx. 5 MB) | Service Desk | Upload validado por *Magic Bytes* fora de `wwwroot` |
| `GET` | `/api/v1/ping` | Público | Gateway v1 | Verificação de disponibilidade do gateway de integração |
| `GET` | `/api/v1/users` | `X-API-Key` ou Bearer (`CanViewUsers`) | Gateway v1 | Exportação paginada e minimizada (LGPD) para ERPs |
| `GET/POST` | `/api/public/*` | Público (`PublicIntegrationPolicy` 30/min) | Utilitários | Consulta CEP, CNPJ, COMPE bancário e validação de CPF |

---

## Backup, Recuperação de Desastres (3-2-1) e Operação

Os scripts em [`scripts/`](scripts/) implementam a estratégia de proteção **3-2-1** (RPO de 24 horas para dumps agendados e RTO inferior a 30 minutos):
* `./scripts/backup-db.sh`: Gera backup íntegro `.bak` comprimido via `sqlcmd` dentro do volume isolado e remove artefatos locais com mais de 7 dias.
* `./scripts/sync-backups-s3.sh`: Sincroniza os backups locais com bucket externo compatível com S3 (AWS S3 / Cloudflare R2).
* `./scripts/restore-db.sh <arquivo.bak>`: Restaura a base `CorvusIAMDb` em modo `SINGLE_USER` com `REPLACE` e revalida a disponibilidade.

---

## Licença

Distribuído sob a licença **MIT** ([`LICENSE`](LICENSE)). Desenvolvido para fins acadêmicos e tecnológicos no âmbito do **PIM IV - CST em Análise e Desenvolvimento de Sistemas (UNIP, 2026)**.
