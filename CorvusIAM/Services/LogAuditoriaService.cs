using System;
using System.Threading.Tasks;
using CorvusIAM.Data;
using CorvusIAM.Entities;
using CorvusIAM.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace CorvusIAM.Services
{
    public class LogAuditoriaService : ILogAuditoriaService
    {
        private readonly ApplicationDbContext _context;

        public LogAuditoriaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task RegistrarLog(int? usuarioId, string acao, string detalhe, string tipo, string origem)
        {
            // Se estiver executando em SQL Server, invoca a Stored Procedure sp_RegistrarLog exigida pelo PIM IV
            if (_context.Database.IsSqlServer())
            {
                try
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC sp_RegistrarLog @p0, @p1, @p2, @p3, @p4",
                        usuarioId.HasValue ? (object)usuarioId.Value : DBNull.Value,
                        acao ?? string.Empty,
                        detalhe ?? string.Empty,
                        tipo ?? "Info",
                        origem ?? "Sistema");
                    return;
                }
                catch
                {
                    // Fallback para persistência direta via EF Core caso a procedure não esteja instalada
                }
            }

            var log = new LogAuditoria
            {
                UsuarioId = usuarioId,
                Acao = acao,
                Detalhe = detalhe,
                Tipo = tipo,
                Origem = origem,
                DataHora = DateTime.UtcNow
            };

            _context.LogsAuditoria.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
