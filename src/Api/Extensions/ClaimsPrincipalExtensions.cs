using System.Security.Claims;

namespace Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetAccountId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier);
        
        if (claim is null || !Guid.TryParse(claim.Value, out var accountId))
            throw new UnauthorizedAccessException("Invalid or missing account ID in token");
        
        return accountId;
    }
}