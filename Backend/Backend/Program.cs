using Backend;
using Backend.Connections;
using Backend.Models;
using Backend.Repositories;
using Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSingleton<ConnectionMediator>();

builder.Services.AddSingleton<DeviceConnections>();
builder.Services.AddSingleton<DashboardConnections>();
builder.Services.AddSingleton<DeviceStore>();
builder.Services.AddHostedService<ServerTimeSync>();

// Devices sign their telemetry with a key derived from this master key.
builder.Services
    .AddOptions<DeviceOptions>()
    .Bind(builder.Configuration.GetSection(DeviceOptions.SectionName));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IAccountRepository, AccountRepository>();

// Bind the "Jwt" section onto JwtOptions
builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddSingleton<IPasswordHasher<Account>, PasswordHasher<Account>>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();


JwtOptions jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? throw new InvalidDataException(nameof(JwtOptions));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();

if (builder.Environment.IsDevelopment())
{
    // allows connection from other devices under the same network
    builder.WebHost
        .UseUrls("http://0.0.0.0:5000")
        .UseKestrel();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseCors(options => options
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
}
else
{
    app.UseHttpsRedirection();
}

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(10),
    KeepAliveTimeout = TimeSpan.FromSeconds(5)
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
