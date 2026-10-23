-- =============================================================================
-- CorvuxIAM - Script Oficial de Criação e Configuração do Banco de Dados
-- Sistema de Gestão de Identidade e Acesso (PIM IV - UNIP)
-- Banco de Dados: Microsoft SQL Server 2019 / 2022
-- Domínio Oficial de Produção: https://corvuxiam.tech
-- =============================================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'CorvusIAMDb')
BEGIN
    CREATE DATABASE [CorvusIAMDb];
END;
GO

USE [CorvusIAMDb];
GO

-- 1. Tabela de Histórico de Migrações do Entity Framework Core
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

-- =============================================================================
-- 2. CRIAÇÃO DAS TABELAS
-- =============================================================================

-- Tabela: ConfiguracoesSistema
IF OBJECT_ID(N'[ConfiguracoesSistema]') IS NULL
BEGIN
    CREATE TABLE [ConfiguracoesSistema] (
        [Id] int NOT NULL IDENTITY(1,1),
        [Chave] nvarchar(100) NOT NULL,
        [Valor] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_ConfiguracoesSistema] PRIMARY KEY ([Id])
    );
END;
GO

-- Tabela: Perfis
IF OBJECT_ID(N'[Perfis]') IS NULL
BEGIN
    CREATE TABLE [Perfis] (
        [Id] int NOT NULL IDENTITY(1,1),
        [Nome] nvarchar(50) NOT NULL,
        [Descricao] nvarchar(200) NOT NULL,
        [Ativo] bit NOT NULL CONSTRAINT [DF_Perfis_Ativo] DEFAULT 1,
        CONSTRAINT [PK_Perfis] PRIMARY KEY ([Id])
    );
END;
GO

-- Tabela: Permissoes
IF OBJECT_ID(N'[Permissoes]') IS NULL
BEGIN
    CREATE TABLE [Permissoes] (
        [Id] int NOT NULL IDENTITY(1,1),
        [Nome] nvarchar(50) NOT NULL,
        [Descricao] nvarchar(200) NOT NULL,
        CONSTRAINT [PK_Permissoes] PRIMARY KEY ([Id])
    );
END;
GO

-- Tabela: PoliticasAcesso
IF OBJECT_ID(N'[PoliticasAcesso]') IS NULL
BEGIN
    CREATE TABLE [PoliticasAcesso] (
        [Id] int NOT NULL IDENTITY(1,1),
        [Nome] nvarchar(100) NOT NULL,
        [Descricao] nvarchar(500) NOT NULL,
        [Ativa] bit NOT NULL CONSTRAINT [DF_PoliticasAcesso_Ativa] DEFAULT 1,
        CONSTRAINT [PK_PoliticasAcesso] PRIMARY KEY ([Id])
    );
END;
GO

-- Tabela: Usuarios
IF OBJECT_ID(N'[Usuarios]') IS NULL
BEGIN
    CREATE TABLE [Usuarios] (
        [Id] int NOT NULL IDENTITY(1,1),
        [Nome] nvarchar(100) NOT NULL,
        [Email] nvarchar(100) NOT NULL,
        [SenhaHash] nvarchar(max) NOT NULL,
        [Ativo] bit NOT NULL CONSTRAINT [DF_Usuarios_Ativo] DEFAULT 1,
        [TentativasLogin] int NOT NULL CONSTRAINT [DF_Usuarios_TentativasLogin] DEFAULT 0,
        [DataDesbloqueio] datetime2 NULL,
        [ResetSenhaToken] nvarchar(max) NOT NULL CONSTRAINT [DF_Usuarios_ResetSenhaToken] DEFAULT '',
        [ResetSenhaExpiracao] datetime2 NULL,
        [RefreshToken] nvarchar(max) NOT NULL CONSTRAINT [DF_Usuarios_RefreshToken] DEFAULT '',
        [RefreshTokenExpiracao] datetime2 NULL,
        [MfaHabilitado] bit NOT NULL CONSTRAINT [DF_Usuarios_MfaHabilitado] DEFAULT 0,
        [MfaSecretKey] nvarchar(100) NOT NULL CONSTRAINT [DF_Usuarios_MfaSecretKey] DEFAULT '',
        [CPF] nvarchar(14) NOT NULL CONSTRAINT [DF_Usuarios_CPF] DEFAULT '',
        [DataNascimento] datetime2 NULL,
        [Cargo] nvarchar(100) NOT NULL CONSTRAINT [DF_Usuarios_Cargo] DEFAULT '',
        [DataContratacao] datetime2 NULL,
        [RG] nvarchar(20) NOT NULL CONSTRAINT [DF_Usuarios_RG] DEFAULT '',
        [Telefone] nvarchar(20) NOT NULL CONSTRAINT [DF_Usuarios_Telefone] DEFAULT '',
        [Departamento] nvarchar(100) NOT NULL CONSTRAINT [DF_Usuarios_Departamento] DEFAULT '',
        [Endereco] nvarchar(200) NOT NULL CONSTRAINT [DF_Usuarios_Endereco] DEFAULT '',
        CONSTRAINT [PK_Usuarios] PRIMARY KEY ([Id])
    );
