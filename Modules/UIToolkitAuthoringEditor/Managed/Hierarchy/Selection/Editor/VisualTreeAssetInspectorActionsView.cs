// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
partial class VisualTreeAssetInspectorActionsView : VisualElement
{
    public const string UssClass = "unity-visual-tree-asset-inspector-actions-view";
    public const string SelectVtaButtonUssClass = UssClass + "__select-button";
    public const string OpenVtaInContextButtonUssClass = UssClass + "__open-in-context-button";
    public const string OpenVtaInBuilderButtonUssClass = UssClass + "__open-in-builder-button";
    public const string HiddenOpenVtaButtonUssClass = OpenVtaInBuilderButtonUssClass + "--hidden";

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/VisualTreeAssetInspectorActionsView.uxml";

    private readonly Button m_SelectButton;
    private readonly Button m_OpenInContextButton;
    private readonly Button m_OpenInBuilderButton;

    private VisualTreeAsset m_VisualTreeAsset;
    private TemplateAsset[] m_SubDocumentPath;

    public PanelSettings PanelSettings { get; set; }

    public TemplateAsset[] SubDocumentPath
    {
        get => m_SubDocumentPath;
        set
        {
            if (m_SubDocumentPath == value)
                return;

            m_SubDocumentPath = value;
            UpdateControlStates();
        }
    }

    public VisualTreeAsset VisualTreeAsset
    {
        get => m_VisualTreeAsset;
        set
        {
            if (m_VisualTreeAsset == value)
                return;

            m_VisualTreeAsset = value;
            UpdateControlStates();
        }
    }

    public VisualTreeAssetInspectorActionsView()
    {
        AddToClassList(UssClass);

        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        vta.CloneTree(this);

        m_SelectButton = this.Q<Button>(className:SelectVtaButtonUssClass);
        m_SelectButton.AddToClassList(HiddenOpenVtaButtonUssClass);
        m_SelectButton.clicked += SelectAssetInProject;

        m_OpenInContextButton = this.Q<Button>(className:OpenVtaInContextButtonUssClass);
        m_OpenInContextButton.AddToClassList(HiddenOpenVtaButtonUssClass);
        m_OpenInContextButton.clicked += OpenInContext;

        m_OpenInBuilderButton = this.Q<Button>(className:OpenVtaInBuilderButtonUssClass);
        m_OpenInBuilderButton.AddToClassList(HiddenOpenVtaButtonUssClass);
        m_OpenInBuilderButton.clicked += OpenInUIBuilder;

        RegisterCallback<AttachToPanelEvent>(_ => UIToolkitAuthoringSettings.UIStagesChanged += OnUIStagesEnabledChanged);
        RegisterCallback<DetachFromPanelEvent>(_ => UIToolkitAuthoringSettings.UIStagesChanged -= OnUIStagesEnabledChanged);
    }

    void OnUIStagesEnabledChanged(bool _)
    {
        UpdateControlStates();
    }

    public void UpdateControlStates()
    {
        var assetPath = String.Empty;
        var isAssetPathValid = false;

        if (m_VisualTreeAsset)
        {
            assetPath = AssetDatabase.GetAssetPath(m_VisualTreeAsset.GetEntityId());
            isAssetPathValid = !string.IsNullOrEmpty(assetPath);
        }

        m_SelectButton.EnableInClassList(HiddenOpenVtaButtonUssClass, !isAssetPathValid);
        m_OpenInContextButton.EnableInClassList(HiddenOpenVtaButtonUssClass, !(UIToolkitAuthoringSettings.EnableUIStages && isAssetPathValid));
        m_OpenInBuilderButton.EnableInClassList(HiddenOpenVtaButtonUssClass, !isAssetPathValid);
    }

    private void SelectAssetInProject()
    {
        EditorGUIUtility.PingObject(VisualTreeAsset);
    }

    private void OpenInContext()
    {
        var options = m_SubDocumentPath is { Length: > 0 } ? SubDocumentOptions.InContext : SubDocumentOptions.None;
        var rootVisualTreeAsset = m_SubDocumentPath is { Length: > 0 } ? m_SubDocumentPath[0].visualTreeAsset : m_VisualTreeAsset;

        var context = new VisualTreeAssetEditingContext(
            rootVisualTreeAsset,
            m_SubDocumentPath,
            options,
            PanelSettings
        );

        var stage = ScriptableObject.CreateInstance<VisualElementEditingStage>();
        stage.SeparatorStyle = BreadcrumbBar.SeparatorStyle.Line;
        stage.SetContext(context);
        StageUtility.GoToStage(stage, false);
    }

    private void OpenInUIBuilder()
    {
        if (!m_VisualTreeAsset)
            return;

        AssetDatabase.OpenAsset(m_VisualTreeAsset.GetEntityId());
    }
}
