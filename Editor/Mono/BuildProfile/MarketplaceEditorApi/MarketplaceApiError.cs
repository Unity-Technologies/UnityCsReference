// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Marketplace;

/// <summary>
/// Problem-details envelope returned by the marketplace editor endpoints. Only <see cref="title"/>
/// and <see cref="status"/> are guaranteed by the schema; the rest may be absent.
/// </summary>
readonly struct MarketplaceApiError
{
    /// <summary>
    /// HTTP status text.
    /// </summary>
    public string title { get; }

    /// <summary>
    /// HTTP status code.
    /// </summary>
    public int status { get; }

    /// <summary>
    /// Application-specific error code. Optional.
    /// </summary>
    public int code { get; }

    /// <summary>
    /// Error type. Optional.
    /// </summary>
    public string type { get; }

    /// <summary>
    /// Human-readable error message. Optional.
    /// </summary>
    public string detail { get; }

    /// <summary>
    /// Request ID for tracing. Optional.
    /// </summary>
    public string requestId { get; }

    public MarketplaceApiError(string title, int status, int code, string type, string detail, string requestId)
    {
        this.title = title;
        this.status = status;
        this.code = code;
        this.type = type;
        this.detail = detail;
        this.requestId = requestId;
    }

    public static MarketplaceApiError FromResponse(MarketplaceWebResponse response)
    {
        var fields = MarketplaceJson.Parse(response.body);
        if (fields == null)
            return new MarketplaceApiError(null, (int)response.responseCode, 0, null, null, null);

        return new MarketplaceApiError(
            MarketplaceJson.ReadString(fields, "title"),
            MarketplaceJson.ReadInt(fields, "status", (int)response.responseCode),
            MarketplaceJson.ReadInt(fields, "code", 0),
            MarketplaceJson.ReadString(fields, "type"),
            MarketplaceJson.ReadString(fields, "detail"),
            MarketplaceJson.ReadString(fields, "requestId"));
    }

    public static MarketplaceApiError Unparsable(MarketplaceWebResponse response, string url)
    {
        return new MarketplaceApiError(
            null,
            (int)response.responseCode,
            0,
            null,
            "Could not parse the response from " + url,
            null);
    }

    public override string ToString()
    {
        var message = !string.IsNullOrEmpty(detail) ? detail : title;
        var identifier = !string.IsNullOrEmpty(requestId) ? " (requestId: " + requestId + ")" : string.Empty;
        return "[" + status + "] " + message + identifier;
    }
}

class MarketplaceApiException : Exception
{
    public MarketplaceApiError error { get; }

    public MarketplaceApiException(MarketplaceApiError error)
        : base(error.ToString())
    {
        this.error = error;
    }
}
