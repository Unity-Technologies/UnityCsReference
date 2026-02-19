// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace Unity.Multiplayer.PlayMode.Editor
{
    /// <summary>
    /// Represents a node in the graph
    /// </summary>
    [Serializable]
    internal abstract class Node
    {
        /// <summary>
        /// ErrorInfo includes the Message, ExceptionType, and StackTrace from a node within a specific scenario.
        /// </summary>
        [Serializable]
        internal class Error
        {
            public string FailureNode;
            public string Message;
            public string ExceptionType;
            public string StackTrace;

            public Error(Exception e, string fullQualifiedName)
            {
                FailureNode = fullQualifiedName;
                Message = e.Message;
                ExceptionType = e.GetType().ToString();
                StackTrace = e.StackTrace;
            }
        }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (MigrationUtility.ShouldDisableMultiplayerPlayMode())
                return;

            // For ensuring that all nodes survive properly to domain reloads we need to validate
            // that all their inputs and outputs are serialized as references so the link between
            // inputs and outputs are not broken.

            var missingSerializeReference = new StringBuilder();
            var nodeTypes = TypeCache.GetTypesDerivedFrom<Node>();
            foreach (var nodeType in nodeTypes)
            {
                var fields = nodeType.GetFields((System.Reflection.BindingFlags)~0);
                foreach (var field in fields)
                {
                    if (!typeof(NodeParameter).IsAssignableFrom(field.FieldType))
                        continue;

                    if (!field.IsDefined(typeof(SerializeReference), true))
                        missingSerializeReference.AppendLine($"'{nodeType}': '{field.Name}'");
                }
            }

            if (missingSerializeReference.Length > 0)
                Debug.LogError($"The following parameters in the nodes are missing the [SerializeReference] attribute:\n{missingSerializeReference}");
        }

        // Will be true if the node has not been interrupted by a domain reload.
        [NonSerialized] private bool m_HasNotBeenInterrupted;

        [SerializeField] private ExecutionState m_State;
        [SerializeField] private float m_Progress;
        [SerializeField] private string m_Name;
        [SerializeReference] private Error m_ErrorInfo;
        [SerializeField] private bool m_LogExceptions = true;

        private NodeTimeData m_TimeData;

        public string Name => m_Name;
        public Error ErrorInfo => m_ErrorInfo;
        public NodeTimeData TimeData => m_TimeData;
        internal bool LogExceptions
        {
            get => m_LogExceptions;
            set => m_LogExceptions = value;
        }

        public float Progress
        {
            get => m_Progress;
            private set
            {
                if (m_Progress != value)
                {
                    m_Progress = value;
                    StatusRefreshed?.Invoke();
                }
            }
        }

        public ExecutionState State
        {
            get => m_State;
            private set
            {
                if (m_State != value)
                {
                    m_State = value;
                    StatusRefreshed?.Invoke();
                }
            }
        }

        /// <summary>
        /// Returns true if the node started but is not running because it was interrupted by a domain reload.
        /// </summary>
        public bool Interrupted => (State == ExecutionState.Running) && !m_HasNotBeenInterrupted;

        internal event Action StatusRefreshed;

        public Node(string name)
        {
            m_Name = name;
            m_State = ExecutionState.Idle;
            m_Progress = 0.0f;
        }

        internal void Reset()
        {
            m_State = ExecutionState.Idle;
            m_Progress = 0.0f;
            m_HasNotBeenInterrupted = false;
            m_ErrorInfo = null;
        }

        protected T GetInput<T>(NodeInput<T> input)
        {
            return input.GetValue<T>();
        }

        protected void SetOutput<T>(NodeOutput<T> output, T value)
        {
            output.SetValue(value);
        }

        /// <summary>
        /// Sets the progress of the node.
        /// </summary>
        /// <param name="progress">
        /// The progress of the node. Must be between 0 and 1.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the node is not running.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the progress is less than 0, greater than 1, or less than the current progress.
        /// </exception>
        protected void SetProgress(float progress)
        {
            if (State != ExecutionState.Running)
                throw new InvalidOperationException("Cannot set progress on a node that is not running.");

            // Prevent floating point precision issues around the edges
            progress = Mathf.Approximately(progress, 1.0f) ? 1.0f : Mathf.Approximately(progress, 0.0f) ? 0.0f : progress;

            if (progress < 0.0f || progress > 1.0f)
                throw new ArgumentOutOfRangeException(nameof(progress), progress, $"Progress ({progress}) must be between 0 and 1.");

            if (!Mathf.Approximately(progress, Progress) && progress < Progress)
                DebugUtils.Trace($"Progress ({progress}) cannot be less than the current progress ({Progress}).");

            Progress = progress;
        }

        /// <summary>
        /// This method is called when the node is executed.
        /// </summary>
        protected abstract Task ExecuteAsync(CancellationToken cancellationToken);

        /// <summary>
        /// This method is called when the node is resumed after being interrupted by a domain reload.
        /// </summary>
        protected virtual Task ExecuteResumeAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException("This node does not support resuming.");

        private bool CanRequestDomainReload => GetType().IsDefined(typeof(CanRequestDomainReloadAttribute), true);

        /// <summary>
        /// Runs the node.
        /// </summary>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            using var turn = await AssignTurnAndLockIfNeeded(cancellationToken);

            if (State != ExecutionState.Idle)
                throw new InvalidOperationException("Cannot run a node that is not idle.");

            State = ExecutionState.Running;
            m_HasNotBeenInterrupted = true;

            m_TimeData.StartTime = DateTime.Now;
            await ExecuteAndComplete(cancellationToken);
        }

        /// <summary>
        /// Resumes the node if it has been terminated by a domain reload.
        /// </summary>
        public async Task ResumeAsync(CancellationToken cancellationToken)
        {
            using var turn = await AssignTurnAndLockIfNeeded(cancellationToken);

            if (m_HasNotBeenInterrupted)
                throw new InvalidOperationException("Cannot resume a node that has not been interrupted by a domain reload.");

            if (State != ExecutionState.Running)
                throw new InvalidOperationException("Cannot resume a node that is not running.");

            m_HasNotBeenInterrupted = true;
            await ExecuteAndComplete(cancellationToken, true);
        }

        private async Task ExecuteAndComplete(CancellationToken cancellationToken, bool resume = false)
        {
            try
            {
                using var _ = new DomainReloadScopeGuard(OnDomainReloadRequestedDuringExecution);

                if (resume)
                    await ExecuteResumeAsync(cancellationToken);
                else
                    await ExecuteAsync(cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                if (Progress < 1.0f)
                {
                    State = ExecutionState.Aborted;
                    return;
                }
            }
            catch (Exception e)
            {
                if (LogExceptions)
                {
                    Debug.LogError($"An error occurred while executing the node '{Name}': {e.Message}");
                    Debug.LogException(e);
                }
                m_ErrorInfo = new Error(e, GetType().FullName);
                State = ExecutionState.Failed;
                return;
            }
            finally
            {
                m_TimeData.EndTime = DateTime.Now;
            }

            SetProgress(1.0f);
            State = ExecutionState.Completed;
        }

        private void OnDomainReloadRequestedDuringExecution(bool executionCompleted)
        {
            var domainReloadAllowed = CanRequestDomainReload;

            if (domainReloadAllowed)
                return;

            var exception = new NotSupportedException($"A domain reload was requested while the node '{Name}' was running. This is not supported.\n(node execution {(executionCompleted ? "completed" : "incomplete")})");

            if (executionCompleted)
                throw exception;

            // As the node won't be able to complete its execution and
            // terminate gracefully, we force it to be in an error state.
            Debug.LogException(exception);
            m_ErrorInfo = new Error(exception, GetType().FullName);
            State = ExecutionState.Failed;
        }

        private async Task<NodeExecutionScope> AssignTurnAndLockIfNeeded(CancellationToken cancellationToken)
        {
            // Schedule this node's run execution via the Node scheduler.
            var turn = NodeScheduler.AssignTurn(CanRequestDomainReload);
            await turn.UntilActive(cancellationToken);

            // On it's turn, check if a domain reload or compilation is pending before
            // starting the node execution, we wait for it to be completed as it can affect it.
            await WaitForPendingReloads(cancellationToken);

            // If we're not running a node that is allowed to request a domain reload,
            // we prevent domain reloads from this scope.
            DomainReloadScopeLock domainReloadScopeLock = CanRequestDomainReload ? null : new DomainReloadScopeLock();
            return new NodeExecutionScope(turn, domainReloadScopeLock);
        }

        private async Task WaitForPendingReloads(CancellationToken cancellationToken)
        {
            await Task.Yield();
            while (InternalUtilities.IsDomainReloadRequested() ||
                   EditorApplication.isCompiling ||
                   EditorApplication.isUpdating)
            {
                await Task.Delay(100);

                // Avoid awaiting for pending reloads when cancellation is requested.
                // We throw a Cancellation exception here to prevent the node from grabbing a DR Lock.
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        internal static long ComputeExecutionDuration(IEnumerable<Node> nodes)
        {
            var minStartTime = DateTime.MaxValue;
            var maxEndTime = DateTime.MinValue;

            foreach (var node in nodes)
            {
                if (node.TimeData.HasStarted)
                {
                    if (node.TimeData.StartTime < minStartTime)
                        minStartTime = node.TimeData.StartTime;
                }

                if (node.TimeData.HasEnded)
                {
                    if (node.TimeData.EndTime > maxEndTime)
                        maxEndTime = node.TimeData.EndTime;
                }
            }

            var duration = (long)Math.Round((maxEndTime - minStartTime).TotalMilliseconds);
            return duration > 0L ? duration : 0L;
        }
    }
}
