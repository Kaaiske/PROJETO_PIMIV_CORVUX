# Gerenciamento Ágil do Projeto — CorvuxIAM

O desenvolvimento do **CorvuxIAM** seguiu o framework Scrum combinado ao quadro visual Kanban (colunas *Backlog*, *Em Progresso*, *Code Review / Testes* e *Concluído*), estruturado em quatro Sprints quinzenais com total de **89 Story Points** entregues.

## Product Backlog Consolidado

| ID | História de Usuário (User Story) | Prioridade | Story Points | Sprint |
| :---: | :--- | :---: | :---: | :---: |
| US01 | Como arquiteto de dados, desejo um banco relacional normalizado em 3FN com triggers imutáveis e procedures transacionais para garantir rastreabilidade. | Alta | 13 | Sprint 1 |
| US02 | Como administrador de segurança, desejo autenticação Zero Trust com BCrypt, bloqueio após 5 falhas, verificação HIBP e proteção Cloudflare Turnstile. | Alta | 13 | Sprint 1 |
| US03 | Como colaborador, desejo segundo fator MFA/TOTP criptografado e rotação de Refresh Token para proteger minha sessão corporativa. | Alta | 8 | Sprint 2 |
| US04 | Como gestor de IAM e RH, desejo gerenciar colaboradores, catálogo de cargos, matriz RBAC e autodeclaração étnico-racial IBGE com segregação LGPD. | Alta | 13 | Sprint 2 |
| US05 | Como analista de suporte e colaborador, desejo registrar e tratar chamados técnicos com cálculo dinâmico de SLA, anexos validados e APIs REST. | Alta | 8 | Sprint 3 |
| US06 | Como integrador externo, desejo um Gateway REST v1 autenticado por `X-API-Key` ou JWT com minimização de dados pessoais e proteção WAF L7. | Média | 8 | Sprint 3 |
| US07 | Como colaborador em campo, desejo acessar o sistema via PWA offline-first (`IndexedDB`) e aplicativo React Native (`Expo`) com sincronização idempotente. | Alta | 13 | Sprint 4 |
| US08 | Como usuário com necessidades assistivas, desejo tradução em Libras (VLibras), leitura por voz (TTS), ditado por fala (STT) e contraste WCAG 2.2 AA. | Alta | 5 | Sprint 4 |
| US09 | Como engenheiro DevOps, desejo containers Docker isolados, pipeline CI/CD no GitHub Actions, `/health`, métricas Prometheus e rotina de backup 3-2-1. | Alta | 8 | Sprint 4 |

## Critérios de Pronto (Definition of Done - DoD)

1. Código compilado em `.NET 8` (`Release`) sem erros e aderente à separação em camadas.
2. Políticas de autorização validadas no servidor (`[Authorize(Policy = "...")]`) e proteção CSRF/WAF ativa.
3. Dados sensíveis protegidos segundo a LGPD (mascaramento em logs, DTOs minimizados e ausência de segredos fixos no código).
4. Testes automatizados xUnit executados com 100% de aprovação no pipeline `ci.yml`.
5. Critérios de acessibilidade WCAG 2.2 nível AA verificados (semântica HTML, navegação por teclado, contraste mínimo de 4,5:1 e recursos assistivos).
