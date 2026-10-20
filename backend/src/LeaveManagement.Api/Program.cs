using System.Text.Json.Serialization;
using FluentValidation;
using LeaveManagement.Api.Auth;
using LeaveManagement.Api.Common;
using LeaveManagement.Api.Employees;
using LeaveManagement.Infrastructure;
using LeaveManagement.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("LeaveManagement")
    ?? throw new InvalidOperationException(
        "Connection string 'LeaveManagement' is missing. Set the environment variable " +
        "ConnectionStrings__LeaveManagement (see database/README.md).");

builder.Services.AddInfrastructure(connectionString, seedDevelopmentData: builder.Environment.IsDevelopment());
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddValidatorsFromAssemblyContaining<Program>(includeInternalTypes: true);
builder.Services.AddAuthFeature(builder.Configuration);
builder.Services.AddEmployeeFeature();

// Enums as names ("Hr"), not numbers, in requests and responses.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.AddHealthChecks().AddDbContextCheck<LeaveManagementDbContext>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapEmployeeEndpoints();

app.Run();
