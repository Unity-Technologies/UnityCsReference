// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEditor.Compilation;
using Assembly = System.Reflection.Assembly;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.PackageManager
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeAsStruct]
    [NativeType(IntermediateScriptingStructName = "PackageManager_PackageInfo")]
    public sealed partial class PackageInfo
    {
        [SerializeField]
        [NativeName("packageId")]
        private string m_PackageId = "";

        [SerializeField]
        [NativeName("isDirectDependency")]
        private bool m_IsDirectDependency = false;

        [SerializeField]
        [NativeName("version")]
        private string m_Version = "";

        [SerializeField]
        [NativeName("source")]
        private PackageSource m_Source = PackageSource.Unknown;

        [SerializeField]
        [NativeName("resolvedPath")]
        private string m_ResolvedPath = "";

        [SerializeField]
        [NativeName("assetPath")]
        private string m_AssetPath = "";

        [SerializeField]
        [NativeName("name")]
        private string m_Name = "";

        [SerializeField]
        [NativeName("displayName")]
        private string m_DisplayName = "";

        [SerializeField]
        [NativeName("category")]
        private string m_Category = "";

        [SerializeField]
        [NativeName("type")]
        private string m_Type = "";

        [SerializeField]
        [NativeName("description")]
        private string m_Description = "";

        [SerializeField]
        [NativeName("status")]
        private PackageStatus m_Status = PackageStatus.Unknown;

        [SerializeField]
        [NativeName("errors")]
        private Error[] m_Errors = new Error[0];

        [SerializeField]
        [NativeName("versions")]
        private VersionsInfo m_Versions = new VersionsInfo(null, null, null);

        [SerializeField]
        [NativeName("dependencies")]
        private DependencyInfo[] m_Dependencies = new DependencyInfo[0];

        [SerializeField]
        [NativeName("resolvedDependencies")]
        private DependencyInfo[] m_ResolvedDependencies = new DependencyInfo[0];

        [SerializeField]
        [NativeName("keywords")]
        private string[] m_Keywords = new string[0];

        [SerializeField]
        [NativeName("author")]
        private AuthorInfo m_Author = new AuthorInfo();

        [SerializeField]
        [NativeName("hasRegistry")]
        private bool m_HasRegistry;

        [SerializeField]
        [NativeName("registry")]
        private RegistryInfo m_Registry = new RegistryInfo();

        [SerializeField]
        [NativeName("hideInEditor")]
        private bool m_HideInEditor;

        [SerializeField]
        [NativeName("datePublishedTicks")]
        private long m_DatePublishedTicks;

        internal PackageInfo() {}

        public string packageId { get { return m_PackageId;  } }
        public bool isDirectDependency { get { return m_IsDirectDependency;  } }
        public string version { get { return m_Version;  } }
        public PackageSource source { get { return m_Source;  } }
        public string resolvedPath { get { return m_ResolvedPath;  } }
        public string assetPath { get { return m_AssetPath;  } }
        public string name { get { return m_Name;  } }
        public string displayName { get { return m_DisplayName;  } }
        public string category { get { return m_Category;  } }
        public string type { get { return m_Type; } }
        public string description { get { return m_Description;  } }
        public PackageStatus status { get { return m_Status;  } }
        public Error[] errors { get { return m_Errors;  } }
        public VersionsInfo versions { get { return m_Versions; } }
        public DependencyInfo[] dependencies { get { return m_Dependencies; } }
        public DependencyInfo[] resolvedDependencies { get { return m_ResolvedDependencies; } }
        public string[] keywords { get { return m_Keywords;  } }
        public AuthorInfo author { get { return m_Author;  } }
        internal bool hideInEditor { get { return m_HideInEditor;  } }

        public RegistryInfo registry
        {
            get
            {
                return m_HasRegistry ? m_Registry : null;
            }
        }

        public DateTime? datePublished
        {
            get
            {
                return m_DatePublishedTicks == 0 ? (DateTime?)null : new DateTime(m_DatePublishedTicks, DateTimeKind.Utc);
            }
        }

        public static PackageInfo FindForAssetPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                throw new ArgumentException("Asset path cannot be null or empty.", "assetPath");

            var result = new PackageInfo();
            return TryGetForAssetPath(assetPath, result) ? result : null;
        }

        public static PackageInfo FindForAssembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            string fullPath = assembly.Location;
            string relativePath = GetRelativePathForAssemblyFilePath(fullPath);
            if (!String.IsNullOrEmpty(relativePath))
                return FindForAssetPath(relativePath);
            if (relativePath == null)
                return null;

            // Path is outside the project dir - or possibly inside the project dir but local (e.g. in LocalPackages in the test project)
            // Might be in the global package cache, might be a built-in engine module, etc. Do a scan through all packages for one that owns
            // this directory.
            foreach (var package in GetAll())
            {
                if (fullPath.StartsWith(package.resolvedPath + System.IO.Path.DirectorySeparatorChar))
                    return package;
            }

            return null;
        }

        private static string GetPathForPackageAssemblyName(string assemblyPath)
        {
            if (assemblyPath == null)
                throw new ArgumentNullException("assemblyName");

            if (assemblyPath == string.Empty)
                throw new ArgumentException("Assembly path cannot be empty.", "assemblyPath");

            var assemblyName = FileUtil.UnityGetFileNameWithoutExtension(assemblyPath);

            var assets = AssetDatabase.FindAssets("a:packages " + assemblyName);
            foreach (var guid in assets)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    return path;
            }

            return null;
        }

        private static string GetRelativePathForAssemblyFilePath(string fullPath)
        {
            if (fullPath == null)
                throw new ArgumentNullException("fullPath");

            if (fullPath.StartsWith(Environment.CurrentDirectory + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                // Path is inside the project dir
                var relativePath = fullPath.Substring(Environment.CurrentDirectory.Length + 1).Replace('\\', '/');

                // See if there is an asmdef file for this assembly - use it if so
                var asmdefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(fullPath);
                if (asmdefPath != null)
                    relativePath = asmdefPath;

                // See if this is a prebuilt package assembly - use it if so
                var packagePath = GetPathForPackageAssemblyName(relativePath);
                if (packagePath != null)
                    relativePath = packagePath;

                // If we don't have a valid path, or it's inside the Assets folder, it's not part of a package
                if (string.IsNullOrEmpty(relativePath) || relativePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    return null;

                if (relativePath.StartsWith(Folders.GetPackagesPath() + "/", StringComparison.OrdinalIgnoreCase))
                    return relativePath;
            }
            return String.Empty;
        }

        internal static List<PackageInfo> GetForAssemblyFilePaths(List<string> assemblyPaths)
        {
            // We will first get all the relative paths from assembly paths
            Dictionary<string, string> matchingRelativePaths = new Dictionary<string, string>();
            foreach (string assemblyPath in assemblyPaths)
            {
                string relativePath = GetRelativePathForAssemblyFilePath(assemblyPath);
                if (relativePath != null)
                    matchingRelativePaths.Add(assemblyPath, relativePath);
            }

            // We will loop thru all the packages and see if they match the relative paths
            List<PackageInfo> matchingPackages = new List<PackageInfo>();
            foreach (var package in GetAll())
            {
                foreach (var item in matchingRelativePaths)
                {
                    bool found;
                    string relativePath = item.Value;
                    if (!String.IsNullOrEmpty(relativePath))
                        found = (relativePath == package.assetPath || relativePath.StartsWith(package.assetPath + '/'));
                    else
                        found = item.Key.StartsWith(package.resolvedPath + System.IO.Path.DirectorySeparatorChar);

                    if (found)
                    {
                        matchingPackages.Add(package);
                        matchingRelativePaths.Remove(item.Key);
                        break;
                    }
                }
                if (matchingRelativePaths.Count == 0)
                    break;
            }
            return matchingPackages;
        }
    }
}
