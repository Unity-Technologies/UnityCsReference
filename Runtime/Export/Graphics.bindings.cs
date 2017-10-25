// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using uei = UnityEngine.Internal;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine
{
    internal enum EnabledOrientation
    {
        kAutorotateToPortrait           = 1,
        kAutorotateToPortraitUpsideDown = 2,
        kAutorotateToLandscapeLeft      = 4,
        kAutorotateToLandscapeRight     = 8,
    };

    public enum FullScreenMode
    {
        ExclusiveFullScreen = 0,
        FullScreenWindow = 1,
        MaximizedWindow = 2,
        Windowed = 3,
    }

    public sealed partial class SleepTimeout
    {
        public const int NeverSleep = -1;
        public const int SystemSetting = -2;
    }

    [NativeHeader("Runtime/Graphics/ScreenManager.h")]
    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    [StaticAccessor("GetScreenManager()", StaticAccessorType.Dot)]
    public sealed partial class Screen
    {
        extern public static int   width  {[NativeMethod(Name = "GetWidth",  IsThreadSafe = true)] get; }
        extern public static int   height {[NativeMethod(Name = "GetHeight", IsThreadSafe = true)] get; }
        extern public static float dpi    {[NativeName("GetDPI")] get; }

        extern public static ScreenOrientation orientation  {[NativeName("GetScreenOrientation")] get; [NativeName("RequestOrientation")] set; }
        [NativeProperty("ScreenTimeout")] extern public static int sleepTimeout { get; set; }

        [NativeName("GetIsOrientationEnabled")] extern private static bool IsOrientationEnabled(EnabledOrientation orient);
        [NativeName("SetIsOrientationEnabled")] extern private static void SetOrientationEnabled(EnabledOrientation orient, bool enabled);

        public static bool autorotateToPortrait
        {
            get { return IsOrientationEnabled(EnabledOrientation.kAutorotateToPortrait); }
            set { SetOrientationEnabled(EnabledOrientation.kAutorotateToPortrait, value); }
        }
        public static bool autorotateToPortraitUpsideDown
        {
            get { return IsOrientationEnabled(EnabledOrientation.kAutorotateToPortraitUpsideDown); }
            set { SetOrientationEnabled(EnabledOrientation.kAutorotateToPortraitUpsideDown, value); }
        }
        public static bool autorotateToLandscapeLeft
        {
            get { return IsOrientationEnabled(EnabledOrientation.kAutorotateToLandscapeLeft); }
            set { SetOrientationEnabled(EnabledOrientation.kAutorotateToLandscapeLeft, value); }
        }
        public static bool autorotateToLandscapeRight
        {
            get { return IsOrientationEnabled(EnabledOrientation.kAutorotateToLandscapeRight); }
            set { SetOrientationEnabled(EnabledOrientation.kAutorotateToLandscapeRight, value); }
        }

        extern public static Resolution currentResolution { get; }
        extern public static bool fullScreen {[NativeName("IsFullscreen")] get; [NativeName("RequestSetFullscreenFromScript")] set; }
        extern public static FullScreenMode fullScreenMode {[NativeName("GetFullscreenMode")] get; [NativeName("RequestSetFullscreenModeFromScript")] set; }

        extern public static Rect safeArea { get; }

        [NativeName("RequestResolution")]
        extern public static void SetResolution(int width, int height, FullScreenMode fullscreenMode, [uei.DefaultValue("0")] int preferredRefreshRate);

        public static void SetResolution(int width, int height, FullScreenMode fullscreenMode)
        {
            SetResolution(width, height, fullscreenMode, 0);
        }

        public static void SetResolution(int width, int height, bool fullscreen, [uei.DefaultValue("0")] int preferredRefreshRate)
        {
            SetResolution(width, height, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed, preferredRefreshRate);
        }

        public static void SetResolution(int width, int height, bool fullscreen)
        {
            SetResolution(width, height, fullscreen, 0);
        }

        extern public static Resolution[] resolutions {[FreeFunction("ScreenScripting::GetResolutions")] get; }
    }
}

