// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using uei = UnityEngine.Internal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine
{
    internal enum EnabledOrientation
    {
        kAutorotateToPortrait           = 1,
        kAutorotateToPortraitUpsideDown = 2,
        kAutorotateToLandscapeLeft      = 4,
        kAutorotateToLandscapeRight     = 8,
    }

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
    internal sealed class EditorScreen
    {
        extern public static int   width  {[NativeMethod(Name = "GetWidth",  IsThreadSafe = true)] get; }
        extern public static int   height {[NativeMethod(Name = "GetHeight", IsThreadSafe = true)] get; }
        extern public static float dpi    {[NativeName("GetDPI")] get; }

        extern private static void RequestOrientation(ScreenOrientation orient);
        extern private static ScreenOrientation GetScreenOrientation();

        public static ScreenOrientation orientation
        {
            get { return GetScreenOrientation(); }
            set
            {
            #pragma warning disable 618 // UnityEngine.ScreenOrientation.Unknown is obsolete
                if (value == ScreenOrientation.Unknown)
            #pragma warning restore 649
                {
                    Debug.Log("ScreenOrientation.Unknown is deprecated. Please use ScreenOrientation.AutoRotation");
                    value = ScreenOrientation.AutoRotation;
                }
                RequestOrientation(value);
            }
        }
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
        extern public static Rect[] cutouts {[FreeFunction("ScreenScripting::GetCutouts")] get; }

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

        extern public static float brightness { get; set; }
    }
}

namespace UnityEngine
{
    public sealed partial class Screen
    {
        public static int width => ShimManager.screenShim.width;
        public static int height => ShimManager.screenShim.height;
        public static float dpi => ShimManager.screenShim.dpi;
        public static Resolution currentResolution => ShimManager.screenShim.currentResolution;
        public static Resolution[] resolutions => ShimManager.screenShim.resolutions;

        public static void SetResolution(int width, int height, FullScreenMode fullscreenMode, [uei.DefaultValue("0")] int preferredRefreshRate)
        {
            ShimManager.screenShim.SetResolution(width, height, fullscreenMode, preferredRefreshRate);
        }

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

        public static bool fullScreen
        {
            get { return ShimManager.screenShim.fullScreen; }
            set { ShimManager.screenShim.fullScreen = value; }
        }

        public static FullScreenMode fullScreenMode
        {
            get { return ShimManager.screenShim.fullScreenMode; }
            set { ShimManager.screenShim.fullScreenMode = value; }
        }

        public static Rect safeArea => ShimManager.screenShim.safeArea;

        public static Rect[] cutouts => ShimManager.screenShim.cutouts;

        public static bool autorotateToPortrait
        {
            get { return ShimManager.screenShim.autorotateToPortrait; }
            set { ShimManager.screenShim.autorotateToPortrait = value; }
        }

        public static bool autorotateToPortraitUpsideDown
        {
            get { return ShimManager.screenShim.autorotateToPortraitUpsideDown; }
            set { ShimManager.screenShim.autorotateToPortraitUpsideDown = value; }
        }

        public static bool autorotateToLandscapeLeft
        {
            get { return ShimManager.screenShim.autorotateToLandscapeLeft; }
            set { ShimManager.screenShim.autorotateToLandscapeLeft = value; }
        }

        public static bool autorotateToLandscapeRight
        {
            get { return ShimManager.screenShim.autorotateToLandscapeRight; }
            set { ShimManager.screenShim.autorotateToLandscapeRight = value; }
        }

        public static ScreenOrientation orientation
        {
            get { return ShimManager.screenShim.orientation; }
            set { ShimManager.screenShim.orientation = value; }
        }

        public static int sleepTimeout
        {
            get { return ShimManager.screenShim.sleepTimeout; }
            set { ShimManager.screenShim.sleepTimeout = value; }
        }

        public static float brightness
        {
            get { return ShimManager.screenShim.brightness; }
            set { ShimManager.screenShim.brightness = value; }
        }
    }
}

