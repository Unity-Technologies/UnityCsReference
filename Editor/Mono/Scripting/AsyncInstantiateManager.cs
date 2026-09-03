// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: ScriptingRuntime not yet converted
using System.Threading;
using UnityEngine;

namespace UnityEditor
{
    [InitializeOnLoad]
    static class AsyncInstantiateManager
    {
        static AsyncInstantiateManager()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            // Pending operations can only leak from playmode to editmode.
            // Editmode executes AsyncInstantiate synchronously.
            if (stateChange == PlayModeStateChange.EnteredPlayMode
                || stateChange == PlayModeStateChange.EnteredEditMode
                || stateChange == PlayModeStateChange.ExitingEditMode)
                return;

            CancelPendingOperations();
        }

        static void CancelPendingOperations()
        {
            AsyncInstantiateOperation.s_GlobalCancellation.Cancel();

            // Ensure new token when no domain reload happens
            AsyncInstantiateOperation.s_GlobalCancellation = new CancellationTokenSource();
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
