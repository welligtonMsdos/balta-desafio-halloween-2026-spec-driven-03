
using LinkShortener.Api.Endpoints;
using LinkShortener.Application;
using LinkShortener.Infrastructure;
using LinkShortener.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddLinkShortenerInfrastructure(builder.Configuration);
builder.Services.AddScoped<LinkService>();

var app = builder.Build();

var dataDirectory = "/data";
Directory.CreateDirectory(dataDirectory);
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LinkShortenerDbContext>();
    dbContext.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "LinkShortener API";
    });
}

app.MapLinkEndpoints();

app.Run();

public partial class Program { }