namespace UnityEngine
{
    [NativeHeader("Runtime/Graphics/CopyTexture.h")]
    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    public partial class Graphics
    {
        [FreeFunction("CopyTexture")] extern private static void CopyTexture_Full(Texture src, Texture dst);
        [FreeFunction("CopyTexture")] extern private static void CopyTexture_Slice_AllMips(Texture src, int srcElement, Texture dst, int dstElement);
        [FreeFunction("CopyTexture")] extern private static void CopyTexture_Slice(Texture src, int srcElement, int srcMip, Texture dst, int dstElement, int dstMip);
        [FreeFunction("CopyTexture")] extern private static void CopyTexture_Region(Texture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, Texture dst, int dstElement, int dstMip, int dstX, int dstY);
        [FreeFunction("ConvertTexture")] extern private static bool ConvertTexture_Full(Texture src, Texture dst);
        [FreeFunction("ConvertTexture")] extern private static bool ConvertTexture_Slice(Texture src, int srcElement, Texture dst, int dstElement);

        [FreeFunction("GraphicsScripting::BlitMaterial")]
        extern private static void Internal_BlitMaterial(Texture source, RenderTexture dest, [NotNull] Material mat, int pass, bool setRT);

        [FreeFunction("GraphicsScripting::BlitMultitap")]
        extern private static void Internal_BlitMultiTap(Texture source, RenderTexture dest, [NotNull] Material mat, [NotNull] Vector2[] offsets);

        [FreeFunction("GraphicsScripting::Blit")]
        extern private static void Blit2(Texture source, RenderTexture dest);

        [FreeFunction("GraphicsScripting::Blit")]
        extern private static void Blit4(Texture source, RenderTexture dest, Vector2 scale, Vector2 offset);
    }
}

namespace UnityEngine
{
    public sealed partial class GL
    {
        public const int TRIANGLES      = 0x0004;
        public const int TRIANGLE_STRIP = 0x0005;
        public const int QUADS          = 0x0007;
        public const int LINES          = 0x0001;
        public const int LINE_STRIP     = 0x0002;
    }


    [NativeHeader("Runtime/GfxDevice/GfxDevice.h")]
    [StaticAccessor("GetGfxDevice()", StaticAccessorType.Dot)]
    public sealed partial class GL
    {
        [NativeName("ImmediateVertex")] extern public static void Vertex3(float x, float y, float z);
        public static void Vertex(Vector3 v) { Vertex3(v.x, v.y, v.z); }

        [NativeName("ImmediateTexCoordAll")] extern public static void TexCoord3(float x, float y, float z);
        public static void TexCoord(Vector3 v)          { TexCoord3(v.x, v.y, v.z); }
        public static void TexCoord2(float x, float y)  { TexCoord3(x, y, 0.0f); }

        [NativeName("ImmediateTexCoord")] extern public static void MultiTexCoord3(int unit, float x, float y, float z);
        public static void MultiTexCoord(int unit, Vector3 v)           { MultiTexCoord3(unit, v.x, v.y, v.z); }
        public static void MultiTexCoord2(int unit, float x, float y)   { MultiTexCoord3(unit, x, y, 0.0f); }

        [NativeName("ImmediateColor")] extern private static void ImmediateColor(float r, float g, float b, float a);
        public static void Color(Color c) { ImmediateColor(c.r, c.g, c.b, c.a); }

        extern public static bool wireframe     { get; set; }
        extern public static bool sRGBWrite     { get; set; }
        [NativeProperty("UserBackfaceMode")] extern public static bool invertCulling { get;  set; }

        extern public static void Flush();
        extern public static void RenderTargetBarrier();

        extern private static Matrix4x4 GetWorldViewMatrix();
        extern private static void SetViewMatrix(Matrix4x4 m);
        static public Matrix4x4 modelview { get { return GetWorldViewMatrix(); } set { SetViewMatrix(value); } }

        [NativeName("SetWorldMatrix")] extern public static void MultMatrix(Matrix4x4 m);