namespace UnityEngine
{
    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    public partial struct RenderBuffer
    {
        [FreeFunction(Name = "RenderBufferScripting::SetLoadAction", HasExplicitThis = true)]
        extern internal void SetLoadAction(RenderBufferLoadAction action);
        [FreeFunction(Name = "RenderBufferScripting::SetStoreAction", HasExplicitThis = true)]
        extern internal void SetStoreAction(RenderBufferStoreAction action);

        [FreeFunction(Name = "RenderBufferScripting::GetLoadAction", HasExplicitThis = true)]
        extern internal RenderBufferLoadAction GetLoadAction();
        [FreeFunction(Name = "RenderBufferScripting::GetStoreAction", HasExplicitThis = true)]
        extern internal RenderBufferStoreAction GetStoreAction();

        [FreeFunction(Name = "RenderBufferScripting::GetNativeRenderBufferPtr", HasExplicitThis = true)]
        extern public IntPtr GetNativeRenderBufferPtr();
    }
}

namespace UnityEngineInternal
{
    public enum MemorylessMode
    {
        Unused,
        Forced,
        Automatic,
    }
    [NativeHeader("Runtime/Misc/PlayerSettings.h")]
    public class MemorylessManager
    {
        public static MemorylessMode depthMemorylessMode
        {
            get { return GetFramebufferDepthMemorylessMode(); }
            set { SetFramebufferDepthMemorylessMode(value); }
        }
        [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
        [NativeMethod(Name = "GetFramebufferDepthMemorylessMode")]
        extern internal static MemorylessMode GetFramebufferDepthMemorylessMode();
        [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
        [NativeMethod(Name = "SetFramebufferDepthMemorylessMode")]
        extern internal static void SetFramebufferDepthMemorylessMode(MemorylessMode mode);
    }
}

namespace UnityEngine
{
    [NativeType("Runtime/GfxDevice/GfxDeviceTypes.h")]
    public enum ComputeBufferMode
    {
        Immutable = 0,
        Dynamic,
        Circular,
        StreamOut,
        SubUpdates,
    }
}

namespace UnityEngine
{
    [NativeHeader("Runtime/Camera/LightProbeProxyVolume.h")]
    [NativeHeader("Runtime/Graphics/ColorGamut.h")]
    [NativeHeader("Runtime/Graphics/CopyTexture.h")]
    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    [NativeHeader("Runtime/Shaders/ComputeShader.h")]
    [NativeHeader("Runtime/Misc/PlayerSettings.h")]
    public partial class Graphics
    {
        [FreeFunction("GraphicsScripting::GetMaxDrawMeshInstanceCount")] extern private static int Internal_GetMaxDrawMeshInstanceCount();
        internal static readonly int kMaxDrawMeshInstanceCount = Internal_GetMaxDrawMeshInstanceCount();

        [FreeFunction] extern private static ColorGamut GetActiveColorGamut();
        public static ColorGamut activeColorGamut { get { return GetActiveColorGamut(); } }

        [StaticAccessor("GetGfxDevice()", StaticAccessorType.Dot)] extern public static UnityEngine.Rendering.GraphicsTier activeTier { get; set; }

