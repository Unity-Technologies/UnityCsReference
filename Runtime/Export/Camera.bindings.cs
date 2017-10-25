// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

using OpaqueSortMode = UnityEngine.Rendering.OpaqueSortMode;
using CameraEvent = UnityEngine.Rendering.CameraEvent;
using CommandBuffer = UnityEngine.Rendering.CommandBuffer;

namespace UnityEngine
{
    [NativeHeader("Runtime/Camera/Camera.h")]
    [NativeHeader("Runtime/Camera/RenderManager.h")]
    [NativeHeader("Runtime/GfxDevice/GfxDeviceTypes.h")]
    [NativeHeader("Runtime/Graphics/RenderTexture.h")]
    [NativeHeader("Runtime/Graphics/CommandBuffer/RenderingCommandBuffer.h")]
    [NativeHeader("Runtime/Misc/GameObjectUtility.h")]
    [NativeHeader("Runtime/Shaders/Shader.h")]
    [UsedByNativeCode]
    [RequireComponent(typeof(Transform))]
    public partial class Camera
    {
        [NativeProperty("Near")] extern public float nearClipPlane { get; set; }
        [NativeProperty("Far")]  extern public float farClipPlane  { get; set; }
        [NativeProperty("Fov")]  extern public float fieldOfView   { get; set; }

        extern public RenderingPath renderingPath { get; set; }
        extern public RenderingPath actualRenderingPath {[NativeName("CalculateRenderingPath")] get;  }

        extern public bool allowHDR { get; set; }
        extern public bool allowMSAA { get; set; }
        extern public bool allowDynamicResolution { get; set; }
        [NativeProperty("ForceIntoRT")] extern public bool forceIntoRenderTexture { get; set; }

        extern public float orthographicSize { get; set; }
        extern public bool  orthographic { get; set; }

        extern public OpaqueSortMode opaqueSortMode { get; set; }
        extern public TransparencySortMode transparencySortMode { get; set; }
        extern public Vector3 transparencySortAxis { get; set; }
        extern public void ResetTransparencySortSettings();

        extern public float depth { get; set; }
        extern public float aspect { get; set; }

        extern public Vector3 velocity { get; }

        extern public int cullingMask { get; set; }
        extern public int eventMask { get; set; }
        extern public bool layerCullSpherical { get; set; }
        extern public CameraType cameraType { get; set; }

        extern public bool useOcclusionCulling { get; set; }
        extern public Matrix4x4 cullingMatrix { get; set; }
        extern public void ResetCullingMatrix();

        extern public Color backgroundColor { get; set; }
        extern public CameraClearFlags clearFlags { get; set; }

        extern public void SetReplacementShader(Shader shader, string replacementTag);
        extern public void ResetReplacementShader();


        [NativeProperty("NormalizedViewportRect")] extern public Rect rect      { get; set; }
        [NativeProperty("ScreenViewportRect")]     extern public Rect pixelRect { get; set; }

        extern public RenderTexture targetTexture { get; set; }
        extern public RenderTexture activeTexture {[NativeName("GetCurrentTargetTexture")] get; }

        extern public Matrix4x4 cameraToWorldMatrix { get; }
        extern public Matrix4x4 worldToCameraMatrix { get; set; }
        extern public Matrix4x4 projectionMatrix    { get; set; }
        extern public Matrix4x4 nonJitteredProjectionMatrix { get; set; }
        [NativeProperty("UseJitteredProjectionMatrixForTransparent")] extern public bool useJitteredProjectionMatrixForTransparentRendering { get; set; }
        extern public Matrix4x4 previousViewProjectionMatrix { get; }
        extern public void ResetWorldToCameraMatrix();
        extern public void ResetProjectionMatrix();

        extern public Vector3 WorldToScreenPoint(Vector3 position);
        extern public Vector3 WorldToViewportPoint(Vector3 position);
        extern public Vector3 ViewportToWorldPoint(Vector3 position);
        extern public Vector3 ScreenToWorldPoint(Vector3 position);
        extern public Vector3 ScreenToViewportPoint(Vector3 position);
        extern public Vector3 ViewportToScreenPoint(Vector3 position);

