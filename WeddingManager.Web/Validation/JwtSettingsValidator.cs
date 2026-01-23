using Microsoft.Extensions.Options;
using WeddingManager.Domain.Utils;

namespace WeddingManager.Web.Validation;

public sealed class JwtSettingsValidator : IValidateOptions<JwtSettings>
{
    public ValidateOptionsResult Validate(string? name, JwtSettings options)
    {
        if (!string.IsNullOrEmpty(name) && name != Options.DefaultName)
        {
            return ValidateOptionsResult.Skip;
        }

        if (string.IsNullOrWhiteSpace(options.Key))
        {
            return ValidateOptionsResult.Fail("No JWT key provided");
        }

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            return ValidateOptionsResult.Fail("No JWT issuer provided");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            return ValidateOptionsResult.Fail("No JWT audience provided");
        }

        if (options.ExpireDays <= 0)
        {
            return ValidateOptionsResult.Fail("No JWT expiration days provided");
        }

        return ValidateOptionsResult.Success;
    }
}
