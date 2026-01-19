using Api.DTOs;
using Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;
using Transfers.Application.Interfaces;

namespace Api.Controllers;

/// <summary>
/// Manages transfer operations between user accounts
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransfersController : ControllerBase
{
    private readonly ITransferMoneyHandler _transferHandler;

    public TransfersController(ITransferMoneyHandler transferHandler)
    {
        _transferHandler = transferHandler;
    }

    /// <summary>
    /// Transfer money to another account
    /// </summary>
    /// <param name="request">Transfer details</param>
    /// <returns>Transaction IDs for the transfer</returns>
    /// <response code="200">Transfer completed successfully</response>
    /// <response code="400">Invalid request, insufficient funds, or account not found</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="409">Concurrent update conflict - please retry</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Transfer([FromBody] TransferToRequest request)
    {
        try
        {
            var fromAccountId = User.GetAccountId();

            var result = await _transferHandler.HandleAsync(
                fromAccountId,
                request.ToAccountNumber,
                request.Amount);

            return Ok(new
            {
                FromTransactionId = result.FromTransactionId,
                ToTransactionId = result.ToTransactionId,
                Message = "Transfer completed successfully"
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Transfer failed due to concurrent account update. Please retry.");
        }
        catch (DomainException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}