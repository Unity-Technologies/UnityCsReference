// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor;

[Serializable]
class StopProcessNode : Node
{
    const float k_ProcessStopTimeoutSeconds = 3f;
    const int k_CheckIntervalMS = 120;

    [SerializeReference] private NodeInput<int> m_ProcessId;

    public NodeInput<int> ProcessId => m_ProcessId;

    public StopProcessNode(string name) : base(name)
    {
        m_ProcessId = new(this);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        => await StopProcessByIdAsync(GetInput(ProcessId), cancellationToken);

    internal static async Task StopProcessByIdAsync(int processId, CancellationToken cancellationToken)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            await StopProcessAsync(process, cancellationToken);
        }
        catch (ArgumentException)
        {
            // Process already exited.
        }
    }

    internal static async Task StopProcessAsync(Process process, CancellationToken cancellationToken)
    {
        if (!process.HasExited)
        {
            DebugUtils.Trace($"Stopping process [Process id:{process.Id} ]");
            process.Kill();

            var timeoutTime = Time.realtimeSinceStartup + k_ProcessStopTimeoutSeconds;
            while (Time.realtimeSinceStartup < timeoutTime)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (process.HasExited)
                {
                    DebugUtils.Trace($"Process stopped [Process id:{process.Id} ]");
                    return;
                }

                await Task.Delay(k_CheckIntervalMS, cancellationToken);
            }

            throw new TimeoutException($"The process ({process.Id}) did not stop within the timeout period of {k_ProcessStopTimeoutSeconds} seconds.");
        }
    }
}
