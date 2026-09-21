using Backend.Services;
using Microsoft.Extensions.Configuration;

// Generate the device key for board firmware.

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();

string masterKey = configuration["Device:MasterKey"] ?? string.Empty;

if (masterKey.Length == 0)
{
    Console.WriteLine("Please set the MasterKey in the backend appsettings.");
    return;
}

Console.Write("MAC address: ");
string macAddress = Console.ReadLine() ?? string.Empty;

if (DeviceKey.NormalizeMacAddress(macAddress).Length != 12)
{
    Console.WriteLine("That is not a MAC address.");
    return;
}

Console.WriteLine();
Console.WriteLine($"DeviceKey = {DeviceKey.Derive(masterKey, macAddress)};");