        [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
        [NativeMethod(Name = "GetPreserveFramebufferAlpha")]
        extern internal static bool GetPreserveFramebufferAlpha();
        public static bool preserveFramebufferAlpha { get { return GetPreserveFramebufferAlpha(); } }

        [FreeFunction("GraphicsScripting::GetActiveColorBuffer")] extern private static RenderBuffer GetActiveColorBuffer();
        [FreeFunction("GraphicsScripting::GetActiveDepthBuffer")] extern private static RenderBuffer GetActiveDepthBuffer();

        [FreeFunction("GraphicsScripting::SetNullRT")] extern private static void Internal_SetNullRT();
        [NativeMethod(Name = "GraphicsScripting::SetRTSimple", IsFreeFunction = true, ThrowsException = true)]
        extern private static void Internal_SetRTSimple(RenderBuffer color, RenderBuffer depth, int mip, CubemapFace face, int depthSlice);
        [NativeMethod(Name = "GraphicsScripting::SetMRTSimple", IsFreeFunction = true, ThrowsException = true)]
        extern private static void Internal_SetMRTSimple([NotNull] RenderBuffer[] color, RenderBuffer depth, int mip, CubemapFace face, int depthSlice);
        [NativeMethod(Name = "GraphicsScripting::SetMRTFull", IsFreeFunction = true, ThrowsException = true)]
        extern private static void Internal_SetMRTFullSetup(
            [NotNull] RenderBuffer[] color, RenderBuffer depth, int mip, CubemapFace face, int depthSlice,
            [NotNull] RenderBufferLoadAction[] colorLA, [NotNull] RenderBufferStoreAction[] colorSA,
            RenderBufferLoadAction depthLA, RenderBufferStoreAction depthSA
        );

        [NativeMethod(Name = "GraphicsScripting::SetRandomWriteTargetRT", IsFreeFunction = true, ThrowsException = true)]
        extern private static void Internal_SetRandomWriteTargetRT(int index, RenderTexture uav);
        [FreeFunction("GraphicsScripting::SetRandomWriteTargetBuffer")]
        extern private static void Internal_SetRandomWriteTargetBuffer(int index, ComputeBuffer uav, bool preserveCounterValue);
        [FreeFunction("GraphicsScripting::SetRandomWriteTargetBuffer")]
        extern private static void Internal_SetRandomWriteTargetGraphicsBuffer(int index, GraphicsBuffer uav, bool preserveCounterValue);

        [StaticAccessor("GetGfxDevice()", StaticAccessorType.Dot)] extern public static void ClearRandomWriteTargets();

        [FreeFunction("CopyTexture")] extern private static void CopyTexture_Full(Texture src, Texture dst);
        [FreeFunction("CopyTexture")] extern private static void CopyTexture_Slice_AllMips(Texture src, int srcElement, Texture dst, int dstElement);
        [FreeFunction("CopyTexture")] extern private static void CopyTexture_Slice(Texture src, int srcElement, int srcMip, Texture dst, int dstElement, int dstMip);
        [FreeFunction("CopyTexture")] extern private static void CopyTexture_Region(Texture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, Texture dst, int dstElement, int dstMip, int dstX, int dstY);
        [FreeFunction("ConvertTexture")] extern private static bool ConvertTexture_Full(Texture src, Texture dst);
        [FreeFunction("ConvertTexture")] extern private static bool ConvertTexture_Slice(Texture src, int srcElement, Texture dst, int dstElement);

        [FreeFunction("GraphicsScripting::DrawMeshNow")] extern private static void Internal_DrawMeshNow1(Mesh mesh, int subsetIndex, Vector3 position, Quaternion rotation);
        [FreeFunction("GraphicsScripting::DrawMeshNow")] extern private static void Internal_DrawMeshNow2(Mesh mesh, int subsetIndex, Matrix4x4 matrix);

        [FreeFunction("GraphicsScripting::DrawTexture")][VisibleToOtherModules("UnityEngine.IMGUIModule")]
        extern internal static void Internal_DrawTexture(ref Internal_DrawTextureArguments args);

        [FreeFunction("GraphicsScripting::DrawMesh")]
        extern private static void Internal_DrawMesh(Mesh mesh, int submeshIndex, Matrix4x4 matrix, Material material, int layer, Camera camera, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, Transform probeAnchor, LightProbeUsage lightProbeUsage, LightProbeProxyVolume lightProbeProxyVolume);

        [FreeFunction("GraphicsScripting::DrawMeshInstanced")]
        extern private static void Internal_DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, Matrix4x4[] matrices, int count, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera, LightProbeUsage lightProbeUsage, LightProbeProxyVolume lightProbeProxyVolume);

