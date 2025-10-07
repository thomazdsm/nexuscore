using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using NexusCore.Domain.Entities;
using NexusCore.Infra.Data.Context;
using NexusCore.Infra.Data.Seed;
using NexusCore.Infra.IoC;
using NexusCore.WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, CustomClaimsPrincipalFactory>();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var maxRetries = 5;
var retryDelay = TimeSpan.FromSeconds(5);
for (int i = 0; i < maxRetries; i++)
{
    try
    {
        logger.LogInformation("Tentando inicializar o banco de dados (Tentativa {Attempt}/{MaxAttempts})...", i + 1, maxRetries);
        await DatabaseInitializer.InitializeDatabaseAsync(app.Services);

        logger.LogInformation("Banco de dados inicializado com sucesso.");
        break; // Sai do loop se for bem-sucedido
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro ao inicializar o banco de dados. Tentando novamente em {RetryDelay} segundos...", retryDelay.Seconds);
        if (i < maxRetries - 1)
        {
            await Task.Delay(retryDelay); // Espera antes da pr�xima tentativa
        }
        else
        {
            logger.LogCritical("N�o foi poss�vel conectar e inicializar o banco de dados ap�s m�ltiplas tentativas. A aplica��o ser� encerrada.");
            throw; // Lan�a a exce��o se todas as tentativas falharem
        }
    }
}

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();