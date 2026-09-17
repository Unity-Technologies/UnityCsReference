// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// The stage transitions as the UI authoring tools see them. Mirrors
/// <see cref="StageNavigationManager"/>'s events, except that a navigation which walks through several stages
/// to reach its destination reports as the single transition the user asked for.
/// </summary>
[InitializeOnLoad]
static class UIStageNavigation
{
    /// <summary>
    /// Raised once a navigation has settled, with the stage it settled on. Replaces
    /// <see cref="StageNavigationManager.afterSuccessfullySwitchedToStage"/> for the UI authoring tools.
    /// </summary>
    [NoAutoStaticsCleanup] // every subscriber unsubscribes in its own teardown, safe to persist
    public static event Action<Stage> StageSettled;

    /// <summary>
    /// Raised before a navigation leaves its starting stage, with the stage it is heading for first. Replaces
    /// <see cref="StageNavigationManager.stageChanging"/> for the UI authoring tools.
    /// </summary>
    [NoAutoStaticsCleanup]
    public static event Action<Stage, Stage> StageChanging;

    [NoAutoStaticsCleanup] // scratch state for one synchronous navigation, never spans a reload
    static int s_BatchDepth;
    [NoAutoStaticsCleanup]
    static bool s_SettledPending;
    [NoAutoStaticsCleanup]
    static bool s_ChangingForwarded;

    static UIStageNavigation()
    {
        StageNavigationManager.instance.afterSuccessfullySwitchedToStage += OnSwitchedToStage;
        StageNavigationManager.instance.stageChanging += OnStageChanging;
    }

    /// <summary>
    /// Opens <paramref name="context"/> for editing, entering one stage per document between the root and the
    /// edited sub-document so the breadcrumbs read as the path into it. Only the stage that is landed on shows
    /// its document; the ones passed through cost no more than their own existence.
    /// </summary>
    /// <returns>
    /// The stage landed on, or <see langword="null"/> when a walk's unsaved-changes prompt was backed out of or
    /// the save it asked for failed to write.
    /// </returns>
    public static VisualElementEditingStage Navigate(VisualTreeAssetEditingContext context, BreadcrumbBar.SeparatorStyle separatorStyle, bool setAsFirstItemAfterMainStage = false)
    {
        // Isolation deliberately detaches the sub-document from the documents enclosing it, so there is no path
        // into it to show and no trail to keep standing behind it.
        if (context.SubDocumentOptions == SubDocumentOptions.Isolation)
        {
            if (StageUtility.GetCurrentStage() is VisualElementEditingStage current
                && current.Context.SubDocumentOptions == SubDocumentOptions.Isolation
                && current.EditedVisualTreeAsset == context.EditedVisualTreeAsset
                && current.Context.PanelSettings == context.PanelSettings)
                return current;

            return VisualElementEditingStage.GoToStage(context, separatorStyle, setAsFirstItemAfterMainStage: true);
        }

        if (context.SubDocumentOptions != SubDocumentOptions.InContext)
            return VisualElementEditingStage.GoToStage(context, separatorStyle, setAsFirstItemAfterMainStage);

        var levels = BuildLevels(context);

        // Keep the stages the trail already holds; enter only the missing levels.
        var trailStart = FindTrailStart();
        var isolationShowsRoot = !setAsFirstItemAfterMainStage && IsolationStageShowsRootLevel(trailStart, levels[0]);
        var firstLevel = isolationShowsRoot ? 1 : 0;
        var entered = setAsFirstItemAfterMainStage
            ? 0
            : firstLevel + CountLevelsAlreadyEntered(levels, trailStart, firstLevel);
        var stage = entered > firstLevel
            ? (VisualElementEditingStage)StageNavigationManager.instance.stageHistory[trailStart + entered - firstLevel - 1]
            : isolationShowsRoot
                ? (VisualElementEditingStage)StageNavigationManager.instance.stageHistory[trailStart - 1]
                : null;

        // The deepest kept level is switched away from too, so it is settled with the levels the walk opens.
        var settleFrom = Math.Max(0, entered - 1);

        if (!SettleDocumentsOnTheWayIn(context, settleFrom, levels.Length - 1))
            return null;

        using var _ = BeginBatch();

        try
        {
            // A switch clears the trail only ahead of the stage it starts from, so resume from the deepest kept level.
            if (stage != null && StageUtility.GetCurrentStage() != stage)
                StageUtility.GoToStage(stage, false);

            for (var i = entered; i < levels.Length; ++i)
            {
                // A refused switch, from an unsaved-changes prompt for instance, leaves the navigation where it
                // was; walking on would put that prompt up once more per level left.
                if (stage != null && StageUtility.GetCurrentStage() != stage)
                    break;

                stage = VisualElementEditingStage.GoToStage(
                    levels[i],
                    i == 0 ? separatorStyle : BreadcrumbBar.SeparatorStyle.Arrow,
                    i == 0 && setAsFirstItemAfterMainStage,
                    deferRealization: i < levels.Length - 1);
            }
        }
        finally
        {
            // A walk cut short by a refusal stops on a stage it meant to pass through.
            (StageUtility.GetCurrentStage() as VisualElementEditingStage)?.OpenForDisplay();
        }

        return stage;
    }

