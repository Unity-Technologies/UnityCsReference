// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    [CanRecoverFromDomainReload]
    class MonitorProcessNode : ExecutionNode
    {
        const int k_ProcessCheckIntervalMS = 120;
        const float k_ProcessFindTimeoutSeconds = 3f;

        [SerializeReference] NodeInput<int> m_ProcessId;

        public NodeInput<int> ProcessId => m_ProcessId;

        public MonitorProcessNode()
        {
            m_ProcessId = new(this);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var process = await FindProcessById(GetInput(ProcessId), cancellationToken);

            try
            {
                while (!process.HasExited && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(k_ProcessCheckIntervalMS, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // A cancellation at this point means it was requested by the user,
                // which means the node actually completed properly. Setting progress to 1.0 will prevent its state to be set to Aborted.
                SetProgress(1f);
            }
            finally
            {
                await StopProcessNode.StopProcessAsync(process, CancellationToken.None);
            }
        }

        protected override Task ExecuteResumeAsync(CancellationToken cancellationToken)
            => ExecuteAsync(cancellationToken);

        internal static async Task<Process> FindProcessById(int processId, CancellationToken cancellationToken)
        {
            var timeoutTime = Time.realtimeSinceStartup + k_ProcessFindTimeoutSeconds;
            while (Time.realtimeSinceStartup < timeoutTime)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return Process.GetProcessById(processId);
                }
                catch (ArgumentException)
                {
                    await Task.Delay(k_ProcessCheckIntervalMS, cancellationToken);
                }
            }
            
            throw new TimeoutException($"The process ({processId}) was not found within the timeout period of {k_ProcessFindTimeoutSeconds} seconds.");
        }
    }
}
