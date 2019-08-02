// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

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

        internal static void ForceRenderBufferLoadActionLoadImpl(bool val)
        {
            Internal_ForceRenderBufferLoadActionLoad(val);
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

        //undocumented
        public static void ForceRenderBufferLoadActionLoad(bool val)
        {
            ForceRenderBufferLoadActionLoadImpl(val);
        }
    }
}


//
// Graphics.Draw*
//


namespace UnityEngine
{
    internal struct Internal_DrawMeshMatrixArguments
    {
        public int layer, submeshIndex;
        public Matrix4x4 matrix;
        public int castShadows, receiveShadows;
        public int reflectionProbeAnchorInstanceID;
        public bool useLightProbes;
    }

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
        private static void DrawMeshImpl(Mesh mesh, Matrix4x4 matrix, Material material, int layer, Camera camera, int submeshIndex, MaterialPropertyBlock properties, Rendering.ShadowCastingMode castShadows, bool receiveShadows, Transform probeAnchor, bool useLightProbes)
        {
            Internal_DrawMeshMatrixArguments args = new Internal_DrawMeshMatrixArguments();
            args.layer = layer;
            args.submeshIndex = submeshIndex;
            args.matrix = matrix;
            args.castShadows = (int)castShadows;
            args.receiveShadows = receiveShadows ? 1 : 0;
            args.reflectionProbeAnchorInstanceID = probeAnchor != null ? probeAnchor.GetInstanceID() : 0;
            args.useLightProbes = useLightProbes;

            Internal_DrawMeshMatrix(ref args, properties, material, mesh, camera);
        }

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

        // NB: currently our c# toolchain do not accept default arguments (bindins generator will create actual functions that pass default values)
        // when we start to accept default params we can move the rest of DrawMesh out of bindings to c#
        private static void DrawMeshInstancedImpl(Mesh mesh, int submeshIndex, Material material, Matrix4x4[] matrices, int count, MaterialPropertyBlock properties, Rendering.ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera)
        {
            if (!SystemInfo.supportsInstancing)
                throw new InvalidOperationException("Instancing is not supported.");
            if (mesh == null)
                throw new ArgumentNullException("mesh");
            if (submeshIndex < 0 || submeshIndex >= mesh.subMeshCount)
                throw new ArgumentOutOfRangeException("submeshIndex", "submeshIndex out of range.");
            if (material == null)
                throw new ArgumentNullException("material");
            if (!material.enableInstancing)
                throw new InvalidOperationException("Material needs to enable instancing for use with DrawMeshInstanced.");
            if (matrices == null)
                throw new ArgumentNullException("matrices");
            if (count < 0 || count > Mathf.Min(kMaxDrawMeshInstanceCount, matrices.Length))
                throw new ArgumentOutOfRangeException("count", String.Format("Count must be in the range of 0 to {0}.", Mathf.Min(kMaxDrawMeshInstanceCount, matrices.Length)));

            if (count > 0)
                Internal_DrawMeshInstanced(mesh, submeshIndex, material, matrices, count, properties, castShadows, receiveShadows, layer, camera);
        }

        private static void DrawMeshInstancedImpl(Mesh mesh, int submeshIndex, Material material, List<Matrix4x4> matrices, MaterialPropertyBlock properties, Rendering.ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera)
        {
            if (matrices == null)
                throw new ArgumentNullException("matrices");
            if (matrices.Count > kMaxDrawMeshInstanceCount)
                throw new ArgumentOutOfRangeException("matrices", String.Format("Matrix list count must be in the range of 0 to {0}.", kMaxDrawMeshInstanceCount));

            DrawMeshInstancedImpl(mesh, submeshIndex, material, (Matrix4x4[])NoAllocHelpers.ExtractArrayFromList(matrices), matrices.Count, properties, castShadows, receiveShadows, layer, camera);
        }

