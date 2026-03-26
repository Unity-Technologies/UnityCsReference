// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class SampleInfo
    {
        public string displayName;
        public string path;
        public string description;
        public bool interactiveImport;
        public string[] images;

        public bool IsEquivalent(SampleInfo other)
        {
            return other != null &&
                   (displayName ?? string.Empty) == (other.displayName ?? string.Empty) &&
                   (path ?? string.Empty) == (other.path ?? string.Empty) &&
                   (description ?? string.Empty) == (other.description ?? string.Empty) &&
                   (images ?? Array.Empty<string>()).IsSequenceEqual(other.images ?? Array.Empty<string>()) &&
                   interactiveImport == other.interactiveImport;
        }
    }

    [Serializable]
    internal class SampleInfoCollection : IReadOnlyCollection<SampleInfo>
    {
        public string packageUniqueId { get; private set; }
        public string packageDisplayName { get; private set; }
        public string packageVersion { get; private set; }

        [SerializeField]
        private SampleInfo[] m_SampleInfos;

        public SampleInfoCollection(string packageUniqueId, string packageDisplayName, string packageVersion, SampleInfo[] samplesInfos)
        {
            this.packageUniqueId = packageUniqueId;
            this.packageDisplayName = packageDisplayName;
            this.packageVersion = packageVersion;
            m_SampleInfos = samplesInfos ?? Array.Empty<SampleInfo>();
        }

        public bool IsEquivalent(SampleInfoCollection other)
        {
            if (Count != other.Count || packageUniqueId != other.packageUniqueId || packageDisplayName != other.packageDisplayName || packageVersion != other.packageVersion)
                return false;

            for (var i = 0; i < Count; i++)
                if (!m_SampleInfos[i].IsEquivalent(other.m_SampleInfos[i]))
                    return false;

            return true;
        }

        public IEnumerator<SampleInfo> GetEnumerator() => ((IEnumerable<SampleInfo>)m_SampleInfos).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public int Count => m_SampleInfos?.Length ?? 0;
    }

    internal interface ISampleCache : IService
    {
        event Action<IReadOnlyCollection<string> /* packageUniqueIds */> onSamplesChanged;
        event Action<IReadOnlyCollection<string> /* sanitizedPackageDisplayNames */> onImportedSamplesChanged;

        IReadOnlyCollection<SampleInfoCollection> sampleInfoCollections { get; }

        void FullScanImportedSamples();
        IReadOnlyCollection<string> ScanImportedSampleVersions(string sanitizedPackageDisplayName, string sanitizedSampleDisplayName);

        void UpdateImportedSamplesOnAssetChanged(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths);

        ImportedSampleCollection GetImportedSampleCollection(string sanitizedPackageDisplayName);
        SampleInfoCollection GetSampleInfoCollection(string packageUniqueId);
        SampleInfoCollection ParseSamples(PackageInfo packageInfo);
    }

    [Serializable]
    internal class SampleCache : BaseService<ISampleCache>, ISampleCache, ISerializationCallbackReceiver
    {
        public event Action<IReadOnlyCollection<string> /* packageUniqueIds */> onSamplesChanged;
        public event Action<IReadOnlyCollection<string> /* sanitizedPackageDisplayNames */> onImportedSamplesChanged;

        private Dictionary<string, SampleInfoCollection> m_SampleInfoCollections = new();

        private Dictionary<string, ImportedSampleCollection> m_ImportedSampleCollections = new();

        [SerializeField]
        private SampleInfoCollection[] m_SerializedSampleInfoCollections;

        [SerializeField]
        private ImportedSampleCollection[] m_SerializedImportedSampleCollections;

        public IReadOnlyCollection<SampleInfoCollection> sampleInfoCollections => m_SampleInfoCollections.Values;

        private readonly IIOProxy m_IOProxy;
        private readonly IAssetDatabaseProxy m_AssetDatabase;
        private readonly IUpmCache m_UpmCache;
        public SampleCache(IIOProxy ioProxy, IAssetDatabaseProxy assetDatabase, IUpmCache upmCache)
        {
            m_IOProxy = RegisterDependency(ioProxy);
            m_AssetDatabase = RegisterDependency(assetDatabase);
            m_UpmCache = RegisterDependency(upmCache);
        }

        public override void OnEnable()
        {
            m_UpmCache.onPackageInfosUpdated += OnPackageInfosUpdated;
        }

        public override void OnDisable()
        {
            m_UpmCache.onPackageInfosUpdated -= OnPackageInfosUpdated;
        }

        private void OnPackageInfosUpdated(IReadOnlyCollection<(PackageInfo oldInfo, PackageInfo newInfo)> updateInfos, PackagesChangedSource changedSource)
        {
            if (changedSource != PackagesChangedSource.UpmList && changedSource != PackagesChangedSource.AddAndRemove)
                return;

            var updatedUniqueIds = new List<string>();
            foreach (var (oldInfo, newInfo) in updateInfos)
            {
                // The unique id may change if the same package changed source from Asset Store to a scoped registry or vice versa
                // In this case, we will treat it as if the old package is removed and a new package is added
                var oldUniqueId = oldInfo?.GetUniqueId() ?? string.Empty;
                var newUniqueId = newInfo?.GetUniqueId() ?? string.Empty;

                var oldCollection = m_SampleInfoCollections.GetValueOrDefault(oldUniqueId);
                var newCollection = ParseSamples(newInfo);
                if (oldCollection == null && newCollection == null)
                    continue;

                if (oldCollection == null || newCollection == null || !oldCollection.IsEquivalent(newCollection))
                {
                    if (!string.IsNullOrEmpty(oldUniqueId))
                        updatedUniqueIds.Add(oldUniqueId);
                    if (oldUniqueId != newUniqueId && !string.IsNullOrEmpty(newUniqueId))
                        updatedUniqueIds.Add(newUniqueId);
                }
                if (newCollection == null)
                    m_SampleInfoCollections.Remove(oldUniqueId);
                else
                    m_SampleInfoCollections[newUniqueId] = newCollection;
            }

            if (updatedUniqueIds.Count > 0)
                onSamplesChanged?.Invoke(updatedUniqueIds);
        }

        public void OnBeforeSerialize()
        {
            m_SampleInfoCollections.Values.ToArray(ref m_SerializedSampleInfoCollections);
            m_ImportedSampleCollections.Values.ToArray(ref m_SerializedImportedSampleCollections);
        }

        public void OnAfterDeserialize()
        {
            m_SerializedSampleInfoCollections.ToDictionary(i => i.packageUniqueId, ref m_SampleInfoCollections);
            m_SerializedImportedSampleCollections.ToDictionary(i => i.sanitizedPackageDisplayName, ref m_ImportedSampleCollections);
        }

        public void FullScanImportedSamples()
        {
            var packageFolders = m_AssetDatabase.GetSubFolders("Assets/Samples");
            var result = new Dictionary<string, ImportedSampleCollection>();
            foreach (var packageFolder in packageFolders)
            {
                var packageDisplayName = IOUtils.GetFileName(packageFolder);
                var samples = new Dictionary<string, ImportedSample>();
                var versionFolders = m_AssetDatabase.GetSubFolders(packageFolder);
                foreach (var versionFolder in versionFolders)
                {
                    var versionString = IOUtils.GetFileName(versionFolder);
                    var sampleFolders = m_AssetDatabase.GetSubFolders(versionFolder);
                    foreach (var sampleFolder in sampleFolders)
                    {
                        var sampleName = IOUtils.GetFileName(sampleFolder);
                        if (samples.TryGetValue(sampleName, out var sample))
                            sample.versions.Add(versionString);
                        else
                            samples[sampleName] = new ImportedSample
                            {
                                sanitizedDisplayName = sampleName,
                                versions = new List<string> { versionString }
                            };
                    }
                }
                foreach (var sample in samples.Values)
                    sample.versions.Sort();
                if (samples.Count > 0)
                    result[packageDisplayName] = new ImportedSampleCollection(packageDisplayName, samples);
            }
            var oldCollections = m_ImportedSampleCollections;
            m_ImportedSampleCollections = result;
            var sanitizedPackageNames = FindUpdatedSamplePackageNames(oldCollections, m_ImportedSampleCollections);
            if (sanitizedPackageNames.Count > 0)
                onImportedSamplesChanged?.Invoke(sanitizedPackageNames);
        }

        public IReadOnlyCollection<string> ScanImportedSampleVersions(string sanitizedPackageDisplayName, string sanitizedSampleDisplayName)
        {
            if (string.IsNullOrEmpty(sanitizedPackageDisplayName) || string.IsNullOrEmpty(sanitizedSampleDisplayName))
                return Array.Empty<string>();
            var versionFolders = m_AssetDatabase.GetSubFolders($"Assets/Samples/{sanitizedPackageDisplayName}");
            if (versionFolders.Length == 0)
                return Array.Empty<string>();

            var result = new List<string>(versionFolders.Length);
            foreach (var versionFolder in versionFolders)
            {
                var versionString = IOUtils.GetFileName(versionFolder);
                if (m_IOProxy.DirectoryExists(IOUtils.PathsCombine(versionFolder, sanitizedSampleDisplayName)))
                    result.Add(versionString);
            }
            return result;
        }

        public void UpdateImportedSamplesOnAssetChanged(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (!importedAssets.Join(deletedAssets, movedAssets, movedFromAssetPaths).AnyMatches(i => i.StartsWith("Assets/Samples/")))
                return;
            FullScanImportedSamples();
        }

        private static IReadOnlyCollection<string> FindUpdatedSamplePackageNames(Dictionary<string, ImportedSampleCollection> oldCollections, Dictionary<string, ImportedSampleCollection> newCollections)
        {
            var sanitizedPackageNames = new List<string>();
            foreach (var oldCollection in oldCollections.Values)
            {
                if (newCollections.TryGetValue(oldCollection.sanitizedPackageDisplayName, out var newCollection) && oldCollection.IsEquivalent(newCollection))
                    continue;
                sanitizedPackageNames.Add(oldCollection.sanitizedPackageDisplayName);
            }

            foreach (var newCollection in newCollections.Values)
            {
                if (oldCollections.ContainsKey(newCollection.sanitizedPackageDisplayName))
                    continue;
                sanitizedPackageNames.Add(newCollection.sanitizedPackageDisplayName);
            }
            return sanitizedPackageNames;
        }

        public ImportedSampleCollection GetImportedSampleCollection(string sanitizedPackageDisplayName) => m_ImportedSampleCollections.GetValueOrDefault(sanitizedPackageDisplayName ?? string.Empty);

        public SampleInfoCollection GetSampleInfoCollection(string packageUniqueId) => m_SampleInfoCollections.GetValueOrDefault(packageUniqueId ?? string.Empty);

        public SampleInfoCollection ParseSamples(PackageInfo packageInfo)
        {
            if (packageInfo == null || (string.IsNullOrEmpty(packageInfo.upmReserved) && string.IsNullOrEmpty(packageInfo.resolvedPath)))
                return null;

            try
            {
                IEnumerable<IDictionary<string, object>> samples = null;
                var upmReserved = m_UpmCache.ParseUpmReserved(packageInfo);
                if (upmReserved != null)
                    samples = upmReserved.GetEnumerable<IDictionary<string, object>>("samples");

                if (samples == null)
                {
                    var jsonPath = IOUtils.PathsCombine(packageInfo.resolvedPath, "package.json");
                    if (m_IOProxy.FileExists(jsonPath))
                    {
                        var packageJson = Json.Deserialize(m_IOProxy.FileReadAllText(jsonPath)) as Dictionary<string, object>;
                        samples = packageJson.GetEnumerable<IDictionary<string, object>>("samples");
                    }
                }

                if (samples == null)
                    return null;

                var result = new List<SampleInfo>();
                foreach (var sample in samples)
                {
                    var sampleInfo = new SampleInfo
                    {
                        displayName = sample.GetString("displayName"),
                        path = sample.GetString("path"),
                        description = sample.GetString("description"),
                        interactiveImport = sample.Get("interactiveImport", false)
                    };
                    var imagePaths = new List<string>();
                    if (sample.TryGetValue("images", out var imgObj) && imgObj is List<object> imgList)
                    {
                        foreach (var item in imgList)
                            if (item is string stringItem && !string.IsNullOrEmpty(stringItem))
                                imagePaths.Add(stringItem);
                    }
                    sampleInfo.images = imagePaths.ToArray();
                    result.Add(sampleInfo);
                }

                return result.Count == 0 ? null : new SampleInfoCollection(packageInfo.GetUniqueId(), packageInfo.displayName, packageInfo.version, result.ToArray());
            }
            catch (IOException e)
            {
                Debug.Log($"[Package Manager Window] Cannot find samples for package {packageInfo.displayName}: {e}");
                return null;
            }
            catch (InvalidCastException e)
            {
                Debug.Log($"[Package Manager Window] Invalid sample data for package {packageInfo.displayName}: {e}");
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
