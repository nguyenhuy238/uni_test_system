using System.Threading.Tasks;

namespace UniTestSystem.Application.Interfaces
{
    public interface IEmailSender
    {
        Task SendAsync(string email, string subject, string message);
    }
}
