// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine
{
    ///<summary>CollisionFlags is a bitmask returned by CharacterController.Move.</summary>
    ///<remarks>It gives you a broad overview of where your character collided with any other objects.</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using UnityEngine;
    ///using System.Collections;
    ///
    ///public class ExampleClass : MonoBehaviour
    ///{
    ///    void Update()
    ///    {
    ///        CharacterController controller = GetComponent<CharacterController>();
    ///
    ///        if (controller.collisionFlags == CollisionFlags.None)
    ///        {
    ///            print("Free floating!");
    ///        }
    ///
    ///        if ((controller.collisionFlags & CollisionFlags.Sides) != 0)
    ///        {
    ///            print("Touching sides!");
    ///        }
    ///
    ///        if (controller.collisionFlags == CollisionFlags.Sides)
    ///        {
    ///            print("Only touching sides, nothing else!");
    ///        }
    ///
    ///        if ((controller.collisionFlags & CollisionFlags.Above) != 0)
    ///        {
    ///            print("Touching Ceiling!");
    ///        }
    ///
    ///        if (controller.collisionFlags == CollisionFlags.Above)
    ///        {
    ///            print("Only touching Ceiling, nothing else!");
    ///        }
    ///
    ///        if ((controller.collisionFlags & CollisionFlags.Below) != 0)
    ///        {
    ///            print("Touching ground!");
    ///        }
    ///
    ///        if (controller.collisionFlags == CollisionFlags.Below)
    ///        {
    ///            print("Only touching ground, nothing else!");
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
    public enum CollisionFlags
    {
        ///<summary>CollisionFlags is a bitmask returned by CharacterController.Move.</summary>
        ///<remarks>It gives you a broad overview of where your character collided with any other objects.</remarks>
        None = 0,
        ///<summary>CollisionFlags is a bitmask returned by CharacterController.Move.</summary>
        ///<remarks>It gives you a broad overview of where your character collided with any other objects.</remarks>
        Sides = 1,
        ///<summary>CollisionFlags is a bitmask returned by CharacterController.Move.</summary>
        ///<remarks>It gives you a broad overview of where your character collided with any other objects.</remarks>
        Above = 2,
        ///<summary>CollisionFlags is a bitmask returned by CharacterController.Move.</summary>
        ///<remarks>It gives you a broad overview of where your character collided with any other objects.</remarks>
        Below = 4,
        ///<exclude />
        CollidedSides = 1,
        ///<exclude />
        CollidedAbove = 2,
        ///<exclude />
        CollidedBelow = 4
    }

    ///<summary>ControllerColliderHit is used by CharacterController.OnControllerColliderHit to give detailed information about the collision and how to deal with it.</summary>
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public partial class ControllerColliderHit
    {
        [AutoStaticsCleanupOnCodeReload(CleanupStrategy = CleanupStrategy.Clear)]
        private static readonly ControllerColliderHit s_ReusableCollision = new ControllerColliderHit();

        internal CharacterController m_Controller;
        internal Collider m_Collider;
        internal Vector3 m_Point;
        internal Vector3 m_Normal;
        internal Vector3 m_MoveDirection;
        internal float m_MoveLength;
        internal int m_Push;

        ///<summary>The controller that hit the collider.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    //Disable collision detection on CharacterControllers we touch.
        ///    void OnControllerColliderHit(ControllerColliderHit hit)
        ///    {
        ///        hit.controller.detectCollisions = false;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public CharacterController controller { get { return m_Controller; } }
        ///<summary>The collider that was hit by the controller.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnControllerColliderHit(ControllerColliderHit hit)
        ///    {
        ///        // Call a damage function on the object we hit.
        ///        hit.gameObject.SendMessage("ApplyDamage", 5);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Collider collider { get { return m_Collider; } }
        ///<summary>The rigidbody that was hit by the controller.</summary>
        ///<remarks>Null if we didn't touch a rigidbody but a static collider.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Activate the gravity of other object we touch
        ///    void OnControllerColliderHit(ControllerColliderHit hit)
        ///    {
        ///        if (hit != null)
        ///        {
        ///            hit.rigidbody.useGravity = true;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Rigidbody rigidbody { get { return m_Collider.attachedRigidbody; } }
        ///<summary>The game object that was hit by the controller.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnControllerColliderHit(ControllerColliderHit hit)
        ///    {
        ///        // Objects we touch, move them to position (0, 0, 0)
        ///        hit.gameObject.transform.position = Vector3.zero;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GameObject gameObject { get { return m_Collider.gameObject; } }
        ///<summary>The transform that was hit by the controller.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Print the transform's name that collided with this ControllerCollider
        ///    void OnControllerColliderHit(ControllerColliderHit hit)
        ///    {
        ///        if (hit != null)
        ///        {
        ///            Debug.Log("I'm colliding with: " + hit.transform.name);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Transform transform { get { return m_Collider.transform; } }
        ///<summary>The impact point in world space.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnControllerColliderHit(ControllerColliderHit hit)
        ///    {
        ///        // print the impact point
        ///        Debug.Log("I impacted at: " + hit.point);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Vector3 point { get { return m_Point; } }
        ///<summary>The normal of the surface we collided with in world space.</summary>
        ///<remarks>**Note:** When the CharacterController is colliding with an edge or a corner rather than a flat surface,
        ///the reported normal may be different when colliding with BoxColliders than when colliding with MeshColliders.
        ///This is due to a limitation in how PhysX handles Capsule/BoxCollider collisions.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnControllerColliderHit(ControllerColliderHit hit)
        ///    {
        ///        // print the impact point's normal
        ///        Debug.Log("Normal vector we collided at: " + hit.normal);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Vector3 normal { get { return m_Normal; } }
        ///<summary>The direction the CharacterController was moving in when the collision occured.</summary>
        ///<remarks>This is the direction that the CharacterController was moving in when the collision occured. It can be used to find a reasonable direction to apply forces to touched rigidbodies.
        ///
        ///Note that this is not necessarily the same as the movement vector passed to <see cref="CharacterController.Move" /> or <see cref="CharacterController.SimpleMove" />. The CharacterController uses a sequence of motions to perform a move in accordance with the move direction and the step Offset, in order to step over obstacles. moveDirection will be the direction of the motion during which the collision was detected.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnControllerColliderHit(ControllerColliderHit hit)
        ///    {
        ///        Debug.Log(hit.moveDirection);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Vector3 moveDirection { get { return m_MoveDirection; } }
        ///<summary>How far the character has travelled until it hit the collider.</summary>
        ///<remarks>Note that this can be different from what you pass to <see cref="CharacterController.Move" />, because the initial movement vector is decomposed into a set of movements, according to <see cref="CharacterController.stepOffset" />.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void OnControllerColliderHit(ControllerColliderHit hit)
        ///    {
        ///        Debug.Log(hit.moveLength);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="ControllerColliderHit.moveDirection" />
        public float moveLength { get { return m_MoveLength; } }
        private bool push { get { return m_Push != 0; } set { m_Push = value ? 1 : 0; } }

        private void SetAllFields(CharacterController controller, Collider collider, Vector3 point, Vector3 normal, Vector3 moveDirection, float moveLength)
        {
            m_Controller = controller;
            m_Collider = collider;
            m_Point = point;
            m_Normal = normal;
            m_MoveDirection = moveDirection;
            m_MoveLength = moveLength;
            m_Push = 0;
        }

        internal void Clear()
        {
            m_Controller = null;
            m_Collider = null;
            m_Point = Vector3.zero;
            m_Normal = Vector3.zero;
            m_MoveDirection = moveDirection;
            m_MoveLength = 0.0f;
            m_Push = 0;
        }

        [RequiredByNativeCode]
        static ControllerColliderHit Create(CharacterController controller, Collider collider, Vector3 point, Vector3 normal, Vector3 moveDirection, float moveLength)
        {
            var hit = new ControllerColliderHit();
            hit.SetAllFields(controller, collider, point, normal, moveDirection, moveLength);
            return hit;
        }

        [RequiredByNativeCode]
        static void Update(ControllerColliderHit hit, CharacterController controller, Collider collider, Vector3 point, Vector3 normal, Vector3 moveDirection, float moveLength)
        {
            hit.SetAllFields(controller, collider, point, normal, moveDirection, moveLength);
        }
    }

    ///<summary>A CharacterController allows you to easily do movement constrained by collisions without having to deal with a rigidbody.</summary>
    ///<remarks>A CharacterController is not affected by forces and will only move when you call the Move function.
    ///It will then carry out the movement but be constrained by collisions.</remarks>
    ///<seealso href="xref:class-CharacterController">Character Controller component</seealso>
    ///<seealso href="http://unity3d.com/learn/tutorials/modules/beginner/animation">Character animation examples</seealso>
    [global::UnityEngine.NativeClass("CharacterController", PersistentTypeId = 143)]
    [NativeHeader("Modules/Physics/CharacterController.h")]
    public class CharacterController : Collider
    {
        ///<summary>Moves the character with <c>speed</c>.</summary>
        ///<remarks>Velocity along the y-axis is ignored.
        ///Speed is in units/s. Gravity is automatically applied.
        ///Returns true if the character is grounded.
        ///It is recommended that you make only one call to <see cref="Move" /> or <see cref="SimpleMove" /> per frame.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.InputSystem;
        ///
        ///[RequireComponent(typeof(CharacterController))]
        ///public class CharacterMover : MonoBehaviour
        ///{
        ///    private float moveSpeed = 3.0f;
        ///    private float rotationSpeed = 90.0f; // degrees per second
        ///
        ///    public CharacterController characterController;
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
        ///    void Update()
        ///    {
        ///        // Rotate character
        ///        transform.Rotate(Vector3.up, moveAction.action.ReadValue<Vector2>().x * rotationSpeed * Time.deltaTime);
        ///
        ///        // Move character
        ///        Vector3 moveDirection = transform.forward * moveAction.action.ReadValue<Vector2>().y * moveSpeed;
        ///        
        ///        characterController.SimpleMove(moveDirection);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public bool SimpleMove(Vector3 speed);
        ///<summary>Supplies the movement of a GameObject with an attached CharacterController component.</summary>
        ///<remarks>The <see cref="CharacterController.Move" /> motion moves the GameObject in the given direction. The given direction requires absolute movement delta values. A collision constrains the <see cref="Move" /> from taking place. The return, <see cref="CollisionFlags" />, indicates the direction of a collision: None, Sides, Above, and Below. <see cref="CharacterController.Move" /> does not use gravity.
        ///
        ///The example below demonstrates how to use <see cref="CharacterController.Move" />. <c>Update</c> causes a <see cref="Move" /> to re-position the player. In addition, <c>Jump</c> changes the player position in a vertical direction.</remarks>
        ///<example nocheck="true">
        ///  <code><![CDATA[
        /// // This first example shows how to move using Input System Package (New)
        ///
        ///using UnityEngine;
        ///using UnityEngine.InputSystem;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    private float playerSpeed = 5.0f;
        ///    private float jumpHeight = 1.5f;
        ///    private float gravityValue = -9.81f;
        ///
        ///    public CharacterController controller;
        ///    private Vector3 playerVelocity;
        ///    private bool groundedPlayer;
        ///
        ///    [Header("Input Actions")]
        ///    public InputActionReference moveAction;
        ///    public InputActionReference jumpAction;
        ///
        ///    private void OnEnable()
        ///    {
        ///        moveAction.action.Enable();
        ///        jumpAction.action.Enable();
        ///    }
        ///
        ///    private void OnDisable()
        ///    {
        ///        moveAction.action.Disable();
        ///        jumpAction.action.Disable();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        groundedPlayer = controller.isGrounded;
        ///
        ///        if (groundedPlayer)
        ///        {
        ///            // Slight downward velocity to keep grounded stable
        ///            if (playerVelocity.y < -2f)
        ///                playerVelocity.y = -2f;
        ///        }
        ///
        ///        // Read input
        ///        Vector2 input = moveAction.action.ReadValue<Vector2>();
        ///        Vector3 move = new Vector3(input.x, 0, input.y);
        ///        move = Vector3.ClampMagnitude(move, 1f);
        ///
        ///        if (move != Vector3.zero)
        ///            transform.forward = move;
        ///
        ///        // Jump using WasPressedThisFrame()
        ///        if (groundedPlayer && jumpAction.action.WasPressedThisFrame())
        ///        {
        ///            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravityValue);
        ///        }
        ///
        ///        // Apply gravity
        ///        playerVelocity.y += gravityValue * Time.deltaTime;
        ///
        ///        // Move
        ///        Vector3 finalMove = move * playerSpeed + Vector3.up * playerVelocity.y;
        ///        controller.Move(finalMove * Time.deltaTime);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public CollisionFlags Move(Vector3 motion);
        ///<summary>The current relative velocity of the Character (see notes).</summary>
        ///<remarks>This allows you to track how fast the character is actually walking, for example
        ///when it is stuck at a wall this value will be the zero vector.
        ///
        ///Note: The velocity returned is simply the difference in distance for the current timestep
        ///before and after a call to <see cref="CharacterController.Move" /> or <see cref="CharacterController.SimpleMove" />.
        ///The velocity is relative because it won't track movements to the transform that happen
        ///outside of the CharacterController (e.g. character parented under another moving Transform,
        ///such as a moving vehicle).</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    CharacterController controller;
        ///    void Start()
        ///    {
        ///        controller = GetComponent<CharacterController>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        Vector3 horizontalVelocity = controller.velocity;
        ///        horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        ///
        ///        // The speed on the x-z plane ignoring any speed
        ///        float horizontalSpeed = horizontalVelocity.magnitude;
        ///        // The speed from gravity or jumping
        ///        float verticalSpeed  = controller.velocity.y;
        ///        // The overall speed
        ///        float overallSpeed = controller.velocity.magnitude;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public Vector3 velocity { get; internal set; }
        ///<summary>Was the CharacterController touching the ground during the last move?</summary>
        ///<remarks>Indicates whether the CharacterController was touching the ground during the most recent call to CharacterController.Move or CharacterController.SimpleMove.
        ///
        ///                This property is updated after each call to Move, based on collision detection with the ground. It returns true if the controller collided with any object below it during the movement — typically used to determine if the character is standing on a surface (e.g., terrain, platform, floor).</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    CharacterController characterController;
        ///
        ///    void Start()
        ///    {
        ///        characterController = GetComponent<CharacterController>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        if (characterController.isGrounded)
        ///        {
        ///            print("CharacterController is grounded");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public bool isGrounded { [NativeName("IsGrounded")] get; }
        ///<summary>What part of the capsule collided with the environment during the last CharacterController.Move call.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    CharacterController controller;
        ///
        ///    void Start()
        ///    {
        ///        controller = GetComponent<CharacterController>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        if ((controller.collisionFlags & CollisionFlags.Above) != 0)
        ///        {
        ///            print("touched the ceiling");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public CollisionFlags collisionFlags { get; internal set; }
        ///<summary>The radius of the character's capsule.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Set the controller radius to 0.3f
        ///    CharacterController controller;
        ///
        ///    void Start()
        ///    {
        ///        controller = GetComponent<CharacterController>();
        ///        controller.radius = 0.3f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float radius { get; set; }
        ///<summary>The height of the character's capsule.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Set the controller height to 2.0
        ///    CharacterController controller;
        ///
        ///    void Start()
        ///    {
        ///        controller = GetComponent<CharacterController>();
        ///        controller.height = 2.0f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float height { get; set; }
        ///<summary>The center of the character's capsule relative to the transform's position.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    CharacterController controller;
        ///    private void Start()
        ///    {
        ///        controller = GetComponent<CharacterController>();
        ///        controller.center = new Vector3(0, 1, 0);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public Vector3 center { get; set; }
        ///<summary>The character controllers slope limit in degrees.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Set the controller slope limit to 45 degrees
        ///    CharacterController controller;
        ///
        ///    void Start()
        ///    {
        ///        controller = GetComponent<CharacterController>();
        ///        controller.slopeLimit = 45.0f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float slopeLimit { get; set; }
        ///<summary>The character controllers step offset in meters.</summary>
        ///<remarks>The maximum height in meters that the character can climb over automatically when moving. This is used to allow the character to smoothly step over small obstacles like stairs or ledges instead of colliding with them.
        ///
        ///Increasing the <c>stepOffset</c> allows the character to step over taller obstacles without jumping. Decreasing the value restricts the character to only stepping over smaller objects.
        ///
        ///* **Higher values** allow smoother traversal over high steps or uneven terrain, but may cause unrealistic movement if set too high (e.g., stepping over full-height walls).
        ///* **Lower values** make the character collide with even small obstacles, potentially requiring jumping or custom logic to overcome them.
        ///
        ///This value works in conjunction with the slope limit—if an obstacle’s slope is too steep, even within the <c>stepOffset</c>, it might still block movement.
        ///
        ///**Note:** See the Manual page [Character Controller component](xref:class-CharacterController) which describes <c>stepOffset</c> in detail.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public CharacterController controller;
        ///
        ///    void Example()
        ///    {
        ///        controller = GetComponent<CharacterController>();
        ///        // Allow the character to step over obstacles up to 0.5 meters high
        ///        controller.stepOffset = 0.5f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public float stepOffset { get; set; }
        ///<summary>The character's collision skin width.</summary>
        ///<remarks>Specifies a skin around the character within which contacts will be generated by the physics engine. Use it to avoid numerical precision issues.
        ///
        ///This is dependant on the scale of the  world, but should be a small, positive non zero value.</remarks>
        extern public float skinWidth { get; set; }
        ///<summary>Gets or sets the minimum move distance of the character controller.</summary>
        ///<remarks>If the character tries to move less than this distance, it will not move at all. This can be used to reduce jitter. In most situations this value should be left at 0.</remarks>
        extern public float minMoveDistance { get; set; }
        ///<summary>Determines whether other rigidbodies or character controllers collide with this character controller (by default this is always enabled).</summary>
        ///<remarks>This method does not affect collisions detected during the character controller's movement but rather decides whether an
        ///incoming collider will be blocked by the controller's collider. For example, a box collider in the Scene will block the movement of the controller,
        ///but the box may still fall through the controller if detectCollisions is false.
        ///This property is useful to disable the character controller temporarily. For example, you might want to mount a character into a car and
        ///disable collision detection until it exits the car again.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    CharacterController controller;
        ///
        ///    void Start()
        ///    {
        ///        controller = GetComponent<CharacterController>();
        ///        controller.detectCollisions = false;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public bool detectCollisions { get; set; }
        ///<summary>Enables or disables overlap recovery. Used to depenetrate character controllers from static objects when an overlap is detected.</summary>
        ///<remarks>Overlap recovery can be used to depenetrate character controllers (CCTs) from static objects when an overlap is detected. This can happen
        /// in three main cases:
        ///
        ///         - when the CCT is directly spawned or teleported in another object
        ///
        ///         - when the CCT algorithm fails due to limited FPU accuracy
        ///
        ///         - when the "up vector" is modified, making the rotated CCT shape overlap surrounding objects
        ///
        ///    When activated, the CCT module will automatically try to resolve the penetration, and move the CCT to a safe place where it does
        ///
        ///not overlap other objects anymore. This only concerns static objects, dynamic objects are ignored by overlap recovery.
        ///
        ///When overlap recovery is not activated, it is possible for the CCTs to go through static objects. By default, overlap recovery is enabled.
        ///
        ///Overlap recovery currently works with all geometries except heightfields.</remarks>
        extern public bool enableOverlapRecovery { get; set; }
        extern internal bool isSupported { get; }

        [StaticAccessor("CharacterController", StaticAccessorType.DoubleColon)]
        extern internal static float deltaTimeOverride { get; set; }
    }
}
