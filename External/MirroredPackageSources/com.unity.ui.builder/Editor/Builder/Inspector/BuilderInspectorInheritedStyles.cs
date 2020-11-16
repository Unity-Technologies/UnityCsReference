using UnityEngine.UIElements;
using UnityEditor.UIElements.Debugger;
using System.Text;
using UnityEngine.UIElements.StyleSheets;
using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Linq;

namespace Unity.UI.Builder
{
    internal class BuilderInspectorInheritedStyles : IBuilderInspectorSection
    {
        BuilderInspector m_Inspector;
        BuilderSelection m_Selection;
        BuilderPaneWindow m_PaneWindow;
        BuilderInspectorMatchingSelectors m_MatchingSelectors;

        PersistedFoldout m_InheritedStylesSection;
        VisualElement m_ClassListContainer;
        PersistedFoldout m_MatchingSelectorsFoldout;

        TextField m_AddClassField;
        Button m_AddClassButton;
        Button m_CreateClassButton;
        VisualTreeAsset m_ClassPillTemplate;
        
        VisualElement currentVisualElement => m_Inspector.currentVisualElement;

        public VisualElement root => m_InheritedStylesSection;

        public BuilderInspectorInheritedStyles(BuilderInspector inspector, BuilderInspectorMatchingSelectors matchingSelectors)
        {
            m_Inspector = inspector;
            m_Selection = inspector.selection;
            m_PaneWindow = inspector.paneWindow;
            m_MatchingSelectors = matchingSelectors;

            m_InheritedStylesSection = m_Inspector.Q<PersistedFoldout>("inspector-inherited-styles-foldout");
            m_ClassListContainer = m_Inspector.Q("class-list-container");
            m_MatchingSelectorsFoldout = m_Inspector.Q<PersistedFoldout>("matching-selectors-container");

            m_AddClassField = m_Inspector.Q<TextField>("add-class-field");
            m_AddClassField.isDelayed = true;
            m_AddClassField.RegisterCallback<KeyUpEvent>(OnAddClassFieldChange);

            m_AddClassButton = m_Inspector.Q<Button>("add-class-button");
            m_CreateClassButton = m_Inspector.Q<Button>("create-class-button");

            m_ClassPillTemplate = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/BuilderClassPill.uxml");

            m_AddClassButton.clickable.clicked += AddStyleClass;
            m_CreateClassButton.clickable.clicked += ExtractLocalStylesToNewClass;
        }

        public void Enable()
        {
            m_Inspector.Query<Button>().ForEach(e =>
            {
                e.SetEnabled(true);
            });
            m_AddClassField.SetEnabled(true);
            m_ClassListContainer.SetEnabled(true);
        }

        public void Disable()
        {
            m_Inspector.Query<Button>().ForEach(e =>
            {
                e.SetEnabled(false);
            });
            m_AddClassField.SetEnabled(false);
            m_ClassListContainer.SetEnabled(false);
        }

        void OnAddClassFieldChange(KeyUpEvent evt)
        {
            if (evt.keyCode != KeyCode.Return)
                return;

            AddStyleClass();

            evt.StopPropagation();
            evt.PreventDefault();

            m_AddClassField.Focus();
        }

        bool VerifyNewClassNameIsValid(string className)
        {
            if (string.IsNullOrEmpty(className))
                return false;
            
            if (className.Contains(" "))
            {
                Builder.ShowWarning(BuilderConstants.AddStyleClassValidationSpaces);
                return false;
            }
            
            if (!BuilderNameUtilities.attributeRegex.IsMatch(className))
            {
                Builder.ShowWarning(BuilderConstants.ClassNameValidationSpacialCharacters);
                return false;
            }

            return true;
        }

        void AddStyleClass()
        {
            var className = m_AddClassField.value;
            className = className.TrimStart(BuilderConstants.UssSelectorClassNameSymbol[0]);
            if (!VerifyNewClassNameIsValid(className))
            {
                m_AddClassField.visualInput.Focus();
                return;
            }

            AddStyleClass(className);
        }

        void ExtractLocalStylesToNewClass()
        {
            var className = m_AddClassField.value;
            className = className.TrimStart(BuilderConstants.UssSelectorClassNameSymbol[0]);

            if (!VerifyNewClassNameIsValid(className))
            {
                m_AddClassField.visualInput.Focus();
                return;
            }

            ExtractLocalStylesToNewClass(className);
        }

        void PreAddStyleClass(string className)
        {
            m_AddClassField.SetValueWithoutNotify(string.Empty);

            // Remove temporary min-size class on VisualElement.
            currentVisualElement.RemoveMinSizeSpecialElement();

            // Actually add the style class to the element in the canvas.
            currentVisualElement.AddToClassList(className);
        }

        void AddStyleClass(string className)
        {
            PreAddStyleClass(className);

            // Update VisualTreeAsset.
            BuilderAssetUtilities.AddStyleClassToElementInAsset(
                m_PaneWindow.document, currentVisualElement, className);

            // We actually want to get the notification back and refresh ourselves.
            m_Selection.NotifyOfHierarchyChange(null);
            m_Selection.NotifyOfStylingChange(null);
        }

