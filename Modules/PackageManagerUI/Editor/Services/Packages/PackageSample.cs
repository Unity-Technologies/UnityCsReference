// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor.PackageManager.UI.Internal;

namespace UnityEditor.PackageManager.UI
{
    /// <summary>
    /// Struct for Package Sample
    /// </summary>
    [Serializable]
    public struct Sample
    {
        private static readonly string k_CopySamplesFilesTitle = L10n.Tr("Copying samples files");
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
            get
            {
                try
                {
                    return !string.IsNullOrEmpty(importPath) && m_IOProxy.DirectoryExists(importPath);
                }
                catch (IOException e)
                {
                    Debug.Log($"[Package Manager Window] Cannot determine if sample {displayName} is imported: {e.Message}");
                    return false;
                }
            }
        }

        [NonSerialized]
        private IOProxy m_IOProxy;
        [NonSerialized]
        private AssetDatabaseProxy m_AssetDatabase;
        internal Sample(IOProxy ioProxy, AssetDatabaseProxy assetDatabase, string displayName, string description, string resolvedPath, string importPath, bool interactiveImport)
        {
            m_PreviousImports = null;
            m_Size = null;
            m_IOProxy = ioProxy;
            m_AssetDatabase = assetDatabase;
            this.displayName = displayName;
            this.description = description;
            this.resolvedPath = resolvedPath;
            this.importPath = importPath;
            this.interactiveImport = interactiveImport;
        }

        internal static IEnumerable<Sample> FindByPackage(PackageInfo package, UpmCache upmCache, IOProxy ioProxy, AssetDatabaseProxy assetDatabaseProxy)
        {
            if (string.IsNullOrEmpty(package?.upmReserved) && string.IsNullOrEmpty(package.resolvedPath))
                return Enumerable.Empty<Sample>();

            try
            {
                IEnumerable<IDictionary<string, object>> samples = null;
                var upmReserved = upmCache.ParseUpmReserved(package);
                if (upmReserved != null)
                    samples = upmReserved.GetList<IDictionary<string, object>>("samples");

                if (samples == null)
                {
                    var jsonPath = ioProxy.PathsCombine(package.resolvedPath, "package.json");
                    if (ioProxy.FileExists(jsonPath))
                    {
                        var packageJson = Json.Deserialize(ioProxy.FileReadAllText(jsonPath)) as Dictionary<string, object>;
                        samples = packageJson.GetList<IDictionary<string, object>>("samples");
                    }
                }

                return samples?.Select(sample =>
                {
                    var displayName = sample.GetString("displayName");
                    var path = sample.GetString("path");
                    var description = sample.GetString("description");
                    var interactiveImport = sample.Get("interactiveImport", false);

                    var resolvedSamplePath = ioProxy.PathsCombine(package.resolvedPath, path);
                    var importPath = ioProxy.PathsCombine(
                        Application.dataPath,
                        "Samples",
                        IOUtils.SanitizeFileName(package.displayName),
                        package.version,
                        string.IsNullOrEmpty(displayName) ? string.Empty : IOUtils.SanitizeFileName(displayName)
                    );
                    return new Sample(ioProxy, assetDatabaseProxy, displayName, description, resolvedSamplePath, importPath, interactiveImport);
                }).ToArray() ?? Enumerable.Empty<Sample>();
            }
            catch (IOException e)
            {
                Debug.Log($"[Package Manager Window] Cannot find samples for package {package.displayName}: {e}");
                return Enumerable.Empty<Sample>();
            }
            catch (InvalidCastException e)
            {
                Debug.Log($"[Package Manager Window] Invalid sample data for package {package.displayName}: {e}");
                return Enumerable.Empty<Sample>();
            }
            catch (Exception)
            {
                return Enumerable.Empty<Sample>();
            }
        }

