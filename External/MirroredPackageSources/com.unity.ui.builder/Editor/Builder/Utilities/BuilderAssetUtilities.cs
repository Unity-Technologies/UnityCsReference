using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    internal static class BuilderAssetUtilities
    {
        public static string GetResourcesPathForAsset(Object asset)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            return GetResourcesPathForAsset(assetPath);
        }

        public static string GetResourcesPathForAsset(string assetPath)
        {
            var resourcesFolder = "Resources/";
            if (string.IsNullOrEmpty(assetPath) || !assetPath.Contains(resourcesFolder))
                return null;

            var lastResourcesSubstring = assetPath.LastIndexOf(resourcesFolder) + resourcesFolder.Length;
            assetPath = assetPath.Substring(lastResourcesSubstring);
            var lastExtDot = assetPath.LastIndexOf(".");

            if (lastExtDot == -1)
            {
                return null;
            }

            assetPath = assetPath.Substring(0, lastExtDot);

            return assetPath;
        }

        public static bool IsBuiltinPath(string assetPath)
        {
            return assetPath == "Resources/unity_builtin_extra";
        }

        public static void AddStyleSheetToAsset(
            BuilderDocument document, string ussPath)
        {
            var styleSheet = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(ussPath);
            if (styleSheet == null)
            {
                if (ussPath.StartsWith("Packages/"))
                    BuilderDialogsUtility.DisplayDialog("Invalid Asset Type", $"Asset at path {ussPath} is not a StyleSheet.\nNote, for assets inside Packages folder, the folder name for the package needs to match the actual official package name (ie. com.example instead of Example).");
                else
                    BuilderDialogsUtility.DisplayDialog("Invalid Asset Type", $"Asset at path {ussPath} is not a StyleSheet.");
                return;
            }

            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, "Add StyleSheet to UXML");

            document.AddStyleSheetToDocument(styleSheet, ussPath);
        }

        public static void RemoveStyleSheetFromAsset(
            BuilderDocument document, int ussIndex)
        {
            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, "Remove StyleSheet from UXML");

            document.RemoveStyleSheetFromDocument(ussIndex);
        }

        public static void RemoveStyleSheetsFromAsset(
            BuilderDocument document, int[] indexes)
        {
            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, "Remove StyleSheets from UXML");

            foreach (var index in indexes)
            {
                document.RemoveStyleSheetFromDocument(index);
            }
        }

        public static void ReorderStyleSheetsInAsset(
            BuilderDocument document, VisualElement styleSheetsContainerElement)
        {
            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, "Reorder StyleSheets in UXML");

            var reorderedUSSList = new List<StyleSheet>();
            foreach (var ussElement in styleSheetsContainerElement.Children())
                reorderedUSSList.Add(ussElement.GetStyleSheet());

            var openUXMLFile = document.activeOpenUXMLFile;
            openUXMLFile.openUSSFiles.Sort((left, right) =>
            {
                var leftOrder = reorderedUSSList.IndexOf(left.styleSheet);
                var rightOrder = reorderedUSSList.IndexOf(right.styleSheet);
                return leftOrder.CompareTo(rightOrder);
            });
        }

        public static VisualElementAsset AddElementToAsset(
            BuilderDocument document, VisualElement ve, int index = -1)
        {
            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, BuilderConstants.CreateUIElementUndoMessage);

            var veParent = ve.parent;
            VisualElementAsset veaParent = null;

            /* If the current parent element is linked to a VisualTreeAsset, it could mean
             that our parent is the TemplateContainer belonging to our parent document and the
             current open document is a sub-document opened in-place. In such a case, we don't
             want to use our parent's VisualElementAsset, as that belongs to our parent document.
             So instead, we just use no parent, indicating that we are adding this new element
             to the root of our document.*/
            if (veParent != null && veParent.GetVisualTreeAsset() != document.visualTreeAsset)
                veaParent = veParent.GetVisualElementAsset();

            if (veaParent == null)
                veaParent = document.visualTreeAsset.GetRootUXMLElement(); // UXML Root Element

            var vea = document.visualTreeAsset.AddElement(veaParent, ve);

            if (index >= 0)
                document.visualTreeAsset.ReparentElement(vea, veaParent, index);

            return vea;
        }

        public static VisualElementAsset AddElementToAsset(
            BuilderDocument document, VisualElement ve,
            Func<VisualTreeAsset, VisualElementAsset, VisualElement, VisualElementAsset> makeVisualElementAsset,
            int index = -1)
        {
            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, BuilderConstants.CreateUIElementUndoMessage);

            var veParent = ve.parent;
            VisualElementAsset veaParent = null;

            /* If the current parent element is linked to a VisualTreeAsset, it could mean
             that our parent is the TemplateContainer belonging to our parent document and the
             current open document is a sub-document opened in-place. In such a case, we don't
             want to use our parent's VisualElementAsset, as that belongs to our parent document.
             So instead, we just use no parent, indicating that we are adding this new element
             to the root of our document.*/
            if (veParent != null && veParent.GetVisualTreeAsset() != document.visualTreeAsset)
                veaParent = veParent.GetVisualElementAsset();

            if (veaParent == null)
                veaParent = document.visualTreeAsset.GetRootUXMLElement(); // UXML Root Element

            var vea = makeVisualElementAsset(document.visualTreeAsset, veaParent, ve);
            ve.SetProperty(BuilderConstants.ElementLinkedVisualElementAssetVEPropertyName, vea);
            ve.SetProperty(BuilderConstants.ElementLinkedBelongingVisualTreeAssetVEPropertyName, document.visualTreeAsset);

            if (index >= 0)
                document.visualTreeAsset.ReparentElement(vea, veaParent, index);

            return vea;
        }

        public static void SortElementsByTheirVisualElementInAsset(VisualElement parentVE)
        {
            var parentVEA = parentVE.GetVisualElementAsset();
            if (parentVEA == null)
                return;

            if (parentVE.childCount <= 1)
                return;

            var correctOrderForElementAssets = new List<VisualElementAsset>();
            var correctOrdersInDocument = new List<int>();
            foreach (var ve in parentVE.Children())
            {
                var vea = ve.GetVisualElementAsset();
                if (vea == null)
                    continue;

                correctOrderForElementAssets.Add(vea);
                correctOrdersInDocument.Add(vea.orderInDocument);
            }

            if (correctOrderForElementAssets.Count <= 1)
                return;

            correctOrdersInDocument.Sort();

            for (int i = 0; i < correctOrderForElementAssets.Count; ++i)
                correctOrderForElementAssets[i].orderInDocument = correctOrdersInDocument[i];
        }

        public static void ReparentElementInAsset(
            BuilderDocument document, VisualElement veToReparent, VisualElement newParent, int index = -1, bool undo = true)
        {
            var veaToReparent = veToReparent.GetVisualElementAsset();
            if (veaToReparent == null)
                return;

            if (undo)
                Undo.RegisterCompleteObjectUndo(
                    document.visualTreeAsset, BuilderConstants.ReparentUIElementUndoMessage);

            VisualElementAsset veaNewParent = null;
            if (newParent != null)
                veaNewParent = newParent.GetVisualElementAsset();

            if (veaNewParent == null)
                veaNewParent = document.visualTreeAsset.GetRootUXMLElement(); // UXML Root Element

            document.visualTreeAsset.ReparentElement(veaToReparent, veaNewParent, index);
        }

        public static void DeleteElementFromAsset(BuilderDocument document, VisualElement ve)
        {
            var vea = ve.GetVisualElementAsset();
            if (vea == null)
                return;

            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, BuilderConstants.DeleteUIElementUndoMessage);

            document.visualTreeAsset.RemoveElement(vea);
        }

        public static void TransferAssetToAsset(
            BuilderDocument document, VisualElementAsset parent, VisualTreeAsset otherVta)
        {
            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, BuilderConstants.CreateUIElementUndoMessage);

            document.visualTreeAsset.Swallow(parent, otherVta);
        }

        public static void TransferAssetToAsset(
            BuilderDocument document, StyleSheet styleSheet, StyleSheet otherStyleSheet)
        {
            Undo.RegisterCompleteObjectUndo(
                styleSheet, BuilderConstants.AddNewSelectorUndoMessage);

            styleSheet.Swallow(otherStyleSheet);
        }

        public static void AddStyleClassToElementInAsset(BuilderDocument document, VisualElement ve, string className)
        {
            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, BuilderConstants.AddStyleClassUndoMessage);

            var vea = ve.GetVisualElementAsset();
            vea.AddStyleClass(className);
        }

        public static void RemoveStyleClassFromElementInAsset(BuilderDocument document, VisualElement ve, string className)
        {
            Undo.RegisterCompleteObjectUndo(
                document.visualTreeAsset, BuilderConstants.RemoveStyleClassUndoMessage);

            var vea = ve.GetVisualElementAsset();
            vea.RemoveStyleClass(className);
        }

        public static void AddStyleComplexSelectorToSelection(StyleSheet styleSheet, StyleComplexSelector scs)
        {
            var selectionProp = styleSheet.AddProperty(
                scs,
                BuilderConstants.SelectedStyleRulePropertyName,
                BuilderConstants.ChangeSelectionUndoMessage);

            // Need to add at least one dummy value because lots of code will die
            // if it encounters a style property with no values.
            styleSheet.AddValue(
                selectionProp, 42.0f, BuilderConstants.ChangeSelectionUndoMessage);
        }

        public static void AddElementToSelectionInAsset(BuilderDocument document, VisualElement ve)
        {
            if (BuilderSharedStyles.IsStyleSheetElement(ve))
            {
                var styleSheet = ve.GetStyleSheet();
                styleSheet.AddSelector(
                    BuilderConstants.SelectedStyleSheetSelectorName,
                    BuilderConstants.ChangeSelectionUndoMessage);
            }
            else if (BuilderSharedStyles.IsSelectorElement(ve))
            {
                var styleSheet = ve.GetClosestStyleSheet();
                var scs = ve.GetStyleComplexSelector();
                AddStyleComplexSelectorToSelection(styleSheet, scs);
            }
            else if (BuilderSharedStyles.IsDocumentElement(ve))
            {
                Undo.RegisterCompleteObjectUndo(
                    document.visualTreeAsset, BuilderConstants.ChangeSelectionUndoMessage);

                var vta = ve.GetVisualTreeAsset();
                var vtaRoot = vta.GetRootUXMLElement();
                vta.AddElement(vtaRoot, BuilderConstants.SelectedVisualTreeAssetSpecialElementTypeName);
            }
            else if (ve.GetVisualElementAsset() != null)
            {
                Undo.RegisterCompleteObjectUndo(
                    document.visualTreeAsset, BuilderConstants.ChangeSelectionUndoMessage);

                var vea = ve.GetVisualElementAsset();
                vea.Select();
            }
        }

        public static void RemoveElementFromSelectionInAsset(BuilderDocument document, VisualElement ve)
        {
            if (BuilderSharedStyles.IsStyleSheetElement(ve))
            {
                var styleSheet = ve.GetStyleSheet();
                styleSheet.RemoveSelector(
                    BuilderConstants.SelectedStyleSheetSelectorName,
                    BuilderConstants.ChangeSelectionUndoMessage);
            }
            else if (BuilderSharedStyles.IsSelectorElement(ve))
            {
                var styleSheet = ve.GetClosestStyleSheet();
                var scs = ve.GetStyleComplexSelector();
                styleSheet.RemoveProperty(
                    scs,
                    BuilderConstants.SelectedStyleRulePropertyName,
                    BuilderConstants.ChangeSelectionUndoMessage);
            }
            else if (BuilderSharedStyles.IsDocumentElement(ve))
            {
                Undo.RegisterCompleteObjectUndo(
                    document.visualTreeAsset, BuilderConstants.ChangeSelectionUndoMessage);

                var vta = ve.GetVisualTreeAsset();
                var selectedElement = vta.FindElementByType(BuilderConstants.SelectedVisualTreeAssetSpecialElementTypeName);
                vta.RemoveElement(selectedElement);
            }
            else if (ve.GetVisualElementAsset() != null)
            {
                Undo.RegisterCompleteObjectUndo(
                    document.visualTreeAsset, BuilderConstants.ChangeSelectionUndoMessage);

                var vea = ve.GetVisualElementAsset();
                vea.Deselect();
            }
        }

        public static string GetVisualTreeAssetAssetName(VisualTreeAsset visualTreeAsset, bool hasUnsavedChanges) =>
            GetAssetName(visualTreeAsset, BuilderConstants.UxmlExtension, hasUnsavedChanges);

        public static string GetStyleSheetAssetName(StyleSheet styleSheet, bool hasUnsavedChanges) =>
            GetAssetName(styleSheet, BuilderConstants.UssExtension, hasUnsavedChanges);

        public static string GetAssetName(ScriptableObject asset, string extension, bool hasUnsavedChanges)
        {
            if (asset == null)
            {
                if (extension == BuilderConstants.UxmlExtension)
                    return BuilderConstants.ToolbarUnsavedFileDisplayMessage + extension;
                else
                    return string.Empty;
            }

            var assetPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(assetPath))
                return BuilderConstants.ToolbarUnsavedFileDisplayMessage + extension;

            return Path.GetFileName(assetPath) + (hasUnsavedChanges ? BuilderConstants.ToolbarUnsavedFileSuffix : "");
        }
    }
}
