// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
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

        public static bool AddUSSToAsset(BuilderPaneWindow paneWindow, string ussPath)
        {
            bool added = BuilderAssetUtilities.AddStyleSheetToAsset(paneWindow.document, ussPath);

            if (added)
            {
                paneWindow.OnEnableAfterAllSerialization();
            }

            return added;
        }

        public static bool CreateNewUSSAsset(BuilderPaneWindow paneWindow)
        {
            string ussPath = s_SaveFileDialogCallback();
            if (string.IsNullOrEmpty(ussPath))
                return false;

            return CreateNewUSSAsset(paneWindow, ussPath);
        }

        public static bool CreateNewUSSAsset(BuilderPaneWindow paneWindow, string ussPath)
        {
            // Create the file.
            File.WriteAllText(ussPath, ":root {\n\n}");
            AssetDatabase.Refresh();

            return AddUSSToAsset(paneWindow, ussPath);
        }

        public static bool AddExistingUSSToAsset(BuilderPaneWindow paneWindow)
        {
            string ussPath = s_OpenFileDialogCallback();
            if (string.IsNullOrEmpty(ussPath))
                return false;

            return AddUSSToAsset(paneWindow, ussPath);
        }

        public static void RemoveUSSFromAsset(BuilderPaneWindow paneWindow, BuilderSelection selection, VisualElement clickedElement)
        {
            // We need to save all files before we remove the USS references.
            // If we don't do this, changes in the removed USS will be lost.
            var shouldContinue = s_CheckForUnsavedChanges(paneWindow);
            if (!shouldContinue)
                return;

            var selectedElements = selection.selection;

            if (!selectedElements.Contains(clickedElement))
            {
                // Removed just clicked element
                var clickedStyleSheetIndex = (int)clickedElement.GetProperty(BuilderConstants.ElementLinkedStyleSheetIndexVEPropertyName);
                BuilderAssetUtilities.RemoveStyleSheetFromAsset(paneWindow.document, clickedStyleSheetIndex);
            }
            else
            {
                // Removed selected elements
                var styleSheetIndexes = selectedElements.Where(x => BuilderSharedStyles.IsStyleSheetElement(x) &&
                    string.IsNullOrEmpty(x.GetProperty(BuilderConstants.ExplorerItemLinkedUXMLFileName) as string))
                    .Select(x => (int)x.GetProperty(BuilderConstants.ElementLinkedStyleSheetIndexVEPropertyName))
                    .OrderByDescending(x => x)
                    .ToArray();

                BuilderAssetUtilities.RemoveStyleSheetsFromAsset(paneWindow.document, styleSheetIndexes);
            }

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

        public static string GetClassNameValidationError(string className)
        {
            if (className.Contains(" "))
            {
                return BuilderConstants.AddStyleClassValidationSpaces;
            }

            if (!BuilderNameUtilities.attributeRegex.IsMatch(className))
            {
                return BuilderConstants.ClassNameValidationSpacialCharacters;
            }

            return string.Empty;
        }
    }
}
