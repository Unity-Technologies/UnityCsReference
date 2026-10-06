// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;

namespace UnityEngine
{
    ///<summary>A pair of SphereColliders used to define shapes for Cloth objects to collide against.</summary>
    ///<remarks>A ClothSphereColliderPair can contain either a single valid SphereCollider instance (with the second one being null), or a pair of two SphereColliders. In the former cases the ClothSphereColliderPair just represents a single SphereCollider for the cloth to collide against. In the latter case, it represents a conic capsule shape defined by the two spheres, and the cone connecting the two. Conic capsule shapes are useful for modelling limbs of a character.
    ///
    ///Select the cloth object to see a visualization of Cloth colliders shapes in the Scene view.</remarks>
    [NativeHeader("Modules/Cloth/Cloth.h")]
    [UsedByNativeCode]
    public struct ClothSphereColliderPair
    {
        ///<summary>The first SphereCollider of a ClothSphereColliderPair.</summary>
        public SphereCollider first { get; set; }
        ///<summary>The second SphereCollider of a ClothSphereColliderPair.</summary>
        public SphereCollider second { get; set; }

        ///<summary>Creates a ClothSphereColliderPair. If only one SphereCollider is given, the ClothSphereColliderPair will define a simple sphere. If two SphereColliders are given, the ClothSphereColliderPair defines a conic capsule shape, composed of the two spheres and the cone connecting the two.</summary>
        ///<param name="a">The first SphereCollider of a ClothSphereColliderPair.</param>
        public ClothSphereColliderPair(SphereCollider a)
        {
            // initialize internal fields so that compiler does not complain about using properties before "this" is ready
            first = a;
            second = null;
        }

        ///<summary>Creates a ClothSphereColliderPair. If only one SphereCollider is given, the ClothSphereColliderPair will define a simple sphere. If two SphereColliders are given, the ClothSphereColliderPair defines a conic capsule shape, composed of the two spheres and the cone connecting the two.</summary>
        ///<param name="a">The first SphereCollider of a ClothSphereColliderPair.</param>
        ///<param name="b">The second SphereCollider of a ClothSphereColliderPair.</param>
        public ClothSphereColliderPair(SphereCollider a, SphereCollider b)
        {
            // initialize internal fields so that compiler does not complain about using properties before "this" is ready
            first = a;
            second = b;
        }
    }

    // The ClothSkinningCoefficient struct is used to set up how a [[Cloth]] component is allowed to move with respect to the [[SkinnedMeshRenderer]] it is attached to.
    ///<summary>The ClothSkinningCoefficient struct is used to set up how a <see cref="Cloth" /> component is allowed to move with respect to the <see cref="SkinnedMeshRenderer" /> it is attached to.</summary>
    ///<remarks>This is set using the <see cref="Cloth.coefficients" /> property on the Cloth component, which is a
    ///per-vertex array of ClothSkinningCoefficient structs. Typically, you will not set these values from code,
    ///but instead, set them up in the editor which shows up when you look at the Cloth component in the inspector.</remarks>
    [UsedByNativeCode]
    public struct ClothSkinningCoefficient
    {
        //Distance a vertex is allowed to travel from the skinned mesh vertex position.
        ///<summary>Distance a vertex is allowed to travel from the skinned mesh vertex position.</summary>
        ///<remarks>The Cloth component makes sure
        ///that the cloth vertices stay within maxDistance from the skinned mesh vertex positions.
        ///If maxDistance is zero, the vertex is not simulated but set to the skinned mesh vertex position.
        ///This behavior is useful for attaching the cloth vertex to the skin of an animated character.
        ///Default: 0.2
        ///Range: [0, inf).</remarks>
        public float maxDistance;

