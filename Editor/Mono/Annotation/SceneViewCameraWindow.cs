// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    class SceneViewCameraWindow : PopupWindowContent
    {
        static class Styles
        {
            static bool s_Initialized;
            public static GUIStyle settingsArea;

            public static void Init()
            {
                if (s_Initialized)
                    return;

                s_Initialized = true;

                settingsArea = new GUIStyle()
                {
                    padding = new RectOffset(4, 4, 4, 4),
                };
            }
        }

        readonly SceneView m_SceneView;

        GUIContent m_CameraSpeedSliderContent;
        GUIContent m_CameraSpeedMin;
        GUIContent m_CameraSpeedMax;
        GUIContent m_FieldOfView;
        GUIContent m_DynamicClip;
        GUIContent m_OcclusionCulling;
        GUIContent m_EasingEnabled;
        GUIContent m_EasingDuration;

        const int kFieldCount = 12;
        const int kWindowWidth = 290;
        const int kWindowHeight = ((int)EditorGUI.kSingleLineHeight) * kFieldCount + kFrameWidth * 2;
        const int kFrameWidth = 10;
        const float kPrefixLabelWidth = 120f;
        const float kMinSpeedLabelWidth = 25f;
        const float kMaxSpeedLabelWidth = 29f;
        const float kMinMaxSpeedFieldWidth = 50f;
        const float kMinMaxSpeedSpace = 8f;
        const float kNearClipMin = .01f;

        float[] m_Vector2Floats = { 0, 0 };

        public override Vector2 GetWindowSize()
        {
            return new Vector2(kWindowWidth, kWindowHeight);
        }

        public SceneViewCameraWindow(SceneView sceneView)
        {
            m_SceneView = sceneView;

            m_CameraSpeedSliderContent = EditorGUIUtility.TrTextContent("Camera Speed", "The current speed of the camera in the Scene view.");
            m_CameraSpeedMin = EditorGUIUtility.TrTextContent("Min", "The minimum speed of the camera in the Scene view. Valid values are between [0.01, 98].");
            m_CameraSpeedMax = EditorGUIUtility.TrTextContent("Max", "The maximum speed of the camera in the Scene view. Valid values are between [0.02, 99].");
            m_FieldOfView = EditorGUIUtility.TrTextContent("Field of View", "The height of the camera's view angle. Measured in degrees vertically, or along the local Y axis.");
            m_DynamicClip = EditorGUIUtility.TrTextContent("Dynamic Clipping", "Check this to enable camera's near and far clipping planes to be calculated relative to the viewport size of the Scene.");
            m_OcclusionCulling = EditorGUIUtility.TrTextContent("Occlusion Culling", "Check this to enable occlusion culling in the Scene view. Occlusion culling disables rendering of objects when they\'re not currently seen by the camera because they\'re hidden (occluded) by other objects.");
            m_EasingEnabled = EditorGUIUtility.TrTextContent("Camera Easing", "Check this to enable camera movement easing. This makes the camera ease in when it starts moving, and ease out when it stops.");
            m_EasingDuration = EditorGUIUtility.TrTextContent("Duration", "How long it takes for the speed of the camera to accelerate to its initial full speed. Measured in seconds.");
        }

        public override void OnGUI(Rect rect)
        {
            if (m_SceneView == null || m_SceneView.sceneViewState == null)
                return;

            Draw(rect);

            // Escape closes the window
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }
        }

        private void Draw(Rect rect)
        {
            var settings = m_SceneView.cameraSettings;
            Styles.Init();

            const int k_SettingsIconPad = 2;
            Vector2 settingsSize = EditorStyles.iconButton.CalcSize(EditorGUI.GUIContents.titleSettingsIcon);
            Rect settingsRect = new Rect(rect.xMax - Styles.settingsArea.padding.right - k_SettingsIconPad - settingsSize.x, Styles.settingsArea.padding.top + k_SettingsIconPad, settingsSize.x, settingsSize.y);

            if (GUI.Button(settingsRect, EditorGUI.GUIContents.titleSettingsIcon, EditorStyles.iconButton))
                ShowContextMenu();

            GUILayout.BeginArea(rect, Styles.settingsArea);

            EditorGUI.BeginChangeCheck();

            EditorGUIUtility.labelWidth = kPrefixLabelWidth;

            GUILayout.Label(EditorGUIUtility.TrTextContent("Scene Camera"), EditorStyles.boldLabel);

            // fov isn't applicable in orthographic mode, and orthographic size is controlled by the user zoom
            using (new EditorGUI.DisabledScope(m_SceneView.orthographic))
            {
                settings.fieldOfView = EditorGUILayout.Slider(m_FieldOfView, settings.fieldOfView, 4f, 179f);
            }

            settings.dynamicClip = EditorGUILayout.Toggle(m_DynamicClip, settings.dynamicClip);

            using (new EditorGUI.DisabledScope(settings.dynamicClip))
            {
                float near = settings.nearClip, far = settings.farClip;
                DrawClipPlanesField(EditorGUI.s_ClipingPlanesLabel, ref near, ref far, EditorGUI.kNearFarLabelsWidth);
                settings.nearClip = near;
                settings.farClip = far;
                settings.nearClip = Mathf.Max(kNearClipMin, settings.nearClip);
                if (settings.nearClip > settings.farClip)
                    settings.farClip = settings.nearClip + kNearClipMin;
            }

            settings.occlusionCulling = EditorGUILayout.Toggle(m_OcclusionCulling, settings.occlusionCulling);

            if (EditorGUI.EndChangeCheck())
                m_SceneView.Repaint();

            GUILayout.Label(EditorGUIUtility.TrTextContent("Navigation"), EditorStyles.boldLabel);

            settings.easingEnabled = EditorGUILayout.Toggle(m_EasingEnabled, settings.easingEnabled);

            using (new EditorGUI.DisabledScope(!settings.easingEnabled))
            {
                EditorGUI.indentLevel += 1;
                settings.easingDuration = EditorGUILayout.Slider(m_EasingDuration, settings.easingDuration, .1f, 2f);
                EditorGUI.indentLevel -= 1;
            }

            settings.speed = EditorGUILayout.Slider(m_CameraSpeedSliderContent, settings.speed, settings.speedMin, settings.speedMax);

            EditorGUI.BeginChangeCheck();

            m_Vector2Floats[0] = settings.speedMin;
            m_Vector2Floats[1] = settings.speedMax;

            DrawSpeedMinMaxFields();

            if (EditorGUI.EndChangeCheck())
                settings.SetSpeedMinMax(m_Vector2Floats);

            EditorGUIUtility.labelWidth = 0f;

            GUILayout.EndArea();
        }

        void DrawSpeedMinMaxFields()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            Rect r = EditorGUILayout.GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.numberField);
            r.width = kMinSpeedLabelWidth + kMinMaxSpeedFieldWidth;
            EditorGUIUtility.labelWidth = kMinSpeedLabelWidth;
            m_Vector2Floats[0] = EditorGUI.FloatField(r, m_CameraSpeedMin, m_Vector2Floats[0]);
            r.x += r.width + kMinMaxSpeedSpace;
            r.width = kMaxSpeedLabelWidth + kMinMaxSpeedFieldWidth;
            EditorGUIUtility.labelWidth = kMaxSpeedLabelWidth;
            m_Vector2Floats[1] = EditorGUI.FloatField(r, m_CameraSpeedMax, m_Vector2Floats[1]);
            GUILayout.EndHorizontal();
        }

        void DrawClipPlanesField(GUIContent label, ref float near, ref float far, float propertyLabelsWidth, params GUILayoutOption[] options)
        {
            bool hasLabel = EditorGUI.LabelHasContent(label);
            const float height = EditorGUI.kSingleLineHeight * 2 + EditorGUI.kVerticalSpacingMultiField;
            Rect r = EditorGUILayout.GetControlRect(hasLabel, height, EditorStyles.numberField, options);

            Rect fieldPosition = EditorGUI.PrefixLabel(r, label);
            fieldPosition.height = EditorGUI.kSingleLineHeight;

            float oldLabelWidth = EditorGUIUtility.labelWidth;
            int oldIndentLevel = EditorGUI.indentLevel;

            EditorGUIUtility.labelWidth = propertyLabelsWidth;
            EditorGUI.indentLevel = 0;

            near = EditorGUI.FloatField(fieldPosition, EditorGUI.s_NearAndFarLabels[0], near);
            fieldPosition.y += EditorGUI.kSingleLineHeight + EditorGUI.kVerticalSpacingMultiField;
            far = EditorGUI.FloatField(fieldPosition, EditorGUI.s_NearAndFarLabels[1], far);

            EditorGUI.indentLevel = oldIndentLevel;
            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        void ShowContextMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(EditorGUIUtility.TrTextContent("Reset"), false, Reset);
            menu.ShowAsContext();
        }

        void Reset()
        {
            m_SceneView.ResetCameraSettings();
            m_SceneView.Repaint();
        }
    }
}
