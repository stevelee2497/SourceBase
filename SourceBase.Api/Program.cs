using SourceBase.Api;
using SourceBase.Api.Middlewares;
using SourceBase.Application;
using SourceBase.Application.Shared;
using SourceBase.Infrastructure;
using SourceBase.Infrastructure.Hubs;

var builder = WebApplication.CreateBuilder(args);


builder.AddServiceDefaults();
builder.AddSeriLog();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwagger();
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMvcConfigs();
builder.Services.AddAppSettings(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCorsPolicies(builder.Configuration);
builder.Services.AddFluentValidation(typeof(AssemblyMarker).Assembly);
builder.Services.AddEndpoints(typeof(AssemblyMarker).Assembly);
builder.Services.AddHandlers(typeof(AssemblyMarker).Assembly);

var app = builder.Build();

if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
    app.EnsureDatabaseMigrated();
}

app.UseGlobalException();
app.UseSeriLog();
app.UseCors(Constants.CorsCustomPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();
app.UseCustomAuthorization();
app.UseMinimalApi();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapDefaultEndpoints();

app.Run();
