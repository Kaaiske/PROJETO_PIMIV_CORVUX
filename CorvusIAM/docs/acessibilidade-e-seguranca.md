# Acessibilidade, Diversidade e Segurança Zero Trust — CorvuxIAM

## Acessibilidade Digital (WCAG 2.2 Nível AA) e Inclusão

- **Conformidade WCAG 2.2 Nível AA:** paleta clara e escura com contraste mínimo de 4,5:1, modo dedicado de alto contraste, foco visível em navegação por teclado, atributos `lang="pt-BR"`, rótulos semânticos e regiões `aria-label`/`aria-live`.
- **Tradução para Libras (VLibras):** integração nativa com o widget governamental `vlibras-plugin.js`, permitindo tradução de textos e instruções da tela por avatar 3D para colaboradores surdos.
- **Síntese e Reconhecimento de Voz (Web Speech API):** painel assistivo com leitura em voz natural (`speechSynthesis` / TTS) com ajuste de idioma e velocidade, além de ditado por voz (`SpeechRecognition` / STT) nos formulários web e na abertura de chamados mobile.
- **Governança Étnico-Racial (Padrão IBGE) e Privacidade:** coleta voluntária segundo as categorias oficiais do IBGE, modalidade de ingresso, nome social e indicador PcD, armazenada de forma segregada das rotas operacionais e tokens de autenticação em respeito à minimização de dados sensíveis (Arts. 5º, II, 6º, III e 11 da LGPD).

## Segurança da Informação (Zero Trust e LGPD)

- **Credenciais e MFA:** senhas com hash `BCrypt` (custo 11/12 e salt individual), consulta k-Anonymity ao `HaveIBeenPwned`, bloqueio temporário após 5 falhas consecutivas e segundo fator TOTP (RFC 6238) com segredo criptografado via ASP.NET Core Data Protection (`MfaSecretProtector`) e sessões `MfaSessoes` com hash SHA-256.
- **Sessões Web e Mobile:** cookies `HttpOnly`, `Secure` e `SameSite=Strict` na interface MVC; tokens JWT assinados com HMAC-SHA256 e Refresh Tokens rotativos de uso único armazenados apenas em hash SHA-256 com comparação em tempo constante (`FixedTimeEquals`).
- **Defesa Perimetral e Aplicação:** Cloudflare Turnstile fail-closed em produção, Rate Limiting (`LoginPolicy` e `PublicIntegrationPolicy`), WAF L7 (`CorvusWafMiddleware`) com validação de proxies confiáveis, cabeçalhos CSP/HSTS/X-Frame-Options e inspeção de *magic bytes* em uploads (limite de 5 MB fora de `wwwroot`).
- **Auditoria e LGPD:** trilha imutável protegida pela trigger `trg_ProtegerLogsImutaveis`, acesso restrito à permissão `logs.visualizar` (`CanViewLogs`), mascaramento automático de CPFs e tokens pelo `SerilogMaskingEnricher` e anonimização irreversível no desligamento/direito de eliminação (`DELETE /api/Usuarios/{id}` e `sp_AnonimizarTitularLGPD`).