        //Definition of a sphere a vertex is not allowed to enter. This allows collision against the animated cloth.
        ///<summary>Definition of a sphere a vertex is not allowed to enter. This allows collision against the animated cloth.</summary>
        ///<remarks>The pair (collisionSphereRadius, collisionSphereDistance) define a sphere for each cloth vertex. The sphere's
        ///center is located at the position
        ///constrainPosition - constrainNormal * (collisionSphereRadius + collisionSphereDistance) and its radius
        ///is collisionSphereRadius, where constrainPosition and constrainNormal are the vertex positions and normals
        ///generated by the SkinnedMeshRenderer. The Cloth makes sure that the cloth vertex does not enter this sphere.
        ///As a typical usecase, set collisionSphereDistance to zero and collisionSphereRadius to a large value
        ///w.r.t. the triangle size. In this setup, the cloth collides against the skinned mesh.
        ///Default: 0.0
        ///Range: [0,inf).</remarks>
        public float collisionSphereDistance;
    }

    ///<summary>The Cloth class provides an interface to cloth simulation physics.</summary>
    [RequireComponent(typeof(Transform), typeof(SkinnedMeshRenderer))]
    [NativeHeader("Modules/Cloth/Cloth.h")]
    [NativeClass("Unity::Cloth", PersistentTypeId = 183)]
    public sealed partial class Cloth : Component
    {
        internal Cloth(global::UnityEngine.EntityId id) : base(id) {}
        public Cloth() {}
        ///<summary>The current vertex positions of the cloth object.</summary>
        ///<remarks>This gives you read access to the vertex positions of the cloth object, so you can analyse it's current simulation state.
        ///Note that the vertex indices may not necessarily correspond to the indices of the source mesh - especially when triangle stripping
        ///or UV seams are used in the source mesh (ie, multiple indices for the same vertex), cloth vertices will be different, as the cloth
        ///simulation only uses a single index for each vertex.</remarks>
        extern public Vector3[] vertices {[NativeName("GetPositions")] get; }
        ///<summary>The current normals of the cloth object.</summary>
        ///<remarks>This gives you read access to the normals of the cloth object, so you can analyse it's current simulation state.
        ///Note that the normal indices may not necessarily correspond to the indices of the source mesh - especially when triangle stripping
        ///or UV seams are used in the source mesh (ie, multiple indices for the same vertex), cloth vertices will be different, as the cloth
        ///simulation only uses a single index for each vertex.</remarks>
        extern public Vector3[] normals {[NativeName("GetNormals")] get; }
        ///<summary>The cloth skinning coefficients used to set up how the cloth interacts with the skinned mesh.</summary>
        extern public ClothSkinningCoefficient[] coefficients {[NativeName("GetCoefficients")] get; [NativeName("SetCoefficients")] set; }
        ///<summary>An array of CapsuleColliders which this Cloth instance should collide with.</summary>
        ///<remarks>Any capsule colliders specified in <see cref="Cloth.capsuleColliders" /> will collide with the cloth vertices. A maximum of 32 capsules are supported.</remarks>
        extern public CapsuleCollider[] capsuleColliders {[NativeName("GetCapsuleColliders")] get; [NativeName("SetCapsuleColliders")] set; }
        ///<summary>An array of ClothSphereColliderPairs which this Cloth instance should collide with.</summary>
        ///<remarks>Any ClothSphereColliderPair specified in <see cref="Cloth.sphereColliders" /> will collide with the cloth vertices. A maximum of 32 spheres are supported.</remarks>
        extern public ClothSphereColliderPair[] sphereColliders {[NativeName("GetSphereColliders")] get; [NativeName("SetSphereColliders")] set; }
        ///<summary>Cloth's sleep threshold.</summary>
        ///<remarks>Sleep threshold is a linear speed of cloth particles, below which the whole cloth piece is going to fall asleep.</remarks>
        extern public float sleepThreshold { get; set; }

        // Bending stiffness of the cloth.
        ///<summary>Bending stiffness of the cloth.</summary>
        ///<remarks>Must be between zero and one. Setting the value to zero disables bending stiffness simulation.
        ///
        ///In many cases, when you want a cloth to be more stiff, actually reducing the number of vertices
        ///will get you better results - and performance.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        GetComponent<Cloth>().bendingStiffness = 1;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float bendingStiffness { get; set; }