    /// <summary>
    /// The editing context of each document between the root and <paramref name="context"/>'s own, one per stage
    /// the path into it is shown as.
    /// </summary>
    static VisualTreeAssetEditingContext[] BuildLevels(in VisualTreeAssetEditingContext context)
    {
        var path = context.SubDocumentPath;
        var levels = new VisualTreeAssetEditingContext[path.Length + 1];
        levels[0] = new VisualTreeAssetEditingContext(context.RootVisualTreeAsset, context.PanelSettings);
        for (var i = 0; i < path.Length; ++i)
        {
            var levelPath = new TemplateAsset[i + 1];
            Array.Copy(path, levelPath, levelPath.Length);
            levels[i + 1] = new VisualTreeAssetEditingContext(context.RootVisualTreeAsset, levelPath, SubDocumentOptions.InContext, context.PanelSettings);
        }

        return levels;
    }

    /// <summary>
    /// Asks once, before a walk starts, what to do with the unsaved changes in the documents of the levels in
    /// <c>[first, last)</c>, and carries the answer out. Returns whether the walk may go ahead.
    /// </summary>
    /// <remarks>
    /// Each document a walk opens gets a stage of its own, so a walk into unsaved documents would otherwise meet
    /// one prompt per level on the way in. Only the levels the walk both opens and passes through have such a
    /// prompt to pre-empt: the levels whose stages the trail already holds are not entered again, and the level
    /// the walk lands on is never switched away from, so both keep whatever they were dirtied with elsewhere.
    /// </remarks>
    static bool SettleDocumentsOnTheWayIn(in VisualTreeAssetEditingContext context, int first, int last)
    {
        if (first >= last)
            return true;

        using var _ = ListPool<VisualTreeAsset>.Get(out var documents);
        CollectDocumentsOnTheWayIn(context, first, last, documents);

        using var __ = ListPool<UnityEngine.Object>.Get(out var dirty);
        CollectDirtyAssets(documents, dirty);

        return UIAssetSavePrompt.AskAndResolve(dirty,
            L10n.Tr("Your changes will be lost if you don't save them.", null), allowCancel: true);
    }

    static void CollectDocumentsOnTheWayIn(in VisualTreeAssetEditingContext context, int first, int last, List<VisualTreeAsset> results)
    {
        for (var i = first; i < last; ++i)
        {
            var document = i == 0 ? context.RootVisualTreeAsset : context.SubDocumentPath[i - 1].ResolveTemplate();
            if (document != null && !results.Contains(document))
                results.Add(document);
        }
    }

    // Documents first, then the stylesheets they reference, as the prompt lists them in the order it is given.
    static void CollectDirtyAssets(List<VisualTreeAsset> documents, List<UnityEngine.Object> results)
    {
        var registry = UIAssetRegistry.LiveInstance;
        if (registry == null)
            return;

        for (var i = 0; i < documents.Count; ++i)
        {
            if (registry.IsDirty(documents[i]))
                results.Add(documents[i]);
        }

        using var _ = ListPool<StyleSheet>.Get(out var styleSheets);
        for (var i = 0; i < documents.Count; ++i)
        {
            styleSheets.Clear();
            UIAssetRegistry.CollectDocumentStyleSheets(documents[i], styleSheets);
            for (var j = 0; j < styleSheets.Count; ++j)
            {
                var styleSheet = styleSheets[j];
                if (styleSheet != null && registry.IsDirty(styleSheet) && !results.Contains(styleSheet))
                    results.Add(styleSheet);
            }
        }
    }

