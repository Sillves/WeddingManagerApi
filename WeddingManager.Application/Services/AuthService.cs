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

public class AuthService(
    UserManager<User> userManager,
    IOptions<JwtSettings> jwtOptions,
    IOptions<FrontendSettings> frontendSettings,
    IEmailService emailService,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly FrontendSettings _frontendSettings = frontendSettings.Value;

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

    public async Task<Result> RequestPasswordResetAsync(string email, string language = "en")
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            // Don't reveal that the user doesn't exist for security
            logger.LogInformation("Password reset requested for non-existent email: {Email}", email);
            return Result.Ok();
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink = BuildPasswordResetLink(email, token);

        try
        {
            await emailService.SendPasswordResetAsync(email, resetLink, language);
            logger.LogInformation("Password reset email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send password reset email to {Email}", email);
            return Result.Fail(new Error(ErrorCodes.ExternalFailure, "Failed to send password reset email"));
        }

        return Result.Ok();
    }

    public async Task<Result> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return Result.Fail(new Error(ErrorCodes.NotFound, "User not found."));
        }

        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            var error = result.Errors.FirstOrDefault()?.Description ?? "Failed to reset password";
            logger.LogWarning("Password reset failed for {Email}: {Error}", email, error);
            return Result.Fail(new Error(ErrorCodes.Validation, error));
        }

        logger.LogInformation("Password reset successful for {Email}", email);
        return Result.Ok();
    }

    public async Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return Result.Fail(new Error(ErrorCodes.NotFound, "User not found."));
        }

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            var error = result.Errors.FirstOrDefault()?.Description ?? "Failed to change password";
            logger.LogWarning("Password change failed for user {UserId}: {Error}", userId, error);
            return Result.Fail(new Error(ErrorCodes.Validation, error));
        }

        logger.LogInformation("Password changed successfully for user {UserId}", userId);
        return Result.Ok();
    }

    private string BuildPasswordResetLink(string email, string token)
    {
        var baseUrl = _frontendSettings.BaseUrl.TrimEnd('/');
        var encodedEmail = Uri.EscapeDataString(email);
        var encodedToken = Uri.EscapeDataString(token);
        return $"{baseUrl}/reset-password?email={encodedEmail}&token={encodedToken}";
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
