// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Compilation;
using System.Collections.Generic;
using Assembly = System.Reflection.Assembly;

namespace UnityEditor.PackageManager
{
    internal partial class Packages
    {
        public static PackageInfo GetForAssetPath(string assetPath)
        {
            if (assetPath == null)
                throw new ArgumentNullException("assetPath");

            if (assetPath == string.Empty)
                throw new ArgumentException("Asset path cannot be empty.", "assetPath");

            foreach (var package in Packages.GetAll())
            {
                if (assetPath == package.assetPath || assetPath.StartsWith(package.assetPath + '/'))
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

                if (relativePath.StartsWith(Folders.GetPackagesMountPoint() + "/", StringComparison.OrdinalIgnoreCase))
                    return relativePath;
            }
            return String.Empty;
        }

        public static List<PackageInfo> GetForAssemblyFilePaths(List<string> assemblyPaths)
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

        public static PackageInfo GetForAssembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            string fullPath = assembly.Location;
            string relativePath = GetRelativePathForAssemblyFilePath(fullPath);
            if (!String.IsNullOrEmpty(relativePath))
                return GetForAssetPath(relativePath);
            else if (relativePath == null)
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
    }
}
