using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;

/// <summary>
/// Administrative operations for managing accounts
/// </summary>
[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAccountService _accountService;

    public AdminController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    /// <summary>
    /// Get all accounts
    /// </summary>
    /// <returns>List of all accounts</returns>
    /// <response code="200">Returns all accounts</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized (admin only)</response>
    [HttpGet("accounts")]
    [ProducesResponseType(typeof(IEnumerable<AccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllAccounts()
    {
        var accounts = await _accountService.GetAllAsync();
        return Ok(accounts);
    }

    /// <summary>
    /// Get a specific account by ID
    /// </summary>
    /// <param name="id">Account ID</param>
    /// <returns>Account details</returns>
    /// <response code="200">Returns the account</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized (admin only)</response>
    /// <response code="404">Account not found</response>
    [HttpGet("accounts/{id:guid}")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccount(Guid id)
    {
        var account = await _accountService.GetByIdAsync(id);
        if (account == null) return NotFound();
        return Ok(account);
    }

    /// <summary>
    /// Delete an account
    /// </summary>
    /// <param name="id">Account ID to delete</param>
    /// <returns>No content</returns>
    /// <response code="204">Account deleted successfully</response>
    /// <response code="400">Cannot delete account (e.g., has balance)</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="403">Not authorized (admin only)</response>
    /// <response code="404">Account not found</response>
    [HttpDelete("accounts/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAccount(Guid id)
    {
        await _accountService.DeleteAsync(id);
        return NoContent();
    }
}