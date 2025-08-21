// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Multiplayer.PlayMode.Editor
{
    /// <summary>
    /// NodeExecutionScope contains the assigned Node Scheduler's Turn, and as well as
    /// corresponding Domain Reload locks associated with the execution to be kept alive
    /// throughout the duration of a Node's operation.
    /// </summary>
    internal class NodeExecutionScope : IDisposable
    {
        private readonly NodeScheduler.Turn m_Turn;
        private readonly DomainReloadScopeLock m_Lock;

        public NodeExecutionScope(NodeScheduler.Turn turn, DomainReloadScopeLock domainReloadLock)
        {
            m_Turn = turn;
            m_Lock = domainReloadLock;
        }

        public void Dispose()
        {
            m_Lock?.Dispose();
            m_Turn?.Dispose();
        }
    }
}
