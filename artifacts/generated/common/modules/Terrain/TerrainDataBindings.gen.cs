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
using System.Collections.Generic;

namespace UnityEngine
{


[StructLayout(LayoutKind.Sequential)]
[UsedByNativeCode]
public sealed partial class TreePrototype
{
    internal GameObject m_Prefab;
    internal float m_BendFactor;
    
    
    public GameObject prefab { get { return m_Prefab; } set { m_Prefab = value; } }
    
    
    public float bendFactor { get { return m_BendFactor; } set { m_BendFactor = value; } }
}

public enum DetailRenderMode
{
    GrassBillboard = 0,
    VertexLit = 1,
    Grass = 2
}

[StructLayout(LayoutKind.Sequential)]
[UsedByNativeCode]
public sealed partial class DetailPrototype
{
    GameObject    m_Prototype = null;
    Texture2D     m_PrototypeTexture = null;
    Color         m_HealthyColor = new Color(67 / 255F, 249 / 255F, 42 / 255F, 1);
    Color         m_DryColor = new Color(205 / 255.0F, 188 / 255.0F, 26 / 255.0F, 1.0F);
    float         m_MinWidth = 1.0F;
    float         m_MaxWidth = 2.0F;
    float         m_MinHeight = 1F;
    float         m_MaxHeight = 2F;
    float         m_NoiseSpread = 0.1F;
    float         m_BendFactor = 0.1F;
    int           m_RenderMode = 2;
    int           m_UsePrototypeMesh = 0;
    
    
    public GameObject prototype { get { return m_Prototype; } set  { m_Prototype = value; } }
    
    
    public Texture2D prototypeTexture { get { return m_PrototypeTexture; } set { m_PrototypeTexture = value; } }
    
    
    public float minWidth { get { return m_MinWidth; } set { m_MinWidth = value; } }
    
    
    public float maxWidth { get { return m_MaxWidth; } set { m_MaxWidth = value; } }
    
    
    public float minHeight { get { return m_MinHeight; } set { m_MinHeight = value; } }
    
    
    public float maxHeight { get { return m_MaxHeight; } set { m_MaxHeight = value; } }
    
    
    public float noiseSpread { get { return m_NoiseSpread; } set { m_NoiseSpread = value; } }
    
    
    public float bendFactor { get { return m_BendFactor; } set { m_BendFactor = value; } }
    
    
    public Color healthyColor { get  { return m_HealthyColor; } set { m_HealthyColor = value; } }
    
    
    public Color dryColor { get  { return m_DryColor; } set { m_DryColor = value; } }
    
    
    public DetailRenderMode renderMode { get { return (DetailRenderMode)m_RenderMode; } set { m_RenderMode = (int)value; } }
    
    
    public bool usePrototypeMesh { get { return m_UsePrototypeMesh != 0; } set { m_UsePrototypeMesh = value ? 1 : 0; } }
}

[StructLayout(LayoutKind.Sequential)]
[UsedByNativeCode]
public sealed partial class SplatPrototype
{
    internal Texture2D m_Texture;
    internal Texture2D m_NormalMap;
    internal Vector2 m_TileSize = new Vector2(15, 15);
    internal Vector2 m_TileOffset = new Vector2(0, 0);
    internal Vector4 m_SpecularMetallic = new Vector4(0, 0, 0, 0);
    internal float m_Smoothness = 0.0f;
    
    
    public Texture2D texture { get { return m_Texture; } set { m_Texture = value; } }
    
    
    public Texture2D normalMap { get { return m_NormalMap; } set { m_NormalMap = value; } }
    
    
    public Vector2 tileSize { get { return m_TileSize; } set { m_TileSize = value; } }
    
    
    public Vector2 tileOffset { get { return m_TileOffset; } set { m_TileOffset = value; } }
    
    
    public Color specular { get { return new Color(m_SpecularMetallic.x, m_SpecularMetallic.y, m_SpecularMetallic.z); } set { m_SpecularMetallic.x = value.r; m_SpecularMetallic.y = value.g; m_SpecularMetallic.z = value.b; } }
    
    
    public float metallic { get { return m_SpecularMetallic.w; } set { m_SpecularMetallic.w = value; } }
    
    
    public float smoothness { get { return m_Smoothness; } set { m_Smoothness = value; } }
}

[UsedByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct TreeInstance
{
    public Vector3    position;
    public float      widthScale;
    public float      heightScale;
    public float      rotation;
    public Color32    color;
    public Color32    lightmapColor;
    public int        prototypeIndex;
    
    
    internal float    temporaryDistance;
}

public sealed partial class TerrainData : Object
{
    private static readonly int kMaximumResolution = Internal_GetMaximumResolution();
    private static readonly int kMinimumDetailResolutionPerPatch = Internal_GetMinimumDetailResolutionPerPatch();
    private static readonly int kMaximumDetailResolutionPerPatch = Internal_GetMaximumDetailResolutionPerPatch();
    private static readonly int kMaximumDetailPatchCount = Internal_GetMaximumDetailPatchCount();
    private static readonly int kMinimumAlphamapResolution = Internal_GetMinimumAlphamapResolution();
    private static readonly int kMaximumAlphamapResolution = Internal_GetMaximumAlphamapResolution();
    private static readonly int kMinimumBaseMapResolution = Internal_GetMinimumBaseMapResolution();
    private static readonly int kMaximumBaseMapResolution = Internal_GetMaximumBaseMapResolution();
    
    
    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int Internal_GetMaximumResolution () ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int Internal_GetMinimumDetailResolutionPerPatch () ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int Internal_GetMaximumDetailResolutionPerPatch () ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int Internal_GetMaximumDetailPatchCount () ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int Internal_GetMinimumAlphamapResolution () ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int Internal_GetMaximumAlphamapResolution () ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int Internal_GetMinimumBaseMapResolution () ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int Internal_GetMaximumBaseMapResolution () ;

    public TerrainData()
        {
            Internal_Create(this);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void Internal_Create ([Writable] TerrainData terrainData) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal bool HasUser (GameObject user) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void AddUser (GameObject user) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void RemoveUser (GameObject user) ;

    public extern  int heightmapWidth
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  int heightmapHeight
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public int heightmapResolution
        {
            get { return Internal_heightmapResolution; }
            set
            {
                int clamped = value;
                if (value < 0 || value > kMaximumResolution)
                {
                    Debug.LogWarning("heightmapResolution is clamped to the range of [0, " + kMaximumResolution + "].");
                    clamped = Math.Min(kMaximumResolution, Math.Max(value, 0));
                }

                Internal_heightmapResolution = clamped;
            }
        }
    private extern  int Internal_heightmapResolution
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public  Vector3 heightmapScale
    {
        get { Vector3 tmp; INTERNAL_get_heightmapScale(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_heightmapScale (out Vector3 value) ;


    public  Vector3 size
    {
        get { Vector3 tmp; INTERNAL_get_size(out tmp); return tmp;  }
        set { INTERNAL_set_size(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_size (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_set_size (ref Vector3 value) ;

    public  Bounds bounds
    {
        get { Bounds tmp; INTERNAL_get_bounds(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_bounds (out Bounds value) ;


    public extern  float thickness
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
    extern public float GetHeight (int x, int y) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public float GetInterpolatedHeight (float x, float y) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public float[,] GetHeights (int xBase, int yBase, int width, int height) ;

    public void SetHeights(int xBase, int yBase, float[,] heights)
        {
            if (heights == null)
            {
                throw new System.NullReferenceException();
            }
            if (xBase + heights.GetLength(1) > heightmapWidth || xBase + heights.GetLength(1) < 0 || yBase + heights.GetLength(0) < 0 || xBase < 0 || yBase < 0 || yBase + heights.GetLength(0) > heightmapHeight)
            {
                throw new System.ArgumentException(UnityString.Format("X or Y base out of bounds. Setting up to {0}x{1} while map size is {2}x{3}", xBase + heights.GetLength(1), yBase + heights.GetLength(0), heightmapWidth, heightmapHeight));
            }

            Internal_SetHeights(xBase, yBase, heights.GetLength(1), heights.GetLength(0), heights);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_SetHeights (int xBase, int yBase, int width, int height, float[,] heights) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_SetHeightsDelayLOD (int xBase, int yBase, int width, int height, float[,] heights) ;

    public void SetHeightsDelayLOD(int xBase, int yBase, float[,] heights)
        {
            if (heights == null) throw new System.ArgumentNullException("heights");

            int height = heights.GetLength(0);
            int width = heights.GetLength(1);

            if (xBase < 0 || (xBase + width) < 0 || (xBase + width) > heightmapWidth)
                throw new System.ArgumentException(UnityString.Format("X out of bounds - trying to set {0}-{1} but the terrain ranges from 0-{2}", xBase, xBase + width, heightmapWidth));

            if (yBase < 0 || (yBase + height) < 0 || (yBase + height) > heightmapHeight)
                throw new System.ArgumentException(UnityString.Format("Y out of bounds - trying to set {0}-{1} but the terrain ranges from 0-{2}", yBase, yBase + height, heightmapHeight));

            Internal_SetHeightsDelayLOD(xBase, yBase, width, height, heights);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public float GetSteepness (float x, float y) ;

    public Vector3 GetInterpolatedNormal (float x, float y) {
        Vector3 result;
        INTERNAL_CALL_GetInterpolatedNormal ( this, x, y, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetInterpolatedNormal (TerrainData self, float x, float y, out Vector3 value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal int GetAdjustedSize (int size) ;

    public extern  float wavingGrassStrength
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  float wavingGrassAmount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern  float wavingGrassSpeed
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public  Color wavingGrassTint
    {
        get { Color tmp; INTERNAL_get_wavingGrassTint(out tmp); return tmp;  }
        set { INTERNAL_set_wavingGrassTint(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_wavingGrassTint (out Color value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_set_wavingGrassTint (ref Color value) ;

    public extern  int detailWidth
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  int detailHeight
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public void SetDetailResolution(int detailResolution, int resolutionPerPatch)
        {
            if (detailResolution < 0)
            {
                Debug.LogWarning("detailResolution must not be negative.");
                detailResolution = 0;
            }

            if (resolutionPerPatch < kMinimumDetailResolutionPerPatch || resolutionPerPatch > kMaximumDetailResolutionPerPatch)
            {
                Debug.LogWarning("resolutionPerPatch is clamped to the range of [" + kMinimumDetailResolutionPerPatch + ", " + kMaximumDetailResolutionPerPatch + "].");
                resolutionPerPatch = Math.Min(kMaximumDetailResolutionPerPatch, Math.Max(resolutionPerPatch, kMinimumDetailResolutionPerPatch));
            }

            int patchCount = detailResolution / resolutionPerPatch;
            if (patchCount > kMaximumDetailPatchCount)
            {
                Debug.LogWarning("Patch count (detailResolution / resolutionPerPatch) is clamped to the range of [0, " + kMaximumDetailPatchCount + "].");
                patchCount = Math.Min(kMaximumDetailPatchCount, Math.Max(patchCount, 0));
            }

            Internal_SetDetailResolution(patchCount, resolutionPerPatch);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_SetDetailResolution (int patchCount, int resolutionPerPatch) ;

    public extern  int detailResolution
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    internal extern  int detailResolutionPerPatch
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void ResetDirtyDetails () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void RefreshPrototypes () ;

    public extern  DetailPrototype[] detailPrototypes
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
    extern public int[] GetSupportedLayers (int xBase, int yBase, int totalWidth, int totalHeight) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public int[,] GetDetailLayer (int xBase, int yBase, int width, int height, int layer) ;

    public void SetDetailLayer(int xBase, int yBase, int layer, int[,] details)
        {
            Internal_SetDetailLayer(xBase, yBase, details.GetLength(1), details.GetLength(0), layer, details);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_SetDetailLayer (int xBase, int yBase, int totalWidth, int totalHeight, int detailIndex, int[,] data) ;

    public extern  TreeInstance[] treeInstances
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public TreeInstance GetTreeInstance (int index) {
        TreeInstance result;
        INTERNAL_CALL_GetTreeInstance ( this, index, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetTreeInstance (TerrainData self, int index, out TreeInstance value);
    public void SetTreeInstance (int index, TreeInstance instance) {
        INTERNAL_CALL_SetTreeInstance ( this, index, ref instance );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetTreeInstance (TerrainData self, int index, ref TreeInstance instance);
    public extern  int treeInstanceCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern  TreePrototype[] treePrototypes
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
    extern internal void RemoveTreePrototype (int index) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void RecalculateTreePositions () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void RemoveDetailPrototype (int index) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal bool NeedUpgradeScaledTreePrototypes () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void UpgradeScaledTreePrototype () ;

    public extern  int alphamapLayers
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public float[,,] GetAlphamaps (int x, int y, int width, int height) ;

    public int alphamapResolution
        {
            get { return Internal_alphamapResolution; }
            set
            {
                int clamped = value;
                if (value < kMinimumAlphamapResolution || value > kMaximumAlphamapResolution)
                {
                    Debug.LogWarning("alphamapResolution is clamped to the range of [" + kMinimumAlphamapResolution + ", " + kMaximumAlphamapResolution + "].");
                    clamped = Math.Min(kMaximumAlphamapResolution, Math.Max(value, kMinimumAlphamapResolution));
                }

                Internal_alphamapResolution = clamped;
            }
        }
    
    
    [RequiredByNativeCode] 
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal float GetAlphamapResolutionInternal () ;

    private extern  int Internal_alphamapResolution
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public int alphamapWidth { get { return alphamapResolution; } }
    
    
    public int alphamapHeight { get { return alphamapResolution; } }
    
    
    public int baseMapResolution
        {
            get { return Internal_baseMapResolution; }
            set
            {
                int clamped = value;
                if (value < kMinimumBaseMapResolution || value > kMaximumBaseMapResolution)
                {
                    Debug.LogWarning("baseMapResolution is clamped to the range of [" + kMinimumBaseMapResolution + ", " + kMaximumBaseMapResolution + "].");
                    clamped = Math.Min(kMaximumBaseMapResolution, Math.Max(value, kMinimumBaseMapResolution));
                }

                Internal_baseMapResolution = clamped;
            }
        }
    private extern  int Internal_baseMapResolution
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public void SetAlphamaps(int x, int y, float[,,] map)
        {
            if (map.GetLength(2) != alphamapLayers)
            {
                throw new System.Exception(UnityString.Format("Float array size wrong (layers should be {0})", alphamapLayers));
            }

            Internal_SetAlphamaps(x, y, map.GetLength(1), map.GetLength(0), map);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_SetAlphamaps (int x, int y, int width, int height, float[,,] map) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void RecalculateBasemapIfDirty () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void SetBasemapDirty (bool dirty) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private Texture2D GetAlphamapTexture (int index) ;

    private extern  int alphamapTextureCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public Texture2D[] alphamapTextures
        {
            get
            {
                Texture2D[] splatTextures = new Texture2D[alphamapTextureCount];
                for (int i = 0; i < splatTextures.Length; i++)
                    splatTextures[i] =  GetAlphamapTexture(i);
                return splatTextures;
            }
        }
    
    
    public extern  SplatPrototype[] splatPrototypes
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
    extern internal void AddTree (out TreeInstance tree) ;

    internal int RemoveTrees (Vector2 position, float radius, int prototypeIndex) {
        return INTERNAL_CALL_RemoveTrees ( this, ref position, radius, prototypeIndex );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_RemoveTrees (TerrainData self, ref Vector2 position, float radius, int prototypeIndex);
}


}
