using System;
using System.Linq;
using CorvusIAM.Entities;
using Microsoft.EntityFrameworkCore;

namespace CorvusIAM.Data
{
    public static class DbDataSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            if (context.Database.IsSqlServer())
            {
                // Garante que as novas tabelas existem no SQL Server sem depender de migration pendente
                context.Database.ExecuteSqlRaw(@"
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

                    -- Índices de Alta Performance para Consultas IAM, SIEM e ITSM
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Usuarios_Email' AND object_id = OBJECT_ID('Usuarios'))
                        CREATE NONCLUSTERED INDEX [IX_Usuarios_Email] ON [Usuarios]([Email]) INCLUDE ([Nome], [Ativo], [MfaHabilitado]);

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LogsAuditoria_DataHora_Tipo' AND object_id = OBJECT_ID('LogsAuditoria'))
                        CREATE NONCLUSTERED INDEX [IX_LogsAuditoria_DataHora_Tipo] ON [LogsAuditoria]([DataHora] DESC, [Tipo]) INCLUDE ([Acao], [Origem], [UsuarioId]);

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ChamadosSuporte_Status_Prioridade' AND object_id = OBJECT_ID('ChamadosSuporte'))
                        CREATE NONCLUSTERED INDEX [IX_ChamadosSuporte_Status_Prioridade] ON [ChamadosSuporte]([Status], [Prioridade]) INCLUDE ([UsuarioId], [DataAbertura], [PrazoSla]);

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FirewallRegras_Ip_Ativo' AND object_id = OBJECT_ID('FirewallRegras'))
                        CREATE NONCLUSTERED INDEX [IX_FirewallRegras_Ip_Ativo] ON [FirewallRegras]([Ip], [Ativo]);

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DispositivosRede_Status_Tipo' AND object_id = OBJECT_ID('DispositivosRede'))
                        CREATE NONCLUSTERED INDEX [IX_DispositivosRede_Status_Tipo] ON [DispositivosRede]([Status], [Tipo]);

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ConfiguracoesSistema_Chave' AND object_id = OBJECT_ID('ConfiguracoesSistema'))
                        CREATE NONCLUSTERED INDEX [IX_ConfiguracoesSistema_Chave] ON [ConfiguracoesSistema]([Chave]);

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_NotasFiscais_DataEmissao' AND object_id = OBJECT_ID('NotasFiscais'))
                        CREATE NONCLUSTERED INDEX [IX_NotasFiscais_DataEmissao] ON [NotasFiscais]([DataEmissao] DESC) INCLUDE ([Numero], [Valor], [Status]);

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UsuarioPerfis_PerfilId' AND object_id = OBJECT_ID('UsuarioPerfis'))
                        CREATE NONCLUSTERED INDEX [IX_UsuarioPerfis_PerfilId] ON [UsuarioPerfis]([PerfilId], [UsuarioId]);
                ");

                // Provisionamento de Views, Funções, Triggers e Stored Procedures no SQL Server (cada objeto em seu próprio batch)
                string[] sqlServerProgrammabilityObjects = new[]
                {
                    @"CREATE OR ALTER VIEW [dbo].[vw_MatrizRbacUsuarios] AS
                      SELECT u.[Id] AS [UsuarioId], u.[Nome], u.[Email], u.[Departamento], u.[Cargo], u.[Ativo], u.[MfaHabilitado],
                             ISNULL(p.[Nome], 'Colaborador') AS [PerfilNome]
                      FROM [dbo].[Usuarios] u
                      LEFT JOIN [dbo].[UsuarioPerfis] up ON u.[Id] = up.[UsuarioId]
                      LEFT JOIN [dbo].[Perfis] p ON up.[PerfilId] = p.[Id];",

                    @"CREATE OR ALTER VIEW [dbo].[vw_ResumoSaudeEndpoints] AS
                      SELECT ISNULL([Vlan], 'VLAN Padrão') AS [Vlan], [Tipo], COUNT([Id]) AS [TotalDispositivos],
                             SUM(CASE WHEN [Status] = 'Saudavel' THEN 1 ELSE 0 END) AS [DispositivosSaudaveis],
                             SUM(CASE WHEN [IsoladoRede] = 1 THEN 1 ELSE 0 END) AS [DispositivosIsolados],
                             SUM(CASE WHEN [EdrAtivo] = 1 THEN 1 ELSE 0 END) AS [ComEdrAtivo],
                             AVG(CAST([CpuUsoPercentual] AS FLOAT)) AS [MediaCpuPercentual],
                             AVG(CAST([RamUsoPercentual] AS FLOAT)) AS [MediaRamPercentual],
                             AVG(CAST([DiscoUsoPercentual] AS FLOAT)) AS [MediaDiscoPercentual],
                             SUM([PatchesPendentes]) AS [TotalPatchesPendentes]
                      FROM [dbo].[DispositivosRede]
                      GROUP BY [Vlan], [Tipo];",

                    @"CREATE OR ALTER VIEW [dbo].[vw_MonitoramentoSlaChamados] AS
                      SELECT c.[Id] AS [ChamadoId], c.[Assunto], c.[Categoria], c.[Prioridade], c.[Status],
                             u.[Nome] AS [SolicitanteNome], u.[Email] AS [SolicitanteEmail], c.[DataAbertura], c.[PrazoSla],
                             CASE 
                                 WHEN c.[Status] IN ('Fechado', 'Resolvido', 'Concluído') THEN 'SLA Cumprido'
                                 WHEN c.[PrazoSla] IS NOT NULL AND c.[PrazoSla] < GETUTCDATE() THEN 'SLA Violado'
                                 WHEN c.[PrazoSla] IS NOT NULL AND DATEDIFF(MINUTE, GETUTCDATE(), c.[PrazoSla]) <= 120 THEN 'Risco de Violação (<2h)'
                                 ELSE 'Dentro do Prazo'
                             END AS [SituacaoSla]
                      FROM [dbo].[ChamadosSuporte] c
                      INNER JOIN [dbo].[Usuarios] u ON c.[UsuarioId] = u.[Id];",

                    @"CREATE OR ALTER FUNCTION [dbo].[fn_CalcularScoreRiscoUsuario] (@UsuarioId INT)
                      RETURNS INT
                      AS
                      BEGIN
                          DECLARE @Score INT = 10;
                          DECLARE @Mfa BIT = 0;
                          DECLARE @Ativo BIT = 1;
                          DECLARE @FalhasRecentes INT = 0;
                          SELECT @Mfa = [MfaHabilitado], @Ativo = [Ativo] FROM [dbo].[Usuarios] WHERE [Id] = @UsuarioId;
                          IF @Mfa = 0 SET @Score = @Score + 35;
                          IF @Ativo = 0 SET @Score = @Score + 25;
                          SELECT @FalhasRecentes = COUNT(1) FROM [dbo].[LogsAuditoria]
                          WHERE [UsuarioId] = @UsuarioId AND [Tipo] IN ('Warning', 'Error', 'Danger') AND [DataHora] >= DATEADD(DAY, -7, GETUTCDATE());
                          SET @Score = @Score + (@FalhasRecentes * 10);
                          IF @Score > 100 SET @Score = 100;
                          RETURN @Score;
                      END;",

                    @"CREATE OR ALTER TRIGGER [dbo].[trg_AuditoriaStatusUsuario]
                      ON [dbo].[Usuarios]
                      AFTER UPDATE
                      AS
                      BEGIN
                          SET NOCOUNT ON;
                          IF UPDATE([Ativo]) OR UPDATE([MfaHabilitado])
                          BEGIN
                              INSERT INTO [dbo].[LogsAuditoria] ([DataHora], [Acao], [Detalhe], [Tipo], [Origem], [UsuarioId])
                              SELECT GETUTCDATE(), 'Trigger DB: Alteração de Segurança de Conta',
                                     CONCAT('Conta ', i.[Email], ' | Ativo: ', d.[Ativo], '->', i.[Ativo], ' | MFA: ', d.[MfaHabilitado], '->', i.[MfaHabilitado]),
                                     CASE WHEN i.[Ativo] = 0 THEN 'Warning' ELSE 'Info' END,
                                     'SQL Trigger trg_AuditoriaStatusUsuario', i.[Id]
                              FROM inserted i
                              INNER JOIN deleted d ON i.[Id] = d.[Id]
                              WHERE i.[Ativo] <> d.[Ativo] OR i.[MfaHabilitado] <> d.[MfaHabilitado];
                          END
                      END;",

                    @"CREATE OR ALTER TRIGGER [dbo].[trg_AuditoriaFirewallWaf]
                      ON [dbo].[FirewallRegras]
                      AFTER INSERT, UPDATE
                      AS
                      BEGIN
                          SET NOCOUNT ON;
                          INSERT INTO [dbo].[LogsAuditoria] ([DataHora], [Acao], [Detalhe], [Tipo], [Origem], [UsuarioId])
                          SELECT GETUTCDATE(),
                                 CASE WHEN i.[Ativo] = 1 THEN 'Trigger WAF: Regra de Bloqueio Ativada' ELSE 'Trigger WAF: Regra de Bloqueio Revogada' END,
                                 CONCAT('IP: ', i.[Ip], ' | Motivo: ', i.[Motivo]),
                                 CASE WHEN i.[Ativo] = 1 THEN 'Danger' ELSE 'Info' END,
                                 'SQL Trigger trg_AuditoriaFirewallWaf', 1
                          FROM inserted i;
                      END;",

                    @"CREATE OR ALTER PROCEDURE [dbo].[sp_RegistrarLogAuditoria]
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
                      END;",

                    @"CREATE OR ALTER PROCEDURE [dbo].[sp_BloquearIpFirewall]
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
                                  SET [Ativo] = 1, [Motivo] = @Motivo, [DataBloqueio] = GETUTCDATE(), [CriadoPor] = @CriadoPor
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
                      END;",

                    @"CREATE OR ALTER PROCEDURE [dbo].[sp_AbrirChamadoSuporteComSla]
                          @UsuarioId INT,
                          @Assunto NVARCHAR(200),
                          @Descricao NVARCHAR(MAX),
                          @Categoria NVARCHAR(50) = 'Acesso',
                          @Prioridade NVARCHAR(30) = 'Normal',
                          @NovoChamadoId INT OUTPUT
                      AS
                      BEGIN
                          SET NOCOUNT ON;
                          DECLARE @HorasSla INT = CASE @Prioridade WHEN 'Critica' THEN 2 WHEN 'Alta' THEN 4 WHEN 'Normal' THEN 8 ELSE 24 END;
                          DECLARE @Agora DATETIME2 = GETUTCDATE();
                          DECLARE @Prazo DATETIME2 = DATEADD(HOUR, @HorasSla, @Agora);
                          INSERT INTO [dbo].[ChamadosSuporte]
                              ([UsuarioId], [Assunto], [Descricao], [Categoria], [Prioridade], [Status], [DataAbertura], [DataAtualizacao], [PrazoSla], [Solucao])
                          VALUES
                              (@UsuarioId, @Assunto, @Descricao, @Categoria, @Prioridade, 'Aberto', @Agora, @Agora, @Prazo, N'');
                          SET @NovoChamadoId = SCOPE_IDENTITY();
                      END;",

                    @"CREATE OR ALTER PROCEDURE [dbo].[sp_RevogarSessoesUsuario]
                          @UsuarioId INT,
                          @AdminExecutorId INT = 1
                      AS
                      BEGIN
                          SET NOCOUNT ON;
                          UPDATE [dbo].[Usuarios]
                          SET [RefreshToken] = N'', [RefreshTokenExpiryTime] = NULL, [ResetSenhaToken] = N'', [ResetSenhaTokenExpira] = NULL
                          WHERE [Id] = @UsuarioId;
                          INSERT INTO [dbo].[LogsAuditoria] ([DataHora], [Acao], [Detalhe], [Tipo], [Origem], [UsuarioId])
                          VALUES (GETUTCDATE(), N'Revogação Zero-Trust de Sessão', N'Tokens JWT/Refresh invalidados via sp_RevogarSessoesUsuario', N'Warning', N'sp_RevogarSessoesUsuario', @AdminExecutorId);
                      END;",

                    @"CREATE OR ALTER PROCEDURE [dbo].[sp_ProcessarFechamentoContabil]
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
                              INSERT INTO [dbo].[LogsAuditoria] ([DataHora], [Acao], [Detalhe], [Tipo], [Origem], [UsuarioId])
                              VALUES (GETUTCDATE(), N'Fechamento Contábil via Procedure', @MesAno, N'Info', N'sp_ProcessarFechamentoContabil', @UsuarioId);
                              COMMIT TRANSACTION;
                          END TRY
                          BEGIN CATCH
                              IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                              THROW;
                          END CATCH
                      END;",

                    @"CREATE OR ALTER PROCEDURE [dbo].[sp_ExecutarManutencaoLgpd]
                          @AdminUsuarioId INT = 1,
                          @RegistrosProcessados INT OUTPUT
                      AS
                      BEGIN
                          SET NOCOUNT ON;
                          DECLARE @DiasRetencao INT = 180;
                          SELECT TOP 1 @DiasRetencao = TRY_CAST([Valor] AS INT) FROM [dbo].[ConfiguracoesSistema] WHERE [Chave] = N'LGPD_DATA_RETENTION_DAYS';
                          IF @DiasRetencao IS NULL OR @DiasRetencao < 30 SET @DiasRetencao = 180;
                          DECLARE @DataCorte DATETIME2 = DATEADD(DAY, -@DiasRetencao, GETUTCDATE());
                          DELETE FROM [dbo].[LogsAuditoria] WHERE [DataHora] < @DataCorte AND [Tipo] = N'Info';
                          SET @RegistrosProcessados = @@ROWCOUNT;
                          UPDATE [dbo].[Usuarios] SET [ResetSenhaToken] = N'', [ResetSenhaTokenExpira] = NULL
                          WHERE [ResetSenhaTokenExpira] IS NOT NULL AND [ResetSenhaTokenExpira] < GETUTCDATE();
                          SET @RegistrosProcessados = @RegistrosProcessados + @@ROWCOUNT;
                          INSERT INTO [dbo].[LogsAuditoria] ([DataHora], [Acao], [Detalhe], [Tipo], [Origem], [UsuarioId])
                          VALUES (GETUTCDATE(), N'Manutenção & Expurgo LGPD (DB Procedure)', CONCAT(N'Retenção ', @DiasRetencao, N' dias | Higienizados: ', @RegistrosProcessados), N'Info', N'sp_ExecutarManutencaoLgpd', @AdminUsuarioId);
                      END;",

                    @"CREATE OR ALTER PROCEDURE [dbo].[sp_ObterMetricasSecOpsDashboard]
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
                      END;"
                };

                foreach (var sqlObj in sqlServerProgrammabilityObjects)
                {
                    try { context.Database.ExecuteSqlRaw(sqlObj); } catch { }
                }
            }
            else
            {
                // SQLite (desenvolvimento / testes) com otimizações WAL, Índices, Tabelas, Views e Triggers
                context.Database.EnsureCreated();
                try
                {
                    context.Database.ExecuteSqlRaw(@"
                        CREATE TABLE IF NOT EXISTS FirewallRegras (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Ip TEXT NOT NULL,
                            Motivo TEXT NOT NULL,
                            PaisOrigem TEXT NULL,
                            DataBloqueio TEXT NOT NULL,
                            Ativo INTEGER NOT NULL DEFAULT 1,
                            CriadoPor TEXT NULL
                        );

                        CREATE TABLE IF NOT EXISTS DispositivosRede (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Hostname TEXT NOT NULL,
                            Ip TEXT NOT NULL,
                            Vlan TEXT NULL,
                            UsuarioResponsavel TEXT NULL,
                            SistemaOperacional TEXT NULL,
                            Tipo TEXT NOT NULL DEFAULT 'estacoes',
                            CpuUsoPercentual INTEGER NOT NULL DEFAULT 15,
                            RamUsoPercentual INTEGER NOT NULL DEFAULT 45,
                            DiscoUsoPercentual INTEGER NOT NULL DEFAULT 35,
                            EdrAtivo INTEGER NOT NULL DEFAULT 1,
                            EdrStatus TEXT NULL,
                            PatchesPendentes INTEGER NOT NULL DEFAULT 0,
                            Status TEXT NOT NULL DEFAULT 'Saudavel',
                            IsoladoRede INTEGER NOT NULL DEFAULT 0,
                            UltimoScan TEXT NULL,
                            UltimoHeartbeat TEXT NOT NULL
                        );

                        CREATE TABLE IF NOT EXISTS NotasFiscais (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Numero INTEGER NOT NULL,
                            TomadorNome TEXT NOT NULL,
                            TomadorCnpj TEXT NOT NULL,
                            Valor REAL NOT NULL,
                            Descricao TEXT NULL,
                            DataEmissao TEXT NOT NULL,
                            ProtocoloSefaz TEXT NULL,
                            Status TEXT NOT NULL DEFAULT 'Autorizada',
                            EmitidoPorUsuarioId INTEGER NULL
                        );

                        CREATE TABLE IF NOT EXISTS FechamentosMensais (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            MesAno TEXT NOT NULL,
                            ReceitaBruta REAL NOT NULL,
                            DespesasOperacionais REAL NOT NULL,
                            LucroLiquido REAL NOT NULL,
                            DataFechamento TEXT NOT NULL,
                            Status TEXT NOT NULL DEFAULT 'Fechado',
                            FechadoPorUsuarioId INTEGER NULL
                        );

                        CREATE TABLE IF NOT EXISTS RegistrosFerias (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            UsuarioId INTEGER NULL,
                            ColaboradorNome TEXT NOT NULL,
                            DataInicio TEXT NOT NULL,
                            DataFim TEXT NOT NULL,
                            Dias INTEGER NOT NULL DEFAULT 30,
                            Status TEXT NOT NULL DEFAULT 'Aprovada',
                            DataAprovacao TEXT NOT NULL
                        );

                        CREATE TABLE IF NOT EXISTS FolhasPagamento (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            MesReferencia TEXT NOT NULL,
                            TotalColaboradores INTEGER NOT NULL DEFAULT 18,
                            ValorBrutoTotal REAL NOT NULL,
                            ValorLiquidoTotal REAL NOT NULL,
                            InssTotal REAL NOT NULL,
                            FgtsTotal REAL NOT NULL,
                            DataProcessamento TEXT NOT NULL,
                            Status TEXT NOT NULL DEFAULT 'Processada'
                        );

                        PRAGMA journal_mode = WAL;
                        PRAGMA synchronous = NORMAL;
                        PRAGMA temp_store = MEMORY;
                        CREATE INDEX IF NOT EXISTS IX_Usuarios_Email ON Usuarios(Email);
                        CREATE INDEX IF NOT EXISTS IX_LogsAuditoria_DataHora_Tipo ON LogsAuditoria(DataHora DESC, Tipo);
                        CREATE INDEX IF NOT EXISTS IX_ChamadosSuporte_Status_Prioridade ON ChamadosSuporte(Status, Prioridade);
                        CREATE INDEX IF NOT EXISTS IX_FirewallRegras_Ip_Ativo ON FirewallRegras(Ip, Ativo);
                        CREATE INDEX IF NOT EXISTS IX_DispositivosRede_Status_Tipo ON DispositivosRede(Status, Tipo);
                        CREATE INDEX IF NOT EXISTS IX_ConfiguracoesSistema_Chave ON ConfiguracoesSistema(Chave);
                        CREATE INDEX IF NOT EXISTS IX_NotasFiscais_DataEmissao ON NotasFiscais(DataEmissao DESC);
                        CREATE INDEX IF NOT EXISTS IX_UsuarioPerfis_PerfilId ON UsuarioPerfis(PerfilId, UsuarioId);

                        CREATE VIEW IF NOT EXISTS vw_MatrizRbacUsuarios AS
                        SELECT u.Id AS UsuarioId, u.Nome, u.Email, u.Departamento, u.Cargo, u.Ativo, u.MfaHabilitado,
                               IFNULL(p.Nome, 'Colaborador') AS PerfilNome
                        FROM Usuarios u
                        LEFT JOIN UsuarioPerfis up ON u.Id = up.UsuarioId
                        LEFT JOIN Perfis p ON up.PerfilId = p.Id;

                        CREATE VIEW IF NOT EXISTS vw_ResumoSaudeEndpoints AS
                        SELECT IFNULL(Vlan, 'VLAN Padrão') AS Vlan, Tipo, COUNT(Id) AS TotalDispositivos,
                               SUM(CASE WHEN Status = 'Saudavel' THEN 1 ELSE 0 END) AS DispositivosSaudaveis,
                               SUM(CASE WHEN IsoladoRede = 1 THEN 1 ELSE 0 END) AS DispositivosIsolados,
                               SUM(CASE WHEN EdrAtivo = 1 THEN 1 ELSE 0 END) AS ComEdrAtivo,
                               AVG(CpuUsoPercentual) AS MediaCpuPercentual,
                               AVG(RamUsoPercentual) AS MediaRamPercentual,
                               AVG(DiscoUsoPercentual) AS MediaDiscoPercentual,
                               SUM(PatchesPendentes) AS TotalPatchesPendentes
                        FROM DispositivosRede
                        GROUP BY Vlan, Tipo;
                    ");
                }
                catch { }
            }

            // 0. Usuários de Demonstração por Perfil Departamental (Fluxo Completo RBAC)
            const string defaultHash = "$2a$11$y4AkJtTDlULmPEEKkqBLk.MM4ZKyMK8ExkIkfyAY5s/iEMDL1UvIS"; // Cv1670!
            void EnsureDemoUser(string email, string nome, string cargo, string depto, string cpf, int perfilId)
            {
                var user = context.Usuarios.FirstOrDefault(u => u.Email == email);
                if (user == null)
                {
                    user = new Usuario
                    {
                        Nome = nome,
                        Email = email,
                        SenhaHash = defaultHash,
                        Ativo = true,
                        CPF = cpf,
                        Cargo = cargo,
                        Departamento = depto,
                        DataNascimento = new DateTime(1992, 5, 15),
                        DataContratacao = new DateTime(2024, 2, 1),
                        RG = "12.345.678-9",
                        Telefone = "(11) 98888-7777",
                        Endereco = "Av. Paulista, 1000 - São Paulo/SP",
                        MfaHabilitado = false,
                        MfaSecretKey = string.Empty,
                        ResetSenhaToken = string.Empty,
                        RefreshToken = string.Empty
                    };
                    context.Usuarios.Add(user);
                    context.SaveChanges();
                }

                if (context.Perfis.Any(p => p.Id == perfilId) &&
                    !context.UsuarioPerfis.Any(up => up.UsuarioId == user.Id && up.PerfilId == perfilId))
                {
                    context.UsuarioPerfis.Add(new UsuarioPerfil { UsuarioId = user.Id, PerfilId = perfilId });
                    context.SaveChanges();
                }
            }

            EnsureDemoUser("rh@corvus.com", "Carla Souza (RH)", "Gerente de Recursos Humanos", "Recursos Humanos", "111.222.333-44", 2);
            EnsureDemoUser("contabil@corvus.com", "Beatriz Lima (Contábil)", "Controller Fiscal & Contábil", "Contabilidade", "222.333.444-55", 3);
            EnsureDemoUser("suporte@corvus.com", "Lucas Mendes (SecOps)", "Analista de Suporte & IAM N2", "Tecnologia", "333.444.555-66", 4);
            EnsureDemoUser("colaborador@corvus.com", "Rafael Oliveira", "Engenheiro de Software Pleno", "Engenharia", "444.555.666-77", 5);

            // 1. Configurações Globais essenciais
            void EnsureConfig(string chave, string valor)
            {
                if (!context.ConfiguracoesSistema.Any(c => c.Chave == chave))
                {
                    context.ConfiguracoesSistema.Add(new ConfiguracaoSistema { Chave = chave, Valor = valor });
                }
            }

            EnsureConfig("API_GATEWAY_TOKEN", Environment.GetEnvironmentVariable("API_GATEWAY_TOKEN") ?? ("corvux_gw_" + Guid.NewGuid().ToString("N")));
            EnsureConfig("LOCKDOWN_MODE", "false");
            EnsureConfig("SESSION_TIMEOUT_MINUTES", "30");
            EnsureConfig("JWT_EXPIRATION_HOURS", "8");
            EnsureConfig("MFA_ENFORCEMENT_ADMINS", "true");
            EnsureConfig("MAX_LOGIN_ATTEMPTS", "5");
            EnsureConfig("ACCOUNT_LOCKOUT_MINUTES", "15");
            EnsureConfig("WAF_RATE_LIMIT_PER_MINUTE", "60");
            EnsureConfig("AUTO_ISOLATE_HIGH_RISK_ENDPOINTS", "true");
            EnsureConfig("ENABLE_DEEP_PACKET_INSPECTION", "true");
            EnsureConfig("LGPD_DATA_RETENTION_DAYS", "180");
            EnsureConfig("AUDIT_LOG_ANONYMIZATION", "true");
            EnsureConfig("COOKIE_CONSENT_STRICT_MODE", "true");
            EnsureConfig("SMTP_HOST", "smtp.gmail.com");
            EnsureConfig("SMTP_PORT", "587");
            EnsureConfig("SYSTEM_SENDER_EMAIL", "seguranca@corvuxiam.tech");
            EnsureConfig("TURNSTILE_SITEKEY", "0x4AAAAAAFXK5J9F5V8zblrT");
            EnsureConfig("TURNSTILE_MODE", "Interactive");

            // 2. Firewall WAF (IPs Bloqueados Ativos)
            if (!context.FirewallRegras.Any())
            {
                context.FirewallRegras.AddRange(
                    new FirewallRegra { Ip = "185.15.22.4", Motivo = "Tentativa de Brute Force SSH/HTTP", PaisOrigem = "RU", Ativo = true, CriadoPor = "WAF Heuristic AI", DataBloqueio = DateTime.UtcNow.AddHours(-14) },
                    new FirewallRegra { Ip = "45.33.12.19", Motivo = "Port Scanning e SYN Flood", PaisOrigem = "CN", Ativo = true, CriadoPor = "WAF Heuristic AI", DataBloqueio = DateTime.UtcNow.AddHours(-8) },
                    new FirewallRegra { Ip = "104.22.1.80", Motivo = "Injeção SQL e Path Traversal", PaisOrigem = "KP", Ativo = true, CriadoPor = "WAF Heuristic AI", DataBloqueio = DateTime.UtcNow.AddHours(-2) }
                );
            }

            // 3. Dispositivos da Rede (Endpoints)
            if (!context.DispositivosRede.Any())
            {
                context.DispositivosRede.AddRange(
                    new DispositivoRede { Hostname = "WS-FIN-03", Ip = "192.168.10.45", Vlan = "VLAN 10 - Finanças", UsuarioResponsavel = "amanda.lima@corvusiam.tech", SistemaOperacional = "Windows 11 Pro 23H2", Tipo = "estacoes", CpuUsoPercentual = 42, RamUsoPercentual = 92, DiscoUsoPercentual = 55, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 2, Status = "Alerta", IsoladoRede = false },
                    new DispositivoRede { Hostname = "WS-RH-01", Ip = "192.168.10.12", Vlan = "VLAN 20 - RH", UsuarioResponsavel = "carla.rh@corvusiam.tech", SistemaOperacional = "Windows 11 Pro 22H2", Tipo = "estacoes", CpuUsoPercentual = 18, RamUsoPercentual = 64, DiscoUsoPercentual = 40, EdrAtivo = true, EdrStatus = "Assinatura Antiga", PatchesPendentes = 1, Status = "Alerta", IsoladoRede = false },
                    new DispositivoRede { Hostname = "SRV-CORVUS-API", Ip = "10.0.1.15", Vlan = "VLAN 99 - Servidores", UsuarioResponsavel = "Backend API (.NET 8)", SistemaOperacional = "Ubuntu Server 24.04 LTS", Tipo = "servidores", CpuUsoPercentual = 35, RamUsoPercentual = 72, DiscoUsoPercentual = 89, EdrAtivo = true, EdrStatus = "AppArmor / ClamAV", PatchesPendentes = 0, Status = "Critico", IsoladoRede = false },
                    new DispositivoRede { Hostname = "SRV-SQL-DATABASE", Ip = "10.0.1.20", Vlan = "VLAN 99 - Servidores", UsuarioResponsavel = "MS SQL Server 2022", SistemaOperacional = "Ubuntu 22.04 LTS (Container)", Tipo = "servidores", CpuUsoPercentual = 22, RamUsoPercentual = 68, DiscoUsoPercentual = 42, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "WS-DEV-01", Ip = "192.168.30.88", Vlan = "VLAN 30 - Desenvolvimento", UsuarioResponsavel = "dev01@corvusiam.tech", SistemaOperacional = "Fedora Workstation 40", Tipo = "estacoes", CpuUsoPercentual = 12, RamUsoPercentual = 45, DiscoUsoPercentual = 38, EdrAtivo = true, EdrStatus = "SELinux Enforcing", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "WS-DIR-MACBOOK", Ip = "192.168.10.99", Vlan = "VLAN 10 - Executiva", UsuarioResponsavel = "diretor.executivo@corvusiam.tech", SistemaOperacional = "macOS Sonoma 14.5", Tipo = "estacoes", CpuUsoPercentual = 8, RamUsoPercentual = 52, DiscoUsoPercentual = 29, EdrAtivo = true, EdrStatus = "FileVault & EDR OK", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "SRV-NGINX-PROXY", Ip = "10.0.1.10", Vlan = "VLAN 99 - Servidores", UsuarioResponsavel = "Reverse Proxy & TLS", SistemaOperacional = "Alpine Linux 3.20", Tipo = "servidores", CpuUsoPercentual = 5, RamUsoPercentual = 28, DiscoUsoPercentual = 22, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "SRV-BACKUP-S3", Ip = "10.0.1.50", Vlan = "VLAN 99 - Servidores", UsuarioResponsavel = "Rotina de Backup AWS S3", SistemaOperacional = "Debian 12 Bookworm", Tipo = "servidores", CpuUsoPercentual = 14, RamUsoPercentual = 36, DiscoUsoPercentual = 51, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "WS-TI-SUPORTE01", Ip = "192.168.20.15", Vlan = "VLAN 20 - Suporte", UsuarioResponsavel = "lucas.suporte@corvusiam.tech", SistemaOperacional = "Windows 11 Pro 23H2", Tipo = "estacoes", CpuUsoPercentual = 25, RamUsoPercentual = 58, DiscoUsoPercentual = 44, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "WS-TI-SUPORTE02", Ip = "192.168.20.16", Vlan = "VLAN 20 - Suporte", UsuarioResponsavel = "marcos.suporte@corvusiam.tech", SistemaOperacional = "Windows 11 Pro 23H2", Tipo = "estacoes", CpuUsoPercentual = 19, RamUsoPercentual = 61, DiscoUsoPercentual = 46, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "WS-CONTAB-01", Ip = "192.168.10.31", Vlan = "VLAN 10 - Finanças", UsuarioResponsavel = "beatriz.contabil@corvusiam.tech", SistemaOperacional = "Windows 11 Pro 23H2", Tipo = "estacoes", CpuUsoPercentual = 22, RamUsoPercentual = 50, DiscoUsoPercentual = 39, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "WS-CONTAB-02", Ip = "192.168.10.32", Vlan = "VLAN 10 - Finanças", UsuarioResponsavel = "fernando.fiscal@corvusiam.tech", SistemaOperacional = "Windows 11 Pro 23H2", Tipo = "estacoes", CpuUsoPercentual = 16, RamUsoPercentual = 48, DiscoUsoPercentual = 35, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "WS-RH-02", Ip = "192.168.10.13", Vlan = "VLAN 20 - RH", UsuarioResponsavel = "juliana.dp@corvusiam.tech", SistemaOperacional = "Windows 11 Pro 23H2", Tipo = "estacoes", CpuUsoPercentual = 14, RamUsoPercentual = 46, DiscoUsoPercentual = 33, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "WS-DEV-02", Ip = "192.168.30.89", Vlan = "VLAN 30 - Desenvolvimento", UsuarioResponsavel = "dev02@corvusiam.tech", SistemaOperacional = "Ubuntu 24.04 LTS", Tipo = "estacoes", CpuUsoPercentual = 28, RamUsoPercentual = 62, DiscoUsoPercentual = 48, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "WS-DEV-03", Ip = "192.168.30.90", Vlan = "VLAN 30 - Desenvolvimento", UsuarioResponsavel = "dev03@corvusiam.tech", SistemaOperacional = "Windows 11 Pro 23H2", Tipo = "estacoes", CpuUsoPercentual = 31, RamUsoPercentual = 65, DiscoUsoPercentual = 52, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "WS-DEV-04", Ip = "192.168.30.91", Vlan = "VLAN 30 - Desenvolvimento", UsuarioResponsavel = "dev04@corvusiam.tech", SistemaOperacional = "Windows 11 Pro 23H2", Tipo = "estacoes", CpuUsoPercentual = 20, RamUsoPercentual = 54, DiscoUsoPercentual = 41, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "WS-DEV-05", Ip = "192.168.30.92", Vlan = "VLAN 30 - Desenvolvimento", UsuarioResponsavel = "dev05@corvusiam.tech", SistemaOperacional = "Linux Mint 22", Tipo = "estacoes", CpuUsoPercentual = 17, RamUsoPercentual = 49, DiscoUsoPercentual = 37, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "WS-RECEP-01", Ip = "192.168.10.05", Vlan = "VLAN 10 - Recepção", UsuarioResponsavel = "recepcao@corvusiam.tech", SistemaOperacional = "Windows 10 Pro 22H2", Tipo = "estacoes", CpuUsoPercentual = 9, RamUsoPercentual = 38, DiscoUsoPercentual = 27, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false }
                );
            }

            // 4. Notas Fiscais Reais
            if (!context.NotasFiscais.Any())
            {
                context.NotasFiscais.AddRange(
                    new NotaFiscal { Numero = 1025, TomadorNome = "Tech Solutions Brasil Ltda", TomadorCnpj = "12.345.678/0001-90", Valor = 18500.00m, Descricao = "Serviços de consultoria em IAM e conformidade LGPD", ProtocoloSefaz = "20261001099231", Status = "Autorizada", DataEmissao = DateTime.UtcNow.AddDays(-12) },
                    new NotaFiscal { Numero = 1026, TomadorNome = "Inovação Digital S.A.", TomadorCnpj = "98.765.432/0001-11", Valor = 42000.00m, Descricao = "Licenciamento de plataforma corporativa CorvuxIAM v2.4", ProtocoloSefaz = "20261003445129", Status = "Autorizada", DataEmissao = DateTime.UtcNow.AddDays(-9) },
                    new NotaFiscal { Numero = 1027, TomadorNome = "Consultoria Alpha Ltda", TomadorCnpj = "45.678.901/0001-22", Valor = 35800.00m, Descricao = "Auditoria e implementação de Zero Trust Architecture", ProtocoloSefaz = "20261007881920", Status = "Autorizada", DataEmissao = DateTime.UtcNow.AddDays(-5) },
                    new NotaFiscal { Numero = 1028, TomadorNome = "Banco Mercantil Parceiro", TomadorCnpj = "67.890.123/0001-33", Valor = 52220.00m, Descricao = "Serviços de auditoria contínua e integração SSO/SAML", ProtocoloSefaz = "20261010112873", Status = "Autorizada", DataEmissao = DateTime.UtcNow.AddDays(-2) }
                );
            }

            // 5. Fechamentos Mensais
            if (!context.FechamentosMensais.Any())
            {
                context.FechamentosMensais.AddRange(
                    new FechamentoMensal { MesAno = "09/2026", ReceitaBruta = 132000.00m, DespesasOperacionais = 38500.00m, LucroLiquido = 93500.00m, Status = "Fechado", DataFechamento = DateTime.UtcNow.AddDays(-13) },
                    new FechamentoMensal { MesAno = "08/2026", ReceitaBruta = 125400.00m, DespesasOperacionais = 36200.00m, LucroLiquido = 89200.00m, Status = "Fechado", DataFechamento = DateTime.UtcNow.AddDays(-43) }
                );
            }

            // 6. Folha de Pagamento
            if (!context.FolhasPagamento.Any())
            {
                context.FolhasPagamento.Add(new FolhaPagamento
                {
                    MesReferencia = "09/2026",
                    TotalColaboradores = 18,
                    ValorBrutoTotal = 102500.00m,
                    ValorLiquidoTotal = 84200.00m,
                    InssTotal = 9260.00m,
                    FgtsTotal = 6736.00m,
                    Status = "Processada",
                    DataProcessamento = DateTime.UtcNow.AddDays(-14)
                });
            }

            // 7. Registros de Férias
            if (!context.RegistrosFerias.Any())
            {
                context.RegistrosFerias.AddRange(
                    new RegistroFerias { ColaboradorNome = "Amanda Lima", DataInicio = DateTime.UtcNow.AddDays(15), DataFim = DateTime.UtcNow.AddDays(45), Dias = 30, Status = "Aprovada", DataAprovacao = DateTime.UtcNow.AddDays(-3) },
                    new RegistroFerias { ColaboradorNome = "Carla Souza", DataInicio = DateTime.UtcNow.AddDays(60), DataFim = DateTime.UtcNow.AddDays(80), Dias = 20, Status = "Aprovada", DataAprovacao = DateTime.UtcNow.AddDays(-1) }
                );
            }

            // 8. Chamados de Suporte Iniciais (ITSM / SLA Zero Trust)
            if (!context.ChamadosSuporte.Any())
            {
                var adminUser = context.Usuarios.FirstOrDefault(u => u.Email == "admin@corvus.com");
                var colabUser = context.Usuarios.FirstOrDefault(u => u.Email == "colaborador@corvus.com") ?? adminUser;
                var suporteUser = context.Usuarios.FirstOrDefault(u => u.Email == "suporte@corvus.com") ?? adminUser;

                if (colabUser != null)
                {
                    context.ChamadosSuporte.AddRange(
                        new ChamadoSuporte
                        {
                            UsuarioId = colabUser.Id,
                            ResponsavelId = suporteUser?.Id,
                            Assunto = "Provisionamento de Acesso VPN Zero Trust (VLAN 30)",
                            Descricao = "Solicito liberação de túnel WireGuard/Zero Trust para acesso ao cluster de homologação.",
                            Categoria = "Acesso",
                            Prioridade = "Alta",
                            Status = "Em Atendimento",
                            DataAbertura = DateTime.UtcNow.AddHours(-6),
                            DataAtualizacao = DateTime.UtcNow.AddHours(-2),
                            PrazoSla = DateTime.UtcNow.AddHours(2),
                            Solucao = string.Empty
                        },
                        new ChamadoSuporte
                        {
                            UsuarioId = colabUser.Id,
                            Assunto = "Recadastramento de Chave TOTP / Autenticador MFA",
                            Descricao = "Troquei de aparelho corporativo e preciso revalidar o QR Code do autenticador 2FA.",
                            Categoria = "Segurança",
                            Prioridade = "Normal",
                            Status = "Aberto",
                            DataAbertura = DateTime.UtcNow.AddHours(-3),
                            DataAtualizacao = DateTime.UtcNow.AddHours(-3),
                            PrazoSla = DateTime.UtcNow.AddHours(21),
                            Solucao = string.Empty
                        }
                    );
                }
            }

            context.SaveChanges();
        }
    }
}