END;
GO

-- Tabela: PerfilPermissoes (N:N)
IF OBJECT_ID(N'[PerfilPermissoes]') IS NULL
BEGIN
    CREATE TABLE [PerfilPermissoes] (
        [PerfilId] int NOT NULL,
        [PermissaoId] int NOT NULL,
        CONSTRAINT [PK_PerfilPermissoes] PRIMARY KEY ([PerfilId], [PermissaoId]),
        CONSTRAINT [FK_PerfilPermissoes_Perfis_PerfilId] FOREIGN KEY ([PerfilId]) REFERENCES [Perfis] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PerfilPermissoes_Permissoes_PermissaoId] FOREIGN KEY ([PermissaoId]) REFERENCES [Permissoes] ([Id]) ON DELETE CASCADE
    );
END;
GO

-- Tabela: ChamadosSuporte
IF OBJECT_ID(N'[ChamadosSuporte]') IS NULL
BEGIN
    CREATE TABLE [ChamadosSuporte] (
        [Id] int NOT NULL IDENTITY(1,1),
        [UsuarioId] int NOT NULL,
        [Assunto] nvarchar(100) NOT NULL,
        [Descricao] nvarchar(max) NOT NULL,
        [Categoria] nvarchar(50) NOT NULL CONSTRAINT [DF_ChamadosSuporte_Categoria] DEFAULT 'Acesso',
        [Prioridade] nvarchar(20) NOT NULL CONSTRAINT [DF_ChamadosSuporte_Prioridade] DEFAULT 'Normal',
        [Status] nvarchar(50) NOT NULL CONSTRAINT [DF_ChamadosSuporte_Status] DEFAULT 'Aberto',
        [ResponsavelId] int NULL,
        [DataAbertura] datetime2 NOT NULL CONSTRAINT [DF_ChamadosSuporte_DataAbertura] DEFAULT GETUTCDATE(),
        [DataAtualizacao] datetime2 NOT NULL CONSTRAINT [DF_ChamadosSuporte_DataAtualizacao] DEFAULT GETUTCDATE(),
        [PrazoSla] datetime2 NULL,
        [DataResolucao] datetime2 NULL,
        [DataFechamento] datetime2 NULL,
        [Solucao] nvarchar(max) NOT NULL CONSTRAINT [DF_ChamadosSuporte_Solucao] DEFAULT '',
        [AnexoUrl] nvarchar(500) NULL,
        [AnexoNomeOriginal] nvarchar(255) NULL,
        [ClientIdempotencyKey] nvarchar(100) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ChamadosSuporte] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChamadosSuporte_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ChamadosSuporte_Usuarios_ResponsavelId] FOREIGN KEY ([ResponsavelId]) REFERENCES [Usuarios] ([Id]) ON DELETE NO ACTION
    );
END;
GO

-- Tabela: LogsAuditoria
IF OBJECT_ID(N'[LogsAuditoria]') IS NULL
BEGIN
    CREATE TABLE [LogsAuditoria] (
        [Id] int NOT NULL IDENTITY(1,1),
        [UsuarioId] int NULL,
        [DataHora] datetime2 NOT NULL CONSTRAINT [DF_LogsAuditoria_DataHora] DEFAULT GETUTCDATE(),
        [Acao] nvarchar(100) NOT NULL,
        [Detalhe] nvarchar(max) NOT NULL,
        [Tipo] nvarchar(50) NOT NULL,
        [Origem] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_LogsAuditoria] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LogsAuditoria_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id])
    );
