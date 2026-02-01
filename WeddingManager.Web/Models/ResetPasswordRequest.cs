namespace WeddingManager.Web.Models;

public record ResetPasswordRequest(string Email, string Token, string NewPassword);
