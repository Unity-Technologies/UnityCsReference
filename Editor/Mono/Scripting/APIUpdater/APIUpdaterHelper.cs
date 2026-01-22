// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Runtime.InteropServices;
using Mono.Cecil;
using UnityEditor.PackageManager;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEditor.Utils;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using static System.Environment;

namespace UnityEditor.Scripting
{
    internal static class APIUpdaterHelper
    {
        private static string ComputePlatformExecutableExtension() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty;

        internal static readonly string PlatformExecutableExtensionWithDot = ComputePlatformExecutableExtension();

        public static bool IsInPackage(this string filePath)
        {
            return EditorCompilationInterface.Instance.IsPathInPackageDirectory(filePath);
        }

        public static bool IsInAssetsFolder(this string filePath)
        {
            return filePath.StartsWith("Assets/", StringComparison.InvariantCultureIgnoreCase);
        }

        internal static void HandlePackageFilePaths(string[] filePathsToUpdate)
        {
            var filesInPackages = new List<string>();
            foreach (var path in filePathsToUpdate)
            {
                var absolutePath = Path.GetFullPath(path);
                var virtualPath = FileUtil.GetLogicalPath(absolutePath);
                if (!virtualPath.StartsWith("Packages/"))
                {
                    // Not a packaged path - skip it
                    continue;
                }

                filesInPackages.Add(virtualPath);

                var fileAttributes = File.GetAttributes(absolutePath);
                if ((fileAttributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    File.SetAttributes(absolutePath, fileAttributes & ~FileAttributes.ReadOnly);
            }

            PackageManager.ImmutableAssets.SetAssetsToBeModified(filesInPackages.ToArray());
        }

        internal static bool CheckReadOnlyFiles(string[] destRelativeFilePaths)
        {
            // Verify that all the files we need to copy are now writable
            // Problems after API updating during ScriptCompilation if the files are not-writable
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var readOnlyFiles = destRelativeFilePaths.Where(path => (File.GetAttributes(path) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly);
#pragma warning restore RS0030
#pragma warning disable RS0031 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (readOnlyFiles.Any())
#pragma warning restore RS0031
            {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                Debug.LogErrorFormat(L10n.Tr("[API Updater] Files cannot be updated (files not writable): {0}"), readOnlyFiles.Select(path => path).Aggregate((acc, curr) => acc + Environment.NewLine + "\t" + curr));
#pragma warning restore RS0030
                return false;
            }

            return true;
        }

        internal static bool MakeEditable(IEnumerable<string> files)
        {
            // We're only interested in files that would be under VCS, i.e. project
            // assets or local packages. Incoming paths might use backward slashes; replace with
            // forward ones as that's what Unity/VCS functions operate on.
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var versionedFiles = files.Select(f => f.Replace('\\', '/')).Where(VersionControlUtils.IsPathVersioned).ToArray();
#pragma warning restore RS0030

            // Fail if the asset database GUID can not be found for the input asset path.
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var assetPath = versionedFiles.FirstOrDefault(f => string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(f)));
#pragma warning restore RS0030
            if (assetPath != null)
            {
                Debug.LogErrorFormat(L10n.Tr("[API Updater] Files cannot be updated (failed to add file to list): {0}"), assetPath);
                return false;
            }

            var notEditableFiles = new List<string>();
            if (!AssetDatabase.MakeEditable(versionedFiles, null, notEditableFiles))
            {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var notEditableList = notEditableFiles.Aggregate(string.Empty, (text, file) => text + $"\n\t{file}");
#pragma warning restore RS0030
                Debug.LogErrorFormat(L10n.Tr("[API Updater] Files cannot be updated (failed to check out): {0}"), notEditableList);
                return false;
            }

            return true;
        }
    }
}
