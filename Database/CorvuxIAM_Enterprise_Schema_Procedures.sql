/* ==========================================================================================
   CORVUX SECURITY IAM - ARQUITETURA CORPORATIVA DE BANCO DE DADOS (MS SQL SERVER 2022)
   Inclui: Tabelas, Índices Non-Clustered Covering, Views Analíticas, Funções Escalares,
           Triggers de Auditoria Automática (SIEM) e Stored Procedures Transacionais.
   ========================================================================================== */

SET NOCOUNT ON;
GO

-- ==========================================================================================
-- 1. VIEWS ANALÍTICAS (GOVERNANÇA RBAC, SAÚDE DE ENDPOINTS & MONITORAMENTO DE SLA)
-- ==========================================================================================

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
    ISNULL(p.[Nome], 'Colaborador') AS [PerfilNome],
    ISNULL(STRING_AGG(perm.[Nome], ', '), 'usuarios.visualizar') AS [PermissoesAtribuidas]
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
    ISNULL([Vlan], 'VLAN Padrão') AS [Vlan],
    [Tipo],
    COUNT([Id]) AS [TotalDispositivos],
    SUM(CASE WHEN [Status] = 'Saudavel' THEN 1 ELSE 0 END) AS [DispositivosSaudaveis],
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
        WHEN c.[Status] IN ('Fechado', 'Resolvido', 'Concluído') THEN 'SLA Cumprido'
        WHEN c.[PrazoSla] IS NOT NULL AND c.[PrazoSla] < GETUTCDATE() THEN 'SLA Violado'
        WHEN c.[PrazoSla] IS NOT NULL AND DATEDIFF(MINUTE, GETUTCDATE(), c.[PrazoSla]) <= 120 THEN 'Risco de Violação (<2h)'
        ELSE 'Dentro do Prazo'
    END AS [SituacaoSla]
FROM [dbo].[ChamadosSuporte] c
INNER JOIN [dbo].[Usuarios] u ON c.[UsuarioId] = u.[Id];
GO

-- ==========================================================================================
-- 2. FUNÇÃO ESCALAR DE CÁLCULO DE RISCO DE IDENTIDADE (ZERO-TRUST UBA SCORE)
-- ==========================================================================================

CREATE OR ALTER FUNCTION [dbo].[fn_CalcularScoreRiscoUsuario] (@UsuarioId INT)
RETURNS INT
AS
BEGIN
    DECLARE @Score INT = 10; -- Risco base mínimo
    DECLARE @Mfa BIT = 0;
    DECLARE @Ativo BIT = 1;
    DECLARE @FalhasRecentes INT = 0;

    SELECT @Mfa = [MfaHabilitado], @Ativo = [Ativo]
    FROM [dbo].[Usuarios]
    WHERE [Id] = @UsuarioId;

    IF @Mfa = 0 SET @Score = @Score + 35;
    IF @Ativo = 0 SET @Score = @Score + 25;

    SELECT @FalhasRecentes = COUNT(1)
    FROM [dbo].[LogsAuditoria]
    WHERE [UsuarioId] = @UsuarioId
      AND [Tipo] IN ('Warning', 'Error', 'Danger')
      AND [DataHora] >= DATEADD(DAY, -7, GETUTCDATE());

    SET @Score = @Score + (@FalhasRecentes * 10);
    IF @Score > 100 SET @Score = 100;

    RETURN @Score;
END;
GO

-- ==========================================================================================
-- 3. TRIGGERS DE AUDITORIA AUTOMÁTICA (SIEM / TRILHA IMUTÁVEL)
-- ==========================================================================================

CREATE OR ALTER TRIGGER [dbo].[trg_AuditoriaStatusUsuario]
ON [dbo].[Usuarios]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF UPDATE([Ativo]) OR UPDATE([MfaHabilitado])
    BEGIN
        INSERT INTO [dbo].[LogsAuditoria] ([DataHora], [Acao], [Detalhe], [Tipo], [Origem], [UsuarioId])
        SELECT 
            GETUTCDATE(),
            'Trigger DB: Alteração de Segurança de Conta',
            CONCAT('Conta ', i.[Email], ' | Ativo: ', d.[Ativo], '->', i.[Ativo], ' | MFA: ', d.[MfaHabilitado], '->', i.[MfaHabilitado]),
            CASE WHEN i.[Ativo] = 0 THEN 'Warning' ELSE 'Info' END,
            'SQL Trigger trg_AuditoriaStatusUsuario',
            i.[Id]
        FROM inserted i
        INNER JOIN deleted d ON i.[Id] = d.[Id]
        WHERE i.[Ativo] <> d.[Ativo] OR i.[MfaHabilitado] <> d.[MfaHabilitado];
    END
END;
GO

