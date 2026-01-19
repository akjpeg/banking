using Accounts.Domain.Entities;
using Shared.Contracts;

namespace Accounts.Application.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid accountId); 
	Task<Account?> GetByEmailAsync(string email);
	Task<Account?> GetByAccountNumberAsync(string accountNumber);
	Task<IReadOnlyList<Account>> GetAllAsync();
	Task<bool> ExistsByEmailAsync(string email);
	Task AddAsync(Account account);
    Task UpdateAsync(Account account);
    Task DeleteAsync(Account account);
    Task SaveChangesAsync();
}