END;
GO

-- Tabela: UsuarioPerfis (N:N)
IF OBJECT_ID(N'[UsuarioPerfis]') IS NULL
BEGIN
    CREATE TABLE [UsuarioPerfis] (
        [UsuarioId] int NOT NULL,
        [PerfilId] int NOT NULL,
        CONSTRAINT [PK_UsuarioPerfis] PRIMARY KEY ([UsuarioId], [PerfilId]),
        CONSTRAINT [FK_UsuarioPerfis_Perfis_PerfilId] FOREIGN KEY ([PerfilId]) REFERENCES [Perfis] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UsuarioPerfis_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE CASCADE
    );
END;
GO

-- =============================================================================
-- 3. REGISTRO DE MIGRAÇÕES DO ENTITY FRAMEWORK CORE
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260913183436_InitialSqlServer')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260913183436_InitialSqlServer', N'8.0.0');
END;
GO

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260913215728_AddChamadoWorkflow')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260913215728_AddChamadoWorkflow', N'8.0.0');
END;
GO

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260913220030_SeedPermissions')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260913220030_SeedPermissions', N'8.0.0');
END;
GO

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260920193416_AddRefreshToken')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260920193416_AddRefreshToken', N'8.0.0');
END;
GO

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261021120000_AddChamadoIdempotencyKey')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261021120000_AddChamadoIdempotencyKey', N'8.0.0');
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChamadosSuporte_UsuarioId_ClientIdempotencyKey' AND object_id = OBJECT_ID('ChamadosSuporte'))
    CREATE UNIQUE INDEX [IX_ChamadosSuporte_UsuarioId_ClientIdempotencyKey]
        ON [ChamadosSuporte]([UsuarioId], [ClientIdempotencyKey])
        WHERE [ClientIdempotencyKey] IS NOT NULL;
GO

-- =============================================================================
-- 4. STORED PROCEDURES (Requisito Acadêmico PIM IV - UNIP)
-- =============================================================================

