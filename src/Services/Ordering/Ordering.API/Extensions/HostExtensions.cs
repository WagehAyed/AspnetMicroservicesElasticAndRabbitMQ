using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Serilog;
using System;

namespace Ordering.API.Extensions
{
    public static class HostExtensions
    {
        public static IHost MigrateDatabase<TContext>(this IHost host,
                                            Action<TContext, IServiceProvider> seeder ) where TContext : DbContext
        {
             

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<TContext>>();
                var context = services.GetService<TContext>();

                try
                {
                    logger.LogInformation("Migrating database associated with context {DbContextName}", typeof(TContext).Name);

                    RetryMaigrationDB(seeder, services, context);
                    logger.LogInformation("Migrated database associated with context {DbContextName}", typeof(TContext).Name);
                }
                catch (SqlException ex)
                {
                    logger.LogError(ex, "An error occurred while migrating the database used on context {DbContextName}", typeof(TContext).Name);

                }
            }
            return host;
        }

        private static void RetryMaigrationDB<TContext>(Action<TContext, IServiceProvider> seeder, IServiceProvider services, TContext context) where TContext : DbContext
        {
            var retry = Policy.Handle<SqlException>()
                .WaitAndRetry(
                 retryCount: 5,
                 sleepDurationProvider: rertyCount =>
                 TimeSpan.FromSeconds(Math.Pow(2, rertyCount)),
                 onRetry: (exception, retryCount, context) =>
                 {
                     Log.Error($"Retry {retryCount} of {context.PolicyKey} at {context.OperationKey}, dueto: {exception}");

                 });
            retry.Execute(() => InvokeSeeder(seeder, context, services));
        }

        private static void InvokeSeeder<TContext>(Action<TContext, IServiceProvider> seeder,
                                                    TContext context,
                                                    IServiceProvider services)
                                                    where TContext : DbContext
        {
            context.Database.Migrate();
            seeder(context, services);
        }
    }
}