        // Stretching stiffness of the cloth.
        ///<summary>Stretching stiffness of the cloth.</summary>
        ///<remarks>Must be greater than zero and smaller or equal to one.
        ///
        ///In many cases, when you want a cloth to be more stiff, actually reducing the number of vertices
        ///will get you better results - and performance.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        GetComponent<Cloth>().stretchingStiffness = 1;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float stretchingStiffness { get; set; }

        // Damp cloth motion.
        ///<summary>Damp cloth motion.</summary>
        ///<remarks>Set this to damp the motions of a cloth instance. Must be between zero and one.
        ///Setting this to zero will disable cloth damping.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        GetComponent<Cloth>().damping = 1;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float damping { get; set; }

        // A constant, external acceleration applied to the cloth.
        ///<summary>A constant, external acceleration applied to the cloth.</summary>
        ///<remarks>Use this to simulate constant forces on the cloth, such as wind waving a flag.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Make this cloth fall at half speed (if is affected by gravity).
        ///    void Start()
        ///    {
        ///        GetComponent<Cloth>().externalAcceleration =  -Physics.gravity / 2;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Cloth.randomAcceleration" />
        extern public Vector3 externalAcceleration { get; set; }

        // A random, external acceleration applied to the cloth.
        ///<summary>A random, external acceleration applied to the cloth.</summary>
        ///<remarks>Use this to simulate randomly changing forces on the cloth, such as wind turbulences waving a flag.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Simulate wind going trough the X axis.
        ///    void Start()
        ///    {
        ///        GetComponent<Cloth>().randomAcceleration = new Vector3(10, 0, 0);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Cloth.externalAcceleration" />
        extern public Vector3 randomAcceleration { get; set; }

        // Should gravity affect the cloth simulation?
        ///<summary>Should gravity affect the cloth simulation?</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Example()
        ///    {
        ///        // Dont use gravity on this cloth regardless if is Interactive or Skinned.
        ///        transform.GetComponent<Cloth>().useGravity = false;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public bool useGravity { get; set; }

        // Is this cloth enabled?
        ///<summary>Is this cloth enabled?</summary>
        ///<remarks>This is the same as the checkbox next to the component label in the inspector.
        ///A disabled cloth component will not update it's physics simulation, so you can use this to suspend the simulation of cloth
        ///objects when they are not needed, as cloth simulation is a very CPU-intensive task.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Example()
        ///    {
        ///        GetComponent<Cloth>().enabled = false;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public bool enabled { get; set; }

        // The friction of the cloth when colliding with the character.
        ///<summary>The friction of the cloth when colliding with the character.</summary>
        extern public float friction { get; set; }

        // How much to increase mass of colliding particles
        ///<summary>How much to increase mass of colliding particles.</summary>
        extern public float collisionMassScale { get; set; }

        // Enable continuous collision to improve collision stability
        ///<summary>Enable continuous collision to improve collision stability.</summary>
        ///<remarks>Continuous collision is around 2x more computationally expensive than discrete collision, but it is necessary to detect collision between fast moving objects. Continuous collision analyzes the trajectory of particles and capsules to determine when a contact occurs. After the first time of contact, the particle is moved with the shape until the end of the iteration.</remarks>
        extern public bool enableContinuousCollision { get; set; }

        // Add 1 virtual particle per triangle to improve collision stability
        ///<summary>Add one virtual particle per triangle to improve collision stability.</summary>
        ///<remarks>Virtual particles provide a way of improving cloth collision without increasing the cloth resolution. They are called 'virtual' particles because they only exist during the collision processing stage and do not have their position, velocity or mass explicitly stored like regular particles, they can be thought of as providing additional samples on the collision surface.</remarks>
        extern public float useVirtualParticles { get; set; }

