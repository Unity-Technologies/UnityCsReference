// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Internal;

namespace UnityEngine
{
    ///<summary>Use these flags to constrain motion of Rigidbodies.</summary>
    ///<example>
    ///  <code><![CDATA[
    /// //This example shows how RigidbodyConstraints is used to freeze the position and rotation of a Rigidbody in the z axis at start-up.
    /// //It also shows what happens when these constraints are removed, when you press the space key
    /// //Attach this to a GameObject with a Rigidbody to see it in action
    ///
    ///using UnityEngine;
    ///using UnityEngine.InputSystem;
    ///
    ///public class RigidBodyConstraitsExample : MonoBehaviour
    ///{
    ///    Rigidbody m_Rigidbody;
    ///    Vector3 m_ZAxis;
    ///
    ///    void Start()
    ///    {
    ///        m_Rigidbody = GetComponent<Rigidbody>();
    ///        //This locks the RigidBody so that it does not move or rotate in the z axis (can be seen in Inspector).
    ///        m_Rigidbody.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationZ;
    ///        //Set up vector for moving the Rigidbody in the z axis
    ///        m_ZAxis = new Vector3(0, 0, 5);
    ///    }
    ///
    ///    void Update()
    ///    {
    ///        //Press space to remove the constraints on the RigidBody
    ///        if (Keyboard.current.spaceKey.wasPressedThisFrame)
    ///        {
    ///            //Remove all constraints
    ///            m_Rigidbody.constraints = RigidbodyConstraints.None;
    ///        }
    ///
    ///        //Press the right arrow key to move positively in the z axis if the constraints are removed
    ///        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
    ///        {
    ///            //If the constraints are removed, the Rigidbody moves along the z axis
    ///            //If the constraints are there, no movement occurs
    ///            m_Rigidbody.velocity = m_ZAxis;
    ///        }
    ///
    ///        //Press the left arrow key to move negatively in the z axis if the constraints are removed
    ///        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
    ///        {
    ///            m_Rigidbody.velocity = -m_ZAxis;
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<seealso cref="Rigidbody.constraints" />
    public enum RigidbodyConstraints
    {
        ///<summary>No constraints.</summary>
        None = 0,
        ///<summary>Freeze motion along the X-axis. Limits motion to the YZ plane only.</summary>
        FreezePositionX = 1 << 1,
        ///<summary>Freeze motion along the Y-axis. Limits motion to the XZ plane only.</summary>
        FreezePositionY = 1 << 2,
        ///<summary>Freeze motion along the Z-axis. Limits motion to the XY plane only.</summary>
        FreezePositionZ = 1 << 3,
        ///<summary>Freeze rotation along the X-axis.</summary>
        FreezeRotationX = 1 << 4,
        ///<summary>Freeze rotation along the Y-axis.</summary>
        FreezeRotationY = 1 << 5,
        ///<summary>Freeze rotation along the Z-axis.</summary>
        FreezeRotationZ = 1 << 6,
        ///<summary>Freeze motion along all axes. Equivalent of RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezePositionZ.</summary>
        FreezePosition = FreezePositionX | FreezePositionY | FreezePositionZ,
        ///<summary>Freeze rotation along all axes. Equivalent of RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ.</summary>
        FreezeRotation = FreezeRotationX | FreezeRotationY | FreezeRotationZ,
        ///<summary>Freeze rotation and motion along all axes. Equivalent of RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation.</summary>
        FreezeAll = FreezePosition | FreezeRotation
    }

    ///<summary>
    ///  <see cref="Rigidbody" /> interpolation mode.</summary>
    ///<remarks>Interpolation calculates the pose of a Rigidbody in frames that fall between physics timestep updates, to reduce the appearance of visible jitter. It is particularly useful for player character GameObjects, and any other GameObject that the camera follows.
    ///By default, interpolation is disabled. When interpolation or extrapolation is enabled, the physics system takes control of the Rigidbody's transform. For this reason, you should follow any direct (non-physics) change to the transform with a <see cref="Physics.SyncTransforms" /> call. Otherwise, Unity ignores any transform change that does not originate from the physics system.
    ///
    ///For the main characters or vehicles that are followed by the camera it is recommended to
    ///use interpolation. For any other rigidbodies it is recommended not to use interpolation.</remarks>
    ///<seealso cref="Rigidbody.interpolation" />
    public enum RigidbodyInterpolation
    {
        ///<summary>No Interpolation.</summary>
        ///<seealso cref="Rigidbody.interpolation" />
        None = 0,
        ///<summary>Interpolation will always lag a little bit behind but can be smoother than extrapolation.</summary>
        ///<seealso cref="Rigidbody.interpolation" />
        Interpolate = 1,
        ///<summary>Extrapolation will predict the position of the rigidbody based on the current velocity.</summary>
        ///<remarks>If you have fast moving objects this can lead to rigidbodies passing
        ///through colliders for one frame and then snapping back.</remarks>
        ///<seealso cref="Rigidbody.interpolation" />
        Extrapolate = 2
    }