        extern private Ray ViewportPointToRay(Vector2 pos);
        public Ray ViewportPointToRay(Vector3 pos) { return ViewportPointToRay((Vector2)pos); }

        extern private Ray ScreenPointToRay(Vector2 pos);
        public Ray ScreenPointToRay(Vector3 pos) { return ScreenPointToRay((Vector2)pos); }


        extern public static Camera main {[FreeFunction("FindMainCamera")] get; }
        extern public static Camera current {[FreeFunction("GetCurrentCameraPtr")] get; }


        public enum StereoscopicEye { Left, Right };
        public enum MonoOrStereoscopicEye { Left, Right, Mono };

        extern public bool  stereoEnabled     { get; }
        extern public float stereoSeparation  { get; set; }
        extern public float stereoConvergence { get; set; }
        extern public bool  areVRStereoViewMatricesWithinSingleCullTolerance {[NativeName("AreVRStereoViewMatricesWithinSingleCullTolerance")] get; }
        extern public StereoTargetEyeMask stereoTargetEye { get; set; }

        extern public Matrix4x4 GetStereoNonJitteredProjectionMatrix(StereoscopicEye eye);
        extern public Matrix4x4 GetStereoViewMatrix(StereoscopicEye eye);
        extern public void CopyStereoDeviceProjectionMatrixToNonJittered(StereoscopicEye eye);

        extern public Matrix4x4 GetStereoProjectionMatrix(StereoscopicEye eye);
        extern public void SetStereoProjectionMatrix(StereoscopicEye eye, Matrix4x4 matrix);
        extern public void ResetStereoProjectionMatrices();

        extern public void SetStereoViewMatrix(StereoscopicEye eye, Matrix4x4 matrix);
        extern public void ResetStereoViewMatrices();


        extern public int  commandBufferCount { get; }
        extern public void RemoveCommandBuffers(CameraEvent evt);
        extern public void RemoveAllCommandBuffers();


        // in old bindings these functions code like this:
        //   self->AddCommandBuffer(evt, &*buffer);
        // this dereference generated null-ref exception (as opposed to "normal" argument-null exception)
        // we want to preserve this behaviour

        // extern public void AddCommandBuffer(CameraEvent evt, [NotNull] CommandBuffer buffer);
        // extern public void RemoveCommandBuffer(CameraEvent evt, [NotNull] CommandBuffer buffer);
        [NativeName("AddCommandBuffer")]    extern private void AddCommandBufferImpl(CameraEvent evt, [NotNull] CommandBuffer buffer);
        [NativeName("RemoveCommandBuffer")] extern private void RemoveCommandBufferImpl(CameraEvent evt, [NotNull] CommandBuffer buffer);

        public void AddCommandBuffer(CameraEvent evt, CommandBuffer buffer)
        {
            if (buffer == null) throw new NullReferenceException("buffer is null");
            AddCommandBufferImpl(evt, buffer);
        }

        public void RemoveCommandBuffer(CameraEvent evt, CommandBuffer buffer)
        {
            if (buffer == null) throw new NullReferenceException("buffer is null");
            RemoveCommandBufferImpl(evt, buffer);
        }
    }

    public partial class Camera
    {
        // called before a camera culls the scene.
        // void OnPreCull();
        // called before a camera starts rendering the scene.
        // void OnPreRender();
        // called after a camera has finished rendering the scene.
        // void OnPostRender();
        // called after all rendering is complete to render image
        // void OnRenderImage(RenderTexture source, RenderTexture destination);
        // called after camera has rendered the scene.
        // void OnRenderObject();
        // called once for each camera if the object is visible.
        // void OnWillRenderObject();

        public delegate void CameraCallback(Camera cam);

        public static CameraCallback onPreCull;
        public static CameraCallback onPreRender;
        public static CameraCallback onPostRender;

        [RequiredByNativeCode]
        private static void FireOnPreCull(Camera cam)
        {
            if (onPreCull != null)
                onPreCull(cam);
        }

        [RequiredByNativeCode]
        private static void FireOnPreRender(Camera cam)
        {
            if (onPreRender != null)
                onPreRender(cam);
        }

        [RequiredByNativeCode]
        private static void FireOnPostRender(Camera cam)
        {
            if (onPostRender != null)
                onPostRender(cam);
        }
    }
}
