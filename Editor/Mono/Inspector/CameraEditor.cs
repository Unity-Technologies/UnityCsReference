// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using AnimatedBool = UnityEditor.AnimatedValues.AnimBool;
using UnityEngine.Scripting;
using UnityEditor.Modules;

namespace UnityEditor
{
    [CustomEditor(typeof(Camera))]
    [CanEditMultipleObjects]
    internal class CameraEditor : Editor
    {
        private class Styles
        {
            public static GUIContent iconRemove = EditorGUIUtility.IconContent("Toolbar Minus", "|Remove command buffer");
            public static GUIStyle invisibleButton = "InvisibleButton";
        }

        // Manually entered rendering path names/values, since we want to show them
        // in different order than they appear in the enum.
        private static readonly GUIContent[] kCameraRenderPaths =
        {
            new GUIContent("Use Graphics Settings"),
            new GUIContent("Forward"),
            new GUIContent("Deferred"),
            new GUIContent("Legacy Vertex Lit"),
            new GUIContent("Legacy Deferred (light prepass)")
        };
        private static readonly int[] kCameraRenderPathValues =
        {
            (int)RenderingPath.UsePlayerSettings,
            (int)RenderingPath.Forward,
            (int)RenderingPath.DeferredShading,
            (int)RenderingPath.VertexLit,
            (int)RenderingPath.DeferredLighting
        };

        SerializedProperty m_ClearFlags;
        SerializedProperty m_BackgroundColor;
        SerializedProperty m_NormalizedViewPortRect;
        SerializedProperty m_FieldOfView;
        SerializedProperty m_Orthographic;
        SerializedProperty m_OrthographicSize;
        SerializedProperty m_Depth;
        SerializedProperty m_CullingMask;
        SerializedProperty m_RenderingPath;
        SerializedProperty m_OcclusionCulling;
        SerializedProperty m_TargetTexture;
        SerializedProperty m_HDR;
        SerializedProperty m_AllowMSAA;
        SerializedProperty m_AllowDynamicResolution;
        SerializedProperty[] m_NearAndFarClippingPlanes;
        SerializedProperty m_StereoConvergence;
        SerializedProperty m_StereoSeparation;

        SerializedProperty m_TargetDisplay;

        SerializedProperty m_TargetEye;

        private static readonly GUIContent[] kTargetEyes =
        {
            new GUIContent("Both"),
            new GUIContent("Left"),
            new GUIContent("Right"),
            new GUIContent("None (Main Display)"),
        };
        private static readonly int[] kTargetEyeValues = { (int)StereoTargetEyeMask.Both, (int)StereoTargetEyeMask.Left, (int)StereoTargetEyeMask.Right, (int)StereoTargetEyeMask.None };

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

        enum ProjectionType { Perspective, Orthographic };

        private Camera m_PreviewCamera;
        private Camera previewCamera
        {
            get
            {
                if (m_PreviewCamera == null)
                    m_PreviewCamera = EditorUtility.CreateGameObjectWithHideFlags("Preview Camera", HideFlags.HideAndDontSave, typeof(Camera), typeof(Skybox)).GetComponent<Camera>();
                m_PreviewCamera.enabled = false;
                return m_PreviewCamera;
            }
        }

        // should match color in GizmosDrawers.cpp
        private static readonly Color kGizmoCamera = new Color(233f / 255f, 233f / 255f, 233f / 255f, 128f / 255f);

        private const float kPreviewNormalizedSize = 0.2f;

        private readonly GUIContent m_ViewportLabel = EditorGUIUtility.TextContent("Viewport Rect|Four values that indicate where on the screen this camera view will be drawn. Measured in Viewport Coordinates (values 0–1).");

        private bool m_CommandBuffersShown = true;

