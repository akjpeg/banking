using Shared.Contracts;

namespace Api.Interfaces;

public interface ITokenService
{
    string GenerateToken(AccountDto account);
}