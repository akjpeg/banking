using Accounts.Application.Interfaces;
using Accounts.Domain.Entities;
using Shared.Contracts;
using Shared.Exceptions;

namespace Accounts.Application.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _repository;
    private readonly ITransactionRecorder _transactionRecorder;

    public AccountService(IAccountRepository repository, ITransactionRecorder transactionRecorder)
    {
        _repository = repository;
        _transactionRecorder = transactionRecorder;
    }

    public async Task<AccountDto?> GetByIdAsync(Guid id)
    {
        var account = await _repository.GetByIdAsync(id);
        
        return account == null
            ? null
            : new AccountDto(account.Id, account.AccountNumber, account.Name, account.Balance);
    }
    
    public async Task<AccountDto?> GetByEmailAsync(string email)
    {
        var account = await _repository.GetByEmailAsync(email);
        return account == null ? null : MapToDto(account);
    }

    public async Task<AccountDto?> GetByAccountNumberAsync(string accountNumber)
    {
        var account = await _repository.GetByAccountNumberAsync(accountNumber);
        return account == null ? null : MapToDto(account);
    }

    public async Task<IReadOnlyList<AccountDto>> GetAllAsync()
    {
        var accounts = await _repository.GetAllAsync();
        return accounts.Select(a => MapToDto(a)).ToList();
    }
    
    public async Task<AccountDto> CreateAsync(string name, string email, string password)
    {
        if (await _repository.ExistsByEmailAsync(email))
            throw new DomainException("Email already registered");

        var passwordHash = HashPassword(password);
        var account = new Account(name, email, passwordHash);

        await _repository.AddAsync(account);
        await _repository.SaveChangesAsync();

        return MapToDto(account);
    }
    
    public async Task<AccountDto?> LoginAsync(string email, string password)
    {
        var account = await _repository.GetByEmailAsync(email);

        if (account == null)
            return null;

        if (!VerifyPassword(password, account.PasswordHash))
            return null;

        return MapToDto(account);
    }
    
    public async Task<bool> ExistsAsync(Guid id)
    {
        var account = await _repository.GetByIdAsync(id);
        return account != null;
    }

    public async Task<bool> HasSufficientBalanceAsync(Guid accountId, decimal amount)
    {
        var account = await _repository.GetByIdAsync(accountId);
        return account != null && account.Balance >= amount;
    }

    public async Task<IReadOnlyList<TransactionDto>> GetAccountTransactionsAsync(Guid accountId)
    {
        var account = await GetByIdAsync(accountId);
        
        if (account == null)
            throw new KeyNotFoundException($"Account {accountId} not found");
        
        var transactions = await _transactionRecorder.GetByAccountIdAsync(accountId);

        return transactions;
    }
    
    public async Task<AccountDto> DebitAsync(Guid accountId, decimal amount)
    {
        var account = await _repository.GetByIdAsync(accountId)
                      ?? throw new DomainException($"Account {accountId} not found");

        account.Debit(amount);
        await _repository.UpdateAsync(account);
        var transactionId = await _transactionRecorder.RecordWithdrawalAsync(accountId, amount);
        await _transactionRecorder.MarkAccountOperationCompletedAsync(transactionId);
        
        await _repository.SaveChangesAsync();

        return MapToDto(account);
    }

    public async Task<AccountDto> CreditAsync(Guid accountId, decimal amount)
    {
        var account = await _repository.GetByIdAsync(accountId)
                      ?? throw new DomainException($"Account {accountId} not found");

        account.Credit(amount);
        await _repository.UpdateAsync(account);
        var transactionId = await _transactionRecorder.RecordDepositAsync(accountId, amount);
        await _transactionRecorder.MarkAccountOperationCompletedAsync(transactionId);
        await _repository.SaveChangesAsync();
        
        return MapToDto(account);
    }

    public async Task DeleteAsync(Guid accountId)
    {
        var account = await _repository.GetByIdAsync(accountId)
                      ?? throw new DomainException($"Account {accountId} not found");
        
        await _repository.DeleteAsync(account);
        await _repository.SaveChangesAsync();
    }

    private static AccountDto MapToDto(Account account)
    {
        return new AccountDto(account.Id, account.AccountNumber, account.Name, account.Balance);
    }
    
    private static string HashPassword(string password)
    {
        return Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(password)));
    }

    private static bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }
}