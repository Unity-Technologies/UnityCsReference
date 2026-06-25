// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
sealed class NewClassWindow : EditorWindow
{
    const string k_WindowTitle = "Add Class";
    const float k_Width = 285f;
    const float k_Height = 110f;

    static NewClassWindow s_Window;

    TextField m_ClassField;
    HelpBox m_WarningBox;
    Action<string> m_OnConfirmed;

    internal TextField classField => m_ClassField;
    internal HelpBox warningBox => m_WarningBox;
    internal void Submit() => OnSubmit();

    public static NewClassWindow Open(Rect anchorScreenRect, Action<string> onConfirmed)
    {
        if (s_Window != null)
            s_Window.Close();

        var windowSize = new Vector2(k_Width, k_Height);
        s_Window = GetWindow<NewClassWindow>(true, k_WindowTitle);
        s_Window.m_OnConfirmed = onConfirmed;
        s_Window.position = new Rect(anchorScreenRect.position, windowSize);
        s_Window.minSize = windowSize;
        return s_Window;
    }

    void OnEnable()
    {
        AssemblyReloadEvents.beforeAssemblyReload += Close;
    }

    void OnDisable()
    {
        AssemblyReloadEvents.beforeAssemblyReload -= Close;
        s_Window = null;
    }

    void CreateGUI()
    {
        var container = new VisualElement();
        container.style.paddingTop = 8;
        container.style.paddingBottom = 8;
        container.style.paddingLeft = 8;
        container.style.paddingRight = 8;

        m_ClassField = new TextField(L10n.Tr("Class Name:"));
        container.Add(m_ClassField);

        var buttonRow = new VisualElement();
        buttonRow.style.flexDirection = FlexDirection.RowReverse;
        buttonRow.style.marginTop = 8;

        var okButton = new Button(OnSubmit) { text = L10n.Tr("OK") };
        var cancelButton = new Button(Close) { text = L10n.Tr("Cancel") };
        buttonRow.Add(okButton);
        buttonRow.Add(cancelButton);
        container.Add(buttonRow);

        rootVisualElement.Add(container);

        m_ClassField.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
                OnSubmit();
        });

        m_ClassField.schedule.Execute(() => m_ClassField.Focus());
    }

    void OnSubmit()
    {
        var className = m_ClassField.value?.Trim().TrimStart('.');
        if (string.IsNullOrEmpty(className))
            return;

        var error = StyleSheetAssetUtilities.GetClassNameValidationError(className);
        if (!string.IsNullOrEmpty(error))
        {
            ShowError(error);
            return;
        }

        m_OnConfirmed?.Invoke(className);
        Close();
    }

    void ShowError(string message)
    {
        if (m_WarningBox == null)
        {
            m_WarningBox = new HelpBox(message, HelpBoxMessageType.Warning);
            rootVisualElement[0].Insert(1, m_WarningBox);
        }
        else
        {
            m_WarningBox.text = message;
        }
    }
}
