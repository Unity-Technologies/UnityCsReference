// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    ///<summary>Cooking options that are available with MeshCollider.</summary>
    [Flags]
    public enum MeshColliderCookingOptions
    {
        ///<summary>No optional cooking steps will be run.</summary>
        None,
        ///<summary>Allow the physics engine to increase the volume of the input mesh in attempt to generate a valid convex mesh.</summary>
        [Obsolete("No longer used because the problem this was trying to solve is gone since Unity 2018.3", true)] InflateConvexMesh = 1 << 0,
        ///<summary>Toggle between cooking for faster simulation or faster cooking time.</summary>
        ///<remarks>When set, this runs some extra steps to guarantee the resulting mesh is optimal for run-time performance. This affects  performance of the physics queries as well as contacts generation. If not set, produces the result as fast as possible. Consequently, the cooked MeshCollider might not be optimal.</remarks>
        CookForFasterSimulation = 1 << 1,
        ///<summary>Toggle cleaning of the mesh.</summary>
        ///<remarks>When set, the cooking will try to eliminate degenerate triangles of the mesh as well as other geometrical artifacts. It results in a mesh that is better suited for use in collision detection and tends to produce more accurate hit points.</remarks>
        EnableMeshCleaning = 1 << 2,
        ///<summary>Toggle the removal of equal vertices.</summary>
        ///<remarks>When set, the vertices that have the same position will be combined. This is important for the collision feedback that happens run-time.</remarks>
        WeldColocatedVertices = 1 << 3,
        ///<summary>Determines whether to use the fast midphase structure that doesn't require R-trees.</summary>
        UseFastMidphase = 1 << 4
    }

    ///<summary>A Collider that takes a Mesh asset and builds its collision shape based on it.</summary>
    ///<seealso cref="BoxCollider" />
    ///<seealso cref="CapsuleCollider" />
    ///<seealso cref="PhysicsMaterial" />
    ///<seealso cref="Rigidbody" />
    ///<seealso href="xref:class-MeshCollider">Collision Detection</seealso>
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Physics/MeshCollider.h")]
    [global::UnityEngine.NativeClass("MeshCollider", PersistentTypeId = 64)]
    [NativeHeader("Runtime/Graphics/Mesh/Mesh.h")]
    public partial class MeshCollider : Collider
    {
        internal MeshCollider(global::UnityEngine.EntityId id) : base(id) {}
        public MeshCollider() {}
        ///<summary>The mesh object used for collision detection.</summary>
        ///<remarks>If prior to setting <see cref="sharedMesh" /> any of the vertices, indices or triangles of the mesh have been changed then the shapes of the MeshCollider will be rebuilt.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Assigns an arbitrary mesh collider to the current transform
        ///
        ///    [SerializeField] Mesh meshToCollide;
        ///
        ///    void Start()
        ///    {
        ///        if (!meshToCollide)
        ///        {
        ///            Debug.LogError("Assign a mesh in the inspector");
        ///            return;
        ///        }
        ///        MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
        ///        meshCollider.sharedMesh = meshToCollide;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public Mesh sharedMesh { get; set; }
        ///<summary>Use a convex collider from the mesh.</summary>
        ///<remarks>This means that if you have this set to true, your mesh collider wont have holes or entrances.
        ///Convex meshes can collide with other convex colliders and non-convex meshes.
        ///Thus convex mesh colliders are suitable on rigidbodies,
        ///if you really need more detailed colliders than what the primitive colliders provide you with.
        ///
        ///**Note**: A convex mesh is required by the physics engine to have a non-zero volume. Flat meshes such as quads or planes that are marked as convex will be modified by the physics engine to have a thickness (and therefore a volume) to satisfy this requirement. The thickness of the resulting mesh is proportional to its size and can be up to 0.05 of its longest dimension in the plane of the mesh.</remarks>
        extern public bool convex { get; set; }

        ///<summary>Options used to enable or disable certain features in mesh cooking.</summary>
        ///<remarks>Mesh cooking is a process of turning a normal mesh into a mesh that is suitable for use in the physics engine. Cooking builds the spatial search structures for the physics queries such as <see cref="Physics.Raycast" /> as well as supporting structures for the contacts generation. Any mesh has to be cooked before using it runtime. This can happen at import time (if you set the "Generate Colliders" option of the ModelImporter) or run-time.
        ///
        ///Mostly useful when generating meshes run-time as it allows to disable certain validity checks that take time to run. Note that with the validity checks disabled, it's the user's responsibilty to provide valid data, otherwise the behaviour might be undefined.</remarks>
        extern public MeshColliderCookingOptions cookingOptions { get; set; }

        [NativeMethod("IsScaleBakingRequired")]
        [VisibleToOtherModules("UnityEditor.ProjectAuditorModule")]
        extern internal bool IsScaleBakingRequired();
    }
}
