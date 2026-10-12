# Acessibilidade, diversidade e segurança

## Acessibilidade e inclusão

- A PWA usa rótulos associados aos campos, feedback em região `aria-live`, contraste alto e layout responsivo.
- As telas devem ser testadas por navegação de teclado e leitor de tela antes da apresentação.
- A solução emprega linguagem neutra e respeitosa e não cria restrições de acesso por origem, etnia, gênero, idade ou deficiência.

## Segurança

- Senhas são armazenadas com BCrypt; nunca em texto puro.
- Cookies são `HttpOnly`, `Secure` e `SameSite=Strict`.
- APIs exigem JWT ou cookie autenticado; dados de usuários são restritos a Admin/RH.
- Perfis restringem os módulos de RH, Contabilidade, Help Desk e Administração.
- Chaves e senhas de infraestrutura são fornecidas por `.env`/variáveis de ambiente e ignoradas pelo Git.
