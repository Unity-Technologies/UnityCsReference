// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Connect;
using UnityEngine.Networking;

namespace UnityEditor.Marketplace;

readonly struct MarketplaceWebResponse
{
    public long responseCode { get; }
    public string body { get; }

    public MarketplaceWebResponse(long responseCode, string body)
    {
        this.responseCode = responseCode;
        this.body = body;
    }
}

interface IMarketplaceWebRequestSender
{
    Task<MarketplaceWebResponse> GetAsync(string url, string bearerToken, CancellationToken cancellationToken);
    Task<MarketplaceWebResponse> PostJsonAsync(string url, string jsonBody, string bearerToken, CancellationToken cancellationToken);
}

class MarketplaceWebRequestSender : IMarketplaceWebRequestSender
{
    const string k_JsonContentType = "application/json";
    const string k_TransportFailureMessage = "Could not reach the marketplace service: {0}";

    public async Task<MarketplaceWebResponse> GetAsync(string url, string bearerToken, CancellationToken cancellationToken)
    {
        using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
            return await SendAsync(request, bearerToken, cancellationToken);
    }

    public async Task<MarketplaceWebResponse> PostJsonAsync(string url, string jsonBody, string bearerToken, CancellationToken cancellationToken)
    {
        using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
            request.SetRequestHeader("Content-Type", k_JsonContentType);
            return await SendAsync(request, bearerToken, cancellationToken);
        }
    }

    static async Task<MarketplaceWebResponse> SendAsync(UnityWebRequest request, string bearerToken, CancellationToken cancellationToken)
    {
        request.downloadHandler = new DownloadHandlerBuffer();
        request.suppressErrorsToConsole = true;

        // No token when signed out; the product endpoint answers unauthenticated calls.
        if (!string.IsNullOrEmpty(bearerToken))
            request.SetRequestHeader("AUTHORIZATION", "Bearer " + bearerToken);

        await UnityConnectWebRequestUtils.SendWebRequestAsync(request, cancellationToken);

        if (request.result == UnityWebRequest.Result.ConnectionError)
        {
            throw UnityConnectWebRequestUtils.CreateUnityWebRequestException(
                request, string.Format(k_TransportFailureMessage, request.error));
        }

        return new MarketplaceWebResponse(request.responseCode, request.downloadHandler.text);
    }
}
