// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using uei = UnityEngine.Internal;

namespace UnityEngine
{
    [RequiredByNativeCode]
    public struct Resolution
    {
        // Keep in sync with ScreenManager::Resolution
        private int m_Width;
        private int m_Height;
        private int m_RefreshRate;

        public int width        { get { return m_Width; } set { m_Width = value; } }
        public int height       { get { return m_Height; } set { m_Height = value; } }
        public int refreshRate  { get { return m_RefreshRate; } set { m_RefreshRate = value; } }

        public override string ToString()
        {
            return UnityString.Format("{0} x {1} @ {2}Hz", m_Width, m_Height, m_RefreshRate);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public partial struct RenderBuffer
    {
        internal int m_RenderTextureInstanceID;
        internal IntPtr m_BufferPtr;

        internal RenderBufferLoadAction  loadAction  { get { return GetLoadAction();  } set { SetLoadAction(value); } }
        internal RenderBufferStoreAction storeAction { get { return GetStoreAction(); } set { SetStoreAction(value); } }
    }

    public struct RenderTargetSetup
    {
        public RenderBuffer[]   color;
        public RenderBuffer     depth;

        public int              mipLevel;
        public CubemapFace      cubemapFace;
        public int              depthSlice;

        public Rendering.RenderBufferLoadAction[]   colorLoad;
        public Rendering.RenderBufferStoreAction[]  colorStore;

        public Rendering.RenderBufferLoadAction     depthLoad;
        public Rendering.RenderBufferStoreAction    depthStore;


        public RenderTargetSetup(
            RenderBuffer[] color, RenderBuffer depth, int mip, CubemapFace face,
            Rendering.RenderBufferLoadAction[] colorLoad, Rendering.RenderBufferStoreAction[] colorStore,
            Rendering.RenderBufferLoadAction depthLoad, Rendering.RenderBufferStoreAction depthStore
        )
        {
            this.color          = color;
            this.depth          = depth;

            this.mipLevel       = mip;
            this.cubemapFace    = face;
            this.depthSlice     = 0;

            this.colorLoad      = colorLoad;
            this.colorStore     = colorStore;

            this.depthLoad      = depthLoad;
            this.depthStore     = depthStore;
        }

        internal static Rendering.RenderBufferLoadAction[] LoadActions(RenderBuffer[] buf)
        {
            // preserve old discard behaviour: render surface flags are applied only on first activation
            // this will be used only in ctor without load/store actions specified
            Rendering.RenderBufferLoadAction[] ret = new Rendering.RenderBufferLoadAction[buf.Length];
            for (int i = 0; i < buf.Length; ++i)
            {
                ret[i] = buf[i].loadAction;
                buf[i].loadAction = Rendering.RenderBufferLoadAction.Load;
            }
            return ret;
        }

        internal static Rendering.RenderBufferStoreAction[] StoreActions(RenderBuffer[] buf)
        {
            // preserve old discard behaviour: render surface flags are applied only on first activation
            // this will be used only in ctor without load/store actions specified
            Rendering.RenderBufferStoreAction[] ret = new Rendering.RenderBufferStoreAction[buf.Length];
            for (int i = 0; i < buf.Length; ++i)
            {
                ret[i] = buf[i].storeAction;
                buf[i].storeAction = Rendering.RenderBufferStoreAction.Store;
            }
            return ret;
        }

        // TODO: when we enable default arguments support these can be combined into one method
        public RenderTargetSetup(RenderBuffer color, RenderBuffer depth)
            : this(new RenderBuffer[] { color }, depth)
        {
        }

        public RenderTargetSetup(RenderBuffer color, RenderBuffer depth, int mipLevel)
            : this(new RenderBuffer[] { color }, depth, mipLevel)
        {
        }

        public RenderTargetSetup(RenderBuffer color, RenderBuffer depth, int mipLevel, CubemapFace face)
            : this(new RenderBuffer[] { color }, depth, mipLevel, face)
        {
        }

        public RenderTargetSetup(RenderBuffer color, RenderBuffer depth, int mipLevel, CubemapFace face, int depthSlice)
            : this(new RenderBuffer[] { color }, depth, mipLevel, face)
        {
            this.depthSlice = depthSlice;
        }

        // TODO: when we enable default arguments support these can be combined into one method
        public RenderTargetSetup(RenderBuffer[] color, RenderBuffer depth)
            : this(color, depth, 0, CubemapFace.Unknown)
        {
        }

        public RenderTargetSetup(RenderBuffer[] color, RenderBuffer depth, int mipLevel)
            : this(color, depth, mipLevel, CubemapFace.Unknown)
        {
        }

        public RenderTargetSetup(RenderBuffer[] color, RenderBuffer depth, int mip, CubemapFace face)
            : this(color, depth, mip, face, LoadActions(color), StoreActions(color), depth.loadAction, depth.storeAction)
        {
        }
    }
}


//
// Graphics.SetRenderTarget
//


namespace UnityEngine
{
    public partial class Graphics
    {
        internal static void CheckLoadActionValid(Rendering.RenderBufferLoadAction load, string bufferType)
        {
            if (load != Rendering.RenderBufferLoadAction.Load && load != Rendering.RenderBufferLoadAction.DontCare)
                throw new ArgumentException(UnityString.Format("Bad {0} LoadAction provided.", bufferType));
        }

