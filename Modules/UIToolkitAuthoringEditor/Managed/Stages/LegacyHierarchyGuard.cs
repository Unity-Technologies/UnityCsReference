// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Keeps UI Toolkit authoring entry points out of reach while the legacy Hierarchy is active, since only the new
/// Hierarchy can display the UI Toolkit editing stage.
/// </summary>
static class LegacyHierarchyGuard
{
    static readonly string k_Title = L10n.Tr("New Hierarchy Required", null);
    static readonly string k_Message = L10n.Tr("UI Toolkit authoring needs the new Hierarchy. Switch this project to the new Hierarchy?", null);
    static readonly string k_SwitchButton = L10n.Tr("Use New Hierarchy", null);
    static readonly string k_CancelButton = L10n.Tr("Cancel", null);

    /// <summary>
    /// Overrides how the prompt is answered. Tests set this to run headless; left null, the user is asked.
    /// </summary>
    [NoAutoStaticsCleanup]
    internal static Func<bool> Resolver;

    /// <summary>
    /// Asks the user to switch to the new Hierarchy when the legacy one is active, and carries out the switch if
    /// they agree. Returns whether the caller may go ahead.
    /// </summary>
    internal static bool EnsureNewHierarchy()
    {
        if (!EditorSettings.useLegacyHierarchy)
            return true;

        bool accepted;
        if (Resolver != null)
            accepted = Resolver();
        else if (UIAssetRegistry.PreventDialogsFromOpening || Application.isBatchMode)
            accepted = false;
        else
            accepted = EditorDialog.DisplayDecisionDialog(k_Title, k_Message, k_SwitchButton, k_CancelButton, DialogIconType.Warning);

        if (accepted)
            EditorSettings.useLegacyHierarchy = false;
        return accepted;
    }
}
