namespace TeamAI.Services;

/// <summary>
/// Shared names for the login session. The per-user HRMS JWT is stored as an authentication
/// token inside the encrypted, httpOnly auth cookie — it is never sent to the browser as JS-readable
/// state, and <see cref="HrmsTokenName"/> is the key used to stash and retrieve it.
/// </summary>
public static class AuthConstants
{
    /// <summary>Key under which the HRMS JWT is stored in the auth cookie's properties.</summary>
    public const string HrmsTokenName = "hrms_jwt";
}