        // NB: currently our c# toolchain do not accept default arguments (bindins generator will create actual functions that pass default values)
        // when we start to accept default params we can move the rest of DrawMesh out of bindings to c#
        private static void DrawMeshInstancedIndirectImpl(Mesh mesh, int submeshIndex, Material material, Bounds bounds, ComputeBuffer bufferWithArgs, int argsOffset, MaterialPropertyBlock properties, Rendering.ShadowCastingMode castShadows, bool receiveShadows, int layer, Camera camera)
        {
            if (!SystemInfo.supportsInstancing)
                throw new InvalidOperationException("Instancing is not supported.");
            if (mesh == null)
                throw new ArgumentNullException("mesh");
            if (submeshIndex < 0 || submeshIndex >= mesh.subMeshCount)
                throw new ArgumentOutOfRangeException("submeshIndex", "submeshIndex out of range.");
            if (material == null)
                throw new ArgumentNullException("material");
            if (bufferWithArgs == null)
                throw new ArgumentNullException("bufferWithArgs");

            Internal_DrawMeshInstancedIndirect(mesh, submeshIndex, material, bounds, bufferWithArgs, argsOffset, properties, castShadows, receiveShadows, layer, camera);
        }
    }
}


//
// MaterialPropertyBlock
//


namespace UnityEngine
{
    public sealed partial class MaterialPropertyBlock
    {
        public void SetFloat(string name, float value)          { SetFloat(Shader.PropertyToID(name), value); }
        public void SetFloat(int nameID, float value)           { SetFloatImpl(nameID, value); }
        public void SetVector(string name, Vector4 value)       { SetVector(Shader.PropertyToID(name), value); }
        public void SetVector(int nameID, Vector4 value)        { SetVectorImpl(nameID, value); }
        public void SetColor(string name, Color value)          { SetColor(Shader.PropertyToID(name), value); }
        public void SetColor(int nameID, Color value)           { SetColorImpl(nameID, value); }
        public void SetMatrix(string name, Matrix4x4 value)     { SetMatrix(Shader.PropertyToID(name), value); }
        public void SetMatrix(int nameID, Matrix4x4 value)      { SetMatrixImpl(nameID, value); }
        public void SetBuffer(string name, ComputeBuffer value) { SetBuffer(Shader.PropertyToID(name), value); }
        public void SetBuffer(int nameID, ComputeBuffer value)  { SetBufferImpl(nameID, value); }

        public void SetTexture(string name, Texture value)      { SetTexture(Shader.PropertyToID(name), value); }
        public void SetTexture(int nameID, Texture value)
        {
            if (value == null) throw new ArgumentNullException("value");
            SetTextureImpl(nameID, value);
        }

