using API.Contexts;
using API.Filters;
using Business.Interfaces;
using Business.Services;
using Core.Constants;
using Core.Contexts;
using Core.Entities;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add ASP.NET MVC services to the container.
builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<ExceptionFilter>(); // Add global exception filter to force all exceptions into our error model
        options.Filters.Add<ModelValidationFilter>(int.MinValue); // Validating json payload and return in error model format
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); // Force to save enum in string format to our database instead of magic numbers
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// Add EF Business
builder.Services.AddDbContext<ApplicationDbContext>();

// Add EF Identity Dependencies
builder.Services.AddIdentityApiEndpoints<UserEntity>()  // Set up Identity managers and stores
    .AddRoles<RoleEntity>()                             // Set up Role-based manager and store
    .AddEntityFrameworkStores<ApplicationDbContext>();  // Attach Identity to our DB context

// Override Identity Authentication Configurations
builder.Services.AddOptions<BearerTokenOptions>(IdentityConstants.BearerScheme).Configure(options =>
{
    options.BearerTokenExpiration = TimeSpan.Parse(builder.Configuration.GetValue<string>(AppSettingKeys.BearerTokenExpiration) ?? string.Empty);
    options.RefreshTokenExpiration = TimeSpan.Parse(builder.Configuration.GetValue<string>(AppSettingKeys.RefreshTokenExpiration) ?? string.Empty);
});

// Add application services
builder.Services.AddScoped<IDbContext, ApplicationDbContext>();
builder.Services.AddScoped<IUserContext, UserContext>();
builder.Services.AddScoped<ITodoService, TodoService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// Configure the HTTP request middlewares pipeline.
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapIdentityApi<IdentityUser>();

// Ensure db migration is applied on start up
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