        public void OnEnable()
        {
            m_ClearFlags = serializedObject.FindProperty("m_ClearFlags");
            m_BackgroundColor = serializedObject.FindProperty("m_BackGroundColor");
            m_NormalizedViewPortRect = serializedObject.FindProperty("m_NormalizedViewPortRect");
            m_NearAndFarClippingPlanes = new[] { serializedObject.FindProperty("near clip plane"), serializedObject.FindProperty("far clip plane") };
            m_FieldOfView = serializedObject.FindProperty("field of view");
            m_Orthographic = serializedObject.FindProperty("orthographic");
            m_OrthographicSize = serializedObject.FindProperty("orthographic size");
            m_Depth = serializedObject.FindProperty("m_Depth");
            m_CullingMask = serializedObject.FindProperty("m_CullingMask");
            m_RenderingPath = serializedObject.FindProperty("m_RenderingPath");
            m_OcclusionCulling = serializedObject.FindProperty("m_OcclusionCulling");
            m_TargetTexture = serializedObject.FindProperty("m_TargetTexture");
            m_HDR = serializedObject.FindProperty("m_HDR");
            m_AllowMSAA = serializedObject.FindProperty("m_AllowMSAA");
            m_AllowDynamicResolution = serializedObject.FindProperty("m_AllowDynamicResolution");

            m_StereoConvergence = serializedObject.FindProperty("m_StereoConvergence");
            m_StereoSeparation = serializedObject.FindProperty("m_StereoSeparation");

            m_TargetDisplay = serializedObject.FindProperty("m_TargetDisplay");

            m_TargetEye = serializedObject.FindProperty("m_TargetEye");

            var c = (Camera)target;
            m_ShowBGColorOptions.value = !m_ClearFlags.hasMultipleDifferentValues && (c.clearFlags == CameraClearFlags.SolidColor || c.clearFlags == CameraClearFlags.Skybox);
            m_ShowOrthoOptions.value = c.orthographic;
            m_ShowTargetEyeOption.value = m_TargetEye.intValue != (int)StereoTargetEyeMask.Both || PlayerSettings.virtualRealitySupported;

            m_ShowBGColorOptions.valueChanged.AddListener(Repaint);
            m_ShowOrthoOptions.valueChanged.AddListener(Repaint);
            m_ShowTargetEyeOption.valueChanged.AddListener(Repaint);
        }

        internal void OnDisable()
        {
            m_ShowBGColorOptions.valueChanged.RemoveListener(Repaint);
            m_ShowOrthoOptions.valueChanged.RemoveListener(Repaint);
            m_ShowTargetEyeOption.valueChanged.RemoveListener(Repaint);
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
                            GameView.RepaintAll();
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
                    GameView.RepaintAll();
                }
            }
            EditorGUI.indentLevel--;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var c = (Camera)target;
            m_ShowBGColorOptions.target = !m_ClearFlags.hasMultipleDifferentValues && (c.clearFlags == CameraClearFlags.SolidColor || c.clearFlags == CameraClearFlags.Skybox);
            m_ShowOrthoOptions.target = !m_Orthographic.hasMultipleDifferentValues && c.orthographic;
            m_ShowTargetEyeOption.target = m_TargetEye.intValue != (int)StereoTargetEyeMask.Both || PlayerSettings.virtualRealitySupported;

            EditorGUILayout.PropertyField(m_ClearFlags, EditorGUIUtility.TextContent("Clear Flags|What to display in empty areas of this Camera's view.\n\nChoose Skybox to display a skybox in empty areas, defaulting to a background color if no skybox is found.\n\nChoose Solid Color to display a background color in empty areas.\n\nChoose Depth Only to display nothing in empty areas.\n\nChoose Don't Clear to display whatever was displayed in the previous frame in empty areas."));
            if (EditorGUILayout.BeginFadeGroup(m_ShowBGColorOptions.faded))
                EditorGUILayout.PropertyField(m_BackgroundColor, EditorGUIUtility.TextContent("Background|The Camera clears the screen to this color before rendering."));
            EditorGUILayout.EndFadeGroup();
            EditorGUILayout.PropertyField(m_CullingMask);

            EditorGUILayout.Space();

