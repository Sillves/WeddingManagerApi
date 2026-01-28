namespace WeddingManager.Domain.Exceptions;

public class SubscriptionLimitExceededException(string message) : Exception(message);
