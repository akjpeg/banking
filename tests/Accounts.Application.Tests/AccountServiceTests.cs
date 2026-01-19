using Accounts.Application.Interfaces;
using Accounts.Application.Services;
using Accounts.Domain.Entities;
using FluentAssertions;
using Moq;
using Shared.Exceptions;

namespace Accounts.Application.Tests.Services;

public class AccountServiceTests
{
    private readonly Mock<IAccountRepository> _repositoryMock;
    private readonly AccountService _sut;

    public AccountServiceTests()
    {
        _repositoryMock = new Mock<IAccountRepository>();
        _sut = new AccountService(_repositoryMock.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingAccount_ReturnsAccountDto()
    {
        // Arrange
        var account = CreateTestAccount();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(account.Id))
            .ReturnsAsync(account);

        // Act
        var result = await _sut.GetByIdAsync(account.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(account.Id);
        result.Name.Should().Be(account.Name);
        result.AccountNumber.Should().Be(account.AccountNumber);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingAccount_ReturnsNull()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync((Account?)null);

        // Act
        var result = await _sut.GetByIdAsync(accountId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByEmailAsync Tests

    [Fact]
    public async Task GetByEmailAsync_WithExistingEmail_ReturnsAccountDto()
    {
        // Arrange
        var account = CreateTestAccount();
        _repositoryMock
            .Setup(r => r.GetByEmailAsync(account.Email))
            .ReturnsAsync(account);

        // Act
        var result = await _sut.GetByEmailAsync(account.Email);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(account.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_WithNonExistingEmail_ReturnsNull()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByEmailAsync("nonexistent@test.com"))
            .ReturnsAsync((Account?)null);

        // Act
        var result = await _sut.GetByEmailAsync("nonexistent@test.com");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByAccountNumberAsync Tests

    [Fact]
    public async Task GetByAccountNumberAsync_WithExistingNumber_ReturnsAccountDto()
    {
        // Arrange
        var account = CreateTestAccount();
        _repositoryMock
            .Setup(r => r.GetByAccountNumberAsync(account.AccountNumber))
            .ReturnsAsync(account);

        // Act
        var result = await _sut.GetByAccountNumberAsync(account.AccountNumber);

        // Assert
        result.Should().NotBeNull();
        result.AccountNumber.Should().Be(account.AccountNumber);
    }

    [Fact]
    public async Task GetByAccountNumberAsync_WithNonExistingNumber_ReturnsNull()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByAccountNumberAsync("999999"))
            .ReturnsAsync((Account?)null);

        // Act
        var result = await _sut.GetByAccountNumberAsync("999999");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WithAccounts_ReturnsAllAccountDtos()
    {
        // Arrange
        var accounts = new List<Account>
        {
            CreateTestAccount("John", "john@test.com"),
            CreateTestAccount("Jane", "jane@test.com"),
            CreateTestAccount("Bob", "bob@test.com")
        };
        
        _repositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(accounts);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Select(a => a.Name).Should().Contain(new[] { "John", "Jane", "Bob" });
    }

    [Fact]
    public async Task GetAllAsync_WithNoAccounts_ReturnsEmptyList()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Account>());

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesAccount()
    {
        // Arrange
        var name = "John Doe";
        var email = "john@test.com";
        var password = "password123";

        _repositoryMock
            .Setup(r => r.ExistsByEmailAsync(email))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.CreateAsync(name, email, password);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(name);
        result.Balance.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_CallsRepositoryAdd()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.ExistsByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        await _sut.CreateAsync("John", "john@test.com", "password");

        // Assert
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithExistingEmail_ThrowsDomainException()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.ExistsByEmailAsync("existing@test.com"))
            .ReturnsAsync(true);

        // Act
        var act = () => _sut.CreateAsync("John", "existing@test.com", "password");

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Email already registered*");
    }

    [Fact]
    public async Task CreateAsync_WithExistingEmail_DoesNotAddAccount()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.ExistsByEmailAsync("existing@test.com"))
            .ReturnsAsync(true);

        // Act
        var act = () => _sut.CreateAsync("John", "existing@test.com", "password");

        // Assert
        await act.Should().ThrowAsync<DomainException>();
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Never);
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsAccountDto()
    {
        // Arrange
        var password = "password123";
        var account = CreateTestAccountWithPassword(password);
        
        _repositoryMock
            .Setup(r => r.GetByEmailAsync(account.Email))
            .ReturnsAsync(account);

        // Act
        var result = await _sut.LoginAsync(account.Email, password);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(account.Id);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ReturnsNull()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByEmailAsync("nonexistent@test.com"))
            .ReturnsAsync((Account?)null);

