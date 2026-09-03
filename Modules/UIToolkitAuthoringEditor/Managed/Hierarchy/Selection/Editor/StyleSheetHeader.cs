// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
using Unity.Properties;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
partial class StyleSheetHeader : UISelectionObjectHeader
{
    public static readonly BindingId StyleSheetProperty = nameof(StyleSheet);
    public static readonly BindingId StyleSheetNameProperty = nameof(StyleSheetName);

    public new const string UssClass = "unity-stylesheet-header";
    public const string StyleSheetNameUssClass = UssClass + "__stylesheet-name";

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/StyleSheetHeader.uxml";
    private const string k_StyleSheet = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspector.uss";
    private const string k_StyleSheetDark = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorDark.uss";
    private const string k_StyleSheetLight = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorLight.uss";

    [AutoStaticsCleanupOnCodeReload]
    static StyleSheet s_StyleSheet;
    [AutoStaticsCleanupOnCodeReload]
    static StyleSheet s_ThemedStyleSheet;
    [AutoStaticsCleanupOnCodeReload]
    static bool s_ThemedStyleSheetIsProSkin;

    private TextField m_StyleSheetName;
    private StyleSheet m_StyleSheet;

    protected override VisualTreeAsset IdentifierDetails => EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;

    [UxmlAttribute, CreateProperty]
    public string StyleSheetName
    {
        get => m_StyleSheetName.value;
        set
        {
            if (string.CompareOrdinal(m_StyleSheetName.value, value) == 0)
                return;
            m_StyleSheetName.value = value;
            NotifyPropertyChanged(StyleSheetNameProperty);
        }
    }

    [CreateProperty]
    public StyleSheet StyleSheet
    {
        get => m_StyleSheet;
        set
        {
            if (m_StyleSheet == value)
                return;
            m_StyleSheet = value;

            m_StyleSheetName.dataSource = StyleSheet;

            TypeName = nameof(StyleSheet);
            TypeIcon = EditorGUIUtility.Load("StyleSheet Icon") as Texture2D;

            if (m_StyleSheet == null)
            {
                StyleSheetName = null;
                m_StyleSheetName.value = null;
            }
            else
            {
                StyleSheetName = m_StyleSheet.name;
            }
            NotifyPropertyChanged(StyleSheetProperty);
        }
    }

    public StyleSheetHeader()
    {
        AddToClassList(UssClass);
        if (s_StyleSheet == null)
            s_StyleSheet = EditorGUIUtility.Load(k_StyleSheet) as StyleSheet;
        if (s_ThemedStyleSheet == null || s_ThemedStyleSheetIsProSkin != EditorGUIUtility.isProSkin)
        {
            s_ThemedStyleSheet = EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight) as StyleSheet;
            s_ThemedStyleSheetIsProSkin = EditorGUIUtility.isProSkin;
        }
        styleSheets.Add(s_StyleSheet);
        styleSheets.Add(s_ThemedStyleSheet);

        TypeIcon = EditorGUIUtility.Load("StyleSheet Icon") as Texture2D;
        TypeName = nameof(StyleSheet);

        m_StyleSheetName = this.Q<TextField>(className: StyleSheetNameUssClass);
        m_StyleSheetName.SetEnabled(false);
        m_StyleSheetName.dataSource = this;
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
