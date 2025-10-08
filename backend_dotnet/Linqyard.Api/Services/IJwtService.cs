using Linqyard.Entities;
using System.Security.Claims;

namespace Linqyard.Api.Services;

public interface IJwtService
{
    string GenerateToken(User user, Guid? sessionId = null);
    ClaimsPrincipal? ValidateToken(string token);
    int GetUserIdFromToken(string token);
}