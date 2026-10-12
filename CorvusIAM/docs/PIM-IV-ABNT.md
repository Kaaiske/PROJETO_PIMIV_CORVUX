# Roteiro do PIM IV — CorvusIAM

> Preencha os campos entre colchetes com dados verdadeiros da organização estudada no PIM III. O desenvolvimento deve ter de 20 a 30 páginas, sem contar capa, resumo, abstract, sumário, referências e anexos.

## Formatação obrigatória

- Papel A4; fonte Arial ou Times New Roman, tamanho 12; texto justificado; espaçamento 1,5.
- Margens: superior/esquerda 3 cm e inferior/direita 2 cm.
- Recuo de primeira linha 1,5 cm; citações longas, resumo e referências em espaço simples.
- Numeração no canto superior direito a partir da Introdução.

## Estrutura

1. Capa: UNIP, curso, autor(es)/RA, título, cidade e ano.
2. Resumo (150 a 500 palavras) e palavras-chave.
3. Abstract e keywords.
4. Sumário.
5. Introdução: objetivo, metodologia e apresentação de [organização].
6. Desenvolvimento — capítulos 1 a 9 abaixo.
7. Conclusão.
8. Referências em ABNT.
9. Anexos: prints, MER, script SQL, Kanban e guia de execução.

## Desenvolvimento sugerido

### 1. Caracterização da organização

Descrever [segmento], produtos/serviços, estrutura, processos, problemas e oportunidades de melhoria identificadas no PIM III.

### 2. Planejamento da solução

Apresentar o problema de gestão de identidades e acessos, público-alvo, proposta de valor, benefícios e diferenciais do CorvusIAM.

### 3. Diversidade, inclusão e responsabilidade social

Registrar linguagem inclusiva, contraste visual, navegação por teclado, textos alternativos, formulários com rótulos e proteção de dados pessoais.

### 4. Aplicação web

Documentar ASP.NET Core 8, MVC, autenticação por cookies/JWT, APIs REST, perfis, painel administrativo e módulos de chamados.

### 5. Aplicação mobile

Descrever a PWA em `/Mobile`: autenticação integrada, abertura e consulta de chamados, design responsivo, manifesto e cache offline de interface.

### 6. Arquitetura de software

Usar o diagrama de `docs/arquitetura.md`; explicar entidades, controllers, serviços, repositório, DTOs e injeção de dependência.

### 7. Banco de dados

Anexar `DatabaseScript.sql` e o MER de `docs/mer.md`; explicar tabelas, relações, índices, procedure `sp_RegistrarLog` e trigger `trg_AuditoriaUsuarios`.

### 8. Cloud e DevOps

Explicar Docker, Docker Compose, SQL Server, health check, secrets no `.env`, GitHub Actions e endpoint `/health`.

### 9. Gestão ágil

Anexar o planejamento de `docs/agil.md` e ajustar prazos, responsáveis e evidências do grupo.

## Referências iniciais

Inclua somente fontes efetivamente consultadas, por exemplo: documentação oficial do ASP.NET Core, Entity Framework Core, Docker, Microsoft SQL Server, OWASP e bibliografia da disciplina, no padrão ABNT.