        internal static void CheckStoreActionValid(Rendering.RenderBufferStoreAction store, string bufferType)
        {
            if (store != Rendering.RenderBufferStoreAction.Store && store != Rendering.RenderBufferStoreAction.DontCare)
                throw new ArgumentException(UnityString.Format("Bad {0} StoreAction provided.", bufferType));
        }

        internal static void SetRenderTargetImpl(RenderTargetSetup setup)
        {
            if (setup.color.Length == 0)
                throw new ArgumentException("Invalid color buffer count for SetRenderTarget");
            if (setup.color.Length != setup.colorLoad.Length)
                throw new ArgumentException("Color LoadAction and Buffer arrays have different sizes");
            if (setup.color.Length != setup.colorStore.Length)
                throw new ArgumentException("Color StoreAction and Buffer arrays have different sizes");

            foreach (var load in setup.colorLoad)
                CheckLoadActionValid(load, "Color");
            foreach (var store in setup.colorStore)
                CheckStoreActionValid(store, "Color");

            CheckLoadActionValid(setup.depthLoad, "Depth");
            CheckStoreActionValid(setup.depthStore, "Depth");

            if ((int)setup.cubemapFace < (int)CubemapFace.Unknown || (int)setup.cubemapFace > (int)CubemapFace.NegativeZ)
                throw new ArgumentException("Bad CubemapFace provided");

            Internal_SetMRTFullSetup(
                setup.color, setup.depth, setup.mipLevel, setup.cubemapFace, setup.depthSlice,
                setup.colorLoad, setup.colorStore, setup.depthLoad, setup.depthStore
            );
        }

        internal static void SetRenderTargetImpl(RenderBuffer colorBuffer, RenderBuffer depthBuffer, int mipLevel, CubemapFace face, int depthSlice)
        {
            Internal_SetRTSimple(colorBuffer, depthBuffer, mipLevel, face, depthSlice);
        }

        internal static void SetRenderTargetImpl(RenderTexture rt, int mipLevel, CubemapFace face, int depthSlice)
        {
            if (rt) SetRenderTargetImpl(rt.colorBuffer, rt.depthBuffer, mipLevel, face, depthSlice);
            else    Internal_SetNullRT();
        }

        internal static void SetRenderTargetImpl(RenderBuffer[] colorBuffers, RenderBuffer depthBuffer, int mipLevel, CubemapFace face, int depthSlice)
        {
            RenderBuffer depth = depthBuffer;
            Internal_SetMRTSimple(colorBuffers, depth, mipLevel, face, depthSlice);
        }

        public static void SetRenderTarget(RenderTexture rt, [uei.DefaultValue("0")] int mipLevel, [uei.DefaultValue("CubemapFace.Unknown")] CubemapFace face, [uei.DefaultValue("0")] int depthSlice)
        {
            SetRenderTargetImpl(rt, mipLevel, face, depthSlice);
        }

        public static void SetRenderTarget(RenderBuffer colorBuffer, RenderBuffer depthBuffer, [uei.DefaultValue("0")] int mipLevel, [uei.DefaultValue("CubemapFace.Unknown")] CubemapFace face, [uei.DefaultValue("0")] int depthSlice)
        {
            SetRenderTargetImpl(colorBuffer, depthBuffer, mipLevel, face, depthSlice);
        }

        public static void SetRenderTarget(RenderBuffer[] colorBuffers, RenderBuffer depthBuffer)
        {
            SetRenderTargetImpl(colorBuffers, depthBuffer, 0, CubemapFace.Unknown, 0);
        }

        public static void SetRenderTarget(RenderTargetSetup setup)
        {
            SetRenderTargetImpl(setup);
        }
    }

