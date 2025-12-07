using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using MiniCache.Core;
using MiniCache.Server.Application;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MiniCache API",
        Version = "v1",
        Description = "Lightweight HTTP cache that stores binary payloads with TTL metadata."
    });
});

builder.Services.AddSingleton<ICache, InMemoryCache>();
builder.Services.AddSingleton<ICacheManager, CacheManager>();

var app = builder.Build();

app.UseExceptionHandler("/error");
app.UseHttpsRedirection();
app.UseStatusCodePages();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "MiniCache API v1");
    options.RoutePrefix = string.Empty;
});

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();
