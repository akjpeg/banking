using Accounts.Application;
using Accounts.Application.Interfaces;
using Accounts.Infrastructure.Persistence;
using Accounts.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Accounts.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAccountsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAccountsApplication();
        
        var connectionString = BuildConnectionString(configuration, "banking_accounts");
        services.AddDbContext<AccountDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IAccountRepository, AccountRepository>();

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