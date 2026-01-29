using Microsoft.Extensions.Options;
using WeddingManager.Domain.Utils;

namespace WeddingManager.Web.Validation;

public sealed class StripeSettingsValidator : IValidateOptions<StripeSettings>
{
    public ValidateOptionsResult Validate(string? name, StripeSettings options)
    {
        if (string.IsNullOrWhiteSpace(options.SecretKey))
        {
            return ValidateOptionsResult.Fail("Stripe SecretKey is required.");
        }

        if (string.IsNullOrWhiteSpace(options.WebhookSecret))
        {
            return ValidateOptionsResult.Fail("Stripe WebhookSecret is required.");
        }

        if (string.IsNullOrWhiteSpace(options.SuccessUrl))
        {
            return ValidateOptionsResult.Fail("Stripe SuccessUrl is required.");
        }

        if (string.IsNullOrWhiteSpace(options.CancelUrl))
        {
            return ValidateOptionsResult.Fail("Stripe CancelUrl is required.");
        }

        if (string.IsNullOrWhiteSpace(options.PortalReturnUrl))
        {
            return ValidateOptionsResult.Fail("Stripe PortalReturnUrl is required.");
        }

        return ValidateOptionsResult.Success;
    }
}
