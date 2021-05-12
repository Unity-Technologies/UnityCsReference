// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.Overlays;

namespace UnityEditor
{
    public partial class SceneView
    {
        [Overlay(typeof(SceneView), k_OverlayID, k_DisplayName)]
        internal class SceneViewIsolationOverlay : TransientSceneViewOverlay
        {
            public const string k_OverlayID = "Scene View/Scene Visibility";
            const string k_DisplayName = "Isolation View";
            bool m_ShouldDisplay;

            internal static class Styles
            {
                public static GUIContent isolationModeExitButton = EditorGUIUtility.TrTextContent("Exit", "Exit isolation mode");
            }

            public override bool visible => m_ShouldDisplay;

            public override void OnCreated()
            {
                SceneVisibilityManager.currentStageIsIsolated += CurrentStageIsolated;
                CurrentStageIsolated(SceneVisibilityState.isolation);
            }

            public override void OnWillBeDestroyed()
            {
                SceneVisibilityManager.currentStageIsIsolated -= CurrentStageIsolated;
            }

            void CurrentStageIsolated(bool isolated)
            {
                m_ShouldDisplay = isolated;
            }

            public override void OnGUI()
            {
                if (GUILayout.Button(Styles.isolationModeExitButton, GUILayout.MinWidth(120)))
                {
                    SceneVisibilityManager.instance.ExitIsolation();
                }
            }
        }

        [Overlay(typeof(SceneView), k_OverlayID, k_DisplayName)]
        [Icon("Icons/Overlays/ToolsToggle.png")]
        class SceneViewToolsOverlay : IMGUIOverlay
        {
            const string k_OverlayID = "Scene View/Component Tools";
            const string k_DisplayName = "Component Tools";
            public override void OnGUI()
            {
                EditorToolGUI.DoContextualToolbarOverlay();
            }
        }
    }
}
// namespace
