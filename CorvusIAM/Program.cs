using Polly;
using Polly.Extensions.Http;
using CorvusIAM.Data;
using CorvusIAM.Interfaces;
using CorvusIAM.Repositories;
using CorvusIAM.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Prometheus;
using Serilog;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/corvusiam-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add DbContext
var databaseProvider = builder.Configuration["DatabaseProvider"];
if (databaseProvider == "SqlServer")
{
    builder.Services.AddDbContext<ApplicationDbContext, SqlServerDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerConnection")));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext, SqliteDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("SqliteConnection")));
}

// DI
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILogAuditoriaService, LogAuditoriaService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IPasswordGeneratorService, PasswordGeneratorService>();
builder.Services.AddScoped<ITotpService, TotpService>();
builder.Services.AddScoped<IStorageService, StorageService>();

// External API Services
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}

builder.Services.AddHttpClient<IPasswordPwnedService, PasswordPwnedService>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

builder.Services.AddHttpClient<IIpGeolocationService, IpGeolocationService>(c =>
{
    c.Timeout = TimeSpan.FromMilliseconds(1500);
});

builder.Services.AddHttpClient<IEmailValidationService, EmailValidationService>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

builder.Services.AddHttpClient<ITurnstileService, TurnstileService>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

builder.Services.AddScoped<IEmailSenderService, EmailSenderService>();
builder.Services.AddHttpClient();

