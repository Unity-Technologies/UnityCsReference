// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal sealed class UpmClient
    {
        private static string[] k_UnityRegistriesUrlHosts = { ".unity.com", ".unity3d.com" };

        static IUpmClient s_Instance = null;
        public static IUpmClient instance { get { return s_Instance ?? UpmClientInternal.instance; } }


        [Serializable]
        internal class UpmClientInternal : ScriptableSingleton<UpmClientInternal>, IUpmClient, ISerializationCallbackReceiver
        {
            public event Action<IOperation> onListOperation = delegate {};
            public event Action<IOperation> onSearchAllOperation = delegate {};
            public event Action<IOperation> onRemoveOperation = delegate {};
            public event Action<IOperation> onAddOperation = delegate {};
            public event Action<IOperation> onEmbedOperation = delegate {};

            public event Action<IEnumerable<IPackage>> onPackagesChanged = delegate {};
            public event Action<string, IPackage> onProductPackageChanged = delegate {};

            public event Action<string, IPackageVersion> onPackageVersionUpdated = delegate {};
            public event Action<string, IPackageVersion> onProductPackageVersionUpdated = delegate {};
            public event Action<string, UIError> onProductPackageFetchError = delegate {};

            [SerializeField]
            private UpmSearchOperation m_SearchOperation;
            [SerializeField]
            private UpmSearchOperation m_SearchOfflineOperation;
            [SerializeField]
            private UpmListOperation m_ListOperation;
            [SerializeField]
            private UpmListOperation m_ListOfflineOperation;

            [SerializeField]
            private UpmAddOperation m_AddOperation;
            [SerializeField]
            private UpmRemoveOperation m_RemoveOperation;
            [SerializeField]
            private UpmEmbedOperation m_EmbedOperation;

            private readonly Dictionary<string, UpmBaseOperation> m_ExtraFetchOperations = new Dictionary<string, UpmBaseOperation>();

            [SerializeField]
            private string[] m_SerializedPRegistriesUrlKeys;

            [SerializeField]
            private bool[] m_SerializedRegistriesUrlValues;

            internal Dictionary<string, bool> m_RegistriesUrl = new Dictionary<string, bool>();

            [NonSerialized]
            private bool m_EventsRegistered;

            public bool isAddRemoveOrEmbedInProgress
            {
                get { return m_AddOperation.isInProgress || m_RemoveOperation.isInProgress || m_EmbedOperation.isInProgress; }
            }

            public bool IsEmbedInProgress(string packageName)
            {
                return m_EmbedOperation.isInProgress && m_EmbedOperation.packageName == packageName;
            }

            public bool IsRemoveInProgress(string packageName)
            {
                return m_RemoveOperation.isInProgress && m_RemoveOperation.packageName == packageName;
            }

            public bool IsAddInProgress(string packageId)
            {
                return m_AddOperation.isInProgress && m_AddOperation.packageId == packageId;
            }

            private UpmClientInternal()
            {
                m_SearchOperation = new UpmSearchOperation();
                m_SearchOfflineOperation = new UpmSearchOperation();
                m_ListOperation = new UpmListOperation();
                m_ListOfflineOperation = new UpmListOperation();

                m_AddOperation = new UpmAddOperation();
                m_RemoveOperation = new UpmRemoveOperation();
                m_EmbedOperation = new UpmEmbedOperation();
            }

            public void OnBeforeSerialize()
            {
                m_SerializedPRegistriesUrlKeys = m_RegistriesUrl?.Keys.ToArray() ?? new string[0];
                m_SerializedRegistriesUrlValues = m_RegistriesUrl?.Values.ToArray() ?? new bool[0];
            }

            public void OnAfterDeserialize()
            {
                for (var i = 0; i < m_SerializedPRegistriesUrlKeys.Length; i++)
                    m_RegistriesUrl[m_SerializedPRegistriesUrlKeys[i]] = m_SerializedRegistriesUrlValues[i];
            }

            public void AddById(string packageId)
            {
                if (isAddRemoveOrEmbedInProgress)
                    return;
                var packageName = packageId.Split(new[] { '@' }, 2)[0];
                m_AddOperation.Add(packageId, UpmCache.instance.GetProductId(packageName));
                SetupAddOperation();
            }

            private void SetupAddOperation()
            {
                m_AddOperation.onProcessResult += OnProcessAddResult;
                m_AddOperation.onOperationError += (op, error) =>
                {
                    var packageId = string.IsNullOrEmpty(m_AddOperation.packageId) ? m_AddOperation.specialUniqueId : m_AddOperation.packageId;
                    Debug.LogError(string.Format(ApplicationUtil.instance.GetTranslationForText("Error adding package: {0}."), packageId));
                };
                onAddOperation(m_AddOperation);
            }

            private void OnProcessAddResult(Request<PackageInfo> request)
            {
                var packageInfo = request.Result;
                UpmCache.instance.SetInstalledPackageInfo(packageInfo);

                PackageManagerExtensions.ExtensionCallback(() =>
                {
                    foreach (var extension in PackageManagerExtensions.Extensions)
                        extension.OnPackageAddedOrUpdated(packageInfo);
                });

                // do a list offline to refresh all the dependencies
                List(true);
            }

            public void AddByPath(string path)
            {
                if (isAddRemoveOrEmbedInProgress)
                    return;

                path = path.Replace('\\', '/');
                var projectPath = Path.GetDirectoryName(Application.dataPath).Replace('\\', '/') + '/';
                if (path.StartsWith(projectPath))
                {
                    var packageFolderPrefix = "Packages/";
                    var relativePathToProjectRoot = path.Substring(projectPath.Length);
                    if (relativePathToProjectRoot.StartsWith(packageFolderPrefix, StringComparison.InvariantCultureIgnoreCase))
                        path = relativePathToProjectRoot.Substring(packageFolderPrefix.Length);
                    else
                        path = $"../{relativePathToProjectRoot}";
                }

                m_AddOperation.AddByUrlOrPath($"file:{path}");
                SetupAddOperation();
            }

            public void AddByUrl(string url)
            {
                if (isAddRemoveOrEmbedInProgress)
                    return;

                // convert SCP-like syntax to SSH URL as currently UPM doesn't support it
                if (url.ToLower().StartsWith("git@"))
                    url = "ssh://" + url.Replace(':', '/');

                m_AddOperation.AddByUrlOrPath(url);
                SetupAddOperation();
            }

            public void List(bool offlineMode = false)
            {
                var operation = offlineMode ? m_ListOfflineOperation : m_ListOperation;
                if (operation.isInProgress)
                    operation.Cancel();
                var errorMessage = offlineMode ? ApplicationUtil.instance.GetTranslationForText("Error fetching package list offline.") : ApplicationUtil.instance.GetTranslationForText("Error fetching package list.");
                if (offlineMode)
                    operation.ListOffline(m_ListOperation.lastSuccessTimestamp);
                else
                    operation.List();
                operation.onProcessResult += request => OnProcessListResult(request, offlineMode);
                operation.onOperationError += (op, error) => Debug.LogError(errorMessage);
                onListOperation(operation);
            }

            private void OnProcessListResult(ListRequest request, bool offlineMode)
            {
                // skip operation when the result from the online operation is more up-to-date.
                if (offlineMode && m_ListOfflineOperation.timestamp < m_ListOperation.lastSuccessTimestamp)
                    return;

                UpmCache.instance.SetInstalledPackageInfos(request.Result);
            }

            public void EmbedByName(string packageName)
            {
                if (isAddRemoveOrEmbedInProgress)
                    return;
                m_EmbedOperation.Embed(packageName, UpmCache.instance.GetProductId(packageName));
                m_EmbedOperation.onProcessResult += OnProcessAddResult;
                m_EmbedOperation.onOperationError += (op, error) => Debug.LogError(string.Format(ApplicationUtil.instance.GetTranslationForText("Error embedding package: {0}."), m_EmbedOperation.packageName));
                onEmbedOperation(m_EmbedOperation);
            }

            public void RemoveByName(string packageName)
            {
                if (isAddRemoveOrEmbedInProgress)
                    return;
                m_RemoveOperation.Remove(packageName, UpmCache.instance.GetProductId(packageName));
                SetupRemoveOperation();
            }

            public void RemoveEmbeddedByName(string packageName)
            {
                if (isAddRemoveOrEmbedInProgress)
                    return;

                var packageInfo = UpmCache.instance.GetInstalledPackageInfo(packageName);
                if (packageInfo != null)
                {
                    Directory.Delete(packageInfo.resolvedPath, true);
                    AssetDatabase.Refresh();
                }
            }

            private void SetupRemoveOperation()
            {
                m_RemoveOperation.onProcessResult += OnProcessRemoveResult;
                m_RemoveOperation.onOperationError += (op, error) => Debug.LogError(string.Format(ApplicationUtil.instance.GetTranslationForText("Error removing package: {0}."), m_RemoveOperation.packageName));
                onRemoveOperation(m_RemoveOperation);
            }

            private void OnProcessRemoveResult(RemoveRequest request)
            {
                var installedPackage = UpmCache.instance.GetInstalledPackageInfo(request.PackageIdOrName);
                if (installedPackage == null)
                    return;
                UpmCache.instance.RemoveInstalledPackageInfo(installedPackage.name);

                PackageManagerExtensions.ExtensionCallback(() =>
                {
                    foreach (var extension in PackageManagerExtensions.Extensions)
                        extension.OnPackageRemoved(installedPackage);
                });

                // do a list offline to refresh all the dependencies
                List(true);
            }

            public void SearchAll(bool offlineMode = false)
            {
                var operation = offlineMode ? m_SearchOfflineOperation : m_SearchOperation;
                if (operation.isInProgress)
                    operation.Cancel();
                var errorMessage = offlineMode ? ApplicationUtil.instance.GetTranslationForText("Error searching for packages offline.") : ApplicationUtil.instance.GetTranslationForText("Error searching for packages.");
                if (offlineMode)
                    operation.SearchAllOffline(m_SearchOperation.lastSuccessTimestamp);
                else
                    operation.SearchAll();
                operation.onProcessResult += request => OnProcessSearchAllResult(request, offlineMode);
                operation.onOperationError += (op, error) => Debug.LogError(errorMessage);
                onSearchAllOperation(operation);
            }

            private void OnProcessSearchAllResult(SearchRequest request, bool offlineMode)
            {
                // skip operation when the result from the online operation is more up-to-date.
                if (offlineMode && m_SearchOfflineOperation.timestamp < m_SearchOperation.lastSuccessTimestamp)
                    return;

                UpmCache.instance.SetSearchPackageInfos(request.Result);
            }

            public void ExtraFetch(string packageId)
            {
                ExtraFetchInternal(packageId);
            }

            private void ExtraFetchInternal(string packageIdOrName, string productId = null)
            {
                if (m_ExtraFetchOperations.ContainsKey(packageIdOrName))
                    return;
                var operation = new UpmSearchOperation();
                operation.Search(packageIdOrName, productId);
                operation.onProcessResult += (requst) => OnProcessExtraFetchResult(requst, productId);
                operation.onOperationError += (op, error) => OnProcessExtraFetchError(error, productId);
                operation.onOperationFinalized += (op) => OnExtraFetchFinalized(packageIdOrName);
                m_ExtraFetchOperations[packageIdOrName] = operation;
            }

            private void OnProcessExtraFetchResult(SearchRequest request, string productId = null)
            {
                var packageInfo = request.Result.FirstOrDefault();

                if (!string.IsNullOrEmpty(productId))
                {
                    var oldInfo = UpmCache.instance.GetProductPackageInfo(packageInfo.name);
                    // remove the created package that's created before asset store info was fetched
                    // such that there won't be two entries of the same package
                    if (oldInfo == null && UpmCache.instance.IsPackageInstalled(packageInfo.name))
                        onPackagesChanged(new[] { CreateUpmPackage(null, null, packageInfo.name) });

                    UpmCache.instance.SetProductPackageInfo(productId, packageInfo);
                }
                else
                {
                    UpmCache.instance.AddExtraPackageInfo(packageInfo);

                    // only trigger the call when the package is not installed, as installed version always have the most up-to-date package info
                    var installedPackageInfo = UpmCache.instance.GetInstalledPackageInfo(packageInfo.name);
                    if (installedPackageInfo?.packageId != packageInfo.packageId)
                    {
                        productId = UpmCache.instance.GetProductId(packageInfo.name);
                        if (string.IsNullOrEmpty(productId))
                            onPackageVersionUpdated?.Invoke(packageInfo.name, new UpmPackageVersion(packageInfo, false, false));
                        else
                            onProductPackageVersionUpdated?.Invoke(productId, new UpmPackageVersion(packageInfo, false, IsUnityPackage(packageInfo)));
                    }
                }
            }

            private void OnProcessExtraFetchError(UIError error, string productId = null)
            {
                if (!string.IsNullOrEmpty(productId))
                    onProductPackageFetchError?.Invoke(productId, error);
            }

            private void OnExtraFetchFinalized(string packageIdOrName)
            {
                m_ExtraFetchOperations.Remove(packageIdOrName);
            }

            public void FetchForProduct(string productId, string packageName)
            {
                ExtraFetchInternal(packageName, productId);
            }

            private void OnPackageInfosUpdated(IEnumerable<PackageInfo> packageInfos)
            {
                if (packageInfos?.Any() != true)
                    return;

                var upmPackages = new List<UpmPackage>();
                var productPackages = new List<UpmPackage>();
                var showPreview = PackageManagerProjectSettings.instance.enablePreviewPackages;
                foreach (var p in packageInfos)
                {
                    var productId = UpmCache.instance.GetProductId(p.name);
                    var installedInfo = UpmCache.instance.GetInstalledPackageInfo(p.name);
                    if (string.IsNullOrEmpty(productId))
                        upmPackages.Add(CreateUpmPackage(UpmCache.instance.GetSearchPackageInfo(p.name), installedInfo, p.name));
                    else
                        productPackages.Add(CreateUpmPackage(UpmCache.instance.GetProductPackageInfo(p.name), installedInfo, p.name));
                }

                foreach (var package in upmPackages.Concat(productPackages))
                {
                    if (!showPreview && HasHidablePreviewVersions(package))
                        RemovePreviewVersions(package);
                    UpdateExtraPackageInfos(package.name, package.versions);
                }

                if (upmPackages.Any())
                    onPackagesChanged(upmPackages.Cast<IPackage>());

                foreach (var package in productPackages)
                    onProductPackageChanged?.Invoke(UpmCache.instance.GetProductId(package.name), package);
            }

            private void OnShowPreviewPackagesChanged(bool showPreview)
            {
                var updatedUpmPackages = new List<UpmPackage>();
                var updatedProductPackages = new List<UpmPackage>();
                foreach (var installedInfo in UpmCache.instance.installedPackageInfos)
                {
                    var productId = UpmCache.instance.GetProductId(installedInfo.name);
                    if (string.IsNullOrEmpty(productId))
                    {
                        var package = CreateUpmPackage(UpmCache.instance.GetSearchPackageInfo(installedInfo.name), installedInfo);
                        if (HasHidablePreviewVersions(package))
                            updatedUpmPackages.Add(package);
                    }
                    else
                    {
                        var package = CreateUpmPackage(UpmCache.instance.GetProductPackageInfo(installedInfo.name), installedInfo);
                        if (HasHidablePreviewVersions(package))
                            updatedProductPackages.Add(package);
                    }
                }

                foreach (var searchInfo in UpmCache.instance.searchPackageInfos.Where(p => !UpmCache.instance.IsPackageInstalled(p.name)))
                {
                    var package = CreateUpmPackage(searchInfo, null);
                    if (HasHidablePreviewVersions(package))
                        updatedUpmPackages.Add(package);
                }

                foreach (var productPackageInfo in UpmCache.instance.productPackageInfos.Where(p => !UpmCache.instance.IsPackageInstalled(p.name)))
                {
                    var package = CreateUpmPackage(productPackageInfo, null);
                    if (HasHidablePreviewVersions(package))
                        updatedProductPackages.Add(package);
                }

                foreach (var package in updatedUpmPackages.Concat(updatedProductPackages))
                {
                    if (!showPreview)
                        RemovePreviewVersions(package);
                    UpdateExtraPackageInfos(package.name, package.versions);
                }

                if (updatedUpmPackages.Any())
                    onPackagesChanged?.Invoke(updatedUpmPackages.Cast<IPackage>());

                foreach (var package in updatedProductPackages)
                    onProductPackageChanged?.Invoke(UpmCache.instance.GetProductId(package.name), package);
            }

            private UpmPackage CreateUpmPackage(PackageInfo searchInfo, PackageInfo installedInfo, string packageName = null)
            {
                if (searchInfo == null && installedInfo == null)
                    return new UpmPackage(packageName, false, PackageType.Installable);

                UpmPackage result;
                if (searchInfo == null)
                    result = new UpmPackage(installedInfo, true, false, IsUnityPackage(installedInfo));
                else
                {
                    var isUnityPackage = IsUnityPackage(searchInfo);
                    result = new UpmPackage(searchInfo, false, true, isUnityPackage);
                    if (installedInfo != null)
                        result.AddInstalledVersion(new UpmPackageVersion(installedInfo, true, isUnityPackage));
                }
                return result;
            }

            private void UpdateExtraPackageInfos(string packageName, IVersionList versions)
            {
                if (!versions.Any())
                    return;

                var extraVersions = UpmCache.instance.GetExtraPackageInfos(packageName);
                if (extraVersions?.Any() ?? false)
                {
                    foreach (var version in versions.Cast<UpmPackageVersion>())
                    {
                        if (version.isFullyFetched)
                            continue;
                        PackageInfo info;
                        if (extraVersions.TryGetValue(version.version.ToString(), out info))
                            version.UpdatePackageInfo(info, IsUnityPackage(info));
                    }
                }

                // if the primary version is not fully fetched, trigger an extra fetch automatically right away to get results early
                // since the primary version's display name is used in the package list
                var primaryVersion = versions.primary;
                if (!primaryVersion.isFullyFetched)
                    ExtraFetch(primaryVersion.uniqueId);
            }

            // check if this package have preview packages that's `hidable` (will be filtered out if `show preview` is not selected).
            // if the installed version is preview, then we we always show other preview versions
            // if no installed version or installed version is not preview, we hide the preview versions according to the `show previews` toggle
            private static bool HasHidablePreviewVersions(IPackage package)
            {
                var previewInstalled = (!package.versions.installed?.HasTag(PackageTag.Release)) ?? false;
                if (previewInstalled)
                    return false;
                return package.versions.Any(v => !v.HasTag(PackageTag.Release));
            }

            private static void RemovePreviewVersions(UpmPackage package)
            {
                package.UpdateVersions(package.versions.Where(v => v.HasTag(PackageTag.Release)).Cast<UpmPackageVersion>());
            }

            public void OnEnable()
            {
                if (m_AddOperation.isInProgress)
                    SetupAddOperation();

                if (m_RemoveOperation.isInProgress)
                    SetupRemoveOperation();

                if (m_SearchOperation.isInProgress)
                    SearchAll();
            }

            public void RegisterEvents()
            {
                if (m_EventsRegistered)
                    return;

                m_EventsRegistered = true;

                PackageManagerProjectSettings.instance.onEnablePreviewPackageChanged += OnShowPreviewPackagesChanged;
                UpmCache.instance.onPackageInfosUpdated += OnPackageInfosUpdated;
            }

            public void UnregisterEvents()
            {
                if (!m_EventsRegistered)
                    return;

                m_EventsRegistered = false;

                PackageManagerProjectSettings.instance.onEnablePreviewPackageChanged -= OnShowPreviewPackagesChanged;
                UpmCache.instance.onPackageInfosUpdated -= OnPackageInfosUpdated;
            }

            public void ClearCache()
            {
                m_ExtraFetchOperations.Clear();

                UpmCache.instance.ClearCache();
            }

            public void ClearProductCache()
            {
                UpmCache.instance.ClearProductCache();
            }

            public bool IsUnityPackage(PackageInfo packageInfo)
            {
                if (!(packageInfo?.registry?.isDefault ?? false) || string.IsNullOrEmpty(packageInfo.registry?.url))
                    return false;

                if (m_RegistriesUrl.TryGetValue(packageInfo.registry.url, out var isUnityRegistry))
                    return isUnityRegistry;

                isUnityRegistry = IsUnityUrl(packageInfo.registry.url);
                m_RegistriesUrl[packageInfo.registry.url] = isUnityRegistry;
                return isUnityRegistry;
            }

            public bool IsUnityUrl(string url)
            {
                if (string.IsNullOrEmpty(url))
                    return false;

                try
                {
                    var uri = new Uri(url);
                    return !uri.IsLoopback && k_UnityRegistriesUrlHosts.Any(unityHost => uri.Host.EndsWith(unityHost, StringComparison.InvariantCultureIgnoreCase));
                }
                catch (UriFormatException)
                {
                    return false;
                }
            }
        }
    }
}
