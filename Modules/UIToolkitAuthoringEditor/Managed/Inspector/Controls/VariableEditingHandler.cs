// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Reflection;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal class VariableEditingHandler
{
    public static readonly string HiddenStyleClassName = "unity-inspector-hidden";
    public static readonly string ReadOnlyStyleClassName = "unity-ui-inspector--readonly";
    public static readonly string InspectorStylePropertyNameVEPropertyName = "__unity-ui-builder-style-property-name";
    public static readonly string InspectorLocalStyleVariableEditingClassName = "unity-ui-inspector__style--variable-editing";
    public static readonly string InspectorContainerClassName = "unity-ui-inspector__container";

    static readonly string s_LabelClassName = "unity-ui-inspector__variable-field-label";
    static readonly string s_PlaceholderText = "Variable";
    static readonly string s_ReadOnlyPlaceholderText = "None";

    static readonly int s_InteractionDisableDelay = 200;

    string m_InitialText;
    bool m_InitialEnabledState;
    bool m_ShowingStyleVariableField;
    IVariableEditingContext m_Context;
    VisualElement m_Row;
    string m_StyleName;
    bool m_PopupTemporarilyDisabled;
    bool m_EditingTemporarilyDisabled;
    VariableCompleter m_CompleterOnTarget;

    public int index { get; set; } = 0;
    public bool editingEnabled { get; set; } = true;

    public bool isVariableFieldVisible => variableField != null && !variableField.ClassListContains(HiddenStyleClassName);

    public VariableField variableField { get; private set; }
    public FieldSearchCompleter<VariableInfo> completer { get; private set; }

    public BindableElement targetField { get; private set; }

    public string styleName
    {
        get
        {
            if (m_StyleName == null)
            {
                if (m_Row is BindableElement bindableElement)
                    m_StyleName = bindableElement.bindingPath;

                if (string.IsNullOrEmpty(m_StyleName))
                    m_StyleName = targetField.bindingPath;
            }

            return m_StyleName;
        }

        set => m_StyleName = value;
    }

    public Label labelElement { get; private set; }

    public IVariableEditingContext context => m_Context;

    public VariableCompleter completerOnTarget => m_CompleterOnTarget;

    public VariableEditingHandler(BindableElement field, IVariableEditingContext context = null, VisualElement styleRow = null, bool attachCompleterOnTarget = false)
    {
        targetField = field;
        m_Context = context;
        m_Row = styleRow;

        if (attachCompleterOnTarget)
        {
            m_CompleterOnTarget = CreateCompleter();
            m_CompleterOnTarget.SetupCompleterField(targetField.Q<TextField>(), false);
        }

        var fieldLabel = targetField.Q<Label>(BaseField<int>.labelUssClassName);
        if (fieldLabel == null)
            return;

        fieldLabel.Add(labelElement);
        fieldLabel.AddToClassList(InspectorContainerClassName);
        fieldLabel.generateVisualContent = null;

        labelElement = new Label().WithClassList(s_LabelClassName).WithClassList(BaseField<int>.labelUssClassName);
        labelElement.RegisterValueChangedCallback(e => { e.StopImmediatePropagation(); });
        labelElement.text = fieldLabel.text;
    }

    public void SetContext(IVariableEditingContext context, VisualElement styleRow = null)
    {
        m_Context = context;
        if (styleRow != null)
            m_Row = styleRow;
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
        completer.AlwaysVisible = true;
        variableField.AddToClassList(HiddenStyleClassName);
        targetField.Add(variableField);

        var input = variableField.Q(TextField.textInputUssName);
        variableField.RegisterCallback<GeometryChangedEvent>((e) =>
        {
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
    }

    internal void SetVariable(string variableName)
    {
        if (m_Context == null)
            return;

        if (string.IsNullOrEmpty(variableName))
        {
            m_Context.UnsetVariable(targetField);
        }
        else
        {
            m_Context.SetVariable(variableName, targetField, styleName, index);
            m_Context.RefreshUI();
        }
    }

    public void ShowVariableField()
    {
        if (styleName == null || m_Context == null || isVariableFieldVisible)
            return;

        InitVariableField();

        var varName = GetBoundVariableName(this);

        variableField.IsReadOnly = !editingEnabled;
        variableField.placeholderText = editingEnabled ? s_PlaceholderText : s_ReadOnlyPlaceholderText;
        variableField.value = m_InitialText = varName;
        m_InitialEnabledState = targetField.enabledSelf;
        targetField.SetEnabled(true);

        var visualInput = GetVisualInput(targetField);

        visualInput?.AddToClassList(HiddenStyleClassName);
        m_ShowingStyleVariableField = true;
        variableField.RemoveFromClassList(HiddenStyleClassName);
        targetField.AddToClassList(InspectorLocalStyleVariableEditingClassName);
        if (editingEnabled)
            targetField.RemoveFromClassList(ReadOnlyStyleClassName);
        else
            targetField.AddToClassList(ReadOnlyStyleClassName);

        completer.EnsurePopupIsCreated();
        ShowPopup(this);
    }

    void HideStyleVariableField()
    {
        var visualInput = GetVisualInput(targetField);

        targetField.RemoveFromClassList(InspectorLocalStyleVariableEditingClassName);
        visualInput?.RemoveFromClassList(HiddenStyleClassName);
        variableField.AddToClassList(HiddenStyleClassName);
        DisablePopupTemporarily();
        DisableEditingTemporarily();
    }

    public StyleProperty GetStyleProperty()
    {
        var cSharpStyleName = StyleSheetUtility.ConvertUssNameToStyleName(styleName);
        return m_Context?.CurrentRule?.FindLastProperty(cSharpStyleName);
    }

    public void RefreshField()
    {
        if (styleName == null || m_Context == null || m_CompleterOnTarget == null)
            return;

        m_CompleterOnTarget.Enabled = true;
    }

    public static string GetBoundVariableName(VariableEditingHandler handler, bool getVariableFromMatchedRule = true)
    {
        if (handler.m_Context == null)
            return null;

        var styleName = handler.targetField.GetProperty(InspectorStylePropertyNameVEPropertyName) as string;

        var varName = handler.m_Context.GetBoundVariableNameFromCurrentRule(styleName, handler.index);

        if (varName == null && getVariableFromMatchedRule)
            varName = handler.m_Context.GetBoundVariableNameFromMatchedRules(styleName, handler.index);

        return varName;
    }

    static void ShowPopup(VariableEditingHandler handler)
    {
        if (handler.m_Context == null || handler.editingEnabled)
            return;

        handler.targetField.AddToClassList(InspectorLocalStyleVariableEditingClassName);
        if (handler.editingEnabled)
            handler.targetField.RemoveFromClassList(ReadOnlyStyleClassName);
        else
            handler.targetField.AddToClassList(ReadOnlyStyleClassName);
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

    static VisualElement GetVisualInput(VisualElement ve)
    {
        return ve.Q("unity-visual-input") ?? ve.Q(className: BaseField<int>.inputUssClassName);
    }
}
