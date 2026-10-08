// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace UnityEditor
{
    static partial class AsyncInstantiateManager
    {
        [OnCodeLoaded]
        static void Initialize()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [OnCodeUnloading]
        static void Shutdown()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
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
