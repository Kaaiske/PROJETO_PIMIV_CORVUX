using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace CorvusIAM.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public GlobalExceptionMiddleware(
            RequestDelegate next, 
            ILogger<GlobalExceptionMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var traceId = context.TraceIdentifier;
                _logger.LogError(ex, "Exceção não tratada capturada pelo middleware. TraceId: {TraceId}", traceId);

                bool isJsonRequest = context.Request.Path.StartsWithSegments("/api") ||
                                     context.Request.Headers["Accept"].ToString().Contains("application/json") ||
                                     context.Request.ContentType?.Contains("application/json") == true ||
                                     context.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

                if (isJsonRequest)
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                    var detailMsg = _environment.IsDevelopment()
                        ? ex.Message
                        : $"Ocorreu um erro interno ao processar a requisição. Código de rastreamento de segurança: {traceId}";

                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status500InternalServerError,
                        Title = "Erro interno no servidor",
                        Detail = detailMsg,
                        Instance = context.Request.Path
                    };
                    problemDetails.Extensions["traceId"] = traceId;

                    await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
                }
                else
                {
                    context.Response.Redirect("/Home/Error");
                }
            }
        }
    }
}
