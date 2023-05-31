// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using UnityEditor.Inspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;
using AnimatedBool = UnityEditor.AnimatedValues.AnimBool;
using UnityEngine.Scripting;
using UnityEditor.Modules;
using UnityEditor.Overlays;
using UnityEditor.UIElements;
using UnityEditorInternal.VR;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [CustomEditor(typeof(Camera))]
    [CanEditMultipleObjects]
    public class CameraEditor : Editor
    {
        internal static class Styles
        {
            public static readonly GUIContent iconRemove = EditorGUIUtility.TrIconContent("Toolbar Minus", "Remove command buffer");
            public static readonly GUIContent clearFlags = EditorGUIUtility.TrTextContent("Clear Flags", "What to display in empty areas of this Camera's view.\n\nChoose Skybox to display a skybox in empty areas, defaulting to a background color if no skybox is found.\n\nChoose Solid Color to display a background color in empty areas.\n\nChoose Depth Only to display nothing in empty areas.\n\nChoose Don't Clear to display whatever was displayed in the previous frame in empty areas.");
            public static readonly GUIContent background = EditorGUIUtility.TrTextContent("Background", "The Camera clears the screen to this color before rendering.");
            public static readonly GUIContent projection = EditorGUIUtility.TrTextContent("Projection", "How the Camera renders perspective.\n\nChoose Perspective to render objects with perspective.\n\nChoose Orthographic to render objects uniformly, with no sense of perspective.");
            public static readonly GUIContent size = EditorGUIUtility.TrTextContent("Size", "The vertical size of the camera view.");
            public static readonly GUIContent fieldOfView = EditorGUIUtility.TrTextContent("Field of View", "The camera's view angle measured in degrees along the selected axis.");
            public static readonly GUIContent viewportRect = EditorGUIUtility.TrTextContent("Viewport Rect", "Four values that indicate where on the screen this camera view will be drawn. Measured in Viewport Coordinates (values 0-1).");
            public static readonly GUIContent sensorSize = EditorGUIUtility.TrTextContent("Sensor Size", "The size of the camera sensor in millimeters.");
            public static readonly GUIContent lensShift = EditorGUIUtility.TrTextContent("Lens Shift", "Offset from the camera sensor. Use these properties to simulate a shift lens. Measured as a multiple of the sensor size.");
            public static readonly GUIContent iso = EditorGUIUtility.TrTextContent("ISO", "The sensor sensitivity (ISO).");
            public static readonly GUIContent shutterSpeed = EditorGUIUtility.TrTextContent("Shutter Speed", "The exposure time, in second.");
            public static readonly GUIContent aperture = EditorGUIUtility.TrTextContent("Aperture", "The aperture number, in f-stop.");
            public static readonly GUIContent focusDistance = EditorGUIUtility.TrTextContent("Focus Distance", "The focus distance of the lens. The Depth of Field Volume override uses this value if you set focusDistanceMode to FocusDistanceMode.Camera.");
            public static readonly GUIContent bladeCount = EditorGUIUtility.TrTextContent("Blade Count", "The number of diaphragm blades.");
            public static readonly GUIContent curvature = EditorGUIUtility.TrTextContent("Curvature", "Maps an aperture range to blade curvature.");
            public static readonly GUIContent barrelClipping = EditorGUIUtility.TrTextContent("Barrel Clipping", "The strength of the \"cat eye\" effect on bokeh (optical vignetting).");
            public static readonly GUIContent anamorphism = EditorGUIUtility.TrTextContent("Anamorphism", "Stretches the sensor to simulate an anamorphic look. Positive values distort the Camera vertically, negative will distort the Camera horizontally.");
            public static readonly GUIContent physicalCamera = EditorGUIUtility.TrTextContent("Physical Camera", "Enables Physical camera mode. When checked, the field of view is calculated from properties for simulating physical attributes (focal length, sensor size, and lens shift)");
            public static readonly GUIContent cameraType = EditorGUIUtility.TrTextContent("Sensor Type", "Common sensor sizes. Choose an item to set Sensor Size, or edit Sensor Size for your custom settings.");
            public static readonly GUIContent renderingPath = EditorGUIUtility.TrTextContent("Rendering Path", "Choose a rendering method for this camera.\n\nUse Graphics Settings to use the rendering path specified in graphics settings.\n\nUse Forward to render all objects with one pass per light.\n\nUse Deferred to draw all objects once without lighting and then draw the lighting of all objects at the end of the render queue.\n\nUse Legacy Vertex Lit to render all lights in a single pass, calculated at vertices.");
            public static readonly GUIContent focalLength = EditorGUIUtility.TrTextContent("Focal Length", "The simulated distance between the lens and the sensor of the physical camera. Larger values give a narrower field of view.");
            public static readonly GUIContent allowOcclusionCulling = EditorGUIUtility.TrTextContent("Occlusion Culling", "Occlusion Culling disables rendering of objects when they are not currently seen by the camera because they are obscured (occluded) by other objects.");
            public static readonly GUIContent allowHDR = EditorGUIUtility.TrTextContent("HDR", "High Dynamic Range gives you a wider range of light intensities, so your lighting looks more realistic. With it, you can still see details and experience less saturation even with bright light.");
            public static readonly GUIContent allowMSAA = EditorGUIUtility.TrTextContent("MSAA", "Use Multi Sample Anti-aliasing to reduce aliasing.");
            public static readonly GUIContent gateFit = EditorGUIUtility.TrTextContent("Gate Fit", "Determines how the rendered area (resolution gate) fits into the sensor area (film gate).");
            public static readonly GUIContent allowDynamicResolution = EditorGUIUtility.TrTextContent("Allow Dynamic Resolution", "Scales render textures to support dynamic resolution if the target platform/graphics API supports it.");
            public static readonly GUIContent FOVAxisMode = EditorGUIUtility.TrTextContent("FOV Axis", "Field of view axis.");
            public static readonly GUIContent targetDisplay = EditorGUIUtility.TrTextContent("Target Display", "Set the target display for this camera.");
            public static readonly GUIContent xrTargetEye = EditorGUIUtility.TrTextContent("Target Eye", "Allows XR rendering for target eye. This disables stereo rendering and only works for the selected eye.");

            public static readonly GUIContent orthoDeferredWarning = EditorGUIUtility.TrTextContent("Deferred rendering does not work with Orthographic camera, will use Forward.");
            public static readonly GUIContent orthoXRWarning = EditorGUIUtility.TrTextContent("Orthographic projection is not supported when running in XR.", "One or more XR Plug-in providers were detected in your project. Using Orthographic projection is not supported when running in XR and enabling this may cause problems.", EditorGUIUtility.warningIcon);
            public static readonly GUIContent deferredMSAAWarning = EditorGUIUtility.TrTextContent("The target texture is using MSAA. Note that this will not affect MSAA behaviour of this camera. MSAA rendering for cameras is configured through the 'MSAA' camera setting and related project settings. The target texture will always contain resolved pixel data.");
            public static readonly GUIContent dynamicResolutionTimingWarning = EditorGUIUtility.TrTextContent("It is recommended to enable Frame Timing Statistics under Rendering Player Settings when using dynamic resolution cameras.");

            public static readonly GUIStyle invisibleButton = "InvisibleButton";

            public const string k_CameraEditorUxmlPath = "UXML/InspectorWindow/CameraEditor.uxml";

            public const string k_ClearFlagsElementName = "clear-flags";
            public const string k_BackgroundElementName = "background";
            public const string k_CullingMaskElementName = "culling-mask";
            public const string k_ClippingPlanesElementName = "clipping-planes";
            public const string k_ViewportRectElementName = "viewport-rect";
            public const string k_DepthElementName = "depth";
            public const string k_RenderingPathElementName = "rendering-path";
            public const string k_OrthographicDeferredWarningElementName = "orthographic-deferred-warning";
            public const string k_DeferredMsaaWarningElementName = "deferred-msaa-warning";
            public const string k_TargetTextureElementName = "target-texture";
            public const string k_OcclusionCullingElementName = "occlusion-culling";
            public const string k_HdrElementName = "hdr";
            public const string k_MsaaElementName = "msaa";
            public const string k_DynamicResolution = "dynamic-resolution";
            public const string k_DynamicResolutionTimingWarningElementName = "dynamic-resolution-timing-warning";
            public const string k_CameraWarningsElementName = "camera-warnings";
            public const string k_VrGroupElementName = "vr-group";
            public const string k_TargetDisplayElementName = "target-display";
            public const string k_TargetEyeElementName = "target-eye";
            public const string k_DepthTextureModeElementName = "depth-texture-mode";
            public const string k_CommandBuffersElementName = "command-buffers";
            public const string k_CommandBuffersListElementName = "list";
            public const string k_CommandBuffersRemoveAllElementName = "remove-all";

            public const string k_PerspectiveGroupElementName = "perspective-group";
            public const string k_OrthographicGroupElementName = "orthographic-group";
            public const string k_ProjectionTypeElementName = "projection-type";
            public const string k_FovAxisModeElementName = "fov-axis-mode";
            public const string k_FieldOfViewElementName = "field-of-view";
            public const string k_PhysicalCameraGroupElementName = "physical-camera-group";
            public const string k_PhysicalCameraElementName = "physical-camera";
            public const string k_IsoElementName = "iso";
            public const string k_ShutterSpeedElementName = "shutter-speed";
            public const string k_ApertureElementName = "aperture";
            public const string k_FocusDistanceElementName = "focus-distance";
            public const string k_BladeCountElementName = "blade-count";
            public const string k_CurvatureElementName = "curvature";
            public const string k_BarrelClippingElementName = "barrel-clipping";
            public const string k_AnamorphismElementName = "anamorphism";
            public const string k_FocalLengthElementName = "focal-length";
            public const string k_SensorTypeElementName = "sensor-type";
            public const string k_SensorSizeElementName = "sensor-size";
            public const string k_LensShiftElementName = "lens-shift";
            public const string k_GateFitElementName = "gate-fit";
            public const string k_OrthographicSizeElementName = "orthographic-size";
            public const string k_OrthographicXrWarningElementName = "orthographic-xr-warning";

            public const string commandBufferUssClassName = "camera-editor__command-buffer__line";
            public const string commandBufferLabelUssClassName = "camera-editor__command-buffer__label";
            public const string commandBufferRemoveButtonUssClassName = "camera-editor__command-buffer__remove-button";
        }

        public sealed class Settings
        {
            readonly SerializedObject m_SerializedObject;

            public Settings(SerializedObject so)
            {
                m_SerializedObject = so;
            }

            public static IEnumerable<string> ApertureFormatNames => k_ApertureFormats;

            internal static readonly string[] k_ApertureFormats =
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

            internal static readonly Vector2[] k_ApertureFormatValues =
            {
                new(4.8f, 3.5f), // 8mm
                new(5.79f, 4.01f), // Super 8mm
                new(10.26f, 7.49f), // 16mm
                new(12.522f, 7.417f), // Super 16mm
                new(21.95f, 9.35f), // 35mm 2-perf
                new(21.946f, 16.002f), // 35mm academy
                new(24.89f, 18.66f), // Super-35
                new(20.726f, 15.545f), // 35mm TV Projection
                new(24.892f, 18.669f), // 35mm Full Aperture
                new(20.955f, 11.328f), // 35mm 1.85 Projection
                new(21.946f, 18.593f), // 35mm Anamorphic
                new(54.12f, 25.59f), // 65mm ALEXA
                new(52.476f, 23.012f), // 70mm
                new(70.41f, 52.63f), // 70mm IMAX
            };

            // Manually entered rendering path names/values, since we want to show them
            // in different order than they appear in the enum.
            internal static readonly GUIContent[] k_CameraRenderPaths =
            {
                EditorGUIUtility.TrTextContent("Use Graphics Settings"),
                EditorGUIUtility.TrTextContent("Forward"),
                EditorGUIUtility.TrTextContent("Deferred"),
                EditorGUIUtility.TrTextContent("Legacy Vertex Lit")
            };

            internal static readonly int[] k_CameraRenderPathValues =
            {
                (int)RenderingPath.UsePlayerSettings,
                (int)RenderingPath.Forward,
                (int)RenderingPath.DeferredShading,
                (int)RenderingPath.VertexLit
            };

            internal static readonly GUIContent[] k_DefaultOptions =
            {
                EditorGUIUtility.TrTextContent("Off"),
                EditorGUIUtility.TrTextContent("Use Graphics Settings"),
            };

            internal static readonly int[] k_DefaultOptionValues =
            {
                0,
                1
            };

            internal static readonly GUIContent[] k_TargetEyes =
            {
                EditorGUIUtility.TrTextContent("Both"),
                EditorGUIUtility.TrTextContent("Left"),
                EditorGUIUtility.TrTextContent("Right"),
                EditorGUIUtility.TrTextContent("None (Main Display)"),
            };

            internal static readonly int[] k_TargetEyeValues =
            {
                (int)StereoTargetEyeMask.Both,
                (int)StereoTargetEyeMask.Left,
                (int)StereoTargetEyeMask.Right,
                (int)StereoTargetEyeMask.None
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
                var projectionType = orthographic.boolValue ? ProjectionType.Orthographic : ProjectionType.Perspective;
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
                        var multipleDifferentFovValues = false;
                        var isPhysicalCamera = projectionMatrixMode.intValue == (int)Camera.ProjectionMatrixMode.PhysicalPropertiesBased;

                        var rect = EditorGUILayout.GetControlRect();
                        var guiContent = EditorGUI.BeginProperty(rect, Styles.FOVAxisMode, fovAxisMode);
                        EditorGUI.showMixedValue = fovAxisMode.hasMultipleDifferentValues;

                        EditorGUI.BeginChangeCheck();
                        var fovAxisNewVal = (int)(Camera.FieldOfViewAxis)EditorGUI.EnumPopup(rect, guiContent, (Camera.FieldOfViewAxis)fovAxisMode.intValue);
                        if (EditorGUI.EndChangeCheck())
                            fovAxisMode.intValue = fovAxisNewVal;
                        EditorGUI.EndProperty();

                        var fovAxisVertical = fovAxisMode.intValue == 0;
                        var targets = m_SerializedObject.targetObjects;
                        var camera0 = targets[0] as Camera;
                        var aspectRatio = isPhysicalCamera ? sensorSize.vector2Value.x / sensorSize.vector2Value.y : camera0.aspect;

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
                                    var sensorLength = fovAxisVertical ? sensorSize.vector2Value.y : sensorSize.vector2Value.x;
                                    var focalLengthVal = fovChanged ? Camera.FieldOfViewToFocalLength(fovNewValue, sensorLength) : focalLength.floatValue;
                                    focalLengthVal = EditorGUILayout.FloatField(Styles.focalLength, focalLengthVal);
                                    if (checkScope.changed || fovChanged)
                                        focalLength.floatValue = focalLengthVal;
                                }

                                EditorGUI.showMixedValue = sensorSize.hasMultipleDifferentValues;
                                EditorGUI.BeginChangeCheck();
                                var filmGateIndex = Array.IndexOf(k_ApertureFormatValues, new Vector2((float)Math.Round(sensorSize.vector2Value.x, 3), (float)Math.Round(sensorSize.vector2Value.y, 3)));
                                if (filmGateIndex == -1)
                                    filmGateIndex = EditorGUILayout.Popup(Styles.cameraType, k_ApertureFormats.Length - 1, k_ApertureFormats);
                                else
                                    filmGateIndex = EditorGUILayout.Popup(Styles.cameraType, filmGateIndex, k_ApertureFormats);
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
                                    var gateValue = (int)(Camera.GateFitMode)EditorGUILayout.EnumPopup(propertyScope.content, (Camera.GateFitMode)gateFit.intValue);
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
                EditorGUILayout.PropertiesField(EditorGUI.s_ClipingPlanesLabel, new[] { nearClippingPlane, farClippingPlane }, EditorGUI.s_NearAndFarLabels, EditorGUI.kNearFarLabelsWidth);
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
                EditorGUILayout.IntPopup(renderingPath, k_CameraRenderPaths, k_CameraRenderPathValues, Styles.renderingPath);
            }

            public void DrawTargetTexture(bool deferred)
            {
                EditorGUILayout.PropertyField(targetTexture);

                // show warning if we have deferred but manual MSAA set
                // only do this if the m_TargetTexture has the same values across all target cameras
                if (!deferred || targetTexture.hasMultipleDifferentValues)
                    return;

                var renderTexture = this.targetTexture.objectReferenceValue as RenderTexture;
                if (renderTexture && renderTexture.antiAliasing > 1)
                    EditorGUILayout.HelpBox(Styles.deferredMSAAWarning.text, MessageType.Warning, true);
            }

            public void DrawOcclusionCulling()
            {
                EditorGUILayout.PropertyField(occlusionCulling, Styles.allowOcclusionCulling);
            }

            public void DrawHDR()
            {
                var rect = EditorGUILayout.GetControlRect(true);
                EditorGUI.BeginProperty(rect, Styles.allowHDR, HDR);
                var value = HDR.boolValue ? 1 : 0;
                value = EditorGUI.IntPopup(rect, Styles.allowHDR, value, k_DefaultOptions, k_DefaultOptionValues);
                HDR.boolValue = value == 1;
                EditorGUI.EndProperty();
            }

            public void DrawMSAA()
            {
                var rect = EditorGUILayout.GetControlRect(true);
                EditorGUI.BeginProperty(rect, Styles.allowMSAA, allowMSAA);
                var value = allowMSAA.boolValue ? 1 : 0;
                value = EditorGUI.IntPopup(rect, Styles.allowMSAA, value, k_DefaultOptions, k_DefaultOptionValues);
                allowMSAA.boolValue = value == 1;
                EditorGUI.EndProperty();
            }

            public void DrawDynamicResolution()
            {
                EditorGUILayout.PropertyField(allowDynamicResolution, Styles.allowDynamicResolution);
                if ((allowDynamicResolution.boolValue || allowDynamicResolution.hasMultipleDifferentValues) && !PlayerSettings.enableFrameTimingStats)
                    EditorGUILayout.HelpBox(Styles.dynamicResolutionTimingWarning.text, MessageType.Warning, true);
            }

            public void DrawVR()
            {
                if (!vrEnabled)
                    return;

                EditorGUILayout.PropertyField(stereoSeparation);
                EditorGUILayout.PropertyField(stereoConvergence);
            }

            public void DrawMultiDisplay()
            {
                if (!showMultiDisplayOptions)
                    return;

                var prevDisplay = targetDisplay.intValue;
                EditorGUILayout.IntPopup(targetDisplay, DisplayUtility.GetDisplayNames(), DisplayUtility.GetDisplayIndices(), Styles.targetDisplay);
                if (prevDisplay != targetDisplay.intValue)
                    PlayModeView.RepaintAll();
            }

            public void DrawTargetEye()
            {
                EditorGUILayout.IntPopup(targetEye, k_TargetEyes, k_TargetEyeValues, Styles.xrTargetEye);
            }

            public static void DrawCameraWarnings(Camera camera)
            {
                var warnings = camera.GetCameraBufferWarnings();
                if (warnings.Length > 0)
                    EditorGUILayout.HelpBox(string.Join("\n\n", warnings), MessageType.Info, true);
            }
        }

        readonly AnimatedBool m_ShowBGColorOptions = new();
        readonly AnimatedBool m_ShowOrthoOptions = new();
        readonly AnimatedBool m_ShowTargetEyeOption = new();

        Camera camera => target as Camera;

        Camera m_PreviewCamera;

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
        
        static bool vrEnabled => VREditor.GetVREnabledOnTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));

        static bool showMultiDisplayOptions
        {
            get => ModuleManager.ShouldShowMultiDisplayOption();
        }

        bool wantsDeferredRendering
        {
            get
            {
                var isCamDeferred = camera.renderingPath == RenderingPath.DeferredShading;
                var isTierDeferred = Rendering.EditorGraphicsSettings.GetCurrentTierSettings().renderingPath == RenderingPath.DeferredShading;
                return isCamDeferred || (camera.renderingPath == RenderingPath.UsePlayerSettings && isTierDeferred);
            }
        }

        internal bool showBackgroundColorOptions => !settings.clearFlags.hasMultipleDifferentValues && camera.clearFlags is CameraClearFlags.SolidColor or CameraClearFlags.Skybox;

        internal bool showOrthographicOptions => !settings.orthographic.hasMultipleDifferentValues && settings.orthographic.boolValue;

        internal bool showPerspectiveOptions => !settings.orthographic.hasMultipleDifferentValues && !settings.orthographic.boolValue;

        internal bool showPhysicalCameraOptions => settings.projectionMatrixMode.intValue == (int)Camera.ProjectionMatrixMode.PhysicalPropertiesBased;

        internal bool showOrthographicXRWarning => k_DisplayDescriptors.Count > 0 && settings.targetEye.intValue != (int)StereoTargetEyeMask.None && camera.orthographic && camera.targetTexture == null;

        internal bool showOrthographicDeferredWarning => showOrthographicOptions && wantsDeferredRendering;

        internal bool showDeferredMSAAWarning
        {
            get
            {
                // show warning if we have deferred but manual MSAA set
                // only do this if the m_TargetTexture has the same values across all target cameras
                var singleValue = !settings.targetTexture.hasMultipleDifferentValues;
                var targetTextureValue = settings.targetTexture.objectReferenceValue as RenderTexture;
                var wantsAntiAliasing = targetTextureValue != null && targetTextureValue.antiAliasing > 1;
                return singleValue && wantsAntiAliasing && wantsDeferredRendering;
            }
        }

        internal bool showDynamicResolutionTimingWarning => (settings.allowDynamicResolution.boolValue || settings.allowDynamicResolution.hasMultipleDifferentValues) && !PlayerSettings.enableFrameTimingStats;

        bool showTargetEyeOption => settings.targetEye.intValue != (int)StereoTargetEyeMask.Both || vrEnabled || k_DisplayDescriptors.Count > 0;

        // Camera's depth texture mode is not serialized data, so can't get to it through
        // serialized property (hence no multi-edit).
        internal bool showDepthTextureMode => targets.Length == 1 && target is Camera { depthTextureMode: not DepthTextureMode.None };

        // Command buffers are not serialized data, so can't get to them through
        // serialized property (hence no multi-edit).
        internal bool showCommandBufferGUI => targets.Length == 1 && target is Camera { commandBufferCount: > 0 };

        internal enum ProjectionType
        {
            Perspective,
            Orthographic
        }

        bool m_CommandBuffersShown = true;

        Settings m_Settings;
        protected internal Settings settings => m_Settings ??= new Settings(serializedObject);

        internal static readonly List<XRDisplaySubsystemDescriptor> k_DisplayDescriptors = new();

        uint m_LastNonSerializedVersion;
        internal event Action onSettingsChanged;

        static void OnReloadSubsystemsComplete()
        {
            SubsystemManager.GetSubsystemDescriptors(k_DisplayDescriptors);
        }

        public void OnEnable()
        {
            settings.OnEnable();

            var c = (Camera)target;
            m_ShowBGColorOptions.value = showBackgroundColorOptions;
            m_ShowOrthoOptions.value = showOrthographicOptions;
            m_ShowTargetEyeOption.value = showTargetEyeOption;

            m_ShowBGColorOptions.valueChanged.AddListener(Repaint);
            m_ShowOrthoOptions.valueChanged.AddListener(Repaint);
            m_ShowTargetEyeOption.valueChanged.AddListener(Repaint);

            SubsystemManager.GetSubsystemDescriptors(k_DisplayDescriptors);
            SubsystemManager.afterReloadSubsystems += OnReloadSubsystemsComplete;
        }

        public void OnDestroy()
        {
            if (m_PreviewCamera != null)
                DestroyImmediate(m_PreviewCamera.gameObject, true);
        }

        public void OnDisable()
        {
            m_ShowBGColorOptions.valueChanged.RemoveListener(Repaint);
            m_ShowOrthoOptions.valueChanged.RemoveListener(Repaint);
            m_ShowTargetEyeOption.valueChanged.RemoveListener(Repaint);
        }

        bool TryGetDepthTextureModeString(out string depthTextureModeString)
        {
            depthTextureModeString = string.Empty;

            var buffers = new List<string>();
            if ((camera.depthTextureMode & DepthTextureMode.Depth) != 0)
                buffers.Add("Depth");
            if ((camera.depthTextureMode & DepthTextureMode.DepthNormals) != 0)
                buffers.Add("DepthNormals");
            if ((camera.depthTextureMode & DepthTextureMode.MotionVectors) != 0)
                buffers.Add("MotionVectors");

            if (buffers.Count == 0)
                return false;

            var sb = new StringBuilder("Info: renders ");
            for (var i = 0; i < buffers.Count; ++i)
            {
                if (i != 0)
                    sb.Append(" & ");

                sb.Append(buffers[i]);
            }

            sb.Append(buffers.Count > 1 ? " textures" : " texture");
            depthTextureModeString = sb.ToString();
            return true;
        }

        void DepthTextureModeGUI()
        {
            if (!showDepthTextureMode || !TryGetDepthTextureModeString(out var depthTextureModeInfo))
                return;

            EditorGUILayout.HelpBox(depthTextureModeInfo, MessageType.None, true);
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
            if (!(SceneView.currentDrawingSceneView?.in2DMode ?? false))
                return;

            if (camera == Camera.main && camera.orthographic)
                RenderGizmo(camera);
        }

        void CommandBufferGUI()
        {
            if(!showCommandBufferGUI)
                return;

            m_CommandBuffersShown = GUILayout.Toggle(m_CommandBuffersShown, GUIContent.Temp(camera.commandBufferCount + " command buffers"), EditorStyles.foldout);
            if (!m_CommandBuffersShown)
                return;
            EditorGUI.indentLevel++;
            foreach (var ce in (CameraEvent[])Enum.GetValues(typeof(CameraEvent)))
            {
                var cbs = camera.GetCommandBuffers(ce);
                foreach (var cb in cbs)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        // row with event & command buffer information label
                        var rowRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.miniLabel);
                        rowRect.xMin += EditorGUI.indent;
                        var minusRect = GetRemoveButtonRect(rowRect);
                        rowRect.xMax = minusRect.x;
                        GUI.Label(rowRect, string.Format("{0}: {1} ({2})", ce, cb.name, EditorUtility.FormatBytes(cb.sizeInBytes)), EditorStyles.miniLabel);
                        // and a button to remove it
                        if (GUI.Button(minusRect, Styles.iconRemove, Styles.invisibleButton))
                        {
                            camera.RemoveCommandBuffer(ce, cb);
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
                    camera.RemoveAllCommandBuffers();
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
            m_ShowBGColorOptions.target = showBackgroundColorOptions;
            m_ShowOrthoOptions.target = showOrthographicOptions;
            m_ShowTargetEyeOption.target = showTargetEyeOption;

            settings.DrawClearFlags();

            if (EditorGUILayout.BeginFadeGroup(m_ShowBGColorOptions.faded))
                settings.DrawBackgroundColor();
            EditorGUILayout.EndFadeGroup();

            settings.DrawCullingMask();

            EditorGUILayout.Space();

            settings.DrawProjection();

            if (showOrthographicXRWarning)
                GUILayout.Label(Styles.orthoXRWarning);

            settings.DrawClippingPlanes();

            settings.DrawNormalizedViewPort();

            EditorGUILayout.Space();
            settings.DrawDepth();
            settings.DrawRenderingPath();

            if (showOrthographicDeferredWarning)
                EditorGUILayout.HelpBox(Styles.orthoDeferredWarning.text, MessageType.Warning, true);

            settings.DrawTargetTexture(wantsDeferredRendering);
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

        public virtual Overlay CreatePreviewOverlay(Camera cam) => SceneViewCameraOverlay.GetOrCreateCameraOverlay(cam);

        [RequiredByNativeCode]
        internal static float GetGameViewAspectRatio()
        {
            var gameViewSize = PlayModeView.GetMainPlayModeViewTargetSize();
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
        [RequiredByNativeCode]
        internal static void RenderGizmo(Camera camera) => CameraEditorUtils.DrawFrustumGizmo(camera);

        static Vector2 s_PreviousMainPlayModeViewTargetSize;

        public virtual void OnSceneGUI()
        {
            if (!target)
                return;
            var c = (Camera)target;

            if (!CameraEditorUtils.IsViewportRectValidToRender(c.rect))
                return;

            var currentMainPlayModeViewTargetSize = PlayModeView.GetMainPlayModeViewTargetSize();
            if (s_PreviousMainPlayModeViewTargetSize != currentMainPlayModeViewTargetSize)
            {
                // a gameView size change can affect horizontal FOV, refresh the inspector when that happens.
                Repaint();
                s_PreviousMainPlayModeViewTargetSize = currentMainPlayModeViewTargetSize;
            }

            CameraEditorUtils.HandleFrustum(c, referenceTargetIndex);
        }

        static T ExtendedQuery<T>(VisualElement editor, string elementName, SerializedProperty property) where T : VisualElement
        {
            var content = EditorGUIUtility.TrTextContent(property.displayName, property.tooltip);
            return ExtendedQuery<T>(editor, elementName, content);
        }

        static T ExtendedQuery<T>(VisualElement editor, string elementName, GUIContent content) where T : VisualElement
        {
            var result = editor.MandatoryQ<T>(elementName);

            switch (result)
            {
                case PropertyField field:
                    field.label = content.text;
                    break;

                case DropdownField field:
                    field.label = content.text;
                    break;

                case EnumField field:
                    field.label = content.text;
                    break;

                case Toggle toggle:
                    toggle.label = content.text;
                    break;

                case Slider slider:
                    slider.label = content.text;
                    break;

                case FloatField field:
                    field.label = content.text;
                    break;

                case ClippingPlanes planes:
                    planes.label = content.text;
                    break;

                case HelpBox box:
                    box.text = content.text;
                    break;
            }

            result.tooltip = content.tooltip;
            return result;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var editor = new VisualElement();
            var c = (Camera)target;
            
            var visualTree = EditorGUIUtility.Load(Styles.k_CameraEditorUxmlPath) as VisualTreeAsset;
            visualTree?.CloneTree(editor);

            var clearFlags = ExtendedQuery<PropertyField>(editor, Styles.k_ClearFlagsElementName, Styles.clearFlags);
            var backgroundColor = ExtendedQuery<PropertyField>(editor, Styles.k_BackgroundElementName, Styles.background);
            ExtendedQuery<PropertyField>(editor, Styles.k_CullingMaskElementName, Styles.allowOcclusionCulling);

            var backgroundCheck = UIElementsEditorUtility.CreateDynamicVisibilityCallback(backgroundColor, () => showBackgroundColorOptions);
            clearFlags.RegisterValueChangeCallback(e => backgroundCheck.Invoke());

            SetupProjectionParameters(editor);

            var clippingPlanes = ExtendedQuery<ClippingPlanes>(editor, Styles.k_ClippingPlanesElementName, EditorGUI.s_ClipingPlanesLabel);
            clippingPlanes.nearClip = settings.nearClippingPlane;
            clippingPlanes.farClip = settings.farClippingPlane;

            ExtendedQuery<PropertyField>(editor, Styles.k_ViewportRectElementName, Styles.viewportRect);
            ExtendedQuery<PropertyField>(editor, Styles.k_DepthElementName, settings.depth);

            var renderingPath = ExtendedQuery<DropdownField>(editor, Styles.k_RenderingPathElementName, Styles.renderingPath);
            var renderingPathUpdate = UIElementsEditorUtility.BindSerializedProperty(renderingPath, settings.renderingPath, Settings.k_CameraRenderPaths, Settings.k_CameraRenderPathValues);

            var orthographicDeferredWarning = ExtendedQuery<HelpBox>(editor, Styles.k_OrthographicDeferredWarningElementName, Styles.orthoDeferredWarning);
            var orthographicDeferrednCheck = UIElementsEditorUtility.CreateDynamicVisibilityCallback(orthographicDeferredWarning, () => showOrthographicDeferredWarning);

            var deferredMSAAWarning = ExtendedQuery<HelpBox>(editor, Styles.k_DeferredMsaaWarningElementName, Styles.deferredMSAAWarning);
            var deferredMSAACheck = UIElementsEditorUtility.CreateDynamicVisibilityCallback(deferredMSAAWarning, () => showDeferredMSAAWarning);

            ExtendedQuery<PropertyField>(editor, Styles.k_TargetTextureElementName, settings.targetTexture);
            ExtendedQuery<PropertyField>(editor, Styles.k_OcclusionCullingElementName, Styles.allowOcclusionCulling);

            var hdr = ExtendedQuery<DropdownField>(editor, Styles.k_HdrElementName, Styles.allowHDR);
            var msaa = ExtendedQuery<DropdownField>(editor, Styles.k_MsaaElementName, Styles.allowMSAA);
            var hdrUpdate = UIElementsEditorUtility.BindSerializedProperty(hdr, settings.HDR, Settings.k_DefaultOptions, Settings.k_DefaultOptionValues);
            var msaaUpdate = UIElementsEditorUtility.BindSerializedProperty(msaa, settings.allowMSAA, Settings.k_DefaultOptions, Settings.k_DefaultOptionValues);

            ExtendedQuery<PropertyField>(editor, Styles.k_DynamicResolution, Styles.allowDynamicResolution);

            var dynamicResolutionTimingWarning = ExtendedQuery<HelpBox>(editor, Styles.k_DynamicResolutionTimingWarningElementName, Styles.dynamicResolutionTimingWarning);
            var dynamicResolutionTimingCheck = UIElementsEditorUtility.CreateDynamicVisibilityCallback(dynamicResolutionTimingWarning, () => showDynamicResolutionTimingWarning);

            var cameraWarnings = editor.MandatoryQ<VisualElement>(Styles.k_CameraWarningsElementName);
            var cameraWarningsUpdate = UIElementsEditorUtility.CreateDynamicVisibilityCallback(cameraWarnings, () =>
            {
                var visible = false;
                cameraWarnings.Clear();

                foreach (var target in targets)
                {
                    if (target is not Camera camera)
                        continue;

                    var warnings = camera.GetCameraBufferWarnings();

                    if (warnings.Length == 0)
                        continue;

                    cameraWarnings.Add(new HelpBox(string.Join("\n\n", warnings), HelpBoxMessageType.Warning));
                    visible = true;
                }

                return visible;
            });

            var vrGroup = editor.MandatoryQ<VisualElement>(Styles.k_VrGroupElementName);
            var vrGroupCheck = UIElementsEditorUtility.CreateDynamicVisibilityCallback(vrGroup, () => vrEnabled);

            Action targetDisplayCheck;
            Action targetDisplayUpdate;
            var targetDisplay = ExtendedQuery<DropdownField>(editor, Styles.k_TargetDisplayElementName, Styles.targetDisplay);
            var choiceContents = DisplayUtility.GetDisplayNames();
            targetDisplay.choices = new List<string>(choiceContents.Length);

            foreach (var choiceContent in choiceContents)
                targetDisplay.choices.Add(choiceContent.text);

            targetDisplayCheck = UIElementsEditorUtility.CreateDynamicVisibilityCallback(targetDisplay, () => showMultiDisplayOptions);
            targetDisplayUpdate = UIElementsEditorUtility.BindSerializedProperty(targetDisplay, settings.targetDisplay, DisplayUtility.GetDisplayNames(), DisplayUtility.GetDisplayIndices());
            
            var targetEye = ExtendedQuery<DropdownField>(editor, Styles.k_TargetEyeElementName, Styles.xrTargetEye);
            var targetEyeCheck = UIElementsEditorUtility.CreateDynamicVisibilityCallback(targetEye, () => showTargetEyeOption);
            var targetEyeUpdate = UIElementsEditorUtility.BindSerializedProperty(targetEye, settings.targetEye, Settings.k_TargetEyes, Settings.k_TargetEyeValues);

            var depthTextureMode = editor.MandatoryQ<HelpBox>(Styles.k_DepthTextureModeElementName);
            var depthTextureModeCheck = UIElementsEditorUtility.CreateDynamicVisibilityCallback(depthTextureMode, () =>
            {
                if (!showDepthTextureMode || !TryGetDepthTextureModeString(out var info))
                    return false;

                depthTextureMode.text = info;
                return true;
            });

            var buffers = new List<CommandBuffer>();
            var bufferEvents = new Dictionary<CommandBuffer, CameraEvent>();
            var commandBuffers = editor.MandatoryQ<Foldout>(Styles.k_CommandBuffersElementName);
            var list = commandBuffers.MandatoryQ<ListView>(Styles.k_CommandBuffersListElementName);

            var commandBuffersUpdate = UIElementsEditorUtility.CreateDynamicVisibilityCallback(commandBuffers, () =>
            {
                commandBuffers.text = L10n.Tr($"{c.commandBufferCount} command buffers");

                buffers.Clear();
                bufferEvents.Clear();
                foreach (var ce in (CameraEvent[])Enum.GetValues(typeof(CameraEvent)))
                {
                    var cbs = camera.GetCommandBuffers(ce);
                    foreach (var cb in cbs)
                    {
                        buffers.Add(cb);
                        bufferEvents[cb] = ce;
                    }
                }
                list.Rebuild();

                return showCommandBufferGUI;
            });
            list.makeItem = () =>
            {
                var entry = new VisualElement();
                entry.AddToClassList(Styles.commandBufferUssClassName);

                var label = new Label();
                label.AddToClassList(Styles.commandBufferLabelUssClassName);

                var button = new VisualElement();
                button.AddToClassList(Styles.commandBufferRemoveButtonUssClassName);
                button.RegisterCallback<ClickEvent>(e =>
                {
                    var cameraEvent = (CameraEvent)entry.userData;
                    var commandBuffer = (CommandBuffer)label.userData;

                    camera.RemoveCommandBuffer(cameraEvent, commandBuffer);
                    SceneView.RepaintAll();
                    PlayModeView.RepaintAll();

                    commandBuffersUpdate?.Invoke();
                });

                entry.Add(label);
                entry.Add(button);
                return entry;
            };
            list.bindItem = (v, i) =>
            {
                var cb = buffers[i];
                var ce = bufferEvents[cb];

                var label = v.Q<Label>();
                label.text = $"{ce}: {cb.name} ({EditorUtility.FormatBytes(cb.sizeInBytes)})";
                label.userData = cb;
                v.userData = ce;
            };
            list.itemsSource = buffers;

            var removeAll = editor.MandatoryQ<Button>(Styles.k_CommandBuffersRemoveAllElementName);
            removeAll.RegisterCallback<ClickEvent>(e =>
            {
                c.RemoveAllCommandBuffers();
                SceneView.RepaintAll();
                PlayModeView.RepaintAll();

                commandBuffersUpdate?.Invoke();
            });

            onSettingsChanged += () =>
            {
                clippingPlanes.Update();

                renderingPathUpdate?.Invoke();
                hdrUpdate?.Invoke();
                msaaUpdate?.Invoke();
                targetDisplayUpdate?.Invoke();
                targetEyeUpdate?.Invoke();

                orthographicDeferrednCheck?.Invoke();
                deferredMSAACheck?.Invoke();
                dynamicResolutionTimingCheck?.Invoke();
                depthTextureModeCheck?.Invoke();
                cameraWarningsUpdate?.Invoke();

                vrGroupCheck?.Invoke();
                targetDisplayCheck?.Invoke();
                targetEyeCheck?.Invoke();
                commandBuffersUpdate?.Invoke();
            };

            editor.RegisterCallback<AttachToPanelEvent>(e => EditorApplication.tick += UpdateNonSerializedIfNeeded);
            editor.RegisterCallback<DetachFromPanelEvent>(e => EditorApplication.tick -= UpdateNonSerializedIfNeeded);
            editor.TrackSerializedObjectValue(serializedObject, (so) => onSettingsChanged?.Invoke());

            return editor;
        }

        void UpdateNonSerializedIfNeeded()
        {
            if (m_LastNonSerializedVersion == camera.m_NonSerializedVersion)
                return;

            onSettingsChanged?.Invoke();
            m_LastNonSerializedVersion = camera.m_NonSerializedVersion;
        }

        void SetupProjectionParameters(VisualElement contents)
        {
            var perspectiveGroup = contents.MandatoryQ<VisualElement>(Styles.k_PerspectiveGroupElementName);
            var perspectiveGroupCheck = UIElementsEditorUtility.CreateDynamicVisibilityCallback(perspectiveGroup, () => showPerspectiveOptions);

            var orthographicGroup = contents.MandatoryQ<VisualElement>(Styles.k_OrthographicGroupElementName);
            var orthographicGroupCheck = UIElementsEditorUtility.CreateDynamicVisibilityCallback(orthographicGroup, () => showOrthographicOptions);

            var projectionType = ExtendedQuery<EnumField>(contents, Styles.k_ProjectionTypeElementName, Styles.projection);
            var projectionTypeUpdate = UIElementsEditorUtility.BindSerializedProperty<ProjectionType>(projectionType, settings.orthographic, _ =>
            {
                orthographicGroupCheck?.Invoke();
                perspectiveGroupCheck?.Invoke();
            });

            var fovAxisMode = ExtendedQuery<EnumField>(contents, Styles.k_FovAxisModeElementName, Styles.FOVAxisMode);
            var fovAxisModeUpdate = UIElementsEditorUtility.BindSerializedProperty<Camera.FieldOfViewAxis>(fovAxisMode, settings.fovAxisMode);

            var fieldOfView = ExtendedQuery<Slider>(contents, Styles.k_FieldOfViewElementName, Styles.fieldOfView);
            fieldOfView.SetEnabled(!fovAxisMode.showMixedValue);

            var physicalCameraGroup = contents.MandatoryQ<VisualElement>(Styles.k_PhysicalCameraGroupElementName);
            var physicalCameraGroupCheck = UIElementsEditorUtility.CreateDynamicVisibilityCallback(physicalCameraGroup, () => showPhysicalCameraOptions);

            var physicalCamera = ExtendedQuery<Toggle>(contents, Styles.k_PhysicalCameraElementName, Styles.physicalCamera);
            var physicalCameraUpdate = UIElementsEditorUtility.BindSerializedProperty(physicalCamera, settings.projectionMatrixMode,
                p => p.intValue == (int)Camera.ProjectionMatrixMode.PhysicalPropertiesBased,
                (v, p) =>
                {
                    p.intValue = (int)(v ? Camera.ProjectionMatrixMode.PhysicalPropertiesBased : Camera.ProjectionMatrixMode.Implicit);
                    p.serializedObject.ApplyModifiedProperties();
                    physicalCameraGroupCheck?.Invoke();
                });

            ExtendedQuery<PropertyField>(contents, Styles.k_IsoElementName, Styles.iso);
            ExtendedQuery<PropertyField>(contents, Styles.k_ShutterSpeedElementName, Styles.shutterSpeed);
            ExtendedQuery<PropertyField>(contents, Styles.k_ApertureElementName, Styles.aperture);
            ExtendedQuery<PropertyField>(contents, Styles.k_FocusDistanceElementName, Styles.focusDistance);
            ExtendedQuery<PropertyField>(contents, Styles.k_BladeCountElementName, Styles.bladeCount);
            ExtendedQuery<PropertyField>(contents, Styles.k_CurvatureElementName, Styles.curvature);
            ExtendedQuery<PropertyField>(contents, Styles.k_BarrelClippingElementName, Styles.barrelClipping);
            ExtendedQuery<PropertyField>(contents, Styles.k_AnamorphismElementName, Styles.anamorphism);

            var fieldOfViewUpdate = UIElementsEditorUtility.BindSerializedProperty(fieldOfView, settings.verticalFOV,
                p =>
                {
                    var isPhysicalCamera = settings.projectionMatrixMode.intValue == (int)Camera.ProjectionMatrixMode.PhysicalPropertiesBased;
                    var fovAxisVertical = settings.fovAxisMode.intValue == (int)Camera.FieldOfViewAxis.Vertical;
                    var aspectRatio = isPhysicalCamera ? settings.sensorSize.vector2Value.x / settings.sensorSize.vector2Value.y : camera.aspect;
                    
                    fieldOfView.lowValue = fovAxisVertical ? 0.00001f : Camera.VerticalToHorizontalFieldOfView(0.00001f, aspectRatio);
                    fieldOfView.highValue = fovAxisVertical ? 179.0f : Camera.VerticalToHorizontalFieldOfView(179.0f, aspectRatio);
                    return fovAxisVertical ? settings.verticalFOV.floatValue : Camera.VerticalToHorizontalFieldOfView(settings.verticalFOV.floatValue, aspectRatio);
                },
                (fov, p) =>
                {
                    var isPhysicalCamera = settings.projectionMatrixMode.intValue == (int)Camera.ProjectionMatrixMode.PhysicalPropertiesBased;
                    var fovAxisVertical = settings.fovAxisMode.intValue == (int)Camera.FieldOfViewAxis.Vertical;
                    var aspectRatio = isPhysicalCamera ? settings.sensorSize.vector2Value.x / settings.sensorSize.vector2Value.y : camera.aspect;

                    p.floatValue = fovAxisVertical ? fov : Camera.HorizontalToVerticalFieldOfView(fov, aspectRatio);
                    p.serializedObject.ApplyModifiedProperties();
                });

            var focalLength = ExtendedQuery<FloatField>(contents, Styles.k_FocalLengthElementName, Styles.focalLength);
            var focalLengthUpdate = UIElementsEditorUtility.BindSerializedProperty(focalLength, settings.focalLength,
                p =>
                {
                    var fovAxisVertical = settings.fovAxisMode.intValue == (int)Camera.FieldOfViewAxis.Vertical;
                    var sensorLength = fovAxisVertical ? settings.sensorSize.vector2Value.y : settings.sensorSize.vector2Value.x;
                    return Camera.FieldOfViewToFocalLength(fieldOfView.value, sensorLength);
                },
                (v, p) =>
                {
                    var isPhysicalCamera = settings.projectionMatrixMode.intValue == (int)Camera.ProjectionMatrixMode.PhysicalPropertiesBased;

                    if (!isPhysicalCamera)
                        return;

                    p.floatValue = v;
                    p.serializedObject.ApplyModifiedProperties();
                });

            var sensorType = ExtendedQuery<DropdownField>(contents, Styles.k_SensorTypeElementName, Styles.cameraType);
            sensorType.choices = new List<string>(Settings.k_ApertureFormats.Length);

            foreach (var apertureFormat in Settings.k_ApertureFormats)
                sensorType.choices.Add(apertureFormat);

            var sensorTypeUpdate = UIElementsEditorUtility.BindSerializedProperty(sensorType, settings.sensorSize,
                p =>
                {
                    var approximateApertureFormat = new Vector2((float)Math.Round(p.vector2Value.x, 3), (float)Math.Round(p.vector2Value.y, 3));
                    var index = Array.IndexOf(Settings.k_ApertureFormatValues, approximateApertureFormat);
                    return index >= 0 ? index : Settings.k_ApertureFormatValues.Length;
                },
                (i, p) =>
                {
                    if (i < 0 || i >= Settings.k_ApertureFormatValues.Length)
                        return;

                    p.vector2Value = Settings.k_ApertureFormatValues[i];
                    p.serializedObject.ApplyModifiedProperties();
                });

            ExtendedQuery<PropertyField>(contents, Styles.k_SensorSizeElementName, Styles.sensorSize);
            ExtendedQuery<PropertyField>(contents, Styles.k_LensShiftElementName, Styles.lensShift);

            var gateFit = ExtendedQuery<EnumField>(contents, Styles.k_GateFitElementName, Styles.gateFit);
            var gateFitUpdate = UIElementsEditorUtility.BindSerializedProperty<Camera.GateFitMode>(gateFit, settings.gateFit);

            // Cannot bind serialized properties with spaces in them via UI Builder
            var orthographicSize = ExtendedQuery<PropertyField>(contents, Styles.k_OrthographicSizeElementName, Styles.size);
            orthographicSize.BindProperty(settings.orthographicSize);

            var orthographicXRWarning = ExtendedQuery<HelpBox>(contents, Styles.k_OrthographicXrWarningElementName, Styles.orthoXRWarning);
            var orthographicXRCheck = UIElementsEditorUtility.CreateDynamicVisibilityCallback(orthographicXRWarning, () => showOrthographicXRWarning);

            onSettingsChanged += () =>
            {
                projectionTypeUpdate?.Invoke();
                fovAxisModeUpdate?.Invoke();
                physicalCameraUpdate?.Invoke();
                fieldOfViewUpdate?.Invoke();
                focalLengthUpdate?.Invoke();
                sensorTypeUpdate?.Invoke();
                gateFitUpdate?.Invoke();

                orthographicXRCheck?.Invoke();
                physicalCameraGroupCheck?.Invoke();
            };
        }
    }
}
