using SourceBase.Api;
using SourceBase.Api.Middlewares;
using SourceBase.Application;
using SourceBase.Application.Shared;
using SourceBase.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);


builder.AddServiceDefaults();
builder.AddSeriLog();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwagger();
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMvcConfigs();
builder.Services.AddAppSettings(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCorsPolicies(builder.Configuration);
builder.Services.AddRateLimiting();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
    app.EnsureDatabaseMigrated();
}

app.UseForwardedHeaders();
app.UseGlobalException();
app.UseSeriLog();
app.UseCors(Constants.CorsCustomPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCustomAuthorization();
app.MapMinimalApi();
app.MapSignalR();
app.MapDefaultEndpoints();

app.Run();
