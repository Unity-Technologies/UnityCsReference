// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.Marketplace;

enum ClaimStatus
{
    Unknown,
    Granted,
    Processing,
}

readonly struct EntitlementClaimArgs
{
    /// <summary>
    /// The organization ID claiming the entitlement.
    /// </summary>
    public string organizationId { get; }

    /// <summary>
    /// The product ID being claimed.
    /// </summary>
    public string productId { get; }

    public EntitlementClaimArgs(string organizationId, string productId)
    {
        this.organizationId = organizationId;
        this.productId = productId;
    }
}

class EntitlementClaimResult
{
    const string k_GrantedStatus = "granted";
    const string k_ProcessingStatus = "processing";

    /// <summary>
    /// Opaque stable identifier for this claim (base64url of organizationId:productId).
    /// </summary>
    public string id { get; }
    public ClaimStatus status { get; }

    EntitlementClaimResult(string id, ClaimStatus status)
    {
        this.id = id;
        this.status = status;
    }

    public static EntitlementClaimResult FromFields(Dictionary<string, object> fields)
    {
        return new EntitlementClaimResult(
            MarketplaceJson.ReadString(fields, "id"),
            ParseStatus(MarketplaceJson.ReadString(fields, "status")));
    }

    static ClaimStatus ParseStatus(string status)
    {
        switch (status)
        {
            case k_GrantedStatus:
                return ClaimStatus.Granted;

            case k_ProcessingStatus:
                return ClaimStatus.Processing;

            default:
                return ClaimStatus.Unknown;
        }
    }
}

class MarketplacePublisherInfo
{
    /// <summary>
    /// Publisher ID.
    /// </summary>
    public string id { get; }

    /// <summary>
    /// Publisher display name.
    /// </summary>
    public string name { get; }

    MarketplacePublisherInfo(string id, string name)
    {
        this.id = id;
        this.name = name;
    }

    public static MarketplacePublisherInfo FromFields(Dictionary<string, object> fields)
    {
        if (fields == null)
            return null;

        return new MarketplacePublisherInfo(
            MarketplaceJson.ReadString(fields, "id"),
            MarketplaceJson.ReadString(fields, "name"));
    }
}

class MarketplaceLicenseInfo
{
    /// <summary>
    /// License ID.
    /// </summary>
    public string id { get; }

    /// <summary>
    /// License display name.
    /// </summary>
    public string name { get; }

    /// <summary>
    /// URL to the full license text. Optional.
    /// </summary>
    public string url { get; }

    /// <summary>
    /// Inline license terms. Optional.
    /// </summary>
    public string terms { get; }

    MarketplaceLicenseInfo(string id, string name, string url, string terms)
    {
        this.id = id;
        this.name = name;
        this.url = url;
        this.terms = terms;
    }

    public static MarketplaceLicenseInfo FromFields(Dictionary<string, object> fields)
    {
        if (fields == null)
            return null;

        return new MarketplaceLicenseInfo(
            MarketplaceJson.ReadString(fields, "id"),
            MarketplaceJson.ReadString(fields, "name"),
            MarketplaceJson.ReadString(fields, "url"),
            MarketplaceJson.ReadString(fields, "terms"));
    }
}

class MarketplaceEntitlementInfo
{
    /// <summary>
    /// Whether the authenticated user is entitled to the product. Optional. Null when the
    /// check could not be performed, which is not the same as not being entitled. Callers
    /// must treat this as a tri-state and consult <see cref="error"/>.
    /// </summary>
    public bool? hasEntitlement { get; }

    /// <summary>
    /// Error message when entitlement check could not be performed (e.g. not authenticated).
    /// Optional.
    /// </summary>
    public string error { get; }

    MarketplaceEntitlementInfo(bool? hasEntitlement, string error)
    {
        this.hasEntitlement = hasEntitlement;
        this.error = error;
    }

    public static MarketplaceEntitlementInfo FromFields(Dictionary<string, object> fields)
    {
        if (fields == null)
            return null;

        return new MarketplaceEntitlementInfo(
            MarketplaceJson.ReadNullableBool(fields, "hasEntitlement"),
            MarketplaceJson.ReadString(fields, "error"));
    }
}

class MarketplacePackageInfo
{
    /// <summary>
    /// Marketplace product ID. Where the package is listed under a product, this is that parent
    /// product's ID rather than the one the registry reports for the package itself.
    /// </summary>
    public string productId { get; }

    /// <summary>
    /// Product display name.
    /// </summary>
    public string name { get; }

    /// <summary>
    /// Short product summary.
    /// </summary>
    public string summary { get; }

    public string publisherId { get; }
    public string licenseId { get; }

    /// <summary>
    /// Card thumbnail URL.
    /// </summary>
    public string thumbnail { get; }

    public MarketplacePublisherInfo publisher { get; }
    public MarketplaceLicenseInfo license { get; }
    public MarketplaceEntitlementInfo entitlement { get; }

    MarketplacePackageInfo(
        string productId,
        string name,
        string summary,
        string publisherId,
        string licenseId,
        string thumbnail,
        MarketplacePublisherInfo publisher,
        MarketplaceLicenseInfo license,
        MarketplaceEntitlementInfo entitlement)
    {
        this.productId = productId;
        this.name = name;
        this.summary = summary;
        this.publisherId = publisherId;
        this.licenseId = licenseId;
        this.thumbnail = thumbnail;
        this.publisher = publisher;
        this.license = license;
        this.entitlement = entitlement;
    }

    public static MarketplacePackageInfo FromFields(Dictionary<string, object> fields)
    {
        var expanded = MarketplaceJson.ReadObject(fields, "expanded");

        return new MarketplacePackageInfo(
            MarketplaceJson.ReadString(fields, "productId"),
            MarketplaceJson.ReadString(fields, "name"),
            MarketplaceJson.ReadString(fields, "summary"),
            MarketplaceJson.ReadString(fields, "publisherId"),
            MarketplaceJson.ReadString(fields, "licenseId"),
            MarketplaceJson.ReadString(fields, "thumbnail"),
            MarketplacePublisherInfo.FromFields(MarketplaceJson.ReadObject(expanded, "publisher")),
            MarketplaceLicenseInfo.FromFields(MarketplaceJson.ReadObject(expanded, "license")),
            MarketplaceEntitlementInfo.FromFields(MarketplaceJson.ReadObject(expanded, "entitlement")));
    }
}
