using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ErrorOr;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace JobScraper.Modules.Auth;

public class LoginDto
{
    public string? Login { get; set; }
    public string? Password { get; set; }
}

public record AuthToken(string Token);

public class AuthHandler
{
    private readonly IConfiguration _config;
    private readonly IPasswordHasher<string> _hasher;

    public AuthHandler(IConfiguration config, IPasswordHasher<string> hasher)
    {
        _config = config;
        _hasher = hasher;
    }

    public async Task<ErrorOr<AuthToken>> Handle(LoginDto request)
    {
        // 1. Mock Database Logic
        var mockUser = new
        {
            Login = "admin",
            PasswordHash = _hasher.HashPassword("admin", "test"),
        };

        if (request.Login != mockUser.Login)
            return Error.Unauthorized(description: "Invalid credentials");

        var result = _hasher.VerifyHashedPassword(request.Login, mockUser.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
            return Error.Unauthorized(description: "Invalid credentials");

        // 3. Generate Token
        var token = GenerateJwtToken(request.Login);
        return new AuthToken(token);
    }

    private string GenerateJwtToken(string username)
    {
        // Use the same key name as defined in your Setup class ("JWT_SIGNING_KEY")
        var secretKey = _config["Jwt:SigningKey"]
         ?? throw new InvalidOperationException("Signing key not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            _config["Jwt:Issuer"], // Ensure these exist in appsettings or remove if not validating
            _config["Jwt:Audience"],
            claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
