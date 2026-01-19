using AccountTransactions.Application.Interfaces;
using AccountTransactions.Domain.Entities;
using Shared.Contracts;
using Shared.Exceptions;

namespace AccountTransactions.Application.Services;

public class TransactionRecorder : ITransactionRecorder
{
    private readonly IAccountTransactionRepository _repository;

    public TransactionRecorder(IAccountTransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task RecordDepositAsync(Guid accountId, decimal amount)
    {
        var transaction = AccountTransaction.CreateDeposit(accountId, amount);
        await _repository.AddAsync(transaction);
        await _repository.SaveChangesAsync();
    }

    public async Task RecordWithdrawalAsync(Guid accountId, decimal amount)
    {
        var transaction = AccountTransaction.CreateWithdrawal(accountId, amount);
        await _repository.AddAsync(transaction);
        await _repository.SaveChangesAsync();
    }

    public async Task<TransferResult> RecordTransferAsync(Guid fromAccountId, Guid toAccountId, decimal amount)
    {
        var outTransaction = AccountTransaction.CreateTransferOut(fromAccountId, toAccountId, amount);
        var inTransaction = AccountTransaction.CreateTransferIn(fromAccountId, toAccountId, amount);

        await _repository.AddAsync(outTransaction);
        await _repository.AddAsync(inTransaction);
        await _repository.SaveChangesAsync();

        return new TransferResult(outTransaction.Id, inTransaction.Id);
    }

    public async Task MarkTransferCompletedAsync(Guid fromTransactionId, Guid toTransactionId)
    {
        var fromTransaction = await _repository.GetByIdAsync(fromTransactionId)
            ?? throw new DomainException($"Transaction {fromTransactionId} not found");

        var toTransaction = await _repository.GetByIdAsync(toTransactionId)
            ?? throw new DomainException($"Transaction {toTransactionId} not found");

        fromTransaction.MarkCompleted();
        toTransaction.MarkCompleted();

        await _repository.UpdateAsync(fromTransaction);
        await _repository.UpdateAsync(toTransaction);
        await _repository.SaveChangesAsync();
    }

    public async Task MarkTransferFailedAsync(Guid fromTransactionId, Guid toTransactionId)
    {
        var fromTransaction = await _repository.GetByIdAsync(fromTransactionId)
            ?? throw new DomainException($"Transaction {fromTransactionId} not found");

        var toTransaction = await _repository.GetByIdAsync(toTransactionId)
            ?? throw new DomainException($"Transaction {toTransactionId} not found");

        fromTransaction.MarkFailed();
        toTransaction.MarkFailed();

        await _repository.UpdateAsync(fromTransaction);
        await _repository.UpdateAsync(toTransaction);
        await _repository.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<TransactionDto>> GetByAccountIdAsync(Guid accountId)
    {
        var transactions = await _repository.GetByAccountIdAsync(accountId);

        return transactions.Select(t => new TransactionDto(
            t.Id,
            t.FromAccountId,
            t.ToAccountId,
            t.Amount,
            t.Type.ToString(),
            t.Status.ToString(),
            t.CreatedAt
        )).ToList();
    }
}