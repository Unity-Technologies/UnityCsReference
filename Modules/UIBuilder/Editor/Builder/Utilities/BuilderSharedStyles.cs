// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System;
using Unity.UIToolkit.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    internal class BuilderSharedStyles
    {
        private static readonly string ElementLinkedStyleSelectorVEPropertyName = "__unity-ui-builder-linked-style-selector";
        private static readonly string ElementLinkedStyleSheetVEPropertyName = "__unity-ui-builder-linked-stylesheet";

        internal static bool IsDocumentElement(VisualElement element)
        {
            return element.name == "document" && element.ClassListContains("unity-builder-viewport__document");
        }

        internal static bool IsSelectorsContainerElement(VisualElement element)
        {
            return element.name == BuilderConstants.StyleSelectorElementContainerName;
        }

        internal static bool IsStyleSheetElement(VisualElement element)
        {
            return element.GetProperty(ElementLinkedStyleSheetVEPropertyName) != null;
        }

        internal static void SetStyleSheetElementProperty(VisualElement element, StyleSheet styleSheet)
        {
            element.SetProperty(ElementLinkedStyleSheetVEPropertyName, styleSheet);
        }

        internal static StyleSheet GetStyleSheetElementProperty(VisualElement element)
        {
            return (StyleSheet)element?.GetProperty(ElementLinkedStyleSheetVEPropertyName);
        }

        internal static bool IsSelectorElement(VisualElement element)
        {
            return GetSelectorProperty(element) != null;
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
            var complexSelector = GetSelectorProperty(element);
            var selectorStr = BuilderStyleSheetExporter.GetSelectorString(complexSelector);
            return selectorStr;
        }

        internal static StyleComplexSelector GetSelectorProperty(VisualElement element)
        {
            return (StyleComplexSelector) element?.GetProperty(ElementLinkedStyleSelectorVEPropertyName);
        }

        internal static void SetSelectorProperty(VisualElement element, StyleComplexSelector selector)
        {
            element.SetProperty(ElementLinkedStyleSelectorVEPropertyName, selector);
        }

        internal static bool SetSelectorString(VisualElement element, StyleSheet styleSheet, string newString, out string error)
        {
            var complexSelector = GetSelectorProperty(element);
            if (!complexSelector.TrySetSelectorsFromString(newString, out error))
                return false;
            return true;

        }

        internal static List<string> GetSelectorParts(VisualElement element)
        {
            var complexSelector = GetSelectorProperty(element);
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
                styleSheetElement.SetProperty(ElementLinkedStyleSheetVEPropertyName, styleSheet);
                if (belongsToParent)
                    styleSheetElement.SetProperty(BuilderConstants.ExplorerItemLinkedUXMLFileName, associatedUXMLFileName);
                styleSheetElement.SetProperty(BuilderConstants.ElementLinkedStyleSheetIndexVEPropertyName, i + startInd);
                styleSheetElement.styleSheets.Add(styleSheet);
                selectorContainerElement?.Add(styleSheetElement);

                foreach(var rule in styleSheet.rules)
                foreach (var complexSelector in rule.complexSelectors)
                {
                    if (StyleSheetExporter.UssExportOptions.Default.IsSelectorIgnored(complexSelector))
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
            var complexSelector =  StyleSheetExtensions.AddSelector(styleSheet, selectorStr);

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

            using var seenRulesHandle = HashSetPool<StyleRule>.Get(out var seenRules);
            using var rulesHandle = ListPool<StyleRule>.Get(out var rules);

            var complexSelectorsList = new List<StyleComplexSelector>();

            foreach (var childElement in styleSheetElement.Children())
            {
                complexSelectorsList.Add(childElement.GetStyleComplexSelector());
            }

            // It is possible that style rules have multiple selectors. If that is the case, after we have reordered
            // them, break them into their own rule. While this is not ideal, the Builder will break them apart
            // at export time anyways and this avoids having a style rule with separated selectors.
            for (var i = 0; i < complexSelectorsList.Count; ++i)
            {
                var selector = complexSelectorsList[i];
                // Rule was already seen, split into two rules.
                if (!seenRules.Add(selector.rule))
                {
                    var previousRule = selector.rule;
                    var rule = styleSheet.AddRule(BuilderStyleSheetExporter.GetSelectorString(selector));
                    rule.AddSelector(BuilderStyleSheetExporter.GetSelectorString(selector));
                    selector.rule.RemoveSelector(selector);
                    StyleSheetExtensions.SwallowStyleRule(styleSheet, rule, styleSheet, previousRule);
                    rules.Add(rule);
                }
                else
                {
                    rules.Add(selector.rule);
                }
            }

            styleSheet.SetRules(rules.ToArray());
            styleSheet.RequestRebuild();
        }

        public static void MoveSelectorBetweenStyleSheets(
            VisualElement fromStyleSheetElement, VisualElement toStyleSheetElement, VisualElement selectorElement, bool undo)
        {
            var fromStyleSheet = fromStyleSheetElement.GetStyleSheet();
            var toStyleSheet = toStyleSheetElement.GetStyleSheet();
            var fromSelector = selectorElement.GetStyleComplexSelector();

            if (fromStyleSheet == null || toStyleSheet == null)
                return;

            if (fromSelector == null)
                throw new ArgumentNullException("Selector VisualElement does not exist.", nameof(selectorElement));

            if (undo)
            {
                Undo.RegisterCompleteObjectUndo(
                    fromStyleSheet, BuilderConstants.MoveUSSSelectorUndoMessage);
                Undo.RegisterCompleteObjectUndo(
                    toStyleSheet, BuilderConstants.MoveUSSSelectorUndoMessage);
            }

            var toSelector = toStyleSheet.Swallow(fromStyleSheet, fromSelector);
            fromStyleSheet.RemoveSelector(fromSelector);

            SetSelectorProperty(selectorElement, toSelector);
        }

        public static VisualElement FindSelectorElement(VisualElement documentRootElement, string selectorStr)
        {
            var selectorContainer = GetSelectorContainerElement(documentRootElement);
            var allSelectorElements = selectorContainer.Query().Where((e) => true).Build();

            foreach (var selectorElement in allSelectorElements)
            {
                var complexSelector = GetSelectorProperty(selectorElement);
                if (complexSelector == null)
                    continue;

                var currentSelectorStr = BuilderStyleSheetExporter.GetSelectorString(complexSelector);
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
            SetSelectorProperty(ssVE, complexSelector);

            return ssVE;
        }

        public static List<string> GetMatchingSelectorsOnElement(VisualElement documentElement)
        {
            var matchedElementsSelector = new MatchedRulesExtractor(AssetDatabase.GetAssetPath);
            matchedElementsSelector.FindMatchingRules(documentElement);

            if (matchedElementsSelector.selectedElementRules == null || matchedElementsSelector.selectedElementRules.Count <= 0)
                return null;

            var complexSelectors = new List<string>();
            foreach (var rule in matchedElementsSelector.selectedElementRules)
            {
                var complexSelector = rule.matchRecord.complexSelector;
                var complexSelectorString = BuilderStyleSheetExporter.GetSelectorString(complexSelector);
                complexSelectors.Add(complexSelectorString);
            }

            return complexSelectors;
        }

        public static List<SelectorMatchRecord> GetMatchingSelectorsOnElementFromLocalStyleSheet(VisualElement documentElement)
        {
            var matchedElementsSelector = new MatchedRulesExtractor(AssetDatabase.GetAssetPath);

            // set all pseudo states to true to get all matching selectors
            var previousPseudoStates = documentElement.pseudoStates;
            documentElement.pseudoStates = PseudoStates.Active | PseudoStates.Disabled | PseudoStates.Focus | PseudoStates.Hover | PseudoStates.Checked;

            matchedElementsSelector.FindMatchingRules(documentElement);

            if (matchedElementsSelector.selectedElementRules == null || matchedElementsSelector.selectedElementRules.Count <= 0)
                return new List<SelectorMatchRecord>();

            var complexSelectors = new List<SelectorMatchRecord>();
            foreach (var rule in matchedElementsSelector.selectedElementRules)
            {
                if (rule.matchRecord.sheet.IsUnityEditorStyleSheet() || rule.matchRecord.sheet.isDefaultStyleSheet)
                    continue;
                complexSelectors.Add(rule.matchRecord);
            }

            // return pseudo states to previous state
            documentElement.pseudoStates = previousPseudoStates;

            return complexSelectors;
        }

        public static IEnumerable<ClassCompleterInfo> GetAllUnappliedClasses(VisualElement documentRootElement, VisualElement currentVisualElement)
        {
            var results = new List<ClassCompleterInfo>();

            if (documentRootElement == null)
                return results;

            var selectorContainer = GetSelectorContainerElement(documentRootElement);
            if (selectorContainer == null)
                return results;

            using var appliedClassesHandle = HashSetPool<string>.Get(out var appliedClasses);
            foreach (var cls in currentVisualElement.GetClasses())
                appliedClasses.Add(cls);

            foreach (var styleSheetElement in selectorContainer.Children())
            {
                var styleSheet = GetStyleSheetElementProperty(styleSheetElement);
                if (styleSheet == null)
                    continue;

                var sheetHeader = new ClassCompleterInfo(styleSheet);
                var headerAdded = false;

                using var seenInSheetHandle = HashSetPool<string>.Get(out var seenInSheet);

                foreach (var selectorElement in styleSheetElement.Children())
                {
                    var complexSelector = GetSelectorProperty(selectorElement);
                    if (complexSelector == null)
                        continue;

                    foreach (var selector in complexSelector.selectors)
                    foreach (var part in selector.parts)
                    {
                        if (part.type != StyleSelectorType.Class || appliedClasses.Contains(part.value) || !seenInSheet.Add(part.value))
                            continue;

                        if (!headerAdded)
                        {
                            results.Add(sheetHeader);
                            headerAdded = true;
                        }

                        results.Add(new ClassCompleterInfo(part, styleSheet));
                    }
                }
            }

            return results;
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
