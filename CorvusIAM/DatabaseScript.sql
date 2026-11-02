-- =============================================================================
-- CorvuxIAM (Solução Técnica: CorvusIAM)
-- Script Consolidado de Instalação Limpa do Banco de Dados Corporativo
-- Projeto Integrado Multidisciplinar IV (PIM IV - UNIP)
-- SGBD Alvo: Microsoft SQL Server 2019 / 2022 / 2025 (Transact-SQL)
-- Escopo: Criação do Banco, 16 Tabelas Normalizadas (3FN), Chaves Primárias,
--         Chaves Estrangeiras, Índices Únicos/Filtrados/Cobertura, Funções,
--         Views Analíticas, Triggers Imutáveis de Auditoria, Stored Procedures
--         Transacionais e Carga Inicial RBAC (sem credenciais padrão).
-- =============================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT [name] FROM [sys].[databases] WHERE [name] = N'CorvusIAMDb')
BEGIN
    CREATE DATABASE [CorvusIAMDb];
END;
GO

USE [CorvusIAMDb];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- =============================================================================
-- 1. HISTÓRICO DE MIGRAÇÕES DO ENTITY FRAMEWORK CORE 8
-- =============================================================================
IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[__EFMigrationsHistory] (
        [MigrationId] NVARCHAR(150) NOT NULL,
        [ProductVersion] NVARCHAR(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

-- =============================================================================
-- 2. CRIAÇÃO DAS TABELAS DO DOMÍNIO (MODELO RELACIONAL EM 3FN)
-- =============================================================================

-- 2.1 Tabela: ConfiguracoesSistema
IF OBJECT_ID(N'[dbo].[ConfiguracoesSistema]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ConfiguracoesSistema] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [Chave] NVARCHAR(100) NOT NULL,
        [Valor] NVARCHAR(MAX) NOT NULL,
        CONSTRAINT [PK_ConfiguracoesSistema] PRIMARY KEY ([Id])
    );
END;
GO

-- 2.2 Tabela: Perfis (Papéis RBAC)
IF OBJECT_ID(N'[dbo].[Perfis]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Perfis] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [Nome] NVARCHAR(50) NOT NULL,
        [Descricao] NVARCHAR(200) NOT NULL CONSTRAINT [DF_Perfis_Descricao] DEFAULT N'',
        [Ativo] BIT NOT NULL CONSTRAINT [DF_Perfis_Ativo] DEFAULT (1),
        CONSTRAINT [PK_Perfis] PRIMARY KEY ([Id])
    );
END;
GO

-- 2.3 Tabela: Permissoes (Escopos Granulares RBAC)
IF OBJECT_ID(N'[dbo].[Permissoes]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Permissoes] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [Nome] NVARCHAR(50) NOT NULL,
        [Descricao] NVARCHAR(200) NOT NULL CONSTRAINT [DF_Permissoes_Descricao] DEFAULT N'',
        CONSTRAINT [PK_Permissoes] PRIMARY KEY ([Id])
    );
END;
GO

-- 2.4 Tabela: PoliticasAcesso
IF OBJECT_ID(N'[dbo].[PoliticasAcesso]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PoliticasAcesso] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [Nome] NVARCHAR(100) NOT NULL,
        [Descricao] NVARCHAR(500) NOT NULL CONSTRAINT [DF_PoliticasAcesso_Descricao] DEFAULT N'',
        [Ativa] BIT NOT NULL CONSTRAINT [DF_PoliticasAcesso_Ativa] DEFAULT (1),
        CONSTRAINT [PK_PoliticasAcesso] PRIMARY KEY ([Id])
    );
END;
GO

-- 2.5 Tabela: Usuarios (Identidades Corporativas)
IF OBJECT_ID(N'[dbo].[Usuarios]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Usuarios] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [Nome] NVARCHAR(100) NOT NULL,
        [Email] NVARCHAR(100) NOT NULL,
        [SenhaHash] NVARCHAR(MAX) NOT NULL,
        [Ativo] BIT NOT NULL CONSTRAINT [DF_Usuarios_Ativo] DEFAULT (1),
        [TentativasLogin] INT NOT NULL CONSTRAINT [DF_Usuarios_TentativasLogin] DEFAULT (0),
        [DataDesbloqueio] DATETIME2 NULL,
        [ResetSenhaToken] NVARCHAR(MAX) NULL CONSTRAINT [DF_Usuarios_ResetSenhaToken] DEFAULT N'',
        [ResetSenhaExpiracao] DATETIME2 NULL,
        [RefreshToken] NVARCHAR(MAX) NULL CONSTRAINT [DF_Usuarios_RefreshToken] DEFAULT N'',
        [RefreshTokenExpiracao] DATETIME2 NULL,
        [MfaHabilitado] BIT NOT NULL CONSTRAINT [DF_Usuarios_MfaHabilitado] DEFAULT (0),
        [MfaSecretKey] NVARCHAR(MAX) NULL CONSTRAINT [DF_Usuarios_MfaSecretKey] DEFAULT N'',
        [CPF] NVARCHAR(14) NULL CONSTRAINT [DF_Usuarios_CPF] DEFAULT N'',
        [DataNascimento] DATETIME2 NULL,
        [Cargo] NVARCHAR(100) NULL CONSTRAINT [DF_Usuarios_Cargo] DEFAULT N'',
        [DataContratacao] DATETIME2 NULL,
        [RG] NVARCHAR(20) NULL CONSTRAINT [DF_Usuarios_RG] DEFAULT N'',
        [Telefone] NVARCHAR(20) NULL CONSTRAINT [DF_Usuarios_Telefone] DEFAULT N'',
        [Departamento] NVARCHAR(100) NULL CONSTRAINT [DF_Usuarios_Departamento] DEFAULT N'',
        [Endereco] NVARCHAR(200) NULL CONSTRAINT [DF_Usuarios_Endereco] DEFAULT N'',
        CONSTRAINT [PK_Usuarios] PRIMARY KEY ([Id])
    );
END;
GO

-- 2.6 Tabela Associativa: UsuarioPerfis (Relacionamento N:N Usuarios <-> Perfis)
IF OBJECT_ID(N'[dbo].[UsuarioPerfis]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UsuarioPerfis] (
        [UsuarioId] INT NOT NULL,
        [PerfilId] INT NOT NULL,
        CONSTRAINT [PK_UsuarioPerfis] PRIMARY KEY ([UsuarioId], [PerfilId]),
        CONSTRAINT [FK_UsuarioPerfis_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId])
            REFERENCES [dbo].[Usuarios] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UsuarioPerfis_Perfis_PerfilId] FOREIGN KEY ([PerfilId])
            REFERENCES [dbo].[Perfis] ([Id]) ON DELETE CASCADE
    );
END;
GO

