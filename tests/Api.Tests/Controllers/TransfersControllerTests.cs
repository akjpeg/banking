using System.Security.Claims;
using Api.Controllers;
using Api.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shared.Contracts;
using Shared.Exceptions;
using Transfers.Application.Interfaces;

namespace Api.Tests.Controllers;

public class TransfersControllerTests
{
    private readonly Mock<ITransferMoneyHandler> _transferHandlerMock;
    private readonly TransfersController _sut;

    public TransfersControllerTests()
    {
        _transferHandlerMock = new Mock<ITransferMoneyHandler>();
        _sut = new TransfersController(_transferHandlerMock.Object);
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

    #endregion

    #region Transfer Tests

    [Fact]
    public async Task Transfer_WithValidData_ReturnsOkWithTransactionIds()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var request = new TransferToRequest
            {
                ToAccountNumber = "654321",
                Amount = 100m
            };
        var transferResult = new TransferResult(Guid.NewGuid(), Guid.NewGuid());

        SetupAuthenticatedUser(fromAccountId);

        _transferHandlerMock
            .Setup(h => h.HandleAsync(fromAccountId, request.ToAccountNumber, request.Amount))
            .ReturnsAsync(transferResult);

        // Act
        var result = await _sut.Transfer(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        
        var response = okResult.Value;
        response.Should().NotBeNull();
        
        var fromTransactionId = response!.GetType().GetProperty("FromTransactionId")!.GetValue(response);
        var toTransactionId = response.GetType().GetProperty("ToTransactionId")!.GetValue(response);
        var message = response.GetType().GetProperty("Message")!.GetValue(response);

        fromTransactionId.Should().Be(transferResult.FromTransactionId);
        toTransactionId.Should().Be(transferResult.ToTransactionId);
        message.Should().Be("Transfer completed successfully");
    }

    [Fact]
    public async Task Transfer_CallsHandlerWithCorrectParameters()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var request = new TransferToRequest
        {
            ToAccountNumber = "654321",
            Amount = 250m
        };
        var transferResult = new TransferResult(Guid.NewGuid(), Guid.NewGuid());

        SetupAuthenticatedUser(fromAccountId);

        _transferHandlerMock
            .Setup(h => h.HandleAsync(fromAccountId, request.ToAccountNumber, request.Amount))
            .ReturnsAsync(transferResult);

        // Act
        await _sut.Transfer(request);

        // Assert
        _transferHandlerMock.Verify(
            h => h.HandleAsync(fromAccountId, "654321", 250m),
            Times.Once);
    }

    [Fact]
    public async Task Transfer_WithInsufficientFunds_ReturnsBadRequest()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var request = new TransferToRequest
        {
            ToAccountNumber = "654321",
            Amount = 1000m
        };

        SetupAuthenticatedUser(fromAccountId);

        _transferHandlerMock
            .Setup(h => h.HandleAsync(fromAccountId, request.ToAccountNumber, request.Amount))
            .ThrowsAsync(new DomainException("Insufficient funds for debit operation"));

        // Act
        var result = await _sut.Transfer(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Insufficient funds for debit operation");
    }

    [Fact]
    public async Task Transfer_ToNonExistingAccount_ReturnsBadRequest()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var request = new TransferToRequest
            {
                ToAccountNumber = "999999",
                Amount = 100m
            };

        SetupAuthenticatedUser(fromAccountId);

        _transferHandlerMock
            .Setup(h => h.HandleAsync(fromAccountId, request.ToAccountNumber, request.Amount))
            .ThrowsAsync(new DomainException("Destination account not found"));

        // Act
        var result = await _sut.Transfer(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Destination account not found");
    }

    [Fact]
    public async Task Transfer_ToSameAccount_ReturnsBadRequest()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var request = new TransferToRequest
            {
                ToAccountNumber = "123456",
                Amount = 100m
            };

        SetupAuthenticatedUser(fromAccountId);

        _transferHandlerMock
            .Setup(h => h.HandleAsync(fromAccountId, request.ToAccountNumber, request.Amount))
            .ThrowsAsync(new DomainException("Cannot transfer to same account"));

        // Act
        var result = await _sut.Transfer(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Cannot transfer to same account");
    }

    [Fact]
    public async Task Transfer_WithZeroAmount_ReturnsBadRequest()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var request = new TransferToRequest
        {
            ToAccountNumber = "654321",
            Amount = 0m
        };

        SetupAuthenticatedUser(fromAccountId);

        _transferHandlerMock
            .Setup(h => h.HandleAsync(fromAccountId, request.ToAccountNumber, request.Amount))
            .ThrowsAsync(new DomainException("Amount must be greater than zero"));

        // Act
        var result = await _sut.Transfer(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Amount must be greater than zero");
    }

    [Fact]
    public async Task Transfer_WithNegativeAmount_ReturnsBadRequest()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var request = new TransferToRequest
        {
            ToAccountNumber = "654321",
            Amount = -50m
        };

        SetupAuthenticatedUser(fromAccountId);

        _transferHandlerMock
            .Setup(h => h.HandleAsync(fromAccountId, request.ToAccountNumber, request.Amount))
            .ThrowsAsync(new DomainException("Amount must be greater than zero"));

        // Act
        var result = await _sut.Transfer(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Transfer_WithSourceAccountNotFound_ReturnsBadRequest()
    {
        // Arrange
        var fromAccountId = Guid.NewGuid();
        var request = new TransferToRequest
        {
            ToAccountNumber = "654321",
            Amount = 100m
        };

        SetupAuthenticatedUser(fromAccountId);

        _transferHandlerMock
            .Setup(h => h.HandleAsync(fromAccountId, request.ToAccountNumber, request.Amount))
            .ThrowsAsync(new DomainException($"Account {fromAccountId} not found"));

        // Act
        var result = await _sut.Transfer(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeOfType<string>()
            .Which.Should().Contain("not found");
    }

    #endregion
}