namespace WeddingManager.Web.Models;

using WeddingManager.Domain.Enums;

public record RegisterRequest(string Email, string FirstName, string LastName, string Password, AccountType AccountType = AccountType.Couple);