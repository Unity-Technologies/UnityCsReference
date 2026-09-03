// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;

namespace Unity.UIToolkit.Editor;

/// <summary>How the user answered an unsaved-changes prompt.</summary>
enum UIAssetSaveChoice
{
    /// <summary>Write the unsaved changes to disk.</summary>
    Save,

    /// <summary>Drop the unsaved changes and revert the assets to what is on disk.</summary>
    Discard,

    /// <summary>Abort whatever was about to take the assets out of reach.</summary>
    Cancel,
}

/// <summary>
/// The batched "these UI assets have unsaved changes" prompt, shown when something is about to take assets
/// edited in the Main Stage out of the user's reach — the editor quitting, or a scene closing.
/// </summary>
/// <remarks>
/// One dialog covers the whole batch, mirroring the editor's own "Scene(s) Have Been Modified" prompt: a
/// scene close that dirtied a document and two of its stylesheets asks once, not three times.
/// </remarks>
static partial class UIAssetSavePrompt
{
    // How many asset names the message lists before collapsing the rest, matching the editor's own
    // modified-scenes dialog.
    const int k_MaxListedAssets = 3;

    static readonly string k_Title = L10n.Tr("UI Assets Have Been Modified", null);
    static readonly string k_SaveButton = L10n.Tr("Save", null);
    static readonly string k_DiscardButton = L10n.Tr("Discard Changes", null);
    static readonly string k_CancelButton = L10n.Tr("Cancel", null);

    /// <summary>
    /// Overrides how the prompt is answered. Tests set this to run headless; left null, the user is asked.
    /// </summary>
    [NoAutoStaticsCleanup]
    internal static Func<IReadOnlyList<UnityEngine.Object>, bool, UIAssetSaveChoice> Resolver;

    /// <summary>
    /// Asks what to do with <paramref name="assets"/> and carries the answer out. Returns whether the caller
    /// may go ahead: <c>false</c> only when the user backed out, or when a save they asked for failed — a
    /// caller that cannot be cancelled (<paramref name="allowCancel"/> is <c>false</c>) proceeds regardless
    /// and only uses the return value to report the failure.
    /// </summary>
    /// <param name="assets">
    /// The dirty assets to settle, documents first (see
    /// <see cref="UIAssetRegistry.CollectDirtyAssetsHeldOnlyBy"/>). An empty batch is a silent no-op.
    /// </param>
    /// <param name="consequence">The sentence explaining what happens to the changes if they are not saved.</param>
    /// <param name="allowCancel">Whether the user can back out of what triggered the prompt.</param>
    internal static bool AskAndResolve(IReadOnlyList<UnityEngine.Object> assets, string consequence, bool allowCancel)
    {
        if (assets == null || assets.Count == 0)
            return true;

        // A caller that opted out of modals must neither be blocked by one nor have an answer invented for it:
        // leave the assets dirty and let the trigger through. Nothing is lost by doing so — the registry keeps
        // the unsaved content, so a later prompt or an explicit save can still settle it.
        if (Resolver == null && UIAssetRegistry.PreventDialogsFromOpening)
            return true;

        return Apply(Ask(assets, consequence, allowCancel), assets);
    }

    static UIAssetSaveChoice Ask(IReadOnlyList<UnityEngine.Object> assets, string consequence, bool allowCancel)
    {
        if (Resolver != null)
            return Resolver(assets, allowCancel);

        var message = BuildMessage(assets, consequence);

        if (!allowCancel)
        {
            return EditorDialog.DisplayDecisionDialog(k_Title, message, k_SaveButton, k_DiscardButton,
                DialogIconType.Warning)
                ? UIAssetSaveChoice.Save
                : UIAssetSaveChoice.Discard;
        }

        var result = EditorDialog.DisplayComplexDecisionDialog(k_Title, message, k_SaveButton, k_DiscardButton,
            k_CancelButton, DialogIconType.Warning);
        return result switch
        {
            DialogResult.DefaultAction => UIAssetSaveChoice.Save,
            DialogResult.AlternateAction => UIAssetSaveChoice.Discard,
            _ => UIAssetSaveChoice.Cancel,
        };
    }

    static bool Apply(UIAssetSaveChoice choice, IReadOnlyList<UnityEngine.Object> assets)
    {
        if (choice == UIAssetSaveChoice.Cancel)
            return false;

        var registry = UIAssetRegistry.instance;
        var succeeded = true;

        foreach (var asset in assets)
        {
            // Saving or discarding a document also covers the stylesheets it references, so by the time we
            // reach those sheets they are usually already settled — skip them rather than rewriting (or
            // force-reimporting) a file that now matches disk.
            if (asset == null || !registry.IsDirty(asset))
                continue;

            if (choice == UIAssetSaveChoice.Save)
                succeeded &= registry.SaveAsset(asset);
            else
                registry.DiscardAsset(asset);
        }

        return succeeded;
    }

    /// <summary>
    /// The body of an unsaved-changes message: the assets at stake (long lists collapsed) and what happens to
    /// them. Shared with the tools that raise the editor's own window-close prompt instead of this one, so the
    /// wording is the same wherever the question is asked.
    /// </summary>
    internal static string BuildMessage(IReadOnlyList<UnityEngine.Object> assets, string consequence)
    {
        var builder = new StringBuilder();
        builder.AppendLine(L10n.Tr("Do you want to save the changes you made in:", null));

        var listed = Math.Min(assets.Count, k_MaxListedAssets);
        for (var i = 0; i < listed; i++)
            builder.Append(' ').AppendLine(GetDisplayName(assets[i]));

        if (assets.Count > listed)
            builder.AppendLine(L10n.Tr(" and others...", null));

        builder.AppendLine();
        builder.Append(consequence);
        return builder.ToString();
    }

    // The file name is what the user recognizes, and it disambiguates a document from the same-named
    // stylesheet beside it; an asset with no path on disk (a never-saved document) falls back to its name.
    static string GetDisplayName(UnityEngine.Object asset)
    {
        if (asset == null)
            return string.Empty;

        var path = AssetDatabase.GetAssetPath(asset);
        return string.IsNullOrEmpty(path) ? asset.name : Path.GetFileName(path);
    }
}
