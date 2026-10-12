IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [ConfiguracoesSistema] (
    [Id] int NOT NULL IDENTITY,
    [Chave] nvarchar(100) NOT NULL,
    [Valor] nvarchar(500) NOT NULL,
    CONSTRAINT [PK_ConfiguracoesSistema] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Perfis] (
    [Id] int NOT NULL IDENTITY,
    [Nome] nvarchar(50) NOT NULL,
    [Descricao] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_Perfis] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Permissoes] (
    [Id] int NOT NULL IDENTITY,
    [Nome] nvarchar(50) NOT NULL,
    [Descricao] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_Permissoes] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [PoliticasAcesso] (
    [Id] int NOT NULL IDENTITY,
    [Nome] nvarchar(100) NOT NULL,
    [Descricao] nvarchar(500) NOT NULL,
    [Ativa] bit NOT NULL,
    CONSTRAINT [PK_PoliticasAcesso] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Usuarios] (
    [Id] int NOT NULL IDENTITY,
    [Nome] nvarchar(100) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [SenhaHash] nvarchar(max) NOT NULL,
    [Ativo] bit NOT NULL,
    [CPF] nvarchar(14) NOT NULL,
    [DataNascimento] datetime2 NULL,
    [Cargo] nvarchar(100) NOT NULL,
    [DataContratacao] datetime2 NULL,
    [RG] nvarchar(20) NOT NULL,
    [Telefone] nvarchar(20) NOT NULL,
    [Departamento] nvarchar(100) NOT NULL,
    [Endereco] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_Usuarios] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [PerfilPermissoes] (
    [PerfilId] int NOT NULL,
    [PermissaoId] int NOT NULL,
    CONSTRAINT [PK_PerfilPermissoes] PRIMARY KEY ([PerfilId], [PermissaoId]),
    CONSTRAINT [FK_PerfilPermissoes_Perfis_PerfilId] FOREIGN KEY ([PerfilId]) REFERENCES [Perfis] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PerfilPermissoes_Permissoes_PermissaoId] FOREIGN KEY ([PermissaoId]) REFERENCES [Permissoes] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ChamadosSuporte] (
    [Id] int NOT NULL IDENTITY,
    [UsuarioId] int NOT NULL,
    [Assunto] nvarchar(100) NOT NULL,
    [Descricao] nvarchar(max) NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [DataAbertura] datetime2 NOT NULL,
    [DataFechamento] datetime2 NULL,
    CONSTRAINT [PK_ChamadosSuporte] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ChamadosSuporte_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [LogsAuditoria] (
    [Id] int NOT NULL IDENTITY,
    [UsuarioId] int NULL,
    [DataHora] datetime2 NOT NULL,
    [Acao] nvarchar(100) NOT NULL,
    [Detalhe] nvarchar(max) NOT NULL,
    [Tipo] nvarchar(50) NOT NULL,
    [Origem] nvarchar(50) NOT NULL,
    CONSTRAINT [PK_LogsAuditoria] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LogsAuditoria_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id])
);
GO

CREATE TABLE [UsuarioPerfis] (
    [UsuarioId] int NOT NULL,
    [PerfilId] int NOT NULL,
    CONSTRAINT [PK_UsuarioPerfis] PRIMARY KEY ([UsuarioId], [PerfilId]),
    CONSTRAINT [FK_UsuarioPerfis_Perfis_PerfilId] FOREIGN KEY ([PerfilId]) REFERENCES [Perfis] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UsuarioPerfis_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE CASCADE
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260907173137_InitialCreate', N'8.0.0');
GO

COMMIT;
GO


GO

-- Procedure exigida pelo PIM
CREATE PROCEDURE sp_RegistrarLog
    @UsuarioId INT,
    @Acao NVARCHAR(100),
    @Detalhe NVARCHAR(MAX),
    @Tipo NVARCHAR(50),
    @Origem NVARCHAR(50)
AS
BEGIN
    INSERT INTO LogsAuditoria (UsuarioId, DataHora, Acao, Detalhe, Tipo, Origem)
    VALUES (@UsuarioId, GETUTCDATE(), @Acao, @Detalhe, @Tipo, @Origem);
END
GO

-- Trigger exigida pelo PIM
CREATE TRIGGER trg_AuditoriaUsuarios
ON Usuarios
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    -- Log basico de alteração na tabela de usuarios
    INSERT INTO LogsAuditoria (DataHora, Acao, Detalhe, Tipo, Origem)
    VALUES (GETUTCDATE(), 'Alteração em Usuários', 'Modificação detectada pelo banco', 'Trigger', 'DB Trigger');
END
GO

-- Carga inicial para testes e validação (Seeds)
IF NOT EXISTS (SELECT 1 FROM Perfis WHERE Nome = 'Administrador')
BEGIN
    INSERT INTO Perfis (Nome, Descricao) VALUES 
        ('Administrador', 'Acesso completo ao sistema e módulos administrativos'),
        ('Colaborador', 'Acesso às funcionalidades operacionais e abertura de chamados');
END
GO

IF NOT EXISTS (SELECT 1 FROM Permissoes WHERE Nome = 'GerenciarUsuarios')
BEGIN
    INSERT INTO Permissoes (Nome, Descricao) VALUES
        ('GerenciarUsuarios', 'Permissão para criar e editar usuários'),
        ('VisualizarAuditoria', 'Permissão para consultar logs de auditoria'),
        ('AbrirChamados', 'Permissão para registrar solicitações de suporte');
END
GO

IF NOT EXISTS (SELECT 1 FROM PoliticasAcesso WHERE Nome = 'PoliticaPadrao')
BEGIN
    INSERT INTO PoliticasAcesso (Nome, Descricao, Ativa) VALUES
        ('PoliticaPadrao', 'Política padrão de autenticação e complexidade de senhas', 1);
END
GO
