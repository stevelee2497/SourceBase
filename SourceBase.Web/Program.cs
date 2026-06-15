using Microsoft.AspNetCore.Components.Authorization;
using Serilog;
using SourceBase.Web;
using SourceBase.Web.Auth;
using SourceBase.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

builder.AddServiceDefaults();

var redisConnection = builder.Configuration.GetConnectionString("RedisConnection");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    var redis = ConnectionMultiplexer.Connect(redisConnection);
    builder.Services.AddDataProtection()
        .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys")
        .SetApplicationName("SourceBase.Web");
}

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<BlazorAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<BlazorAuthStateProvider>());

builder.Services.AddHttpClient("api", client => client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!));
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("api"));
builder.Services.AddScoped<ApiHttpClient>();
builder.Services.AddScoped<NotificationService>();

var appSettings = builder.Configuration.Get<AppSettings>() ?? new AppSettings();
builder.Services.AddSingleton(appSettings);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapDefaultEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AllowAnonymous();

app.Run();
