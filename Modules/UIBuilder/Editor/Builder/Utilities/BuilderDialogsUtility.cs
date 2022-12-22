// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
                directory = Application.dataPath;

            var newPath = EditorUtility.OpenFilePanel(
                title,
                directory,
                extension);

            if (string.IsNullOrEmpty(newPath?.Trim()))
                return null;

            var appPathLength = Application.dataPath.Length - 6; // - "Assets".Length
            newPath = newPath.Substring(appPathLength);

            return newPath;
        }

        public static string DisplaySaveFileDialog(string title, string directory, string defaultName, string extension)
        {
            if (cannotOpenDialogs)
                return null;

            if (string.IsNullOrEmpty(directory))
                directory = Application.dataPath;

            var newPath = EditorUtility.SaveFilePanel(
                title,
                directory,
                defaultName,
                extension);

            if (string.IsNullOrEmpty(newPath?.Trim()))
                return null;

            var appPathLength = Application.dataPath.Length - 6; // - "Assets".Length
            newPath = newPath.Substring(appPathLength);

            return newPath;
        }
    }
}
