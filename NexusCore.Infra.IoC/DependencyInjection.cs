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
using Microsoft.AspNetCore.Http; 

namespace NexusCore.Infra.IoC
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // REGISTRO DO IDENTITY
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            // CONFIGURAÇÃO DO COOKIE DO IDENTITY (O MODO CORRETO)
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.None;
            });

            // REGISTRO DO DBCONTEXT
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                });
                options.UseOpenIddict();
            });

            // CONFIGURAÇÃO DO OPENIDDICT
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
            services.AddHostedService<BackgroundEmailSender>();
            services.AddTransient<IEmailSender, EmailSender>();

            services.AddAutoMapper(cfg => cfg.LicenseKey = configuration["AutoMapper:LicenseKey"], AppDomain.CurrentDomain.GetAssemblies());

            // Registra os Repositórios e Serviços
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductService, ProductService>();

            return services;
        }
    }
}