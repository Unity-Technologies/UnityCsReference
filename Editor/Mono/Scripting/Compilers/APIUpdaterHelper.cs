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
using NiceIO;
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

        internal static void HandleFilesInPackagesVirtualFolder(string[] destRelativeFilePaths)
        {
            var filesFromReadOnlyPackages = new List<string>();
            foreach (var path in destRelativeFilePaths.Select(path => path.Replace("\\", "/"))) // package manager paths are always separated by /
            {
                var packageInfo = PackageManager.PackageInfo.FindForAssetPath(path);
                if (packageInfo == null)
                {
                    if (filesFromReadOnlyPackages.Count > 0)
                    {
                        Console.WriteLine(
                            L10n.Tr("[API Updater] At least one file from a readonly package and one file from other location have been updated (that is not expected).{0}File from other location: {0}\t{1}{0}Files from packages already processed: {0}{2}"),
                            Environment.NewLine,
                            path,
                            string.Join($"{Environment.NewLine}\t", filesFromReadOnlyPackages.ToArray()));
                    }

                    continue;
                }

                if (packageInfo.source == PackageSource.BuiltIn)
                {
                    Debug.LogError($"[API Updater] Builtin package '{packageInfo.displayName}' ({packageInfo.version}) files requires updating (Unity version {Application.unityVersion}). This should not happen. Please, report to Unity");
                    return;
                }

                if (packageInfo.source != PackageSource.Local && packageInfo.source != PackageSource.Embedded)
                {
                    // Packman creates a (readonly) cache under Library/PackageCache in a way that even if multiple projects uses the same package each one should have its own
                    // private cache so it is safe for the updater to simply remove the readonly attribute and update the file.
                    filesFromReadOnlyPackages.Add(path);
                }

                // PackageSource.Embedded / PackageSource.Local are considered writtable, so nothing to do, i.e, we can simply overwrite the file contents.
            }

            foreach (var relativeFilePath in filesFromReadOnlyPackages)
            {
                var fileAttributes = File.GetAttributes(relativeFilePath);
                if ((fileAttributes & FileAttributes.ReadOnly) != FileAttributes.ReadOnly)
                    continue;

                File.SetAttributes(relativeFilePath, fileAttributes & ~FileAttributes.ReadOnly);
            }

            PackageManager.ImmutableAssets.SetAssetsAllowedToBeModified(filesFromReadOnlyPackages.ToArray());
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

        internal static bool CheckoutAndValidateVCSFiles(IEnumerable<string> files)
        {
            // We're only interested in files that would be under VCS, i.e. project
            // assets or local packages. Incoming paths might use backward slashes; replace with
            // forward ones as that's what Unity/VCS functions operate on.
            var versionedFiles = files.Select(f => f.Replace('\\', '/')).Where(Provider.PathIsVersioned).ToArray();

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
