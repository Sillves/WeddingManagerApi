namespace WeddingManager.Web.Models;

public record ForgotPasswordRequest(string Email, string Language = "en");
