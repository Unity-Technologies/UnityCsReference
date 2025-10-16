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

        internal static bool IsUserAssembly(string assemblyName)
        {
            return GetPrecompiledAssemblyPaths(PrecompiledAssemblyTypes.UserAssembly).FirstOrDefault(a => a.Contains(assemblyName)) != null;
        }

        internal static bool IsUnityEngineAssembly(string assemblyName)
        {
            return GetPrecompiledAssemblyPaths(PrecompiledAssemblyTypes.UnityEngine).FirstOrDefault(a => a.Contains(assemblyName)) != null;
        }

        internal static bool IsReadOnlyAssembly(string assemblyName)
        {
            var info = GetAssemblyInfoFromAssemblyName(assemblyName);
            return info.IsPackageReadOnly;
        }

        internal static bool IsPackageAssembly(string assemblyName)
        {
            var info = GetAssemblyInfoFromAssemblyName(assemblyName);
            return info.PackageResolvedPath != null ? true : false;
        }

        internal static AssemblyInfo GetAssemblyInfoFromAssemblyPath(string assemblyPath)
        {
            var info = GetAssemblyInfoFromAssemblyName(Path.GetFileNameWithoutExtension(assemblyPath));
            info.Path = assemblyPath;
            return info;
        }

        static AssemblyInfo GetAssemblyInfoFromAssemblyName(string assemblyName)
        {
            // by default let's assume it's not a package
            var assemblyInfo = new AssemblyInfo
            {
                Name = assemblyName,
                RelativePath = "Assets",
                IsPackageReadOnly = false
            };

            if (assemblyInfo.Name.Equals(AssemblyInfo.DefaultAssemblyName))
            {
                assemblyInfo.AsmDefPath = "Built-in";
                return assemblyInfo;
            }

            var asmDefPath = UnityEditor.Compilation.CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assemblyInfo.Name);
            if (asmDefPath != null)
            {
                assemblyInfo.AsmDefPath = asmDefPath;
                var folders = PathUtils.Split(asmDefPath);
                if (folders.Length > 2 && folders[0].Equals(k_VirtualPackagesRoot))
                {
                    assemblyInfo.RelativePath = PathUtils.Combine(folders[0], folders[1]);

                    var info =  UnityEditor.PackageManager.PackageInfo.FindForAssetPath(asmDefPath);
                    if (info != null)
                    {
                        assemblyInfo.IsPackageReadOnly = info.source != PackageSource.Embedded && info.source != PackageSource.Local;
                        assemblyInfo.PackageResolvedPath = PathUtils.ReplaceSeparators(info.resolvedPath);
                    }
                }
                else
                {
                    // non-package user-defined assembly
                    return assemblyInfo;
                }
            }
            else
            {
                // this might happen when loading a report from a different project
                Debug.LogWarningFormat("Assembly Definition cannot be found for " + assemblyInfo.Name);
            }

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