-- 2.7 Tabela Associativa: PerfilPermissoes (Relacionamento N:N Perfis <-> Permissoes)
IF OBJECT_ID(N'[dbo].[PerfilPermissoes]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PerfilPermissoes] (
        [PerfilId] INT NOT NULL,
        [PermissaoId] INT NOT NULL,
        CONSTRAINT [PK_PerfilPermissoes] PRIMARY KEY ([PerfilId], [PermissaoId]),
        CONSTRAINT [FK_PerfilPermissoes_Perfis_PerfilId] FOREIGN KEY ([PerfilId])
            REFERENCES [dbo].[Perfis] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PerfilPermissoes_Permissoes_PermissaoId] FOREIGN KEY ([PermissaoId])
            REFERENCES [dbo].[Permissoes] ([Id]) ON DELETE CASCADE
    );
END;
GO

-- 2.8 Tabela: MfaSessoes (Desafios Temporários de Segundo Fator TOTP)
IF OBJECT_ID(N'[dbo].[MfaSessoes]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[MfaSessoes] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [TokenHash] NVARCHAR(128) NOT NULL,
        [UsuarioId] INT NOT NULL,
        [Email] NVARCHAR(100) NOT NULL,
        [ExpiraEm] DATETIME2 NOT NULL,
        [Tentativas] INT NOT NULL CONSTRAINT [DF_MfaSessoes_Tentativas] DEFAULT (0),
        [Canal] NVARCHAR(20) NOT NULL CONSTRAINT [DF_MfaSessoes_Canal] DEFAULT N'API',
        CONSTRAINT [PK_MfaSessoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MfaSessoes_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId])
            REFERENCES [dbo].[Usuarios] ([Id]) ON DELETE CASCADE
    );
END;
GO

-- 2.9 Tabela: ChamadosSuporte (Service Desk ITSM com SLA e Idempotência Offline)
IF OBJECT_ID(N'[dbo].[ChamadosSuporte]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ChamadosSuporte] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [UsuarioId] INT NOT NULL,
        [Assunto] NVARCHAR(100) NOT NULL,
        [Descricao] NVARCHAR(MAX) NULL CONSTRAINT [DF_ChamadosSuporte_Descricao] DEFAULT N'',
        [Categoria] NVARCHAR(50) NOT NULL CONSTRAINT [DF_ChamadosSuporte_Categoria] DEFAULT N'Acesso',
        [Prioridade] NVARCHAR(20) NOT NULL CONSTRAINT [DF_ChamadosSuporte_Prioridade] DEFAULT N'Normal',
        [Status] NVARCHAR(50) NOT NULL CONSTRAINT [DF_ChamadosSuporte_Status] DEFAULT N'Aberto',
        [ResponsavelId] INT NULL,
        [DataAbertura] DATETIME2 NOT NULL CONSTRAINT [DF_ChamadosSuporte_DataAbertura] DEFAULT (GETUTCDATE()),
        [DataAtualizacao] DATETIME2 NOT NULL CONSTRAINT [DF_ChamadosSuporte_DataAtualizacao] DEFAULT (GETUTCDATE()),
        [PrazoSla] DATETIME2 NULL,
        [DataResolucao] DATETIME2 NULL,
        [DataFechamento] DATETIME2 NULL,
        [Solucao] NVARCHAR(MAX) NULL CONSTRAINT [DF_ChamadosSuporte_Solucao] DEFAULT N'',
        [AnexoUrl] NVARCHAR(500) NULL,
        [AnexoNomeOriginal] NVARCHAR(255) NULL,
        [ClientIdempotencyKey] NVARCHAR(100) NULL,
        [RowVersion] ROWVERSION NOT NULL,
        CONSTRAINT [PK_ChamadosSuporte] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChamadosSuporte_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId])
            REFERENCES [dbo].[Usuarios] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ChamadosSuporte_Usuarios_ResponsavelId] FOREIGN KEY ([ResponsavelId])
            REFERENCES [dbo].[Usuarios] ([Id]) ON DELETE NO ACTION
    );
END;
GO

-- 2.10 Tabela: LogsAuditoria (Trilha Imutável de Auditoria e Eventos SIEM)
IF OBJECT_ID(N'[dbo].[LogsAuditoria]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[LogsAuditoria] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [UsuarioId] INT NULL,
        [DataHora] DATETIME2 NOT NULL CONSTRAINT [DF_LogsAuditoria_DataHora] DEFAULT (GETUTCDATE()),
        [Acao] NVARCHAR(100) NOT NULL,
        [Detalhe] NVARCHAR(MAX) NULL CONSTRAINT [DF_LogsAuditoria_Detalhe] DEFAULT N'',
        [Tipo] NVARCHAR(50) NOT NULL,
        [Origem] NVARCHAR(50) NOT NULL,
        CONSTRAINT [PK_LogsAuditoria] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LogsAuditoria_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId])
            REFERENCES [dbo].[Usuarios] ([Id]) ON DELETE NO ACTION
    );
END;
GO

-- 2.11 Tabela: FirewallRegras (Regras Dinâmicas de Bloqueio WAF L7 / IDS)
IF OBJECT_ID(N'[dbo].[FirewallRegras]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FirewallRegras] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [Ip] NVARCHAR(50) NOT NULL,
        [Motivo] NVARCHAR(200) NOT NULL,
        [PaisOrigem] NVARCHAR(10) NULL CONSTRAINT [DF_FirewallRegras_PaisOrigem] DEFAULT N'BR',
        [DataBloqueio] DATETIME2 NOT NULL CONSTRAINT [DF_FirewallRegras_DataBloqueio] DEFAULT (GETUTCDATE()),
        [Ativo] BIT NOT NULL CONSTRAINT [DF_FirewallRegras_Ativo] DEFAULT (1),
        [CriadoPor] NVARCHAR(100) NULL CONSTRAINT [DF_FirewallRegras_CriadoPor] DEFAULT N'SecOps WAF',
        CONSTRAINT [PK_FirewallRegras] PRIMARY KEY ([Id])
    );
END;
GO

-- 2.12 Tabela: DispositivosRede (Inventário e Telemetria de Endpoints)
IF OBJECT_ID(N'[dbo].[DispositivosRede]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[DispositivosRede] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [Hostname] NVARCHAR(100) NOT NULL,
        [Ip] NVARCHAR(50) NOT NULL,
        [Vlan] NVARCHAR(50) NULL CONSTRAINT [DF_DispositivosRede_Vlan] DEFAULT N'VLAN 10 - Corporativa',
        [UsuarioResponsavel] NVARCHAR(100) NULL CONSTRAINT [DF_DispositivosRede_UsuarioResp] DEFAULT N'',
        [SistemaOperacional] NVARCHAR(100) NULL CONSTRAINT [DF_DispositivosRede_SO] DEFAULT N'',
        [Tipo] NVARCHAR(50) NOT NULL CONSTRAINT [DF_DispositivosRede_Tipo] DEFAULT N'estacoes',
        [CpuUsoPercentual] INT NOT NULL CONSTRAINT [DF_DispositivosRede_Cpu] DEFAULT (15),
        [RamUsoPercentual] INT NOT NULL CONSTRAINT [DF_DispositivosRede_Ram] DEFAULT (45),
        [DiscoUsoPercentual] INT NOT NULL CONSTRAINT [DF_DispositivosRede_Disco] DEFAULT (35),
        [EdrAtivo] BIT NOT NULL CONSTRAINT [DF_DispositivosRede_EdrAtivo] DEFAULT (1),
        [EdrStatus] NVARCHAR(100) NULL CONSTRAINT [DF_DispositivosRede_EdrStatus] DEFAULT N'EDR Protegido',
        [PatchesPendentes] INT NOT NULL CONSTRAINT [DF_DispositivosRede_Patches] DEFAULT (0),
        [Status] NVARCHAR(50) NOT NULL CONSTRAINT [DF_DispositivosRede_Status] DEFAULT N'Saudavel',
        [IsoladoRede] BIT NOT NULL CONSTRAINT [DF_DispositivosRede_Isolado] DEFAULT (0),
        [UltimoScan] DATETIME2 NULL,
        [UltimoHeartbeat] DATETIME2 NOT NULL CONSTRAINT [DF_DispositivosRede_Heartbeat] DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_DispositivosRede] PRIMARY KEY ([Id])
    );
