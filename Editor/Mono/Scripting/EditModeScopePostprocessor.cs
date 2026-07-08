// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.Scripting.LifecycleManagement;
using UnityEngine;

// This class is responsible for Entering EditModeScope after a domain reload
// after all assets has been processed
sealed class EditModeScopePostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
    {
        // Guard against re-entering EditModeScope when it is already active.
        // When script compilation fails, the domain reload is skipped (assemblies are not reloaded)
        // but didDomainReload can still be true from a prior reload in the same import cycle.
        // In that case EditModeScope was never exited and must not be entered again.
        if (didDomainReload && !EditorApplication.isPlayingOrWillChangePlaymode
            && !LifecycleController.Instance.IsScopePresent<EditModeScope>())
        {
            LifecycleController.Instance.EnterScope<EditModeScope>();
        }
    }
}
