// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.PlayMode.Editor;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class PlayModeStatusWindow : EditorWindow
    {
        internal static void OpenWindow()
        {
            GetWindow<PlayModeStatusWindow>(typeof(InspectorWindow));
        }

        private PlaymodeStatusElement m_ScenarioElement;

        // Add this as a workaround to address the issue where selecting an active scenario config from the playmode popup content doesn’t refresh the playmode status window
        [InitializeOnLoadMethod]
        private static void Init()
        {
            if (MigrationUtility.ShouldDisableMultiplayerPlayMode())
                return;

            PlaymodePopupContent.OpenPlayModeConfigurationsWindowDelegate = OpenWindow;
            PlayModeManager.instance.ConfigAssetChanged += () =>
            {
                var windows = Resources.FindObjectsOfTypeAll<PlayModeStatusWindow>();
                if (windows.Length > 0)
                {
                    foreach (var window in windows)
                    {
                        window.Refresh(null);
                    }
                }
            };
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Play Mode Status");

            Refresh(null);
            Scenario.ScenarioStarted += Refresh;
        }

        private void OnFocus()
        {
            Refresh(null);
        }

        private void Refresh(Scenario scenario)
        {
            m_ScenarioElement = new PlaymodeStatusElement();
            rootVisualElement.Clear();
            rootVisualElement.Add(m_ScenarioElement);
        }
    }
}