    ///<summary>Control of an object's position through physics simulation.</summary>
    ///<remarks>Adding a Rigidbody component to an object will put its motion under the control of Unity's physics engine. Even without adding any code, a Rigidbody object will be pulled downward by gravity and will react to collisions with incoming objects if the right <see cref="Collider" /> component is also present.
    ///
    ///The Rigidbody also has a scripting API that lets you apply forces to the object and control it in a physically realistic way. For example, a car's behaviour can be specified in terms of the forces applied by the wheels. Given this information, the physics engine can handle most other aspects of the car's motion, so it will accelerate realistically and respond correctly to collisions.
    ///
    ///In a script, the <see cref="M:UnityEngine.MonoBehaviour.FixedUpdate">FixedUpdate</see> function is recommended as the place to apply forces and change Rigidbody settings (as opposed to <see cref="M:UnityEngine.MonoBehaviour.Update">Update</see>, which is used for most other frame update tasks). The reason for this is that physics updates are carried out in measured time steps that don't coincide with the frame update. FixedUpdate is called immediately before each physics update and so any changes made there will be processed directly.
    ///
    ///A common problem when starting out with Rigidbodies is that the game physics appears to run in "slow motion". This is actually due to the scale used for your models. The default gravity settings assume that one world unit corresponds to one metre of distance. With non-physical games, it doesn't make much difference if your models are all 100 units long but when using physics, they will be treated as very large objects. If a large scale is used for objects that are supposed to be small, they will appear to fall very slowly - the physics engine thinks they are very large objects falling over very large distances. With this in mind, be sure to keep your objects more or less at their scale in real life (so a car should be about 4 units = 4 metres, for example).</remarks>
    [RequireComponent(typeof(Transform))]
    [global::UnityEngine.NativeClass("Rigidbody", PersistentTypeId = 54)]
    [NativeHeader("Modules/Physics/Rigidbody.h")]
    public partial class Rigidbody : Component
    {
        internal Rigidbody(global::UnityEngine.EntityId id) : base(id) {}
        public Rigidbody() {}
        ///<summary>The linear velocity vector of the rigidbody. It represents the rate of change of Rigidbody position.</summary>
        ///<remarks>
        ///  <para>In most cases you should not modify the velocity directly, as this can result in unrealistic behaviour - use AddForce instead
        ///Do not set the linear velocity of an object every physics step, this will lead to unrealistic physics simulation.
        ///A typical usage is where you would change the velocity is when jumping in a first person shooter, because you want an immediate change in velocity.
        ///
        ///**Note:** The linearVelocity is a world-space property.</para>
        ///  <para>**Note:** A velocity in Unity is units per second.  The units are often thought of as metres but could be millimetres or light years.  Unity velocity also has the speed in X, Y, and Z defining the direction. Additionally, setting the linear velocity of a kinematic rigidbody is not allowed and will have no effect.</para>
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.InputSystem;
        ///
        /// // The velocity along the y axis is 10 units per second.  If the GameObject starts at (0,0,0) then
        /// // it will reach (0,100,0) units after 10 seconds.
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rigidbody rb;
        ///
        ///    private float time = 0.0f;
        ///    private bool isMoving = false;
        ///    private bool isJumpPressed = false;
        ///
        ///
        ///    void Start()
        ///    {
        ///        rb = GetComponent<Rigidbody>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        isJumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
        ///    }
        ///
        ///    void FixedUpdate()
        ///    {
        ///        if (isJumpPressed)
        ///        {
        ///            // the cube moves up the y axis at a rate of 10 units per second
        ///            rb.linearVelocity = new Vector3(0, 10, 0);
        ///            isMoving = true;
        ///            Debug.Log("jump");
        ///        }
        ///
        ///        if (isMoving)
        ///        {
        ///            // when the cube has moved for 10 seconds, report its position
        ///            time = time + Time.fixedDeltaTime;
        ///            if (time > 10.0f)
        ///            {
        ///                Debug.Log(gameObject.transform.position.y + " : " + time);
        ///                time = 0.0f;
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public Vector3 linearVelocity { get; set; }
        ///<summary>The angular velocity vector of the rigidbody measured in radians per second.</summary>
        ///<remarks>In most cases you should not modify it directly, as this can result in unrealistic behaviour. Note that if the Rigidbody has rotational constraints set, the corresponding angular velocity components are set to zero in the mass space (ie relative to the inertia tensor rotation) at the time of the call. Additionally, setting the angular velocity of a kinematic rigidbody is not allowed and will have no effect.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.InputSystem;
        ///
        /// // The angular velocity around the y-axis is 2 radians per second.
        /// // If the GameObject starts facing forward, it will complete about 1 full rotation every 3.14 seconds.
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rigidbody rb;
        ///    public float spinSpeed = 2f;
        ///
        ///    void Start()
        ///    {
        ///        rb = GetComponent<Rigidbody>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        ///        {
        ///            // Start spinning around y-axis
        ///            rb.angularVelocity = new Vector3(0, spinSpeed, 0);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public Vector3 angularVelocity { get; set; }
        ///<summary>The linear damping of the Rigidbody linear velocity.</summary>
        ///<remarks>linearDamping can be used to slow down an object.
        ///                
        ///                Zero indicates that no damping should be used whereas higher values increase the damping, effectively slowing down the linear motion faster.
        ///                
        ///                **Note:** The following formula is how the linear damping is applied: <c>linearVelocity *= ( 1 - linearDamping * dt )</c></remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.InputSystem;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rigidbody rb;
        ///
        ///    void Start()
        ///    {
        ///        rb = GetComponent<Rigidbody>();
        ///    }
        ///
        ///    void OpenParachute()
        ///    {
        ///        rb.linearDamping = 20;
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        if (Keyboard.current.spaceKey.isPressed)
        ///            OpenParachute();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Rigidbody.angularDamping" />
        extern public float linearDamping { get; set; }
        ///<summary>The angular damping of the object.</summary>
        ///<remarks>Angular damping can be used to slow down the rotation of an object.
        ///The higher the damping the more the rotation slows down.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.InputSystem;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rigidbody rb;
        ///
        ///    void Start()
        ///    {
        ///        rb = GetComponent<Rigidbody>();
        ///    }
        ///
        ///    // Get a wild spin under control when the user
        ///    // presses the spacebar.
        ///    void Update()
        ///    {
        ///        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        ///            rb.angularDamping = 0.8F;
        ///        else
        ///            rb.angularDamping = 0;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float angularDamping { get; set; }
        ///<summary>The mass of the rigidbody.</summary>
        ///<remarks>Different Rigidbodies with large differences in mass can make the physics simulation unstable.
        ///
        ///Higher mass objects push lower mass objects more when colliding. Think of a big truck, hitting a small car.
        ///
        ///A common mistake is to assume that heavy objects fall faster than light ones.
        ///This is not true as the speed is dependent on gravity and damping.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Expose mass to allow adjustment from
        /// // the inspector.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float mass;
        ///    public Rigidbody rb;
        ///
        ///    void Start()
        ///    {
        ///        rb = GetComponent<Rigidbody>();
        ///        rb.mass = mass;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float mass { get; set; }
        ///<summary>Controls whether gravity affects this rigidbody.</summary>
        ///<remarks>If set to false the rigidbody will behave as in outer space.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Collider coll;
        ///
        ///    void Start()
        ///    {
        ///        coll = GetComponent<Collider>();
        ///        coll.isTrigger = true;
        ///    }
        ///
        ///    // Disables gravity on all rigidbodies entering this collider.
        ///    void OnTriggerEnter(Collider other)
        ///    {
        ///        if (other.attachedRigidbody)
        ///            other.attachedRigidbody.useGravity = false;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public bool useGravity { get; set; }
        ///<summary>Maximum velocity of a rigidbody when moving out of penetrating state.</summary>
        ///<remarks>Use this property when you want to make your bodies move out of colliding state in a more smooth way than by default.</remarks>
        extern public float maxDepenetrationVelocity { get; set; }
        ///<summary>Controls whether physics affects the rigidbody.</summary>
        ///<remarks>If isKinematic is enabled, Forces, collisions or joints will not affect the rigidbody anymore.
        ///The rigidbody will be under full control of animation or script control by changing transform.position.
        ///Kinematic bodies also affect the motion of other rigidbodies through collisions or joints.
        ///Eg. can connect a kinematic rigidbody to a normal rigidbody with a joint
        ///and the rigidbody will be constrained with the motion of the kinematic body.
        ///Kinematic rigidbodies are also particularly useful for making characters which are normally driven by an animation,
        ///but on certain events can be quickly turned into a ragdoll by setting isKinematic to false.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rigidbody rb;
        ///
        ///    void Start()
        ///    {
        ///        rb = GetComponent<Rigidbody>();
        ///    }
        ///
        ///    // Let the rigidbody take control and detect collisions.
        ///    void EnableRagdoll()
        ///    {
        ///        rb.isKinematic = false;
        ///        rb.detectCollisions = true;
        ///    }
        ///
        ///    // Let animation control the rigidbody and ignore collisions.
        ///    void DisableRagdoll()
        ///    {
        ///        rb.isKinematic = true;
        ///        rb.detectCollisions = false;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public bool isKinematic { get; set; }
        ///<summary>Controls whether physics will change the rotation of the object.</summary>
        ///<remarks>If freezeRotation is enabled, the rotation is not modified by the physics simulation.
        ///This is useful for creating first person shooters,
        ///because the player needs full control of the rotation using the mouse.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    Rigidbody m_Rigidbody;
        ///
        ///    private void Start()
        ///    {
        ///        //Fetch the Rigidbody from the GameObject with this script attached
        ///        m_Rigidbody = GetComponent<Rigidbody>();
        ///        //Stop the Rigidbody from rotating
        ///        m_Rigidbody.freezeRotation = true;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Rigidbody.constraints" />
        public bool freezeRotation
        {
            get => constraints.HasFlag(RigidbodyConstraints.FreezeRotation);
            set
            {
                if (value)
                    constraints |= RigidbodyConstraints.FreezeRotation;
                else
                    constraints &= RigidbodyConstraints.FreezePosition;
            }
        }
        ///<summary>Controls which degrees of freedom are allowed for the simulation of this Rigidbody.</summary>
        ///<remarks>By default this is set to <see cref="RigidbodyConstraints.None" />, allowing rotation and movement along all axes.
        ///In some cases, you may want to constrain a <see cref="Rigidbody" /> to only move or rotate along some axes, for
        ///example when developing 2D games. You can use the bitwise OR operator to combine multiple
        ///constraints.
        ///
        ///Note that position constraints are applied in World space, and rotation constraints are applied in the inertia space (relative to <see cref="Rigidbody.inertiaTensorRotation" />).</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// //Attach this script to a GameObject.
        /// //Attach a Rigidbody to the GameObject by clicking the GameObject in the Hierarchy and
        /// //clicking the __Add Component__ button. Search for Rigidbody in the field and select
        /// //it when shown.
        ///
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    Rigidbody m_Rigidbody;
        ///
        ///    void Start()
        ///    {
        ///        m_Rigidbody = GetComponent<Rigidbody>();
        ///        //This locks the RigidBody so that it does not move or rotate in the Z axis.
        ///        m_Rigidbody.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationZ;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public RigidbodyConstraints constraints { get; set; }
        ///<summary>The Rigidbody's collision detection mode.</summary>
        ///<remarks>Use this to set up a Rigidbody for continuous collision detection. You can use continuous collision detection to 
        ///prevent fast moving objects from passing through other objects without detecting collisions. 
        ///
        ///For best results, set this value to <see cref="CollisionDetectionMode.ContinuousDynamic" /> for fast moving objects. For other objects which these 
        ///need to collide with, set it to <see cref="CollisionDetectionMode.Continuous" />. These two options impact physics performance. Alternatively, 
        ///you can use <see cref="CollisionDetectionMode.ContinuousSpeculative" />, which has less of an impact on performance and can also be used on kinematic 
        ///objects. If you don't have issues with collisions of fast objects, leave it set to the default value of <see cref="CollisionDetectionMode.Discrete" />.
        ///
        ///Continuous Collision Detection is only supported for Rigidbodies with Sphere-, Capsule- or BoxColliders.</remarks>
        ///<seealso cref="CollisionDetectionMode" />
        extern public CollisionDetectionMode collisionDetectionMode { get; set; }
        ///<summary>Whether or not to calculate the center of mass automatically.</summary>
        ///<remarks>If enabled, the center of mass is calculated automatically based on the settings of attached <see cref="Collider" />s. If no colliders are present, the center of mass has a default value of Vector3(0,0,0).</remarks>
        extern public bool automaticCenterOfMass { get; set; }
        ///<summary>The center of mass relative to the transform's origin.</summary>
        ///<remarks>If you don't set the center of mass from a script it will be calculated automatically from all colliders attached to the rigidbody. After a custom center of mass set, it will no longer be recomputed automatically on modifications such as adding or removing colliders, translating them, scaling etc. To revert back to the automatically computed center of mass,  use <see cref="Rigidbody.ResetCenterOfMass" />.
        ///
        ///Setting the center of mass is often useful when simulating cars to make them more stable.
        ///A car with a lower center of mass is less likely to topple over.
        ///
        ///Note: <c>centerOfMass</c> is relative to the transform's position and rotation, but will not reflect the transform's scale!</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Expose center of mass to allow it to be set from
        /// // the inspector.
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Vector3 com;
        ///    public Rigidbody rb;
        ///
        ///    void Start()
        ///    {
        ///        rb = GetComponent<Rigidbody>();
        ///        rb.centerOfMass = com;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public Vector3 centerOfMass { get; set; }
        ///<summary>The center of mass of the rigidbody in world space (RO).</summary>
        extern public Vector3 worldCenterOfMass { get; }
        ///<summary>Whether or not to calculate the inertia tensor automatically.</summary>
        ///<remarks>If enabled, the inertia tensor is calculated automatically based on the settings of attached <see cref="Collider" />s. If no colliders are present, the tensor has a default value of Vector3(1,1,1).</remarks>
        extern public bool automaticInertiaTensor { get; set; }
        ///<summary>The rotation of the inertia tensor.</summary>
        ///<remarks>If you don't set inertia tensor rotation from a script it will be calculated automatically from all colliders attached to the rigidbody.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Resets the inertia tensor to be the coordinate system of the transform
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void ResetTensor()
        ///    {
        ///        GetComponent<Rigidbody>().inertiaTensorRotation = Quaternion.identity;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public Quaternion inertiaTensorRotation { get; set; }
        ///<summary>The inertia tensor of this body, defined as a diagonal matrix in a reference frame positioned at this body's center of mass and rotated by <see cref="Rigidbody.inertiaTensorRotation" />.</summary>
        ///<remarks>Inertia tensor is a rotational analog of mass: the larger the inertia component about a particular axis is, the more torque that is required to achieve the same angular acceleration about that axis.
        ///
        ///Zero is not a valid inertia tensor component. Therefore, the physics system interprets zeros as infinite values instead. So, for example, a body with (0, 1, 1) inertia tensor is impossible to rotate around X.
        ///
        ///Note that the rotational Constraints <see cref="RigidbodyConstraints" /> of Rigidbody are actually implemented by setting the inertia tensor components about the locked degrees of freedom to zero.
        ///
        ///If you don't set the inertia tensor from a script, it is calculated automatically from all colliders attached to the Rigidbody. To reset the inertia tensor to the automatically computed value, call <see cref="Rigidbody.ResetInertiaTensor" />.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Expose tensor of inertia to allow adjustment from
        /// // the inspector.
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Vector3 tensor;
        ///    public Rigidbody rb;
        ///    void Start()
        ///    {
        ///        rb = GetComponent<Rigidbody>();
        ///        rb.inertiaTensor = tensor;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public Vector3 inertiaTensor { get; set; }
        extern internal Matrix4x4 worldInertiaTensorMatrix { get; }
        ///<summary>Should collision detection be enabled? (By default always enabled).</summary>
        ///<remarks>Disabling collision detections is useful when you have a ragdoll which is setup to be kinematic and you want to avoid
        ///heavy collision detection calculations on that rigidbody.
        ///detectCollisions is not serialized. This means it doesn't show up in the Inspector
        ///and when Instantiating the rigidbody or saving it in a Scene, it will not be saved.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rigidbody rb;
        ///
        ///    void Start()
        ///    {
        ///        rb = GetComponent<Rigidbody>();
        ///    }
        ///
        ///    // Let the rigidbody take control and detect collisions.
        ///    void EnableRagdoll()
        ///    {
        ///        rb.isKinematic = false;
        ///        rb.detectCollisions = true;
        ///    }
        ///
        ///    // Let animation control the rigidbody and ignore collisions.
        ///    void DisableRagdoll()
        ///    {
        ///        rb.isKinematic = true;
        ///        rb.detectCollisions = false;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public bool detectCollisions { get; set; }
        ///<summary>The position of the rigidbody.</summary>
        ///<remarks>
        ///  <see cref="Rigidbody.position" /> allows you to get and set the position of a Rigidbody using the physics engine. If you change the position of a Rigidbody using <see cref="Rigidbody.position" />, the transform will be updated after the next physics simulation step. This is faster than updating the position using <see cref="Transform.position" />, as the latter will cause all attached Colliders to recalculate their positions relative to the Rigidbody.
        ///
        ///If you want to continuously move a rigidbody use <see cref="Rigidbody.MovePosition" /> instead, which takes interpolation into account.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        GetComponent<Rigidbody>().position = Vector3.zero;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public Vector3 position { get; set; }
        ///<summary>The rotation of the Rigidbody.</summary>
        ///<remarks>Use Rigidbody.rotation to get and set the rotation of a Rigidbody using the physics engine.
        ///
        ///Changing the rotation of a Rigidbody using Rigidbody.rotation updates the Transform after the next physics simulation step. This is faster than updating the rotation using Transform.rotation, as Transform.rotation causes all attached Colliders to recalculate their rotation relative to the Rigidbody, whereas Rigidbody.rotation sets the values directly to the physics system.
        ///
        ///If you want to continuously rotate a rigidbody use <see cref="MoveRotation" /> instead, which takes interpolation into account.
        ///
        ///**Note:** <see cref="rotation" /> is world-space.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        // Set a custom rotation: 45 degrees around the Y-axis
        ///        GetComponent<Rigidbody>().rotation = Quaternion.Euler(0, 45, 0);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public Quaternion rotation { get; set; }
        ///<summary>Interpolation provides a way to manage the appearance of jitter in the movement of your Rigidbody GameObjects at run time.</summary>
        ///<remarks>Interpolation calculates the pose of a Rigidbody in frames that fall between physics timestep updates, to reduce the appearance of visible jitter. It is particularly useful for player character GameObjects, and any other GameObject that the camera follows.
        ///By default, interpolation is disabled. When interpolation or extrapolation is enabled, the physics system takes control of the Rigidbody's transform. For this reason, you should follow any direct (non-physics) change to the transform with a <see cref="Physics.SyncTransforms" /> call. Otherwise, Unity ignores any transform change that does not originate from the physics system.
        ///Physics simulation runs at discrete timesteps, while graphics are rendered at variable frame rates.
        ///This can lead to visual jitter on some GameObjects, because the physics and graphics updates are not synchronized.
        ///The visual effect is particularly noticeable on GameObjects that the camera follows (such as player characters and vehicles).
        ///It is recommended to turn on interpolation for the main character but disable it for everything else.</remarks>
        extern public RigidbodyInterpolation interpolation { get; set; }
        ///<summary>The solverIterations determines how accurately Rigidbody joints and collision contacts are resolved. Overrides <see cref="Physics.defaultSolverIterations" />. Must be positive.</summary>
        ///<remarks>If you are having trouble with connected Rigidbodies oscillating and behaving erratically setting
        ///a higher solver iteration count may improve their stability (but is slower).</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Rigidbody rb = GetComponent<Rigidbody>();
        ///        rb.solverIterations = 30;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Rigidbody.solverVelocityIterations" />
        extern public int solverIterations { get; set; }
        ///<summary>The mass-normalized energy threshold, below which objects start going to sleep.</summary>
        extern public float sleepThreshold { get; set; }
        ///<summary>The maximum angular velocity of the rigidbody measured in radians per second. (Default 7) range { 0, infinity }.</summary>
        ///<remarks>The angular velocity of rigidbodies is clamped to maxAngularVelocity to avoid numerical instability with fast rotating bodies. The maxAngularVelocity is applied to the body before the simulation step. This means that after the simulation frame, the angular velocity might exceed the set maximum. You can override this value per Rigidbody to enable faster rotations on objects such as wheels.</remarks>
        extern public float maxAngularVelocity { get; set; }
        ///<summary>The maximum linear velocity of the rigidbody measured in meters per second.</summary>
        ///<remarks>The linear velocity of Rigidbody components is clamped to maxLinearVelocity to avoid numerical instability with fast moving bodies.
        ///The maxLinearVelocity is applied to the body before the simulation step. This means that after the simulation frame, the linear velocity might exceed the set maximum. You can override this value per Rigidbody.</remarks>
        extern public float maxLinearVelocity { get; set; }
        ///<summary>Moves the kinematic <see cref="Rigidbody" /> towards <c>position</c>.</summary>
        ///<remarks>
        ///  <see cref="Rigidbody.MovePosition" /> moves a Rigidbody and complies with the interpolation settings. When Rigidbody interpolation is enabled, <see cref="Rigidbody.MovePosition" /> creates a smooth transition between frames. Unity moves a <see cref="Rigidbody" /> in each <c>FixedUpdate</c> call. The <c>position</c> occurs in world space. To teleport a <see cref="Rigidbody" /> from one position to another, use <see cref="Rigidbody.position" /> instead of <see cref="MovePosition" />.</remarks>
        ///<param name="position">Provides the new position for the <see cref="Rigidbody" /> object.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.InputSystem;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    private Rigidbody rb;
        ///    private float moveSpeed = 5f;
        ///
        ///    [Header("Input Actions")]
        ///    public InputActionReference moveAction;
        ///
        ///    private void OnEnable()
        ///    {
        ///        moveAction.action.Enable();
        ///    }
        ///
        ///    private void OnDisable()
        ///    {
        ///        moveAction.action.Disable();
        ///    }
        ///    
        ///    void Awake()
        ///    {
        ///        //Fetch the Rigidbody from the GameObject with this script attached
        ///        rb = GetComponent<Rigidbody>();
        ///    }
        ///
        ///    void FixedUpdate()
        ///    {
        ///        //Store user input as a movement vector
        ///        Vector3 m_Input = new Vector3(moveAction.action.ReadValue<Vector2>().x, 0, moveAction.action.ReadValue<Vector2>().y);
        ///
        ///        //Apply the movement vector to the current position, which is
        ///        //multiplied by deltaTime and speed for a smooth MovePosition
        ///        rb.MovePosition(transform.position + m_Input * Time.fixedDeltaTime * moveSpeed);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public void MovePosition(Vector3 position);
        ///<summary>Rotates the rigidbody to <c>rotation</c>.</summary>
        ///<remarks>Use <see cref="Rigidbody.MoveRotation" /> to rotate a <see cref="Rigidbody" />, complying with the Rigidbody's interpolation setting.
        ///
        ///If Rigidbody interpolation is enabled on the <see cref="Rigidbody" />, calling <see cref="Rigidbody.MoveRotation" /> will resulting in a smooth transition between the two rotations in any intermediate frames rendered. This should be used if you want to continuously rotate a rigidbody in each <see cref="M:UnityEngine.MonoBehaviour.FixedUpdate" />.
        ///
        ///Set <see cref="Rigidbody.rotation" /> instead, if you want to teleport a rigidbody from one rotation to another, with no intermediate positions being rendered.</remarks>
        ///<param name="rotation">The new rotation for the Rigidbody.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    Rigidbody m_Rigidbody;
        ///    Vector3 m_EulerAngleVelocity;
        ///
        ///    void Start()
        ///    {
        ///        //Fetch the Rigidbody from the GameObject with this script attached
        ///        m_Rigidbody = GetComponent<Rigidbody>();
        ///
        ///        //Set the angular velocity of the Rigidbody (rotating around the Y axis, 100 deg/sec)
        ///        m_EulerAngleVelocity = new Vector3(0, 100, 0);
        ///    }
        ///
        ///    void FixedUpdate()
        ///    {
        ///        Quaternion deltaRotation = Quaternion.Euler(m_EulerAngleVelocity * Time.fixedDeltaTime);
        ///        m_Rigidbody.MoveRotation(m_Rigidbody.rotation * deltaRotation);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public void MoveRotation(Quaternion rotation);
        ///<summary>Moves the Rigidbody to <c>position</c> and rotates the Rigidbody to <c>rotation</c>.</summary>
        ///<remarks>Use <see cref="Rigidbody.Move" /> to move and rotate a <see cref="Rigidbody" />, complying with the Rigidbody's interpolation setting.
        ///
        ///If you enable Rigidbody interpolation on the <see cref="Rigidbody" />, calling <see cref="Rigidbody.Move" /> results in a smooth transition between the two positions and rotations in any intermediate frames that Unity renders. Use <see cref="Rigidbody.Move" /> if you want to continuously move and rotate a Rigidbody in each <see cref="M:UnityEngine.MonoBehaviour.FixedUpdate" />.
        ///
        ///To teleport a Rigidbody from one position and rotation to another position and rotation, without Unity rendering intermediate positions, set <see cref="Rigidbody.position" /> and <see cref="Rigidbody.rotation" /> instead.</remarks>
        ///<param name="position">The new position for the Rigidbody.</param>
        ///<param name="rotation">The new rotation for the Rigidbody.</param>
        extern public void Move(Vector3 position, Quaternion rotation);
        ///<summary>Forces a rigidbody to sleep until woken up.</summary>
        ///<remarks>A Rigidbody can be put to sleep only if it is not in contact with an awake rigidbody, and if it does not come in contact with any rigidbody during the next simulation step. 
        ///                    A common use is to call this from Awake in order to make a rigidbody sleep at startup.
        ///See the [Rigidbodies Overview](xref:RigidbodiesOverview) in the manual for more information about Rigidbody sleeping.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    private float fallTime;
        ///    private Rigidbody rbGO;
        ///    private bool sleeping;
        ///
        ///    void Start()
        ///    {
        ///        rbGO = gameObject.AddComponent<Rigidbody>();
        ///        rbGO.mass = 10.0f;
        ///        Physics.gravity = new Vector3(0, -2.0f, 0);
        ///        sleeping = false;
        ///        fallTime = 0.0f;
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        if (fallTime > 1.0f)
        ///        {
        ///            if (sleeping)
        ///            {
        ///                rbGO.WakeUp();
        ///                Debug.Log("wakeup");
        ///            }
        ///            else
        ///            {
        ///                rbGO.Sleep();
        ///                Debug.Log("sleep");
        ///            }
        ///
        ///            sleeping = !sleeping;
        ///
        ///            fallTime = 0.0f;
        ///        }
        ///
        ///        fallTime += Time.deltaTime;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public void Sleep();
        ///<summary>Is the rigidbody sleeping?</summary>
        ///<remarks>See the [Physics Overview](xref:PhysicsOverview) in the manual for more information about Rigidbody sleeping.</remarks>
        extern public bool IsSleeping();
        ///<summary>Forces a rigidbody to wake up.</summary>
        ///<remarks>For more information about <see cref="Rigidbody" /> sleeping, refer to the [Rigidbody overview](xref:RigidbodiesOverview).</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    private float fallTime;
        ///    private Rigidbody rbGO;
        ///    private bool sleeping;
        ///
        ///    void Start()
        ///    {
        ///        rbGO = gameObject.AddComponent<Rigidbody>();
        ///        rbGO.mass = 10.0f;
        ///        Physics.gravity = new Vector3(0, -2.0f, 0);
        ///        sleeping = false;
        ///        fallTime = 0.0f;
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        if (fallTime > 1.0f)
        ///        {
        ///            if (sleeping)
        ///            {
        ///                rbGO.WakeUp();
        ///                Debug.Log("wakeup");
        ///            }
        ///            else
        ///            {
        ///                rbGO.Sleep();
        ///                Debug.Log("sleep");
        ///            }
        ///
        ///            sleeping = !sleeping;
        ///
        ///            fallTime = 0.0f;
        ///        }
        ///
        ///        fallTime += Time.deltaTime;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public void WakeUp();
        //controls if rigidbody operations should automatically wake up the body. This causes "WakeUp" to be triggered which will reset any wakeCounter that was provided
        //operations that can wakeup the body: Adding/Removing a shape, changing linear/angular velocity, setting gravity usage, changing constraints, changing mass properties
        extern internal bool autoWake { get; set; }
        //transient value returns 0 if the rigidbody has been disabled. Any value set to this property is only valid until the next WakeUp call issues by the Rigidbody
        extern internal float wakeCounter { get; set; }
        ///<summary>Reset the center of mass of the rigidbody.</summary>
        ///<remarks>Computes the actual center of mass of the rigidbody from all the colliders attached, and stores it. After calling this function, the center of mass will get updated automatically after any modification to the rigidbody.</remarks>
        extern public void ResetCenterOfMass();
        ///<summary>Reset the inertia tensor value and rotation.</summary>
        ///<remarks>Computes the  inertia tensor, and the inertia tensor rotation from the colliders attached to this rigidbody and stores it. After calling this function, the inertia tensor and tensor rotation will be updated automatically after any modification of the rigidbody.</remarks>
        extern public void ResetInertiaTensor();
        ///<summary>The velocity relative to the rigidbody at the point <c>relativePoint</c>.</summary>
        ///<remarks>GetRelativePointVelocity will take the angularVelocity of the rigidbody into account when calculating the velocity.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rigidbody rb;
        ///
        ///    void Start()
        ///    {
        ///        rb = GetComponent<Rigidbody>();
        ///    }
        ///
        ///    // Get the velocity of a wheel, specified by its position in local space.
        ///    // This method assumes that the wheel is a child of the Rigidbody, or that the wheel translates relative to the Rigidbody. 
        ///    Vector3 CalcWheelVelocity(Vector3 localWheelPos)
        ///    {
        ///        return rb.GetRelativePointVelocity(localWheelPos);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public Vector3 GetRelativePointVelocity(Vector3 relativePoint);
        ///<summary>The velocity of the rigidbody at the point <c>worldPoint</c> in global space.</summary>
        ///<remarks>GetPointVelocity will take the angularVelocity of the rigidbody into account when calculating the velocity.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Rigidbody rb;
        ///
        ///    void Start()
        ///    {
        ///        rb = GetComponent<Rigidbody>();
        ///    }
        ///
        ///    // Get the velocity of a wheel, specified by its position in local space.
        ///    Vector3 CalcWheelVelocity(Vector3 localWheelPos)
        ///    {
        ///        return rb.GetPointVelocity(transform.TransformPoint(localWheelPos));
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public Vector3 GetPointVelocity(Vector3 worldPoint);
        ///<summary>The solverVelocityIterations affects how how accurately Rigidbody joints and collision contacts are resolved. Overrides <see cref="Physics.defaultSolverVelocityIterations" />. Must be positive.</summary>
        ///<remarks>Increasing this value will result in higher accuracy of the resulting exit velocity after a Rigidbody bounce.
        ///If you are experiencing issues with jointed Rigidbodies or Ragdolls moving too much after collisions you can try to increase this value.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Rigidbody rb = GetComponent<Rigidbody>();
        ///        rb.solverVelocityIterations = 30;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Rigidbody.solverIterations" />
        extern public int solverVelocityIterations { get; set; }
        ///<summary>Applies the <see cref="position" /> and <see cref="rotation" /> of the Rigidbody to the corresponding <see cref="Transform" /> component.</summary>
        ///<remarks>
        ///  <para>This is more efficient than setting <see cref="Transform.position" /> and <see cref="Transform.rotation" /> manually.
        ///
        ///Note: This method doesn't update the child Transforms. It should be called from the topmost Transform, down the hierarchy.</para>
        ///  <para>Simulate a <see cref="PhysicsScene" /> with a <see cref="Rigidbody" /> and use <see cref="PublishTransform" /> to read the information from the physics system to the <see cref="Transform" /> component.</para>
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class PositionTracker : MonoBehaviour
        ///{
        ///    private PhysicsScene m_SomeScene;
        ///    private Rigidbody m_Rigidbody;
        ///
        ///    public void SimulateTrajectory(float totalTime)
        ///    {
        ///        m_SomeScene.RunSimulationStages(0f, SimulationStage.PrepareSimulation);
        ///
        ///        for (int i = 0; i < totalTime / Time.fixedDeltaTime; i++)
        ///            m_SomeScene.RunSimulationStages(Time.fixedDeltaTime, SimulationStage.RunSimulation);
        ///
        ///        // Transform of the m_Rigidbody is still like it was at the start of the method
        ///        m_Rigidbody.PublishTransform();
        ///        // Transform of the m_Rigidbody is now up to date and can be accessed to see where the body ended up after simulating for "totalTime" seconds
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public void PublishTransform();
        ///<summary>The additional layers that all <see cref="Collider" />s attached to this <see cref="Rigidbody" /> should exclude when deciding if the <see cref="Collider" /> can come into contact with another <see cref="Collider" />.</summary>
        ///<remarks>The Layer Collision Matrix defines which layers can and cannot contact other layers. Use this property to specify additional layers that all <see cref="Collider" />s attached to this <see cref="Rigidbody" /> instance can't contact.
        ///
        ///When deciding which layers can contact each other, the Layer Collision Matrix first includes layers, then excludes layers. If a layer is set to be included and excluded, it is excluded.
        ///
        ///**NOTE**: Layers can be included or excluded differently depending on the settings of each <see cref="Collider" /> instance. As such, there could be a conflicting decision for whether two <see cref="Collider" /> instances can come into contact with each other. To learn how Unity decides this, see <see cref="Collider.layerOverridePriority" />.</remarks>
        ///<seealso cref="Collider.includeLayers" />
        ///<seealso cref="Rigidbody.includeLayers" />
        ///<seealso cref="ArticulationBody.includeLayers" />
        ///<seealso cref="ArticulationBody.excludeLayers" />
        extern public LayerMask excludeLayers { get; set; }
        ///<summary>The additional layers that all <see cref="Collider" />s attached to this <see cref="Rigidbody" /> should include when deciding if the <see cref="Collider" /> can come into contact with another <see cref="Collider" />.</summary>
        ///<remarks>The Layer Collision Matrix defines which layers can contact other layers. Use this property to specify additional layers that all <see cref="Collider" />s attached to this <see cref="Rigidbody" /> instance can contact.
        ///
        ///**NOTE**: Layers can be included or excluded differently depending on the settings of each <see cref="Collider" /> instance. As such, there could be a conflicting decision for whether two <see cref="Collider" /> instances can come into contact with each other. To learn how Unity decides this, see <see cref="Collider.layerOverridePriority" />.</remarks>
        ///<seealso cref="Collider.excludeLayers" />
        ///<seealso cref="Rigidbody.excludeLayers" />
        ///<seealso cref="ArticulationBody.includeLayers" />
        ///<seealso cref="ArticulationBody.excludeLayers" />
        extern public LayerMask includeLayers { get; set; }
        ///<summary>Returns the force that the <see cref="Rigidbody" /> has accumulated before the simulation step.</summary>
        ///<remarks>
        ///  <para>The accumulated force is reset during each physics simulation step.</para>
        ///  <para>In this example, the Rigidbody doesn't move.</para>
        ///</remarks>
        ///<param name="step">The timestep of the next physics simulation.</param>
        ///<returns>Accumulated force expressed in <see cref="ForceMode.Force" />.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class AddForceScript : MonoBehaviour
        ///{
        ///    private Rigidbody rigidbody;
        ///
        ///    void Start()
        ///    {
        ///        rigidbody = GetComponent<Rigidbody>();
        ///        rigidbody.useGravity = false;
        ///    }
        ///
        ///    private void FixedUpdate()
        ///    {
        ///        rigidbody.AddForce(Vector3.up * 10f, ForceMode.Impulse);
        ///        var accumulatedForce = rigidbody.GetAccumulatedForce();
        ///        rigidbody.AddForce(accumulatedForce * -1f, ForceMode.Force);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public Vector3 GetAccumulatedForce([DefaultValue("Time.fixedDeltaTime")] float step);