        [FreeFunction("GraphicsScripting::DrawMeshInstancedProcedural")]
        extern private static void Internal_DrawMeshInstancedProcedural(Mesh mesh, int submeshIndex, Material material, Bounds bounds, int count, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera, LightProbeUsage lightProbeUsage, LightProbeProxyVolume lightProbeProxyVolume);

        [FreeFunction("GraphicsScripting::DrawMeshInstancedIndirect")]
        extern private static void Internal_DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, Bounds bounds, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera, LightProbeUsage lightProbeUsage, LightProbeProxyVolume lightProbeProxyVolume);
        [FreeFunction("GraphicsScripting::DrawMeshInstancedIndirect")]
        extern private static void Internal_DrawMeshInstancedIndirectGraphicsBuffer(Mesh mesh, int submeshIndex, Material material, Bounds bounds, GraphicsBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera, LightProbeUsage lightProbeUsage, LightProbeProxyVolume lightProbeProxyVolume);

        [FreeFunction("GraphicsScripting::DrawProceduralNow")]
        extern private static void Internal_DrawProceduralNow(MeshTopology topology, int vertexCount, int instanceCount);

        [FreeFunction("GraphicsScripting::DrawProceduralIndexedNow")]
        extern private static void Internal_DrawProceduralIndexedNow(MeshTopology topology, GraphicsBuffer indexBuffer, int indexCount, int instanceCount);

        [FreeFunction("GraphicsScripting::DrawProceduralIndirectNow")]
        extern private static void Internal_DrawProceduralIndirectNow(MeshTopology topology, ComputeBuffer bufferWithArgs, int argsOffset);

        [FreeFunction("GraphicsScripting::DrawProceduralIndexedIndirectNow")]
        extern private static void Internal_DrawProceduralIndexedIndirectNow(MeshTopology topology, GraphicsBuffer indexBuffer, ComputeBuffer bufferWithArgs, int argsOffset);

        [FreeFunction("GraphicsScripting::DrawProceduralIndirectNow")]
        extern private static void Internal_DrawProceduralIndirectNowGraphicsBuffer(MeshTopology topology, GraphicsBuffer bufferWithArgs, int argsOffset);

        [FreeFunction("GraphicsScripting::DrawProceduralIndexedIndirectNow")]
        extern private static void Internal_DrawProceduralIndexedIndirectNowGraphicsBuffer(MeshTopology topology, GraphicsBuffer indexBuffer, GraphicsBuffer bufferWithArgs, int argsOffset);

        [FreeFunction("GraphicsScripting::DrawProcedural")]
        extern private static void Internal_DrawProcedural(Material material, Bounds bounds, MeshTopology topology, int vertexCount, int instanceCount, Camera camera, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer);

        [FreeFunction("GraphicsScripting::DrawProceduralIndexed")]
        extern private static void Internal_DrawProceduralIndexed(Material material, Bounds bounds, MeshTopology topology, GraphicsBuffer indexBuffer, int indexCount, int instanceCount, Camera camera, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer);

        [FreeFunction("GraphicsScripting::DrawProceduralIndirect")]
        extern private static void Internal_DrawProceduralIndirect(Material material, Bounds bounds, MeshTopology topology, ComputeBuffer bufferWithArgs, int argsOffset, Camera camera, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer);

        [FreeFunction("GraphicsScripting::DrawProceduralIndexedIndirect")]
        extern private static void Internal_DrawProceduralIndexedIndirect(Material material, Bounds bounds, MeshTopology topology, GraphicsBuffer indexBuffer, ComputeBuffer bufferWithArgs, int argsOffset, Camera camera, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, int layer);

        [FreeFunction("GraphicsScripting::BlitMaterial")]
        extern private static void Internal_BlitMaterial5(Texture source, RenderTexture dest, [NotNull] Material mat, int pass, bool setRT);

        [FreeFunction("GraphicsScripting::BlitMaterial")]
        extern private static void Internal_BlitMaterial6(Texture source, RenderTexture dest, [NotNull] Material mat, int pass, bool setRT, int destDepthSlice);