        // How much world-space movement of the character will affect cloth vertices.
        ///<summary>How much world-space movement of the character will affect cloth vertices.</summary>
        extern public float worldVelocityScale { get; set; }

        // How much world-space acceleration of the character will affect cloth vertices.
        ///<summary>How much world-space acceleration of the character will affect cloth vertices.</summary>
        extern public float worldAccelerationScale { get; set; }

        ///<summary>Number of cloth solver iterations per second.</summary>
        ///<remarks>The solver frequency is specified as iterations per second. A solver frequency value of 240 corresponds to 4 iterations per frame at 60 frames per second. In general, simulation will become more accurate if higher solver frequency value is used. However, simulation time grows roughly linearly with solver frequency. Typically this value is between 120 and 300.</remarks>
        extern public float clothSolverFrequency { get; set; }

        ///<summary>Use Tether Anchors.</summary>
        ///<remarks>Apply constraints that help to prevent the moving cloth particles from going too far away from the fixed ones. This helps to reduce excess stretchiness.</remarks>
        extern public bool useTethers { get; set; }

        ///<summary>Sets the stiffness frequency parameter.</summary>
        ///<remarks>The stiffness frequency controls the power-law nonlinearity of all rate of change parameters (stretch stiffness, shear stiffness, bending stiffness, tether stiffness, self-collision stiffness, motion constraint stiffness, damp coefficient, linear and angular drag coefficients). Increasing the frequency avoids numerical cancellation for values near zero or one, but increases the non-linearity of the parameter. It is not recommended to change this parameter after cloth initialization. For example, the portion of edge overstretch removed per second is equal to the stretch stiffness raised to the power of the stiffness frequency.</remarks>
        extern public float stiffnessFrequency { get; set; }

        ///<summary>Minimum distance at which two cloth particles repel each other (default: 0.0).</summary>
        ///<remarks>A value larger than 0.0 enables particle versus particle collision. Self-collision distance should be smaller than the smallest distance between two particles in the rest configuration. If the distance is larger, self-collision may violate some distance constraints and result in jittering.</remarks>
        extern public float selfCollisionDistance { get; set; }

        ///<summary>Self-collision stiffness defines how strong the separating impulse should be for colliding particles.</summary>
        extern public float selfCollisionStiffness { get; set; }

        ///<summary>Clear the pending transform changes from affecting the cloth simulation.</summary>
        ///<remarks>When the transform of a cloth changes, the cloth will not directly follow that change, but instead, the new positions of the SkinnedMeshRenderer's vertices will affect the cloth through the configured constraints in the next cloth simulation update, so that moving the tranform will result in realistic motion of the cloth.
        ///
        ///You can call ClearTransformMotion on the cloth to change this behavior. Calling ClearTransformMotion will move the cloth simulation particles along with the transform, so that the transform movement has no effect on the cloth simulation. This is useful if you want to teleport Characters from one point in the Scene to another, without having the cloth suddenly jerk into place.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    Vector3 newPosition;
        ///
        ///    void Start()
        ///    {
        ///        transform.position = newPosition;
        ///        GetComponent<Cloth>().ClearTransformMotion();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public void ClearTransformMotion();

        ///<summary>Get list of particles to be used for self and inter collision.</summary>
        ///<remarks>This allows you to access the cloth indices used for self and inter collision. The same set of indices is used in both situations if necessary. To enable self-collision, both self-collision distance and self-collision stiffness should be set to to non-zero values. To enable inter-collision, both inter-collision distance and inter-collision stiffness should be set to to non-zero values.</remarks>
        ///<param name="indices">List to be populated with cloth particle indices that are used for self and/or inter collision.</param>
        extern public void GetSelfAndInterCollisionIndices([NotNull] List<uint> indices);

