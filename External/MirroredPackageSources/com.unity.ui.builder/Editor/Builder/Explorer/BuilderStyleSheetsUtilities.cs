using System;
using System.IO;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class BuilderStyleSheetsUtilities
    {
        public static void SetActiveUSS(BuilderSelection selection, BuilderPaneWindow paneWindow, StyleSheet styleSheet)
        {
            paneWindow.document.UpdateActiveStyleSheet(selection, styleSheet, null);
        }

        public static void AddUSSToAsset(BuilderPaneWindow paneWindow, string ussPath)
        {
            BuilderAssetUtilities.AddStyleSheetToAsset(paneWindow.document, ussPath);
            paneWindow.OnEnableAfterAllSerialization();
        }

        public static bool CreateNewUSSAsset(BuilderPaneWindow paneWindow)
        {
            string ussPath = s_SaveFileDialogCallback();
            if (string.IsNullOrEmpty(ussPath))
                return false;

            CreateNewUSSAsset(paneWindow, ussPath);

            return true;
        }

        public static void CreateNewUSSAsset(BuilderPaneWindow paneWindow, string ussPath)
        {
            // Create the file. Can be empty.
            File.WriteAllText(ussPath, string.Empty);
            AssetDatabase.Refresh();

            AddUSSToAsset(paneWindow, ussPath);
        }

        public static bool AddExistingUSSToAsset(BuilderPaneWindow paneWindow)
        {
            string ussPath = s_OpenFileDialogCallback();
            if (string.IsNullOrEmpty(ussPath))
                return false;

            AddUSSToAsset(paneWindow, ussPath);
            return true;
        }

        public static void RemoveUSSFromAsset(BuilderPaneWindow paneWindow, int selectedStyleSheetIndex)
        {
            // We need to save all files before we remove the USS.
            // If we don't do this, changes in the removed USS will be lost.
            var shouldContinue = s_CheckForUnsavedChanges(paneWindow);
            if (!shouldContinue)
                return;

            BuilderAssetUtilities.RemoveStyleSheetFromAsset(paneWindow.document, selectedStyleSheetIndex);
            paneWindow.OnEnableAfterAllSerialization();
        }

        // For tests only.

        static string DisplaySaveFileDialogForUSS()
        {
            var path = BuilderDialogsUtility.DisplaySaveFileDialog(
                "Save USS File", null, null, "uss");
            return path;
        }

        static string DisplayOpenFileDialogForUSS()
        {
            var path = BuilderDialogsUtility.DisplayOpenFileDialog(
                "Open USS File", null, "uss");
            return path;
        }

        static bool CheckForUnsavedChanges(BuilderPaneWindow paneWindow)
        {
            return paneWindow.document.CheckForUnsavedChanges();
        }

        internal static Func<string> s_SaveFileDialogCallback = DisplaySaveFileDialogForUSS;
        internal static Func<string> s_OpenFileDialogCallback = DisplayOpenFileDialogForUSS;
        internal static Func<BuilderPaneWindow, bool> s_CheckForUnsavedChanges = CheckForUnsavedChanges;

        internal static void RestoreTestCallbacks()
        {
            s_SaveFileDialogCallback = DisplaySaveFileDialogForUSS;
            s_OpenFileDialogCallback = DisplayOpenFileDialogForUSS;
            s_CheckForUnsavedChanges = CheckForUnsavedChanges;
        }
    }
}
