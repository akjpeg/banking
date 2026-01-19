using AccountTransactions.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts;

namespace AccountTransactions.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAccountTransactionsApplication(this IServiceCollection services)
    {
        services.AddScoped<ITransactionRecorder, TransactionRecorder>();

        return services;
    }
}