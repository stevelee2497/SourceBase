using Microsoft.AspNetCore.Components.Authorization;
using SourceBase.Web.Auth;
using SourceBase.Web.Http;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<BlazorAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<BlazorAuthStateProvider>());
builder.Services.AddScoped<AuthHeaderHandler>();

builder.Services.AddHttpClient("auth-api", client => client.BaseAddress = new Uri("http://localhost:3000"));

builder.Services.AddHttpClient("api", client => client.BaseAddress = new Uri("http://localhost:3000"))
    .AddHttpMessageHandler<AuthHeaderHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("api"));

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
