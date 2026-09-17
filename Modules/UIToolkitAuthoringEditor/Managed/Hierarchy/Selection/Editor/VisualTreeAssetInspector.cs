// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
internal sealed partial class VisualTreeAssetInspector : VisualElement
{
    public static readonly BindingId VisualTreeAssetProperty = nameof(VisualTreeAsset);
    public static readonly BindingId PanelSettingsProperty = nameof(PanelSettings);

    public const string UssClass = "unity-visual-tree-asset-inspector";
    public const string HeaderUssClass = UssClass + "__header";
    public const string AssetActionsViewUssClass = UssClass + "__asset-actions-view";

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/VisualTreeAssetInspector.uxml";

    private VisualTreeAsset m_VisualTreeAsset;
    private PanelSettings m_PanelSettings;

    private readonly VisualTreeAssetHeader m_Header;
    private readonly VisualTreeAssetInspectorActionsView m_AssetActionsView;

    [CreateProperty]
    public VisualTreeAsset VisualTreeAsset
    {
        get => m_VisualTreeAsset;
        set
        {
            if (m_VisualTreeAsset == value)
                return;
            m_VisualTreeAsset = value;

            m_Header.VisualTreeAsset = m_VisualTreeAsset;
            m_AssetActionsView.VisualTreeAsset = m_VisualTreeAsset;
            NotifyPropertyChanged(VisualTreeAssetProperty);
        }
    }

    [CreateProperty]
    public PanelSettings PanelSettings
    {
        get => m_PanelSettings;
        set
        {
            if (m_PanelSettings == value)
                return;
            m_PanelSettings = value;

            m_AssetActionsView.PanelSettings = m_PanelSettings;
            NotifyPropertyChanged(PanelSettingsProperty);
        }
    }

    public VisualTreeAssetInspector()
    {
        AddToClassList(UssClass);

        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        vta.CloneTree(this);

        m_Header = this.Q<VisualTreeAssetHeader>(className: HeaderUssClass);

        // The header is display-only, but the actions menu lives inside it and must stay
        // clickable, so the header cannot be disabled wholesale.
        m_Header.Q(className: UISelectionObjectHeader.ObjectTypeIconUssClass)?.SetEnabled(false);
        m_Header.Q(className: UISelectionObjectHeader.ObjectTypeNameUssClass)?.SetEnabled(false);

        m_AssetActionsView = new VisualTreeAssetInspectorActionsView();
        m_AssetActionsView.AddToClassList(AssetActionsViewUssClass);
        m_Header.Q(className: VisualTreeAssetHeader.AssetPathRowUssClass).Add(m_AssetActionsView);
    }
}
