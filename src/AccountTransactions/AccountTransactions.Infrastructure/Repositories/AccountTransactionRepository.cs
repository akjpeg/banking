using AccountTransactions.Application.Interfaces;
using AccountTransactions.Domain.Entities;
using AccountTransactions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccountTransactions.Infrastructure.Repositories;

public class AccountTransactionRepository : IAccountTransactionRepository
{
    private readonly AccountTransactionDbContext _context;

    public AccountTransactionRepository(AccountTransactionDbContext context)
    {
        _context = context;
    }

    public async Task<AccountTransaction?> GetByIdAsync(Guid id)
    {
        return await _context.Transactions.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IReadOnlyList<AccountTransaction>> GetByAccountIdAsync(Guid accountId)
    {
        return await _context.Transactions
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(AccountTransaction transaction)
    {
        await _context.Transactions.AddAsync(transaction);
    }

    public Task UpdateAsync(AccountTransaction transaction)
    {
        _context.Transactions.Update(transaction);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}