// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.PackageManager.UI.Internal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor;

internal class InstanceStatusElement : VisualElement
{
    internal const string k_InstanceFoldoutClass = "unity-instance-status__foldout";
    internal const string k_InstanceFoldoutTitleBarClass = "unity-instance-status__title-bar";
    internal const string k_InstanceFoldoutTitleCheckmarkClass = "unity-instance-status__title-checkmark";
    internal const string k_InstanceFoldoutTitleContentClass = "unity-instance-status__title-content";
    internal const string k_InstanceFoldoutTitleIconClass = "unity-instance-status__title-icon";
    internal const string k_InstanceFoldoutTitleLabelClass = "unity-instance-status__title-label";
    internal const string k_InstanceFoldoutTitleCustomClass = "unity-instance-status__title-custom";
    internal const string k_InstanceFoldoutTitleStatusClass = "unity-instance-status__title-status";
    internal const string k_InstanceFoldoutTitleStatusIconClass = "unity-instance-status__title-status-icon";
    internal const string k_InstanceFoldoutTitleFreeRunIconClass = "unity-instance-status__title-free-run-icon";
    internal const string k_InstanceFoldoutTitleFreeRunIconActiveClass = "unity-instance-status__title-free-run-icon--active";
    internal const string k_InstanceFoldoutTitleFreeRunIconInactiveClass = "unity-instance-status__title-free-run-icon--inactive";
    internal const string k_InstanceFoldoutTitleStatusLabelClass = "unity-instance-status__title-status-label";
    internal const string k_InstanceFoldoutContentClass = "unity-instance-status__content";
    internal const string k_InstanceInspectorClass = "unity-instance-status__inspector";
    internal const string k_InstanceFoldoutTitleDriftIconClass = "unity-instance-status__title-drift-icon";
    internal const string k_DriftToolTip = "This instance might be drifting. This is caused by running an instance for a long time while possible changes were detected in the Main Editor. Consider exiting and restarting the instance.";

    Instance m_Instance;
    VisualElement m_FreeRunIcon;
    VisualElement m_StatusIcon;
    Label m_StatusLabel;
    VisualElement m_DriftIcon;

    internal InstanceStatusElement(Instance instance)
    {
        m_Instance = instance;

        RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

        var foldout = new Foldout();
        foldout.AddToClassList(k_InstanceFoldoutClass);

        var titlebar = foldout.Q<Toggle>();
        titlebar.AddToClassList(k_InstanceFoldoutTitleBarClass);
        titlebar.ElementAt(0).AddToClassList(k_InstanceFoldoutTitleCheckmarkClass);
        titlebar.Add(CreateTitleContent(instance));

        var content = foldout.Q("unity-content");
        content.AddToClassList(k_InstanceFoldoutContentClass);

        foldout.Add(CreateContent(instance));
        Add(foldout);

        RefreshStatus(instance, instance.StatusData);
    }

    void OnAttachToPanel(AttachToPanelEvent e)
    {
        m_Instance.StatusRefreshed += RefreshStatus;
    }

    void OnDetachFromPanel(DetachFromPanelEvent e)
    {
        m_Instance.StatusRefreshed -= RefreshStatus;
    }

    VisualElement CreateTitleContent(Instance instance)
    {
        var titleContent = new VisualElement();
        titleContent.AddToClassList(k_InstanceFoldoutTitleContentClass);

        var icon = new VisualElement();
        icon.AddToClassList(k_InstanceFoldoutTitleIconClass);
        icon.style.backgroundImage = GetIconTextureForInstance(instance);

        var label = new Label(instance.Name);
        label.AddToClassList(k_InstanceFoldoutTitleLabelClass);

        titleContent.Add(icon);
        titleContent.Add(label);
        titleContent.Add(CreateTitleBarCustomUI(instance));
        titleContent.Add(CreateStatusElement());

        return titleContent;
    }

    static Texture2D GetIconTextureForInstance(Instance instance)
    {
        // var instanceDescription = instance.GetInstanceDescription();
        Texture2D iconTexture = null;
        var controller = instance.Controller;
        if (controller is MainEditorController || controller is CloneEditorController)
            iconTexture = EditorGUIUtility.FindTexture("UnityLogo");
        else if (controller is LocalPlayerController localPlayerController)
            iconTexture = InternalUtilities.GetBuildProfileTypeIcon(localPlayerController.Settings.BuildProfile);
        return iconTexture;
    }

