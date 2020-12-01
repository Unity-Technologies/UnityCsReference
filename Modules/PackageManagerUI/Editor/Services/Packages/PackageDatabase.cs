// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.PackageManager.UI
{
    internal sealed class PackageDatabase
    {
        static IPackageDatabase s_Instance = null;
        public static IPackageDatabase instance { get { return s_Instance ?? PackageDatabaseInternal.instance; } }

        [Serializable]
        private class PackageDatabaseInternal : ScriptableSingleton<PackageDatabaseInternal>, IPackageDatabase, ISerializationCallbackReceiver
        {
            public event Action<IPackage, IPackageVersion> onInstallSuccess = delegate {};
            public event Action<IPackage> onUninstallSuccess = delegate {};
            public event Action<IPackage> onPackageProgressUpdate = delegate {};

            // args 1 ,2, 3, 4 are added, removed and preUpdated, and postUpdated packages respectively
            public event Action<IEnumerable<IPackage>, IEnumerable<IPackage>, IEnumerable<IPackage>, IEnumerable<IPackage>> onPackagesChanged = delegate {};

            private readonly Dictionary<string, IPackage> m_Packages = new Dictionary<string, IPackage>();

            // a list of unique ids (could be specialUniqueId or packageId)
            [SerializeField]
            private List<string> m_SpecialInstallations = new List<string>();

            [SerializeField]
            private List<UpmPackage> m_SerializedUpmPackages = new List<UpmPackage>();

            [SerializeField]
            private List<AssetStorePackage> m_SerializedAssetStorePackages = new List<AssetStorePackage>();

            [SerializeField]
            private List<PlaceholderPackage> m_SerializedPlaceholderPackages = new List<PlaceholderPackage>();

            [NonSerialized]
            private bool m_EventsRegistered;

            public bool isEmpty { get { return !m_Packages.Any(); } }

            private static readonly IPackage[] k_EmptyList = new IPackage[0] {};

            public bool isInstallOrUninstallInProgress
            {
                // add, embed -> install, remove -> uninstall
                get { return UpmClient.instance.isAddRemoveOrEmbedInProgress; }
            }

            public IEnumerable<IPackage> allPackages { get { return m_Packages.Values; } }
            public IEnumerable<IPackage> assetStorePackages { get { return m_Packages.Values.Where(p => p is AssetStorePackage); } }
            public IEnumerable<IPackage> upmPackages { get { return m_Packages.Values.Where(p => p is UpmPackage); } }

            private PackageDatabaseInternal()
            {
            }

            public bool IsUninstallInProgress(IPackage package)
            {
                return UpmClient.instance.IsRemoveInProgress(package.uniqueId);
            }

            public bool IsInstallInProgress(IPackageVersion version)
            {
                return UpmClient.instance.IsAddInProgress(version.uniqueId) || UpmClient.instance.IsEmbedInProgress(version.uniqueId);
            }

            // In some situations, we only know an id (could be package unique id, or version unique id) or just a name (package Name, or display name)
            // but we still might be able to find a package and a version that matches the criteria
            public void GetPackageAndVersionByIdOrName(string idOrName, out IPackage package, out IPackageVersion version)
            {
                // GetPackage by packageUniqueId itself is not an expensive operation, so we want to try and see if the input string is a packageUniqueId first.
                package = GetPackage(idOrName);
                if (package != null)
                {
                    version = null;
                    return;
                }

                // if we are able to break the string into two by looking at '@' sign, it's possible that the input idOrDisplayName is a versionId
                var idOrDisplayNameSplit = idOrName?.Split(new[] { '@' }, 2);
                if (idOrDisplayNameSplit?.Length == 2)
                {
                    var packageUniqueId = idOrDisplayNameSplit[0];
                    GetPackageAndVersion(packageUniqueId, idOrName, out package, out version);
                    if (package != null)
                        return;
                }

                // If none of those find-by-index options work, we'll just have to find it the brute force way by matching the name & display name
                package = m_Packages.Values.FirstOrDefault(p => p.name == idOrName || p.displayName == idOrName);
                version = null;
            }

            public IPackage GetPackage(string uniqueId)
            {
                return string.IsNullOrEmpty(uniqueId) ? null : m_Packages.Get(uniqueId);
            }

            public IPackage GetPackage(IPackageVersion version)
            {
                return GetPackage(version?.packageUniqueId);
            }

            public void GetPackageAndVersion(string packageUniqueId, string versionUniqueId, out IPackage package, out IPackageVersion version)
            {
                package = GetPackage(packageUniqueId);
                version = package?.versions.FirstOrDefault(v => v.uniqueId == versionUniqueId);
            }

            public IPackageVersion GetPackageVersion(string packageUniqueId, string versionUniqueId)
            {
                IPackage package;
                IPackageVersion version;
                GetPackageAndVersion(packageUniqueId, versionUniqueId, out package, out version);
                return version;
            }

            public IPackageVersion GetPackageVersion(DependencyInfo info)
            {
                IPackage package;
                IPackageVersion version;
                GetUpmPackageAndVersion(info.name, info.version, out package, out version);
                return version;
            }

            private void GetUpmPackageAndVersion(string name, string versionIdentifier, out IPackage package, out IPackageVersion version)
            {
                package = GetPackage(name) as UpmPackage;
                if (package == null)
                {
                    version = null;
                    return;
                }

                // the versionIdentifier could either be SemVersion or file, git or ssh reference
                // and the two cases are handled differently.
                if (!string.IsNullOrEmpty(versionIdentifier) && char.IsDigit(versionIdentifier.First()))
                {
                    SemVersion? parsedVersion;
                    SemVersionParser.TryParse(versionIdentifier, out parsedVersion);
                    version = package.versions.FirstOrDefault(v => v.version == parsedVersion);
                }
                else
                {
                    var packageId = UpmPackageVersion.FormatPackageId(name, versionIdentifier);
                    version = package.versions.FirstOrDefault(v => v.uniqueId == packageId);
                }
            }

            public IEnumerable<IPackageVersion> GetReverseDependencies(IPackageVersion version)
            {
                if (version?.dependencies == null)
                    return null;
                var installedRoots = allPackages.Select(p => p.versions.installed).Where(p => p?.isDirectDependency ?? false);
                var dependsOnPackage = installedRoots.Where(p => p.resolvedDependencies?.Any(r => r.name == version.name) ?? false);
                return dependsOnPackage;
            }

            public void OnAfterDeserialize()
            {
                foreach (var p in m_SerializedPlaceholderPackages)
                    m_Packages[p.uniqueId] = p;

                foreach (var p in m_SerializedUpmPackages)
                    m_Packages[p.uniqueId] = p;

                foreach (var p in m_SerializedAssetStorePackages)
                    m_Packages[p.uniqueId] = p;
            }

            public void OnBeforeSerialize()
            {
                m_SerializedUpmPackages = new List<UpmPackage>();
                m_SerializedAssetStorePackages = new List<AssetStorePackage>();
                m_SerializedPlaceholderPackages = new List<PlaceholderPackage>();

                foreach (var package in m_Packages.Values)
                {
                    if (package is AssetStorePackage)
                        m_SerializedAssetStorePackages.Add((AssetStorePackage)package);
                    else if (package is UpmPackage)
                        m_SerializedUpmPackages.Add((UpmPackage)package);
                    else if (package is PlaceholderPackage)
                        m_SerializedPlaceholderPackages.Add((PlaceholderPackage)package);
                }
            }

            public void AddPackageError(IPackage package, UIError error)
            {
                var packagePreUpdate = package.Clone();
                package.AddError(error);
                onPackagesChanged?.Invoke(k_EmptyList, k_EmptyList, new[] { packagePreUpdate }, new[] { package });
            }

            public void ClearPackageErrors(IPackage package)
            {
                var packagePreUpdate = package.Clone();
                package.ClearErrors();
                onPackagesChanged?.Invoke(k_EmptyList, k_EmptyList, new[] { packagePreUpdate }, new[] { package });
            }

            public IEnumerable<IPackage> packagesInError => allPackages.Where(p => p.errors.Any());

            public void SetPackageProgress(IPackage package, PackageProgress progress)
            {
                if (package == null || package.progress == progress)
                    return;

                package.progress = progress;

                onPackageProgressUpdate?.Invoke(package);
            }

            public void RegisterEvents()
            {
                if (m_EventsRegistered)
                    return;

                m_EventsRegistered = true;

                UpmClient.instance.onPackagesChanged += OnPackagesChanged;
                UpmClient.instance.onPackageVersionUpdated += OnUpmPackageVersionUpdated;
                UpmClient.instance.onAddOperation += OnUpmAddOperation;
                UpmClient.instance.onEmbedOperation += OnUpmEmbedOperation;
                UpmClient.instance.onRemoveOperation += OnUpmRemoveOperation;
                UpmClient.instance.RegisterEvents();

                AssetStoreClient.instance.onPackagesChanged += OnPackagesChanged;
                AssetStoreClient.instance.onPackageVersionUpdated += OnUpmPackageVersionUpdated;
                AssetStoreClient.instance.RegisterEvents();

                AssetStoreDownloadManager.instance.onDownloadProgress += OnDownloadProgress;
                AssetStoreDownloadManager.instance.onDownloadFinalized += OnDownloadFinalized;

                ApplicationUtil.instance.onUserLoginStateChange += OnUserLoginStateChange;
            }

            public void UnregisterEvents()
            {
                if (!m_EventsRegistered)
                    return;

                m_EventsRegistered = false;

                UpmClient.instance.onPackagesChanged -= OnPackagesChanged;
                UpmClient.instance.onPackageVersionUpdated -= OnUpmPackageVersionUpdated;
                UpmClient.instance.onAddOperation -= OnUpmAddOperation;
                UpmClient.instance.onEmbedOperation -= OnUpmEmbedOperation;
                UpmClient.instance.onRemoveOperation -= OnUpmRemoveOperation;
                UpmClient.instance.UnregisterEvents();

                AssetStoreClient.instance.onPackagesChanged -= OnPackagesChanged;
                AssetStoreClient.instance.onPackageVersionUpdated -= OnUpmPackageVersionUpdated;
                AssetStoreClient.instance.UnregisterEvents();

                AssetStoreDownloadManager.instance.onDownloadProgress -= OnDownloadProgress;
                AssetStoreDownloadManager.instance.onDownloadFinalized -= OnDownloadFinalized;

                ApplicationUtil.instance.onUserLoginStateChange -= OnUserLoginStateChange;
            }

            public void Reload()
            {
                onPackagesChanged?.Invoke(Enumerable.Empty<IPackage>(), m_Packages.Values, Enumerable.Empty<IPackage>(), Enumerable.Empty<IPackage>());

                UnregisterEvents();

                AssetStoreClient.instance.ClearCache();
                UpmClient.instance.ClearCache();

                m_Packages.Clear();
                m_SerializedUpmPackages = new List<UpmPackage>();
                m_SerializedAssetStorePackages = new List<AssetStorePackage>();
                m_SpecialInstallations.Clear();

                RegisterEvents();
            }

            private void OnDownloadProgress(IOperation operation)
            {
                var package = GetPackage(operation.packageUniqueId);
                if (package == null)
                    return;
                SetPackageProgress(package, operation.isInProgress ? PackageProgress.Downloading : PackageProgress.None);
            }

            private void OnDownloadFinalized(IOperation operation)
            {
                var package = GetPackage(operation.packageUniqueId);
                if (package == null)
                    return;

                var downloadOperation = operation as AssetStoreDownloadOperation;
                if (downloadOperation.state == DownloadState.Error || downloadOperation.state == DownloadState.Aborted)
                    AddPackageError(package, new UIError(UIErrorCode.AssetStoreOperationError, downloadOperation.errorMessage));
                else if (downloadOperation.state == DownloadState.Completed)
                    AssetStoreClient.instance.RefreshLocal();
                SetPackageProgress(package, PackageProgress.None);
            }

            private void OnUserLoginStateChange(bool loggedIn)
            {
                if (!loggedIn)
                {
                    var assetStorePackages = m_Packages.Where(kp => kp.Value is AssetStorePackage).Select(kp => kp.Value).ToList();
                    foreach (var p in assetStorePackages)
                        m_Packages.Remove(p.uniqueId);
                    m_SerializedAssetStorePackages = new List<AssetStorePackage>();

                    onPackagesChanged?.Invoke(k_EmptyList, assetStorePackages, k_EmptyList, k_EmptyList);
                }
            }

            private void OnPackagesChanged(IEnumerable<IPackage> packages)
            {
                if (!packages.Any())
                    return;

                var packagesAdded = new List<IPackage>();
                var packagesRemoved = new List<IPackage>();

                var packagesPreUpdate = new List<IPackage>();
                var packagesPostUpdate = new List<IPackage>();

                var packagesInstalled = new List<IPackage>();

                foreach (var package in packages)
                {
                    var packageUniqueId = package.uniqueId;
                    var isEmptyPackage = !package.versions.Any();
                    var oldPackage = GetPackage(packageUniqueId);

                    if (oldPackage != null && isEmptyPackage)
                    {
                        packagesRemoved.Add(m_Packages[packageUniqueId]);
                        m_Packages.Remove(packageUniqueId);
                    }
                    else if (!isEmptyPackage)
                    {
                        m_Packages[packageUniqueId] = package;
                        if (oldPackage != null)
                        {
                            packagesPreUpdate.Add(oldPackage);
                            packagesPostUpdate.Add(package);
                        }
                        else
                            packagesAdded.Add(package);
                    }

                    if (m_SpecialInstallations.Any() && package.versions.installed != null && oldPackage?.versions.installed == null)
                        packagesInstalled.Add(package);
                }

                if (packagesAdded.Count + packagesRemoved.Count + packagesPostUpdate.Count > 0)
                    onPackagesChanged?.Invoke(packagesAdded, packagesRemoved, packagesPreUpdate, packagesPostUpdate);

                // special handling to make sure onInstallSuccess events are called correctly when special unique id is used
                for (var i = m_SpecialInstallations.Count - 1; i >= 0; i--)
                {
                    var match = packagesInstalled.FirstOrDefault(p => p.versions.installed.uniqueId.ToLower().Contains(m_SpecialInstallations[i].ToLower()));
                    if (match != null)
                    {
                        onInstallSuccess(match, match.versions.installed);
                        SetPackageProgress(match, PackageProgress.None);
                        m_SpecialInstallations.RemoveAt(i);
                    }
                }
            }

            private void OnUpmAddOperation(IOperation operation)
            {
                // if we don't know the package unique id before hand, we'll do some special handling
                // as we don't know what package will be installed until the installation finishes (e.g, git packages)
                if (string.IsNullOrEmpty(operation.packageUniqueId))
                {
                    m_SpecialInstallations.Add(operation.specialUniqueId);
                    operation.onOperationError += (op, error) =>
                    {
                        m_SpecialInstallations.Remove(operation.specialUniqueId);
                    };
                    return;
                }
                SetPackageProgress(GetPackage(operation.packageUniqueId), PackageProgress.Installing);
                operation.onOperationSuccess += (op) =>
                {
                    IPackage package;
                    IPackageVersion version;
                    GetPackageAndVersion(operation.packageUniqueId, operation.versionUniqueId, out package, out version);
                    onInstallSuccess(package, version);
                };
                operation.onOperationError += OnUpmOperationError;
                operation.onOperationFinalized += OnUpmOperationFinalized;
            }

            private void OnUpmEmbedOperation(IOperation operation)
            {
                SetPackageProgress(GetPackage(operation.packageUniqueId), PackageProgress.Installing);
                operation.onOperationSuccess += (op) =>
                {
                    var package = GetPackage(operation.packageUniqueId);
                    onInstallSuccess(package, package?.versions.installed);
                };
                operation.onOperationError += OnUpmOperationError;
                operation.onOperationFinalized += OnUpmOperationFinalized;
            }

            private void OnUpmRemoveOperation(IOperation operation)
            {
                SetPackageProgress(GetPackage(operation.packageUniqueId), PackageProgress.Removing);
                operation.onOperationSuccess += (op) => onUninstallSuccess(GetPackage(operation.packageUniqueId));
                operation.onOperationError += OnUpmOperationError;
                operation.onOperationFinalized += OnUpmOperationFinalized;
            }

            private void OnUpmOperationError(IOperation operation, UIError error)
            {
                var package = GetPackage(operation.packageUniqueId);
                if (package != null)
                    AddPackageError(package, error);
            }

            private void OnUpmOperationFinalized(IOperation operation)
            {
                SetPackageProgress(GetPackage(operation.packageUniqueId), PackageProgress.None);
            }

            private void OnUpmPackageVersionUpdated(string packageUniqueId, IPackageVersion version)
            {
                var package = GetPackage(packageUniqueId);
                var upmVersions = package?.versions as UpmVersionList;
                if (upmVersions != null)
                {
                    var packagePreUpdate = package.Clone();
                    upmVersions.UpdateVersion(version as UpmPackageVersion);
                    onPackagesChanged?.Invoke(k_EmptyList, k_EmptyList, new[] { packagePreUpdate }, new[] { package });
                }
            }

            public void Install(IPackageVersion version)
            {
                if (version.isInstalled)
                    return;
                UpmClient.instance.AddById(version.uniqueId);
            }

            public void InstallFromUrl(string url)
            {
                UpmClient.instance.AddByUrl(url);
            }

            public void InstallFromPath(string path)
            {
                UpmClient.instance.AddByPath(path);
            }

            public void Uninstall(IPackage package)
            {
                if (package.versions.installed == null)
                    return;
                UpmClient.instance.RemoveByName(package.name);
            }

            public void Embed(IPackageVersion packageVersion)
            {
                if (packageVersion == null || !packageVersion.HasTag(PackageTag.Embeddable))
                    return;
                UpmClient.instance.EmbedByName(packageVersion.name);
            }

            public void RemoveEmbedded(IPackage package)
            {
                if (package.versions.installed == null)
                    return;
                UpmClient.instance.RemoveEmbeddedByName(package.name);
            }

            public void FetchExtraInfo(IPackageVersion version)
            {
                if (version.isFullyFetched)
                    return;
                UpmClient.instance.ExtraFetch(version.uniqueId);
            }

            public bool IsDownloadInProgress(IPackageVersion version)
            {
                return AssetStoreDownloadManager.instance.GetDownloadOperation(version.packageUniqueId)?.isInProgress ?? false;
            }

            public void Download(IPackage package)
            {
                if (!(package is AssetStorePackage))
                    return;

                if (!PlayModeDownload.CanBeginDownload())
                    return;

                SetPackageProgress(package, PackageProgress.Downloading);
                AssetStoreDownloadManager.instance.Download(package.uniqueId);
            }

            public void AbortDownload(IPackage package)
            {
                if (!(package is AssetStorePackage))
                    return;
                SetPackageProgress(package, PackageProgress.None);
                AssetStoreDownloadManager.instance.AbortDownload(package.uniqueId);
            }

            public void Import(IPackage package)
            {
                if (!(package is AssetStorePackage))
                    return;

                var path = package.versions.primary.localPath;
                if (File.Exists(path))
                {
                    AssetDatabase.ImportPackage(path, true);
                }
            }
        }
    }
}