        StyleSheet GetOrCreateOrAddMainStyleSheet()
        {
            // Get StyleSheet.
            var mainStyleSheet = m_PaneWindow.document.activeStyleSheet;
            if (mainStyleSheet == null)
            {
                var option = BuilderDialogsUtility.DisplayDialogComplex(
                    BuilderConstants.ExtractInlineStylesNoUSSDialogTitle,
                    BuilderConstants.ExtractInlineStylesNoUSSDialogMessage,
                    BuilderConstants.ExtractInlineStylesNoUSSDialogNewUSSOption,
                    BuilderConstants.ExtractInlineStylesNoUSSDialogExistingUSSOption,
                    BuilderConstants.DialogCancelOption);

                switch (option)
                {
                    // New
                    case 0:
                        if (!BuilderStyleSheetsUtilities.CreateNewUSSAsset(m_PaneWindow))
                            return null;
                        break;
                    // Existing
                    case 1:
                        if (!BuilderStyleSheetsUtilities.AddExistingUSSToAsset(m_PaneWindow))
                            return null;
                        break;
                    // Cancel
                    case 2:
                        return null;
                }

                mainStyleSheet = m_PaneWindow.document.activeStyleSheet;
            }
            return mainStyleSheet;
        }

        void ExtractLocalStylesToNewClass(string className)
        {
            // Get StyleSheet.
            var mainStyleSheet = GetOrCreateOrAddMainStyleSheet();
            if (mainStyleSheet == null)
                return;

            PreAddStyleClass(className);

            // Create new selector in main StyleSheet.
            var selectorString = BuilderConstants.UssSelectorClassNameSymbol + className;
            var selectorsRootElement = BuilderSharedStyles.GetSelectorContainerElement(m_Selection.documentRootElement);
            var newSelector = BuilderSharedStyles.CreateNewSelector(selectorsRootElement, mainStyleSheet, selectorString);

            // Transfer all properties from inline styles rule to new selector.
            mainStyleSheet.TransferRulePropertiesToSelector(
                newSelector, m_Inspector.styleSheet, m_Inspector.currentRule);

            // Update VisualTreeAsset.
            BuilderAssetUtilities.AddStyleClassToElementInAsset(
                m_PaneWindow.document, currentVisualElement, className);

            // Overwrite Undo Message.
            Undo.RegisterCompleteObjectUndo(
                new UnityEngine.Object[] { m_PaneWindow.document.visualTreeAsset, mainStyleSheet },
                BuilderConstants.CreateStyleClassUndoMessage);

            // We actually want to get the notification back and refresh ourselves.
            m_Selection.NotifyOfStylingChange(null);
            m_Selection.NotifyOfHierarchyChange(null, currentVisualElement);
        }

        void OnStyleClassDelete(EventBase evt)
        {
            var target = evt.target as VisualElement;
            var className = target.userData as string;

            // Remove temporary min-size class on VisualElement.
            currentVisualElement.RemoveMinSizeSpecialElement();

            // Actually remove the style class from the element in the canvas.
            currentVisualElement.RemoveFromClassList(className);

            // Update VisualTreeAsset.
            BuilderAssetUtilities.RemoveStyleClassFromElementInAsset(
                m_PaneWindow.document, currentVisualElement, className);

            // We actually want to get the notification back and refresh ourselves.
            m_Selection.NotifyOfHierarchyChange(null);
            m_Selection.NotifyOfStylingChange(null);

            evt.StopPropagation();
        }

        Clickable CreateClassPillClickableManipulator()
        {
            var clickable = new Clickable(OnClassPillDoubleClick);
            var activator = clickable.activators[0];
            activator.clickCount = 2;
            clickable.activators[0] = activator;
            return clickable;
        }

        bool IsClassInUXMLDoc(string className)
        {
            var vea = currentVisualElement?.GetVisualElementAsset();
            return vea != null && vea.classes != null && vea.classes.Contains(className);
        }

