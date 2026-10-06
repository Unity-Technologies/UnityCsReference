// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    ///<summary>Character Joints are mainly used for Ragdoll effects.</summary>
    ///<remarks>They are an extended ball-socket joint which allows you to limit the joint on each axis.</remarks>
    [RequireComponent(typeof(Rigidbody))]
    [NativeHeader("Modules/Physics/CharacterJoint.h")]
    [NativeClass("Unity::CharacterJoint", PersistentTypeId = 144)]
    public partial class CharacterJoint : Joint
    {
        internal CharacterJoint(global::UnityEngine.EntityId id) : base(id) {}
        public CharacterJoint() {}
        ///<summary>The secondary axis around which the joint can rotate.</summary>
        ///<remarks>
        ///  <see cref="CharacterJoint.swing1Limit" /> are the limits of the rotation allowed around this axis.</remarks>
        extern public Vector3 swingAxis { get; set; }
        ///<summary>The configuration of the spring attached to the twist limits of the joint.</summary>
        extern public SoftJointLimitSpring twistLimitSpring { get; set; }
        ///<summary>The configuration of the spring attached to the swing limits of the joint.</summary>
        extern public SoftJointLimitSpring swingLimitSpring { get; set; }
        ///<summary>The lower limit around the primary axis of the character joint.</summary>
        ///<remarks>The limit is relative to the angle the two rigidbodies started the simulation out with.</remarks>
        extern public SoftJointLimit lowTwistLimit { get; set; }
        ///<summary>The upper limit around the primary axis of the character joint.</summary>
        ///<remarks>The limit is relative to the angle the two rigidbodies started the simulation out with.</remarks>
        extern public SoftJointLimit highTwistLimit { get; set; }
        ///<summary>The angular limit of rotation (in degrees) around the primary axis of the character joint.</summary>
        ///<remarks>The limit is symmetric. For example, a value of 30 will limit the rotation between -30 and +30 degrees.
        ///The limit is relative to the angle the two rigidbodies started the simulation out with.</remarks>
        extern public SoftJointLimit swing1Limit { get; set; }
        ///<summary>The angular limit of rotation (in degrees) around the primary axis of the character joint.</summary>
        ///<remarks>The limit is symmetric. Thus a value of eg. 30 will limit the rotation between -30 and +30 degrees.
        ///The limit is relative to the angle the two rigidbodies started the simulation out with.</remarks>
        extern public SoftJointLimit swing2Limit { get; set; }
        ///<summary>Brings violated constraints back into alignment even when the solver fails.</summary>
        ///<remarks>Projection is not a physical process and does not preserve momentum or respect collision geometry. It is best avoided if practical, but can be useful in improving simulation quality where joint separation results in unacceptable artifacts.</remarks>
        extern public bool enableProjection { get; set; }
        ///<summary>Set the linear tolerance threshold for projection.</summary>
        ///<remarks>If the joint separates by more than this distance along its locked degrees of freedom, the solver
        ///will move the bodies to close the distance.
        ///
        ///Setting a very small tolerance may result in simulation jitter or other artifacts.
        ///
        ///Sometimes it is not possible to project (for example when the joints form a cycle).</remarks>
        extern public float projectionDistance { get; set; }
        ///<summary>Set the angular tolerance threshold (in degrees) for projection.</summary>
        ///<remarks>If the joint deviates by more than this angle around its locked angular degrees of freedom,
        ///the solver will move the bodies to close the angle.
        ///
        ///Setting a very small tolerance may result in simulation jitter or other artifacts.
        ///
        ///Sometimes it is not possible to project (for example when the joints form a cycle).</remarks>
        extern public float projectionAngle { get; set; }
    }
}
