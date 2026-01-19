using Shared.Exceptions;

namespace Accounts.Domain.Entities;

public class Account
{
    public Guid Id { get; private set; }
    public string AccountNumber { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public decimal Balance { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public uint Version { get; private set; }

    protected Account() {}

    public Account(string name, string email, string passwordHash)
    {
        Id = Guid.NewGuid();
        AccountNumber = GenerateAccountNumber();
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        CreatedAt = DateTime.UtcNow;
        Balance = 0;
        Version = 1;
    }

    public void Credit(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("Credit amount must be greater than zero.");

        Balance += amount;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    public void Debit(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("Debit amount must be greater than zero.");
        
        if (Balance < amount)
            throw new DomainException("Insufficient funds for debit operation");
        
        Balance -= amount;
        UpdatedAt = DateTime.UtcNow;
        Version++;
    }

    private static string GenerateAccountNumber() => Random.Shared.Next(100000, 999999).ToString();
}