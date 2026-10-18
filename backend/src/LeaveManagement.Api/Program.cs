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
builder.Services.AddEmployeeFeature();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddDbContextCheck<LeaveManagementDbContext>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHealthChecks("/health");
app.MapEmployeeEndpoints();

app.Run();
