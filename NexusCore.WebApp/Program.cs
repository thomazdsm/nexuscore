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
await SeedData.EnsureSeedData(app.Services);

app.Run();
