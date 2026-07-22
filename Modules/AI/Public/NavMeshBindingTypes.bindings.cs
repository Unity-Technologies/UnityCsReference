// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.AI
{
    // Keep this enum in sync with the one defined in "NavMeshBindingTypes.h"
    ///<summary>Bitmask used for operating with debug data from the NavMesh build process.</summary>
    ///<remarks>Used in two situations:
    ///
    ///- within <see cref="NavMeshBuildSettings.debug" /> to specify which debug data to retain after the build process has completed, preserving the world position and orientation;
    ///
    ///- as a parameter of <see cref="M:UnityEditor.AI.NavMeshEditorHelpers.DrawBuildDebug" /> to control which of the available debug data types to display for a specified NavMesh.</remarks>
    ///<seealso cref="NavMeshBuildSettings" />
    [Flags]
    public enum NavMeshBuildDebugFlags
    {
        ///<summary>No debug data from the NavMesh build process is taken into consideration.</summary>
        ///<seealso cref="NavMeshBuildSettings.debug" />
        ///<seealso cref="M:UnityEditor.AI.NavMeshEditorHelpers.DrawBuildDebug" />
        None = 0,
        ///<summary>The triangles of all the geometry that is used as a base for computing the new NavMesh.</summary>
        ///<seealso cref="NavMeshBuilder.CollectSources" />
        ///<seealso cref="NavMeshBuildSource" />
        ///<seealso cref="NavMeshBuildSettings.debug" />
        ///<seealso cref="M:UnityEditor.AI.NavMeshEditorHelpers.DrawBuildDebug" />
        InputGeometry = 1 << 0,
        ///<summary>The voxels produced by rasterizing the source geometry into walkable and unwalkable areas.</summary>
        ///<seealso cref="NavMeshBuildSettings.voxelSize" />
        ///<seealso cref="NavMeshBuildSettings.debug" />
        ///<seealso cref="M:UnityEditor.AI.NavMeshEditorHelpers.DrawBuildDebug" />
        Voxels = 1 << 1,
        ///<summary>The segmentation of the traversable surfaces into smaller areas necessary for producing simple polygons.</summary>
        ///<seealso cref="NavMeshBuildSettings.debug" />
        ///<seealso cref="M:UnityEditor.AI.NavMeshEditorHelpers.DrawBuildDebug" />
        Regions = 1 << 2,
        ///<summary>The contours that follow precisely the edges of each surface region.</summary>
        ///<seealso cref="NavMeshBuildSettings.debug" />
        ///<seealso cref="M:UnityEditor.AI.NavMeshEditorHelpers.DrawBuildDebug" />
        RawContours = 1 << 3,
        ///<summary>Contours bounding each of the surface regions, described through fewer vertices and straighter edges compared to <see cref="RawContours" />.</summary>
        ///<seealso cref="NavMeshBuildSettings.debug" />
        ///<seealso cref="M:UnityEditor.AI.NavMeshEditorHelpers.DrawBuildDebug" />
        SimplifiedContours = 1 << 4,
        ///<summary>Meshes of convex polygons constructed within the unified contours of adjacent regions.</summary>
        ///<seealso cref="NavMeshBuildSettings.debug" />
        ///<seealso cref="M:UnityEditor.AI.NavMeshEditorHelpers.DrawBuildDebug" />
        PolygonMeshes = 1 << 5,
        ///<summary>The triangulated meshes with height details that better approximate the source geometry.</summary>
        ///<seealso cref="NavMeshBuildSettings.debug" />
        ///<seealso cref="M:UnityEditor.AI.NavMeshEditorHelpers.DrawBuildDebug" />
        PolygonMeshesDetail = 1 << 6,
        ///<summary>All debug data from the NavMesh build process is taken into consideration.</summary>
        ///<seealso cref="NavMeshBuildSettings.debug" />
        ///<seealso cref="M:UnityEditor.AI.NavMeshEditorHelpers.DrawBuildDebug" />
        All = unchecked((int)(~(~0U << 7)))
    }

    // Keep this enum in sync with the one defined in "NavMeshBindingTypes.h"
    ///<summary>Used with <see cref="NavMeshBuildSource" /> to define the shape for building NavMesh.</summary>
    public enum NavMeshBuildSourceShape
    {
        ///<summary>Describes a Mesh source for use with <see cref="NavMeshBuildSource" />. Mesh sources must be positioned within 100,000 units of the world origin and must not exceed 100,000 units in any axis-aligned dimension.</summary>
        Mesh = 0,
        ///<summary>Describes a <see cref="T:UnityEngine.TerrainData" /> source for use with <see cref="NavMeshBuildSource" />.</summary>
        Terrain = 1,
        ///<summary>Describes a box primitive for use with <see cref="NavMeshBuildSource" />.</summary>
        Box = 2,
        ///<summary>Describes a sphere primitive for use with <see cref="NavMeshBuildSource" />.</summary>
        Sphere = 3,
        ///<summary>Describes a capsule primitive for use with <see cref="NavMeshBuildSource" />.</summary>
        Capsule = 4,
        ///<summary>Describes a ModifierBox source for use with <see cref="NavMeshBuildSource" />.</summary>
        ///<remarks>This shape changes the area type of the walkable surface inside the box. Because this modification happens in a voxel representation of the scene, NavMesh does not follow the outline of the box precisely. If several ModifierBoxes overlap, and have different area types, the area type with the highest index takes precedence. A ModifierBox that you set to Not Walkable takes precedence over any other ModifierBoxes, regardless of their area type. This is useful when you need to block out an area.</remarks>
        ///<seealso href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/AreasAndCosts.html">Areas and Costs</seealso>
        ModifierBox = 5
    }

    // Keep this enum in sync with the one defined in "NavMeshBindingTypes.h"
    ///<summary>Used for specifying the type of geometry to collect. Used with <see cref="NavMeshBuilder.CollectSources" />.</summary>
    public enum NavMeshCollectGeometry
    {
        ///<summary>Collect meshes form the rendered geometry.</summary>
        RenderMeshes = 0,
        ///<summary>Collect geometry from the 3D physics collision representation.</summary>
        PhysicsColliders = 1
    }

    ///<summary>The input to the NavMesh builder is a list of NavMesh build sources.</summary>
    ///<remarks>Their shape can be one of the following: mesh, terrain, box, sphere, or capsule. Each of them is described by a NavMeshBuildSource struct.
    ///
    ///You can specify a build source by filling a NavMeshBuildSource struct and adding that to the list of sources that are passed to the bake function. Alternatively, you can use the collect API to quickly create NavMesh build sources from available render meshes or physics colliders. See <see cref="NavMeshBuilder.CollectSources" />.
    ///
    ///If you use this function at runtime, any meshes with read/write access disabled will not be processed or included in the final NavMesh. See <see cref="Mesh.isReadable" />.</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using UnityEngine;
    ///using UnityEngine.AI;
    ///
    ///public class Example : MonoBehaviour
    ///{
    ///    // Make a build source for a box in local space
    ///    public NavMeshBuildSource BoxSource10x10()
    ///    {
    ///        var src = new NavMeshBuildSource();
    ///        src.transform = transform.localToWorldMatrix;
    ///        src.shape = NavMeshBuildSourceShape.Box;
    ///        src.size = new Vector3(10.0f, 0.1f, 10.0f);
    ///        return src;
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [UsedByNativeCode]
    [NativeHeader("Modules/AI/Public/NavMeshBindingTypes.h")]
    public struct NavMeshBuildSource
    {
        ///<summary>Describes the local to world transformation matrix of the build source. That is, position and orientation and scale of the shape.</summary>
        public Matrix4x4 transform { get { return m_Transform; } set { m_Transform = value; } }
        ///<summary>Describes the dimensions of the shape.</summary>
        ///<remarks>Used only for the primitive shapes: Sphere, Capsule, Box.
        ///
        ///• Sphere: size is the dimensions of a box enclosing the sphere (i.e., x, y, and z are all equal to the diameter).
        ///
        ///• Box: size is the dimensions of the box.
        ///
        ///• Capsule: size is the dimensions of a box enclosing the capsule (i.e., x and z are equal to the diameter of the capsule and y is the height).</remarks>
        public Vector3 size { get { return m_Size; } set { m_Size = value; } }
        ///<summary>The type of the shape this source describes.</summary>
        ///<seealso cref="NavMeshBuildSourceShape" />
        public NavMeshBuildSourceShape shape { get { return m_Shape; } set { m_Shape = value; } }
        ///<summary>Describes the area type of the NavMesh surface for this object.</summary>
        public int area { get { return m_Area; } set { m_Area = value; } }
        ///<summary>Enables the links generation for this object.</summary>
        ///<remarks>When build sources are obtained using <see cref="NavMeshBuilder.CollectSources" />, this value can be affected by using <see cref="NavMeshBuildMarkup" />. If this value is <c>true</c> and links parameters are valid in <see cref="NavMeshBuildSettings" /> this source will be considered during links generation.</remarks>
        public bool generateLinks { get { return m_GenerateLinks != 0; } set { m_GenerateLinks = value ? 1 : 0; } }
        ///<summary>Describes the object referenced for Mesh and Terrain types of input sources.</summary>
        ///<remarks>Used only for the types: <see cref="Mesh" /> and <see cref="T:UnityEngine.TerrainData" />.</remarks>
        public Object sourceObject { get { return InternalGetObject(m_EntityId); } set { m_EntityId = value != null ? value.GetEntityId() : EntityId.None; } }
        ///<summary>Points to the owning component - if available, otherwise null.</summary>
        ///<remarks>When build sources are obtained using <see cref="NavMeshBuilder.CollectSources" />, this value typically refers to a mesh or collider component - however for shared meshes it will be null.</remarks>
        ///<seealso cref="MeshFilter.sharedMesh" />
        public Component component { get { return InternalGetComponent(m_ComponentID); } set { m_ComponentID = value != null ? value.GetEntityId() : EntityId.None; } }

        Matrix4x4 m_Transform;
        Vector3 m_Size;
        NavMeshBuildSourceShape m_Shape;
        int m_Area;
        EntityId m_EntityId;
        EntityId m_ComponentID;
        int m_GenerateLinks;

        [StaticAccessor("NavMeshBuildSource", StaticAccessorType.DoubleColon)]
        static extern Component InternalGetComponent(EntityId instanceID);

        [StaticAccessor("NavMeshBuildSource", StaticAccessorType.DoubleColon)]
        static extern Object InternalGetObject(EntityId instanceID);
    }

    ///<summary>The NavMesh build markup allows you to control how certain objects are treated during the NavMesh build process, specifically when collecting sources for building.</summary>
    ///<remarks>You can override the area type or specify that certain objects should be excluded from collected sources. The markup can be applied hierarchically or to only the specified object.</remarks>
    ///<seealso cref="NavMeshBuilder.CollectSources" />
    [NativeHeader("Modules/AI/Public/NavMeshBindingTypes.h")]
    public struct NavMeshBuildMarkup
    {
        ///<summary>Use this to specify whether the area type of the GameObject and its children should be overridden by the area type specified in this struct.</summary>
        public bool overrideArea { get { return m_OverrideArea != 0; } set { m_OverrideArea = value ? 1 : 0; } }
        ///<summary>The area type to use when override area is enabled.</summary>
        public int area { get { return m_Area; } set { m_Area = value; } }
        ///<summary>Set this to <c>true</c> in order to enable the <see cref="ignoreFromBuild" /> property.</summary>
        ///<remarks>In the case when a <c>NavMeshBuildMarkup</c> is used to change only the area type of an object, <c>overrideIgnore</c> should be set to <c>false</c> so that the <c>ignoreFromBuild</c> property will not have any effect.
        ///
        ///                If none of the objects in a hierarchy are marked with <c>ignoreFromBuild</c> set to <c>true</c> then no objects in that hierarchy will be ignored while building the NavMesh.</remarks>
        public bool overrideIgnore { get { return m_InheritIgnoreFromBuild == 0; } set { m_InheritIgnoreFromBuild = value ? 0: 1; } }
        ///<summary>Use this to specify whether the GameObject and its children should be ignored.</summary>
        ///<remarks>If you set this to <c>true</c>, the GameObject and its children will not be included as part of the NavMesh.
        ///
        ///                Set <see cref="overrideIgnore" /> to <c>true</c> in order for this property to have the intended effect. When <c>overrideIgnore</c> is <c>false</c> this property inherits the value from the markup of a parent object, if that exists, otherwise it is set to <c>false</c>.</remarks>
        public bool ignoreFromBuild { get { return m_IgnoreFromBuild != 0; } set { m_IgnoreFromBuild = value ? 1 : 0; } }
        ///<summary>Use this to specify whether the default links generation condition for the GameObject and its children should be overridden by the generateLinks option specified in this struct.</summary>
        public bool overrideGenerateLinks { get { return m_OverrideGenerateLinks != 0; } set { m_OverrideGenerateLinks = value ? 1 : 0; } }
        ///<summary>Use this to specify whether the GameObject and its children should be included in the link generation process.</summary>
        public bool generateLinks { get { return m_GenerateLinks != 0; } set { m_GenerateLinks = value ? 1 : 0; } }
        ///<summary>Use this to specify if the GameObject's children also use these markup settings.</summary>
        public bool applyToChildren { get { return m_IgnoreChildren == 0; } set { m_IgnoreChildren = value ? 0 : 1; } }
        ///<summary>Use this to specify which GameObject (including the GameObject’s children) the markup should be applied to.</summary>
        ///<remarks>This markup will be shared with the children of the <c>root</c> GameObject only if <see cref="applyToChildren" /> is set to <c>true</c>.</remarks>
        public Transform root { get { return InternalGetRootGO(m_EntityId); } set { m_EntityId = value != null ? value.GetEntityId() : EntityId.None; } }

        int m_OverrideArea;
        int m_Area;
        int m_InheritIgnoreFromBuild; // backing field is reversed for the default value to align with the legacy default behaviour
        int m_IgnoreFromBuild;
        int m_OverrideGenerateLinks;
        int m_GenerateLinks;
        EntityId m_EntityId;
        int m_IgnoreChildren; // backing field is reversed for the default value to align with the legacy default behaviour

        [StaticAccessor("NavMeshBuildMarkup", StaticAccessorType.DoubleColon)]
        static extern Transform InternalGetRootGO(EntityId instanceID);
    }
}
