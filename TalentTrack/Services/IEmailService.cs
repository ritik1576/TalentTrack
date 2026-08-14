using System.Threading.Tasks;

namespace TalentTrack.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body, string attachmentPath, string attachmentName);
    }
}
