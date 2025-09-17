// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    internal class DeployFleetNode : MultiplayNode
    {
        private const string k_ErrorMessage_FleetDeploymentFailed = "Failed to deploy the fleet.";

        [SerializeReference] public NodeInput<string> FleetName;
        [SerializeReference] public NodeInput<string> Region;
        [SerializeReference] public NodeInput<string> BuildConfigurationName;
        [SerializeReference] public NodeInput<long> BuildConfigurationId;
        [SerializeReference] public NodeInput<string> BuildExecutablePath;

        public DeployFleetNode(string name) : base(name)
        {
            FleetName = new(this);
            Region = new(this);
            BuildConfigurationName = new(this);
            BuildConfigurationId = new(this);
            BuildExecutablePath = new(this);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            ValidateInputs();

            var architecture = GetInput(BuildExecutablePath).EndsWith(".aarch64") ? "arm64" : "amd64";

            await IPlayModeServices.Instance.DeployFleetsAsync(
                GetInput(FleetName),
                GetInput(Region),
                architecture,
                GetInput(BuildConfigurationName),
                GetInput(BuildConfigurationId),
                (progress) => SetProgress(Mathf.Clamp01(progress / 100f)),
                cancellationToken);
        }

        private void ValidateInputs()
        {
            ValidateInputIsSet(FleetName, nameof(FleetName));
            ValidateNameParameter(GetInput(FleetName), nameof(FleetName));
            ValidateInputIsSet(Region, nameof(Region));
            ValidateInputIsSet(BuildConfigurationName, nameof(BuildConfigurationName));
            ValidateInputIsSet(BuildConfigurationId, nameof(BuildConfigurationId));
            ValidateInputIsSet(BuildExecutablePath, nameof(BuildExecutablePath));
        }
    }
}