    public partial class Graphics
    {
        public static RenderBuffer activeColorBuffer { get { return GetActiveColorBuffer(); } }
        public static RenderBuffer activeDepthBuffer { get { return GetActiveDepthBuffer(); } }

        public static void SetRandomWriteTarget(int index, RenderTexture uav)
        {
            if (index < 0 || index >= SystemInfo.supportedRandomWriteTargetCount)
                throw new ArgumentOutOfRangeException("index", string.Format("must be non-negative less than {0}.", SystemInfo.supportedRandomWriteTargetCount));

            Internal_SetRandomWriteTargetRT(index, uav);
        }

        public static void SetRandomWriteTarget(int index, ComputeBuffer uav, [uei.DefaultValue("false")] bool preserveCounterValue)
        {
            if (uav == null) throw new ArgumentNullException("uav");
            if (uav.m_Ptr == IntPtr.Zero) throw new System.ObjectDisposedException("uav");
            if (index < 0 || index >= SystemInfo.supportedRandomWriteTargetCount)
                throw new ArgumentOutOfRangeException("index", string.Format("must be non-negative less than {0}.", SystemInfo.supportedRandomWriteTargetCount));

            Internal_SetRandomWriteTargetBuffer(index, uav, preserveCounterValue);
        }

        public static void CopyTexture(Texture src, Texture dst)
        {
            CopyTexture_Full(src, dst);
        }

        public static void CopyTexture(Texture src, int srcElement, Texture dst, int dstElement)
        {
            CopyTexture_Slice_AllMips(src, srcElement, dst, dstElement);
        }

        public static void CopyTexture(Texture src, int srcElement, int srcMip, Texture dst, int dstElement, int dstMip)
        {
            CopyTexture_Slice(src, srcElement, srcMip, dst, dstElement, dstMip);
        }

        public static void CopyTexture(Texture src, int srcElement, int srcMip, int srcX, int srcY, int srcWidth, int srcHeight, Texture dst, int dstElement, int dstMip, int dstX, int dstY)
        {
            CopyTexture_Region(src, srcElement, srcMip, srcX, srcY, srcWidth, srcHeight, dst, dstElement, dstMip, dstX, dstY);
        }

        public static bool ConvertTexture(Texture src, Texture dst)
        {
            return ConvertTexture_Full(src, dst);
        }

        public static bool ConvertTexture(Texture src, int srcElement, Texture dst, int dstElement)
        {
            return ConvertTexture_Slice(src, srcElement, dst, dstElement);
        }

        public static GPUFence CreateGPUFence([uei.DefaultValue("SynchronisationStage.PixelProcessing")] SynchronisationStage stage)
        {
            GPUFence newFence = new GPUFence();
            newFence.m_Ptr = CreateGPUFenceImpl(stage);
            newFence.InitPostAllocation();
            newFence.Validate();
            return newFence;
        }

        public static void WaitOnGPUFence(GPUFence fence, [uei.DefaultValue("SynchronisationStage.VertexProcessing")] SynchronisationStage stage)
        {
            fence.Validate();

            //Don't wait on a fence that's already known to have passed
            if (fence.IsFencePending())
                WaitOnGPUFenceImpl(fence.m_Ptr, stage);
        }

        [uei.ExcludeFromDocs] public static GPUFence CreateGPUFence() { return CreateGPUFence(SynchronisationStage.PixelProcessing); }
        [uei.ExcludeFromDocs] public static void WaitOnGPUFence(GPUFence fence) { WaitOnGPUFence(fence, SynchronisationStage.VertexProcessing); }
    }
}


//
// Graphics.Draw*
//


namespace UnityEngine
{
    [VisibleToOtherModules("UnityEngine.IMGUIModule")]
    internal struct Internal_DrawTextureArguments
    {
        public Rect screenRect, sourceRect;
        public int leftBorder, rightBorder, topBorder, bottomBorder;
        public Color color;
        public Vector4 borderWidths;
        public Vector4 cornerRadiuses;
        public int pass;
        public Texture texture;
        public Material mat;
    }


    public partial class Graphics
    {
        private static void DrawTextureImpl(Rect screenRect, Texture texture, Rect sourceRect, int leftBorder, int rightBorder, int topBorder, int bottomBorder, Color color, Material mat, int pass)
        {
            Internal_DrawTextureArguments args = new Internal_DrawTextureArguments();
            args.screenRect = screenRect; args.sourceRect = sourceRect;
            args.leftBorder = leftBorder; args.rightBorder = rightBorder; args.topBorder = topBorder; args.bottomBorder = bottomBorder;
            args.color = color;
            args.pass = pass;
            args.texture = texture;
            args.mat = mat;

            Internal_DrawTexture(ref args);
        }

