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

                bool isJsonRequest = context.Request.Path.StartsWithSegments("/api") ||
                                     context.Request.Headers["Accept"].ToString().Contains("application/json") ||
                                     context.Request.ContentType?.Contains("application/json") == true ||
                                     context.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

                if (isJsonRequest)
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                    var problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status500InternalServerError,
                        Title = "Erro interno no servidor",
                        Detail = ex.Message,
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

