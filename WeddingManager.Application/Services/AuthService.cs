using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;
using WeddingManager.Domain.Utils;

namespace WeddingManager.Application.Services;

public class AuthService(UserManager<User> userManager, IOptions<JwtSettings> jwtOptions, ILogger<AuthService> logger) : IAuthService
{

    public async Task<Result<AuthResult>> RegisterAsync(string email, string firstName, string lastName, string password)
    {
        var user = new User
        {
            Email = email,
            UserName = email,
            FirstName = firstName,
            LastName = lastName
        };

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var error = result.Errors.FirstOrDefault()?.Description ?? "Registration failed";
            return Result<AuthResult>.Fail(new Error(ErrorCodes.Validation, error));
        }

        var token = GenerateJwtToken(user);
        return Result<AuthResult>.Ok(new AuthResult
        {
            Success = true,
            Message = "Registered successfully",
            Token = token
        });
    }

    public async Task<Result<AuthResult>> LoginAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null || !await userManager.CheckPasswordAsync(user, password))
        {
            return Result<AuthResult>.Fail(new Error(ErrorCodes.Unauthorized, "Invalid credentials"));
        }

        var token = GenerateJwtToken(user);
        return Result<AuthResult>.Ok(new AuthResult
        {
            Success = true,
            Message = "Login successful",
            Token = token
        });
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = jwtOptions.Value;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddDays(jwtSettings.ExpireDays);

        var token = new JwtSecurityToken(
            jwtSettings.Issuer,
            jwtSettings.Audience,
            claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
