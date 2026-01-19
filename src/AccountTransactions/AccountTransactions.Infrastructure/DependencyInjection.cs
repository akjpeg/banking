using AccountTransactions.Application;
using AccountTransactions.Application.Interfaces;
using AccountTransactions.Infrastructure.Persistence;
using AccountTransactions.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AccountTransactions.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAccountTransactionsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAccountTransactionsApplication();
        
        var connectionString = BuildConnectionString(configuration, "banking_transactions");
        services.AddDbContext<AccountTransactionDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IAccountTransactionRepository, AccountTransactionRepository>();

        return services;
    }
    
    private static string BuildConnectionString(IConfiguration configuration, string database)
    {
        var host = configuration["Database:Host"] ?? "localhost";
        var username = configuration["Database:Username"] ?? "postgres";
        var password = configuration["Database:Password"] ?? "postgres";

        return $"Host={host};Database={database};Username={username};Password={password}";
    }
}