IF OBJECT_ID(N'[MfaSessoes]') IS NULL
BEGIN
    CREATE TABLE [MfaSessoes] (
        [Id] int NOT NULL IDENTITY(1,1),
        [TokenHash] nvarchar(128) NOT NULL,
        [UsuarioId] int NOT NULL,
        [Email] nvarchar(100) NOT NULL,
        [ExpiraEm] datetime2 NOT NULL,
        [Tentativas] int NOT NULL CONSTRAINT [DF_MfaSessoes_Tentativas] DEFAULT 0,
        [Canal] nvarchar(20) NOT NULL CONSTRAINT [DF_MfaSessoes_Canal] DEFAULT 'API',
        CONSTRAINT [PK_MfaSessoes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MfaSessoes_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MfaSessoes_TokenHash' AND object_id = OBJECT_ID('MfaSessoes'))
    CREATE UNIQUE INDEX [IX_MfaSessoes_TokenHash] ON [MfaSessoes]([TokenHash]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MfaSessoes_UsuarioId' AND object_id = OBJECT_ID('MfaSessoes'))
    CREATE INDEX [IX_MfaSessoes_UsuarioId] ON [MfaSessoes]([UsuarioId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Usuarios_CPF' AND object_id = OBJECT_ID('Usuarios'))
    CREATE UNIQUE INDEX [IX_Usuarios_CPF] ON [Usuarios]([CPF]) WHERE [CPF] IS NOT NULL AND [CPF] <> '';
GO

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261023020728_AddMfaSessionsAndUniqueUserIdentifiers')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261023020728_AddMfaSessionsAndUniqueUserIdentifiers', N'8.0.0');
END;
GO

-- Stored Procedure 1: sp_RegistrarLog
-- Responsável pelo registro centralizado e seguro de eventos de auditoria
CREATE OR ALTER PROCEDURE sp_RegistrarLog
    @UsuarioId INT = NULL,
    @Acao NVARCHAR(100),
    @Detalhe NVARCHAR(MAX),
    @Tipo NVARCHAR(50) = 'Info',
    @Origem NVARCHAR(50) = 'Sistema'
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO LogsAuditoria (UsuarioId, DataHora, Acao, Detalhe, Tipo, Origem)
    VALUES (@UsuarioId, GETUTCDATE(), @Acao, @Detalhe, @Tipo, @Origem);
END;
GO

-- Stored Procedure 2: sp_InativarColaborador
-- Realiza Soft Delete em conformidade com a LGPD e registra trilha de auditoria
CREATE OR ALTER PROCEDURE sp_InativarColaborador
    @UsuarioId INT,
    @AdminId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;
    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM Usuarios WHERE Id = @UsuarioId)
        BEGIN
            RAISERROR('Usuário não encontrado.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        -- 1. Inativa o colaborador e revoga sessões/tokens ativos
        UPDATE Usuarios
        SET Ativo = 0,
            RefreshToken = '',
            RefreshTokenExpiracao = NULL,
            ResetSenhaToken = '',
            ResetSenhaExpiracao = NULL
        WHERE Id = @UsuarioId;

        -- 2. Registra na auditoria
        INSERT INTO LogsAuditoria (UsuarioId, DataHora, Acao, Detalhe, Tipo, Origem)
        VALUES (
            @AdminId,
            GETUTCDATE(),
            'Inativação de Colaborador',
            CONCAT('Colaborador ID ', @UsuarioId, ' inativado via Stored Procedure sp_InativarColaborador'),
            'Warning',
            'Stored Procedure'
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

-- Stored Procedure 3: sp_ObterResumoAuditoria
-- Fornece consolidação analítica e métricas de incidentes para relatórios de conformidade
CREATE OR ALTER PROCEDURE sp_ObterResumoAuditoria
    @DiasRetroativos INT = 30
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @DataLimite DATETIME2 = DATEADD(DAY, -@DiasRetroativos, GETUTCDATE());

    -- Relatório 1: Quantitativo por Tipo e Origem
    SELECT 
        Tipo,
        Origem,
        COUNT(*) AS TotalEventos,
        MIN(DataHora) AS PrimeiroRegistro,
        MAX(DataHora) AS UltimoRegistro
    FROM LogsAuditoria
    WHERE DataHora >= @DataLimite
    GROUP BY Tipo, Origem
    ORDER BY TotalEventos DESC;

    -- Relatório 2: Top 10 Eventos Críticos Mais Recentes
    SELECT TOP 10
        Id,
        UsuarioId,
        DataHora,
        Acao,
        Detalhe,
        Tipo,
        Origem
    FROM LogsAuditoria
    WHERE DataHora >= @DataLimite 
      AND Tipo IN ('Danger', 'Warning', 'SecurityAlert')
    ORDER BY DataHora DESC;
END;
GO

-- =============================================================================
-- 5. TRIGGERS (Requisito Acadêmico PIM IV - UNIP)
-- =============================================================================

-- Trigger 1: trg_AuditoriaUsuarios
-- Disparada automaticamente após modificações cadastrais na tabela Usuarios
CREATE OR ALTER TRIGGER trg_AuditoriaUsuarios
ON Usuarios
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- Caso 1: INSERT
    IF EXISTS (SELECT 1 FROM inserted) AND NOT EXISTS (SELECT 1 FROM deleted)
    BEGIN
        INSERT INTO LogsAuditoria (UsuarioId, DataHora, Acao, Detalhe, Tipo, Origem)
        SELECT 
            i.Id,
            GETUTCDATE(),
            'Criação de Usuário',
            CONCAT('Novo colaborador cadastrado: ', i.Email, ' (Nome: ', i.Nome, ')'),
            'Info',
            'DB Trigger [trg_AuditoriaUsuarios]'
        FROM inserted i;
    END;

    -- Caso 2: UPDATE
    IF EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
    BEGIN
        INSERT INTO LogsAuditoria (UsuarioId, DataHora, Acao, Detalhe, Tipo, Origem)
        SELECT 
            i.Id,
            GETUTCDATE(),
            'Alteração Cadastral',
            CONCAT('Usuário ', i.Email, ' modificado. Ativo: ', CAST(i.Ativo AS VARCHAR(1)), ', MFA: ', CAST(i.MfaHabilitado AS VARCHAR(1))),
            'Warning',
            'DB Trigger [trg_AuditoriaUsuarios]'
        FROM inserted i;
    END;

    -- Caso 3: DELETE
    IF NOT EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
    BEGIN
        INSERT INTO LogsAuditoria (UsuarioId, DataHora, Acao, Detalhe, Tipo, Origem)
        SELECT 
            d.Id,
            GETUTCDATE(),
            'Exclusão Física de Usuário',
            CONCAT('Registro removido fisicamente da base: ', d.Email),
            'Danger',
            'DB Trigger [trg_AuditoriaUsuarios]'
        FROM deleted d;
    END;
END;
GO

-- Trigger 2: trg_AuditoriaChamados
-- Disparada em alterações de chamado para registrar transições e fechar data de resolução
CREATE OR ALTER TRIGGER trg_AuditoriaChamados
ON ChamadosSuporte
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Registra transições de status
    INSERT INTO LogsAuditoria (UsuarioId, DataHora, Acao, Detalhe, Tipo, Origem)
    SELECT 
        i.UsuarioId,
        GETUTCDATE(),
        'Transição de Chamado',
        CONCAT('Chamado #', i.Id, ' alterado de status [', d.Status, '] para [', i.Status, ']'),
        CASE WHEN i.Status IN ('Fechado', 'Resolvido') THEN 'Info' ELSE 'Warning' END,
        'DB Trigger [trg_AuditoriaChamados]'
    FROM inserted i
    INNER JOIN deleted d ON i.Id = d.Id
    WHERE i.Status <> d.Status;
END;
GO

-- =============================================================================
-- 6. CARGA INICIAL DE DADOS (SEEDS)
-- =============================================================================

-- Carga de Perfis
IF NOT EXISTS (SELECT 1 FROM Perfis WHERE Id = 1)
BEGIN
    SET IDENTITY_INSERT Perfis ON;
    INSERT INTO Perfis (Id, Nome, Descricao, Ativo) VALUES 
        (1, 'Admin', 'Administrador do Sistema com Acesso Total', 1),
        (2, 'Recursos Humanos', 'Recursos Humanos (RH)', 1),
        (3, 'Contabilidade', 'Contabilidade', 1),
        (4, 'Help Desk', 'Suporte de TI / Help Desk', 1),
        (5, 'Colaborador', 'Usuário Padrão', 1);
    SET IDENTITY_INSERT Perfis OFF;
END;
GO

-- Carga de Permissões
IF NOT EXISTS (SELECT 1 FROM Permissoes WHERE Id = 1)
BEGIN
    SET IDENTITY_INSERT Permissoes ON;
    INSERT INTO Permissoes (Id, Nome, Descricao) VALUES
        (1, 'usuarios.visualizar', 'Visualizar listagem de colaboradores'),
        (2, 'usuarios.gerenciar', 'Criar, editar e inativar colaboradores'),
        (3, 'perfis.gerenciar', 'Gerenciar perfis e matriz de acesso'),
        (4, 'chamados.atender', 'Triar e atender chamados de suporte'),
        (5, 'logs.visualizar', 'Consultar e exportar logs de auditoria'),
        (6, 'politicas.gerenciar', 'Configurar parâmetros de segurança e IAM');
    SET IDENTITY_INSERT Permissoes OFF;
END;
GO

-- Mapeamento Perfil-Permissão
IF NOT EXISTS (SELECT 1 FROM PerfilPermissoes WHERE PerfilId = 1 AND PermissaoId = 1)
BEGIN
    INSERT INTO PerfilPermissoes (PerfilId, PermissaoId) VALUES
        (1, 1), (1, 2), (1, 3), (1, 4), (1, 5), (1, 6),
        (2, 1), (2, 2),
        (4, 4),
        (5, 1);
END;
GO

-- Carga de Políticas de Acesso
IF NOT EXISTS (SELECT 1 FROM PoliticasAcesso WHERE Nome = 'PoliticaPadrao')
BEGIN
    INSERT INTO PoliticasAcesso (Nome, Descricao, Ativa) VALUES
        ('PoliticaPadrao', 'Política padrão de autenticação corporativa, lockout de 5 tentativas e complexidade de senhas (PIM IV)', 1);
END;
GO

-- O administrador inicial e configurado somente por ADMIN_INITIAL_PASSWORD.
-- Nenhuma credencial padrao e inserida neste script.
-- A conta administrativa e sua associacao RBAC sao provisionadas pelo seeding da aplicacao.
-- 8. Novas Tabelas Corporativas: Firewall, Endpoints DevOps, Contabilidade e RH
IF OBJECT_ID(N'[FirewallRegras]') IS NULL
BEGIN
    CREATE TABLE [FirewallRegras] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Ip] NVARCHAR(50) NOT NULL,
        [Motivo] NVARCHAR(200) NOT NULL,
        [PaisOrigem] NVARCHAR(10) NULL,
        [DataBloqueio] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [Ativo] BIT NOT NULL DEFAULT 1,
        [CriadoPor] NVARCHAR(100) NULL
    );
END;
GO

IF OBJECT_ID(N'[DispositivosRede]') IS NULL
BEGIN
    CREATE TABLE [DispositivosRede] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Hostname] NVARCHAR(100) NOT NULL,
        [Ip] NVARCHAR(50) NOT NULL,
        [Vlan] NVARCHAR(50) NULL,
        [UsuarioResponsavel] NVARCHAR(100) NULL,
        [SistemaOperacional] NVARCHAR(100) NULL,
        [Tipo] NVARCHAR(50) NOT NULL DEFAULT 'estacoes',
        [CpuUsoPercentual] INT NOT NULL DEFAULT 15,
        [RamUsoPercentual] INT NOT NULL DEFAULT 45,
        [DiscoUsoPercentual] INT NOT NULL DEFAULT 35,
        [EdrAtivo] BIT NOT NULL DEFAULT 1,
        [EdrStatus] NVARCHAR(100) NULL,
        [PatchesPendentes] INT NOT NULL DEFAULT 0,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Saudavel',
        [IsoladoRede] BIT NOT NULL DEFAULT 0,
        [UltimoScan] DATETIME2 NULL,
        [UltimoHeartbeat] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END;
GO

IF OBJECT_ID(N'[NotasFiscais]') IS NULL
BEGIN
    CREATE TABLE [NotasFiscais] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Numero] INT NOT NULL,
        [TomadorNome] NVARCHAR(150) NOT NULL,
        [TomadorCnpj] NVARCHAR(20) NOT NULL,
        [Valor] DECIMAL(18,2) NOT NULL,
        [Descricao] NVARCHAR(500) NULL,
        [DataEmissao] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ProtocoloSefaz] NVARCHAR(50) NULL,
        [Status] NVARCHAR(20) NOT NULL DEFAULT 'Autorizada',
        [EmitidoPorUsuarioId] INT NULL
    );
