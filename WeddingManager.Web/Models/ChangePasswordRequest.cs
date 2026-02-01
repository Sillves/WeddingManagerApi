namespace WeddingManager.Web.Models;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