        public void SetFloatArray(string name, List<float> values) { SetFloatArray(Shader.PropertyToID(name), values); }
        public void SetFloatArray(int nameID, List<float> values)  { SetFloatArray(nameID, (float[])NoAllocHelpers.ExtractArrayFromList(values), values.Count); }
        public void SetFloatArray(string name, float[] values)     { SetFloatArray(Shader.PropertyToID(name), values); }
        public void SetFloatArray(int nameID, float[] values)      { SetFloatArray(nameID, values, values.Length); }
        private void SetFloatArray(int nameID, float[] values, int count)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Length == 0) throw new ArgumentException("Zero-sized array is not allowed.");
            if (values.Length < count) throw new ArgumentException("array has less elements than passed count.");
            SetFloatArrayImpl(nameID, values, count);
        }

        public void SetVectorArray(string name, List<Vector4> values) { SetVectorArray(Shader.PropertyToID(name), values); }
        public void SetVectorArray(int nameID, List<Vector4> values)  { SetVectorArray(nameID, (Vector4[])NoAllocHelpers.ExtractArrayFromList(values), values.Count); }
        public void SetVectorArray(string name, Vector4[] values)     { SetVectorArray(Shader.PropertyToID(name), values); }
        public void SetVectorArray(int nameID, Vector4[] values)      { SetVectorArray(nameID, values, values.Length); }
        private void SetVectorArray(int nameID, Vector4[] values, int count)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Length == 0) throw new ArgumentException("Zero-sized array is not allowed.");
            if (values.Length < count) throw new ArgumentException("array has less elements than passed count.");
            SetVectorArrayImpl(nameID, values, count);
        }

        public void SetMatrixArray(string name, List<Matrix4x4> values) { SetMatrixArray(Shader.PropertyToID(name), values); }
        public void SetMatrixArray(int nameID, List<Matrix4x4> values)  { SetMatrixArray(nameID, (Matrix4x4[])NoAllocHelpers.ExtractArrayFromList(values), values.Count); }
        public void SetMatrixArray(string name, Matrix4x4[] values)     { SetMatrixArray(Shader.PropertyToID(name), values); }
        public void SetMatrixArray(int nameID, Matrix4x4[] values)      { SetMatrixArray(nameID, values, values.Length); }
        private void SetMatrixArray(int nameID, Matrix4x4[] values, int count)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Length == 0) throw new ArgumentException("Zero-sized array is not allowed.");
            if (values.Length < count) throw new ArgumentException("array has less elements than passed count.");
            SetMatrixArrayImpl(nameID, values, count);
        }

        public float GetFloat(string name)      { return GetFloat(Shader.PropertyToID(name)); }
        public float GetFloat(int nameID)       { return GetFloatImpl(nameID); }
        public Vector4 GetVector(string name)   { return GetVector(Shader.PropertyToID(name)); }
        public Vector4 GetVector(int nameID)    { return GetVectorImpl(nameID); }
        public Color GetColor(string name)   { return GetColor(Shader.PropertyToID(name)); }
        public Color GetColor(int nameID)    { return GetColorImpl(nameID); }
        public Matrix4x4 GetMatrix(string name) { return GetMatrix(Shader.PropertyToID(name)); }
        public Matrix4x4 GetMatrix(int nameID)  { return GetMatrixImpl(nameID); }

        // List<T> version
        public void GetFloatArray(string name, List<float> values)  { GetFloatArray(Shader.PropertyToID(name), values); }
        public void GetFloatArray(int nameID, List<float> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            GetFloatArrayImplList(nameID, values);
        }

        // T[] version
        public float[] GetFloatArray(string name)   { return GetFloatArray(Shader.PropertyToID(name)); }
        public float[] GetFloatArray(int nameID)    { return GetFloatArrayImpl(nameID); }

        // List<T> version
        public void GetVectorArray(string name, List<Vector4> values)   { GetVectorArray(Shader.PropertyToID(name), values); }
        public void GetVectorArray(int nameID, List<Vector4> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            GetVectorArrayImplList(nameID, values);
        }

        // T[] version
        public Vector4[] GetVectorArray(string name) { return GetVectorArray(Shader.PropertyToID(name)); }
        public Vector4[] GetVectorArray(int nameID) { return GetVectorArrayImpl(nameID); }

        // List<T> version
        public void GetMatrixArray(string name, List<Matrix4x4> values) { GetMatrixArray(Shader.PropertyToID(name), values); }
        public void GetMatrixArray(int nameID, List<Matrix4x4> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            GetMatrixArrayImplList(nameID, values);
        }

        // T[] version
        public Matrix4x4[] GetMatrixArray(string name) { return GetMatrixArray(Shader.PropertyToID(name)); }
        public Matrix4x4[] GetMatrixArray(int nameID) { return GetMatrixArrayImpl(nameID); }

        public Texture GetTexture(string name)  { return GetTexture(Shader.PropertyToID(name)); }
        public Texture GetTexture(int nameID)   { return GetTextureImpl(nameID); }
    }
}


//
// Shader
//