            ProjectionType projectionType = m_Orthographic.boolValue ? ProjectionType.Orthographic : ProjectionType.Perspective;
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = m_Orthographic.hasMultipleDifferentValues;
            projectionType = (ProjectionType)EditorGUILayout.EnumPopup(EditorGUIUtility.TextContent("Projection|How the Camera renders perspective.\n\nChoose Perspective to render objects with perspective.\n\nChoose Orthographic to render objects uniformly, with no sense of perspective."), projectionType);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                m_Orthographic.boolValue = (projectionType == ProjectionType.Orthographic);

            if (!m_Orthographic.hasMultipleDifferentValues)
            {
                if (EditorGUILayout.BeginFadeGroup(m_ShowOrthoOptions.faded))
                    EditorGUILayout.PropertyField(m_OrthographicSize, new GUIContent("Size"));
                EditorGUILayout.EndFadeGroup();
                if (EditorGUILayout.BeginFadeGroup(1 - m_ShowOrthoOptions.faded))
                    EditorGUILayout.Slider(m_FieldOfView, 1f, 179f, EditorGUIUtility.TextContent("Field of View|The width of the Camera’s view angle, measured in degrees along the local Y axis."));
                EditorGUILayout.EndFadeGroup();
            }

            EditorGUILayout.PropertiesField(EditorGUI.s_ClipingPlanesLabel, m_NearAndFarClippingPlanes, EditorGUI.s_NearAndFarLabels, EditorGUI.kNearFarLabelsWidth);

