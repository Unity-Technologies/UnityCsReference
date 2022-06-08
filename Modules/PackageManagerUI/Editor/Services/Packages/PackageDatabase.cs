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
    internal struct PackagesChangeArgs
    {
        public IEnumerable<IPackage> added;
        public IEnumerable<IPackage> removed;
        public IEnumerable<IPackage> updated;

        // To avoid unnecessary cloning of packages, preUpdate is now set to be optional, the list is either empty or the same size as the the postUpdate list
        public IEnumerable<IPackage> preUpdate;
        public IEnumerable<IPackage> progressUpdated;
    }

    [Serializable]
    internal class PackageDatabase : ISerializationCallbackReceiver
    {
        // Normally package unique Id never changes for a package, but when we are installing a package from git or a tarball
        // we only had a temporary unique id at first. For example, for `com.unity.a` is a unique id for a package, but when
        // we are installing from git, the only identifier we know is something like `git@example.com/com.unity.a.git`.
        // We only know the id `com.unity.a` after the package has been successfully installed, and we'll trigger an event for that.
        public virtual event Action<string, string> onPackageUniqueIdFinalize = delegate {};

        public virtual event Action<IPackage> onVerifiedGitPackageUpToDate = delegate {};

        public virtual event Action<PackagesChangeArgs> onPackagesChanged = delegate {};

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

        public virtual bool isEmpty => !m_Packages.Any();

        private static readonly IPackage[] k_EmptyList = new IPackage[0] { };

        public virtual bool isInstallOrUninstallInProgress
        {
            // add, embed -> install, remove -> uninstall or addAndRemove -> install/uninstall
            get { return m_UpmClient.isAddRemoveOrEmbedInProgress; }
        }

        public virtual IEnumerable<IPackage> allPackages => m_Packages.Values;

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
            GetPackageAndVersionByIdOrName(idOrName, out var package, out _);
            return package;
        }

        public virtual void GetPackageAndVersion(string packageUniqueId, string versionUniqueId, out IPackage package, out IPackageVersion version)
        {
            package = GetPackage(packageUniqueId);
            version = package?.versions.FirstOrDefault(v => v.uniqueId == versionUniqueId);
        }

        public virtual void GetPackageAndVersion(PackageAndVersionIdPair pair, out IPackage package, out IPackageVersion version)
        {
            GetPackageAndVersion(pair?.packageUniqueId, pair?.versionUniqueId, out package, out version);
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

        public virtual IEnumerable<IPackageVersion> GetFeaturesThatUseThisPackage(IPackageVersion version)
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
            var serializedPackages = m_SerializedPlaceholderPackages.Concat<BasePackage>(m_SerializedUpmPackages).Concat(m_SerializedAssetStorePackages);
            foreach (var p in serializedPackages)
            {
                p.LinkPackageAndVersions();
                m_Packages[p.uniqueId] = p;
            }
        }

        public void OnBeforeSerialize()
        {
            m_SerializedUpmPackages.Clear();
            m_SerializedAssetStorePackages.Clear();
            m_SerializedPlaceholderPackages.Clear();

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

        private void TriggerOnPackagesChanged(IEnumerable<IPackage> added = null, IEnumerable<IPackage> removed = null, IEnumerable<IPackage> updated = null, IEnumerable<IPackage> preUpdate = null, IEnumerable<IPackage> progressUpdated = null)
        {
            added ??= k_EmptyList;
            updated ??= k_EmptyList;
            removed ??= k_EmptyList;
            preUpdate ??= k_EmptyList;
            progressUpdated ??= k_EmptyList;

            if (!added.Any() && !updated.Any() && !removed.Any() && !preUpdate .Any() && !progressUpdated.Any())
                return;

            onPackagesChanged?.Invoke(new PackagesChangeArgs { added = added, updated = updated, removed = removed, preUpdate = preUpdate, progressUpdated = progressUpdated });
        }

        public virtual void AddPackageError(IPackage package, UIError error)
        {
            package.AddError(error);
            TriggerOnPackagesChanged(updated: new[] { package });
        }

        public virtual void ClearPackageErrors(IPackage package, Predicate<UIError> match = null)
        {
            package.ClearErrors(match);
            TriggerOnPackagesChanged(updated: new[] { package });
        }

        public virtual IEnumerable<IPackage> packagesInError => allPackages.Where(p => p.errors.Any());

        public virtual void SetPackagesProgress(IEnumerable<IPackage> packages, PackageProgress progress)
        {
            var packagesUpdated = packages.Where(p => p != null && p.progress != progress).ToList();
            foreach (var package in packagesUpdated)
                package.progress = progress;
            if (packagesUpdated.Any())
                TriggerOnPackagesChanged(updated: packagesUpdated, progressUpdated: packagesUpdated);
        }

        public virtual void SetPackageProgress(IPackage package, PackageProgress progress)
        {
            SetPackagesProgress(new[] { package }, progress);
        }

        public void OnEnable()
        {
            m_UpmClient.onPackagesChanged += OnPackagesChanged;
            m_UpmClient.onAddOperation += OnUpmAddOperation;
            m_UpmClient.onEmbedOperation += OnUpmEmbedOperation;
            m_UpmClient.onRemoveOperation += OnUpmRemoveOperation;
            m_UpmClient.onAddAndRemoveOperation += OnUpmAddAndRemoveOperation;

            m_UpmCache.onVerifiedGitPackageUpToDate += OnVerifiedGitPackageUpToDate;

            m_AssetStoreClient.onPackagesChanged += OnAssetStorePackagesChanged;

            m_AssetStoreDownloadManager.onDownloadProgress += OnDownloadProgress;
            m_AssetStoreDownloadManager.onDownloadFinalized += OnDownloadFinalized;
            m_AssetStoreDownloadManager.onDownloadError += OnDownloadError;
            m_AssetStoreDownloadManager.onDownloadPaused += OnDownloadPaused;

            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
        }

        public void OnDisable()
        {
            m_UpmClient.onPackagesChanged -= OnPackagesChanged;
            m_UpmClient.onAddOperation -= OnUpmAddOperation;
            m_UpmClient.onEmbedOperation -= OnUpmEmbedOperation;
            m_UpmClient.onRemoveOperation -= OnUpmRemoveOperation;
            m_UpmClient.onAddAndRemoveOperation -= OnUpmAddAndRemoveOperation;

            m_AssetStoreClient.onPackagesChanged -= OnAssetStorePackagesChanged;

            m_AssetStoreDownloadManager.onDownloadProgress -= OnDownloadProgress;
            m_AssetStoreDownloadManager.onDownloadFinalized -= OnDownloadFinalized;
            m_AssetStoreDownloadManager.onDownloadError -= OnDownloadError;
            m_AssetStoreDownloadManager.onDownloadPaused -= OnDownloadPaused;

            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
        }

        public virtual void Reload()
        {
            TriggerOnPackagesChanged(removed: allPackages);

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
            // We want to call RefreshLocal() before calling GetPackage(operation.packageUniqueId),
            // because if we call GetPackage first, we might get an old instance of the package.
            // This is due to RefreshLocal potentially replacing the instance in the database with a new one.
            var downloadOperation = operation as AssetStoreDownloadOperation;
            if (downloadOperation?.state == DownloadState.Completed)
                m_AssetStoreClient.RefreshLocal();

            var package = GetPackage(operation.packageUniqueId);
            if (package == null)
                return;

            if (downloadOperation?.state == DownloadState.Error)
                AddPackageError(package, new UIError(UIErrorCode.AssetStoreOperationError, downloadOperation.errorMessage, UIError.Attribute.IsClearable));
            else if (downloadOperation?.state == DownloadState.Aborted)
                AddPackageError(package, new UIError(UIErrorCode.AssetStoreOperationError, downloadOperation.errorMessage ?? L10n.Tr("Download aborted"), UIError.Attribute.IsWarning | UIError.Attribute.IsClearable));

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
                var notInstalledAssetStorePackages = m_Packages.Values.Where(p => p.Is(PackageType.AssetStore) && p.versions.installed == null).ToArray();
                foreach (var p in notInstalledAssetStorePackages)
                    m_Packages.Remove(p.uniqueId);
                TriggerOnPackagesChanged(removed: notInstalledAssetStorePackages);
            }
        }

        private void OnVerifiedGitPackageUpToDate(string packageId)
        {
            onVerifiedGitPackageUpToDate.Invoke(GetPackage(packageId));
        }

        // Since an asset store needs to go through the transition from PlaceholderPackage to AssetStorePackage
        // the progress we set on the PlaceholderPackage is not preserved when the transition happens
        // as a result, we need a preprocessing step to set the progress again on newly created AssetStorePackages
        private void OnAssetStorePackagesChanged(IEnumerable<IPackage> packages)
        {
            foreach (var package in packages)
            {
                if (package.progress != PackageProgress.Downloading && GetPackage(package.uniqueId)?.progress == PackageProgress.Downloading
                    && m_AssetStoreDownloadManager.GetDownloadOperation(package.uniqueId)?.isInProgress == true)
                    package.progress = PackageProgress.Downloading;
            }
            OnPackagesChanged(packages);
        }

        private void OnPackagesChanged(IEnumerable<IPackage> packages)
        {
            if (!packages.Any())
                return;

            var packagesAdded = new List<IPackage>();
            var packagesRemoved = new List<IPackage>();

            var packagesPreUpdate = new List<IPackage>();
            var packagesUpdated = new List<IPackage>();

            var packageProgressUpdated = new List<IPackage>();

            var specialInstallationChecklist = new List<IPackage>();

            foreach (var package in packages)
            {
                var packageUniqueId = package.uniqueId;
                var isEmptyPackage = !package.versions.Any();
                var oldPackage = GetPackage(packageUniqueId);

                if (oldPackage != null && isEmptyPackage)
                {
                    packagesRemoved.Add(oldPackage);
                    m_Packages.Remove(packageUniqueId);
                }
                else if (!isEmptyPackage)
                {
                    m_Packages[packageUniqueId] = package;
                    if (oldPackage != null)
                    {
                        packagesPreUpdate.Add(oldPackage);
                        packagesUpdated.Add(package);

                        if (oldPackage.progress != package.progress)
                            packageProgressUpdated.Add(package);
                    }
                    else
                        packagesAdded.Add(package);

                    // For special installation like git, we want to check newly installed or updated packages.
                    // To make sure that placeholders packages are removed properly.
                    if (m_UpmClient.specialInstallations.Any() && package.versions.installed != null)
                        specialInstallationChecklist.Add(package);
                }

                // It could happen that before the productId info was available, another package was created with packageName as the uniqueId
                // Once the productId becomes available it should be the new uniqueId, we want to old package such that there won't be two
                // entries of the same package with different uniqueIds (one productId, one with packageName)
                if (!string.IsNullOrEmpty(package.productId) && !string.IsNullOrEmpty(package.name))
                {
                    var packageWithNameAsUniqueId = GetPackage(package.name);
                    if (packageWithNameAsUniqueId != null)
                    {
                        packagesRemoved.Add(packageWithNameAsUniqueId);
                        m_Packages.Remove(package.name);
                    }
                }

                // if the primary version is not fully fetched, trigger an extra fetch automatically right away to get results early
                // since the primary version's display name is used in the package list
                var primaryVersion = package.versions.primary;
                if (primaryVersion?.isFullyFetched == false)
                    m_UpmClient.ExtraFetch(primaryVersion.uniqueId);
            }

            TriggerOnPackagesChanged(added: packagesAdded, removed: packagesRemoved, preUpdate: packagesPreUpdate, updated: packagesUpdated, progressUpdated: packageProgressUpdated);

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
                    onPackageUniqueIdFinalize?.Invoke(specialUniqueId, match.uniqueId);
                    SetPackageProgress(match, PackageProgress.None);
                    RemoveSpecialInstallation(specialUniqueId);
                }
            }
        }

        private void OnUpmAddAndRemoveOperation(UpmAddAndRemoveOperation operation)
        {
            SetPackagesProgress(operation.packageIdsToReset.Select(idOrName => GetPackageByIdOrName(idOrName)), PackageProgress.Resetting);
            SetPackagesProgress(operation.packageIdsToAdd.Select(idOrName => GetPackageByIdOrName(idOrName)), PackageProgress.Installing);
            SetPackagesProgress(operation.packagesNamesToRemove.Select(name => GetPackage(name)), PackageProgress.Removing);

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
                var packagesToAddOrReset = operation.packageIdsToAdd.Concat(operation.packageIdsToReset).Select(idOrName => GetPackageByIdOrName(idOrName));
                var packagesToRemove = operation.packagesNamesToRemove.Select(name => GetPackage(name));
                SetPackagesProgress(packagesToAddOrReset.Concat(packagesToRemove), PackageProgress.None);
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
                var placeholerPackage = new PlaceholderPackage(specialUniqueId, L10n.Tr("Adding a new package"), PackageType.Upm, addOperation.packageTag, PackageProgress.Installing);
                OnPackagesChanged(new[] { placeholerPackage });
                operation.onOperationError += (op, error) => RemoveSpecialInstallation(specialUniqueId);
                return;
            }
            SetPackageProgress(GetPackage(operation.packageUniqueId), PackageProgress.Installing);
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
                TriggerOnPackagesChanged(removed: new[] { placeHolderPackage });
            }
            m_UpmClient.specialInstallations.Remove(specialUniqueId);
        }

        private void OnUpmEmbedOperation(IOperation operation)
        {
            SetPackageProgress(GetPackage(operation.packageUniqueId), PackageProgress.Installing);
            operation.onOperationError += OnUpmOperationError;
            operation.onOperationFinalized += OnUpmOperationFinalized;
        }

        private void OnUpmRemoveOperation(IOperation operation)
        {
            SetPackageProgress(GetPackage(operation.packageUniqueId), PackageProgress.Removing);
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

        public virtual void Install(IEnumerable<IPackageVersion> versions)
        {
            if (versions == null || !versions.Any())
                return;

            m_UpmClient.AddByIds(versions.Select(v => v.uniqueId));
        }

        public virtual void Install(string packageId)
        {
            m_UpmClient.AddById(packageId);
        }

        public virtual void InstallFromUrl(string url)
        {
            m_UpmClient.AddByUrl(url);
        }

        public virtual bool InstallFromPath(string path, out string tempPackageId)
        {
            return m_UpmClient.AddByPath(path, out tempPackageId);
        }

        public virtual void Uninstall(IPackage package)
        {
            if (package.versions.installed == null)
                return;
            m_UpmClient.RemoveByName(package.name);
        }

        public virtual void Uninstall(IEnumerable<IPackage> packages)
        {
            if (packages == null || !packages.Any())
                return;
            m_UpmClient.RemoveByNames(packages.Select(p => p.name));
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

        private bool CheckTermOfServiceAgreement(Action onTosAccepted, Action<UIError> onError)
        {
            if (termOfServiceAgreementStatus == TermOfServiceAgreementStatus.NotAccepted)
            {
                m_AssetStoreClient.CheckTermOfServiceAgreement(status =>
                {
                    termOfServiceAgreementStatus = status;
                    if (termOfServiceAgreementStatus == TermOfServiceAgreementStatus.Accepted)
                        onTosAccepted?.Invoke();
                }, error => onError?.Invoke(error));
                return false;
            }

            onTosAccepted?.Invoke();
            return true;
        }

        public virtual bool Download(IPackage package)
        {
            return Download(new[] { package });
        }

        public virtual bool Download(IEnumerable<IPackage> packages)
        {
            if (!PlayModeDownload.CanBeginDownload())
                return false;

            return CheckTermOfServiceAgreement(() => Download_Internal(packages), error =>
            {
                var firstPackage = packages.FirstOrDefault();
                if (firstPackage != null && !packages.Skip(1).Any())
                    AddPackageError(firstPackage, error);
                else
                {
                    // In the case of bulk download, we don't know which package to add the error to.
                    // We might need to think of better way to present the error to the user.
                    // It will be addressed in https://jira.unity3d.com/browse/PAX-1994.
                    Debug.Log(error);
                }
            });
        }

        private void Download_Internal(IEnumerable<IPackage> packages)
        {
            foreach (var package in packages)
            {
                // When we start a new download, we want to clear past operation errors to give it a fresh start.
                // Eventually we want a better design on how to show errors, to be further addressed in https://jira.unity3d.com/browse/PAX-1332
                // We need to clear errors before calling download because Download can fail right away
                package.ClearErrors(e => e.errorCode == UIErrorCode.AssetStoreOperationError);
                m_AssetStoreDownloadManager.Download(package);
            }
            SetPackagesProgress(packages, PackageProgress.Downloading);
        }

        public virtual void AbortDownload(IPackage package)
        {
            AbortDownload(new[] { package });
        }

        public virtual void AbortDownload(IEnumerable<IPackage> packages)
        {
            // We need to figure out why the IEnumerable is being altered instead of using ToArray.
            // It will be addressed in https://jira.unity3d.com/browse/PAX-1995.
            foreach (var package in packages.ToArray())
                m_AssetStoreDownloadManager.AbortDownload(package.uniqueId);
            SetPackagesProgress(packages, PackageProgress.None);
        }

        public virtual void PauseDownload(IPackage package)
        {
            if (!package.Is(PackageType.AssetStore))
                return;
            SetPackageProgress(package, PackageProgress.Pausing);
            m_AssetStoreDownloadManager.PauseDownload(package.uniqueId);
        }

        public virtual void ResumeDownload(IPackage package)
        {
            if (!package.Is(PackageType.AssetStore))
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
                Debug.Log($"[Package Manager Window] Cannot import package {package.displayName}: {e.Message}");
            }
        }
    }
}
