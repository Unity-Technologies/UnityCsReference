// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface ISampleFactory : IService
    {
        IReadOnlyCollection<Sample> ParseSamples(PackageInfo packageInfo);
    }

    internal class SampleFactory : BaseService<ISampleFactory>, ISampleFactory
    {
        private readonly IIOProxy m_IOProxy;
        private readonly IUpmCache m_UpmCache;
        private readonly ISampleCache m_SampleCache;
        private readonly IPackageDatabase m_PackageDatabase;
        public SampleFactory(IIOProxy ioProxy,
            IUpmCache upmCache,
            ISampleCache sampleCache,
            IPackageDatabase packageDatabase)
        {
            m_IOProxy = RegisterDependency(ioProxy);
            m_UpmCache = RegisterDependency(upmCache);
            m_SampleCache = RegisterDependency(sampleCache);
            m_PackageDatabase = RegisterDependency(packageDatabase);
        }

        public override void OnEnable()
        {
            m_SampleCache.onSamplesChanged += OnSamplesChanged;
            m_SampleCache.onImportedSamplesChanged += OnImportedSamplesChanged;

            m_PackageDatabase.onPackagesChanged += OnPackagesChanged;
        }

        public override void OnDisable()
        {
            m_SampleCache.onSamplesChanged -= OnSamplesChanged;
            m_SampleCache.onImportedSamplesChanged -= OnImportedSamplesChanged;

            m_PackageDatabase.onPackagesChanged -= OnPackagesChanged;
        }

        private void OnSamplesChanged(IReadOnlyCollection<string> packageTechnicalNames)
        {
            GenerateSamplesAndTriggerChangeEvent(packageTechnicalNames);
        }

        private void OnImportedSamplesChanged(IReadOnlyCollection<string> sanitizedPackageDisplayNames)
        {
            if (sanitizedPackageDisplayNames.Count == 0)
                return;

            var packageTechnicalNames = new List<string>(sanitizedPackageDisplayNames.Count);
            foreach (var collection in m_SampleCache.sampleInfoCollections)
                if (sanitizedPackageDisplayNames.ContainsMatches(IOUtils.SanitizeFileName(collection.packageDisplayName)))
                    packageTechnicalNames.Add(collection.packageTechnicalName);
            GenerateSamplesAndTriggerChangeEvent(packageTechnicalNames);
        }

        private void OnPackagesChanged(PackagesChangeArgs args)
        {
            var packageTechnicalNames = new List<string>(args.added.Count + args.updated.Count + args.removed.Count);
            packageTechnicalNames.AddRange(args.added.SelectAsEnumerable(i => i.name));
            packageTechnicalNames.AddRange(args.updated.SelectAsEnumerable(i => i.name));
            packageTechnicalNames.AddRange(args.removed.SelectAsEnumerable(i => i.name));
            GenerateSamplesAndTriggerChangeEvent(packageTechnicalNames);
        }

        public void GenerateSamplesAndTriggerChangeEvent(IReadOnlyCollection<string> packageTechnicalNames)
        {
            if (packageTechnicalNames.Count == 0)
                return;

            var addedOrUpdated = new List<SampleCollection>();
            var removed = new List<string>();
            foreach (var packageTechnicalName in packageTechnicalNames)
            {
                var package = m_PackageDatabase.GetPackageByIdOrName(packageTechnicalName);
                var sampleInfoCollection = m_SampleCache.GetSampleInfoCollection(packageTechnicalName);
                var packageInfo = m_UpmCache.GetInstalledPackageInfo(packageTechnicalName);
                if (sampleInfoCollection == null || packageInfo == null || package == null)
                    removed.Add(packageTechnicalName);
                else
                    addedOrUpdated.Add(Convert(packageInfo, sampleInfoCollection, package));
            }
            if (addedOrUpdated.Count > 0 || removed.Count > 0)
                m_PackageDatabase.UpdateSamples(addedOrUpdated, removed);
        }

        public IReadOnlyCollection<Sample> ParseSamples(PackageInfo packageInfo)
        {
            var sampleCollection = m_SampleCache.ParseSamples(packageInfo);
            if (sampleCollection == null)
                return Array.Empty<Sample>();
            var package = m_PackageDatabase.GetPackageByIdOrName(sampleCollection.packageTechnicalName);
            return Convert(packageInfo, sampleCollection, package);
        }

        private SampleCollection Convert(PackageInfo packageInfo, SampleInfoCollection sampleInfoCollection, IPackage package)
        {
            var samples = sampleInfoCollection.SelectToNewArray(sample =>
            {
                var resolvedSamplePath = string.IsNullOrEmpty(sample.path)
                    ? string.Empty
                    : IOUtils.PathsCombine(packageInfo.resolvedPath, sample.path);
                var sanitizedPackageDisplayName = IOUtils.SanitizeFileName(packageInfo.displayName);
                var sanitizedSampleDisplayName = IOUtils.SanitizeFileName(sample.displayName);
                var importPath = IOUtils.PathsCombine(
                    Application.dataPath,
                    "Samples",
                    sanitizedPackageDisplayName,
                    packageInfo.version,
                    sanitizedSampleDisplayName
                );
                var isImported = m_IOProxy.DirectoryExists(importPath);
                var sizeInBytes = !string.IsNullOrEmpty(resolvedSamplePath) && m_IOProxy.DirectoryExists(resolvedSamplePath)
                    ? m_IOProxy.DirectorySizeInBytes(resolvedSamplePath)
                    : 0;
                var previousImportPaths = GetPreviousImportPaths(sanitizedPackageDisplayName, sanitizedSampleDisplayName);
                var assetPackagePath = string.IsNullOrEmpty(resolvedSamplePath) ? null : FindAssetPackagePath(resolvedSamplePath);
                return new Sample(sample, sampleInfoCollection.packageTechnicalName, resolvedSamplePath, importPath, isImported, sizeInBytes, previousImportPaths, assetPackagePath, package);
            });
            return new SampleCollection(sampleInfoCollection.packageTechnicalName, samples);
        }

        private string FindAssetPackagePath(string resolvedSamplePath)
        {
            try
            {
                var assetPackages = m_IOProxy.GetFiles(resolvedSamplePath, "*.unitypackage");
                return assetPackages.Length > 0 ? assetPackages[0] : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string[] GetPreviousImportPaths(string sanitizedPackageDisplayName, string sanitizedSampleDisplayName)
        {
            if (string.IsNullOrEmpty(sanitizedPackageDisplayName) || string.IsNullOrEmpty(sanitizedSampleDisplayName))
                return Array.Empty<string>();

            var packageDir = IOUtils.PathsCombine(
                Application.dataPath,
                "Samples",
                sanitizedPackageDisplayName);

            IReadOnlyCollection<string> importedVersions = m_SampleCache.GetImportedSampleCollection(sanitizedPackageDisplayName)?.GetImportedSample(sanitizedSampleDisplayName)?.versions;
            importedVersions ??= m_SampleCache.ScanImportedSampleVersions(sanitizedPackageDisplayName, sanitizedSampleDisplayName) ?? Array.Empty<string>();
            return importedVersions.SelectToNewArray(v => IOUtils.PathsCombine(packageDir, v, sanitizedSampleDisplayName));
        }
    }
}
