// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    ///<summary>Contact information for the wheel, reported by <see cref="WheelCollider" />.</summary>
    ///<remarks>Friction for the <see cref="WheelCollider" /> is computed separately from the rest of the physics, using
    ///a slip based tire friction model. This allows for more realistic behaviour, but makes
    ///wheel colliders ignore standard <see cref="PhysicsMaterial" /> settings.
    ///
    ///The way to simulate different ground materials is to query <see cref="WheelCollider" /> for its collision
    ///information (see <see cref="WheelCollider.GetGroundHit" />). Usually, you get the other collider the wheel
    ///is hitting, and modify the wheel's <see cref="WheelCollider.forwardFriction" /> and <see cref="WheelCollider.sidewaysFriction" />
    ///based on the physics material of the ground.
    ///
    ///The other members of the WheelHit structure are usually queried for information purposes or special
    ///effects. For example, a "slipping tire" sound can be played if <see cref="forwardSlip" /> or <see cref="sidewaysSlip" />
    ///exceed some threshold.</remarks>
    ///<seealso cref="WheelCollider.GetGroundHit" />
    [NativeHeader("Modules/Vehicles/WheelCollider.h")]
    public struct WheelHit
    {
        [NativeName("point")] private Vector3 m_Point;
        [NativeName("normal")] private Vector3 m_Normal;
        [NativeName("forwardDir")] private Vector3 m_ForwardDir;
        [NativeName("sidewaysDir")] private Vector3 m_SidewaysDir;
        [NativeName("force")] private float m_Force;
        [NativeName("forwardSlip")] private float m_ForwardSlip;
        [NativeName("sidewaysSlip")] private float m_SidewaysSlip;
        [NativeName("collider")] private Collider m_Collider;

        ///<summary>The other <see cref="Collider" /> the wheel is hitting.</summary>
        public Collider collider { get { return m_Collider; } set { m_Collider = value; }}
        ///<summary>The point of contact between the wheel and the ground.</summary>
        public Vector3    point { get { return m_Point; } set { m_Point = value; } }
        ///<summary>The normal at the point of contact.</summary>
        public Vector3    normal { get { return m_Normal; } set { m_Normal = value; } }
        ///<summary>The direction the wheel is pointing in.</summary>
        public Vector3    forwardDir { get { return m_ForwardDir; } set { m_ForwardDir = value; } }
        ///<summary>The sideways direction of the wheel.</summary>
        public Vector3    sidewaysDir { get { return m_SidewaysDir; } set { m_SidewaysDir = value; } }
        ///<summary>The magnitude of the force being applied for the contact.</summary>
        public float      force { get { return m_Force; } set { m_Force = value; } }
        ///<summary>Tire slip in the rolling direction. Acceleration slip is negative, braking slip is positive.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Prints "braking slip!" when tire slips badly.
        ///    void FixedUpdate()
        ///    {
        ///        WheelHit hit = new WheelHit();
        ///        WheelCollider wheel = GetComponent<WheelCollider>();
        ///        if (wheel.GetGroundHit(out hit))
        ///        {
        ///            if (hit.forwardSlip > 0.5)
        ///                print("braking slip!");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public float      forwardSlip { get { return m_ForwardSlip; } set { m_ForwardSlip = value; } }
        ///<summary>Tire slip in the sideways direction.</summary>
        public float      sidewaysSlip { get { return m_SidewaysSlip; } set { m_SidewaysSlip = value; } }
    }

    ///<summary>A special collider for vehicle wheels.</summary>
    ///<remarks>Wheel collider is used to model vehicle wheels. It simulates a spring and damper suspension setup,
    ///and uses a slip based tire friction model to calculate wheel contact forces.
    ///
    ///Wheel's collision detection is performed by casting a ray from <see cref="center" /> downwards the local
    ///y-axis. The wheel has a <see cref="radius" /> and can extend downwards by <see cref="suspensionDistance" />
    ///amount.
    ///
    ///The wheel is controlled with <see cref="motorTorque" />, <see cref="brakeTorque" /> and <see cref="steerAngle" /> properties.
    ///
    ///Wheel collider computes friction separately from the rest of physics engine, using a slip based
    ///friction model. This allows for more realistic behaviour, but makes
    ///wheel colliders ignore standard <see cref="PhysicsMaterial" /> settings. Simulation of different road materials
    ///is done by changing the <see cref="forwardFriction" /> and <see cref="sidewaysFriction" />
    ///based on what material the wheel is hitting.  and <see cref="WheelFrictionCurve" />.</remarks>
    ///<seealso cref="GetGroundHit" />
    [NativeHeader("Modules/Vehicles/WheelCollider.h")]
    [global::UnityEngine.NativeClass("WheelCollider", PersistentTypeId = 146)]
    [NativeHeader("PhysicsScriptingClasses.h")]
    public class WheelCollider : Collider
    {
        internal WheelCollider(global::UnityEngine.EntityId id) : base(id) {}
        public WheelCollider() {}
        ///<summary>The center of the wheel, measured in the object's local space.</summary>
        ///<remarks>The center of the wheel describes the coordinate that the wheel would achieve if the car was suspended in mid-air.  This is equivalent to the coordinate of the wheel center when the spring is at maximum elongation.</remarks>
        public extern Vector3 center {get; set; }
        ///<summary>The radius of the wheel, measured in local space.</summary>
        ///<remarks>The radius will be scaled by the transform's scale.</remarks>
        public extern float radius {get; set; }
        ///<summary>Maximum extension distance of wheel suspension, measured in local space.</summary>
        ///<remarks>Suspension always extends downwards the local y-axis.
        ///Suspension travel will be scaled by the transform's scale.
        ///The value <c>suspensionDistance</c> is the distance that the wheel travels as it moves along the local up vector of the rigid body from the coordinate of the wheel center at maximum spring elongation to the coordinate of the wheel center at maximum spring compression. It is expressed in metres.
        ///The range of suspension travel will be scaled by the transform's scale.</remarks>
        public extern float suspensionDistance {get; set; }
        ///<summary>The parameters of wheel's suspension. The suspension attempts to reach a target position by applying a linear force and a damping force.</summary>
        ///<remarks>The value <see cref="JointSpring.spring">suspensionSpring.spring</see> describes the stiffness of the spring. It is expressed in Newtons per metre. The spring strength has a profound influence on handling by modulating the time it takes for the vehicle to respond to bumps in the road and on the amount of load experienced by the tire.  Larger values make the suspension reach the target position faster but at the cost of increased load and handling variability.  Smaller values provide a smoother but less responsive ride.
        ///
        ///The value <see cref="JointSpring.damper">suspensionSpring.damper</see> describes the rate at which the spring dissipates the energy stored in the spring. It is expressed in Newtons seconds per metre (equivalent to Newtons per unit speed). Larger values make the suspension reach the target position slower, while lower values make the car appear more bouncy.  Vehicle suspensions typically have a response close to critical damping.
        ///
        ///The rest coordinate of the wheel is specified by <see cref="JointSpring.targetPosition">suspensionSpring.targetPosition</see>.  This value describes the rest coordinate of the wheel as a fraction in range (0, 1) along the <c>suspensionDistance</c>. Zero value maps to full extension along the suspension travel,  while a value of one maps to fully compressed suspension. Default value is 0.5, which sets the rest coordinate of the wheel to the mid-point between the suspension at maximum elongation and maximum compression.  Typical values would be in range (0.3, 0.7).</remarks>
        public extern JointSpring suspensionSpring {get; set; }
        ///<summary>Limits the expansion velocity of the Wheel Collider's suspension. If you set this property on a Rigidbody that has several Wheel Colliders, such as a vehicle, then it affects all other Wheel Colliders on the Rigidbody.</summary>
        ///<remarks>If you use a Wheel Collider that has extreme values for Suspension Spring properties, such as  <see cref="JointSpring.damper">suspensionSpring.damper</see> or <see cref="JointSpring.spring">suspensionSpring.spring</see>, the large damping forces might make the vehicle stick to the ground, instead of lifting off. While it's best practice to use realistic damping ratios, you can use this property to limit the velocity of suspension.
        ///
        ///In more detail, the simulation checks whether the suspension can extend to the target length in the given simulation time step. If it can, Unity computes the suspension force as usual, otherwise it sets the force to zero. If you use this feature, it results in a slightly more realistic behavior at the potential cost of losing control when steering the vehicle.</remarks>
        public extern bool suspensionExpansionLimited {get; set; }
        ///<summary>Application point of the suspension and tire forces measured from the base of the resting wheel.</summary>
        ///<remarks>This is specified as a distance along the local up vector of the vehicle's rigid body from the base of the wheel at its rest coordinate (the rest coordinate of the wheel is determined by the value <see cref="JointSpring.targetPosition">WheelCollider.suspensionSpring.targetPosition</see>).  This parameter simulates the effective roll center of the suspension geometry.  For a standard family car the value of forceAppPointDistance should be tuned to place the application point approximately 0.3m below the rigid body center of mass.  Moving the application point downwards introduces more roll when cornering, while moving it upwards results in less roll when cornering. The force application point is typically below the rigid body center of mass.
        ///
        ///Please note that having this parameter equal to zero could be undesirable as it contributes to simulation instability in certain configurations. Once you observe your vehicle failing to go asleep resting on flat surface, exhibiting jittering behavior or drifting along the surface when no user input is applied, check the forceAppPointDistance values. In the editor, when a WheelCollider game object is selected, there is a green spherical gizmo displayed to show where the force application point is at the moment. Try increasing forceAppPointDistance value gradually, until you're satisfied with the result.</remarks>
        public extern float forceAppPointDistance {get; set; }
        ///<summary>The mass of the wheel, expressed in kilograms. Must be larger than zero. Typical values would be in range (20,80).</summary>
        public extern float mass {get; set; }
        ///<summary>The damping rate of the wheel. Must be larger than zero.</summary>
        public extern float wheelDampingRate {get; set; }
        ///<summary>Properties of tire friction in the direction the wheel is pointing in.</summary>
        public extern WheelFrictionCurve forwardFriction {get; set; }
        ///<summary>Properties of tire friction in the sideways direction.</summary>
        public extern WheelFrictionCurve sidewaysFriction {get; set; }
        ///<summary>Motor torque on the wheel axle expressed in Newton metres. Positive or negative depending on direction.</summary>
        ///<remarks>To simulate brakes, do not use negative motor torque - use <see cref="brakeTorque" /> instead.</remarks>
        public extern float motorTorque {get; set; }
        ///<summary>Brake torque expressed in Newton metres.</summary>
        ///<remarks>Must be positive.</remarks>
        public extern float brakeTorque {get; set; }
        ///<summary>Steering angle in degrees, always around the local y-axis.</summary>
        ///<remarks>At high velocities use only small steer angles; a few degrees should suffice.</remarks>
        public extern float steerAngle {get; set; }
        ///<summary>Indicates whether the wheel currently collides with something (RO).</summary>
        public extern bool isGrounded {[NativeName("IsGrounded")] get; }
        ///<summary>Current wheel axle rotation speed, in rotations per minute (RO).</summary>
        public extern float rpm { get; }
        ///<summary>The mass supported by this WheelCollider.</summary>
        ///<remarks>Vehicle simulation uses the sprung mass model that assumes each wheel supports a particular portion of the vehicle's total mass at rest. By default, the sprung mass distribution is computed automatically based on the positions of the wheels relative to the vehicle's center of mass. However, it's also possible to set the masses explicitly. In this case, the whole vehicle is marked as having an explicit mass distribution and no sprung masses will ever be computed for it until the explicit flag is reset by calling <see cref="WheelCollider.ResetSprungMasses" />. Note that the sum of all the sprung masses should be equivalent to the total mass of the vehicle. Because of that, adjusting a wheel's sprung mass will naturally require updating the sprung masses for the other wheels of the vehicle in order to match the vehicle's mass.</remarks>
        public extern float sprungMass { get; set; }
        ///<summary>Rotation speed of the wheel, measured in degrees per second.</summary>
        public extern float rotationSpeed { get; set; }
        ///<summary>Reset the sprung masses of the vehicle.</summary>
        ///<remarks>Recomputes the sprung masses of all wheels of the vehicle this wheel belongs to. In addition, it clears the internal explicit sprung mass distribution flag if that was raised before by calling <see cref="WheelCollider.sprungMass" />. Note that because this function works with the vehicle itself but not just one wheel, it's enough to call it once for a vehicle, invoked with any wheel.</remarks>
        public extern void ResetSprungMasses();
        ///<summary>Configure vehicle sub-stepping parameters.</summary>
        ///<remarks>Every time a fixed update happens, the vehicle simulation splits this fixed delta time into smaller sub-steps and calculates suspension and tire forces per each smaller delta. Then, it would sum up all resulting forces and torques, integrate them, and apply to the vehicle's body.
        ///
        ///Using this function you can customize how many sub-steps will be performed by the simulation above and below the speed threshold.
        ///
        ///It's enough to call this function only once per each vehicle, as it actually sets parameters to the vehicle but not to a wheel.</remarks>
        ///<param name="speedThreshold">The speed threshold of the sub-stepping algorithm.</param>
        ///<param name="stepsBelowThreshold">Amount of simulation sub-steps when vehicle's speed is below speedThreshold.</param>
        ///<param name="stepsAboveThreshold">Amount of simulation sub-steps when vehicle's speed is above speedThreshold.</param>
        public extern void ConfigureVehicleSubsteps(float speedThreshold, int stepsBelowThreshold, int stepsAboveThreshold);
        ///<summary>Gets the world space pose of the wheel accounting for ground contact, suspension limits, steer angle, and rotation angle (angles in degrees).</summary>
        ///<param name="pos">Position of the wheel in world space.</param>
        ///<param name="quat">Rotation of the wheel in world space.</param>
        public extern void GetWorldPose(out Vector3 pos, out Quaternion quat);
        ///<summary>Gets ground collision data for the wheel.</summary>
        ///<remarks>If the wheel collides with something, returns <c>true</c> and fills the <c>hit</c> structure. If the wheel
        ///is not colliding, returns <c>false</c> and leaves <c>hit</c> structure unchanged.
        ///
        ///The reported hit is always the closest one. Because the tire friction model does not automatically
        ///respond to other <see cref="PhysicsMaterial" />s, any simulation of different ground materials must be done
        ///manually by adjusting <see cref="forwardFriction" /> and <see cref="sidewaysFriction" /> based on collider's
        ///material returned here.</remarks>
        public extern bool GetGroundHit(out WheelHit hit);
        extern internal bool isSupported { get; }
    }
}