        [FreeFunction("GraphicsScripting::BlitMultitap")]
        extern private static void Internal_BlitMultiTap4(Texture source, RenderTexture dest, [NotNull] Material mat, [NotNull] Vector2[] offsets);

        [FreeFunction("GraphicsScripting::BlitMultitap")]
        extern private static void Internal_BlitMultiTap5(Texture source, RenderTexture dest, [NotNull] Material mat, [NotNull] Vector2[] offsets, int destDepthSlice);

        [FreeFunction("GraphicsScripting::Blit")]
        extern private static void Blit2(Texture source, RenderTexture dest);

        [FreeFunction("GraphicsScripting::Blit")]
        extern private static void Blit3(Texture source, RenderTexture dest, int sourceDepthSlice, int destDepthSlice);

        [FreeFunction("GraphicsScripting::Blit")]
        extern private static void Blit4(Texture source, RenderTexture dest, Vector2 scale, Vector2 offset);

        [FreeFunction("GraphicsScripting::Blit")]
        extern private static void Blit5(Texture source, RenderTexture dest, Vector2 scale, Vector2 offset, int sourceDepthSlice, int destDepthSlice);

        [NativeMethod(Name = "GraphicsScripting::CreateGPUFence", IsFreeFunction = true, ThrowsException = true)]
        extern private static IntPtr CreateGPUFenceImpl(GraphicsFenceType fenceType, SynchronisationStageFlags stage);

        [NativeMethod(Name = "GraphicsScripting::WaitOnGPUFence", IsFreeFunction = true, ThrowsException = true)]
        extern private static void WaitOnGPUFenceImpl(IntPtr fencePtr, SynchronisationStageFlags stage);

        [NativeMethod(Name = "GraphicsScripting::ExecuteCommandBuffer", IsFreeFunction = true, ThrowsException = true)]
        extern public static void ExecuteCommandBuffer([NotNull] CommandBuffer buffer);

        [NativeMethod(Name = "GraphicsScripting::ExecuteCommandBufferAsync", IsFreeFunction = true, ThrowsException = true)]
        extern public  static void ExecuteCommandBufferAsync([NotNull] CommandBuffer buffer, ComputeQueueType queueType);
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

    [StaticAccessor("GetUncheckedRealGfxDevice().GetFrameTimingManager()", StaticAccessorType.Dot)]
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
    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    [StaticAccessor("GeometryUtilityScripting", StaticAccessorType.DoubleColon)]
    public sealed partial class GeometryUtility
    {
        extern public static bool TestPlanesAABB(Plane[] planes, Bounds bounds);

        [NativeName("ExtractPlanes")]   extern private static void Internal_ExtractPlanes([Out] Plane[] planes, Matrix4x4 worldToProjectionMatrix);
        [NativeName("CalculateBounds")] extern private static Bounds Internal_CalculateBounds(Vector3[] positions, Matrix4x4 transform);
    }
}

namespace UnityEngine
{
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Graphics/LightmapData.h")]
    public sealed partial class LightmapData
    {
        internal Texture2D m_Light;
        internal Texture2D m_Dir;
        internal Texture2D m_ShadowMask;

        [System.Obsolete("Use lightmapColor property (UnityUpgradable) -> lightmapColor", false)]
        public Texture2D lightmapLight { get { return m_Light; }        set { m_Light = value; } }

        public Texture2D lightmapColor { get { return m_Light; }        set { m_Light = value; } }
        public Texture2D lightmapDir   { get { return m_Dir; }          set { m_Dir = value; } }
        public Texture2D shadowMask    { get { return m_ShadowMask; }   set { m_ShadowMask = value; } }
    }

    // Stores lightmaps of the scene.
    [NativeHeader("Runtime/Graphics/LightmapSettings.h")]
    [StaticAccessor("GetLightmapSettings()")]
    public sealed partial class LightmapSettings : Object
    {
        private LightmapSettings() {}

        // Lightmap array.
        public extern static LightmapData[] lightmaps {[FreeFunction] get; [FreeFunction(ThrowsException = true)] set; }

