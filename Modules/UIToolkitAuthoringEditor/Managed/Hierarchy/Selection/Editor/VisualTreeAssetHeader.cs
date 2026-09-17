// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
partial class VisualTreeAssetHeader : UISelectionObjectHeader
{
    public static readonly BindingId VisualTreeAssetProperty = nameof(VisualTreeAsset);

    public new const string UssClass = "unity-visual-tree-asset-header";
    public const string AssetPathRowUssClass = UssClass + "__asset-path-row";
    public const string AssetPathUssClass = UssClass + "__asset-path";

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/VisualTreeAssetHeader.uxml";

    private VisualTreeAsset m_VisualTreeAsset;
    private ObjectField m_AssetPath;

    [UxmlAttribute, CreateProperty]
    public VisualTreeAsset VisualTreeAsset
    {
        get => m_VisualTreeAsset;
        set
        {
            if (m_VisualTreeAsset == value)
                return;
            m_VisualTreeAsset = value;

            RefreshAssetPath();
            NotifyPropertyChanged(VisualTreeAssetProperty);
        }
    }

    internal void RefreshAssetPath()
    {
        m_AssetPath.value = m_VisualTreeAsset;
        m_AssetPath.tooltip = m_VisualTreeAsset
            ? AssetDatabase.GetAssetPath(m_VisualTreeAsset.GetEntityId())
            : string.Empty;
    }

    protected override VisualTreeAsset IdentifierDetails => EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;

    public VisualTreeAssetHeader()
    {
        AddToClassList(UssClass);

        TypeIcon = UIResources.GetIconForType(typeof(TemplateContainer), UIResources.RequestSize.Px32);
        TypeName = nameof(VisualTreeAsset);

        m_AssetPath = this.Q<ObjectField>(className: AssetPathUssClass);
        m_AssetPath.objectType = typeof(VisualTreeAsset);
        m_AssetPath.allowSceneObjects = false;
        m_AssetPath.SetEnabled(false);

        RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
    }

    void OnAttachToPanel(AttachToPanelEvent evt) => EditorApplication.projectChanged += RefreshAssetPath;

    void OnDetachFromPanel(DetachFromPanelEvent evt) => EditorApplication.projectChanged -= RefreshAssetPath;
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
