// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Connect;

namespace UnityEditor.Marketplace;

interface IMarketplaceEditorApiClient
{
    /// <summary>
    /// Resolves a UPM technical name to product info. Returns null when the service has no product
    /// for the name, which covers both a package outside the partner whitelist and one with no
    /// published version.
    /// </summary>
    Task<MarketplacePackageInfo> GetPackageInfoAsync(string technicalName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Claims an entitlement, and doubles as the poll for one still processing: the endpoint is
    /// idempotent, so calling again with the same arguments returns the current grant status.
    /// </summary>
    Task<EntitlementClaimResult> ClaimEntitlementAsync(EntitlementClaimArgs args, CancellationToken cancellationToken = default);
}

class MarketplaceEditorApiClient : IMarketplaceEditorApiClient
{
    const string k_ProductsPath = "/api/marketplace/products/editor/v1";
    const string k_EntitlementsPath = "/api/marketplace/entitlements/editor/v1";
    const int k_NotFoundResponseCode = 404;

    readonly string m_BaseUrl;
    readonly IMarketplaceWebRequestSender m_RequestSender;
    readonly IServiceTokenProvider m_TokenProvider;

    public MarketplaceEditorApiClient()
        : this(
            UnityConnect.instance.GetConfigurationURL(CloudConfigUrl.ServicesGateway),
            new MarketplaceWebRequestSender(),
            new ServiceTokenProvider())
    {
    }

    public MarketplaceEditorApiClient(string baseUrl, IMarketplaceWebRequestSender requestSender, IServiceTokenProvider tokenProvider)
    {
        m_BaseUrl = baseUrl;
        m_RequestSender = requestSender;
        m_TokenProvider = tokenProvider;
    }

    public async Task<MarketplacePackageInfo> GetPackageInfoAsync(string technicalName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(technicalName))
            throw new ArgumentException("A technical name is required.", nameof(technicalName));

        var url = m_BaseUrl + k_ProductsPath + "/packages/" + Uri.EscapeDataString(technicalName);
        var token = await m_TokenProvider.GetServiceTokenAsync(cancellationToken);
        var response = await m_RequestSender.GetAsync(url, token, cancellationToken);

        if (response.responseCode == k_NotFoundResponseCode)
            return null;

        return MarketplacePackageInfo.FromFields(ReadSuccessBody(response, url));
    }

    public async Task<EntitlementClaimResult> ClaimEntitlementAsync(EntitlementClaimArgs args, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(args.organizationId))
            throw new ArgumentException("An organization id is required to claim an entitlement.", nameof(args));
        if (string.IsNullOrEmpty(args.productId))
            throw new ArgumentException("A product id is required to claim an entitlement.", nameof(args));

        var url = m_BaseUrl + k_EntitlementsPath + "/entitlements/claim/product";
        var body = Json.Serialize(new Dictionary<string, object>
        {
            { "organizationId", args.organizationId },
            { "productId", args.productId },
        });

        var token = await m_TokenProvider.GetServiceTokenAsync(cancellationToken);
        var response = await m_RequestSender.PostJsonAsync(url, body, token, cancellationToken);

        return EntitlementClaimResult.FromFields(ReadSuccessBody(response, url));
    }

    static Dictionary<string, object> ReadSuccessBody(MarketplaceWebResponse response, string url)
    {
        if (response.responseCode < 200 || response.responseCode >= 300)
            throw new MarketplaceApiException(MarketplaceApiError.FromResponse(response));

        var fields = MarketplaceJson.Parse(response.body);
        if (fields == null)
            throw new MarketplaceApiException(MarketplaceApiError.Unparsable(response, url));

        return fields;
    }
}
