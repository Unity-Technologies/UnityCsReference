// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.PackageManager.UI.AssetStore;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal sealed class PackageDatabase
    {
        static IPackageDatabase s_Instance = null;
        public static IPackageDatabase instance { get { return s_Instance ?? PackageDatabaseInternal.instance; } }

        public static RefreshOptions GetRefreshOptionsFromFilterTab(PackageFilterTab tab)
        {
            var options = RefreshOptions.None;
            switch (tab)
            {
                case PackageFilterTab.Unity:
                case PackageFilterTab.Other:
                    options |= RefreshOptions.UpmList;
                    options |= RefreshOptions.UpmSearch;
                    break;
                case PackageFilterTab.Local:
                    options |= RefreshOptions.UpmList;
                    break;
                case PackageFilterTab.Modules:
                    options |= RefreshOptions.UpmSearchOffline;
                    options |= RefreshOptions.UpmListOffline;
                    break;
                case PackageFilterTab.AssetStore:
                    options |= RefreshOptions.Purchased;
                    break;
                case PackageFilterTab.Custom:
                    options |= RefreshOptions.UpmList;
                    break;
            }

            return options;
        }

        [Serializable]
        private class PackageDatabaseInternal : ScriptableSingleton<PackageDatabaseInternal>, IPackageDatabase, ISerializationCallbackReceiver
        {
            public event Action<IPackage, IPackageVersion> onInstallSuccess = delegate {};
            public event Action<IPackage> onUninstallSuccess = delegate {};
            public event Action<IPackage> onPackageOperationStart = delegate {};
            public event Action<IPackage> onPackageOperationFinish = delegate {};
            public event Action onRefreshOperationStart = delegate {};
            public event Action<PackageFilterTab> onRefreshOperationFinish = delegate {};
            public event Action<Error> onRefreshOperationError = delegate {};

            public event Action<DownloadProgress> onDownloadProgress = delegate {};

            // args 1,2, 3 are added, removed and preUpdated, and postUpdated packages respectively
            public event Action<IEnumerable<IPackage>, IEnumerable<IPackage>, IEnumerable<IPackage>, IEnumerable<IPackage>> onPackagesChanged = delegate {};

            public event Action<TermOfServiceAgreementStatus> onTermOfServiceAgreementStatusChange = delegate {};

            private readonly Dictionary<string, IPackage> m_Packages = new Dictionary<string, IPackage>();
            // a list of unique ids (could be specialUniqueId or packageId)
            private List<string> m_SpecialInstallations = new List<string>();

            // array created to help serialize dictionaries
            [SerializeField]
            private List<UpmPackage> m_SerializedUpmPackages = new List<UpmPackage>();

            [SerializeField]
            private List<AssetStorePackage> m_SerializedAssetStorePackages = new List<AssetStorePackage>();

            [SerializeField]
            private RefreshOptions[] m_SerializedRefreshTimestampsKeys = new RefreshOptions[0];

            [SerializeField]
            private long[] m_SerializedRefreshTimestampsValues = new long[0];

            [NonSerialized]
            private List<IOperation> m_RefreshOperationsInProgress = new List<IOperation>();

            [SerializeField]
            private List<PlaceholderPackage> m_SerializedPlaceholderPackages = new List<PlaceholderPackage>();

            [NonSerialized]
            private bool m_EventsRegistered;

            [SerializeField]
            private TermOfServiceAgreementStatus m_TermOfServiceAgreementStatus = TermOfServiceAgreementStatus.NotAccepted;

            public bool isEmpty { get { return !m_Packages.Any(); } }

            private static readonly IPackage[] k_EmptyList = new IPackage[0] {};

            public bool isInstallOrUninstallInProgress
            {
                // add, embed -> install, remove -> uninstall
                get { return UpmClient.instance.isAddRemoveOrEmbedInProgress; }
            }

            public virtual TermOfServiceAgreementStatus termOfServiceAgreementStatus
            {
                get
                {
                    return m_TermOfServiceAgreementStatus;
                }
                internal set
                {
                    m_TermOfServiceAgreementStatus = value;
                    onTermOfServiceAgreementStatusChange?.Invoke(m_TermOfServiceAgreementStatus);
                }
            }

            public IEnumerable<IPackage> allPackages { get { return m_Packages.Values; } }
            public IEnumerable<IPackage> assetStorePackages { get { return m_Packages.Values.Where(p => p is AssetStorePackage); } }
            public IEnumerable<IPackage> upmPackages { get { return m_Packages.Values.Where(p => p is UpmPackage); } }

            private Dictionary<RefreshOptions, long> m_RefreshTimestamps = new Dictionary<RefreshOptions, long>();

            private PackageDatabaseInternal()
            {
                m_RefreshOperationsInProgress = new List<IOperation>();
            }

            public bool IsUninstallInProgress(IPackage package)
            {
                return UpmClient.instance.IsRemoveInProgress(package.uniqueId);
            }

            public bool IsInstallInProgress(IPackageVersion version)
            {
                return UpmClient.instance.IsAddInProgress(version.uniqueId) || UpmClient.instance.IsEmbedInProgress(version.uniqueId);
            }

            public IPackage GetPackageByDisplayName(string displayName)
            {
                return m_Packages.Values.FirstOrDefault(p => p.displayName == displayName);
            }

            public IPackage GetPackage(string uniqueId)
            {
                if (string.IsNullOrEmpty(uniqueId))
                    return null;
                IPackage result;
                return m_Packages.TryGetValue(uniqueId, out result) ? result : null;
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
                    SemVersion parsedVersion = versionIdentifier;
                    version = package.versions.FirstOrDefault(v => v.version == parsedVersion);
                }
                else
                {
                    var packageId = UpmPackageVersion.FormatPackageId(name, versionIdentifier);
                    version = package.versions.FirstOrDefault(v => v.uniqueId == packageId);
                }
            }

            public IEnumerable<IPackageVersion> GetDependentVersions(IPackageVersion version)
            {
                var installedRoots = allPackages.Select(p => p.installedVersion).Where(p => p?.isDirectDependency ?? false);
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

                for (var i = 0; i < m_SerializedRefreshTimestampsKeys.Length; i++)
                    m_RefreshTimestamps[m_SerializedRefreshTimestampsKeys[i]] = m_SerializedRefreshTimestampsValues[i];
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

                m_SerializedRefreshTimestampsKeys = m_RefreshTimestamps.Keys.ToArray();
                m_SerializedRefreshTimestampsValues = m_RefreshTimestamps.Values.ToArray();
            }

            public void AddPackageError(IPackage package, Error error)
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

            public void RegisterEvents()
            {
                if (m_EventsRegistered)
                    return;

                m_EventsRegistered = true;

                UpmClient.instance.onPackagesChanged += OnPackagesChanged;
                UpmClient.instance.onPackageVersionUpdated += OnUpmPackageVersionUpdated;

                UpmClient.instance.onListOperation += OnUpmListOrSearchOperation;
                UpmClient.instance.onListOperation += OnUpmListOperation;
                UpmClient.instance.onSearchAllOperation += OnUpmListOrSearchOperation;
                UpmClient.instance.onSearchAllOperation += OnUpmSearchAllOperation;

                UpmClient.instance.onAddOperation += OnUpmAddOperation;
                UpmClient.instance.onEmbedOperation += OnUpmEmbedOperation;
                UpmClient.instance.onRemoveOperation += OnUpmRemoveOperation;
                UpmClient.instance.RegisterEvents();

                AssetStore.AssetStoreClient.instance.onPackagesChanged += OnPackagesChanged;
                AssetStore.AssetStoreClient.instance.onPackageVersionUpdated += OnUpmPackageVersionUpdated;
                AssetStore.AssetStoreClient.instance.onDownloadProgress += OnDownloadProgress;
                AssetStore.AssetStoreClient.instance.onListOperationStart += OnAssetStoreOperationStart;
                AssetStore.AssetStoreClient.instance.onListOperationFinish += OnAssetStoreOperationFinish;
                AssetStore.AssetStoreClient.instance.onOperationError += OnAssetStoreOperationError;
                AssetStore.AssetStoreClient.instance.RegisterEvents();

                ApplicationUtil.instance.onUserLoginStateChange += OnUserLoginStateChange;
            }

            public void UnregisterEvents()
            {
                if (!m_EventsRegistered)
                    return;

                m_EventsRegistered = false;

                UpmClient.instance.onPackagesChanged -= OnPackagesChanged;
                UpmClient.instance.onPackageVersionUpdated -= OnUpmPackageVersionUpdated;

                UpmClient.instance.onListOperation -= OnUpmListOrSearchOperation;
                UpmClient.instance.onListOperation -= OnUpmListOperation;
                UpmClient.instance.onSearchAllOperation -= OnUpmListOrSearchOperation;
                UpmClient.instance.onSearchAllOperation -= OnUpmSearchAllOperation;

                UpmClient.instance.onAddOperation -= OnUpmAddOperation;
                UpmClient.instance.onEmbedOperation -= OnUpmEmbedOperation;
                UpmClient.instance.onRemoveOperation -= OnUpmRemoveOperation;
                UpmClient.instance.UnregisterEvents();

                AssetStore.AssetStoreClient.instance.onPackagesChanged -= OnPackagesChanged;
                AssetStore.AssetStoreClient.instance.onPackageVersionUpdated -= OnUpmPackageVersionUpdated;
                AssetStore.AssetStoreClient.instance.onDownloadProgress -= OnDownloadProgress;
                AssetStore.AssetStoreClient.instance.onListOperationStart -= OnAssetStoreOperationStart;
                AssetStore.AssetStoreClient.instance.onListOperationFinish -= OnAssetStoreOperationFinish;
                AssetStore.AssetStoreClient.instance.onOperationError -= OnAssetStoreOperationError;
                AssetStore.AssetStoreClient.instance.UnregisterEvents();

                ApplicationUtil.instance.onUserLoginStateChange -= OnUserLoginStateChange;
            }

            public void Reload()
            {
                onPackagesChanged?.Invoke(Enumerable.Empty<IPackage>(), m_Packages.Values, Enumerable.Empty<IPackage>(), Enumerable.Empty<IPackage>());

                UnregisterEvents();

                m_TermOfServiceAgreementStatus = TermOfServiceAgreementStatus.NotAccepted;

                AssetStore.AssetStoreClient.instance.ClearCache();
                UpmClient.instance.ClearCache();

                m_Packages.Clear();
                m_SerializedUpmPackages = new List<UpmPackage>();
                m_SerializedAssetStorePackages = new List<AssetStorePackage>();
                m_SpecialInstallations.Clear();

                m_RefreshTimestamps.Clear();
                InitializeRefreshTimestamps();
                m_RefreshOperationsInProgress.Clear();

                RegisterEvents();
            }

            private void InitializeRefreshTimestamps()
            {
                foreach (RefreshOptions filter in Enum.GetValues(typeof(RefreshOptions)))
                {
                    if (m_RefreshTimestamps.ContainsKey(filter))
                        continue;
                    m_RefreshTimestamps[filter] = 0;
                }
            }

            private void OnDownloadProgress(DownloadProgress progress)
            {
                var package = GetPackage(progress.packageId);
                if (package != null)
                {
                    var hasError = progress.state == DownloadProgress.State.Error || progress.state == DownloadProgress.State.Aborted;
                    if (hasError)
                        package.AddError(new Error(NativeErrorCode.Unknown, progress.message));

                    if (progress.state == DownloadProgress.State.Completed)
                    {
                        AssetStore.AssetStoreClient.instance.RefreshLocal();
                    }

                    onDownloadProgress?.Invoke(progress);
                }
            }

            private void OnAssetStoreOperationStart()
            {
                onRefreshOperationStart?.Invoke();
            }

            private void OnAssetStoreOperationFinish()
            {
                m_RefreshTimestamps[RefreshOptions.Purchased] = DateTime.Now.Ticks;
                onRefreshOperationFinish?.Invoke(PackageFilterTab.AssetStore);
            }

            private void OnAssetStoreOperationError(Error error)
            {
                onRefreshOperationError?.Invoke(error);
            }

            private void OnUserLoginStateChange(bool loggedIn)
            {
                m_TermOfServiceAgreementStatus = TermOfServiceAgreementStatus.NotAccepted;

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

                    if (m_SpecialInstallations.Any() && package.installedVersion != null && oldPackage?.installedVersion == null)
                        packagesInstalled.Add(package);
                }

                if (packagesAdded.Count + packagesRemoved.Count + packagesPostUpdate.Count > 0)
                    onPackagesChanged?.Invoke(packagesAdded, packagesRemoved, packagesPreUpdate, packagesPostUpdate);

                // special handling to make sure onInstallSuccess events are called correctly when special unique id is used
                for (var i = m_SpecialInstallations.Count - 1; i >= 0; i--)
                {
                    var match = packagesInstalled.FirstOrDefault(p => p.installedVersion.uniqueId.ToLower().Contains(m_SpecialInstallations[i].ToLower()));
                    if (match != null)
                    {
                        onInstallSuccess(match, match.installedVersion);
                        onPackageOperationFinish(match);
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
                    onPackageOperationStart(null);
                    operation.onOperationError += error =>
                    {
                        m_SpecialInstallations.Remove(operation.specialUniqueId);
                        onPackageOperationFinish(null);
                    };
                    return;
                }

                onPackageOperationStart(GetPackage(operation.packageUniqueId));
                operation.onOperationSuccess += () =>
                {
                    IPackage package;
                    IPackageVersion version;
                    GetPackageAndVersion(operation.packageUniqueId, operation.versionUniqueId, out package, out version);
                    onInstallSuccess(package, version);
                };
                operation.onOperationError += error =>
                {
                    var package = GetPackage(operation.packageUniqueId);
                    if (package != null)
                        AddPackageError(package, error);
                };
                operation.onOperationFinalized += () => onPackageOperationFinish(GetPackage(operation.packageUniqueId));
            }

            private void OnUpmEmbedOperation(IOperation operation)
            {
                onPackageOperationStart(GetPackage(operation.packageUniqueId));
                operation.onOperationSuccess += () =>
                {
                    var package = GetPackage(operation.packageUniqueId);
                    onInstallSuccess(package, package?.installedVersion);
                };
                operation.onOperationError += error =>
                {
                    var package = GetPackage(operation.packageUniqueId);
                    if (package != null)
                        AddPackageError(package, error);
                };
                operation.onOperationFinalized += () => onPackageOperationFinish(GetPackage(operation.packageUniqueId));
            }

            private void OnUpmRemoveOperation(IOperation operation)
            {
                onPackageOperationStart(GetPackage(operation.packageUniqueId));
                operation.onOperationSuccess += () => onUninstallSuccess(GetPackage(operation.packageUniqueId));
                operation.onOperationError += error =>
                {
                    var package = GetPackage(operation.packageUniqueId);
                    if (package != null)
                        AddPackageError(package, error);
                };
                operation.onOperationFinalized += () => onPackageOperationFinish(GetPackage(operation.packageUniqueId));
            }

            private void OnUpmSearchAllOperation(IOperation operation)
            {
                var refreshOption = operation.isOfflineMode ? RefreshOptions.UpmSearchOffline : RefreshOptions.UpmSearch;
                operation.onOperationSuccess += () => UpdateTimestamp(refreshOption, operation);
            }

            private void OnUpmListOperation(IOperation operation)
            {
                var refreshOption = operation.isOfflineMode ? RefreshOptions.UpmListOffline : RefreshOptions.UpmList;
                operation.onOperationSuccess += () => UpdateTimestamp(refreshOption, operation);
            }

            private void UpdateTimestamp(RefreshOptions refreshOption, IOperation operation)
            {
                m_RefreshTimestamps[refreshOption] = operation.timestamp;
            }

            private void OnUpmListOrSearchOperation(IOperation operation)
            {
                m_RefreshOperationsInProgress.Add(operation);
                operation.onOperationFinalized += () => { OnListOrSearchOperationFinalized(operation); };
                operation.onOperationError += onRefreshOperationError;
                if (m_RefreshOperationsInProgress.Count > 1)
                    return;
                onRefreshOperationStart();
            }

            private void OnListOrSearchOperationFinalized(IOperation operation)
            {
                m_RefreshOperationsInProgress.Remove(operation);
                if (m_RefreshOperationsInProgress.Any())
                    return;
                onRefreshOperationFinish(PackageFiltering.instance.currentFilterTab);
            }

            private void OnUpmPackageVersionUpdated(string packageUniqueId, IPackageVersion version)
            {
                var package = GetPackage(packageUniqueId);
                var upmVersions = package?.versionList as UpmVersionList;
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
                if (package.installedVersion == null)
                    return;
                UpmClient.instance.RemoveByName(package.name);
            }

            public void Embed(IPackageVersion packageVersion)
            {
                if (packageVersion == null || !packageVersion.canBeEmbedded)
                    return;
                UpmClient.instance.EmbedByName(packageVersion.name);
            }

            public void RemoveEmbedded(IPackage package)
            {
                if (package.installedVersion == null)
                    return;
                UpmClient.instance.RemoveEmbeddedByName(package.name);
            }

            public void FetchExtraInfo(IPackageVersion version)
            {
                if (version.isFullyFetched)
                    return;
                UpmClient.instance.ExtraFetch(version.uniqueId);
            }

            public DownloadProgress GetDownloadProgress(IPackageVersion version)
            {
                DownloadProgress progress;
                AssetStore.AssetStoreClient.instance.GetDownloadProgress(version.packageUniqueId, out progress);
                return progress;
            }

            public bool IsDownloadInProgress(IPackageVersion version)
            {
                return AssetStore.AssetStoreClient.instance.IsDownloadInProgress(version.packageUniqueId);
            }

            public bool Download(IPackage package)
            {
                if (!(package is AssetStorePackage))
                    return false;

                if (!PlayModeDownload.CanBeginDownload())
                    return false;

                if (termOfServiceAgreementStatus == TermOfServiceAgreementStatus.NotAccepted)
                {
                    AssetStore.AssetStoreClient.instance.CheckTermOfServiceAgreement(status =>
                    {
                        termOfServiceAgreementStatus = status;
                        if (termOfServiceAgreementStatus == TermOfServiceAgreementStatus.Accepted)
                            Download_Internal(package);
                    });
                    return false;
                }

                Download_Internal(package);
                return true;
            }

            private void Download_Internal(IPackage package)
            {
                AssetStore.AssetStoreClient.instance.Download(package.uniqueId);
            }

            public void AbortDownload(IPackage package)
            {
                if (!(package is AssetStorePackage))
                    return;

                AssetStore.AssetStoreClient.instance.AbortDownload(package.uniqueId);
            }

            public void Import(IPackage package)
            {
                if (!(package is AssetStorePackage))
                    return;

                var path = package.primaryVersion.localPath;
                if (File.Exists(path))
                {
                    AssetDatabase.ImportPackage(path, true);
                }
            }

            public long GetRefreshTimestamp(PackageFilterTab? tab = null)
            {
                var filterTab = tab ?? PackageFiltering.instance.currentFilterTab;
                return GetRefreshTimestamp(GetRefreshOptionsFromFilterTab(filterTab));
            }

            public bool IsRefreshInProgress(PackageFilterTab? tab = null)
            {
                var filterTab = tab ?? PackageFiltering.instance.currentFilterTab;
                var refreshOptions = GetRefreshOptionsFromFilterTab(filterTab);
                return m_RefreshOperationsInProgress.Any(operation => (operation.refreshOptions & refreshOptions) != 0);
            }

            private long GetRefreshTimestamp(RefreshOptions option)
            {
                var result = 0L;
                foreach (var item in m_RefreshTimestamps)
                {
                    if ((option & item.Key) == 0)
                        continue;
                    if (result == 0)
                        result = item.Value;
                    else if (result > item.Value && item.Value > 0)
                        result = item.Value;
                }
                return result;
            }
        }
    }
}
