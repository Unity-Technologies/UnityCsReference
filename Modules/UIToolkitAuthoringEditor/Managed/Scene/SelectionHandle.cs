// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

sealed class SelectionHandle : VisualElement
{
    const string k_VisualTreeAsset = "UIToolkitAuthoring/UIViewportWindow/SelectionHandle.uxml";
    const string k_StyleSheetDark = "UIToolkitAuthoring/UIViewportWindow/SelectionHandleDark.uss";
    const string k_StyleSheetLight = "UIToolkitAuthoring/UIViewportWindow/SelectionHandleLight.uss";

    public const string UssClass = "unity-selection-handle";
    public const string HeaderUssClass = UssClass + "__header";
    public const string HeaderTypeNameUssClass = HeaderUssClass + "__type-name";
    public const string OutlineUssClass = UssClass + "__outline";

    readonly VisualElement m_Header;
    readonly Label m_TypeName;
    readonly VisualElement m_Outline;

    VisualElement m_Target;

    public VisualElement Target
    {
        get => m_Target;
        set
        {
            if (m_Target == value)
                return;
            m_Target = value;
            m_TypeName.text = GetHeaderText(m_Target);
            SetLayoutFromTarget();
        }
    }

    public SelectionHandle()
    {
        AddToClassList(UssClass);
        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        if (vta)
            vta.CloneTree(this);

        var styleSheetPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
        var styleSheet = EditorGUIUtility.Load(styleSheetPath) as StyleSheet;
        if (styleSheet)
            styleSheets.Add(styleSheet);

        m_Header = this.Q(className: HeaderUssClass);
        m_TypeName = this.Q<Label>(className: HeaderTypeNameUssClass);
        m_Outline = this.Q(className: OutlineUssClass);
        RefreshBackgroundColors();
    }

    protected override void HandleEventBubbleUp(EventBase evt)
    {
        switch (evt)
        {
            case AttachToPanelEvent attachToPanelEvent:
                PrefSettings.settingChanged += OnPrefsChanged;
                RefreshBackgroundColors();
                break;
            case DetachFromPanelEvent detachFromPanelEvent:
                PrefSettings.settingChanged -= OnPrefsChanged;
                break;
        }

        base.HandleEventBubbleUp(evt);
    }

    void OnPrefsChanged(string prefName, Type prefType)
    {
        if (string.CompareOrdinal(ColorPreferences.SelectionOutlineColor, prefName) == 0)
        {
            RefreshBackgroundColors();
        }
    }

    void RefreshBackgroundColors()
    {
        m_Header.style.backgroundColor = ColorPreferences.SelectionOutline;
        m_Outline.SetInlineBorderColor(ColorPreferences.SelectionOutline);
    }

    public void SetLayoutFromTarget()
    {
        if (m_Target is { resourcesReleased: true })
            m_Target = null;

        if (m_Target == null)
        {
            style.left = StyleKeyword.Null;
            style.top = StyleKeyword.Null;
            style.width = StyleKeyword.Null;
            style.height = StyleKeyword.Null;
            style.position = StyleKeyword.Null;
        }
        else
        {
            var bounds = m_Target.worldBound;

            var pixelsPerPoint = ((Panel)m_Target.panel)?.pixelsPerPoint ?? 1.0f;
            style.left = bounds.x / pixelsPerPoint;
            style.top = bounds.y / pixelsPerPoint;
            style.width = bounds.width / pixelsPerPoint;
            style.height = bounds.height / pixelsPerPoint;
            style.position = Position.Absolute;
        }
    }

    static string GetHeaderText(VisualElement target)
    {
        if (target == null)
            return null;

        return string.IsNullOrEmpty(target.name)
            ? target.typeName
            : target.name;
    }
}
