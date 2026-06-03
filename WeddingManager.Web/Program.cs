using Microsoft.EntityFrameworkCore;
using WeddingManager.Application.Extensions;
using WeddingManager.Infrastructure.Data;
using WeddingManager.Infrastructure.Extensions;
using WeddingManager.Web.Endpoints;
using WeddingManager.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

var isDevelopment = builder.Environment.IsDevelopment();
var configuration = builder.ConfigureAppConfiguration();

builder.Services.AddDatabaseSettings(configuration, isDevelopment);
builder.Services.AddSmtpSettings(configuration, isDevelopment);
builder.Services.AddJwtSettings(configuration, isDevelopment);
builder.Services.AddStripeSettings(configuration, isDevelopment);
builder.Services.AddCorsPolicy(configuration, isDevelopment);
builder.Services.AddFrontendSettings(configuration, isDevelopment);
builder.Services.AddSubscriptionPlans(configuration);
builder.Services.AddScalewayStorageSettings(configuration, isDevelopment);

// Add services to the container.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(configuration);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();
builder.Services.AddDataProtection();
builder.Services.AddPublicRateLimiting();

var app = builder.Build();

// Auto-apply EF Core migrations in Development so the containerised API is
// self-sufficient (no host-side `dotnet ef` step needed). Production is
// unaffected and keeps using the explicit migration scripts.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<WeddingDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
    // CORS must be before static files to apply to all requests
    app.UseCors("AllowFrontend");

}

app.UseHttpsRedirection();

app.UsePublicRateLimiting();

app.UseAuthentication();
app.UseAuthorization();

app.MapAllEndpoints(useApiPrefix: app.Environment.IsDevelopment());

app.Run();
