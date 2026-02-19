// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Multiplayer.PlayMode.Editor;

// TODO: ScenarioStatusData and InstanceStatusData are identical, consider merging them.
[Serializable]
struct InstanceStatusData
{
    public ExecutionStatusData OverallStatus;
    public ExecutionStatusData[] StageStatuses;
    public ExecutionStage CurrentStage;

    public void Clear()
    {
        OverallStatus = default;
        CurrentStage = default;

        if (StageStatuses == null || StageStatuses.Length != ExecutionGraph.k_StagesCount)
            StageStatuses = new ExecutionStatusData[ExecutionGraph.k_StagesCount];

        Array.Clear(StageStatuses, 0, StageStatuses.Length);
    }

    public bool IsExecuting()
    {
        return OverallStatus.State is ExecutionState.Running;
    }

    public bool IsExecutingLaunchingStages()
    {
        return IsExecuting() &&
            CurrentStage is ExecutionStage.Prepare or ExecutionStage.Deploy or ExecutionStage.Start;
    }

    public bool IsExecutingRunningStage()
    {
        return IsExecuting() &&
            CurrentStage is ExecutionStage.Run;
    }
}
