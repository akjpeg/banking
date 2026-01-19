using AccountTransactions.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccountTransactions.Infrastructure.Persistence;

public class AccountTransactionDbContext : DbContext
{
    public AccountTransactionDbContext(DbContextOptions<AccountTransactionDbContext> options)
        : base(options)
    {
    }

    public DbSet<AccountTransaction> Transactions => Set<AccountTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountTransaction>(builder =>
        {
            builder.ToTable("account_transactions");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Status).HasConversion<string>();
            builder.Property(t => t.Type).HasConversion<string>();
            builder.Property(t => t.Amount).HasPrecision(18, 2);
            builder.HasIndex(t => t.AccountId);
        });
    }
}