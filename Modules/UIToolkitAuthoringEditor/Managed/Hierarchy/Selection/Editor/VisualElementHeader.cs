// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using Unity.Properties;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
partial class VisualElementHeader : UISelectionObjectHeader
{
    public static readonly BindingId ElementProperty = nameof(Element);

    public new const string UssClass = "unity-visual-element-header";
    public const string AssetPathContainerUssClass = "unity-visual-element-inspector__asset-path-container";
    public const string AssetPathTypeIconUssClass = "unity-visual-element-inspector__asset-path-type-icon";
    public const string AssetPathFieldUssClass = "unity-visual-element-inspector__asset-path-field";
    public const string AssetActionsViewUssClass = "unity-visual-element-inspector__asset-actions-view";

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/VisualElementHeader.uxml";
    private const string k_StyleSheet = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspector.uss";
    private const string k_StyleSheetDark = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorDark.uss";
    private const string k_StyleSheetLight = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorLight.uss";
    private const string k_NoAssetPath = "<none>.uxml";

    static StyleSheet s_StyleSheet;
    static StyleSheet s_ThemedStyleSheet;
    static bool s_ThemedStyleSheetIsProSkin;

    private VisualElement m_Element;
    UxmlAttributesView m_AttributesView;

    VisualElement m_AssetPathContainer;
    Image m_AssetPathTypeIcon;
    TextField m_AssetPathField;
    VisualTreeAssetInspectorActionsView m_AssetActionsView;

    public UxmlAttributesView AttributesView => m_AttributesView;
    public InspectorSearchField SearchField { get; }

    protected override VisualTreeAsset IdentifierDetails => EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;

    [CreateProperty]
    public VisualElement Element
    {
        get => m_Element;
        set
        {
            if (m_Element == value)
                return;
            m_Element = value;
            if (m_Element == null)
            {
                TypeIcon = UIResources.GetIconForType(typeof(VisualElement), UIResources.RequestSize.Px32);
                TypeName = nameof(VisualElement);
                m_AssetPathField.value = k_NoAssetPath;
                m_AssetPathField.tooltip = string.Empty;
                m_AssetActionsView.VisualTreeAsset = null;
                m_AssetActionsView.PanelSettings = null;
                m_AssetActionsView.SubDocumentPath = null;
            }
            else
            {
                TypeIcon = UIResources.GetIconForElement(m_Element, UIResources.RequestSize.Px32);
                TypeName = TypeUtility.GetTypeDisplayName(m_Element.GetType());

                var visualTreeAsset = m_Element.visualTreeAssetSource
                    ? m_Element.visualTreeAssetSource
                    : m_Element.GetFirstAncestorWhere(ve => ve.visualTreeAssetSource)?.visualTreeAssetSource;

                var assetPath = k_NoAssetPath;
                var assetPathToolTip = string.Empty;

                if (visualTreeAsset)
                {
                    var fullPath = AssetDatabase.GetAssetPath(visualTreeAsset.GetEntityId());
                    assetPath = Path.GetFileName(fullPath);
                    assetPathToolTip = fullPath;
                }

                m_AssetPathField.value = assetPath;
                m_AssetPathField.tooltip = assetPathToolTip;
                m_AssetActionsView.VisualTreeAsset = visualTreeAsset;
                using var _ = ListPool<TemplateAsset>.Get(out var templateAssetPath);
                m_Element.GenerateSubDocumentPath(templateAssetPath);
                m_AssetActionsView.SubDocumentPath = templateAssetPath.Count > 0
                    ? templateAssetPath.ToArray()
                    : Array.Empty<TemplateAsset>();
                m_AssetActionsView.PanelSettings = m_Element.GetPanelSettings();
            }
            NotifyPropertyChanged(ElementProperty);
        }
    }

    public void SetEditState(VisualElementEditFlags editFlags)
    {
        var enabled = editFlags == VisualElementEditFlags.FullyEditable;
        m_AttributesView?.SetEnabled(enabled);
        m_AssetPathContainer?.SetEnabled(enabled);
    }

    public void UpdateAssetVisibility(VisualElementEditFlags editFlags, bool isRecording = false, bool inStagingMode = false)
    {
        m_AssetActionsView.style.display = (!inStagingMode && (isRecording || editFlags == VisualElementEditFlags.None))
            ? DisplayStyle.Flex : DisplayStyle.None;
        m_AssetPathContainer.style.display = (editFlags == VisualElementEditFlags.None && !inStagingMode)
            ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public VisualElementHeader()
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

        TypeIcon = UIResources.GetIconForType(typeof(VisualElement), UIResources.RequestSize.Px32);
        TypeName = nameof(VisualElement);

        m_AttributesView = this.Q<UxmlAttributesView>();

        m_AssetPathContainer = new VisualElement();
        m_AssetPathContainer.AddToClassList(AssetPathContainerUssClass);

        var assetPathLabel = new Label("UXML");
        assetPathLabel.AddToClassList("unity-visual-element-inspector__asset-path-label");
        m_AssetPathContainer.Add(assetPathLabel);

        m_AssetPathTypeIcon = new Image();
        m_AssetPathTypeIcon.AddToClassList(AssetPathTypeIconUssClass);
        m_AssetPathTypeIcon.image = EditorGUIUtility.Load("VisualTreeAsset Icon") as Texture2D;
        m_AssetPathContainer.Add(m_AssetPathTypeIcon);

        m_AssetPathField = new TextField { value = k_NoAssetPath };
        m_AssetPathField.isReadOnly = true;
        m_AssetPathField.AddToClassList(AssetPathFieldUssClass);
        m_AssetPathContainer.Add(m_AssetPathField);

        Add(m_AssetPathContainer);

        m_AssetActionsView = new VisualTreeAssetInspectorActionsView();
        m_AssetActionsView.AddToClassList(AssetActionsViewUssClass);
        Add(m_AssetActionsView);

        SearchField = new InspectorSearchField();
        Add(SearchField);
    }
}
