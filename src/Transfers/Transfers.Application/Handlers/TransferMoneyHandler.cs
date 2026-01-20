using Shared.Contracts;
using Shared.Exceptions;
using Transfers.Application.Interfaces;

namespace Transfers.Application.Handlers;

public class TransferMoneyHandler : ITransferMoneyHandler
{
    private readonly IAccountService _accountService;
    private readonly ITransactionRecorder _transactionRecorder;

    public TransferMoneyHandler(
        IAccountService accountService,
        ITransactionRecorder transactionRecorder)
    {
        _accountService = accountService;
        _transactionRecorder = transactionRecorder;
    }

    public async Task<TransferResult> HandleAsync(Guid fromAccountId, string toAccountNumber, decimal amount)
    {
        var fromAccount = await _accountService.GetByIdAsync(fromAccountId)
                          ?? throw new DomainException($"Origin account {fromAccountId} not found");

        var toAccount = await _accountService.GetByAccountNumberAsync(toAccountNumber)
                        ?? throw new DomainException($"Destination account {toAccountNumber} not found");
        
        if (!await _accountService.HasSufficientBalanceAsync(fromAccountId, amount))
            throw new DomainException("Insufficient balance");
        
        var transferResult = await _transactionRecorder.RecordTransferAsync(
            fromAccountId, toAccount.Id, amount);

        try
        {
            await _accountService.DebitForTransferAsync(fromAccountId, amount);
            await _accountService.CreditForTransferAsync(toAccount.Id, amount);
            
            await _transactionRecorder.MarkTransferCompletedAsync(
                transferResult.FromTransactionId,
                transferResult.ToTransactionId);

            return transferResult;
        }
        catch
        {
            await _transactionRecorder.MarkTransferFailedAsync(
                transferResult.FromTransactionId,
                transferResult.ToTransactionId);
            throw;
        }
    }
}