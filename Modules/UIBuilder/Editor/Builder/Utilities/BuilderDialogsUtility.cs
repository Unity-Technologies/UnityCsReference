// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal static class BuilderDialogsUtility
    {
        public static bool preventDialogsFromOpening { get; set; }

        private static bool cannotOpenDialogs => Application.isBatchMode || preventDialogsFromOpening;

        public static bool DisplayDialog(string title, string message)
        {
            return DisplayDialog(title, message, BuilderConstants.DialogOkOption);
        }

        public static bool DisplayDialog(string title, string message, string ok)
        {
            return DisplayDialog(title, message, ok, string.Empty);
        }

        public static bool DisplayDialog(string title, string message, string ok, string cancel)
        {
            if (cannotOpenDialogs)
                return true;

            return EditorUtility.DisplayDialog(title, message, ok, cancel);
        }

        public static int DisplayDialogComplex(string title, string message, string ok, string cancel, string alt)
        {
            if (cannotOpenDialogs)
                return 0;

            return EditorUtility.DisplayDialogComplex(title, message, ok, cancel, alt);
        }

        public static string DisplayOpenFileDialog(string title, string directory, string extension)
        {
            if (cannotOpenDialogs)
                return null;

            if (string.IsNullOrEmpty(directory))
                directory = BuilderAssetUtilities.assetsPath;

            var newPath = EditorUtility.OpenFilePanel(
                title,
                directory,
                extension);

            if (string.IsNullOrWhiteSpace(newPath))
                return null;

            
            var projectPath = BuilderAssetUtilities.GetPathRelativeToProject(newPath.Trim());
            if (string.IsNullOrWhiteSpace(projectPath))
                DisplayDialog("Opening document failed", $"Could not open the document at the requested path ('{newPath}'): the path is outside of the project.");
            return projectPath;
        }

        public static string DisplaySaveFileDialog(string title, string directory, string defaultName, string extension)
        {
            if (cannotOpenDialogs)
                return null;

            if (string.IsNullOrEmpty(directory))
                directory = BuilderAssetUtilities.assetsPath;

            var newPath = EditorUtility.SaveFilePanel(
                title,
                directory,
                defaultName,
                extension);

            if (string.IsNullOrWhiteSpace(newPath))
                return null;

            var projectPath = BuilderAssetUtilities.GetPathRelativeToProject(newPath.Trim());

            if (string.IsNullOrWhiteSpace(projectPath))
                DisplayDialog("Saving new document failed", $"Could not save the current document at the requested path ('{newPath}'): the path is outside of the project.");
            return projectPath;
        }
    }
}
