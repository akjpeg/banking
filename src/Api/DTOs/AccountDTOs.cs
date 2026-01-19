using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

/// <summary>
/// Request to register a new account
/// </summary>
public record RegisterRequest
{
    /// <summary>
    /// Full name of the account holder
    /// </summary>
    /// <example>John Doe</example>
    [Required, MinLength(2), MaxLength(150)]
    public required string Name { get; init; }

    /// <summary>
    /// Email address (used for login)
    /// </summary>
    /// <example>john.doe@example.com</example>
    [Required, EmailAddress]
    public required string Email { get; init; }

    /// <summary>
    /// Password (minimum 8 characters)
    /// </summary>
    /// <example>SecureP@ss123</example>
    [Required, MinLength(8)]
    public required string Password { get; init; }
}

/// <summary>
/// Request to login
/// </summary>
public record LoginRequest
{
    /// <summary>
    /// Email address
    /// </summary>
    /// <example>john.doe@example.com</example>
    [Required, EmailAddress]
    public required string Email { get; init; }

    /// <summary>
    /// Password
    /// </summary>
    /// <example>SecureP@ss123</example>
    [Required]
    public required string Password { get; init; }
}

/// <summary>
/// Response after successful authentication
/// </summary>
public record LoginResponse(
    string Token,
    Guid AccountId,
    string AccountNumber,
    string Name
);

/// <summary>
/// Request to deposit funds
/// </summary>
public record DepositRequest
{
    /// <summary>
    /// Amount to deposit (must be positive)
    /// </summary>
    /// <example>100.00</example>
    [Required, Range(0.01, double.MaxValue)]
    public decimal Amount { get; init; }
}

/// <summary>
/// Request to withdraw funds
/// </summary>
public record WithdrawRequest
{
    /// <summary>
    /// Amount to withdraw (must be positive)
    /// </summary>
    /// <example>50.00</example>
    [Required, Range(0.01, double.MaxValue)]
    public decimal Amount { get; init; }
}