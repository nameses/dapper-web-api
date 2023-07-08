using DapperWebAPI.Context;
using DapperWebAPI.Contracts;
using DapperWebAPI.Repository;
using Microsoft.AspNetCore.Builder;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost";
    options.InstanceName = "local";
});
builder.Services.AddResponseCompression(options => options.EnableForHttps = true);
builder.Services.AddOutputCache(cfg =>
{
    cfg.AddBasePolicy(plc =>
    {
        plc.With(r => r.HttpContext.Request.Path.StartsWithSegments("/api"));
        plc.Expire(TimeSpan.FromSeconds(7));
    });
    cfg.AddPolicy("ShortCache", plc =>
    {
        plc.Expire(TimeSpan.FromSeconds(3));
    });
});
builder.Services.AddSingleton<DapperContext>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();

builder.Services.AddMemoryCache();
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();

app.UseResponseCompression();

app.UseRouting();

app.UseAuthorization();

app.UseOutputCache();

app.MapControllers();

app.Run();