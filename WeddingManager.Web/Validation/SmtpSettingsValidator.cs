using Microsoft.Extensions.Options;
using WeddingManager.Domain.Utils;

namespace WeddingManager.Web.Validation;

public sealed class SmtpSettingsValidator : IValidateOptions<SmtpSettings>
{
    public ValidateOptionsResult Validate(string? name, SmtpSettings options)
    {
        if (!string.IsNullOrEmpty(name) && name != Options.DefaultName)
        {
            return ValidateOptionsResult.Skip;
        }

        if (string.IsNullOrWhiteSpace(options.Host))
        {
            return ValidateOptionsResult.Fail("No SMTP host provided");
        }

        if (options.Port <= 0)
        {
            return ValidateOptionsResult.Fail("No SMTP port provided");
        }

        if (string.IsNullOrWhiteSpace(options.FromAddress))
        {
            return ValidateOptionsResult.Fail("No SMTP from address provided");
        }

        if (string.IsNullOrWhiteSpace(options.Username))
        {
            return ValidateOptionsResult.Fail("No SMTP username provided");
        }

        if (string.IsNullOrWhiteSpace(options.Password))
        {
            return ValidateOptionsResult.Fail("No SMTP password provided");
        }

        return ValidateOptionsResult.Success;
    }
}
