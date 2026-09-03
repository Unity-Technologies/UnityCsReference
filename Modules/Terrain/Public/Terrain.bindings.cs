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
    ///<summary>Indicate the types of changes to the terrain in OnTerrainChanged callback.</summary>
    ///<remarks>
    ///  <para>Use bitwise AND to detect multiple changes.</para>
    ///  <para>The above example shows how you can detect terrain changes by using OnTerrainChanged callback and TerrainChangedFlags enum.</para>
    ///</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using UnityEngine;
    ///
    ///[ExecuteInEditMode]
    ///public class DetectTerrainChanges : MonoBehaviour
    ///{
    ///    void OnTerrainChanged(TerrainChangedFlags flags)
    ///    {
    ///        if ((flags & TerrainChangedFlags.Heightmap) != 0)
    ///        {
    ///            Debug.Log("Heightmap changes");
    ///        }
    ///
    ///        if ((flags & TerrainChangedFlags.DelayedHeightmapUpdate) != 0)
    ///        {
    ///            Debug.Log("Heightmap painting");
    ///        }
    ///
    ///        if ((flags & TerrainChangedFlags.TreeInstances) != 0)
    ///        {
    ///            Debug.Log("Tree changes");
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [Flags]
    public enum TerrainChangedFlags
    {
        ///<summary>Indicates a change to the heightmap data.</summary>
        ///<remarks>This flag is set when heightmap data has been changed, for instance after <see cref="TerrainData.SetHeights" /> calls. Note that <see cref="TerrainData.SetHeightsDelayLOD" /> doesn't set this flag, even though the heightmap data is updated. See <see cref="DelayedHeightmapUpdate" />.</remarks>
        Heightmap = 1,
        ///<summary>Indicates a change to the tree data.</summary>
        TreeInstances = 2,
        ///<summary>Indicates a change to the heightmap data without computing LOD.</summary>
        ///<remarks>This flag is set after calls to <see cref="TerrainData.SetHeightsDelayLOD" />.</remarks>
        DelayedHeightmapUpdate = 4,
        ///<summary>Indicates that a change was made to the terrain that was so significant that the internal rendering data need to be flushed and recreated.</summary>
        ///<remarks>This flag is set when the terrain is loaded or when tree or detail prototypes are changed.</remarks>
        FlushEverythingImmediately = 8,
        ///<summary>Indicates a change to the detail data.</summary>
        RemoveDirtyDetailsImmediately = 16,
        ///<summary>Indicates a change to the heightmap resolution.</summary>
        HeightmapResolution = 32,
        ///<summary>Indicates a change to the Terrain holes data.</summary>
        ///<remarks>This flag is set when there are changes to the Terrain holes data, for example, after calls to <see cref="TerrainData.SetHoles" />. Note that calls to <see cref="TerrainData.SetHolesDelayLOD" /> don't set this flag, even though those calls update the Terrain holes data. See <see cref="DelayedHolesUpdate" />.</remarks>
        Holes = 64,
        ///<summary>Indicates a change to the Terrain holes data, which doesn't include LOD calculations and tree/vegetation updates.</summary>
        ///<remarks>This flag is set after calls to <see cref="TerrainData.SetHolesDelayLOD" />.</remarks>
        DelayedHolesUpdate = 128,
        ///<summary>Indicates that the TerrainData object is about to be destroyed.</summary>
        WillBeDestroyed = 256,
    }

    ///<summary>Enum provding terrain rendering options.</summary>
    [Flags]
    public enum TerrainRenderFlags
    {
        ///<exclude />
        [Obsolete("TerrainRenderFlags.heightmap is obsolete, use TerrainRenderFlags.Heightmap instead. (UnityUpgradable) -> Heightmap")]
        heightmap = 1,

        ///<exclude />
        [Obsolete("TerrainRenderFlags.trees is obsolete, use TerrainRenderFlags.Trees instead. (UnityUpgradable) -> Trees")]
        trees = 2,

        ///<exclude />
        [Obsolete("TerrainRenderFlags.details is obsolete, use TerrainRenderFlags.Details instead. (UnityUpgradable) -> Details")]
        details = 4,

        ///<exclude />
        [Obsolete("TerrainRenderFlags.all is obsolete, use TerrainRenderFlags.All instead. (UnityUpgradable) -> All")]
        all = All,

        ///<summary>Render heightmap.</summary>
        Heightmap = 1,
        ///<summary>Render trees.</summary>
        Trees = 2,
        ///<summary>Render terrain details.</summary>
        Details = 4,
        ///<summary>Render all options.</summary>
        All = Heightmap | Trees | Details
    }

    ///<summary>The Terrain component renders the terrain.</summary>
    [UsedByNativeCode]
    [NativeHeader("Modules/Terrain/Public/Terrain.h")]
    [NativeHeader("Runtime/Interfaces/ITerrainManager.h")]
    [NativeHeader("TerrainScriptingClasses.h")]
    [global::UnityEngine.NativeClass("Terrain", PersistentTypeId = 218)]
    [StaticAccessor("GetITerrainManager()", StaticAccessorType.Arrow)]
    public sealed partial class Terrain : Behaviour
    {
        ///<summary>The Terrain Data that stores heightmaps, terrain textures, detail meshes and trees.</summary>
        extern public TerrainData terrainData { get; set; }

        ///<summary>The maximum distance at which trees are rendered.</summary>
        ///<remarks>The higher this is, the further the distance trees can be seen and the slower it will run.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Terrain.activeTerrain.treeDistance = 2000;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Terrain.treeBillboardDistance" />
        extern public float treeDistance { get; set; }

        ///<summary>Distance from the camera where trees will be rendered as billboards only.</summary>
        ///<remarks>Decreasing this value improves performance but makes the transition look worse
        ///because the difference between billboards and trees will be more obvious.</remarks>
        extern public float treeBillboardDistance { get; set; }

        ///<summary>Total distance delta that trees will use to transition from billboard orientation to mesh orientation.</summary>
        ///<remarks>Decreasing this value makes the transition happen faster.
        ///Setting it to 0 will produce a visible pop when switching from mesh to billboard representation.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Terrain.activeTerrain.treeCrossFadeLength = 20;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float treeCrossFadeLength { get; set; }

        ///<summary>Maximum number of trees rendered at full LOD.</summary>
        ///<remarks>This is an easy setting to prevent too many trees being rendered at too high resolution in dense forests.
        ///Since there will be no fade if <c>treeMaximumFullLODCount</c> is exceeded you should tweak the <c>treeBillboardDistance</c> to
        ///not include unnecessary trees that are not being seen but, still rendered.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Terrain.activeTerrain.treeMaximumFullLODCount = 200;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public int treeMaximumFullLODCount { get; set; }

        ///<summary>Detail objects will be displayed up to this distance.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Terrain.activeTerrain.detailObjectDistance = 40;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="detailObjectDensity" />
        extern public float detailObjectDistance { get; set; }

        ///<summary>Density of detail objects.</summary>
        ///<remarks>This number goes from 0.0 to 1.0, with 1.0 being the original density, and lower numbers
        ///resulting in less detail objects being rendered.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Terrain.activeTerrain.detailObjectDensity = 0.5f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="detailObjectDistance" />
        extern public float detailObjectDensity { get; set; }

        ///<summary>An approximation of how many pixels the terrain will pop in the worst case when switching lod.</summary>
        ///<remarks>A higher value reduces the number of polygons drawn.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Terrain.activeTerrain.heightmapPixelError = 10;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float heightmapPixelError { get; set; }
        ///<summary>Limits the Terrain's maximum rendering resolution.</summary>
        ///<remarks>Use on low-end graphics cards to block the highest level of detail for Terrain.
        ///                    A value of 0 for this property allows the terrain to be shown at the highest detail. A value of 1 reduces the maximum triangle count to one-fourth of its current value and halves the width and height of the heightmap resolution.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Terrain.activeTerrain.heightmapMaximumLOD = 1;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public int heightmapMaximumLOD { get; set; }
        ///<summary>Limits how simplified the rendered terrain can be.</summary>
        ///<remarks>Sets the lowest level of simplification for the Terrain. Also affects areas that are outside of the camera frustum. Use this property to correct a situation where Terrain that is outside of the camera's view casts an overly simplified shadow inside the camera's view.
        ///                A value of 0 means there's no limit on reducing the Terrain's level of detail. Each increment of the value quadruples the minimum number of triangles used to render the Terrain. A high value can reduce performance because of high culling time.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Terrain.activeTerrain.heightmapMinimumLODSimplification = 2;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public int heightmapMinimumLODSimplification { get; set; }

        ///<summary>Heightmap patches beyond basemap distance will use a precomputed low res basemap.</summary>
        ///<remarks>This improves performance for far away patches. Close up Unity renders the heightmap using splat maps by blending between
        ///any amount of provided terrain textures.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Terrain.activeTerrain.basemapDistance = 100;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float basemapDistance { get; set; }

        ///<summary>The index of the baked lightmap applied to this terrain.</summary>
        ///<seealso cref="Renderer.lightmapIndex" />
        [NativeProperty("StaticLightmapIndexInt")]
        extern public int lightmapIndex { get; set; }

        ///<summary>The index of the realtime lightmap applied to this terrain.</summary>
        ///<seealso cref="Renderer.realtimeLightmapIndex" />
        [NativeProperty("DynamicLightmapIndexInt")]
        [Obsolete("Enlighten realtime Global Illumination is deprecated and will be removed in a future release. #from(6000.7)", false)]
        extern public int realtimeLightmapIndex { get; set; }

        ///<summary>The UV scale &amp; offset used for a baked lightmap.</summary>
        ///<seealso cref="Renderer.lightmapScaleOffset" />
        [NativeProperty("StaticLightmapST")]
        extern public Vector4 lightmapScaleOffset { get; set; }

        ///<summary>The UV scale &amp; offset used for a realtime lightmap.</summary>
        ///<seealso cref="Renderer.realtimeLightmapScaleOffset" />
        [NativeProperty("DynamicLightmapST")]
        [Obsolete("Enlighten realtime Global Illumination is deprecated and will be removed in a future release. #from(6000.7)", false)]
        extern public Vector4 realtimeLightmapScaleOffset { get; set; }

        [Obsolete("Terrain.freeUnusedRenderingResources is obsolete; use keepUnusedRenderingResources instead.")]
        [NativeProperty("FreeUnusedRenderingResourcesObsolete")]
        extern public bool freeUnusedRenderingResources { get; set; }

        ///<summary>Defines whether Unity frees per-Camera rendering resources for the Terrain when those resources aren't in use after a certain number of frames.</summary>
        ///<remarks>By default, this property is <c>false</c>, which implies that Unity deletes these rendering resources from memory if the Camera they're associated with hasn't rendered for 100 frames. You might sometimes not want this behavior because the next time these resources are required (for example, when that Camera starts to render again), you must reallocate them, which can negatively impact performance. In such cases, set this property to <c>true</c> to keep those resources in memory unless the Camera they're associated with is destroyed. You can also use <see cref="Terrain.SetKeepUnusedCameraRenderingResources" /> and <see cref="Terrain.GetKeepUnusedCameraRenderingResources" /> to configure the setting for a specific Camera.
        ///
        ///The value is not serialized with Terrain component.</remarks>
        [NativeProperty("KeepUnusedRenderingResources")]
        extern public bool keepUnusedRenderingResources { get; set; }

        ///<summary>Each Camera has its own <c>KeepUnusedRenderingResources</c> setting, which is <c>false</c> by default. Unity uses this per-Camera setting in combination with the Terrain's overall <c>KeepUnusedRenderingResources</c> setting. If either setting is <c>true</c>, Unity preserves all rendering resources regardless of how long they've remained unused.</summary>
        ///<param name="cameraEntityId">The EntityId of the camera being queried. See <see cref="Object.GetEntityId" />.</param>
        ///<returns>Returns <c>true</c> if all rendering resources for the given camera are saved regardless of usage. Returns <c>false</c> if garbage collection is allowed to free unused resources.</returns>
        ///<seealso cref="Terrain.SetKeepUnusedCameraRenderingResources" />
        ///<seealso cref="Terrain.keepUnusedRenderingResources" />
        extern public bool GetKeepUnusedCameraRenderingResources(EntityId cameraEntityId);
        ///<summary>Defines whether Unity cleans up rendering resources for a given Camera during garbage collection.</summary>
        ///<remarks>Each Camera has its own <c>KeepUnusedRenderingResources</c> setting, which is <c>false</c> by default. Unity uses this per-Camera setting in combination with the Terrain's overall <c>KeepUnusedRenderingResources</c> setting. If either setting is <c>true</c>, Unity preserves all rendering resources regardless of how long they've remained unused.</remarks>
        ///<param name="keepUnused">The value to set to this camera's keepUnused flag.</param>
        ///<param name="cameraEntityId">The EntityId of the camera for which freeUnusedRenderingResources is being set. See <see cref="Object.GetEntityId" />.</param>
        ///<seealso cref="Terrain.GetKeepUnusedCameraRenderingResources" />
        ///<seealso cref="Terrain.keepUnusedRenderingResources" />
        extern public void SetKeepUnusedCameraRenderingResources(EntityId cameraEntityId, bool keepUnused);

        ///<summary>Each Camera has its own <c>KeepUnusedRenderingResources</c> setting, which is <c>false</c> by default. Unity uses this per-Camera setting in combination with the Terrain's overall <c>KeepUnusedRenderingResources</c> setting. If either setting is <c>true</c>, Unity preserves all rendering resources regardless of how long they've remained unused.</summary>
        ///<param name="cameraInstanceID">The InstanceID of the camera being queried. See <see cref="Object.GetInstanceID" />.</param>
        ///<returns>Returns <c>true</c> if all rendering resources for the given camera are saved regardless of usage. Returns <c>false</c> if garbage collection is allowed to free unused resources.</returns>
        ///<seealso cref="Terrain.SetKeepUnusedCameraRenderingResources" />
        ///<seealso cref="Terrain.keepUnusedRenderingResources" />
        [Obsolete("GetKeepUnusedCameraRenderingResources(int) is obsolete. Use GetKeepUnusedCameraRenderingResources(EntityId) instead.", true)]
        public bool GetKeepUnusedCameraRenderingResources(int cameraInstanceID) => GetKeepUnusedCameraRenderingResources((EntityId)cameraInstanceID);
        ///<summary>Defines whether Unity cleans up rendering resources for a given Camera during garbage collection.</summary>
        ///<remarks>Each Camera has its own <c>KeepUnusedRenderingResources</c> setting, which is <c>false</c> by default. Unity uses this per-Camera setting in combination with the Terrain's overall <c>KeepUnusedRenderingResources</c> setting. If either setting is <c>true</c>, Unity preserves all rendering resources regardless of how long they've remained unused.</remarks>
        ///<param name="cameraInstanceID">The InstanceID of the camera for which freeUnusedRenderingResources is being set. See <see cref="Object.GetInstanceID" />.</param>
        ///<param name="keepUnused">The value to set to this camera's keepUnused flag.</param>
        ///<seealso cref="Terrain.GetKeepUnusedCameraRenderingResources" />
        ///<seealso cref="Terrain.keepUnusedRenderingResources" />
        [Obsolete("SetKeepUnusedCameraRenderingResources(int, bool) is obsolete. Use SetKeepUnusedCameraRenderingResources(EntityId, bool) instead.", true)]
        public void SetKeepUnusedCameraRenderingResources(int cameraInstanceID, bool keepUnused) => SetKeepUnusedCameraRenderingResources((EntityId)cameraInstanceID, keepUnused);

        ///<summary>Allows you to set the shadow casting mode for the terrain.</summary>
        ///<remarks>
        ///  <see cref="Rendering.ShadowCastingMode" /> enum defines how and if shadows are cast from this object.
        ///Typically shadows are either cast or not, but it's also possible to make shadows two-sided (useful for otherwise single-sided geometry) or make a shadows-only object (that is otherwise invisible in the Scene, but casts a shadow).</remarks>
        extern public ShadowCastingMode shadowCastingMode { get; set; }

        ///<summary>How reflection probes are used for terrain. See <see cref="Rendering.ReflectionProbeUsage" />.</summary>
        ///<remarks>If enabled and reflection probes are present in the Scene, a reflection texture will be picked for the terrain object and set as a uniform for the shader. 
        ///Not applicable to materials using built-in Legacy shaders.</remarks>
        extern public ReflectionProbeUsage reflectionProbeUsage { get; set; }

        ///<summary>Fills the list with reflection probes whose AABB intersects with terrain's AABB. Their weights are also provided. Weight shows how much influence the probe has on the terrain, and is used when the blending between multiple reflection probes occurs.</summary>
        ///<remarks>This function won't touch <c>result</c> if <see cref="Terrain.reflectionProbeUsage" /> is <see cref="Rendering.ReflectionProbeUsage.Off" />, otherwise the original content of the list will be cleared.</remarks>
        ///<param name="result">[in / out] A list to hold the returned reflection probes and their weights. See <see cref="ReflectionProbeBlendInfo" />.</param>
        extern public void GetClosestReflectionProbes([Out,NotNull] List<ReflectionProbeBlendInfo> result);

        ///<summary>The custom material Unity uses to render the Terrain.</summary>
        ///<remarks>You can use this property to give the Terrain a custom material to render with. 
        ///
        ///
        ///Unity doesn't make a copy of the custom material internally, so modifying <c>materialTemplate</c> will affect all Terrain objects that use the same material.</remarks>
        extern public Material materialTemplate { get; set; }

        ///<summary>Indicates whether Unity draws the Terrain geometry itself.</summary>
        ///<remarks>If <c>false</c>, Unity doesn't draw the Terrain geometry, and only draws other Terrain details such as grass and trees.</remarks>
        extern public bool drawHeightmap { get; set; }
        ///<summary>Specifies if the terrain tile will be automatically connected to adjacent tiles.</summary>
        extern public bool allowAutoConnect { get; set; }
        ///<summary>Grouping ID for auto connect.</summary>
        ///<remarks>Terrain tiles that share a grouping ID are automatically connected together.</remarks>
        extern public int groupingID { get; set; }

        ///<summary>Set to true to enable the terrain instance renderer. The default value is false.</summary>
        extern public bool drawInstanced { get; set; }

        ///<summary>When this options is enabled, Terrain heightmap geometries will be added in acceleration structures used for Ray Tracing.</summary>
        ///<remarks>Use <see cref="RayTracingAccelerationStructure.CullInstances" /> function to add Terrain heightmap geometries to the acceleration structure.</remarks>
        ///<seealso cref="RayTracingAccelerationStructure" />
        ///<seealso cref="SystemInfo.supportsRayTracing" />
        extern public bool enableHeightmapRayTracing { get; set; }
        ///<summary>Controls frustum culling for the terrain heightmap LOD system.</summary>
        ///<remarks>When enabled (the default), terrain patches outside the camera frustum are simplified aggressively, which reduces heightmap tessellation cost.
        ///
        ///When disabled, terrain is tessellated based on camera distance and pixel error only, regardless of whether it is visible.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        // Note: disabling this increases heightmap tessellation cost for off-screen terrain.
        ///        foreach (var terrain in Terrain.activeTerrains)
        ///            terrain.enableHeightmapLODFrustumCulling = false;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public bool enableHeightmapLODFrustumCulling { get; set; }

        ///<summary>Returns the normal map texture computed from sampling the heightmap. It is only used when terrain is rendered using instancing.</summary>
        extern public RenderTexture normalmapTexture { [NativeMethod("TryGetNormalMapTexture")] get; }

        ///<summary>Returns the material used to render the terrain splatmap.</summary>
        extern public Material splatBaseMaterial { [NativeMethod("TryGetSplatBaseMaterial")] get; }

        ///<summary>Specify if terrain trees and details should be drawn. If disabled, this will also disable updates to renderer positions for trees and details. Tree and detail renderer positions will update again once this setting is re-enabled.</summary>
        extern public bool drawTreesAndFoliage { get; set; }

        ///<summary>Set the terrain bounding box scale.</summary>
        extern public Vector3 patchBoundsMultiplier { get; set; }

        ///<summary>Samples the height at the given position defined in world space, relative to the Terrain space.</summary>
        ///<remarks>This method automatically clamps the world position to the Terrain boundaries.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void LateUpdate()
        ///    {
        ///        Vector3 pos = transform.position;
        ///        pos.y = Terrain.activeTerrain.SampleHeight(transform.position);
        ///        transform.position = pos;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float SampleHeight(Vector3 worldPosition);

        ///<summary>Adds a tree instance to the terrain.</summary>
        extern public void AddTreeInstance(TreeInstance instance);

        ///<summary>Lets you set up the connection between neighboring Terrain tiles. This ensures LOD matches up on neighboring Terrain tiles.</summary>
        ///<remarks>Note that it isn't enough to call this function on one Terrain; you need to set the neighbors of each Terrain.</remarks>
        ///<param name="left">The Terrain tile to the left is in the negative X direction.</param>
        ///<param name="top">The Terrain tile to the top is in the positive Z direction.</param>
        ///<param name="right">The Terrain tile to the right is in the positive X direction.</param>
        ///<param name="bottom">The Terrain tile to the bottom is in the negative Z direction.</param>
        extern public void SetNeighbors(Terrain left, Terrain top, Terrain right, Terrain bottom);

        ///<summary>The multiplier to the current LOD bias used for rendering LOD trees (i.e. SpeedTree trees).</summary>
        ///<remarks>The value by default is 1 and must be greater than 0. The exact LOD bias value used by tree rendering is <see cref="QualitySettings.lodBias" /> * value.
        ///
        ///The value is not serialized with Terrain component.</remarks>
        extern public float treeLODBiasMultiplier { get; set; }

        ///<summary>Collect detail patches from memory.</summary>
        ///<remarks>By default the property value is true, meaning the detail patches in the Terrain will be removed from memory when not visible. If the property is set to false, the patches are kept in memory until the Terrain object is destroyed or the collectDetailPatches property is set to true. By setting the property to false all the detail patches for a given density will be initialized and kept in memory. Changing the density will recreate the patches.
        ///
        ///Note that detail patches can use a large amount of memory, therefore this property when set to false can increase the memory usage of your application significantly.
        ///
        ///The value is not serialized with Terrain component.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Terrain.activeTerrain.collectDetailPatches = false;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="detailObjectDensity" />
        extern public bool collectDetailPatches { get; set; }

        ///<summary>When enabled, the terrain ignores the terrain overrides set in the <see cref="QualitySettings" />.</summary>
        extern public bool ignoreQualitySettings { get; set; }

        ///<summary>Controls what part of the terrain should be rendered.</summary>
        ///<remarks>Set the value to a combination of <see cref="TerrainRenderFlags" /> values. The default value is <see cref="TerrainRenderFlags.all" />.
        ///
        ///The value is not serialized with Terrain component.</remarks>
        extern public TerrainRenderFlags editorRenderFlags { get; set; }

        ///<summary>Get the world space position of the terrain.</summary>
        extern public Vector3 GetPosition();

        ///<summary>Flushes any change done in the terrain so it takes effect.</summary>
        extern public void Flush();

        extern internal void RemoveTrees(Vector2 position, float radius, int prototypeIndex);

        ///<summary>Set the additional material properties when rendering the terrain heightmap using the splat material.</summary>
        ///<seealso cref="GetSplatMaterialPropertyBlock" />
        ///<seealso cref="Renderer.SetPropertyBlock" />
        ///<seealso cref="MaterialPropertyBlock" />
        [NativeMethod("CopySplatMaterialCustomProps")]
        extern public void SetSplatMaterialPropertyBlock(MaterialPropertyBlock properties);

        ///<summary>Get the previously set splat material properties by copying to the <c>dest</c> MaterialPropertyBlock object.</summary>
        ///<seealso cref="SetSplatMaterialPropertyBlock" />
        ///<seealso cref="Renderer.GetPropertyBlock" />
        ///<seealso cref="MaterialPropertyBlock" />
        public void GetSplatMaterialPropertyBlock(MaterialPropertyBlock dest)
        {
            if (dest == null)
                throw new ArgumentNullException("dest");

            Internal_GetSplatMaterialPropertyBlock(dest);
        }

        [NativeMethod("GetSplatMaterialCustomProps")]
        extern private void Internal_GetSplatMaterialPropertyBlock(MaterialPropertyBlock dest);

        ///<summary>Whether to bake an array of internal light probes for Tree Editor trees (Editor only).</summary>
        extern public bool bakeLightProbesForTrees { get; set; }

        ///<summary>Removes ringing from light probes on Tree Editor trees (Editor only).</summary>
        ///<remarks>Ringing is an artifact that can occur on probes if they are subject to drastic changes in lighting. Light may overshoot and appear in the opposite end of the probe. This feature can remove ringing on probes if enabled, but may reduce overall contrast.</remarks>
        extern public bool deringLightProbesForTrees { get; set; }

        ///<summary>The motion vector rendering mode for all SpeedTree models painted on the terrain.</summary>
        extern public TreeMotionVectorModeOverride treeMotionVectorModeOverride { get; set; }

        ///<summary>Allows you to specify how Unity chooses the [layer](xref:Layers) for tree instances.</summary>
        ///<remarks>Unity automatically assigns a [layer](xref:Layers) to the tree instances on your terrain. This property allows you to specify whether the tree instances should have the same layer value as the terrain GameObject, or whether they should take on the same layer value as their tree prototype Prefab (which means each type of tree can have a unique layer value). The default is false, which means trees get the terrain GameObject's layer. Set this value to true if you want your trees to take on the layer value of their prototype Prefabs.</remarks>
        extern public bool preserveTreePrototypeLayers { get; set; }

        ///<summary>Graphics format of the Terrain heightmap.</summary>
        ///<seealso cref="GraphicsFormat" />
        [StaticAccessor("Terrain", StaticAccessorType.DoubleColon)]
        extern static public GraphicsFormat heightmapFormat { get; }

        ///<exclude />
        static public TextureFormat heightmapTextureFormat
        {
            get { return GraphicsFormatUtility.GetTextureFormat(heightmapFormat); }
        }

        ///<summary>RenderTextureFormat of the terrain heightmap.</summary>
        ///<seealso cref="RenderTextureFormat" />
        static public RenderTextureFormat heightmapRenderTextureFormat
        {
            get { return GraphicsFormatUtility.GetRenderTextureFormat(heightmapFormat); }
        }

        ///<summary>Graphics format of the Terrain normal map texture.</summary>
        ///<seealso cref="normalmapTexture" />
        [StaticAccessor("Terrain", StaticAccessorType.DoubleColon)]
        extern static public GraphicsFormat normalmapFormat { get; }

        ///<summary>Texture format of the Terrain normal map texture.</summary>
        ///<seealso cref="normalmapFormat" />
        static public TextureFormat normalmapTextureFormat
        {
            get { return GraphicsFormatUtility.GetTextureFormat(normalmapFormat); }
        }

        ///<summary>Render texture format of the Terrain normal map texture.</summary>
        ///<seealso cref="normalmapFormat" />
        static public RenderTextureFormat normalmapRenderTextureFormat
        {
            get { return GraphicsFormatUtility.GetRenderTextureFormat(normalmapFormat); }
        }

        ///<summary>Graphics format of the Terrain holes Texture when it is not compressed.</summary>
        ///<seealso cref="TerrainData.holesTexture" />
        [StaticAccessor("Terrain", StaticAccessorType.DoubleColon)]
        extern static public GraphicsFormat holesFormat { get; }

        ///<summary>Render texture format of the Terrain holes Texture.</summary>
        ///<seealso cref="holesFormat" />
        static public RenderTextureFormat holesRenderTextureFormat
        {
            get { return GraphicsFormatUtility.GetRenderTextureFormat(holesFormat); }
        }

        ///<summary>Graphics format of the Terrain holes Texture when it is compressed.</summary>
        ///<seealso cref="TerrainData.holesTexture" />
        [StaticAccessor("Terrain", StaticAccessorType.DoubleColon)]
        extern static public GraphicsFormat compressedHolesFormat { get; }

        ///<summary>Texture format of the Terrain holes Texture when it is compressed.</summary>
        ///<seealso cref="holesFormat" />
        static public TextureFormat compressedHolesTextureFormat
        {
            get { return GraphicsFormatUtility.GetTextureFormat(compressedHolesFormat); }
        }

        ///<summary>The active Terrain. This is a convenient function to get to the main Terrain in the Scene.</summary>
        ///<remarks>If you have multiple active Terrains, this returns only one of them. If you need all the terrains in the scene, use <see cref="Terrain.activeTerrains" /> instead. A terrain is active when the component that represents it is enabled and the GameObject it is on is active.</remarks>
        extern public static Terrain activeTerrain { get; }
        ///<summary>Marks the current connectivity status as invalid.</summary>
        ///<remarks>Use this method after adding / removing terrain tiles to inform the terrain that the connectivity needs to be rebuilt.</remarks>
        extern public static void SetConnectivityDirty();

        ///<summary>The active terrains in the Scene.</summary>
        ///<remarks>This returns all the active terrains in the scene. A terrain is active when the component that represents it is enabled and the GameObject it is on is active.</remarks>
        [NativeProperty("ActiveTerrainsScriptingArray")]
        extern public static Terrain[] activeTerrains { [return:UnityMarshalAs(NativeType.ScriptingObjectPtr)] get; }

        ///<summary>Populates a List of Terrains with the active Terrains in the Scene.</summary>
        ///<remarks>This function differs from <see cref="Terrain.activeTerrains" /> in that it gives you control of memory allocation. .</remarks>
        ///<param name="terrainList">A List of Terrains this function populates with the active Terrains in the Scene.</param>
        ///<seealso cref="Terrain.activeTerrains" />
        public static void GetActiveTerrains(List<Terrain> terrainList)
        {
            Internal_FillActiveTerrainList(terrainList);
        }

        extern private static void Internal_FillActiveTerrainList([NotNull] [Out] List<Terrain> terrainList);

        ///<summary>Creates a Terrain including collider from <see cref="TerrainData" />.</summary>
        [UsedByNativeCode]
        extern public static GameObject CreateTerrainGameObject(TerrainData assignTerrain);

        ///<summary>The Terrain tile to the left, which is in the negative X direction.</summary>
        extern public Terrain leftNeighbor { get; }
        ///<summary>The Terrain tile to the left, which is in the positive X direction.</summary>
        extern public Terrain rightNeighbor { get; }
        ///<summary>Terrain top neighbor.</summary>
        extern public Terrain topNeighbor { get; }
        ///<summary>Terrain bottom neighbor.</summary>
        extern public Terrain bottomNeighbor { get; }

        ///<summary>Determines which rendering layers the Terrain renderer lives on.</summary>
        ///<remarks>When using a Scriptable Render Pipeline, you can specify an additional rendering-specific layer mask. This filters the renderers based on the mask the renderer has, and the mask passed to the DrawRenderers command.</remarks>
        extern public UInt32 renderingLayerMask { get; set; }
    }

    ///<summary>Extension methods to the Terrain class, used only for the UpdateGIMaterials method used by the Global Illumination System.</summary>
    public static partial class TerrainExtensions
    {
        ///<summary>Schedules an update of the albedo and emissive Textures of a system that contains the Terrain.</summary>
        ///<remarks>The second overload specifies a region of the Terrain that needs to be updated. This makes sure that only the systems that overlap with the specified rectangle get updated, which could help improve performance. The coordinates are specified the same way as in <see cref="TerrainData.SetAlphamaps" />.</remarks>
        [Obsolete("Enlighten realtime Global Illumination is deprecated and will be removed in a future release. #from(6000.7)", false)]
        public static void UpdateGIMaterials(this Terrain terrain)
        {
            if (terrain.terrainData == null)
                throw new ArgumentException("Invalid terrainData.");

            UpdateGIMaterialsForTerrain(terrain.GetEntityId(), new Rect(0, 0, 1, 1));
        }

        ///<summary>Schedules an update of the albedo and emissive Textures of a system that contains the Terrain.</summary>
        ///<remarks>The second overload specifies a region of the Terrain that needs to be updated. This makes sure that only the systems that overlap with the specified rectangle get updated, which could help improve performance. The coordinates are specified the same way as in <see cref="TerrainData.SetAlphamaps" />.</remarks>
        [Obsolete("Enlighten realtime Global Illumination is deprecated and will be removed in a future release. #from(6000.7)", false)]
        public static void UpdateGIMaterials(this Terrain terrain, int x, int y, int width, int height)
        {
            if (terrain.terrainData == null)
                throw new ArgumentException("Invalid terrainData.");

            float alphamapWidth = terrain.terrainData.alphamapWidth;
            float alphamapHeight = terrain.terrainData.alphamapHeight;
            UpdateGIMaterialsForTerrain(terrain.GetEntityId(), new Rect(x / alphamapWidth, y / alphamapHeight, width / alphamapWidth, height / alphamapHeight));
        }

        [FreeFunction]
        [NativeConditional("INCLUDE_DYNAMIC_GI && ENABLE_RUNTIME_GI")]
        extern internal static void UpdateGIMaterialsForTerrain(EntityId terrainInstanceID, Rect uvBounds);
    }

    ///<summary>Tree Component for the tree creator.</summary>
    [NativeHeader("Modules/Terrain/Public/Tree.h")]
    [global::UnityEngine.NativeClass("Tree", PersistentTypeId = 193)]
    [ExcludeFromPreset]
    public sealed partial class Tree : Component
    {
        ///<summary>Data asociated to the Tree.</summary>
        ///<remarks>Check the tree creator.</remarks>
        [NativeProperty("TreeData")]
        extern public ScriptableObject data { get; set; }

        ///<summary>Tells if there is wind data exported from SpeedTree are saved on this component.</summary>
        extern public bool hasSpeedTreeWind
        {
            [NativeMethod("HasSpeedTreeWind")]
            get;
        }

        ///<summary>Gets or sets the SpeedTreeWindAsset associated with this Tree component.</summary>
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
    [global::UnityEngine.NativeClass("SpeedTreeWindAsset", PersistentTypeId = 228)]
    public partial class SpeedTreeWindAsset : Object
    {
        extern public int Version { get; set; }

        internal SpeedTreeWindAsset(int version, SpeedTreeWindConfig9 config)
        {
            Internal_Create(this, version, SpeedTreeWindConfig9.Serialize(config));
        }

        [NativeMethod(ThrowsException = true)]
        static extern void Internal_Create([Writable] SpeedTreeWindAsset notSelf, int version, byte[] data);
    }
}