        public static void DrawTexture(Rect screenRect, Texture texture, Rect sourceRect, int leftBorder, int rightBorder, int topBorder, int bottomBorder, Color color, [uei.DefaultValue("null")] Material mat, [uei.DefaultValue("-1")] int pass)
        {
            DrawTextureImpl(screenRect, texture, sourceRect, leftBorder, rightBorder, topBorder, bottomBorder, color, mat, pass);
        }

        public static void DrawTexture(Rect screenRect, Texture texture, Rect sourceRect, int leftBorder, int rightBorder, int topBorder, int bottomBorder, [uei.DefaultValue("null")] Material mat, [uei.DefaultValue("-1")] int pass)
        {
            Color32 color = new Color32(128, 128, 128, 128);
            DrawTextureImpl(screenRect, texture, sourceRect, leftBorder, rightBorder, topBorder, bottomBorder, color, mat, pass);
        }

        public static void DrawTexture(Rect screenRect, Texture texture, int leftBorder, int rightBorder, int topBorder, int bottomBorder, [uei.DefaultValue("null")] Material mat, [uei.DefaultValue("-1")] int pass)
        {
            DrawTexture(screenRect, texture, new Rect(0, 0, 1, 1), leftBorder, rightBorder, topBorder, bottomBorder, mat, pass);
        }

        public static void DrawTexture(Rect screenRect, Texture texture, [uei.DefaultValue("null")] Material mat, [uei.DefaultValue("-1")] int pass)
        {
            DrawTexture(screenRect, texture, 0, 0, 0, 0, mat, pass);
        }

        public static void DrawMeshNow(Mesh mesh, Vector3 position, Quaternion rotation, int materialIndex)
        {
            if (mesh == null)
                throw new ArgumentNullException("mesh");
            Internal_DrawMeshNow1(mesh, materialIndex, position, rotation);
        }

        public static void DrawMeshNow(Mesh mesh, Matrix4x4 matrix, int materialIndex)
        {
            if (mesh == null)
                throw new ArgumentNullException("mesh");
            Internal_DrawMeshNow2(mesh, materialIndex, matrix);
        }

        public static void DrawMeshNow(Mesh mesh, Vector3 position, Quaternion rotation) { DrawMeshNow(mesh, position, rotation, -1); }
        public static void DrawMeshNow(Mesh mesh, Matrix4x4 matrix) { DrawMeshNow(mesh, matrix, -1); }


        public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, [uei.DefaultValue("null")] Camera camera, [uei.DefaultValue("0")] int submeshIndex, [uei.DefaultValue("null")] MaterialPropertyBlock properties, [uei.DefaultValue("true")] bool castShadows, [uei.DefaultValue("true")] bool receiveShadows, [uei.DefaultValue("true")] bool useLightProbes)
        {
            DrawMesh(mesh, Matrix4x4.TRS(position, rotation, Vector3.one), material, layer, camera, submeshIndex, properties, castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off, receiveShadows, null, useLightProbes ? LightProbeUsage.BlendProbes : LightProbeUsage.Off, null);
        }

        public static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, ShadowCastingMode castShadows, [uei.DefaultValue("true")] bool receiveShadows, [uei.DefaultValue("null")] Transform probeAnchor, [uei.DefaultValue("true")] bool useLightProbes)
        {
            DrawMesh(mesh, Matrix4x4.TRS(position, rotation, Vector3.one), material, layer, camera, submeshIndex, properties, castShadows, receiveShadows, probeAnchor, useLightProbes ? LightProbeUsage.BlendProbes : LightProbeUsage.Off, null);
        }

        public static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer, [uei.DefaultValue("null")] Camera camera, [uei.DefaultValue("0")] int submeshIndex, [uei.DefaultValue("null")] MaterialPropertyBlock properties, [uei.DefaultValue("true")] bool castShadows, [uei.DefaultValue("true")] bool receiveShadows, [uei.DefaultValue("true")] bool useLightProbes)
        {
            DrawMesh(mesh, matrix, material, layer, camera, submeshIndex, properties, castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off, receiveShadows, null, useLightProbes ? LightProbeUsage.BlendProbes : LightProbeUsage.Off, null);
        }

