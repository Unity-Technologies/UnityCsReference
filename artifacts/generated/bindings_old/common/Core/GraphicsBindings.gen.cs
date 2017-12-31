// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace UnityEngine
{
internal sealed partial class NoAllocHelpers
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void Internal_ResizeList (object list, int size) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  System.Array ExtractArrayFromList (object list) ;

    
    
    public static int SafeLength(System.Array values)
        {
            return values != null ? values.Length : 0;
        }
    
    
    public static int SafeLength<T>(List<T> values)
        {
            return values != null ? values.Count : 0;
        }
    
    
}

public enum TextureCompressionQuality
{
    
    Fast = 0,
    
    Normal = 50,
    
    Best = 100
}

public static partial class RendererExtensions
{
    static public void UpdateGIMaterials(this Renderer renderer)
        {
            UpdateGIMaterialsForRenderer(renderer);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void UpdateGIMaterialsForRenderer (Renderer renderer) ;

}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
internal partial struct RenderBufferHelper
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  int GetLoadAction (out RenderBuffer b) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetLoadAction (out RenderBuffer b, int a) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  int GetStoreAction (out RenderBuffer b) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetStoreAction (out RenderBuffer b, int a) ;

    internal static IntPtr GetNativeRenderBufferPtr (IntPtr rb) {
        IntPtr result;
        INTERNAL_CALL_GetNativeRenderBufferPtr ( rb, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetNativeRenderBufferPtr (IntPtr rb, out IntPtr value);
}

public sealed partial class Graphics
{
    private static void Internal_DrawMeshNow1 (Mesh mesh, int subsetIndex, Vector3 position, Quaternion rotation) {
        INTERNAL_CALL_Internal_DrawMeshNow1 ( mesh, subsetIndex, ref position, ref rotation );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_DrawMeshNow1 (Mesh mesh, int subsetIndex, ref Vector3 position, ref Quaternion rotation);
    private static void Internal_DrawMeshNow2 (Mesh mesh, int subsetIndex, Matrix4x4 matrix) {
        INTERNAL_CALL_Internal_DrawMeshNow2 ( mesh, subsetIndex, ref matrix );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_DrawMeshNow2 (Mesh mesh, int subsetIndex, ref Matrix4x4 matrix);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void DrawProcedural (MeshTopology topology, int vertexCount, [uei.DefaultValue("1")]  int instanceCount ) ;

    [uei.ExcludeFromDocs]
    public static void DrawProcedural (MeshTopology topology, int vertexCount) {
        int instanceCount = 1;
        DrawProcedural ( topology, vertexCount, instanceCount );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void DrawProceduralIndirect (MeshTopology topology, ComputeBuffer bufferWithArgs, [uei.DefaultValue("0")]  int argsOffset ) ;

    [uei.ExcludeFromDocs]
    public static void DrawProceduralIndirect (MeshTopology topology, ComputeBuffer bufferWithArgs) {
        int argsOffset = 0;
        DrawProceduralIndirect ( topology, bufferWithArgs, argsOffset );
    }

    internal static readonly int kMaxDrawMeshInstanceCount = Internal_GetMaxDrawMeshInstanceCount();
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int Internal_GetMaxDrawMeshInstanceCount () ;

    [uei.ExcludeFromDocs]
public static void DrawTexture (Rect screenRect, Texture texture, Material mat ) {
    int pass = -1;
    DrawTexture ( screenRect, texture, mat, pass );
}

[uei.ExcludeFromDocs]
public static void DrawTexture (Rect screenRect, Texture texture) {
    int pass = -1;
    Material mat = null;
    DrawTexture ( screenRect, texture, mat, pass );
}

public static void DrawTexture(Rect screenRect, Texture texture, [uei.DefaultValue("null")]  Material mat , [uei.DefaultValue("-1")]  int pass )
        {
            DrawTexture(screenRect, texture, 0, 0, 0, 0, mat, pass);
        }

    
    
    [uei.ExcludeFromDocs]
public static void DrawTexture (Rect screenRect, Texture texture, int leftBorder, int rightBorder, int topBorder, int bottomBorder, Material mat ) {
    int pass = -1;
    DrawTexture ( screenRect, texture, leftBorder, rightBorder, topBorder, bottomBorder, mat, pass );
}

[uei.ExcludeFromDocs]
public static void DrawTexture (Rect screenRect, Texture texture, int leftBorder, int rightBorder, int topBorder, int bottomBorder) {
    int pass = -1;
    Material mat = null;
    DrawTexture ( screenRect, texture, leftBorder, rightBorder, topBorder, bottomBorder, mat, pass );
}

public static void DrawTexture(Rect screenRect, Texture texture, int leftBorder, int rightBorder, int topBorder, int bottomBorder, [uei.DefaultValue("null")]  Material mat , [uei.DefaultValue("-1")]  int pass )
        {
            DrawTexture(screenRect, texture, new Rect(0, 0, 1, 1), leftBorder, rightBorder, topBorder, bottomBorder, mat, pass);
        }

    
    
    [uei.ExcludeFromDocs]
public static void DrawTexture (Rect screenRect, Texture texture, Rect sourceRect, int leftBorder, int rightBorder, int topBorder, int bottomBorder, Material mat ) {
    int pass = -1;
    DrawTexture ( screenRect, texture, sourceRect, leftBorder, rightBorder, topBorder, bottomBorder, mat, pass );
}

[uei.ExcludeFromDocs]
public static void DrawTexture (Rect screenRect, Texture texture, Rect sourceRect, int leftBorder, int rightBorder, int topBorder, int bottomBorder) {
    int pass = -1;
    Material mat = null;
    DrawTexture ( screenRect, texture, sourceRect, leftBorder, rightBorder, topBorder, bottomBorder, mat, pass );
}

public static void DrawTexture(Rect screenRect, Texture texture, Rect sourceRect, int leftBorder, int rightBorder, int topBorder, int bottomBorder, [uei.DefaultValue("null")]  Material mat , [uei.DefaultValue("-1")]  int pass )
        {
            Color32 color = new Color32(128, 128, 128, 128);
            DrawTextureImpl(screenRect, texture, sourceRect, leftBorder, rightBorder, topBorder, bottomBorder, color, mat, pass);
        }

    
    
    [uei.ExcludeFromDocs]
public static void DrawTexture (Rect screenRect, Texture texture, Rect sourceRect, int leftBorder, int rightBorder, int topBorder, int bottomBorder, Color color, Material mat ) {
    int pass = -1;
    DrawTexture ( screenRect, texture, sourceRect, leftBorder, rightBorder, topBorder, bottomBorder, color, mat, pass );
}

[uei.ExcludeFromDocs]
public static void DrawTexture (Rect screenRect, Texture texture, Rect sourceRect, int leftBorder, int rightBorder, int topBorder, int bottomBorder, Color color) {
    int pass = -1;
    Material mat = null;
    DrawTexture ( screenRect, texture, sourceRect, leftBorder, rightBorder, topBorder, bottomBorder, color, mat, pass );
}

public static void DrawTexture(Rect screenRect, Texture texture, Rect sourceRect, int leftBorder, int rightBorder, int topBorder, int bottomBorder, Color color, [uei.DefaultValue("null")]  Material mat , [uei.DefaultValue("-1")]  int pass )
        {
            DrawTextureImpl(screenRect, texture, sourceRect, leftBorder, rightBorder, topBorder, bottomBorder, color, mat, pass);
        }

    
    
    [VisibleToOtherModules("UnityEngine.IMGUIModule")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void Internal_DrawTexture (ref Internal_DrawTextureArguments args) ;

    [uei.ExcludeFromDocs]
public static GPUFence CreateGPUFence () {
    SynchronisationStage stage = SynchronisationStage.PixelProcessing;
    return CreateGPUFence ( stage );
}

public static GPUFence CreateGPUFence( [uei.DefaultValue("SynchronisationStage.PixelProcessing")] SynchronisationStage stage )
        {
            GPUFence newFence = new GPUFence();
            newFence.m_Ptr = Internal_CreateGPUFence(stage);
            newFence.InitPostAllocation();
            newFence.Validate();
            return newFence;

        }

    
    
    private static IntPtr Internal_CreateGPUFence (SynchronisationStage stage) {
        IntPtr result;
        INTERNAL_CALL_Internal_CreateGPUFence ( stage, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_CreateGPUFence (SynchronisationStage stage, out IntPtr value);
    [uei.ExcludeFromDocs]
public static void WaitOnGPUFence (GPUFence fence) {
    SynchronisationStage stage = SynchronisationStage.VertexProcessing;
    WaitOnGPUFence ( fence, stage );
}

public static void WaitOnGPUFence(GPUFence fence, [uei.DefaultValue("SynchronisationStage.VertexProcessing")]  SynchronisationStage stage )
        {
            fence.Validate();

            if (fence.IsFencePending())
                WaitOnGPUFence_Internal(fence.m_Ptr, stage);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void WaitOnGPUFence_Internal (IntPtr fencePtr, SynchronisationStage stage) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ExecuteCommandBuffer (UnityEngine.Rendering.CommandBuffer buffer) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ExecuteCommandBufferAsync (UnityEngine.Rendering.CommandBuffer buffer, ComputeQueueType queueType) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SetNullRT () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SetRTSimple (out RenderBuffer color, out RenderBuffer depth, int mip, CubemapFace face, int depthSlice) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SetMRTFullSetup (
            RenderBuffer[] colorSA, out RenderBuffer depth, int mip, CubemapFace face, int depthSlice,
            Rendering.RenderBufferLoadAction[] colorLoadSA, Rendering.RenderBufferStoreAction[] colorStoreSA,
            Rendering.RenderBufferLoadAction depthLoad, Rendering.RenderBufferStoreAction depthStore
            ) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SetMRTSimple (RenderBuffer[] colorSA, out RenderBuffer depth, int mip, CubemapFace face, int depthSlice) ;

    public static RenderBuffer activeColorBuffer { get { RenderBuffer res; GetActiveColorBuffer(out res); return res; } }
    
    
    public static RenderBuffer activeDepthBuffer { get { RenderBuffer res; GetActiveDepthBuffer(out res); return res; } }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void GetActiveColorBuffer (out RenderBuffer res) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void GetActiveDepthBuffer (out RenderBuffer res) ;

    public static void SetRandomWriteTarget(int index, RenderTexture uav)
        {
            Internal_SetRandomWriteTargetRT(index, uav);
        }
    
    
    [uei.ExcludeFromDocs]
public static void SetRandomWriteTarget (int index, ComputeBuffer uav) {
    bool preserveCounterValue = false;
    SetRandomWriteTarget ( index, uav, preserveCounterValue );
}

public static void SetRandomWriteTarget(int index, ComputeBuffer uav, [uei.DefaultValue("false")]  bool preserveCounterValue )
        {
            if (uav == null) throw new ArgumentNullException("uav");
            if (uav.m_Ptr == IntPtr.Zero) throw new System.ObjectDisposedException("uav");

            Internal_SetRandomWriteTargetBuffer(index, uav, preserveCounterValue);
        }

    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ClearRandomWriteTargets () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SetRandomWriteTargetRT (int index, RenderTexture uav) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SetRandomWriteTargetBuffer (int index, ComputeBuffer uav, bool preserveCounterValue) ;

    public extern static GraphicsTier activeTier
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static ColorGamut activeColorGamut
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

}

public sealed partial class LightProbes : Object
{
    private LightProbes() {}
    
    
    public static void GetInterpolatedProbe (Vector3 position, Renderer renderer, out SphericalHarmonicsL2 probe) {
        INTERNAL_CALL_GetInterpolatedProbe ( ref position, renderer, out probe );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetInterpolatedProbe (ref Vector3 position, Renderer renderer, out SphericalHarmonicsL2 probe);
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

            Internal_CalculateInterpolatedLightAndOcclusionProbes(positions, positions.Length, lightProbes, occlusionProbes);
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

            Internal_CalculateInterpolatedLightAndOcclusionProbes(NoAllocHelpers.ExtractArrayFromListT(positions), positions.Count, NoAllocHelpers.ExtractArrayFromListT(lightProbes), NoAllocHelpers.ExtractArrayFromListT(occlusionProbes));
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void Internal_CalculateInterpolatedLightAndOcclusionProbes (Vector3[] positions, int count, SphericalHarmonicsL2[] lightProbes, Vector4[] occlusionProbes) ;

    public extern  Vector3[] positions
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  UnityEngine.Rendering.SphericalHarmonicsL2[] bakedProbes
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  int count
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  int cellCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool AreLightProbesAllowed (Renderer renderer) ;

}

public sealed partial class LightmapSettings : Object
{
    private LightmapSettings() {}
    
    
    public extern static LightmapData[] lightmaps
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static LightmapsMode lightmapsMode
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static LightProbes lightProbes
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void Reset () ;

}


}

namespace UnityEditor.Experimental
{
public sealed partial class RenderSettings
{
    public extern static bool useRadianceAmbientProbe
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

}

}
