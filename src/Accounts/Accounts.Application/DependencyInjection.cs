using Accounts.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts;

namespace Accounts.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAccountsApplication(this IServiceCollection services)
    {
        services.AddScoped<IAccountService, AccountService>();
        return services;
    }
}