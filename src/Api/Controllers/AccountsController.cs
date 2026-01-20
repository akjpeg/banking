using Api.DTOs;
using Api.Extensions;
using Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;
using Shared.Exceptions;

namespace Api.Controllers;

/// <summary>
/// Manages user accounts, authentication, and transactions
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ITransactionRecorder _transactionRecorder;
    private readonly ITokenService _tokenService;

    public AccountsController(
        IAccountService accountService,
        ITransactionRecorder transactionRecorder,
        ITokenService tokenService)
    {
        _accountService = accountService;
        _transactionRecorder = transactionRecorder;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Register a new account
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <returns>Account info with JWT token</returns>
    /// <response code="201">Account created successfully</response>
    /// <response code="400">Invalid data or email already exists</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var account = await _accountService.CreateAsync(
                request.Name,
                request.Email,
                request.Password);
            
            var token = _tokenService.GenerateToken(account);

            return Created($"/api/accounts/me", new LoginResponse(
                token,
                account.Id,
                account.AccountNumber,
                account.Name));
        }
        catch (DomainException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Login to an existing account
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token and account info</returns>
    /// <response code="200">Login successful</response>
    /// <response code="401">Invalid credentials</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var account = await _accountService.LoginAsync(request.Email, request.Password);

        if (account == null)
            return Unauthorized("Invalid email or password");
        
        var token = _tokenService.GenerateToken(account);

        return Ok(new LoginResponse(
            token,
            account.Id,
            account.AccountNumber,
            account.Name));
    }
    
    /// <summary>
    /// Get current user's account details
    /// </summary>
    /// <returns>Account information</returns>
    /// <response code="200">Returns account details</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="404">Account not found</response>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyAccount()
    {
        var accountId = User.GetAccountId();
        var account = await _accountService.GetByIdAsync(accountId);

        if (account == null)
            return NotFound();

        return Ok(account);
    }

    /// <summary>
    /// Get current user's balance
    /// </summary>
    /// <returns>Current balance</returns>
    /// <response code="200">Returns balance</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="404">Account not found</response>
    [Authorize]
    [HttpGet("me/balance")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyBalance()
    {
        var accountId = User.GetAccountId();
        var account = await _accountService.GetByIdAsync(accountId);

        if (account == null)
            return NotFound();

        return Ok(new { account.Balance });
    }

    /// <summary>
    /// Get transaction history
    /// </summary>
    /// <returns>List of transactions</returns>
    /// <response code="200">Returns transactions</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="404">Account not found</response>
    [Authorize]
    [HttpGet("me/transactions")]
    [ProducesResponseType(typeof(IEnumerable<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactions()
    {
        try
        {
            var accountId = User.GetAccountId();
            var transactions = await _accountService.GetAccountTransactionsAsync(accountId);
            return Ok(transactions);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound($"Could not find requested account");
        }
    }
    
    /// <summary>
    /// Deposit funds into account
    /// </summary>
    /// <param name="request">Deposit amount</param>
    /// <returns>Updated account info</returns>
    /// <response code="200">Deposit successful</response>
    /// <response code="400">Invalid amount</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="409">Concurrent update conflict - please retry</response>
    [Authorize]
    [HttpPost("me/deposit")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deposit([FromBody] DepositRequest request)
    {
        try
        {
            var accountId = User.GetAccountId();
            var account = await _accountService.CreditAsync(accountId, request.Amount);
            return Ok(account);
        }
        catch (DomainException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    /// <summary>
    /// Withdraw funds from account
    /// </summary>
    /// <param name="request">Withdrawal amount</param>
    /// <returns>Updated account info</returns>
    /// <response code="200">Withdrawal successful</response>
    /// <response code="400">Invalid amount or insufficient funds</response>
    /// <response code="401">Not authenticated</response>
    /// <response code="409">Concurrent update conflict - please retry</response>
    [Authorize]
    [HttpPost("me/withdraw")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawRequest request)
    {
        try
        {
            var accountId = User.GetAccountId();
            var account = await _accountService.DebitAsync(accountId, request.Amount);
            return Ok(account);
        }
        catch (DomainException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}