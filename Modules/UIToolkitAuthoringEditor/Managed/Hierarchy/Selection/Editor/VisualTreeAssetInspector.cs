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
    public readonly static string AssetNotEditableMessageWhenUIStagesEnabled = L10n.Tr("UI elements are not yet editable in this view. To edit, open in context with UI Staging Mode or open the asset in the UI Builder.");
    public readonly static string AssetNotEditableMessageWhenUIStagesDisabled = L10n.Tr("UI elements are not yet editable in this view. To edit, open the asset in the UI Builder.");
    public static string AssetNotEditableMessage => UIToolkitAuthoringSettings.EnableUIStages ? AssetNotEditableMessageWhenUIStagesEnabled : AssetNotEditableMessageWhenUIStagesDisabled;

    public static readonly BindingId VisualTreeAssetProperty = nameof(VisualTreeAsset);

    public const string UssClass = "unity-visual-tree-asset-inspector";
    public const string HeaderUssClass = UssClass + "__header";
    public const string AssetNotEditableHelpBoxUssClass = UssClass + "__not-editable-help-box";
    public const string AssetActionsViewUssClass = UssClass + "__asset-actions-view";

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/VisualTreeAssetInspector.uxml";

    private VisualTreeAsset m_VisualTreeAsset;

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

    public VisualTreeAssetInspector()
    {
        AddToClassList(UssClass);

        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        vta.CloneTree(this);

        m_Header = this.Q<VisualTreeAssetHeader>(className: HeaderUssClass);
        m_Header.SetEnabled(false);
        m_AssetActionsView = this.Q<VisualTreeAssetInspectorActionsView>(className: AssetActionsViewUssClass);
    }
}
