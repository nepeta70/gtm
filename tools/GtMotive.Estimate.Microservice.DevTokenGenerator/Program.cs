using System;
using System.Collections.Generic;
using GtMotive.Estimate.Microservice.Infrastructure.Authorization;

var secret = GetOption(args, "--secret", "-s") ?? Environment.GetEnvironmentVariable("Jwt__Secret");

if (string.IsNullOrWhiteSpace(secret) || HasFlag(args, "--help", "-h"))
{
    PrintUsage();
    return string.IsNullOrWhiteSpace(secret) ? 1 : 0;
}

var subject = GetOption(args, "--subject", "-u") ?? "dev-user";
var issuer = GetOption(args, "--issuer", "-i");
var audience = GetOption(args, "--audience", "-a");
var roles = GetOptionValues(args, "--role", "-r");
var minutesOption = GetOption(args, "--minutes", "-m");
var lifetime = int.TryParse(minutesOption, out var minutes)
    ? TimeSpan.FromMinutes(minutes)
    : TimeSpan.FromHours(1);

var token = DevJwtTokenGenerator.GenerateToken(secret, subject, roles, issuer, audience, lifetime);

Console.WriteLine(token);

return 0;

static string GetOption(string[] arguments, string longName, string shortName)
{
    for (var i = 0; i < arguments.Length - 1; i++)
    {
        if (string.Equals(arguments[i], longName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(arguments[i], shortName, StringComparison.OrdinalIgnoreCase))
        {
            return arguments[i + 1];
        }
    }

    return null;
}

static List<string> GetOptionValues(string[] arguments, string longName, string shortName)
{
    var values = new List<string>();
    for (var i = 0; i < arguments.Length - 1; i++)
    {
        if (string.Equals(arguments[i], longName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(arguments[i], shortName, StringComparison.OrdinalIgnoreCase))
        {
            values.Add(arguments[i + 1]);
        }
    }

    return values;
}

static bool HasFlag(string[] arguments, string longName, string shortName)
{
    return Array.Exists(arguments, argument =>
        string.Equals(argument, longName, StringComparison.OrdinalIgnoreCase)
        || string.Equals(argument, shortName, StringComparison.OrdinalIgnoreCase));
}

static void PrintUsage()
{
    Console.WriteLine("GtMotive.Estimate.Microservice.DevTokenGenerator");
    Console.WriteLine();
    Console.WriteLine("Generates an HS256 JWT for local development / Docker testing.");
    Console.WriteLine("The secret MUST match the API's Jwt:Secret (Jwt__Secret) configuration.");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run -- --secret <value> [--subject <sub>] [--role <role>]... [--issuer <iss>] [--audience <aud>] [--minutes <n>]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  -s, --secret     Symmetric signing secret. Falls back to the Jwt__Secret environment variable.");
    Console.WriteLine("  -u, --subject    Value of the 'sub' claim. Defaults to 'dev-user'.");
    Console.WriteLine("  -r, --role       Role claim to include. Repeatable.");
    Console.WriteLine("  -i, --issuer     Value of the 'iss' claim. Must match Jwt:Issuer if the API validates it.");
    Console.WriteLine("  -a, --audience   Value of the 'aud' claim. Must match Jwt:Audience if the API validates it.");
    Console.WriteLine("  -m, --minutes    Token lifetime in minutes. Defaults to 60.");
    Console.WriteLine();
    Console.WriteLine("Example:");
    Console.WriteLine("  dotnet run --project tools/GtMotive.Estimate.Microservice.DevTokenGenerator -- --secret \"dev-super-secret-key-change-me\" --role Admin");
}
