using SourceBase.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddSeriLog();
builder.Services.AddCompression();
builder.Services.AddBlazorOptions();
builder.Services.AddSignalROptions();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorizationCore();
builder.Services.AddAppSettings(builder.Configuration);
builder.Services.AddDependencyInjection();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
    app.UseResponseCompression();
}
app.UseStaticFilesWithCache();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AllowAnonymous();

app.Run();
