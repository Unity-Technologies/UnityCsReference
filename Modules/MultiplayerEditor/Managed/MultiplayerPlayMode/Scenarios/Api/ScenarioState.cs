// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.Multiplayer.PlayMode.Editor
{
    /// <summary>
    /// Describes the running state of an scenario.
    /// </summary>
    internal enum ScenarioState
    {
        Idle,
        Running,
        Completed,
        Aborted,
        Failed
    }

    // The NodeStatus is a struct that is used to report the state of a node to the UI. As such it is a subset of the Node class that only contains what we want to show the the end user.
    // [?] Would we want to capture the errors here instead of at the scenario level ?
    internal struct NodeStatus
    {
        public string NodeName;
        public ExecutionState State;
        public NodeTimeData TimeData;
        public float Progress;

        public NodeStatus(string nodeName, ExecutionState state, NodeTimeData timeData, float progress)
        {
            NodeName = nodeName;
            State = state;
            TimeData = timeData;
            Progress = progress;
        }
    }

    /// <summary>
    /// Aggregates all the values that describe the current state of a scenario.
    /// </summary>
    [Serializable]
    internal struct ScenarioStatus
    {
        public ScenarioState State;
        public ExecutionStage CurrentStage;
        public ExecutionState StageState;
        public float TotalProgress;
        public float[] StageProgress;
        public List<Node.Error> Errors;
        public List<NodeStatus> NodeStateReports;
        public ExecutionState[] StageStates;

        public static readonly ScenarioStatus Default = new(ExecutionStage.None, ExecutionState.Idle);
        public static readonly ScenarioStatus Invalid = new(ExecutionStage.None, ExecutionState.Invalid);

        public ScenarioStatus(ExecutionStage stage = ExecutionStage.None, ExecutionState state = ExecutionState.Idle)
        {
            State = ScenarioState.Idle;
            CurrentStage = stage;
            StageState = state;
            TotalProgress = 0;
            StageProgress = Array.Empty<float>();
            Errors = new List<Node.Error>();
            NodeStateReports = new List<NodeStatus>();
            StageStates = Array.Empty<ExecutionState>();
        }

        override public string ToString()
        {
            return $"ScenarioState: {State}, CurrentStage: {CurrentStage}, StageState: {StageState}, TotalProgress: {TotalProgress}, StageProgress: {StageProgress}, ErrorInfo: {Errors?.Count} NodesStatus: {NodeStateReports?.Count}";
        }
    }
}
