// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Raised when the last read-write holder of a dirty asset releases its reference. Carries the actions to
/// resolve it so a listener can present its own UI without the registry depending on any window.
/// </summary>
readonly struct UIAssetSaveRequest
{
    public readonly UnityEngine.Object Asset;
    readonly Action m_Save;
    readonly Action m_Discard;

    internal UIAssetSaveRequest(UnityEngine.Object asset, Action save, Action discard)
    {
        Asset = asset;
        m_Save = save;
        m_Discard = discard;
    }

    /// <summary>Writes the asset's unsaved changes to disk.</summary>
    public void Save() => m_Save?.Invoke();

    /// <summary>Reverts the asset to its on-disk state, dropping the unsaved changes.</summary>
    public void Discard() => m_Discard?.Invoke();
}

/// <summary>
/// Raised when a tracked asset is changed outside our control (reimported from disk) while it still has
/// unsaved in-memory edits. The registry never silently overwrites in this case; it resolves the conflict
/// itself and notifies listeners of the <see cref="Choice"/> it made, so each tool can react (rebind its
/// live views, and — for <see cref="UIAssetConflictChoice.SaveBackupAndUseImported"/> — preserve its work).
/// </summary>
readonly struct UIAssetConflict
{
    /// <summary>The asset whose in-memory edits conflicted with a fresh reimport from disk.</summary>
    public readonly UnityEngine.Object Asset;

    /// <summary>How the registry resolved the conflict.</summary>
    public readonly UIAssetConflictChoice Choice;

    internal UIAssetConflict(UnityEngine.Object asset, UIAssetConflictChoice choice)
    {
        Asset = asset;
        Choice = choice;
    }
}

/// <summary>How to resolve an external-change conflict on an asset with unsaved edits.</summary>
enum UIAssetConflictChoice
{
    /// <summary>Restore the user's in-memory edits over the reimported content (stays dirty).</summary>
    Keep,

    /// <summary>Discard the user's edits and adopt the freshly imported on-disk version.</summary>
    UseImported,

    /// <summary>
    /// Adopt the freshly imported on-disk version, but first let a tool preserve the discarded edits (e.g. the
    /// UI Builder writes its work-in-progress to a temporary backup file). For the registry's own bookkeeping
    /// this behaves like <see cref="UseImported"/> (the tracked asset ends up clean, matching disk); the
    /// backup is written by whichever tool observes the conflict.
    /// </summary>
    SaveBackupAndUseImported,
}
