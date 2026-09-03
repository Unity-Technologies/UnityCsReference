// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace UnityEngine
{
    ///<summary>Simple class that contains a pointer to a tree prototype.</summary>
    ///<remarks>This class is used by the TerrainData gameObject.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [NativeAsStruct]
    public sealed partial class TreePrototype
    {
        [NativeName("prefab")]
        internal GameObject m_Prefab;
        [NativeName("bendFactor")]
        internal float m_BendFactor;
        [NativeName("navMeshLod")]
        internal int m_NavMeshLod;

        ///<summary>Retrieves the actual GameObject used by the tree.</summary>
        public GameObject prefab { get { return m_Prefab; } set { m_Prefab = value; } }

        ///<summary>Bend factor of the tree prototype.</summary>
        public float bendFactor { get { return m_BendFactor; } set { m_BendFactor = value; } }

        ///<summary>The LOD index of a Tree LODGroup that Unity uses to generate a NavMesh. It uses this value only for Trees with a LODGroup, and ignores this value for regular Trees.</summary>
        public int navMeshLod { get { return m_NavMeshLod; } set { m_NavMeshLod = value; } }

        public TreePrototype() {}

        public TreePrototype(TreePrototype other)
        {
            prefab = other.prefab;
            bendFactor = other.bendFactor;
            navMeshLod = other.navMeshLod;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TreePrototype);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private bool Equals(TreePrototype other)
        {
            if (ReferenceEquals(other, null))
                return false;

            if (ReferenceEquals(other, this))
                return true;

            if (GetType() != other.GetType())
                return false;

            bool equals = prefab == other.prefab &&
                bendFactor == other.bendFactor &&
                navMeshLod == other.navMeshLod;

            return equals;
        }

        internal bool Validate(out string errorMessage)
            => ValidateTreePrototype(this, out errorMessage);

        [FreeFunction("TerrainDataScriptingInterface::ValidateTreePrototype")]
        extern internal static bool ValidateTreePrototype([NotNull] TreePrototype prototype, out string errorMessage);
    }

    ///<summary>Render mode for detail prototypes.</summary>
    public enum DetailRenderMode
    {
        ///<summary>The detail prototype will be rendered as billboards that are always facing the camera.</summary>
        ///<remarks>Grass will take normals from terrain directly underneath it, so that the shading matches the terrain.</remarks>
        GrassBillboard = 0,
        ///<summary>Will show the prototype using diffuse shading.</summary>
        VertexLit = 1,
        ///<summary>The detail prototype will use the grass shader.</summary>
        ///<remarks>When using custom meshes in this mode, control the wave amount by setting vertex color's
        ///alpha channel. Grass will take normals from terrain directly
        ///underneath it, so that the shading matches the terrain.</remarks>
        Grass = 2
    }

    ///<summary>Provides options to specify how details are scattered on the terrain.</summary>
    public enum DetailScatterMode
    {
        ///<summary>The detail map holds values that represent how much area to cover at each sample, based on the detail's density.</summary>
        ///<remarks>When you use this mode, detail prototypes have a Density slider enabled.
        ///                    The detail map holds values between 0 and 255. The values represent how much area this detail covers at each
        ///                    sample, based on its density setting.</remarks>
        CoverageMode = 0,
        ///<summary>The detail map holds values that represent the number of detail instances to render at each sample.</summary>
        ///<remarks>The detail map holds values between 0 and 16. The values specify how many instances  appear at a given sample.</remarks>
        InstanceCountMode = 1
    }

    // should match TreeMotionVectorModeOverride enum in Terrain.h
    ///<summary>Options for motion vector rendering on the terrain.</summary>
    public enum TreeMotionVectorModeOverride
    {
        ///<summary>Use only camera movement to track motion for all SpeedTree models painted on the terrain.</summary>
        CameraMotionOnly = 0,
        ///<summary>Use a specific pass to track motion for all SpeedTree models painted on the terrain.</summary>
        PerObjectMotion = 1,
        ///<summary>Don't track motion for SpeedTree models painted on the terrain. Note that models are still rendered in the object motion vector pass, so the CPU performance is similar to `Per Object Motion`.</summary>
        ForceNoMotion = 2,
        ///<summary>For each SpeedTree model painted on the terrain, inherit the motion vector rendering mode from the import settings, instead of a terrain-global value.</summary>
        InheritFromPrototype = 3,
    }

    ///<summary>Detail prototype used by the Terrain GameObject.</summary>
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("TerrainScriptingClasses.h")]
    [NativeHeader("Modules/Terrain/Public/TerrainDataScriptingInterface.h")]
    [UsedByNativeCode]
    [NativeAsStruct]
    public sealed partial class DetailPrototype
    {
        internal static readonly Color DefaultHealthColor = new Color(67 / 255F, 249 / 255F, 42 / 255F, 1);
        internal static readonly Color DefaultDryColor = new Color(205 / 255.0F, 188 / 255.0F, 26 / 255.0F, 1.0F);

        [NativeName("prototype")]
        internal GameObject m_Prototype = null;
        [NativeName("prototypeTexture")]
        internal Texture2D m_PrototypeTexture = null;
        [NativeName("healthyColor")]
        internal Color m_HealthyColor = DefaultHealthColor;
        [NativeName("dryColor")]
        internal Color m_DryColor = DefaultDryColor;
        [NativeName("minWidth")]
        internal float m_MinWidth = 1.0F;
        [NativeName("maxWidth")]
        internal float m_MaxWidth = 2.0F;
        [NativeName("minHeight")]
        internal float m_MinHeight = 1F;
        [NativeName("maxHeight")]
        internal float m_MaxHeight = 2F;
        [NativeName("noiseSeed")]
        internal int m_NoiseSeed = 0;
        [NativeName("noiseSpread")]
        internal float m_NoiseSpread = 0.1F;
        [NativeName("density")]
        internal float m_Density = 1.0F;
        [NativeName("holeTestRadius")]
        internal float m_HoleEdgePadding = 0.0F;
        [NativeName("renderMode")]
        internal int m_RenderMode = 2;
        [NativeName("usePrototypeMesh")]
        internal int m_UsePrototypeMesh = 0;
        [NativeName("useInstancing")]
        internal int m_UseInstancing = 0;
        [NativeName("useDensityScaling")]
        internal int m_UseDensityScaling = 0;
        [NativeName("alignToGround")]
        internal float m_AlignToGround = 0;
        [NativeName("positionJitter")]
        internal float m_PositionJitter = 0;
        [NativeName("targetCoverage")]
        internal float m_TargetCoverage = 1.0F;

        ///<summary>GameObject used by the DetailPrototype.</summary>
        public GameObject prototype { get { return m_Prototype; } set { m_Prototype = value; } }

        ///<summary>Texture used by the DetailPrototype.</summary>
        public Texture2D prototypeTexture { get { return m_PrototypeTexture; } set { m_PrototypeTexture = value; } }

        ///<summary>Minimum width of the grass billboards (if render mode is GrassBillboard).</summary>
        public float minWidth { get { return m_MinWidth; } set { m_MinWidth = value; } }

        ///<summary>Maximum width of the grass billboards (if render mode is GrassBillboard).</summary>
        public float maxWidth { get { return m_MaxWidth; } set { m_MaxWidth = value; } }

        ///<summary>Minimum height of the grass billboards (if render mode is GrassBillboard).</summary>
        public float minHeight { get { return m_MinHeight; } set { m_MinHeight = value; } }

        ///<summary>Maximum height of the grass billboards (if render mode is GrassBillboard).</summary>
        public float maxHeight { get { return m_MaxHeight; } set { m_MaxHeight = value; } }

        ///<summary>Specifies the random seed value for detail object placement.</summary>
        public int noiseSeed { get { return m_NoiseSeed; } set { m_NoiseSeed = value; } }

        ///<summary>Controls the spatial frequency of the noise pattern used to vary the scale and color of the detail objects.</summary>
        public float noiseSpread { get { return m_NoiseSpread; } set { m_NoiseSpread = value; } }

        ///<summary>Controls detail density for this detail prototype, relative to it's size.</summary>
        public float density { get { return m_Density; } set { m_Density = value; } }

        ///<summary>Bend factor of the detailPrototype.</summary>
        [Obsolete("bendFactor has no effect and is deprecated.", false)]
        public float bendFactor { get { return 0.0f; } set {} }

        ///<summary>Controls how far away detail objects are from the edge of the hole area.</summary>
        ///<remarks>Specify a non-negative value, which is a scale of the detail mesh's width in world space. (<see cref="DetailRenderMode.GrassBillboard" /> details have a width of 1.) Unity multiplies this value by the detail mesh's width, and uses the result to determine the radius of a circular area around detail objects, which it then applies for testing against holes.</remarks>
        public float holeEdgePadding { get { return m_HoleEdgePadding; } set { m_HoleEdgePadding = value; } }

        ///<summary>Color when the DetailPrototypes are "healthy".</summary>
        public Color healthyColor { get { return m_HealthyColor; } set { m_HealthyColor = value; } }

        ///<summary>Color when the DetailPrototypes are "dry".</summary>
        public Color dryColor { get { return m_DryColor; } set { m_DryColor = value; } }

        ///<summary>Render mode for the DetailPrototype.</summary>
        public DetailRenderMode renderMode { get { return (DetailRenderMode)m_RenderMode; } set { m_RenderMode = (int)value; } }

        ///<summary>Indicates whether this detail prototype uses the Mesh object from the GameObject specified by <see cref="prototype" />.</summary>
        ///<remarks>If <see cref="renderMode" /> is <see cref="DetailRenderMode.Grass" />, you can set this value to either <c>true</c> or <c>false</c>. However, if <see cref="renderMode" /> is <see cref="DetailRenderMode.GrassBillboard" />, you must set this value to <c>false</c>. And if <see cref="renderMode" /> is <see cref="DetailRenderMode.VertexLit" />, you must set this value to <c>true</c>. Otherwise, this detail prototype won't render.</remarks>
        public bool usePrototypeMesh { get { return m_UsePrototypeMesh != 0; } set { m_UsePrototypeMesh = value ? 1 : 0; } }

        ///<summary>Indicates whether this detail prototype uses [ GPU Instancing](xref:GPUInstancing ) for rendering.</summary>
        ///<remarks>When you set this value to <c>true</c>, Unity uses the Material and Shader used by the <see cref="prototype" /> GameObject for rendering.
        ///
        ///Currently this setting is only effective when you specify <see cref="DetailRenderMode.VertexLit" /> as the <see cref="renderMode" />.</remarks>
        public bool useInstancing {
            get { return m_UseInstancing != 0; }
            set { m_UseInstancing = value ? 1 : 0; }
        }

        ///<summary>Controls the detail's target coverage.</summary>
        ///<remarks>Controls the amount of target coverage desired while scattering the detail.</remarks>
        public float targetCoverage {
            get { return m_TargetCoverage; }
            set { m_TargetCoverage = value; } }

        ///<summary>Indicates the global density scale set in the terrain settings affects this detail prototype.</summary>
        ///<remarks>When you set this value to <c>true</c>, Unity multiplies this detail prototype's density by the value set for the global density scale in Terrain settings.</remarks>
        public bool useDensityScaling { get { return m_UseDensityScaling != 0; } set { m_UseDensityScaling = value ? 1 : 0; } }

        ///<summary>Rotate detail axis parallel to the ground's normal direction, so that the detail is perpendicular to the ground.</summary>
        public float alignToGround { get { return m_AlignToGround; } set { m_AlignToGround = value; } }

        ///<summary>Controls how Unity generates the detail positions.</summary>
        ///<remarks>Controls how to generate positioning, based on a value between ordered (0) and random (100). Lower values are less likely to cause overlaps but look more ordered and less random. High values look more random but have more overlaps and more gaps in between details.</remarks>
        public float positionJitter { get { return m_PositionJitter; } set { m_PositionJitter = value; } }

        ///<exclude />
        public DetailPrototype() {}

        public DetailPrototype(DetailPrototype other)
        {
            m_Prototype = other.m_Prototype;
            m_PrototypeTexture = other.m_PrototypeTexture;
            m_HealthyColor = other.m_HealthyColor;
            m_DryColor = other.m_DryColor;
            m_MinWidth = other.m_MinWidth;
            m_MaxWidth = other.m_MaxWidth;
            m_MinHeight = other.m_MinHeight;
            m_MaxHeight = other.m_MaxHeight;
            m_NoiseSeed = other.m_NoiseSeed;
            m_NoiseSpread = other.m_NoiseSpread;
            m_Density = other.m_Density;
            m_HoleEdgePadding = other.m_HoleEdgePadding;
            m_RenderMode = other.m_RenderMode;
            m_UsePrototypeMesh = other.m_UsePrototypeMesh;
            m_UseInstancing = other.m_UseInstancing;
            m_UseDensityScaling = other.m_UseDensityScaling;
            m_AlignToGround = other.m_AlignToGround;
            m_PositionJitter = other.m_PositionJitter;
            m_TargetCoverage = other.m_TargetCoverage;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DetailPrototype);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private bool Equals(DetailPrototype other)
        {
            if (ReferenceEquals(other, null))
                return false;

            if (ReferenceEquals(other, this))
                return true;

            if (GetType() != other.GetType())
                return false;

            return m_Prototype == other.m_Prototype
                && m_PrototypeTexture == other.m_PrototypeTexture
                && m_HealthyColor == other.m_HealthyColor
                && m_DryColor == other.m_DryColor
                && m_MinWidth == other.m_MinWidth
                && m_MaxWidth == other.m_MaxWidth
                && m_MinHeight == other.m_MinHeight
                && m_MaxHeight == other.m_MaxHeight
                && m_NoiseSeed == other.m_NoiseSeed
                && m_NoiseSpread == other.m_NoiseSpread
                && m_Density == other.m_Density
                && m_HoleEdgePadding == other.m_HoleEdgePadding
                && m_RenderMode == other.m_RenderMode
                && m_UsePrototypeMesh == other.m_UsePrototypeMesh
                && m_UseInstancing == other.m_UseInstancing
                && m_TargetCoverage == other.m_TargetCoverage
                && m_UseDensityScaling == other.m_UseDensityScaling;
        }

        ///<summary>Returns <c>true</c> if the detail prototype is valid and the Terrain can accept it.</summary>
        public bool Validate()
            => ValidateDetailPrototype(this, out _);

        ///<summary>Returns <c>true</c> if the detail prototype is valid and the Terrain can accept it.</summary>
        ///<param name="errorMessage">Returns a message that indicates the cause of failed validation.</param>
        public bool Validate(out string errorMessage)
            => ValidateDetailPrototype(this, out errorMessage);

        [FreeFunction("TerrainDataScriptingInterface::ValidateDetailPrototype")]
        extern internal static bool ValidateDetailPrototype([NotNull] DetailPrototype prototype, out string errorMessage);

        internal bool ValidateTextures(out string errorMessage)
            => ValidateDetailPrototypeTextures(this, out errorMessage);

        [FreeFunction("TerrainDataScriptingInterface::ValidateDetailPrototypeTextures")]
        extern internal static bool ValidateDetailPrototypeTextures([NotNull] DetailPrototype prototype, out string errorMessage);

        internal bool ValidateMesh(out string errorMessage)
            => ValidateDetailPrototypeMesh(this, out errorMessage);

        [FreeFunction("TerrainDataScriptingInterface::ValidateDetailPrototypeMesh")]
        extern internal static bool ValidateDetailPrototypeMesh([NotNull] DetailPrototype prototype, out string errorMessage);

        internal static bool IsModeSupportedByRenderPipeline(DetailRenderMode renderMode, bool useInstancing, out string errorMessage)
        {
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                if (renderMode == DetailRenderMode.GrassBillboard && GraphicsSettings.GetDefaultShader(DefaultShaderType.TerrainDetailGrassBillboard) == null)
                {
                    errorMessage = "The current render pipeline does not support Billboard details. Details will not be rendered.";
                    return false;
                }
                else if (renderMode == DetailRenderMode.VertexLit && !useInstancing && GraphicsSettings.GetDefaultShader(DefaultShaderType.TerrainDetailLit) == null)
                {
                    errorMessage = "The current render pipeline does not support VertexLit details. Details will be rendered using the default shader.";
                    return false;
                }
                else if (renderMode == DetailRenderMode.Grass && GraphicsSettings.GetDefaultShader(DefaultShaderType.TerrainDetailGrass) == null)
                {
                    errorMessage = "The current render pipeline does not support Grass details. Details will be rendered using the default shader without alpha test and animation.";
                    return false;
                }
            }
            errorMessage = string.Empty;
            return true;
        }
    }
    ///<summary>Obsolete. Use <see cref="TerrainLayer" /> instead. A Splat prototype is just a texture that is used by the TerrainData.</summary>
    ///<remarks>A class on a Terrain GameObject.</remarks>
    [Obsolete("SplatPrototype is obsolete. Use TerrainLayer instead.", false)]
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [NativeAsStruct]
    public sealed partial class SplatPrototype
    {
        [NativeName("texture")]
        internal Texture2D m_Texture;
        [NativeName("normalMap")]
        internal Texture2D m_NormalMap;
        [NativeName("tileSize")]
        internal Vector2 m_TileSize = new Vector2(15, 15);
        [NativeName("tileOffset")]
        internal Vector2 m_TileOffset = new Vector2(0, 0);
        [NativeName("specularMetallic")]
        internal Vector4 m_SpecularMetallic = new Vector4(0, 0, 0, 0);
        [NativeName("smoothness")]
        internal float m_Smoothness = 0.0f;

        ///<summary>Texture of the splat applied to the Terrain.</summary>
        public Texture2D texture { get { return m_Texture; } set { m_Texture = value; } }

        ///<summary>Normal map of the splat applied to the Terrain.</summary>
        public Texture2D normalMap { get { return m_NormalMap; } set { m_NormalMap = value; } }

        ///<summary>Size of the tile used in the texture of the SplatPrototype.</summary>
        public Vector2 tileSize { get { return m_TileSize; } set { m_TileSize = value; } }

        ///<summary>Offset of the tile texture of the SplatPrototype.</summary>
        public Vector2 tileOffset { get { return m_TileOffset; } set { m_TileOffset = value; } }

        ///<exclude />
        public Color specular { get { return new Color(m_SpecularMetallic.x, m_SpecularMetallic.y, m_SpecularMetallic.z); } set { m_SpecularMetallic.x = value.r; m_SpecularMetallic.y = value.g; m_SpecularMetallic.z = value.b; } }

        ///<summary>The metallic value of the splat layer.</summary>
        ///<remarks>This is only applicable when using the built-in standard material for terrain.
        ///
        ///
        ///Valid range is 0.0f to 1.0f.</remarks>
        public float metallic { get { return m_SpecularMetallic.w; } set { m_SpecularMetallic.w = value; } }

        ///<summary>The smoothness value of the splat layer when the main texture has no alpha channel.</summary>
        ///<remarks>This is only applicable when using the built-in standard material for terrain.
        ///
        ///
        ///Valid range is 0.0f to 1.0f.</remarks>
        public float smoothness { get { return m_Smoothness; } set { m_Smoothness = value; } }
    }

    ///<summary>Contains information about a tree placed in the Terrain game object.</summary>
    ///<remarks>This struct can be accessed from the TerrainData Object.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    public partial struct TreeInstance
    {
        ///<summary>The position of the tree in the local space of the terrain. The value is a Vector3 clamped to 0-1, and describes a percentage of the terrain width, length, and height.</summary>
        public Vector3 position;

        ///<summary>Width scale of this instance (compared to the prototype's size).</summary>
        public float widthScale;

        ///<summary>Height scale of this instance (compared to the prototype's size).</summary>
        public float heightScale;

        ///<summary>Read-only.
        ///
        ///Rotation of the tree on X-Z plane (in radians).</summary>
        public float rotation;

        ///<summary>Color of this instance.</summary>
        public Color32 color;

        ///<summary>Lightmap color calculated for this instance.</summary>
        public Color32 lightmapColor;

        ///<summary>Index of this instance in the TerrainData.treePrototypes array.</summary>
        public int prototypeIndex;

        internal float temporaryDistance;
    }

    ///<summary>Structure containing minimum and maximum terrain patch height values.</summary>
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    public struct PatchExtents
    {
        internal float m_min;
        internal float m_max;

        ///<summary>Minimum height of a terrain patch.</summary>
        public float min { get { return m_min; } set { m_min = value; } }
        ///<summary>Maximum height of a terrain patch.</summary>
        public float max { get { return m_max; } set { m_max = value; } }
    }

    // Must Match Heightmap::HeightmapSyncControl
    ///<summary>Controls what Terrain heightmap data to synchronize when there are changes to the heightmap texture.</summary>
    ///<seealso cref="TerrainData.CopyActiveRenderTextureToHeightmap" />
    ///<seealso cref="TerrainData.DirtyHeightmapRegion" />
    ///<seealso cref="TerrainData.SyncHeightmap" />
    public enum TerrainHeightmapSyncControl
    {
        ///<summary>Does not synchronize the height data nor the LOD data.</summary>
        ///<remarks>This option forces the tessellation level to maximum on the modified heightmap region. Use <see cref="TerrainData.SyncHeightmap" /> afterward to readback height data from the heightmap texture, and rectify the tessellation.</remarks>
        None = 0,
        ///<summary>Synchronizes only height data of the heightmap texture from the GPU back to CPU memory.</summary>
        ///<remarks>This option forces the tessellation level to maximum on the modified heightmap region. Use <see cref="TerrainData.SyncHeightmap" /> afterward to rectify the tessellation.</remarks>
        HeightOnly,
        ///<summary>Synchronizes height data of the heightmap texture from the GPU back to CPU memory. Then computes LOD data, used for determining the tessellation level, from the height data.</summary>
        HeightAndLod
    }

    ///<summary>Describes the transform of a Terrain detail object.</summary>
    ///<seealso cref="TerrainData.ComputeDetailInstanceTransforms" />
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    public struct DetailInstanceTransform
    {
        ///<summary>The X coordinate of the detail object in the Terrain's local space. To get the X in world space, add this value to position X of the Terrain.</summary>
        public float posX;
        ///<summary>The Y coordinate of the detail object in the Terrain's local space. To get the Y in world space, add this value to position Y of the Terrain.</summary>
        ///<remarks>Note that the Y coordinate is already displaced by the Terrain heightmap.</remarks>
        public float posY;
        ///<summary>The Z coordinate of the detail object in the Terrain's local space. To get the Z in world space, add this value to position Z of the Terrain.</summary>
        public float posZ;
        ///<summary>The X and Z scale values of the detail object. These two values are always the same.</summary>
        public float scaleXZ;
        ///<summary>The Y scale value of the detail object.</summary>
        public float scaleY;
        ///<summary>The angle, in radians, at which the detail object rotates around the Y-axis.</summary>
        public float rotationY;
    }

    ///<summary>The TerrainData class stores heightmaps, detail mesh positions, tree instances, and terrain texture alpha maps.</summary>
    ///<remarks>The <see cref="Terrain" /> component links to the terrain data and renders it.</remarks>
    [NativeHeader("TerrainScriptingClasses.h")]
    [NativeHeader("Modules/Terrain/Public/TerrainDataScriptingInterface.h")]
    [global::UnityEngine.NativeClass("TerrainData", PersistentTypeId = 156)]
    [UsedByNativeCode]
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
            // THESE VALUES ARE SYNCED WITH C CODE (see the same enum in TerrainDataScriptingInterface.h)
            MaxHeightmapRes = 0,
            MinDetailResPerPatch = 1,
            MaxDetailResPerPatch = 2,
            MaxDetailPatchCount = 3,
            MaxCoveragePerRes = 4,
            MinAlphamapRes = 5,
            MaxAlphamapRes = 6,
            MinBaseMapRes = 7,
            MaxBaseMapRes = 8
        }

        [NativeMethod(IsThreadSafe = true)]
        [StaticAccessor(k_ScriptingInterfaceName, StaticAccessorType.DoubleColon)]
        extern private static int GetBoundaryValue(BoundaryValueType type);

        internal static readonly int k_MaximumResolution = GetBoundaryValue(BoundaryValueType.MaxHeightmapRes);
        internal static readonly int k_MinimumDetailResolutionPerPatch = GetBoundaryValue(BoundaryValueType.MinDetailResPerPatch);
        internal static readonly int k_MaximumDetailResolutionPerPatch = GetBoundaryValue(BoundaryValueType.MaxDetailResPerPatch);
        internal static readonly int k_MaximumDetailPatchCount = GetBoundaryValue(BoundaryValueType.MaxDetailPatchCount);
        internal static readonly int k_MinimumAlphamapResolution = GetBoundaryValue(BoundaryValueType.MinAlphamapRes);
        internal static readonly int k_MaximumAlphamapResolution = GetBoundaryValue(BoundaryValueType.MaxAlphamapRes);
        internal static readonly int k_MinimumBaseMapResolution = GetBoundaryValue(BoundaryValueType.MinBaseMapRes);
        internal static readonly int k_MaximumBaseMapResolution = GetBoundaryValue(BoundaryValueType.MaxBaseMapRes);

        ///<exclude />
        public TerrainData()
        {
            Internal_Create(this);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "Create")]
        extern private static void Internal_Create([Writable] TerrainData terrainData);

        ///<summary>Triggers an update to integrate modifications done to the heightmap outside of unity.</summary>
        ///<remarks>This function is obsolete. Use either <see cref="CopyActiveRenderTextureToHeightmap" /> or <see cref="DirtyHeightmapRegion" /> instead.
        ///
        ///Invoke this function whenever custom Terrain heightmap paint tools modify the heightmap, to let Unity integrate the changes such as physics and collision updates.</remarks>
        ///<param name="x">Start X position of the dirty heightmap region.</param>
        ///<param name="y">Start Y position of the dirty heightmap region.</param>
        ///<param name="width">Width of the dirty heightmap region.</param>
        ///<param name="height">Width of the dirty heightmap region.</param>
        ///<param name="syncHeightmapTextureImmediately">Update immediately, instead of deferring the update.</param>
        [Obsolete("Please use DirtyHeightmapRegion instead.", false)]
        public void UpdateDirtyRegion(int x, int y, int width, int height, bool syncHeightmapTextureImmediately)
        {
            DirtyHeightmapRegion(new RectInt(x, y, width, height), syncHeightmapTextureImmediately ? TerrainHeightmapSyncControl.HeightOnly : TerrainHeightmapSyncControl.None);
        }

        ///<summary>Width of the terrain in samples (RO).</summary>
        ///<remarks>Obsolete. Use <see cref="TerrainData.heightmapResolution" /> instead.</remarks>
        [Obsolete("Please use heightmapResolution instead. (UnityUpgradable) -> heightmapResolution", false)]
        public int heightmapWidth => heightmapResolution;

        ///<summary>Height of the terrain in samples (RO).</summary>
        ///<remarks>Obsolete. Use <see cref="TerrainData.heightmapResolution" /> instead.</remarks>
        [Obsolete("Please use heightmapResolution instead. (UnityUpgradable) -> heightmapResolution", false)]
        public int heightmapHeight => heightmapResolution;

        ///<summary>Returns the heightmap texture.</summary>
        ///<remarks>See <see cref="TerrainData.UpdateDirtyRegion" />.</remarks>
        extern public RenderTexture heightmapTexture
        {
            [NativeName(k_HeightmapPrefix + "GetHeightmapTexture")]
            get;
        }

        ///<summary>The size of the [heightmap](xref:terrain-Heightmaps) in texels for both the width and height. When setting the heightmap resolution, Unity clamps the value to one of 33, 65, 129, 257, 513, 1025, 2049, or 4097.</summary>
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

        ///<summary>Returns a Vector3 where the x and z components are the size of each heightmap sample (i.e. the space between two neighboring heightmap samples), and the y component is the entire Terrain's height range in world space.</summary>
        extern public Vector3 heightmapScale
        {
            [NativeName(k_HeightmapPrefix + "GetScale")]
            get;
        }

        ///<summary>Returns the Terrain holes Texture.</summary>
        ///<remarks>An uncompressed Terrain holes Texture is a RenderTexture in the Editor and at runtime. A compressed Terrain holes Texture is a Texture2D at runtime only. You might use an uncompressed Terrain holes Texture as a render target for the custom painting of Terrain holes data. Use the <see cref="TerrainData.enableHolesTextureCompression" /> property to enable or disable Terrain holes Texture compression at runtime.</remarks>
        public Texture holesTexture
        {
            get
            {
                if (IsHolesTextureCompressed())
                {
                    return GetCompressedHolesTexture();
                }
                else
                {
                    return GetHolesTexture();
                }
            }
        }

        ///<summary>Enable the Terrain holes Texture compression.</summary>
        ///<remarks>Set to <c>true</c> to compress the Terrain holes Texture. This property takes effect only at runtime, and is <c>true</c> by default. Terrain holes Textures are never compressed in the Editor. See <see cref="TerrainData.holesTexture" /> for details.</remarks>
        extern public bool enableHolesTextureCompression
        {
            [NativeName(k_HeightmapPrefix + "GetEnableHolesTextureCompression")]
            get;

            [NativeName(k_HeightmapPrefix + "SetEnableHolesTextureCompression")]
            set;
        }

        internal RenderTexture holesRenderTexture
        {
            get
            {
                return GetHolesTexture();
            }
        }

        [NativeName(k_HeightmapPrefix + "IsHolesTextureCompressed")]
        extern internal bool IsHolesTextureCompressed();

        [NativeName(k_HeightmapPrefix + "GetHolesTexture")]
        extern internal RenderTexture GetHolesTexture();

        [NativeName(k_HeightmapPrefix + "GetCompressedHolesTexture")]
        extern internal Texture2D GetCompressedHolesTexture();

        ///<summary>Returns the Terrain holes resolution for both the data and the Texture.</summary>
        public int holesResolution => heightmapResolution - 1;

        ///<summary>The total size in world units of the terrain: width, height, and length.</summary>
        extern public Vector3 size
        {
            [NativeName(k_HeightmapPrefix + "GetSize")]
            get;

            [NativeName(k_HeightmapPrefix + "SetSize")]
            set;
        }

        ///<summary>The local bounding box of the TerrainData object.</summary>
        extern public Bounds bounds
        {
            [NativeName(k_HeightmapPrefix + "CalculateBounds")]
            get;
        }

        ///<summary>The thickness of the terrain used for collision detection.</summary>
        ///<remarks>This lets the physics engine know how thick the Terrain is when used with a TerrainCollider. Any other colliders which are no less then thickness units underneath the Terrain will be considered to collide with the terrain, and will be moved above the terrain.</remarks>
        [Obsolete("Terrain thickness is no longer required by the physics engine. Set appropriate continuous collision detection modes to fast moving bodies.")]
        public float thickness
        {
            get { return 0; }
            set {}
        }

        ///<summary>Calculates the height in world space units of a point on the heightmap. x and y are pixel coordinates in the heightmap, and the returned value does not take into account the heightmap's position.</summary>
        [NativeName(k_HeightmapPrefix + "GetHeight")]
        extern public float GetHeight(int x, int y);

        ///<summary>Gets an interpolated height at a point x,y. The x and y coordinates are clamped to [0, 1].</summary>
        ///<param name="x">X coordinate of the point in the range of [0, 1].</param>
        ///<param name="y">Y coordinate of the point in the range of [0, 1].</param>
        [NativeName(k_HeightmapPrefix + "GetInterpolatedHeight")]
        extern public float GetInterpolatedHeight(float x, float y);

        ///<summary>Gets an array of terrain height values using the normalized x,y coordinates.</summary>
        ///<remarks>The function returns a two-dimensional array of size [yCount, xCount]. Each returned value is an interpolation between the four neighboring Terrain height samples, based on where the sampling point is located within the quad of the four neighboring samples. The sampling points are evenly distributed, starting at (xBase, yBase). Points are spaced <c>xInterval</c> apart along the X axis, and <c>yInterval</c> apart along the Y axis. All the floating point arguments are specified as normalized coordinates, with 0 indicating the left/top border of the Terrain, and 1 indicating the right/bottom border of the Terrain. If a sampling point is placed outside of [0,1], it is clamped to the range.</remarks>
        ///<param name="xBase">The base x coordinate.</param>
        ///<param name="yBase">The base y coordinate.</param>
        ///<param name="xCount">The number of queries along the X axis.</param>
        ///<param name="yCount">The number of queries along the Y axis.</param>
        ///<param name="xInterval">The interval between each query along the X axis.</param>
        ///<param name="yInterval">The interval between each query along the Y axis.</param>
        public float[,] GetInterpolatedHeights(float xBase, float yBase, int xCount, int yCount, float xInterval, float yInterval)
        {
            if (xCount <= 0)
                throw new ArgumentOutOfRangeException("xCount");
            else if (yCount <= 0)
                throw new ArgumentOutOfRangeException("yCount");

            float[,] results = new float[yCount, xCount];
            Internal_GetInterpolatedHeights(results, xCount, 0, 0, xBase, yBase, xCount, yCount, xInterval, yInterval);
            return results;
        }

        ///<summary>Fills the array with Terrain height values using normalized x,y coordinates.</summary>
        ///<remarks>The function takes a two-dimensional array, and fills height values into the part starting at (resultXOffset, resultYOffset). Unlike the function overload above, Unity guarantees not to allocate any memory during calls to the <c>GetInterpolatedHeights</c> function.</remarks>
        ///<param name="results">The array to fill with height values.</param>
        ///<param name="resultXOffset">The offset from the beginning of the array, along the X axis, at which to start filling in height values.</param>
        ///<param name="resultYOffset">The offset from the beginning of the array, along the Y axis, at which to start filling in height values.</param>
        ///<param name="xBase">The base x coordinate.</param>
        ///<param name="yBase">The base y coordinate.</param>
        ///<param name="xCount">The number of queries along the X axis.</param>
        ///<param name="yCount">The number of queries along the Y axis.</param>
        ///<param name="xInterval">The interval between each query along the X axis.</param>
        ///<param name="yInterval">The interval between each query along the Y axis.</param>
        public void GetInterpolatedHeights(float[,] results, int resultXOffset, int resultYOffset, float xBase, float yBase, int xCount, int yCount, float xInterval, float yInterval)
        {
            if (results == null)
                throw new ArgumentNullException("results");
            else if (xCount <= 0)
                throw new ArgumentOutOfRangeException("xCount");
            else if (yCount <= 0)
                throw new ArgumentOutOfRangeException("yCount");
            else if (resultXOffset < 0 || resultXOffset + xCount > results.GetLength(1))
                throw new ArgumentOutOfRangeException("resultXOffset");
            else if (resultYOffset < 0 || resultYOffset + yCount > results.GetLength(0))
                throw new ArgumentOutOfRangeException("resultYOffset");

            Internal_GetInterpolatedHeights(results, results.GetLength(1), resultXOffset, resultYOffset, xBase, yBase, xCount, yCount, xInterval, yInterval);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "GetInterpolatedHeights", HasExplicitThis = true)]
        private extern void Internal_GetInterpolatedHeights(float[,] results, int resultXDimension, int resultXOffset, int resultYOffset, float xBase, float yBase, int xCount, int yCount, float xInterval, float yInterval);

        ///<summary>Gets an array of heightmap samples.</summary>
        ///<remarks>Returns a two dimensional array of heightmap samples. The samples are represented as float values ranging from 0 to 1. The array has the dimensions [height,width] and is indexed as [y,x].</remarks>
        ///<param name="xBase">First index of heightmap samples to retrieve along the Terrain's x axis.</param>
        ///<param name="yBase">First index of heightmap samples to retrieve along the Terrain's z axis.</param>
        ///<param name="width">Number of samples to retrieve along the Terrain's x axis.</param>
        ///<param name="height">Number of samples to retrieve along the Terrain's z axis.</param>
        public float[,] GetHeights(int xBase, int yBase, int width, int height)
        {
            if (xBase < 0 || yBase < 0 || xBase + width < 0 || yBase + height < 0 || xBase + width > heightmapResolution || yBase + height > heightmapResolution)
            {
                throw new System.ArgumentException("Trying to access out-of-bounds terrain height information.");
            }

            float[,] heights = new float[height, width];
            Internal_GetHeights(xBase, yBase, width, height, heights);
            return heights;
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "GetHeights", HasExplicitThis = true)]
        extern private void Internal_GetHeights(int xBase, int yBase, int width, int height, float[,] heights);

        ///<summary>Sets an array of heightmap samples.</summary>
        ///<remarks>Sets heightmap data using a two dimensional array of heightmap samples. The samples are represented as float values ranging from 0 to 1. The area affected is defined by the array dimensions and starts at xBase and yBase. The heights array is indexed as [y,x].
        ///
        ///This method recomputes all the LOD and vegetation information for the terrain on each call, which can be computationally expensive. In interactive editing scenarios, it may be better to call <see cref="TerrainData.SetHeightsDelayLOD" /> instead, followed by <see cref="TerrainData.SyncHeightmap" /> when the user completes an editing action.</remarks>
        ///<param name="xBase">First x index of heightmap samples to set.</param>
        ///<param name="yBase">First y index of heightmap samples to set.</param>
        ///<param name="heights">Array of heightmap samples to set (values range from 0 to 1, array indexed as [y,x]).</param>
        public void SetHeights(int xBase, int yBase, float[,] heights)
        {
            if (heights == null)
            {
                throw new System.NullReferenceException();
            }
            if (xBase + heights.GetLength(1) > heightmapResolution || xBase + heights.GetLength(1) < 0 || yBase + heights.GetLength(0) < 0 || xBase < 0 || yBase < 0 || yBase + heights.GetLength(0) > heightmapResolution)
            {
                throw new System.ArgumentException(string.Format("X or Y base out of bounds. Setting up to {0}x{1} while map size is {2}x{2}", xBase + heights.GetLength(1), yBase + heights.GetLength(0), heightmapResolution));
            }

            Internal_SetHeights(xBase, yBase, heights.GetLength(1), heights.GetLength(0), heights);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "SetHeights", HasExplicitThis = true)]
        extern private void Internal_SetHeights(int xBase, int yBase, int width, int height, float[,] heights);

        ///<summary>Returns an array of min max height values for all the renderable patches in a terrain.  The returned array can be modified and then passed to <see cref="OverrideMinMaxPatchHeights" />.</summary>
        ///<returns>Minimum and maximum height values for each patch.</returns>
        [FreeFunction(k_ScriptingInterfacePrefix + "GetPatchMinMaxHeights", HasExplicitThis = true)]
        extern public PatchExtents[] GetPatchMinMaxHeights();

        ///<summary>Override the minimum and maximum patch heights for every renderable terrain patch.  Note that the overriden values get reset when the terrain resolution is changed and stays unchanged when the terrain heightmap is painted or changed via script.</summary>
        ///<param name="minMaxHeights">Array of minimum and maximum terrain patch height values.</param>
        [FreeFunction(k_ScriptingInterfacePrefix + "OverrideMinMaxPatchHeights", HasExplicitThis = true)]
        extern public void OverrideMinMaxPatchHeights(PatchExtents[] minMaxHeights);

        ///<summary>Returns an array of tesselation maximum height error values per renderable terrain patch.  The returned array can be modified and passed to <see cref="OverrideMaximumHeightError" />.</summary>
        ///<returns>Float array of maximum height error values.</returns>
        [FreeFunction(k_ScriptingInterfacePrefix + "GetMaximumHeightError", HasExplicitThis = true)]
        extern public float[] GetMaximumHeightError();

        ///<summary>Override the maximum tessellation height error with user provided values.  Note that the overriden values get reset when the terrain resolution is changed and stays unchanged when the terrain heightmap is painted or changed via script.</summary>
        ///<param name="maxError">Provided maximum height error values.</param>
        [FreeFunction(k_ScriptingInterfacePrefix + "OverrideMaximumHeightError", HasExplicitThis = true)]
        extern public void OverrideMaximumHeightError(float[] maxError);

        ///<summary>Sets an array of heightmap samples.</summary>
        ///<remarks>Sets heightmap data using a two dimensional array of heightmap samples. The samples are represented as float values ranging from 0 to 1. The area affected is defined by the array dimensions and starts at xBase and yBase. The heights array is indexed as [y,x].
        ///
        ///Unlike <see cref="TerrainData.SetHeights" />, this method does not update the LOD information for the terrain, or any trees/vegetation objects; this means the terrain may be temporarily rendered at an inappropriately high level of detail, but makes the method fast enough to be used in interactive editing scenarios. Once modifications to the terrain have been completed - for example, when the user releases the mouse button - call <see cref="TerrainData.SyncHeightmap" /> to update all the LOD and vegetation information.</remarks>
        ///<param name="xBase">First x index of heightmap samples to set.</param>
        ///<param name="yBase">First y index of heightmap samples to set.</param>
        ///<param name="heights">Array of heightmap samples to set (values range from 0 to 1, array indexed as [y,x]).</param>
        public void SetHeightsDelayLOD(int xBase, int yBase, float[,] heights)
        {
            if (heights == null) throw new System.ArgumentNullException("heights");

            int height = heights.GetLength(0);
            int width = heights.GetLength(1);

            if (xBase < 0 || (xBase + width) < 0 || (xBase + width) > heightmapResolution)
                throw new System.ArgumentException(string.Format("X out of bounds - trying to set {0}-{1} but the terrain ranges from 0-{2}", xBase, xBase + width, heightmapResolution));

            if (yBase < 0 || (yBase + height) < 0 || (yBase + height) > heightmapResolution)
                throw new System.ArgumentException(string.Format("Y out of bounds - trying to set {0}-{1} but the terrain ranges from 0-{2}", yBase, yBase + height, heightmapResolution));

            Internal_SetHeightsDelayLOD(xBase, yBase, width, height, heights);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "SetHeightsDelayLOD", HasExplicitThis = true)]
        extern private void Internal_SetHeightsDelayLOD(int xBase, int yBase, int width, int height, float[,] heights);

        ///<summary>Gets whether a certain point at x,y is a hole.</summary>
        public bool IsHole(int x, int y)
        {
            if (x < 0 || x >= holesResolution || y < 0 || y >= holesResolution)
            {
                throw new ArgumentException("Trying to access out-of-bounds terrain holes information.");
            }

            return Internal_IsHole(x, y);
        }

        ///<summary>Gets an array of Terrain holes samples.</summary>
        ///<remarks>Returns a two-dimensional array of Terrain holes samples. The samples are represented as bool values: <c>true</c> for surface and <c>false</c> for hole. The array has the dimensions [height,width] and is indexed as [y,x].</remarks>
        ///<param name="xBase">First x index of Terrain holes samples to retrieve.</param>
        ///<param name="yBase">First y index of Terrain holes samples to retrieve.</param>
        ///<param name="width">Number of samples to retrieve along the Terrain holes x axis.</param>
        ///<param name="height">Number of samples to retrieve along the Terrain holes y axis.</param>
        public bool[,] GetHoles(int xBase, int yBase, int width, int height)
        {
            if (xBase < 0 || yBase < 0 || width <= 0 || height <= 0 || xBase + width > holesResolution || yBase + height > holesResolution)
            {
                throw new ArgumentException("Trying to access out-of-bounds terrain holes information.");
            }

            bool[,] holes = new bool[height, width];
            Internal_GetHoles(xBase, yBase, width, height, holes);
            return holes;
        }

        ///<summary>Sets an array of Terrain holes samples.</summary>
        ///<remarks>Sets Terrain holes data using a two-dimensional array of Terrain holes samples. The samples are represented as bool values: <c>true</c> for surface and <c>false</c> for hole. The array dimensions define the area affected, which starts at <c>xBase</c> and <c>yBase</c>. The Terrain holes array is indexed as [y,x].
        ///
        ///This method recomputes all LOD and vegetation information for the Terrain on each call, which can be computationally expensive. In interactive editing scenarios, it might be better to call <see cref="TerrainData.SetHolesDelayLOD" /> instead, followed by <see cref="TerrainData.SyncTexture" /> when the user completes an editing action.</remarks>
        ///<param name="xBase">First x index of Terrain holes samples to set.</param>
        ///<param name="yBase">First y index of Terrain holes samples to set.</param>
        ///<param name="holes">Array of Terrain holes samples to set (array indexed as [y,x]).</param>
        public void SetHoles(int xBase, int yBase, bool[,] holes)
        {
            if (holes == null) throw new ArgumentNullException("holes");

            int height = holes.GetLength(0);
            int width = holes.GetLength(1);

            if (xBase < 0 || (xBase + width) > holesResolution)
                throw new ArgumentException(string.Format("X out of bounds - trying to set {0}-{1} but the terrain ranges from 0-{2}", xBase, xBase + width, holesResolution));

            if (yBase < 0 || (yBase + height) > holesResolution)
                throw new ArgumentException(string.Format("Y out of bounds - trying to set {0}-{1} but the terrain ranges from 0-{2}", yBase, yBase + height, holesResolution));

            Internal_SetHoles(xBase, yBase, holes.GetLength(1), holes.GetLength(0), holes);
        }

        ///<summary>Sets an array of Terrain holes samples.</summary>
        ///<remarks>Sets Terrain holes data using a two-dimensional array of Terrain holes samples. The samples are represented as bool values: <c>true</c> for surface and <c>false</c> for hole. The array dimensions define the area affected, which starts at <c>xBase</c> and <c>yBase</c>. The Terrain holes array is indexed as [y,x].
        ///
        ///Unlike <see cref="TerrainData.SetHoles" />, this method does not update LOD information for the Terrain, or any tree/vegetation objects; this means that some tree/vegetation objects might still be present over holes, but makes the method fast enough to be used in interactive editing scenarios. After modifications to the Terrain are complete - for example, when the user releases the mouse button - call <see cref="TerrainData.SyncTexture" /> and use <see cref="TerrainData.HolesTextureName" /> as a Texture name to update all LOD and vegetation information.</remarks>
        ///<param name="xBase">First x index of Terrain holes samples to set.</param>
        ///<param name="yBase">First y index of Terrain holes samples to set.</param>
        ///<param name="holes">Array of Terrain holes samples to set (array indexed as [y,x]).</param>
        public void SetHolesDelayLOD(int xBase, int yBase, bool[,] holes)
        {
            if (holes == null) throw new ArgumentNullException("holes");

            int height = holes.GetLength(0);
            int width = holes.GetLength(1);

            if (xBase < 0 || (xBase + width) > holesResolution)
                throw new ArgumentException(string.Format("X out of bounds - trying to set {0}-{1} but the terrain ranges from 0-{2}", xBase, xBase + width, holesResolution));

            if (yBase < 0 || (yBase + height) > holesResolution)
                throw new ArgumentException(string.Format("Y out of bounds - trying to set {0}-{1} but the terrain ranges from 0-{2}", yBase, yBase + height, holesResolution));

            Internal_SetHolesDelayLOD(xBase, yBase, width, height, holes);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "SetHoles", HasExplicitThis = true)]
        extern private void Internal_SetHoles(int xBase, int yBase, int width, int height, bool[,] holes);

        [FreeFunction(k_ScriptingInterfacePrefix + "GetHoles", HasExplicitThis = true)]
        extern private void Internal_GetHoles(int xBase, int yBase, int width, int height, bool[,] holes);


        [FreeFunction(k_ScriptingInterfacePrefix + "IsHole", HasExplicitThis = true)]
        extern private bool Internal_IsHole(int x, int y);

        [FreeFunction(k_ScriptingInterfacePrefix + "SetHolesDelayLOD", HasExplicitThis = true)]
        extern private void Internal_SetHolesDelayLOD(int xBase, int yBase, int width, int height, bool[,] holes);

        ///<summary>Gets the gradient of the terrain at point (x,y). The gradient's value is always positive.</summary>
        ///<remarks>The <c>x</c> and <c>y</c> values are normalized coordinates in the range 0 to 1.</remarks>
        [NativeName(k_HeightmapPrefix + "GetSteepness")]
        extern public float GetSteepness(float x, float y);

        ///<summary>Get an interpolated normal vector at a given location on the heightmap.</summary>
        ///<remarks>The <c>x</c> and <c>y</c> parameters are normalized coordinates that specify a position on the heightmap.
        ///The function first computes surface normals at the surrounding grid points using the Sobel filter, then performs bilinear interpolation on these normals to calculate the final surface normal at the given location.
        ///
        ///This function does not loop or wrap around the heightmap. Coordinates outside the normalized range [0, 1] are extrapolated based on the nearest valid points, ensuring a valid normal vector is always returned.
        ///The returned normal is a unit vector.</remarks>
        ///<param name="x">The normalized x-coordinate of the location, in the range [0, 1].</param>
        ///<param name="y">The normalized y-coordinate of the location, in the range [0, 1].</param>
        ///<returns>A normalized normal vector representing the surface orientation at the given location.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleTerrainInterpolatedNormal : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        // Reference to the active terrain
        ///        Terrain terrain = Terrain.activeTerrain;
        ///        if (terrain == null)
        ///        {
        ///            Debug.LogError("No active terrain found.");
        ///            return;
        ///        }
        ///
        ///        // Get the TerrainData
        ///        TerrainData terrainData = terrain.terrainData;
        ///
        ///        // Example world position
        ///        Vector3 worldPosition = new Vector3(50f, 0f, 75f);
        ///
        ///        // Convert world position to normalized terrain coordinates
        ///        Vector3 terrainPosition = worldPosition - terrain.transform.position;
        ///        float normalizedX = Mathf.InverseLerp(0, terrainData.size.x, terrainPosition.x);
        ///        float normalizedZ = Mathf.InverseLerp(0, terrainData.size.z, terrainPosition.z);
        ///
        ///        // Ensure coordinates are within the valid range of [0, 1]
        ///        normalizedX = Mathf.Clamp01(normalizedX);
        ///        normalizedZ = Mathf.Clamp01(normalizedZ);
        ///
        ///        // Get the interpolated normal
        ///        Vector3 interpolatedNormal = terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);
        ///
        ///        // Output the normal vector
        ///        Debug.Log($"Interpolated Normal at ({worldPosition.x}, {worldPosition.z}): {interpolatedNormal}");
        ///        // Example output from a terrain with differing heights around the sampled position:
        ///        // Interpolated Normal at (50, 75): (-0.57, 0.67, 0.47)
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeName(k_HeightmapPrefix + "GetInterpolatedNormal")]
        extern public Vector3 GetInterpolatedNormal(float x, float y);

        [NativeName(k_HeightmapPrefix + "GetAdjustedSize")]
        extern internal int GetAdjustedSize(int size);

        ///<summary>Strength of the waving grass in the terrain.</summary>
        extern public float wavingGrassStrength
        {
            [NativeName(k_DetailDatabasePrefix + "GetWavingGrassStrength")]
            get;

            [FreeFunction(k_ScriptingInterfacePrefix + "SetWavingGrassStrength", HasExplicitThis = true)]
            set;
        }

        ///<summary>Amount of waving grass in the terrain.</summary>
        extern public float wavingGrassAmount
        {
            [NativeName(k_DetailDatabasePrefix + "GetWavingGrassAmount")]
            get;

            [FreeFunction(k_ScriptingInterfacePrefix + "SetWavingGrassAmount", HasExplicitThis = true)]
            set;
        }

        ///<summary>Speed of the waving grass.</summary>
        extern public float wavingGrassSpeed
        {
            [NativeName(k_DetailDatabasePrefix + "GetWavingGrassSpeed")]
            get;

            [FreeFunction(k_ScriptingInterfacePrefix + "SetWavingGrassSpeed", HasExplicitThis = true)]
            set;
        }

        ///<summary>Color of the waving grass that the terrain has.</summary>
        extern public Color wavingGrassTint
        {
            [NativeName(k_DetailDatabasePrefix + "GetWavingGrassTint")]
            get;

            [FreeFunction(k_ScriptingInterfacePrefix + "SetWavingGrassTint", HasExplicitThis = true)]
            set;
        }

        ///<summary>The resolution of the detail data stored in <c>TerrainData</c>.</summary>
        extern public int detailWidth
        {
            [NativeName(k_DetailDatabasePrefix + "GetWidth")]
            get;
        }

        ///<summary>The resolution of the detail data stored in <c>TerrainData</c>.</summary>
        extern public int detailHeight
        {
            [NativeName(k_DetailDatabasePrefix + "GetHeight")]
            get;
        }

        ///<summary>The maximum value of each sample in the detail map of the terrain data.</summary>
        ///<remarks>This value will depend on the set <see cref="DetailScatterMode" />. In <see cref="DetailScatterMode.CoverageMode" />, values of up to 255 are stored.
        ///                    In <see cref="DetailScatterMode.InstanceCountMode" />, values of up to 16 are stored.</remarks>
        extern public int maxDetailScatterPerRes
        {
            [NativeName(k_DetailDatabasePrefix + "GetMaximumScatterPerRes")]
            get;
        }

        ///<summary>Sets the resolution of the detail map.</summary>
        ///<param name="detailResolution">Specifies the number of pixels in the detail resolution map. A larger detailResolution, leads to more accurate detail object painting.</param>
        ///<param name="resolutionPerPatch">Specifies the size in pixels of each individually rendered detail patch. A larger number reduces draw calls, but might increase triangle count since detail patches are culled on a per batch basis. A recommended value is 16. If you use a very large detail object distance and your grass is very sparse, it makes sense to increase the value.</param>
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

        ///<summary>Sets the <see cref="DetailScatterMode" />.</summary>
        ///<param name="scatterMode">Sets the <see cref="DetailScatterMode" /> that specifies how to represent detail density at each sample of the detail resolution map.</param>
        public void SetDetailScatterMode(DetailScatterMode scatterMode)
        {
            Internal_SetDetailScatterMode(scatterMode);
        }

        [NativeName(k_DetailDatabasePrefix + "SetDetailScatterMode")]
        extern private void Internal_SetDetailScatterMode(DetailScatterMode scatterMode);

        ///<summary>The number of patches along a terrain tile edge. This is squared to make a grid of patches.</summary>
        extern public int detailPatchCount
        {
            [NativeName(k_DetailDatabasePrefix + "GetPatchCount")]
            get;
        }

        ///<summary>Detail Resolution of the TerrainData.</summary>
        extern public int detailResolution
        {
            [NativeName(k_DetailDatabasePrefix + "GetResolution")]
            get;
        }

        ///<summary>Detail Resolution of each patch. A larger value will decrease the number of batches used by detail objects.</summary>
        extern public int detailResolutionPerPatch
        {
            [NativeName(k_DetailDatabasePrefix + "GetResolutionPerPatch")]
            get;
        }

        ///<seealso cref="DetailScatterMode" />
        extern public DetailScatterMode detailScatterMode
        {
            [NativeName(k_DetailDatabasePrefix + "GetDetailScatterMode")]
            get;
        }

        [NativeName(k_DetailDatabasePrefix + "ResetDirtyDetails")]
        extern internal void ResetDirtyDetails();

        ///<summary>Reloads all the values of the available prototypes (ie, detail mesh assets) in the TerrainData Object.</summary>
        ///<remarks>This can be used in editor scripts to update the terrain when the prototype assets change, much like the Terrain &gt; Refresh Tree and Detail Prototypes menu command.</remarks>
        [FreeFunction(k_ScriptingInterfacePrefix + "RefreshPrototypes", HasExplicitThis = true)]
        extern public void RefreshPrototypes();

        ///<summary>Contains the detail texture/meshes that the Terrain has.</summary>
        ///<remarks>For more information, see <see cref="DetailPrototype" />.</remarks>
        extern public DetailPrototype[] detailPrototypes
        {
            [FreeFunction(k_ScriptingInterfacePrefix + "GetDetailPrototypes", HasExplicitThis = true)]
            get;

            [FreeFunction(k_ScriptingInterfacePrefix + "SetDetailPrototypes", HasExplicitThis = true)]
            set;
        }

        ///<summary>Returns an array of all supported detail layer indices in the area.</summary>
        ///<remarks>The Terrain uses a detail layer density map. Each pixel in the map determines the amount of details objects that will be procedurally placed in the pixel area.
        ///The layer determines the detail prototype that will be instantiated at the location.</remarks>
        [FreeFunction(k_ScriptingInterfacePrefix + "GetSupportedLayers", HasExplicitThis = true)]
        extern public int[] GetSupportedLayers(int xBase, int yBase, int totalWidth, int totalHeight);

        ///<summary>Returns an array of all supported detail layer indices in the area.</summary>
        ///<remarks>The Terrain uses a detail layer density map. Each pixel in the map determines the amount of details objects that will be procedurally placed in the pixel area.
        ///The layer determines the detail prototype that will be instantiated at the location.</remarks>
        public int[] GetSupportedLayers(Vector2Int positionBase, Vector2Int size)
        {
            return GetSupportedLayers(positionBase.x, positionBase.y, size.x, size.y);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "GetDetailLayer", HasExplicitThis = true)]
        extern void GetDetailLayer(int xBase, int yBase, int width, int height, int layer, int[,] detailLayer);

        ///<summary>Returns a 2D array of the detail object density (i.e. the number of detail objects for this layer) in the specific location.</summary>
        ///<remarks>The Terrain system uses detail layer density maps. Each map is essentially a grayscale image, where each pixel value denotes the number of detail objects that will be procedurally placed Terrain area. That corresponds to the pixel. Since several different detail types may be used, the map is arranged
        /// into "layers" - the array indices of the layers are determined by the order of the detail types defined
        /// in the Terrain inspector (i.e. when the Paint Details tool is selected).
        ///
        ///The returned array has the dimensions [height,width] and is indexed as [y,x].</remarks>
        ///<param name="xBase">First x index of detail object density data to retrieve.</param>
        ///<param name="yBase">First y index of detail object density data to retrieve.</param>
        ///<param name="width">The amount of detail object density data to retrieve along the Terrain's x axis.</param>
        ///<param name="height">The amount of detail object density data to retrieve along the Terrain's z axis.</param>
        ///<param name="layer">The index of the detail in the /TerrainData.detailPrototypes/ array.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Set all pixels in a detail map below a certain threshold to zero.
        ///    void DetailMapCutoff(Terrain t, float threshold)
        ///    {
        ///        // Get all of layer zero.
        ///        var map = t.terrainData.GetDetailLayer(0, 0, t.terrainData.detailWidth, t.terrainData.detailHeight, 0);
        ///
        ///        // For each pixel in the detail map...
        ///        for (var y = 0; y < t.terrainData.detailHeight; y++)
        ///        {
        ///            for (var x = 0; x < t.terrainData.detailWidth; x++)
        ///            {
        ///                // If the pixel value is below the threshold then
        ///                // set it to zero.
        ///                if (map[y, x] < threshold)
        ///                {
        ///                    map[y, x] = 0;
        ///                }
        ///            }
        ///        }
        ///
        ///        // Assign the modified map back.
        ///        t.terrainData.SetDetailLayer(0, 0, 0, map);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public int[,] GetDetailLayer(int xBase, int yBase, int width, int height, int layer)
        {
            int[,] detailLayer = new int[height, width];
            GetDetailLayer(xBase, yBase, width, height, layer, detailLayer);
            return detailLayer;
        }

        public int[,] GetDetailLayer(Vector2Int positionBase, Vector2Int size, int layer)
        {
            return GetDetailLayer(positionBase.x, positionBase.y, size.x, size.y, layer);
        }

        ///<summary>This function computes and returns an array of detail object transforms for the specified patch and the specified prototype. You can use this function to retrieve the exact same transform data the Unity engine uses for detail rendering.</summary>
        ///<param name="patchX">The x index of the patch.</param>
        ///<param name="patchY">The y index of the patch.</param>
        ///<param name="layer">The prototype index.</param>
        ///<param name="density">The density setting of the detail.</param>
        ///<param name="bounds">Returns the bounds of the detail objects.</param>
        ///<seealso cref="DetailInstanceTransform" />
        [FreeFunction(k_ScriptingInterfacePrefix + "ComputeDetailInstanceTransforms", HasExplicitThis = true)]
        extern public DetailInstanceTransform[] ComputeDetailInstanceTransforms(int patchX, int patchY, int layer, float density, out Bounds bounds);


        ///<summary>This function computes and returns the coverage (how many instances fit in a square unit) of a detail prototype, given its index.</summary>
        ///<remarks>Computes detail coverage. In  <see cref="DetailScatterMode.CoverageMode" />, this coverage represents the number of scattered instances per unit squared, based on the detail prototype's size and density settings. Unavailable in <see cref="DetailScatterMode.InstanceCountMode" />.</remarks>
        [FreeFunction(k_ScriptingInterfacePrefix + "ComputeDetailCoverage", HasExplicitThis = true)]
        extern public float ComputeDetailCoverage(int detailPrototypeIndex);

        ///<summary>Sets the detail layer density map.</summary>
        ///<remarks>The Terrain system uses detail layer density maps. Each map is essentially a grayscale image
        /// where each pixel value specifies the number of detail objects to procedurally place in the terrain area that corresponds to the pixel. These values depend on which <see cref="DetailScatterMode" /> is set. Because several different detail types may be used, the map is arranged
        /// into "layers" - the array indices of the layers are determined by the order of the detail types defined
        ///in the Terrain inspector (ie, when the Paint Details tool is selected).
        ///
        ///The <c>details</c> array must have the dimensions [height,width] and is indexed as [y,x].</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    // Set all pixels in a detail map below a certain threshold to zero.
        ///    void DetailMapCutoff(Terrain t, float threshold)
        ///    {
        ///        // Get all of layer zero.
        ///        var map = t.terrainData.GetDetailLayer(0, 0, t.terrainData.detailWidth, t.terrainData.detailHeight, 0);
        ///
        ///        // For each pixel in the detail map...
        ///        for (int y = 0; y < t.terrainData.detailHeight; y++)
        ///        {
        ///            for (int x = 0; x < t.terrainData.detailWidth; x++)
        ///            {
        ///                // If the pixel value is below the threshold then
        ///                // set it to zero.
        ///                if (map[y, x] < threshold)
        ///                {
        ///                    map[y, x] = 0;
        ///                }
        ///            }
        ///        }
        ///
        ///        // Assign the modified map back.
        ///        t.terrainData.SetDetailLayer(0, 0, 0, map);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void SetDetailLayer(int xBase, int yBase, int layer, int[,] details)
        {
            Internal_SetDetailLayer(xBase, yBase, details.GetLength(1), details.GetLength(0), layer, details);
        }

        public void SetDetailLayer(Vector2Int basePosition, int layer, int[,] details)
        {
            SetDetailLayer(basePosition.x, basePosition.y, layer, details);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "SetDetailLayer", HasExplicitThis = true)]
        extern private void Internal_SetDetailLayer(int xBase, int yBase, int totalWidth, int totalHeight, int detailIndex, int[,] data);

        ///<summary>Returns an array of detail patches, which are each identified by X-Z coordinates. Detail objects in the patches are clamped to the maximum count.</summary>
        ///<remarks>This function is only available in the Editor.</remarks>
        ///<param name="density">The detail density value. See <see cref="Terrain.detailObjectDensity" />.</param>
        [FreeFunction(k_ScriptingInterfacePrefix + "GetClampedDetailPatches", HasExplicitThis = true)]
        extern public Vector2Int[] GetClampedDetailPatches(float density);

        ///<summary>Contains the current trees placed in the terrain.</summary>
        ///<remarks>Note that setting the treeInstances property will not automatically snap Tree instances onto the surface of the Terrain heightmap. To do so, use <see cref="SetTreeInstances" /> instead.</remarks>
        public TreeInstance[] treeInstances
        {
            get
            {
                return Internal_GetTreeInstances();
            }

            set
            {
                SetTreeInstances(value, false);
            }
        }

        [NativeName(k_TreeDatabasePrefix + "GetInstances")]
        extern private TreeInstance[] Internal_GetTreeInstances();

        ///<summary>Sets the Tree Instance array, and optionally snaps Trees onto the surface of the Terrain heightmap.</summary>
        ///<param name="instances">The array of <see cref="TreeInstance" /> objects.</param>
        ///<param name="snapToHeightmap">Specifies whether to snap Trees to the Terrain heightmap.</param>
        ///<seealso cref="treeInstances" />
        [FreeFunction(k_ScriptingInterfacePrefix + "SetTreeInstances", HasExplicitThis = true)]
        extern public void SetTreeInstances([NotNull] TreeInstance[] instances, bool snapToHeightmap);

        ///<summary>Gets the tree instance at the specified index. It is used as a faster version of <see cref="treeInstances" />[index] as this function doesn't create the entire tree instances array.</summary>
        ///<param name="index">The index of the tree instance.</param>
        public TreeInstance GetTreeInstance(int index)
        {
            if (index < 0 || index >= treeInstanceCount)
                throw new ArgumentOutOfRangeException("index");

            return Internal_GetTreeInstance(index);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "GetTreeInstance", HasExplicitThis = true)]
        extern private TreeInstance Internal_GetTreeInstance(int index);

        ///<summary>Sets the tree instance with new parameters at the specified index. However, you cannot change <see cref="TreeInstance.prototypeIndex" /> and <see cref="TreeInstance.position" />. If you change them, the method throws an ArgumentException.</summary>
        ///<param name="index">The index of the tree instance.</param>
        ///<param name="instance">The new TreeInstance value.</param>
        [FreeFunction(k_ScriptingInterfacePrefix + "SetTreeInstance", HasExplicitThis = true, ThrowsException = true)]
        extern public void SetTreeInstance(int index, TreeInstance instance);

        ///<summary>Returns the number of tree instances.</summary>
        extern public int treeInstanceCount
        {
            [NativeName(k_TreeDatabasePrefix + "GetInstances().size")]
            get;
        }

        ///<summary>The list of <see cref="TreePrototype" />s available in the inspector.</summary>
        ///<remarks>If you change any value here, you should call <see cref="TerrainData.RefreshPrototypes" /> so the changes take effect.</remarks>
        extern public TreePrototype[] treePrototypes
        {
            [FreeFunction(k_ScriptingInterfacePrefix + "GetTreePrototypes", HasExplicitThis = true)]
            get;

            [FreeFunction(k_ScriptingInterfacePrefix + "SetTreePrototypes", HasExplicitThis = true)]
            set;
        }

        [NativeName(k_TreeDatabasePrefix + "RemoveTreePrototype")]
        extern internal void RemoveTreePrototype(int index);

        ///<summary>Removes the detail prototype at the specified index.</summary>
        ///<param name="index">The index of the detail prototype.</param>
        [NativeName(k_DetailDatabasePrefix + "RemoveDetailPrototype")]
        extern public void RemoveDetailPrototype(int index);

        [NativeName(k_TreeDatabasePrefix + "NeedUpgradeScaledPrototypes")]
        extern internal bool NeedUpgradeScaledTreePrototypes();

        [FreeFunction(k_ScriptingInterfacePrefix + "UpgradeScaledTreePrototype", HasExplicitThis = true)]
        extern internal void UpgradeScaledTreePrototype();

        ///<summary>Number of alpha map layers.</summary>
        extern public int alphamapLayers
        {
            [NativeName(k_SplatDatabasePrefix + "GetSplatCount")]
            get;
        }

        ///<summary>Returns the alpha map at a position x, y given a width and height.</summary>
        ///<remarks>The returned array is three-dimensional - the first two dimensions
        ///represent y and x coordinates on the map, while the third denotes the
        ///splatmap texture to which the alphamap is applied.</remarks>
        ///<param name="x">The x offset to read from.</param>
        ///<param name="y">The y offset to read from.</param>
        ///<param name="width">The width of the alpha map area to read.</param>
        ///<param name="height">The height of the alpha map area to read.</param>
        ///<returns>A 3D array of floats, where the 3rd dimension represents the mixing weight of each splatmap at each x,y coordinate.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    // Add some random "noise" to the alphamaps.
        ///    void AddAlphaNoise(Terrain t, float noiseScale)
        ///    {
        ///        float[,,] maps = t.terrainData.GetAlphamaps(0, 0, t.terrainData.alphamapWidth, t.terrainData.alphamapHeight);
        ///
        ///        for (int y = 0; y < t.terrainData.alphamapHeight; y++)
        ///        {
        ///            for (int x = 0; x < t.terrainData.alphamapWidth; x++)
        ///            {
        ///                float a0 = maps[y, x, 0];
        ///                float a1 = maps[y, x, 1];
        ///
        ///                a0 += Random.value * noiseScale;
        ///                a1 += Random.value * noiseScale;
        ///
        ///                float total = a0 + a1;
        ///
        ///                maps[y, x, 0] = a0 / total;
        ///                maps[y, x, 1] = a1 / total;
        ///            }
        ///        }
        ///
        ///        t.terrainData.SetAlphamaps(0, 0, maps);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public float[,,] GetAlphamaps(int x, int y, int width, int height)
        {
            if (x < 0 || y < 0 || width < 0 || height < 0)
                throw new ArgumentException("Invalid argument for GetAlphaMaps");

            OutArray3D<float> alphamaps = default;
            Internal_GetAlphamaps(x, y, width, height, in alphamaps);
            return alphamaps.Value;

        }

        [FreeFunction(k_ScriptingInterfacePrefix + "GetAlphamaps", HasExplicitThis = true)]
        extern private void Internal_GetAlphamaps(int x, int y, int width, int height, in OutArray3D<float> alphamaps);


        ///<summary>The size of the alpha map in texels for either the width or the height.</summary>
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

        ///<summary>Width of the alpha map.</summary>
        public int alphamapWidth { get { return alphamapResolution; } }

        ///<summary>Height of the alpha map. (Read only.)</summary>
        public int alphamapHeight { get { return alphamapResolution; } }

        ///<summary>Resolution of the base map used for rendering far patches on the terrain.</summary>
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

        ///<summary>Assign all splat values in the given map area.</summary>
        ///<remarks>The array supplied to this function determines the width and height
        ///of the portion to be replaced. The third dimension of the array
        ///corresponds to the number of splatmap textures. Note that the order of the array is [y,x,i].</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    public Terrain t;
        ///    // Blend the two terrain textures according to the steepness of
        ///    // the slope at each point.
        ///    void Start()
        ///    {
        ///        float[,,] map = new float[t.terrainData.alphamapWidth, t.terrainData.alphamapHeight, 2];
        ///
        ///        // For each point on the alphamap...
        ///        for (int y = 0; y < t.terrainData.alphamapHeight; y++)
        ///        {
        ///            for (int x = 0; x < t.terrainData.alphamapWidth; x++)
        ///            {
        ///                // Get the normalized terrain coordinate that
        ///                // corresponds to the point.
        ///                float normX = x * 1.0f / (t.terrainData.alphamapWidth - 1);
        ///                float normY = y * 1.0f / (t.terrainData.alphamapHeight - 1);
        ///
        ///                // Get the steepness value at the normalized coordinate.
        ///                var angle = t.terrainData.GetSteepness(normX, normY);
        ///
        ///                // Steepness is given as an angle, 0..90 degrees. Divide
        ///                // by 90 to get an alpha blending value in the range 0..1.
        ///                var frac = angle / 90.0;
        ///
        ///                // Note the y and x are not in the traditional order.
        ///                map[y, x, 0] = (float)frac;
        ///                map[y, x, 1] = (float)(1 - frac);
        ///            }
        ///        }
        ///        t.terrainData.SetAlphamaps(0, 0, map);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void SetAlphamaps(int x, int y, float[,,] map)
        {
            if (map.GetLength(2) != alphamapLayers)
            {
                throw new System.Exception(string.Format("Float array size wrong (layers should be {0})", alphamapLayers));
            }

            // TODO: crop the map or throw if outside.

            Internal_SetAlphamaps(x, y, map.GetLength(1), map.GetLength(0), map);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "SetAlphamaps", HasExplicitThis = true)]
        extern private void Internal_SetAlphamaps(int x, int y, int width, int height, float[,,] map);

        ///<summary>Marks the terrain data as dirty to trigger an update of the terrain basemap texture.</summary>
        [NativeName(k_SplatDatabasePrefix + "SetBaseMapsDirty")]
        extern public void SetBaseMapDirty();

        ///<summary>Returns the alphamap texture at the specified index.</summary>
        ///<param name="index">Index of the alphamap.</param>
        ///<returns>Alphamap texture at the specified index.</returns>
        [NativeName(k_SplatDatabasePrefix + "GetAlphaTexture")]
        extern public Texture2D GetAlphamapTexture(int index);

        ///<summary>Returns the number of alphamap textures.</summary>
        public extern int alphamapTextureCount
        {
            [NativeName(k_SplatDatabasePrefix + "GetAlphaTextureCount")]
            get;
        }

        ///<summary>Alpha map textures used by the Terrain. Used by Terrain Inspector for undo.</summary>
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

        ///<summary>Obsolete. Use <see cref="TerrainData.terrainLayers" /> instead. Splat texture used by the terrain.</summary>
        ///<remarks>These are the ground textures.</remarks>
        [Obsolete("TerrainData.splatPrototypes is obsolete. Use TerrainData.terrainLayers instead.", false)]
        extern public SplatPrototype[] splatPrototypes
        {
            [FreeFunction(k_ScriptingInterfacePrefix + "GetSplatPrototypes", HasExplicitThis = true)]
            get;

            [FreeFunction(k_ScriptingInterfacePrefix + "SetSplatPrototypes", HasExplicitThis = true)]
            set;
        }

        ///<summary>Retrieves the terrain layers used by the current terrain.</summary>
        ///<seealso cref="M:UnityEngine.TerrainData.SetTerrainLayersRegisterUndo(TerrainLayer[], string)" />
        extern public TerrainLayer[] terrainLayers
        {
            [FreeFunction(k_ScriptingInterfacePrefix + "GetTerrainLayers", HasExplicitThis = true)]
            [return: UnityMarshalAs(NativeType.ScriptingObjectPtr)]
            get;

            [FreeFunction(k_ScriptingInterfacePrefix + "SetTerrainLayers", HasExplicitThis = true)]
            [param: UnityMarshalAs(NativeType.ScriptingObjectPtr)] set;
        }

        ///<summary>This function sets the <see cref="terrainLayers" /> property, and in addition, registers the action to the Editor's undo stack.</summary>
        ///<remarks>This function is only available in the Editor.</remarks>
        ///<param name="terrainLayers">The Terrain Layer assets to set.</param>
        ///<param name="undoName">The name of the Editor's undo action.</param>
        public void SetTerrainLayersRegisterUndo(TerrainLayer[] terrainLayers, string undoName)
        {
            if (string.IsNullOrEmpty(undoName))
            {
                // The native code will skip creating undo if the name is empty (for the native path without undo).
                // Make sure we don't hit that path by using an empty string.
                throw new ArgumentNullException("undoName");
            }
            Internal_SetTerrainLayersRegisterUndo(terrainLayers, undoName);
        }

        [FreeFunction(k_ScriptingInterfacePrefix + "SetTerrainLayersRegisterUndo", HasExplicitThis = true)]
        extern private void Internal_SetTerrainLayersRegisterUndo([UnityMarshalAs(NativeType.ScriptingObjectPtr)] TerrainLayer[] terrainLayers, string undoName);

        [NativeName(k_TreeDatabasePrefix + "AddTree")]
        extern internal void AddTree(ref TreeInstance tree);

        [NativeName(k_TreeDatabasePrefix + "RemoveTrees")]
        extern internal int RemoveTrees(Vector2 position, float radius, int prototypeIndex);

        [NativeName(k_HeightmapPrefix + "CopyHeightmapFromActiveRenderTexture")]
        private extern void Internal_CopyActiveRenderTextureToHeightmap(RectInt rect, int destX, int destY, TerrainHeightmapSyncControl syncControl);

        [NativeName(k_HeightmapPrefix + "DirtyHeightmapRegion")]
        private extern void Internal_DirtyHeightmapRegion(int x, int y, int width, int height, TerrainHeightmapSyncControl syncControl);

        ///<summary>Performs synchronization queued by previous calls to <see cref="CopyActiveRenderTextureToHeightmap" /> and <see cref="DirtyHeightmapRegion" />, which makes the height data and LOD data used for tessellation up to date.</summary>
        [NativeName(k_HeightmapPrefix + "SyncHeightmapGPUModifications")]
        public extern void SyncHeightmap();

        [NativeName(k_HeightmapPrefix + "CopyHolesFromActiveRenderTexture")]
        private extern void Internal_CopyActiveRenderTextureToHoles(RectInt rect, int destX, int destY, bool allowDelayedCPUSync);

        [NativeName(k_HeightmapPrefix + "DirtyHolesRegion")]
        private extern void Internal_DirtyHolesRegion(int x, int y, int width, int height, bool allowDelayedCPUSync);

        [NativeName(k_HeightmapPrefix + "SyncHolesGPUModifications")]
        private extern void Internal_SyncHoles();

        [NativeName(k_SplatDatabasePrefix + "MarkDirtyRegion")]
        private extern void Internal_MarkAlphamapDirtyRegion(int alphamapIndex, int x, int y, int width, int height);

        [NativeName(k_SplatDatabasePrefix + "ClearDirtyRegion")]
        private extern void Internal_ClearAlphamapDirtyRegion(int alphamapIndex);

        [NativeName(k_SplatDatabasePrefix + "SyncGPUModifications")]
        private extern void Internal_SyncAlphamaps();

        extern internal TextureFormat atlasFormat
        {
            [NativeName(k_DetailDatabasePrefix + "GetAtlasTextureFormat")]
            get;
        }

        internal extern Terrain[] users
        {
            get;
        }
    }
}
