// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

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
        internal GameObject m_Prototype = null;
        internal Texture2D m_PrototypeTexture = null;
        internal Color m_HealthyColor = new Color(67 / 255F, 249 / 255F, 42 / 255F, 1);
        internal Color m_DryColor = new Color(205 / 255.0F, 188 / 255.0F, 26 / 255.0F, 1.0F);
        internal float m_MinWidth = 1.0F;
        internal float m_MaxWidth = 2.0F;
        internal float m_MinHeight = 1F;
        internal float m_MaxHeight = 2F;
        internal float m_NoiseSpread = 0.1F;
        internal float m_BendFactor = 0.1F;
        internal int m_RenderMode = 2;
        internal int m_UsePrototypeMesh = 0;

        public GameObject prototype { get { return m_Prototype; } set { m_Prototype = value; } }

        public Texture2D prototypeTexture { get { return m_PrototypeTexture; } set { m_PrototypeTexture = value; } }

        public float minWidth { get { return m_MinWidth; } set { m_MinWidth = value; } }

        public float maxWidth { get { return m_MaxWidth; } set { m_MaxWidth = value; } }

        public float minHeight { get { return m_MinHeight; } set { m_MinHeight = value; } }

        public float maxHeight { get { return m_MaxHeight; } set { m_MaxHeight = value; } }

        public float noiseSpread { get { return m_NoiseSpread; } set { m_NoiseSpread = value; } }

        public float bendFactor { get { return m_BendFactor; } set { m_BendFactor = value; } }

        public Color healthyColor { get { return m_HealthyColor; } set { m_HealthyColor = value; } }

        public Color dryColor { get { return m_DryColor; } set { m_DryColor = value; } }

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

    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    public partial struct TreeInstance
    {
        public Vector3 position;

        public float widthScale;

        public float heightScale;

        public float rotation;

        public Color32 color;

        public Color32 lightmapColor;

        public int prototypeIndex;

        internal float temporaryDistance;
    }

    [NativeHeader("TerrainScriptingClasses.h")]
    [NativeHeader("Modules/Terrain/Public/TerrainDataScriptingInterface.h")]
    public sealed partial class TerrainData : Object
    {
        private const string k_ScriptingInterfaceName = "TerrainDataScriptingInterface";
        private const string k_ScriptingInterfacePrefix = k_ScriptingInterfaceName + "::";
        private const string k_HeightmapPrefix = "GetHeightmap().";
        private const string k_DetailDatabasePrefix = "GetDetailDatabase().";
        private const string k_TreeDatabasePrefix = "GetTreeDatabase().";
        private const string k_SplatDatabasePrefix = "GetSplatDatabase().";

        private enum BoundaryValueType
        {
            MaxHeightmapRes = 0,
            MinDetailResPerPatch = 1,
            MaxDetailResPerPatch = 2,
            MaxDetailPatchCount = 3,
            MinAlphamapRes = 4,
            MaxAlphamapRes = 5,
            MinBaseMapRes = 6,
            MaxBaseMapRes = 7
        }

        [ThreadSafe]
        [StaticAccessor(k_ScriptingInterfaceName, StaticAccessorType.DoubleColon)]
        extern private static int GetBoundaryValue(BoundaryValueType type);

        private static readonly int k_MaximumResolution = GetBoundaryValue(BoundaryValueType.MaxHeightmapRes);
        private static readonly int k_MinimumDetailResolutionPerPatch = GetBoundaryValue(BoundaryValueType.MinDetailResPerPatch);
        private static readonly int k_MaximumDetailResolutionPerPatch = GetBoundaryValue(BoundaryValueType.MaxDetailResPerPatch);
        private static readonly int k_MaximumDetailPatchCount = GetBoundaryValue(BoundaryValueType.MaxDetailPatchCount);
        private static readonly int k_MinimumAlphamapResolution = GetBoundaryValue(BoundaryValueType.MinAlphamapRes);
        private static readonly int k_MaximumAlphamapResolution = GetBoundaryValue(BoundaryValueType.MaxAlphamapRes);
        private static readonly int k_MinimumBaseMapResolution = GetBoundaryValue(BoundaryValueType.MinBaseMapRes);
        private static readonly int k_MaximumBaseMapResolution = GetBoundaryValue(BoundaryValueType.MaxBaseMapRes);

        public TerrainData()
        {
            Internal_Create(this);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "Create")]
        extern private static void Internal_Create([Writable] TerrainData terrainData);

        extern internal bool HasUser(GameObject user);

        extern internal void AddUser(GameObject user);

        extern internal void RemoveUser(GameObject user);

        extern public int heightmapWidth
        {
            [NativeName(k_HeightmapPrefix + "GetWidth")]
            get;
        }

        extern public int heightmapHeight
        {
            [NativeName(k_HeightmapPrefix + "GetHeight")]
            get;
        }

        public int heightmapResolution
        {
            get { return internalHeightmapResolution; }
            set
            {
                int clamped = value;
                if (value < 0 || value > k_MaximumResolution)
                {
                    Debug.LogWarning("heightmapResolution is clamped to the range of [0, " + k_MaximumResolution + "].");
                    clamped = Math.Min(k_MaximumResolution, Math.Max(value, 0));
                }

                internalHeightmapResolution = clamped;
            }
        }

        extern private int internalHeightmapResolution
        {
            [NativeName(k_HeightmapPrefix + "GetResolution")]
            get;

            [NativeName(k_HeightmapPrefix + "SetResolution")]
            set;
        }

        extern public Vector3 heightmapScale
        {
            [NativeName(k_HeightmapPrefix + "GetScale")]
            get;
        }

        extern public Vector3 size
        {
            [NativeName(k_HeightmapPrefix + "GetSize")]
            get;

            [NativeName(k_HeightmapPrefix + "SetSize")]
            set;
        }

        extern public Bounds bounds
        {
            [NativeName(k_HeightmapPrefix + "CalculateBounds")]
            get;
        }

        extern public float thickness
        {
            [NativeName(k_HeightmapPrefix + "GetThickness")]
            get;

            [NativeName(k_HeightmapPrefix + "SetThickness")]
            set;
        }

        [NativeName(k_HeightmapPrefix + "GetHeight")]
        extern public float GetHeight(int x, int y);

        [NativeName(k_HeightmapPrefix + "GetInterpolatedHeight")]
        extern public float GetInterpolatedHeight(float x, float y);

        public float[,] GetHeights(int xBase, int yBase, int width, int height)
        {
            if (xBase < 0 || yBase < 0 || xBase + width < 0 || yBase + height < 0 || xBase + width > heightmapWidth || yBase + height > heightmapHeight)
            {
                throw new System.ArgumentException("Trying to access out-of-bounds terrain height information.");
            }

            return Internal_GetHeights(xBase, yBase, width, height);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "GetHeights", HasExplicitThis = true)]
        extern private float[,] Internal_GetHeights(int xBase, int yBase, int width, int height);

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

        [FreeFunction(k_ScriptingInterfacePrefix + "SetHeights", HasExplicitThis = true)]
        extern private void Internal_SetHeights(int xBase, int yBase, int width, int height, float[,] heights);

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

        [FreeFunction(k_ScriptingInterfacePrefix + "SetHeightsDelayLOD", HasExplicitThis = true)]
        extern private void Internal_SetHeightsDelayLOD(int xBase, int yBase, int width, int height, float[,] heights);

        [NativeName(k_HeightmapPrefix + "GetSteepness")]
        extern public float GetSteepness(float x, float y);

        [NativeName(k_HeightmapPrefix + "GetInterpolatedNormal")]
        extern public Vector3 GetInterpolatedNormal(float x, float y);

        [NativeName(k_HeightmapPrefix + "GetAdjustedSize")]
        extern internal int GetAdjustedSize(int size);

        extern public float wavingGrassStrength
        {
            [NativeName(k_DetailDatabasePrefix + "GetWavingGrassStrength")]
            get;

            [FreeFunction(k_ScriptingInterfacePrefix + "SetWavingGrassStrength", HasExplicitThis = true)]
            set;
        }

        extern public float wavingGrassAmount
        {
            [NativeName(k_DetailDatabasePrefix + "GetWavingGrassAmount")]
            get;

            [FreeFunction(k_ScriptingInterfacePrefix + "SetWavingGrassAmount", HasExplicitThis = true)]
            set;
        }

        extern public float wavingGrassSpeed
        {
            [NativeName(k_DetailDatabasePrefix + "GetWavingGrassSpeed")]
            get;

            [FreeFunction(k_ScriptingInterfacePrefix + "SetWavingGrassSpeed", HasExplicitThis = true)]
            set;
        }

        extern public Color wavingGrassTint
        {
            [NativeName(k_DetailDatabasePrefix + "GetWavingGrassTint")]
            get;

            [FreeFunction(k_ScriptingInterfacePrefix + "SetWavingGrassTint", HasExplicitThis = true)]
            set;
        }

        extern public int detailWidth
        {
            [NativeName(k_DetailDatabasePrefix + "GetWidth")]
            get;
        }

        extern public int detailHeight
        {
            [NativeName(k_DetailDatabasePrefix + "GetHeight")]
            get;
        }

        public void SetDetailResolution(int detailResolution, int resolutionPerPatch)
        {
            if (detailResolution < 0)
            {
                Debug.LogWarning("detailResolution must not be negative.");
                detailResolution = 0;
            }

            if (resolutionPerPatch < k_MinimumDetailResolutionPerPatch || resolutionPerPatch > k_MaximumDetailResolutionPerPatch)
            {
                Debug.LogWarning("resolutionPerPatch is clamped to the range of [" + k_MinimumDetailResolutionPerPatch + ", " + k_MaximumDetailResolutionPerPatch + "].");
                resolutionPerPatch = Math.Min(k_MaximumDetailResolutionPerPatch, Math.Max(resolutionPerPatch, k_MinimumDetailResolutionPerPatch));
            }

            int patchCount = detailResolution / resolutionPerPatch;
            if (patchCount > k_MaximumDetailPatchCount)
            {
                Debug.LogWarning("Patch count (detailResolution / resolutionPerPatch) is clamped to the range of [0, " + k_MaximumDetailPatchCount + "].");
                patchCount = Math.Min(k_MaximumDetailPatchCount, Math.Max(patchCount, 0));
            }

            Internal_SetDetailResolution(patchCount, resolutionPerPatch);
        }

        [NativeName(k_DetailDatabasePrefix + "SetDetailResolution")]
        extern private void Internal_SetDetailResolution(int patchCount, int resolutionPerPatch);

        extern public int detailResolution
        {
            [NativeName(k_DetailDatabasePrefix + "GetResolution")]
            get;
        }

        extern internal int detailResolutionPerPatch
        {
            [NativeName(k_DetailDatabasePrefix + "GetResolutionPerPatch")]
            get;
        }

        [NativeName(k_DetailDatabasePrefix + "ResetDirtyDetails")]
        extern internal void ResetDirtyDetails();

        [FreeFunction(k_ScriptingInterfacePrefix + "RefreshPrototypes", HasExplicitThis = true)]
        extern public void RefreshPrototypes();

        extern public DetailPrototype[] detailPrototypes
        {
            [FreeFunction(k_ScriptingInterfacePrefix + "GetDetailPrototypes", HasExplicitThis = true)]
            get;

            [FreeFunction(k_ScriptingInterfacePrefix + "SetDetailPrototypes", HasExplicitThis = true)]
            set;
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "GetSupportedLayers", HasExplicitThis = true)]
        extern public int[] GetSupportedLayers(int xBase, int yBase, int totalWidth, int totalHeight);

        [FreeFunction(k_ScriptingInterfacePrefix + "GetDetailLayer", HasExplicitThis = true)]
        extern public int[,] GetDetailLayer(int xBase, int yBase, int width, int height, int layer);

        public void SetDetailLayer(int xBase, int yBase, int layer, int[,] details)
        {
            Internal_SetDetailLayer(xBase, yBase, details.GetLength(1), details.GetLength(0), layer, details);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "SetDetailLayer", HasExplicitThis = true)]
        extern private void Internal_SetDetailLayer(int xBase, int yBase, int totalWidth, int totalHeight, int detailIndex, int[,] data);

        public TreeInstance[] treeInstances
        {
            get
            {
                return Internal_GetTreeInstances();
            }

            set
            {
                Internal_SetTreeInstances(value);
            }
        }

        [NativeName(k_TreeDatabasePrefix + "GetInstances")]
        extern private TreeInstance[] Internal_GetTreeInstances();

        [FreeFunction(k_ScriptingInterfacePrefix + "SetTreeInstances", HasExplicitThis = true)]
        extern private void Internal_SetTreeInstances([NotNull] TreeInstance[] instances);

        public TreeInstance GetTreeInstance(int index)
        {
            if (index < 0 || index >= treeInstanceCount)
                throw new ArgumentOutOfRangeException("index");

            return Internal_GetTreeInstance(index);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "GetTreeInstance", HasExplicitThis = true)]
        extern private TreeInstance Internal_GetTreeInstance(int index);

        [NativeThrows]
        [FreeFunction(k_ScriptingInterfacePrefix + "SetTreeInstance", HasExplicitThis = true)]
        extern public void SetTreeInstance(int index, TreeInstance instance);

        extern public int treeInstanceCount
        {
            [NativeName(k_TreeDatabasePrefix + "GetInstances().size")]
            get;
        }

        extern public TreePrototype[] treePrototypes
        {
            [FreeFunction(k_ScriptingInterfacePrefix + "GetTreePrototypes", HasExplicitThis = true)]
            get;

            [FreeFunction(k_ScriptingInterfacePrefix + "SetTreePrototypes", HasExplicitThis = true)]
            set;
        }

        [NativeName(k_TreeDatabasePrefix + "RemoveTreePrototype")]
        extern internal void RemoveTreePrototype(int index);

        [NativeName(k_TreeDatabasePrefix + "RecalculateTreePositions")]
        extern internal void RecalculateTreePositions();

        [NativeName(k_DetailDatabasePrefix + "RemoveDetailPrototype")]
        extern internal void RemoveDetailPrototype(int index);

        [NativeName(k_TreeDatabasePrefix + "NeedUpgradeScaledPrototypes")]
        extern internal bool NeedUpgradeScaledTreePrototypes();

        [FreeFunction(k_ScriptingInterfacePrefix + "UpgradeScaledTreePrototype", HasExplicitThis = true)]
        extern internal void UpgradeScaledTreePrototype();

        extern public int alphamapLayers
        {
            [NativeName(k_SplatDatabasePrefix + "GetDepth")]
            get;
        }

        public float[,,] GetAlphamaps(int x, int y, int width, int height)
        {
            if (x < 0 || y < 0 || width < 0 || height < 0)
                throw new ArgumentException("Invalid argument for GetAlphaMaps");

            return Internal_GetAlphamaps(x, y, width, height);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "GetAlphamaps", HasExplicitThis = true)]
        extern private float[,,] Internal_GetAlphamaps(int x, int y, int width, int height);

        public int alphamapResolution
        {
            get { return Internal_alphamapResolution; }
            set
            {
                int clamped = value;
                if (value < k_MinimumAlphamapResolution || value > k_MaximumAlphamapResolution)
                {
                    Debug.LogWarning("alphamapResolution is clamped to the range of [" + k_MinimumAlphamapResolution + ", " + k_MaximumAlphamapResolution + "].");
                    clamped = Math.Min(k_MaximumAlphamapResolution, Math.Max(value, k_MinimumAlphamapResolution));
                }

                Internal_alphamapResolution = clamped;
            }
        }

        // Needed by GI code which will call this by reflection
        [RequiredByNativeCode]
        [NativeName(k_SplatDatabasePrefix + "GetAlphamapResolution")]
        extern internal float GetAlphamapResolutionInternal();

        extern private int Internal_alphamapResolution
        {
            [NativeName(k_SplatDatabasePrefix + "GetAlphamapResolution")]
            get;

            [NativeName(k_SplatDatabasePrefix + "SetAlphamapResolution")]
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
                if (value < k_MinimumBaseMapResolution || value > k_MaximumBaseMapResolution)
                {
                    Debug.LogWarning("baseMapResolution is clamped to the range of [" + k_MinimumBaseMapResolution + ", " + k_MaximumBaseMapResolution + "].");
                    clamped = Math.Min(k_MaximumBaseMapResolution, Math.Max(value, k_MinimumBaseMapResolution));
                }

                Internal_baseMapResolution = clamped;
            }
        }

        extern private int Internal_baseMapResolution
        {
            [NativeName(k_SplatDatabasePrefix + "GetBaseMapResolution")]
            get;

            [NativeName(k_SplatDatabasePrefix + "SetBaseMapResolution")]
            set;
        }

        public void SetAlphamaps(int x, int y, float[,,] map)
        {
            if (map.GetLength(2) != alphamapLayers)
            {
                throw new System.Exception(UnityString.Format("Float array size wrong (layers should be {0})", alphamapLayers));
            }

            // TODO: crop the map or throw if outside.

            Internal_SetAlphamaps(x, y, map.GetLength(1), map.GetLength(0), map);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "SetAlphamaps", HasExplicitThis = true)]
        extern private void Internal_SetAlphamaps(int x, int y, int width, int height, float[,,] map);

        [NativeName(k_SplatDatabasePrefix + "RecalculateBasemapIfDirty")]
        extern internal void RecalculateBasemapIfDirty();

        [NativeName(k_SplatDatabasePrefix + "SetBasemapDirty")]
        extern internal void SetBasemapDirty(bool dirty);

        [NativeName(k_SplatDatabasePrefix + "GetAlphaTexture")]
        extern private Texture2D GetAlphamapTexture(int index);

        private extern int alphamapTextureCount
        {
            [NativeName(k_SplatDatabasePrefix + "GetAlphaTextureCount")]
            get;
        }

        public Texture2D[] alphamapTextures
        {
            get
            {
                Texture2D[] splatTextures = new Texture2D[alphamapTextureCount];
                for (int i = 0; i < splatTextures.Length; i++)
                    splatTextures[i] = GetAlphamapTexture(i);
                return splatTextures;
            }
        }

        extern public SplatPrototype[] splatPrototypes
        {
            [FreeFunction(k_ScriptingInterfacePrefix + "GetSplatPrototypes", HasExplicitThis = true)]
            get;

            [FreeFunction(k_ScriptingInterfacePrefix + "SetSplatPrototypes", HasExplicitThis = true)]
            set;
        }

        [NativeName(k_TreeDatabasePrefix + "AddTree")]
        extern internal void AddTree(ref TreeInstance tree);

        [NativeName(k_TreeDatabasePrefix + "RemoveTrees")]
        extern internal int RemoveTrees(Vector2 position, float radius, int prototypeIndex);
    }
}
