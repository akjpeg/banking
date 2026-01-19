using Accounts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Accounts.Infrastructure.Persistence;

public class AccountEntityConfig : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");
        
        builder.HasKey(a => a.Id);
        
        builder.Property(a => a.AccountNumber)
            .IsRequired();
        builder.HasIndex(a => a.AccountNumber);
        
        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(a => a.Email)
            .IsUnique();

        builder.Property(a => a.Balance)
            .HasPrecision(18, 2)
            .IsRequired();
        
        builder.Property(a => a.PasswordHash)
            .IsRequired();

        builder.Property(a => a.Version)
            .IsConcurrencyToken();
    }
}