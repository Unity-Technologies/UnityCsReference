// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using InstanceSettings = Unity.Multiplayer.PlayMode.Editor.MainEditorController.InstanceSettings;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class ValidateMainEditorSettingsNode : ExecutionNode
    {
        [SerializeReference] public NodeInput<InstanceSettings> Settings;

        public ValidateMainEditorSettingsNode()
        {
            Settings = new(this);
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var initialScene = GetInput(Settings).InitialScene;
            if (initialScene == null)
                return Task.CompletedTask;

            var scenePath = AssetDatabase.GetAssetPath(initialScene);
            if (PackageUtils.IsAssetInReadOnlyPackage(scenePath, out var packageName))
            {
                throw new InvalidOperationException(
                    $"Initial Scene '{initialScene.name}' is in the read-only package '{packageName}'. " +
                    "Unity cannot open scenes from read-only packages. Copy the scene into the Assets folder and select the copy.");
            }

            return Task.CompletedTask;
        }
    }
}