        // Act
        var result = await _sut.LoginAsync("nonexistent@test.com", "password");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ReturnsNull()
    {
        // Arrange
        var account = CreateTestAccountWithPassword("correctPassword");
        
        _repositoryMock
            .Setup(r => r.GetByEmailAsync(account.Email))
            .ReturnsAsync(account);

        // Act
        var result = await _sut.LoginAsync(account.Email, "wrongPassword");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithExistingAccount_ReturnsTrue()
    {
        // Arrange
        var account = CreateTestAccount();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(account.Id))
            .ReturnsAsync(account);

        // Act
        var result = await _sut.ExistsAsync(account.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingAccount_ReturnsFalse()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync((Account?)null);

        // Act
        var result = await _sut.ExistsAsync(accountId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region HasSufficientBalanceAsync Tests

    [Fact]
    public async Task HasSufficientBalanceAsync_WithSufficientFunds_ReturnsTrue()
    {
        // Arrange
        var account = CreateTestAccount();
        account.Credit(100m);
        
        _repositoryMock
            .Setup(r => r.GetByIdAsync(account.Id))
            .ReturnsAsync(account);

        // Act
        var result = await _sut.HasSufficientBalanceAsync(account.Id, 50m);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasSufficientBalanceAsync_WithExactBalance_ReturnsTrue()
    {
        // Arrange
        var account = CreateTestAccount();
        account.Credit(100m);
        
        _repositoryMock
            .Setup(r => r.GetByIdAsync(account.Id))
            .ReturnsAsync(account);

        // Act
        var result = await _sut.HasSufficientBalanceAsync(account.Id, 100m);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasSufficientBalanceAsync_WithInsufficientFunds_ReturnsFalse()
    {
        // Arrange
        var account = CreateTestAccount();
        account.Credit(50m);
        
        _repositoryMock
            .Setup(r => r.GetByIdAsync(account.Id))
            .ReturnsAsync(account);

        // Act
        var result = await _sut.HasSufficientBalanceAsync(account.Id, 100m);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasSufficientBalanceAsync_WithNonExistingAccount_ReturnsFalse()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync((Account?)null);

        // Act
        var result = await _sut.HasSufficientBalanceAsync(accountId, 50m);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region DebitAsync Tests

    [Fact]
    public async Task DebitAsync_WithExistingAccount_DebitsAndSaves()
    {
        // Arrange
        var account = CreateTestAccount();
        account.Credit(100m);
        
        _repositoryMock
            .Setup(r => r.GetByIdAsync(account.Id))
            .ReturnsAsync(account);

        // Act
        await _sut.DebitAsync(account.Id, 30m);

        // Assert
        account.Balance.Should().Be(70m);
        _repositoryMock.Verify(r => r.UpdateAsync(account), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DebitAsync_WithNonExistingAccount_ThrowsDomainException()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync((Account?)null);

        // Act
        var act = () => _sut.DebitAsync(accountId, 50m);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage($"*{accountId}*not found*");
    }

    #endregion

    #region CreditAsync Tests

    [Fact]
    public async Task CreditAsync_WithExistingAccount_CreditsAndSaves()
    {
        // Arrange
        var account = CreateTestAccount();
        
        _repositoryMock
            .Setup(r => r.GetByIdAsync(account.Id))
            .ReturnsAsync(account);

        // Act
        await _sut.CreditAsync(account.Id, 100m);

        // Assert
        account.Balance.Should().Be(100m);
        _repositoryMock.Verify(r => r.UpdateAsync(account), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreditAsync_WithNonExistingAccount_ThrowsDomainException()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync((Account?)null);

        // Act
        var act = () => _sut.CreditAsync(accountId, 100m);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage($"*{accountId}*not found*");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingAccount_DeletesAndSaves()
    {
        // Arrange
        var account = CreateTestAccount();
        
        _repositoryMock
            .Setup(r => r.GetByIdAsync(account.Id))
            .ReturnsAsync(account);

        // Act
        await _sut.DeleteAsync(account.Id);

        // Assert
        _repositoryMock.Verify(r => r.DeleteAsync(account), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingAccount_ThrowsDomainException()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(accountId))
            .ReturnsAsync((Account?)null);

        // Act
        var act = () => _sut.DeleteAsync(accountId);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage($"*{accountId}*not found*");
    }

    #endregion

    #region Helper Methods

    private static Account CreateTestAccount(string name = "John Doe", string email = "john@test.com")
    {
        return new Account(name, email, "hashedPassword");
    }

    private static Account CreateTestAccountWithPassword(string password)
    {
        var hash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(password)));
        
        return new Account("John Doe", "john@test.com", hash);
    }

    #endregion
}