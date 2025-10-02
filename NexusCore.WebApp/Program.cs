using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using NexusCore.Domain.Entities;
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
// Adiciona uma política de retentativa manual na inicialização
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var maxRetries = 5;
for (int i = 0; i < maxRetries; i++)
{
    try
    {
        logger.LogInformation("Tentando popular os dados iniciais (Tentativa {Attempt}/{MaxAttempts})...", i + 1, maxRetries);
        SeedData.EnsureSeedData(app.Services);
        logger.LogInformation("Dados iniciais populados com sucesso.");
        break; // Sai do loop se for bem-sucedido
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro ao popular dados iniciais. Tentando novamente em 5 segundos...");
        if (i < maxRetries - 1)
        {
            await Task.Delay(5000); // Espera 5 segundos antes da próxima tentativa
        }
        else
        {
            logger.LogCritical("Não foi possível conectar ao banco de dados após múltiplas tentativas. A aplicação será encerrada.");
            throw; // Lança a exceção se todas as tentativas falharem
        }
    }
}

app.Run();
