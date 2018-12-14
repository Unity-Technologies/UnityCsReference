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
        GUIContent[] m_CameraSpeedMinMax;
        GUIContent m_FieldOfView;
        GUIContent m_DynamicClip;
        GUIContent m_OcclusionCulling;

        const int k_FieldCount = 10;
        const int k_WindowWidth = 300;
        const int k_WindowHeight = ((int)EditorGUI.kSingleLineHeight) * k_FieldCount + kFrameWidth * 2;
        const int kFrameWidth = 10;
        const float k_PrefixLabelWidth = 120f;
        const float kMinMaxSpeedLabelWidth = 26f;
        const float k_NearClipMin = .01f;

        float[] m_Vector2Floats = { 0, 0 };

        public override Vector2 GetWindowSize()
        {
            return new Vector2(k_WindowWidth, k_WindowHeight);
        }

        public SceneViewCameraWindow(SceneView sceneView)
        {
            m_SceneView = sceneView;

            m_CameraSpeedSliderContent = EditorGUIUtility.TrTextContent("Speed", "The current speed of the camera in the Scene view.");
            m_CameraSpeedMinMax = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("Min", "The minimum speed of the camera in the Scene view. Valid values are between [0.01, 98]."),
                EditorGUIUtility.TrTextContent("Max", "The maximum speed of the camera in the Scene view. Valid values are between [0.02, 99].")
            };
            m_FieldOfView = EditorGUIUtility.TrTextContent("Field of View", "The height of the Camera's view angle. Measured in degrees vertically, or along the local Y axis.");
            m_DynamicClip = EditorGUIUtility.TrTextContent("Dynamic Clipping", "Check this to enable camera's near and far clipping planes to be calculated relative to the viewport size of the Scene.");
            m_OcclusionCulling = EditorGUIUtility.TrTextContent("Occlusion Culling", "Check this to enable occlusion culling in the Scene view. Occlusion culling disables rendering of objects when they\'re not currently seen by the Camera because they\'re hidden (occluded) by other objects.");
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
            var settings = m_SceneView.sceneViewCameraSettings;
            Styles.Init();

            const int k_SettingsIconPad = 2;
            Vector2 settingsSize = EditorStyles.iconButton.CalcSize(EditorGUI.GUIContents.titleSettingsIcon);
            Rect settingsRect = new Rect(rect.xMax - Styles.settingsArea.padding.right - k_SettingsIconPad - settingsSize.x, Styles.settingsArea.padding.top + k_SettingsIconPad, settingsSize.x, settingsSize.y);

            if (GUI.Button(settingsRect, EditorGUI.GUIContents.titleSettingsIcon, EditorStyles.iconButton))
                ShowContextMenu();

            GUILayout.BeginArea(rect, Styles.settingsArea);

            EditorGUI.BeginChangeCheck();

            EditorGUIUtility.labelWidth = k_PrefixLabelWidth;

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
                ClipPlanesField(EditorGUI.s_ClipingPlanesLabel, ref near, ref far, EditorGUI.kNearFarLabelsWidth);
                settings.nearClip = near;
                settings.farClip = far;
                settings.nearClip = Mathf.Max(k_NearClipMin, settings.nearClip);
                if (settings.nearClip > settings.farClip)
                    settings.farClip = settings.nearClip + k_NearClipMin;
            }

            settings.occlusionCulling = EditorGUILayout.Toggle(m_OcclusionCulling, settings.occlusionCulling);

            if (EditorGUI.EndChangeCheck())
                m_SceneView.Repaint();

            GUILayout.Label(EditorGUIUtility.TrTextContent("Navigation"), EditorStyles.boldLabel);

            settings.speed = EditorGUILayout.Slider(m_CameraSpeedSliderContent, settings.speed, settings.speedMin, settings.speedMax);

            EditorGUI.BeginChangeCheck();

            m_Vector2Floats[0] = settings.speedMin;
            m_Vector2Floats[1] = settings.speedMax;

            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            Rect r = EditorGUILayout.GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.numberField);
            EditorGUI.MultiFloatField(r, m_CameraSpeedMinMax, m_Vector2Floats, kMinMaxSpeedLabelWidth);
            GUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
                settings.SetSpeedMinMax(m_Vector2Floats);

            EditorGUIUtility.labelWidth = 0f;

            GUILayout.EndArea();
        }

        internal static void ClipPlanesField(GUIContent label, ref float near, ref float far, float propertyLabelsWidth, params GUILayoutOption[] options)
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
            m_SceneView.ResetSceneViewCameraSettings();
            m_SceneView.Repaint();
        }
    }
}
