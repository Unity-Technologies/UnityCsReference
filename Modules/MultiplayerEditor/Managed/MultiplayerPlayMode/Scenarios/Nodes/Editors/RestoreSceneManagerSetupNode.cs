// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class RestoreSceneManagerSetupNode : ExecutionNode
    {
        [SerializeReference] public NodeInput<SceneSetup[]> ScenesSetup;

        public RestoreSceneManagerSetupNode()
        {
            ScenesSetup = new(this);
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var sceneSetups = GetInput(ScenesSetup);
            if (sceneSetups == null || sceneSetups.Length == 0)
            {
                return Task.CompletedTask;
            }

            EditorSceneManager.RestoreSceneManagerSetup(sceneSetups);
            return Task.CompletedTask;
        }
    }
}
