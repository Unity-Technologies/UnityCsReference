// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using uei = UnityEngine.Internal;

namespace UnityEngine
{
    internal sealed partial class NoAllocHelpers
    {
        public static void ResizeList<T>(List<T> list, int size)
        {
            if (list == null)
                throw new ArgumentNullException("list");
            if (size < 0 || size > list.Capacity)
                throw new ArgumentException("list", "invalid size to resize.");
            if (size != list.Count)
                Internal_ResizeList(list, size);
        }


        public static T[] ExtractArrayFromListT<T>(List<T> list) { return (T[])ExtractArrayFromList(list); }

        public static void EnsureListElemCount<T>(List<T> list, int count)
        {
            list.Clear();

            // make sure capacity is enough (that's where alloc WILL happen if needed)
            if (list.Capacity < count)
                list.Capacity = count;

            ResizeList(list, count);
        }
    }
}


namespace UnityEngine
{
    [UsedByNativeCode]
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
    [UsedByNativeCode]
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

    [StructLayout(LayoutKind.Sequential)]
    public struct RenderBuffer
    {
        internal int m_RenderTextureInstanceID;
        internal IntPtr m_BufferPtr;

        internal void SetLoadAction(Rendering.RenderBufferLoadAction action)    { RenderBufferHelper.SetLoadAction(out this, (int)action); }
        internal void SetStoreAction(Rendering.RenderBufferStoreAction action)  { RenderBufferHelper.SetStoreAction(out this, (int)action); }

        internal Rendering.RenderBufferLoadAction loadAction
        {
            get { return (Rendering.RenderBufferLoadAction)RenderBufferHelper.GetLoadAction(out this); }
            set { SetLoadAction(value); }
        }
        internal Rendering.RenderBufferStoreAction storeAction
        {
            get { return (Rendering.RenderBufferStoreAction)RenderBufferHelper.GetStoreAction(out this); }
            set { SetStoreAction(value); }
        }

        public IntPtr GetNativeRenderBufferPtr()    { return RenderBufferHelper.GetNativeRenderBufferPtr(m_BufferPtr); }
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
                setup.color, out setup.depth, setup.mipLevel, setup.cubemapFace, setup.depthSlice,
                setup.colorLoad, setup.colorStore, setup.depthLoad, setup.depthStore
                );
        }

        internal static void SetRenderTargetImpl(RenderBuffer colorBuffer, RenderBuffer depthBuffer, int mipLevel, CubemapFace face, int depthSlice)
        {
            RenderBuffer color = colorBuffer, depth = depthBuffer;
            Internal_SetRTSimple(out color, out depth, mipLevel, face, depthSlice);
        }

        internal static void SetRenderTargetImpl(RenderTexture rt, int mipLevel, CubemapFace face, int depthSlice)
        {
            if (rt)  SetRenderTargetImpl(rt.colorBuffer, rt.depthBuffer, mipLevel, face, depthSlice);
            else    Internal_SetNullRT();
        }

        internal static void SetRenderTargetImpl(RenderBuffer[] colorBuffers, RenderBuffer depthBuffer, int mipLevel, CubemapFace face, int depthSlice)
        {
            RenderBuffer depth = depthBuffer;
            Internal_SetMRTSimple(colorBuffers, out depth, mipLevel, face, depthSlice);
        }
    }

    public partial class Graphics
    {
        // TODO: when we enable default arguments support these can be combined into one method
        public static void SetRenderTarget(RenderTexture rt)
        {
            SetRenderTargetImpl(rt, 0, CubemapFace.Unknown, 0);
        }

        public static void SetRenderTarget(RenderTexture rt, int mipLevel)
        {
            SetRenderTargetImpl(rt, mipLevel, CubemapFace.Unknown, 0);
        }

        public static void SetRenderTarget(RenderTexture rt, int mipLevel, CubemapFace face)
        {
            SetRenderTargetImpl(rt, mipLevel, face, 0);
        }

        public static void SetRenderTarget(RenderTexture rt, int mipLevel, CubemapFace face, int depthSlice)
        {
            SetRenderTargetImpl(rt, mipLevel, face, depthSlice);
        }

        // TODO: when we enable default arguments support these can be combined into one method
        public static void SetRenderTarget(RenderBuffer colorBuffer, RenderBuffer depthBuffer)
        {
            SetRenderTargetImpl(colorBuffer, depthBuffer, 0, CubemapFace.Unknown, 0);
        }

        public static void SetRenderTarget(RenderBuffer colorBuffer, RenderBuffer depthBuffer, int mipLevel)
        {
            SetRenderTargetImpl(colorBuffer, depthBuffer, mipLevel, CubemapFace.Unknown, 0);
        }

        public static void SetRenderTarget(RenderBuffer colorBuffer, RenderBuffer depthBuffer, int mipLevel, CubemapFace face)
        {
            SetRenderTargetImpl(colorBuffer, depthBuffer, mipLevel, face, 0);
        }

        public static void SetRenderTarget(RenderBuffer colorBuffer, RenderBuffer depthBuffer, int mipLevel, CubemapFace face, int depthSlice)
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

        // TODO: when we enable default arguments support these can be combined into one method
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

        // TODO: when we enable default arguments support these can be combined into one method
        public static bool ConvertTexture(Texture src, Texture dst)
        {
            return ConvertTexture_Full(src, dst);
        }

        public static bool ConvertTexture(Texture src, int srcElement, Texture dst, int dstElement)
        {
            return ConvertTexture_Slice(src, srcElement, dst, dstElement);
        }
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
        public Color32 color;
        public Vector4 borderWidths;
        public Vector4 cornerRadiuses;
        public int pass;
        public Texture texture;
        public Material mat;
    }


    public partial class Graphics
    {
        // NB: currently our c# toolchain do not accept default arguments (bindins generator will create actual functions that pass default values)
        // when we start to accept default params we can move the rest of DrawMesh out of bindings to c#
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

        public static void DrawMeshNow(Mesh mesh, Vector3 position, Quaternion rotation)
        {
            DrawMeshNow(mesh, position, rotation, -1);
        }

        public static void DrawMeshNow(Mesh mesh, Vector3 position, Quaternion rotation, int materialIndex)
        {
            if (mesh == null)
                throw new ArgumentNullException("mesh");
            Internal_DrawMeshNow1(mesh, materialIndex, position, rotation);
        }

        public static void DrawMeshNow(Mesh mesh, Matrix4x4 matrix)
        {
            DrawMeshNow(mesh, matrix, -1);
        }

        public static void DrawMeshNow(Mesh mesh, Matrix4x4 matrix, int materialIndex)
        {
            if (mesh == null)
                throw new ArgumentNullException("mesh");
            Internal_DrawMeshNow2(mesh, materialIndex, matrix);
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
