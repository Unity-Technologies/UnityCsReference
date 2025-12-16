// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditorInternal;

namespace Unity.ProjectAuditor.Editor.AssemblyUtils
{
    // PrecompiledAssemblyType is a 1:1 match to PrecompiledAssemblySources (https://docs.unity3d.com/2019.4/Documentation/ScriptReference/Compilation.CompilationPipeline.PrecompiledAssemblySources.html)
    [Flags]
    enum PrecompiledAssemblyTypes
    {
        /// <summary>
        ///   <para>Matches precompiled assemblies present in the project and packages.</para>
        /// </summary>
        UserAssembly = 1,
        /// <summary>
        ///   <para>Matches UnityEngine and runtime module assemblies.</para>
        /// </summary>
        UnityEngine = 2,
        /// <summary>
        ///   <para>Matches UnityEditor and editor module assemblies.</para>
        /// </summary>
        UnityEditor = 4,
        /// <summary>
        ///   <para>Matches assemblies supplied by the target framework.</para>
        /// </summary>
        SystemAssembly = 8,
        /// <summary>
        ///   <para>Matches all assembly sources.</para>
        /// </summary>
        All = -1 // 0xFFFFFFFF
    }

    static class AssemblyInfoProvider
    {
        const string k_VirtualPackagesRoot = "Packages";

        [Serializable]
        private class AsmdefData
        {
#pragma warning disable 0649
            public string name;
            public string[] optionalUnityReferences;
            public string[] includePlatforms;
            public string[] excludePlatforms;
#pragma warning restore 0649
        }

        internal static IEnumerable<string> GetPrecompiledAssemblyPaths(PrecompiledAssemblyTypes flags)
        {
            var assemblyPaths = new List<string>();
            var precompiledAssemblySources = (UnityEditor.Compilation.CompilationPipeline.PrecompiledAssemblySources)flags;
            assemblyPaths.AddRange(UnityEditor.Compilation.CompilationPipeline.GetPrecompiledAssemblyPaths(precompiledAssemblySources));

            return assemblyPaths.Select(PathUtils.ReplaceSeparators);
        }

        internal static IEnumerable<string> GetPrecompiledAssemblyDirectories(PrecompiledAssemblyTypes flags)
        {
            foreach (var dir in GetPrecompiledAssemblyPaths(flags).Select(Path.GetDirectoryName).Distinct())
                yield return dir;
        }

        internal static bool FilterAssembly(string assemblyName, bool allowPackages, bool allowUnityCode, bool allowUserCode)
        {
            var info = GetAssemblyInfoFromAssemblyName(assemblyName, null, false);

            if (!allowUserCode)
            {
                if (info.Name == AssemblyInfo.DefaultAssemblyName)
                    return true;
                if (info.Name == AssemblyInfo.DefaultEditorAssemblyName)
                    return true;
            }

            if (info.PackageResolvedPath != null)
            {
                if (!allowPackages)
                    return true;

                if (info.IsUnityOwned)
                {
                    if (allowUnityCode)
                        return false;
                }
                else if (allowUserCode)
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        internal static AssemblyInfo GetAssemblyInfoFromAssemblyPath(string assemblyPath, bool? editorAssembly)
        {
            var info = GetAssemblyInfoFromAssemblyName(Path.GetFileNameWithoutExtension(assemblyPath), editorAssembly);
            info.Path = assemblyPath;
            return info;
        }

        internal static AssemblyInfo GetAssemblyInfoFromAssemblyName(string assemblyName, bool? editorAssembly, bool reportErrors = true)
        {
            // by default let's assume it's not a package
            var assemblyInfo = new AssemblyInfo
            {
                Name = assemblyName,
                RelativePath = "Assets",
                IsReadOnly = false,
                IsEditorAssembly = editorAssembly
            };

            if (assemblyInfo.Name.Equals(AssemblyInfo.DefaultAssemblyName) || assemblyInfo.Name.Equals(AssemblyInfo.DefaultEditorAssemblyName))
            {
                assemblyInfo.AsmDefPath = "Default";
                return assemblyInfo;
            }

            var asmDefPath = UnityEditor.Compilation.CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assemblyInfo.Name);
            if (asmDefPath != null)
            {
                try
                {
                    string fileContent = File.ReadAllText(asmDefPath);
                    var data = JsonUtility.FromJson<AsmdefData>(fileContent);

                    if (data.optionalUnityReferences != null)
                    {
                        if (data.optionalUnityReferences.Contains("TestAssemblies"))
                            assemblyInfo.IsTestAssembly = true;

                        if (editorAssembly == null)
                        {
                            bool targetsEditorPlatform = (data.includePlatforms == null || data.includePlatforms.Length == 0) || (data.includePlatforms != null && data.includePlatforms.Contains("Editor"));
                            bool excludesEditorPlatform = data.excludePlatforms != null && data.excludePlatforms.Contains("Editor");
                            assemblyInfo.IsEditorAssembly = targetsEditorPlatform && !excludesEditorPlatform;
                        }
                    }
                }
                catch (Exception)
                {
                }

                assemblyInfo.AsmDefPath = asmDefPath;
                var folders = PathUtils.Split(asmDefPath);
                if (folders.Length > 2 && folders[0].Equals(k_VirtualPackagesRoot))
                {
                    assemblyInfo.RelativePath = PathUtils.Combine(folders[0], folders[1]);

                    var info = PackageInfo.FindForAssetPath(asmDefPath);
                    if (info != null)
                    {
                        assemblyInfo.IsReadOnly = info.source != PackageSource.Embedded && info.source != PackageSource.Local;
                        assemblyInfo.PackageResolvedPath = PathUtils.ReplaceSeparators(info.resolvedPath);
                    }
                }

                if (assemblyInfo.PackageResolvedPath != null)
                    assemblyInfo.IsUnityOwned = assemblyInfo.PackageResolvedPath.Contains("com.unity.", StringComparison.Ordinal) || asmDefPath.Contains("com.unity.", StringComparison.Ordinal);
            }
            else
            {
                // this might happen when loading a report from a different project, or when looking for asmdefs for Unity's internal assemblies
                if (reportErrors && assemblyInfo.Name != AssemblyInfo.DefaultEditorAssemblyName)
                    Debug.LogWarningFormat("Assembly Definition cannot be found for " + assemblyInfo.Name);
            }

            return assemblyInfo;
        }

        internal static AssemblyInfo GetAssemblyInfoFromUnityAssemblyPath(string assemblyPath, bool editorAssembly)
        {
            var assemblyInfo = new AssemblyInfo
            {
                Name = Path.GetFileNameWithoutExtension(assemblyPath),
                IsReadOnly = true,
                IsEditorAssembly = editorAssembly,
                IsUnityInternalAssembly = true,
                IsUnityOwned = true
            };

            assemblyInfo.Path = assemblyPath;

            string exePath = UnityEditor.EditorApplication.applicationPath;
            assemblyInfo.RelativePath = Path.GetRelativePath(Path.GetDirectoryName(exePath), assemblyInfo.Path);

            return assemblyInfo;
        }

        internal static string ResolveAssetPath(AssemblyInfo assemblyInfo, string path)
        {
            var fullPath = PathUtils.GetFullPath(path);
            // if it's a package, resolve from absolute+physical to logical+relative path
            if (!string.IsNullOrEmpty(assemblyInfo.PackageResolvedPath))
                return fullPath.Replace(assemblyInfo.PackageResolvedPath, assemblyInfo.RelativePath);

            // if it lives in Assets/... convert to relative path
            return fullPath.Replace(ProjectAuditor.ProjectPath + PathUtils.Separator, "");
        }
    }
}
