// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    // Whether an Android device is plugged in and picked is a precondition for running, not a property of
    // the saved Scenario, so it is checked here rather than in OrchestratedScenario.IsValid. The device is
    // stored per user in OrchestratedScenarioUserSettings, which no validity surface observes; checking it
    // from IsValid made those surfaces render a stale verdict (UUM-152807).
    [Serializable]
    class ValidateRunDeviceNode : ExecutionNode
    {
        [SerializeReference] NodeInput<string> m_InstanceName;
        [SerializeReference] NodeInput<string> m_DeviceId;

        public NodeInput<string> InstanceName => m_InstanceName;
        public NodeInput<string> DeviceId => m_DeviceId;

        public ValidateRunDeviceNode()
        {
            m_InstanceName = new(this);
            m_DeviceId = new(this);
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(GetInput(DeviceId)))
            {
                throw new InvalidOperationException(
                    $"'{GetInput(InstanceName)}' has no run device selected. Connect an Android device and select it in the instance's Run Device dropdown.");
            }

            return Task.CompletedTask;
        }
    }
}
