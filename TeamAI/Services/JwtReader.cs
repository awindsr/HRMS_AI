using System.Text;
using System.Text.Json;

namespace TeamAI.Services;

/// <summary>
/// Reads identity claims out of the HRMS JWT. This only decodes the payload to surface the
/// signed-in user's own ids — it does NOT validate the signature (the token is server-trusted)
/// and never exposes the token or privileged claims. Display details come from
/// <see cref="HrmsProfileService"/>, not from the token.
/// </summary>
public static class JwtReader
{
    /// <summary>Reads the integer CompanyId claim from the token, or null if absent/unreadable.</summary>
    public static int? ReadCompanyId(string? token) => ReadIntClaim(token, "CompanyId");

    /// <summary>
    /// Reads the signed-in user's display/login name from the token, trying the common claim
    /// spellings, or null if none are present.
    /// </summary>
    public static string? ReadUserName(string? token)
    {
        var payload = DecodePayload(token);
        if (payload is null) return null;
        foreach (var name in new[] { "unique_name", "UserName", "name", "given_name" })
        {
            var value = GetString(payload.Value, name);
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }
        return null;
    }

    /// <summary>
    /// Reads the integer EmployeeId claim from the token, or null if absent/unreadable. This is the
    /// signed-in user's OWN employee id — the only id this assistant ever acts on (no team ids).
    /// </summary>
    public static int? ReadEmployeeId(string? token) => ReadIntClaim(token, "EmployeeId");

    private static int? ReadIntClaim(string? token, string name)
    {
        var payload = DecodePayload(token);
        if (payload is null) return null;

        var raw = GetString(payload.Value, name);
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
