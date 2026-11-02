# Modelo Entidade-Relacionamento (MER) — CorvuxIAM (`CorvusIAMDb`)

```mermaid
erDiagram
    Usuarios ||--o{ UsuarioPerfis : possui
    Perfis ||--o{ UsuarioPerfis : atribui
    Perfis ||--o{ PerfilPermissoes : contem
    Permissoes ||--o{ PerfilPermissoes : concede
    Usuarios ||--o{ MfaSessoes : valida_mfa
    Usuarios ||--o{ ChamadosSuporte : solicita
    Usuarios o|--o{ ChamadosSuporte : atende
    Usuarios o|--o{ LogsAuditoria : registra
    Usuarios o|--o{ NotasFiscais : emite
    Usuarios o|--o{ FechamentosMensais : encerra
    Usuarios o|--o{ RegistrosFerias : agenda
```

O script Transact-SQL consolidado de instalação limpa (contemplando `CREATE DATABASE`, as 16 tabelas de domínio normalizadas em 3FN, chaves primárias e estrangeiras, índices filtrados e de cobertura, função escalar `fn_CalcularScoreRiscoUsuario`, 4 views analíticas, 5 triggers e 12 stored procedures) encontra-se unificado em `CorvusIAM/DatabaseScript.sql`.
