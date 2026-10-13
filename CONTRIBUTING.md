# 🤝 Guia de Contribuição — CorvuxIAM

Obrigado pelo seu interesse em contribuir para o **CorvuxIAM**!

---

## 🛠️ Como Contribuir

### 1. Criando Issues
- Antes de abrir uma issue, verifique se o tópico já não foi discutido anteriormente.
- Utilize os nossos templates oficiais: `bug_report.md` ou `feature_request.md`.

### 2. Desenvolvimento Local
1. Faça o Fork do repositório e clone para sua máquina:
   ```bash
   git clone https://github.com/<seu-usuario>/PROJETO_PIMIV_CORVUX.git
   cd PROJETO_PIMIV_CORVUX
   ```
2. Crie uma branch com nome descritivo:
   ```bash
   git checkout -b feat/sua-feature-incrivel
   ```
3. Restaure e compile a solução:
   ```bash
   dotnet restore CorvusIAM.sln
   dotnet build CorvusIAM.sln
   ```
4. Execute os testes unitários obrigatórios:
   ```bash
   dotnet test CorvusIAM.sln
   ```
   > ⚠️ Todos os testes devem passar com 100% de sucesso antes de enviar o PR.

### 3. Padrões de Código
- Siga as convenções oficiais do C# / .NET 8 e boas práticas de arquitetura limpa.
- Mantenha comentários de documentação XML em interfaces e serviços públicos.
- **Não commite segredos, senhas ou dados pessoais (LGPD)**.

### 4. Abrindo o Pull Request
- Preencha todos os itens do nosso checklist de Pull Request.
- Aguarde a execução bem-sucedida do pipeline de CI (`ci.yml`).

