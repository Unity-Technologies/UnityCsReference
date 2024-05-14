// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Mono.Cecil;
using UnityEditor.PackageManager;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEditor.Utils;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using static System.Environment;

namespace UnityEditor.Scripting.Compilers
{
    internal static class APIUpdaterHelper
    {
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
            var readOnlyFiles = destRelativeFilePaths.Where(path => (File.GetAttributes(path) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly);
            if (readOnlyFiles.Any())
            {
                Debug.LogErrorFormat(L10n.Tr("[API Updater] Files cannot be updated (files not writable): {0}"), readOnlyFiles.Select(path => path).Aggregate((acc, curr) => acc + Environment.NewLine + "\t" + curr));
                return false;
            }

            return true;
        }

        internal static bool MakeEditable(IEnumerable<string> files)
        {
            // We're only interested in files that would be under VCS, i.e. project
            // assets or local packages. Incoming paths might use backward slashes; replace with
            // forward ones as that's what Unity/VCS functions operate on.
            var versionedFiles = files.Select(f => f.Replace('\\', '/')).Where(VersionControlUtils.IsPathVersioned).ToArray();

            // Fail if the asset database GUID can not be found for the input asset path.
            var assetPath = versionedFiles.FirstOrDefault(f => string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(f)));
            if (assetPath != null)
            {
                Debug.LogErrorFormat(L10n.Tr("[API Updater] Files cannot be updated (failed to add file to list): {0}"), assetPath);
                return false;
            }

            var notEditableFiles = new List<string>();
            if (!AssetDatabase.MakeEditable(versionedFiles, null, notEditableFiles))
            {
                var notEditableList = notEditableFiles.Aggregate(string.Empty, (text, file) => text + $"\n\t{file}");
                Debug.LogErrorFormat(L10n.Tr("[API Updater] Files cannot be updated (failed to check out): {0}"), notEditableList);
                return false;
            }

            return true;
        }
    }
}
