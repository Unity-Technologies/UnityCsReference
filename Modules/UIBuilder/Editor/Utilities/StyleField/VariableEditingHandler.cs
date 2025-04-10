// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    internal class VariableEditingHandler
    {
        static readonly string s_LabelClassName = "unity-builder-inspector__variable-field-label";
        static readonly string s_PlaceholderText = "Variable";
        static readonly string s_ReadOnlyPlaceholderText = "None";

        static readonly int s_InteractionDisableDelay = 200;

        IVisualElementScheduledItem m_ScheduledShowPopup;

        string m_InitialText;
        bool m_InitialEnabledState;
        bool m_ShowingStyleVariableField = false;
        Builder m_Builder;
        BuilderInspector m_Inspector;
        BuilderStyleRow m_Row;
        string m_StyleName;
        bool m_PopupTemporarilyDisabled = false;
        bool m_EditingTemporarilyDisabled = false;
        VariableCompleter m_CompleterOnTarget;
        public int index = 0;

        public bool editingEnabled { get; set; } = true;

        public bool isVariableFieldVisible
        {
            get
            {
                return variableField != null && !variableField.ClassListContains(BuilderConstants.HiddenStyleClassName);
            }
        }

        public VariableInfoTooltip variableInfoTooltip { get; private set; }

        public VariableField variableField { get; private set; }
        public FieldSearchCompleter<VariableInfo> completer { get; private set; }

        public BindableElement targetField { get; private set; }

        public string styleName
        {
            get
            {
                if (m_StyleName == null)
                {
                    m_StyleName = m_Row.bindingPath;

                    if (string.IsNullOrEmpty(m_StyleName))
                        m_StyleName = targetField.bindingPath;
                }

                return m_StyleName;
            }
        }

        public Label labelElement { get; private set; }

        public BuilderInspector inspector => m_Inspector;

        public VariableCompleter completerOnTarget => m_CompleterOnTarget;

        public VariableEditingHandler(BindableElement field)
        {
            targetField = field;

            if (targetField is DimensionStyleField || targetField is NumericStyleField || targetField is IntegerStyleField)
            {
                m_CompleterOnTarget = CreateCompleter();
                m_CompleterOnTarget.SetupCompleterField(targetField.Q<TextField>(), false);
            }

            labelElement = new Label();

            var fieldLabel = targetField.GetValueByReflection("labelElement") as Label;
            // TODO: Will need to bring this back once we can also do the dragger at the same time.
            //fieldLabel.RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
            labelElement.RegisterValueChangedCallback(e => { e.StopImmediatePropagation(); });

            fieldLabel.Add(labelElement);
            labelElement.AddToClassList(BaseField<int>.labelUssClassName);
            labelElement.AddToClassList(s_LabelClassName);
            labelElement.text = fieldLabel.text;
            fieldLabel.AddToClassList(BuilderConstants.InspectorContainerClassName);
            fieldLabel.generateVisualContent = null; // Leave the text of the default label as it is used in some queries (in tests) but prevent the text from being rendered

            m_Inspector = targetField.GetFirstAncestorOfType<BuilderInspector>();
            if (m_Inspector != null)
            {
                m_Builder = m_Inspector.paneWindow as Builder;
                m_Row = targetField.GetFirstAncestorOfType<BuilderStyleRow>();
            }
        }

        public VariableCompleter CreateCompleter()
        {
            return new VariableCompleter(this);
        }

        void InitVariableField()
        {
            if (variableField != null)
                return;

            variableField = new VariableField();

            completer = CreateCompleter();
            completer.alwaysVisible = true;
            variableField.AddToClassList(BuilderConstants.HiddenStyleClassName);
            targetField.Add(variableField);

            var input = variableField.Q(TextField.textInputUssName);
            variableField.RegisterCallback<GeometryChangedEvent>((e) => {
                if (!m_ShowingStyleVariableField)
                    return;

                m_ShowingStyleVariableField = false;
                input.Focus();
            });

            variableField.RegisterValueChangedCallback<string>(e => e.StopImmediatePropagation());
            input.RegisterCallback<BlurEvent>(e => OnVariableEditingFinished(), TrickleDown.TrickleDown);
        }

        void OnVariableEditingFinished()
        {
            var newVarName = variableField.value;

            targetField.SetEnabled(m_InitialEnabledState);

            if (m_InitialText != newVarName)
            {
                SetVariable(newVarName);
            }
            HideStyleVariableField();
            variableInfoTooltip?.Hide();
        }

        internal void SetVariable(string variableName)
        {
            if (string.IsNullOrEmpty(variableName))
            {
                m_Inspector.styleFields.UnsetStylePropertyForElement(targetField, true);
            }
            else
            {
                m_Inspector.styleFields.OnFieldVariableChange(variableName, targetField, styleName, index);
                m_Inspector.RefreshUI(false);
            }
        }

        public void ShowVariableField()
        {
            if (m_Row == null || m_Inspector == null || isVariableFieldVisible)
                return;

            InitVariableField();

            var varName = GetBoundVariableName(this);

            variableField.isReadOnly = !editingEnabled;
            variableField.placeholderText = editingEnabled ? s_PlaceholderText : s_ReadOnlyPlaceholderText;
            variableField.value = m_InitialText = varName;
            m_InitialEnabledState = targetField.enabledSelf;
            targetField.SetEnabled(true);

            var visualInput = targetField.GetVisualInput();

            visualInput?.AddToClassList(BuilderConstants.HiddenStyleClassName);
            m_ShowingStyleVariableField = true;
            variableField.RemoveFromClassList(BuilderConstants.HiddenStyleClassName);
            targetField.AddToClassList(BuilderConstants.InspectorLocalStyleVariableEditingClassName);
            if (editingEnabled)
                targetField.RemoveFromClassList(BuilderConstants.ReadOnlyStyleClassName);
            else
                targetField.AddToClassList(BuilderConstants.ReadOnlyStyleClassName);

            completer.EnsurePopupIsCreated();
            ShowPopup(this);
        }

        void HideStyleVariableField()
        {
            var visualInput = targetField.GetVisualInput();

            targetField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleVariableEditingClassName);
            visualInput?.RemoveFromClassList(BuilderConstants.HiddenStyleClassName);
            variableField.AddToClassList(BuilderConstants.HiddenStyleClassName);
            DisablePopupTemporarily();
            DisableEditingTemporarily();
        }

        void OnPointerDownEvent(PointerDownEvent e)
        {
            if (e.button != (int)MouseButton.LeftMouse)
                return;

            if (e.clickCount == 1 && (variableInfoTooltip == null || variableInfoTooltip.currentHandler != this) && !m_PopupTemporarilyDisabled && !isVariableFieldVisible)
            {
                m_ScheduledShowPopup = targetField.schedule.Execute(() => ShowPopup(this)).StartingIn(BuilderConstants.DoubleClickDelay);
            }
            else if (e.clickCount == 2 && !m_EditingTemporarilyDisabled)
            {
                m_ScheduledShowPopup?.Pause();
                m_ScheduledShowPopup = null;
                ShowVariableField();
            }
        }

        public StyleProperty GetStyleProperty()
        {
            var cSharpStyleName = BuilderNameUtilities.ConvertUssNameToStyleName(styleName);
            return BuilderInspectorStyleFields.GetLastStyleProperty(m_Inspector.currentRule, cSharpStyleName);
        }

        public void RefreshField()
        {
            if (m_Row == null || m_Inspector == null)
                return;

            // Disable completion on text field-based property fields when editing inline styles
            if (m_CompleterOnTarget != null)
                m_CompleterOnTarget.enabled = BuilderSharedStyles.IsSelectorElement(m_Inspector.currentVisualElement);
        }

        public static string GetBoundVariableName(VariableEditingHandler handler)
        {
            var styleName = handler.targetField.GetProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName) as string;
            var property = BuilderInspectorStyleFields.GetLastStyleProperty(handler.m_Inspector.currentRule, styleName);
            string varName = null;

            // Find the name of the variable bound to the field
            if (property != null)
            {
                using (var manipulator = handler.m_Inspector.styleSheet.GetStylePropertyManipulator(
                    handler.m_Inspector.currentVisualElement, handler.m_Inspector.currentRule, property.name,
                    handler.m_Inspector.document.fileSettings.editorExtensionMode))
                {
                    var displayIndex = handler.index;
                    displayIndex = AdjustDisplayIndexForTransitions(displayIndex, manipulator);
                    if (displayIndex >= 0 && manipulator.IsVariableAtIndex(displayIndex))
                        varName = manipulator.GetVariableNameAtIndex(displayIndex);
                }
            }
            else if (!handler.editingEnabled)
            {
                var matchedRules = handler.m_Inspector.matchingSelectors.matchedRulesExtractor.selectedElementRules;

                for (var i = matchedRules.Count - 1; i >= 0; --i)
                {
                    var matchRecord = matchedRules.ElementAt(i).matchRecord;
                    var ruleProperty = matchRecord.sheet.FindLastProperty(matchRecord.complexSelector.rule, styleName);

                    if (ruleProperty != null)
                    {
                        using (var manipulator = matchRecord.sheet.GetStylePropertyManipulator(
                            handler.m_Inspector.currentVisualElement, matchRecord.complexSelector.rule, ruleProperty.name,
                            handler.m_Inspector.document.fileSettings.editorExtensionMode))
                        {
                            var displayIndex = handler.index;
                            displayIndex = AdjustDisplayIndexForTransitions(displayIndex, manipulator);

                            if (displayIndex >= 0 && manipulator.IsVariableAtIndex(displayIndex))
                            {
                                varName = manipulator.GetVariableNameAtIndex(displayIndex);
                                break;
                            }
                        }

                    }
                }
            }

            return varName;
        }

        static void ShowPopup(VariableEditingHandler handler)
        {
            if (handler.m_Builder == null || handler.m_Inspector == null || handler.editingEnabled)
                return;

            string varName = GetBoundVariableName(handler);

            VariableInfo varInfo = default;

            if (!string.IsNullOrEmpty(varName))
            {
                varInfo = StyleVariableUtilities.FindVariable(handler.m_Inspector.currentVisualElement, varName, handler.inspector.document.fileSettings.editorExtensionMode);

                if (!varInfo.IsValid())
                    varInfo = new VariableInfo(varName);
            }

            if (handler.variableInfoTooltip == null)
            {
                handler.variableInfoTooltip = handler.m_Builder.rootVisualElement.GetProperty(BuilderConstants.ElementLinkedVariableTooltipVEPropertyName) as VariableInfoTooltip;

                if (handler.variableInfoTooltip == null)
                {
                    handler.variableInfoTooltip = new VariableInfoTooltip();
                    handler.m_Builder.rootVisualElement.SetProperty(
                        BuilderConstants.ElementLinkedVariableTooltipVEPropertyName, handler.variableInfoTooltip);
                    handler.m_Builder.rootVisualElement.Add(handler.variableInfoTooltip);
                    handler.variableInfoTooltip.onHide += () => OnPopupClosed(handler.variableInfoTooltip);
                }
            }

            handler.targetField.AddToClassList(BuilderConstants.InspectorLocalStyleVariableEditingClassName);
            if (handler.editingEnabled)
                handler.targetField.RemoveFromClassList(BuilderConstants.ReadOnlyStyleClassName);
            else
                handler.targetField.AddToClassList(BuilderConstants.ReadOnlyStyleClassName);
            handler.variableInfoTooltip.Show(handler, varInfo);
            handler.m_ScheduledShowPopup = null;
        }

        static void OnPopupClosed(VariableInfoTooltip tooltip)
        {
            VariableEditingHandler handler = tooltip.currentHandler;

            if (!handler.isVariableFieldVisible)
            {
                handler.targetField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleVariableEditingClassName);
            }
            handler.DisablePopupTemporarily();
        }

        void DisablePopupTemporarily()
        {
            if (m_PopupTemporarilyDisabled)
                return;
            m_PopupTemporarilyDisabled = true;
            targetField.schedule.Execute(() => m_PopupTemporarilyDisabled = false).ExecuteLater(s_InteractionDisableDelay);
        }

        void DisableEditingTemporarily()
        {
            if (m_EditingTemporarilyDisabled)
                return;
            m_EditingTemporarilyDisabled = true;
            targetField.schedule.Execute(() => m_EditingTemporarilyDisabled = false).ExecuteLater(s_InteractionDisableDelay);
        }

        static int AdjustDisplayIndexForTransitions(int index, StylePropertyManipulator manipulator)
        {
            var valueCount = manipulator.GetValuesCount();
            if (index < valueCount ||
                !StylePropertyUtil.propertyNameToStylePropertyId.TryGetValue(manipulator.propertyName, out var id) ||
                !id.IsTransitionId())
                return index;

            switch (id)
            {
                case StylePropertyId.TransitionProperty:
                    index = -1;
                    break;
                case StylePropertyId.TransitionDuration:
                    index %= manipulator.GetValuesCount();
                    break;
                case StylePropertyId.TransitionTimingFunction:
                    index %= manipulator.GetValuesCount();
                    break;
                case StylePropertyId.TransitionDelay:
                    index %= manipulator.GetValuesCount();
                    break;
            }

            return index;
        }
    }
}
