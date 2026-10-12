using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exceção não tratada capturada pelo middleware.");

                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.ContentType = "application/problem+json";
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status500InternalServerError,
                        Title = "Erro interno no servidor",
                        Detail = "Ocorreu um erro inesperado ao processar sua solicitação. Tente novamente mais tarde.",
                        Instance = context.Request.Path
                    };

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

