// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    internal class DeployBuildNode : MultiplayNode
    {
        private const string k_ErrorMessage_BuildPathDoesNotExist = "Build path does not exist";
        private const string k_ErrorMessage_ExecutablePathDoesNotExist = "Executable path does not exist";

        [SerializeReference] public NodeInput<string> BuildName;
        [SerializeReference] public NodeInput<string> BuildPath;
        [SerializeReference] public NodeInput<string> ExecutablePath;
        [SerializeReference] public NodeInput<Hash128> BuildHash;

        [SerializeReference] public NodeOutput<long> BuildId;

        public DeployBuildNode(string name) : base(name)
        {
            BuildName = new(this);
            BuildPath = new(this);
            ExecutablePath = new(this);
            BuildHash = new(this);

            BuildId = new(this);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            ValidateInputs();

            if (await ProcessReuseBuild(cancellationToken))
                return;

            var result = await IPlayModeServices.Instance.UploadAndSyncBuildsAsync(
                GetInput(BuildName),
                GetInput(BuildPath),
                GetInput(ExecutablePath),
                (progress) => SetProgress(Mathf.Clamp01(progress/ 100f)),
                cancellationToken);

            var buildId = result.BuildId;

            SetOutput(BuildId, buildId);

            var versionResult = await IPlayModeServices.Instance.GetVersionOfBuildAsync(result.BuildId, cancellationToken);
            DeployData.instance.AssignHashToBuildId(buildId, versionResult.Version, GetInput(BuildHash));
        }

        private async Task<bool> ProcessReuseBuild( CancellationToken cancellationToken)
        {
            var buildHash = GetInput(BuildHash);
            if (DeployData.instance.FindBuildIdAndVersionForHash(buildHash, out var buildId, out var localVersion)
                && localVersion != -1)
            {
                var remoteVersionResult = await IPlayModeServices.Instance.GetVersionOfBuildAsync(buildId, cancellationToken);

                if (remoteVersionResult.Version != localVersion)
                    return false;

                SetOutput(BuildId, buildId);
                return true;
            }

            return false;
        }

        private void ValidateInputs()
        {
            ValidateInputIsSet(BuildName, nameof(BuildName));
            ValidateNameParameter(GetInput(BuildName), nameof(BuildName));
            ValidateInputIsSet(BuildPath, nameof(BuildPath));
            ValidateInputIsSet(ExecutablePath, nameof(ExecutablePath));
            ValidateInputIsSet(BuildHash, nameof(BuildHash));

            if (!Directory.Exists(GetInput(BuildPath)))
                throw new FileNotFoundException(k_ErrorMessage_BuildPathDoesNotExist, GetInput(BuildPath));

            if (!File.Exists(GetInput(ExecutablePath)))
                throw new FileNotFoundException(k_ErrorMessage_ExecutablePathDoesNotExist, GetInput(ExecutablePath));
        }
    }
}
