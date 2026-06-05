namespace HrmsAgent.Tools;

/// <summary>Uniform result wrapper so tools handle success/failure without try/catch everywhere.</summary>
public sealed record ApiResult<T>(bool Ok, T? Data, string? ErrorCode, string? ErrorMessage)
{
    public static ApiResult<T> Success(T data) => new(true, data, null, null);
    public static ApiResult<T> Fail(string code, string message) => new(false, default, code, message);
}
