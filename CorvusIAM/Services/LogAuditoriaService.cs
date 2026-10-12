using System;
using System.Threading.Tasks;
using CorvusIAM.Data;
using CorvusIAM.Entities;
using CorvusIAM.Interfaces;

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
