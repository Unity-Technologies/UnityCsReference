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
        static IUpmClient s_Instance = null;
        public static IUpmClient instance { get { return s_Instance ?? UpmClientInternal.instance; } }

        [Serializable]
        private class UpmClientInternal : ScriptableSingleton<UpmClientInternal>, IUpmClient, ISerializationCallbackReceiver
        {
            public event Action<IOperation> onListOperation = delegate {};
            public event Action<IOperation> onSearchAllOperation = delegate {};
            public event Action<IOperation> onRemoveOperation = delegate {};
            public event Action<IOperation> onAddOperation = delegate {};
            public event Action<IOperation> onEmbedOperation = delegate {};

            public event Action<IEnumerable<IPackage>> onPackagesChanged = delegate {};
            public event Action<IPackageVersion> onPackageVersionUpdated = delegate {};

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
            private PackageInfo[] m_SerializedExtraPackageInfos;

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

            public void AddById(string packageId)
            {
                if (isAddRemoveOrEmbedInProgress)
                    return;
                m_AddOperation.Add(packageId);
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
                var packageId = GetPackageIdFromPath(path);
                if (string.IsNullOrEmpty(packageId))
                {
                    Debug.LogError($"Error loading package id from path: \"{path}\".");
                    return;
                }
                AddById(packageId);
            }

            private string GetPackageIdFromPath(string path)
            {
                var jsonPath = Directory.Exists(path) ? Path.Combine(path, "package.json") : path;
                if (!File.Exists(jsonPath))
                    return null;

                try
                {
                    var packageJson = Json.Deserialize(File.ReadAllText(jsonPath)) as Dictionary<string, object>;
                    var name = packageJson["name"] as string;
                    var directoryPath = Path.GetDirectoryName(jsonPath).Replace("\\", "/");
                    return $"{name}@file:{directoryPath}";
                }
                catch (Exception)
                {
                    return null;
                }
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
                var errorMessage = offlineMode ? "Error fetching package list." : "Error fetching package list offline.";
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
                m_EmbedOperation.Embed(packageName);
                m_EmbedOperation.onProcessResult += OnProcessAddResult;
                m_EmbedOperation.onOperationError += error => Debug.LogError($"Error embedding package: {m_EmbedOperation.packageName}.");
                onEmbedOperation(m_EmbedOperation);
            }

            public void RemoveByName(string packageName)
            {
                if (isAddRemoveOrEmbedInProgress)
                    return;
                m_RemoveOperation.Remove(packageName);
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
                var errorMessage = offlineMode ? "Error searching for packages." : "Error searching for packages offline.";
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

            public void ExtraFetch(string packageIdOrName)
            {
                if (m_ExtraFetchOperations.ContainsKey(packageIdOrName))
                    return;
                var operation = new UpmSearchOperation();
                operation.Search(packageIdOrName);
                operation.onProcessResult += OnProcessExtraFetchResult;
                operation.onOperationError += error => Debug.LogError($"Error searching for package {packageIdOrName} online.");
                m_ExtraFetchOperations[packageIdOrName] = operation;
            }

            private void OnProcessExtraFetchResult(SearchRequest request)
            {
                var packageInfo = request.Result.FirstOrDefault();

                AddExtraPackageInfo(packageInfo);
                m_ExtraFetchOperations.Remove(packageInfo.packageId);

                // only trigger the call when the package is not installed, as installed version always have the most up-to-date package info
                var installedPackageInfo = GetInstalledPackageInfo(packageInfo.name);
                if (installedPackageInfo?.packageId != packageInfo.packageId)
                    onPackageVersionUpdated(new UpmPackageVersion(packageInfo, false));
            }

            private void OnPackageInfosUpdated(IEnumerable<PackageInfo> packageInfos)
            {
                if (!packageInfos.Any())
                    return;

                var upmPackages = new List<UpmPackage>();
                var showPreview = PackageManagerPrefs.instance.showPreviewPackages;
                foreach (var p in packageInfos)
                {
                    var installedInfo = GetInstalledPackageInfo(p.name);
                    var searchInfo = GetSearchPackageInfo(p.name);

                    upmPackages.Add(CreateUpmPackage(searchInfo, installedInfo, p.name));
                }

                foreach (var package in upmPackages)
                {
                    if (!showPreview && ShouldPreviewsBeRemoved(package))
                        RemovePreviewVersions(package);
                    UpdateExtraPackageInfos(package);
                }

                if (upmPackages.Any())
                    onPackagesChanged(upmPackages.Cast<IPackage>());
            }

            private void OnShowPreviewPackagesChanged(bool showPreview)
            {
                var updatedUpmPackages = new List<UpmPackage>();
                foreach (var installedInfo in m_InstalledPackageInfos.Values)
                {
                    var package = CreateUpmPackage(GetSearchPackageInfo(installedInfo.name), installedInfo);
                    if (ShouldPreviewsBeRemoved(package))
                        updatedUpmPackages.Add(package);
                }

                foreach (var searchInfo in m_SearchPackageInfos.Values.Where(p => !m_InstalledPackageInfos.ContainsKey(p.name)))
                {
                    var package = CreateUpmPackage(searchInfo, null);
                    if (ShouldPreviewsBeRemoved(package))
                        updatedUpmPackages.Add(package);
                }

                foreach (var package in updatedUpmPackages)
                {
                    if (!showPreview)
                        RemovePreviewVersions(package);
                    UpdateExtraPackageInfos(package);
                }

                if (updatedUpmPackages.Any())
                    onPackagesChanged(updatedUpmPackages.Cast<IPackage>());
            }

            private UpmPackage CreateUpmPackage(PackageInfo searchInfo, PackageInfo installedInfo, string packageName = null)
            {
                if (searchInfo == null && installedInfo == null)
                    return new UpmPackage(packageName, Enumerable.Empty<UpmPackageVersion>(), false);

                UpmPackage result;
                if (searchInfo == null)
                    result = new UpmPackage(installedInfo, true, false);
                else
                {
                    result = new UpmPackage(searchInfo, false, true);
                    if (installedInfo != null)
                        result.AddInstalledVersion(new UpmPackageVersion(installedInfo, true));
                }
                return result;
            }

            private void UpdateExtraPackageInfos(UpmPackage package)
            {
                if (!package.versions.Any())
                    return;

                Dictionary<string, PackageInfo> extraVersions;
                if (m_ExtraPackageInfo.TryGetValue(package.name, out extraVersions))
                {
                    foreach (var version in package.versions.Cast<UpmPackageVersion>())
                    {
                        if (version.isFullyFetched)
                            continue;
                        PackageInfo info;
                        if (extraVersions.TryGetValue(version.version.ToString(), out info))
                            version.UpdatePackageInfo(info);
                    }
                }

                // if the primary version is not fully fetched, trigger an extra fetch automatically right away to get results early
                // since the primary version's display name is used in the package list
                var primaryVersion = package.primaryVersion;
                if (!primaryVersion.isFullyFetched)
                    ExtraFetch(primaryVersion.uniqueId);
            }

            private static bool IsPreviewInstalled(UpmPackage package)
            {
                return (!package.installedVersion?.HasTag(PackageTag.Release)) ?? false;
            }

            private static bool IsPreviewInstalled(PackageInfo packageInfo)
            {
                return packageInfo != null && packageInfo.isDirectDependency &&
                    packageInfo.source == PackageSource.Registry && !SemVersion.Parse(packageInfo.version).IsRelease();
            }

            // check if the preview versions be filtered out if the user have `show previews` turned off
            private static bool ShouldPreviewsBeRemoved(UpmPackage package)
            {
                if (IsPreviewInstalled(package))
                    return false;
                return package.versions.Any(v => !v.HasTag(PackageTag.Release));
            }

            private static void RemovePreviewVersions(UpmPackage package)
            {
                package.UpdateVersions(package.versions.Where(v => v.HasTag(PackageTag.Release)).Cast<UpmPackageVersion>());
            }

            // we want to only compare a subset of PackageInfo attributes
            // because most attributes never change if their PackageId is the same.
            private static bool IsDifferent(PackageInfo oldInfo, PackageInfo newInfo)
            {
                if (oldInfo.packageId != newInfo.packageId ||
                    oldInfo.source != newInfo.source ||
                    oldInfo.resolvedPath != newInfo.resolvedPath ||
                    oldInfo.isDirectDependency != newInfo.isDirectDependency ||
                    oldInfo.entitlements.isAllowed != newInfo.entitlements.isAllowed)
                    return true;

                var oldVersions = oldInfo.versions.compatible;
                var newVersions = newInfo.versions.compatible;
                if (oldVersions.Length != newVersions.Length || !oldVersions.SequenceEqual(newVersions))
                    return true;

                if (oldInfo.errors.Length != newInfo.errors.Length || !oldInfo.errors.SequenceEqual(newInfo.errors))
                    return true;

                return false;
            }

            private static List<PackageInfo> FindUpdatedPackageInfos(Dictionary<string, PackageInfo> oldInfos, Dictionary<string, PackageInfo> newInfos)
            {
                PackageInfo info;
                return newInfos.Values.Where(p => { return !oldInfos.TryGetValue(p.name, out info) || IsDifferent(info, p); })
                    .Concat(oldInfos.Values.Where(p => { return !newInfos.TryGetValue(p.name, out info); })).ToList();
            }

            public void OnBeforeSerialize()
            {
                m_SerializedInstalledPackageInfos = m_InstalledPackageInfos.Values.ToArray();
                m_SerializedSearchPackageInfos = m_SearchPackageInfos.Values.ToArray();
                m_SerializedExtraPackageInfos = m_ExtraPackageInfo.Values.SelectMany(p => p.Values).ToArray();
            }

            public void OnAfterDeserialize()
            {
                foreach (var p in m_SerializedInstalledPackageInfos)
                    m_InstalledPackageInfos[p.name] = p;

                foreach (var p in m_SerializedSearchPackageInfos)
                    m_SearchPackageInfos[p.name] = p;

                foreach (var p in m_SerializedExtraPackageInfos)
                    AddExtraPackageInfo(p);
            }

            public void OnEnable()
            {
                if (m_AddOperation.isInProgress)
                    SetupAddOperation();

                if (m_RemoveOperation.isInProgress)
                    SetupRemoveOperation();
            }

            public void Setup()
            {
                PackageManagerPrefs.instance.onShowPreviewPackagesChanged += OnShowPreviewPackagesChanged;
            }

            public void Clear()
            {
                m_InstalledPackageInfos.Clear();
                m_SearchPackageInfos.Clear();
                m_ExtraPackageInfo.Clear();

                m_SerializedInstalledPackageInfos = new PackageInfo[0];
                m_SerializedSearchPackageInfos = new PackageInfo[0];
                m_SerializedExtraPackageInfos = new PackageInfo[0];
            }
        }
    }
}