        public static void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, ShadowCastingMode castShadows, bool receiveShadows, Transform probeAnchor, LightProbeUsage lightProbeUsage, [uei.DefaultValue("null")] LightProbeProxyVolume lightProbeProxyVolume)
        {
            if (lightProbeUsage == LightProbeUsage.UseProxyVolume && lightProbeProxyVolume == null)
                throw new ArgumentException("Argument lightProbeProxyVolume must not be null if lightProbeUsage is set to UseProxyVolume.", "lightProbeProxyVolume");
            Internal_DrawMesh(mesh, submeshIndex, matrix, material, layer, camera, properties, castShadows, receiveShadows, probeAnchor, lightProbeUsage, lightProbeProxyVolume);
        }

        public static void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, Matrix4x4[] matrices, [uei.DefaultValue("matrices.Length")] int count, [uei.DefaultValue("null")] MaterialPropertyBlock properties, [uei.DefaultValue("ShadowCastingMode.On")] ShadowCastingMode castShadows, [uei.DefaultValue("true")] bool receiveShadows, [uei.DefaultValue("0")] int layer, [uei.DefaultValue("null")] Camera camera, [uei.DefaultValue("LightProbeUsage.BlendProbes")] LightProbeUsage lightProbeUsage, [uei.DefaultValue("null")] LightProbeProxyVolume lightProbeProxyVolume)
        {
            if (!SystemInfo.supportsInstancing)
                throw new InvalidOperationException("Instancing is not supported.");
            else if (mesh == null)
                throw new ArgumentNullException("mesh");
            else if (submeshIndex < 0 || submeshIndex >= mesh.subMeshCount)
                throw new ArgumentOutOfRangeException("submeshIndex", "submeshIndex out of range.");
            else if (material == null)
                throw new ArgumentNullException("material");
            else if (!material.enableInstancing)
                throw new InvalidOperationException("Material needs to enable instancing for use with DrawMeshInstanced.");
            else if (matrices == null)
                throw new ArgumentNullException("matrices");
            else if (count < 0 || count > Mathf.Min(kMaxDrawMeshInstanceCount, matrices.Length))
                throw new ArgumentOutOfRangeException("count", String.Format("Count must be in the range of 0 to {0}.", Mathf.Min(kMaxDrawMeshInstanceCount, matrices.Length)));
            else if (lightProbeUsage == LightProbeUsage.UseProxyVolume && lightProbeProxyVolume == null)
                throw new ArgumentException("Argument lightProbeProxyVolume must not be null if lightProbeUsage is set to UseProxyVolume.", "lightProbeProxyVolume");

            if (count > 0)
                Internal_DrawMeshInstanced(mesh, submeshIndex, material, matrices, count, properties, castShadows, receiveShadows, layer, camera, lightProbeUsage, lightProbeProxyVolume);
        }

        public static void DrawMeshInstanced(Mesh mesh, int submeshIndex, Material material, List<Matrix4x4> matrices, [uei.DefaultValue("null")] MaterialPropertyBlock properties, [uei.DefaultValue("ShadowCastingMode.On")] ShadowCastingMode castShadows, [uei.DefaultValue("true")] bool receiveShadows, [uei.DefaultValue("0")] int layer, [uei.DefaultValue("null")] Camera camera, [uei.DefaultValue("LightProbeUsage.BlendProbes")] LightProbeUsage lightProbeUsage, [uei.DefaultValue("null")] LightProbeProxyVolume lightProbeProxyVolume)
        {
            if (matrices == null)
                throw new ArgumentNullException("matrices");

            DrawMeshInstanced(mesh, submeshIndex, material, NoAllocHelpers.ExtractArrayFromListT(matrices), matrices.Count, properties, castShadows, receiveShadows, layer, camera, lightProbeUsage, lightProbeProxyVolume);
        }

        public static void DrawMeshInstancedIndirect(Mesh mesh, int submeshIndex, Material material, Bounds bounds, ComputeBuffer bufferWithArgs, [uei.DefaultValue("0")] int argsOffset, [uei.DefaultValue("null")] MaterialPropertyBlock properties, [uei.DefaultValue("ShadowCastingMode.On")] ShadowCastingMode castShadows, [uei.DefaultValue("true")] bool receiveShadows, [uei.DefaultValue("0")] int layer, [uei.DefaultValue("null")] Camera camera, [uei.DefaultValue("LightProbeUsage.BlendProbes")] LightProbeUsage lightProbeUsage, [uei.DefaultValue("null")] LightProbeProxyVolume lightProbeProxyVolume)
        {
            if (!SystemInfo.supportsInstancing)
                throw new InvalidOperationException("Instancing is not supported.");
            else if (mesh == null)
                throw new ArgumentNullException("mesh");
            else if (submeshIndex < 0 || submeshIndex >= mesh.subMeshCount)
                throw new ArgumentOutOfRangeException("submeshIndex", "submeshIndex out of range.");
            else if (material == null)
                throw new ArgumentNullException("material");
            else if (bufferWithArgs == null)
                throw new ArgumentNullException("bufferWithArgs");
            if (lightProbeUsage == LightProbeUsage.UseProxyVolume && lightProbeProxyVolume == null)
                throw new ArgumentException("Argument lightProbeProxyVolume must not be null if lightProbeUsage is set to UseProxyVolume.", "lightProbeProxyVolume");

            Internal_DrawMeshInstancedIndirect(mesh, submeshIndex, material, bounds, bufferWithArgs, argsOffset, properties, castShadows, receiveShadows, layer, camera, lightProbeUsage, lightProbeProxyVolume);
        }

        public static void DrawProcedural(MeshTopology topology, int vertexCount, [uei.DefaultValue("1")] int instanceCount)
        {
            Internal_DrawProcedural(topology, vertexCount, instanceCount);
        }

        public static void DrawProceduralIndirect(MeshTopology topology, ComputeBuffer bufferWithArgs, [uei.DefaultValue("0")] int argsOffset)
        {
            if (bufferWithArgs == null)
                throw new ArgumentNullException("bufferWithArgs");

            Internal_DrawProceduralIndirect(topology, bufferWithArgs, argsOffset);
        }
    }
}


