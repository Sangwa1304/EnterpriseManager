namespace EnterpriseManager.Client.Core.Networking;

public interface IApiClient
{
    Task<TResponse> GetAsync<TResponse>(string relativeUri, CancellationToken cancellationToken = default) where TResponse : notnull;
    Task<TResponse> PostAsync<TRequest, TResponse>(
        string relativeUri,
        TRequest request,
        CancellationToken cancellationToken = default) where TResponse : notnull;
}
