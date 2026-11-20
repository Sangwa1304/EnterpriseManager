namespace EnterpriseManager.Shared.Api;

public sealed record ApiEnvelope<T>(T? Data, ApiError? Error)
{
    public static ApiEnvelope<T> Success(T data) => new(data, null);
    public static ApiEnvelope<T> Failure(string code, string message) =>
        new(default, new ApiError(code, message));
}

public sealed record ApiError(string Code, string Message);
