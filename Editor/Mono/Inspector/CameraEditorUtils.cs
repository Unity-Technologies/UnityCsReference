// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System;

namespace UnityEditor
{
    public static class CameraEditorUtils
    {
        static readonly Color k_ColorThemeCameraGizmo = new Color(233f / 255f, 233f / 255f, 233f / 255f, 128f / 255f);
        static readonly Color k_ColorThemeCameraSensorGizmo = new Color(180f / 255f, 180f / 255f, 180f / 255f, 128f / 255f);

        public static float GameViewAspectRatio => CameraEditor.GetGameViewAspectRatio();

        static int s_MovingHandleId = 0;
        static Vector3 s_InitialFarMid;
        static readonly int[] s_FrustumHandleIds =
        {
            "CameraEditor_FrustumHandleTop".GetHashCode(),
            "CameraEditor_FrustumHandleBottom".GetHashCode(),
            "CameraEditor_FrustumHandleLeft".GetHashCode(),
            "CameraEditor_FrustumHandleRight".GetHashCode()
        };

        public static Func<Camera> virtualCameraPreviewInstantiator
        {
            get => CameraPreviewUtils.s_VirtualCameraPreviewInstantiator;
            set => CameraPreviewUtils.s_VirtualCameraPreviewInstantiator = value;
        }

        public static void HandleFrustum(Camera c, int cameraEditorTargetIndex)
        {
            bool ContainsHandleId(int targetId)
            {
                foreach (int id in s_FrustumHandleIds)
                    if (id == targetId)
                        return true;
                return false;
            }

            if (c.projectionMatrixMode == Camera.ProjectionMatrixMode.Explicit)
                return;
            Color orgHandlesColor = Handles.color;
            Color slidersColor = k_ColorThemeCameraGizmo;
            slidersColor.a *= 2f;
            Handles.color = slidersColor;

            // get the corners of the far clip plane in world space
            var far = new Vector3[4];
            float frustumAspect;
            if (c.usePhysicalProperties)
            {
                if (!TryGetSensorGateFrustum(c, null, far, out frustumAspect))
                    return;
            }
            else
            {
                if (!TryGetFrustum(c, null, far, out frustumAspect))
                    return;
            }

            var leftBottomFar = far[0];
            var leftTopFar = far[1];
            var rightTopFar = far[2];
            var rightBottomFar = far[3];

            // manage our own gui changed state, so we can use it for individual slider changes
            bool guiChanged = GUI.changed;

            Vector3 farMid = Vector3.Lerp(leftBottomFar, rightTopFar, 0.5f);
            if (s_MovingHandleId != 0)
            {
                if (!ContainsHandleId(GUIUtility.hotControl - cameraEditorTargetIndex))
                    s_MovingHandleId = GUIUtility.hotControl;
                else
                    farMid = s_InitialFarMid;
            }
            else if (ContainsHandleId(GUIUtility.hotControl - cameraEditorTargetIndex))
            {
                s_MovingHandleId = GUIUtility.hotControl;
                s_InitialFarMid = farMid;
            }

            // FOV handles
            // Top and bottom handles
            float halfHeight = -1.0f;
            Vector3 changedPosition = MidPointPositionSlider(s_FrustumHandleIds[0] + cameraEditorTargetIndex, leftTopFar, rightTopFar, c.transform.up);
            if (!GUI.changed)
                changedPosition = MidPointPositionSlider(s_FrustumHandleIds[1] + cameraEditorTargetIndex, leftBottomFar, rightBottomFar, -c.transform.up);
            if (GUI.changed)
                halfHeight = (changedPosition - farMid).magnitude;

            // Left and right handles
            GUI.changed = false;
            changedPosition = MidPointPositionSlider(s_FrustumHandleIds[2] + cameraEditorTargetIndex, rightBottomFar, rightTopFar, c.transform.right);
            if (!GUI.changed)
                changedPosition = MidPointPositionSlider(s_FrustumHandleIds[3] + cameraEditorTargetIndex, leftBottomFar, leftTopFar, -c.transform.right);
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
                    Vector3 posUp = farMid + c.transform.up * halfHeight;
                    Vector3 posDown = farMid - c.transform.up * halfHeight;
                    Vector3 nearMid = farMid - c.transform.forward * c.farClipPlane;
                    c.fieldOfView = Vector3.Angle((posDown - nearMid), (posUp - nearMid));
                }
                guiChanged = true;
            }

