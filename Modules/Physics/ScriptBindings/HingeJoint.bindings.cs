// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    ///<summary>The HingeJoint groups together 2 rigid bodies, constraining them to move like connected by a hinge.</summary>
    ///<remarks>This joint is great for, well, doors, but can also be used to model chains, etc...
    ///
    ///The HingeJoint has a motor which can be used to make the hinge spin around the joints axis.
    ///A spring which attempts to reach for a target angle by spinning around the joints axis.
    ///And a limit which constrains the joint angle.</remarks>
    [RequireComponent(typeof(Rigidbody))]
    [NativeHeader("Modules/Physics/HingeJoint.h")]
    [NativeClass("Unity::HingeJoint", PersistentTypeId = 59)]
    public class HingeJoint : Joint
    {
        internal HingeJoint(global::UnityEngine.EntityId id) : base(id) {}
        public HingeJoint() {}
        ///<summary>The motor will apply a force up to a maximum force to achieve the target velocity in degrees per second.</summary>
        ///<remarks>
        ///  <para>The motor tries to reach <see cref="JointMotor.targetVelocity" /> angular velocity in degrees per second.
        ///The motor will only be able to reach <c>targetVelocity</c>, if <see cref="JointMotor.force" /> is sufficiently large.
        ///If the joint is spinning faster than <c>targetVelocity</c> the motor will brake.
        ///A negative <c>targetVelocity</c> will make the motor spin in the opposite direction.
        ///
        ///The <c>force</c> is the maximum torque the motor can exert. If it is zero the motor is disabled.
        ///
        ///The motor will brake when it is spinning faster than <c>targetVelocity</c> only, if <see cref="JointMotor.freeSpin" /> is false.
        ///If <c>freeSpin</c> is true the motor will not brake.
        ///
        ///</para>
        ///  <para>Modifying the motor does **not** automatically enable the motor.
        ///
        ///Enabling the motor **overrides** the <see cref="spring" />, given the spring was enabled. If the motor is again disabled the spring will be restored.</para>
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        var hinge = GetComponent<HingeJoint>();
        ///
        ///        // Make the hinge motor rotate with 90 degrees per second and a strong force.
        ///        var motor = hinge.motor;
        ///        motor.force = 100;
        ///        motor.targetVelocity = 90;
        ///        motor.freeSpin = false;
        ///        hinge.motor = motor;
        ///        hinge.useMotor = true;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="useMotor" />
        ///<seealso cref="JointMotor" />
        extern public JointMotor motor { get; set; }
        ///<summary>Limit of angular rotation (in degrees) on the hinge joint.</summary>
        ///<remarks>
        ///  <para>The joint will be limited so that the angle is always between <see cref="JointLimits.min" /> and <see cref="JointLimits.max" />.
        ///The joint angle is in degrees relative to the rest angle. The rest angle between the bodies is always zero at the beginning of the simulation.
        ///
        ///</para>
        ///  <para>Modifying the limits does **not** automatically enable the limits.</para>
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        // Set the hinge limits for a door.
        ///        HingeJoint hinge = GetComponent<HingeJoint>();
        ///
        ///        JointLimits limits = hinge.limits;
        ///        limits.min = 0;
        ///        limits.bounciness = 0;
        ///        limits.bounceMinVelocity = 0;
        ///        limits.max = 90;
        ///        hinge.limits = limits;
        ///        hinge.useLimits = true;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="useLimits" />
        ///<seealso cref="JointLimits" />
        extern public JointLimits limits { get; set; }
        ///<summary>The spring attempts to reach a target angle by adding spring and damping forces.</summary>
        ///<remarks>
        ///  <para>The <see cref="JointSpring.spring" /> force attempts to reach the target angle. A larger value makes the spring reach the target position faster.
        ///
        ///The <see cref="JointSpring.damper" /> force dampens the angular velocity. A larger value makes the spring reach the goal slower.
        ///
        ///The spring reaches for the <see cref="JointSpring.targetPosition" /> angle in degrees relative to the rest angle. The rest angle between the bodies is always zero at the beginning of the simulation.
        ///
        ///</para>
        ///  <para>Modifying the spring does **not** automatically enable it.
        ///
        ///Enabling the <see cref="motor" /> **overrides** the spring, given the spring was enabled. If the motor is again disabled the spring will be restored.</para>
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///
        ///public class HingeExample : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        HingeJoint hinge = GetComponent<HingeJoint>();
        ///
        ///        // Make the spring reach shoot for a 70 degree angle.
        ///        // This could be used to fire off a catapult.
        ///
        ///        JointSpring hingeSpring = hinge.spring;
        ///        hingeSpring.spring = 10;
        ///        hingeSpring.damper = 3;
        ///        hingeSpring.targetPosition = 70;
        ///        hinge.spring = hingeSpring;
        ///        hinge.useSpring = true;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="useSpring" />
        ///<seealso cref="JointSpring" />
        ///<seealso cref="useAcceleration" />
        extern public JointSpring spring { get; set; }
        ///<summary>Enables the joint's <see cref="motor" />. Disabled by default.</summary>
        ///<remarks>
        ///
        ///Enabling the motor **overrides** the <see cref="spring" />, given the spring was enabled. If the motor is again disabled the spring will be restored.</remarks>
        ///<seealso cref="JointMotor" />
        extern public bool useMotor { get; set; }
        ///<summary>Enables the joint's <see cref="limits" />. Disabled by default.</summary>
        ///<seealso cref="JointLimits" />
        extern public bool useLimits { get; set; }
        ///<summary>If enabled, the angle of the hinge is extended to [-360, 360] degrees.</summary>
        extern public bool extendedLimits { get; set; }
        ///<summary>Enables the joint's <see cref="spring" />. Disabled by default.</summary>
        ///<remarks>
        ///
        ///Enabling the <see cref="motor" /> **overrides** the spring, given the spring was enabled. If the motor is again disabled the spring will be restored.</remarks>
        ///<seealso cref="JointSpring" />
        extern public bool useSpring { get; set; }
        ///<summary>The angular velocity of the joint in degrees per second. (RO)</summary>
        extern public float velocity { get; }
        ///<summary>The current angle in degrees of the joint relative to its rest position. (RO)</summary>
        ///<remarks>The rest angle between the bodies is always zero at the beginning of the simulation.</remarks>
        extern public float angle { get; }
        ///<summary>Defines whether the <see cref="HingeJoint.spring" /> outputs accelerations instead of forces.</summary>
        extern public bool useAcceleration { get; set; }
    }
}
