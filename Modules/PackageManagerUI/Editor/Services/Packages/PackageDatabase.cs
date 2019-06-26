// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal sealed class PackageDatabase
    {
        static IPackageDatabase s_Instance = null;
        public static IPackageDatabase instance { get { return s_Instance ?? PackageDatabaseInternal.instance; } }

        [Serializable]
        private class PackageDatabaseInternal : ScriptableSingleton<PackageDatabaseInternal>, IPackageDatabase, ISerializationCallbackReceiver
        {
            public event Action<long> onUpdateTimeChange = delegate {};

            public event Action<IEnumerable<IPackage>, IEnumerable<IPackage>, IEnumerable<IPackage>> onPackagesChanged = delegate {};
            public event Action<IPackageVersion> onPackageVersionUpdated = delegate {};

            public event Action<IPackage, IPackageVersion> onInstallSuccess = delegate {};
            public event Action<IPackage> onUninstallSuccess = delegate {};
            public event Action<IPackage> onPackageOperationStart = delegate {};
            public event Action<IPackage> onPackageOperationFinish = delegate {};
            public event Action onRefreshOperationStart = delegate {};
            public event Action onRefreshOperationFinish = delegate {};
            public event Action<Error> onRefreshOperationError = delegate {};

            private readonly Dictionary<string, IPackage> m_Packages = new Dictionary<string, IPackage>();

            // a list of unique ids (could be specialUniqueId or packageId)
            private List<string> m_SpecialInstallation = new List<string>();

            // arrays created to help serialize dictionaries
            private UpmPackage[] m_SerializedUpmPackages;

            [NonSerialized]
            private List<IOperation> m_RefreshOperationsInProgress;

            public bool isEmpty { get { return !m_Packages.Any(); } }

            public bool isInstallOrUninstallInProgress
            {
                // add, embed -> install, remove -> uninstall
                get { return UpmClient.instance.isAddRemoveOrEmbedInProgress; }
            }

            public IEnumerable<IPackage> allPackages { get { return m_Packages.Values; } }

            private long m_LastUpdateTimestamp = 0;
            public long lastUpdateTimestamp { get { return m_LastUpdateTimestamp; } }

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
                var dependsOnPackage = installedRoots.Where(p =>
                    p.resolvedDependencies.Select(r => r.name).Contains(version.name));

                return dependsOnPackage;
            }

            public void OnAfterDeserialize()
            {
                foreach (var p in m_SerializedUpmPackages)
                    m_Packages[p.uniqueId] = p;
            }

            public void OnBeforeSerialize()
            {
                m_SerializedUpmPackages = m_Packages.Values.Cast<UpmPackage>().ToArray();
            }

            public void AddPackageError(IPackage package, Error error)
            {
                package.AddError(error);
                onPackagesChanged(Enumerable.Empty<IPackage>(), Enumerable.Empty<IPackage>(), new IPackage[] { package });
            }

            public void ClearPackageErrors(IPackage package)
            {
                package.ClearErrors();
                onPackagesChanged(Enumerable.Empty<IPackage>(), Enumerable.Empty<IPackage>(), new IPackage[] { package });
            }

            public void Setup()
            {
                UpmClient.instance.Setup();

                UpmClient.instance.onPackagesChanged += OnUpmPackagesChanged;
                UpmClient.instance.onPackageVersionUpdated += OnUpmPackageVersionUpdated;

                UpmClient.instance.onListOperation += OnUpmListOrSearchOperation;
                UpmClient.instance.onSearchAllOperation += OnUpmListOrSearchOperation;
                UpmClient.instance.onSearchAllOperation += OnUpmSearchAllOperation;

                UpmClient.instance.onAddOperation += OnUpmAddOperation;
                UpmClient.instance.onEmbedOperation += OnUpmEmbedOperation;
                UpmClient.instance.onRemoveOperation += OnUpmRemoveOperation;
            }

            public void Clear()
            {
                m_Packages.Clear();
                m_SerializedUpmPackages = new UpmPackage[0];
                m_SpecialInstallation.Clear();
                m_RefreshOperationsInProgress.Clear();
                m_LastUpdateTimestamp = 0;

                UpmClient.instance.Clear();
            }

            private void OnUpmPackagesChanged(IEnumerable<IPackage> packages)
            {
                if (!packages.Any())
                    return;

                var addedList = new List<IPackage>();
                var updatedList = new List<IPackage>();
                var removedList = new List<IPackage>();
                foreach (var package in packages)
                {
                    var packageName = package.name;
                    var isEmptyPackage = !package.versions.Any();
                    var oldPackage = GetPackage(packageName);

                    if (oldPackage != null && isEmptyPackage)
                    {
                        removedList.Add(m_Packages[packageName]);
                        m_Packages.Remove(packageName);
                    }
                    else if (!isEmptyPackage)
                    {
                        m_Packages[packageName] = package;
                        if (oldPackage != null)
                            updatedList.Add(package);
                        else
                            addedList.Add(package);
                    }
                }
                if (addedList.Any() || updatedList.Any() || removedList.Any())
                    onPackagesChanged(addedList, removedList, updatedList);

                // special handling to make sure onInstallSuccess events are called correctly when special unique id is used
                if (m_SpecialInstallation.Any())
                {
                    var potentialInstalls = addedList.Concat(updatedList).Where(p => p.installedVersion != null);

                    for (var i = m_SpecialInstallation.Count - 1; i >= 0; i--)
                    {
                        var match = potentialInstalls.FirstOrDefault(p => p.installedVersion.uniqueId.ToLower().Contains(m_SpecialInstallation[i].ToLower()));
                        if (match != null)
                        {
                            onInstallSuccess(match, match.installedVersion);
                            onPackageOperationFinish(match);
                            m_SpecialInstallation.RemoveAt(i);
                        }
                    }
                }
            }

            private void OnUpmAddOperation(IOperation operation)
            {
                // if we don't know the package unique id before hand, we'll do some special handling
                // as we don't know what package will be installed until the installation finishes (e.g, git packages)
                if (string.IsNullOrEmpty(operation.packageUniqueId))
                {
                    m_SpecialInstallation.Add(operation.specialUniqueId);
                    onPackageOperationStart(null);
                    operation.onOperationError += error =>
                    {
                        m_SpecialInstallation.Remove(operation.specialUniqueId);
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
                if (!operation.isOfflineMode)
                    operation.onOperationSuccess += () =>
                    {
                        m_LastUpdateTimestamp = operation.timestamp;
                        onUpdateTimeChange(operation.timestamp);
                    };
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
                onRefreshOperationFinish();
            }

            private void OnUpmPackageVersionUpdated(IPackageVersion version)
            {
                var upmPackage = GetPackage(version.packageInfo.name) as UpmPackage;
                if (upmPackage != null)
                {
                    upmPackage.UpdateVersion(version as UpmPackageVersion);
                    onPackageVersionUpdated(version);
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
                UpmClient.instance.RemoveByName(package.uniqueId);
            }

            public void Embed(IPackage package)
            {
                if (package.installedVersion == null)
                    return;
                UpmClient.instance.EmbedByName(package.uniqueId);
            }

            public void RemoveEmbedded(IPackage package)
            {
                if (package.installedVersion == null)
                    return;
                UpmClient.instance.RemoveEmbeddedByName(package.uniqueId);
            }

            public void FetchExtraInfo(IPackageVersion version)
            {
                if (version.isFullyFetched)
                    return;
                UpmClient.instance.ExtraFetch(version.uniqueId);
            }

            public void Refresh(RefreshOptions options)
            {
                var offlineMode = (options & RefreshOptions.OfflineMode) != 0;
                if ((options & RefreshOptions.ListInstalled) != 0)
                    UpmClient.instance.List(offlineMode);
                if ((options & RefreshOptions.SearchAll) != 0)
                    UpmClient.instance.SearchAll(offlineMode);
            }
        }
    }
}
