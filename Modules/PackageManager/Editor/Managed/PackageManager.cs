// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Compilation;
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

        public static PackageInfo GetForAssembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            var fullPath = assembly.Location;
            if (fullPath.StartsWith(Environment.CurrentDirectory + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                // Path is inside the project dir
                var relativePath = fullPath.Substring(Environment.CurrentDirectory.Length + 1).Replace('\\', '/');

                // See if there is an asmdef file for this assembly - use it if so
                var asmdefPath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assembly.GetName().Name);
                if (asmdefPath != null)
                    relativePath = asmdefPath;

                // If we don't have a valid path, or it's inside the Assets folder, it's not part of a package
                if (string.IsNullOrEmpty(relativePath) || relativePath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    return null;

                if (relativePath.StartsWith(Folders.GetPackagesMountPoint() + "/", StringComparison.OrdinalIgnoreCase))
                    return GetForAssetPath(relativePath);
            }

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

