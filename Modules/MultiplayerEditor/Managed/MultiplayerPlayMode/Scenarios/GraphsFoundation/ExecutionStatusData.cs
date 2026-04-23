// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.Multiplayer.PlayMode.Editor;

[Serializable]
struct ExecutionStatusData
{
    public float ProgressSum;
    public int NodesCount;
    public int IdleNodesCount;
    public int RunningNodesCount;
    public int CompletedNodesCount;
    public int FailedNodesCount;
    public int AbortedNodesCount;
    public ExecutionState State;

    public readonly float Progress => ProgressSum / Math.Max(1, NodesCount);

    ExecutionState AggregateState(ExecutionState a, ExecutionState b)
    {
        // TODO: Do we really need `Invalid` state? It adds complexity to the state aggregation logic and
        // it's not clear if it adds value compared to just treating it as `Idle` until the execution starts.
        if (a == ExecutionState.Invalid && b == ExecutionState.Invalid) return ExecutionState.Invalid;
        if (a == ExecutionState.Invalid) return b;
        if (b == ExecutionState.Invalid) return a;

        if (a == ExecutionState.Failed || b == ExecutionState.Failed)
            return ExecutionState.Failed;

        if (a == ExecutionState.Idle && b == ExecutionState.Idle)
            return ExecutionState.Idle;

        if (a == ExecutionState.Aborted || b == ExecutionState.Aborted)
            return ExecutionState.Aborted;

        if (a == ExecutionState.Completed && b == ExecutionState.Completed)
            return ExecutionState.Completed;

        return ExecutionState.Running;
    }

    public void Aggregate(
        ExecutionState state,
        float progress,
        int nodesCount,
        int idleNodesCount,
        int runningNodesCount,
        int completedNodesCount,
        int failedNodesCount,
        int abortedNodesCount)
    {
        State = AggregateState(State, state);
        ProgressSum += progress;
        NodesCount += nodesCount;
        IdleNodesCount += idleNodesCount;
        RunningNodesCount += runningNodesCount;
        CompletedNodesCount += completedNodesCount;
        FailedNodesCount += failedNodesCount;
        AbortedNodesCount += abortedNodesCount;
    }

    public void Aggregate(ExecutionStatusData other) => Aggregate(
        state: other.State,
        progress: other.ProgressSum,
        nodesCount: other.NodesCount,
        idleNodesCount: other.IdleNodesCount,
        runningNodesCount: other.RunningNodesCount,
        completedNodesCount: other.CompletedNodesCount,
        failedNodesCount: other.FailedNodesCount,
        abortedNodesCount: other.AbortedNodesCount);

    public void Aggregate(ExecutionState state, IEnumerable<ExecutionNode> nodes)
    {
        State = AggregateState(State, state);
        foreach (var node in nodes)
        {
            ProgressSum += node.Progress;
            NodesCount++;

            switch (node.State)
            {
                case ExecutionState.Idle:
                    IdleNodesCount++;
                    break;
                case ExecutionState.Running:
                    RunningNodesCount++;
                    break;
                case ExecutionState.Completed:
                    CompletedNodesCount++;
                    break;
                case ExecutionState.Failed:
                    FailedNodesCount++;
                    break;
                case ExecutionState.Aborted:
                    AbortedNodesCount++;
                    break;
                default:
                    throw new NotImplementedException($"Unknown node state {node.State}");
            }
        }
    }
}
