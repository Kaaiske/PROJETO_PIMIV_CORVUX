# Arquitetura de Software — CorvuxIAM (`CorvusIAM`)

```mermaid
flowchart LR
    U["Clientes Web (Razor Bento UI)<br/>Mobile PWA (/Mobile) e React Native (Expo)"] --> EDGE["Cloudflare Edge TLS 1.3 + Turnstile"]
    EDGE --> MW["Pipeline Middlewares ASP.NET Core 8<br/>(GlobalException + CorvusWaf L7 + RateLimiter + SecurityHeaders)"]
    MW --> MVC["Controllers MVC<br/>(Auth, Admin, Portal, Mobile)"]
    MW --> API["Controllers API REST<br/>(/api/Auth, /api/Usuarios, /api/Chamados, /api/v1/users)"]
    MVC --> SRV["Camada de Serviços e Interfaces<br/>(AuthService, TotpService, MfaSecretProtector, StorageService, PasswordPwnedService)"]
    API --> SRV
    SRV --> REPO["Repositórios e Persistência<br/>(UsuarioRepository, ApplicationDbContext)"]
    REPO --> DB[("SQL Server 2022 (Produção)<br/>SQLite (Dev / Testes)")]
    DB --> AUD["Triggers Imutáveis + Stored Procedures<br/>(LogsAuditoria)"]
```

A solução aplica separação de responsabilidades em camadas, orientação a objetos (abstração por interfaces em `CorvusIAM.Interfaces`, encapsulamento em serviços e DTOs, herança em `SqlServerDbContext`/`SqliteDbContext : ApplicationDbContext` e polimorfismo via Injeção de Dependência nativa do ASP.NET Core 8) e autenticação híbrida (`CookiesAndJwt`).
