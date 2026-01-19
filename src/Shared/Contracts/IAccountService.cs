namespace Shared.Contracts;

public interface IAccountService
{
    Task<AccountDto?> GetByIdAsync(Guid id);
    Task<AccountDto?> GetByEmailAsync(string email);
    Task<AccountDto?> GetByAccountNumberAsync(string accountNumber);
    Task<IReadOnlyList<AccountDto>> GetAllAsync();
    Task<AccountDto> CreateAsync(string name, string email, string password);
    Task<AccountDto?> LoginAsync(string email, string password);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> HasSufficientBalanceAsync(Guid accountId, decimal amount);
    Task DebitAsync(Guid accountId, decimal amount);
    Task CreditAsync(Guid accountId, decimal amount);
    Task DeleteAsync(Guid accountId);
}

public record AccountDto(Guid Id, string AccountNumber, string Name, decimal Balance);