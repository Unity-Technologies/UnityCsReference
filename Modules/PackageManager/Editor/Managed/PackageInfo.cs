// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
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
        [NativeName("entitlements")]
        private EntitlementsInfo m_Entitlements = new EntitlementsInfo();

        [SerializeField]
        [NativeName("datePublishedTicks")]
        private long m_DatePublishedTicks;

        [SerializeField]
        [NativeName("git")]
        private GitInfo m_Git = new GitInfo();

        [SerializeField]
        [NativeName("isAssetStorePackage")]
        private bool m_IsAssetStorePackage = false;

        [SerializeField]
        [NativeName("documentationUrl")]
        private string m_DocumentationUrl = "";

        [SerializeField]
        [NativeName("changelogUrl")]
        private string m_ChangelogUrl = "";

        [SerializeField]
        [NativeName("licensesUrl")]
        private string m_LicensesUrl = "";

        [SerializeField]
        [NativeName("hasRepository")]
        private bool m_HasRepository;

        [SerializeField]
        [NativeName("repository")]
        private RepositoryInfo m_Repository = new RepositoryInfo();

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
        internal EntitlementsInfo entitlements { get { return m_Entitlements; } }
        internal bool isAssetStorePackage { get { return m_IsAssetStorePackage;  } }
        public string documentationUrl { get { return m_DocumentationUrl; } }
        public string changelogUrl { get { return m_ChangelogUrl; } }
        public string licensesUrl { get { return m_LicensesUrl; } }

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

        public GitInfo git
        {
            get
            {
                return m_Source  == PackageSource.Git ? m_Git : null;
            }
        }

        public RepositoryInfo repository
        {
            get
            {
                return m_HasRepository ? m_Repository : null;
            }
        }

        public static PackageInfo FindForAssetPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                throw new ArgumentException("Asset path cannot be null or empty.", "assetPath");

            var packageInfo = GetPackageByAssetPath(assetPath);

            // We assume the package is found only if the name field is set.
            // This is because there is no straightforward way to make this nullable in native and,
            // returning extra arguments with the [Out] attribute has expensive unmarshalling costs.
            return string.IsNullOrEmpty(packageInfo.name) ? null : packageInfo;
        }

        public static PackageInfo FindForAssembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            string fullPath = assembly.Location;

            // See if there is an asmdef file for this assembly - use it if so
            var asmdefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(fullPath);
            if (!String.IsNullOrEmpty(asmdefPath))
                return FindForAssetPath(asmdefPath);

            // No asmdef - this is a precompiled DLL.
            // Do a scan through all packages for one that owns the directory in which it is.
            foreach (var package in GetAll())
            {
                if (fullPath.StartsWith(package.resolvedPath + Path.DirectorySeparatorChar))
                    return package;
            }

            return null;
        }

        internal static List<PackageInfo> GetForAssemblyFilePaths(List<string> assemblyPaths)
        {
            // We will first get all the relative asmdef paths from assembly paths
            var pathsToProcess = new HashSet<string>();
            foreach (string assemblyPath in assemblyPaths)
            {
                // See if there is an asmdef file for this assembly - use it if so
                var asmdefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assemblyPath);
                pathsToProcess.Add(String.IsNullOrEmpty(asmdefPath) ? assemblyPath : asmdefPath);
            }

            // We will loop through all the packages and see if they match the relative or absolute paths of asmdefs or assemblies
            List<PackageInfo> matchingPackages = new List<PackageInfo>();
            foreach (var package in GetAll())
            {
                foreach (var path in pathsToProcess)
                {
                    bool found;
                    if (Path.IsPathRooted(path))
                        found = path.StartsWith(package.resolvedPath + Path.DirectorySeparatorChar);
                    else
                        found = path.StartsWith(package.assetPath + '/');

                    if (found)
                    {
                        matchingPackages.Add(package);
                        pathsToProcess.Remove(path);
                        break;
                    }
                }
                if (pathsToProcess.Count == 0)
                    break;
            }
            return matchingPackages;
        }
    }
}
