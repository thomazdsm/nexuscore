using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexusCore.Application.Interfaces;
using NexusCore.Application.Services;
using NexusCore.Domain.Entities;
using NexusCore.Domain.Interfaces;
using NexusCore.Infra.Data.Context;
using NexusCore.Infra.Data.Repositories;
using NexusCore.Infra.IoC.Services;

namespace NexusCore.Infra.IoC
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {

            // 1. REGISTRO DO IDENTITY
            // O AddIdentity já registra os serviços de autenticação necessários, incluindo o cookie handler.
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            // 2. CONFIGURAÇÃO DO COOKIE DO IDENTITY (O MODO CORRETO)
            // Usamos ConfigureApplicationCookie para customizar as opções do cookie que o AddIdentity já registrou.
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
            });

            // 3. REGISTRO DO DBCONTEXT
            // A única fonte de verdade para a configuração do DbContext.
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), npgsqlOptions =>
                {
                    // Adiciona a política de retentativa para a conexão com o banco
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5, // Tenta reconectar até 5 vezes
                        maxRetryDelay: TimeSpan.FromSeconds(10), // Espera no máximo 10s entre as tentativas
                        errorCodesToAdd: null);
                });
                options.UseOpenIddict();

                //options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));
                //options.UseOpenIddict();
            });

            // 4. CONFIGURAÇÃO DO OPENIDDICT
            services.AddOpenIddict()
                .AddCore(options =>
                {
                    options.UseEntityFrameworkCore()
                           .UseDbContext<AppDbContext>();
                })
                .AddServer(options =>
                {
                    options.SetAuthorizationEndpointUris("/connect/authorize")
                           .SetTokenEndpointUris("/connect/token")
                           .SetUserInfoEndpointUris("/connect/userinfo");

                    options.AllowAuthorizationCodeFlow()
                           .AllowRefreshTokenFlow();
                    
                    options.AddDevelopmentEncryptionCertificate()
                           .AddDevelopmentSigningCertificate();

                    options.UseAspNetCore()
                           .EnableTokenEndpointPassthrough()
                           .EnableAuthorizationEndpointPassthrough()
                           .EnableUserInfoEndpointPassthrough();
                });
            
            // Configura a fila de e-mails
            services.AddSingleton<IEmailQueue, EmailQueue>();
            // Adiciona o serviço que processará a fila em background
            services.AddHostedService<BackgroundEmailSender>();
            // O IEmailSender agora apenas enfileira os e-mails
            services.AddTransient<IEmailSender, EmailSender>();

            services.AddAutoMapper(cfg => cfg.LicenseKey = configuration["AutoMapper:LicenseKey"], AppDomain.CurrentDomain.GetAssemblies());

            // Registra os Repositórios e Serviços
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductService, ProductService>();

            return services;
        }
    }
}