        public extern static LightmapsMode lightmapsMode { get; [FreeFunction(ThrowsException = true)] set; }

        // Holds all data needed by the light probes.
        public extern static LightProbes lightProbes { get; set; }

        [NativeName("ResetAndAwakeFromLoad")]
        internal static extern void Reset();
    }
}

namespace UnityEngine
{
    // Stores light probes for the scene.
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Export/Graphics/Graphics.bindings.h")]
    public sealed partial class LightProbes : Object
    {
        private LightProbes() {}

        [FreeFunction]
        public static extern void Tetrahedralize();

        [FreeFunction]
        public static extern void TetrahedralizeAsync();

        [FreeFunction]
        public extern static void GetInterpolatedProbe(Vector3 position, Renderer renderer, out UnityEngine.Rendering.SphericalHarmonicsL2 probe);

        [FreeFunction]
        internal static extern bool AreLightProbesAllowed(Renderer renderer);

        public static void CalculateInterpolatedLightAndOcclusionProbes(Vector3[] positions, SphericalHarmonicsL2[] lightProbes, Vector4[] occlusionProbes)
        {
            if (positions == null)
                throw new ArgumentNullException("positions");
            else if (lightProbes == null && occlusionProbes == null)
                throw new ArgumentException("Argument lightProbes and occlusionProbes cannot both be null.");
            else if (lightProbes != null && lightProbes.Length < positions.Length)
                throw new ArgumentException("lightProbes", "Argument lightProbes has less elements than positions");
            else if (occlusionProbes != null && occlusionProbes.Length < positions.Length)
                throw new ArgumentException("occlusionProbes", "Argument occlusionProbes has less elements than positions");

            CalculateInterpolatedLightAndOcclusionProbes_Internal(positions, positions.Length, lightProbes, occlusionProbes);
        }

        public static void CalculateInterpolatedLightAndOcclusionProbes(List<Vector3> positions, List<SphericalHarmonicsL2> lightProbes, List<Vector4> occlusionProbes)
        {
            if (positions == null)
                throw new ArgumentNullException("positions");
            else if (lightProbes == null && occlusionProbes == null)
                throw new ArgumentException("Argument lightProbes and occlusionProbes cannot both be null.");

            if (lightProbes != null)
            {
                if (lightProbes.Capacity < positions.Count)
                    lightProbes.Capacity = positions.Count;
                if (lightProbes.Count < positions.Count)
                    NoAllocHelpers.ResizeList(lightProbes, positions.Count);
            }

            if (occlusionProbes != null)
            {
                if (occlusionProbes.Capacity < positions.Count)
                    occlusionProbes.Capacity = positions.Count;
                if (occlusionProbes.Count < positions.Count)
                    NoAllocHelpers.ResizeList(occlusionProbes, positions.Count);
            }

            CalculateInterpolatedLightAndOcclusionProbes_Internal(NoAllocHelpers.ExtractArrayFromListT(positions), positions.Count, NoAllocHelpers.ExtractArrayFromListT(lightProbes), NoAllocHelpers.ExtractArrayFromListT(occlusionProbes));
        }

        [FreeFunction]
        [NativeName("CalculateInterpolatedLightAndOcclusionProbes")]
        internal extern static void CalculateInterpolatedLightAndOcclusionProbes_Internal(Vector3[] positions, int positionsCount, SphericalHarmonicsL2[] lightProbes, Vector4[] occlusionProbes);

        // Positions of the baked light probes.
        public extern Vector3[] positions
        {
            [FreeFunction(HasExplicitThis = true)]
            [NativeName("GetLightProbePositions")] get;
        }

        public extern UnityEngine.Rendering.SphericalHarmonicsL2[] bakedProbes
        {
            [FreeFunction(HasExplicitThis = true)]
            [NativeName("GetBakedCoefficients")] get;

            [FreeFunction(HasExplicitThis = true)]
            [NativeName("SetBakedCoefficients")] set;
        }

