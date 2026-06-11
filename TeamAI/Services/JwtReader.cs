using System.Text;
using System.Text.Json;
using TeamAI.Models.Api;

namespace TeamAI.Services;

/// <summary>
/// Reads display claims out of the HRMS JWT. This only decodes the payload to surface the
/// user's own name/email/id for the UI greeting — it does NOT validate the signature (the
/// token is server-trusted config) and never exposes the token or privileged claims.
/// </summary>
public static class JwtReader
{
    public static UserProfile ReadProfile(string? token)
    {
        var payload = DecodePayload(token);
        if (payload is null)
            return new UserProfile(null, null, null);

        return new UserProfile(
            Name: GetString(payload.Value, "unique_name"),
            Email: GetString(payload.Value, "email"),
            EmployeeId: GetString(payload.Value, "EmployeeId"));
    }

    /// <summary>Reads the integer CompanyId claim from the token, or null if absent/unreadable.</summary>
    public static int? ReadCompanyId(string? token)
    {
        var payload = DecodePayload(token);
        if (payload is null) return null;

        var raw = GetString(payload.Value, "CompanyId");
        return int.TryParse(raw, out var id) ? id : null;
    }

    private static JsonElement? DecodePayload(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var parts = token.Split('.');
        if (parts.Length < 2) return null;

        try
        {
            var json = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            // Clone so the JsonDocument can be disposed while the element stays usable.
            return JsonDocument.Parse(json).RootElement.Clone();
        }
        catch
        {
            return null;
        }
    }

    private static string? GetString(JsonElement obj, string name) =>
        obj.ValueKind == JsonValueKind.Object
        && obj.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}
