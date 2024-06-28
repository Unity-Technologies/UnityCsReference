// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEngine
{
    // List of changes done to the terrain for OnTerrainChanged
    // OnTerrainChanged is called with a bitfield of these items telling it what was changed.
    [Flags]
    public enum TerrainChangedFlags
    {
        Heightmap = 1,
        TreeInstances = 2,
        DelayedHeightmapUpdate = 4,
        FlushEverythingImmediately = 8,
        RemoveDirtyDetailsImmediately = 16,
        HeightmapResolution = 32,
        Holes = 64,
        DelayedHolesUpdate = 128,
        WillBeDestroyed = 256,
    }

    [Flags]
    public enum TerrainRenderFlags
    {
        [Obsolete("TerrainRenderFlags.heightmap is obsolete, use TerrainRenderFlags.Heightmap instead. (UnityUpgradable) -> Heightmap")]
        heightmap = 1,

        [Obsolete("TerrainRenderFlags.trees is obsolete, use TerrainRenderFlags.Trees instead. (UnityUpgradable) -> Trees")]
        trees = 2,

        [Obsolete("TerrainRenderFlags.details is obsolete, use TerrainRenderFlags.Details instead. (UnityUpgradable) -> Details")]
        details = 4,

        [Obsolete("TerrainRenderFlags.all is obsolete, use TerrainRenderFlags.All instead. (UnityUpgradable) -> All")]
        all = All,

        Heightmap = 1,
        Trees = 2,
        Details = 4,
        All = Heightmap | Trees | Details
    }

    [UsedByNativeCode]
    [NativeHeader("Modules/Terrain/Public/Terrain.h")]
    [NativeHeader("Runtime/Interfaces/ITerrainManager.h")]
    [NativeHeader("TerrainScriptingClasses.h")]
    [StaticAccessor("GetITerrainManager()", StaticAccessorType.Arrow)]
    public sealed partial class Terrain : Behaviour
    {
        extern public TerrainData terrainData { get; set; }

        extern public float treeDistance { get; set; }

        extern public float treeBillboardDistance { get; set; }

        extern public float treeCrossFadeLength { get; set; }

        extern public int treeMaximumFullLODCount { get; set; }

        extern public float detailObjectDistance { get; set; }

        extern public float detailObjectDensity { get; set; }

        extern public float heightmapPixelError { get; set; }
        extern public int heightmapMaximumLOD { get; set; }
        extern public int heightmapMinimumLODSimplification { get; set; }

        extern public float basemapDistance { get; set; }

        [NativeProperty("StaticLightmapIndexInt")]
        extern public int lightmapIndex { get; set; }

        [NativeProperty("DynamicLightmapIndexInt")]
        extern public int realtimeLightmapIndex { get; set; }

        [NativeProperty("StaticLightmapST")]
        extern public Vector4 lightmapScaleOffset { get; set; }

        [NativeProperty("DynamicLightmapST")]
        extern public Vector4 realtimeLightmapScaleOffset { get; set; }

        [Obsolete("Terrain.freeUnusedRenderingResources is obsolete; use keepUnusedRenderingResources instead.")]
        [NativeProperty("FreeUnusedRenderingResourcesObsolete")]
        extern public bool freeUnusedRenderingResources { get; set; }

        [NativeProperty("KeepUnusedRenderingResources")]
        extern public bool keepUnusedRenderingResources { get; set; }

        extern public bool GetKeepUnusedCameraRenderingResources(int cameraInstanceID);
        extern public void SetKeepUnusedCameraRenderingResources(int cameraInstanceID, bool keepUnused);

        extern public ShadowCastingMode shadowCastingMode { get; set; }

        extern public ReflectionProbeUsage reflectionProbeUsage { get; set; }

        extern public void GetClosestReflectionProbes(List<ReflectionProbeBlendInfo> result);

        extern public Material materialTemplate { get; set; }

        extern public bool drawHeightmap { get; set; }
        extern public bool allowAutoConnect { get; set; }
        extern public int groupingID { get; set; }

        extern public bool drawInstanced { get; set; }

        extern public bool enableHeightmapRayTracing { get; set; }

        extern public RenderTexture normalmapTexture { [NativeMethod("TryGetNormalMapTexture")] get; }

        extern public bool drawTreesAndFoliage { get; set; }

        extern public Vector3 patchBoundsMultiplier { get; set; }

        extern public float SampleHeight(Vector3 worldPosition);

        extern public void AddTreeInstance(TreeInstance instance);

        extern public void SetNeighbors(Terrain left, Terrain top, Terrain right, Terrain bottom);

        extern public float treeLODBiasMultiplier { get; set; }

        extern public bool collectDetailPatches { get; set; }

        extern public bool ignoreQualitySettings { get; set; }

        extern public TerrainRenderFlags editorRenderFlags { get; set; }

        extern public Vector3 GetPosition();

        extern public void Flush();

        extern internal void RemoveTrees(Vector2 position, float radius, int prototypeIndex);

        [NativeMethod("CopySplatMaterialCustomProps")]
        extern public void SetSplatMaterialPropertyBlock(MaterialPropertyBlock properties);

        public void GetSplatMaterialPropertyBlock(MaterialPropertyBlock dest)
        {
            if (dest == null)
                throw new ArgumentNullException("dest");

            Internal_GetSplatMaterialPropertyBlock(dest);
        }

        [NativeMethod("GetSplatMaterialCustomProps")]
        extern private void Internal_GetSplatMaterialPropertyBlock(MaterialPropertyBlock dest);

        extern public bool bakeLightProbesForTrees { get; set; }

        extern public bool deringLightProbesForTrees { get; set; }

        extern public TreeMotionVectorModeOverride treeMotionVectorModeOverride { get; set; }

        extern public bool preserveTreePrototypeLayers { get; set; }

        [StaticAccessor("Terrain", StaticAccessorType.DoubleColon)]
        extern static public GraphicsFormat heightmapFormat { get; }

        static public TextureFormat heightmapTextureFormat
        {
            get { return GraphicsFormatUtility.GetTextureFormat(heightmapFormat); }
        }

        static public RenderTextureFormat heightmapRenderTextureFormat
        {
            get { return GraphicsFormatUtility.GetRenderTextureFormat(heightmapFormat); }
        }

        [StaticAccessor("Terrain", StaticAccessorType.DoubleColon)]
        extern static public GraphicsFormat normalmapFormat { get; }

        static public TextureFormat normalmapTextureFormat
        {
            get { return GraphicsFormatUtility.GetTextureFormat(normalmapFormat); }
        }

        static public RenderTextureFormat normalmapRenderTextureFormat
        {
            get { return GraphicsFormatUtility.GetRenderTextureFormat(normalmapFormat); }
        }

        [StaticAccessor("Terrain", StaticAccessorType.DoubleColon)]
        extern static public GraphicsFormat holesFormat { get; }

        static public RenderTextureFormat holesRenderTextureFormat
        {
            get { return GraphicsFormatUtility.GetRenderTextureFormat(holesFormat); }
        }

        [StaticAccessor("Terrain", StaticAccessorType.DoubleColon)]
        extern static public GraphicsFormat compressedHolesFormat { get; }

        static public TextureFormat compressedHolesTextureFormat
        {
            get { return GraphicsFormatUtility.GetTextureFormat(compressedHolesFormat); }
        }

        extern public static Terrain activeTerrain { get; }
        extern public static void SetConnectivityDirty();

        [NativeProperty("ActiveTerrainsScriptingArray")]
        extern public static Terrain[] activeTerrains { [return:Unmarshalled] get; }

        public static void GetActiveTerrains(List<Terrain> terrainList)
        {
            Internal_FillActiveTerrainList(terrainList);
        }

        extern private static void Internal_FillActiveTerrainList([NotNull] object terrainList);

        [UsedByNativeCode]
        extern public static GameObject CreateTerrainGameObject(TerrainData assignTerrain);

        extern public Terrain leftNeighbor { get; }
        extern public Terrain rightNeighbor { get; }
        extern public Terrain topNeighbor { get; }
        extern public Terrain bottomNeighbor { get; }

        extern public UInt32 renderingLayerMask { get; set; }
    }

    public static partial class TerrainExtensions
    {
        public static void UpdateGIMaterials(this Terrain terrain)
        {
            if (terrain.terrainData == null)
                throw new ArgumentException("Invalid terrainData.");

            UpdateGIMaterialsForTerrain(terrain.GetInstanceID(), new Rect(0, 0, 1, 1));
        }

        public static void UpdateGIMaterials(this Terrain terrain, int x, int y, int width, int height)
        {
            if (terrain.terrainData == null)
                throw new ArgumentException("Invalid terrainData.");

            float alphamapWidth = terrain.terrainData.alphamapWidth;
            float alphamapHeight = terrain.terrainData.alphamapHeight;
            UpdateGIMaterialsForTerrain(terrain.GetInstanceID(), new Rect(x / alphamapWidth, y / alphamapHeight, width / alphamapWidth, height / alphamapHeight));
        }

        [FreeFunction]
        [NativeConditional("INCLUDE_DYNAMIC_GI && ENABLE_RUNTIME_GI")]
        extern internal static void UpdateGIMaterialsForTerrain(int terrainInstanceID, Rect uvBounds);
    }

    [NativeHeader("Modules/Terrain/Public/Tree.h")]
    [ExcludeFromPreset]
    public sealed partial class Tree : Component
    {
        [NativeProperty("TreeData")]
        extern public ScriptableObject data { get; set; }

        extern public bool hasSpeedTreeWind
        {
            [NativeMethod("HasSpeedTreeWind")]
            get;
        }

        [NativeProperty("SpeedTreeWindAsset")]
        extern public SpeedTreeWindAsset windAsset
        {
            [NativeMethod("GetSpeedTreeWind")]
            get;
            [NativeMethod("SetSpeedTreeWind")]
            set;
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct SpeedTreeWindConfig9
    {
        public float strengthResponse;
        public float directionResponse;

        public float gustFrequency;
        public float gustStrengthMin;
        public float gustStrengthMax;
        public float gustDurationMin;
        public float gustDurationMax;
        public float gustRiseScalar;
        public float gustFallScalar;

        // branch stretch limits + shared height start
        public float branch1StretchLimit;
        public float branch2StretchLimit;

        // BranchWindLevel: Shared
        public float       sharedHeightStart;
        public fixed float bendShared[20];
        public fixed float oscillationShared[20];
        public fixed float speedShared[20];
        public fixed float turbulenceShared[20];
        public fixed float flexibilityShared[20];
        public float independenceShared;

        // BranchWindLevel: Branch1
        //BranchWindLevel m_sBranch1;
        public fixed float bendBranch1[20];
        public fixed float oscillationBranch1[20];
        public fixed float speedBranch1[20];
        public fixed float turbulenceBranch1[20];
        public fixed float flexibilityBranch1[20];
        public float independenceBranch1;

        //BranchWindLevel m_sBranch2;
        public fixed float bendBranch2[20];
        public fixed float oscillationBranch2[20];
        public fixed float speedBranch2[20];
        public fixed float turbulenceBranch2[20];
        public fixed float flexibilityBranch2[20];
        public float independenceBranch2;

        //RippleGroup m_sRipple;
        public fixed float planarRipple[20];
        public fixed float directionalRipple[20];
        public fixed float speedRipple[20];
        public fixed float flexibilityRipple[20];
        public float independenceRipple;
        public float shimmerRipple;

        public float treeExtentX;
        public float treeExtentY;
        public float treeExtentZ;

        public float windIndependence;
        public int doShared;
        public int doBranch1;
        public int doBranch2;
        public int doRipple;
        public int doShimmer;
        public int lodFade;
        public float importScale;

        public SpeedTreeWindConfig9()
        {
            // defaults from SpeedTree SDK example headers
            strengthResponse    = 5.0f;
            directionResponse   = 2.5f;
            gustFrequency       = 0.0f;
            gustStrengthMin     = 0.5f;
            gustStrengthMax     = 1.0f;
            gustDurationMin     = 1.0f;
            gustDurationMax     = 4.0f;
            gustRiseScalar      = 1.0f;
            gustFallScalar      = 1.0f;

            branch1StretchLimit = 1.0f;
            branch2StretchLimit = 1.0f;
            sharedHeightStart   = 0.0f;
            independenceShared  = 0.0f;
            independenceBranch1 = 0.0f;
            independenceBranch2 = 0.0f;
            independenceRipple  = 0.0f;
            shimmerRipple       = 0.0f;
            windIndependence    = 0.0f;
            treeExtentX         = 0.0f;
            treeExtentY         = 0.0f;
            treeExtentZ         = 0.0f;

            doShared            = 0 /*false */;
            doBranch1           = 0 /*false */;
            doBranch2           = 0 /*false */;
            doRipple            = 0 /*false */;
            doShimmer           = 0 /*false */;
            lodFade             = 0 /*false */;
            importScale         = 1.0f;
        }

        public readonly bool IsWindEnabled => (doShared != 0 || doBranch1 != 0 || doBranch2 != 0 || doRipple != 0);

        static public byte[] Serialize(SpeedTreeWindConfig9 config)
        {
            int size = Marshal.SizeOf(config);
            byte[] data = new byte[size];
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject();
                Marshal.StructureToPtr(config, ptr, false);
            }
            finally
            {
                handle.Free();
            }
            return data;
        }
    };

    [NativeHeader("Modules/Terrain/Public/SpeedTreeWind.h")]
    [ExcludeFromPreset] // ?
    public partial class SpeedTreeWindAsset : Object
    {
        extern public int Version { get; set; }

        internal SpeedTreeWindAsset(int version, SpeedTreeWindConfig9 config)
        {
            Internal_Create(this, version, SpeedTreeWindConfig9.Serialize(config));
        }

        [NativeThrows]
        static extern void Internal_Create([Writable] SpeedTreeWindAsset notSelf, int version, byte[] data);
    }
}
