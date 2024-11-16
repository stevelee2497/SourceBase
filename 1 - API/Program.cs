using API.Contexts;
using API.Filters;
using API.Interceptors;
using Core.Contexts;
using Core.Entities;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using Services.Todo;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add ASP.NET MVC services to the container.
builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<ExceptionFilter>();
        options.Filters.Add<ModelValidationFilter>(int.MinValue);
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// Add EF Services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer("name=ConnectionStrings:DefaultConnection");
    options.AddInterceptors(new AuditingInterceptor());
});

// Add EF Identity Dependencies
builder.Services.AddIdentityApiEndpoints<UserEntity>().AddEntityFrameworkStores<ApplicationDbContext>();

// Override Identity Authentication Configurations
builder.Services.AddOptions<BearerTokenOptions>(IdentityConstants.BearerScheme).Configure(options =>
{
    options.BearerTokenExpiration = TimeSpan.Parse(builder.Configuration.GetValue<string>("BearerTokenOptions:BearerTokenExpiration") ?? string.Empty);
    options.RefreshTokenExpiration = TimeSpan.Parse(builder.Configuration.GetValue<string>("BearerTokenOptions:RefreshTokenExpiration") ?? string.Empty);
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

app.Run();
