using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor.UIElements.Debugger;
using System;
using UnityEditor;

namespace Unity.UI.Builder
{
    internal class BuilderSharedStyles
    {
        internal static bool IsDocumentElement(VisualElement element)
        {
            return element.name == "document" && element.ClassListContains("unity-builder-viewport__document");
        }

        public static VisualElement GetDocumentRootLevelElement(VisualElement element, VisualElement documentRootElement)
        {
            if (element == null)
                return null;

            while (element.parent != null)
            {
                if (element.parent == documentRootElement || IsDocumentElement(element.parent))
                    return element;

                element = element.parent;
            }

            return null;
        }

        internal static bool IsSelectorsContainerElement(VisualElement element)
        {
            return element.name == BuilderConstants.StyleSelectorElementContainerName;
        }

        internal static bool IsStyleSheetElement(VisualElement element)
        {
            return element.GetProperty(BuilderConstants.ElementLinkedStyleSheetVEPropertyName) != null;
        }

        internal static bool IsSelectorElement(VisualElement element)
        {
            return element.GetProperty(BuilderConstants.ElementLinkedStyleSelectorVEPropertyName) != null;
        }

        internal static bool IsParentSelectorElement(VisualElement element)
        {
            return element.ClassListContains(BuilderConstants.StyleSelectorBelongsParent);
        }

        public static bool IsSharedStyleSpecialElement(VisualElement element)
        {
            return IsSelectorElement(element) || IsSelectorsContainerElement(element);
        }

        internal static string GetSelectorString(VisualElement element)
        {
            var complexSelector = element.GetProperty(BuilderConstants.ElementLinkedStyleSelectorVEPropertyName) as StyleComplexSelector;
            var selectorStr = StyleSheetToUss.ToUssSelector(complexSelector);
            return selectorStr;
        }

        internal static StyleComplexSelector GetSelector(VisualElement element)
        {
            return element.GetProperty(BuilderConstants.ElementLinkedStyleSelectorVEPropertyName) as StyleComplexSelector;
        }

        internal static void SetSelectorString(VisualElement element, StyleSheet styleSheet, string newString)
        {
            var complexSelector = element.GetProperty(BuilderConstants.ElementLinkedStyleSelectorVEPropertyName) as StyleComplexSelector;
            styleSheet.SetSelectorString(complexSelector, newString);
        }

        internal static List<string> GetSelectorParts(VisualElement element)
        {
            var complexSelector = element.GetProperty(BuilderConstants.ElementLinkedStyleSelectorVEPropertyName) as StyleComplexSelector;
            if (complexSelector == null)
                return null;

            return GetSelectorParts(complexSelector);
        }

