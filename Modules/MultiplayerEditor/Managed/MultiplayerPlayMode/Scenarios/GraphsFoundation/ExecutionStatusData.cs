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

    public readonly float Progress => ProgressSum / Math.Max(1, NodesCount);

    public readonly ExecutionState State
    {
        get
        {
            if (NodesCount == 0)
                return ExecutionState.Invalid;

            if (FailedNodesCount > 0)
                return ExecutionState.Failed;

            if (IdleNodesCount == NodesCount)
                return ExecutionState.Idle;

            if (AbortedNodesCount > 0)
                return ExecutionState.Aborted;

            if (CompletedNodesCount == NodesCount)
                return ExecutionState.Completed;

            return ExecutionState.Running;
        }
    }

    public void Aggregate(
        float progress,
        int nodesCount,
        int idleNodesCount,
        int runningNodesCount,
        int completedNodesCount,
        int failedNodesCount,
        int abortedNodesCount)
    {
        ProgressSum += progress;
        NodesCount += nodesCount;
        IdleNodesCount += idleNodesCount;
        RunningNodesCount += runningNodesCount;
        CompletedNodesCount += completedNodesCount;
        FailedNodesCount += failedNodesCount;
        AbortedNodesCount += abortedNodesCount;
    }

    public void Aggregate(ExecutionStatusData other) => Aggregate(
        progress: other.ProgressSum,
        nodesCount: other.NodesCount,
        idleNodesCount: other.IdleNodesCount,
        runningNodesCount: other.RunningNodesCount,
        completedNodesCount: other.CompletedNodesCount,
        failedNodesCount: other.FailedNodesCount,
        abortedNodesCount: other.AbortedNodesCount);

    public void Aggregate(IEnumerable<ExecutionNode> nodes)
    {
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
