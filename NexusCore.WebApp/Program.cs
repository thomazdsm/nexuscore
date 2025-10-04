using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using NexusCore.Domain.Entities;
using NexusCore.Infra.Data.Context;
using NexusCore.Infra.Data.Seed;
using NexusCore.Infra.IoC;
using NexusCore.WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Registra todos os serviços da nossa camada de infraestrutura (DB, Identity, OIDC).
builder.Services.AddInfrastructure(builder.Configuration);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Substitui a fábrica de claims padrão pela nossa versão customizada.
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, CustomClaimsPrincipalFactory>();


// Configura o middleware para confiar nos cabeçalhos do proxy reverso (Caddy).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Aplica migrações e faz o seed dos dados com política de retentativa.
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var maxRetries = 5;
var retryDelay = TimeSpan.FromSeconds(5);

for (int i = 0; i < maxRetries; i++)
{
    try
    {
        logger.LogInformation("Tentando inicializar o banco de dados (Tentativa {Attempt}/{MaxAttempts})...", i + 1, maxRetries);

        // Chama nosso novo método que aplica migrações e faz o seed
        await DatabaseInitializer.InitializeDatabaseAsync(app.Services);

        logger.LogInformation("Banco de dados inicializado com sucesso.");
        break; // Sai do loop se for bem-sucedido
    }
    catch (Exception ex)
    {
        // O Npgsql.NpgsqlException é comum aqui se o contêiner do DB ainda não estiver pronto.
        logger.LogError(ex, "Erro ao inicializar o banco de dados. Tentando novamente em {RetryDelay} segundos...", retryDelay.Seconds);
        if (i < maxRetries - 1)
        {
            await Task.Delay(retryDelay); // Espera antes da próxima tentativa
        }
        else
        {
            logger.LogCritical("Não foi possível conectar e inicializar o banco de dados após múltiplas tentativas. A aplicação será encerrada.");
            throw; // Lança a exceção se todas as tentativas falharem
        }
    }
}

// Usa o middleware de Forwarded Headers no início do pipeline.
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Habilita a autenticação e autorização, na ordem correta.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Executa o método de seed para popular o banco de dados.
//await SeedData.EnsureSeedData(app.Services);


app.Run();
