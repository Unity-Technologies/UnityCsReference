// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// The confirmation shown before discarding a single UI asset from a context menu.
/// </summary>
/// <remarks>
/// Every other way of discarding is already reached through a question — the quit/scene-close prompt, or the
/// editor's own window-close prompt — so this is the one entry point that would otherwise drop the user's work
/// on a single click. The changes are unrecoverable once it goes ahead: the discard clears the asset's undo
/// entries along with them.
/// </remarks>
static partial class UIAssetSavePrompt
{
    // Unique to this dialog, and stable: it is what an opted-out user's decision is stored against.
    const string k_DiscardOptOutKey = "UIToolkit.DiscardSingleUIAsset";

    static readonly string k_DiscardTitle = L10n.Tr("Discard Changes", null);
    static readonly string k_DiscardConfirmButton = L10n.Tr("Discard", null);

    /// <summary>
    /// Overrides how the confirmation is answered. Tests set this to run headless; left null, the user is
    /// asked.
    /// </summary>
    [NoAutoStaticsCleanup]
    internal static Func<UnityEngine.Object, bool> DiscardResolver;

    /// <summary>
    /// Asks whether to drop <paramref name="asset"/>'s unsaved changes and, if the user agrees, reverts that
    /// asset alone. Returns whether the discard happened.
    /// </summary>
    internal static bool ConfirmAndDiscardSingleAsset(UnityEngine.Object asset, object source)
    {
        var registry = UIAssetRegistry.LiveInstance;
        if (registry == null || !registry.CanSettleSingleAsset(asset))
            return false;

        if (!AskToDiscard(asset))
            return false;

        return registry.DiscardSingleAsset(asset, source);
    }

    static bool AskToDiscard(UnityEngine.Object asset)
    {
        if (DiscardResolver != null)
            return DiscardResolver(asset);

        // A caller that opted out of modals must not be blocked by one, and answering "yes" on the user's
        // behalf would destroy work they were never asked about: leave the changes where they are.
        if (UIAssetRegistry.PreventDialogsFromOpening)
            return false;

        var message = string.Format(
            L10n.Tr("The unsaved changes in '{0}' will be lost.\n\nAre you sure you want to discard them?", null),
            GetDisplayName(asset));

        return EditorDialog.DisplayDecisionDialogWithOptOut(k_DiscardTitle, message, k_DiscardConfirmButton,
            k_CancelButton, DialogOptOutDecisionType.ForThisSession, k_DiscardOptOutKey, DialogIconType.Warning);
    }
}
