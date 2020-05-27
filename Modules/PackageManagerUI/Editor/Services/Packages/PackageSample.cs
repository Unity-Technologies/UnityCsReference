// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.PackageManager.UI
{
    /// <summary>
    /// Struct for Package Sample
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
        public bool isImported
        {
            get { return !string.IsNullOrEmpty(importPath) && m_IOProxy.DirectoryExists(importPath); }
        }

        [NonSerialized]
        private IOProxy m_IOProxy;
        [NonSerialized]
        private AssetDatabaseProxy m_AssetDatabase;
        internal Sample(IOProxy ioProxy, AssetDatabaseProxy assetDatabase, string displayName, string description, string resolvedPath, string importPath, bool interactiveImport)
        {
            m_IOProxy = ioProxy;
            m_AssetDatabase = assetDatabase;
            this.displayName = displayName;
            this.description = description;
            this.resolvedPath = resolvedPath;
            this.importPath = importPath;
            this.interactiveImport = interactiveImport;
        }

        /// <summary>
        /// Given a package of a specific version, find a list of samples in that package.
        /// </summary>
        /// <param name="packageName">The name of the package</param>
        /// <param name="packageVersion">The version of the package</param>
        /// <returns>A list of samples in the given package</returns>
        public static IEnumerable<Sample> FindByPackage(string packageName, string packageVersion)
        {
            var packageDatabase = ServicesContainer.instance.Resolve<PackageDatabase>();
            var package = packageDatabase.GetPackage(packageName);
            if (package != null)
            {
                var version = package.versions.installed;
                if (!string.IsNullOrEmpty(packageVersion))
                    version = package.versions.FirstOrDefault(v => v.version?.ToString() == packageVersion);
                return packageDatabase.GetSamples(version);
            }
            return Enumerable.Empty<Sample>();
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
            string[] unityPackages;
            var interactive = (options & ImportOptions.HideImportWindow) != ImportOptions.None ? false : interactiveImport;
            if ((unityPackages = m_IOProxy.DirectoryGetFiles(resolvedPath, "*.unitypackage")).Length == 1)
                m_AssetDatabase.ImportPackage(unityPackages[0], interactive);
            else
            {
                var prevImports = previousImports;
                if (prevImports.Count > 0 && (options & ImportOptions.OverridePreviousImports) == ImportOptions.None)
                    return false;
                foreach (var v in prevImports)
                    m_IOProxy.RemovePathAndMeta(v, true);

                m_IOProxy.DirectoryCopy(resolvedPath, importPath, true);
                m_AssetDatabase.Refresh();
            }
            return true;
        }

        internal List<string> previousImports
        {
            get
            {
                var result = new List<string>();
                if (!string.IsNullOrEmpty(importPath))
                {
                    var importDirectoryInfo = new DirectoryInfo(importPath);
                    if (importDirectoryInfo.Parent.Parent.Exists)
                    {
                        var versionDirs = importDirectoryInfo.Parent.Parent.GetDirectories();
                        foreach (var d in versionDirs)
                        {
                            var p = Path.Combine(d.ToString(), importDirectoryInfo.Name);
                            if (m_IOProxy.DirectoryExists(p))
                                result.Add(p);
                        }
                    }
                }
                return result;
            }
        }

        internal string size
        {
            get
            {
                if (string.IsNullOrEmpty(resolvedPath) || !m_IOProxy.DirectoryExists(resolvedPath))
                    return "0 KB";
                var sizeInBytes = m_IOProxy.DirectorySizeInBytes(resolvedPath);
                return UIUtils.ConvertToHumanReadableSize(sizeInBytes);
            }
        }
    }
}
