// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// The <see cref="VisualElementEditingStage"/> as seen by the viewport: the stage owns the panel, the
/// document and the editing context, so this only forwards to it.
/// </summary>
sealed class StageViewportContext : IUIViewportContext
{
    readonly VisualElementEditingStage m_Stage;

    public StageViewportContext(VisualElementEditingStage stage)
    {
        m_Stage = stage;
    }

    public object Source => m_Stage;

    public PanelElement PanelElement => m_Stage ? m_Stage.PanelElement : null;

    public VisualTreeAsset RootVisualTreeAsset => m_Stage ? m_Stage.Context.RootVisualTreeAsset : null;

    public VisualTreeAsset EditedVisualTreeAsset => m_Stage ? m_Stage.EditedVisualTreeAsset : null;

    public PanelSettings PanelSettings => m_Stage ? m_Stage.Context.PanelSettings : null;

    public string HeaderTitle
    {
        get
        {
            var document = EditedVisualTreeAsset;
            return document ? document.name + ".uxml" : string.Empty;
        }
    }

    public string CanvasStorageKey => m_Stage ? $"CanvasSettings-{m_Stage.GetHashForStateStorage()}" : null;

    public bool IsValid => m_Stage != null && m_Stage.EditedVisualTreeAsset != null;

    // Entering the stage is itself the way to author a document.
    public bool AllowsAuthoring => true;

    // The stage created the panel when it opened and destroys it when it closes; the viewport only borrows it.
    public void Acquire()
    {
    }

    public void Release()
    {
    }

    public void RequestRefresh()
    {
        if (m_Stage)
            m_Stage.RequestRefresh();
    }

    // Nothing resolved means we cannot prove the drop is safe; refuse it.
    public bool WillCauseCircularDependency(VisualTreeAsset visualTreeAsset)
        => !m_Stage || m_Stage.Context.WillCauseCircularDependency(visualTreeAsset);

    // The stage history is the path to what is shown; every crumb but the last navigates back to its stage.
    public void PopulateBreadcrumbs(UIViewport viewport)
    {
        viewport.ClearBreadcrumbs();

        var history = StageNavigationManager.instance.stageHistory;
        for (var i = 0; i < history.Count; i++)
        {
            var stage = history[i];
            var content = stage.CreateHeaderContent();
            var icon = content.image as Texture2D;
            var label = content.text;

            var isCurrentStage = i == history.Count - 1;
            if (isCurrentStage)
            {
                viewport.PushBreadcrumb(label, icon);
            }
            else
            {
                viewport.PushBreadcrumb(label, icon, () => StageUtility.GoToStage(stage, false));
            }
        }
    }
}
