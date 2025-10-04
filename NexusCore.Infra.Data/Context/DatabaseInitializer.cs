using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusCore.Infra.Data.Seed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusCore.Infra.Data.Context
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeDatabaseAsync(IServiceProvider services)
        {
            // Cria um escopo para resolver os serviços
            using (var scope = services.CreateScope())
            {
                var scopedProvider = scope.ServiceProvider;
                try
                {
                    var dbContext = scopedProvider.GetRequiredService<AppDbContext>();

                    // Aplica quaisquer migrações pendentes. O banco de dados será criado se não existir.
                    await dbContext.Database.MigrateAsync();

                    // Agora, executa o seu método de Seed
                    await SeedData.EnsureSeedData(scopedProvider);
                }
                catch (Exception ex)
                {
                    throw; // Lança a exceção para que a política de retentativa no Program.cs possa capturá-la
                }
            }
        }
    }
}