        // The number of light probes.
        public extern int count
        {
            [FreeFunction(HasExplicitThis = true)]
            [NativeName("GetLightProbeCount")] get;
        }

        // The number of cells (tetrahedra + outer cells) the space is divided to.
        public extern int cellCount
        {
            [FreeFunction(HasExplicitThis = true)]
            [NativeName("GetTetrahedraSize")] get;
        }

        [FreeFunction]
        [NativeName("GetLightProbeCount")]
        internal static extern int GetCount();
    }
}

namespace UnityEngine
{
    public enum D3DHDRDisplayBitDepth
    {
        D3DHDRDisplayBitDepth10,
        D3DHDRDisplayBitDepth16
    }

    [NativeHeader("Runtime/GfxDevice/HDROutputSettings.h")]
    [UsedByNativeCode]
    public class HDROutputSettings
    {
        private int m_DisplayIndex;

        //Don't allow users to construct these themselves, instead they need to be accessed from an internally managed list
        //This lines up with how multiple displays are handled, and while HDR is currently primary display only this will help with
        //future proofing this implementation, see Display in Display.bindings.cs
        internal HDROutputSettings() { m_DisplayIndex = 0; }
        internal HDROutputSettings(int displayIndex) { this.m_DisplayIndex = displayIndex; }

        public static HDROutputSettings[] displays = new HDROutputSettings[1] { new HDROutputSettings() };
        private static HDROutputSettings _mainDisplay = displays[0];
        public static HDROutputSettings main { get { return _mainDisplay; } }

        public bool active { get { return GetActive(m_DisplayIndex); } }
        public bool available { get { return GetAvailable(m_DisplayIndex); } }
        public bool automaticHDRTonemapping
        {
            get
            {
                return GetAutomaticHDRTonemapping(m_DisplayIndex);
            }
            set
            {
                SetAutomaticHDRTonemapping(m_DisplayIndex, value);
            }
        }
        public ColorGamut displayColorGamut { get { return GetDisplayColorGamut(m_DisplayIndex); } }
        public RenderTextureFormat format { get { return GraphicsFormatUtility.GetRenderTextureFormat(GetGraphicsFormat(m_DisplayIndex)); } }
        public GraphicsFormat graphicsFormat { get { return GetGraphicsFormat(m_DisplayIndex); }  }
        public float paperWhiteNits
        {
            get
            {
                return GetPaperWhiteNits(m_DisplayIndex);
            }
            set
            {
                SetPaperWhiteNits(m_DisplayIndex, value);
            }
        }
        public int maxFullFrameToneMapLuminance { get { return GetMaxFullFrameToneMapLuminance(m_DisplayIndex); } }
        public int maxToneMapLuminance { get { return GetMaxToneMapLuminance(m_DisplayIndex); } }
        public int minToneMapLuminance { get { return GetMinToneMapLuminance(m_DisplayIndex); } }
        public bool HDRModeChangeRequested { get { return GetHDRModeChangeRequested(m_DisplayIndex); } }

        public void RequestHDRModeChange(bool enabled)
        {
            RequestHDRModeChangeInternal(m_DisplayIndex, enabled);
        }

        [Obsolete("SetPaperWhiteInNits is deprecated, please use paperWhiteNits instead.")]
        public static void SetPaperWhiteInNits(float paperWhite)
        {
            int mainDisplay = 0;
            //Set paper white on the primary display
            if (GetAvailable(mainDisplay))
                SetPaperWhiteNits(mainDisplay, paperWhite);
        }

        [FreeFunction("HDROutputSettingsBindings::GetActive", HasExplicitThis = false, ThrowsException = true)]
        extern private static bool GetActive(int displayIndex);

        [FreeFunction("HDROutputSettingsBindings::GetAvailable", HasExplicitThis = false, ThrowsException = true)]
        extern private static bool GetAvailable(int displayIndex);

        [FreeFunction("HDROutputSettingsBindings::GetAutomaticHDRTonemapping", HasExplicitThis = false, ThrowsException = true)]
        extern private static bool GetAutomaticHDRTonemapping(int displayIndex);

