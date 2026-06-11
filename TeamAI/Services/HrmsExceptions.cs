namespace TeamAI.Services;

/// <summary>HRMS rejected the bearer token (401). Controller maps this to 401 hrms_unauthorized.</summary>
public sealed class HrmsUnauthorizedException : Exception
{
    public HrmsUnauthorizedException(string message) : base(message) { }
}

/// <summary>HRMS was unreachable, timed out, or returned 5xx. Controller maps this to 502 hrms_unavailable.</summary>
public sealed class HrmsUnavailableException : Exception
{
    public HrmsUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}
