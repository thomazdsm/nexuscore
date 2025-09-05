using System;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit;
using NexusCore.Application.Interfaces;

namespace NexusCore.Infra.IoC.Services
{
    public class BackgroundEmailSender : BackgroundService
    {
        private readonly IEmailQueue _emailQueue;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BackgroundEmailSender> _logger;

        public BackgroundEmailSender(IEmailQueue emailQueue, IConfiguration configuration, ILogger<BackgroundEmailSender> logger)
        {
            _emailQueue = emailQueue;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var job = await _emailQueue.DequeueAsync(stoppingToken);

                    var emailSettings = _configuration.GetSection("EmailSettings");
                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress(emailSettings["SenderName"], emailSettings["SenderEmail"]));
                    message.To.Add(new MailboxAddress(job.To, job.To));
                    message.Subject = job.Subject;
                    message.Body = new TextPart("html") { Text = job.Body };

                    using var client = new SmtpClient();

                    await client.ConnectAsync(
                        emailSettings["SmtpServer"], 
                        int.Parse(emailSettings["Port"]), 
                        SecureSocketOptions.SslOnConnect, // Correto para porta 465
                        stoppingToken);

                    await client.AuthenticateAsync(emailSettings["Username"], emailSettings["Password"], stoppingToken);
                    await client.SendAsync(message, stoppingToken);
                    await client.DisconnectAsync(true, stoppingToken);

                    _logger.LogInformation("E-mail para {To} enviado com sucesso.", job.To);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao enviar e-mail em background.");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }
    }
}