            EditorGUILayout.PropertyField(m_NormalizedViewPortRect, m_ViewportLabel);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_Depth);
            EditorGUILayout.IntPopup(m_RenderingPath, kCameraRenderPaths, kCameraRenderPathValues, EditorGUIUtility.TempContent("Rendering Path"));
            if (m_ShowOrthoOptions.target && wantDeferredRendering)
            {
                EditorGUILayout.HelpBox("Deferred rendering does not work with Orthographic camera, will use Forward.", MessageType.Warning, true);
            }


            EditorGUILayout.PropertyField(m_TargetTexture);

            // show warning if we have deferred but manual MSAA set
            // only do this if the m_TargetTexture has the same values across all target cameras
            if (!m_TargetTexture.hasMultipleDifferentValues)
            {
                var targetTexture = m_TargetTexture.objectReferenceValue as RenderTexture;
                if (targetTexture
                    && targetTexture.antiAliasing > 1
                    && wantDeferredRendering)
                {
                    EditorGUILayout.HelpBox("Manual MSAA target set with deferred rendering. This will lead to undefined behavior.", MessageType.Warning, true);
                }
            }

            EditorGUILayout.PropertyField(m_OcclusionCulling);
            EditorGUILayout.PropertyField(m_HDR, EditorGUIUtility.TempContent("Allow HDR"));
            EditorGUILayout.PropertyField(m_AllowMSAA);
            EditorGUILayout.PropertyField(m_AllowDynamicResolution);

            DisplayCameraWarnings();

            if (PlayerSettings.virtualRealitySupported)
            {
                EditorGUILayout.PropertyField(m_StereoSeparation);
                EditorGUILayout.PropertyField(m_StereoConvergence);
            }

            if (ModuleManager.ShouldShowMultiDisplayOption())
            {
                int prevDisplay = m_TargetDisplay.intValue;
                EditorGUILayout.Space();
                EditorGUILayout.IntPopup(m_TargetDisplay, DisplayUtility.GetDisplayNames(), DisplayUtility.GetDisplayIndices(), EditorGUIUtility.TempContent("Target Display"));
                if (prevDisplay != m_TargetDisplay.intValue)
                    GameView.RepaintAll();
            }

            if (EditorGUILayout.BeginFadeGroup(m_ShowTargetEyeOption.faded))
                EditorGUILayout.IntPopup(m_TargetEye, kTargetEyes, kTargetEyeValues, EditorGUIUtility.TempContent("Target Eye"));
            EditorGUILayout.EndFadeGroup();

            DepthTextureModeGUI();
            CommandBufferGUI();

            serializedObject.ApplyModifiedProperties();
        }

        private void DisplayCameraWarnings()
        {
            Camera camera = target as Camera;
            if (camera != null)
            {
                string[] warnings = camera.GetCameraBufferWarnings();
                if (warnings.Length > 0)
                    EditorGUILayout.HelpBox(string.Join("\n\n", warnings), MessageType.Warning, true);
            }
        }

        public void OnOverlayGUI(Object target, SceneView sceneView)
        {
            if (target == null) return;

            // cache some deep values
            var c = (Camera)target;

            Vector2 previewSize = GameView.GetMainGameViewTargetSize();
            if (previewSize.x < 0f)
            {
                // Fallback to Scene View of not a valid game view size
                previewSize.x = sceneView.position.width;
                previewSize.y = sceneView.position.height;
            }
            // Apply normalizedviewport rect of camera
            Rect normalizedViewPortRect = c.rect;
            previewSize.x *= Mathf.Max(normalizedViewPortRect.width, 0f);
            previewSize.y *= Mathf.Max(normalizedViewPortRect.height, 0f);

            // Prevent using invalid previewSize
            if (previewSize.x <= 0f || previewSize.y <= 0f)
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

            if (Event.current.type == EventType.Repaint)
            {
                // setup camera and render
                previewCamera.CopyFrom(c);
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

                previewCamera.targetTexture = RenderTexture.GetTemporary((int)previewSize.x, (int)previewSize.y, 24, previewCamera.allowHDR ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32);
                Handles.EmitGUIGeometryForCamera(c, previewCamera);
                previewCamera.Render();

                GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
                GUI.DrawTexture(cameraRect, previewCamera.targetTexture, ScaleMode.StretchToFill, false);
                GL.sRGBWrite = false;

                RenderTexture.ReleaseTemporary(previewCamera.targetTexture);
            }
        }

        [RequiredByNativeCode]
        static float GetGameViewAspectRatio()
        {
            Vector2 gameViewSize = GameView.GetMainGameViewTargetSize();
            if (gameViewSize.x < 0f)
            {
                // Fallback to Scene View of not a valid game view size
                gameViewSize.x = Screen.width;
                gameViewSize.y = Screen.height;
            }

            return gameViewSize.x / gameViewSize.y;
        }

        static float GetFrustumAspectRatio(Camera camera)
        {
            Rect normalizedViewPortRect = camera.rect;
            if (normalizedViewPortRect.width <= 0f || normalizedViewPortRect.height <= 0f)
                return -1f;

            float viewportAspect = normalizedViewPortRect.width / normalizedViewPortRect.height;
            return GetGameViewAspectRatio() * viewportAspect;
        }

        // Returns near- and far-corners in this order: leftBottom, leftTop, rightTop, rightBottom
        // Assumes input arrays are of length 4 (if allocated)
        static bool GetFrustum(Camera camera, Vector3[] near, Vector3[] far, out float frustumAspect)
        {
            frustumAspect = GetFrustumAspectRatio(camera);
            if (frustumAspect < 0)
                return false;

            if (far != null)
            {
                far[0] = new Vector3(0, 0, camera.farClipPlane); // leftBottomFar
                far[1] = new Vector3(0, 1, camera.farClipPlane); // leftTopFar
                far[2] = new Vector3(1, 1, camera.farClipPlane); // rightTopFar
                far[3] = new Vector3(1, 0, camera.farClipPlane); // rightBottomFar
                for (int i = 0; i < 4; ++i)
                    far[i] = camera.ViewportToWorldPoint(far[i]);
            }

            if (near != null)
            {
                near[0] = new Vector3(0, 0, camera.nearClipPlane); // leftBottomNear
                near[1] = new Vector3(0, 1, camera.nearClipPlane); // leftTopNear
                near[2] = new Vector3(1, 1, camera.nearClipPlane); // rightTopNear
                near[3] = new Vector3(1, 0, camera.nearClipPlane); // rightBottomNear
                for (int i = 0; i < 4; ++i)
                    near[i] = camera.ViewportToWorldPoint(near[i]);
            }
            return true;
        }

        // Called from C++ when we need to render a Camera's gizmo
        static internal void RenderGizmo(Camera camera)
        {
            var near = new Vector3[4];
            var far = new Vector3[4];
            float frustumAspect;
            if (GetFrustum(camera, near, far, out frustumAspect))
            {
                Color orgColor = Handles.color;
                Handles.color = kGizmoCamera;
                for (int i = 0; i < 4; ++i)
                {
                    Handles.DrawLine(near[i], near[(i + 1) % 4]);
                    Handles.DrawLine(far[i], far[(i + 1) % 4]);
                    Handles.DrawLine(near[i], far[i]);
                }
                Handles.color = orgColor;
            }
        }

        static bool IsViewPortRectValidToRender(Rect normalizedViewPortRect)
        {
            if (normalizedViewPortRect.width <= 0f || normalizedViewPortRect.height <= 0f)
                return false;
            if (normalizedViewPortRect.x >= 1f || normalizedViewPortRect.xMax <= 0f)
                return false;
            if (normalizedViewPortRect.y >= 1f || normalizedViewPortRect.yMax <= 0f)
                return false;
            return true;
        }

        public void OnSceneGUI()
        {
            var c = (Camera)target;

            if (!IsViewPortRectValidToRender(c.rect))
                return;

            SceneViewOverlay.Window(new GUIContent("Camera Preview"), OnOverlayGUI, (int)SceneViewOverlay.Ordering.Camera, target, SceneViewOverlay.WindowDisplayOption.OneWindowPerTarget);

            Color orgHandlesColor = Handles.color;
            Color slidersColor = kGizmoCamera;
            slidersColor.a *= 2f;
            Handles.color = slidersColor;

            // get the corners of the far clip plane in world space
            var far = new Vector3[4];
            float frustumAspect;
            if (!GetFrustum(c, null, far, out frustumAspect))
                return;
            Vector3 leftBottomFar = far[0];
            Vector3 leftTopFar = far[1];
            Vector3 rightTopFar = far[2];
            Vector3 rightBottomFar = far[3];

            // manage our own gui changed state, so we can use it for individual slider changes
            bool guiChanged = GUI.changed;

            // FOV handles
            Vector3 farMid = Vector3.Lerp(leftBottomFar, rightTopFar, 0.5f);

            // Top and bottom handles
            float halfHeight = -1.0f;
            Vector3 changedPosition = MidPointPositionSlider(leftTopFar, rightTopFar, c.transform.up);
            if (!GUI.changed)
                changedPosition = MidPointPositionSlider(leftBottomFar, rightBottomFar, -c.transform.up);
            if (GUI.changed)
                halfHeight = (changedPosition - farMid).magnitude;

            // Left and right handles
            GUI.changed = false;
            changedPosition = MidPointPositionSlider(rightBottomFar, rightTopFar, c.transform.right);
            if (!GUI.changed)
                changedPosition = MidPointPositionSlider(leftBottomFar, leftTopFar, -c.transform.right);
            if (GUI.changed)
                halfHeight = (changedPosition - farMid).magnitude / frustumAspect;

            // Update camera settings if changed
            if (halfHeight >= 0.0f)
            {
                Undo.RecordObject(c, "Adjust Camera");
                if (c.orthographic)
                {
                    c.orthographicSize = halfHeight;
                }
                else
                {
                    Vector3 pos = farMid + c.transform.up * halfHeight;
                    c.fieldOfView = Vector3.Angle(c.transform.forward, (pos - c.transform.position)) * 2f;
                }
                guiChanged = true;
            }

            GUI.changed = guiChanged;
            Handles.color = orgHandlesColor;
        }

        private static Vector3 MidPointPositionSlider(Vector3 position1, Vector3 position2, Vector3 direction)
        {
            Vector3 midPoint = Vector3.Lerp(position1, position2, 0.5f);
            return Handles.Slider(midPoint, direction, HandleUtility.GetHandleSize(midPoint) * 0.03f, Handles.DotHandleCap, 0f);
        }
    }
}
