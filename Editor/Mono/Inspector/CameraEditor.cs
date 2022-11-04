// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;
using AnimatedBool = UnityEditor.AnimatedValues.AnimBool;
using UnityEngine.Scripting;
using UnityEditor.Modules;
using UnityEditor.Overlays;
using UnityEditorInternal.VR;
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
            public static GUIContent iso = EditorGUIUtility.TrTextContent("ISO", "The sensor sensitivity (ISO).");
            public static GUIContent shutterSpeed = EditorGUIUtility.TrTextContent("Shutter Speed", "The exposure time, in second.");
            public static GUIContent aperture = EditorGUIUtility.TrTextContent("Aperture", "The aperture number, in f-stop.");
            public static GUIContent focusDistance = EditorGUIUtility.TrTextContent("FocusDistance", "The focus distance of the lens. The Depth of Field Volume override uses this value if you set focusDistanceMode to FocusDistanceMode.Camera.");
            public static GUIContent bladeCount = EditorGUIUtility.TrTextContent("Blade Count", "The number of diaphragm blades.");
            public static GUIContent curvature = EditorGUIUtility.TrTextContent("Curvature", "Maps an aperture range to blade curvature.");
            public static GUIContent barrelClipping = EditorGUIUtility.TrTextContent("Barrel Clipping", "The strength of the \"cat eye\" effect on bokeh (optical vignetting).");
            public static GUIContent anamorphism = EditorGUIUtility.TrTextContent("Anamorphism", "Stretches the sensor to simulate an anamorphic look. Positive values distort the Camera vertically, negative will distort the Camera horizontally.");
            public static GUIContent physicalCamera = EditorGUIUtility.TrTextContent("Physical Camera", "Enables Physical camera mode. When checked, the field of view is calculated from properties for simulating physical attributes (focal length, sensor size, and lens shift)");
            public static GUIContent cameraType = EditorGUIUtility.TrTextContent("Sensor Type", "Common sensor sizes. Choose an item to set Sensor Size, or edit Sensor Size for your custom settings.");
            public static GUIContent renderingPath = EditorGUIUtility.TrTextContent("Rendering Path", "Choose a rendering method for this camera.\n\nUse Graphics Settings to use the rendering path specified in graphics settings.\n\nUse Forward to render all objects with one pass per light.\n\nUse Deferred to draw all objects once without lighting and then draw the lighting of all objects at the end of the render queue.\n\nUse Legacy Vertex Lit to render all lights in a single pass, calculated at vertices.");
            public static GUIContent focalLength = EditorGUIUtility.TrTextContent("Focal Length", "The simulated distance between the lens and the sensor of the physical camera. Larger values give a narrower field of view.");
            public static GUIContent allowOcclusionCulling = EditorGUIUtility.TrTextContent("Occlusion Culling", "Occlusion Culling disables rendering of objects when they are not currently seen by the camera because they are obscured (occluded) by other objects.");
            public static GUIContent allowHDR = EditorGUIUtility.TrTextContent("HDR", "High Dynamic Range gives you a wider range of light intensities, so your lighting looks more realistic. With it, you can still see details and experience less saturation even with bright light.");
            public static GUIContent allowMSAA = EditorGUIUtility.TrTextContent("MSAA", "Use Multi Sample Anti-aliasing to reduce aliasing.");
            public static GUIContent gateFit = EditorGUIUtility.TrTextContent("Gate Fit", "Determines how the rendered area (resolution gate) fits into the sensor area (film gate).");
            public static GUIContent allowDynamicResolution = EditorGUIUtility.TrTextContent("Allow Dynamic Resolution", "Scales render textures to support dynamic resolution if the target platform/graphics API supports it.");
            public static GUIContent FOVAxisMode = EditorGUIUtility.TrTextContent("FOV Axis", "Field of view axis.");
            public static GUIContent targetDisplay = EditorGUIUtility.TrTextContent("Target Display", "Set the target display for this camera.");
            public static GUIContent xrTargetEye = EditorGUIUtility.TrTextContent("Target Eye", "Allows XR rendering for target eye. This disables stereo rendering and only works for the selected eye.");

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
                "35mm TV Projection",
                "35mm Full Aperture",
                "35mm 1.85 Projection",
                "35mm Anamorphic",
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
                new Vector2(12.522f, 7.417f) , // Super 16mm
                new Vector2(21.95f, 9.35f) , // 35mm 2-perf
                new Vector2(21.946f, 16.002f) , // 35mm academy
                new Vector2(24.89f, 18.66f) , // Super-35
                new Vector2(20.726f, 15.545f), // 35mm TV Projection
                new Vector2(24.892f, 18.669f), // 35mm Full Aperture
                new Vector2(20.955f, 11.328f), // 35mm 1.85 Projection
                new Vector2(21.946f, 18.593f), // 35mm Anamorphic
                new Vector2(54.12f, 25.59f) , // 65mm ALEXA
                new Vector2(52.476f, 23.012f) , // 70mm
                new Vector2(70.41f, 52.63f), // 70mm IMAX
            };

            // Manually entered rendering path names/values, since we want to show them
            // in different order than they appear in the enum.
            private static readonly GUIContent[] kCameraRenderPaths =
            {
                EditorGUIUtility.TrTextContent("Use Graphics Settings"),
                EditorGUIUtility.TrTextContent("Forward"),
                EditorGUIUtility.TrTextContent("Deferred"),
                EditorGUIUtility.TrTextContent("Legacy Vertex Lit")
            };
            private static readonly int[] kCameraRenderPathValues =
            {
                (int)RenderingPath.UsePlayerSettings,
                (int)RenderingPath.Forward,
                (int)RenderingPath.DeferredShading,
                (int)RenderingPath.VertexLit
            };

            public SerializedProperty clearFlags { get; private set; }
            public SerializedProperty backgroundColor { get; private set; }
            public SerializedProperty normalizedViewPortRect { get; private set; }
            internal SerializedProperty projectionMatrixMode { get; private set; }
            public SerializedProperty iso { get; private set; }
            public SerializedProperty shutterSpeed { get; private set; }
            public SerializedProperty aperture { get; private set; }
            public SerializedProperty focusDistance { get; private set; }
            public SerializedProperty focalLength { get; private set; }
            public SerializedProperty bladeCount { get; private set; }
            public SerializedProperty curvature { get; private set; }
            public SerializedProperty barrelClipping { get; private set; }
            public SerializedProperty anamorphism { get; private set; }
            public SerializedProperty sensorSize { get; private set; }
            public SerializedProperty lensShift { get; private set; }
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
                iso = m_SerializedObject.FindProperty("m_Iso");
                shutterSpeed = m_SerializedObject.FindProperty("m_ShutterSpeed");
                aperture = m_SerializedObject.FindProperty("m_Aperture");
                focusDistance = m_SerializedObject.FindProperty("m_FocusDistance");
                focalLength = m_SerializedObject.FindProperty("m_FocalLength");
                bladeCount = m_SerializedObject.FindProperty("m_BladeCount");
                curvature = m_SerializedObject.FindProperty("m_Curvature");
                barrelClipping = m_SerializedObject.FindProperty("m_BarrelClipping");
                anamorphism = m_SerializedObject.FindProperty("m_Anamorphism");
                sensorSize = m_SerializedObject.FindProperty("m_SensorSize");
                lensShift = m_SerializedObject.FindProperty("m_LensShift");
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

                var controlRect = EditorGUILayout.GetControlRect();
                var label = EditorGUI.BeginProperty(controlRect, Styles.projection, orthographic);

                projectionType = (ProjectionType)EditorGUI.EnumPopup(controlRect, label, projectionType);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                    orthographic.boolValue = (projectionType == ProjectionType.Orthographic);
                EditorGUI.EndProperty();

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
                        var targets = m_SerializedObject.targetObjects;
                        var camera0 = targets[0] as Camera;
                        float aspectRatio = isPhysicalCamera ? sensorSize.vector2Value.x / sensorSize.vector2Value.y : camera0.aspect;

                        if (!fovAxisVertical && !fovAxisMode.hasMultipleDifferentValues)
                        {
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
                        var fovMinValue = fovAxisVertical ? 0.00001f : Camera.VerticalToHorizontalFieldOfView(0.00001f, aspectRatio);
                        var fovMaxValue = fovAxisVertical ? 179.0f : Camera.VerticalToHorizontalFieldOfView(179.0f, aspectRatio);
                        var fovNewValue = EditorGUILayout.Slider(content, fovCurrentValue, fovMinValue, fovMaxValue);
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
                                EditorGUILayout.PropertyField(iso, Styles.iso);
                                EditorGUILayout.PropertyField(shutterSpeed, Styles.shutterSpeed);
                                EditorGUILayout.PropertyField(aperture, Styles.aperture);
                                EditorGUILayout.PropertyField(focusDistance, Styles.focusDistance);
                                EditorGUILayout.PropertyField(bladeCount, Styles.bladeCount);
                                EditorGUILayout.PropertyField(curvature, Styles.curvature);
                                EditorGUILayout.PropertyField(barrelClipping, Styles.barrelClipping);
                                EditorGUILayout.PropertyField(anamorphism, Styles.anamorphism);

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
                        && targetTexture.antiAliasing > 1)
                    {
                        EditorGUILayout.HelpBox("The target texture is using MSAA. Note that this will not affect MSAA behaviour of this camera. MSAA rendering for cameras is configured through the 'MSAA' camera setting and related project settings. The target texture will always contain resolved pixel data.", MessageType.Warning, true);
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
                if (VREditor.GetVREnabledOnTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)))
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
                    EditorGUILayout.IntPopup(targetDisplay, DisplayUtility.GetDisplayNames(), DisplayUtility.GetDisplayIndices(), Styles.targetDisplay);
                    if (prevDisplay != targetDisplay.intValue)
                        GameView.RepaintAll();
                }
            }

            public void DrawTargetEye()
            {
                EditorGUILayout.IntPopup(targetEye, kTargetEyes, kTargetEyeValues, Styles.xrTargetEye);
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

        private Camera m_PreviewCamera;

        [Obsolete("Preview camera is obsolete, use Overlays to create a Camera preview.")]
        protected Camera previewCamera
        {
            get
            {
                if (m_PreviewCamera == null)
                {
                    // Only log a warning once when creating the camera so that we don't flood the console with
                    // redundant logs.
                    Debug.LogWarning("Preview camera is obsolete, use Overlays to create a Camera preview.");
                    m_PreviewCamera = EditorUtility.CreateGameObjectWithHideFlags("Preview Camera", HideFlags.HideAndDontSave, typeof(Camera), typeof(Skybox)).GetComponent<Camera>();
                }
                m_PreviewCamera.enabled = false;
                return m_PreviewCamera;
            }
        }

        private static bool IsDeferredRenderingPath(RenderingPath rp) { return rp == RenderingPath.DeferredShading; }

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

        Overlay m_PreviewOverlay;

        static List<XRDisplaySubsystemDescriptor> displayDescriptors = new List<XRDisplaySubsystemDescriptor>();

        static void OnReloadSubsystemsComplete()
        {
            SubsystemManager.GetSubsystemDescriptors(displayDescriptors);
        }

        public void OnEnable()
        {
            settings.OnEnable();

            var c = (Camera)target;
            m_ShowBGColorOptions.value = !clearFlagsHasMultipleValues && (c.clearFlags == CameraClearFlags.SolidColor || c.clearFlags == CameraClearFlags.Skybox);
            m_ShowOrthoOptions.value = c.orthographic;
            m_ShowTargetEyeOption.value = targetEyeValue != (int)StereoTargetEyeMask.Both || VREditor.GetVREnabledOnTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));

            m_ShowBGColorOptions.valueChanged.AddListener(Repaint);
            m_ShowOrthoOptions.valueChanged.AddListener(Repaint);
            m_ShowTargetEyeOption.valueChanged.AddListener(Repaint);

            SubsystemManager.GetSubsystemDescriptors(displayDescriptors);
            SubsystemManager.afterReloadSubsystems += OnReloadSubsystemsComplete;

            if(!SceneViewCameraOverlay.forceDisable)
                SceneView.AddOverlayToActiveView(m_PreviewOverlay = CreatePreviewOverlay(c));
        }

        public void OnDestroy()
        {
            if (m_PreviewCamera != null)
                DestroyImmediate(m_PreviewCamera.gameObject, true);
        }

        public void OnDisable()
        {
            SceneView.RemoveOverlayFromActiveView(m_PreviewOverlay);
            m_ShowBGColorOptions.valueChanged.RemoveListener(Repaint);
            m_ShowOrthoOptions.valueChanged.RemoveListener(Repaint);
            m_ShowTargetEyeOption.valueChanged.RemoveListener(Repaint);
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
            m_ShowTargetEyeOption.target = targetEyeValue != (int)StereoTargetEyeMask.Both || VREditor.GetVREnabledOnTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)) || displaySubsystemPresent;

            settings.DrawClearFlags();

            if (EditorGUILayout.BeginFadeGroup(m_ShowBGColorOptions.faded))
                settings.DrawBackgroundColor();
            EditorGUILayout.EndFadeGroup();

            settings.DrawCullingMask();

            EditorGUILayout.Space();

            settings.DrawProjection();

            if (displaySubsystemPresent && targetEyeValue != (int)StereoTargetEyeMask.None && c.orthographic && c.targetTexture == null)
                GUILayout.Label(EditorGUIUtility.TrTextContent("Orthographic projection is not supported when running in XR.", "One or more XR Plug-in providers were detected in your project. Using Orthographic projection is not supported when running in XR and enabling this may cause problems.", EditorGUIUtility.warningIcon));

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

        // marked obsolete @karlh 2021/02/13
        [Obsolete("OnOverlayGUI is obsolete. Override CreatePreviewOverlay to create a preview.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual void OnOverlayGUI(Object target, SceneView sceneView)
        {
        }

        public virtual Overlay CreatePreviewOverlay(Camera cam) => new SceneViewCameraOverlay(cam);

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

        static Vector2 s_PreviousMainPlayModeViewTargetSize;

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
    }
}