        [ExcludeFromDocs]
        public Vector3 GetAccumulatedForce()
        {
            return GetAccumulatedForce(Time.fixedDeltaTime);
        }

        ///<summary>Returns the torque that the <see cref="Rigidbody" /> has accumulated before the simulation step.</summary>
        ///<remarks>
        ///  <para>The accumulated torque is reset during each physics simulation step.</para>
        ///  <para>In this example, the angular velocity of the Rigidbody is 0.</para>
        ///</remarks>
        ///<param name="step">The timestep of the next physics simulation.</param>
        ///<returns>Accumulated torque expressed in <see cref="ForceMode.Force" />.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class AddTorqueScript : MonoBehaviour
        ///{
        ///    private Rigidbody rigidbody;
        ///
        ///    void Start()
        ///    {
        ///        rigidbody = GetComponent<Rigidbody>();
        ///        rigidbody.useGravity = false;
        ///    }
        ///
        ///    private void FixedUpdate()
        ///    {
        ///        rigidbody.AddTorque(Vector3.right * 10f, ForceMode.Impulse);
        ///        var accumulatedTorque = rigidbody.GetAccumulatedTorque();
        ///        rigidbody.AddTorque(accumulatedTorque * -1f, ForceMode.Force);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public Vector3 GetAccumulatedTorque([DefaultValue("Time.fixedDeltaTime")] float step);

