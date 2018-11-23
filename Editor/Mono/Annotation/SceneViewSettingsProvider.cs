// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    class SceneViewSettingsProvider : SettingsProvider
    {
        class Styles
        {
            static bool s_Initialized;

            public static GUIContent cameraMovementEasingEnabled = new GUIContent("Camera Easing", "Check this to enable camera movement easing. This makes the Camera ease in when it starts moving, and ease out when it stops.");
            public static GUIContent cameraMovementEasingDuration = new GUIContent("Duration", "How long it takes for the Camera speed to accelerate to full speed. Measured in seconds.");

            public static GUIStyle settings;

            public static void Init()
            {
                if (s_Initialized)
                    return;

                s_Initialized = true;

                settings = new GUIStyle()
                {
                    margin = new RectOffset(8, 4, 4, 4)
                };
            }
        }

        [SettingsProvider]
        static SettingsProvider CreateSceneViewSettingsProvider()
        {
            return new SceneViewSettingsProvider("Preferences/Scene View");
        }

        public SceneViewSettingsProvider(string path, SettingsScope scopes = SettingsScope.User)
            : base(path, scopes)
        {
            PopulateSearchKeywordsFromGUIContentProperties<Styles>();
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            Styles.Init();
            var searching = !string.IsNullOrEmpty(searchContext);

            GUILayout.BeginVertical(Styles.settings, GUILayout.MaxWidth(SettingsWindow.s_DefaultLayoutMaxWidth));

            if (!searching)
                GUILayout.Label("Navigation", EditorStyles.boldLabel);

            if (!searching || SearchUtils.MatchSearch(searchContext, Styles.cameraMovementEasingEnabled.text))
                SceneViewMotion.movementEasingEnabled = EditorGUILayout.Toggle(
                    Styles.cameraMovementEasingEnabled,
                    SceneViewMotion.movementEasingEnabled);

            using (new EditorGUI.DisabledScope(!SceneViewMotion.movementEasingEnabled))
            {
                EditorGUI.indentLevel += 1;
                if (!searching || SearchUtils.MatchSearch(searchContext, Styles.cameraMovementEasingDuration.text))
                    SceneViewMotion.movementEasingDuration = EditorGUILayout.Slider(Styles.cameraMovementEasingDuration, SceneViewMotion.movementEasingDuration, 0.001f, 3f);
                EditorGUI.indentLevel -= 1;
            }

            GUILayout.EndVertical();
        }
    }
}