namespace UnityEngine
{
    public sealed partial class Shader
    {
        public static void SetGlobalFloat(string name, float value)     { SetGlobalFloat(Shader.PropertyToID(name), value); }
        public static void SetGlobalFloat(int nameID, float value)      { SetGlobalFloatImpl(nameID, value); }
        public static void SetGlobalInt(string name, int value)         { SetGlobalInt(Shader.PropertyToID(name), value); }
        public static void SetGlobalInt(int nameID, int value)          { SetGlobalIntImpl(nameID, value); }
        public static void SetGlobalVector(string name, Vector4 value)  { SetGlobalVector(Shader.PropertyToID(name), value); }
        public static void SetGlobalVector(int nameID, Vector4 value)   { SetGlobalVectorImpl(nameID, value); }
        public static void SetGlobalColor(string name, Color value)     { SetGlobalColor(Shader.PropertyToID(name), value); }
        public static void SetGlobalColor(int nameID, Color value)      { SetGlobalColorImpl(nameID, value); }
        public static void SetGlobalMatrix(string name, Matrix4x4 value) { SetGlobalMatrix(Shader.PropertyToID(name), value); }
        public static void SetGlobalMatrix(int nameID, Matrix4x4 value) { SetGlobalMatrixImpl(nameID, value); }
        public static void SetGlobalTexture(string name, Texture value) { SetGlobalTexture(Shader.PropertyToID(name), value); }
        public static void SetGlobalTexture(int nameID, Texture value)  { SetGlobalTextureImpl(nameID, value); }
        public static void SetGlobalBuffer(string name, ComputeBuffer buffer) { SetGlobalBuffer(Shader.PropertyToID(name), buffer); }