END;
GO

-- 2.13 Tabela: NotasFiscais (Módulo Contábil e Fiscal)
IF OBJECT_ID(N'[dbo].[NotasFiscais]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[NotasFiscais] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [Numero] INT NOT NULL,
        [TomadorNome] NVARCHAR(150) NOT NULL,
        [TomadorCnpj] NVARCHAR(20) NOT NULL,
        [Valor] DECIMAL(18,2) NOT NULL,
        [Descricao] NVARCHAR(500) NULL CONSTRAINT [DF_NotasFiscais_Descricao] DEFAULT N'',
        [DataEmissao] DATETIME2 NOT NULL CONSTRAINT [DF_NotasFiscais_DataEmissao] DEFAULT (GETUTCDATE()),
        [ProtocoloSefaz] NVARCHAR(50) NULL CONSTRAINT [DF_NotasFiscais_Protocolo] DEFAULT N'',
        [Status] NVARCHAR(20) NOT NULL CONSTRAINT [DF_NotasFiscais_Status] DEFAULT N'Autorizada',
        [EmitidoPorUsuarioId] INT NULL,
        CONSTRAINT [PK_NotasFiscais] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_NotasFiscais_Usuarios_EmitidoPorUsuarioId] FOREIGN KEY ([EmitidoPorUsuarioId])
            REFERENCES [dbo].[Usuarios] ([Id]) ON DELETE SET NULL
    );
END;
GO

-- 2.14 Tabela: FechamentosMensais (Consolidação Contábil DRE)
IF OBJECT_ID(N'[dbo].[FechamentosMensais]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FechamentosMensais] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [MesAno] NVARCHAR(20) NOT NULL,
        [ReceitaBruta] DECIMAL(18,2) NOT NULL,
        [DespesasOperacionais] DECIMAL(18,2) NOT NULL,
        [LucroLiquido] DECIMAL(18,2) NOT NULL,
        [DataFechamento] DATETIME2 NOT NULL CONSTRAINT [DF_FechamentosMensais_Data] DEFAULT (GETUTCDATE()),
        [Status] NVARCHAR(20) NOT NULL CONSTRAINT [DF_FechamentosMensais_Status] DEFAULT N'Fechado',
        [FechadoPorUsuarioId] INT NULL,
        CONSTRAINT [PK_FechamentosMensais] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FechamentosMensais_Usuarios_FechadoPorUsuarioId] FOREIGN KEY ([FechadoPorUsuarioId])
            REFERENCES [dbo].[Usuarios] ([Id]) ON DELETE SET NULL
    );
END;
GO

-- 2.15 Tabela: RegistrosFerias (Portal de Recursos Humanos)
IF OBJECT_ID(N'[dbo].[RegistrosFerias]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RegistrosFerias] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [UsuarioId] INT NULL,
        [ColaboradorNome] NVARCHAR(100) NOT NULL,
        [DataInicio] DATETIME2 NOT NULL,
        [DataFim] DATETIME2 NOT NULL,
        [Dias] INT NOT NULL CONSTRAINT [DF_RegistrosFerias_Dias] DEFAULT (30),
        [Status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_RegistrosFerias_Status] DEFAULT N'Aprovada',
        [DataAprovacao] DATETIME2 NOT NULL CONSTRAINT [DF_RegistrosFerias_DataAprov] DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_RegistrosFerias] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RegistrosFerias_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId])
            REFERENCES [dbo].[Usuarios] ([Id]) ON DELETE SET NULL
    );
END;
GO

-- 2.16 Tabela: FolhasPagamento (Processamento Mensal de RH e Encargos)
IF OBJECT_ID(N'[dbo].[FolhasPagamento]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[FolhasPagamento] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [MesReferencia] NVARCHAR(20) NOT NULL,
        [TotalColaboradores] INT NOT NULL CONSTRAINT [DF_FolhasPagamento_TotalColab] DEFAULT (18),
        [ValorBrutoTotal] DECIMAL(18,2) NOT NULL,
        [ValorLiquidoTotal] DECIMAL(18,2) NOT NULL,
        [InssTotal] DECIMAL(18,2) NOT NULL,
        [FgtsTotal] DECIMAL(18,2) NOT NULL,
        [DataProcessamento] DATETIME2 NOT NULL CONSTRAINT [DF_FolhasPagamento_DataProc] DEFAULT (GETUTCDATE()),
        [Status] NVARCHAR(30) NOT NULL CONSTRAINT [DF_FolhasPagamento_Status] DEFAULT N'Processada',
        CONSTRAINT [PK_FolhasPagamento] PRIMARY KEY ([Id])
    );
END;
GO

-- =============================================================================
-- 3. ÍNDICES ÚNICOS, FILTRADOS E NÃO-CLUSTERIZADOS DE COBERTURA
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM [sys].[indexes] WHERE [name] = N'IX_Usuarios_Email' AND [object_id] = OBJECT_ID(N'[dbo].[Usuarios]'))
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Usuarios_Email]
        ON [dbo].[Usuarios]([Email])
        INCLUDE ([Nome], [Ativo], [MfaHabilitado]);
GO

IF NOT EXISTS (SELECT 1 FROM [sys].[indexes] WHERE [name] = N'IX_Usuarios_CPF' AND [object_id] = OBJECT_ID(N'[dbo].[Usuarios]'))
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Usuarios_CPF]
        ON [dbo].[Usuarios]([CPF])
        WHERE [CPF] IS NOT NULL AND [CPF] <> N'';
GO

IF NOT EXISTS (SELECT 1 FROM [sys].[indexes] WHERE [name] = N'IX_MfaSessoes_TokenHash' AND [object_id] = OBJECT_ID(N'[dbo].[MfaSessoes]'))
    CREATE UNIQUE NONCLUSTERED INDEX [IX_MfaSessoes_TokenHash]
        ON [dbo].[MfaSessoes]([TokenHash]);
GO

IF NOT EXISTS (SELECT 1 FROM [sys].[indexes] WHERE [name] = N'IX_MfaSessoes_UsuarioId' AND [object_id] = OBJECT_ID(N'[dbo].[MfaSessoes]'))
    CREATE NONCLUSTERED INDEX [IX_MfaSessoes_UsuarioId]
        ON [dbo].[MfaSessoes]([UsuarioId]);
