using System.Threading.Tasks;
using NexusCore.Application.Interfaces;
namespace NexusCore.Infra.IoC.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IEmailQueue _emailQueue;
        public EmailSender(IEmailQueue emailQueue) { _emailQueue = emailQueue; }
        public Task SendEmailAsync(string email, string subject, string message)
        {
            _emailQueue.Enqueue(new EmailJob(email, subject, message));
            return Task.CompletedTask;
        }
    }
}