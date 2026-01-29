using System.Text.Json;
using System.Text.Json.Serialization;
using LodeKennes.Extensions.Scaleway.SecretManager;
using Microsoft.Extensions.Options;
using WeddingManager.Domain.Utils;
using WeddingManager.Web.Json;
using WeddingManager.Web.Validation;

namespace WeddingManager.Web.Extensions;

public static class ConfigurationExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new BoolStringJsonConverter() }
    };

    public static IConfiguration ConfigureAppConfiguration(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddScalewayCliSecrets(options =>
            {
                options.ProjectId = Guid.Parse(Environment.GetEnvironmentVariable("SCW_DEFAULT_PROJECT_ID") ?? throw new InvalidOperationException("No default project ID found in environment variables"));
                options.EnableCaching(TimeSpan.FromMinutes(15));
                options.UseCredentials(
                    Environment.GetEnvironmentVariable("SCW_SECRET_KEY") ?? throw new InvalidOperationException("SCW_SECRET_KEY not found"),
                    Environment.GetEnvironmentVariable("SCW_REGION") ?? "fr-par",
                    Environment.GetEnvironmentVariable("SCW_DEFAULT_ORGANIZATION_ID") ?? throw new InvalidOperationException("SCW_DEFAULT_ORGANIZATION_ID not found")
                );            
            });
        }

        return builder.Configuration;
    }

    extension(IServiceCollection services)
    {
        public IServiceCollection AddDatabaseSettings(IConfiguration configuration,
            bool isDevelopment)
        {
            if (isDevelopment)
            {
                services.Configure<DatabaseSettings>(configuration.GetSection("DatabaseSettings"));
            }
            else
            {
                services.Configure<DatabaseSettings>(options =>
                {
                    options.ConnectionString = configuration["DatabaseSettings__ConnectionString"] ?? throw new InvalidOperationException("No database connection string provided");
                });
            }

            services.PostConfigure<DatabaseSettings>(options =>
            {
                if (string.IsNullOrWhiteSpace(options.ConnectionString))
                {
                    throw new InvalidOperationException("No database connection string provided");
                }
            });

            return services;
        }

        public IServiceCollection AddSmtpSettings(IConfiguration configuration, bool isDevelopment)
        {
            if (isDevelopment)
            {
                services.Configure<SmtpSettings>(configuration.GetSection(nameof(SmtpSettings)));
            }
            else
            {
                var json = configuration[nameof(SmtpSettings)]
                           ?? throw new InvalidOperationException("No SMTP settings provided");

                var smtpSettings = JsonSerializer.Deserialize<SmtpSettings>(
                    json,
                    JsonOptions
                ) ?? throw new InvalidOperationException("Invalid SMTP settings JSON");
                
                services.Configure<SmtpSettings>(options =>
                {
                    options.Host = smtpSettings.Host;
                    options.Port = smtpSettings.Port;
                    options.UseSsl = smtpSettings.UseSsl;
                    options.Username = smtpSettings.Username;
                    options.Password = smtpSettings.Password;
                    options.FromAddress = smtpSettings.FromAddress;
                });
            }
            services.AddSingleton<IValidateOptions<SmtpSettings>, SmtpSettingsValidator>();

            return services;
        }
        
        public IServiceCollection AddJwtSettings(IConfiguration configuration, bool isDevelopment)
        {
            if (isDevelopment)
            {
                services.Configure<JwtSettings>(configuration.GetSection(nameof(JwtSettings)));
            }
            else
            {
                var json = configuration[nameof(JwtSettings)] ?? throw new InvalidOperationException("No JWT settings provided");

                var jwtSettings = JsonSerializer.Deserialize<JwtSettings>(
                    json,
                    JsonOptions
                    ) ?? throw new InvalidOperationException("Invalid JWT settings JSON");

                services.Configure<JwtSettings>(options =>
                {
                    options.Key = jwtSettings.Key;
                    options.Issuer = jwtSettings.Issuer;
                    options.Audience = jwtSettings.Audience;
                    options.ExpireDays = jwtSettings.ExpireDays;
                });
            }
            services.AddSingleton<IValidateOptions<JwtSettings>, JwtSettingsValidator>();
            return services;
        }

        public IServiceCollection AddStripeSettings(IConfiguration configuration, bool isDevelopment)
        {
            if (isDevelopment)
            {
                services.Configure<StripeSettings>(configuration.GetSection(nameof(StripeSettings)));
            }
            else
            {
                var json = configuration[nameof(StripeSettings)];
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var stripeSettings = JsonSerializer.Deserialize<StripeSettings>(
                        json,
                        JsonOptions
                    ) ?? throw new InvalidOperationException("Invalid Stripe settings JSON");

                    services.Configure<StripeSettings>(options =>
                    {
                        options.SecretKey = stripeSettings.SecretKey;
                        options.WebhookSecret = stripeSettings.WebhookSecret;
                        options.SuccessUrl = stripeSettings.SuccessUrl;
                        options.CancelUrl = stripeSettings.CancelUrl;
                        options.PortalReturnUrl = stripeSettings.PortalReturnUrl;
                        options.Prices = stripeSettings.Prices;
                    });
                }
                else
                {
                    services.Configure<StripeSettings>(options =>
                    {
                        options.SecretKey = configuration["StripeSettings__SecretKey"] ?? string.Empty;
                        options.WebhookSecret = configuration["StripeSettings__WebhookSecret"] ?? string.Empty;
                        options.SuccessUrl = configuration["StripeSettings__SuccessUrl"] ?? string.Empty;
                        options.CancelUrl = configuration["StripeSettings__CancelUrl"] ?? string.Empty;
                        options.PortalReturnUrl = configuration["StripeSettings__PortalReturnUrl"] ?? string.Empty;

                        var pricesJson = configuration["StripeSettings__PricesJson"];
                        if (!string.IsNullOrWhiteSpace(pricesJson))
                        {
                            options.Prices = JsonSerializer.Deserialize<Dictionary<string, StripePriceOptions>>(
                                                 pricesJson,
                                                 JsonOptions
                                             ) ?? new Dictionary<string, StripePriceOptions>();
                        }
                    });
                }
            }

            services.AddSingleton<IValidateOptions<StripeSettings>, StripeSettingsValidator>();
            return services;
        }

        public IServiceCollection AddCorsPolicy(IConfiguration configuration, bool isDevelopment)
        {
            // Only add CORS in development or if explicitly configured
            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

            if (isDevelopment || allowedOrigins is { Length: > 0 })
            {
                services.AddCors(options =>
                {
                    options.AddPolicy("AllowFrontend", policy =>
                    {
                        var origins = allowedOrigins ?? ["http://localhost:5173"];

                        policy.WithOrigins(origins)
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    });
                });
            }

            return services;
        }

        public IServiceCollection AddFrontendSettings(IConfiguration configuration, bool isDevelopment)
        {
            if (isDevelopment)
            {
                services.Configure<FrontendSettings>(configuration.GetSection("Frontend"));
            }
            else
            {
                services.Configure<FrontendSettings>(options =>
                {
                    options.BaseUrl = configuration["Frontend__BaseUrl"] ?? throw new InvalidOperationException("No frontend base URL provided");
                });
            }

            services.PostConfigure<FrontendSettings>(options =>
            {
                if (string.IsNullOrWhiteSpace(options.BaseUrl))
                {
                    throw new InvalidOperationException("No database connection string provided");
                }
            });

            return services;
        }

        public IServiceCollection AddSubscriptionPlans(IConfiguration configuration)
        {
            services.Configure<SubscriptionPlanOptions>(configuration.GetSection("SubscriptionPlans"));
            return services;
        }
    }
}