GO

IF NOT EXISTS (SELECT 1 FROM [sys].[indexes] WHERE [name] = N'IX_ChamadosSuporte_UsuarioId_ClientIdempotencyKey' AND [object_id] = OBJECT_ID(N'[dbo].[ChamadosSuporte]'))
    CREATE UNIQUE NONCLUSTERED INDEX [IX_ChamadosSuporte_UsuarioId_ClientIdempotencyKey]
        ON [dbo].[ChamadosSuporte]([UsuarioId], [ClientIdempotencyKey])
        WHERE [ClientIdempotencyKey] IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM [sys].[indexes] WHERE [name] = N'IX_ChamadosSuporte_Status_Prioridade' AND [object_id] = OBJECT_ID(N'[dbo].[ChamadosSuporte]'))
    CREATE NONCLUSTERED INDEX [IX_ChamadosSuporte_Status_Prioridade]
        ON [dbo].[ChamadosSuporte]([Status], [Prioridade])
        INCLUDE ([UsuarioId], [DataAbertura], [PrazoSla]);
GO

IF NOT EXISTS (SELECT 1 FROM [sys].[indexes] WHERE [name] = N'IX_LogsAuditoria_DataHora_Tipo' AND [object_id] = OBJECT_ID(N'[dbo].[LogsAuditoria]'))
    CREATE NONCLUSTERED INDEX [IX_LogsAuditoria_DataHora_Tipo]
        ON [dbo].[LogsAuditoria]([DataHora] DESC, [Tipo])
        INCLUDE ([Acao], [Origem], [UsuarioId]);
GO

IF NOT EXISTS (SELECT 1 FROM [sys].[indexes] WHERE [name] = N'IX_FirewallRegras_Ip_Ativo' AND [object_id] = OBJECT_ID(N'[dbo].[FirewallRegras]'))
    CREATE NONCLUSTERED INDEX [IX_FirewallRegras_Ip_Ativo]
        ON [dbo].[FirewallRegras]([Ip], [Ativo]);
GO

IF NOT EXISTS (SELECT 1 FROM [sys].[indexes] WHERE [name] = N'IX_DispositivosRede_Status_Tipo' AND [object_id] = OBJECT_ID(N'[dbo].[DispositivosRede]'))
    CREATE NONCLUSTERED INDEX [IX_DispositivosRede_Status_Tipo]
        ON [dbo].[DispositivosRede]([Status], [Tipo]);
GO

IF NOT EXISTS (SELECT 1 FROM [sys].[indexes] WHERE [name] = N'IX_ConfiguracoesSistema_Chave' AND [object_id] = OBJECT_ID(N'[dbo].[ConfiguracoesSistema]'))
    CREATE UNIQUE NONCLUSTERED INDEX [IX_ConfiguracoesSistema_Chave]
        ON [dbo].[ConfiguracoesSistema]([Chave]);
GO

IF NOT EXISTS (SELECT 1 FROM [sys].[indexes] WHERE [name] = N'IX_NotasFiscais_DataEmissao' AND [object_id] = OBJECT_ID(N'[dbo].[NotasFiscais]'))
    CREATE NONCLUSTERED INDEX [IX_NotasFiscais_DataEmissao]
        ON [dbo].[NotasFiscais]([DataEmissao] DESC);
GO

-- =============================================================================
-- 4. FUNÇÃO ESCALAR DE CÁLCULO DE RISCO DE IDENTIDADE (ZERO-TRUST SCORE)
-- =============================================================================

CREATE OR ALTER FUNCTION [dbo].[fn_CalcularScoreRiscoUsuario] (@UsuarioId INT)
RETURNS INT
AS
BEGIN
    DECLARE @Score INT = 10;
    DECLARE @Mfa BIT = 0;
    DECLARE @Ativo BIT = 1;
    DECLARE @FalhasRecentes INT = 0;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Usuarios] WHERE [Id] = @UsuarioId)
        RETURN 0;

    SELECT @Mfa = [MfaHabilitado], @Ativo = [Ativo]
    FROM [dbo].[Usuarios]
    WHERE [Id] = @UsuarioId;

    IF @Mfa = 0 SET @Score = @Score + 35;
    IF @Ativo = 0 SET @Score = @Score + 25;

    SELECT @FalhasRecentes = COUNT(1)
    FROM [dbo].[LogsAuditoria]
    WHERE [UsuarioId] = @UsuarioId
      AND [Tipo] IN (N'Warning', N'Error', N'Danger', N'Bloqueio')
      AND [DataHora] >= DATEADD(DAY, -7, GETUTCDATE());

    SET @Score = @Score + (@FalhasRecentes * 10);
    IF @Score > 100 SET @Score = 100;

    RETURN @Score;
END;
GO

-- =============================================================================
-- 5. VIEWS ANALÍTICAS (GOVERNANÇA RBAC, ENDPOINTS, DEPARTAMENTOS E SLA)
-- =============================================================================

CREATE OR ALTER VIEW [dbo].[vw_ResumoSegurancaDepartamental]
AS
SELECT
    ISNULL(NULLIF(u.[Departamento], N''), N'Não Informado') AS [Departamento],
    COUNT(u.[Id]) AS [TotalColaboradores],
    SUM(CASE WHEN u.[Ativo] = 1 THEN 1 ELSE 0 END) AS [ColaboradoresAtivos],
    SUM(CASE WHEN u.[MfaHabilitado] = 1 THEN 1 ELSE 0 END) AS [ComMfaAtivo]
FROM [dbo].[Usuarios] u
GROUP BY ISNULL(NULLIF(u.[Departamento], N''), N'Não Informado');
GO

CREATE OR ALTER VIEW [dbo].[vw_MatrizRbacUsuarios]
AS
SELECT
    u.[Id] AS [UsuarioId],
    u.[Nome],
    u.[Email],
    u.[Departamento],
    u.[Cargo],
    u.[Ativo],
    u.[MfaHabilitado],
    ISNULL(p.[Nome], N'Colaborador') AS [PerfilNome],
    ISNULL(STRING_AGG(perm.[Nome], N', '), N'usuarios.visualizar') AS [PermissoesAtribuidas]
FROM [dbo].[Usuarios] u
LEFT JOIN [dbo].[UsuarioPerfis] up ON u.[Id] = up.[UsuarioId]
LEFT JOIN [dbo].[Perfis] p ON up.[PerfilId] = p.[Id]
LEFT JOIN [dbo].[PerfilPermissoes] pp ON p.[Id] = pp.[PerfilId]
LEFT JOIN [dbo].[Permissoes] perm ON pp.[PermissaoId] = perm.[Id]
GROUP BY
    u.[Id], u.[Nome], u.[Email], u.[Departamento], u.[Cargo], u.[Ativo], u.[MfaHabilitado], p.[Nome];
GO

