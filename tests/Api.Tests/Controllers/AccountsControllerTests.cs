using System.Security.Claims;
using Api.Controllers;
using Api.DTOs;
using Api.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shared.Contracts;
using Shared.Exceptions;

namespace Api.Tests.Controllers;

public class AccountsControllerTests
{
    private readonly Mock<IAccountService> _accountServiceMock;
    private readonly Mock<ITransactionRecorder> _transactionRecorderMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly AccountsController _sut;

    public AccountsControllerTests()
    {
        _accountServiceMock = new Mock<IAccountService>();
        _transactionRecorderMock = new Mock<ITransactionRecorder>();
        _tokenServiceMock = new Mock<ITokenService>();
        
        _sut = new AccountsController(
            _accountServiceMock.Object,
            _transactionRecorderMock.Object,
            _tokenServiceMock.Object);
    }

    #region Helper Methods

    private void SetupAuthenticatedUser(Guid accountId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, accountId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    private static AccountDto CreateTestAccountDto(
        Guid? id = null, 
        string accountNumber = "123456", 
        string name = "John Doe", 
        decimal balance = 0)
    {
        return new AccountDto(id ?? Guid.NewGuid(), accountNumber, name, balance);
    }

    #endregion

    #region Register Tests

    [Fact]
    public async Task Register_WithValidData_ReturnsCreatedWithLoginResponse()
    {
        // Arrange
        var request = new RegisterRequest 
        { 
            Name = "John Doe", 
            Email = "john@test.com", 
            Password = "password123" 
        };
        var account = CreateTestAccountDto();
        var token = "jwt-token-123";

        _accountServiceMock
            .Setup(s => s.CreateAsync(request.Name, request.Email, request.Password))
            .ReturnsAsync(account);

        _tokenServiceMock
            .Setup(s => s.GenerateToken(account))
            .Returns(token);

        // Act
        var result = await _sut.Register(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
        createdResult.Location.Should().Be("/api/accounts/me");
        
        var response = createdResult.Value.Should().BeOfType<LoginResponse>().Subject;
        response.Token.Should().Be(token);
        response.AccountId.Should().Be(account.Id);
        response.AccountNumber.Should().Be(account.AccountNumber);
        response.Name.Should().Be(account.Name);
    }

    [Fact]
    public async Task Register_CallsAccountServiceCreate()
    {
        // Arrange
        var request = new RegisterRequest 
        { 
            Name = "John Doe", 
            Email = "john@test.com", 
            Password = "password123" 
        };
        var account = CreateTestAccountDto();

        _accountServiceMock
            .Setup(s => s.CreateAsync(request.Name, request.Email, request.Password))
            .ReturnsAsync(account);

        _tokenServiceMock
            .Setup(s => s.GenerateToken(It.IsAny<AccountDto>()))
            .Returns("token");

        // Act
        await _sut.Register(request);

        // Assert
        _accountServiceMock.Verify(
            s => s.CreateAsync(request.Name, request.Email, request.Password), 
            Times.Once);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest 
        { 
            Name = "John Doe", 
            Email = "john@test.com", 
            Password = "password123" 
        };

        _accountServiceMock
            .Setup(s => s.CreateAsync(request.Name, request.Email, request.Password))
            .ThrowsAsync(new DomainException("Email already registered"));

        // Act
        var result = await _sut.Register(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Email already registered");
    }

    [Fact]
    public async Task Register_WithDomainException_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest 
        { 
            Name = "John Doe", 
            Email = "john@test.com", 
            Password = "password123" 
        };

        _accountServiceMock
            .Setup(s => s.CreateAsync(request.Name, request.Email, request.Password))
            .ThrowsAsync(new DomainException("Name is required"));

        // Act
        var result = await _sut.Register(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithLoginResponse()
    {
        // Arrange
        var request = new LoginRequest 
        { 
            Email = "john@test.com", 
            Password = "password123" 
        };
        var account = CreateTestAccountDto();
        var token = "jwt-token-123";

        _accountServiceMock
            .Setup(s => s.LoginAsync(request.Email, request.Password))
            .ReturnsAsync(account);

        _tokenServiceMock
            .Setup(s => s.GenerateToken(account))
            .Returns(token);

        // Act
        var result = await _sut.Login(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        
        var response = okResult.Value.Should().BeOfType<LoginResponse>().Subject;
        response.Token.Should().Be(token);
        response.AccountId.Should().Be(account.Id);
        response.AccountNumber.Should().Be(account.AccountNumber);
        response.Name.Should().Be(account.Name);
    }

    [Fact]
    public async Task Login_WithInvalidEmail_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
            {
                Email = "nonexistent@test.com",
                Password = "password123"
            };

        _accountServiceMock
            .Setup(s => s.LoginAsync(request.Email, request.Password))
            .ReturnsAsync((AccountDto?)null);

        // Act
        var result = await _sut.Login(request);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().Be("Invalid email or password");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
            {
                Email = "john@test.com",
                Password = "wrongpassword"
            };

        _accountServiceMock
            .Setup(s => s.LoginAsync(request.Email, request.Password))
            .ReturnsAsync((AccountDto?)null);

        // Act
        var result = await _sut.Login(request);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().Be("Invalid email or password");
    }

    [Fact]
    public async Task Login_DoesNotGenerateTokenForInvalidCredentials()
    {
        // Arrange
        var request = new LoginRequest
            {
                Email = "john@test.com", 
                Password = "wrongpassword"
            };

        _accountServiceMock
            .Setup(s => s.LoginAsync(request.Email, request.Password))
            .ReturnsAsync((AccountDto?)null);

        // Act
        await _sut.Login(request);

        // Assert
        _tokenServiceMock.Verify(s => s.GenerateToken(It.IsAny<AccountDto>()), Times.Never);
    }

    #endregion

    #region GetMyAccount Tests

    [Fact]
    public async Task GetMyAccount_WithValidToken_ReturnsOkWithAccount()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = CreateTestAccountDto(accountId);
        
        SetupAuthenticatedUser(accountId);

        _accountServiceMock
            .Setup(s => s.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        // Act
        var result = await _sut.GetMyAccount();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(account);
    }

    [Fact]
    public async Task GetMyAccount_WithNonExistingAccount_ReturnsNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        
        SetupAuthenticatedUser(accountId);

        _accountServiceMock
            .Setup(s => s.GetByIdAsync(accountId))
            .ReturnsAsync((AccountDto?)null);

        // Act
        var result = await _sut.GetMyAccount();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region GetMyBalance Tests

    [Fact]
    public async Task GetMyBalance_WithValidToken_ReturnsOkWithBalance()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = CreateTestAccountDto(accountId, balance: 500m);
        
        SetupAuthenticatedUser(accountId);

        _accountServiceMock
            .Setup(s => s.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        // Act
        var result = await _sut.GetMyBalance();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        
        var balance = okResult.Value;
        balance.Should().NotBeNull();
        balance!.GetType().GetProperty("Balance")!.GetValue(balance).Should().Be(500m);
    }

    [Fact]
    public async Task GetMyBalance_WithNonExistingAccount_ReturnsNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        
        SetupAuthenticatedUser(accountId);

        _accountServiceMock
            .Setup(s => s.GetByIdAsync(accountId))
            .ReturnsAsync((AccountDto?)null);

        // Act
        var result = await _sut.GetMyBalance();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region GetTransactions Tests

    [Fact]
    public async Task GetTransactions_WithValidToken_ReturnsOkWithTransactions()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = CreateTestAccountDto(accountId);
        var transactions = new List<TransactionDto>
        {
            new(Guid.NewGuid(), null, accountId, 100m, "Deposit", "Completed", DateTime.UtcNow),
            new(Guid.NewGuid(), accountId, null, 50m, "Withdrawal", "Completed", DateTime.UtcNow)
        };
        
        SetupAuthenticatedUser(accountId);

        _accountServiceMock
            .Setup(s => s.GetAccountTransactionsAsync(accountId))
            .ReturnsAsync(transactions);

        // Act
        var result = await _sut.GetTransactions();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(transactions);
    }

    [Fact]
    public async Task GetTransactions_WithNonExistingAccount_ReturnsNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        
        SetupAuthenticatedUser(accountId);

        _accountServiceMock
            .Setup(s => s.GetAccountTransactionsAsync(accountId))
            .ThrowsAsync(new KeyNotFoundException($"Account {accountId} not found"));

        // Act
        var result = await _sut.GetTransactions();

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetTransactions_DoesNotCallRecorderIfAccountNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        
        SetupAuthenticatedUser(accountId);

        _accountServiceMock
            .Setup(s => s.GetByIdAsync(accountId))
            .ReturnsAsync((AccountDto?)null);

        // Act
        await _sut.GetTransactions();

        // Assert
        _transactionRecorderMock.Verify(
            t => t.GetByAccountIdAsync(It.IsAny<Guid>()), 
            Times.Never);
    }

    #endregion

    #region Deposit Tests

    [Fact]
    public async Task Deposit_WithValidData_ReturnsOkWithUpdatedAccount()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new DepositRequest
        {
            Amount = 100m
        };
        var updatedAccount = CreateTestAccountDto(accountId, balance: 100m);
        
        SetupAuthenticatedUser(accountId);

        _accountServiceMock
            .Setup(s => s.CreditAsync(accountId, request.Amount))
            .ReturnsAsync(updatedAccount);

        // Act
        var result = await _sut.Deposit(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(updatedAccount);
    }

    [Fact]
    public async Task Deposit_CallsCreditAndRecordsTransaction()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new DepositRequest
        {
            Amount = 100m
        };
        var account = CreateTestAccountDto(accountId);
        
        SetupAuthenticatedUser(accountId);

        _accountServiceMock
            .Setup(s => s.CreditAsync(accountId, request.Amount))
            .ReturnsAsync(account);

        // Act
        var result = await _sut.Deposit(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _accountServiceMock.Verify(s => s.CreditAsync(accountId, 100m), Times.Once);
    }

    [Fact]
    public async Task Deposit_WithInvalidAmount_ReturnsBadRequest()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new DepositRequest
        {
            Amount = -50m
        };
        
        SetupAuthenticatedUser(accountId);

        _accountServiceMock
            .Setup(s => s.CreditAsync(accountId, request.Amount))
            .ThrowsAsync(new DomainException("Amount must be greater than zero"));

        // Act
        var result = await _sut.Deposit(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Amount must be greater than zero");
    }

    [Fact]
    public async Task Deposit_WithDomainException_DoesNotRecordTransaction()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new DepositRequest
        {
            Amount = -50m
        };
        
        SetupAuthenticatedUser(accountId);

        _accountServiceMock
            .Setup(s => s.CreditAsync(accountId, request.Amount))
            .ThrowsAsync(new DomainException("Amount must be greater than zero"));

        // Act
        await _sut.Deposit(request);

        // Assert
        _transactionRecorderMock.Verify(
            t => t.RecordDepositAsync(It.IsAny<Guid>(), It.IsAny<decimal>()), 
            Times.Never);
    }

    #endregion

    #region Withdraw Tests

    [Fact]
    public async Task Withdraw_WithValidData_ReturnsOkWithUpdatedAccount()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new WithdrawRequest
        {
            Amount = 50m
        };
        var updatedAccount = CreateTestAccountDto(accountId, balance: 50m);
        
        SetupAuthenticatedUser(accountId);

        _accountServiceMock
            .Setup(s => s.DebitAsync(accountId, request.Amount))
            .ReturnsAsync(updatedAccount);

        // Act
        var result = await _sut.Withdraw(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(updatedAccount);
    }

    [Fact]
    public async Task Withdraw_CallsDebitAndRecordsTransaction()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new WithdrawRequest
        {
            Amount = 50m
        };
        var account = CreateTestAccountDto(accountId);
        
        SetupAuthenticatedUser(accountId);

        _accountServiceMock
            .Setup(s => s.DebitAsync(accountId, request.Amount))
            .ReturnsAsync(account);

        // Act
        var result = await _sut.Withdraw(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _accountServiceMock.Verify(s => s.DebitAsync(accountId, 50m), Times.Once);
    }

    [Fact]
    public async Task Withdraw_WithInsufficientFunds_ReturnsBadRequest()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new WithdrawRequest
        {
            Amount = 1000m
        };
        
        SetupAuthenticatedUser(accountId);

        _accountServiceMock
            .Setup(s => s.DebitAsync(accountId, request.Amount))
            .ThrowsAsync(new DomainException("Insufficient funds"));

        // Act
        var result = await _sut.Withdraw(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Insufficient funds");
    }

    [Fact]
    public async Task Withdraw_WithInvalidAmount_ReturnsBadRequest()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new WithdrawRequest
        {
            Amount = -50m
        };
        
        SetupAuthenticatedUser(accountId);

        _accountServiceMock
            .Setup(s => s.DebitAsync(accountId, request.Amount))
            .ThrowsAsync(new DomainException("Amount must be greater than zero"));

        // Act
        var result = await _sut.Withdraw(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Withdraw_WithDomainException_DoesNotRecordTransaction()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var request = new WithdrawRequest
        {
            Amount = 1000m
        };
        
        SetupAuthenticatedUser(accountId);

        _accountServiceMock
            .Setup(s => s.DebitAsync(accountId, request.Amount))
            .ThrowsAsync(new DomainException("Insufficient funds"));

        // Act
        await _sut.Withdraw(request);

        // Assert
        _transactionRecorderMock.Verify(
            t => t.RecordWithdrawalAsync(It.IsAny<Guid>(), It.IsAny<decimal>()), 
            Times.Never);
    }

    #endregion
}