END;
GO

IF OBJECT_ID(N'[FechamentosMensais]') IS NULL
BEGIN
    CREATE TABLE [FechamentosMensais] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [MesAno] NVARCHAR(20) NOT NULL,
        [ReceitaBruta] DECIMAL(18,2) NOT NULL,
        [DespesasOperacionais] DECIMAL(18,2) NOT NULL,
        [LucroLiquido] DECIMAL(18,2) NOT NULL,
        [DataFechamento] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [Status] NVARCHAR(20) NOT NULL DEFAULT 'Fechado',
        [FechadoPorUsuarioId] INT NULL
    );
END;
GO

IF OBJECT_ID(N'[RegistrosFerias]') IS NULL
BEGIN
    CREATE TABLE [RegistrosFerias] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [UsuarioId] INT NULL,
        [ColaboradorNome] NVARCHAR(100) NOT NULL,
        [DataInicio] DATETIME2 NOT NULL,
        [DataFim] DATETIME2 NOT NULL,
        [Dias] INT NOT NULL DEFAULT 30,
        [Status] NVARCHAR(30) NOT NULL DEFAULT 'Aprovada',
        [DataAprovacao] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END;
GO

IF OBJECT_ID(N'[FolhasPagamento]') IS NULL
BEGIN
    CREATE TABLE [FolhasPagamento] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [MesReferencia] NVARCHAR(20) NOT NULL,
        [TotalColaboradores] INT NOT NULL DEFAULT 18,
        [ValorBrutoTotal] DECIMAL(18,2) NOT NULL,
        [ValorLiquidoTotal] DECIMAL(18,2) NOT NULL,
        [InssTotal] DECIMAL(18,2) NOT NULL,
        [FgtsTotal] DECIMAL(18,2) NOT NULL,
        [DataProcessamento] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [Status] NVARCHAR(30) NOT NULL DEFAULT 'Processada'
    );
