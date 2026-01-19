using Shared.Contracts;

namespace Transfers.Application.Interfaces;

public interface ITransferMoneyHandler
{
    Task<TransferResult> HandleAsync(Guid fromAccountId, string toAccountNumber, decimal amount);
}