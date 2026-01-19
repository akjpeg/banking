using Accounts.Domain.Entities;
using Accounts.Application.Interfaces;
using Accounts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Accounts.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly AccountDbContext _context;

    public AccountRepository(AccountDbContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetByIdAsync(Guid id)  // ← fixed
    {
        return await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Account?> GetByEmailAsync(string email)
    {
        return await _context.Accounts
            .FirstOrDefaultAsync(a => a.Email == email);
    }

    public async Task<Account?> GetByAccountNumberAsync(string accountNumber)
    {
        return await _context.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
    }

    public async Task<IReadOnlyList<Account>> GetAllAsync()
    {
        return await _context.Accounts
            .ToListAsync();
    }
    
    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.Accounts.AnyAsync(a => a.Email == email);
    }

    public async Task AddAsync(Account account)
    {
        await _context.Accounts.AddAsync(account);
    }

    public Task UpdateAsync(Account account)
    {
        _context.Accounts.Update(account);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Account account)
    {
        _context.Accounts.Remove(account);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}