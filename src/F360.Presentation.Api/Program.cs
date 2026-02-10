using F360.Api;
using F360.Api.Middleware;
using FluentValidation.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

DependencyInjection.AddCustomSerilog(builder.Configuration);

builder.Services
    .AddConfigureOptions(builder.Configuration)
    .AddMongoDb(builder.Configuration)
    .AddRepositories()
    .AddUseCases()
    .AddFluentValidation()
    .AddHealthChecks(builder.Configuration)
    .AddPresentationApi();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
