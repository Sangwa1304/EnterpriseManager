using System.Net.Http.Json;

namespace EnterpriseManager.Client.Core.Networking;

public sealed class HttpApiClient(HttpClient httpClient) : IApiClient
{
    private readonly HttpClient _httpClient =
        httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    public async Task<TResponse> GetAsync<TResponse>(
        string relativeUri,
        CancellationToken cancellationToken = default)
    {
        ValidateRelativeUri(relativeUri);

        using var response = await _httpClient.GetAsync(
            relativeUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TResponse>(
                   cancellationToken: cancellationToken)
               ?? throw new InvalidDataException("The API returned an empty response body.");
    }

    public async Task<TResponse> PostAsync<TRequest, TResponse>(
        string relativeUri,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRelativeUri(relativeUri);

        using var response = await _httpClient.PostAsJsonAsync(
            relativeUri, request, cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TResponse>(
                   cancellationToken: cancellationToken)
               ?? throw new InvalidDataException("The API returned an empty response body.");
    }

    private static void ValidateRelativeUri(string relativeUri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativeUri);

        if (Uri.TryCreate(relativeUri, UriKind.Absolute, out _))
            throw new ArgumentException("Only relative API paths are accepted.", nameof(relativeUri));
    }
}
