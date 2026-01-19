using Accounts.Domain.Entities;
using FluentAssertions;
using Shared.Exceptions;

namespace Accounts.Domain.Tests.Entities;

public class AccountTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesAccount()
    {
        // Arrange
        var name = "John Doe";
        var email = "john@example.com";
        var passwordHash = "hashedPassword123";

        // Act
        var account = new Account(name, email, passwordHash);

        // Assert
        account.Id.Should().NotBeEmpty();
        account.Name.Should().Be(name);
        account.Email.Should().Be(email);
        account.PasswordHash.Should().Be(passwordHash);
    }

    [Fact]
    public void Constructor_CreatesAccountWithZeroBalance()
    {
        // Act
        var account = new Account("John Doe", "john@example.com", "hash");

        // Assert
        account.Balance.Should().Be(0);
    }

    [Fact]
    public void Constructor_GeneratesSixDigitAccountNumber()
    {
        // Act
        var account = new Account("John Doe", "john@example.com", "hash");

        // Assert
        account.AccountNumber.Should().HaveLength(6);
        account.AccountNumber.Should().MatchRegex(@"^\d{6}$");
    }

    [Fact]
    public void Constructor_SetsCreatedAtToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var account = new Account("John Doe", "john@example.com", "hash");

        // Assert
        var after = DateTime.UtcNow;
        account.CreatedAt.Should().BeOnOrAfter(before);
        account.CreatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Constructor_UpdatedAtIsNull()
    {
        // Act
        var account = new Account("John Doe", "john@example.com", "hash");

        // Assert
        account.UpdatedAt.Should().BeNull();
    }

    #endregion

    #region Credit Tests

    [Fact]
    public void Credit_WithPositiveAmount_IncreasesBalance()
    {
        // Arrange
        var account = new Account("John Doe", "john@example.com", "hash");

        // Act
        account.Credit(100m);

        // Assert
        account.Balance.Should().Be(100m);
    }

    [Fact]
    public void Credit_MultipleTimes_AccumulatesBalance()
    {
        // Arrange
        var account = new Account("John Doe", "john@example.com", "hash");

        // Act
        account.Credit(100m);
        account.Credit(50m);
        account.Credit(25.50m);

        // Assert
        account.Balance.Should().Be(175.50m);
    }

    [Fact]
    public void Credit_WithPositiveAmount_SetsUpdatedAt()
    {
        // Arrange
        var account = new Account("John Doe", "john@example.com", "hash");
        var before = DateTime.UtcNow;

        // Act
        account.Credit(100m);

        // Assert
        var after = DateTime.UtcNow;
        account.UpdatedAt.Should().NotBeNull();
        account.UpdatedAt.Should().BeOnOrAfter(before);
        account.UpdatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Credit_WithZeroAmount_ThrowsDomainException()
    {
        // Arrange
        var account = new Account("John Doe", "john@example.com", "hash");

        // Act
        var act = () => account.Credit(0);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*greater than zero*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(-0.01)]
    public void Credit_WithNegativeAmount_ThrowsDomainException(decimal amount)
    {
        // Arrange
        var account = new Account("John Doe", "john@example.com", "hash");

        // Act
        var act = () => account.Credit(amount);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*greater than zero*");
    }

    [Fact]
    public void Credit_WithNegativeAmount_DoesNotChangeBalance()
    {
        // Arrange
        var account = new Account("John Doe", "john@example.com", "hash");
        account.Credit(100m);

        // Act
        var act = () => account.Credit(-50m);

        // Assert
        act.Should().Throw<DomainException>();
        account.Balance.Should().Be(100m);
    }

    #endregion

    #region Debit Tests

    [Fact]
    public void Debit_WithSufficientFunds_DecreasesBalance()
    {
        // Arrange
        var account = new Account("John Doe", "john@example.com", "hash");
        account.Credit(100m);

        // Act
        account.Debit(30m);

        // Assert
        account.Balance.Should().Be(70m);
    }

    [Fact]
    public void Debit_EntireBalance_SetsBalanceToZero()
    {
        // Arrange
        var account = new Account("John Doe", "john@example.com", "hash");
        account.Credit(100m);

        // Act
        account.Debit(100m);

        // Assert
        account.Balance.Should().Be(0);
    }

    [Fact]
    public void Debit_WithSufficientFunds_SetsUpdatedAt()
    {
        // Arrange
        var account = new Account("John Doe", "john@example.com", "hash");
        account.Credit(100m);
        var before = DateTime.UtcNow;

        // Act
        account.Debit(30m);

        // Assert
        var after = DateTime.UtcNow;
        account.UpdatedAt.Should().BeOnOrAfter(before);
        account.UpdatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Debit_WithInsufficientFunds_ThrowsDomainException()
    {
        // Arrange
        var account = new Account("John Doe", "john@example.com", "hash");
        account.Credit(50m);

        // Act
        var act = () => account.Debit(100m);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Insufficient funds*");
    }

    [Fact]
    public void Debit_WithInsufficientFunds_DoesNotChangeBalance()
    {
        // Arrange
        var account = new Account("John Doe", "john@example.com", "hash");
        account.Credit(50m);

        // Act
        var act = () => account.Debit(100m);

        // Assert
        act.Should().Throw<DomainException>();
        account.Balance.Should().Be(50m);
    }

    [Fact]
    public void Debit_WithZeroBalance_ThrowsDomainException()
    {
        // Arrange
        var account = new Account("John Doe", "john@example.com", "hash");

        // Act
        var act = () => account.Debit(10m);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Insufficient funds*");
    }

    [Fact]
    public void Debit_WithZeroAmount_ThrowsDomainException()
    {
        // Arrange
        var account = new Account("John Doe", "john@example.com", "hash");
        account.Credit(100m);

        // Act
        var act = () => account.Debit(0);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*greater than zero*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(-0.01)]
    public void Debit_WithNegativeAmount_ThrowsDomainException(decimal amount)
    {
        // Arrange
        var account = new Account("John Doe", "john@example.com", "hash");
        account.Credit(100m);

        // Act
        var act = () => account.Debit(amount);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*greater than zero*");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Credit_WithDecimalPrecision_MaintainsPrecision()
    {
        // Arrange
        var account = new Account("John Doe", "john@example.com", "hash");

        // Act
        account.Credit(100.99m);
        account.Credit(0.01m);

        // Assert
        account.Balance.Should().Be(101.00m);
    }

    [Fact]
    public void Debit_WithDecimalPrecision_MaintainsPrecision()
    {
        // Arrange
        var account = new Account("John Doe", "john@example.com", "hash");
        account.Credit(100.00m);

        // Act
        account.Debit(33.33m);

        // Assert
        account.Balance.Should().Be(66.67m);
    }

    [Fact]
    public void MultipleOperations_MaintainsCorrectBalance()
    {
        // Arrange
        var account = new Account("John Doe", "john@example.com", "hash");

        // Act
        account.Credit(1000m);
        account.Debit(250m);
        account.Credit(100m);
        account.Debit(50m);

        // Assert
        account.Balance.Should().Be(800m);
    }

    #endregion
}