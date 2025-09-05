using System.Threading.Channels;
using System.Threading.Tasks;
using NexusCore.Application.Interfaces;

namespace NexusCore.Infra.IoC.Services
{
    public class EmailQueue : IEmailQueue
    {
        private readonly Channel<EmailJob> _channel = Channel.CreateUnbounded<EmailJob>();

        public void Enqueue(EmailJob job)
        {
            _channel.Writer.TryWrite(job);
        }

        public async Task<EmailJob> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _channel.Reader.ReadAsync(cancellationToken);
        }
    }
}