        public static void SetGlobalFloatArray(string name, List<float> values) { SetGlobalFloatArray(Shader.PropertyToID(name), values); }
        public static void SetGlobalFloatArray(int nameID, List<float> values)  { SetGlobalFloatArray(nameID, (float[])NoAllocHelpers.ExtractArrayFromList(values), values.Count); }
        public static void SetGlobalFloatArray(string name, float[] values)     { SetGlobalFloatArray(Shader.PropertyToID(name), values); }
        public static void SetGlobalFloatArray(int nameID, float[] values)      { SetGlobalFloatArray(nameID, values, values.Length); }
        private static void SetGlobalFloatArray(int nameID, float[] values, int count)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Length == 0) throw new ArgumentException("Zero-sized array is not allowed.");
            if (values.Length < count) throw new ArgumentException("array has less elements than passed count.");
            SetGlobalFloatArrayImpl(nameID, values, count);
        }

        public static void SetGlobalVectorArray(string name, List<Vector4> values) { SetGlobalVectorArray(Shader.PropertyToID(name), values); }
        public static void SetGlobalVectorArray(int nameID, List<Vector4> values)  { SetGlobalVectorArray(nameID, (Vector4[])NoAllocHelpers.ExtractArrayFromList(values), values.Count); }
        public static void SetGlobalVectorArray(string name, Vector4[] values)     { SetGlobalVectorArray(Shader.PropertyToID(name), values); }
        public static void SetGlobalVectorArray(int nameID, Vector4[] values)      { SetGlobalVectorArray(nameID, values, values.Length); }
        private static void SetGlobalVectorArray(int nameID, Vector4[] values, int count)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Length == 0) throw new ArgumentException("Zero-sized array is not allowed.");
            if (values.Length < count) throw new ArgumentException("array has less elements than passed count.");
            SetGlobalVectorArrayImpl(nameID, values, count);
        }

        public static void SetGlobalMatrixArray(string name, List<Matrix4x4> values) { SetGlobalMatrixArray(Shader.PropertyToID(name), values); }
        public static void SetGlobalMatrixArray(int nameID, List<Matrix4x4> values)  { SetGlobalMatrixArray(nameID, (Matrix4x4[])NoAllocHelpers.ExtractArrayFromList(values), values.Count); }
        public static void SetGlobalMatrixArray(string name, Matrix4x4[] values)     { SetGlobalMatrixArray(Shader.PropertyToID(name), values); }
        public static void SetGlobalMatrixArray(int nameID, Matrix4x4[] values)      { SetGlobalMatrixArray(nameID, values, values.Length); }
        private static void SetGlobalMatrixArray(int nameID, Matrix4x4[] values, int count)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Length == 0) throw new ArgumentException("Zero-sized array is not allowed.");
            if (values.Length < count) throw new ArgumentException("array has less elements than passed count.");
            SetGlobalMatrixArrayImpl(nameID, values, count);
        }

        public static float GetGlobalFloat(string name) { return GetGlobalFloat(Shader.PropertyToID(name)); }
        public static float GetGlobalFloat(int nameID) { return GetGlobalFloatImpl(nameID); }
        public static int GetGlobalInt(string name) { return GetGlobalInt(Shader.PropertyToID(name)); }
        public static int GetGlobalInt(int nameID) { return GetGlobalIntImpl(nameID); }
        public static Vector4 GetGlobalVector(string name) { return GetGlobalVector(Shader.PropertyToID(name)); }
        public static Vector4 GetGlobalVector(int nameID) { return GetGlobalVectorImpl(nameID); }
        public static Color GetGlobalColor(string name) { return GetGlobalColor(Shader.PropertyToID(name)); }
        public static Color GetGlobalColor(int nameID) { return GetGlobalColorImpl(nameID); }
        public static Matrix4x4 GetGlobalMatrix(string name) { return GetGlobalMatrix(Shader.PropertyToID(name)); }
        public static Matrix4x4 GetGlobalMatrix(int nameID) { return GetGlobalMatrixImpl(nameID); }
        public static Texture GetGlobalTexture(string name) { return GetGlobalTexture(Shader.PropertyToID(name)); }
        public static Texture GetGlobalTexture(int nameID) { return GetGlobalTextureImpl(nameID); }

        // List<T> version
        public static void GetGlobalFloatArray(string name, List<float> values) { GetGlobalFloatArray(Shader.PropertyToID(name), values); }
        public static void GetGlobalFloatArray(int nameID, List<float> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            GetGlobalFloatArrayImplList(nameID, values);
        }

        // T[] version
        public static float[] GetGlobalFloatArray(string name) { return GetGlobalFloatArray(Shader.PropertyToID(name)); }
        public static float[] GetGlobalFloatArray(int nameID) { return GetGlobalFloatArrayImpl(nameID); }

        // List<T> version
        public static void GetGlobalVectorArray(string name, List<Vector4> values) { GetGlobalVectorArray(Shader.PropertyToID(name), values); }
        public static void GetGlobalVectorArray(int nameID, List<Vector4> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            GetGlobalVectorArrayImplList(nameID, values);
        }

        // T[] version
        public static Vector4[] GetGlobalVectorArray(string name) { return GetGlobalVectorArray(Shader.PropertyToID(name)); }
        public static Vector4[] GetGlobalVectorArray(int nameID) { return GetGlobalVectorArrayImpl(nameID); }

        // List<T> version
        public static void GetGlobalMatrixArray(string name, List<Matrix4x4> values) { GetGlobalMatrixArray(Shader.PropertyToID(name), values); }
        public static void GetGlobalMatrixArray(int nameID, List<Matrix4x4> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            GetGlobalMatrixArrayImplList(nameID, values);
        }

        // T[] version
        public static Matrix4x4[] GetGlobalMatrixArray(string name) { return GetGlobalMatrixArray(Shader.PropertyToID(name)); }
        public static Matrix4x4[] GetGlobalMatrixArray(int nameID) { return GetGlobalMatrixArrayImpl(nameID); }
    }
}


//
// Material
//