CREATE OR ALTER VIEW [dbo].[vw_ResumoSaudeEndpoints]
AS
SELECT
    ISNULL([Vlan], N'VLAN Padrão') AS [Vlan],
    [Tipo],
    COUNT([Id]) AS [TotalDispositivos],
    SUM(CASE WHEN [Status] = N'Saudavel' THEN 1 ELSE 0 END) AS [DispositivosSaudaveis],
    SUM(CASE WHEN [IsoladoRede] = 1 THEN 1 ELSE 0 END) AS [DispositivosIsolados],
    SUM(CASE WHEN [EdrAtivo] = 1 THEN 1 ELSE 0 END) AS [ComEdrAtivo],
    AVG(CAST([CpuUsoPercentual] AS FLOAT)) AS [MediaCpuPercentual],
    AVG(CAST([RamUsoPercentual] AS FLOAT)) AS [MediaRamPercentual],
    AVG(CAST([DiscoUsoPercentual] AS FLOAT)) AS [MediaDiscoPercentual],
    SUM([PatchesPendentes]) AS [TotalPatchesPendentes]
FROM [dbo].[DispositivosRede]
GROUP BY [Vlan], [Tipo];
GO

CREATE OR ALTER VIEW [dbo].[vw_MonitoramentoSlaChamados]
AS
SELECT
    c.[Id] AS [ChamadoId],
    c.[Assunto],
    c.[Categoria],
    c.[Prioridade],
    c.[Status],
    u.[Nome] AS [SolicitanteNome],
    u.[Email] AS [SolicitanteEmail],
    c.[DataAbertura],
    c.[PrazoSla],
    CASE
        WHEN c.[Status] IN (N'Fechado', N'Resolvido', N'Concluído') THEN N'SLA Cumprido'
        WHEN c.[PrazoSla] IS NOT NULL AND c.[PrazoSla] < GETUTCDATE() THEN N'SLA Violado'
        WHEN c.[PrazoSla] IS NOT NULL AND DATEDIFF(MINUTE, GETUTCDATE(), c.[PrazoSla]) <= 120 THEN N'Risco de Violação (<2h)'
        ELSE N'Dentro do Prazo'
    END AS [SituacaoSla]
FROM [dbo].[ChamadosSuporte] c
INNER JOIN [dbo].[Usuarios] u ON c.[UsuarioId] = u.[Id];
GO

-- =============================================================================
-- 6. TRIGGERS DE AUDITORIA AUTOMÁTICA E IMUTABILIDADE DE LOGS
-- =============================================================================

-- 6.1 Trigger: trg_AuditoriaStatusUsuario (compatível com EF Core HasTrigger)
CREATE OR ALTER TRIGGER [dbo].[trg_AuditoriaStatusUsuario]
ON [dbo].[Usuarios]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- Inclusão de novo usuário
    IF EXISTS (SELECT 1 FROM inserted) AND NOT EXISTS (SELECT 1 FROM deleted)
    BEGIN
        INSERT INTO [dbo].[LogsAuditoria] ([UsuarioId], [DataHora], [Acao], [Detalhe], [Tipo], [Origem])
        SELECT
            i.[Id],
            GETUTCDATE(),
            N'Criação de Usuário',
            CONCAT(N'Novo colaborador cadastrado: ', i.[Email], N' (Cargo: ', ISNULL(i.[Cargo], N'N/A'), N')'),
            N'Info',
            N'DB Trigger [trg_AuditoriaStatusUsuario]'
        FROM inserted i;
    END;

    -- Alteração cadastral ou de estado de segurança
    IF EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
    BEGIN
        INSERT INTO [dbo].[LogsAuditoria] ([UsuarioId], [DataHora], [Acao], [Detalhe], [Tipo], [Origem])
        SELECT
            i.[Id],
            GETUTCDATE(),
            N'Alteração de Conta / Segurança',
            CONCAT(N'Conta ', i.[Email], N' | Ativo: ', d.[Ativo], N'->', i.[Ativo], N' | MFA: ', d.[MfaHabilitado], N'->', i.[MfaHabilitado], N' | Cargo: ', ISNULL(i.[Cargo], N'')),
            CASE WHEN i.[Ativo] = 0 THEN N'Warning' ELSE N'Info' END,
            N'DB Trigger [trg_AuditoriaStatusUsuario]'
        FROM inserted i
        INNER JOIN deleted d ON i.[Id] = d.[Id];
    END;

    -- Remoção física (UsuarioId gravado como NULL para respeitar integridade referencial após DELETE)
    IF NOT EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
    BEGIN
        INSERT INTO [dbo].[LogsAuditoria] ([UsuarioId], [DataHora], [Acao], [Detalhe], [Tipo], [Origem])
        SELECT
            NULL,
            GETUTCDATE(),
            N'Exclusão Física de Usuário',
            CONCAT(N'Registro removido fisicamente da base (ID anterior: ', d.[Id], N', E-mail: ', d.[Email], N')'),
            N'Danger',
            N'DB Trigger [trg_AuditoriaStatusUsuario]'
        FROM deleted d;
    END;
END;
GO

-- 6.2 Trigger: trg_AuditoriaChamados
CREATE OR ALTER TRIGGER [dbo].[trg_AuditoriaChamados]
ON [dbo].[ChamadosSuporte]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[LogsAuditoria] ([UsuarioId], [DataHora], [Acao], [Detalhe], [Tipo], [Origem])
    SELECT
        i.[UsuarioId],
        GETUTCDATE(),
        N'Transição de Chamado',
        CONCAT(N'Chamado #', i.[Id], N' alterado de [', d.[Status], N'] para [', i.[Status], N']'),
        CASE WHEN i.[Status] IN (N'Fechado', N'Resolvido') THEN N'Info' ELSE N'Warning' END,
        N'DB Trigger [trg_AuditoriaChamados]'
    FROM inserted i
    INNER JOIN deleted d ON i.[Id] = d.[Id]
    WHERE ISNULL(i.[Status], N'') <> ISNULL(d.[Status], N'');
END;
GO

-- 6.3 Trigger: trg_AuditoriaFirewallWaf
CREATE OR ALTER TRIGGER [dbo].[trg_AuditoriaFirewallWaf]
ON [dbo].[FirewallRegras]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[LogsAuditoria] ([UsuarioId], [DataHora], [Acao], [Detalhe], [Tipo], [Origem])
    SELECT
        NULL,
        GETUTCDATE(),
        CASE WHEN i.[Ativo] = 1 THEN N'Trigger WAF: Regra de Bloqueio Ativada' ELSE N'Trigger WAF: Regra de Bloqueio Revogada' END,
        CONCAT(N'IP: ', i.[Ip], N' | Motivo: ', i.[Motivo], N' | Origem: ', ISNULL(i.[PaisOrigem], N'N/A')),
        CASE WHEN i.[Ativo] = 1 THEN N'Danger' ELSE N'Info' END,
        N'DB Trigger [trg_AuditoriaFirewallWaf]'
    FROM inserted i;
END;
GO