    /// <summary>
    /// Where the UI trail begins in the stage history: the first of the editing stages the history ends on.
    /// </summary>
    /// <remarks>
    /// A trail hangs off whichever stage the authoring tools were entered from, which is the Main Stage, a
    /// Prefab Stage, or an isolated document, so its levels sit at no fixed place in the history. An isolation
    /// stage is an editing stage like the trail's own levels, so it is told apart by its options.
    /// </remarks>
    static int FindTrailStart()
    {
        var history = StageNavigationManager.instance.stageHistory;
        var start = history.Count;
        while (start > 0
               && history[start - 1] is VisualElementEditingStage stage
               && stage.Context.SubDocumentOptions != SubDocumentOptions.Isolation)
            --start;

        return start;
    }

    /// <summary>
    /// Whether the stage the trail hangs off is an isolation stage showing <paramref name="rootLevel"/>'s
    /// document. A navigation from inside an isolation stage roots its path at the document that stage
    /// isolates, so the stage stands in for the root level rather than the document being entered again.
    /// </summary>
    static bool IsolationStageShowsRootLevel(int trailStart, in VisualTreeAssetEditingContext rootLevel)
    {
        var history = StageNavigationManager.instance.stageHistory;
        return trailStart > 0
               && history[trailStart - 1] is VisualElementEditingStage stage
               && stage.Context.SubDocumentOptions == SubDocumentOptions.Isolation
               && stage.EditedVisualTreeAsset == rootLevel.RootVisualTreeAsset
               && stage.Context.PanelSettings == rootLevel.PanelSettings;
    }

    /// <summary>
    /// How many of the levels from <paramref name="firstLevel"/> on the trail already holds, in order, from
    /// <paramref name="trailStart"/> on.
    /// </summary>
    static int CountLevelsAlreadyEntered(VisualTreeAssetEditingContext[] levels, int trailStart, int firstLevel)
    {
        var history = StageNavigationManager.instance.stageHistory;
        var count = 0;
        while (firstLevel + count < levels.Length
               && trailStart + count < history.Count
               && history[trailStart + count] is VisualElementEditingStage stage
               && ShowsSameDocument(stage.Context, levels[firstLevel + count]))
            ++count;

        return count;
    }

    // The contexts are built fresh on every navigation, so their template paths never share an array with the
    // ones the open stages hold and the record's own equality would call every level a mismatch.
    static bool ShowsSameDocument(in VisualTreeAssetEditingContext a, in VisualTreeAssetEditingContext b)
    {
        if (a.RootVisualTreeAsset != b.RootVisualTreeAsset
            || a.SubDocumentOptions != b.SubDocumentOptions
            || a.PanelSettings != b.PanelSettings)
            return false;

        var pathA = a.SubDocumentPath;
        var pathB = b.SubDocumentPath;
        var length = pathA?.Length ?? 0;
        if (length != (pathB?.Length ?? 0))
            return false;

        for (var i = 0; i < length; ++i)
        {
            if (pathA[i] == null || pathB[i] == null || pathA[i].id != pathB[i].id)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Groups the stage switches that make up one navigation, so subscribers see one transition rather than one
    /// per stage walked through.
    /// </summary>
    public static Scope BeginBatch()
    {
        ++s_BatchDepth;
        return default;
    }

    public readonly struct Scope : IDisposable
    {
        public void Dispose() => EndBatch();
    }

    static void EndBatch()
    {
        if (s_BatchDepth == 0)
            return;

        if (--s_BatchDepth > 0)
            return;

        s_ChangingForwarded = false;

        if (!s_SettledPending)
            return;

        s_SettledPending = false;
        StageSettled?.Invoke(StageUtility.GetCurrentStage());
    }

    static void OnSwitchedToStage(Stage stage)
    {
        if (s_BatchDepth > 0)
        {
            s_SettledPending = true;
            return;
        }

        StageSettled?.Invoke(stage);
    }

    // Only the first transition of a batch is forwarded: subscribers use this to put the editor in the shape the
    // incoming stage needs, such as registering hierarchy handlers, which has to happen before that stage
    // becomes current rather than after the batch has walked past it.
    static void OnStageChanging(Stage previous, Stage next)
    {
        if (s_BatchDepth > 0)
        {
            if (s_ChangingForwarded)
                return;
            s_ChangingForwarded = true;
        }

        StageChanging?.Invoke(previous, next);
    }
}
