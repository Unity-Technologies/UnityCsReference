// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.PackageManager.UI.Internal;

namespace UnityEditor.PackageManager.UI
{
    /// <summary>
    /// Class for Package Sample
    /// </summary>
    [Serializable]
    public struct Sample
    {
        /// <summary>
        /// Sample import options
        /// </summary>
        [Flags]
        public enum ImportOptions
        {
            ///<summary>None</summary>
            None = 0x0,
            ///<summary>Override previous imports of the sample</summary>
            OverridePreviousImports = 0x1,
            ///<summary>Hide the import window when importing a sample that is an asset package (a .unitypackage file)</summary>
            HideImportWindow = 0x2
        }

        /// <value>
        /// The display name of the package sample
        /// </value>
        public string displayName { get; private set; }

        /// <value>
        /// The description of the package sample
        /// </value>
        public string description { get; private set; }

        /// <value>
        /// <para>The full path to where the sample is on disk, inside the package that contains the sample.</para>
        /// It is usually in the form of `Resolved Full Path to Package/Samples~/Sample Display Name/`
        /// </value>
        public string resolvedPath { get; private set; }

        /// <value>
        /// <para>The full path to where the sample will be imported, under the project assets folder.</para>
        /// <para>It is in the form of `Project Full Path/Assets/Samples/Package Display Name/Package Version/Sample Display Name/`.</para>
        /// If the sample is an asset package (a .unitypackage file), this value won't be taken into consideration during import
        /// </value>
        public string importPath { get; private set; }

        /// <value>
        /// Indicates whether to show the import window when importing a sample that is an asset package (a .unitypackage file)
        /// </value>
        public bool interactiveImport { get; private set; }

        /// <value>
        /// Indicates if the sample has already been imported
        /// </value>
        public bool isImported { get; private set; }

        [SerializeField]
        private string[] m_PreviousImportPaths;
        internal IReadOnlyList<string> previousImportPaths => m_PreviousImportPaths;

        internal ulong sizeInBytes { get; private set; }

        internal string packageUniqueId { get; private set; }

        private Internal.IPackage m_Package;
        internal Internal.IPackage package => m_Package;

        internal string uniqueId => $"{packageUniqueId}/{displayName}";

        internal Sample(SampleInfo sampleInfo, string packageUniqueId, string resolvedPath, string importPath, bool isImported, ulong sizeInBytes, string[] previousImportPaths)
        {
            displayName = sampleInfo.displayName;
            description = sampleInfo.description;
            interactiveImport = sampleInfo.interactiveImport;
            this.packageUniqueId = packageUniqueId;
            this.resolvedPath = resolvedPath;
            this.importPath = importPath;
            this.isImported = isImported;
            this.sizeInBytes = sizeInBytes;
            m_PreviousImportPaths = previousImportPaths ?? Array.Empty<string>();
        }

        internal bool IsEquivalent(Sample other)
        {
            return displayName == other.displayName &&
                   description == other.description &&
                   packageUniqueId == other.packageUniqueId &&
                   resolvedPath == other.resolvedPath &&
                   importPath == other.importPath &&
                   interactiveImport == other.interactiveImport &&
                   isImported == other.isImported &&
                   sizeInBytes == other.sizeInBytes &&
                   m_PreviousImportPaths.IsSequenceEqual(other.m_PreviousImportPaths);
        }

        internal bool isDefault =>
            displayName == null &&
            description == null &&
            packageUniqueId == null &&
            resolvedPath == null &&
            importPath == null &&
            !interactiveImport &&
            !isImported &&
            sizeInBytes == 0 &&
            m_PreviousImportPaths.IsSequenceEqual(null);

        internal static IReadOnlyCollection<Sample> FindByPackage(PackageInfo packageInfo)
        {
            return ServicesContainer.instance.Resolve<ISampleFactory>().ParseSamples(packageInfo);
        }

        internal bool MatchesSearchText(string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
                return true;

            if ((displayName ?? string.Empty).Contains(searchText, StringComparison.CurrentCultureIgnoreCase)
                || (description ?? string.Empty).Contains(searchText, StringComparison.CurrentCultureIgnoreCase)
                || m_Package.versions.primary.MatchesSearchText(searchText, SearchTextParams.DisplayName | SearchTextParams.TechnicalName | SearchTextParams.Author | SearchTextParams.SignatureOrgName))
                return true;

            return false;
        }

        /// <summary>
        /// Given a package of a specific version, find a list of samples in that package.
        /// </summary>
        /// <param name="packageName">The name of the package</param>
        /// <param name="packageVersion">The version of the package</param>
        /// <returns>A list of samples in the given package</returns>
        public static IEnumerable<Sample> FindByPackage(string packageName, string packageVersion)
        {
            var upmCache = ServicesContainer.instance.Resolve<IUpmCache>();
            var packageInfo = upmCache.installedPackageInfosReady ? upmCache.GetInstalledPackageInfoByName(packageName) : PackageInfo.GetAllRegisteredPackages().FirstMatch(p => p.name == packageName);
            if (packageInfo != null && (packageInfo.version == packageVersion || string.IsNullOrEmpty(packageVersion)))
                return FindByPackage(packageInfo);
            return Array.Empty<Sample>();
        }

        /// <summary>
        /// Imports the package sample into the `Assets` folder.
        /// </summary>
        /// <param name="options">
        /// <para>Custom import options. See <see cref="UnityEditor.PackageManager.UI.Sample.ImportOptions"/> for more information.</para>
        /// Note that <see cref="UnityEditor.PackageManager.UI.Sample.ImportOptions"/> are flag attributes,
        /// therefore you can set multiple import options using the `|` operator
        /// </param>
        /// <returns>Returns whether the import is successful</returns>
        public bool Import(ImportOptions options = ImportOptions.None)
        {
            return ServicesContainer.instance.Resolve<ISampleImporter>().Import(this, options);
        }

        internal class SampleModifier
        {
            public Sample SetPackage(Sample sample, Internal.IPackage package)
            {
                sample.m_Package = package;
                return sample;
            }
        }
    }
}
