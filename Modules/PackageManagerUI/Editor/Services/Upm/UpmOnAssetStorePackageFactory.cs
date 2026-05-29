// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal partial class PackageFactory
    {
        private void RegisterEventsForUpmOnAssetStorePackages()
        {
            m_FetchStatusTracker.onSearchInfoFetchStatusChanged += OnSearchInfoFetchStatusChanged;

            m_UpmRegistryClient.onRegistriesModified += OnRegistriesModified;
        }

        private void UnregisterEventsForUpmOnAssetStorePackages()
        {
            m_FetchStatusTracker.onSearchInfoFetchStatusChanged -= OnSearchInfoFetchStatusChanged;

            m_UpmRegistryClient.onRegistriesModified -= OnRegistriesModified;
        }

        private void OnSearchInfoFetchStatusChanged(string packageName)
        {
            GeneratePackagesAndTriggerChangeEvent(new[] { packageName });
        }

        private void OnRegistriesModified()
        {
            m_UpmCache.ClearNonDiscoverableSearchInfos();
            m_UpmCache.ClearExtraInfoCache();
            m_FetchStatusTracker.ClearSearchInfoFetchStatuses();

            var productIds = new List<long>();
            foreach (var p in m_PackageDatabase.allPackages)
                if (p.product != null && p.versions.AnyMatches(v => v.HasTag(PackageTag.UpmFormat)))
                    productIds.Add(p.product.id);
            GeneratePackagesAndTriggerChangeEvent(productIds);
        }

        private IPackage CreateUpmOnAssetStorePackage(long productId, string packageName, AssetStoreProductInfo productInfo, AssetStorePurchaseInfo purchaseInfo, IUpmPackageData packageData, PackagesChangedSource changedSource = PackagesChangedSource.Other)
        {
            if (packageData == null && productInfo == null)
                return null;

            var searchInfoFetchStatus = m_FetchStatusTracker.GetSearchInfoFetchStatus(packageName);
            if ((searchInfoFetchStatus.error is { errorCode: UIErrorCode.UpmError_NotFound } || packageData is { availableRegistryType: not RegistryType.AssetStore })
                && HasMatchingScopedRegistry(packageName, out var registry) )
            {
                var versionString = productInfo?.versionString ?? packageData?.installedInfo?.version ?? packageData?.mainSearchInfo?.version ?? string.Empty;
                var displayName = productInfo?.displayName ?? packageData?.installedInfo?.displayName ?? packageData?.mainSearchInfo?.displayName ?? string.Empty;
                var errorMessage = L10n.Tr(
                    "This package is not accessible due to scope conflict with the \"{0}\" scoped registry. Please remove the conflicting entry in your Project Settings to restore access to this package on Asset Store.");
                var error = new UIError(UIErrorCode.AssetStorePackageError, string.Format(errorMessage, registry.name));
                var version = new PlaceholderPackageVersion($"{packageName}@{versionString}", displayName, versionString, PackageTag.UpmFormat, error);
                return CreatePackage(packageName, new PlaceholderVersionList(version), new Product(productId, purchaseInfo, productInfo));
            }

            if (packageData?.mainSearchInfo == null && searchInfoFetchStatus is { inProgress: false, error: null })
                m_BackgroundFetchHandler.AddToSearchNonDiscoverableQueue(packageName);

            Package package;
            if (packageData != null)
            {
                var productInfoFetchStatus = m_FetchStatusTracker.GetProductInfoFetchStatus(productId);
                if (productInfo == null && productInfoFetchStatus is { inProgress: false, error: null })
                {
                    m_BackgroundFetchHandler.AddToFetchProductInfoQueue(productId);
                    m_BackgroundFetchHandler.AddToFetchPurchaseInfoQueue(productId);
                }

                var tagsToExclude = m_SettingsProxy.seeAllPackageVersions ? PackageTag.None :
                    m_SettingsProxy.enablePreReleasePackages ? PackageTag.Experimental : PackageTag.Experimental | PackageTag.PreRelease;
                var versionList = new UpmVersionList(packageData, tagsToExclude, m_IOProxy, m_ApplicationProxy, changedSource != PackagesChangedSource.AddAndRemove);
                package = CreatePackage(packageName, versionList, new Product(productId, purchaseInfo, productInfo), isDeprecated: packageData.isDeprecated, deprecationMessage: packageData.deprecationMessage);
                if (productInfoFetchStatus.error != null)
                    AddError(package, productInfoFetchStatus.error);
                else if (productInfo == null)
                    SetProgress(package, PackageProgress.Refreshing);
                else if (packageData.mainSearchInfo != null && packageData.mainSearchInfo.ParseProductId() != productId)
                {
                    // This is not really supposed to happen - this happening would mean there's an issue with data from the backend
                    // Right now there isn't any recommended actions we can suggest the users to take, so we'll just add a message here
                    // to expose it if it ever happens (rather than letting it pass silently)
                    var errorMessage = L10n.Tr("Product Id mismatch between product details and package details. Please try to refresh in a few minutes or report the issue.");
                    AddError(package, new UIError(UIErrorCode.AssetStorePackageError, errorMessage, UIError.Attribute.Warning));
                }
            }
            else // packageData == null, productInfo != null
            {
                if (searchInfoFetchStatus.error != null)
                {
                    var version = new PlaceholderPackageVersion($"{packageName}@{productInfo.versionString}", productInfo.displayName, productInfo.versionString, PackageTag.UpmFormat, searchInfoFetchStatus.error);
                    package = CreatePackage(packageName, new PlaceholderVersionList(version), new Product(productId, purchaseInfo, productInfo));
                }
                else
                {
                    var version = new PlaceholderPackageVersion($"{packageName}@{productInfo.versionString}", productInfo.displayName, productInfo.versionString, PackageTag.UpmFormat);
                    package = CreatePackage(packageName, new PlaceholderVersionList(version), new Product(productId, purchaseInfo, productInfo));
                    SetProgress(package, PackageProgress.Refreshing);
                }
            }

            // if the primary version is not fully fetched, trigger an extra fetch automatically right away to get results early
            // since the primary version's display name is used in the package list
            var primaryVersion = package.versions.primary;
            if (!primaryVersion.isFullyFetched)
                m_BackgroundFetchHandler.AddToExtraFetchPackageInfoQueue(primaryVersion.packageId);

            return package;
        }
    }
}
