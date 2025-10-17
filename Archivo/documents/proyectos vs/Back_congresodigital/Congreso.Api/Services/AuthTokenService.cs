using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Congreso.Api.Services;

public interface IAuthTokenService
{
    (string token, DateTime exp) CreateToken(Guid id, string email, string[]? roles, int roleLevel, string? fullName = null);
}

public class AuthTokenService : IAuthTokenService
{
    private readonly IConfiguration _cfg;

    public AuthTokenService(IConfiguration cfg)
    {
        _cfg = cfg;
    }

    public (string, DateTime) CreateToken(Guid id, string email, string[]? roles, int roleLevel, string? fullName = null)
    {
        var jwt = _cfg.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var exp = DateTime.UtcNow.AddMinutes(int.TryParse(jwt["AccessTokenMinutes"], out var m) ? m : 120);
        
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, id.ToString()),
            new(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email, email),
            new(System.Security.Claims.ClaimTypes.NameIdentifier, id.ToString()),
            new("roleLevel", roleLevel.ToString())
        };
        
        // Agregar roles como claims múltiples
        if (roles != null && roles.Length > 0)
        {
            foreach (var role in roles)
            {
                if (!string.IsNullOrWhiteSpace(role))
                {
                    claims.Add(new(ClaimTypes.Role, role));
                }
            }
            
            // Agregar claim personalizado "roles" como JSON array para el frontend
            claims.Add(new("roles", string.Join(",", roles.Where(r => !string.IsNullOrWhiteSpace(r)))));
        }
        
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            claims.Add(new(System.Security.Claims.ClaimTypes.Name, fullName));
        }
        
        var tok = new JwtSecurityToken(
            jwt["Issuer"],
            jwt["Audience"],
            claims,
            expires: exp,
            signingCredentials: cred);
        
        return (new JwtSecurityTokenHandler().WriteToken(tok), exp);
    }
}