namespace UnityEngine
{
    public partial class Material
    {
        public void SetFloat(string name, float value)      { SetFloat(Shader.PropertyToID(name), value); }
        public void SetFloat(int nameID, float value)       { SetFloatImpl(nameID, value); }
        public void SetInt(string name, int value)          { SetInt(Shader.PropertyToID(name), value); }
        public void SetInt(int nameID, int value)           { SetIntImpl(nameID, value); }
        public void SetColor(string name, Color value)      { SetColor(Shader.PropertyToID(name), value); }
        public void SetColor(int nameID, Color value)       { SetColorImpl(nameID, value); }
        public void SetVector(string name, Vector4 value)   { SetVector(Shader.PropertyToID(name), value); }
        public void SetVector(int nameID, Vector4 value)    { SetVectorImpl(nameID, value); }
        public void SetMatrix(string name, Matrix4x4 value) { SetMatrix(Shader.PropertyToID(name), value); }
        public void SetMatrix(int nameID, Matrix4x4 value)  { SetMatrixImpl(nameID, value); }
        public void SetTexture(string name, Texture value)  { SetTexture(Shader.PropertyToID(name), value); }
        public void SetTexture(int nameID, Texture value)   { SetTextureImpl(nameID, value); }
        public void SetBuffer(string name, ComputeBuffer value) { SetBuffer(Shader.PropertyToID(name), value); }
        public void SetBuffer(int nameID, ComputeBuffer value) { SetBufferImpl(nameID, value); }
        public void SetTextureOffset(string name, Vector2 value) { SetTextureOffset(Shader.PropertyToID(name), value); }
        public void SetTextureOffset(int nameID, Vector2 value) { SetTextureOffsetImpl(nameID, value); }
        public void SetTextureScale(string name, Vector2 value) { SetTextureScale(Shader.PropertyToID(name), value); }
        public void SetTextureScale(int nameID, Vector2 value) { SetTextureScaleImpl(nameID, value); }