//
// Graphics.Blit*
//


namespace UnityEngine
{
    public partial class Graphics
    {
        public static void Blit(Texture source, RenderTexture dest)
        {
            Blit2(source, dest);
        }

        public static void Blit(Texture source, RenderTexture dest, Vector2 scale, Vector2 offset)
        {
            Blit4(source, dest, scale, offset);
        }

        public static void Blit(Texture source, RenderTexture dest, Material mat, [uei.DefaultValue("-1")] int pass)
        {
            Internal_BlitMaterial(source, dest, mat, pass, true);
        }

        public static void Blit(Texture source, RenderTexture dest, Material mat)
        {
            Blit(source, dest, mat, -1);
        }

        public static void Blit(Texture source, Material mat, [uei.DefaultValue("-1")] int pass)
        {
            Internal_BlitMaterial(source, null, mat, pass, false);
        }

        public static void Blit(Texture source, Material mat)
        {
            Blit(source, mat, -1);
        }

        public static void BlitMultiTap(Texture source, RenderTexture dest, Material mat, params Vector2[] offsets)
        {
            // in case params were not passed, we will end up with empty array (not null) but our cpp code is not ready for that.
            // do explicit argument exception instead of potential nullref coming from native side
            if (offsets.Length == 0)
                throw new ArgumentException("empty offsets list passed.", "offsets");
            Internal_BlitMultiTap(source, dest, mat, offsets);
        }
    }
}


//
// QualitySettings
//


namespace UnityEngine
{
    public sealed partial class QualitySettings
    {
        public static void IncreaseLevel([uei.DefaultValue("false")] bool applyExpensiveChanges)
        {
            SetQualityLevel(GetQualityLevel() + 1, applyExpensiveChanges);
        }

        public static void DecreaseLevel([uei.DefaultValue("false")] bool applyExpensiveChanges)
        {
            SetQualityLevel(GetQualityLevel() - 1, applyExpensiveChanges);
        }

        public static void SetQualityLevel(int index) { SetQualityLevel(index, true); }
        public static void IncreaseLevel() { IncreaseLevel(false); }
        public static void DecreaseLevel() { DecreaseLevel(false); }
    }
}

//
// Extensions
//

namespace UnityEngine
{
    public static partial class RendererExtensions
    {
        static public void UpdateGIMaterials(this Renderer renderer) { UpdateGIMaterialsForRenderer(renderer); }
    }
}

//
// Attributes
//

namespace UnityEngine
{
    [UsedByNativeCode]
    public sealed partial class ImageEffectTransformsToLDR : Attribute
    {
    }

    public sealed partial class ImageEffectAllowedInSceneView : Attribute
    {
    }

    [UsedByNativeCode]
    public sealed partial class ImageEffectOpaque : Attribute
    {
    }

    [UsedByNativeCode]
    public sealed partial class ImageEffectAfterScale : Attribute
    {
    }
}