-- 6.4 Trigger: trg_AuditoriaConfig (mascara valores de chaves sensíveis)
CREATE OR ALTER TRIGGER [dbo].[trg_AuditoriaConfig]
ON [dbo].[ConfiguracoesSistema]
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM inserted)
    BEGIN
        INSERT INTO [dbo].[LogsAuditoria] ([UsuarioId], [DataHora], [Acao], [Detalhe], [Tipo], [Origem])
        SELECT
            NULL,
            GETUTCDATE(),
            N'Atualização de Parâmetro Global',
            CONCAT(
                N'Chave: ', i.[Chave], N' | Valor: ',
                CASE
                    WHEN UPPER(i.[Chave]) IN (N'API_GATEWAY_TOKEN', N'JWT_KEY', N'SMTP_PASSWORD', N'TURNSTILE_SECRETKEY', N'TURNSTILE_SECRET_KEY')
                        THEN N'***PROTEGIDO***'
                    ELSE LEFT(i.[Valor], 120)
                END
            ),
            N'Info',
            N'DB Trigger [trg_AuditoriaConfig]'
        FROM inserted i;
    END;
END;
GO

-- 6.5 Trigger: trg_ProtegerLogsImutaveis (impede adulteração ou exclusão na trilha de auditoria)
CREATE OR ALTER TRIGGER [dbo].[trg_ProtegerLogsImutaveis]
ON [dbo].[LogsAuditoria]
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    RAISERROR(N'Violação de Governança: Os registros da tabela LogsAuditoria são imutáveis para conformidade com auditoria e LGPD.', 16, 1);
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
END;
GO

-- =============================================================================
-- 7. STORED PROCEDURES TRANSACIONAIS (IAM, SECOPS, ITSM, CONTÁBIL E LGPD)
-- =============================================================================

-- 7.1 Procedure: sp_RegistrarLog
CREATE OR ALTER PROCEDURE [dbo].[sp_RegistrarLog]
    @UsuarioId INT = NULL,
    @Acao NVARCHAR(100),
    @Detalhe NVARCHAR(MAX),
    @Tipo NVARCHAR(50) = N'Info',
    @Origem NVARCHAR(50) = N'Sistema'
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ValidUserId INT = NULL;
    IF @UsuarioId IS NOT NULL AND EXISTS (SELECT 1 FROM [dbo].[Usuarios] WHERE [Id] = @UsuarioId)
        SET @ValidUserId = @UsuarioId;

    INSERT INTO [dbo].[LogsAuditoria] ([UsuarioId], [DataHora], [Acao], [Detalhe], [Tipo], [Origem])
    VALUES (@ValidUserId, GETUTCDATE(), @Acao, @Detalhe, @Tipo, @Origem);
END;
GO

-- 7.2 Procedure: sp_RegistrarLogAuditoria (interface compatível SIEM)
CREATE OR ALTER PROCEDURE [dbo].[sp_RegistrarLogAuditoria]
    @Acao NVARCHAR(100),
    @Detalhe NVARCHAR(MAX),
    @Tipo NVARCHAR(50) = N'Info',
    @Origem NVARCHAR(50) = N'System Core',
    @UsuarioId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    EXEC [dbo].[sp_RegistrarLog]
        @UsuarioId = @UsuarioId,
        @Acao = @Acao,
        @Detalhe = @Detalhe,
        @Tipo = @Tipo,
        @Origem = @Origem;
END;
GO

-- 7.3 Procedure: sp_InativarColaborador
CREATE OR ALTER PROCEDURE [dbo].[sp_InativarColaborador]
    @UsuarioId INT,
    @AdminId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM [dbo].[Usuarios] WHERE [Id] = @UsuarioId)
        BEGIN
            RAISERROR(N'Usuário não encontrado.', 16, 1);
        END;

        UPDATE [dbo].[Usuarios]
        SET [Ativo] = 0,
            [RefreshToken] = N'',
            [RefreshTokenExpiracao] = NULL,
            [ResetSenhaToken] = N'',
            [ResetSenhaExpiracao] = NULL
        WHERE [Id] = @UsuarioId;

        DELETE FROM [dbo].[MfaSessoes] WHERE [UsuarioId] = @UsuarioId;

        DECLARE @DetalheMsg NVARCHAR(300) = CONCAT(N'Colaborador ID ', @UsuarioId, N' inativado e sessões revogadas via sp_InativarColaborador.');
        EXEC [dbo].[sp_RegistrarLog]
            @UsuarioId = @AdminId,
            @Acao = N'Inativação de Colaborador',
            @Detalhe = @DetalheMsg,
            @Tipo = N'Warning',
            @Origem = N'sp_InativarColaborador';

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- 7.4 Procedure: sp_AnonimizarTitularLGPD (Direito de Eliminação / Anonimização - Art. 18 LGPD)
CREATE OR ALTER PROCEDURE [dbo].[sp_AnonimizarTitularLGPD]
    @UsuarioId INT,
    @AdminId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM [dbo].[Usuarios] WHERE [Id] = @UsuarioId)
        BEGIN
            RAISERROR(N'Titular não localizado para anonimização.', 16, 1);
        END;

        UPDATE [dbo].[Usuarios]
        SET [Nome] = CONCAT(N'Titular Anonimizado #', [Id]),
            [Email] = CONCAT(N'anonimizado_', [Id], N'@lgpd.corvuxiam.tech'),
            [CPF] = N'',
            [RG] = N'ANONIMIZADO',
            [Telefone] = N'',
            [Endereco] = N'DADOS_ANONIMIZADOS_LGPD',
            [DataNascimento] = NULL,
            [MfaHabilitado] = 0,
            [MfaSecretKey] = N'',
            [RefreshToken] = N'',
            [RefreshTokenExpiracao] = NULL,
            [ResetSenhaToken] = N'',
            [ResetSenhaExpiracao] = NULL,
            [Ativo] = 0
        WHERE [Id] = @UsuarioId;

        DELETE FROM [dbo].[MfaSessoes] WHERE [UsuarioId] = @UsuarioId;

        DECLARE @MsgLgpd NVARCHAR(300) = CONCAT(N'Anonimização irreversível aplicada ao titular ID ', @UsuarioId, N' conforme Art. 18 da Lei nº 13.709/2018.');
        EXEC [dbo].[sp_RegistrarLog]
            @UsuarioId = @AdminId,
            @Acao = N'Anonimização de Titular (LGPD Art. 18)',
            @Detalhe = @MsgLgpd,
            @Tipo = N'Warning',
            @Origem = N'sp_AnonimizarTitularLGPD';

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- 7.5 Procedure: sp_ObterResumoAuditoria
CREATE OR ALTER PROCEDURE [dbo].[sp_ObterResumoAuditoria]
    @DiasRetroativos INT = 30
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @DataLimite DATETIME2 = DATEADD(DAY, -@DiasRetroativos, GETUTCDATE());

    SELECT
        [Tipo],
        [Origem],
        COUNT(*) AS [TotalEventos],
        MIN([DataHora]) AS [PrimeiroRegistro],
        MAX([DataHora]) AS [UltimoRegistro]
    FROM [dbo].[LogsAuditoria]
    WHERE [DataHora] >= @DataLimite
    GROUP BY [Tipo], [Origem]
    ORDER BY [TotalEventos] DESC;

    SELECT TOP (10)
        [Id],
        [UsuarioId],
        [DataHora],
        [Acao],
        [Detalhe],
        [Tipo],
        [Origem]
    FROM [dbo].[LogsAuditoria]
    WHERE [DataHora] >= @DataLimite
      AND [Tipo] IN (N'Danger', N'Warning', N'SecurityAlert', N'Bloqueio')
    ORDER BY [DataHora] DESC;
