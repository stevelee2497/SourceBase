using Microsoft.AspNetCore.Components.Authorization;
using Serilog;
using SourceBase.Web;
using SourceBase.Web.Auth;
using SourceBase.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

builder.AddServiceDefaults();

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
app.MapRazorComponents<SourceBase.Web.App>()
    .AddInteractiveServerRenderMode()
    .AllowAnonymous();

app.Run();
