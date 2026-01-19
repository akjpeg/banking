using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

/// <summary>
/// Request to transfer funds
/// </summary>
public record TransferToRequest
{
    /// <summary>
    /// Destination account number
    /// </summary>
    /// <example>654321</example>
    [Required]
    public required string ToAccountNumber { get; init; }

    /// <summary>
    /// Amount to transfer (must be positive)
    /// </summary>
    /// <example>25.00</example>
    [Required, Range(0.01, double.MaxValue)]
    public decimal Amount { get; init; }
}