        internal static List<string> GetSelectorParts(StyleComplexSelector complexSelector)
        {
            var selectorParts = new List<string>();
            foreach (var selector in complexSelector.selectors)
            {
                if (selector.previousRelationship != StyleSelectorRelationship.None)
                    selectorParts.Add(selector.previousRelationship == StyleSelectorRelationship.Child ? " > " : " ");

                foreach (var selectorPart in selector.parts)
                {
                    switch (selectorPart.type)
                    {
                        case StyleSelectorType.Wildcard:
                            selectorParts.Add("*");
                            break;
                        case StyleSelectorType.Type:
                            selectorParts.Add(selectorPart.value);
                            break;
                        case StyleSelectorType.Class:
                            selectorParts.Add(BuilderConstants.UssSelectorClassNameSymbol + selectorPart.value);
                            break;
                        case StyleSelectorType.PseudoClass:
                            selectorParts.Add(BuilderConstants.UssSelectorPseudoStateSymbol + selectorPart.value);
                            break;
                        case StyleSelectorType.ID:
                            selectorParts.Add(BuilderConstants.UssSelectorNameSymbol + selectorPart.value);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            return selectorParts;
        }

        internal static VisualElement GetSelectorContainerElement(VisualElement root)
        {
            var sharedStylesContainer = root.parent.Q(BuilderConstants.StyleSelectorElementContainerName);
            return sharedStylesContainer;
        }

        internal static void ClearContainer(VisualElement documentElement)
        {
            var selectorContainerElement = GetSelectorContainerElement(documentElement);
            selectorContainerElement?.Clear();
        }

        internal static void AddSelectorElementsFromStyleSheet(
            VisualElement documentElement,
            List<BuilderDocumentOpenUSS> USSFiles,
            int startInd = 0,
            bool belongsToParent = false,
            string associatedUXMLFileName = null)
        {
            for (int i = 0; i < USSFiles.Count; ++i)
            {
                var selectorContainerElement = GetSelectorContainerElement(documentElement);
                var styleSheet = USSFiles[i].styleSheet;

                var styleSheetElement = new VisualElement();
                styleSheetElement.name = styleSheet.name;
                styleSheetElement.SetProperty(BuilderConstants.ElementLinkedStyleSheetVEPropertyName, styleSheet);
                if (belongsToParent)
                    styleSheetElement.SetProperty(BuilderConstants.ExplorerItemLinkedUXMLFileName, associatedUXMLFileName);
                styleSheetElement.SetProperty(BuilderConstants.ElementLinkedStyleSheetIndexVEPropertyName, i + startInd);
                styleSheetElement.styleSheets.Add(styleSheet);
                selectorContainerElement?.Add(styleSheetElement);

                foreach (var complexSelector in styleSheet.complexSelectors)
                {
                    var complexSelectorStr = StyleSheetToUss.ToUssSelector(complexSelector);
                    if (complexSelectorStr == BuilderConstants.SelectedStyleSheetSelectorName
                        || complexSelectorStr.StartsWith(BuilderConstants.UssSelectorNameSymbol + BuilderConstants.StyleSelectorElementName))
                        continue;

                    var ssVE = CreateNewSelectorElement(styleSheet, complexSelector);
                    if (belongsToParent)
                        ssVE.AddToClassList(BuilderConstants.StyleSelectorBelongsParent);
                    styleSheetElement.Add(ssVE);
                }
            }
        }

        internal static StyleComplexSelector CreateNewSelector(VisualElement selectorContainerElement, StyleSheet styleSheet, string selectorStr)
        {
            var complexSelector = styleSheet.AddSelector(selectorStr);

            VisualElement styleSheetElement = null;
            foreach (var child in selectorContainerElement.Children())
            {
                if (child.GetStyleSheet() == styleSheet)
                {
                    styleSheetElement = child;
                    break;
                }
            }

            if (styleSheetElement != null)
            {
                var ssVE = CreateNewSelectorElement(styleSheet, complexSelector);
                styleSheetElement.Add(ssVE);
            }

            return complexSelector;
        }

        public static void MatchSelectorElementOrderInAsset(VisualElement styleSheetElement, bool undo)
        {
            var styleSheet = styleSheetElement.GetStyleSheet();
            if (styleSheet == null)
                return;

            if (undo)
                Undo.RegisterCompleteObjectUndo(
                    styleSheet, BuilderConstants.MoveUSSSelectorUndoMessage);

            var complexSelectorsList = new List<StyleComplexSelector>();

            foreach (var childElement in styleSheetElement.Children())
            {
                complexSelectorsList.Add(childElement.GetStyleComplexSelector());
            }

            styleSheet.complexSelectors = complexSelectorsList.ToArray();
        }

        public static void MoveSelectorBetweenStyleSheets(
            VisualElement fromStyleSheetElement, VisualElement toStyleSheetElement, VisualElement selectorElement, bool undo)
        {
            var fromStyleSheet = fromStyleSheetElement.GetStyleSheet();
            var toStyleSheet = toStyleSheetElement.GetStyleSheet();
            var fromSelector = selectorElement.GetStyleComplexSelector();

            if (undo)
            {
                Undo.RegisterCompleteObjectUndo(
                    fromStyleSheet, BuilderConstants.MoveUSSSelectorUndoMessage);
                Undo.RegisterCompleteObjectUndo(
                    toStyleSheet, BuilderConstants.MoveUSSSelectorUndoMessage);
            }

            var toSelector = toStyleSheet.Swallow(fromStyleSheet, fromSelector);
            fromStyleSheet.RemoveSelector(fromSelector);

            selectorElement.SetProperty(BuilderConstants.ElementLinkedStyleSelectorVEPropertyName, toSelector);
        }

        public static VisualElement FindSelectorElement(VisualElement documentRootElement, string selectorStr)
        {
            var selectorContainer = GetSelectorContainerElement(documentRootElement);
            var allSelectorElements = selectorContainer.Query().Where((e) => true).ToList();

            foreach (var selectorElement in allSelectorElements)
            {
                var complexSelector = selectorElement.GetProperty(BuilderConstants.ElementLinkedStyleSelectorVEPropertyName) as StyleComplexSelector;
                if (complexSelector == null)
                    continue;

                var currentSelectorStr = StyleSheetToUss.ToUssSelector(complexSelector);
                if (currentSelectorStr == selectorStr)
                    return selectorElement;
            }

            return null;
        }

        static VisualElement CreateNewSelectorElement(StyleSheet styleSheet, StyleComplexSelector complexSelector)
        {
            var ssVE = new VisualElement();
            var ssVEName = BuilderConstants.StyleSelectorElementName + complexSelector.ruleIndex;
            ssVE.name = ssVEName;
            ssVE.SetProperty(BuilderConstants.ElementLinkedStyleSelectorVEPropertyName, complexSelector);

            return ssVE;
        }

        public static List<string> GetMatchingSelectorsOnElement(VisualElement documentElement)
        {
            var matchedElementsSelector = new MatchedRulesExtractor();
            matchedElementsSelector.FindMatchingRules(documentElement);

            if (matchedElementsSelector.selectedElementRules == null || matchedElementsSelector.selectedElementRules.Count <= 0)
                return null;

            var complexSelectors = new List<string>();
            foreach (var rule in matchedElementsSelector.selectedElementRules)
            {
                var complexSelector = rule.matchRecord.complexSelector;
                var complexSelectorString = StyleSheetToUss.ToUssSelector(complexSelector);
                complexSelectors.Add(complexSelectorString);
            }

            return complexSelectors;
        }

        public static List<VisualElement> GetMatchingElementsForSelector(VisualElement documentRootElement, string selectorStr)
        {
            var allElements = documentRootElement.Query().Where((e) => true);
            var matchedElements = new List<VisualElement>();

            // TODO: Seems we are calling this before the DefaultCommon stylesheet has been fully initialized
            // (during OnEnable()). Need to fix this at some point. But for now, you just won't have the matching
            // selectors highlight properly after initial load.
            try
            {
                allElements.ForEach((e) =>
                {
                    var matchedSelectors = GetMatchingSelectorsOnElement(e);
                    if (matchedSelectors != null && matchedSelectors.Contains(selectorStr))
                        matchedElements.Add(e);
                });
            }
            catch {}

            return matchedElements;
        }
    }
}