        /// <summary>
        /// Given a package of a specific version, find a list of samples in that package.
        /// </summary>
        /// <param name="packageName">The name of the package</param>
        /// <param name="packageVersion">The version of the package</param>
        /// <returns>A list of samples in the given package</returns>
        public static IEnumerable<Sample> FindByPackage(string packageName, string packageVersion)
        {
            var upmCache = ServicesContainer.instance.Resolve<UpmCache>();
            if (upmCache.installedPackageInfos.Count() == 0)
                upmCache.SetInstalledPackageInfos(PackageInfo.GetAllRegisteredPackages());

            var package = upmCache.GetInstalledPackageInfo(packageName);
            if (package?.version == packageVersion || string.IsNullOrEmpty(packageVersion))
            {
                var ioProxy = ServicesContainer.instance.Resolve<IOProxy>();
                var assetDatabaseProxy = ServicesContainer.instance.Resolve<AssetDatabaseProxy>();
                return FindByPackage(package, upmCache, ioProxy, assetDatabaseProxy);
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
            try
            {
                var interactive = (options & ImportOptions.HideImportWindow) == ImportOptions.None && interactiveImport;
                var unityPackages = m_IOProxy.DirectoryGetFiles(resolvedPath, "*.unitypackage");
                if (unityPackages.Any())
                    m_AssetDatabase.ImportPackage(unityPackages[0], interactive);
                else
                {
                    var prevImports = previousImports;
                    if (prevImports.Any() && (options & ImportOptions.OverridePreviousImports) == ImportOptions.None)
                        return false;
                    foreach (var v in prevImports)
                    {
                        EditorUtility.DisplayProgressBar(k_CopySamplesFilesTitle, L10n.Tr("Cleaning previous import..."), 0);
                        m_IOProxy.RemovePathAndMeta(v, true);
                    }

                    var sourcePath = resolvedPath;
                    if (!m_IOProxy.DirectoryExists(importPath))
                    {
                        // We create the directory by itself to guarantee that we will be able to ping it after import
                        m_IOProxy.CreateDirectory(importPath);
                        // It's safe to use immediate Refresh here since it's just a folder creation, and we have not imported any assets yet
                        m_AssetDatabase.Refresh();
                    }
                    m_IOProxy.DirectoryCopy(sourcePath, importPath, true,
                        (fileName, progress) =>
                        {
                            var name = fileName.Replace(sourcePath + Path.DirectorySeparatorChar, "");
                            EditorUtility.DisplayProgressBar(k_CopySamplesFilesTitle, name, progress);
                        }
                    );
                    EditorUtility.ClearProgressBar();
                    // According to the ADB team, we are incapable of performing a synchronous refresh when it involves modified scripts and assets
                    // Therefore, we have to schedule a refresh to happen at the end of the frame
                    m_AssetDatabase.ScheduleRefresh();
                }

                return true;
            }
            catch (IOException e)
            {
                Debug.Log($"[Package Manager Window] Cannot import sample {displayName}: {e.Message}");
                return false;
            }
        }

        [NonSerialized]
        private List<string> m_PreviousImports;
        internal List<string> previousImports
        {
            get
            {
                if (m_PreviousImports == null)
                {
                    m_PreviousImports = new List<string>();
                    if (!string.IsNullOrEmpty(importPath))
                    {
                        try
                        {
                            var importDirectory = m_IOProxy.GetParentDirectory(m_IOProxy.GetParentDirectory(importPath));
                            if (m_IOProxy.DirectoryExists(importDirectory))
                            {
                                var versionDirs = m_IOProxy.DirectoryGetDirectories(importDirectory, "*");
                                foreach (var d in versionDirs)
                                {
                                    var p = m_IOProxy.PathsCombine(d, m_IOProxy.GetFileName(importPath));
                                    if (m_IOProxy.DirectoryExists(p))
                                        m_PreviousImports.Add(p);
                                }
                            }
                        }
                        catch (IOException e)
                        {
                            Debug.Log($"[Package Manager Window] Cannot get previous import for sample {displayName}: {e.Message}");
                        }
                    }
                }

                return m_PreviousImports;
            }
        }

        [NonSerialized]
        private string m_Size;
        internal string size
        {
            get
            {
                if (m_Size == null)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(resolvedPath) || !m_IOProxy.DirectoryExists(resolvedPath))
                            return "0 KB";
                        var sizeInBytes = m_IOProxy.DirectorySizeInBytes(resolvedPath);
                        m_Size = UIUtils.ConvertToHumanReadableSize(sizeInBytes);
                    }
                    catch (IOException e)
                    {
                        Debug.Log($"[Package Manager Window] Cannot determine sample {displayName} size: {e.Message}");
                        m_Size = "- KB";
                    }
                }

                return m_Size;
            }
        }
    }
}
