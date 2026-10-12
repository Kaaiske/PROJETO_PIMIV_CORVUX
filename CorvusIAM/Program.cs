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

builder.Services.AddHttpClient<IIpGeolocationService, IpGeolocationService>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

builder.Services.AddHttpClient<IEmailValidationService, EmailValidationService>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

builder.Services.AddHttpClient<ITurnstileService, TurnstileService>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

builder.Services.AddScoped<IEmailSenderService, EmailSenderService>();

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

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CorvuxIAM API", Version = "v1", Description = "API de Gestão de Identidades e Acessos - https://corvuxiam.tech" });
    c.AddServer(new OpenApiServer { Url = "https://corvuxiam.tech", Description = "Servidor de Produção (https://corvuxiam.tech)" });
    c.AddServer(new OpenApiServer { Url = "http://localhost:5052", Description = "Servidor Local de Desenvolvimento" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// Health Checks
builder.Services.AddHealthChecks();

// JWT Authentication
var keyStr = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(keyStr)) {
    if (builder.Environment.IsDevelopment())
    {
        keyStr = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    }
    else
    {
        throw new Exception("JWT Key is missing from configuration.");
    }
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

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (context.Database.IsRelational())
    {
        context.Database.Migrate();
    }
}

// Configure the HTTP request pipeline.
app.UseMiddleware<CorvusIAM.Middlewares.GlobalExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CorvusIAM API v1"));
}

app.UseHttpsRedirection();
app.UseStaticFiles();

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


