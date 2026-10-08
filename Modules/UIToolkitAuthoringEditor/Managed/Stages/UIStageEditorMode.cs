// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Unity.UIToolkit.Editor;

// A stage with no scene has nowhere to put a GameObject, so while one is current the editor runs in a
// mode whose menu bar leaves the GameObject creation entries out entirely (UUM-152231).
static partial class UIStageEditorMode
{
    const string k_ModeId = "uitoolkitauthoring";
    const string k_DefaultModeId = "default";

    // The mode to go back to, kept in session state so it survives a code reload while the stage is open.
    internal const string k_ModeBeforeStageKey = "UIStageEditorMode.ModeBeforeStage";

    [AutoStaticsCleanupOnCodeReload] // the subscription is lost with the reload too; Enter redoes both
    static bool s_WatchingModeChanges;

    [OnCodeInitializing]
    static void Initialize()
    {
        // Once per navigation, after it settles, rather than once per stage walked through.
        UIStageNavigation.StageSettled += OnStageSettled;
    }

    // The mode is not remembered across a code reload. MenuItemGenerator calls this once the element entries
    // are registered again; rebuilding before that would empty the GameObject root and move it in the bar.
    public static void ApplyToCurrentStage()
    {
        OnStageSettled(StageUtility.GetCurrentStage());
    }

    static void OnStageSettled(Stage stage)
    {
        if (stage != null && !StageHasGameObjects(stage))
            Enter();
        else
            Exit();
    }

    static void Enter()
    {
        if (ModeService.currentId != k_ModeId)
        {
            // After a reload the current mode is the fallback, not the one the user had, so keep the first record.
            if (string.IsNullOrEmpty(SessionState.GetString(k_ModeBeforeStageKey, string.Empty)))
                SessionState.SetString(k_ModeBeforeStageKey, ModeService.currentId);

            ModeService.ChangeModeById(k_ModeId);
        }

        // Subscribed after our own change so the handler only ever sees someone else's.
        if (!s_WatchingModeChanges)
        {
            ModeService.modeChanged += OnModeChanged;
            s_WatchingModeChanges = true;
        }
    }

    static void Exit()
    {
        // Unsubscribed before the restore, or it would be taken for someone else's change and re-entered.
        if (s_WatchingModeChanges)
        {
            ModeService.modeChanged -= OnModeChanged;
            s_WatchingModeChanges = false;
        }

        if (ModeService.currentId == k_ModeId)
        {
            ModeService.ChangeModeById(SessionState.GetString(k_ModeBeforeStageKey, k_DefaultModeId));

            // ChangeModeById ignores an id that no longer exists, e.g. a package mode removed meanwhile.
            if (ModeService.currentId == k_ModeId)
                ModeService.ChangeModeById(k_DefaultModeId);
        }

        SessionState.EraseString(k_ModeBeforeStageKey);
    }

    // Reset All Layouts and the like switch the mode from under the stage; put it back after they finish.
    static void OnModeChanged(ModeService.ModeChangedArgs args)
    {
        if (ModeService.currentId == k_ModeId)
            return;

        SessionState.SetString(k_ModeBeforeStageKey, ModeService.currentId);
        EditorApplication.delayCall += ApplyToCurrentStage;
    }

    // Only the scenes a stage exposes count: a stage may hold a hidden preview scene for its own bookkeeping.
    static bool StageHasGameObjects(Stage stage)
    {
        if (stage is MainStage)
            return true;
        if (stage is not PreviewSceneStage previewStage)
            return false;

        for (int i = 0; i < previewStage.sceneCount; ++i)
        {
            if (previewStage.GetSceneAt(i).IsValid())
                return true;
        }

        return false;
    }
}