// CORS for corvuxiam.tech
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowCorvuxDomain", policy =>
    {
        policy.WithOrigins(
            "https://corvuxiam.tech",
            "http://corvuxiam.tech",
            "http://localhost:5052",
            "https://localhost:7052",
            "http://localhost:8080")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Memory Cache
builder.Services.AddMemoryCache();

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("LoginPolicy", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Swagger / OpenAPI Specification
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CorvusIAM API — Enterprise Identity & Access Management",
        Version = "v1.0",
        Description = @"### 🛡️ Visão Geral da API CorvuxIAM
A **CorvusIAM API** fornece controle unificado de Identidade, Autenticação Corporativa, Governança de Acessos (IAM/IGA) e Cibersegurança em conformidade com os princípios de **Zero-Trust**, **LGPD** e modelos **RBAC/ABAC**.

---

### 🔑 Métodos de Autenticação Suportados:
1. **Bearer Token (JWT)**:
   - Obtenha o token através do endpoint `POST /api/Auth/login`.
   - Inclua no cabeçalho HTTP de cada requisição: `Authorization: Bearer <seu_token_jwt>`.
2. **API Key Corporativa (M2M / Gateway)**:
   - Para integrações entre sistemas (ERPs, CRMs, Webhooks, automações), utilize a chave configurada no Gateway do sistema.
   - Inclua no cabeçalho HTTP: `X-API-Key: <sua_api_key>` ou `Authorization: Bearer <sua_api_key>` nos endpoints de integração (`/api/v1/*`).

---

### 🌐 Módulos e Recursos Disponíveis:
- **Autenticação & Sessões**: Geração e renovação de tokens JWT com mitigação de brute-force por Rate Limiting.
- **Inbound Gateway (v1)**: Sincronização e consulta externa de diretório corporativo e status de colaboradores.
- **IAM Core**: Ciclo de vida completo de usuários, atribuição de perfis, controle de MFA e anonimização LGPD.
- **Service Desk & Chamados**: Abertura, tramitação e encerramento de tickets com cálculo automático de SLA de acordo com a prioridade.
- **CyberSec, IDS & DPI**: Inspeção e bloqueio de ameaças de rede, telemetria de tráfego de sockets e firewall por host.
- **Serviços Governamentais & Nacionais**: Integração em tempo real com ViaCEP, BrasilAPI (CNPJ, Bancos, Feriados Nacionais) e validação oficial de CPF (Módulo 11 da Receita Federal).",
        Contact = new OpenApiContact
        {
            Name = "Corvux SecOps & DevOps Engineering Team",
            Email = "suporte@corvuxiam.tech",
            Url = new Uri("https://corvuxiam.tech")
        },
        License = new OpenApiLicense
        {
            Name = "MIT Enterprise License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    c.AddServer(new OpenApiServer { Url = "https://corvuxiam.tech", Description = "Servidor de Produção Oficial (Cloudflare Edge SSL)" });
    c.AddServer(new OpenApiServer { Url = "http://localhost:8080", Description = "Ambiente Container Docker Local" });
    c.AddServer(new OpenApiServer { Url = "http://localhost:5052", Description = "Ambiente de Desenvolvimento Local (HTTP)" });

    // JWT Bearer Security Definition
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT no formato: Bearer {seu_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // API Key Security Definition
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Chave de API corporativa para integrações máquina-a-máquina (M2M) via cabeçalho 'X-API-Key'",
        Name = "X-API-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        },
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (System.IO.File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

// Health Checks
builder.Services.AddHealthChecks();

// JWT Authentication
var keyStr = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(keyStr)) {
    keyStr = builder.Configuration["JWT_KEY"] 
             ?? builder.Configuration["Jwt__Key"]
             ?? "CorvuxIAM_SegurancaCorporativa_JWT_Secret_Key_2026_Producao!";
}
var key = Encoding.ASCII.GetBytes(keyStr);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireHR", policy => policy.RequireRole("Recursos Humanos", "Admin"));
    options.AddPolicy("RequireAccounting", policy => policy.RequireRole("Contabilidade", "Admin"));
    options.AddPolicy("RequireHelpDesk", policy => policy.RequireRole("Help Desk", "Admin"));
    options.AddPolicy("RequireAdminOrHelpDesk", policy => policy.RequireRole("Help Desk", "Admin"));
    options.AddPolicy("CanViewUsers", policy => policy.RequireClaim("permission", "usuarios.visualizar"));
    options.AddPolicy("CanManageUsers", policy => policy.RequireClaim("permission", "usuarios.gerenciar"));
    options.AddPolicy("CanManageProfiles", policy => policy.RequireClaim("permission", "perfis.gerenciar"));
    options.AddPolicy("CanViewLogs", policy => policy.RequireClaim("permission", "logs.visualizar"));
});

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = "CookiesAndJwt";
    x.DefaultChallengeScheme = "CookiesAndJwt";
})
.AddPolicyScheme("CookiesAndJwt", "JWT and Cookies", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader != null && authHeader.StartsWith("Bearer "))
            return JwtBearerDefaults.AuthenticationScheme;
        return "Cookies";
    };
})
.AddCookie("Cookies", options =>
{
    options.LoginPath = "/Auth/Login";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

var app = builder.Build();

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (context.Database.IsRelational())
    {
        int retries = 10;
        while (retries > 0)
        {
            try
            {
                context.Database.Migrate();

                try
                {
                    if (context.Database.IsSqlServer())
                    {
                        context.Database.ExecuteSqlRaw(@"
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Perfis]') AND name = 'Ativo')
                                ALTER TABLE [Perfis] ADD [Ativo] BIT NOT NULL CONSTRAINT DF_Perfis_Ativo DEFAULT 1;
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Usuarios]') AND name = 'MfaHabilitado')
                                ALTER TABLE [Usuarios] ADD [MfaHabilitado] BIT NOT NULL CONSTRAINT DF_Usuarios_Mfa DEFAULT 0;
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Usuarios]') AND name = 'MfaSecretKey')
                                ALTER TABLE [Usuarios] ADD [MfaSecretKey] NVARCHAR(100) NULL;
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[ChamadosSuporte]') AND name = 'AnexoUrl')
                                ALTER TABLE [ChamadosSuporte] ADD [AnexoUrl] NVARCHAR(500) NULL;
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[ChamadosSuporte]') AND name = 'AnexoNomeOriginal')
                                ALTER TABLE [ChamadosSuporte] ADD [AnexoNomeOriginal] NVARCHAR(255) NULL;

                            UPDATE [Usuarios] SET [MfaSecretKey] = '' WHERE [MfaSecretKey] IS NULL;
                            UPDATE [Usuarios] SET [ResetSenhaToken] = '' WHERE [ResetSenhaToken] IS NULL;
                            UPDATE [Usuarios] SET [RefreshToken] = '' WHERE [RefreshToken] IS NULL;
                            UPDATE [Usuarios] SET [CPF] = '' WHERE [CPF] IS NULL;
                            UPDATE [Usuarios] SET [Cargo] = '' WHERE [Cargo] IS NULL;
                            UPDATE [Usuarios] SET [RG] = '' WHERE [RG] IS NULL;
                            UPDATE [Usuarios] SET [Telefone] = '' WHERE [Telefone] IS NULL;
                            UPDATE [Usuarios] SET [Departamento] = '' WHERE [Departamento] IS NULL;
                            UPDATE [Usuarios] SET [Endereco] = '' WHERE [Endereco] IS NULL;
                        ");

                        // Assegura criação/atualização das Stored Procedures (PIM IV)
                        context.Database.ExecuteSqlRaw(@"
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
                        ");

                        context.Database.ExecuteSqlRaw(@"
                            CREATE OR ALTER PROCEDURE sp_InativarColaborador
                                @UsuarioId INT,
                                @AdminId INT = NULL
                            AS
                            BEGIN
                                SET NOCOUNT ON;
                                BEGIN TRANSACTION;
                                BEGIN TRY
                                    UPDATE Usuarios
                                    SET Ativo = 0,
                                        RefreshToken = '',
                                        RefreshTokenExpiracao = NULL,
                                        ResetSenhaToken = '',
                                        ResetSenhaExpiracao = NULL
                                    WHERE Id = @UsuarioId;

                                    INSERT INTO LogsAuditoria (UsuarioId, DataHora, Acao, Detalhe, Tipo, Origem)
                                    VALUES (@AdminId, GETUTCDATE(), 'Inativação de Colaborador', CONCAT('Colaborador ID ', @UsuarioId, ' inativado via Stored Procedure sp_InativarColaborador'), 'Warning', 'Stored Procedure');

                                    COMMIT TRANSACTION;
                                END TRY
                                BEGIN CATCH
                                    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                                    THROW;
                                END CATCH;
                            END;
                        ");

                        context.Database.ExecuteSqlRaw(@"
                            CREATE OR ALTER PROCEDURE sp_ObterResumoAuditoria
                                @DiasRetroativos INT = 30
                            AS
                            BEGIN
                                SET NOCOUNT ON;
                                DECLARE @DataLimite DATETIME2 = DATEADD(DAY, -@DiasRetroativos, GETUTCDATE());

                                SELECT Tipo, Origem, COUNT(*) AS TotalEventos, MIN(DataHora) AS PrimeiroRegistro, MAX(DataHora) AS UltimoRegistro
                                FROM LogsAuditoria WHERE DataHora >= @DataLimite GROUP BY Tipo, Origem ORDER BY TotalEventos DESC;

                                SELECT TOP 10 Id, UsuarioId, DataHora, Acao, Detalhe, Tipo, Origem
                                FROM LogsAuditoria WHERE DataHora >= @DataLimite AND Tipo IN ('Danger', 'Warning', 'SecurityAlert') ORDER BY DataHora DESC;
                            END;
                        ");

                        // Assegura criação/atualização dos Triggers (PIM IV)
                        context.Database.ExecuteSqlRaw(@"
                            CREATE OR ALTER TRIGGER trg_AuditoriaUsuarios
                            ON Usuarios
                            AFTER INSERT, UPDATE, DELETE
                            AS
                            BEGIN
                                SET NOCOUNT ON;
                                IF EXISTS (SELECT 1 FROM inserted) AND NOT EXISTS (SELECT 1 FROM deleted)
                                BEGIN
                                    INSERT INTO LogsAuditoria (UsuarioId, DataHora, Acao, Detalhe, Tipo, Origem)
                                    SELECT i.Id, GETUTCDATE(), 'Criação de Usuário', CONCAT('Novo colaborador cadastrado: ', i.Email, ' (Nome: ', i.Nome, ')'), 'Info', 'DB Trigger [trg_AuditoriaUsuarios]'
                                    FROM inserted i;
                                END;
                                IF EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
                                BEGIN
                                    INSERT INTO LogsAuditoria (UsuarioId, DataHora, Acao, Detalhe, Tipo, Origem)
                                    SELECT i.Id, GETUTCDATE(), 'Alteração Cadastral', CONCAT('Usuário ', i.Email, ' modificado. Ativo: ', CAST(i.Ativo AS VARCHAR(1)), ', MFA: ', CAST(i.MfaHabilitado AS VARCHAR(1))), 'Warning', 'DB Trigger [trg_AuditoriaUsuarios]'
                                    FROM inserted i;
                                END;
                                IF NOT EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
                                BEGIN
                                    INSERT INTO LogsAuditoria (UsuarioId, DataHora, Acao, Detalhe, Tipo, Origem)
                                    SELECT d.Id, GETUTCDATE(), 'Exclusão Física de Usuário', CONCAT('Registro removido fisicamente da base: ', d.Email), 'Danger', 'DB Trigger [trg_AuditoriaUsuarios]'
                                    FROM deleted d;
                                END;
                            END;
                        ");

                        context.Database.ExecuteSqlRaw(@"
                            CREATE OR ALTER TRIGGER trg_AuditoriaChamados
                            ON ChamadosSuporte
                            AFTER UPDATE
                            AS
                            BEGIN
                                SET NOCOUNT ON;
                                INSERT INTO LogsAuditoria (UsuarioId, DataHora, Acao, Detalhe, Tipo, Origem)
                                SELECT i.UsuarioId, GETUTCDATE(), 'Transição de Chamado', CONCAT('Chamado #', i.Id, ' alterado de status [', d.Status, '] para [', i.Status, ']'), CASE WHEN i.Status IN ('Fechado', 'Resolvido') THEN 'Info' ELSE 'Warning' END, 'DB Trigger [trg_AuditoriaChamados]'
                                FROM inserted i INNER JOIN deleted d ON i.Id = d.Id WHERE i.Status <> d.Status;
                            END;
                        ");
                    }
                    else if (context.Database.IsSqlite())
                    {
                        try { context.Database.ExecuteSqlRaw("ALTER TABLE Perfis ADD COLUMN Ativo INTEGER NOT NULL DEFAULT 1;"); } catch { }
                        try { context.Database.ExecuteSqlRaw("ALTER TABLE Usuarios ADD COLUMN MfaHabilitado INTEGER NOT NULL DEFAULT 0;"); } catch { }
                        try { context.Database.ExecuteSqlRaw("ALTER TABLE Usuarios ADD COLUMN MfaSecretKey TEXT NULL;"); } catch { }
                        try { context.Database.ExecuteSqlRaw("ALTER TABLE ChamadosSuporte ADD COLUMN AnexoUrl TEXT NULL;"); } catch { }
                        try { context.Database.ExecuteSqlRaw("ALTER TABLE ChamadosSuporte ADD COLUMN AnexoNomeOriginal TEXT NULL;"); } catch { }
                    }
                }
                catch (Exception sqlEx)
                {
                    Log.Information("Esquema do banco validado: {Msg}", sqlEx.Message);
                }

                try
                {
                    DbDataSeeder.Seed(context);
                }
                catch (Exception seedEx)
                {
                    Log.Warning("Aviso ao rodar DbDataSeeder: {Msg}", seedEx.Message);
                }

                break;
            }
            catch (Exception ex)
            {
                retries--;
                if (retries == 0)
                {
                    Log.Error(ex, "Erro ao aplicar migrações após múltiplas tentativas.");
                    throw;
                }
                Log.Warning("Aguardando banco de dados iniciar... Restam {Retries} tentativas.", retries);
                System.Threading.Thread.Sleep(3000);
            }
        }
    }
}

// Configure the HTTP request pipeline.
app.UseMiddleware<CorvusIAM.Middlewares.GlobalExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CorvusIAM Enterprise API v1.0");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "CorvusIAM — Enterprise API & Security Specification";
    c.InjectStylesheet("/css/swagger-dark.css");
    c.DisplayRequestDuration();
    c.EnableFilter();
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
});

app.UseHttpsRedirection();
var staticFileContentTypeProvider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
staticFileContentTypeProvider.Mappings[".webmanifest"] = "application/manifest+json";
staticFileContentTypeProvider.Mappings[".json"] = "application/json";
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = staticFileContentTypeProvider
});

app.UseRouting();
app.UseCors("AllowCorvuxDomain");
app.UseHttpMetrics();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.MapHealthChecks("/health");
app.MapMetrics();

app.Run();
public partial class Program { }


