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
            public event Action<string, Error> onProductPackageFetchError = delegate {};

            private UpmSearchOperation m_SearchOperation;
            private UpmSearchOperation m_SearchOfflineOperation;
            private UpmListOperation m_ListOperation;
            private UpmListOperation m_ListOfflineOperation;

            private UpmAddOperation m_AddOperation;
            private UpmRemoveOperation m_RemoveOperation;
            private UpmEmbedOperation m_EmbedOperation;

            private Dictionary<string, PackageInfo> m_SearchPackageInfos = new Dictionary<string, PackageInfo>();
            private PackageInfo GetSearchPackageInfo(string packageName)
            {
                PackageInfo result = null;
                return m_SearchPackageInfos.TryGetValue(packageName, out result) ? result : null;
            }

            private Dictionary<string, PackageInfo> m_InstalledPackageInfos = new Dictionary<string, PackageInfo>();
            private PackageInfo GetInstalledPackageInfo(string packageName)
            {
                PackageInfo result = null;
                return m_InstalledPackageInfos.TryGetValue(packageName, out result) ? result : null;
            }

            private Dictionary<string, PackageInfo> m_ProductPackageInfos = new Dictionary<string, PackageInfo>();

            // the mapping between package name (key) to asset store product id (value)
            private Dictionary<string, string> m_ProductIdMap = new Dictionary<string, string>();

            private readonly Dictionary<string, UpmBaseOperation> m_ExtraFetchOperations = new Dictionary<string, UpmBaseOperation>();
            private Dictionary<string, Dictionary<string, PackageInfo>> m_ExtraPackageInfo = new Dictionary<string, Dictionary<string, PackageInfo>>();
            private void AddExtraPackageInfo(PackageInfo packageInfo)
            {
                Dictionary<string, PackageInfo> dict;
                if (!m_ExtraPackageInfo.TryGetValue(packageInfo.name, out dict))
                {
                    dict = new Dictionary<string, PackageInfo>();
                    m_ExtraPackageInfo[packageInfo.name] = dict;
                }
                dict[packageInfo.version] = packageInfo;
            }

            // arrays created to help serialize dictionaries
            private PackageInfo[] m_SerializedInstalledPackageInfos;
            private PackageInfo[] m_SerializedSearchPackageInfos;
            private PackageInfo[] m_SerializedProductPackageInfos;
            private PackageInfo[] m_SerializedExtraPackageInfos;
            private string[] m_SerializedProductIdMapKeys;
            private string[] m_SerializedProductIdMapValues;

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
                m_SerializedInstalledPackageInfos = m_InstalledPackageInfos.Values.ToArray();
                m_SerializedSearchPackageInfos = m_SearchPackageInfos.Values.ToArray();
                m_SerializedProductPackageInfos = m_ProductPackageInfos.Values.ToArray();
                m_SerializedExtraPackageInfos = m_ExtraPackageInfo.Values.SelectMany(p => p.Values).ToArray();
                m_SerializedProductIdMapKeys = m_ProductIdMap.Keys.ToArray();
                m_SerializedProductIdMapValues = m_ProductIdMap.Values.ToArray();
                m_SerializedPRegistriesUrlKeys = m_RegistriesUrl?.Keys.ToArray() ?? new string[0];
                m_SerializedRegistriesUrlValues = m_RegistriesUrl?.Values.ToArray() ?? new bool[0];
            }

            public void OnAfterDeserialize()
            {
                foreach (var p in m_SerializedInstalledPackageInfos)
                    m_InstalledPackageInfos[p.name] = p;

                foreach (var p in m_SerializedSearchPackageInfos)
                    m_SearchPackageInfos[p.name] = p;

                m_ProductPackageInfos = m_SerializedProductPackageInfos.ToDictionary(p => p.name, p => p);

                foreach (var p in m_SerializedExtraPackageInfos)
                    AddExtraPackageInfo(p);

                for (var i = 0; i < m_SerializedProductIdMapKeys.Length; i++)
                    m_ProductIdMap[m_SerializedProductIdMapKeys[i]] = m_SerializedProductIdMapValues[i];

                for (var i = 0; i < m_SerializedPRegistriesUrlKeys.Length; i++)
                    m_RegistriesUrl[m_SerializedPRegistriesUrlKeys[i]] = m_SerializedRegistriesUrlValues[i];
            }

            public void AddById(string packageId)
            {
                if (isAddRemoveOrEmbedInProgress)
                    return;
                var packageName = packageId.Split(new[] { '@' }, 2)[0];
                m_AddOperation.Add(packageId, m_ProductIdMap.Get(packageName));
                SetupAddOperation();
            }

            private void SetupAddOperation()
            {
                m_AddOperation.onProcessResult += OnProcessAddResult;
                m_AddOperation.onOperationError += error => Debug.LogError($"Error adding package: {m_AddOperation.packageId}.");
                onAddOperation(m_AddOperation);
            }

            private void OnProcessAddResult(Request<PackageInfo> request)
            {
                var packageInfo = request.Result;
                var oldPackageInfo = GetInstalledPackageInfo(packageInfo.name);
                m_InstalledPackageInfos[packageInfo.name] = packageInfo;
                OnPackageInfosUpdated(new PackageInfo[] { packageInfo });

                PackageManagerExtensions.ExtensionCallback(() =>
                {
                    foreach (var extension in PackageManagerExtensions.Extensions)
                        extension.OnPackageAddedOrUpdated(packageInfo);
                });

                if (IsPreviewInstalled(packageInfo) || IsPreviewInstalled(oldPackageInfo))
                    OnInstalledPreviewPackagesChanged();

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
                var errorMessage = offlineMode ? "Error fetching package list offline." : "Error fetching package list.";
                if (offlineMode)
                    operation.ListOffline(m_ListOperation.lastSuccessTimestamp);
                else
                    operation.List();
                operation.onProcessResult += request => OnProcessListResult(request, offlineMode);
                operation.onOperationError += error => Debug.LogError(errorMessage);
                onListOperation(operation);
            }

            private void OnProcessListResult(ListRequest request, bool offlineMode)
            {
                // skip operation when the result from the online operation is more up-to-date.
                if (offlineMode && m_ListOfflineOperation.timestamp < m_ListOperation.lastSuccessTimestamp)
                    return;

                var listResult = request.Result.ToDictionary(p => p.name, p => p);

                var oldListResult = m_InstalledPackageInfos;
                m_InstalledPackageInfos = listResult;

                var updatedInfos = FindUpdatedPackageInfos(oldListResult, listResult);
                OnPackageInfosUpdated(updatedInfos);

                if (updatedInfos.Any())
                    OnInstalledPreviewPackagesChanged();
            }

            public void EmbedByName(string packageName)
            {
                if (isAddRemoveOrEmbedInProgress)
                    return;
                m_EmbedOperation.Embed(packageName, m_ProductIdMap.Get(packageName));
                m_EmbedOperation.onProcessResult += OnProcessAddResult;
                m_EmbedOperation.onOperationError += error => Debug.LogError($"Error embedding package: {m_EmbedOperation.packageName}.");
                onEmbedOperation(m_EmbedOperation);
            }

            public void RemoveByName(string packageName)
            {
                if (isAddRemoveOrEmbedInProgress)
                    return;
                m_RemoveOperation.Remove(packageName, m_ProductIdMap.Get(packageName));
                SetupRemoveOperation();
            }

            public void RemoveEmbeddedByName(string packageName)
            {
                if (isAddRemoveOrEmbedInProgress)
                    return;

                var packageInfo = GetInstalledPackageInfo(packageName);
                if (packageInfo != null)
                {
                    Directory.Delete(packageInfo.resolvedPath, true);
                    AssetDatabase.Refresh();
                }
            }

            private void SetupRemoveOperation()
            {
                m_RemoveOperation.onProcessResult += OnProcessRemoveResult;
                m_RemoveOperation.onOperationError += error => Debug.LogError($"Error removing package: {m_RemoveOperation.packageName}.");
                onRemoveOperation(m_RemoveOperation);
            }

            private void OnProcessRemoveResult(RemoveRequest request)
            {
                var installedPackage = GetInstalledPackageInfo(request.PackageIdOrName);
                if (installedPackage == null)
                    return;
                m_InstalledPackageInfos.Remove(installedPackage.name);
                OnPackageInfosUpdated(new PackageInfo[] { installedPackage });

                PackageManagerExtensions.ExtensionCallback(() =>
                {
                    foreach (var extension in PackageManagerExtensions.Extensions)
                        extension.OnPackageRemoved(installedPackage);
                });

                if (IsPreviewInstalled(installedPackage))
                    OnInstalledPreviewPackagesChanged();

                // do a list offline to refresh all the dependencies
                List(true);
            }

            private void OnInstalledPreviewPackagesChanged()
            {
                if (!PackageManagerPrefs.instance.hasShowPreviewPackagesKey)
                    PackageManagerPrefs.instance.showPreviewPackagesFromInstalled = m_InstalledPackageInfos.Values.Any(p => !SemVersion.Parse(p.version).IsRelease());
            }

            public void SearchAll(bool offlineMode = false)
            {
                var operation = offlineMode ? m_SearchOfflineOperation : m_SearchOperation;
                if (operation.isInProgress)
                    operation.Cancel();
                var errorMessage = offlineMode ? "Error searching for packages offline." : "Error searching for packages.";
                if (offlineMode)
                    operation.SearchAllOffline(m_SearchOperation.lastSuccessTimestamp);
                else
                    operation.SearchAll();
                operation.onProcessResult += request => OnProcessSearchAllResult(request, offlineMode);
                operation.onOperationError += error => Debug.LogError(errorMessage);
                onSearchAllOperation(operation);
            }

            private void OnProcessSearchAllResult(SearchRequest request, bool offlineMode)
            {
                // skip operation when the result from the online operation is more up-to-date.
                if (offlineMode && m_SearchOfflineOperation.timestamp < m_SearchOperation.lastSuccessTimestamp)
                    return;

                var searchResult = request.Result.ToDictionary(p => p.name, p => p);

                var oldSearchResult = m_SearchPackageInfos;
                m_SearchPackageInfos = searchResult;

                OnPackageInfosUpdated(FindUpdatedPackageInfos(oldSearchResult, searchResult));
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
                operation.onOperationError += (error) => OnProcessExtraFetchError(error, productId);
                operation.onOperationFinalized += () => OnExtraFetchFinalized(packageIdOrName);
                m_ExtraFetchOperations[packageIdOrName] = operation;
            }

            private void OnProcessExtraFetchResult(SearchRequest request, string productId = null)
            {
                var packageInfo = request.Result.FirstOrDefault();

                if (!string.IsNullOrEmpty(productId))
                {
                    var oldInfo = m_ProductPackageInfos.Get(packageInfo.name);
                    // remove the created package that's created before asset store info was fetched
                    if (oldInfo == null && m_InstalledPackageInfos.ContainsKey(packageInfo.name))
                        onPackagesChanged(new[] { CreateUpmPackage(null, null, packageInfo.name) });

                    if (oldInfo == null || IsDifferent(oldInfo, packageInfo))
                    {
                        m_ProductPackageInfos[packageInfo.name] = packageInfo;
                        OnPackageInfosUpdated(request.Result.Take(1));
                    }
                }
                else
                {
                    AddExtraPackageInfo(packageInfo);

                    // only trigger the call when the package is not installed, as installed version always have the most up-to-date package info
                    var installedPackageInfo = m_InstalledPackageInfos.Get(packageInfo.name);
                    if (installedPackageInfo?.packageId != packageInfo.packageId)
                    {
                        productId = m_ProductIdMap.Get(packageInfo.name);
                        if (string.IsNullOrEmpty(productId))
                            onPackageVersionUpdated?.Invoke(packageInfo.name, new UpmPackageVersion(packageInfo, false, false));
                        else
                            onProductPackageVersionUpdated?.Invoke(productId, new UpmPackageVersion(packageInfo, false, IsUnityPackage(packageInfo)));
                    }
                }
            }

            private void OnProcessExtraFetchError(Error error, string productId = null)
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
                m_ProductIdMap[packageName] = productId;
                ExtraFetchInternal(packageName, productId);
            }

            private void OnPackageInfosUpdated(IEnumerable<PackageInfo> packageInfos)
            {
                if (!packageInfos.Any())
                    return;

                var upmPackages = new List<UpmPackage>();
                var productPackages = new List<UpmPackage>();
                var showPreview = PackageManagerPrefs.instance.showPreviewPackages;
                foreach (var p in packageInfos)
                {
                    var productId = m_ProductIdMap.Get(p.name);
                    var installedInfo = m_InstalledPackageInfos.Get(p.name);
                    if (string.IsNullOrEmpty(productId))
                        upmPackages.Add(CreateUpmPackage(m_SearchPackageInfos.Get(p.name), installedInfo, p.name));
                    else
                        productPackages.Add(CreateUpmPackage(m_ProductPackageInfos.Get(p.name), installedInfo, p.name));
                }

                foreach (var package in upmPackages.Concat(productPackages))
                {
                    if (!showPreview && ShouldPreviewsBeRemoved(package.versionList))
                        RemovePreviewVersions(package);
                    UpdateExtraPackageInfos(package.name, package.versionList);
                }

                if (upmPackages.Any())
                    onPackagesChanged(upmPackages.Cast<IPackage>());

                foreach (var package in productPackages)
                    onProductPackageChanged?.Invoke(m_ProductIdMap.Get(package.name), package);
            }

            private void OnShowPreviewPackagesChanged(bool showPreview)
            {
                var updatedUpmPackages = new List<UpmPackage>();
                var updatedProductPackages = new List<UpmPackage>();
                foreach (var installedInfo in m_InstalledPackageInfos.Values)
                {
                    var productId = m_ProductIdMap.Get(installedInfo.name);
                    if (string.IsNullOrEmpty(productId))
                    {
                        var package = CreateUpmPackage(m_SearchPackageInfos.Get(installedInfo.name), installedInfo);
                        if (ShouldPreviewsBeRemoved(package.versionList))
                            updatedUpmPackages.Add(package);
                    }
                    else
                    {
                        var package = CreateUpmPackage(m_ProductPackageInfos.Get(installedInfo.name), installedInfo);
                        if (ShouldPreviewsBeRemoved(package.versionList))
                            updatedProductPackages.Add(package);
                    }
                }

                foreach (var searchInfo in m_SearchPackageInfos.Values.Where(p => !m_InstalledPackageInfos.ContainsKey(p.name)))
                {
                    var package = CreateUpmPackage(searchInfo, null);
                    if (ShouldPreviewsBeRemoved(package.versionList))
                        updatedUpmPackages.Add(package);
                }

                foreach (var productPackageInfo in m_ProductPackageInfos.Values.Where(p => !m_InstalledPackageInfos.ContainsKey(p.name)))
                {
                    var package = CreateUpmPackage(productPackageInfo, null);
                    if (ShouldPreviewsBeRemoved(package.versionList))
                        updatedProductPackages.Add(package);
                }

                foreach (var package in updatedUpmPackages.Concat(updatedProductPackages))
                {
                    if (!showPreview)
                        RemovePreviewVersions(package);
                    UpdateExtraPackageInfos(package.name, package.versionList);
                }

                if (updatedUpmPackages.Any())
                    onPackagesChanged?.Invoke(updatedUpmPackages.Cast<IPackage>());

                foreach (var package in updatedProductPackages)
                    onProductPackageChanged?.Invoke(m_ProductIdMap.Get(package.name), package);
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
                if (!versions.all.Any())
                    return;

                Dictionary<string, PackageInfo> extraVersions;
                if (m_ExtraPackageInfo.TryGetValue(packageName, out extraVersions))
                {
                    foreach (var version in versions.all.Cast<UpmPackageVersion>())
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

            private static bool IsPreviewInstalled(IVersionList versions)
            {
                return (!versions.installed?.HasTag(PackageTag.Release)) ?? false;
            }

            private static bool IsPreviewInstalled(PackageInfo packageInfo)
            {
                return packageInfo != null && packageInfo.isDirectDependency &&
                    packageInfo.source == PackageSource.Registry && !SemVersion.Parse(packageInfo.version).IsRelease();
            }

            // check if the preview versions be filtered out if the user have `show previews` turned off
            private static bool ShouldPreviewsBeRemoved(IVersionList versions)
            {
                if (IsPreviewInstalled(versions))
                    return false;
                return versions.all.Any(v => !v.HasTag(PackageTag.Release));
            }

            private static void RemovePreviewVersions(UpmPackage package)
            {
                package.UpdateVersions(package.versions.Where(v => v.HasTag(PackageTag.Release)).Cast<UpmPackageVersion>());
            }

            // For BuiltIn and Registry packages, we want to only compare a subset of PackageInfo attributes,
            // as most attributes never change if their PackageId is the same. For other types of packages, always consider them different.
            private static bool IsDifferent(PackageInfo p1, PackageInfo p2)
            {
                if (p1.packageId != p2.packageId ||
                    p1.isDirectDependency != p2.isDirectDependency ||
                    p1.version != p2.version ||
                    p1.source != p2.source ||
                    p1.resolvedPath != p2.resolvedPath ||
                    p1.status != p2.status ||
                    p1.entitlements.isAllowed != p2.entitlements.isAllowed ||
                    p1.registry?.name != p2.registry?.name ||
                    p1.registry?.url != p2.registry?.url ||
                    p1.registry?.isDefault != p2.registry?.isDefault ||
                    p1.versions.verified != p2.versions.verified ||
                    p1.versions.compatible.Length != p2.versions.compatible.Length || !p1.versions.compatible.SequenceEqual(p2.versions.compatible) ||
                    p1.versions.all.Length != p2.versions.all.Length || !p1.versions.all.SequenceEqual(p2.versions.all) ||
                    p1.errors.Length != p2.errors.Length || !p1.errors.SequenceEqual(p2.errors) ||
                    p1.dependencies.Length != p2.dependencies.Length || !p1.dependencies.SequenceEqual(p2.dependencies) ||
                    p1.resolvedDependencies.Length != p2.resolvedDependencies.Length || !p1.resolvedDependencies.SequenceEqual(p2.resolvedDependencies))
                    return true;

                if (p1.source == PackageSource.BuiltIn || p1.source == PackageSource.Registry)
                    return false;

                if (p1.source == PackageSource.Git)
                    return p1.git.hash != p2.git?.hash || p1.git.revision != p2.git?.revision;

                return true;
            }

            private static List<PackageInfo> FindUpdatedPackageInfos(Dictionary<string, PackageInfo> oldInfos, Dictionary<string, PackageInfo> newInfos)
            {
                PackageInfo info;
                return newInfos.Values.Where(p => { return !oldInfos.TryGetValue(p.name, out info) || IsDifferent(info, p); })
                    .Concat(oldInfos.Values.Where(p => { return !newInfos.TryGetValue(p.name, out info); })).ToList();
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

                PackageManagerPrefs.instance.onShowPreviewPackagesChanged += OnShowPreviewPackagesChanged;
            }

            public void UnregisterEvents()
            {
                if (!m_EventsRegistered)
                    return;

                m_EventsRegistered = false;

                PackageManagerPrefs.instance.onShowPreviewPackagesChanged -= OnShowPreviewPackagesChanged;
            }

            public void ClearCache()
            {
                m_InstalledPackageInfos.Clear();
                m_SearchPackageInfos.Clear();
                m_ExtraPackageInfo.Clear();
                m_ProductIdMap.Clear();
                m_ExtraFetchOperations.Clear();

                m_SerializedInstalledPackageInfos = new PackageInfo[0];
                m_SerializedSearchPackageInfos = new PackageInfo[0];
                m_SerializedExtraPackageInfos = new PackageInfo[0];

                ClearProductCache();
            }

            public void ClearProductCache()
            {
                m_ProductPackageInfos.Clear();
                m_ProductIdMap.Clear();

                m_SerializedProductPackageInfos = new PackageInfo[0];
                m_SerializedProductIdMapKeys = new string[0];
                m_SerializedProductIdMapValues = new string[0];
            }

            public bool IsUnityPackage(PackageInfo packageInfo)
            {
                if (!(packageInfo?.registry?.isDefault ?? false) || string.IsNullOrEmpty(packageInfo.registry?.url))
                    return false;

                bool isUnityRegistry;
                if (m_RegistriesUrl.TryGetValue(packageInfo.registry.url, out isUnityRegistry))
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
