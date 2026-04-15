// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal class VariableField : VisualElement, INotifyValueChanged<string>
{
    static readonly string s_UssClassName = "unity-variable-field";
    static readonly string s_PlaceholderLabelClassName = s_UssClassName + "__placeholder-label";

    const string k_StyleSheet = "UIToolkitAuthoring/Inspector/Controls/VariableEditing.uss";
    const string k_StyleSheetDark = "UIToolkitAuthoring/Inspector/Controls/VariableEditingDark.uss";
    const string k_StyleSheetLight = "UIToolkitAuthoring/Inspector/Controls/VariableEditingLight.uss";

    TextField m_Field;
    Label m_PlaceholderLabel;
    string m_Value;

    public TextField TextField => m_Field;

    public bool IsReadOnly
    {
        get => m_Field.isReadOnly;
        set => m_Field.isReadOnly = value;
    }

    public string value
    {
        get => m_Value;
        set => SetValue(value, true);
    }

    public void SetValueWithoutNotify(string value)
    {
        SetValue(value, false);
    }

    void SetValue(string value, bool notify)
    {
        string cleanValue = VariableNameUtilities.GetCleanVariableName(value);

        if (m_Value == cleanValue)
            return;

        var oldValue = m_Value;
        m_Value = cleanValue;

        if (panel != null && notify)
        {
            using (var evt = ChangeEvent<string>.GetPooled(oldValue, cleanValue))
            {
                evt.elementTarget = this;
                SendEvent(evt);
            }
        }
        m_Field.SetValueWithoutNotify(m_Value);
        UpdatePlaceholderLabelVisibility();
    }

    public string placeholderText
    {
        get => m_PlaceholderLabel.text;
        set
        {
            if (placeholderText == value)
                return;
            m_PlaceholderLabel.text = value;
            UpdatePlaceholderLabelVisibility();
        }
    }

    public VariableField()
    {
        m_Field = new TextField();

        AddToClassList(s_UssClassName);

        styleSheets.Add(EditorGUIUtility.Load(k_StyleSheet) as StyleSheet);
        var themeUssPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
        styleSheets.Add(EditorGUIUtility.Load(themeUssPath) as StyleSheet);

        m_PlaceholderLabel = new Label();
        m_PlaceholderLabel.pickingMode = PickingMode.Ignore;
        m_PlaceholderLabel.AddToClassList(s_PlaceholderLabelClassName);

        Add(m_Field);
        Add(m_PlaceholderLabel);

        m_Field.Q(TextField.textInputUssName).RegisterCallback<BlurEvent>(e =>
        {
            value = m_Field?.value?.Trim();
        }, TrickleDown.TrickleDown);

        m_Field.RegisterValueChangedCallback<string>(e =>
        {
            UpdatePlaceholderLabelVisibility();
            e.StopImmediatePropagation();
        });
        m_PlaceholderLabel.RegisterValueChangedCallback<string>(e =>
        {
            e.StopImmediatePropagation();
        });
    }

    void UpdatePlaceholderLabelVisibility()
    {
        m_PlaceholderLabel.visible = string.IsNullOrEmpty(m_Field.value);
    }
}