        ///<summary>This allows you to set the cloth indices used for self and inter collision.</summary>
        ///<remarks>The same set of indices is used in both situations if necessary. To enable self-collision, both self-collision distance and self-collision stiffness should be set to to non-zero values. To enable inter-collision, both inter-collision distance and inter-collision stiffness should be set to to non-zero values.</remarks>
        ///<param name="indices">List of cloth particles indices to use for cloth self and/or inter collision.</param>
        extern public void SetSelfAndInterCollisionIndices([NotNull] List<uint> indices);

        ///<summary>Get list of indices to be used when generating virtual particles.</summary>
        ///<remarks>Virtual particles provide more robust and accurate collision handling against collision spheres and capsules. More virtual particles will generally increase the accuracy of collision handling, and thus a sufficient number of virtual particles can mimic triangle-based collision handling.
        ///Virtual particles are specified as barycentric interpolation of real particles: The position of a virtual particle is w0 * P0 + w1 * P1 + w2 * P2, where P1, P2, P3 real particle positions. The barycentric weights w0, w1, w2 are stored in a separate table so they can be shared across multiple virtual particles.</remarks>
        ///<param name="indicesOutList">List to be populated with virtual particle indices.</param>
        extern public void GetVirtualParticleIndices([NotNull] List<uint> indicesOutList);

        ///<summary>Set indices to use when generating virtual particles.</summary>
        ///<remarks>Virtual particles provide more robust and accurate collision handling against collision spheres and capsules. More virtual particles will generally increase the accuracy of collision handling, and thus a sufficient number of virtual particles can mimic triangle-based collision handling.
        ///Virtual particles are specified as barycentric interpolation of real particles: The position of a virtual particle is w0 * P0 + w1 * P1 + w2 * P2, where P1, P2, P3 real particle positions. The barycentric weights w0, w1, w2 are stored in a separate table so they can be shared across multiple virtual particles.</remarks>
        ///<param name="indicesIn">List of cloth particle indices to use when generating virtual particles.</param>
        extern public void SetVirtualParticleIndices([NotNull] List<uint> indicesIn);

        ///<summary>Get weights to be used when generating virtual particles for cloth.</summary>
        ///<remarks>Virtual particles provide more robust and accurate collision handling against collision spheres and capsules. More virtual particles will generally increase the accuracy of collision handling, and thus a sufficient number of virtual particles can mimic triangle-based collision handling.
        ///Virtual particles are specified as barycentric interpolation of real particles: The position of a virtual particle is w0 * P0 + w1 * P1 + w2 * P2, where P1, P2, P3 real particle positions. The barycentric weights w0, w1, w2 are stored in a separate table so they can be shared across multiple virtual particles.</remarks>
        ///<param name="weightsOutList">List to be populated with virtual particle weights.</param>
        extern public void GetVirtualParticleWeights([NotNull] List<Vector3> weightsOutList);

        ///<summary>Sets weights to be used when generating virtual particles for cloth.</summary>
        ///<remarks>Virtual particles provide more robust and accurate collision handling against collision spheres and capsules. More virtual particles will generally increase the accuracy of collision handling, and thus a sufficient number of virtual particles can mimic triangle-based collision handling.
        ///Virtual particles are specified as barycentric interpolation of real particles: The position of a virtual particle is w0 * P0 + w1 * P1 + w2 * P2, where P1, P2, P3 real particle positions. The barycentric weights w0, w1, w2 are stored in a separate table so they can be shared across multiple virtual particles.</remarks>
        ///<param name="weights">List of weights to be used when setting virutal particles for cloth.</param>
        extern public void SetVirtualParticleWeights([NotNull] List<Vector3> weights);

        ///<summary>Fade the cloth simulation in or out.</summary>
        ///<param name="enabled">Fading enabled or not.</param>
        extern public void SetEnabledFading(bool enabled, float interpolationTime);

        [ExcludeFromDocs]
        public void SetEnabledFading(bool enabled)
        {
            SetEnabledFading(enabled, 0.5f);
        }
    }
}