        [Obsolete("IssuePluginEvent(eventID) is deprecated. Use IssuePluginEvent(callback, eventID) instead.", false)]
        [NativeName("InsertCustomMarker")] extern public static void IssuePluginEvent(int eventID);

        [Obsolete("SetRevertBackfacing(revertBackFaces) is deprecated. Use invertCulling property instead.", false)]
        [NativeName("SetUserBackfaceMode")] extern public static void SetRevertBackfacing(bool revertBackFaces);
    }

    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    [NativeHeader("Runtime/Camera/Camera.h")]
    [NativeHeader("Runtime/Camera/CameraUtil.h")]
    public sealed partial class GL
    {
        [FreeFunction("GLPushMatrixScript")]            extern public static void PushMatrix();
        [FreeFunction("GLPopMatrixScript")]             extern public static void PopMatrix();
        [FreeFunction("GLLoadIdentityScript")]          extern public static void LoadIdentity();
        [FreeFunction("GLLoadOrthoScript")]             extern public static void LoadOrtho();
        [FreeFunction("GLLoadPixelMatrixScript")]       extern public static void LoadPixelMatrix();
        [FreeFunction("GLLoadProjectionMatrixScript")]  extern public static void LoadProjectionMatrix(Matrix4x4 mat);
        [FreeFunction("GLInvalidateState")]             extern public static void InvalidateState();
        [FreeFunction("GLGetGPUProjectionMatrix")]      extern public static Matrix4x4 GetGPUProjectionMatrix(Matrix4x4 proj, bool renderIntoTexture);

        [FreeFunction] extern private static void GLLoadPixelMatrixScript(float left, float right, float bottom, float top);
        public static void LoadPixelMatrix(float left, float right, float bottom, float top)
        {
            GLLoadPixelMatrixScript(left, right, bottom, top);
        }

        [FreeFunction] extern private static void GLIssuePluginEvent(IntPtr callback, int eventID);
        public static void IssuePluginEvent(IntPtr callback, int eventID)
        {
            if (callback == IntPtr.Zero)
                throw new ArgumentException("Null callback specified.", "callback");
            GLIssuePluginEvent(callback, eventID);
        }

        [FreeFunction("GLBegin", ThrowsException = true)] extern public static void Begin(int mode);
        [FreeFunction("GLEnd")]                           extern public static void End();

        [FreeFunction] extern private static void GLClear(bool clearDepth, bool clearColor, Color backgroundColor, float depth);
        static public void Clear(bool clearDepth, bool clearColor, Color backgroundColor, [uei.DefaultValue("1.0f")] float depth)
        {
            GLClear(clearDepth, clearColor, backgroundColor, depth);
        }

        static public void Clear(bool clearDepth, bool clearColor, Color backgroundColor)
        {
            GLClear(clearDepth, clearColor, backgroundColor, 1.0f);
        }

        [FreeFunction("SetGLViewport")] extern public static void Viewport(Rect pixelRect);
        [FreeFunction("ClearWithSkybox")] extern public static void ClearWithSkybox(bool clearDepth, Camera camera);
    }
}

namespace UnityEngine
{
    // Scales render textures to support dynamic resolution.
    [NativeHeader("Runtime/GfxDevice/ScalableBufferManager.h")]
    [StaticAccessor("ScalableBufferManager::GetInstance()", StaticAccessorType.Dot)]
    static public class ScalableBufferManager
    {
        extern static public float widthScaleFactor { get; }
        extern static public float heightScaleFactor { get; }

        static extern public void ResizeBuffers(float widthScale, float heightScale);
    }

    [NativeHeader("Runtime/GfxDevice/FrameTiming.h")]
    [StructLayout(LayoutKind.Sequential)]
    public struct FrameTiming
    {
        // CPU events

        [NativeName("m_CPUTimePresentCalled")]  public UInt64 cpuTimePresentCalled;
        [NativeName("m_CPUFrameTime")]          public double cpuFrameTime;

        // GPU events

        //This is the time the GPU finishes rendering the frame and interrupts the CPU
        [NativeName("m_CPUTimeFrameComplete")]  public UInt64 cpuTimeFrameComplete;
        [NativeName("m_GPUFrameTime")]          public double gpuFrameTime;

