namespace NexusCore.Application.Interfaces
{
    public record EmailJob(string To, string Subject, string Body);
    public interface IEmailQueue
    {
        void Enqueue(EmailJob job);
        Task<EmailJob> DequeueAsync(CancellationToken cancellationToken);
    }
}