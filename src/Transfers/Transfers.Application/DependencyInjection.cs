using Microsoft.Extensions.DependencyInjection;
using Transfers.Application.Handlers;
using Transfers.Application.Interfaces;

namespace Transfers.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddTransfersModule(this IServiceCollection services)
    {
        services.AddScoped<ITransferMoneyHandler, TransferMoneyHandler>();

        return services;
    }
}