END;
GO

-- 7.6 Procedure: sp_ManutencaoSegurancaIAM
CREATE OR ALTER PROCEDURE [dbo].[sp_ManutencaoSegurancaIAM]
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [dbo].[Usuarios]
    SET [ResetSenhaToken] = N'',
        [ResetSenhaExpiracao] = NULL
    WHERE [ResetSenhaExpiracao] IS NOT NULL AND [ResetSenhaExpiracao] < GETUTCDATE();

    UPDATE [dbo].[Usuarios]
    SET [RefreshToken] = N'',
        [RefreshTokenExpiracao] = NULL
    WHERE [RefreshTokenExpiracao] IS NOT NULL AND [RefreshTokenExpiracao] < GETUTCDATE();

    UPDATE [dbo].[Usuarios]
    SET [TentativasLogin] = 0,
        [DataDesbloqueio] = NULL
    WHERE [DataDesbloqueio] IS NOT NULL AND [DataDesbloqueio] < GETUTCDATE();

    DELETE FROM [dbo].[MfaSessoes]
    WHERE [ExpiraEm] < GETUTCDATE();
END;
GO

-- 7.7 Procedure: sp_BloquearIpFirewall
CREATE OR ALTER PROCEDURE [dbo].[sp_BloquearIpFirewall]
    @Ip NVARCHAR(50),
    @Motivo NVARCHAR(200),
    @PaisOrigem NVARCHAR(10) = N'BR',
    @CriadoPor NVARCHAR(100) = N'SecOps Admin',
    @AdminUsuarioId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        IF EXISTS (SELECT 1 FROM [dbo].[FirewallRegras] WHERE [Ip] = @Ip)
        BEGIN
            UPDATE [dbo].[FirewallRegras]
            SET [Ativo] = 1,
                [Motivo] = @Motivo,
                [DataBloqueio] = GETUTCDATE(),
                [CriadoPor] = @CriadoPor
            WHERE [Ip] = @Ip;
        END
        ELSE
        BEGIN
            INSERT INTO [dbo].[FirewallRegras] ([Ip], [Motivo], [PaisOrigem], [DataBloqueio], [Ativo], [CriadoPor])
            VALUES (@Ip, @Motivo, @PaisOrigem, GETUTCDATE(), 1, @CriadoPor);
        END;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- 7.8 Procedure: sp_AbrirChamadoSuporteComSla
CREATE OR ALTER PROCEDURE [dbo].[sp_AbrirChamadoSuporteComSla]
    @UsuarioId INT,
    @Assunto NVARCHAR(100),
    @Descricao NVARCHAR(MAX),
    @Categoria NVARCHAR(50) = N'Acesso',
    @Prioridade NVARCHAR(20) = N'Normal',
    @ClientIdempotencyKey NVARCHAR(100) = NULL,
    @NovoChamadoId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @HorasSla INT = CASE @Prioridade
        WHEN N'Critica' THEN 2
        WHEN N'Alta' THEN 4
        WHEN N'Normal' THEN 8
        ELSE 24
    END;

    DECLARE @Agora DATETIME2 = GETUTCDATE();
    DECLARE @Prazo DATETIME2 = DATEADD(HOUR, @HorasSla, @Agora);

    INSERT INTO [dbo].[ChamadosSuporte]
        ([UsuarioId], [Assunto], [Descricao], [Categoria], [Prioridade], [Status], [DataAbertura], [DataAtualizacao], [PrazoSla], [Solucao], [ClientIdempotencyKey])
    VALUES
        (@UsuarioId, @Assunto, @Descricao, @Categoria, @Prioridade, N'Aberto', @Agora, @Agora, @Prazo, N'', @ClientIdempotencyKey);

    SET @NovoChamadoId = CAST(SCOPE_IDENTITY() AS INT);

    EXEC [dbo].[sp_RegistrarLog]
        @UsuarioId = @UsuarioId,
        @Acao = N'Abertura de Chamado via Procedure',
        @Detalhe = @Assunto,
        @Tipo = N'Info',
        @Origem = N'sp_AbrirChamadoSuporteComSla';
END;
GO

-- 7.9 Procedure: sp_RevogarSessoesUsuario
CREATE OR ALTER PROCEDURE [dbo].[sp_RevogarSessoesUsuario]
    @UsuarioId INT,
    @AdminExecutorId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Usuarios]
    SET [RefreshToken] = N'',
        [RefreshTokenExpiracao] = NULL,
        [ResetSenhaToken] = N'',
        [ResetSenhaExpiracao] = NULL
    WHERE [Id] = @UsuarioId;

    DELETE FROM [dbo].[MfaSessoes]
    WHERE [UsuarioId] = @UsuarioId;

    EXEC [dbo].[sp_RegistrarLog]
        @UsuarioId = @AdminExecutorId,
        @Acao = N'Revogação Zero-Trust de Sessão',
        @Detalhe = N'Refresh tokens, tokens de recuperação e sessões MFA revogados via sp_RevogarSessoesUsuario.',
        @Tipo = N'Warning',
        @Origem = N'sp_RevogarSessoesUsuario';
END;
GO

-- 7.10 Procedure: sp_ProcessarFechamentoContabil
CREATE OR ALTER PROCEDURE [dbo].[sp_ProcessarFechamentoContabil]
    @MesAno NVARCHAR(20),
    @ReceitaBruta DECIMAL(18,2),
    @DespesasOperacionais DECIMAL(18,2),
    @UsuarioId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @ValidUserId INT = NULL;
        IF @UsuarioId IS NOT NULL AND EXISTS (SELECT 1 FROM [dbo].[Usuarios] WHERE [Id] = @UsuarioId)
            SET @ValidUserId = @UsuarioId;

        DECLARE @LucroLiquido DECIMAL(18,2) = @ReceitaBruta - @DespesasOperacionais;

        INSERT INTO [dbo].[FechamentosMensais]
            ([MesAno], [ReceitaBruta], [DespesasOperacionais], [LucroLiquido], [DataFechamento], [Status], [FechadoPorUsuarioId])
        VALUES
            (@MesAno, @ReceitaBruta, @DespesasOperacionais, @LucroLiquido, GETUTCDATE(), N'Fechado', @ValidUserId);

        EXEC [dbo].[sp_RegistrarLog]
            @UsuarioId = @ValidUserId,
            @Acao = N'Fechamento Contábil via Procedure',
            @Detalhe = @MesAno,
            @Tipo = N'Info',
            @Origem = N'sp_ProcessarFechamentoContabil';

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- 7.11 Procedure: sp_ExecutarManutencaoLgpd (expurgo de artefatos efêmeros sem violar imutabilidade de LogsAuditoria)
CREATE OR ALTER PROCEDURE [dbo].[sp_ExecutarManutencaoLgpd]
    @AdminUsuarioId INT = NULL,
    @RegistrosProcessados INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @RegistrosProcessados = 0;

    DELETE FROM [dbo].[MfaSessoes]
    WHERE [ExpiraEm] < GETUTCDATE();
    SET @RegistrosProcessados = @RegistrosProcessados + @@ROWCOUNT;

    UPDATE [dbo].[Usuarios]
    SET [ResetSenhaToken] = N'',
        [ResetSenhaExpiracao] = NULL
    WHERE [ResetSenhaExpiracao] IS NOT NULL AND [ResetSenhaExpiracao] < GETUTCDATE();
    SET @RegistrosProcessados = @RegistrosProcessados + @@ROWCOUNT;

    UPDATE [dbo].[Usuarios]
    SET [RefreshToken] = N'',
        [RefreshTokenExpiracao] = NULL
    WHERE [RefreshTokenExpiracao] IS NOT NULL AND [RefreshTokenExpiracao] < GETUTCDATE();
    SET @RegistrosProcessados = @RegistrosProcessados + @@ROWCOUNT;

    DECLARE @Msg NVARCHAR(300) = CONCAT(N'Higienização LGPD de sessões e tokens expirados concluída. Registros processados: ', @RegistrosProcessados);
    EXEC [dbo].[sp_RegistrarLog]
        @UsuarioId = @AdminUsuarioId,
        @Acao = N'Manutenção LGPD de Tokens Efêmeros',
        @Detalhe = @Msg,
        @Tipo = N'Info',
        @Origem = N'sp_ExecutarManutencaoLgpd';
