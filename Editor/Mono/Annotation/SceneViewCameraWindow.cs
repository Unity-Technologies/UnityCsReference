// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor
{
    public class SceneViewCameraWindow : PopupWindowContent
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
                    border = new RectOffset(4, 4, 4, 4),
                };
            }
        }

        readonly SceneView m_SceneView;

        GUIContent m_CameraSpeedSliderContent;
        GUIContent m_AccelerationEnabled;
        GUIContent[] m_MinMaxContent;
        GUIContent m_FieldOfView;
        GUIContent m_DynamicClip;
        GUIContent m_OcclusionCulling;
        GUIContent m_EasingEnabled;

        const int kFieldCount = 12;
        const int kWindowWidth = 290;
        const int kContentPadding = 4;
        const int kWindowHeight = ((int)EditorGUI.kSingleLineHeight) * kFieldCount + kContentPadding * 2;
        const float kPrefixLabelWidth = 120f;
        const float kNearClipMin = .01f;
        const int k_HeaderSpacing = 2;
        // FOV values chosen to be the smallest and largest before extreme visual corruption
        const float k_MinFieldOfView = 4f;
        const float k_MaxFieldOfView = 160f;

        Vector2 m_WindowSize;
        Vector2 m_Scroll;
        float[] m_Vector2Floats = { 0, 0 };

        public static event Action<SceneView> additionalSettingsGui;

        public override Vector2 GetWindowSize()
        {
            return m_WindowSize;
        }

        SceneViewCameraWindow() {}

        public SceneViewCameraWindow(SceneView sceneView)
        {
            m_SceneView = sceneView;

            m_CameraSpeedSliderContent = EditorGUIUtility.TrTextContent("Camera Speed", "The current speed of the camera in the Scene view.");
            m_AccelerationEnabled = EditorGUIUtility.TrTextContent("Camera Acceleration", "Check this to enable acceleration when moving the camera. When enabled, camera speed is evaluated as a modifier. With acceleration disabled, the camera is accelerated to the Camera Speed.");
            m_FieldOfView = EditorGUIUtility.TrTextContent("Field of View", "The height of the camera's view angle. Measured in degrees vertically, or along the local Y axis.");
            m_DynamicClip = EditorGUIUtility.TrTextContent("Dynamic Clipping", "Check this to enable camera's near and far clipping planes to be calculated relative to the viewport size of the Scene.");
            m_OcclusionCulling = EditorGUIUtility.TrTextContent("Occlusion Culling", "Check this to enable occlusion culling in the Scene view. Occlusion culling disables rendering of objects when they\'re not currently seen by the camera because they\'re hidden (occluded) by other objects.");
            m_EasingEnabled = EditorGUIUtility.TrTextContent("Camera Easing", "Check this to enable camera movement easing. This makes the camera ease in when it starts moving and ease out when it stops.");
            m_WindowSize = new Vector2(kWindowWidth, kWindowHeight);
            m_MinMaxContent = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("Min", "The minimum speed of the camera in the Scene view. Valid values are between [0.001, 98]."),
                EditorGUIUtility.TrTextContent("Max", "The maximum speed of the camera in the Scene view. Valid values are between [0.002, 99].")
            };
        }

        public override void OnGUI(Rect rect)
        {
            if (m_SceneView == null || m_SceneView.sceneViewState == null)
                return;

            Draw();

            // Escape closes the window
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }
        }

        void Draw()
        {
            Styles.Init();

            var settings = m_SceneView.cameraSettings;

            m_Scroll = GUILayout.BeginScrollView(m_Scroll);

            GUILayout.BeginVertical(Styles.settingsArea);
            EditorGUI.BeginChangeCheck();

            GUILayout.Space(k_HeaderSpacing);

            GUILayout.BeginHorizontal();
            GUILayout.Label(EditorGUIUtility.TrTextContent("Scene Camera"), EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(EditorGUI.GUIContents.titleSettingsIcon, EditorStyles.iconButton))
                ShowContextMenu();
            GUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = kPrefixLabelWidth;

            // fov isn't applicable in orthographic mode, and orthographic size is controlled by the user zoom
            using (new EditorGUI.DisabledScope(m_SceneView.orthographic))
            {
                settings.fieldOfView = EditorGUILayout.Slider(m_FieldOfView, settings.fieldOfView, k_MinFieldOfView, k_MaxFieldOfView);
            }

            settings.dynamicClip = EditorGUILayout.Toggle(m_DynamicClip, settings.dynamicClip);

            using (new EditorGUI.DisabledScope(settings.dynamicClip))
            {
                float near = settings.nearClip, far = settings.farClip;
                DrawClipPlanesField(EditorGUI.s_ClipingPlanesLabel, ref near, ref far, EditorGUI.kNearFarLabelsWidth);
                settings.SetClipPlanes(near, far);
            }

            settings.occlusionCulling = EditorGUILayout.Toggle(m_OcclusionCulling, settings.occlusionCulling);

            if (EditorGUI.EndChangeCheck())
                m_SceneView.Repaint();

            EditorGUILayout.Space(k_HeaderSpacing);

            GUILayout.Label(EditorGUIUtility.TrTextContent("Navigation"), EditorStyles.boldLabel);

            settings.easingEnabled = EditorGUILayout.Toggle(m_EasingEnabled, settings.easingEnabled);
            settings.accelerationEnabled = EditorGUILayout.Toggle(m_AccelerationEnabled, settings.accelerationEnabled);

            EditorGUI.BeginChangeCheck();
            float min = settings.speedMin, max = settings.speedMax, speed = settings.RoundSpeedToNearestSignificantDecimal(settings.speed);
            speed = EditorGUILayout.Slider(m_CameraSpeedSliderContent, speed, min, max);
            if (EditorGUI.EndChangeCheck())
                settings.speed = settings.RoundSpeedToNearestSignificantDecimal(speed);

            EditorGUI.BeginChangeCheck();

            m_Vector2Floats[0] = settings.speedMin;
            m_Vector2Floats[1] = settings.speedMax;

            DrawSpeedMinMaxFields();

            if (EditorGUI.EndChangeCheck())
                settings.SetSpeedMinMax(m_Vector2Floats);

            EditorGUIUtility.labelWidth = 0f;

            if (additionalSettingsGui != null)
            {
                EditorGUILayout.Space(k_HeaderSpacing);
                additionalSettingsGui(m_SceneView);
            }

            if (Event.current.type == EventType.Repaint)
                m_WindowSize.y = Math.Min(GUILayoutUtility.GetLastRect().yMax + kContentPadding, kWindowHeight * 3);

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        void DrawSpeedMinMaxFields()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            Rect r = EditorGUILayout.GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.numberField);
            EditorGUI.MultiFloatField(r, m_MinMaxContent, m_Vector2Floats);
            GUILayout.EndHorizontal();
        }

        void DrawClipPlanesField(GUIContent label, ref float near, ref float far, float propertyLabelsWidth, params GUILayoutOption[] options)
        {
            bool hasLabel = EditorGUI.LabelHasContent(label);
            float height = EditorGUI.kSingleLineHeight * 2 + EditorGUI.kVerticalSpacingMultiField;
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
