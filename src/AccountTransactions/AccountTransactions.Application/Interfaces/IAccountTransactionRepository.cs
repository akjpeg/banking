using AccountTransactions.Domain.Entities;

namespace AccountTransactions.Application.Interfaces;

public interface IAccountTransactionRepository
{
    Task<AccountTransaction?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<AccountTransaction>> GetByAccountIdAsync(Guid accountId);
    Task AddAsync(AccountTransaction transaction);
    Task UpdateAsync(AccountTransaction transaction);
    Task SaveChangesAsync();
}