// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using Unity.Properties;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
partial class VisualTreeAssetHeader : UISelectionObjectHeader
{
    public static readonly BindingId VisualTreeAssetProperty = nameof(VisualTreeAsset);

    public new const string UssClass = "unity-visual-tree-asset-header";
    public const string AssetPathUssClass = UssClass + "__asset-path";

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/VisualTreeAssetHeader.uxml";
    private const string k_NoAssetPath = "<none>.uxml";

    private VisualTreeAsset m_VisualTreeAsset;
    private TextField m_AssetPath;

    [UxmlAttribute, CreateProperty]
    public VisualTreeAsset VisualTreeAsset
    {
        get => m_VisualTreeAsset;
        set
        {
            if (m_VisualTreeAsset == value)
                return;
            m_VisualTreeAsset = value;

            var path = k_NoAssetPath;
            var toolTip = k_NoAssetPath;

            if (m_VisualTreeAsset)
            {
                var fullPath = AssetDatabase.GetAssetPath(m_VisualTreeAsset.GetEntityId());
                path = Path.GetFileName(fullPath);
                toolTip = fullPath;
            }

            m_AssetPath.value = path;
            m_AssetPath.tooltip = toolTip;
            NotifyPropertyChanged(VisualTreeAssetProperty);
        }
    }

    protected override VisualTreeAsset IdentifierDetails => EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;

    public VisualTreeAssetHeader()
    {
        AddToClassList(UssClass);

        TypeIcon = UIResources.GetIconForType(typeof(TemplateContainer), UIResources.RequestSize.Px32);
        TypeName = nameof(VisualTreeAsset);

        m_AssetPath = this.Q<TextField>(className: AssetPathUssClass);
        m_AssetPath.value = k_NoAssetPath;
        m_AssetPath.isReadOnly = true;
    }
}
