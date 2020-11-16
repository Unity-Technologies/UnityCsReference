using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

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
        bool m_ShowingStyleVariableField = false;
        Builder m_Builder;
        BuilderInspector m_Inspector;
        BuilderStyleRow m_Row;
        string m_StyleName;
        bool m_PopupTemporarilyDisabled = false;
        bool m_EditingTemporarilyDisabled = false;

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

        public VariableEditingHandler(BindableElement field)
        {
            targetField = field;

            labelElement = new Label();
            //labelElement.pickingMode = PickingMode.Position;

            var fieldLabel = targetField.GetValueByReflection("labelElement") as Label;
            // TODO: Will need to bring this back once we can also do the dragger at the same time.
            //fieldLabel.RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
            labelElement.RegisterValueChangedCallback(e => { e.StopImmediatePropagation(); });

            fieldLabel.Add(labelElement);
            labelElement.AddToClassList(s_LabelClassName);
            labelElement.text = fieldLabel.text;

            fieldLabel.generateVisualContent = null; // Leave the text of the default label as it is used in some queries (in tests) but prevent the text from being rendered

            m_Inspector = targetField.GetFirstAncestorOfType<BuilderInspector>();
            if (m_Inspector != null)
            {
                m_Builder = m_Inspector.paneWindow as Builder;
                m_Row = targetField.GetFirstAncestorOfType<BuilderStyleRow>();
            }
        }

        void InitVariableField()
        {
            if (variableField != null)
                return;

            variableField = new VariableField();
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
            input.RegisterCallback<BlurEvent>(e => OnVariableEditingFinished());
        }

        void OnVariableEditingFinished()
        {
            var newVarName = variableField.value;

            if (m_InitialText != newVarName)
            {
                if (string.IsNullOrEmpty(newVarName))
                {
                    m_Inspector.styleFields.UnsetStylePropertyForElement(targetField);
                }
                else
                {
                    m_Inspector.styleFields.OnFieldVariableChange(newVarName, targetField, styleName);
                }
            }

            HideStyleVariableField();
            variableInfoTooltip?.Hide();
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

            var visualInput = targetField.GetVisualInput();

            visualInput?.AddToClassList(BuilderConstants.HiddenStyleClassName);
            m_ShowingStyleVariableField = true;
            variableField.RemoveFromClassList(BuilderConstants.HiddenStyleClassName);
            targetField.AddToClassList(BuilderConstants.InspectorLocalStyleVariableEditingClassName);
            if (editingEnabled)
                targetField.RemoveFromClassList(BuilderConstants.ReadOnlyStyleClassName);
            else
                targetField.AddToClassList(BuilderConstants.ReadOnlyStyleClassName);

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

        void OnMouseDownEvent(MouseDownEvent e)
        {
            if (e.button != (int)MouseButton.LeftMouse)
                return;

            if (e.clickCount == 1 && (variableInfoTooltip == null || variableInfoTooltip.currentHandler != this) && !m_PopupTemporarilyDisabled && !isVariableFieldVisible)
            {
                m_ScheduledShowPopup = labelElement.schedule.Execute(() => ShowPopup(this)).StartingIn(BuilderConstants.DoubleClickDelay);
            }
            else if (e.clickCount == 2 && !m_EditingTemporarilyDisabled)
            {
                m_ScheduledShowPopup?.Pause();
                m_ScheduledShowPopup = null;
                ShowVariableField();
            }
        }

        public void RefreshField()
        {
            if (m_Row == null || m_Inspector == null)
                return;

            var cShartStyleName = BuilderInspectorStyleFields.ConvertUssStyleNameToCSharpStyleName(styleName);
            var property = BuilderInspectorStyleFields.GetStyleProperty(m_Inspector.currentRule, cShartStyleName);

            if (property != null)
            {
                if (property.IsVariable())
                {
                    targetField.AddToClassList(BuilderConstants.InspectorLocalStyleVariableClassName);
                }
                else
                {
                    targetField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleVariableClassName);
                }
            }
            else if (!string.IsNullOrEmpty(styleName))
            {
                bool found = false;
                var styleSheet = m_Inspector.styleSheet;
                var matchedRules = m_Inspector.matchingSelectors.matchedRulesExtractor.selectedElementRules;

                // Search for a variable in the best matching rule
                for (var i = matchedRules.Count - 1; i >= 0; --i)
                {
                    var matchRecord = matchedRules.ElementAt(i).matchRecord;
                    var ruleProperty = styleSheet.FindProperty(matchRecord.complexSelector.rule, styleName);

                    if (ruleProperty != null)
                    {
                        if (ruleProperty.IsVariable())
                        {
                            targetField.AddToClassList(BuilderConstants.InspectorLocalStyleVariableClassName);
                            found = true;
                        }

                        break;
                    }
                }

                if (!found)
                    targetField.RemoveFromClassList(BuilderConstants.InspectorLocalStyleVariableClassName);
            }
        }

        static string GetBoundVariableName(VariableEditingHandler handler)
        {
            var styleName = handler.targetField.GetProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName) as string;
            var property = BuilderInspectorStyleFields.GetStyleProperty(handler.m_Inspector.currentRule, styleName);
            string varName = null;

            // Find the name of the variable bound to the field
            if (property != null)
            {
                if (property.IsVariable())
                {
                    varName = handler.m_Inspector.styleSheet.ReadVariable(property);
                }
            }
            else if (!handler.editingEnabled)
            {
                var matchedRules = handler.m_Inspector.matchingSelectors.matchedRulesExtractor.selectedElementRules;

                for (var i = matchedRules.Count - 1; i >= 0; --i)
                {
                    var matchRecord = matchedRules.ElementAt(i).matchRecord;
                    var ruleProperty = matchRecord.sheet.FindProperty(matchRecord.complexSelector.rule, styleName);

                    if (ruleProperty != null)
                    {
                        if (ruleProperty.IsVariable())
                        {
                            varName = matchRecord.sheet.ReadVariable(ruleProperty);
                            break;
                        }
                    }
                }
            }

            return varName;
        }

        static void ShowPopup(VariableEditingHandler handler)
        {
            if (handler.m_Builder == null || handler.m_Inspector == null)
                return;

            string varName = GetBoundVariableName(handler);

            StyleSheet varStyleSheetOrigin = null;
            StyleComplexSelector varSourceSelectorOrigin = null;

            if (!string.IsNullOrEmpty(varName))
            {
                StyleVariableUtilities.FindVariableOrigin(handler.m_Inspector.currentVisualElement, varName, out varStyleSheetOrigin, out varSourceSelectorOrigin);
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
            handler.variableInfoTooltip.Show(handler, varName, varStyleSheetOrigin, varSourceSelectorOrigin);
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
            labelElement.schedule.Execute(() => m_PopupTemporarilyDisabled = false).ExecuteLater(s_InteractionDisableDelay);
        }

        void DisableEditingTemporarily()
        {
            if (m_EditingTemporarilyDisabled)
                return;
            m_EditingTemporarilyDisabled = true;
            labelElement.schedule.Execute(() => m_EditingTemporarilyDisabled = false).ExecuteLater(s_InteractionDisableDelay);
        }
    }
}
