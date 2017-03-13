// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class LookDevSettingsWindow
        : PopupWindowContent
    {
        public class Styles
        {
            public readonly GUIStyle sMenuItem              = "MenuItem";
            public readonly GUIStyle sSeparator             = "sv_iconselector_sep";

            public readonly GUIContent sTitle       = EditorGUIUtility.TextContent("Settings");
            public readonly GUIContent sMultiView   = EditorGUIUtility.TextContent("Multi-view");
            public readonly GUIContent sCamera      = EditorGUIUtility.TextContent("Camera");
            public readonly GUIContent sLighting    = EditorGUIUtility.TextContent("Lighting");
            public readonly GUIContent sAnimation   = EditorGUIUtility.TextContent("Animation");
            public readonly GUIContent sViewport    = EditorGUIUtility.TextContent("Viewport");
            public readonly GUIContent sEnvLibrary  = EditorGUIUtility.TextContent("Environment Library");
            public readonly GUIContent sMisc        = EditorGUIUtility.TextContent("Misc");

            public readonly GUIContent sResetCamera             = EditorGUIUtility.TextContent("Fit View        F");
            public readonly GUIContent sCreateNewLibrary        = EditorGUIUtility.TextContent("Save as new library");
            public readonly GUIContent sSaveCurrentLibrary      = EditorGUIUtility.TextContent("Save current library");
            public readonly GUIContent sResetView               = EditorGUIUtility.TextContent("Reset View");
            public readonly GUIContent sEnableToneMap           = EditorGUIUtility.TextContent("Enable Tone Mapping");
            public readonly GUIContent sEnableAutoExp           = EditorGUIUtility.TextContent("Enable Auto Exposure");
            public readonly GUIContent sExposureRange           = EditorGUIUtility.TextContent("Exposure Range");
            public readonly GUIContent sEnableShadows           = EditorGUIUtility.TextContent("Enable Shadows");
            public readonly GUIContent sShadowDistance          = EditorGUIUtility.TextContent("Shadow distance");
            public readonly GUIContent sShowBalls               = EditorGUIUtility.TextContent("Show Chrome/grey balls");
            public readonly GUIContent sShowControlWindows      = EditorGUIUtility.TextContent("Show Controls");
            public readonly GUIContent sAllowDifferentObjects   = EditorGUIUtility.TextContent("Allow Different Objects");
            public readonly GUIContent sResyncObjects           = EditorGUIUtility.TextContent("Resynchronize Objects");
            public readonly GUIContent sRotateObjectMode        = EditorGUIUtility.TextContent("Rotate Objects");
            public readonly GUIContent sObjRotationSpeed        = EditorGUIUtility.TextContent("Rotate Objects speed");
            public readonly GUIContent sRotateEnvMode           = EditorGUIUtility.TextContent("Rotate environment");
            public readonly GUIContent sEnvRotationSpeed        = EditorGUIUtility.TextContent("Rotate Env. speed");
            public readonly GUIContent sEnableShadowIcon        = EditorGUIUtility.IconContent("LookDevShadow",  "Shadow|Toggles shadows on and off");
            public readonly GUIContent sEnableObjRotationIcon   = EditorGUIUtility.IconContent("LookDevObjRotation",  "ObjRotation|Toggles object rotation (turntable) on and off");
            public readonly GUIContent sEnableEnvRotationIcon   = EditorGUIUtility.IconContent("LookDevEnvRotation",  "EnvRotation|Toggles environment rotation on and off");
            public readonly Texture    sEnableShadowTexture     = EditorGUIUtility.FindTexture("LookDevShadow");
            public readonly Texture    sEnableObjRotationTexture = EditorGUIUtility.FindTexture("LookDevObjRotation");
            public readonly Texture    sEnableEnvRotationTexture = EditorGUIUtility.FindTexture("LookDevEnvRotation");

            public readonly GUIContent[] sMultiViewMode =
            {
                EditorGUIUtility.TextContent("Single1"),
                EditorGUIUtility.TextContent("Single2"),
                EditorGUIUtility.TextContent("Side by side"),
                EditorGUIUtility.TextContent("Split-screen"),
                EditorGUIUtility.TextContent("Zone"),
            };


            public readonly Texture[] sMultiViewTextures =
            {
                EditorGUIUtility.FindTexture("LookDevSingle1"),
                EditorGUIUtility.FindTexture("LookDevSingle2"),
                EditorGUIUtility.FindTexture("LookDevSideBySide"),
                EditorGUIUtility.FindTexture("LookDevSplit"),
                EditorGUIUtility.FindTexture("LookDevZone"),
            };
        }

        static Styles s_Styles = null;
        public static Styles styles { get { if (s_Styles == null) s_Styles = new Styles(); return s_Styles; } }

        // This enum is only to calculate the size of the windows for settings.
        // Below are listed the number of label, separator, checkbox, button and slider use in the menu.
        // Keep in sync with the code below that generate the menu to have a correct windows size.
        enum UINumElement
        {
            UINumDrawHeader = 6,
            UINumToggle = (int)LookDevMode.Count + 7,
            UINumSlider = 4,
            UINumSeparator = 7,
            UINumButton = 6,

            UITotalElement = UINumDrawHeader + UINumToggle + UINumSlider + UINumSeparator + UINumButton
        }

        readonly float m_WindowHeight = (int)(UINumElement.UITotalElement) * EditorGUI.kSingleLineHeight;
        const float m_WindowWidth = 180;

        const float kIconSize = 16.0f;
        const float kIconHorizontalPadding = 3.0f;

        readonly LookDevView m_LookDevView;

        public LookDevSettingsWindow(LookDevView lookDevView)
        {
            m_LookDevView = lookDevView;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(m_WindowWidth, m_WindowHeight);
        }

        public override void OnGUI(Rect rect)
        {
            if (m_LookDevView == null)
                return;

            GUILayout.BeginVertical();
            {
                // We need to have a sufficient size to display negative float number
                EditorGUIUtility.labelWidth = 130;
                EditorGUIUtility.fieldWidth = 35;

                // Look Dev view mode
                DrawHeader(styles.sMultiView);

                for (int i = 0; i < (int)LookDevMode.Count; ++i)
                {
                    EditorGUI.BeginChangeCheck();
                    bool value = GUILayout.Toggle(m_LookDevView.config.lookDevMode == (LookDevMode)i, styles.sMultiViewMode[i], styles.sMenuItem);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_LookDevView.UpdateLookDevModeToggle((LookDevMode)i, value);
                        m_LookDevView.Repaint();
                        GUIUtility.ExitGUI();
                    }
                }

                // Camera settings
                DrawSeparator();
                DrawHeader(styles.sCamera);

                if (GUILayout.Button(styles.sResetCamera, styles.sMenuItem))
                {
                    m_LookDevView.Frame();
                }

                m_LookDevView.config.enableToneMap = GUILayout.Toggle(m_LookDevView.config.enableToneMap, styles.sEnableToneMap, styles.sMenuItem);
                EditorGUI.BeginChangeCheck();
                // Cast to int to have integer step
                float newExposureRange = (float)EditorGUILayout.IntSlider(styles.sExposureRange, (int)m_LookDevView.config.exposureRange, 1, 32);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m_LookDevView.config, "Change exposure range");
                    m_LookDevView.config.exposureRange = newExposureRange;
                }

                DrawSeparator();
                DrawHeader(styles.sLighting);

                EditorGUI.BeginChangeCheck();

                GUILayout.BeginHorizontal();
                m_LookDevView.config.enableShadowCubemap = GUILayout.Toggle(m_LookDevView.config.enableShadowCubemap, styles.sEnableShadows, styles.sMenuItem);
                GUILayout.EndHorizontal();
                if (EditorGUI.EndChangeCheck())
                {
                    m_LookDevView.Repaint();
                }

                EditorGUI.BeginChangeCheck();
                float newShadowDistance = EditorGUILayout.Slider(styles.sShadowDistance, m_LookDevView.config.shadowDistance, 0.0f, 1000.0f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m_LookDevView.config, "Change shadow distance");
                    m_LookDevView.config.shadowDistance = newShadowDistance;
                }

                DrawSeparator();
                DrawHeader(styles.sAnimation);

                GUILayout.BeginHorizontal();
                m_LookDevView.config.rotateObjectMode = GUILayout.Toggle(m_LookDevView.config.rotateObjectMode, styles.sRotateObjectMode, styles.sMenuItem);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                m_LookDevView.config.rotateEnvMode = GUILayout.Toggle(m_LookDevView.config.rotateEnvMode, styles.sRotateEnvMode, styles.sMenuItem);
                GUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();
                float newObjRotationSpeed = EditorGUILayout.Slider(styles.sObjRotationSpeed, m_LookDevView.config.objRotationSpeed, -5.0f, 5.0f);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m_LookDevView.config, "Change rotation speed");
                    m_LookDevView.config.objRotationSpeed = newObjRotationSpeed;
                }

                EditorGUI.BeginChangeCheck();
                float newEnvRotationSpeed = EditorGUILayout.Slider(styles.sEnvRotationSpeed, m_LookDevView.config.envRotationSpeed, -5.0f, 5.0f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(m_LookDevView.config, "Change env speed");
                    m_LookDevView.config.envRotationSpeed = newEnvRotationSpeed;
                }

                DrawSeparator();
                DrawHeader(styles.sViewport);
                if (GUILayout.Button(styles.sResetView, styles.sMenuItem))
                {
                    m_LookDevView.ResetView();
                }

                DrawSeparator();
                DrawHeader(styles.sEnvLibrary);
                using (new EditorGUI.DisabledScope(!m_LookDevView.envLibrary.dirty))
                {
                    if (GUILayout.Button(styles.sSaveCurrentLibrary, styles.sMenuItem))
                    {
                        editorWindow.Close();
                        if (m_LookDevView.SaveLookDevLibrary())
                            m_LookDevView.envLibrary.dirty = false;
                        GUIUtility.ExitGUI();
                    }
                }
                if (GUILayout.Button(styles.sCreateNewLibrary, styles.sMenuItem))
                {
                    editorWindow.Close();
                    string assetPath = EditorUtility.SaveFilePanelInProject("Save New Environment Library", "New Env Library", "asset", "");
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        m_LookDevView.CreateNewLibrary(assetPath);
                    }

                    GUIUtility.ExitGUI();
                }
                EditorGUI.BeginChangeCheck();
                LookDevEnvironmentLibrary library = EditorGUILayout.ObjectField(m_LookDevView.userEnvLibrary, typeof(LookDevEnvironmentLibrary), false) as LookDevEnvironmentLibrary;
                if (EditorGUI.EndChangeCheck())
                {
                    m_LookDevView.envLibrary = library;
                }

                DrawSeparator();
                DrawHeader(styles.sMisc);
                m_LookDevView.config.showBalls = GUILayout.Toggle(m_LookDevView.config.showBalls, styles.sShowBalls, styles.sMenuItem);
                m_LookDevView.config.showControlWindows = GUILayout.Toggle(m_LookDevView.config.showControlWindows, styles.sShowControlWindows, styles.sMenuItem);
                EditorGUI.BeginChangeCheck();
                bool allowDifferentObjects = GUILayout.Toggle(m_LookDevView.config.allowDifferentObjects, styles.sAllowDifferentObjects, styles.sMenuItem);
                if (EditorGUI.EndChangeCheck())
                {
                    m_LookDevView.config.allowDifferentObjects = allowDifferentObjects;
                }
                if (GUILayout.Button(styles.sResyncObjects, styles.sMenuItem))
                {
                    m_LookDevView.config.ResynchronizeObjects();
                }
            }
            GUILayout.EndVertical();

            // Use mouse move so we get hover state correctly in the menu item rows
            if (Event.current.type == EventType.MouseMove)
                Event.current.Use();

            // Escape closes the window
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }
        }

        private void DrawSeparator()
        {
            GUILayout.Space(3.0f);
            GUILayout.Label(GUIContent.none, styles.sSeparator);
        }

        private void DrawHeader(GUIContent label)
        {
            GUILayout.Label(label, EditorStyles.miniLabel);
        }
    }
}