END;
GO

-- =============================================================================
-- 9. ÍNDICES NÃO-CLUSTERIZADOS DE PERFORMANCE, VIEWS E MANUTENÇÃO AUTOMÁTICA
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Usuarios_Email' AND object_id = OBJECT_ID('Usuarios'))
    CREATE NONCLUSTERED INDEX [IX_Usuarios_Email] ON [Usuarios]([Email]) INCLUDE ([Nome], [Ativo], [MfaHabilitado]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LogsAuditoria_DataHora_Tipo' AND object_id = OBJECT_ID('LogsAuditoria'))
    CREATE NONCLUSTERED INDEX [IX_LogsAuditoria_DataHora_Tipo] ON [LogsAuditoria]([DataHora] DESC, [Tipo]) INCLUDE ([Acao], [Origem], [UsuarioId]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChamadosSuporte_Status_Prioridade' AND object_id = OBJECT_ID('ChamadosSuporte'))
    CREATE NONCLUSTERED INDEX [IX_ChamadosSuporte_Status_Prioridade] ON [ChamadosSuporte]([Status], [Prioridade]) INCLUDE ([UsuarioId], [DataAbertura], [PrazoSla]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FirewallRegras_Ip_Ativo' AND object_id = OBJECT_ID('FirewallRegras'))
    CREATE NONCLUSTERED INDEX [IX_FirewallRegras_Ip_Ativo] ON [FirewallRegras]([Ip], [Ativo]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DispositivosRede_Status_Tipo' AND object_id = OBJECT_ID('DispositivosRede'))
    CREATE NONCLUSTERED INDEX [IX_DispositivosRede_Status_Tipo] ON [DispositivosRede]([Status], [Tipo]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ConfiguracoesSistema_Chave' AND object_id = OBJECT_ID('ConfiguracoesSistema'))
    CREATE NONCLUSTERED INDEX [IX_ConfiguracoesSistema_Chave] ON [ConfiguracoesSistema]([Chave]);
GO

CREATE OR ALTER PROCEDURE sp_ManutencaoSegurancaIAM
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Usuarios
    SET ResetSenhaToken = '', ResetSenhaExpiracao = NULL
    WHERE ResetSenhaExpiracao IS NOT NULL AND ResetSenhaExpiracao < GETUTCDATE();

    UPDATE Usuarios
    SET RefreshToken = '', RefreshTokenExpiracao = NULL
    WHERE RefreshTokenExpiracao IS NOT NULL AND RefreshTokenExpiracao < GETUTCDATE();

    UPDATE Usuarios
    SET TentativasLogin = 0, DataDesbloqueio = NULL
    WHERE DataDesbloqueio IS NOT NULL AND DataDesbloqueio < GETUTCDATE();
END;
GO

CREATE OR ALTER VIEW vw_ResumoSegurancaDepartamental AS
SELECT 
    ISNULL(NULLIF(u.Departamento, ''), 'Não Informado') AS Departamento,
    COUNT(u.Id) AS TotalColaboradores,
    SUM(CASE WHEN u.Ativo = 1 THEN 1 ELSE 0 END) AS ColaboradoresAtivos,
    SUM(CASE WHEN u.MfaHabilitado = 1 THEN 1 ELSE 0 END) AS ComMfaAtivo
FROM Usuarios u
GROUP BY ISNULL(NULLIF(u.Departamento, ''), 'Não Informado');
GO

PRINT 'Banco de Dados CorvuxIAM, Índices, Views e Stored Procedures configurados com sucesso!';
GO
