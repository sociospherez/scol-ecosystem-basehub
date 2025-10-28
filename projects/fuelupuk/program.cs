using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace FuelUp.Notify;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var allowed = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        var csvPath = builder.Configuration["CsvPath"] ?? "notify.csv";
        var apiKey  = builder.Configuration["ApiKey"] ?? "";

        // CORS
        builder.Services.AddCors(o => o.AddPolicy("cors", p =>
            p.WithOrigins(allowed.Length == 0 ? new[] { "*" } : allowed)
             .AllowAnyHeader()
             .AllowAnyMethod()
             .SetIsOriginAllowed(_ => true) // keep simple for MVP
        ));

        var app = builder.Build();
        app.UseCors("cors");
        // Resolve a safe absolute csv path
string ResolveCsvPath(string? configured, IWebHostEnvironment env)
{
    if (!string.IsNullOrWhiteSpace(configured))
        return configured;

    // default to /data/notify.csv under the app content root
    var dir = Path.Combine(env.ContentRootPath, "data");
    Directory.CreateDirectory(dir);
    return Path.Combine(dir, "notify.csv");
}

var csvFullPath = ResolveCsvPath(csvPath, app.Environment);

app.MapGet("/api/ping", () => Results.Json(new { ok = true })); // quick health check

app.MapPost("/api/notify", async (HttpContext ctx) =>
{
    try
    {
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            if (!ctx.Request.Headers.TryGetValue("x-api-key", out var key) || key != apiKey)
                return Results.Json(new { ok = false, error = "Unauthorized" }, statusCode: (int)HttpStatusCode.Unauthorized);
        }

        using var sr = new StreamReader(ctx.Request.Body, Encoding.UTF8);
        var body = await sr.ReadToEndAsync();

        Payload? p;
        try
        {
            p = JsonSerializer.Deserialize<Payload>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return Results.Json(new { ok = false, error = "Bad JSON" }, statusCode: 400);
        }

        var email = (p?.email ?? "").Trim();
        var hp    = (p?.hp ?? "").Trim();

        if (!string.IsNullOrEmpty(hp))
            return Results.Json(new { ok = false, error = "Bot" }, statusCode: 400);

        if (string.IsNullOrEmpty(email) || !Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$", RegexOptions.IgnoreCase) || email.Length > 120)
            return Results.Json(new { ok = false, error = "Invalid email" }, statusCode: 400);

        // Ensure directory exists for the final path
        var dir = Path.GetDirectoryName(csvFullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // Create header if new
        if (!File.Exists(csvFullPath))
            await File.WriteAllTextAsync(csvFullPath, "timestamp,email,ua,ref,ip\n", Encoding.UTF8);

        var ua  = (p?.ua ?? "").Replace("\"", "\"\"");
        var rf  = (p?.referrer ?? p?.origin ?? "").Replace("\"", "\"\"");
        var ip  = ctx.Connection.RemoteIpAddress?.ToString() ?? "";
        var ts  = DateTime.UtcNow.ToString("o");
        var row = $"\"{ts}\",\"{email.Replace("\"", "\"")}\",\"{ua}\",\"{rf}\",\"{ip}\"\n";

        await File.AppendAllTextAsync(csvFullPath, row, Encoding.UTF8);

        return Results.Json(new { ok = true });
    }
    catch (Exception ex)
    {
        // Return a safe error; details will go to stdout log
        return Results.Json(new { ok = false, error = "Server error" }, statusCode: 500);
    }
});

        app.Run();
    }

    private static bool LooksLikeEmail(string e) =>
        Regex.IsMatch(e, @"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$", RegexOptions.IgnoreCase);
}

// Keep the record as a separate type (not inside top-level statements)
public record Payload(string? email, string? hp, string? ua, string? referrer, string? origin);