CREATE OR ALTER TRIGGER [dbo].[trg_AuditoriaFirewallWaf]
ON [dbo].[FirewallRegras]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[LogsAuditoria] ([DataHora], [Acao], [Detalhe], [Tipo], [Origem], [UsuarioId])
    SELECT 
        GETUTCDATE(),
        CASE WHEN i.[Ativo] = 1 THEN 'Trigger WAF: Regra de Bloqueio Ativada' ELSE 'Trigger WAF: Regra de Bloqueio Revogada' END,
        CONCAT('IP: ', i.[Ip], ' | Motivo: ', i.[Motivo], ' | Origem: ', ISNULL(i.[PaisOrigem], 'N/A')),
        CASE WHEN i.[Ativo] = 1 THEN 'Danger' ELSE 'Info' END,
        'SQL Trigger trg_AuditoriaFirewallWaf',
        1
    FROM inserted i;
END;
GO

-- ==========================================================================================
-- 4. STORED PROCEDURES CORPORATIVAS (SECOPS, IAM, ITSM, CONTABILIDADE & LGPD)
-- ==========================================================================================

-- Procedure 1: Registro padronizado de Log de Auditoria SIEM
CREATE OR ALTER PROCEDURE [dbo].[sp_RegistrarLogAuditoria]
    @Acao NVARCHAR(150),
    @Detalhe NVARCHAR(1000),
    @Tipo NVARCHAR(30) = 'Info',
    @Origem NVARCHAR(100) = 'System Core',
    @UsuarioId INT = 1
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[LogsAuditoria] ([DataHora], [Acao], [Detalhe], [Tipo], [Origem], [UsuarioId])
    VALUES (GETUTCDATE(), @Acao, @Detalhe, @Tipo, @Origem, @UsuarioId);
END;
GO

-- Procedure 2: Bloqueio Atômico de IP no Firewall WAF com Auditoria Transacional
CREATE OR ALTER PROCEDURE [dbo].[sp_BloquearIpFirewall]
    @Ip NVARCHAR(50),
    @Motivo NVARCHAR(200),
    @PaisOrigem NVARCHAR(10) = 'BR',
    @CriadoPor NVARCHAR(100) = 'SecOps Admin',
    @AdminUsuarioId INT = 1
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
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- Procedure 3: Abertura de Chamado ITSM com Cálculo Automático de SLA por Prioridade
CREATE OR ALTER PROCEDURE [dbo].[sp_AbrirChamadoSuporteComSla]
    @UsuarioId INT,
    @Assunto NVARCHAR(200),
    @Descricao NVARCHAR(MAX),
    @Categoria NVARCHAR(50) = 'Acesso',
    @Prioridade NVARCHAR(30) = 'Normal',
    @NovoChamadoId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @HorasSla INT = CASE @Prioridade
        WHEN 'Critica' THEN 2
        WHEN 'Alta' THEN 4
        WHEN 'Normal' THEN 8
        ELSE 24
    END;

    DECLARE @Agora DATETIME2 = GETUTCDATE();
    DECLARE @Prazo DATETIME2 = DATEADD(HOUR, @HorasSla, @Agora);

    INSERT INTO [dbo].[ChamadosSuporte]
        ([UsuarioId], [Assunto], [Descricao], [Categoria], [Prioridade], [Status], [DataAbertura], [DataAtualizacao], [PrazoSla], [Solucao])
    VALUES
        (@UsuarioId, @Assunto, @Descricao, @Categoria, @Prioridade, 'Aberto', @Agora, @Agora, @Prazo, N'');

    SET @NovoChamadoId = SCOPE_IDENTITY();

    EXEC [dbo].[sp_RegistrarLogAuditoria]
        @Acao = N'Abertura de Chamado via Procedure',
        @Detalhe = @Assunto,
        @Tipo = N'Info',
        @Origem = N'sp_AbrirChamadoSuporteComSla',
        @UsuarioId = @UsuarioId;
END;
GO

-- Procedure 4: Revogação Imediata de Sessões e Tokens Zero-Trust
CREATE OR ALTER PROCEDURE [dbo].[sp_RevogarSessoesUsuario]
    @UsuarioId INT,
    @AdminExecutorId INT = 1
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Usuarios]
    SET [RefreshToken] = N'',
        [RefreshTokenExpiryTime] = NULL,
        [ResetSenhaToken] = N'',
        [ResetSenhaTokenExpira] = NULL
    WHERE [Id] = @UsuarioId;

    EXEC [dbo].[sp_RegistrarLogAuditoria]
        @Acao = N'Revogação Zero-Trust de Sessão',
        @Detalhe = N'Tokens JWT/Refresh e sessões ativas invalidados via sp_RevogarSessoesUsuario',
        @Tipo = N'Warning',
        @Origem = N'Zero-Trust Procedure',
        @UsuarioId = @AdminExecutorId;
