// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor;

[Serializable]
[CanRequestDomainReload]
class MainEditorStartNode : ExecutionNode
{
    // Note that this timeout doesn't account for the time spent in domain reloads
    const float k_PlayModeStateChangeTimeoutSeconds = 5f;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        EditorPlayModeGuard.EnterPlayModeSafely();
        await UntilPlayStateCompleted(cancellationToken);
    }

    protected override Task ExecuteResumeAsync(CancellationToken cancellationToken) => UntilPlayStateCompleted(cancellationToken);

    async Task UntilPlayStateCompleted(CancellationToken cancellationToken)
    {
        try
        {
            await UntilPlayStateTransitionComplete(true, cancellationToken, "Timeout waiting for Editor to finish Play Mode state change.");
        }
        catch (OperationCanceledException)
        {
            EditorApplication.ExitPlaymode();
            await UntilPlayStateTransitionComplete(false, CancellationToken.None, "Timeout waiting for Editor to exit Play Mode.");
        }
    }

    static bool IsPlayStateChanging() => EditorApplication.isPlaying != EditorApplication.isPlayingOrWillChangePlaymode;

    internal static async Task UntilPlayStateTransitionComplete(bool play, CancellationToken cancellationToken, string timeoutMessage)
    {
        var timeout = Time.realtimeSinceStartup + k_PlayModeStateChangeTimeoutSeconds;
        while (EditorApplication.isPlaying != play || IsPlayStateChanging())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Time.realtimeSinceStartup > timeout)
                throw new TimeoutException(timeoutMessage);

            await Task.Yield();
        }
    }
}
