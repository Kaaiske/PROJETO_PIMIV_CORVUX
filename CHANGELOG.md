# 📜 Histórico de Versões & Changelog (CorvuxIAM)

Todas as alterações notáveis neste projeto serão documentadas neste arquivo, seguindo o padrão [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/) e [Versionamento Semântico](https://semver.org/).

---

## [2.4.0] - 2026-10-12

### 🌟 Adicionado
- **Responsividade Total:** Menu gaveta off-canvas lateral com backdrop e suporte a smartphones (360px a 480px), tablets e desktops de alta resolução.
- **PWA Aprimorado:** Manifesto Web com suporte a instalação nativa em celulares (Android e iOS) via banner inteligente e Service Worker atualizado.
- **Saúde & Telemetria de Endpoints (DevOps):** Monitoramento em tempo real de 18 estações de trabalho e servidores com varredura global, isolamento em quarentena de rede e remediação em lote.
- **SecOps Copilot & IA Heurística:** Terminal interativo com comandos de análise de ameaças, radar de CVEs sincronizado com NIST/CISA e WAF com bloqueio ativo de IPs.
- **Módulo Financeiro & Contábil:** Emissão de Notas Fiscais de Serviço (NFS-e) com validação de CNPJ via BrasilAPI, fechamento contábil e exportação oficial do SPED Fiscal.
- **Portal de Recursos Humanos:** Cálculo de folha de pagamento, controle e escala de férias com integração à API pública de feriados nacionais e espelho de ponto eletrônico AFD.
- **Autenticação em Duas Etapas (2FA/TOTP):** Suporte nativo a Google Authenticator e Microsoft Authenticator com fallback por e-mail e chave de recuperação.
- **Governança & Anonimização LGPD:** Stored Procedure `sp_InativarColaborador` e triggers automáticos `trg_AuditoriaUsuarios` para auditoria imutável de todas as ações.

### 🛡️ Segurança
- Rate Limiting estrito de 5 requisições por minuto no login.
- Hash de senhas irreversível via BCrypt com verificação na base de vazamentos pública (HaveIBeenPwned API).
- Cloudflare Turnstile anti-bot integrado ao formulário de autenticação.
- Anonimização de registros acadêmicos e conformidade estrita com LGPD.

---

## [2.0.0] - 2026-09-20
- Arquitetura corporativa em camadas .NET 8 (Entities, Repositories, Services, Controllers).
- Painel Administrativo com Tailwind CSS, Modo Escuro/Claro nativo e controle de acessibilidade WCAG 2.2 (A+/A- e Alto Contraste).
- Containerização oficial com Docker Compose e suporte a SQL Server 2022.

---

## [1.0.0] - 2026-08-15
- Versão inicial do sistema de autenticação e RBAC (Administrador, Colaborador, TI).