        //Linked data

        [NativeName("m_HeightScale")]           public float heightScale;
        [NativeName("m_WidthScale")]            public float widthScale;
        [NativeName("m_SyncInterval")]          public UInt32 syncInterval;
    }

    [StaticAccessor("GetFrameTimingManager()", StaticAccessorType.Dot)]
    static public class FrameTimingManager
    {
        static extern public void CaptureFrameTimings();
        static extern public UInt32 GetLatestTimings(UInt32 numFrames, FrameTiming[] timings);

        static extern public float GetVSyncsPerSecond();
        static extern public UInt64 GetGpuTimerFrequency();
        static extern public UInt64 GetCpuTimerFrequency();
    }
}

namespace UnityEngine
{
    [NativeHeader("Runtime/Shaders/ShaderPropertySheet.h")]
    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    [NativeHeader("Runtime/Shaders/ComputeShader.h")]
    public sealed partial class MaterialPropertyBlock
    {
        // TODO: set int is missing
        // TODO: get int/color/buffer is missing

        [NativeName("GetFloatFromScript")]   extern private float     GetFloatImpl(int name);
        [NativeName("GetVectorFromScript")]  extern private Vector4   GetVectorImpl(int name);
        [NativeName("GetColorFromScript")]   extern private Color     GetColorImpl(int name);
        [NativeName("GetMatrixFromScript")]  extern private Matrix4x4 GetMatrixImpl(int name);
        [NativeName("GetTextureFromScript")] extern private Texture   GetTextureImpl(int name);

        private object GetValueImpl(int name, Type t)
        {
            if (t == typeof(float))          return GetFloatImpl(name);
            else if (t == typeof(Vector4))   return GetVectorImpl(name);
            else if (t == typeof(Color))     return GetColorImpl(name);
            else if (t == typeof(Matrix4x4)) return GetMatrixImpl(name);
            else if (t == typeof(Texture))   return GetTextureImpl(name);
            else throw new ArgumentException("Unsupported type for value");
        }

        [NativeName("SetFloatFromScript")]   extern private void SetFloatImpl(int name, float value);
        [NativeName("SetVectorFromScript")]  extern private void SetVectorImpl(int name, Vector4 value);
        [NativeName("SetColorFromScript")]   extern private void SetColorImpl(int name, Color value);
        [NativeName("SetMatrixFromScript")]  extern private void SetMatrixImpl(int name, Matrix4x4 value);
        [NativeName("SetTextureFromScript")] extern private void SetTextureImpl(int name, [NotNull] Texture value);
        [NativeName("SetBufferFromScript")]  extern private void SetBufferImpl(int name, ComputeBuffer value);

        private void SetValueImpl(int name, object value, Type t)
        {
            if (t == typeof(float))              SetFloatImpl(name, (float)value);
            else if (t == typeof(Color))         SetColorImpl(name, (Color)value);
            else if (t == typeof(Vector4))       SetVectorImpl(name, (Vector4)value);
            else if (t == typeof(Matrix4x4))     SetMatrixImpl(name, (Matrix4x4)value);
            else if (t == typeof(Texture))       SetTextureImpl(name, (Texture)value);
            else if (t == typeof(ComputeBuffer)) SetBufferImpl(name, (ComputeBuffer)value);
            else throw new ArgumentException("Unsupported type for value");
        }

        [NativeName("SetFloatArrayFromScript")]  extern private void SetFloatArrayImpl(int name, float[] values, int count);
        [NativeName("SetVectorArrayFromScript")] extern private void SetVectorArrayImpl(int name, Vector4[] values, int count);
        [NativeName("SetMatrixArrayFromScript")] extern private void SetMatrixArrayImpl(int name, Matrix4x4[] values, int count);

        private void SetValueArrayImpl(int name, System.Array values, int count, Type t)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Length == 0) throw new ArgumentException("Zero-sized array is not allowed.");
            if (values.Length < count) throw new ArgumentException("array has less elements than passed count.");

