namespace Shared.Contracts;

public interface ITransactionRecorder
{
    Task RecordDepositAsync(Guid accountId, decimal amount);
    Task RecordWithdrawalAsync(Guid accountId, decimal amount);
    Task<TransferResult> RecordTransferAsync(Guid fromAccountId, Guid toAccountId, decimal amount);
    Task MarkTransferCompletedAsync(Guid fromTransactionId, Guid toTransactionId);
    Task MarkTransferFailedAsync(Guid fromTransactionId, Guid toTransactionId);
    Task<IReadOnlyList<TransactionDto>> GetByAccountIdAsync(Guid accountId);
}

public record TransferResult(Guid FromTransactionId, Guid ToTransactionId);

public record TransactionDto(
    Guid Id,
    Guid? FromAccountId,
    Guid? ToAccountId,
    decimal Amount,
    string Type,
    string Status,
    DateTime CreatedAt);