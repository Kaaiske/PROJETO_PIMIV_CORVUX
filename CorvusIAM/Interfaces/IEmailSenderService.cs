using System.Threading.Tasks;

namespace CorvusIAM.Interfaces
{
    public interface IEmailSenderService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }
}
