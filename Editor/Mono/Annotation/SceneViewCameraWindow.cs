// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityEditor
{
    public class SceneViewCameraWindow : PopupWindowContent
    {
        static class Styles
        {
            public static readonly GUIContent copyPlacementLabel = EditorGUIUtility.TrTextContent("Copy Placement");
            public static readonly GUIContent pastePlacementLabel = EditorGUIUtility.TrTextContent("Paste Placement");
            public static readonly GUIContent copySettingsLabel = EditorGUIUtility.TrTextContent("Copy Settings");
            public static readonly GUIContent pasteSettingsLabel = EditorGUIUtility.TrTextContent("Paste Settings");
            public static readonly GUIContent resetSettingsLabel = EditorGUIUtility.TrTextContent("Reset Settings");
            public static readonly GUIContent cameraSpeedRange = EditorGUIUtility.TrTextContent(" ");

            public static readonly GUIStyle settingsArea;

            static Styles()
            {
                settingsArea = new GUIStyle
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
        GUIContent m_SceneCameraLabel = EditorGUIUtility.TrTextContent("Scene Camera");
        GUIContent m_NavigationLabel = EditorGUIUtility.TrTextContent("Navigation");

        readonly string k_ClippingPlaneWarning = L10n.Tr("Using extreme values between the near and far planes may cause rendering issues. In general, to get better precision move the Near plane as far as possible.");

        const int kFieldCount = 13;
        const int kWindowWidth = 290;
        const int kContentPadding = 4;
        const int k_HeaderSpacing = 2;
        const int kWindowHeight = ((int)EditorGUI.kSingleLineHeight) * kFieldCount + kContentPadding * 2 + k_HeaderSpacing * 2;
        const float kPrefixLabelWidth = 120f;

        // FOV values chosen to be the smallest and largest before extreme visual corruption
        const float k_MinFieldOfView = 4f;
        const float k_MaxFieldOfView = 120f;

        Vector2 m_WindowSize;
        Vector2 m_Scroll;

        public static event Action<SceneView> additionalSettingsGui;

        public override Vector2 GetWindowSize()
        {
            return m_WindowSize;
        }

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
            m_MinMaxContent = new[]
            {
                EditorGUIUtility.TrTextContent("Min", $"The minimum speed of the camera in the Scene view. Valid values are between [{SceneView.CameraSettings.kAbsoluteSpeedMin}, {SceneView.CameraSettings.kAbsoluteSpeedMax - 1}]."),
                EditorGUIUtility.TrTextContent("Max", $"The maximum speed of the camera in the Scene view. Valid values are between [{SceneView.CameraSettings.kAbsoluteSpeedMin + .0001f}, {SceneView.CameraSettings.kAbsoluteSpeedMax}].")
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
            var settings = m_SceneView.cameraSettings;

            m_Scroll = GUILayout.BeginScrollView(m_Scroll);

            GUILayout.BeginVertical(Styles.settingsArea);
            EditorGUI.BeginChangeCheck();

            GUILayout.Space(k_HeaderSpacing);

            GUILayout.BeginHorizontal();
            GUILayout.Label(m_SceneCameraLabel, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(EditorGUI.GUIContents.titleSettingsIcon, EditorStyles.iconButton))
                ShowContextMenu(m_SceneView);
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
                DrawStackedFloatField(EditorGUI.s_ClipingPlanesLabel,
                    EditorGUI.s_NearAndFarLabels[0],
                    EditorGUI.s_NearAndFarLabels[1], ref near, ref far,
                    EditorGUI.kNearFarLabelsWidth);
                settings.SetClipPlanes(near, far);
                if(far/near > 10000000 || near < 0.0001f)
                    EditorGUILayout.HelpBox(k_ClippingPlaneWarning,MessageType.Warning);
            }

            settings.occlusionCulling = EditorGUILayout.Toggle(m_OcclusionCulling, settings.occlusionCulling);

            if (EditorGUI.EndChangeCheck())
                m_SceneView.Repaint();

            EditorGUILayout.Space(k_HeaderSpacing);

            GUILayout.Label(m_NavigationLabel, EditorStyles.boldLabel);

            settings.easingEnabled = EditorGUILayout.Toggle(m_EasingEnabled, settings.easingEnabled);
            settings.accelerationEnabled = EditorGUILayout.Toggle(m_AccelerationEnabled, settings.accelerationEnabled);

            EditorGUI.BeginChangeCheck();
            float min = settings.speedMin, max = settings.speedMax, speed = settings.RoundSpeedToNearestSignificantDecimal(settings.speed);
            speed = EditorGUILayout.Slider(m_CameraSpeedSliderContent, speed, min, max);
            if (EditorGUI.EndChangeCheck())
                settings.speed = settings.RoundSpeedToNearestSignificantDecimal(speed);

            EditorGUI.BeginChangeCheck();

            DrawStackedFloatField(Styles.cameraSpeedRange,
                m_MinMaxContent[0],
                m_MinMaxContent[1],
                ref min, ref max,
                EditorGUI.kNearFarLabelsWidth);


            if (EditorGUI.EndChangeCheck())
                settings.SetSpeedMinMax(min, max);

            EditorGUIUtility.labelWidth = 0f;

            if (additionalSettingsGui != null)
            {
                EditorGUILayout.Space(k_HeaderSpacing);
                additionalSettingsGui(m_SceneView);

                if (Event.current.type == EventType.Repaint)
                    m_WindowSize.y = Math.Min(GUILayoutUtility.GetLastRect().yMax + kContentPadding * 2, kWindowHeight * 3);
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        static void DrawStackedFloatField(GUIContent label, GUIContent firstFieldLabel, GUIContent secondFieldLabel, ref float near, ref float far, float propertyLabelsWidth, params GUILayoutOption[] options)
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

            near = EditorGUI.FloatField(fieldPosition, firstFieldLabel, near);
            fieldPosition.y += EditorGUI.kSingleLineHeight + EditorGUI.kVerticalSpacingMultiField;
            far = EditorGUI.FloatField(fieldPosition, secondFieldLabel, far);

            EditorGUI.indentLevel = oldIndentLevel;
            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        internal static void ShowContextMenu(SceneView view)
        {
            var menu = new GenericMenu();
            menu.AddItem(Styles.copyPlacementLabel, false, () => CopyPlacement(view));
            if (CanPastePlacement())
                menu.AddItem(Styles.pastePlacementLabel, false, () => PastePlacement(view));
            else
                menu.AddDisabledItem(Styles.pastePlacementLabel);
            menu.AddItem(Styles.copySettingsLabel, false, () => CopySettings(view));
            if (Clipboard.HasCustomValue<SceneView.CameraSettings>())
                menu.AddItem(Styles.pasteSettingsLabel, false, () => PasteSettings(view));
            else
                menu.AddDisabledItem(Styles.pasteSettingsLabel);
            menu.AddItem(Styles.resetSettingsLabel, false, () => ResetSettings(view));

            menu.ShowAsContext();
        }

        // ReSharper disable once UnusedMember.Local - called by a shortcut
        [Shortcut("Camera/Copy Placement")]
        static void CopyPlacementShortcut()
        {
            // if we are interacting with a game view, copy the main camera placement
            var playView = PlayModeView.GetLastFocusedPlayModeView();
            if (playView != null && (EditorWindow.focusedWindow == playView || EditorWindow.mouseOverWindow == playView))
            {
                var cam = Camera.main;
                if (cam != null)
                    Clipboard.SetCustomValue(new TransformWorldPlacement(cam.transform));
            }
            // otherwise copy the last active scene view placement
            else
            {
                var sceneView = SceneView.lastActiveSceneView;
                if (sceneView != null)
                    CopyPlacement(sceneView);
            }
        }

        static void CopyPlacement(SceneView view)
        {
            Clipboard.SetCustomValue(new TransformWorldPlacement(view.camera.transform));
        }

        // ReSharper disable once UnusedMember.Local - called by a shortcut
        [Shortcut("Camera/Paste Placement")]
        static void PastePlacementShortcut()
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null) return;
            if (CanPastePlacement())
                PastePlacement(sceneView);
        }

        static bool CanPastePlacement()
        {
            return Clipboard.HasCustomValue<TransformWorldPlacement>();
        }

        static void PastePlacement(SceneView view)
        {
            var tr = view.camera.transform;
            var placement = Clipboard.GetCustomValue<TransformWorldPlacement>();
            tr.position = placement.position;
            tr.rotation = placement.rotation;
            tr.localScale = placement.scale;

            // Similar to what AlignViewToObject does, except we need to do that instantly
            // in case the shortcut key was pressed while FPS camera controls (right click drag)
            // were active.
            view.size = 10;
            view.LookAt(tr.position + tr.forward * view.cameraDistance, tr.rotation, view.size, view.orthographic, true);

            view.Repaint();
        }

        static void CopySettings(SceneView view)
        {
            Clipboard.SetCustomValue(view.cameraSettings);
        }

        static void PasteSettings(SceneView view)
        {
            view.cameraSettings = Clipboard.GetCustomValue<SceneView.CameraSettings>();
            view.Repaint();
        }

        static void ResetSettings(SceneView view)
        {
            view.ResetCameraSettings();
            view.Repaint();
        }
    }
}
