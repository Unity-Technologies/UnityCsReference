// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class MainEditorRunNode : ExecutionNode
    {
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && EditorApplication.isPlayingOrWillChangePlaymode)
            {
                await Task.Yield();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                // A cancellation at this point means it was requested by the user,
                // which means the node actually completed properly. Setting progress to 1.0 will prevent its state to be set to Aborted.
                SetProgress(1f);
            }

            EditorApplication.ExitPlaymode();
            await MainEditorStartNode.UntilPlayStateTransitionComplete(false, CancellationToken.None, "Timeout waiting for Editor to exit Play Mode.");
        }        
    }
}
