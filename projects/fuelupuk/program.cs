using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

var allowed = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
var csvPath = builder.Configuration["CsvPath"] ?? "notify.csv";
var apiKey  = builder.Configuration["ApiKey"] ?? "";

// CORS
builder.Services.AddCors(o => o.AddPolicy("cors", p =>
    p.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().WithOrigins(allowed)));

var app = builder.Build();
app.UseCors("cors");

record Payload(string? email, string? hp, string? ua, string? referrer, string? origin);
bool LooksLikeEmail(string e)
{
  if (string.IsNullOrWhiteSpace(e)) return false;
  return Regex.IsMatch(e, @"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$", RegexOptions.IgnoreCase);
}

app.MapPost("/api/notify", async (HttpContext ctx) =>
{
    if (!string.IsNullOrWhiteSpace(apiKey) &&
        (!ctx.Request.Headers.TryGetValue("x-api-key", out var key) || key != apiKey))
      return Results.Json(new { ok=false, error="Unauthorized" }, statusCode:(int)HttpStatusCode.Unauthorized);

    using var sr = new StreamReader(ctx.Request.Body, Encoding.UTF8);
    var body = await sr.ReadToEndAsync();
    var p = JsonSerializer.Deserialize<Payload>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    var email = (p?.email ?? "").Trim();
    var hp    = (p?.hp ?? "").Trim();
    if (!string.IsNullOrEmpty(hp))             return Results.Json(new { ok=false, error="Bot" }, statusCode:400);
    if (string.IsNullOrEmpty(email) || !LooksLikeEmail(email) || email.Length > 120)
                                              return Results.Json(new { ok=false, error="Invalid email" }, statusCode:400);

    Directory.CreateDirectory(Path.GetDirectoryName(csvPath)!);
    if (!File.Exists(csvPath))
      await File.WriteAllTextAsync(csvPath, "timestamp,email,ua,ref,ip\n", Encoding.UTF8);

    var ua  = (p?.ua ?? "").Replace("\"","\"\"");
    var rf  = (p?.referrer ?? p?.origin ?? "").Replace("\"","\"\"");
    var ip  = ctx.Connection.RemoteIpAddress?.ToString() ?? "";
    var ts  = DateTime.UtcNow.ToString("o");
    var row = $"\"{ts}\",\"{email.Replace("\"","\"")}\",\"{ua}\",\"{rf}\",\"{ip}\"\n";
    await File.AppendAllTextAsync(csvPath, row, Encoding.UTF8);

    return Results.Json(new { ok=true });
});

app.Run();