END;
GO

-- 7.12 Procedure: sp_ObterMetricasSecOpsDashboard
CREATE OR ALTER PROCEDURE [dbo].[sp_ObterMetricasSecOpsDashboard]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        (SELECT COUNT(1) FROM [dbo].[Usuarios] WHERE [Ativo] = 1) AS [UsuariosAtivos],
        (SELECT COUNT(1) FROM [dbo].[Usuarios] WHERE [Ativo] = 1 AND [MfaHabilitado] = 1) AS [UsuariosComMfa],
        (SELECT COUNT(1) FROM [dbo].[ChamadosSuporte] WHERE [Status] NOT IN (N'Fechado', N'Resolvido', N'Concluído')) AS [ChamadosAbertos],
        (SELECT COUNT(1) FROM [dbo].[FirewallRegras] WHERE [Ativo] = 1) AS [IpsBloqueadosWaf],
        (SELECT COUNT(1) FROM [dbo].[DispositivosRede] WHERE [Status] = N'Saudavel' AND [IsoladoRede] = 0) AS [EndpointsSaudaveis],
        (SELECT COUNT(1) FROM [dbo].[DispositivosRede] WHERE [IsoladoRede] = 1 OR [Status] = N'Critico') AS [EndpointsCriticosOuIsolados],
        (SELECT COUNT(1) FROM [dbo].[LogsAuditoria] WHERE [DataHora] >= DATEADD(HOUR, -24, GETUTCDATE()) AND [Tipo] IN (N'Warning', N'Error', N'Danger', N'Bloqueio')) AS [IncidentesUltimas24h];
END;
GO

-- =============================================================================
-- 8. SINCRONIZAÇÃO DE MIGRAÇÕES EF CORE E CARGA INICIAL RBAC (SEM SENHAS PADRÃO)
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260913183436_InitialSqlServer')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260913183436_InitialSqlServer', N'8.0.0');
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260913215728_AddChamadoWorkflow')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260913215728_AddChamadoWorkflow', N'8.0.0');
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260913220030_SeedPermissions')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260913220030_SeedPermissions', N'8.0.0');
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260920193416_AddRefreshToken')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260920193416_AddRefreshToken', N'8.0.0');
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20261021120000_AddChamadoIdempotencyKey')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261021120000_AddChamadoIdempotencyKey', N'8.0.0');
IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20261023020728_AddMfaSessionsAndUniqueUserIdentifiers')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20261023020728_AddMfaSessionsAndUniqueUserIdentifiers', N'8.0.0');
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Perfis] WHERE [Id] = 1)
BEGIN
    SET IDENTITY_INSERT [dbo].[Perfis] ON;
    INSERT INTO [dbo].[Perfis] ([Id], [Nome], [Descricao], [Ativo]) VALUES
        (1, N'Admin', N'Administrador do Sistema com Acesso Total', 1),
        (2, N'Recursos Humanos', N'Gestão de Colaboradores, Admissão, Férias e Folha (RH)', 1),
        (3, N'Contabilidade', N'Gestão Fiscal, NFS-e e Fechamento Mensal DRE', 1),
        (4, N'Help Desk', N'Suporte Técnico de TI, Triagem e Atendimento de SLA', 1),
        (5, N'Colaborador', N'Usuário Corporativo Padrão com Privilégio Mínimo', 1);
    SET IDENTITY_INSERT [dbo].[Perfis] OFF;
END;
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[Permissoes] WHERE [Id] = 1)
BEGIN
    SET IDENTITY_INSERT [dbo].[Permissoes] ON;
    INSERT INTO [dbo].[Permissoes] ([Id], [Nome], [Descricao]) VALUES
        (1, N'usuarios.visualizar', N'Visualizar listagem de colaboradores'),
        (2, N'usuarios.gerenciar', N'Criar, editar e inativar colaboradores'),
        (3, N'perfis.gerenciar', N'Gerenciar perfis e matriz de acesso RBAC'),
        (4, N'chamados.atender', N'Triar e atender chamados de suporte'),
        (5, N'logs.visualizar', N'Consultar e exportar trilha de auditoria'),
        (6, N'politicas.gerenciar', N'Configurar parâmetros de segurança e IAM');
    SET IDENTITY_INSERT [dbo].[Permissoes] OFF;
END;
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[PerfilPermissoes] WHERE [PerfilId] = 1 AND [PermissaoId] = 1)
BEGIN
    INSERT INTO [dbo].[PerfilPermissoes] ([PerfilId], [PermissaoId]) VALUES
        (1, 1), (1, 2), (1, 3), (1, 4), (1, 5), (1, 6),
        (2, 1), (2, 2),
        (4, 4),
        (5, 1);
END;
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[PoliticasAcesso] WHERE [Nome] = N'PoliticaZeroTrustPadrao')
BEGIN
    INSERT INTO [dbo].[PoliticasAcesso] ([Nome], [Descricao], [Ativa]) VALUES
        (N'PoliticaZeroTrustPadrao', N'Política corporativa Zero Trust: bloqueio após 5 falhas, MFA TOTP, rotação de Refresh Token e trilha imutável.', 1);
END;
GO

-- Nenhuma credencial ou senha padrão é inserida neste script.
-- O administrador inicial é provisionado na inicialização da aplicação exclusivamente
-- mediante a variável de ambiente obrigatória ADMIN_INITIAL_PASSWORD.

PRINT N'Instalação limpa do banco CorvusIAMDb (16 tabelas, índices, função, 4 views, 5 triggers e 12 procedures) concluída com sucesso!';
GO
