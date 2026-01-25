using WeddingManager.Application.Extensions;
using WeddingManager.Infrastructure.Extensions;
using WeddingManager.Web.Endpoints;
using WeddingManager.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

var isDevelopment = builder.Environment.IsDevelopment();
var configuration = builder.ConfigureAppConfiguration();

builder.Services.AddDatabaseSettings(configuration, isDevelopment);
builder.Services.AddSmtpSettings(configuration, isDevelopment);
builder.Services.AddJwtSettings(configuration, isDevelopment);
builder.Services.AddCorsPolicy(configuration, isDevelopment);
builder.Services.AddFrontendSettings(configuration);

// Add services to the container.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(configuration);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();
builder.Services.AddPublicRateLimiting();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("AllowFrontend");
}

app.UseHttpsRedirection();

app.UsePublicRateLimiting();

app.UseAuthentication();
app.UseAuthorization();

app.MapAllEndpoints(useApiPrefix: app.Environment.IsDevelopment());

app.Run();