            if (t == typeof(float))          SetFloatArrayImpl(name, (float[])values, count);
            else if (t == typeof(Vector4))   SetVectorArrayImpl(name, (Vector4[])values, count);
            else if (t == typeof(Matrix4x4)) SetMatrixArrayImpl(name, (Matrix4x4[])values, count);
            else throw new ArgumentException("Unsupported type for value");
        }

        [NativeName("GetFloatArrayFromScript")]  extern private float[]     GetFloatArrayImpl(int name);
        [NativeName("GetVectorArrayFromScript")] extern private Vector4[]   GetVectorArrayImpl(int name);
        [NativeName("GetMatrixArrayFromScript")] extern private Matrix4x4[] GetMatrixArrayImpl(int name);

        private System.Array GetValueArrayImpl(int name, Type t)
        {
            if (t == typeof(float))          return GetFloatArrayImpl(name);
            else if (t == typeof(Vector4))   return GetVectorArrayImpl(name);
            else if (t == typeof(Matrix4x4)) return GetMatrixArrayImpl(name);
            else throw new ArgumentException("Unsupported type for value");
        }

        [NativeName("GetFloatArrayCountFromScript")]  extern private int GetFloatArrayCountImpl(int name);
        [NativeName("GetVectorArrayCountFromScript")] extern private int GetVectorArrayCountImpl(int name);
        [NativeName("GetMatrixArrayCountFromScript")] extern private int GetMatrixArrayCountImpl(int name);

        private int GetValueArrayCountImpl(int name, Type t)
        {
            if (t == typeof(float))          return GetFloatArrayCountImpl(name);
            else if (t == typeof(Vector4))   return GetVectorArrayCountImpl(name);
            else if (t == typeof(Matrix4x4)) return GetMatrixArrayCountImpl(name);
            else throw new ArgumentException("Unsupported type for value");
        }

        [NativeName("ExtractFloatArrayFromScript")]  extern private void ExtractFloatArrayImpl(int name, [Out] float[] val);
        [NativeName("ExtractVectorArrayFromScript")] extern private void ExtractVectorArrayImpl(int name, [Out] Vector4[] val);
        [NativeName("ExtractMatrixArrayFromScript")] extern private void ExtractMatrixArrayImpl(int name, [Out] Matrix4x4[] val);

        private void ExtractValueArrayImpl(int name, System.Array values, Type t)
        {
            if (t == typeof(float))          ExtractFloatArrayImpl(name,  (float[])values);
            else if (t == typeof(Vector4))   ExtractVectorArrayImpl(name, (Vector4[])values);
            else if (t == typeof(Matrix4x4)) ExtractMatrixArrayImpl(name, (Matrix4x4[])values);
            else throw new ArgumentException("Unsupported type for value");
        }

    }

    public sealed partial class MaterialPropertyBlock
    {
        [NativeMethod(Name = "MaterialPropertyBlockScripting::Create", IsFreeFunction = true)]
        extern private static System.IntPtr CreateImpl();
        [NativeMethod(Name = "MaterialPropertyBlockScripting::Destroy", IsFreeFunction = true, IsThreadSafe = true)]
        extern private static void DestroyImpl(System.IntPtr mpb);

        extern public bool isEmpty {[NativeName("IsEmpty")] get; }

        extern private void Clear(bool keepMemory);
        public void Clear() { Clear(true); }
    }
}

namespace UnityEngine
{
    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    public sealed partial class GeometryUtility
    {
        [FreeFunction("GeometryUtilityScripting::ExtractPlanes")]
        extern private static void Internal_ExtractPlanes(Plane[] planes, Matrix4x4 worldToProjectionMatrix);
        [FreeFunction("GeometryUtilityScripting::TestPlanesAABB")]
        extern public static bool TestPlanesAABB(Plane[] planes, Bounds bounds);
        [FreeFunction("GeometryUtilityScripting::CalculateBounds")]
        extern private static Bounds Internal_CalculateBounds(Vector3[] positions, Matrix4x4 transform);
    }
}
