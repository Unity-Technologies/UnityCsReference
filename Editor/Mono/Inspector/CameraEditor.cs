// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;
using UnityEngine.Experimental.Rendering;
using AnimatedBool = UnityEditor.AnimatedValues.AnimBool;
using UnityEngine.Scripting;
using UnityEditor.Modules;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [CustomEditor(typeof(Camera))]
    [CanEditMultipleObjects]
    public class CameraEditor : Editor
    {
        private static class Styles
        {
            public static GUIContent iconRemove = EditorGUIUtility.TrIconContent("Toolbar Minus", "Remove command buffer");
            public static GUIContent clearFlags = EditorGUIUtility.TrTextContent("Clear Flags", "What to display in empty areas of this Camera's view.\n\nChoose Skybox to display a skybox in empty areas, defaulting to a background color if no skybox is found.\n\nChoose Solid Color to display a background color in empty areas.\n\nChoose Depth Only to display nothing in empty areas.\n\nChoose Don't Clear to display whatever was displayed in the previous frame in empty areas.");
            public static GUIContent background = EditorGUIUtility.TrTextContent("Background", "The Camera clears the screen to this color before rendering.");
            public static GUIContent projection = EditorGUIUtility.TrTextContent("Projection", "How the Camera renders perspective.\n\nChoose Perspective to render objects with perspective.\n\nChoose Orthographic to render objects uniformly, with no sense of perspective.");
            public static GUIContent size = EditorGUIUtility.TrTextContent("Size", "The vertical size of the camera view.");
            public static GUIContent fieldOfView = EditorGUIUtility.TrTextContent("Field of View", "The camera's view angle measured in degrees along the selected axis.");
            public static GUIContent viewportRect = EditorGUIUtility.TrTextContent("Viewport Rect", "Four values that indicate where on the screen this camera view will be drawn. Measured in Viewport Coordinates (values 0-1).");
            public static GUIContent sensorSize = EditorGUIUtility.TrTextContent("Sensor Size", "The size of the camera sensor in millimeters.");
            public static GUIContent lensShift = EditorGUIUtility.TrTextContent("Lens Shift", "Offset from the camera sensor. Use these properties to simulate a shift lens. Measured as a multiple of the sensor size.");
            public static GUIContent physicalCamera = EditorGUIUtility.TrTextContent("Physical Camera", "Enables Physical camera mode. When checked, the field of view is calculated from properties for simulating physical attributes (focal length, sensor size, and lens shift)");
            public static GUIContent cameraType = EditorGUIUtility.TrTextContent("Sensor Type", "Common sensor sizes. Choose an item to set Sensor Size, or edit Sensor Size for your custom settings.");
            public static GUIContent renderingPath = EditorGUIUtility.TrTextContent("Rendering Path", "Choose a rendering method for this camera.\n\nUse Graphics Settings to use the rendering path specified in Player settings.\n\nUse Forward to render all objects with one pass per material.\n\nUse Deferred to draw all objects once without lighting and then draw the lighting of all objects at the end of the render queue.\n\nUse Legacy Vertex Lit to render all lights in a single pass, calculated in vertices.\n\nLegacy Deferred has been deprecated.");
            public static GUIContent focalLength = EditorGUIUtility.TrTextContent("Focal Length", "The simulated distance between the lens and the sensor of the physical camera. Larger values give a narrower field of view.");
            public static GUIContent allowOcclusionCulling = EditorGUIUtility.TrTextContent("Occlusion Culling", "Occlusion Culling disables rendering of objects when they are not currently seen by the camera because they are obscured (occluded) by other objects.");
            public static GUIContent allowHDR = EditorGUIUtility.TrTextContent("HDR", "High Dynamic Range gives you a wider range of light intensities, so your lighting looks more realistic. With it, you can still see details and experience less saturation even with bright light.");
            public static GUIContent allowMSAA = EditorGUIUtility.TrTextContent("MSAA", "Use Multi Sample Anti-aliasing to reduce aliasing.");
            public static GUIContent gateFit = EditorGUIUtility.TrTextContent("Gate Fit", "Determines how the rendered area (resolution gate) fits into the sensor area (film gate).");
            public static GUIContent allowDynamicResolution = EditorGUIUtility.TrTextContent("Allow Dynamic Resolution", "Scales render textures to support dynamic resolution if the target platform/graphics API supports it.");
            public static GUIContent FOVAxisMode = EditorGUIUtility.TrTextContent("FOV Axis", "Field of view axis.");
            public static GUIStyle invisibleButton = "InvisibleButton";
            public static GUIContent[] displayedOptions = new[] { new GUIContent("Off"), new GUIContent("Use Graphics Settings") };
            public static int[] optionValues = new[] { 0, 1 };
        }
        public sealed class Settings
        {
            private SerializedObject m_SerializedObject;
            public Settings(SerializedObject so)
            {
                m_SerializedObject = so;
            }

            public static IEnumerable<string> ApertureFormatNames => k_ApertureFormatNames;

            private static readonly string[] k_ApertureFormatNames =
            {
                "8mm",
                "Super 8mm",
                "16mm",
                "Super 16mm",
                "35mm 2-perf",
                "35mm Academy",
                "Super-35",
                "65mm ALEXA",
                "70mm",
                "70mm IMAX",
                "Custom"
            };

            public static IEnumerable<Vector2> ApertureFormatValues => k_ApertureFormatValues;

            private static readonly Vector2[] k_ApertureFormatValues =
            {
                new Vector2(4.8f, 3.5f) , // 8mm
                new Vector2(5.79f, 4.01f) , // Super 8mm
                new Vector2(10.26f, 7.49f) , // 16mm
                new Vector2(12.52f, 7.41f) , // Super 16mm
                new Vector2(21.95f, 9.35f) , // 35mm 2-perf
                new Vector2(21.0f, 15.2f) , // 35mm academy
                new Vector2(24.89f, 18.66f) , // Super-35
                new Vector2(54.12f, 25.59f) , // 65mm ALEXA
                new Vector2(70.0f, 51.0f) , // 70mm
                new Vector2(70.41f, 52.63f), // 70mm IMAX
            };

            // Manually entered rendering path names/values, since we want to show them
            // in different order than they appear in the enum.
            private static readonly GUIContent[] kCameraRenderPaths =
            {
                EditorGUIUtility.TrTextContent("Use Graphics Settings"),
                EditorGUIUtility.TrTextContent("Forward"),
                EditorGUIUtility.TrTextContent("Deferred"),
                EditorGUIUtility.TrTextContent("Legacy Vertex Lit"),
                EditorGUIUtility.TrTextContent("Legacy Deferred (light prepass)")
            };
            private static readonly int[] kCameraRenderPathValues =
            {
                (int)RenderingPath.UsePlayerSettings,
                (int)RenderingPath.Forward,
                (int)RenderingPath.DeferredShading,
                (int)RenderingPath.VertexLit,
                (int)RenderingPath.DeferredLighting
            };

            public SerializedProperty clearFlags { get; private set; }
            public SerializedProperty backgroundColor { get; private set; }
            public SerializedProperty normalizedViewPortRect { get; private set; }
            internal SerializedProperty projectionMatrixMode { get; private set; }
            public SerializedProperty sensorSize { get; private set; }
            public SerializedProperty lensShift { get; private set; }
            public SerializedProperty focalLength { get; private set; }
            public SerializedProperty gateFit { get; private set; }
            public SerializedProperty verticalFOV { get; private set; }
            public SerializedProperty orthographic { get; private set; }
            public SerializedProperty orthographicSize { get; private set; }
            public SerializedProperty depth { get; private set; }
            public SerializedProperty cullingMask { get; private set; }
            public SerializedProperty renderingPath { get; private set; }
            public SerializedProperty occlusionCulling { get; private set; }
            public SerializedProperty targetTexture { get; private set; }
            public SerializedProperty HDR { get; private set; }
            public SerializedProperty allowMSAA { get; private set; }
            public SerializedProperty allowDynamicResolution { get; private set; }
            public SerializedProperty stereoConvergence { get; private set; }
            public SerializedProperty stereoSeparation { get; private set; }
            public SerializedProperty nearClippingPlane { get; private set; }
            public SerializedProperty farClippingPlane { get; private set; }
            public SerializedProperty fovAxisMode { get; private set; }

            public SerializedProperty targetDisplay { get; private set; }

            public SerializedProperty targetEye { get; private set; }

            private static readonly GUIContent[] kTargetEyes =
            {
                EditorGUIUtility.TrTextContent("Both"),
                EditorGUIUtility.TrTextContent("Left"),
                EditorGUIUtility.TrTextContent("Right"),
                EditorGUIUtility.TrTextContent("None (Main Display)"),
            };
            private static readonly int[] kTargetEyeValues = { (int)StereoTargetEyeMask.Both, (int)StereoTargetEyeMask.Left, (int)StereoTargetEyeMask.Right, (int)StereoTargetEyeMask.None };

            public void OnEnable()
            {
                clearFlags = m_SerializedObject.FindProperty("m_ClearFlags");
                backgroundColor = m_SerializedObject.FindProperty("m_BackGroundColor");
                normalizedViewPortRect = m_SerializedObject.FindProperty("m_NormalizedViewPortRect");
                sensorSize = m_SerializedObject.FindProperty("m_SensorSize");
                lensShift = m_SerializedObject.FindProperty("m_LensShift");
                focalLength = m_SerializedObject.FindProperty("m_FocalLength");
                gateFit = m_SerializedObject.FindProperty("m_GateFitMode");
                projectionMatrixMode = m_SerializedObject.FindProperty("m_projectionMatrixMode");
                nearClippingPlane = m_SerializedObject.FindProperty("near clip plane");
                farClippingPlane = m_SerializedObject.FindProperty("far clip plane");
                verticalFOV = m_SerializedObject.FindProperty("field of view");
                fovAxisMode = m_SerializedObject.FindProperty("m_FOVAxisMode");
                orthographic = m_SerializedObject.FindProperty("orthographic");
                orthographicSize = m_SerializedObject.FindProperty("orthographic size");
                depth = m_SerializedObject.FindProperty("m_Depth");
                cullingMask = m_SerializedObject.FindProperty("m_CullingMask");
                renderingPath = m_SerializedObject.FindProperty("m_RenderingPath");
                occlusionCulling = m_SerializedObject.FindProperty("m_OcclusionCulling");
                targetTexture = m_SerializedObject.FindProperty("m_TargetTexture");
                HDR = m_SerializedObject.FindProperty("m_HDR");
                allowMSAA = m_SerializedObject.FindProperty("m_AllowMSAA");
                allowDynamicResolution = m_SerializedObject.FindProperty("m_AllowDynamicResolution");

                stereoConvergence = m_SerializedObject.FindProperty("m_StereoConvergence");
                stereoSeparation = m_SerializedObject.FindProperty("m_StereoSeparation");

                targetDisplay = m_SerializedObject.FindProperty("m_TargetDisplay");

                targetEye = m_SerializedObject.FindProperty("m_TargetEye");
            }

            public void Update()
            {
                m_SerializedObject.Update();
            }

            public void ApplyModifiedProperties()
            {
                m_SerializedObject.ApplyModifiedProperties();
            }

            public void DrawClearFlags()
            {
                EditorGUILayout.PropertyField(clearFlags, Styles.clearFlags);
            }

            public void DrawBackgroundColor()
            {
                EditorGUILayout.PropertyField(backgroundColor, Styles.background);
            }

            public void DrawCullingMask()
            {
                EditorGUILayout.PropertyField(cullingMask);
            }

            public void DrawProjection()
            {
                ProjectionType projectionType = orthographic.boolValue ? ProjectionType.Orthographic : ProjectionType.Perspective;
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = orthographic.hasMultipleDifferentValues;
                projectionType = (ProjectionType)EditorGUILayout.EnumPopup(Styles.projection, projectionType);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                    orthographic.boolValue = (projectionType == ProjectionType.Orthographic);

                if (!orthographic.hasMultipleDifferentValues)
                {
                    if (projectionType == ProjectionType.Orthographic)
                        EditorGUILayout.PropertyField(orthographicSize, Styles.size);
                    else
                    {
                        float fovCurrentValue;
                        bool multipleDifferentFovValues = false;
                        bool isPhysicalCamera = projectionMatrixMode.intValue == (int)Camera.ProjectionMatrixMode.PhysicalPropertiesBased;

                        var rect = EditorGUILayout.GetControlRect();
                        var guiContent = EditorGUI.BeginProperty(rect, Styles.FOVAxisMode, fovAxisMode);
                        EditorGUI.showMixedValue = fovAxisMode.hasMultipleDifferentValues;

                        EditorGUI.BeginChangeCheck();
                        var fovAxisNewVal = (int)(Camera.FieldOfViewAxis)EditorGUI.EnumPopup(rect, guiContent, (Camera.FieldOfViewAxis)fovAxisMode.intValue);
                        if (EditorGUI.EndChangeCheck())
                            fovAxisMode.intValue = fovAxisNewVal;
                        EditorGUI.EndProperty();

                        bool fovAxisVertical = fovAxisMode.intValue == 0;

                        if (!fovAxisVertical && !fovAxisMode.hasMultipleDifferentValues)
                        {
                            var targets = m_SerializedObject.targetObjects;
                            var camera0 = targets[0] as Camera;
                            float aspectRatio = isPhysicalCamera ? sensorSize.vector2Value.x / sensorSize.vector2Value.y : camera0.aspect;
                            // camera.aspect is not serialized so we have to check all targets.
                            fovCurrentValue = Camera.VerticalToHorizontalFieldOfView(camera0.fieldOfView, aspectRatio);
                            if (m_SerializedObject.targetObjectsCount > 1)
                            {
                                foreach (Camera camera in targets)
                                {
                                    if (camera.fieldOfView != fovCurrentValue)
                                    {
                                        multipleDifferentFovValues = true;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            fovCurrentValue = verticalFOV.floatValue;
                            multipleDifferentFovValues = fovAxisMode.hasMultipleDifferentValues;
                        }

                        EditorGUI.showMixedValue = multipleDifferentFovValues;
                        var content = EditorGUI.BeginProperty(EditorGUILayout.BeginHorizontal(), Styles.fieldOfView, verticalFOV);
                        EditorGUI.BeginDisabled(projectionMatrixMode.hasMultipleDifferentValues || isPhysicalCamera && (sensorSize.hasMultipleDifferentValues || fovAxisMode.hasMultipleDifferentValues));
                        EditorGUI.BeginChangeCheck();
                        var fovNewValue = EditorGUILayout.Slider(content, fovCurrentValue, 0.00001f, 179f);
                        var fovChanged = EditorGUI.EndChangeCheck();
                        EditorGUI.EndDisabled();
                        EditorGUILayout.EndHorizontal();
                        EditorGUI.EndProperty();
                        EditorGUI.showMixedValue = false;

                        content = EditorGUI.BeginProperty(EditorGUILayout.BeginHorizontal(), Styles.physicalCamera, projectionMatrixMode);
                        EditorGUI.showMixedValue = projectionMatrixMode.hasMultipleDifferentValues;

                        EditorGUI.BeginChangeCheck();
                        isPhysicalCamera = EditorGUILayout.Toggle(content, isPhysicalCamera);
                        if (EditorGUI.EndChangeCheck())
                            projectionMatrixMode.intValue = isPhysicalCamera ? (int)Camera.ProjectionMatrixMode.PhysicalPropertiesBased : (int)Camera.ProjectionMatrixMode.Implicit;
                        EditorGUILayout.EndHorizontal();
                        EditorGUI.EndProperty();

                        EditorGUI.showMixedValue = false;
                        if (isPhysicalCamera && !projectionMatrixMode.hasMultipleDifferentValues)
                        {
                            using (new EditorGUI.IndentLevelScope())
                            {
                                using (var horizontal = new EditorGUILayout.HorizontalScope())
                                using (new EditorGUI.PropertyScope(horizontal.rect, Styles.focalLength, focalLength))
                                using (var checkScope = new EditorGUI.ChangeCheckScope())
                                {
                                    EditorGUI.showMixedValue = focalLength.hasMultipleDifferentValues;
                                    float sensorLength = fovAxisVertical ? sensorSize.vector2Value.y : sensorSize.vector2Value.x;
                                    float focalLengthVal = fovChanged ? Camera.FieldOfViewToFocalLength(fovNewValue, sensorLength) : focalLength.floatValue;
                                    focalLengthVal = EditorGUILayout.FloatField(Styles.focalLength, focalLengthVal);
                                    if (checkScope.changed || fovChanged)
                                        focalLength.floatValue = focalLengthVal;
                                }

                                EditorGUI.showMixedValue = sensorSize.hasMultipleDifferentValues;
                                EditorGUI.BeginChangeCheck();
                                int filmGateIndex = Array.IndexOf(k_ApertureFormatValues, new Vector2((float)Math.Round(sensorSize.vector2Value.x, 3), (float)Math.Round(sensorSize.vector2Value.y, 3)));
                                if (filmGateIndex == -1)
                                    filmGateIndex = EditorGUILayout.Popup(Styles.cameraType, k_ApertureFormatNames.Length - 1, k_ApertureFormatNames);
                                else
                                    filmGateIndex = EditorGUILayout.Popup(Styles.cameraType, filmGateIndex, k_ApertureFormatNames);
                                EditorGUI.showMixedValue = false;
                                if (EditorGUI.EndChangeCheck() && filmGateIndex < k_ApertureFormatValues.Length)
                                {
                                    sensorSize.vector2Value = k_ApertureFormatValues[filmGateIndex];
                                }

                                EditorGUILayout.PropertyField(sensorSize, Styles.sensorSize);

                                EditorGUILayout.PropertyField(lensShift, Styles.lensShift);

                                using (var horizontal = new EditorGUILayout.HorizontalScope())
                                using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, Styles.gateFit, gateFit))
                                using (var checkScope = new EditorGUI.ChangeCheckScope())
                                {
                                    int gateValue = (int)(Camera.GateFitMode)EditorGUILayout.EnumPopup(propertyScope.content, (Camera.GateFitMode)gateFit.intValue);
                                    if (checkScope.changed)
                                        gateFit.intValue = gateValue;
                                }
                            }
                        }
                        else if (fovChanged)
                        {
                            verticalFOV.floatValue = fovAxisVertical ? fovNewValue : Camera.HorizontalToVerticalFieldOfView(fovNewValue, (m_SerializedObject.targetObjects[0] as Camera).aspect);
                        }
                        EditorGUILayout.Space();
                    }
                }
            }

            public void DrawClippingPlanes()
            {
                EditorGUILayout.PropertiesField(EditorGUI.s_ClipingPlanesLabel, new[] {nearClippingPlane, farClippingPlane}, EditorGUI.s_NearAndFarLabels, EditorGUI.kNearFarLabelsWidth);
            }

            public void DrawNormalizedViewPort()
            {
                EditorGUILayout.PropertyField(normalizedViewPortRect, Styles.viewportRect);
            }

            public void DrawDepth()
            {
                EditorGUILayout.PropertyField(depth);
            }

            public void DrawRenderingPath()
            {
                EditorGUILayout.IntPopup(renderingPath, kCameraRenderPaths, kCameraRenderPathValues, Styles.renderingPath);
            }

            public void DrawTargetTexture(bool deferred)
            {
                EditorGUILayout.PropertyField(targetTexture);

                // show warning if we have deferred but manual MSAA set
                // only do this if the m_TargetTexture has the same values across all target cameras
                if (!targetTexture.hasMultipleDifferentValues)
                {
                    var targetTexture = this.targetTexture.objectReferenceValue as RenderTexture;
                    if (targetTexture
                        && targetTexture.antiAliasing > 1
                        && deferred)
                    {
                        EditorGUILayout.HelpBox("Manual MSAA target set with deferred rendering. This will lead to undefined behavior.", MessageType.Warning, true);
                    }
                }
            }

            public void DrawOcclusionCulling()
            {
                EditorGUILayout.PropertyField(occlusionCulling, Styles.allowOcclusionCulling);
            }

            public void DrawHDR()
            {
                var rect = EditorGUILayout.GetControlRect(true);
                EditorGUI.BeginProperty(rect, Styles.allowHDR, HDR);
                int value = HDR.boolValue ? 1 : 0;
                value = EditorGUI.IntPopup(rect, Styles.allowHDR, value, Styles.displayedOptions, Styles.optionValues);
                HDR.boolValue = value == 1;
                EditorGUI.EndProperty();
            }

            public void DrawMSAA()
            {
                var rect = EditorGUILayout.GetControlRect(true);
                EditorGUI.BeginProperty(rect, Styles.allowMSAA, allowMSAA);
                int value = allowMSAA.boolValue ? 1 : 0;
                value = EditorGUI.IntPopup(rect, Styles.allowMSAA, value, Styles.displayedOptions, Styles.optionValues);
                allowMSAA.boolValue = value == 1;
                EditorGUI.EndProperty();
            }

            public void DrawDynamicResolution()
            {
                EditorGUILayout.PropertyField(allowDynamicResolution, Styles.allowDynamicResolution);
                if ((allowDynamicResolution.boolValue || allowDynamicResolution.hasMultipleDifferentValues) && !PlayerSettings.enableFrameTimingStats)
                    EditorGUILayout.HelpBox("It is recommended to enable Frame Timing Statistics under Rendering Player Settings when using dynamic resolution cameras.",
                        MessageType.Warning, true);
            }

            public void DrawVR()
            {
                if (PlayerSettings.virtualRealitySupported)
                {
                    EditorGUILayout.PropertyField(stereoSeparation);
                    EditorGUILayout.PropertyField(stereoConvergence);
                }
            }

            public void DrawMultiDisplay()
            {
                if (ModuleManager.ShouldShowMultiDisplayOption())
                {
                    int prevDisplay = targetDisplay.intValue;
                    EditorGUILayout.IntPopup(targetDisplay, DisplayUtility.GetDisplayNames(), DisplayUtility.GetDisplayIndices(), EditorGUIUtility.TempContent("Target Display"));
                    if (prevDisplay != targetDisplay.intValue)
                        GameView.RepaintAll();
                }
            }

            public void DrawTargetEye()
            {
                EditorGUILayout.IntPopup(targetEye, kTargetEyes, kTargetEyeValues, EditorGUIUtility.TempContent("Target Eye"));
            }

            public static void DrawCameraWarnings(Camera camera)
            {
                string[] warnings = camera.GetCameraBufferWarnings();
                if (warnings.Length > 0)
                    EditorGUILayout.HelpBox(String.Join("\n\n", warnings), MessageType.Info, true);
            }
        }

        readonly AnimatedBool m_ShowBGColorOptions = new AnimatedBool();
        readonly AnimatedBool m_ShowOrthoOptions = new AnimatedBool();
        readonly AnimatedBool m_ShowTargetEyeOption = new AnimatedBool();

        private Camera camera { get { return target as Camera; } }

        private static bool IsDeferredRenderingPath(RenderingPath rp) { return rp == RenderingPath.DeferredLighting || rp == RenderingPath.DeferredShading; }

        private bool wantDeferredRendering
        {
            get
            {
                bool isCamDeferred  = IsDeferredRenderingPath(camera.renderingPath);
                bool isTierDeferred = IsDeferredRenderingPath(Rendering.EditorGraphicsSettings.GetCurrentTierSettings().renderingPath);
                return isCamDeferred || (camera.renderingPath == RenderingPath.UsePlayerSettings && isTierDeferred);
            }
        }

        enum ProjectionType { Perspective, Orthographic }

        private Camera m_PreviewCamera;
        protected Camera previewCamera
        {
            get
            {
                if (m_PreviewCamera == null)
                    m_PreviewCamera = EditorUtility.CreateGameObjectWithHideFlags("Preview Camera", HideFlags.HideAndDontSave, typeof(Camera), typeof(Skybox)).GetComponent<Camera>();
                m_PreviewCamera.enabled = false;
                return m_PreviewCamera;
            }
        }

        private RenderTexture m_PreviewTexture;

        int m_QualitySettingsAntiAliasing = -1;

        // should match color in GizmosDrawers.cpp
        private const float kPreviewNormalizedSize = 0.2f;

        private bool m_CommandBuffersShown = true;

        private Settings m_Settings;
        protected Settings settings
        {
            get
            {
                if (m_Settings == null)
                    m_Settings = new Settings(serializedObject);
                return m_Settings;
            }
        }

        bool clearFlagsHasMultipleValues
        {
            get { return settings.clearFlags.hasMultipleDifferentValues; }
        }
        bool orthographicHasMultipleValues
        {
            get { return settings.orthographic.hasMultipleDifferentValues; }
        }

        int targetEyeValue
        {
            get { return settings.targetEye.intValue; }
        }

        static List<XRDisplaySubsystemDescriptor> displayDescriptors = new List<XRDisplaySubsystemDescriptor>();

        static void OnReloadSubsystemsComplete()
        {
            SubsystemManager.GetSubsystemDescriptors(displayDescriptors);
        }

        Dictionary<Camera, OverlayWindow> m_OverlayWindows = new Dictionary<Camera, OverlayWindow>();

        public void OnEnable()
        {
            settings.OnEnable();

            var c = (Camera)target;
            m_ShowBGColorOptions.value = !clearFlagsHasMultipleValues && (c.clearFlags == CameraClearFlags.SolidColor || c.clearFlags == CameraClearFlags.Skybox);
            m_ShowOrthoOptions.value = c.orthographic;
            m_ShowTargetEyeOption.value = targetEyeValue != (int)StereoTargetEyeMask.Both || PlayerSettings.virtualRealitySupported;

            m_ShowBGColorOptions.valueChanged.AddListener(Repaint);
            m_ShowOrthoOptions.valueChanged.AddListener(Repaint);
            m_ShowTargetEyeOption.valueChanged.AddListener(Repaint);

            SubsystemManager.GetSubsystemDescriptors(displayDescriptors);
            SubsystemManager.afterReloadSubsystems += OnReloadSubsystemsComplete;

            SceneView.duringSceneGui += DuringSceneGUI;

            foreach (var camera in targets)
            {
                m_OverlayWindows[(Camera)camera] = new OverlayWindow(new GUIContent(camera.name), OnOverlayGUI,
                    (int)SceneViewOverlay.Ordering.Camera, camera,
                    SceneViewOverlay.WindowDisplayOption.OneWindowPerTarget);
            }
        }

        public void OnDisable()
        {
            m_ShowBGColorOptions.valueChanged.RemoveListener(Repaint);
            m_ShowOrthoOptions.valueChanged.RemoveListener(Repaint);
            m_ShowTargetEyeOption.valueChanged.RemoveListener(Repaint);
            SceneView.duringSceneGui -= DuringSceneGUI;
        }

        public void OnDestroy()
        {
            if (m_PreviewCamera != null)
                DestroyImmediate(m_PreviewCamera.gameObject, true);
        }

        private void DepthTextureModeGUI()
        {
            // Camera's depth texture mode is not serialized data, so can't get to it through
            // serialized property (hence no multi-edit).
            if (targets.Length != 1)
                return;
            var cam = target as Camera;
            if (cam == null || cam.depthTextureMode == DepthTextureMode.None)
                return;

            List<string> buffers = new List<string>();
            if ((cam.depthTextureMode & DepthTextureMode.Depth) != 0)
                buffers.Add("Depth");
            if ((cam.depthTextureMode & DepthTextureMode.DepthNormals) != 0)
                buffers.Add("DepthNormals");
            if ((cam.depthTextureMode & DepthTextureMode.MotionVectors) != 0)
                buffers.Add("MotionVectors");
            if (buffers.Count == 0)
                return;

            StringBuilder sb = new StringBuilder("Info: renders ");
            for (int i = 0; i < buffers.Count; ++i)
            {
                if (i != 0)
                    sb.Append(" & ");

                sb.Append(buffers[i]);
            }
            sb.Append(buffers.Count > 1 ? " textures" : " texture");
            EditorGUILayout.HelpBox(sb.ToString(), MessageType.None, true);
        }

        static Rect GetRemoveButtonRect(Rect r)
        {
            var buttonSize = Styles.invisibleButton.CalcSize(Styles.iconRemove);
            return new Rect(r.xMax - buttonSize.x, r.y + (int)(r.height / 2 - buttonSize.y / 2), buttonSize.x, buttonSize.y);
        }

        /**
         * Draws the 2D bounds of the camera when in 2D mode.
         */
        [DrawGizmo(GizmoType.NonSelected)]
        static void DrawCameraBound(Camera camera, GizmoType gizmoType)
        {
            var sv = SceneView.currentDrawingSceneView;
            if (sv != null && sv.in2DMode)
            {
                if (camera == Camera.main && camera.orthographic)
                    CameraEditor.RenderGizmo(camera);
            }
        }

        private void CommandBufferGUI()
        {
            // Command buffers are not serialized data, so can't get to them through
            // serialized property (hence no multi-edit).
            if (targets.Length != 1)
                return;
            var cam = target as Camera;
            if (cam == null)
                return;
            int count = cam.commandBufferCount;
            if (count == 0)
                return;

            m_CommandBuffersShown = GUILayout.Toggle(m_CommandBuffersShown, GUIContent.Temp(count + " command buffers"), EditorStyles.foldout);
            if (!m_CommandBuffersShown)
                return;
            EditorGUI.indentLevel++;
            foreach (CameraEvent ce in (CameraEvent[])System.Enum.GetValues(typeof(CameraEvent)))
            {
                CommandBuffer[] cbs = cam.GetCommandBuffers(ce);
                foreach (CommandBuffer cb in cbs)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        // row with event & command buffer information label
                        Rect rowRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.miniLabel);
                        rowRect.xMin += EditorGUI.indent;
                        Rect minusRect = GetRemoveButtonRect(rowRect);
                        rowRect.xMax = minusRect.x;
                        GUI.Label(rowRect, string.Format("{0}: {1} ({2})", ce, cb.name, EditorUtility.FormatBytes(cb.sizeInBytes)), EditorStyles.miniLabel);
                        // and a button to remove it
                        if (GUI.Button(minusRect, Styles.iconRemove, Styles.invisibleButton))
                        {
                            cam.RemoveCommandBuffer(ce, cb);
                            SceneView.RepaintAll();
                            PlayModeView.RepaintAll();
                            GUIUtility.ExitGUI();
                        }
                    }
                }
            }
            // "remove all" button
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Remove all", EditorStyles.miniButton))
                {
                    cam.RemoveAllCommandBuffers();
                    SceneView.RepaintAll();
                    PlayModeView.RepaintAll();
                }
            }
            EditorGUI.indentLevel--;
        }

        public override void OnInspectorGUI()
        {
            settings.Update();

            var c = (Camera)target;
            m_ShowBGColorOptions.target = !clearFlagsHasMultipleValues && (c.clearFlags == CameraClearFlags.SolidColor || c.clearFlags == CameraClearFlags.Skybox);
            m_ShowOrthoOptions.target = !orthographicHasMultipleValues && c.orthographic;

            bool displaySubsystemPresent = displayDescriptors.Count > 0;
            m_ShowTargetEyeOption.target = targetEyeValue != (int)StereoTargetEyeMask.Both || PlayerSettings.virtualRealitySupported || displaySubsystemPresent;

            settings.DrawClearFlags();

            if (EditorGUILayout.BeginFadeGroup(m_ShowBGColorOptions.faded))
                settings.DrawBackgroundColor();
            EditorGUILayout.EndFadeGroup();

            settings.DrawCullingMask();

            EditorGUILayout.Space();

            settings.DrawProjection();

            settings.DrawClippingPlanes();

            settings.DrawNormalizedViewPort();

            EditorGUILayout.Space();
            settings.DrawDepth();
            settings.DrawRenderingPath();
            if (m_ShowOrthoOptions.target && wantDeferredRendering)
                EditorGUILayout.HelpBox("Deferred rendering does not work with Orthographic camera, will use Forward.",
                    MessageType.Warning, true);

            settings.DrawTargetTexture(wantDeferredRendering);
            settings.DrawOcclusionCulling();
            settings.DrawHDR();
            settings.DrawMSAA();
            settings.DrawDynamicResolution();

            foreach (Camera camera in targets)
            {
                if (camera != null)
                {
                    Settings.DrawCameraWarnings(camera);
                }
            }

            settings.DrawVR();
            EditorGUILayout.Space();
            settings.DrawMultiDisplay();

            if (EditorGUILayout.BeginFadeGroup(m_ShowTargetEyeOption.faded))
                settings.DrawTargetEye();
            EditorGUILayout.EndFadeGroup();

            DepthTextureModeGUI();
            CommandBufferGUI();

            serializedObject.ApplyModifiedProperties();
        }

        public virtual void OnOverlayGUI(Object target, SceneView sceneView)
        {
            if (target == null) return;

            var c = (Camera)target;

            // Do not render the Camera Preview overlay if the target camera GameObject is not part of the objects the SceneView is rendering
            if (!sceneView.IsGameObjectInThisSceneView(c.gameObject))
                return;

            Vector2 previewSize = c.targetTexture ? new Vector2(c.targetTexture.width, c.targetTexture.height) : PlayModeView.GetMainPlayModeViewTargetSize();

            if (previewSize.x < 0f)
            {
                // Fallback to Scene View of not a valid game view size
                previewSize.x = sceneView.position.width;
                previewSize.y = sceneView.position.height;
            }

            // Apply normalizedviewport rect of camera
            Rect normalizedViewPortRect = c.rect;
            // clamp normalized rect in [0,1]
            normalizedViewPortRect.xMin = Math.Max(normalizedViewPortRect.xMin, 0f);
            normalizedViewPortRect.yMin = Math.Max(normalizedViewPortRect.yMin, 0f);
            normalizedViewPortRect.xMax = Math.Min(normalizedViewPortRect.xMax, 1f);
            normalizedViewPortRect.yMax = Math.Min(normalizedViewPortRect.yMax, 1f);

            previewSize.x *= Mathf.Max(normalizedViewPortRect.width, 0f);
            previewSize.y *= Mathf.Max(normalizedViewPortRect.height, 0f);

            // Prevent using invalid previewSize
            if (previewSize.x < 1f || previewSize.y < 1f)
                return;

            float aspect = previewSize.x / previewSize.y;

            // Scale down (fit to scene view)
            previewSize.y = kPreviewNormalizedSize * sceneView.position.height;
            previewSize.x = previewSize.y * aspect;
            if (previewSize.y > sceneView.position.height * 0.5f)
            {
                previewSize.y = sceneView.position.height * 0.5f;
                previewSize.x = previewSize.y * aspect;
            }
            if (previewSize.x > sceneView.position.width * 0.5f)
            {
                previewSize.x = sceneView.position.width * 0.5f;
                previewSize.y = previewSize.x / aspect;
            }

            // Get and reserve rect
            Rect cameraRect = GUILayoutUtility.GetRect(previewSize.x, previewSize.y);
            cameraRect.width = Mathf.Floor(cameraRect.width);

            if (Event.current.type == EventType.Repaint)
            {
                Graphics.DrawTexture(cameraRect, Texture2D.whiteTexture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, Color.black);
            }

            var properWidth = cameraRect.height * aspect;
            cameraRect.x += (cameraRect.width - properWidth) * 0.5f;
            cameraRect.width = properWidth;


            if (Event.current.type == EventType.Repaint)
            {
                // setup camera and render
                previewCamera.CopyFrom(c);

                // make sure the preview camera is rendering the same stage as the SceneView is
                if (sceneView.overrideSceneCullingMask != 0)
                    previewCamera.overrideSceneCullingMask = sceneView.overrideSceneCullingMask;
                else
                    previewCamera.scene = sceneView.customScene;

                // also make sure to sync any Skybox component on the preview camera
                var dstSkybox = previewCamera.GetComponent<Skybox>();
                if (dstSkybox)
                {
                    var srcSkybox = c.GetComponent<Skybox>();
                    if (srcSkybox && srcSkybox.enabled)
                    {
                        dstSkybox.enabled = true;
                        dstSkybox.material = srcSkybox.material;
                    }
                    else
                    {
                        dstSkybox.enabled = false;
                    }
                }


                var previewTexture = GetPreviewTextureWithSize((int)cameraRect.width, (int)cameraRect.height);
                previewTexture.antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing);
                previewCamera.targetTexture = previewTexture;
                previewCamera.pixelRect = new Rect(0, 0, cameraRect.width, cameraRect.height);

                Handles.EmitGUIGeometryForCamera(c, previewCamera);

                if (c.usePhysicalProperties)
                {
                    // when sensor size is reduced, the previous frame is still visible behing so we need to clear the texture before rendering.
                    RenderTexture rt = RenderTexture.active;
                    RenderTexture.active = previewTexture;
                    GL.Clear(false, true, Color.clear);
                    RenderTexture.active = rt;
                }

                previewCamera.Render();
                Graphics.DrawTexture(cameraRect, previewTexture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, GUI.color, EditorGUIUtility.GUITextureBlit2SRGBMaterial);
            }
        }

        private RenderTexture GetPreviewTextureWithSize(int width, int height)
        {
            if (m_PreviewTexture == null || m_PreviewTexture.width != width || m_PreviewTexture.height != height || m_QualitySettingsAntiAliasing != QualitySettings.antiAliasing)
            {
                m_PreviewTexture = new RenderTexture(width, height, 24, SystemInfo.GetGraphicsFormat(DefaultFormat.LDR));
                m_QualitySettingsAntiAliasing = QualitySettings.antiAliasing;
            }
            return m_PreviewTexture;
        }

        [RequiredByNativeCode]
        internal static float GetGameViewAspectRatio()
        {
            Vector2 gameViewSize = PlayModeView.GetMainPlayModeViewTargetSize();
            if (gameViewSize.x < 0f)
            {
                // Fallback to Scene View of not a valid game view size
                gameViewSize.x = Screen.width;
                gameViewSize.y = Screen.height;
            }

            return gameViewSize.x / gameViewSize.y;
        }

        [RequiredByNativeCode]
        internal static void GetMainPlayModeViewSize(out Vector2 size)
        {
            size = PlayModeView.GetMainPlayModeViewTargetSize();
        }

        // Called from C++ when we need to render a Camera's gizmo
        internal static void RenderGizmo(Camera camera)
        {
            CameraEditorUtils.DrawFrustumGizmo(camera);
        }

        private static Vector2 s_PreviousMainPlayModeViewTargetSize;

        public virtual void OnSceneGUI()
        {
            if (!target)
                return;
            var c = (Camera)target;

            if (!CameraEditorUtils.IsViewportRectValidToRender(c.rect))
                return;

            Vector2 currentMainPlayModeViewTargetSize = PlayModeView.GetMainPlayModeViewTargetSize();
            if (s_PreviousMainPlayModeViewTargetSize != currentMainPlayModeViewTargetSize)
            {
                // a gameView size change can affect horizontal FOV, refresh the inspector when that happens.
                Repaint();
                s_PreviousMainPlayModeViewTargetSize = currentMainPlayModeViewTargetSize;
            }

            CameraEditorUtils.HandleFrustum(c, referenceTargetIndex);
        }

        void DuringSceneGUI(SceneView sceneView)
        {
            if (!target)
                return;
            var c = (Camera)target;

            if (!CameraEditorUtils.IsViewportRectValidToRender(c.rect))
                return;

            SceneViewOverlay.ShowWindow(m_OverlayWindows[c]);
        }
    }
}