            GUI.changed = guiChanged;
            Handles.color = orgHandlesColor;
        }

        public static void DrawFrustumGizmo(Camera camera)
        {
            var near = new Vector3[4];
            var far = new Vector3[4];
            float frustumAspect;
            if (camera.usePhysicalProperties)
            {
                if (TryGetSensorGateFrustum(camera, null, far, out frustumAspect))
                {
                    Color orgColor = Handles.color;
                    Handles.color = k_ColorThemeCameraSensorGizmo;
                    for (int i = 0; i < 4; ++i)
                    {
                        Handles.DrawLine(far[i], far[(i + 1) % 4]);
                    }
                    Handles.color = orgColor;
                }
                if (TryGetFrustum(camera, near, far, out frustumAspect))
                {
                    Color orgColor = Handles.color;
                    Handles.color = k_ColorThemeCameraGizmo;
                    for (int i = 0; i < 4; ++i)
                    {
                        Handles.DrawLine(far[i], far[(i + 1) % 4]);
                        Handles.DrawLine(near[i], far[i]);
                        Handles.DrawLine(near[i], near[(i + 1) % 4]);
                    }
                    Handles.color = orgColor;
                }
            }
            else if (TryGetFrustum(camera, near, far, out frustumAspect))
            {
                Color orgColor = Handles.color;
                Handles.color = k_ColorThemeCameraGizmo;
                for (int i = 0; i < 4; ++i)
                {
                    Handles.DrawLine(near[i], near[(i + 1) % 4]);
                    Handles.DrawLine(far[i], far[(i + 1) % 4]);
                    Handles.DrawLine(near[i], far[i]);
                }
                Handles.color = orgColor;
            }
        }

        // Returns near- and far-corners in this order: leftBottom, leftTop, rightTop, rightBottom
        // Assumes input arrays are of length 4 (if allocated)
        public static bool TryGetSensorGateFrustum(Camera camera, Vector3[] near, Vector3[] far, out float frustumAspect)
        {
            frustumAspect = GetFrustumAspectRatio(camera);
            if (frustumAspect < 0)
                return false;

            if (far != null)
            {
                Vector2 planeSize;
                planeSize.y = camera.farClipPlane * Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * 0.5f);
                planeSize.x = planeSize.y * camera.sensorSize.x / camera.sensorSize.y;

                Vector3 rightOffset = camera.gameObject.transform.right * planeSize.x;
                Vector3 upOffset = camera.gameObject.transform.up * planeSize.y;
                Vector3 localAim = camera.GetLocalSpaceAim() * camera.farClipPlane;
                localAim.z = -localAim.z;

                Vector3 planePosition = camera.cameraToWorldMatrix.MultiplyPoint(localAim);

                far[0] = planePosition - rightOffset - upOffset; // leftBottom
                far[1] = planePosition - rightOffset + upOffset; // leftTop
                far[2] = planePosition + rightOffset + upOffset; // rightTop
                far[3] = planePosition + rightOffset - upOffset; // rightBottom
            }

            if (near != null)
            {
                Vector2 planeSize;
                planeSize.y = camera.nearClipPlane * Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * 0.5f);
                planeSize.x = planeSize.y * camera.sensorSize.x / camera.sensorSize.y;

                Vector3 rightOffset = camera.gameObject.transform.right * planeSize.x;
                Vector3 upOffset = camera.gameObject.transform.up * planeSize.y;
                Vector3 localAim = camera.GetLocalSpaceAim() * camera.nearClipPlane;
                localAim.z = -localAim.z;

                Vector3 planePosition = camera.cameraToWorldMatrix.MultiplyPoint(localAim);

                near[0] = planePosition - rightOffset - upOffset; // leftBottom
                near[1] = planePosition - rightOffset + upOffset; // leftTop
                near[2] = planePosition + rightOffset + upOffset; // rightTop
                near[3] = planePosition + rightOffset - upOffset; // rightBottom
            }
            return true;
        }

        // Returns near- and far-corners in this order: leftBottom, leftTop, rightTop, rightBottom
        // Assumes input arrays are of length 4 (if allocated)
        public static bool TryGetFrustum(Camera camera, Vector3[] near, Vector3[] far, out float frustumAspect)
        {
            frustumAspect = GetFrustumAspectRatio(camera);
            if (frustumAspect < 0)
                return false;

            if (far != null)
            {
                if (camera.projectionMatrixMode == (int)Camera.ProjectionMatrixMode.Explicit)
                {
                    far[0] = new Vector3(0, 0, camera.farClipPlane); // leftBottomFar
                    far[1] = new Vector3(0, 1, camera.farClipPlane); // leftTopFar
                    far[2] = new Vector3(1, 1, camera.farClipPlane); // rightTopFar
                    far[3] = new Vector3(1, 0, camera.farClipPlane); // rightBottomFar
                    for (int i = 0; i < 4; ++i)
                        far[i] = camera.ViewportToWorldPoint(far[i]);
                }
                else
                {
                    CalculateFrustumPlaneAt(camera, camera.farClipPlane, far);
                }
            }

            if (near != null)
            {
                if (camera.projectionMatrixMode == (int)Camera.ProjectionMatrixMode.Explicit)
                {
                    near[0] = new Vector3(0, 0, camera.nearClipPlane); // leftBottomNear
                    near[1] = new Vector3(0, 1, camera.nearClipPlane); // leftTopNear
                    near[2] = new Vector3(1, 1, camera.nearClipPlane); // rightTopNear
                    near[3] = new Vector3(1, 0, camera.nearClipPlane); // rightBottomNear
                    for (int i = 0; i < 4; ++i)
                        near[i] = camera.ViewportToWorldPoint(near[i]);
                }
                else
                {
                    CalculateFrustumPlaneAt(camera, camera.nearClipPlane, near);
                }
            }
            return true;
        }

        private static void CalculateFrustumPlaneAt(Camera camera, float distance, Vector3[] plane)
        {
            Vector2 planeSize = camera.GetFrustumPlaneSizeAt(distance) * .5f;
            Vector3 rightOffset = camera.gameObject.transform.right * planeSize.x;
            Vector3 upOffset = camera.gameObject.transform.up * planeSize.y;
            Vector3 localAim = camera.GetLocalSpaceAim() * distance;
            localAim.z = -localAim.z;

            Vector3 planePosition = camera.cameraToWorldMatrix.MultiplyPoint(localAim);

            plane[0] = planePosition - rightOffset - upOffset; // leftBottom
            plane[1] = planePosition - rightOffset + upOffset; // leftTop
            plane[2] = planePosition + rightOffset + upOffset; // rightTop
            plane[3] = planePosition + rightOffset - upOffset; // rightBottom
        }

        public static bool IsViewportRectValidToRender(Rect normalizedViewPortRect)
        {
            if (normalizedViewPortRect.width <= 0f || normalizedViewPortRect.height <= 0f)
                return false;
            if (normalizedViewPortRect.x >= 1f || normalizedViewPortRect.xMax <= 0f)
                return false;
            if (normalizedViewPortRect.y >= 1f || normalizedViewPortRect.yMax <= 0f)
                return false;
            return true;
        }

        public static float GetFrustumAspectRatio(Camera camera)
        {
            var normalizedViewPortRect = camera.rect;
            if (normalizedViewPortRect.width <= 0f || normalizedViewPortRect.height <= 0f)
                return -1f;

            return camera.usePhysicalProperties ?
                camera.sensorSize.x / camera.sensorSize.y : GameViewAspectRatio * normalizedViewPortRect.width / normalizedViewPortRect.height;
        }

        public static Vector3 PerspectiveClipToWorld(Matrix4x4 clipToWorld, Vector3 viewPositionWS, Vector3 positionCS)
        {
            var tempCS = new Vector3(positionCS.x, positionCS.y, 0.95f);
            var result = clipToWorld.MultiplyPoint(tempCS);
            var r = result - viewPositionWS;
            return r.normalized * positionCS.z + viewPositionWS;
        }

        public static void GetFrustumPlaneAt(Matrix4x4 clipToWorld, Vector3 viewPosition, float distance, Vector3[] points)
        {
            points[0] = new Vector3(-1, -1, distance); // leftBottomFar
            points[1] = new Vector3(-1, 1, distance); // leftTopFar
            points[2] = new Vector3(1, 1, distance); // rightTopFar
            points[3] = new Vector3(1, -1, distance); // rightBottomFar
            for (var i = 0; i < 4; ++i)
                points[i] = PerspectiveClipToWorld(clipToWorld, viewPosition, points[i]);
        }

        static Vector3 MidPointPositionSlider(int controlID, Vector3 position1, Vector3 position2, Vector3 direction)
        {
            Vector3 midPoint = Vector3.Lerp(position1, position2, 0.5f);
            return Handles.Slider(controlID, midPoint, direction, HandleUtility.GetHandleSize(midPoint) * 0.03f, Handles.DotHandleCap, 0f);
        }
    }

    internal static class CameraPreviewUtils
    {
        static Camera s_PreviewCamera;
        static RenderTexture s_PreviewTexture;

        internal static Func<Camera> s_VirtualCameraPreviewInstantiator;

        internal struct PreviewSettings
        {
            public Vector2 size;
            public ulong overrideSceneCullingMask;
            public Scene scene;
            public bool useHDR;

            public float aspect => size.x / size.y;

            internal PreviewSettings(Vector2 previewSize)
            {
                size = previewSize;
                overrideSceneCullingMask = 0;
                scene = default;
                useHDR = false;
            }
        }

        class SavedStateForCameraPreview : IDisposable
        {
            Camera m_Target;

            public CameraType cameraType;
            public ulong overrideSceneCullingMask;
            public RenderTexture renderTarget;
            public Scene scene;

            public SavedStateForCameraPreview()
            {
                m_Target = null;
                renderTarget = null;
                overrideSceneCullingMask = 0;
                cameraType = CameraType.Game;
                scene = default;
            }

            public SavedStateForCameraPreview(Camera source)
            {
                m_Target = source;

                renderTarget = source.targetTexture;
                overrideSceneCullingMask = source.overrideSceneCullingMask;
                cameraType = source.cameraType;
                scene = source.scene;
            }

            public void Dispose()
            {
                m_Target.targetTexture = renderTarget;
                m_Target.overrideSceneCullingMask = overrideSceneCullingMask;
                m_Target.cameraType = cameraType;
                m_Target.scene = scene;
            }
        }

        static RenderTexture GetPreviewTexture(int width,  int height, bool hdr)
        {
            if (s_PreviewTexture != null
                && (s_PreviewTexture.width != width || s_PreviewTexture.height != height
                    || (GraphicsSettings.currentRenderPipeline == null && s_PreviewTexture.antiAliasing != Math.Max(1, QualitySettings.antiAliasing))))
            {
                s_PreviewTexture.Release();

                s_PreviewTexture.width = width;
                s_PreviewTexture.height = height;
                s_PreviewTexture.antiAliasing = Math.Max(1, QualitySettings.antiAliasing);
            }

            if (s_PreviewTexture == null)
            {
                GraphicsFormat format = (hdr) ? SystemInfo.GetGraphicsFormat(DefaultFormat.HDR) : SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
                s_PreviewTexture = new RenderTexture(width, height, 24, format);
            }

            if (GraphicsSettings.currentRenderPipeline == null)
            {
                // Built-in Render Pipeline, ensure that antiAliasing is set to 1 or more
                s_PreviewTexture.antiAliasing = Math.Max(1, QualitySettings.antiAliasing);
            }
            else
            {
                // SRPs
                s_PreviewTexture.enableRandomWrite = true;
            }

            return s_PreviewTexture;
        }

        static Camera previewCamera
        {
            get
            {
                if (s_PreviewCamera == null && CameraEditorUtils.virtualCameraPreviewInstantiator != null)
                    s_PreviewCamera = CameraEditorUtils.virtualCameraPreviewInstantiator();

                if (s_PreviewCamera == null)
                {
                    s_PreviewCamera = EditorUtility.CreateGameObjectWithHideFlags("Preview Camera",
                        HideFlags.HideAndDontSave,
                        typeof(Camera)).GetComponent<Camera>();

                    s_PreviewCamera.enabled = false;
                    s_PreviewCamera.cameraType = CameraType.Preview;
                }
                return s_PreviewCamera;
            }
        }

        internal static RenderTexture GetPreview(Camera camera, PreviewSettings settings)
        {
            if (RenderPipeline.SupportsRenderRequest(camera, new RenderPipeline.StandardRequest()))
                return RenderInternal(camera, settings);
            return RenderPreviewWithCameraCopy(camera, settings);
        }

        internal static RenderTexture GetPreview(IViewpoint virtualCameraSource, PreviewSettings settings)
        {
            var sourceCamera = virtualCameraSource.TargetObject as Camera;
            if (sourceCamera)
                return GetPreview(sourceCamera, settings);

            // Viewpoint represents a virtual camera.
            ViewpointUtility.ApplyTransformData(virtualCameraSource, previewCamera.gameObject.transform);
            ViewpointUtility.ApplyCameraLensData(virtualCameraSource as ICameraLensData, previewCamera);

            return RenderInternal(previewCamera, settings);
        }

        static RenderTexture RenderPreviewWithCameraCopy(Camera sourceCamera, PreviewSettings settings)
        {
            previewCamera.CopyFrom(sourceCamera);

            // Only for Legacy/Built-in Render Pipeline.
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                // Make sure to sync any Skybox component on the preview camera
                var dstSkybox = previewCamera.GetComponent<Skybox>();
                if (dstSkybox == null)
                    dstSkybox = previewCamera.gameObject.AddComponent<Skybox>();

                var srcSkybox = sourceCamera.GetComponent<Skybox>();
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

            Handles.EmitGUIGeometryForCamera(sourceCamera, previewCamera);

            return RenderInternal(previewCamera, settings);
        }

        static RenderTexture RenderInternal(Camera cameraToRender, PreviewSettings settings)
        {
            var rt = GetPreviewTexture((int)settings.size.x, (int)settings.size.y, settings.useHDR);

            // When sensor size is reduced, the previous frame is still visible behind so we need to clear the texture before rendering.
            if (cameraToRender.usePhysicalProperties)
            {
                RenderTexture oldRt = RenderTexture.active;
                RenderTexture.active = rt;
                GL.Clear(false, true, Color.clear);
                RenderTexture.active = oldRt;
            }

            // Honor async shader compilation editor settings for this preview
            bool oldShaderAsyncState = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = EditorSettings.asyncShaderCompilation;

            using (new SavedStateForCameraPreview(cameraToRender))
            {
                // make sure the preview camera is rendering the same stage as the SceneView is
                if (settings.overrideSceneCullingMask != 0)
                    cameraToRender.overrideSceneCullingMask = settings.overrideSceneCullingMask;
                else
                    cameraToRender.scene = settings.scene;

                RenderPipeline.StandardRequest request = new RenderPipeline.StandardRequest()
                {
                    destination = rt,
                };

                // Use RenderRequest API when the active SRP supports it for implementation-agnostic rendering.
                if (RenderPipeline.SupportsRenderRequest(cameraToRender, request))
                    cameraToRender.SubmitRenderRequest<RenderPipeline.StandardRequest>(request);
                else
                {
                    // Built-in RP and SRPs that don't support the RenderRequest API will
                    // render the old way.
                    previewCamera.targetTexture = rt;
                    previewCamera.Render();
                }
            }

            ShaderUtil.allowAsyncCompilation = oldShaderAsyncState;
            return rt;
        }
    }
}
