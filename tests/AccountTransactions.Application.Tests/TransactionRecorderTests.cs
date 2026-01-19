// tests/AccountTransactions.Application.Tests/Services/TransactionRecorderTests.cs
using AccountTransactions.Application.Interfaces;
using AccountTransactions.Application.Services;
using AccountTransactions.Domain.Entities;
using AccountTransactions.Domain.Enums;
using FluentAssertions;
using Moq;
using Shared.Exceptions;

namespace AccountTransactions.Application.Tests.Services;

public class TransactionRecorderTests
{
    private readonly Mock<IAccountTransactionRepository> _repositoryMock;
    private readonly TransactionRecorder _sut;

    public TransactionRecorderTests()
    {
        _repositoryMock = new Mock<IAccountTransactionRepository>();
        _sut = new TransactionRecorder(_repositoryMock.Object);
    }

    #region RecordDepositAsync Tests

    [Fact]
    public async Task RecordDepositAsync_WithValidData_AddsTransaction()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = 100m;
        AccountTransaction? capturedTransaction = null;

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<AccountTransaction>()))
            .Callback<AccountTransaction>(t => capturedTransaction = t)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.RecordDepositAsync(accountId, amount);

        // Assert
        capturedTransaction.Should().NotBeNull();
        capturedTransaction!.AccountId.Should().Be(accountId);
        capturedTransaction.Amount.Should().Be(amount);
        capturedTransaction.Type.Should().Be(TransactionType.Deposit);
        capturedTransaction.Status.Should().Be(AccountTransactionStatus.Completed);
    }

    [Fact]
    public async Task RecordDepositAsync_CallsAddAndSaveChanges()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        // Act
        await _sut.RecordDepositAsync(accountId, 100m);

        // Assert
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<AccountTransaction>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RecordDepositAsync_WithZeroAmount_ThrowsDomainException()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        // Act
        var act = () => _sut.RecordDepositAsync(accountId, 0);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*greater than zero*");
        
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<AccountTransaction>()), Times.Never);
    }

    [Fact]
    public async Task RecordDepositAsync_WithNegativeAmount_ThrowsDomainException()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        // Act
        var act = () => _sut.RecordDepositAsync(accountId, -50m);

        // Assert
        await act.Should().ThrowAsync<DomainException>();
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    #endregion

    #region RecordWithdrawalAsync Tests

    [Fact]
    public async Task RecordWithdrawalAsync_WithValidData_AddsTransaction()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = 50m;
        AccountTransaction? capturedTransaction = null;

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<AccountTransaction>()))
            .Callback<AccountTransaction>(t => capturedTransaction = t)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.RecordWithdrawalAsync(accountId, amount);

        // Assert
        capturedTransaction.Should().NotBeNull();
        capturedTransaction!.AccountId.Should().Be(accountId);
        capturedTransaction.Amount.Should().Be(amount);
        capturedTransaction.Type.Should().Be(TransactionType.Withdrawal);
        capturedTransaction.Status.Should().Be(AccountTransactionStatus.Completed);
    }

    [Fact]
    public async Task RecordWithdrawalAsync_CallsAddAndSaveChanges()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        // Act
        await _sut.RecordWithdrawalAsync(accountId, 50m);

        // Assert
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<AccountTransaction>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RecordWithdrawalAsync_WithZeroAmount_ThrowsDomainException()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        // Act
        var act = () => _sut.RecordWithdrawalAsync(accountId, 0);

        // Assert
        await act.Should().ThrowAsync<DomainException>();
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<AccountTransaction>()), Times.Never);
    }

    #endregion

    #region RecordTransferAsync Tests

    [Fact]
    public async Task RecordTransferAsync_WithValidData_AddsTwoTransactions()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        var amount = 75m;
        var capturedTransactions = new List<AccountTransaction>();

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<AccountTransaction>()))
            .Callback<AccountTransaction>(t => capturedTransactions.Add(t))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.RecordTransferAsync(fromAccountId, toAccountId, amount);

        // Assert
        capturedTransactions.Should().HaveCount(2);
    }

    [Fact]
    public async Task RecordTransferAsync_CreatesTransferOutTransaction()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        var amount = 75m;
        var capturedTransactions = new List<AccountTransaction>();

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<AccountTransaction>()))
            .Callback<AccountTransaction>(t => capturedTransactions.Add(t))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.RecordTransferAsync(fromAccountId, toAccountId, amount);

        // Assert
        var outTransaction = capturedTransactions.First(t => t.AccountId == fromAccountId);
        outTransaction.FromAccountId.Should().Be(fromAccountId);
        outTransaction.ToAccountId.Should().Be(toAccountId);
        outTransaction.Amount.Should().Be(amount);
        outTransaction.Type.Should().Be(TransactionType.Transfer);
        outTransaction.Status.Should().Be(AccountTransactionStatus.Pending);
    }

    [Fact]
    public async Task RecordTransferAsync_CreatesTransferInTransaction()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        var amount = 75m;
        var capturedTransactions = new List<AccountTransaction>();

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<AccountTransaction>()))
            .Callback<AccountTransaction>(t => capturedTransactions.Add(t))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.RecordTransferAsync(fromAccountId, toAccountId, amount);

        // Assert
        var inTransaction = capturedTransactions.First(t => t.AccountId == toAccountId);
        inTransaction.FromAccountId.Should().Be(fromAccountId);
        inTransaction.ToAccountId.Should().Be(toAccountId);
        inTransaction.Amount.Should().Be(amount);
        inTransaction.Type.Should().Be(TransactionType.Transfer);
        inTransaction.Status.Should().Be(AccountTransactionStatus.Pending);
    }

    [Fact]
    public async Task RecordTransferAsync_ReturnsTransferResultWithBothIds()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        var capturedTransactions = new List<AccountTransaction>();

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<AccountTransaction>()))
            .Callback<AccountTransaction>(t => capturedTransactions.Add(t))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.RecordTransferAsync(fromAccountId, toAccountId, 100m);

        // Assert
        result.Should().NotBeNull();
        result.FromTransactionId.Should().NotBeEmpty();
        result.ToTransactionId.Should().NotBeEmpty();
        result.FromTransactionId.Should().NotBe(result.ToTransactionId);
    }

    [Fact]
    public async Task RecordTransferAsync_CallsAddTwiceAndSaveChangesOnce()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();

        // Act
        await _sut.RecordTransferAsync(fromAccountId, toAccountId, 100m);

        // Assert
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<AccountTransaction>()), Times.Exactly(2));
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RecordTransferAsync_ToSameAccount_ThrowsDomainException()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        // Act
        var act = () => _sut.RecordTransferAsync(accountId, accountId, 100m);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*same account*");
        
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<AccountTransaction>()), Times.Never);
    }

    [Fact]
    public async Task RecordTransferAsync_WithZeroAmount_ThrowsDomainException()
    {
        // Act
        var act = () => _sut.RecordTransferAsync(Guid.NewGuid(), Guid.NewGuid(), 0);

        // Assert
        await act.Should().ThrowAsync<DomainException>();
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    #endregion

    #region MarkTransferCompletedAsync Tests

    [Fact]
    public async Task MarkTransferCompletedAsync_WithValidIds_MarksBothCompleted()
    {
        // Arrange
        var fromTransaction = AccountTransaction.CreateTransferOut(Guid.NewGuid(), Guid.NewGuid(), 100m);
        var toTransaction = AccountTransaction.CreateTransferIn(Guid.NewGuid(), Guid.NewGuid(), 100m);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(fromTransaction.Id))
            .ReturnsAsync(fromTransaction);
        
        _repositoryMock
            .Setup(r => r.GetByIdAsync(toTransaction.Id))
            .ReturnsAsync(toTransaction);

        // Act
        await _sut.MarkTransferCompletedAsync(fromTransaction.Id, toTransaction.Id);

        // Assert
        fromTransaction.Status.Should().Be(AccountTransactionStatus.Completed);
        toTransaction.Status.Should().Be(AccountTransactionStatus.Completed);
    }

    [Fact]
    public async Task MarkTransferCompletedAsync_CallsUpdateAndSaveChanges()
    {
        // Arrange
        var fromTransaction = AccountTransaction.CreateTransferOut(Guid.NewGuid(), Guid.NewGuid(), 100m);
        var toTransaction = AccountTransaction.CreateTransferIn(Guid.NewGuid(), Guid.NewGuid(), 100m);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(fromTransaction.Id))
            .ReturnsAsync(fromTransaction);
        
        _repositoryMock
            .Setup(r => r.GetByIdAsync(toTransaction.Id))
            .ReturnsAsync(toTransaction);

        // Act
        await _sut.MarkTransferCompletedAsync(fromTransaction.Id, toTransaction.Id);

        // Assert
        _repositoryMock.Verify(r => r.UpdateAsync(fromTransaction), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(toTransaction), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task MarkTransferCompletedAsync_WithNonExistingFromTransaction_ThrowsDomainException()
    {
        // Arrange
        var fromTransactionId = Guid.NewGuid();
        var toTransactionId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(fromTransactionId))
            .ReturnsAsync((AccountTransaction?)null);

        // Act
        var act = () => _sut.MarkTransferCompletedAsync(fromTransactionId, toTransactionId);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage($"*{fromTransactionId}*not found*");
    }

    [Fact]
    public async Task MarkTransferCompletedAsync_WithNonExistingToTransaction_ThrowsDomainException()
    {
        // Arrange
        var fromTransaction = AccountTransaction.CreateTransferOut(Guid.NewGuid(), Guid.NewGuid(), 100m);
        var toTransactionId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(fromTransaction.Id))
            .ReturnsAsync(fromTransaction);
        
        _repositoryMock
            .Setup(r => r.GetByIdAsync(toTransactionId))
            .ReturnsAsync((AccountTransaction?)null);

        // Act
        var act = () => _sut.MarkTransferCompletedAsync(fromTransaction.Id, toTransactionId);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage($"*{toTransactionId}*not found*");
    }

    [Fact]
    public async Task MarkTransferCompletedAsync_WithNonExistingFromTransaction_DoesNotCallUpdate()
    {
        // Arrange
        var fromTransactionId = Guid.NewGuid();
        var toTransactionId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(fromTransactionId))
            .ReturnsAsync((AccountTransaction?)null);

        // Act
        var act = () => _sut.MarkTransferCompletedAsync(fromTransactionId, toTransactionId);

        // Assert
        await act.Should().ThrowAsync<DomainException>();
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<AccountTransaction>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    #endregion

    #region MarkTransferFailedAsync Tests

    [Fact]
    public async Task MarkTransferFailedAsync_WithValidIds_MarksBothFailed()
    {
        // Arrange
        var fromTransaction = AccountTransaction.CreateTransferOut(Guid.NewGuid(), Guid.NewGuid(), 100m);
        var toTransaction = AccountTransaction.CreateTransferIn(Guid.NewGuid(), Guid.NewGuid(), 100m);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(fromTransaction.Id))
            .ReturnsAsync(fromTransaction);
        
        _repositoryMock
            .Setup(r => r.GetByIdAsync(toTransaction.Id))
            .ReturnsAsync(toTransaction);

        // Act
        await _sut.MarkTransferFailedAsync(fromTransaction.Id, toTransaction.Id);

        // Assert
        fromTransaction.Status.Should().Be(AccountTransactionStatus.Failed);
        toTransaction.Status.Should().Be(AccountTransactionStatus.Failed);
    }

    [Fact]
    public async Task MarkTransferFailedAsync_CallsUpdateAndSaveChanges()
    {
        // Arrange
        var fromTransaction = AccountTransaction.CreateTransferOut(Guid.NewGuid(), Guid.NewGuid(), 100m);
        var toTransaction = AccountTransaction.CreateTransferIn(Guid.NewGuid(), Guid.NewGuid(), 100m);

        _repositoryMock
            .Setup(r => r.GetByIdAsync(fromTransaction.Id))
            .ReturnsAsync(fromTransaction);
        
        _repositoryMock
            .Setup(r => r.GetByIdAsync(toTransaction.Id))
            .ReturnsAsync(toTransaction);

        // Act
        await _sut.MarkTransferFailedAsync(fromTransaction.Id, toTransaction.Id);

        // Assert
        _repositoryMock.Verify(r => r.UpdateAsync(fromTransaction), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(toTransaction), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task MarkTransferFailedAsync_WithNonExistingFromTransaction_ThrowsDomainException()
    {
        // Arrange
        var fromTransactionId = Guid.NewGuid();
        var toTransactionId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(fromTransactionId))
            .ReturnsAsync((AccountTransaction?)null);

        // Act
        var act = () => _sut.MarkTransferFailedAsync(fromTransactionId, toTransactionId);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage($"*{fromTransactionId}*not found*");
    }

    [Fact]
    public async Task MarkTransferFailedAsync_WithNonExistingToTransaction_ThrowsDomainException()
    {
        // Arrange
        var fromTransaction = AccountTransaction.CreateTransferOut(Guid.NewGuid(), Guid.NewGuid(), 100m);
        var toTransactionId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByIdAsync(fromTransaction.Id))
            .ReturnsAsync(fromTransaction);
        
        _repositoryMock
            .Setup(r => r.GetByIdAsync(toTransactionId))
            .ReturnsAsync((AccountTransaction?)null);

        // Act
        var act = () => _sut.MarkTransferFailedAsync(fromTransaction.Id, toTransactionId);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage($"*{toTransactionId}*not found*");
    }

    #endregion

    #region GetByAccountIdAsync Tests

    [Fact]
    public async Task GetByAccountIdAsync_WithTransactions_ReturnsMappedDtos()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var transactions = new List<AccountTransaction>
        {
            AccountTransaction.CreateDeposit(accountId, 100m),
            AccountTransaction.CreateWithdrawal(accountId, 50m)
        };

        _repositoryMock
            .Setup(r => r.GetByAccountIdAsync(accountId))
            .ReturnsAsync(transactions);

        // Act
        var result = await _sut.GetByAccountIdAsync(accountId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByAccountIdAsync_MapsPropertiesCorrectly()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var deposit = AccountTransaction.CreateDeposit(accountId, 100m);

        _repositoryMock
            .Setup(r => r.GetByAccountIdAsync(accountId))
            .ReturnsAsync(new List<AccountTransaction> { deposit });

        // Act
        var result = await _sut.GetByAccountIdAsync(accountId);

        // Assert
        var dto = result.First();
        dto.Id.Should().Be(deposit.Id);
        dto.FromAccountId.Should().Be(deposit.FromAccountId);
        dto.ToAccountId.Should().Be(deposit.ToAccountId);
        dto.Amount.Should().Be(deposit.Amount);
        dto.Type.Should().Be("Deposit");
        dto.Status.Should().Be("Completed");
        dto.CreatedAt.Should().Be(deposit.CreatedAt);
    }

    [Fact]
    public async Task GetByAccountIdAsync_WithNoTransactions_ReturnsEmptyList()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByAccountIdAsync(accountId))
            .ReturnsAsync(new List<AccountTransaction>());

        // Act
        var result = await _sut.GetByAccountIdAsync(accountId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByAccountIdAsync_WithMixedTransactionTypes_MapsAllCorrectly()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var otherAccountId = Guid.NewGuid();
        
        var transactions = new List<AccountTransaction>
        {
            AccountTransaction.CreateDeposit(accountId, 100m),
            AccountTransaction.CreateWithdrawal(accountId, 30m),
            AccountTransaction.CreateTransferOut(accountId, otherAccountId, 50m)
        };

        _repositoryMock
            .Setup(r => r.GetByAccountIdAsync(accountId))
            .ReturnsAsync(transactions);

        // Act
        var result = await _sut.GetByAccountIdAsync(accountId);

        // Assert
        result.Should().HaveCount(3);
        result.Select(t => t.Type).Should().Contain(new[] { "Deposit", "Withdrawal", "Transfer" });
    }

    [Fact]
    public async Task GetByAccountIdAsync_ReturnsReadOnlyList()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.GetByAccountIdAsync(accountId))
            .ReturnsAsync(new List<AccountTransaction>());

        // Act
        var result = await _sut.GetByAccountIdAsync(accountId);

        // Assert
        result.Should().BeAssignableTo<IReadOnlyList<Shared.Contracts.TransactionDto>>();
    }

    #endregion
}