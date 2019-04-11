// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class PackageCollection
    {
        public static readonly SemVersion EmbdeddedVersion = new SemVersion(int.MaxValue, int.MaxValue, int.MaxValue, "embedded");
        public static readonly SemVersion LocalVersion = new SemVersion(int.MaxValue, int.MaxValue, int.MaxValue, "local");

        public event Action<PackageFilter, IEnumerable<Package>> OnPackagesChanged = delegate {};
        public event Action<PackageFilter> OnFilterChanged = delegate {};
        public event Action<PackageInfo, bool> OnLatestPackageInfoFetched = delegate {};
        public event Action<string> OnUpdateTimeChange = delegate {};

        private Dictionary<string, SemVersion> projectDependencies;
        public Dictionary<string, SemVersion> ProjectDependencies
        {
            get
            {
                if (projectDependencies == null)
                    RebuildDependenciesDictionnary();

                return projectDependencies;
            }
        }

        internal static readonly Dictionary<string, Package> packages = new Dictionary<string, Package>();

        private static readonly Dictionary<string, PackageInfo> latestInfos = new Dictionary<string, PackageInfo>();

        [SerializeField]
        private PackageFilter filter;

        [SerializeField]
        internal string lastUpdateTime;

        [SerializeField]
        internal List<PackageInfo> listPackagesOffline;
        [SerializeField]
        internal List<PackageInfo> listPackages;
        [SerializeField]
        internal List<PackageInfo> searchPackages;
        [SerializeField]
        private List<PackageInfo> latestInfoListCache;

        [SerializeField]
        private readonly List<PackageError> packageErrors;

        [SerializeField]
        private int listPackagesVersion;
        [SerializeField]
        private int listPackagesOfflineVersion;

        [SerializeField]
        public bool searchOperationOngoing;
        [SerializeField]
        public bool listOperationOngoing;
        [SerializeField]
        public bool listOperationOfflineOngoing;

        [SerializeField]
        private IListOperation listOperationOffline;
        [SerializeField]
        private IListOperation listOperation;
        [SerializeField]
        private ISearchOperation searchOperation;

        public readonly OperationSignal<ISearchOperation> SearchSignal = new OperationSignal<ISearchOperation>();
        public readonly OperationSignal<IListOperation> ListSignal = new OperationSignal<IListOperation>();

        public PackageFilter Filter
        {
            get { return filter; }

            // For public usage, use SetFilter() instead
            private set
            {
                var changed = value != filter;
                filter = value;

                if (changed)
                    OnFilterChanged(filter);
            }
        }

        private IEnumerable<PackageInfo> CompletePackageInfosList
        {
            get { return listPackagesOffline.Concat(listPackages).Concat(searchPackages); }
        }

        public IEnumerable<PackageInfo> LatestListPackages
        {
            get { return listPackagesVersion > listPackagesOfflineVersion ? listPackages : listPackagesOffline; }
        }

        private PackageInfo GetLatestInfoInCache(string packageId)
        {
            // Sync the list and the dictionary (as dictionaries are not serialized hence won't survive domain reload)
            if (latestInfoListCache.Count != latestInfos.Count)
                foreach (var p in latestInfoListCache)
                    latestInfos[p.PackageId] = p;
            PackageInfo result;
            return latestInfos.TryGetValue(packageId, out result) ? result : null;
        }

        private void UpdateLatestInfoCache(PackageInfo info)
        {
            if (latestInfos.ContainsKey(info.PackageId))
                return;
            latestInfoListCache.Add(info);
            latestInfos[info.PackageId] = info;
        }

        public IEnumerable<PackageInfo> LatestSearchPackages { get { return searchPackages; } }

        public PackageCollection()
        {
            packages.Clear();

            listPackagesOffline = new List<PackageInfo>();
            listPackages = new List<PackageInfo>();
            searchPackages = new List<PackageInfo>();
            latestInfoListCache = new List<PackageInfo>();

            packageErrors = new List<PackageError>();

            listPackagesVersion = 0;
            listPackagesOfflineVersion = 0;

            searchOperationOngoing = false;
            listOperationOngoing = false;
            listOperationOfflineOngoing = false;

            Filter = PackageFilter.All;
        }

        public bool SetFilter(PackageFilter value, bool refresh = true)
        {
            if (value == Filter)
                return false;

            Filter = value;
            if (refresh)
            {
                UpdatePackageCollection();
            }
            return true;
        }

        public void UpdatePackageCollection(bool rebuildDictionary = false)
        {
            if (rebuildDictionary)
                RebuildPackageDictionary();

            RebuildDependenciesDictionnary();

            TriggerPackagesChanged();
        }

        public void TriggerPackagesChanged()
        {
            OnPackagesChanged(Filter, OrderedPackages());
        }

        internal void FetchListOfflineCache(bool forceRefetch = false)
        {
            if (!forceRefetch && (listOperationOfflineOngoing || listPackagesOffline.Any())) return;
            if (listOperationOffline != null)
                listOperationOffline.Cancel();
            listOperationOfflineOngoing = true;
            listOperationOffline = OperationFactory.Instance.CreateListOperation(true);
            listOperationOffline.OnOperationFinalized += () =>
            {
                listOperationOfflineOngoing = false;
                UpdatePackageCollection(true);
            };
            listOperationOffline.GetPackageListAsync(
                infos =>
                {
                    var version = listPackagesVersion;
                    UpdateListPackageInfosOffline(infos, version);
                },
                error => { Debug.LogError("Error fetching package list (offline mode)."); });
        }

        internal void FetchListCache(bool forceRefetch = false)
        {
            if (!forceRefetch && (listOperationOngoing || listPackages.Any())) return;
            if (listOperation != null)
                listOperation.Cancel();
            listOperationOngoing = true;
            listOperation = OperationFactory.Instance.CreateListOperation();
            listOperation.OnOperationFinalized += () =>
            {
                listOperationOngoing = false;
                UpdatePackageCollection(true);
            };
            listOperation.GetPackageListAsync(UpdateListPackageInfos,
                error => { Debug.LogError("Error fetching package list."); });
            ListSignal.SetOperation(listOperation);
        }

        internal void FetchSearchCache(bool forceRefetch = false)
        {
            if (!forceRefetch && (searchOperationOngoing || searchPackages.Any())) return;
            if (searchOperation != null)
                searchOperation.Cancel();
            searchOperationOngoing = true;
            searchOperation = OperationFactory.Instance.CreateSearchOperation();
            searchOperation.OnOperationFinalized += () =>
            {
                searchOperationOngoing = false;
                UpdatePackageCollection(true);
            };
            searchOperation.GetAllPackageAsync(null, UpdateSearchPackageInfos,
                error => { Debug.LogError("Error searching packages online."); });
            SearchSignal.SetOperation(searchOperation);
        }

        public bool NeedsFetchLatest(PackageInfo packageInfo)
        {
            if (packageInfo == null) return false;
            if (packageInfo.HasFullFetch) return false;
            if (packageInfo.Origin != PackageSource.Registry) return false;

            return true;
        }

        public void FetchLatestPackageInfo(PackageInfo packageInfo, bool isDefaultVersion = false)
        {
            if (!NeedsFetchLatest(packageInfo))
                return;

            var fetchLatestInfoOperation = OperationFactory.Instance.CreateSearchOperation();
            fetchLatestInfoOperation.GetAllPackageAsync(packageInfo.PackageId, packageInfos =>
            {
                var result = packageInfos.FirstOrDefault(p => p.PackageId == packageInfo.PackageId);
                result.HasFullFetch = true;

                UpdateLatestInfoCache(result);
                packageInfo.Consolidate(result);

                OnLatestPackageInfoFetched(result, isDefaultVersion);
            }, error => { Debug.LogError("Error fetching latest package info for " + packageInfo.PackageId); });
        }

        private void UpdateListPackageInfosOffline(IEnumerable<PackageInfo> newInfos, int version)
        {
            // List request returns indirect dependencies, and we don't want to see them in list
            foreach (var packageInfo in newInfos)
                packageInfo.IsDiscoverable = packageInfo.IsDirectDependency || searchPackages.Any(p => p.PackageId == packageInfo.PackageId && p.IsDiscoverable);

            listPackagesOfflineVersion = version;
            listPackagesOffline = newInfos.Where(p => p.IsUserVisible).ToList();
        }

        private void UpdateListPackageInfos(IEnumerable<PackageInfo> newInfos)
        {
            // List request returns indirect dependencies, and we don't want to see them in list
            foreach (var packageInfo in newInfos)
                packageInfo.IsDiscoverable = packageInfo.IsDirectDependency || searchPackages.Any(p => p.PackageId == packageInfo.PackageId && p.IsDiscoverable);

            // Each time we fetch list packages, the cache for offline mode will be updated
            // We keep track of the list packages version so that we know which version of cache
            // we are getting with the offline fetch operation.
            listPackagesVersion++;
            listPackages = newInfos.Where(p => p.IsUserVisible).ToList();
            listPackagesOffline = listPackages;
        }

        private void UpdateSearchPackageInfos(IEnumerable<PackageInfo> newInfos)
        {
            // Search request doesn't return indirect dependencies, so we can always set discoverable to true
            foreach (var packageInfo in newInfos)
                packageInfo.IsDiscoverable = true;

            searchPackages = newInfos.Where(p => p.IsUserVisible).ToList();

            // Only refresh update time after a search operation successfully returns while online
            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                lastUpdateTime = DateTime.Now.ToString("MMM d, HH:mm");
                OnUpdateTimeChange(lastUpdateTime);
            }
        }

        private IEnumerable<Package> OrderedPackages()
        {
            return packages.Values.OrderBy(pkg => pkg.VersionToDisplay == null ? pkg.Name : pkg.VersionToDisplay.DisplayName).AsEnumerable();
        }

        public Package GetPackageByName(string name)
        {
            Package package;
            packages.TryGetValue(name, out package);
            return package;
        }

        public Package GetPackage(PackageInfo packageInfo)
        {
            return GetPackageByName(packageInfo.Name);
        }

        public PackageVersion GetPackageVersion(PackageInfo packageInfo)
        {
            return new PackageVersion(GetPackage(packageInfo), packageInfo);
        }

        /// <summary>
        /// Get package from package Id
        /// </summary>
        /// <param name="packageId">mypackage@1.0.0</param>
        /// <returns></returns>
        public PackageVersion GetPackageVersion(string packageId)
        {
            var tokens = packageId.Split('@');    // 0 = PackageName -- 1 = PackageVersion
            var package = GetPackageByName(tokens[0]);
            if (package == null)
                return null;

            return new PackageVersion(package, package.Versions.FirstOrDefault(p => p.PackageId == packageId));
        }

        private Error GetPackageError(Package package)
        {
            if (null == package) return null;
            var firstMatchingError = packageErrors.FirstOrDefault(p => p.PackageName == package.Name);
            return firstMatchingError != null ? firstMatchingError.Error : null;
        }

        public void AddPackageError(Package package, Error error)
        {
            if (null == package || null == error) return;
            package.Error = error;
            packageErrors.Add(new PackageError(package.Name, error));
        }

        public void RemovePackageErrors(Package package)
        {
            if (null == package) return;
            package.Error = null;
            packageErrors.RemoveAll(p => p.PackageName == package.Name);
        }

        private void RebuildPackageDictionary()
        {
            // Merge list & search packages
            var allPackageInfos = new List<PackageInfo>(LatestListPackages);
            var installedPackageIds = new HashSet<string>(allPackageInfos.Select(p => p.PackageId));
            allPackageInfos.AddRange(searchPackages.Where(p => !installedPackageIds.Contains(p.PackageId)));

            PackageManagerPrefs.ShowPreviewPackagesFromInstalled = allPackageInfos.Any(p => p.IsInstalled && p.IsPreview);

            // Filter Preview versions
            if (!PackageManagerPrefs.ShowPreviewPackages)
            {
                allPackageInfos = allPackageInfos.Where(p => !p.IsPreRelease || installedPackageIds.Contains(p.PackageId)).ToList();
            }

            // Consolidate with latest fetched package infos
            foreach (var p in allPackageInfos)
            {
                if (p.HasFullFetch)
                    continue;
                var latestInfo = GetLatestInfoInCache(p.PackageId);
                if (latestInfo != null)
                    p.Consolidate(latestInfo);
            }

            // Rebuild packages dictionary
            packages.Clear();
            var outdatedDisplayPackages = new List<PackageInfo>();
            foreach (var p in allPackageInfos)
            {
                var packageName = p.Name;
                if (packages.ContainsKey(packageName))
                    continue;

                var packageQuery = from pkg in allPackageInfos where pkg.Name == packageName select pkg;
                var package = new Package(packageName, packageQuery);
                var displayVersion = package.VersionToDisplay;
                if (NeedsFetchLatest(displayVersion))
                    outdatedDisplayPackages.Add(displayVersion);
                package.Error = GetPackageError(package);
                packages[packageName] = package;
            }

            // Send a request for latest info
            foreach (var p in outdatedDisplayPackages)
                FetchLatestPackageInfo(p, true);

            MarkDependentModulesAsInstalled();
        }

        private void RebuildDependenciesDictionnary()
        {
            projectDependencies = new Dictionary<string, SemVersion>();
            var allPackageInfos = new List<PackageInfo>(LatestListPackages);
            var installedPackages = allPackageInfos.Where(p => p.IsInstalled);

            foreach (var p in installedPackages)
            {
                if (projectDependencies.ContainsKey(p.Name))
                {
                    var version = projectDependencies[p.Name];
                    if (p.Version.CompareByPrecedence(version) > 0)
                        projectDependencies[p.Name] = p.Version;
                }
                else
                {
                    if (p.IsInDevelopment)
                        projectDependencies[p.Name] = EmbdeddedVersion;
                    else if (p.IsLocal)
                        projectDependencies[p.Name] = LocalVersion;
                    else
                        projectDependencies[p.Name] = p.Version;
                }

                var dependencies = p.Info == null ? null : p.Info.resolvedDependencies;
                if (dependencies == null)
                    continue;
                foreach (var dependency in dependencies)
                {
                    if (dependency.version.StartsWith("file:"))
                    {
                        var dependencyPath = dependency.version.Substring(5).Replace('\\', '/');
                        var projectPath = Directory.GetCurrentDirectory().Replace('\\', '/');
                        if (dependencyPath.StartsWith(projectPath))
                            projectDependencies[dependency.name] = EmbdeddedVersion;
                        else
                            projectDependencies[dependency.name] = LocalVersion;
                    }
                    else
                    {
                        SemVersion newVersion = dependency.version;
                        if (projectDependencies.ContainsKey(dependency.name))
                        {
                            var version = projectDependencies[dependency.name];
                            if (newVersion.CompareByPrecedence(version) > 0)
                                projectDependencies[dependency.name] = newVersion;
                        }
                        else
                            projectDependencies[dependency.name] = newVersion;
                    }
                }
            }
        }

        /// <summary>
        /// We need no mark modules that are a dependency of any package/module as installed.
        /// This is how the module system currently behaves, so the UI needs to match this.
        /// </summary>
        private void MarkDependentModulesAsInstalled()
        {
            // Reset previous installed state
            foreach (var packageInfo in CompletePackageInfosList.Where(p => p.IsBuiltIn))
                packageInfo.IsInstalledByDependency = false;

            // Set dependent installed state
            var installedPackages = LatestListPackages.Where(p => p.IsInstalled);
            foreach (var package in installedPackages)
                MarkModulesAsInstalled(package.DependentModules);
        }

        private void MarkModulesAsInstalled(IEnumerable<DependencyInfo> modules)
        {
            foreach (var module in modules)
            {
                var latestInfo = CompletePackageInfosList.FirstOrDefault(p => p.PackageId == PackageInfo.FormatPackageId(module));
                if (latestInfo == null)
                    Debug.LogWarning("Module does not have package info: " + module.name);
                if (latestInfo != null && !latestInfo.IsInstalled)
                    latestInfo.IsInstalledByDependency = true;
            }
        }

        public IEnumerable<PackageInfo> GetDependents(PackageInfo packageInfo)
        {
            var installedRoots = LatestListPackages.Where(p => p.IsInstalled && p.Info.isDirectDependency);
            var dependsOnPackage = installedRoots.Where(p =>
                p.Info.resolvedDependencies.Select(r => r.name).Contains(packageInfo.Name));

            return dependsOnPackage;
        }
    }
}
