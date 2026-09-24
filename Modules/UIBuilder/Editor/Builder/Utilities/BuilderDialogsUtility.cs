// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal static class BuilderDialogsUtility
    {
        public static bool preventDialogsFromOpening { get; set; }

        private static bool cannotOpenDialogs => Application.isBatchMode || preventDialogsFromOpening;

        // Used for testing
        internal static int CannotOpenDisplayDialogComplexDefaultValue = 0;

        // Used for testing: records the default file name requested for the most recent save-file dialog,
        // captured even when the dialog itself cannot be shown (e.g. in tests), so tests can assert on it
        // without a real dialog ever appearing.
        [NoAutoStaticsCleanup] // Plain string test hook overwritten on every call; holds no managed references, safe to persist across code reload.
        internal static string s_LastSaveFileDialogDefaultName;

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

            return EditorDialog.DisplayDecisionDialog(
                titleText: title,
                messageText: message,
                yesButtonText: ok,
                noButtonText: cancel);
        }

        public static int DisplayDialogComplex(string title, string message, string ok, string cancel, string alt)
        {
            if (cannotOpenDialogs)
                return CannotOpenDisplayDialogComplexDefaultValue;

            var result = EditorDialog.DisplayComplexDecisionDialog(
                titleText: title,
                messageText: message,
                defaultButtonText: ok,
                cancelButtonText: cancel,
                altButtonText: alt);

            switch (result)
            {
                case DialogResult.DefaultAction:
                    return 0;
                case DialogResult.Cancel:
                    return 1;
                case DialogResult.AlternateAction:
                    return 2;
                default:
                    throw new NotImplementedException();
            }
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
            s_LastSaveFileDialogDefaultName = defaultName;

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

            var projectPath = BuilderAssetUtilities.GetPathRelativeToProject(newPath.Trim(), false);

            if (string.IsNullOrWhiteSpace(projectPath))
                DisplayDialog("Saving new document failed", $"Could not save the current document at the requested path ('{newPath}'): the path is outside of the project.");
            return projectPath;
        }
    }
}
