// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;
using UnityEngine;
using UnityEngine.Multiplayer.Internal;
using UnityEditor.Multiplayer.Internal;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class CurrentScenarioConnectionData
    {
        public static ConnectionData connection;
    }

    [CanRequestDomainReload]
    class EditorPlayNode : Node, IInstanceRunNode
    {
        [SerializeReference] public NodeInput<MultiplayerRoleFlags> MultiplayerRole;
        [SerializeReference] public NodeInput<SceneAsset> InitialScene;
        [SerializeReference] public NodeInput<ConnectionData> ConnectionData;
        [SerializeReference] public NodeOutput<ConnectionData> ConnectionDataOut;

        NodeInput<ConnectionData> IConnectableNode.ConnectionDataIn => ConnectionData;
        NodeOutput<ConnectionData> IConnectableNode.ConnectionDataOut => ConnectionDataOut;

        public bool IsRunning() => EditorApplication.isPlaying;

        public EditorPlayNode(string name) : base(name)
        {
            MultiplayerRole = new(this);
            InitialScene = new(this);
            ConnectionData = new(this);

            ConnectionDataOut = new(this);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var initialScene = GetInput(InitialScene);
            var connectionData = GetInput(ConnectionData);
            var multiplayerRole = GetInput(MultiplayerRole);

            var currentScene = SceneManager.GetActiveScene();
            if (initialScene != null && AssetDatabase.GetAssetPath(initialScene) != currentScene.path)
            {
                EditorApplication.playModeStateChanged -= LoadInitialScene;
                EditorApplication.playModeStateChanged += LoadInitialScene;
            }

            CurrentScenarioConnectionData.connection = connectionData;
            SetOutput(ConnectionDataOut, connectionData);

            EditorMultiplayerManager.activeMultiplayerRoleMask = multiplayerRole;
            EditorPlayModeGuard.EnterPlayModeSafely();

            // Entering play mode could take a few frames, so we wait until the state changes.
            while (EditorApplication.isPlaying != EditorApplication.isPlayingOrWillChangePlaymode)
                await Task.Delay(100);
        }

        protected override async Task ExecuteResumeAsync(CancellationToken cancellationToken)
        {
            while (EditorApplication.isPlaying != EditorApplication.isPlayingOrWillChangePlaymode)
                await Task.Delay(100);

            Assert.IsTrue(EditorApplication.isPlaying, $"Editor should be already in play mode when resuming the editor play node. isPlaying: {EditorApplication.isPlaying}, isPlayingOrWillChangePlaymode: {EditorApplication.isPlayingOrWillChangePlaymode}");
        }

        private EditorBuildSettingsScene[] m_OriginalScenes;
        private void LoadInitialScene(PlayModeStateChange state)
        {
            var initialScene = GetInput(InitialScene);

            if (state == PlayModeStateChange.ExitingEditMode)
            {
                // Make sure the scene is part of the build settings.
                m_OriginalScenes = EditorBuildSettings.scenes;
                var scenePath = AssetDatabase.GetAssetPath(initialScene);
                var isSceneInSettings = false;
                foreach (var scene in m_OriginalScenes)
                {
                    if (scene.path == scenePath)
                    {
                        isSceneInSettings = true;
                        break;
                    }
                }

                if (!isSceneInSettings)
                {
                    int originalLength = m_OriginalScenes.Length;
                    var newScenes = new EditorBuildSettingsScene[originalLength + 1];
                    for (int i = 0; i < originalLength; i++)
                    {
                        newScenes[i] = m_OriginalScenes[i];
                    }
                    newScenes[originalLength] = new EditorBuildSettingsScene(scenePath, true);
                    EditorBuildSettings.scenes = newScenes;
                }
            }
            else if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.playModeStateChanged -= LoadInitialScene;
                SceneManager.LoadScene(initialScene.name, LoadSceneMode.Single);

                EditorBuildSettings.scenes = m_OriginalScenes;
                m_OriginalScenes = null;
            }
        }

        protected override async Task MonitorAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100);
            }

            EditorApplication.ExitPlaymode();
        }
    }
}
