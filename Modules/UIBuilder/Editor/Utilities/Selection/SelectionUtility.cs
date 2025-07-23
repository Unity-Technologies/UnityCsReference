// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class SelectionUtility
    {
        private static bool IsSelected(VisualTreeAsset vta)
        {
            var foundElement = vta.FindElementByType(BuilderConstants.SelectedVisualTreeAssetSpecialElementTypeName);
            return foundElement != null;
        }

        public static void AddToSelection(VisualTreeAsset vta)
        {
            var vtaRoot = vta.visualTree;
            var vea = vta.AddElementOfType(vtaRoot, BuilderConstants.SelectedVisualTreeAssetSpecialElementTypeName);
            // We don't want this element to be cloned.
            vea.skipClone = true;
        }

        public static void RemoveFromSelection(VisualTreeAsset vta)
        {
            var selectedElement = vta.FindElementByType(BuilderConstants.SelectedVisualTreeAssetSpecialElementTypeName);
            if (selectedElement != null)
                selectedElement.RemoveFromHierarchy();
        }

        private static bool IsSelected(VisualElementAsset vea)
        {
            var value = vea.GetAttributeValue(BuilderConstants.SelectedVisualElementAssetAttributeName);
            return value == BuilderConstants.SelectedVisualElementAssetAttributeValue;
        }

        public static void AddToSelection(VisualElementAsset vea)
        {
            vea.SetAttribute(
                BuilderConstants.SelectedVisualElementAssetAttributeName,
                BuilderConstants.SelectedVisualElementAssetAttributeValue);
        }

        public static void RemoveFromSelection(VisualElementAsset vea)
        {
            vea.SetAttribute(
                BuilderConstants.SelectedVisualElementAssetAttributeName,
                string.Empty);
        }

        private static bool IsSelected(StyleSheet styleSheet)
        {
            var selector = styleSheet.FindSelector(BuilderConstants.SelectedStyleSheetSelectorName);
            return selector != null;
        }

        public static void AddToSelection(StyleSheet styleSheet)
        {
            styleSheet.AddSelector(
                BuilderConstants.SelectedStyleSheetSelectorName,
                BuilderConstants.ChangeSelectionUndoMessage);
        }

        public static void RemoveFromSelection(StyleSheet styleSheet)
        {
            styleSheet.RemoveSelector(
                BuilderConstants.SelectedStyleSheetSelectorName,
                BuilderConstants.ChangeSelectionUndoMessage);
        }

        private static bool IsSelected(StyleComplexSelector scs)
        {
            var selectionProperty = scs.rule?.FindLastProperty(BuilderConstants.SelectedStyleRulePropertyName);
            return selectionProperty != null;
        }

        public static void AddToSelection(StyleSheet styleSheet, StyleComplexSelector scs)
        {
            var selectionProp = styleSheet.AddProperty(
                scs.rule,
                BuilderConstants.SelectedStyleRulePropertyName,
                BuilderConstants.ChangeSelectionUndoMessage);

            // Need to add at least one dummy value because lots of code will die
            // if it encounters a style property with no values.
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeSelectionUndoMessage);
            selectionProp.SetFloat(styleSheet, 42.0f);
        }

        public static void RemoveFromSelection(StyleSheet styleSheet, StyleComplexSelector scs)
        {
            styleSheet.RemoveProperty(
                scs.rule,
                BuilderConstants.SelectedStyleRulePropertyName,
                BuilderConstants.ChangeSelectionUndoMessage);
        }

        public static bool IsSelected(VisualElement element)
        {
            var vta = element.GetVisualTreeAsset();
            if (vta != null)
                return IsSelected(vta);

            var vea = element.GetVisualElementAsset();
            if (vea != null)
                return IsSelected(vea);

            var veaInTemplate = element.GetVisualElementAssetInTemplate();
            if (veaInTemplate != null)
                return IsSelected(veaInTemplate);

            var styleSheet = element.GetStyleSheet();
            if (styleSheet != null)
                return IsSelected(styleSheet);

            var scs = element.GetStyleComplexSelector();
            if (scs != null)
                return IsSelected(scs);

            return false;
        }

        public static void AddElementToSelectionInAsset(BuilderDocument document, VisualElement ve)
        {
            if (BuilderSharedStyles.IsStyleSheetElement(ve))
            {
                var styleSheet = ve.GetStyleSheet();
                AddToSelection(styleSheet);
            }
            else if (BuilderSharedStyles.IsSelectorElement(ve))
            {
                var styleSheet = ve.GetClosestStyleSheet();
                var scs = ve.GetStyleComplexSelector();
                AddToSelection(styleSheet, scs);
            }
            else if (BuilderSharedStyles.IsDocumentElement(ve))
            {
                Undo.RegisterCompleteObjectUndo(
                    document.visualTreeAsset, BuilderConstants.ChangeSelectionUndoMessage);

                var vta = ve.GetVisualTreeAsset();
                AddToSelection(vta);
            }
            else if (ve.GetVisualElementAsset() != null)
            {
                Undo.RegisterCompleteObjectUndo(
                    document.visualTreeAsset, BuilderConstants.ChangeSelectionUndoMessage);

                var vea = ve.GetVisualElementAsset();
                AddToSelection(vea);
            }
            else if (ve.GetVisualElementAssetInTemplate() != null)
            {
                Undo.RegisterCompleteObjectUndo(
                    document.visualTreeAsset, BuilderConstants.ChangeSelectionUndoMessage);

                var vea = ve.GetVisualElementAssetInTemplate();
                AddToSelection(vea);
            }
        }

        public static void RemoveElementFromSelectionInAsset(BuilderDocument document, VisualElement ve)
        {
            if (BuilderSharedStyles.IsStyleSheetElement(ve))
            {
                var styleSheet = ve.GetStyleSheet();
                RemoveFromSelection(styleSheet);
            }
            else if (BuilderSharedStyles.IsSelectorElement(ve))
            {
                var styleSheet = ve.GetClosestStyleSheet();
                var scs = ve.GetStyleComplexSelector();
                RemoveFromSelection(styleSheet, scs);
            }
            else if (BuilderSharedStyles.IsDocumentElement(ve))
            {
                Undo.RegisterCompleteObjectUndo(document.visualTreeAsset, BuilderConstants.ChangeSelectionUndoMessage);

                var vta = ve.GetVisualTreeAsset();
                RemoveFromSelection(vta);
            }
            else if (ve.GetVisualElementAsset() != null)
            {
                Undo.RegisterCompleteObjectUndo(document.visualTreeAsset, BuilderConstants.ChangeSelectionUndoMessage);

                var vea = ve.GetVisualElementAsset();
                RemoveFromSelection(vea);
            }
            else if (ve.GetVisualElementAssetInTemplate() != null)
            {
                Undo.RegisterCompleteObjectUndo(document.visualTreeAsset, BuilderConstants.ChangeSelectionUndoMessage);

                var vea = ve.GetVisualElementAssetInTemplate();
                RemoveFromSelection(vea);
            }
        }
    }
}
