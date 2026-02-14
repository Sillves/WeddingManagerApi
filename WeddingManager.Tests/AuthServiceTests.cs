using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WeddingManager.Application.Services;
using WeddingManager.Domain.Entities;
using WeddingManager.Domain.Enums;
using WeddingManager.Domain.Interfaces;
using WeddingManager.Domain.Models;
using WeddingManager.Domain.Utils;

namespace WeddingManager.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_ReturnsValidationWhenCreateFails()
    {
        var userManagerMock = CreateUserManager();
        userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Registration failed" }));
        var service = CreateService(userManagerMock);

        var result = await service.RegisterAsync("fail@example.com", "Fail", "User", "P@ssword1!", AccountType.Couple);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Validation, result.Errors[0].Code);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsTokenAndUserDetails()
    {
        var userManagerMock = CreateUserManager();
        User? captured = null;
        userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .Callback<User, string>((user, _) => captured = user)
            .ReturnsAsync(IdentityResult.Success);
        var service = CreateService(userManagerMock);

        var result = await service.RegisterAsync("test@example.com", "Test", "User", "P@ssword1!", AccountType.Couple);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(captured);
        Assert.Equal("test@example.com", captured!.Email);
        Assert.Equal("Test", captured.FirstName);
        Assert.Equal("User", captured.LastName);
        var token = result.Value!.Token;
        Assert.False(string.IsNullOrWhiteSpace(token));
        AssertTokenContains(token!, captured);
    }

    [Fact]
    public async Task LoginAsync_ReturnsUnauthorizedWhenInvalid()
    {
        var userManagerMock = CreateUserManager();
        userManagerMock.Setup(m => m.FindByEmailAsync("missing@example.com"))
            .ReturnsAsync((User?)null);
        var service = CreateService(userManagerMock);

        var result = await service.LoginAsync("missing@example.com", "bad");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Unauthorized, result.Errors[0].Code);
    }

    [Fact]
    public async Task LoginAsync_ReturnsTokenWhenValid()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "login@example.com",
            FirstName = "Login",
            LastName = "User"
        };
        var userManagerMock = CreateUserManager();
        userManagerMock.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManagerMock.Setup(m => m.CheckPasswordAsync(user, "P@ssword1!")).ReturnsAsync(true);
        var service = CreateService(userManagerMock);

        var result = await service.LoginAsync(user.Email!, "P@ssword1!");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        var token = result.Value!.Token;
        Assert.False(string.IsNullOrWhiteSpace(token));
        AssertTokenContains(token!, user);
    }

    private static Mock<UserManager<User>> CreateUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }

    private static AuthService CreateService(
        Mock<UserManager<User>> userManagerMock,
        Mock<IEmailService>? emailServiceMock = null)
    {
        var jwtSettings = new JwtSettings
        {
            Key = "test_key_that_is_long_enough_for_hs256!",
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpireDays = 7
        };
        var frontendSettings = new FrontendSettings
        {
            BaseUrl = "https://test.example.com"
        };
        var jwtOptions = Options.Create(jwtSettings);
        var frontendOptions = Options.Create(frontendSettings);
        emailServiceMock ??= new Mock<IEmailService>();
        var referralServiceMock = new Mock<IReferralService>();
        var logger = new Mock<ILogger<AuthService>>();
        return new AuthService(userManagerMock.Object, jwtOptions, frontendOptions, emailServiceMock.Object, referralServiceMock.Object, logger.Object);
    }

    [Fact]
    public async Task LoginAsync_ReturnsAccountLockedWhenLockedOut()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "locked@example.com", FirstName = "Locked", LastName = "User" };
        var userManagerMock = CreateUserManager();
        userManagerMock.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManagerMock.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(true);
        var service = CreateService(userManagerMock);

        var result = await service.LoginAsync(user.Email!, "P@ssword1!");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.AccountLocked, result.Errors[0].Code);
    }

    [Fact]
    public async Task LoginAsync_CallsAccessFailedWhenWrongPassword()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "wrong@example.com", FirstName = "Wrong", LastName = "User" };
        var userManagerMock = CreateUserManager();
        userManagerMock.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManagerMock.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        userManagerMock.Setup(m => m.CheckPasswordAsync(user, "bad")).ReturnsAsync(false);
        userManagerMock.Setup(m => m.AccessFailedAsync(user)).ReturnsAsync(IdentityResult.Success);
        var service = CreateService(userManagerMock);

        var result = await service.LoginAsync(user.Email!, "bad");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Unauthorized, result.Errors[0].Code);
        userManagerMock.Verify(m => m.AccessFailedAsync(user), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ResetsAccessFailedCountOnSuccess()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "success@example.com", FirstName = "Good", LastName = "User" };
        var userManagerMock = CreateUserManager();
        userManagerMock.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManagerMock.Setup(m => m.IsLockedOutAsync(user)).ReturnsAsync(false);
        userManagerMock.Setup(m => m.CheckPasswordAsync(user, "P@ssword1!")).ReturnsAsync(true);
        userManagerMock.Setup(m => m.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
        var service = CreateService(userManagerMock);

        var result = await service.LoginAsync(user.Email!, "P@ssword1!");

        Assert.True(result.IsSuccess);
        userManagerMock.Verify(m => m.ResetAccessFailedCountAsync(user), Times.Once);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_ReturnsOkWhenUserNotFound()
    {
        var userManagerMock = CreateUserManager();
        userManagerMock.Setup(m => m.FindByEmailAsync("ghost@example.com")).ReturnsAsync((User?)null);
        var service = CreateService(userManagerMock);

        var result = await service.RequestPasswordResetAsync("ghost@example.com");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_SendsEmailOnSuccess()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "reset@example.com", FirstName = "Reset", LastName = "User" };
        var userManagerMock = CreateUserManager();
        userManagerMock.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManagerMock.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
        var emailServiceMock = new Mock<IEmailService>();
        emailServiceMock.Setup(e => e.SendPasswordResetAsync(user.Email!, It.IsAny<string>(), "en"))
            .Returns(Task.CompletedTask);
        var service = CreateService(userManagerMock, emailServiceMock);

        var result = await service.RequestPasswordResetAsync(user.Email!);

        Assert.True(result.IsSuccess);
        emailServiceMock.Verify(e => e.SendPasswordResetAsync(user.Email!, It.IsAny<string>(), "en"), Times.Once);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_ReturnsExternalFailureOnEmailError()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "fail@example.com", FirstName = "Fail", LastName = "User" };
        var userManagerMock = CreateUserManager();
        userManagerMock.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManagerMock.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
        var emailServiceMock = new Mock<IEmailService>();
        emailServiceMock.Setup(e => e.SendPasswordResetAsync(user.Email!, It.IsAny<string>(), "en"))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));
        var service = CreateService(userManagerMock, emailServiceMock);

        var result = await service.RequestPasswordResetAsync(user.Email!);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.ExternalFailure, result.Errors[0].Code);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsNotFoundWhenUserMissing()
    {
        var userManagerMock = CreateUserManager();
        userManagerMock.Setup(m => m.FindByEmailAsync("ghost@example.com")).ReturnsAsync((User?)null);
        var service = CreateService(userManagerMock);

        var result = await service.ResetPasswordAsync("ghost@example.com", "token", "NewP@ss1!");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Errors[0].Code);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsValidationOnInvalidToken()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "reset@example.com", FirstName = "Reset", LastName = "User" };
        var userManagerMock = CreateUserManager();
        userManagerMock.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManagerMock.Setup(m => m.ResetPasswordAsync(user, "bad-token", "NewP@ss1!"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));
        var service = CreateService(userManagerMock);

        var result = await service.ResetPasswordAsync(user.Email!, "bad-token", "NewP@ss1!");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Validation, result.Errors[0].Code);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsOkOnSuccess()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "reset@example.com", FirstName = "Reset", LastName = "User" };
        var userManagerMock = CreateUserManager();
        userManagerMock.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManagerMock.Setup(m => m.ResetPasswordAsync(user, "valid-token", "NewP@ss1!"))
            .ReturnsAsync(IdentityResult.Success);
        var service = CreateService(userManagerMock);

        var result = await service.ResetPasswordAsync(user.Email!, "valid-token", "NewP@ss1!");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsNotFoundWhenUserMissing()
    {
        var userId = Guid.NewGuid().ToString();
        var userManagerMock = CreateUserManager();
        userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync((User?)null);
        var service = CreateService(userManagerMock);

        var result = await service.ChangePasswordAsync(userId, "OldP@ss1!", "NewP@ss1!");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Errors[0].Code);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsValidationOnWrongCurrentPassword()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "change@example.com", FirstName = "Change", LastName = "User" };
        var userManagerMock = CreateUserManager();
        userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        userManagerMock.Setup(m => m.ChangePasswordAsync(user, "WrongOld!", "NewP@ss1!"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Incorrect password" }));
        var service = CreateService(userManagerMock);

        var result = await service.ChangePasswordAsync(user.Id.ToString(), "WrongOld!", "NewP@ss1!");

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.Validation, result.Errors[0].Code);
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsOkOnSuccess()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "change@example.com", FirstName = "Change", LastName = "User" };
        var userManagerMock = CreateUserManager();
        userManagerMock.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        userManagerMock.Setup(m => m.ChangePasswordAsync(user, "OldP@ss1!", "NewP@ss1!"))
            .ReturnsAsync(IdentityResult.Success);
        var service = CreateService(userManagerMock);

        var result = await service.ChangePasswordAsync(user.Id.ToString(), "OldP@ss1!", "NewP@ss1!");

        Assert.True(result.IsSuccess);
    }

    private static void AssertTokenContains(string token, User user)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.GivenName && c.Value == user.FirstName);
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.FamilyName && c.Value == user.LastName);
        Assert.Contains(jwt.Claims, c => c.Type == "account_type" && c.Value == ((int)user.AccountType).ToString());
    }
}
