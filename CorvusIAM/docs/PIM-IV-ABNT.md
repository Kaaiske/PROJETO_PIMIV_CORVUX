# Estrutura Documental ABNT do PIM IV — CorvuxIAM (`CorvusIAM`)

Este guia resume o mapeamento entre o relatório acadêmico oficial (`PIM_IV.docx`, formatado segundo as normas ABNT NBR 14724, NBR 6027 e NBR 6023 e o Manual do PIM IV da UNIP) e os artefatos reais implementados neste repositório.

## Convenção de Nomenclatura

- **`CorvuxIAM`**: denominação institucional, comercial e acadêmica da plataforma (`https://corvuxiam.tech`), desenvolvida para a organização **Corvux Tech Solutions Ltda.** em continuidade ao diagnóstico do PIM III.
- **`CorvusIAM`**: identificador técnico mantido nos artefatos de código-fonte (`CorvusIAM.sln`, `CorvusIAM/CorvusIAM.csproj`, `CorvusIAM.Mobile`, `CorvusIAM.Tests`, namespaces C# `CorvusIAM.*` e banco `CorvusIAMDb`).

## Rastreabilidade Código-Fonte x Relatório Acadêmico

1. **Planejamento do Negócio e Continuidade PIM III -> PIM IV:** diagnóstico organizacional da Corvux Tech Solutions Ltda., modelo SaaS B2B em reais, estrutura de custos, margem operacional, cálculo do ponto de equilíbrio e metodologia detalhada de ROI (`docs/politica-backup-e-custos.md`).
2. **Aplicação Web e APIs REST:** ASP.NET Core 8 MVC em Bento UI (`Controllers/`, `Views/`) integrado aos controladores REST (`Controllers/Api/`) sob esquema híbrido `CookiesAndJwt`, antiforgery global e matriz RBAC.
3. **Aplicação Mobile Híbrida (PWA + React Native):** PWA touch-first em `/Mobile` (`Views/Mobile/Index.cshtml`, `wwwroot/mobile/app.js`, `service-worker.js`, fila offline `IndexedDB` `CorvusOfflineDB`) e aplicativo nativo em `CorvusIAM.Mobile/` (React Native 0.74 + Expo 51, `expo-secure-store`, interceptador Axios de refresh token e sincronização idempotente via `X-Idempotency-Key`).
4. **Arquitetura de Software e POO:** separação em Domínio (`Entities/`), Persistência (`Data/`, `Repositories/`), Serviços (`Interfaces/`, `Services/`), Middlewares (`Middlewares/`) e Apresentação, aplicando abstração, encapsulamento, herança, polimorfismo e injeção de dependência (`docs/arquitetura.md`).
5. **Banco de Dados Consolidado:** script único de instalação limpa em `CorvusIAM/DatabaseScript.sql` contendo `CREATE DATABASE`, 16 tabelas de domínio em 3FN, PKs, 11 FKs, índices únicos/filtrados/cobertura, 1 função escalar, 4 views, 5 triggers e 12 stored procedures (`docs/mer.md`).
6. **Segurança Zero Trust, Acessibilidade WCAG 2.2 AA e LGPD:** detalhados em `docs/acessibilidade-e-seguranca.md`.
7. **Cloud, Containers e DevOps CI/CD:** `CorvusIAM/Dockerfile` multi-stage, `docker-compose.yml` com isolamento de rede e variáveis obrigatórias via `.env`, workflows `.github/workflows/ci.yml`, `docker-publish.yml` e `release.yml`, `/health` e `/metrics` (`Deploy.md`).
8. **Gerenciamento Ágil e Testes:** Product Backlog, Sprint Backlog, Definition of Done e Kanban em `docs/agil.md`; suíte automatizada xUnit + Coverlet em `CorvusIAM.Tests/`.