        public void SetFloatArray(string name, List<float> values)  { SetFloatArray(Shader.PropertyToID(name), values); }
        public void SetFloatArray(int nameID, List<float> values)   { SetFloatArray(nameID, (float[])NoAllocHelpers.ExtractArrayFromList(values), values.Count); }
        public void SetFloatArray(string name, float[] values)      { SetFloatArray(Shader.PropertyToID(name), values); }
        public void SetFloatArray(int nameID, float[] values)       { SetFloatArray(nameID, values, values.Length); }
        private void SetFloatArray(int nameID, float[] values, int count)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Length == 0) throw new ArgumentException("Zero-sized array is not allowed.");
            if (values.Length < count) throw new ArgumentException("array has less elements than passed count.");
            SetFloatArrayImpl(nameID, values, count);
        }

        public void SetColorArray(string name, List<Color> values)  { SetColorArray(Shader.PropertyToID(name), values); }
        public void SetColorArray(int nameID, List<Color> values)   { SetColorArray(nameID, (Color[])NoAllocHelpers.ExtractArrayFromList(values), values.Count); }
        public void SetColorArray(string name, Color[] values)      { SetColorArray(Shader.PropertyToID(name), values); }
        public void SetColorArray(int nameID, Color[] values)       { SetColorArray(nameID, values, values.Length); }
        private void SetColorArray(int nameID, Color[] values, int count)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Length == 0) throw new ArgumentException("Zero-sized array is not allowed.");
            if (values.Length < count) throw new ArgumentException("array has less elements than passed count.");
            SetColorArrayImpl(nameID, values, count);
        }

        public void SetVectorArray(string name, List<Vector4> values)   { SetVectorArray(Shader.PropertyToID(name), values); }
        public void SetVectorArray(int nameID, List<Vector4> values)    { SetVectorArray(nameID, (Vector4[])NoAllocHelpers.ExtractArrayFromList(values), values.Count); }
        public void SetVectorArray(string name, Vector4[] values)       { SetVectorArray(Shader.PropertyToID(name), values); }
        public void SetVectorArray(int nameID, Vector4[] values)        { SetVectorArray(nameID, values, values.Length); }
        private void SetVectorArray(int nameID, Vector4[] values, int count)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Length == 0) throw new ArgumentException("Zero-sized array is not allowed.");
            if (values.Length < count) throw new ArgumentException("array has less elements than passed count.");
            SetVectorArrayImpl(nameID, values, count);
        }

        public void SetMatrixArray(string name, List<Matrix4x4> values) { SetMatrixArray(Shader.PropertyToID(name), values); }
        public void SetMatrixArray(int nameID, List<Matrix4x4> values)  { SetMatrixArray(nameID, (Matrix4x4[])NoAllocHelpers.ExtractArrayFromList(values), values.Count); }
        public void SetMatrixArray(string name, Matrix4x4[] values)     { SetMatrixArray(Shader.PropertyToID(name), values); }
        public void SetMatrixArray(int nameID, Matrix4x4[] values)      { SetMatrixArray(nameID, values, values.Length); }
        private void SetMatrixArray(int nameID, Matrix4x4[] values, int count)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Length == 0) throw new ArgumentException("Zero-sized array is not allowed.");
            if (values.Length < count) throw new ArgumentException("array has less elements than passed count.");
            SetMatrixArrayImpl(nameID, values, count);
        }

        public float        GetFloat(string name)   { return GetFloat(Shader.PropertyToID(name)); }
        public float        GetFloat(int nameID)    { return GetFloatImpl(nameID); }
        public int          GetInt(string name)     { return GetInt(Shader.PropertyToID(name)); }
        public int          GetInt(int nameID)      { return GetIntImpl(nameID); }
        public Color        GetColor(string name)   { return GetColor(Shader.PropertyToID(name)); }
        public Color        GetColor(int nameID)    { return GetColorImpl(nameID); }
        public Vector4      GetVector(string name)  { return GetVector(Shader.PropertyToID(name)); }
        public Vector4      GetVector(int nameID)   { return GetVectorImpl(nameID); }
        public Matrix4x4    GetMatrix(string name)  { return GetMatrix(Shader.PropertyToID(name)); }
        public Matrix4x4    GetMatrix(int nameID)   { return GetMatrixImpl(nameID); }

        // List<T> version
        public void GetFloatArray(string name, List<float> values) { GetFloatArray(Shader.PropertyToID(name), values); }
        public void GetFloatArray(int nameID, List<float> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            GetFloatArrayImplList(nameID, values);
        }

        // T[] version
        public float[] GetFloatArray(string name) { return GetFloatArray(Shader.PropertyToID(name)); }
        public float[] GetFloatArray(int nameID) { return GetFloatArrayImpl(nameID); }

        // List<T> version
        public void GetVectorArray(string name, List<Vector4> values) { GetVectorArray(Shader.PropertyToID(name), values); }
        public void GetVectorArray(int nameID, List<Vector4> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            GetVectorArrayImplList(nameID, values);
        }

        // T[] version
        public Color[] GetColorArray(string name) { return GetColorArray(Shader.PropertyToID(name)); }
        public Color[] GetColorArray(int nameID) { return GetColorArrayImpl(nameID); }

        // List<T> version
        public void GetColorArray(string name, List<Color> values) { GetColorArray(Shader.PropertyToID(name), values); }
        public void GetColorArray(int nameID, List<Color> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            GetColorArrayImplList(nameID, values);
        }

        // T[] version
        public Vector4[] GetVectorArray(string name) { return GetVectorArray(Shader.PropertyToID(name)); }
        public Vector4[] GetVectorArray(int nameID) { return GetVectorArrayImpl(nameID); }

        // List<T> version
        public void GetMatrixArray(string name, List<Matrix4x4> values) { GetMatrixArray(Shader.PropertyToID(name), values); }
        public void GetMatrixArray(int nameID, List<Matrix4x4> values)
        {
            if (values == null)
                throw new ArgumentNullException("values");
            GetMatrixArrayImplList(nameID, values);
        }

        // T[] version
        public Matrix4x4[] GetMatrixArray(string name) { return GetMatrixArray(Shader.PropertyToID(name)); }
        public Matrix4x4[] GetMatrixArray(int nameID) { return GetMatrixArrayImpl(nameID); }

        public Texture      GetTexture(string name) { return GetTexture(Shader.PropertyToID(name)); }
        public Texture      GetTexture(int nameID)  { return GetTextureImpl(nameID); }

        public Vector2 GetTextureOffset(string name) { return GetTextureOffset(Shader.PropertyToID(name)); }
        public Vector2 GetTextureOffset(int nameID) { Vector4 st = GetTextureScaleAndOffsetImpl(nameID); return new Vector2(st.z, st.w); }
        public Vector2 GetTextureScale(string name) { return GetTextureScale(Shader.PropertyToID(name)); }
        public Vector2 GetTextureScale(int nameID) { Vector4 st = GetTextureScaleAndOffsetImpl(nameID); return new Vector2(st.x, st.y); }
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