END;
GO

-- Procedure 5: Processamento Transacional de Fechamento Contábil Mensal
CREATE OR ALTER PROCEDURE [dbo].[sp_ProcessarFechamentoContabil]
    @MesAno NVARCHAR(20),
    @ReceitaBruta DECIMAL(18,2),
    @DespesasOperacionais DECIMAL(18,2),
    @UsuarioId INT = 1
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @LucroLiquido DECIMAL(18,2) = @ReceitaBruta - @DespesasOperacionais;

        INSERT INTO [dbo].[FechamentosMensais]
            ([MesAno], [ReceitaBruta], [DespesasOperacionais], [LucroLiquido], [DataFechamento], [Status], [FechadoPorUsuarioId])
        VALUES
            (@MesAno, @ReceitaBruta, @DespesasOperacionais, @LucroLiquido, GETUTCDATE(), N'Fechado', @UsuarioId);

        EXEC [dbo].[sp_RegistrarLogAuditoria]
            @Acao = N'Fechamento Contábil via Procedure',
            @Detalhe = @MesAno,
            @Tipo = N'Info',
            @Origem = N'sp_ProcessarFechamentoContabil',
            @UsuarioId = @UsuarioId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- Procedure 6: Manutenção de Retenção de Dados e Conformidade LGPD (Lei 13.709/2018)
CREATE OR ALTER PROCEDURE [dbo].[sp_ExecutarManutencaoLgpd]
    @AdminUsuarioId INT = 1,
    @RegistrosProcessados INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @DiasRetencao INT = 180;

    SELECT TOP 1 @DiasRetencao = TRY_CAST([Valor] AS INT)
    FROM [dbo].[ConfiguracoesSistema]
    WHERE [Chave] = N'LGPD_DATA_RETENTION_DAYS';

    IF @DiasRetencao IS NULL OR @DiasRetencao < 30 SET @DiasRetencao = 180;

    DECLARE @DataCorte DATETIME2 = DATEADD(DAY, -@DiasRetencao, GETUTCDATE());

    DELETE FROM [dbo].[LogsAuditoria]
    WHERE [DataHora] < @DataCorte
      AND [Tipo] = N'Info';

    SET @RegistrosProcessados = @@ROWCOUNT;

    -- Limpa tokens de recuperação expirados
    UPDATE [dbo].[Usuarios]
    SET [ResetSenhaToken] = N'',
        [ResetSenhaTokenExpira] = NULL
    WHERE [ResetSenhaTokenExpira] IS NOT NULL AND [ResetSenhaTokenExpira] < GETUTCDATE();

    SET @RegistrosProcessados = @RegistrosProcessados + @@ROWCOUNT;

    DECLARE @Msg NVARCHAR(300) = CONCAT(N'Rotina LGPD concluída (Retenção ', @DiasRetencao, N' dias). Registros higienizados: ', @RegistrosProcessados);
    EXEC [dbo].[sp_RegistrarLogAuditoria]
        @Acao = N'Manutenção & Expurgo LGPD (DB Procedure)',
        @Detalhe = @Msg,
        @Tipo = N'Info',
        @Origem = N'sp_ExecutarManutencaoLgpd',
        @UsuarioId = @AdminUsuarioId;
END;
GO

-- Procedure 7: Consolidação de Telemetria e KPIs do Dashboard SecOps
CREATE OR ALTER PROCEDURE [dbo].[sp_ObterMetricasSecOpsDashboard]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        (SELECT COUNT(1) FROM [dbo].[Usuarios] WHERE [Ativo] = 1) AS [UsuariosAtivos],
        (SELECT COUNT(1) FROM [dbo].[Usuarios] WHERE [Ativo] = 1 AND [MfaHabilitado] = 1) AS [UsuariosComMfa],
        (SELECT COUNT(1) FROM [dbo].[ChamadosSuporte] WHERE [Status] NOT IN ('Fechado', 'Resolvido', 'Concluído')) AS [ChamadosAbertos],
        (SELECT COUNT(1) FROM [dbo].[FirewallRegras] WHERE [Ativo] = 1) AS [IpsBloqueadosWaf],
        (SELECT COUNT(1) FROM [dbo].[DispositivosRede] WHERE [Status] = 'Saudavel' AND [IsoladoRede] = 0) AS [EndpointsSaudaveis],
        (SELECT COUNT(1) FROM [dbo].[DispositivosRede] WHERE [IsoladoRede] = 1 OR [Status] = 'Critico') AS [EndpointsCriticosOuIsolados],
        (SELECT COUNT(1) FROM [dbo].[LogsAuditoria] WHERE [DataHora] >= DATEADD(HOUR, -24, GETUTCDATE()) AND [Tipo] IN ('Warning', 'Error', 'Danger')) AS [IncidentesUltimas24h];
END;
GO

