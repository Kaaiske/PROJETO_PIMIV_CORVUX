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
                ");
            }
            else
            {
                // SQLite (desenvolvimento / testes)
                context.Database.EnsureCreated();
            }

            // 1. Configurações Globais essenciais
            if (!context.ConfiguracoesSistema.Any(c => c.Chave == "API_GATEWAY_TOKEN"))
            {
                context.ConfiguracoesSistema.Add(new ConfiguracaoSistema { Chave = "API_GATEWAY_TOKEN", Valor = "corvus_sec_live_9f83a218d47b0192e" });
            }
            if (!context.ConfiguracoesSistema.Any(c => c.Chave == "LOCKDOWN_MODE"))
            {
                context.ConfiguracoesSistema.Add(new ConfiguracaoSistema { Chave = "LOCKDOWN_MODE", Valor = "false" });
            }

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
                    new DispositivoRede { Hostname = "WS-DEV-GABRIEL", Ip = "192.168.30.88", Vlan = "VLAN 30 - Desenvolvimento", UsuarioResponsavel = "gabriel.ti@corvusiam.tech", SistemaOperacional = "Fedora Workstation 40", Tipo = "estacoes", CpuUsoPercentual = 12, RamUsoPercentual = 45, DiscoUsoPercentual = 38, EdrAtivo = true, EdrStatus = "SELinux Enforcing", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "WS-DIR-MACBOOK", Ip = "192.168.10.99", Vlan = "VLAN 10 - Executiva", UsuarioResponsavel = "diretor.executivo@corvusiam.tech", SistemaOperacional = "macOS Sonoma 14.5", Tipo = "estacoes", CpuUsoPercentual = 8, RamUsoPercentual = 52, DiscoUsoPercentual = 29, EdrAtivo = true, EdrStatus = "FileVault & EDR OK", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "SRV-NGINX-PROXY", Ip = "10.0.1.10", Vlan = "VLAN 99 - Servidores", UsuarioResponsavel = "Reverse Proxy & TLS", SistemaOperacional = "Alpine Linux 3.20", Tipo = "servidores", CpuUsoPercentual = 5, RamUsoPercentual = 28, DiscoUsoPercentual = 22, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "SRV-BACKUP-S3", Ip = "10.0.1.50", Vlan = "VLAN 99 - Servidores", UsuarioResponsavel = "Rotina de Backup AWS S3", SistemaOperacional = "Debian 12 Bookworm", Tipo = "servidores", CpuUsoPercentual = 14, RamUsoPercentual = 36, DiscoUsoPercentual = 51, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "WS-TI-SUPORTE01", Ip = "192.168.20.15", Vlan = "VLAN 20 - Suporte", UsuarioResponsavel = "lucas.suporte@corvusiam.tech", SistemaOperacional = "Windows 11 Pro 23H2", Tipo = "estacoes", CpuUsoPercentual = 25, RamUsoPercentual = 58, DiscoUsoPercentual = 44, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "WS-TI-SUPORTE02", Ip = "192.168.20.16", Vlan = "VLAN 20 - Suporte", UsuarioResponsavel = "marcos.suporte@corvusiam.tech", SistemaOperacional = "Windows 11 Pro 23H2", Tipo = "estacoes", CpuUsoPercentual = 19, RamUsoPercentual = 61, DiscoUsoPercentual = 46, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "WS-CONTAB-01", Ip = "192.168.10.31", Vlan = "VLAN 10 - Finanças", UsuarioResponsavel = "beatriz.contabil@corvusiam.tech", SistemaOperacional = "Windows 11 Pro 23H2", Tipo = "estacoes", CpuUsoPercentual = 22, RamUsoPercentual = 50, DiscoUsoPercentual = 39, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "WS-CONTAB-02", Ip = "192.168.10.32", Vlan = "VLAN 10 - Finanças", UsuarioResponsavel = "fernando.fiscal@corvusiam.tech", SistemaOperacional = "Windows 11 Pro 23H2", Tipo = "estacoes", CpuUsoPercentual = 16, RamUsoPercentual = 48, DiscoUsoPercentual = 35, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "WS-RH-02", Ip = "192.168.10.13", Vlan = "VLAN 20 - RH", UsuarioResponsavel = "juliana.dp@corvusiam.tech", SistemaOperacional = "Windows 11 Pro 23H2", Tipo = "estacoes", CpuUsoPercentual = 14, RamUsoPercentual = 46, DiscoUsoPercentual = 33, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "WS-DEV-RAPHAEL", Ip = "192.168.30.89", Vlan = "VLAN 30 - Desenvolvimento", UsuarioResponsavel = "raphael.dev@corvusiam.tech", SistemaOperacional = "Ubuntu 24.04 LTS", Tipo = "estacoes", CpuUsoPercentual = 28, RamUsoPercentual = 62, DiscoUsoPercentual = 48, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "WS-DEV-VICTOR", Ip = "192.168.30.90", Vlan = "VLAN 30 - Desenvolvimento", UsuarioResponsavel = "victor.dev@corvusiam.tech", SistemaOperacional = "Windows 11 Pro 23H2", Tipo = "estacoes", CpuUsoPercentual = 31, RamUsoPercentual = 65, DiscoUsoPercentual = 52, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "WS-DEV-NICOLAS", Ip = "192.168.30.91", Vlan = "VLAN 30 - Desenvolvimento", UsuarioResponsavel = "nicolas.dev@corvusiam.tech", SistemaOperacional = "Windows 11 Pro 23H2", Tipo = "estacoes", CpuUsoPercentual = 20, RamUsoPercentual = 54, DiscoUsoPercentual = 41, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
                    new DispositivoRede { Hostname = "WS-DEV-JHEFERSON", Ip = "192.168.30.92", Vlan = "VLAN 30 - Desenvolvimento", UsuarioResponsavel = "jheferson.dev@corvusiam.tech", SistemaOperacional = "Linux Mint 22", Tipo = "estacoes", CpuUsoPercentual = 17, RamUsoPercentual = 49, DiscoUsoPercentual = 37, EdrAtivo = true, EdrStatus = "EDR Protegido", PatchesPendentes = 0, Status = "Saudavel", IsoladoRede = false },
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

            context.SaveChanges();
        }
    }
}