        [FreeFunction("HDROutputSettingsBindings::SetAutomaticHDRTonemapping", HasExplicitThis = false, ThrowsException = true)]
        extern private static void SetAutomaticHDRTonemapping(int displayIndex, bool scripted);

        [FreeFunction("HDROutputSettingsBindings::GetDisplayColorGamut", HasExplicitThis = false, ThrowsException = true)]
        extern private static ColorGamut GetDisplayColorGamut(int displayIndex);

        [FreeFunction("HDROutputSettingsBindings::GetGraphicsFormat", HasExplicitThis = false, ThrowsException = true)]
        extern private static GraphicsFormat GetGraphicsFormat(int displayIndex);

        [FreeFunction("HDROutputSettingsBindings::GetPaperWhiteNits", HasExplicitThis = false, ThrowsException = true)]
        extern private static float GetPaperWhiteNits(int displayIndex);

        [FreeFunction("HDROutputSettingsBindings::SetPaperWhiteNits", HasExplicitThis = false, ThrowsException = true)]
        extern private static void SetPaperWhiteNits(int displayIndex, float paperWhite);

        [FreeFunction("HDROutputSettingsBindings::GetMaxFullFrameToneMapLuminance", HasExplicitThis = false, ThrowsException = true)]
        extern private static int GetMaxFullFrameToneMapLuminance(int displayIndex);

        [FreeFunction("HDROutputSettingsBindings::GetMaxToneMapLuminance", HasExplicitThis = false, ThrowsException = true)]
        extern private static int GetMaxToneMapLuminance(int displayIndex);

        [FreeFunction("HDROutputSettingsBindings::GetMinToneMapLuminance", HasExplicitThis = false, ThrowsException = true)]
        extern private static int GetMinToneMapLuminance(int displayIndex);

        [FreeFunction("HDROutputSettingsBindings::GetHDRModeChangeRequested", HasExplicitThis = false, ThrowsException = true)]
        extern private static bool GetHDRModeChangeRequested(int displayIndex);

        [FreeFunction("HDROutputSettingsBindings::RequestHDRModeChange", HasExplicitThis = false, ThrowsException = true)]
        extern private static void RequestHDRModeChangeInternal(int displayIndex, bool enabled);
    }
}

namespace UnityEngine.Rendering
{
    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    public class PIX
    {
        [FreeFunction("PIX::BeginGPUCapture")]
        public static extern void BeginGPUCapture();

        [FreeFunction("PIX::EndGPUCapture")]
        public static extern void EndGPUCapture();

        [FreeFunction("PIX::IsAttached")]
        public static extern bool IsAttached();
    }
}

namespace UnityEngine.Experimental.Rendering
{
    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    public static class ExternalGPUProfiler
    {
        [FreeFunction("ExternalGPUProfilerBindings::BeginGPUCapture")]
        public static extern void BeginGPUCapture();

        [FreeFunction("ExternalGPUProfilerBindings::EndGPUCapture")]
        public static extern void EndGPUCapture();

        [FreeFunction("ExternalGPUProfilerBindings::IsAttached")]
        public static extern bool IsAttached();
    }
}

namespace UnityEngine.Experimental.Rendering
{
    public enum WaitForPresentSyncPoint
    {
        BeginFrame = 0,
        EndFrame = 1
    }

    public enum GraphicsJobsSyncPoint
    {
        EndOfFrame = 0,
        AfterScriptUpdate = 1,
        AfterScriptLateUpdate = 2,
        WaitForPresent = 3
    }

    public static partial class GraphicsDeviceSettings
    {
        [StaticAccessor("GetGfxDevice()", StaticAccessorType.Dot)]
        extern public static WaitForPresentSyncPoint waitForPresentSyncPoint { get; set; }

        [StaticAccessor("GetGfxDevice()", StaticAccessorType.Dot)]
        extern public static GraphicsJobsSyncPoint graphicsJobsSyncPoint { get; set; }
    }
}
