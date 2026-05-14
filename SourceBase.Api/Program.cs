using SourceBase.Api.Domain.Entities;
using SourceBase.Api.Extensions;
using SourceBase.Api.Filters;
using SourceBase.Api.Common;
using SourceBase.Api.Infrastructure.DbContexts;
using SourceBase.Api.Infrastructure.Helpers;
using SourceBase.Api.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.UseSeriLog();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwagger();
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMvcConfigs();
builder.Services.AddAppSettings(builder.Configuration);
builder.Services.AddDbContext<ApplicationDbContext>();
builder.Services.AddIdentityApiEndpoints<ApplicationUser>().AddRoles<ApplicationRole>().AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<SendGridEmailHelper>();
builder.Services.AddCorsPolicies(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}
app.UseMiddleware<ExceptionFilter>();
app.UseCors(Constants.CorsCustomPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI();
app.UseSeeding();
app.UseMinimalApi();

app.Run();
