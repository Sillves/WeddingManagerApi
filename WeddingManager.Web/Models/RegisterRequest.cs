namespace WeddingManager.Web.Models;

public record RegisterRequest(string Email, string FirstName, string LastName, string Password);