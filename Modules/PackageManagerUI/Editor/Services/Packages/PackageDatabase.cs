// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class PackageDatabase : ISerializationCallbackReceiver
    {
        public virtual event Action<IPackage> onInstallOrUninstallSuccess = delegate {};

        public virtual event Action<IPackage> onPackageProgressUpdate = delegate {};
        public virtual event Action<IPackage> onVerifiedGitPackageUpToDate = delegate {};

        public virtual event Action<IEnumerable<IPackage> /*added*/,
                                    IEnumerable<IPackage> /*removed*/,
                                    IEnumerable<IPackage> /*preUpdated*/,
                                    IEnumerable<IPackage> /*postUpdated*/> onPackagesChanged = delegate {};

        public virtual event Action<TermOfServiceAgreementStatus> onTermOfServiceAgreementStatusChange = delegate {};

        private readonly Dictionary<string, IPackage> m_Packages = new Dictionary<string, IPackage>();

        private readonly Dictionary<string, IEnumerable<Sample>> m_ParsedSamples = new Dictionary<string, IEnumerable<Sample>>();

        [SerializeField]
        private TermOfServiceAgreementStatus m_TermOfServiceAgreementStatus = TermOfServiceAgreementStatus.NotAccepted;

        public virtual TermOfServiceAgreementStatus termOfServiceAgreementStatus
        {
            get => m_TermOfServiceAgreementStatus;
            internal set
            {
                m_TermOfServiceAgreementStatus = value;
                onTermOfServiceAgreementStatusChange?.Invoke(m_TermOfServiceAgreementStatus);
            }
        }

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
        private UpmCache m_UpmCache;
        [NonSerialized]
        private UpmClient m_UpmClient;
        [NonSerialized]
        private IOProxy m_IOProxy;
        [NonSerialized]
        private AssetStoreUtils m_AssetStoreUtils;

        public void ResolveDependencies(UnityConnectProxy unityConnect,
            AssetDatabaseProxy assetDatabase,
            AssetStoreUtils assetStoreUtils,
            AssetStoreClient assetStoreClient,
            AssetStoreDownloadManager assetStoreDownloadManager,
            UpmCache upmCache,
            UpmClient upmClient,
            IOProxy ioProxy)
        {
            m_UnityConnect = unityConnect;
            m_AssetDatabase = assetDatabase;
            m_AssetStoreUtils = assetStoreUtils;
            m_AssetStoreClient = assetStoreClient;
            m_AssetStoreDownloadManager = assetStoreDownloadManager;
            m_UpmCache = upmCache;
            m_UpmClient = upmClient;
            m_IOProxy = ioProxy;

            foreach (var package in m_SerializedAssetStorePackages)
                package.ResolveDependencies(m_AssetStoreUtils, ioProxy);
        }

        public virtual bool isEmpty { get { return !m_Packages.Any(); } }

        private static readonly IPackage[] k_EmptyList = new IPackage[0] {};

        public virtual bool isInstallOrUninstallInProgress
        {
            // add, embed -> install, remove -> uninstall or addAndRemove -> install/uninstall
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

        public virtual IPackage GetPackageByIdOrName(string idOrName)
        {
            GetPackageAndVersionByIdOrName(idOrName, out var package, out var version);
            return package;
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

        public virtual void GetPackageAndVersion(DependencyInfo info, out IPackage package, out IPackageVersion version)
        {
            GetUpmPackageAndVersion(info.name, info.version, out package, out version);
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

        public virtual IEnumerable<IPackageVersion> GetReverseDependencies(IPackageVersion version, bool directDependenciesOnly = false)
        {
            if (version?.dependencies == null)
                return null;
            var installedRoots = allPackages.Select(p => p.versions.installed).Where(p => p?.isDirectDependency ?? false);
            return installedRoots.Where(p
                => (directDependenciesOnly ? p.dependencies : p.resolvedDependencies)?.Any(r => r.name == version.name) ?? false);
        }

        public virtual IEnumerable<IPackageVersion> GetFeatureDependents(IPackageVersion version)
        {
            return GetReverseDependencies(version, true)?.Where(p => p.HasTag(PackageTag.Feature)) ?? Enumerable.Empty<IPackageVersion>();
        }

        public virtual IPackage[] GetCustomizedDependencies(IPackageVersion version, bool? rootDependenciesOnly = null)
        {
            return version?.dependencies?.Select(d => GetPackage(d.name)).Where(p =>
            {
                return p?.versions.isNonLifecycleVersionInstalled == true
                && (rootDependenciesOnly == null || p.versions.installed.isDirectDependency == rootDependenciesOnly);
            }).ToArray() ?? new IPackage[0];
        }

        public virtual IEnumerable<Sample> GetSamples(IPackageVersion version)
        {
            if (version?.packageInfo == null || version.packageInfo.version != version.version?.ToString())
                return Enumerable.Empty<Sample>();

            if (m_ParsedSamples.TryGetValue(version.uniqueId, out var parsedSamples))
                return parsedSamples;

            var samples = Sample.FindByPackage(version.packageInfo, m_UpmCache, m_IOProxy, m_AssetDatabase);
            m_ParsedSamples[version.uniqueId] = samples;
            return samples;
        }

        public virtual IPackageVersion GetPackageInFeatureVersion(string packageId)
        {
            var versions = GetPackage(packageId)?.versions;
            return versions?.lifecycleVersion ?? versions?.primary;
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

        public virtual void ClearPackageErrors(IPackage package, Predicate<UIError> match = null)
        {
            var packagePreUpdate = package.Clone();
            package.ClearErrors(match);
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
            m_UpmClient.onAddAndRemoveOperation += OnUpmAddAndRemoveOperation;

            m_UpmCache.onVerifiedGitPackageUpToDate += OnVerifiedGitPackageUpToDate;

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
            m_UpmClient.onAddAndRemoveOperation -= OnUpmAddAndRemoveOperation;

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

            m_TermOfServiceAgreementStatus = TermOfServiceAgreementStatus.NotAccepted;
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
            if (downloadOperation != null)
            {
                if (downloadOperation.state == DownloadState.Error)
                    AddPackageError(package, new UIError(UIErrorCode.AssetStoreOperationError, downloadOperation.errorMessage, UIError.Attribute.IsClearable));
                else if (downloadOperation.state == DownloadState.Aborted)
                    AddPackageError(package, new UIError(UIErrorCode.AssetStoreOperationError, downloadOperation.errorMessage ?? L10n.Tr("Download aborted"), UIError.Attribute.IsWarning | UIError.Attribute.IsClearable));
                else if (downloadOperation.state == DownloadState.Completed)
                    m_AssetStoreClient.RefreshLocal();
            }

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

        private void OnVerifiedGitPackageUpToDate(string packageId)
        {
            onVerifiedGitPackageUpToDate.Invoke(GetPackage(packageId));
        }

        private void OnPackagesChanged(IEnumerable<IPackage> packages)
        {
            if (!packages.Any())
                return;

            var packagesAdded = new List<IPackage>();
            var packagesRemoved = new List<IPackage>();

            var packagesPreUpdate = new List<IPackage>();
            var packagesPostUpdate = new List<IPackage>();

            var specialInstallationChecklist = new List<IPackage>();

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

                    // For special installation like git, we want to check newly installed or updated packages.
                    // To make sure that placeholders packages are removed properly.
                    if (m_UpmClient.specialInstallations.Any() && package.versions.installed != null)
                        specialInstallationChecklist.Add(package);
                }
            }

            if (packagesAdded.Count + packagesRemoved.Count + packagesPostUpdate.Count > 0)
                onPackagesChanged?.Invoke(packagesAdded, packagesRemoved, packagesPreUpdate, packagesPostUpdate);

            // special handling to make sure onInstallSuccess events are called correctly when special unique id is used
            for (var i = m_UpmClient.specialInstallations.Count - 1; i >= 0; i--)
            {
                var specialUniqueId = m_UpmClient.specialInstallations[i];

                // match specialInstallationChecklist's uniqueId with m_UpmClient.specialInstallations
                // to find which packages are placeholder packages and can be removed now
                IPackage match;
                var fileUriPrefix = "file:";
                if (specialUniqueId.StartsWith(fileUriPrefix)) // local installation (path or tarball)
                {
                    var specialInstallationPath = specialUniqueId.Substring(fileUriPrefix.Length);
                    match = specialInstallationChecklist.FirstOrDefault(p =>
                    {
                        var uniqueId = p.versions.installed.uniqueId;
                        var fileUriPrefixIndex = uniqueId.IndexOf(fileUriPrefix);

                        return fileUriPrefixIndex >= 0 && m_IOProxy.IsSamePackageDirectory(specialInstallationPath, uniqueId.Substring(fileUriPrefixIndex + fileUriPrefix.Length));
                    });
                }
                else // git installation
                {
                    match = specialInstallationChecklist.FirstOrDefault(p => p.versions.installed.uniqueId.ToLower().Contains(specialUniqueId.ToLower()));
                }

                if (match != null)
                {
                    onInstallOrUninstallSuccess?.Invoke(match);
                    SetPackageProgress(match, PackageProgress.None);
                    RemoveSpecialInstallation(specialUniqueId);
                }
            }
        }

        private void OnUpmAddAndRemoveOperation(UpmAddAndRemoveOperation operation)
        {
            foreach (var packageId in operation.packageIdsToReset)
                SetPackageProgress(GetPackageByIdOrName(packageId), PackageProgress.Resetting);
            foreach (var packageId in operation.packageIdsToAdd)
                SetPackageProgress(GetPackageByIdOrName(packageId), PackageProgress.Installing);
            foreach (var packageName in operation.packagesNamesToRemove)
                SetPackageProgress(GetPackage(packageName), PackageProgress.Removing);

            operation.onOperationSuccess += (op) =>
            {
                var package = GetPackage(op.packageUniqueId);
                if (package != null)
                    onInstallOrUninstallSuccess?.Invoke(package);
            };

            operation.onOperationError += (op, err) =>
            {
                // For now we all the actions we use `AddAndRemoveOperation` for should have a packageUniqueId
                // because we don't support multi-select yet. We need to update the logic when we do multi-selection
                // And we'll need to discuss with the PAK team.
                var package = GetPackage(op.packageUniqueId);
                if (package != null)
                    AddPackageError(package, err);
            };
            operation.onOperationFinalized += (op) =>
            {
                foreach (var packageIdOrName in operation.packageIdsToAdd.Concat(operation.packageIdsToReset).Concat(operation.packagesNamesToRemove))
                    SetPackageProgress(GetPackageByIdOrName(packageIdOrName), PackageProgress.None);
            };
        }

        private void OnUpmAddOperation(IOperation operation)
        {
            var package = GetPackage(operation.packageUniqueId);
            if (package == null)
            {
                // When adding any package that's not already in the PackageDatabase, we consider it a `special` installation and we'll create a placeholder package for it accordingly
                var addOperation = operation as UpmAddOperation;
                var specialUniqueId = !string.IsNullOrEmpty(addOperation.specialUniqueId) ? addOperation.specialUniqueId : addOperation.packageId;

                m_UpmClient.specialInstallations.Add(specialUniqueId);
                var placeholerPackage = new PlaceholderPackage(specialUniqueId, L10n.Tr("Adding a new package"), PackageType.Installable, addOperation.packageTag, PackageProgress.Installing);
                OnPackagesChanged(new[] { placeholerPackage });
                operation.onOperationError += (op, error) => RemoveSpecialInstallation(specialUniqueId);
                return;
            }
            SetPackageProgress(GetPackage(operation.packageUniqueId), PackageProgress.Installing);
            operation.onOperationSuccess += (op) => onInstallOrUninstallSuccess?.Invoke(GetPackage(op.packageUniqueId));
            operation.onOperationError += OnUpmOperationError;
            operation.onOperationFinalized += OnUpmOperationFinalized;
        }

        private void RemoveSpecialInstallation(string specialUniqueId)
        {
            var placeHolderPackage = GetPackage(specialUniqueId);
            // Fix issue where package was added by id without version. Remove package from package database only if it's a placeholder
            if (placeHolderPackage is PlaceholderPackage)
            {
                m_Packages.Remove(specialUniqueId);
                onPackagesChanged?.Invoke(Enumerable.Empty<IPackage>(), new[] { placeHolderPackage }, Enumerable.Empty<IPackage>(), Enumerable.Empty<IPackage>());
            }
            m_UpmClient.specialInstallations.Remove(specialUniqueId);
        }

        private void OnUpmEmbedOperation(IOperation operation)
        {
            SetPackageProgress(GetPackage(operation.packageUniqueId), PackageProgress.Installing);
            operation.onOperationSuccess += (op) => onInstallOrUninstallSuccess?.Invoke(GetPackage(op.packageUniqueId));
            operation.onOperationError += OnUpmOperationError;
            operation.onOperationFinalized += OnUpmOperationFinalized;
        }

        private void OnUpmRemoveOperation(IOperation operation)
        {
            SetPackageProgress(GetPackage(operation.packageUniqueId), PackageProgress.Removing);
            operation.onOperationSuccess += (op) => onInstallOrUninstallSuccess?.Invoke(GetPackage(operation.packageUniqueId));
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

        public void ClearSamplesCache()
        {
            m_ParsedSamples.Clear();
        }

        public virtual void Install(IPackageVersion version)
        {
            if (version == null || version.isInstalled)
                return;
            m_UpmClient.AddById(version.uniqueId);
        }

        public virtual void Install(string packageId)
        {
            m_UpmClient.AddById(packageId);
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

        public virtual void InstallAndResetDependencies(IPackageVersion version, IEnumerable<IPackage> dependenciesToReset)
        {
            m_UpmClient.AddAndResetDependencies(version.uniqueId, dependenciesToReset?.Select(package => package.name) ?? Enumerable.Empty<string>());
        }

        public virtual void ResetDependencies(IPackageVersion version, IEnumerable<IPackage> dependenciesToReset)
        {
            m_UpmClient.ResetDependencies(version.uniqueId, dependenciesToReset?.Select(package => package.name) ?? Enumerable.Empty<string>());
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

        public virtual bool Download(IPackage package)
        {
            if (!(package is AssetStorePackage))
                return false;

            if (!PlayModeDownload.CanBeginDownload())
                return false;

            if (termOfServiceAgreementStatus == TermOfServiceAgreementStatus.NotAccepted)
            {
                m_AssetStoreClient.CheckTermOfServiceAgreement(status =>
                {
                    termOfServiceAgreementStatus = status;
                    if (termOfServiceAgreementStatus == TermOfServiceAgreementStatus.Accepted)
                        Download_Internal(package);
                }, error => AddPackageError(package, error));
                return false;
            }

            Download_Internal(package);
            return true;
        }

        private void Download_Internal(IPackage package)
        {
            SetPackageProgress(package, PackageProgress.Downloading);
            m_AssetStoreDownloadManager.Download(package);
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
            try
            {
                if (m_IOProxy.FileExists(path))
                    m_AssetDatabase.ImportPackage(path, true);
            }
            catch (System.IO.IOException e)
            {
                Debug.Log($"[Package Manager] Cannot import package {package.displayName}: {e.Message}");
            }
        }
    }
}
