using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WeddingManager.Application.Services;
using WeddingManager.Domain.Entities;
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

        var result = await service.RegisterAsync("fail@example.com", "Fail", "User", "P@ssword1!");

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

        var result = await service.RegisterAsync("test@example.com", "Test", "User", "P@ssword1!");

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

    private static AuthService CreateService(Mock<UserManager<User>> userManagerMock)
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
        var emailService = new Mock<IEmailService>();
        var logger = new Mock<ILogger<AuthService>>();
        return new AuthService(userManagerMock.Object, jwtOptions, frontendOptions, emailService.Object, logger.Object);
    }

    private static void AssertTokenContains(string token, User user)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.GivenName && c.Value == user.FirstName);
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.FamilyName && c.Value == user.LastName);
    }
}
