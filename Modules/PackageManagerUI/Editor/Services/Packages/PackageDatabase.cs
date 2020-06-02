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
    [Serializable]
    internal class PackageDatabase : ISerializationCallbackReceiver
    {
        // This instance reference is kept for compatibility reasons, as it is internal visible to the Upm Develop package
        // To be addressed further in https://jira.unity3d.com/browse/PAX-1317
        internal static PackageDatabase instance => ServicesContainer.instance.Resolve<PackageDatabase>();

        public virtual event Action<IPackage, IPackageVersion> onInstallSuccess = delegate {};
        public virtual event Action<IPackage> onUninstallSuccess = delegate {};
        public virtual event Action<IPackage> onPackageProgressUpdate = delegate {};

        public virtual event Action<IEnumerable<IPackage> /*added*/,
                                    IEnumerable<IPackage> /*removed*/,
                                    IEnumerable<IPackage> /*preUpdated*/,
                                    IEnumerable<IPackage> /*postUpdated*/> onPackagesChanged = delegate {};

        private readonly Dictionary<string, IPackage> m_Packages = new Dictionary<string, IPackage>();

        private readonly Dictionary<string, IEnumerable<Sample>> m_ParsedSamples = new Dictionary<string, IEnumerable<Sample>>();

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
        private UnityConnectProxy m_UnityConnect;
        [NonSerialized]
        private AssetDatabaseProxy m_AssetDatabase;
        [NonSerialized]
        private AssetStoreClient m_AssetStoreClient;
        [NonSerialized]
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        [NonSerialized]
        private UpmClient m_UpmClient;
        [NonSerialized]
        private IOProxy m_IOProxy;
        public void ResolveDependencies(UnityConnectProxy unityConnect,
            AssetDatabaseProxy assetDatabase,
            AssetStoreUtils assetStoreUtils,
            AssetStoreClient assetStoreClient,
            AssetStoreDownloadManager assetStoreDownloadManager,
            UpmClient upmClient,
            IOProxy ioProxy)
        {
            m_UnityConnect = unityConnect;
            m_AssetDatabase = assetDatabase;
            m_AssetStoreClient = assetStoreClient;
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_UpmClient = upmClient;
            m_IOProxy = ioProxy;

            foreach (var package in m_SerializedAssetStorePackages)
                package.ResolveDependencies(assetStoreUtils, ioProxy);
        }

        public virtual bool isEmpty { get { return !m_Packages.Any(); } }

        private static readonly IPackage[] k_EmptyList = new IPackage[0] {};

        public virtual bool isInstallOrUninstallInProgress
        {
            // add, embed -> install, remove -> uninstall
            get { return m_UpmClient.isAddRemoveOrEmbedInProgress; }
        }

        public virtual IEnumerable<IPackage> allPackages { get { return m_Packages.Values; } }
        public virtual IEnumerable<IPackage> assetStorePackages { get { return m_Packages.Values.Where(p => p is AssetStorePackage); } }
        public virtual IEnumerable<IPackage> upmPackages { get { return m_Packages.Values.Where(p => p is UpmPackage); } }

        public virtual bool IsUninstallInProgress(IPackage package)
        {
            return m_UpmClient.IsRemoveInProgress(package.uniqueId);
        }

        public virtual bool IsInstallInProgress(IPackageVersion version)
        {
            return m_UpmClient.IsAddInProgress(version.uniqueId) || m_UpmClient.IsEmbedInProgress(version.uniqueId);
        }

        public virtual IPackage GetPackage(string uniqueId)
        {
            return string.IsNullOrEmpty(uniqueId) ? null : m_Packages.Get(uniqueId);
        }

        public virtual IPackage GetPackage(IPackageVersion version)
        {
            return GetPackage(version?.packageUniqueId);
        }

        // In some situations, we only know an id (could be package unique id, or version unique id) or just a name (package Name, or display name)
        // but we still might be able to find a package and a version that matches the criteria
        public virtual void GetPackageAndVersionByIdOrName(string idOrName, out IPackage package, out IPackageVersion version)
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

        public virtual void GetPackageAndVersion(string packageUniqueId, string versionUniqueId, out IPackage package, out IPackageVersion version)
        {
            package = GetPackage(packageUniqueId);
            version = package?.versions.FirstOrDefault(v => v.uniqueId == versionUniqueId);
        }

        public virtual IPackageVersion GetPackageVersion(string packageUniqueId, string versionUniqueId)
        {
            IPackage package;
            IPackageVersion version;
            GetPackageAndVersion(packageUniqueId, versionUniqueId, out package, out version);
            return version;
        }

        public virtual IPackageVersion GetPackageVersion(DependencyInfo info)
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

        public virtual IEnumerable<IPackageVersion> GetReverseDependencies(IPackageVersion version)
        {
            if (version?.dependencies == null)
                return null;
            var installedRoots = allPackages.Select(p => p.versions.installed).Where(p => p?.isDirectDependency ?? false);
            var dependsOnPackage = installedRoots.Where(p => p.resolvedDependencies?.Any(r => r.name == version.name) ?? false);
            return dependsOnPackage;
        }

        private IEnumerable<Sample> GetSamplesFromPackageInfo(PackageInfo packageInfo)
        {
            if (string.IsNullOrEmpty(packageInfo?.resolvedPath))
                return Enumerable.Empty<Sample>();

            var jsonPath = Path.Combine(packageInfo.resolvedPath, "package.json");
            if (!m_IOProxy.FileExists(jsonPath))
                return Enumerable.Empty<Sample>();

            try
            {
                var packageJson = Json.Deserialize(m_IOProxy.FileReadAllText(jsonPath)) as Dictionary<string, object>;
                var samples = packageJson.GetList<IDictionary<string, object>>("samples");
                return samples?.Select(sample =>
                {
                    var displayName = sample.GetString("displayName");
                    var path = sample.GetString("path");
                    var description = sample.GetString("description");
                    var interactiveImport = sample.Get("interactiveImport", false);

                    var resolvedSamplePath = Path.Combine(packageInfo.resolvedPath, path);
                    var importPath = IOUtils.CombinePaths(
                        Application.dataPath,
                        "Samples",
                        IOUtils.SanitizeFileName(packageInfo.displayName),
                        packageInfo.version,
                        IOUtils.SanitizeFileName(displayName)
                    );
                    return new Sample(m_IOProxy, m_AssetDatabase, displayName, description, resolvedSamplePath, importPath, interactiveImport);
                });
            }
            catch (Exception)
            {
                return Enumerable.Empty<Sample>();
            }
        }

        public virtual IEnumerable<Sample> GetSamples(IPackageVersion version)
        {
            if (version?.packageInfo == null || version.packageInfo.version != version.version?.ToString())
                return Enumerable.Empty<Sample>();

            if (m_ParsedSamples.TryGetValue(version.uniqueId, out var parsedSamples))
                return parsedSamples;

            var samples = GetSamplesFromPackageInfo(version.packageInfo);
            m_ParsedSamples[version.uniqueId] = samples;
            return samples;
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

        public virtual void AddPackageError(IPackage package, UIError error)
        {
            var packagePreUpdate = package.Clone();
            package.AddError(error);
            onPackagesChanged?.Invoke(k_EmptyList, k_EmptyList, new[] { packagePreUpdate }, new[] { package });
        }

        public virtual void ClearPackageErrors(IPackage package)
        {
            var packagePreUpdate = package.Clone();
            package.ClearErrors();
            onPackagesChanged?.Invoke(k_EmptyList, k_EmptyList, new[] { packagePreUpdate }, new[] { package });
        }

        public virtual IEnumerable<IPackage> packagesInError => allPackages.Where(p => p.errors.Any());

        public virtual void SetPackageProgress(IPackage package, PackageProgress progress)
        {
            if (package == null || package.progress == progress)
                return;

            package.progress = progress;

            onPackageProgressUpdate?.Invoke(package);
        }

        public void OnEnable()
        {
            m_UpmClient.onPackagesChanged += OnPackagesChanged;
            m_UpmClient.onPackageVersionUpdated += OnUpmPackageVersionUpdated;
            m_UpmClient.onAddOperation += OnUpmAddOperation;
            m_UpmClient.onEmbedOperation += OnUpmEmbedOperation;
            m_UpmClient.onRemoveOperation += OnUpmRemoveOperation;

            m_AssetStoreClient.onPackagesChanged += OnPackagesChanged;
            m_AssetStoreClient.onPackageVersionUpdated += OnUpmPackageVersionUpdated;

            m_AssetStoreDownloadManager.onDownloadProgress += OnDownloadProgress;
            m_AssetStoreDownloadManager.onDownloadFinalized += OnDownloadFinalized;
            m_AssetStoreDownloadManager.onDownloadError += OnDownloadError;
            m_AssetStoreDownloadManager.onDownloadPaused += OnDownloadPaused;

            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
        }

        public void OnDisable()
        {
            m_UpmClient.onPackagesChanged -= OnPackagesChanged;
            m_UpmClient.onPackageVersionUpdated -= OnUpmPackageVersionUpdated;
            m_UpmClient.onAddOperation -= OnUpmAddOperation;
            m_UpmClient.onEmbedOperation -= OnUpmEmbedOperation;
            m_UpmClient.onRemoveOperation -= OnUpmRemoveOperation;

            m_AssetStoreClient.onPackagesChanged -= OnPackagesChanged;
            m_AssetStoreClient.onPackageVersionUpdated -= OnUpmPackageVersionUpdated;

            m_AssetStoreDownloadManager.onDownloadProgress -= OnDownloadProgress;
            m_AssetStoreDownloadManager.onDownloadFinalized -= OnDownloadFinalized;
            m_AssetStoreDownloadManager.onDownloadError -= OnDownloadError;
            m_AssetStoreDownloadManager.onDownloadPaused -= OnDownloadPaused;

            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
        }

        public virtual void Reload()
        {
            onPackagesChanged?.Invoke(Enumerable.Empty<IPackage>(), m_Packages.Values, Enumerable.Empty<IPackage>(), Enumerable.Empty<IPackage>());

            m_AssetStoreClient.ClearCache();
            m_UpmClient.ClearCache();

            m_Packages.Clear();
            m_SerializedUpmPackages = new List<UpmPackage>();
            m_SerializedAssetStorePackages = new List<AssetStorePackage>();
            m_SpecialInstallations.Clear();
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
            if (downloadOperation.state == DownloadState.Completed)
                m_AssetStoreClient.RefreshLocal();
            SetPackageProgress(package, PackageProgress.None);
        }

        private void OnDownloadError(IOperation operation, UIError error)
        {
            var package = GetPackage(operation.packageUniqueId);
            if (package == null)
                return;

            AddPackageError(package, error);
        }

        private void OnDownloadPaused(IOperation operation)
        {
            var package = GetPackage(operation.packageUniqueId);
            if (package == null)
                return;

            SetPackageProgress(package, PackageProgress.Pausing);
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
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

        public virtual void Install(IPackageVersion version)
        {
            if (version.isInstalled)
                return;
            m_UpmClient.AddById(version.uniqueId);
        }

        public virtual void InstallFromUrl(string url)
        {
            m_UpmClient.AddByUrl(url);
        }

        public virtual void InstallFromPath(string path)
        {
            m_UpmClient.AddByPath(path);
        }

        public virtual void Uninstall(IPackage package)
        {
            if (package.versions.installed == null)
                return;
            m_UpmClient.RemoveByName(package.name);
        }

        public virtual void Embed(IPackageVersion packageVersion)
        {
            if (packageVersion == null || !packageVersion.HasTag(PackageTag.Embeddable))
                return;
            m_UpmClient.EmbedByName(packageVersion.name);
        }

        public virtual void RemoveEmbedded(IPackage package)
        {
            if (package.versions.installed == null)
                return;
            m_UpmClient.RemoveEmbeddedByName(package.name);
        }

        public virtual void FetchExtraInfo(IPackageVersion version)
        {
            if (version.isFullyFetched)
                return;
            m_UpmClient.ExtraFetch(version.uniqueId);
        }

        public virtual bool IsDownloadInProgress(IPackageVersion version)
        {
            return m_AssetStoreDownloadManager.GetDownloadOperation(version.packageUniqueId)?.isInProgress ?? false;
        }

        public virtual bool IsDownloadInPause(IPackageVersion version)
        {
            return m_AssetStoreDownloadManager.GetDownloadOperation(version.packageUniqueId)?.isInPause ?? false;
        }

        public virtual void Download(IPackage package)
        {
            if (!(package is AssetStorePackage))
                return;

            if (!PlayModeDownload.CanBeginDownload())
                return;

            SetPackageProgress(package, PackageProgress.Downloading);
            m_AssetStoreDownloadManager.Download(package.uniqueId);
            // When we start a new download, we want to clear past operation errors to give it a fresh start.
            // Eventually we want a better design on how to show errors, to be further addressed in https://jira.unity3d.com/browse/PAX-1332
            package.ClearErrors(e => e.errorCode == UIErrorCode.AssetStoreOperationError);
        }

        public virtual void AbortDownload(IPackage package)
        {
            if (!(package is AssetStorePackage))
                return;
            SetPackageProgress(package, PackageProgress.None);
            m_AssetStoreDownloadManager.AbortDownload(package.uniqueId);
        }

        public virtual void PauseDownload(IPackage package)
        {
            if (!(package is AssetStorePackage))
                return;
            SetPackageProgress(package, PackageProgress.Pausing);
            m_AssetStoreDownloadManager.PauseDownload(package.uniqueId);
        }

        public virtual void ResumeDownload(IPackage package)
        {
            if (!(package is AssetStorePackage))
                return;

            if (!PlayModeDownload.CanBeginDownload())
                return;

            SetPackageProgress(package, PackageProgress.Resuming);
            m_AssetStoreDownloadManager.ResumeDownload(package.uniqueId);
        }

        public virtual void Import(IPackage package)
        {
            if (!(package is AssetStorePackage))
                return;

            var path = package.versions.primary.localPath;
            if (m_IOProxy.FileExists(path))
            {
                m_AssetDatabase.ImportPackage(path, true);
            }
        }
    }
}
