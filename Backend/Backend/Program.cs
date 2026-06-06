using Backend;
using Backend.Connections;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSingleton<DeviceConnections>();
builder.Services.AddSingleton<DeviceStore>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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
    app.MapOpenApi();
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

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
