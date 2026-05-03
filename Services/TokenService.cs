using AuctionSystem.API.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace AuctionSystem.API.Services;

public interface ITokenService
{
    string GenerateToken(User user);
}

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config) => _config = config;

    public string GenerateToken(User user)
    {
        var secret  = _config["Jwt:Secret"]!;
        var expires = int.Parse(_config["Jwt:ExpiresHours"] ?? "24");
        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var handler = new JsonWebTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("id",       user.Id.ToString()),
                new Claim("username", user.Username),
                new Claim("email",    user.Email)
            }),
            Expires            = DateTime.UtcNow.AddHours(expires),
            SigningCredentials  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        return handler.CreateToken(descriptor);
    }
}
