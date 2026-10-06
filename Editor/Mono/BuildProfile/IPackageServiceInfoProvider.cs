// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading.Tasks;
using UnityEditor.Marketplace;

namespace UnityEditor.Build.Profile
{
    interface IPackageServiceInfoProvider
    {
        bool HasPackageInfo(string packageName);
        bool IsAssetStorePackage(string packageName);
        bool? HasMarketplaceEntitlement(string packageName);
        string GetMarketplaceProductId(string packageName);

        Task<MarketplacePackageInfo> GetOrFetchProductInfoAsync(string packageName);

        /// <exception cref="NoOrganizationIdException">No organization ID to claim against.</exception>
        Task<EntitlementClaimResult> ClaimEntitlementAsync(string productId);
    }

    /// <summary>
    /// Thrown when a claim cannot be made because the project has no organization to claim against.
    /// Raised only after waiting: an organization that has not arrived yet is not an absent one.
    /// </summary>
    class NoOrganizationIdException : Exception
    {
        public NoOrganizationIdException(string message) : base(message) { }
    }
}
