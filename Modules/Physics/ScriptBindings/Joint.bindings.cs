// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    ///<summary>Joint is the base class for all joints.</summary>
    ///<seealso cref="CharacterJoint" />
    ///<seealso cref="HingeJoint" />
    ///<seealso cref="SpringJoint" />
    ///<seealso cref="FixedJoint" />
    ///<seealso cref="ConfigurableJoint" />
    [NativeHeader("Modules/Physics/Joint.h")]
    [NativeClass("Unity::Joint", PersistentTypeId = 57)]
    public class Joint : Component
    {
        public Joint() {}
        internal Joint(global::UnityEngine.EntityId id) : base(id) {}
        ///<summary>A reference to another rigidbody this joint connects to.</summary>
        ///<remarks>If not set then the joint connects the object to a fixed point in world space.
        ///
        ///Unity does not support connecting a joint to a <see cref="Rigidbody" /> in a different <see cref="SceneManagement.Scene" /> that is using a local physics Scene. If a joint is connected to a <see cref="Rigidbody" />, and then that <see cref="Rigidbody" /> is moved to a <see cref="SceneManagement.Scene" /> that uses <see cref="UnityEngine.SceneManagement.LocalPhysicsMode.Physics3D" />, then the joint is automatically disconnected from the <see cref="Rigidbody" />.</remarks>
        ///<seealso cref="UnityEngine.SceneManagement.Scene" />
        ///<seealso cref="UnityEngine.SceneManagement.LocalPhysicsMode" />
        extern public Rigidbody connectedBody
        {
            [NativeName("GetConnectedRigidbody")]
            get;
            [NativeName("SetConnectedRigidbody")]
            set;
        }

        ///<summary>A reference to an articulation body this joint connects to.</summary>
        ///<remarks>A joint can connect to only one of <see cref="Joint.connectedBody" /> and <see cref="Joint.connectedArticulationBody" />. If both are set, <see cref="Joint.connectedBody" /> takes precedence.</remarks>
        extern public ArticulationBody connectedArticulationBody { get; set; }
        ///<summary>The Direction of the axis around which the body is constrained.</summary>
        ///<remarks>The Axis is defined in local space.</remarks>
        extern public Vector3 axis { get; set; }
        ///<summary>The Position of the anchor around which the joints motion is constrained.</summary>
        ///<remarks>The Position is defined in local space.</remarks>
        extern public Vector3 anchor { get; set; }
        ///<summary>Position of the anchor relative to the connected Rigidbody.</summary>
        ///<remarks>If /Joint.autoConfigureConnectedAnchor/ is not enabled, then this will be used to set the position of the anchor on the connected rigidbody. The position is given in local coordinates of the connected rigidbody, or in world coordinates if there is no connected rigidbody.</remarks>
        extern public Vector3 connectedAnchor { get; set; }
        ///<summary>Should the <c>connectedAnchor</c> be calculated automatically?</summary>
        ///<remarks>If this is enabled, then the <c>connectedAnchor</c> property will be calculated automatically to match the global position of the <c>anchor</c> property. This is the default behavior. If this is disabled, you can configure the position of the connected anchor using the <c>connectedAnchor</c> property.</remarks>
        extern public bool autoConfigureConnectedAnchor { get; set; }
        ///<summary>The force that needs to be applied for this joint to break.</summary>
        ///<remarks>The force might come from collisions with other objects, forces applied with <see cref="Rigidbody.AddTorque" /> or from other joints.
        ///The break force can be set to <see cref="Mathf.Infinity" /> to render the joint unbreakable.</remarks>
        ///<seealso cref="M:UnityEngine.MonoBehaviour.OnJointBreak(System.Single)" />
        extern public float breakForce { get; set; }
        ///<summary>The torque that needs to be applied for this joint to break. To be able to break, a joint must be _Locked_ or _Limited_ on the axis of rotation where the torque is being applied. This means that some joints cannot break, such as an unconstrained Configurable Joint.</summary>
        ///<remarks>The torque might come from collisions with other objects, forces applied with <see cref="Rigidbody.AddTorque" /> or from other joints.
        ///The break torque can be set to <see cref="Mathf.Infinity" /> to render the joint unbreakable.</remarks>
        ///<seealso cref="M:UnityEngine.MonoBehaviour.OnJointBreak(System.Single)" />
        extern public float breakTorque { get; set; }
        ///<summary>Enable collision between bodies connected with the joint.</summary>
        extern public bool enableCollision { get; set; }
        ///<summary>Toggle preprocessing for this joint.</summary>
        ///<remarks>This flag has a connection with rigidbodies that have some of their rotational degrees of freedom frozen. The common example is a 2D game that uses 3D rigidbodies with some of their translational and rotational degrees of freedom frozen.
        ///
        ///Rigidbody rotations freezing is internally implemented by setting an infinite inertia around those frozen axes so that the body does not rotate because it's very resistant to.
        ///
        ///This approach has some nice properties: most significantly it lets such bodies to correctly go to sleep as opposed to the approach where we would cancel out the rotations around the frozen axes as a post-solver step.
        ///
        ///However the downside is that very stiff solver constraints can be generated when such bodies are connected with joints. When the flag is set, PhysX would ignore constraints that produce huge impulses generating only a small change in velocity.
        ///
        ///Whilst it may reduce the overall accuracy of the joint simulation, it's been proven to help with overconstrained configurations like in the 2D case.</remarks>
        extern public bool enablePreprocessing { get; set; }
        ///<summary>The scale to apply to the inverse mass and inertia tensor of the body prior to solving the constraints.</summary>
        ///<remarks>Scale mass and the inertia tensor to make the joints solver converge faster, thus resulting in less stretch of the limbs of a typical ragdoll. Most useful in conjunction with <see cref="Joint.connectedMassScale" />.
        ///
        ///For example, if you have two objects in a ragdoll of masses 1 and 10, the physics engine will typically resolve the joint by changing the velocity of the lighter body much more than the heavier one. Applying a mass scale of 10 to the first body makes solver change the velocity of both bodies by an equal amount. Applying mass scales such that the joint sees similar effective masses and inertias makes the solver converge faster, which can make individual joints seem less rubbery or separated, and sets of jointed bodies appear less twitchy
        ///
        ///Note that scaling mass and inertia is fundamentally nonphysical and momentum won't be conserved.
        ///
        ///The following script is useful to adjust the mass and inertia scaling in order to get the same corrective velocity out of the solver. Attach it to the ragdoll's root, or to a limb that is over-stretched during the gameplay and it will find all joints down in the transform hierarchy below itself and adjust the mass scale.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using System.Collections;
        ///using System.Collections.Generic;
        ///using UnityEngine;
        ///
        ///public class NormalizeMass : MonoBehaviour
        ///{
        ///    private void Apply(Transform root)
        ///    {
        ///        var j = root.GetComponent<Joint>();
        ///
        ///        // Apply the inertia scaling if possible
        ///        if (j && j.connectedBody)
        ///        {
        ///            // Make sure that both of the connected bodies will be moved by the solver with equal speed
        ///            j.massScale = j.connectedBody.mass / root.GetComponent<Rigidbody>().mass;
        ///            j.connectedMassScale = 1f;
        ///        }
        ///
        ///        // Continue for all children...
        ///        for (int childId = 0; childId < root.childCount; ++childId)
        ///        {
        ///            Apply(root.GetChild(childId));
        ///        }
        ///    }
        ///
        ///    public void Start()
        ///    {
        ///        Apply(this.transform);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float massScale { get; set; }
        ///<summary>The scale to apply to the inverse mass and inertia tensor of the connected body prior to solving the constraints.</summary>
        ///<seealso cref="Joint.massScale" />
        extern public float connectedMassScale { get; set; }

        extern private void GetCurrentForces(ref Vector3 linearForce, ref Vector3 angularForce);

        ///<summary>The force applied by the solver to satisfy all constraints.</summary>
        ///<remarks>The returned value is relative to <see cref="Joint.connectedBody" /> if it's set. Otherwise, in world space. .</remarks>
        ///<seealso cref="Joint.currentTorque" />
        public Vector3 currentForce
        {
            get
            {
                Vector3 force = Vector3.zero;
                Vector3 torque = Vector3.zero;
                GetCurrentForces(ref force, ref torque);
                return force;
            }
        }

        ///<summary>The torque applied by the solver to satisfy all constraints.</summary>
        ///<remarks>The returned value is relative to <see cref="Joint.connectedBody" /> if it's set. Otherwise, in world space. .</remarks>
        ///<seealso cref="Joint.currentForce" />
        public Vector3 currentTorque
        {
            get
            {
                Vector3 force = Vector3.zero;
                Vector3 torque = Vector3.zero;
                GetCurrentForces(ref force, ref torque);
                return torque;
            }
        }

        extern internal Matrix4x4 GetLocalPoseMatrix(int bodyIndex);
    }
}
