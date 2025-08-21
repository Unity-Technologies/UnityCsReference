// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class StyleSheetUtilities
    {
        static readonly StyleSheetImporterImpl s_StyleSheetImporter = new StyleSheetImporterImpl();

        public static void AddFakeSelector(VisualElement selectorElement)
        {
            if (selectorElement == null)
                return;

            var styleSheet = selectorElement.GetClosestStyleSheet();

            if (styleSheet == null)
                return;

            StyleComplexSelector complexSelector = BuilderSharedStyles.GetSelectorProperty(selectorElement);
            var fakeSelectorString = BuilderConstants.UssSelectorNameSymbol + selectorElement.name;

            var fakeSelector = styleSheet.FindSelector(fakeSelectorString); // May already exist because of Undo/Redo

            if ((fakeSelector == null || fakeSelector.rule != complexSelector.rule))
            {
                Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.AddNewSelectorUndoMessage);
                complexSelector.rule.AddSelector(fakeSelectorString);
            }

            selectorElement.SetProperty(BuilderConstants.ElementLinkedFakeStyleSelectorVEPropertyName, fakeSelector);
            // To ensure that the fake selector is removed from the stylesheet if the builder gets closed with a selector still selected
            selectorElement.RegisterCallback<DetachFromPanelEvent>(OnSelectorElementDetachedFromPanel);
            selectorElement.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet);
        }

        public static void RemoveFakeSelector(VisualElement selectorElement)
        {
            if (selectorElement == null)
                return;

            var styleSheet = selectorElement.GetClosestStyleSheet();

            if (styleSheet == null)
                return;

            StyleComplexSelector fakeSelector = selectorElement.GetProperty(BuilderConstants.ElementLinkedFakeStyleSelectorVEPropertyName) as StyleComplexSelector;

            if (fakeSelector != null)
            {
                selectorElement.SetProperty(BuilderConstants.ElementLinkedFakeStyleSelectorVEPropertyName, null);
                styleSheet.RemoveSelector(fakeSelector);
                selectorElement.UnregisterCallback<DetachFromPanelEvent>(OnSelectorElementDetachedFromPanel);
            }
        }

        static void OnSelectorElementDetachedFromPanel(DetachFromPanelEvent e)
        {
            RemoveFakeSelector(e.elementTarget);
        }

        public static string GetCleanVariableName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var cleanName = Regex.Replace(value.Trim(), BuilderConstants.USSVariablePattern, BuilderConstants.USSVariableInvalidCharFiller);

            if (!cleanName.StartsWith(BuilderConstants.UssVariablePrefix))
                cleanName = BuilderConstants.UssVariablePrefix + cleanName;

            return cleanName;
        }
    }
}