        void RefreshClassListContainer()
        {
            if (currentVisualElement == null)
                return;

            m_ClassListContainer.Clear();
            if (BuilderSharedStyles.IsSelectorElement(currentVisualElement))
                return;

            var builderWindow = m_PaneWindow as Builder;
            if (builderWindow == null)
                return;

            var documentRootElement = builderWindow.documentRootElement;

            foreach (var className in currentVisualElement.GetClasses())
            {
                m_ClassPillTemplate.CloneTree(m_ClassListContainer.contentContainer);
                var pill = m_ClassListContainer.contentContainer.ElementAt(m_ClassListContainer.childCount - 1);
                var pillLabel = pill.Q<Label>("class-name-label");
                var pillDeleteButton = pill.Q<Button>("delete-class-button");
                pillDeleteButton.userData = className;
                pill.userData = className;

                // Add ellipsis if the class name is too long.
                var classNameShortened = BuilderNameUtilities.CapStringLengthAndAddEllipsis(className, BuilderConstants.ClassNameInPillMaxLength);
                pillLabel.text = BuilderConstants.UssSelectorClassNameSymbol + classNameShortened;

                if (IsClassInUXMLDoc(className))
                {
                    pillDeleteButton.clickable.clickedWithEventInfo += OnStyleClassDelete;
                }
                else
                {
                    // Don't show "x" button if the class can't actually be removed.
                    pillDeleteButton.style.display = DisplayStyle.None;
                }

                // See if the class is in document as its own selector.
                var selector = BuilderSharedStyles.FindSelectorElement(documentRootElement, BuilderConstants.UssSelectorClassNameSymbol + className);
                pill.SetProperty(BuilderConstants.InspectorClassPillLinkedSelectorElementVEPropertyName, selector);
                var clickable = CreateClassPillClickableManipulator();
                pill.AddManipulator(clickable);
                if (selector == null)
                {
                    pill.AddToClassList(BuilderConstants.InspectorClassPillNotInDocumentClassName);
                    pill.tooltip = BuilderConstants.InspectorClassPillDoubleClickToCreate;
                }
                else
                {
                    pill.tooltip = BuilderConstants.InspectorClassPillDoubleClickToSelect;
                }
            }
        }

        void OnClassPillDoubleClick(EventBase evt)
        {
            var pill = evt.currentTarget as VisualElement;
            var className = pill.userData as string;
            var selectorString = BuilderConstants.UssSelectorClassNameSymbol + className;
            var selectorElement = pill.GetProperty(BuilderConstants.InspectorClassPillLinkedSelectorElementVEPropertyName) as VisualElement;

            if (selectorElement == null)
            {
                // Get StyleSheet.
                var mainStyleSheet = GetOrCreateOrAddMainStyleSheet();
                if (mainStyleSheet == null)
                    return;

                var selectorsRootElement = BuilderSharedStyles.GetSelectorContainerElement(m_Selection.documentRootElement);
                BuilderSharedStyles.CreateNewSelector(selectorsRootElement, mainStyleSheet, selectorString);

                m_Selection.NotifyOfStylingChange();
                m_Selection.NotifyOfHierarchyChange();
            }
            else
            {
                m_Selection.Select(null, selectorElement);
            }
        }

        VisualElement GeneratedMatchingSelectors()
        {
            m_MatchingSelectors.GetElementMatchers();
            if (m_MatchingSelectors.matchedRulesExtractor.selectedElementRules == null ||
                m_MatchingSelectors.matchedRulesExtractor.selectedElementRules.Count <= 0)
                return null;

            var container = new VisualElement();

            int ruleIndex = 0;
            var options = new UssExportOptions();
            var sb = new StringBuilder();

            foreach (var rule in m_MatchingSelectors.matchedRulesExtractor.selectedElementRules)
            {
                var selectorStr = StyleSheetToUss.ToUssSelector(rule.matchRecord.complexSelector);

                StyleProperty[] props = rule.matchRecord.complexSelector.rule.properties;
                var ruleFoldout = new PersistedFoldout()
                {
                    value = false,
                    text = selectorStr,
                    viewDataKey = "builder-inspector-rule-foldout__" + ruleIndex
                };
                ruleIndex++;
                container.Add(ruleFoldout);

                if (props.Length == 0)
                {
                    var label = new Label("None");
                    label.AddToClassList(BuilderConstants.InspectorEmptyFoldoutLabelClassName);
                    ruleFoldout.Add(label);
                    continue;
                }

                for (int j = 0; j < props.Length; j++)
                {
                    sb.Length = 0;
                    StyleSheetToUss.ToUssString(rule.matchRecord.sheet, options, props[j], sb);
                    string s = sb.ToString();

                    s = s?.ToLower();
                    var textField = new TextField(props[j].name) { value = s };
                    textField.isReadOnly = true;
                    ruleFoldout.Add(textField);
                }
            }

            return container;
        }

        void RefreshMatchingSelectorsContainer()
        {
            if (currentVisualElement == null)
                return;

            m_MatchingSelectorsFoldout.Clear();
            if (BuilderSharedStyles.IsSelectorElement(currentVisualElement))
                return;

            VisualElement matchingSelectors = GeneratedMatchingSelectors();
            if (matchingSelectors != null)
            {
                m_MatchingSelectorsFoldout.Add(matchingSelectors);

                // Forward focus to the panel header.
                matchingSelectors
                    .Query()
                    .Where(e => e.focusable)
                    .ForEach((e) => m_Inspector.AddFocusable(e));
            }
            else
            {
                var label = new Label("None");
                label.AddToClassList(BuilderConstants.InspectorEmptyFoldoutLabelClassName);
                m_MatchingSelectorsFoldout.Add(label);
            }
        }

        public void Refresh()
        {
            RefreshClassListContainer();
            RefreshMatchingSelectorsContainer();
        }
    }
}
