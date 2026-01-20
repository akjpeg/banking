using AccountTransactions.Domain.Enums;
using Shared.Exceptions;

namespace AccountTransactions.Domain.Entities;

public class AccountTransaction
{
    public Guid Id { get; private set; }
    public Guid? FromAccountId { get; private set; }
    public Guid? ToAccountId { get; private set; }
    public Guid AccountId { get; private set; }
    public decimal Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public AccountTransactionStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    protected AccountTransaction() { }

    // For deposits
    public static AccountTransaction CreateDeposit(Guid accountId, decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("Amount must be greater than zero");

        return new AccountTransaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            ToAccountId = accountId,
            FromAccountId = null,
            Amount = amount,
            Type = TransactionType.Deposit,
            Status = AccountTransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public static AccountTransaction CreateWithdrawal(Guid accountId, decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("Amount must be greater than zero");

        return new AccountTransaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            FromAccountId = accountId,
            ToAccountId = null,
            Amount = amount,
            Type = TransactionType.Withdrawal,
            Status = AccountTransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    public static AccountTransaction CreateTransferOut(Guid fromAccountId, Guid toAccountId, decimal amount)
    {
        if (fromAccountId == toAccountId)
            throw new DomainException("Cannot transfer to same account");

        if (amount <= 0)
            throw new DomainException("Amount must be greater than zero");

        return new AccountTransaction
        {
            Id = Guid.NewGuid(),
            AccountId = fromAccountId,
            FromAccountId = fromAccountId,
            ToAccountId = toAccountId,
            Amount = amount,
            Type = TransactionType.Transfer,
            Status = AccountTransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static AccountTransaction CreateTransferIn(Guid fromAccountId, Guid toAccountId, decimal amount)
    {
        if (fromAccountId == toAccountId)
            throw new DomainException("Cannot transfer to same account");

        if (amount <= 0)
            throw new DomainException("Amount must be greater than zero");

        return new AccountTransaction
        {
            Id = Guid.NewGuid(),
            AccountId = toAccountId,
            FromAccountId = fromAccountId,
            ToAccountId = toAccountId,
            Amount = amount,
            Type = TransactionType.Transfer,
            Status = AccountTransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkCompleted()
    {
        Status = AccountTransactionStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        Status = AccountTransactionStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }
}