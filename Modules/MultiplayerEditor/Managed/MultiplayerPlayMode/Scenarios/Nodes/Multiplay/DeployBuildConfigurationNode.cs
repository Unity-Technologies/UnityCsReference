// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    internal class DeployBuildConfigurationNode : MultiplayNode
    {
        [SerializeReference] public NodeInput<string> BuildConfigurationName;
        [SerializeReference] public NodeInput<string> BuildName;
        [SerializeReference] public NodeInput<long> BuildId;
        [SerializeReference] public NodeInput<string> BinaryPath;
        [SerializeReference] public NodeInput<BuildConfigurationSettings> Settings;

        [SerializeReference] public NodeOutput<long> BuildConfigurationId;

        public DeployBuildConfigurationNode(string name) : base(name)
        {
            BuildConfigurationName = new(this);
            BuildName = new(this);
            BuildId = new(this);
            BinaryPath = new(this);
            Settings = new(this);

            BuildConfigurationId = new(this);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            ValidateInputs();

            var result = await IPlayModeServices.Instance.DeployBuildConfigurationAsync(
                GetInput(BuildId),
                GetInput(BuildConfigurationName),
                GetInput(BuildName),
                GetInput(BinaryPath),
                GetInput(Settings).CommandLineArguments,
                GetInput(Settings).CoresCount,
                GetInput(Settings).MemoryMiB,
                GetInput(Settings).SpeedMhz,
                progress => SetProgress(Mathf.Clamp01(progress / 100f)),
                cancellationToken);

            SetOutput(BuildConfigurationId, result.BuildConfigurationId);
        }

        private void ValidateInputs()
        {
            ValidateInputIsSet(BuildConfigurationName, nameof(BuildConfigurationName));
            ValidateNameParameter(GetInput(BuildConfigurationName), nameof(BuildConfigurationName));
            ValidateInputIsSet(BuildName, nameof(BuildName));
            ValidateInputIsSet(BuildId, nameof(BuildId));
            ValidateInputIsSet(BinaryPath, nameof(BinaryPath));
            ValidateInputIsSet(Settings, nameof(Settings));

            var settings = GetInput(Settings);
            ValidateSettingsIsSet(settings.CommandLineArguments, nameof(settings.CommandLineArguments));
            ValidateSettingsIsSet(settings.CoresCount, nameof(settings.CoresCount));
            ValidateSettingsIsSet(settings.MemoryMiB, nameof(settings.MemoryMiB));
            ValidateSettingsIsSet(settings.SpeedMhz, nameof(settings.SpeedMhz));
        }

        private static void ValidateSettingsIsSet<T>(T value, string parameterName)
        {
            if (value == null || value.Equals(default(T)))
                throw new InvalidOperationException($"{parameterName} is not set");
        }
    }
}
