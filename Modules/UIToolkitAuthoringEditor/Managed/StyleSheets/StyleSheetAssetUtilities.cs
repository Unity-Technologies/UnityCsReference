// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.UIToolkit.Editor
{
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal static class StyleSheetAssetUtilities
    {
        static readonly Regex k_ValidClassNameRegex = new(@"^[a-zA-Z][a-zA-Z0-9\-_]*$", RegexOptions.Compiled);

        const string k_SpacesError = "Class names cannot contain spaces.";
        const string k_StartsWithNumberError = "Class names cannot start with a number.";
        const string k_InvalidCharactersError = "Class name can only contain letters, numbers, underscores, and dashes.";

        /// <summary>
        /// Creates a new USS file at the specified path and returns whether it was successful.
        /// </summary>
        /// <param name="ussPath">Full project-relative path where the USS file should be created</param>
        /// <returns>True if the file was created successfully, false otherwise</returns>
        public static bool CreateNewUSSFile(string ussPath)
        {
            if (string.IsNullOrEmpty(ussPath))
                return false;

            try
            {
                if (!ussPath.EndsWith(".uss", StringComparison.OrdinalIgnoreCase))
                {
                    ussPath += ".uss";
                }

                File.WriteAllText(ussPath, ":root {\n\n}");
                AssetDatabase.ImportAsset(ussPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to create USS file at {ussPath}: {e.Message}");
                return false;
            }
        }


        /// <summary>
        /// Displays a save file dialog for creating a new USS file.
        /// </summary>
        /// <returns>Project-relative path where the user wants to save the file, or null if cancelled</returns>
        public static string DisplaySaveFileDialogForUSS()
        {
            var directory = Application.dataPath;
            var newPath = EditorUtility.SaveFilePanel( "Save USS File", directory, null, "uss");

            if (string.IsNullOrWhiteSpace(newPath))
                return null;

            var projectPath = GetPathRelativeToProject(newPath.Trim());

            if (string.IsNullOrWhiteSpace(projectPath))
            {
                EditorUtility.DisplayDialog(
                    "Saving new USS failed",
                    $"Could not save the USS file at the requested path ('{newPath}'): the path is outside of the project.",
                    "OK");
                return null;
            }

            return projectPath;
        }

        /// <summary>
        /// Displays an open file dialog for selecting an existing USS file.
        /// </summary>
        /// <returns>Project-relative path to the selected file, or null if cancelled</returns>
        public static string DisplayOpenFileDialogForUSS()
        {
            var directory = Application.dataPath;
            var newPath = EditorUtility.OpenFilePanel("Open USS File", directory, "uss");

            if (string.IsNullOrWhiteSpace(newPath))
                return null;

            // Convert to project-relative path
            var projectPath = GetPathRelativeToProject(newPath.Trim());

            if (string.IsNullOrWhiteSpace(projectPath))
            {
                EditorUtility.DisplayDialog(
                    "Opening USS failed",
                    $"Could not open the USS file at the requested path ('{newPath}'): the path is outside of the project.",
                    "OK");
                return null;
            }

            return projectPath;
        }

        static string GetPathRelativeToProject(string path)
        {
            var fullPath = Path.GetFullPath(path).Replace("\\", "/");
            var projectPath = Path.GetFullPath(Application.dataPath).Replace("\\", "/");
            projectPath = projectPath.Substring(0, projectPath.Length - "/Assets".Length);

            var assetsPath = projectPath + "/Assets";
            var packagesPath = projectPath + "/Packages";

            if (fullPath.StartsWith(assetsPath, System.StringComparison.InvariantCultureIgnoreCase))
                return fullPath.Substring(projectPath.Length + 1); // Remove leading "/"

            if (fullPath.StartsWith(packagesPath, System.StringComparison.InvariantCultureIgnoreCase))
                return fullPath.Substring(projectPath.Length + 1); // Remove leading "/"

            return null;
        }

        public static string GetClassNameValidationError(string className)
        {
            if (string.IsNullOrEmpty(className))
                throw new ArgumentNullException(nameof(className), "Class name must not be null");

            if (!k_ValidClassNameRegex.IsMatch(className))
                return char.IsNumber(className[0])
                    ? k_StartsWithNumberError
                    : k_InvalidCharactersError;

            return className.Contains(" ") ? k_SpacesError : string.Empty;
        }
    }
}
