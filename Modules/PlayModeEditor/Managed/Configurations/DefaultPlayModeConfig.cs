// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

namespace Unity.PlayMode.Editor
{
    class DefaultPlayModeConfiguration : PlayModeConfiguration
    {
        public override bool SupportsPauseAndStep => true;
        public override Task ExecuteStartAsync(CancellationToken cancellationToken)
        {
            // Save assets before entering Playmode to synchronize project settings for virtual player and main editor
            AssetDatabase.SaveAssets();
            EditorApplication.EnterPlaymode();

            if (EditorUtility.scriptCompilationFailed)
                throw new TaskCanceledException();

            return Task.CompletedTask;
        }

        public override void ExecuteStop()
        {
            EditorApplication.ExitPlaymode();
        }

        void OnEnable()
        {
            name = "Default";
            Description = "Default play mode";
        }
    }
}
