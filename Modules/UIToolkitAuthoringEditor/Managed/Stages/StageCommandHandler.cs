// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.SceneManagement;

namespace Unity.UIToolkit.Editor;

static partial class StageCommandHandler
{
    [NoAutoStaticsCleanup]
    static CommandCategory s_PendingChanges = CommandCategory.None;

    [OnCodeLoaded]
    static void Initialize()
    {
        UICommandQueue.GroupBegan += OnCommandGroupBegan;
        UICommandQueue.GroupEnded += OnCommandGroupEnded;

        UICommandQueue.RegisterHandlerForCategory(~CommandCategory.None, ProcessCommand);
    }

    [OnCodeUnloading]
    static void Dispose()
    {
        UICommandQueue.GroupBegan -= OnCommandGroupBegan;
        UICommandQueue.GroupEnded -= OnCommandGroupEnded;

        UICommandQueue.UnregisterHandlerForCategory(~CommandCategory.None, ProcessCommand);
    }

    static void OnCommandGroupBegan(string undoName)
    {
        s_PendingChanges = CommandCategory.None;
    }

    static void OnCommandGroupEnded(in GroupEndedContext context)
    {
        FlushPendingChanges(context.UndoObjects);
    }

    static void ProcessCommand(in CommandContext context)
    {
        if (context.Status != CommandExecutionStatus.Success)
            return;

        s_PendingChanges |= context.Command.Category;
    }

    // Pushes what a finished command group changed to the views that only learn of it through a re-clone: the
    // scene panels, and — for a hierarchy change — the UI Stage's authoring panel, where asset-only edits
    // (paste, duplicate, template drops, …) would otherwise never appear in the live tree or the hierarchy.
    static void FlushPendingChanges(IReadOnlyCollection<UnityEngine.Object> changedAssets = null)
    {
        var changes = s_PendingChanges;
        s_PendingChanges = CommandCategory.None;

        // The scene panels do not live reload their documents while Main Stage authoring is enabled, so a
        // change made here (in any stage, and in the UI Builder) has to be pushed to the views of it that
        // nothing wrote to directly. That push is deferred, so it never runs inside the change that caused it.
        UIAssetRegistrySceneTracking.ReloadScenePanelsAfterChanges(changes, changedAssets);

        if ((changes & CommandCategory.Hierarchy) != CommandCategory.Hierarchy)
            return;

        if (StageUtility.GetCurrentStage() is VisualElementEditingStage uiStage)
            uiStage.RequestRefresh();
    }
}
