using AccountTransactions.Domain.Entities;
using AccountTransactions.Domain.Enums;
using FluentAssertions;
using Shared.Exceptions;

namespace AccountTransactions.Domain.Tests.Entities;

public class AccountTransactionTests
{
    #region CreateDeposit Tests

    [Fact]
    public void CreateDeposit_WithValidData_CreatesDepositTransaction()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = 100m;

        // Act
        var transaction = AccountTransaction.CreateDeposit(accountId, amount);

        // Assert
        transaction.Id.Should().NotBeEmpty();
        transaction.AccountId.Should().Be(accountId);
        transaction.ToAccountId.Should().Be(accountId);
        transaction.FromAccountId.Should().BeNull();
        transaction.Amount.Should().Be(amount);
        transaction.Type.Should().Be(TransactionType.Deposit);
        transaction.Status.Should().Be(AccountTransactionStatus.Completed);
    }

    [Fact]
    public void CreateDeposit_SetsCreatedAtToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var transaction = AccountTransaction.CreateDeposit(Guid.NewGuid(), 100m);

        // Assert
        var after = DateTime.UtcNow;
        transaction.CreatedAt.Should().BeOnOrAfter(before);
        transaction.CreatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void CreateDeposit_UpdatedAtIsNull()
    {
        // Act
        var transaction = AccountTransaction.CreateDeposit(Guid.NewGuid(), 100m);

        // Assert
        transaction.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void CreateDeposit_WithZeroAmount_ThrowsDomainException()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        // Act
        var act = () => AccountTransaction.CreateDeposit(accountId, 0);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*greater than zero*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(-0.01)]
    public void CreateDeposit_WithNegativeAmount_ThrowsDomainException(decimal amount)
    {
        // Arrange
        var accountId = Guid.NewGuid();

        // Act
        var act = () => AccountTransaction.CreateDeposit(accountId, amount);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*greater than zero*");
    }

    [Fact]
    public void CreateDeposit_WithDecimalPrecision_MaintainsPrecision()
    {
        // Act
        var transaction = AccountTransaction.CreateDeposit(Guid.NewGuid(), 123.45m);

        // Assert
        transaction.Amount.Should().Be(123.45m);
    }

    #endregion

    #region CreateWithdrawal Tests

    [Fact]
    public void CreateWithdrawal_WithValidData_CreatesWithdrawalTransaction()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = 50m;

        // Act
        var transaction = AccountTransaction.CreateWithdrawal(accountId, amount);

        // Assert
        transaction.Id.Should().NotBeEmpty();
        transaction.AccountId.Should().Be(accountId);
        transaction.FromAccountId.Should().Be(accountId);
        transaction.ToAccountId.Should().BeNull();
        transaction.Amount.Should().Be(amount);
        transaction.Type.Should().Be(TransactionType.Withdrawal);
        transaction.Status.Should().Be(AccountTransactionStatus.Completed);
    }

    [Fact]
    public void CreateWithdrawal_SetsCreatedAtToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var transaction = AccountTransaction.CreateWithdrawal(Guid.NewGuid(), 100m);

        // Assert
        var after = DateTime.UtcNow;
        transaction.CreatedAt.Should().BeOnOrAfter(before);
        transaction.CreatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void CreateWithdrawal_WithZeroAmount_ThrowsDomainException()
    {
        // Act
        var act = () => AccountTransaction.CreateWithdrawal(Guid.NewGuid(), 0);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*greater than zero*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(-0.01)]
    public void CreateWithdrawal_WithNegativeAmount_ThrowsDomainException(decimal amount)
    {
        // Act
        var act = () => AccountTransaction.CreateWithdrawal(Guid.NewGuid(), amount);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*greater than zero*");
    }

    #endregion

    #region CreateTransferOut Tests

    [Fact]
    public void CreateTransferOut_WithValidData_CreatesTransferOutTransaction()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        var amount = 75m;

        // Act
        var transaction = AccountTransaction.CreateTransferOut(fromAccountId, toAccountId, amount);

        // Assert
        transaction.Id.Should().NotBeEmpty();
        transaction.AccountId.Should().Be(fromAccountId);
        transaction.FromAccountId.Should().Be(fromAccountId);
        transaction.ToAccountId.Should().Be(toAccountId);
        transaction.Amount.Should().Be(amount);
        transaction.Type.Should().Be(TransactionType.Transfer);
        transaction.Status.Should().Be(AccountTransactionStatus.Pending);
    }

    [Fact]
    public void CreateTransferOut_StatusIsPending()
    {
        // Act
        var transaction = AccountTransaction.CreateTransferOut(
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            100m);

        // Assert
        transaction.Status.Should().Be(AccountTransactionStatus.Pending);
    }

    [Fact]
    public void CreateTransferOut_SetsCreatedAtToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var transaction = AccountTransaction.CreateTransferOut(
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            100m);

        // Assert
        var after = DateTime.UtcNow;
        transaction.CreatedAt.Should().BeOnOrAfter(before);
        transaction.CreatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void CreateTransferOut_ToSameAccount_ThrowsDomainException()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        // Act
        var act = () => AccountTransaction.CreateTransferOut(accountId, accountId, 100m);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*same account*");
    }

    [Fact]
    public void CreateTransferOut_WithZeroAmount_ThrowsDomainException()
    {
        // Act
        var act = () => AccountTransaction.CreateTransferOut(
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            0);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*greater than zero*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(-0.01)]
    public void CreateTransferOut_WithNegativeAmount_ThrowsDomainException(decimal amount)
    {
        // Act
        var act = () => AccountTransaction.CreateTransferOut(
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            amount);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*greater than zero*");
    }

    #endregion

    #region CreateTransferIn Tests

    [Fact]
    public void CreateTransferIn_WithValidData_CreatesTransferInTransaction()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        var amount = 75m;

        // Act
        var transaction = AccountTransaction.CreateTransferIn(fromAccountId, toAccountId, amount);

        // Assert
        transaction.Id.Should().NotBeEmpty();
        transaction.AccountId.Should().Be(toAccountId);
        transaction.FromAccountId.Should().Be(fromAccountId);
        transaction.ToAccountId.Should().Be(toAccountId);
        transaction.Amount.Should().Be(amount);
        transaction.Type.Should().Be(TransactionType.Transfer);
        transaction.Status.Should().Be(AccountTransactionStatus.Pending);
    }

    [Fact]
    public void CreateTransferIn_StatusIsPending()
    {
        // Act
        var transaction = AccountTransaction.CreateTransferIn(
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            100m);

        // Assert
        transaction.Status.Should().Be(AccountTransactionStatus.Pending);
    }

    [Fact]
    public void CreateTransferIn_ToSameAccount_ThrowsDomainException()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        // Act
        var act = () => AccountTransaction.CreateTransferIn(accountId, accountId, 100m);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*same account*");
    }

    [Fact]
    public void CreateTransferIn_WithZeroAmount_ThrowsDomainException()
    {
        // Act
        var act = () => AccountTransaction.CreateTransferIn(
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            0);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*greater than zero*");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(-0.01)]
    public void CreateTransferIn_WithNegativeAmount_ThrowsDomainException(decimal amount)
    {
        // Act
        var act = () => AccountTransaction.CreateTransferIn(
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            amount);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*greater than zero*");
    }

    #endregion

    #region MarkCompleted Tests

    [Fact]
    public void MarkCompleted_ChangesStatusToCompleted()
    {
        // Arrange
        var transaction = AccountTransaction.CreateTransferOut(
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            100m);
        
        transaction.Status.Should().Be(AccountTransactionStatus.Pending);

        // Act
        transaction.MarkCompleted();

        // Assert
        transaction.Status.Should().Be(AccountTransactionStatus.Completed);
    }

    [Fact]
    public void MarkCompleted_SetsUpdatedAt()
    {
        // Arrange
        var transaction = AccountTransaction.CreateTransferOut(
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            100m);
        
        var before = DateTime.UtcNow;

        // Act
        transaction.MarkCompleted();

        // Assert
        var after = DateTime.UtcNow;
        transaction.UpdatedAt.Should().NotBeNull();
        transaction.UpdatedAt.Should().BeOnOrAfter(before);
        transaction.UpdatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void MarkCompleted_OnAlreadyCompletedTransaction_UpdatesTimestamp()
    {
        // Arrange
        var transaction = AccountTransaction.CreateDeposit(Guid.NewGuid(), 100m);
        var firstUpdatedAt = transaction.UpdatedAt;

        // Act
        transaction.MarkCompleted();

        // Assert
        transaction.Status.Should().Be(AccountTransactionStatus.Completed);
        transaction.UpdatedAt.Should().NotBe(firstUpdatedAt);
    }

    #endregion

    #region MarkFailed Tests

    [Fact]
    public void MarkFailed_ChangesStatusToFailed()
    {
        // Arrange
        var transaction = AccountTransaction.CreateTransferOut(
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            100m);

        // Act
        transaction.MarkFailed();

        // Assert
        transaction.Status.Should().Be(AccountTransactionStatus.Failed);
    }

    [Fact]
    public void MarkFailed_SetsUpdatedAt()
    {
        // Arrange
        var transaction = AccountTransaction.CreateTransferOut(
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            100m);
        
        var before = DateTime.UtcNow;

        // Act
        transaction.MarkFailed();

        // Assert
        var after = DateTime.UtcNow;
        transaction.UpdatedAt.Should().NotBeNull();
        transaction.UpdatedAt.Should().BeOnOrAfter(before);
        transaction.UpdatedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void MarkFailed_OnPendingTransfer_ChangesToFailed()
    {
        // Arrange
        var transaction = AccountTransaction.CreateTransferIn(
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            100m);
        
        transaction.Status.Should().Be(AccountTransactionStatus.Pending);

        // Act
        transaction.MarkFailed();

        // Assert
        transaction.Status.Should().Be(AccountTransactionStatus.Failed);
    }

    #endregion

    #region TransferOut vs TransferIn Comparison

    [Fact]
    public void CreateTransferOutAndIn_WithSameData_HaveDifferentAccountIds()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        var amount = 100m;

        // Act
        var transferOut = AccountTransaction.CreateTransferOut(fromAccountId, toAccountId, amount);
        var transferIn = AccountTransaction.CreateTransferIn(fromAccountId, toAccountId, amount);

        // Assert
        transferOut.AccountId.Should().Be(fromAccountId);
        transferIn.AccountId.Should().Be(toAccountId);
        
        // Both have same from/to
        transferOut.FromAccountId.Should().Be(fromAccountId);
        transferIn.FromAccountId.Should().Be(fromAccountId);
        transferOut.ToAccountId.Should().Be(toAccountId);
        transferIn.ToAccountId.Should().Be(toAccountId);
    }

    [Fact]
    public void CreateTransferOutAndIn_BothHavePendingStatus()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();

        // Act
        var transferOut = AccountTransaction.CreateTransferOut(fromAccountId, toAccountId, 100m);
        var transferIn = AccountTransaction.CreateTransferIn(fromAccountId, toAccountId, 100m);

        // Assert
        transferOut.Status.Should().Be(AccountTransactionStatus.Pending);
        transferIn.Status.Should().Be(AccountTransactionStatus.Pending);
    }

    #endregion

    #region Unique IDs

    [Fact]
    public void CreateMultipleTransactions_EachHasUniqueId()
    {
        // Act
        var deposit = AccountTransaction.CreateDeposit(Guid.NewGuid(), 100m);
        var withdrawal = AccountTransaction.CreateWithdrawal(Guid.NewGuid(), 50m);
        var transferOut = AccountTransaction.CreateTransferOut(Guid.NewGuid(), Guid.NewGuid(), 75m);
        var transferIn = AccountTransaction.CreateTransferIn(Guid.NewGuid(), Guid.NewGuid(), 25m);

        // Assert
        var ids = new[] { deposit.Id, withdrawal.Id, transferOut.Id, transferIn.Id };
        ids.Should().OnlyHaveUniqueItems();
    }

    #endregion
}