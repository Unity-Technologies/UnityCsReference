// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;
using UnityEngine.Serialization;

namespace Unity.Multiplayer.PlayMode.Editor
{
    /// <summary>
    /// InstanceData contains info about an instance when entering play mode from scenario,
    /// including fields such as Type, BuildTarget, RunningMode, IsActive, and InstanceLaunchingDuration
    /// </summary>
    [Serializable]
    internal struct InstanceData
    {
        public string Type; // The type of the instance, expected value : "MainEditor", "VirtualEditor", "Local" or "Remote"
        public string BuildTarget;// The build target the instance is running on, e.g. "StandaloneOSX", "StandaloneLinux64Server", etc.
        public string RunningMode;// The running mode of the instance, expected value: "ScenarioControl" or "ManualControl"
        public bool IsActive; // Indicate whether the instance in currently active (launching or launched)
        public long InstanceLaunchingDuration;  // Duration in MS the instance has spent during launching, retrieve all nodes from the instance and calculate the sum of time difference between the earliest start time and the latest end time for a scenario stage
        public long InstancePrepareStageDurationMs; // Duration in MS the instance spent in the prepare stage, currently includes the build node for both local and remote instances, so it could represent the build time
        public long InstanceDeployStageDurationMs; // Duration in MS the instance has spent duration in deploy stage
        public long InstanceRunStageDurationMs; // Duration in MS the instance has spent duration in run stage
        public string MultiplayerRole; // Multiplayer role the instance selected, expected value : "Client", "Server", "ClientAndServer";
        public string UseMultiplay; // [Obsolete] Use Multiplay to determine whether the user is using Multiplay, expected values: MultiplaySimulated, Multiplay, or null.
    }

    /// <summary>
    /// ErrorData contains info about error(s) when entering play mode from scenario
    /// including details such as FailureNode, ExceptionType, Message and StackTrace
    /// </summary>
    [Serializable]
    internal struct ErrorData
    {
        public string FailureNode; // fully qualified name of the class of the Node in which the error occurs
        public string ExceptionType; // type of the exception when the error occurs
        public string Message; // message of the exception
        public string StackTrace; // stack trace of the exception
    }

    [Serializable]
    internal struct OnPlayFromScenarioData : IAnalytic.IData
    {
        public const string k_OnPlayFromScenarioEventName = "multiplayer_playmode_onPlayFromScenario";

        public InstanceData[] Instances;
        public string ScenarioState;// ScenarioState indicates whether the scenario completed successfully or failed - "Running" or "Failed"
        public long ScenarioLaunchingDurationMs; // Duration in MS the scenario has spent during launching, retrieve all nodes from the scenario and calculate the time difference between the earliest start time and the latest end time
        public ErrorData[] Errors;
    }

    [AnalyticInfo(eventName: OnPlayFromScenarioData.k_OnPlayFromScenarioEventName, vendorKey: Constants.k_VendorKey, version: 2)]
    internal class AnalyticsOnPlayFromScenarioEvent : AnalyticsEvent<AnalyticsOnPlayFromScenarioEvent, OnPlayFromScenarioData>
    {
        // TODO: This event is currently not used.
        public static void SendValidationErrorData(InstanceData[] instances, ErrorData[] errors)
        {
            var analyticsData = new OnPlayFromScenarioData
            {
                Instances = instances,
                ScenarioState = "ValidationFailed",
                Errors = errors
            };
            Send(analyticsData);
        }
    }
}