    VisualElement CreateStatusElement()
    {
        var container = new VisualElement();
        container.AddToClassList(k_InstanceFoldoutTitleStatusClass);

        m_StatusLabel = new Label();
        m_StatusLabel.AddToClassList(k_InstanceFoldoutTitleStatusLabelClass);
        m_StatusIcon = new VisualElement();
        m_StatusIcon.AddToClassList(k_InstanceFoldoutTitleStatusIconClass);
        m_FreeRunIcon = new VisualElement();
        m_FreeRunIcon.AddToClassList(k_InstanceFoldoutTitleFreeRunIconClass);
        m_DriftIcon = new VisualElement();
        m_DriftIcon.AddToClassList(k_InstanceFoldoutTitleDriftIconClass);
        m_DriftIcon.style.backgroundImage = Icons.GetImage(Icons.ImageName.Drift);
        m_DriftIcon.tooltip = k_DriftToolTip;



        container.Add(m_DriftIcon);
        container.Add(m_FreeRunIcon);
        container.Add(m_StatusIcon);
        container.Add(m_StatusLabel);

        return container;
    }

    VisualElement CreateTitleBarCustomUI(Instance instance)
    {
        var container = new VisualElement();
        container.AddToClassList(k_InstanceFoldoutTitleCustomClass);
        container.Add(instance.Controller.CreateTitleBarUI(instance));

        foreach (var decorator in instance.DecoratorsControllers)
        {
            container.Add(decorator.CreateTitleBarUI(instance));
        }

        return container;
    }

    void RefreshStatus(Instance instance, InstanceStatusData status)
    {
        ComputeStatusValues(status, out var state, out var stage, out var stageProgress, out var totalProgress);

        m_StatusIcon.style.backgroundImage = Icons.GetImage(state switch
        {
            ExecutionState.Idle => Icons.ImageName.Idle,
            ExecutionState.Running => status.IsExecutingRunningStage() ? Icons.ImageName.CompletedTask : Icons.ImageName.Loading,
            ExecutionState.Completed => Icons.ImageName.Idle,
            ExecutionState.Aborted => Icons.ImageName.Warning,
            ExecutionState.Failed => Icons.ImageName.Error,
            ExecutionState.Invalid => Icons.ImageName.Error,
            _ => Icons.ImageName.Error
        });

        m_FreeRunIcon.EnableClassToggle(k_InstanceFoldoutTitleFreeRunIconActiveClass, k_InstanceFoldoutTitleFreeRunIconInactiveClass, m_Instance.IsFreeRunMode());
        m_DriftIcon.visible = instance.Drifted;

        if (state is ExecutionState.Idle or ExecutionState.Completed)
        {
            m_StatusLabel.text = "Idle";
            return;
        }

        if (status.IsExecutingRunningStage())
        {
            m_StatusLabel.text = $"Running";
            return;
        }

        m_StatusLabel.text = $"{LaunchingScenarioWindow.GetLabelForStage(stage)}... {stageProgress:P0}";
    }

    void ComputeStatusValues(InstanceStatusData status, out ExecutionState state, out ExecutionStage stage, out float stageProgress, out float totalProgress)
    {
        state = status.OverallStatus.State;

        if (state is ExecutionState.Invalid)
        {
            stage = default;
            stageProgress = 0;
            totalProgress = 0;
            return;
        }

        totalProgress = status.OverallStatus.ProgressSum / Mathf.Max(1, status.OverallStatus.NodesCount);
        stage = status.CurrentStage;
        stageProgress = status.StageStatuses[(int)stage].ProgressSum / Mathf.Max(1, status.StageStatuses[(int)stage].NodesCount);
    }

    VisualElement CreateContent(Instance instance)
    {
        var container = new VisualElement();
        container.AddToClassList(k_InstanceInspectorClass);
        container.AddToClassList("unity-inspector-element");
        container.Add(instance.Controller.CreateControllerUI(instance));
        foreach (var decorator in instance.DecoratorsControllers)
        {
            container.Add(decorator.CreateControllerUI(instance));
        }
        return container;
    }
}
