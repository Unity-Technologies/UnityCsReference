// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Properties;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class StyleSheetInspector : VisualElement
{
    [Serializable]
    public new class UxmlSerializedData : VisualElement.UxmlSerializedData
    {
        /// <summary>
        /// This is used by the code generator when a custom control is using the <see cref="UxmlElementAttribute"/>. You should not need to call it.
        /// </summary>
        [Conditional("UNITY_EDITOR"), RegisterUxmlCache]
        public new static void Register()
        {
            UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), Array.Empty<UxmlAttributeNames>(), true);
        }

        public override object CreateInstance()
        {
            return new StyleSheetInspector();
        }
    }

    public static readonly BindingId StyleSheetProperty = nameof(StyleSheet);

    private NewSelectorField m_NewSelectorField;

    public const string UssClass = "unity-stylesheet-inspector";

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/StyleSheet/StyleSheetInspector.uxml";
    private const string k_StyleSheetDark = "UIToolkitAuthoring/Inspector/StyleSheet/StyleSheetInspectorDark.uss";
    private const string k_StyleSheetLight = "UIToolkitAuthoring/Inspector/StyleSheet/StyleSheetInspectorLight.uss";
    private const string k_UIToolkitAuthoringInspectorStyleSheetDark = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorDark.uss";
    private const string k_UIToolkitAuthoringInspectorStyleSheetLight = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorLight.uss";

    private StyleSheet m_StyleSheet;

    [CreateProperty]
    public StyleSheet StyleSheet
    {
        get => m_StyleSheet;
        set
        {
            if (m_StyleSheet == value)
                return;
            m_StyleSheet = value;

            NotifyPropertyChanged(StyleSheetProperty);
        }
    }

    public StyleSheetInspector()
    {
        AddToClassList(UssClass);

        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        vta.CloneTree(this);

        var styleSheetPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
        var styleSheet = EditorGUIUtility.Load(styleSheetPath) as StyleSheet;
        styleSheets.Add(styleSheet);

        var authoringInspectorStyleSheetPath = EditorGUIUtility.isProSkin ? k_UIToolkitAuthoringInspectorStyleSheetDark : k_UIToolkitAuthoringInspectorStyleSheetLight;
        var authoringInspectorStyleSheet = EditorGUIUtility.Load(authoringInspectorStyleSheetPath) as StyleSheet;
        styleSheets.Add(authoringInspectorStyleSheet);

        m_NewSelectorField = this.Q<NewSelectorField>("new-selector-field");
        m_NewSelectorField.RegisterCallback<NewSelectorSubmitEvent>(OnCreateNewSelector);
    }

    void OnCreateNewSelector(NewSelectorSubmitEvent evt)
    {
        // TODO: use command when UI-4401 lands
    }
}