        [ExcludeFromDocs]
        public Vector3 GetAccumulatedTorque()
        {
            return GetAccumulatedTorque(Time.fixedDeltaTime);
        }

        ///<summary>Clears all pending forces accumulated on this <see cref="Rigidbody" />.</summary>
        ///<remarks>Removes any forces that have been applied with <see cref="AddForce" /> since the last simulation step.
        ///Has no effect on kinematic or inactive rigidbodies.</remarks>
        extern internal void ClearForce();

        ///<summary>Clears all pending torques accumulated on this <see cref="Rigidbody" />.</summary>
        ///<remarks>Removes any torques that have been applied with <see cref="AddTorque" /> since the last simulation step.
        ///Has no effect on kinematic or inactive rigidbodies.</remarks>
        extern internal void ClearTorque();

        ///<summary>Adds a force to the <see cref="Rigidbody" />.</summary>
        ///<remarks>Force is applied continuously along the direction of the <c>force</c> vector. Specifying the <see cref="ForceMode" /><c>mode</c> allows the type of force to be changed to an Acceleration, Impulse or Velocity Change.
        ///
        ///The effects of the forces applied with this function are accumulated at the time of the call. The physics system applies the effects during the next simulation run (either after <see cref="M:UnityEngine.MonoBehaviour.FixedUpdate" />, or when the script explicitly calls the <see cref="Physics.Simulate" /> method).
        ///Because this function has different modes, the physics system only accumulates the resulting velocity change, not the passed force values. Assuming deltaTime (DT) is equal to the simulation step length (<see cref="Time.fixedDeltaTime" />), and mass is equal to the mass of the Rigidbody the force is being applied to, here is how the velocity change is calculated for all the modes:
        ///
        ///* <see cref="ForceMode.Force" />: Interprets the input as force (measured in Newtons), and changes the velocity by the value of force * DT / mass. The effect depends on the simulation step length and the mass of the body.
        ///* <see cref="ForceMode.Acceleration" />: Interprets the parameter as acceleration (measured in meters per second squared), and changes the velocity by the value of force * DT. The effect depends on the simulation step length but doesn't depend on the mass of the body.
        ///* <see cref="ForceMode.Impulse" />: Interprets the parameter as an impulse (measured in newton-seconds), and changes the velocity by the value of force / mass. The effect depends on the mass of the body but doesn't depend on the simulation step length.
        ///* <see cref="ForceMode.VelocityChange" />: Interprets the parameter as a direct velocity change (measured in meters per second), and changes the velocity by the value of force. The effect doesn't depend on the mass of the body or the simulation step length.
        ///
        ///Force can only be applied to an active Rigidbody. If a GameObject is inactive, AddForce has no effect. Also, the Rigidbody cannot be kinematic.
        ///
        ///By default the Rigidbody's state is set to awake once a force is applied, unless the force is <see cref="Vector3.zero" />.
        ///
        ///
        ///
        ///This example applies a forward force to the GameObject's Rigidbody.</remarks>
        ///<param name="force">Force vector in world coordinates.</param>
        ///<param name="mode">Type of force to apply.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.InputSystem;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    Rigidbody m_Rigidbody;
        ///    public float m_Thrust = 20f;
        ///
        ///    void Start()
        ///    {
        ///        //Fetch the Rigidbody from the GameObject with this script attached
        ///        m_Rigidbody = GetComponent<Rigidbody>();
        ///    }
        ///
        ///    void FixedUpdate()
        ///    {
        ///        if (Keyboard.current.spaceKey.isPressed)
        ///        {
        ///            //Apply a force to this Rigidbody in direction of this GameObjects up axis
        ///            m_Rigidbody.AddForce(transform.up * m_Thrust);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AddForceAtPosition" />
        ///<seealso cref="AddRelativeForce" />
        ///<seealso cref="AddTorque" />
        extern public void AddForce(Vector3 force, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddForce(Vector3 force)
        {
            AddForce(force, ForceMode.Force);
        }

        ///<summary>Adds a force to the <see cref="Rigidbody" />.</summary>
        ///<remarks>This example applies an Impulse force along the Z axis to the GameObject's Rigidbody.</remarks>
        ///<param name="x">Size of force along the world x-axis.</param>
        ///<param name="y">Size of force along the world y-axis.</param>
        ///<param name="z">Size of force along the world z-axis.</param>
        ///<param name="mode">Type of force to apply.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    public float thrust = 1.0f;
        ///    public Rigidbody rb;
        ///
        ///    void Start()
        ///    {
        ///        rb = GetComponent<Rigidbody>();
        ///        rb.AddForce(0, 0, thrust, ForceMode.Impulse);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void AddForce(float x, float y, float z, [DefaultValue("ForceMode.Force")] ForceMode mode) { AddForce(new Vector3(x, y, z), mode); }

        [ExcludeFromDocs]
        public void AddForce(float x, float y, float z)
        {
            AddForce(new Vector3(x, y, z), ForceMode.Force);
        }

        ///<summary>Adds a force to the rigidbody relative to its coordinate system.</summary>
        ///<remarks>Force can be applied only to an active rigidbody. If a GameObject is inactive, AddRelativeForce has no effect.
        ///
        ///Wakes up the Rigidbody by default. If the force size is zero then the Rigidbody will not be woken up.
        ///
        ///For more information on how ForceMode affects velocity, see <see cref="Rigidbody.AddForce" />.</remarks>
        ///<param name="force">Force vector in local coordinates.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        /// // Add a thrust force to push an object in its current forward
        /// // direction (to simulate a rocket motor, say).
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float thrust;
        ///    public Rigidbody rb;
        ///    void Start()
        ///    {
        ///        rb = GetComponent<Rigidbody>();
        ///    }
        ///
        ///    void FixedUpdate()
        ///    {
        ///        rb.AddRelativeForce(Vector3.forward * thrust);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AddForce" />
        ///<seealso cref="AddForceAtPosition" />
        ///<seealso cref="AddRelativeTorque" />
        extern public void AddRelativeForce(Vector3 force, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddRelativeForce(Vector3 force)
        {
            AddRelativeForce(force, ForceMode.Force);
        }

        ///<summary>Adds a force to the rigidbody relative to its coordinate system.</summary>
        ///<remarks>Force can be applied only to an active rigidbody. If a GameObject is inactive, AddRelativeForce has no effect.
        ///
        ///Wakes up the Rigidbody by default. If the force size is zero then the Rigidbody will not be woken up.</remarks>
        ///<param name="x">Size of force along the local x-axis.</param>
        ///<param name="y">Size of force along the local y-axis.</param>
        ///<param name="z">Size of force along the local z-axis.</param>
        public void AddRelativeForce(float x, float y, float z, [DefaultValue("ForceMode.Force")] ForceMode mode) { AddRelativeForce(new Vector3(x, y, z), mode); }

        [ExcludeFromDocs]
        public void AddRelativeForce(float x, float y, float z)
        {
            AddRelativeForce(new Vector3(x, y, z), ForceMode.Force);
        }

        ///<summary>Adds a torque to the rigidbody.</summary>
        ///<remarks>Force can be applied only to an active rigidbody. If a GameObject is inactive, AddTorque has no effect.
        ///
        ///The effects of the torques applied with this function are accumulated at the time of the call. The physics system applies the effects during the next simulation run (either after <see cref="M:UnityEngine.MonoBehaviour.FixedUpdate" />, or when the script explicitly calls the <see cref="Physics.Simulate" /> method).
        ///Because this function has different modes, the physics system only accumulates the resulting angular velocity change, not the passed torque values. Assuming deltaTime (DT) is equal to the simulation step length (<see cref="Time.fixedDeltaTime" />), and mass is equal to the mass of the Rigidbody the torque is being applied to, here is how the angular velocity change is calculated for all the modes:
        ///
        ///* ForceMode.Force: Interprets the input as torque (measured in Newton-metres), and changes the angular velocity by the value of torque * DT / inertia. The effect depends on the simulation step length and the mass of the body.
        ///* ForceMode.Acceleration: Interprets the parameter as angular acceleration (measured in radians per second squared), and changes the angular velocity by the value of torque * DT. The effect depends on the simulation step length but does not depend on the mass of the body.
        ///* ForceMode.Impulse: Interprets the parameter as an angular momentum (measured in kilogram-meters-squared per second), and changes the angular velocity by the value of torque / inertia. The effect depends on the mass of the body but doesn't depend on the simulation step length.
        ///* ForceMode.VelocityChange: Interprets the parameter as a direct angular velocity change (measured in radians per second), and changes the angular velocity by the value of torque. The effect doesn't depend on the mass of the body and the simulation step length.
        ///
        ///Wakes up the Rigidbody by default. If the torque size is zero then the Rigidbody will not be woken up.</remarks>
        ///<param name="torque">Torque vector in world coordinates.</param>
        ///<param name="mode">The type of torque to apply.</param>
        ///<example>
        ///  <code><![CDATA[
        /// // Rotate an object around its Y (upward) axis in response to
        /// // left/right controls.
        ///
        ///using UnityEngine;
        ///using UnityEngine.InputSystem;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    private float torque = 4;
        ///    private Rigidbody rb;
        ///                        
        ///    void Awake()
        ///    {
        ///        rb = GetComponent<Rigidbody>();
        ///    }
        ///    
        ///    void FixedUpdate()
        ///    {
        ///        float turnInput = Keyboard.current.spaceKey.isPressed ? 1f : 0f;
        ///
        ///        rb.AddTorque(Vector3.up * torque * turnInput * Time.fixedDeltaTime);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AddRelativeTorque" />
        ///<seealso cref="AddForce" />
        extern public void AddTorque(Vector3 torque, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddTorque(Vector3 torque)
        {
            AddTorque(torque, ForceMode.Force);
        }

        ///<summary>Adds a torque to the rigidbody.</summary>
        ///<remarks>Force can be applied only to an active rigidbody. If a GameObject is inactive, AddTorque has no effect.
        ///
        ///Wakes up the Rigidbody by default. If the torque size is zero then the Rigidbody will not be woken up.</remarks>
        ///<param name="x">Size of torque along the world x-axis.</param>
        ///<param name="y">Size of torque along the world y-axis.</param>
        ///<param name="z">Size of torque along the world z-axis.</param>
        ///<param name="mode">The type of torque to apply.</param>
        public void AddTorque(float x, float y, float z, [DefaultValue("ForceMode.Force")] ForceMode mode) { AddTorque(new Vector3(x, y, z), mode); }

        [ExcludeFromDocs]
        public void AddTorque(float x, float y, float z)
        {
            AddTorque(new Vector3(x, y, z), ForceMode.Force);
        }

        ///<summary>Adds a torque to the rigidbody relative to its coordinate system.</summary>
        ///<remarks>Force can be applied only to an active rigidbody. If a GameObject is inactive, AddRelativeTorque has no effect.
        ///
        ///Wakes up the Rigidbody by default. If the torque size is zero then the Rigidbody will not be woken up.
        ///
        ///For more information on how ForceMode affects angular velocity, see <see cref="Rigidbody.AddTorque" />.</remarks>
        ///<param name="torque">Torque vector in local coordinates.</param>
        ///<example>
        ///  <code><![CDATA[
        /// // Rotate an object around its Y (upward) axis in response to
        /// // left/right controls.
        ///
        ///using UnityEngine;
        ///using UnityEngine.InputSystem;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    private float torque = 4;
        ///    private Rigidbody rb;
        ///                        
        ///    void Awake()
        ///    {
        ///        rb = GetComponent<Rigidbody>();
        ///    }
        ///    
        ///    void FixedUpdate()
        ///    {
        ///        float turnInput = Keyboard.current.spaceKey.isPressed ? 1f : 0f;
        ///
        ///        rb.AddRelativeTorque(Vector3.up * torque * turnInput);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AddTorque" />
        ///<seealso cref="AddRelativeForce" />
        extern public void AddRelativeTorque(Vector3 torque, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddRelativeTorque(Vector3 torque)
        {
            AddRelativeTorque(torque, ForceMode.Force);
        }

        ///<summary>Adds a torque to the rigidbody relative to its coordinate system.</summary>
        ///<remarks>Force can be applied only to an active rigidbody. If a GameObject is inactive, AddRelativeTorque has no effect.
        ///
        ///Wakes up the Rigidbody by default. If the torque size is zero then the Rigidbody will not be woken up.</remarks>
        ///<param name="x">Size of torque along the local x-axis.</param>
        ///<param name="y">Size of torque along the local y-axis.</param>
        ///<param name="z">Size of torque along the local z-axis.</param>
        public void AddRelativeTorque(float x, float y, float z, [DefaultValue("ForceMode.Force")] ForceMode mode) { AddRelativeTorque(new Vector3(x, y, z), mode); }

        [ExcludeFromDocs]
        public void AddRelativeTorque(float x, float y, float z)
        {
            AddRelativeTorque(x, y, z, ForceMode.Force);
        }

        ///<summary>Applies <c>force</c> at <c>position</c>. As a result this will apply a torque and force on the object.</summary>
        ///<remarks>For realistic effects <c>position</c> should be approximately in the range of the surface of the rigidbody.
        ///This is most commonly used for explosions. When applying explosions it is best to apply forces over several frames instead of just one.
        ///Note that when <c>position</c> is far away from the center of the rigidbody the applied torque will be unrealistically large.
        ///
        ///Force can be applied only to an active rigidbody. If a GameObject is inactive, AddForceAtPosition has no effect.
        ///
        ///Wakes up the Rigidbody by default. If the force size is zero then the Rigidbody will not be woken up.
        ///
        ///For more information on how ForceMode affects velocity, see <see cref="Rigidbody.AddForce" />.
        ///
        ///Note: If you are using <c>ForceMode.Acceleration</c> or <c>ForceMode.VelocityChange</c>, the force applied by <c>AddForceAtPosition</c> is scaled by the world-space inertia tensor matrix.</remarks>
        ///<param name="force">Force vector in world coordinates.</param>
        ///<param name="position">Position in world coordinates.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void ApplyForce(Rigidbody body)
        ///    {
        ///        Vector3 direction = body.transform.position - transform.position;
        ///        body.AddForceAtPosition(direction.normalized, transform.position);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="AddForce" />
        ///<seealso cref="AddRelativeForce" />
        ///<seealso cref="AddTorque" />
        ///<seealso cref="inertiaTensor" />
        extern public void AddForceAtPosition(Vector3 force, Vector3 position, [DefaultValue("ForceMode.Force")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddForceAtPosition(Vector3 force, Vector3 position)
        {
            AddForceAtPosition(force, position, ForceMode.Force);
        }

        ///<summary>Applies a force to a rigidbody that simulates explosion effects.</summary>
        ///<remarks>The explosion is modelled as a sphere with a certain centre position and radius in world space; normally, anything outside the sphere is not affected by the explosion and the force decreases in proportion to distance from the centre. However, if a value of zero is passed for the radius then the full force will be applied regardless of how far the centre is from the rigidbody.
        ///
        ///This function applies a force to the object at the point on the surface of the rigidbody that is closest to <c>explosionPosition</c>. The force acts along the direction from <c>explosionPosition</c> to the surface point on the rigidbody. If <c>explosionPosition</c> is inside the rigidbody, or the rigidbody has no active colliders, then the center of mass is used instead of the closest point on the surface.
        ///
        ///The magnitude of the force depends on the distance between <c>explosionPosition</c> and the point where the force was applied. As the distance between <c>explosionPosition</c> and the force point increases, the actual applied force decreases.
        ///
        ///The vertical direction of the force can be modified using <c>upwardsModifier</c>. If this parameter is greater than zero, the force is applied at the point on the surface of the Rigidbody that is closest to <c>explosionPosition</c> but shifted along the y-axis by negative <c>upwardsModifier</c>. Using this parameter, you can make the explosion appear to throw objects up into the air, which can give a more dramatic effect rather than a simple outward force.
        ///Force can be applied only to an active rigidbody. If a GameObject is inactive, <c>AddExplosionForce</c> has no effect.
        ///
        ///Note: If you are using <c>ForceMode.Acceleration</c> or <c>ForceMode.VelocityChange</c>, the force applied by <c>AddExplosionForce</c> is scaled by the world-space inertia tensor matrix.</remarks>
        ///<param name="explosionForce">The force of the explosion (which may be modified by distance).</param>
        ///<param name="explosionPosition">The centre of the sphere within which the explosion has its effect.</param>
        ///<param name="explosionRadius">The radius of the sphere within which the explosion has its effect.</param>
        ///<param name="upwardsModifier">Adjustment to the apparent position of the explosion to make it seem to lift objects.</param>
        ///<param name="mode">The method used to apply the force to its targets.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    [Header("Explosion Settings")]
        ///    public float radius = 5.0f;
        ///    public float power = 10.0f;
        ///    public float upwardsModifier = 3.0f;
        ///    public ForceMode forceMode = ForceMode.Force;
        ///
        ///    [Header("Optional Settings")]
        ///    public LayerMask affectedLayers = ~0; // All layers by default
        ///
        ///    void Update()
        ///    {
        ///        ApplyExplosionForce();
        ///    }
        ///    
        ///    public void ApplyExplosionForce()
        ///    {
        ///        Vector3 explosionPosition = transform.position;
        ///        Collider[] colliders = Physics.OverlapSphere(explosionPosition, radius, affectedLayers);
        ///
        ///        foreach (Collider collider in colliders)
        ///        {
        ///            if (collider.TryGetComponent<Rigidbody>(out Rigidbody rb))
        ///            {
        ///                rb.AddExplosionForce(power, explosionPosition, radius, upwardsModifier, forceMode);
        ///            }
        ///        }
        ///    }
        ///
        ///    void OnDrawGizmosSelected()
        ///    {
        ///        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        ///        Gizmos.DrawWireSphere(transform.position, radius);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Rigidbody.inertiaTensor" />
        extern public void AddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius, [DefaultValue("0.0f")] float upwardsModifier, [DefaultValue("ForceMode.Force)")] ForceMode mode);

        [ExcludeFromDocs]
        public void AddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius, float upwardsModifier)
        {
            AddExplosionForce(explosionForce, explosionPosition, explosionRadius, upwardsModifier, ForceMode.Force);
        }

        [ExcludeFromDocs]
        public void AddExplosionForce(float explosionForce, Vector3 explosionPosition, float explosionRadius)
        {
            AddExplosionForce(explosionForce, explosionPosition, explosionRadius, 0.0f, ForceMode.Force);
        }

        [NativeName("ClosestPointOnBounds")]
        extern private void Internal_ClosestPointOnBounds(Vector3 point, ref Vector3 outPos, ref float distance);

        ///<summary>The closest point to the bounding box of the attached colliders.</summary>
        ///<example>
        ///  <code><![CDATA[
        /// // Subtract damage from a character's hit points when an
        /// // explosion occurs.
        ///
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float hitPoints = 10.0F;
        ///    public Rigidbody rb;
        ///
        ///    void Start()
        ///    {
        ///        rb = GetComponent<Rigidbody>();
        ///    }
        ///
        ///    void ApplyDamage(Vector3 explosionPos, float radius)
        ///    {
        ///        Vector3 closestPoint = rb.ClosestPointOnBounds(explosionPos);
        ///        float distance = Vector3.Distance(closestPoint, explosionPos);
        ///        float damage = 1.0F - Mathf.Clamp01(distance / radius);
        ///        damage *= 10;
        ///        hitPoints -= damage;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Vector3 ClosestPointOnBounds(Vector3 position)
        {
            float dist = 0f;
            Vector3 outpos = Vector3.zero;
            Internal_ClosestPointOnBounds(position, ref outpos, ref dist);
            return outpos;
        }

        extern private RaycastHit SweepTest(Vector3 direction, float maxDistance, QueryTriggerInteraction queryTriggerInteraction, ref bool hasHit);

        ///<summary>Tests if a rigidbody would collide with anything, if it was moved through the Scene.</summary>
        ///<remarks>Tests if a rigidbody would collide with anything, if it was moved through the Scene.
        ///This is similar to doing a <see cref="Physics.Raycast" /> for all points contained in any of a Rigidbody's colliders and returning the closest of all hits (if any) reported. This is useful for AI code, say if you need to know that an object would fit through a gap without colliding with anything.
        ///
        ///Note that this function only works when a primitive collider type (sphere, cube or capsule) or a convex mesh is attached to the rigidbody object - concave mesh colliders will not work, although they can be detected in the Scene by the sweep.</remarks>
        ///<param name="direction">The direction into which to sweep the rigidbody.</param>
        ///<param name="hitInfo">If true is returned, <c>hitInfo</c> will contain more information about where the collider was hit ().</param>
        ///<param name="maxDistance">The length of the sweep.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>True when the rigidbody sweep intersects any collider, otherwise false.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float collisionCheckDistance;
        ///    public bool aboutToCollide;
        ///    public float distanceToCollision;
        ///    public Rigidbody rb;
        ///
        ///    void Start()
        ///    {
        ///        rb = GetComponent<Rigidbody>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        RaycastHit hit;
        ///        if (rb.SweepTest(transform.forward, out hit, collisionCheckDistance))
        ///        {
        ///            aboutToCollide = true;
        ///            distanceToCollision = hit.distance;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Physics.SphereCast" />
        ///<seealso cref="Physics.CapsuleCast" />
        ///<seealso cref="Rigidbody.SweepTestAll" />
        ///<seealso cref="RaycastHit" />
        public bool SweepTest(Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;

            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;
                bool hasHit = false;
                hitInfo = SweepTest(normalizedDirection, maxDistance, queryTriggerInteraction, ref hasHit);
                return hasHit;
            }
            else
            {
                hitInfo = new RaycastHit();
                return false;
            }
        }

        [ExcludeFromDocs]
        public bool SweepTest(Vector3 direction, out RaycastHit hitInfo, float maxDistance)
        {
            return SweepTest(direction, out hitInfo, maxDistance, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public bool SweepTest(Vector3 direction, out RaycastHit hitInfo)
        {
            return SweepTest(direction, out hitInfo, Mathf.Infinity, QueryTriggerInteraction.UseGlobal);
        }

        [NativeName("SweepTestAll")]
        extern private RaycastHit[] Internal_SweepTestAll(Vector3 direction, float maxDistance, QueryTriggerInteraction queryTriggerInteraction);

        ///<summary>Like <see cref="Rigidbody.SweepTest" />, but returns all hits.</summary>
        ///<remarks>The sweep may return multiple hits against the same collider if more then one of the rigidbody's attached colliders would hit it.
        ///
        ///Note that this function only works when a primitive collider type (sphere, cube or capsule) or a convex mesh is attached to the rigidbody object - concave mesh colliders will not work, although they can be detected in the Scene by the sweep.
        ///
        ///This function can only return up to 128 hits.</remarks>
        ///<param name="direction">The direction into which to sweep the rigidbody.</param>
        ///<param name="maxDistance">The length of the sweep.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>An array of all colliders hit in the sweep.</returns>
        public RaycastHit[] SweepTestAll(Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;
                return Internal_SweepTestAll(normalizedDirection, maxDistance, queryTriggerInteraction);
            }
            else
            {
                return Array.Empty<RaycastHit>();
            }
        }

        [ExcludeFromDocs]
        public RaycastHit[] SweepTestAll(Vector3 direction, float maxDistance)
        {
            return SweepTestAll(direction, maxDistance, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public RaycastHit[] SweepTestAll(Vector3 direction)
        {
            return SweepTestAll(direction, Mathf.Infinity, QueryTriggerInteraction.UseGlobal);
        }
    }
}
