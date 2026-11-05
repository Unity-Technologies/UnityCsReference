// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using UnityEngine.Internal;

namespace UnityEngine
{
    ///<summary>Overrides the global <see cref="Physics.queriesHitTriggers" />.</summary>
    ///<remarks>Overrides the global <see cref="Physics.queriesHitTriggers" /> to specify whether queries (raycasts, spherecasts, overlap tests, etc.) hit Triggers by default.
    ///Use Ignore for queries to ignore trigger Colliders.</remarks>
    ///<example>
    ///  <code><![CDATA[
    /// //Create two GameObjects (e.g. a Cube) and place them near each other. Attach this script to one of them.
    /// //Click on the GameObject with the script. Attach the other GameObject to the “My Game Object” field in the Inspector.
    /// //Make sure both have Collider components
    /// //Choose your own “Max Distance” in the Inspector (e.g. 600).
    ///
    /// //This script casts a ray that ignores Trigger Colliders.
    /// //Press space to switch the second GameObject between a Trigger and non-Trigger GameObject. When the Trigger is off, the ray detects a collision. When it is on, no collisions occur.
    ///
    ///using UnityEngine;
    ///
    ///public class Example : MonoBehaviour
    ///{
    ///    //The maximum distance from your GameObject. Make sure to set this in the Inspector
    ///    public float m_MaxDistance;
    ///    public LayerMask m_Mask = -1;
    ///
    ///    //Assign a GameObject in the Inspector that you want to test collisions with
    ///    public GameObject m_MyGameObject;
    ///    //This is the Collider of the GameObject you assign in the Inspector
    ///    Collider m_OtherGameObjectCollider;
    ///
    ///    void Start()
    ///    {
    ///        //Fetch the Collider from the GameObject you assign in the Inspector
    ///        m_OtherGameObjectCollider = m_MyGameObject.GetComponent<Collider>();
    ///    }
    ///
    ///    void FixedUpdate()
    ///    {
    ///        //Set the direction as forward
    ///        Vector3 direction = transform.TransformDirection(Vector3.forward);
    ///
    ///        //Use Physics to calculate the raycast
    ///        //Uses your GameObject's original position, the direction (above), the max distance from your GameObject, and the LayerMask value to calculate raycast.
    ///        //Also tells it to ignore trigger colliders using QueryTriggerInteraction
    ///        if (Physics.Raycast(transform.position, direction, m_MaxDistance, m_Mask.value, QueryTriggerInteraction.Ignore))
    ///            print("There is something in front of the object!");
    ///    }
    ///
    ///    void Update()
    ///    {
    ///        //Press space to turn the other GameObject's trigger status on and off
    ///        if (Input.GetKeyDown(KeyCode.Space))
    ///        {
    ///            //Test if the trigger collisions are ignored by turning the GameObject's trigger collider on and off
    ///            m_OtherGameObjectCollider.isTrigger = !m_OtherGameObjectCollider.isTrigger;
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
    public enum QueryTriggerInteraction
    {
        ///<summary>Queries use the global <see cref="Physics.queriesHitTriggers" /> setting.</summary>
        UseGlobal = 0,
        ///<summary>Queries never report Trigger hits.</summary>
        ///<example>
        ///  <code><![CDATA[
        /// //Create two GameObjects (e.g. a Cube) and place them near each other. Attach this script to one of them.
        /// //Click on the GameObject with the script. Attach the other GameObject to the “My Game Object” field in the Inspector.
        /// //Make sure both have Collider components
        /// //Choose your own “Max Distance” in the Inspector (e.g. 600).
        ///
        /// //This script casts a ray that ignores Trigger Colliders.
        /// //Press space to switch the second GameObject between a Trigger and non-Trigger GameObject. When the Trigger is off, the ray detects a collision. When it is on, no collisions occur.
        ///
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    //The maximum distance from your GameObject. Make sure to set this in the Inspector
        ///    public float m_MaxDistance;
        ///    public LayerMask m_Mask = -1;
        ///
        ///    //Assign a GameObject in the Inspector that you want to test collisions with
        ///    public GameObject m_MyGameObject;
        ///    //This is the Collider of the GameObject you assign in the Inspector
        ///    Collider m_OtherGameObjectCollider;
        ///
        ///    void Start()
        ///    {
        ///        //Fetch the Collider from the GameObject you assign in the Inspector
        ///        m_OtherGameObjectCollider = m_MyGameObject.GetComponent<Collider>();
        ///    }
        ///
        ///    void FixedUpdate()
        ///    {
        ///        //Set the direction as forward
        ///        Vector3 direction = transform.TransformDirection(Vector3.forward);
        ///
        ///        //Use Physics to calculate the raycast
        ///        //Uses your GameObject's original position, the direction (above), the max distance from your GameObject, and the LayerMask value to calculate raycast.
        ///        //Also tells it to ignore trigger colliders using QueryTriggerInteraction
        ///        if (Physics.Raycast(transform.position, direction, m_MaxDistance, m_Mask.value, QueryTriggerInteraction.Ignore))
        ///            print("There is something in front of the object!");
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        //Press space to turn the other GameObject's trigger status on and off
        ///        if (Input.GetKeyDown(KeyCode.Space))
        ///        {
        ///            //Test if the trigger collisions are ignored by turning the GameObject's trigger collider on and off
        ///            m_OtherGameObjectCollider.isTrigger = !m_OtherGameObjectCollider.isTrigger;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        Ignore = 1,
        ///<summary>Queries always report Trigger hits.</summary>
        Collide = 2
    }
    ///<summary>A selection of modes that control when Unity executes the physics simulation.</summary>
    ///<example>
    ///  <code><![CDATA[
    /// // SimulationMode.FixedUpdate is the default setting in Unity.
    /// // Attach this script to a gameObject and enter runtime mode.
    ///
    ///using UnityEngine;
    ///
    ///public class ManualPhysicsSimulation : MonoBehaviour
    ///{
    ///    void Start()
    ///    {
    ///        Debug.Log("Current Physics.simulationMode: " + Physics.simulationMode);
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<seealso cref="Physics.simulationMode" />
    public enum SimulationMode
    {
        ///<summary>Use this enumeration to instruct Unity to execute the physics simulation immediately after the <see cref="M:UnityEngine.MonoBehaviour.FixedUpdate" />.</summary>
        ///<remarks>Executing the physics simulation during <see cref="M:UnityEngine.MonoBehaviour.FixedUpdate" /> provides the most stable physics simulation, however Unity might render multiple frames between simulation updates. This might prevent synchronization of changes made per frame, including contacts between <see cref="Collider" />s, with the physics simulation. This mode requires <see cref="Rigidbody" /> interpolation to provide smoother movement per frame where appropriate.</remarks>
        ///<seealso cref="Physics.simulationMode" />
        FixedUpdate = 0,
        ///<summary>Use this enumeration to instruct Unity to execute the physics simulation immediately after <see cref="M:UnityEngine.MonoBehaviour.Update" />.</summary>
        ///<remarks>Executing the physics simulation during <see cref="M:UnityEngine.MonoBehaviour.Update" /> provides a less stable and deterministic physics simulation due to the unpredictable fluctuations in framerate that can arise. However, the physics simulation is always synchronized each frame, including contacts between <see cref="Collider" />s. This mode doesn't require <see cref="Rigidbody" /> interpolation.</remarks>
        ///<seealso cref="Physics.simulationMode" />
        Update = 1,
        ///<summary>Use this enumeration to instruct Unity to execute the physics simulation manually when you call <see cref="Physics.Simulate" />.</summary>
        ///<remarks>Executing the physics simulation in a script provides full control over when the simulation runs and the duration of simulation. Use this mode to emulate both <see cref="SimulationMode.FixedUpdate" /> and <see cref="SimulationMode.Update" />, and any other custom time interval.
        ///
        ///The stability and determinism of the simulation depends on when Unity executes the simulation. If the simulation runs each frame, then it is always synchronized, including contacts between <see cref="Collider" />s. This means that <see cref="Rigidbody" /> interpolation is not required.</remarks>
        ///<seealso cref="Physics.simulationMode" />
        Script = 2
    }

    ///<summary>A flag enum to determine which simulation stages to run.</summary>
    ///<seealso cref="PhysicsScene.RunSimulationStages" />
    public enum SimulationStage : ushort
    {
        ///<summary>Shorthand for none of the <see cref="SimulationStage" />s.</summary>
        None = 0,
        ///<summary>This stage prepares the physics scene for simulation.</summary>
        ///<remarks>Specifically, this stage:
        ///
        ///- Displays any pending errors from the physics system.
        ///- Calls <see cref="Physics.SyncTransforms" /> to apply any pending <see cref="Transform" /> changes to the physics system.
        ///- Stores interpolation poses.</remarks>
        PrepareSimulation = 1 << 0,
        ///<summary>This stage advances the scene in time.</summary>
        ///<remarks>Specifically, this stage:
        ///
        ///- Clears stored contact, trigger, and joint break events.
        ///- Updates all vehicles forward in time.
        ///- Advances the physics scene forward in time.
        ///- Invokes the <see cref="Physics.ContactModifyEvent" />.
        ///- Reads and stores contact, trigger, and joint break events.</remarks>
        RunSimulation = 1 << 1,
        ///<summary>This stage publishes simulation results.</summary>
        ///<remarks>Specifically, the stage:
        ///
        ///- Applies latest <see cref="Rigidbody" /> poses to corresponding <see cref="Transform" /> components.
        ///- Applies latest <see cref="ArticulationBody" /> poses to corresponding <see cref="Transform" /> components.
        ///- Invokes OnCollision, OnTrigger, OnJointBreak, and <see cref="Physics.ContactEvent" /> events.</remarks>
        PublishSimulationResults = 1 << 2,
        ///<summary>Shorthand for combining all the <see cref="SimulationStage" />s.</summary>
        ///<seealso cref="SimulationStage.PrepareSimulation" />
        ///<seealso cref="SimulationStage.RunSimulation" />
        ///<seealso cref="SimulationStage.PublishSimulationResults" />
        ///<seealso cref="PhysicsScene.RunSimulationStages" />
        All = PrepareSimulation | RunSimulation | PublishSimulationResults
    }

    ///<summary>An enumerator that specifies physics simulation options.</summary>
    public enum SimulationOption : ushort
    {
        ///<summary>Shorthand for no <see cref="SimulationOption" /> flags.</summary>
        None = 0,
        ///<summary>Define whether or not to sync modified <see cref="Transform" /> poses to the physics system.</summary>
        ///<remarks>This option has no effect if the <see cref="SimulationStage.PrepareSimulation" /> stage is not specified in the <see cref="PhysicsScene.RunSimulationStages" /> call.</remarks>
        SyncTransforms = 1 << 0,
        ///<summary>Define whether or not to skip the simulation if there are no active physics objects in the scene.</summary>
        ///<remarks>Skips the <see cref="SimulationStage.RunSimulation" /> stage if there are no active Rigidbodies, ArticulationBodies, or Colliders in the scene.
        ///This option has no effect if the <see cref="SimulationStage.RunSimulation" /> stage is not specified in the <see cref="PhysicsScene.RunSimulationStages" /> call.</remarks>
        IgnoreEmptyScenes = 1 << 1,
        ///<summary>Shorthand for all <see cref="SimulationOption" /> flags.</summary>
        All = SyncTransforms | IgnoreEmptyScenes
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct JointLimitRange
    {
        public float min;
        public float max;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct IntegrationLimits
    {
        public const int JointTypeCount = 7; // Matches JointType::Count in C++

        fixed float m_Joints[JointTypeCount * 2]; // Each JointLimitRange has 2 floats (cannot use fixed JointLimitRange[] - C# limitation)

        public JointLimitRange GetJointLimit(ArticulationJointType jointType)
        {
            int index = (int)jointType * 2;
            if (index < 0 || index >= JointTypeCount * 2 - 1)
                return default;

            return new JointLimitRange { min = m_Joints[index], max = m_Joints[index + 1] };
        }
    }

    ///<exclude />
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct IntegrationInfo
    {
        [Flags]
        internal enum SupportedUnityFeatures
        {
            None = 0,
            DynamicsSupport = 1 << 1,
            SDKVisualDebuggerSupport = 1 << 2,
            ArticulationSupport = 1 << 3,
            ImmediateModeSupport = 1 << 4,
            VehicleSupport = 1 << 5,
            CharacterControllerSupport = 1 << 6
        };

        internal const uint k_InvalidID = 0;
        internal const uint k_FallbackIntegrationId = 0xDECAFBAD;

        [FieldOffset(0)]
        readonly uint m_Id;
        [FieldOffset(4)]
        fixed ushort m_IntegrationVersion[3];
        [FieldOffset(10)]
        fixed ushort m_SdkVersion[3];
        [FieldOffset(16)]
        readonly SupportedUnityFeatures m_Features;
        [FieldOffset(20)]
        fixed byte m_Name[16];
        [FieldOffset(36)]
        fixed byte m_Desc[220];
        [FieldOffset(256)]
        IntegrationLimits m_Limit;

        ///<exclude />
        public readonly uint id => m_Id;

        ///<exclude />
        public unsafe string name {
            get
            {
                fixed(byte* ptr = m_Name)
                    return Marshal.PtrToStringAnsi(new IntPtr(ptr));
            }
        }

        ///<exclude />
        public unsafe string description
        {
            get
            {
                fixed (byte* ptr = m_Desc)
                    return Marshal.PtrToStringAnsi(new IntPtr(ptr));
            }
        }

        internal ushort sDKMajorVersion => m_SdkVersion[0];
        internal ushort sDKMinorVersion => m_SdkVersion[1];
        internal ushort sDKPatchVersion => m_SdkVersion[2];

        internal ushort majorVersion => m_IntegrationVersion[0];
        internal ushort minorVersion => m_IntegrationVersion[1];
        internal ushort patchVersion => m_IntegrationVersion[2];

        ///<exclude />
        public bool isFallback => id == k_FallbackIntegrationId;

        internal bool isExperimental => m_IntegrationVersion[0] < 1;

        ///<exclude />
        internal IntegrationLimits limit => m_Limit;
    }

    [NativeHeader("Modules/Physics/PhysicsQuery.h")]
    [NativeHeader("Modules/Physics/PhysicsManager.h")]
    [StaticAccessor("GetPhysicsManager()", StaticAccessorType.Dot)]
    public partial class Physics
    {
        //Matches kFloatMaxMinusEpsilon in PhysicsConstants.h; currently used in e.g., EnforceJointLimitsConsistency()
        internal const float k_MaxFloatMinusEpsilon = 340282326356119260000000000000000000000f;

        ///<summary>Layer mask constant to select ignore raycast layer.</summary>
        ///<remarks>This can be used in the layermask field of <see cref="Physics.Raycast" /> and other methods to select the "ignore raycast" layer (which does not receive raycasts by default).</remarks>
        ///<seealso cref="Physics.AllLayers" />
        ///<seealso cref="Physics.DefaultRaycastLayers" />
        public const int IgnoreRaycastLayer = 1 << 2;
        ///<summary>Layer mask constant to select default raycast layers.</summary>
        ///<remarks>This can be used in the layermask field of <see cref="Physics.Raycast" /> and other
        ///        methods to select the default raycast layers. The default layers are all layers except for the ignore raycast layer.</remarks>
        ///<seealso cref="Physics.AllLayers" />
        ///<seealso cref="Physics.IgnoreRaycastLayer" />
        public const int DefaultRaycastLayers = ~IgnoreRaycastLayer;
        ///<summary>Layer mask constant to select all layers.</summary>
        ///<remarks>This can be used in the layermask field of <see cref="Physics.Raycast" /> and other methods to select all layers.</remarks>
        ///<seealso cref="Physics.DefaultRaycastLayers" />
        ///<seealso cref="Physics.IgnoreRaycastLayer" />
        public const int AllLayers = ~0;

        extern private unsafe static void GetIntegrationInfos(out IntPtr integrations, out ulong integrationCount);

        [NativeMethod(IsThreadSafe = true)]
        extern private unsafe static void GetCurrentIntegrationInfo(out IntPtr integration);

        internal static ReadOnlySpan<IntegrationInfo> GetIntegrationInfos()
        {
            unsafe
            {
                IntPtr integrations;
                ulong count;
                GetIntegrationInfos(out integrations, out count);

                return new ReadOnlySpan<IntegrationInfo>(integrations.ToPointer(), (int)count);
            }
        }

        ///<exclude />
        public unsafe static IntegrationInfo GetCurrentIntegrationInfo()
        {
            IntPtr infoPtr;
            GetCurrentIntegrationInfo(out infoPtr);

            return *(IntegrationInfo*)infoPtr.ToPointer();
        }

        ///<summary>The gravity applied to all rigid bodies in the Scene.</summary>
        ///<remarks>
        ///  <para>Gravity can be turned off for an individual rigidbody using its <see cref="Rigidbody.useGravity" /> property.</para>
        ///  <para>
        ///    <c>ParticleSystem.gravityModifier</c> is  Obsolete.</para>
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Example()
        ///    {
        ///        Physics.gravity = new Vector3(0, -1.0F, 0);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public static Vector3 gravity { [NativeMethod(IsThreadSafe = true)] get; set; }
        ///<summary>The default contact offset of the newly created colliders.</summary>
        ///<remarks>Colliders whose distance is less than the sum of their contactOffset values will generate contacts. The contact offset must be positive. Contact offset allows the collision detection system to predictively enforce the contact constraint even when the objects are slightly separated.</remarks>
        extern public static float defaultContactOffset { get; set; }
        ///<summary>The mass-normalized energy threshold, below which objects start going to sleep.</summary>
        extern public static float sleepThreshold { get; set; }
        ///<summary>Specifies whether queries (raycasts, spherecasts, overlap tests, etc.) hit Triggers by default.</summary>
        ///<remarks>This can be overridden on a per-query level by specifying the QueryTriggerInteraction parameter.</remarks>
        ///<seealso cref="Physics.Raycast" />
        extern public static bool queriesHitTriggers { get; set; }
        ///<summary>Whether physics queries should hit back-face triangles.</summary>
        ///<remarks>By default, all physics queries such as a raycast or a shape sweep (e.g. SphereCastAll) won't detect hits with back-face triangles.</remarks>
        extern public static bool queriesHitBackfaces { get; set; }
        ///<summary>Two colliding objects with a relative velocity below this will not bounce (default 2). Must be positive.</summary>
        ///<remarks>This value is usually changed in <c>Edit-&gt;Project Settings-&gt;Physics</c> inspector instead of from scripts.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Physics.bounceThreshold = 1;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public static float bounceThreshold { get; set; }
        ///<summary>The maximum default velocity needed to move a Rigidbody's collider out of another collider's surface penetration. Must be positive.</summary>
        ///<remarks>This value is usually changed in <c>Edit-&gt;Project Settings-&gt;Physics-&gt;Settings-&gt;Game Object</c> inspector instead of from scripts.
        ///
        ///**Note:** Very large values can introduce instability during collision detection; too small values might cause the collider depenetration to fail.
        ///
        ///You can also set a maximum depenetration velocity for individual Rigidbodies via <see cref="Rigidbody.maxDepenetrationVelocity" />.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        // By default defaultMaxDepenetrationVelocity has a value of 10.0f
        ///        Physics.defaultMaxDepenetrationVelocity = 5.0f;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public static float defaultMaxDepenetrationVelocity { get; set; }
        ///<summary>The defaultSolverIterations determines how accurately Rigidbody joints and collision contacts are resolved. (default 6). Must be positive.</summary>
        ///<remarks>If you are having trouble with connected Rigidbodies oscillating and behaving erratically setting
        ///a higher solver iteration count may improve their stability (but is slower).
        ///
        ///This value is usually changed in <c>Edit-&gt;Project Settings-&gt;Physics</c> inspector instead of from scripts.
        ///
        ///**Note:** Changing the defaultSolverIterations does not affect already created Rigidbodies. To change an existing Rigidbody please use <see cref="Rigidbody.solverIterations" />.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Physics.defaultSolverIterations = 10;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Physics.defaultSolverVelocityIterations" />
        extern public static int defaultSolverIterations { get; set; }
        ///<summary>The defaultSolverVelocityIterations affects how accurately the Rigidbody joints and collision contacts are resolved. (default 1). Must be positive.</summary>
        ///<remarks>Increasing this value will result in higher accuracy of the resulting exit velocity after a Rigidbody bounce.
        ///If you are experiencing issues with jointed Rigidbodies or Ragdolls moving too much after collisions you can try to increase this value.
        ///
        ///This value is usually changed in <c>Edit-&gt;Project Settings-&gt;Physics</c> inspector instead of from scripts.
        ///
        ///**Note:** Changing the defaultSolverVelocityIterations does not affect already created Rigidbodies. To change an existing Rigidbody please use <see cref="Rigidbody.solverVelocityIterations" />.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        Physics.defaultSolverVelocityIterations = 10;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Physics.defaultSolverIterations" />
        extern public static int defaultSolverVelocityIterations { get; set; }
        ///<summary>Controls when Unity executes the physics simulation.</summary>
        ///<seealso cref="SimulationMode" />
        extern public static SimulationMode simulationMode { get; set; }

        ///<summary>Default maximum angular speed of the dynamic Rigidbody, in radians (default 50).</summary>
        ///<remarks>Controls the maximum angualr speed of the dynamic Rigidbody, measured in radians. The angular speed limit can also be modified with <see cref="Rigidbody.maxAngularVelocity" /> on a per rigid body basis.</remarks>
        extern static public float defaultMaxAngularSpeed { get; set; }
        ///<summary>Enables an improved patch friction mode that guarantees static and dynamic friction do not exceed analytical results.</summary>
        ///<remarks>This improved mode only applies when patch friction is enabled, otherwise it has no effect.
        ///
        ///The physics engine computes contact points for each pair of colliders that are in contact. From those contacts, the engine produces a set of up to two friction anchor points. With the flag set, the engine distributes the normal force between the friction anchors so that the total amount of friction applied does not exceed the analytical results.</remarks>
        extern static public bool improvedPatchFriction { get; set; }

        ///<summary>Whether or not <see cref="MonoBehaviour" /> collision messages will be sent by the physics system.</summary>
        ///<remarks>If this property is set to <c>true</c>, <see cref="M:UnityEngine.MonoBehaviour.OnCollisionEnter(UnityEngine.Collision)" />, <see cref="M:UnityEngine.MonoBehaviour.OnCollisionStay(UnityEngine.Collision)" />, and <see cref="M:UnityEngine.MonoBehaviour.OnCollisionExit(UnityEngine.Collision)" /> messages will be sent to the corresponding scripts that have these methods implemented.
        ///
        ///If this property is set to <c>false</c>, no <see cref="M:UnityEngine.MonoBehaviour.OnCollisionEnter(UnityEngine.Collision)" />, <see cref="M:UnityEngine.MonoBehaviour.OnCollisionStay(UnityEngine.Collision)" />, or <see cref="M:UnityEngine.MonoBehaviour.OnCollisionExit(UnityEngine.Collision)" /> messages will be sent. This can be beneficial when only the <see cref="Physics.ContactEvent" /> is used to read contacts as this will stop the physics system from iterating simulation results.
        ///
        ///Note: This does not affect trigger events.</remarks>
        extern static public bool invokeCollisionCallbacks { get; set; }

        ///<summary>Whether the physics system sends <c>OnTriggerStay</c> events.</summary>
        ///<remarks>Use this API to check whether <c>OnTriggerStay</c> events are enabled. This lets you add fallback paths to scripts that assume <c>OnTriggerStay</c> events are enabled.
        ///
        ///Physics backends typically only send events when a collider enters or exits a trigger. Unity's physics system keeps track of which colliders remain inside a trigger, and generates an <c>OnTriggerStay</c> event.
        ///
        ///However, generating <c>OnTriggerStay</c> events has some overhead. If you only make use of <c>OnTriggerEnter</c> and <c>OnTriggerExit</c>, you can turn this behaviour off in the Physics Settings. Disabling the generation of <c>OnTriggerStay</c> events provides performance benefits, especially for complex scenes.
        ///
        ///**Note**: This property is read-only. To change this setting, use Project Settings in the Editor.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///            using UnityEngine;
        ///
        ///            public class Example : MonoBehaviour
        ///            {
        ///                void Start()
        ///                {
        ///                    if (Physics.generateOnTriggerStayEvents)
        ///                        Debug.LogWarning("OnTriggerStay events are enabled. Consider disabling these from Physics Settings if you do not make use of OnTriggerStay, as it comes with performance gains.");
        ///                }
        ///            }
        ///]]></code>
        ///</example>
        extern static public bool generateOnTriggerStayEvents { get; }

        ///<summary>The <see cref="PhysicsScene" /> automatically created when Unity starts.</summary>
        ///<remarks>A default <see cref="PhysicsScene" /> is automatically created when Unity starts. It is used by any <see cref="UnityEngine.SceneManagement.Scene" /> that does not request a local 3D physics Scene.</remarks>
        public static PhysicsScene defaultPhysicsScene => PhysicsScene.GetDefaultScene();

        ///<summary>Makes the collision detection system ignore all collisions between <c>collider1</c> and <c>collider2</c>.</summary>
        ///<remarks>This is useful, say, for preventing projectiles from colliding with the object that fires them.
        ///
        ///Note that <see cref="IgnoreCollision" /> is not persistent. This means ignore collision state will not be stored in the editor when saving a scene.
        ///
        ///If <c>ignore</c> is false, collisions can occur. Set <c>ignore</c> to true to ignore collisions.</remarks>
        ///<param name="collider1">Any collider.</param>
        ///<param name="collider2">Another collider you want to have <c>collider1</c> to start or stop ignoring collisions with.</param>
        ///<param name="ignore">Whether or not the collisions between the two colliders should be ignored or not.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Transform bulletPrefab;
        ///
        ///    void Start()
        ///    {
        ///        Transform bullet = Instantiate(bulletPrefab) as Transform;
        ///        Physics.IgnoreCollision(bullet.GetComponent<Collider>(), GetComponent<Collider>());
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Physics.IgnoreLayerCollision" />
        extern public static void IgnoreCollision([NotNull] Collider collider1, [NotNull] Collider collider2, [DefaultValue("true")] bool ignore);

        [ExcludeFromDocs]
        public static void IgnoreCollision(Collider collider1, Collider collider2)
        {
            IgnoreCollision(collider1, collider2, true);
        }

        ///<summary>Makes the collision detection system ignore all collisions between any collider in <c>layer1</c> and any collider in <c>layer2</c>.
        ///
        ///Note that IgnoreLayerCollision will reset the trigger state of affected colliders, so you might receive OnTriggerExit and OnTriggerEnter messages in response to calling this.</summary>
        ///<remarks>You can set the default values for your project for any layer combinations in the Physics inspector.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// //Attach this script to a GameObject and make sure it has a Rigidbody component
        /// //Make a second GameObject with a Collider to test collisions on. Make sure both GameObjects are the same on the y and z axes
        ///
        /// //This script stops collisions between two layers (in this case layers 0 and 8). Set up a new layer in the Inspector window by clicking the Layer option.
        /// //Next click “Add Layer”. Then, assign this layer to the second GameObject.
        ///
        /// //In Play Mode, press the left and right keys to move the Rigidbody to the left and right. If your first GameObject is in layer 0 and your second GameObject is in layer 8, the collision is ignored.
        ///
        ///
        ///using UnityEngine;
        ///using UnityEngine.InputSystem;
        ///                        
        ///public class Example : MonoBehaviour
        ///{
        ///    //Set the speed number in the Inspector window
        ///    public float m_Speed;
        ///    Rigidbody m_Rigidbody;
        ///
        ///    void Start()
        ///    {
        ///        //Fetch the Rigidbody component from the GameObject
        ///        m_Rigidbody = GetComponent<Rigidbody>();
        ///        //Ignore the collisions between layer 0 (default) and layer 8 (custom layer you set in Inspector window)
        ///        Physics.IgnoreLayerCollision(0, 8);
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        //Press right to move the GameObject to the right. Make sure you set the speed high in the Inspector window.
        ///        if (Keyboard.current.rightArrowKey.isPressed)
        ///        {
        ///            m_Rigidbody.AddForce(Vector3.right * m_Speed);
        ///        }
        ///
        ///        //Press the left arrow key to move the GameObject to the left
        ///        if (Keyboard.current.leftArrowKey.isPressed)
        ///        {
        ///            m_Rigidbody.AddForce(Vector3.left * m_Speed);
        ///        }
        ///    }
        ///
        ///    //Detect when there is a collision
        ///    void OnCollisionStay(Collision collide)
        ///    {
        ///        //Output the name of the GameObject you collide with
        ///        Debug.Log("I hit the GameObject : " + collide.gameObject.name);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Physics.GetIgnoreLayerCollision" />
        ///<seealso cref="Physics.IgnoreCollision" />
        [NativeName("IgnoreCollision")]
        extern public static void IgnoreLayerCollision(int layer1, int layer2, [DefaultValue("true")] bool ignore);

        [ExcludeFromDocs]
        public static void IgnoreLayerCollision(int layer1, int layer2)
        {
            IgnoreLayerCollision(layer1, layer2, true);
        }

        ///<summary>Are collisions between <c>layer1</c> and <c>layer2</c> being ignored?</summary>
        ///<remarks>Returns the value set by <see cref="Physics.IgnoreLayerCollision" /> or in the Physics inspector.</remarks>
        extern public static bool GetIgnoreLayerCollision(int layer1, int layer2);

        ///<summary>Checks whether the collision detection system will ignore all collisions/triggers between <c>collider1</c> and <c>collider2</c> or not.</summary>
        ///<param name="collider1">The first collider to compare to <c>collider2</c>.</param>
        ///<param name="collider2">The second collider to compare to <c>collider1</c>.</param>
        ///<returns>Whether the collision detection system will ignore all collisions/triggers between <c>collider1</c> and <c>collider2</c> or not.</returns>
        extern public static bool GetIgnoreCollision([NotNull] Collider collider1, [NotNull] Collider collider2);
        ///<summary>Casts a ray, from point <c>origin</c>, in direction <c>direction</c>, of length <c>maxDistance</c>, against all colliders in the Scene.</summary>
        ///<remarks>
        ///  <para>To select which layers a ray should collide with, use a <see cref="LayerMask" />.
        ///
        ///Specifying <c>queryTriggerInteraction</c> allows you to control whether or not Trigger colliders generate a hit, or whether to use the global <see cref="Physics.queriesHitTriggers" /> setting.
        ///
        ///**Notes:** Raycasts will not detect Colliders for which the Raycast origin is inside the Collider. In all these examples <see cref="M:UnityEngine.MonoBehaviour.FixedUpdate" /> is used rather than <see cref="M:UnityEngine.MonoBehaviour.Update" />. Refer to [Order of execution for event functions](xref:execution-order) to understand the difference between <see cref="M:UnityEngine.MonoBehaviour.Update" /> and <see cref="M:UnityEngine.MonoBehaviour.FixedUpdate" />, and to see how they relate to physics queries.</para>
        ///  <para>This example creates a simple Raycast, projecting forwards from the position of the object's current position, extending for 10 units.</para>
        ///</remarks>
        ///<param name="origin">The starting point of the ray in world coordinates.</param>
        ///<param name="direction">The direction of the ray.</param>
        ///<param name="maxDistance">The max distance the ray should check for collisions.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layer mask) that is used to selectively filter which colliders are considered when casting a ray.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>Returns true if the ray intersects with a Collider, otherwise false.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    LayerMask layerMask;
        ///
        ///    void Awake()
        ///    {
        ///        layerMask = LayerMask.GetMask("Wall", "Character");
        ///    }
        ///
        ///    // See [[wiki:ExecutionOrder|Order of Execution for Event Functions]] for information on FixedUpdate() and Update() related to physics queries
        ///    void FixedUpdate()
        ///    {
        ///
        ///        RaycastHit hit;
        ///        // Does the ray intersect any objects excluding the player layer
        ///        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity, layerMask))
        ///
        ///        {
        ///            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);
        ///            Debug.Log("Did Hit");
        ///        }
        ///        else
        ///        {
        ///            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1000, Color.white);
        ///            Debug.Log("Did not Hit");
        ///        }
        ///
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    // See [[wiki:ExecutionOrder|Order of Execution for Event Functions]] for information on FixedUpdate() and Update() related to physics queries
        ///    void FixedUpdate()
        ///    {
        ///        Vector3 fwd = transform.TransformDirection(Vector3.forward);
        ///
        ///        if (Physics.Raycast(transform.position, fwd, 10))
        ///            print("There is something in front of the object!");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        static public bool Raycast(Vector3 origin, Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.Raycast(origin, direction, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, int layerMask)
        {
            return defaultPhysicsScene.Raycast(origin, direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance)
        {
            return defaultPhysicsScene.Raycast(origin, direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Vector3 origin, Vector3 direction)
        {
            return defaultPhysicsScene.Raycast(origin, direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        ///<summary>Casts a ray against all colliders in the Scene and returns detailed information on what was hit.</summary>
        ///<remarks>
        ///  <para>This example reports the distance between the current object and the reported Collider:</para>
        ///  <para>This example re-introduces the <c>maxDistance</c> parameter to limit how far ahead to cast the Ray:</para>
        ///</remarks>
        ///<param name="origin">The starting point of the ray in world coordinates.</param>
        ///<param name="direction">The direction of the ray.</param>
        ///<param name="hitInfo">If true is returned, <c>hitInfo</c> will contain more information about where the closest collider was hit. ().</param>
        ///<param name="maxDistance">The max distance the ray should check for collisions.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layer mask) that is used to selectively filter which colliders are considered when casting a ray.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>Returns true when the ray intersects any collider, otherwise false.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class RaycastExample : MonoBehaviour
        ///{
        ///    // See [[wiki:ExecutionOrder|Order of Execution for Event Functions]] for information on FixedUpdate() and Update() related to physics queries
        ///    void FixedUpdate()
        ///    {
        ///        RaycastHit hit;
        ///
        ///        if (Physics.Raycast(transform.position, -Vector3.up, out hit))
        ///            print("Found an object - distance: " + hit.distance);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class RaycastExample : MonoBehaviour
        ///{
        ///    // See [[wiki:ExecutionOrder|Order of Execution for Event Functions]] for information on FixedUpdate() and Update() related to physics queries
        ///    void FixedUpdate()
        ///    {
        ///        RaycastHit hit;
        ///
        ///        if (Physics.Raycast(transform.position, -Vector3.up, out hit, 100.0f))
        ///            print("Found an object - distance: " + hit.distance);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="RaycastHit" />
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.Raycast(origin, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        // This is not actually called by native code, but needs the [RequiredByNativeCode]
        // attribute as it is called by reflection from GraphicsRaycaster.cs, to avoid a hard
        // dependency to this module.
        [RequiredByNativeCode]
        [ExcludeFromDocs]
        static public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
            return defaultPhysicsScene.Raycast(origin, direction, out hitInfo, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance)
        {
            return defaultPhysicsScene.Raycast(origin, direction, out hitInfo, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo)
        {
            return defaultPhysicsScene.Raycast(origin, direction, out hitInfo, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        ///<summary>Same as above using /ray.origin/ and /ray.direction/ instead of <c>origin</c> and <c>direction</c>.</summary>
        ///<param name="ray">The starting point and direction of the ray.</param>
        ///<param name="maxDistance">The max distance the ray should check for collisions.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layer mask) that is used to selectively filter which colliders are considered when casting a ray.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>Returns true when the ray intersects any collider, otherwise false.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.InputSystem;
        ///                        
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    // See [[wiki:ExecutionOrder|Order of Execution for Event Functions]] for information on FixedUpdate() and Update() related to physics queries
        ///    void FixedUpdate()
        ///    {
        ///        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        ///        if (Physics.Raycast(ray, 100))
        ///            print("Hit something!");
        ///    }
        ///}
        ///]]></code>
        ///</example>
        static public bool Raycast(Ray ray, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Ray ray, float maxDistance, int layerMask)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Ray ray, float maxDistance)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Ray ray)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        ///<summary>Same as above using /ray.origin/ and /ray.direction/ instead of <c>origin</c> and <c>direction</c>.</summary>
        ///<remarks>This example draws a line along the length of the Ray whenever a collision is detected:</remarks>
        ///<param name="ray">The starting point and direction of the ray.</param>
        ///<param name="hitInfo">If true is returned, <c>hitInfo</c> will contain more information about where the closest collider was hit. ().</param>
        ///<param name="maxDistance">The max distance the ray should check for collisions.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layer mask) that is used to selectively filter which colliders are considered when casting a ray.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>Returns true when the ray intersects any collider, otherwise false.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.InputSystem;
        ///                        
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    // See [[wiki:ExecutionOrder|Order of Execution for Event Functions]] for information on FixedUpdate() and Update() related to physics queries
        ///    void FixedUpdate()
        ///    {
        ///        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        ///        RaycastHit hit;
        ///
        ///        if (Physics.Raycast(ray, out hit, 100))
        ///            Debug.DrawLine(ray.origin, hit.point);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="RaycastHit" />
        static public bool Raycast(Ray ray, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
            return Raycast(ray.origin, ray.direction, out hitInfo, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, out hitInfo, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Raycast(Ray ray, out RaycastHit hitInfo)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, out hitInfo, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        ///<summary>Returns true if there is any collider intersecting the line between <c>start</c> and <c>end</c>.</summary>
        ///<param name="start">Start point.</param>
        ///<param name="end">End point.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layer mask) that is used to selectively filter which colliders are considered when casting a ray.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Transform target;
        ///    void Update()
        ///    {
        ///        if (Physics.Linecast(transform.position, target.position))
        ///        {
        ///            Debug.Log("blocked");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        static public bool Linecast(Vector3 start, Vector3 end, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dir = end - start;
            return defaultPhysicsScene.Raycast(start, dir, dir.magnitude, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool Linecast(Vector3 start, Vector3 end, int layerMask)
        {
            return Linecast(start, end, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Linecast(Vector3 start, Vector3 end)
        {
            return Linecast(start, end, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        ///<summary>Returns true if there is any collider intersecting the line between <c>start</c> and <c>end</c>.</summary>
        ///<remarks>If true is returned, <c>hitInfo</c> will contain more information about where the collider was hit. ().
        ///[Layer mask](xref:Layers) is used to selectively ignore colliders when casting a ray.</remarks>
        ///<param name="start">Start point.</param>
        ///<param name="end">End point.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layer mask) that is used to selectively filter which colliders are considered when casting a ray.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<param name="hitInfo">If true is returned, <c>hitInfo</c> will contain more information about where the collider was hit. ().</param>
        ///<seealso cref="RaycastHit" />
        ///<seealso cref="RaycastHit" />
        static public bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dir = end - start;
            return defaultPhysicsScene.Raycast(start, dir, out hitInfo, dir.magnitude, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo, int layerMask)
        {
            return Linecast(start, end, out hitInfo, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool Linecast(Vector3 start, Vector3 end, out RaycastHit hitInfo)
        {
            return Linecast(start, end, out hitInfo, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        ///<summary>Casts a capsule against all colliders in the Scene and returns detailed information on what was hit.</summary>
        ///<remarks>The capsule is defined by the two spheres with <c>radius</c> around <c>point1</c> and <c>point2</c>, which form the two ends of the capsule.
        ///Hits are returned for the first collider which would collide against this capsule if the capsule was moved along <c>direction</c>.
        ///This is useful when a Raycast does not give enough precision, because you want to find out if an object of a specific size,
        ///such as a character, will be able to move somewhere without colliding with anything on the way.
        ///
        ///**Notes:** CapsuleCast will not detect colliders for which the capsule overlaps the collider. Passing a zero radius results in undefined output and doesn't always behave the same as <see cref="Physics.Raycast" />.</remarks>
        ///<param name="point1">The center of the sphere at the <c>start</c> of the capsule.</param>
        ///<param name="point2">The center of the sphere at the <c>end</c> of the capsule.</param>
        ///<param name="radius">The radius of the capsule.</param>
        ///<param name="direction">The direction into which to sweep the capsule.</param>
        ///<param name="maxDistance">The max length of the sweep.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a capsule.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>True if the capsule sweep intersects any collider, otherwise false.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Update()
        ///    {
        ///        RaycastHit hit;
        ///        CharacterController charContr = GetComponent<CharacterController>();
        ///        Vector3 p1 = transform.position + charContr.center + Vector3.up * -charContr.height * 0.5F;
        ///        Vector3 p2 = p1 + Vector3.up * charContr.height;
        ///        float distanceToObstacle = 0;
        ///
        ///        // Cast character controller shape 10 meters forward to see if it is about to hit anything.
        ///        if (Physics.CapsuleCast(p1, p2, charContr.radius, transform.forward, out hit, 10))
        ///            distanceToObstacle = hit.distance;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Physics.SphereCast" />
        ///<seealso cref="Physics.CapsuleCastAll" />
        ///<seealso cref="Physics.Raycast" />
        ///<seealso cref="Rigidbody.SweepTest" />
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            RaycastHit hit;
            return defaultPhysicsScene.CapsuleCast(point1, point2, radius, direction, out hit, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, int layerMask)
        {
            return CapsuleCast(point1, point2, radius, direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance)
        {
            return CapsuleCast(point1, point2, radius, direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction)
        {
            return CapsuleCast(point1, point2, radius, direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        ///<remarks>Casts a capsule along a ray and reports the first collider hit. In addition to checking for collisions, this overload provides detailed information about the collider hit via the out RaycastHit hitInfo parameter.
        ///                    The capsule is defined by two spheres of <c>radius</c> centered at <c>point1</c> and <c>point2</c>. These define the start and end of the capsule’s axis. The capsule is then swept in the <c>direction</c> for a maximum distance of maxDistance.
        ///                    This is particularly useful when you want to simulate movement of an object with volume (such as a character or a vehicle), and need to determine where, what, and how it hit something.</remarks>
        ///<param name="point1">The center of the sphere at the <c>start</c> of the capsule.</param>
        ///<param name="point2">The center of the sphere at the <c>end</c> of the capsule.</param>
        ///<param name="radius">The radius of the capsule.</param>
        ///<param name="direction">The direction into which to sweep the capsule.</param>
        ///<param name="maxDistance">The max length of the sweep.</param>
        ///<param name="hitInfo">If true is returned, <c>hitInfo</c> will contain more information about where the collider was hit. ().</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a capsule.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>True if the capsule sweep intersects any collider, otherwise false.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class CapsuleCastExample : MonoBehaviour
        ///{
        ///    public Transform pointStart;
        ///    public Transform pointEnd;
        ///    public float radius = 0.5f;
        ///    public float maxDistance = 10f;
        ///
        ///    void Update()
        ///    {
        ///        RaycastHit hit;
        ///        Vector3 direction = transform.forward;
        ///
        ///        if (Physics.CapsuleCast(pointStart.position, pointEnd.position, radius, direction, out hit, maxDistance))
        ///        {
        ///            Debug.Log("Hit: " + hit.collider.name + " at " + hit.point);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="RaycastHit" />
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.CapsuleCast(point1, point2, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
            return CapsuleCast(point1, point2, radius, direction, out hitInfo, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance)
        {
            return CapsuleCast(point1, point2, radius, direction, out hitInfo, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo)
        {
            return CapsuleCast(point1, point2, radius, direction, out hitInfo, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        ///<summary>Casts a sphere along a ray and returns detailed information on what was hit.</summary>
        ///<remarks>This is useful when a Raycast does not give enough precision, because you want to find out if an object of a specific size,
        ///such as a character, will be able to move somewhere without colliding with anything on the way.
        ///Think of the sphere cast like a thick raycast.  In this case the ray is specified by a start vector and a
        ///direction.
        ///
        ///**Notes:** SphereCast will not detect colliders for which the sphere overlaps the collider. Passing a zero radius results in undefined output and doesn't always behave the same as <see cref="Physics.Raycast" />.
        ///
        ///**Notes:** hit.normal from a <see cref="Physics.SphereCast" /> does not always represent the surface normal. It is often the direction from the contact point to the center of the sphere. This can be misleading if you're using it for sliding, bouncing, or aligning objects. Consider using a <see cref="Physics.Raycast" /> if you need the true surface normal.</remarks>
        ///<param name="origin">The center of the sphere at the start of the sweep.</param>
        ///<param name="radius">The radius of the sphere.</param>
        ///<param name="direction">The direction into which to sweep the sphere.</param>
        ///<param name="hitInfo">If true is returned, <c>hitInfo</c> will contain more information about where the collider was hit. ().</param>
        ///<param name="maxDistance">The max length of the cast.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a sphere.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>True when the sphere sweep intersects any collider, otherwise false.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    CharacterController charCtrl;
        ///
        ///    void Start()
        ///    {
        ///        charCtrl = GetComponent<CharacterController>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        RaycastHit hit;
        ///
        ///        Vector3 p1 = transform.position + charCtrl.center;
        ///        float distanceToObstacle = 0;
        ///
        ///        // Cast a sphere wrapping character controller 10 meters forward
        ///        // to see if it is about to hit anything.
        ///        if (Physics.SphereCast(p1, charCtrl.height / 2, transform.forward, out hit, 10))
        ///        {
        ///            distanceToObstacle = hit.distance;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Physics.SphereCastAll" />
        ///<seealso cref="Physics.CapsuleCast" />
        ///<seealso cref="Physics.Raycast" />
        ///<seealso cref="Rigidbody.SweepTest" />
        ///<seealso cref="RaycastHit" />
        static public bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.SphereCast(origin, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
            return SphereCast(origin, radius, direction, out hitInfo, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance)
        {
            return SphereCast(origin, radius, direction, out hitInfo, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo)
        {
            return SphereCast(origin, radius, direction, out hitInfo, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        ///<summary>Casts a sphere along a ray and returns detailed information on what was hit.</summary>
        ///<remarks>This is useful when a Raycast does not give enough precision, because you want to find out if an object of a specific size,
        ///such as a character, will be able to move somewhere without colliding with anything on the way.
        ///Think of the sphere cast like a thick raycast.</remarks>
        ///<param name="ray">The starting point and direction of the ray into which the sphere sweep is cast.</param>
        ///<param name="radius">The radius of the sphere.</param>
        ///<param name="maxDistance">The max length of the cast.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a sphere.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>True when the sphere sweep intersects any collider, otherwise false.</returns>
        ///<seealso cref="Physics.SphereCastAll" />
        ///<seealso cref="Physics.CapsuleCast" />
        ///<seealso cref="Physics.Raycast" />
        ///<seealso cref="Rigidbody.SweepTest" />
        static public bool SphereCast(Ray ray, float radius, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            RaycastHit hitInfo;
            return SphereCast(ray.origin, radius, ray.direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Ray ray, float radius, float maxDistance, int layerMask)
        {
            return SphereCast(ray, radius, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Ray ray, float radius, float maxDistance)
        {
            return SphereCast(ray, radius, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Ray ray, float radius)
        {
            return SphereCast(ray, radius, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        ///<param name="ray">The starting point and direction of the ray into which the sphere sweep is cast.</param>
        ///<param name="radius">The radius of the sphere.</param>
        ///<param name="hitInfo">If true is returned, <c>hitInfo</c> will contain more information about where the collider was hit. ().</param>
        ///<param name="maxDistance">The max length of the cast.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a sphere.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<seealso cref="RaycastHit" />
        static public bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return SphereCast(ray.origin, radius, ray.direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, float maxDistance, int layerMask)
        {
            return SphereCast(ray, radius, out hitInfo, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo, float maxDistance)
        {
            return SphereCast(ray, radius, out hitInfo, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool SphereCast(Ray ray, float radius, out RaycastHit hitInfo)
        {
            return SphereCast(ray, radius, out hitInfo, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        ///<summary>Casts the box along a ray and returns detailed information on what was hit.</summary>
        ///<param name="center">Center of the box.</param>
        ///<param name="halfExtents">Half the size of the box in each dimension.</param>
        ///<param name="direction">The direction in which to cast the box.</param>
        ///<param name="orientation">Rotation of the box.</param>
        ///<param name="maxDistance">The max length of the cast.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a box.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>True, if any intersections were found.</returns>
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            RaycastHit hitInfo;
            return defaultPhysicsScene.BoxCast(center, halfExtents, direction, out hitInfo, orientation, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, int layerMask)
        {
            return BoxCast(center, halfExtents, direction, orientation, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance)
        {
            return BoxCast(center, halfExtents, direction, orientation, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation)
        {
            return BoxCast(center, halfExtents, direction, orientation, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction)
        {
            return BoxCast(center, halfExtents, direction, Quaternion.identity, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        ///<summary>Casts the box along a ray and returns detailed information on what was hit.</summary>
        ///<param name="center">Center of the box.</param>
        ///<param name="halfExtents">Half the size of the box in each dimension.</param>
        ///<param name="direction">The direction in which to cast the box.</param>
        ///<param name="hitInfo">If true is returned, <c>hitInfo</c> will contain more information about where the collider was hit. ().</param>
        ///<param name="orientation">Rotation of the box.</param>
        ///<param name="maxDistance">The max length of the cast.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a box.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>True, if any intersections were found.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// //Attach this script to a GameObject. Make sure it has a Collider component by clicking the __Add Component__ button. Then click __Physics__>__Box Collider__ to attach a Box Collider component.
        /// //This script creates a BoxCast in front of the GameObject and outputs a message if another Collider is hit with the Collider’s name.
        /// //It also draws where the ray and BoxCast extends to. Just press the Gizmos button to see it in Play Mode.
        /// //Make sure to have another GameObject with a Collider component for the BoxCast to collide with.
        ///
        ///using UnityEngine;
        ///
        ///[RequireComponent(typeof(Collider))]
        ///public class Example : MonoBehaviour
        ///{
        ///    [SerializeField] private float maxDistance = 300f;
        ///    [SerializeField] private Vector3 boxSizeMultiplier = Vector3.one * 0.5f;
        ///
        ///    private Collider col;
        ///    private RaycastHit hit;
        ///    private bool hitDetected;
        ///    
        ///    void Awake()
        ///    {
        ///        col = GetComponent<Collider>();
        ///    }
        ///    
        ///    void FixedUpdate()
        ///    {
        ///        Vector3 halfExtents = Vector3.Scale(transform.localScale, boxSizeMultiplier);
        ///        Vector3 origin = col.bounds.center;
        ///        Vector3 direction = transform.forward;
        ///        
        ///        //Test to see if there is a hit using a BoxCast
        ///        //Calculate using the center of the GameObject's Collider(could also just use the GameObject's position), half the GameObject's size, the direction, the GameObject's rotation, and the maximum distance as variables.
        ///        //Also fetch the hit data
        ///        hitDetected = Physics.BoxCast(
        ///            origin, 
        ///            halfExtents, 
        ///            direction,
        ///            out hit, 
        ///            transform.rotation,
        ///            maxDistance);
        ///        
        ///        if (hitDetected)
        ///        {
        ///            //Output the name of the Collider your Box hit
        ///            Debug.Log("Hit : " + hit.collider.name);
        ///        }
        ///    }
        ///
        ///    //Draw the BoxCast as a gizmo to show where it currently is testing. Click the Gizmos button to see this
        ///    void OnDrawGizmos()
        ///    {
        ///        Gizmos.color = Color.red;
        ///        
        ///        if (hitDetected)
        ///        {
        ///            //Draw a Ray forward from GameObject toward the hit
        ///            Gizmos.DrawRay(transform.position, transform.forward * hit.distance);
        ///            //Draw a cube that extends to where the hit exists
        ///            Gizmos.DrawWireCube(transform.position + transform.forward * hit.distance, transform.localScale);
        ///        }
        ///        else
        ///        {
        ///            //Draw a Ray forward from GameObject toward the maximum distance
        ///            Gizmos.DrawRay(transform.position, transform.forward * maxDistance);
        ///            //Draw a cube at the maximum distance
        ///            Gizmos.DrawWireCube(transform.position + transform.forward * maxDistance, transform.localScale);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="RaycastHit" />
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.BoxCast(center, halfExtents, direction, out hitInfo, orientation, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, Quaternion orientation, float maxDistance, int layerMask)
        {
            return BoxCast(center, halfExtents, direction, out hitInfo, orientation, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, Quaternion orientation, float maxDistance)
        {
            return BoxCast(center, halfExtents, direction, out hitInfo, orientation, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, Quaternion orientation)
        {
            return BoxCast(center, halfExtents, direction, out hitInfo, orientation, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo)
        {
            return BoxCast(center, halfExtents, direction, out hitInfo, Quaternion.identity, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [FreeFunction("Physics::RaycastAll")]
        extern static RaycastHit[] Internal_RaycastAll(PhysicsScene physicsScene, Ray ray, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        ///<remarks>See example above.</remarks>
        ///<param name="origin">The starting point of the ray in world coordinates.</param>
        ///<param name="direction">The direction of the ray.</param>
        ///<param name="maxDistance">The max distance the rayhit is allowed to be from the start of the ray.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layer mask) that is used to selectively filter which colliders are considered when casting a ray.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<seealso cref="Raycast" />
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;
                Ray ray = new Ray(origin, normalizedDirection);
                return Internal_RaycastAll(defaultPhysicsScene, ray, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return Array.Empty<RaycastHit>();
            }
        }

        [ExcludeFromDocs]
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance, int layerMask)
        {
            return RaycastAll(origin, direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance)
        {
            return RaycastAll(origin, direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction)
        {
            return RaycastAll(origin, direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        ///<summary>Casts a ray through the Scene and returns all hits. Note that order of the results is undefined.</summary>
        ///<remarks>
        ///  <para />
        ///  <para>**Notes:** Raycasts will not detect colliders for which the raycast origin is inside the collider.</para>
        ///</remarks>
        ///<param name="ray">The starting point and direction of the ray.</param>
        ///<param name="maxDistance">The max distance the rayhit is allowed to be from the start of the ray.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layer mask) that is used to selectively filter which colliders are considered when casting a ray.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>An array of RaycastHit objects. Note that the order of the results is undefined.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Update()
        ///    {
        ///        RaycastHit[] hits;
        ///        hits = Physics.RaycastAll(transform.position, transform.forward, 100.0F);
        ///
        ///        for (int i = 0; i < hits.Length; i++)
        ///        {
        ///            RaycastHit hit = hits[i];
        ///            Renderer rend = hit.transform.GetComponent<Renderer>();
        ///
        ///            if (rend)
        ///            {
        ///                // Change the material of all hit colliders
        ///                // to use a transparent shader.
        ///                rend.material.shader = Shader.Find("Transparent/Diffuse");
        ///                Color tempColor = rend.material.color;
        ///                tempColor.a = 0.3F;
        ///                rend.material.color = tempColor;
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Raycast" />
        static public RaycastHit[] RaycastAll(Ray ray, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return RaycastAll(ray.origin, ray.direction, maxDistance, layerMask, queryTriggerInteraction);
        }

        // This is not actually called by native code, but needs the [RequiredByNativeCode]
        // attribute as it is called by reflection from GraphicsRaycaster.cs, to avoid a hard
        // dependency to this module.
        [RequiredByNativeCode]
        [ExcludeFromDocs]
        static public RaycastHit[] RaycastAll(Ray ray, float maxDistance, int layerMask)
        {
            return RaycastAll(ray.origin, ray.direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] RaycastAll(Ray ray, float maxDistance)
        {
            return RaycastAll(ray.origin, ray.direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] RaycastAll(Ray ray)
        {
            return RaycastAll(ray.origin, ray.direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        ///<summary>Cast a ray through the Scene and store the hits into the buffer.</summary>
        ///<remarks>Like <see cref="Physics.RaycastAll" />, but generates no garbage.
        ///
        ///The raycast query ends when there are no more hits and/or the results buffer is full. The order of the results is undefined. When a full buffer is returned it is not guaranteed that the results are the closest hits and the length of the buffer is returned. If a null buffer is passed in, no results are returned and no errors or exceptions are thrown.</remarks>
        ///<param name="ray">The starting point and direction of the ray.</param>
        ///<param name="results">The buffer to store the hits into.</param>
        ///<param name="maxDistance">The max distance the rayhit is allowed to be from the start of the ray.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layer mask) that is used to selectively filter which colliders are considered when casting a ray.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>The amount of hits stored into the <c>results</c> buffer.</returns>
        static public int RaycastNonAlloc(Ray ray, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, results, maxDistance, layerMask, queryTriggerInteraction);
        }

        // This is not actually called by native code, but needs the [RequiredByNativeCode]
        // attribute as it is called by reflection from GraphicsRaycaster.cs, to avoid a hard
        // dependency to this module.
        [RequiredByNativeCode]
        [ExcludeFromDocs]
        static public int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance, int layerMask)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, results, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int RaycastNonAlloc(Ray ray, RaycastHit[] results, float maxDistance)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, results, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int RaycastNonAlloc(Ray ray, RaycastHit[] results)
        {
            return defaultPhysicsScene.Raycast(ray.origin, ray.direction, results, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        ///<summary>Cast a ray through the Scene and store the hits into the buffer.</summary>
        ///<param name="origin">The starting point and direction of the ray.</param>
        ///<param name="results">The buffer to store the hits into.</param>
        ///<param name="direction">The direction of the ray.</param>
        ///<param name="maxDistance">The max distance the rayhit is allowed to be from the start of the ray.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layer mask) that is used to selectively filter which colliders are considered when casting a ray.</param>
        ///<returns>The amount of hits stored into the <c>results</c> buffer.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class RaycastFanNonAlloc : MonoBehaviour
        ///{
        ///    public int rayCount = 10;                       // Number of rays in the fan
        ///    public float angle = 60f;                       // Total spread angle in degrees
        ///    public float maxDistance = 20f;                 // Ray length
        ///
        ///    // The size of the array determines how many raycasts will occur
        ///    RaycastHit[] m_Results = new RaycastHit[5];     // Reused buffer to avoid GC allocations
        ///
        ///    // See [[wiki:ExecutionOrder|Order of Execution for Event Functions]] for information on FixedUpdate() and Update() related to physics queries
        ///    void FixedUpdate()
        ///    {
        ///        Vector3 origin = transform.position;
        ///        Vector3 forward = transform.forward;
        ///        float halfAngle = angle / 2f;
        ///
        ///        for (int i = 0; i < rayCount; i++)
        ///        {
        ///            // Interpolate angle across the spread range
        ///            float lerp = (float)i / (rayCount - 1);
        ///            float currentAngle = Mathf.Lerp(-halfAngle, halfAngle, lerp);
        ///
        ///            // Rotate direction around Y axis
        ///            Quaternion rotation = Quaternion.AngleAxis(currentAngle, Vector3.up);
        ///            Vector3 direction = rotation * forward;
        ///
        ///            // Note: The buffer is overwritten from index 0 up to the number of hits returned. Unused slots remain unchanged.
        ///            int hits = Physics.RaycastNonAlloc(origin, direction, m_Results, maxDistance);
        ///
        ///            if (hits > 0)
        ///            {
        ///                for (int j = 0; j < hits; j++)
        ///                {
        ///                    Debug.Log($"Ray {i} hit {m_Results[j].collider.gameObject.name}");
        ///                    Debug.DrawLine(origin, m_Results[j].point, Color.green);
        ///                }
        ///            }
        ///            else
        ///            {
        ///                Debug.DrawRay(origin, direction * maxDistance, Color.red);
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        static public int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.Raycast(origin, direction, results, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask)
        {
            return defaultPhysicsScene.Raycast(origin, direction, results, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, float maxDistance)
        {
            return defaultPhysicsScene.Raycast(origin, direction, results, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results)
        {
            return defaultPhysicsScene.Raycast(origin, direction, results, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [FreeFunction("Physics::CapsuleCastAll")]
        extern private static RaycastHit[] Query_CapsuleCastAll(PhysicsScene physicsScene, Vector3 p0, Vector3 p1, float radius, Vector3 direction, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        ///<summary>Like <see cref="Physics.CapsuleCast" />, but this function will return all hits the capsule sweep intersects.</summary>
        ///<remarks>Casts a capsule against all colliders in the Scene and returns detailed information on each collider which was hit.
        ///The capsule is defined by the two spheres with <c>radius</c> around <c>point1</c> and <c>point2</c>, which form the two ends of the capsule.
        ///Hits are returned all colliders which would collide against this capsule if the capsule was moved along <c>direction</c>.
        ///This is useful when a Raycast does not give enough precision, because you want to find out if an object of a specific size,
        ///such as a character, will be able to move somewhere without colliding with anything on the way.
        ///
        ///**Notes:** For colliders that overlap the capsule at the start of the sweep,  <see cref="RaycastHit.normal" /> is set opposite to the direction of the sweep, <see cref="RaycastHit.distance" /> is set to zero, and the zero vector gets returned in <see cref="RaycastHit.point" />.  You might want to check whether this is the case in your particular query and perform additional queries to refine the result. Passing a zero radius results in undefined output and doesn't always behave the same as <see cref="Physics.Raycast" />.</remarks>
        ///<param name="point1">The center of the sphere at the <c>start</c> of the capsule.</param>
        ///<param name="point2">The center of the sphere at the <c>end</c> of the capsule.</param>
        ///<param name="radius">The radius of the capsule.</param>
        ///<param name="direction">The direction into which to sweep the capsule.</param>
        ///<param name="maxDistance">The max length of the sweep.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a capsule.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>An array of all colliders hit in the sweep.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Update()
        ///    {
        ///        RaycastHit[] hits;
        ///        CharacterController charCtrl = GetComponent<CharacterController>();
        ///        Vector3 p1 = transform.position + charCtrl.center + Vector3.up * -charCtrl.height * 0.5F;
        ///        Vector3 p2 = p1 + Vector3.up * charCtrl.height;
        ///
        ///        // Cast character controller shape 10 meters forward, to see if it is about to hit anything
        ///        hits = Physics.CapsuleCastAll(p1, p2, charCtrl.radius, transform.forward, 10);
        ///
        ///        // Change the material of all hit colliders
        ///        // to use a transparent Shader
        ///        for (int i = 0; i < hits.Length; i++)
        ///        {
        ///            RaycastHit hit = hits[i];
        ///            Renderer rend = hit.transform.GetComponent<Renderer>();
        ///
        ///            if (rend)
        ///            {
        ///                rend.material.shader = Shader.Find("Transparent/Diffuse");
        ///                Color tempColor = rend.material.color;
        ///                tempColor.a = 0.3F;
        ///                rend.material.color = tempColor;
        ///            }
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Physics.SphereCast" />
        ///<seealso cref="Physics.CapsuleCast" />
        ///<seealso cref="Physics.Raycast" />
        ///<seealso cref="Rigidbody.SweepTest" />
        public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;

                return Query_CapsuleCastAll(defaultPhysicsScene, point1, point2, radius, normalizedDirection, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return Array.Empty<RaycastHit>();
            }
        }

        [ExcludeFromDocs]
        public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, int layerMask)
        {
            return CapsuleCastAll(point1, point2, radius, direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance)
        {
            return CapsuleCastAll(point1, point2, radius, direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] CapsuleCastAll(Vector3 point1, Vector3 point2, float radius, Vector3 direction)
        {
            return CapsuleCastAll(point1, point2, radius, direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [FreeFunction("Physics::SphereCastAll")]
        extern private static RaycastHit[] Query_SphereCastAll(PhysicsScene physicsScene, Vector3 origin, float radius, Vector3 direction, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        ///<summary>Like <see cref="Physics.SphereCast" />, but this function will return all hits the sphere sweep intersects.</summary>
        ///<remarks>Casts a sphere against all colliders in the Scene and returns detailed information on each collider which was hit.
        ///This is useful when a Raycast does not give enough precision, because you want to find out if an object of a specific size,
        ///such as a character, will be able to move somewhere without colliding with anything on the way.
        ///
        ///**Notes:** For colliders that overlap the sphere at the start of the sweep,  <see cref="RaycastHit.normal" /> is set opposite to the direction of the sweep, <see cref="RaycastHit.distance" /> is set to zero, and the zero vector gets returned in <see cref="RaycastHit.point" />. You might want to check whether this is the case in your particular query and perform additional queries to refine the result. Passing a zero radius results in undefined output and doesn't always behave the same as <see cref="Physics.Raycast" />.</remarks>
        ///<param name="origin">The center of the sphere at the start of the sweep.</param>
        ///<param name="radius">The radius of the sphere.</param>
        ///<param name="direction">The direction in which to sweep the sphere.</param>
        ///<param name="maxDistance">The max length of the sweep.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a sphere.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>An array of all colliders hit in the sweep.</returns>
        ///<seealso cref="Physics.SphereCast" />
        ///<seealso cref="Physics.CapsuleCast" />
        ///<seealso cref="Physics.Raycast" />
        ///<seealso cref="Rigidbody.SweepTest" />
        public static RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;

                return Query_SphereCastAll(defaultPhysicsScene, origin, radius, normalizedDirection, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return Array.Empty<RaycastHit>();
            }
        }

        [ExcludeFromDocs]
        static public RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction, float maxDistance, int layerMask)
        {
            return SphereCastAll(origin, radius, direction, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction, float maxDistance)
        {
            return SphereCastAll(origin, radius, direction, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] SphereCastAll(Vector3 origin, float radius, Vector3 direction)
        {
            return SphereCastAll(origin, radius, direction, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        ///<summary>Like <see cref="Physics.SphereCast" />, but this function will return all hits the sphere sweep intersects.</summary>
        ///<param name="ray">The starting point and direction of the ray into which the sphere sweep is cast.</param>
        ///<param name="radius">The radius of the sphere.</param>
        ///<param name="maxDistance">The max length of the sweep.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a sphere.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        static public RaycastHit[] SphereCastAll(Ray ray, float radius, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return SphereCastAll(ray.origin, radius, ray.direction, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] SphereCastAll(Ray ray, float radius, float maxDistance, int layerMask)
        {
            return SphereCastAll(ray, radius, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] SphereCastAll(Ray ray, float radius, float maxDistance)
        {
            return SphereCastAll(ray, radius, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public RaycastHit[] SphereCastAll(Ray ray, float radius)
        {
            return SphereCastAll(ray, radius, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [FreeFunction("Physics::OverlapCapsule")]
        extern private static Collider[] OverlapCapsule_Internal(PhysicsScene physicsScene, Vector3 point0, Vector3 point1, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
        ///<summary>Check the given capsule against the physics world and return all overlapping colliders.</summary>
        ///<param name="point0">The center of the sphere at the <c>start</c> of the capsule.</param>
        ///<param name="point1">The center of the sphere at the <c>end</c> of the capsule.</param>
        ///<param name="radius">The radius of the capsule.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a capsule.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>Colliders touching or inside the capsule.</returns>
        public static Collider[] OverlapCapsule(Vector3 point0, Vector3 point1, float radius, [DefaultValue("AllLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return OverlapCapsule_Internal(defaultPhysicsScene, point0, point1, radius, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapCapsule(Vector3 point0, Vector3 point1, float radius, int layerMask)
        {
            return OverlapCapsule(point0, point1, radius, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapCapsule(Vector3 point0, Vector3 point1, float radius)
        {
            return OverlapCapsule(point0, point1, radius, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        [FreeFunction("Physics::OverlapSphere")]
        extern private static Collider[] OverlapSphere_Internal(PhysicsScene physicsScene, Vector3 position, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
        ///<summary>Computes and stores colliders touching or inside the sphere.</summary>
        ///<remarks>
        ///  <para>
        ///Allocates memory. Consider using <see cref="Physics.OverlapSphereNonAlloc" /> instead.</para>
        ///  <para />
        ///</remarks>
        ///<param name="position">Center of the sphere.</param>
        ///<param name="radius">Radius of the sphere.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layer mask) defines which layers of colliders to include in the query.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>Returns an array with all colliders touching or inside the sphere.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void ExplosionDamage(Vector3 center, float radius)
        ///    {
        ///        Collider[] hitColliders = Physics.OverlapSphere(center, radius);
        ///        foreach (var hitCollider in hitColliders)
        ///        {
        ///            hitCollider.SendMessage("AddDamage");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Physics.AllLayers" />
        ///<seealso href="xref:use-layers "> Use of layers in Unity</seealso>
        public static Collider[] OverlapSphere(Vector3 position, float radius, [DefaultValue("AllLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return OverlapSphere_Internal(defaultPhysicsScene, position, radius, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapSphere(Vector3 position, float radius, int layerMask)
        {
            return OverlapSphere(position, radius, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapSphere(Vector3 position, float radius)
        {
            return OverlapSphere(position, radius, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        [NativeName("Simulate")]
        extern internal static void Simulate_Internal(PhysicsScene physicsScene, float step, SimulationStage stages, SimulationOption options);

        ///<summary>Simulate physics in the Scene.</summary>
        ///<remarks>Call this to simulate physics manually when the simulation mode is set to Script. Simulation includes all the stages of collision detection, rigidbody and joints integration, and filing of the physics callbacks (contact, trigger and joints). Calling Physics.Simulate does not cause FixedUpdate to be called. <see cref="M:UnityEngine.MonoBehaviour.FixedUpdate" /> will still be called at the rate defined by <see cref="Time.fixedDeltaTime" /> whether simulation mode is set to Script or not, and regardless of when you call Physics.Simulate.
        ///
        ///Note that if you pass framerate-dependent step values (such as <see cref="Time.deltaTime" />) to the physics engine, your simulation will be non-deterministic because of the unpredictable fluctuations in framerate that can arise.
        ///
        ///To achieve deterministic physics results, you should pass a fixed step value to Physics.Simulate every time you call it. Usually, <c>step</c> should be a small positive number. Using <c>step</c> values greater than 0.03 is likely to produce inaccurate results.
        ///
        ///
        ///
        ///Here is an example of a basic simulation that implements what's being done in the <see cref="SimulationMode.FixedUpdate" /> simulation mode (excluding <see cref="Time.maximumDeltaTime" />).</remarks>
        ///<param name="step">The time to advance physics by.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class BasicSimulation : MonoBehaviour
        ///{
        ///    private float timer;
        ///
        ///    void Update()
        ///    {
        ///        if (Physics.simulationMode != SimulationMode.Script)
        ///            return; // do nothing if the automatic simulation is enabled
        ///
        ///        timer += Time.deltaTime;
        ///
        ///        // Catch up with the game time.
        ///        // Advance the physics simulation in portions of Time.fixedDeltaTime
        ///        // Note that generally, we don't want to pass variable delta to Simulate as that leads to unstable results.
        ///        while (timer >= Time.fixedDeltaTime)
        ///        {
        ///            timer -= Time.fixedDeltaTime;
        ///            Physics.Simulate(Time.fixedDeltaTime);
        ///        }
        ///
        ///        // Here you can access the transforms state right after the simulation, if needed
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Physics.simulationMode" />
        ///<seealso cref="SimulationMode" />
        public static void Simulate(float step)
        {
            if (simulationMode != SimulationMode.Script)
            {
                Debug.LogWarning("Physics.Simulate(...) was called but simulation mode is not set to Script. You should set simulation mode to Script first before calling this function therefore the simulation was not run.");
                return;
            }

            Simulate_Internal(defaultPhysicsScene, step, SimulationStage.All, SimulationOption.All);
        }

        [NativeName("InterpolateBodies")]
        extern internal static void InterpolateBodies_Internal(PhysicsScene physicsScene);

        [NativeName("ResetInterpolatedTransformPosition")]
        extern internal static void ResetInterpolationPoses_Internal(PhysicsScene physicsScene);

        ///<summary>Apply Transform changes to the physics engine.</summary>
        ///<remarks>When a <see cref="Transform" /> component changes, any <see cref="Rigidbody" /> or <see cref="Collider" /> on that <see cref="Transform" /> or its children may need to be repositioned, rotated or scaled depending on the change to the <see cref="Transform" />. Use this function to flush those changes to the physics engine manually.
        ///
        /// Appropriate usage scenarios:
        ///* Don’t use it in FixedUpdate() — Unity already syncs transforms automatically before each physics step.
        ///* Do use it after Transform changes in Update()/LateUpdate() if you’re immediately performing a physics query.
        ///* Don’t overuse it — it’s expensive if called every frame unnecessarily.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class SyncTransformExample : MonoBehaviour
        ///{
        ///    public Transform target;
        ///
        ///    void Update()
        ///    {
        ///        // 1. Move a target object by setting its transform.position directly.
        ///        target.position = new Vector3(5, 0, 0);
        ///
        ///        // 2. Immediately tell Unity's physics engine to update its internal representation
        ///        //    of all transforms (including the one just moved).
        ///        Physics.SyncTransforms();
        ///
        ///        // 3. Run a physics query — in this case, check if a sphere with radius 0.5
        ///        //    centered at the new target position overlaps any colliders.
        ///        if (Physics.CheckSphere(target.position, 0.5f))
        ///        {
        ///            Debug.Log("Something is overlapping the moved object!");
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        extern public static void SyncTransforms();
        ///<summary>Determines whether the garbage collector should reuse only a single instance of a Collision type for all collision callbacks.</summary>
        ///<remarks>When an <see cref="M:UnityEngine.MonoBehaviour.OnCollisionEnter(UnityEngine.Collision)" />, <see cref="M:UnityEngine.MonoBehaviour.OnCollisionStay(UnityEngine.Collision)" /> or <see cref="M:UnityEngine.MonoBehaviour.OnCollisionExit(UnityEngine.Collision)" /> collision callback occurs, the <see cref="Collision" /> object passed to it is created for each individual callback. This means the garbage collector has to remove each object, which reduces performance.
        ///
        ///When this option is true, only a single instance of the <see cref="Collision" /> type is created and reused for each individual callback. This reduces waste for the garbage collector to handle and improves performance.
        ///
        ///You would only set this option to false if the <see cref="Collision" /> object is referenced outside of the collision callback for processing later, so recycling the <see cref="Collision" /> object is not desired.</remarks>
        extern public static bool reuseCollisionCallbacks { get; set; }

        [FreeFunction("Physics::ComputePenetration")]
        extern private static bool Query_ComputePenetration([NotNull] Collider colliderA, Vector3 positionA, Quaternion rotationA, [NotNull] Collider colliderB, Vector3 positionB, Quaternion rotationB, ref Vector3 direction, ref float distance);

        ///<summary>Compute the minimal translation required to separate the given colliders apart at specified poses.</summary>
        ///<remarks>Translating the first collider by direction * distance will separate the colliders apart if the function returned true. Otherwise, direction and distance are not defined.
        ///
        ///One of the colliders has to be BoxCollider, SphereCollider CapsuleCollider or a convex MeshCollider. The other one can be any type.
        ///
        ///Note that you aren't restricted to the position and rotation the colliders have at the moment of the call. Passing position or rotation that is different from the currently set one doesn't have an effect of physically moving any colliders thus has no side effects on the Scene.
        ///
        ///Doesn't depend on any spatial structures to be updated first, so is not bound to be used only within FixedUpdate timeframe.
        ///
        ///Ignores backfaced triangles and doesn't respect <see cref="Physics.queriesHitBackfaces" />.
        ///
        ///This function is useful to write custom depenetration functions. One particular example is an implementation of a character controller where a specific reaction to collision with the surrounding physics objects is required. In this case, one would first query for the colliders nearby using OverlapSphere and then adjust the character's position using the data returned by ComputePenetration.</remarks>
        ///<param name="colliderA">The first collider.</param>
        ///<param name="positionA">Position of the first collider.</param>
        ///<param name="rotationA">Rotation of the first collider.</param>
        ///<param name="colliderB">The second collider.</param>
        ///<param name="positionB">Position of the second collider.</param>
        ///<param name="rotationB">Rotation of the second collider.</param>
        ///<param name="direction">Direction along which the translation required to separate the colliders apart is minimal.</param>
        ///<param name="distance">The distance along direction that is required to separate the colliders apart.</param>
        ///<returns>True, if the colliders overlap at the given poses.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        /// // Visualizes the minimum translation vectors (MTV) required to separate this object from overlapping colliders within a given radius.
        /// // Attach to a GameObject with a Collider component.
        /// // Note: To compute physics penetration both colliders have to be enabled.
        ///[ExecuteInEditMode]
        ///public class ShowPenetration : MonoBehaviour
        ///{
        ///    // Radius in which to search for overlapping colliders
        ///    private float radius = 3f;
        ///
        ///    // Maximum number of neighbors to check for overlap
        ///    private int maxNeighbours = 16;
        ///
        ///    private Collider[] neighbours;
        ///    private Collider thisCollider;
        ///
        ///    private void OnEnable()
        ///    {
        ///        neighbours = new Collider[maxNeighbours];
        ///        thisCollider = GetComponent<Collider>();
        ///
        ///        if (!thisCollider)
        ///            Debug.LogWarning($"{nameof(ShowPenetration)} requires a Collider component.", this);
        ///    }
        ///
        ///    private void OnDrawGizmos()
        ///    {
        ///        if (thisCollider == null)
        ///            return;
        ///
        ///        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, neighbours);
        ///
        ///        for (int i = 0; i < count; ++i)
        ///        {
        ///            Collider other = neighbours[i];
        ///            if (!other || other == thisCollider)
        ///                continue;
        ///
        ///            if (Physics.ComputePenetration(
        ///                    thisCollider, transform.position, transform.rotation,
        ///                    other, other.transform.position, other.transform.rotation,
        ///                    out Vector3 direction, out float distance))
        ///            {
        ///                // Draw penetration vector starting from the other collider's position.
        ///                Gizmos.color = Color.red;
        ///                Gizmos.DrawRay(other.transform.position, direction * distance);
        ///            }
        ///        }
        ///    }
        ///
        ///    private void OnValidate()
        ///    {
        ///        radius = Mathf.Max(0f, radius);
        ///        maxNeighbours = Mathf.Clamp(maxNeighbours, 1, 256);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool ComputePenetration(Collider colliderA, Vector3 positionA, Quaternion rotationA, Collider colliderB, Vector3 positionB, Quaternion rotationB, out Vector3 direction, out float distance)
        {
            direction = Vector3.zero;
            distance = 0f;
            return Query_ComputePenetration(colliderA, positionA, rotationA, colliderB, positionB, rotationB, ref direction, ref distance);
        }

        [FreeFunction("Physics::ClosestPoint")]
        extern private static Vector3 Query_ClosestPoint([NotNull] Collider collider, Vector3 position, Quaternion rotation, Vector3 point);

        ///<summary>Returns a point on the given collider that is closest to the specified location.</summary>
        ///<remarks>Note that in case the specified location is inside the collider, or exactly on the boundary of it, the input location is returned instead.
        ///
        ///The collider can only be BoxCollider, SphereCollider, CapsuleCollider or a convex MeshCollider.</remarks>
        ///<param name="point">Location you want to find the closest point to.</param>
        ///<param name="collider">The collider that you find the closest point on.</param>
        ///<param name="position">The position of the collider.</param>
        ///<param name="rotation">The rotation of the collider.</param>
        ///<returns>The point on the collider that is closest to the specified location.</returns>
        ///<seealso cref="Collider.ClosestPoint" />
        public static Vector3 ClosestPoint(Vector3 point, Collider collider, Vector3 position, Quaternion rotation)
        {
            return Query_ClosestPoint(collider, position, rotation, point);
        }

        ///<summary>Sets the minimum separation distance for cloth inter-collision.</summary>
        ///<remarks>Cloth particles closer than this distance that belong to different Cloth objects will be separated.</remarks>
        [StaticAccessor("GetPhysicsManager()")]
        public extern static float interCollisionDistance {[NativeName("GetClothInterCollisionDistance")] get; [NativeName("SetClothInterCollisionDistance")] set; }

        ///<summary>Sets the cloth inter-collision stiffness.</summary>
        ///<remarks>Inter-collision stiffness controls how much two particles repel each other when they are closer than the inter-collision distance.</remarks>
        [StaticAccessor("GetPhysicsManager()")]
        public extern static float interCollisionStiffness {[NativeName("GetClothInterCollisionStiffness")] get; [NativeName("SetClothInterCollisionStiffness")] set; }

        ///<exclude />
        [StaticAccessor("GetPhysicsManager()")]
        public extern static bool interCollisionSettingsToggle {[NativeName("GetClothInterCollisionSettingsToggle")] get; [NativeName("SetClothInterCollisionSettingsToggle")] set; }

        ///<summary>Cloth Gravity setting.
        ///Set gravity for all cloth components.</summary>
        extern public static Vector3 clothGravity { [NativeMethod(IsThreadSafe = true)] get; set; }

        ///<summary>Computes and stores colliders touching or inside the sphere into the provided buffer.</summary>
        ///<remarks>
        ///  <para>Does not attempt to grow the buffer if it runs out of space. The length of the buffer is returned when the buffer is full.
        ///Like <see cref="Physics.OverlapSphere" />, but generates no garbage.
        ///</para>
        ///  <para />
        ///</remarks>
        ///<param name="position">Center of the sphere.</param>
        ///<param name="radius">Radius of the sphere.</param>
        ///<param name="results">The buffer to store the results into.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layer mask) defines which layers of colliders to include in the query.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>Returns the amount of colliders stored into the <c>results</c> buffer.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    // Declare hitColliders as a reusable field.
        ///    private Collider[] hitColliders;
        ///
        ///    // Set the maximum number of colliders that can be detected at once.
        ///    private const int maxColliders = 10;
        ///
        ///    void Awake()
        ///    {
        ///        // Initialize the array just once.
        ///        hitColliders = new Collider[maxColliders];
        ///    }
        ///
        ///    void ExplosionDamage(Vector3 center, float radius)
        ///    {
        ///        // Reuse the pre-allocated array for Physics.OverlapSphereNonAlloc.
        ///        int numColliders = Physics.OverlapSphereNonAlloc(center, radius, hitColliders);
        ///
        ///        // Iterate through detected colliders and send the AddDamage message.
        ///        for (int i = 0; i < numColliders; i++)
        ///        {
        ///            hitColliders[i].SendMessage("AddDamage");
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Physics.AllLayers" />
        ///<seealso href="xref:use-layers "> Use of layers in Unity</seealso>
        public static int OverlapSphereNonAlloc(Vector3 position, float radius, Collider[] results, [DefaultValue("AllLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.OverlapSphere(position, radius, results, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static int OverlapSphereNonAlloc(Vector3 position, float radius, Collider[] results, int layerMask)
        {
            return OverlapSphereNonAlloc(position, radius, results, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int OverlapSphereNonAlloc(Vector3 position, float radius, Collider[] results)
        {
            return OverlapSphereNonAlloc(position, radius, results, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        [FreeFunction("Physics::SphereTest")]
        extern private static bool CheckSphere_Internal(PhysicsScene physicsScene, Vector3 position, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
        ///<summary>Returns true if there are any colliders overlapping the sphere defined by <c>position</c> and <c>radius</c> in world coordinates.</summary>
        ///<param name="position">Center of the sphere.</param>
        ///<param name="radius">Radius of the sphere.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a sphere.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public float sphereRadius;
        ///    AudioSource audioSource;
        ///
        ///    void Start()
        ///    {
        ///        audioSource = GetComponent<AudioSource>();
        ///    }
        ///
        ///    void WarningNoise()
        ///    {
        ///        // Play a noise if an object is within the sphere's radius.
        ///        if (Physics.CheckSphere(transform.position, sphereRadius))
        ///        {
        ///            audioSource.Play();
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool CheckSphere(Vector3 position, float radius, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return CheckSphere_Internal(defaultPhysicsScene, position, radius, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static bool CheckSphere(Vector3 position, float radius, int layerMask)
        {
            return CheckSphere(position, radius, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static bool CheckSphere(Vector3 position, float radius)
        {
            return CheckSphere(position, radius, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        ///<summary>Casts a capsule against all colliders in the Scene and returns detailed information on what was hit into the buffer.</summary>
        ///<remarks>Like <see cref="Physics.CapsuleCastAll" />, but generates no garbage.</remarks>
        ///<param name="point1">The center of the sphere at the <c>start</c> of the capsule.</param>
        ///<param name="point2">The center of the sphere at the <c>end</c> of the capsule.</param>
        ///<param name="radius">The radius of the capsule.</param>
        ///<param name="direction">The direction into which to sweep the capsule.</param>
        ///<param name="results">The buffer to store the hits into.</param>
        ///<param name="maxDistance">The max length of the sweep.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a capsule.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>The amount of hits stored into the buffer.</returns>
        public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.CapsuleCast(point1, point2, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask)
        {
            return CapsuleCastNonAlloc(point1, point2, radius, direction, results, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, float maxDistance)
        {
            return CapsuleCastNonAlloc(point1, point2, radius, direction, results, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results)
        {
            return CapsuleCastNonAlloc(point1, point2, radius, direction, results, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        ///<summary>Cast sphere along the direction and store the results into buffer.</summary>
        ///<remarks>This is  variant of <see cref="Physics.SphereCastAll" />, but instead of allocating the array with the results of the query, it stores the results into the user-provided array. It will only compute as many hits as fit into the buffer, and store them in no particular order. It's not guaranteed that it will store only the closest hits. Generates no garbage.</remarks>
        ///<param name="origin">The center of the sphere at the start of the sweep.</param>
        ///<param name="radius">The radius of the sphere.</param>
        ///<param name="direction">The direction in which to sweep the sphere.</param>
        ///<param name="results">The buffer to save the hits into.</param>
        ///<param name="maxDistance">The max length of the sweep.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a sphere.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>The amount of hits stored into the <c>results</c> buffer.</returns>
        public static int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.SphereCast(origin, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, float maxDistance, int layerMask)
        {
            return SphereCastNonAlloc(origin, radius, direction, results, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, float maxDistance)
        {
            return SphereCastNonAlloc(origin, radius, direction, results, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results)
        {
            return SphereCastNonAlloc(origin, radius, direction, results, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        ///<summary>Cast sphere along the direction and store the results into buffer.</summary>
        ///<param name="ray">The starting point and direction of the ray into which the sphere sweep is cast.</param>
        ///<param name="radius">The radius of the sphere.</param>
        ///<param name="results">The buffer to save the results to.</param>
        ///<param name="maxDistance">The max length of the sweep.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a sphere.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>The amount of hits stored into the <c>results</c> buffer.</returns>
        static public int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return SphereCastNonAlloc(ray.origin, radius, ray.direction, results, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        static public int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results, float maxDistance, int layerMask)
        {
            return SphereCastNonAlloc(ray, radius, results, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results, float maxDistance)
        {
            return SphereCastNonAlloc(ray, radius, results, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        static public int SphereCastNonAlloc(Ray ray, float radius, RaycastHit[] results)
        {
            return SphereCastNonAlloc(ray, radius, results, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [FreeFunction("Physics::CapsuleTest")]
        extern private static bool CheckCapsule_Internal(PhysicsScene physicsScene, Vector3 start, Vector3 end, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
        ///<summary>Checks if any colliders overlap a capsule-shaped volume in world space.</summary>
        ///<remarks>The capsule is defined by the two spheres with <c>radius</c> around <c>point1</c> and <c>point2</c>, which form the two ends of the capsule.</remarks>
        ///<param name="start">The center of the sphere at the <c>start</c> of the capsule.</param>
        ///<param name="end">The center of the sphere at the <c>end</c> of the capsule.</param>
        ///<param name="radius">The radius of the capsule.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a capsule.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Given the start and end waypoints of a corridor, check if there is enough
        ///    // room for an object of a certain width to pass through.
        ///    bool CorridorIsWideEnough(Vector3 startPt, Vector3 endPt, float width)
        ///    {
        ///        return Physics.CheckCapsule(startPt, endPt, width);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool CheckCapsule(Vector3 start, Vector3 end, float radius, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return CheckCapsule_Internal(defaultPhysicsScene, start, end, radius, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static bool CheckCapsule(Vector3 start, Vector3 end, float radius, int layerMask)
        {
            return CheckCapsule(start, end, radius, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static bool CheckCapsule(Vector3 start, Vector3 end, float radius)
        {
            return CheckCapsule(start, end, radius, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [FreeFunction("Physics::BoxTest")]
        extern private static bool CheckBox_Internal(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Quaternion orientation, int layermask, QueryTriggerInteraction queryTriggerInteraction);
        ///<summary>Check whether the given box overlaps with other colliders or not.</summary>
        ///<param name="center">Center of the box.</param>
        ///<param name="halfExtents">Half the size of the box in each dimension.</param>
        ///<param name="orientation">Rotation of the box.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>True, if the box overlaps with any colliders.</returns>
        public static bool CheckBox(Vector3 center, Vector3 halfExtents, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("DefaultRaycastLayers")] int layermask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return CheckBox_Internal(defaultPhysicsScene, center, halfExtents, orientation, layermask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static bool CheckBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask)
        {
            return CheckBox(center, halfExtents, orientation, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static bool CheckBox(Vector3 center, Vector3 halfExtents, Quaternion orientation)
        {
            return CheckBox(center, halfExtents, orientation, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static bool CheckBox(Vector3 center, Vector3 halfExtents)
        {
            return CheckBox(center, halfExtents, Quaternion.identity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [FreeFunction("Physics::OverlapBox")]
        extern private static Collider[] OverlapBox_Internal(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
        ///<summary>Find all colliders touching or inside of the given box.</summary>
        ///<remarks>Creates an invisible box you define that tests collisions by outputting any colliders that come into contact with the box.</remarks>
        ///<param name="center">Center of the box.</param>
        ///<param name="halfExtents">Half of the size of the box in each dimension.</param>
        ///<param name="orientation">Rotation of the box.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layer mask) that is used to selectively filter which colliders are considered when casting a ray.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>Colliders that overlap with the given box.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// //Attach this script to your GameObject. This GameObject doesn’t need to have a Collider component
        /// //Set the Layer Mask field in the Inspector to the layer you would like to see collisions in (set to __Everything__ if you are unsure).
        /// //Create a second Gameobject for testing collisions. Make sure your GameObject has a Collider component (if it doesn’t, click on the __Add Component__ button in the GameObject’s Inspector, and go to __Physics__>__Box Collider__).
        /// //Place it so it is overlapping your other GameObject.
        /// //Press Play to see the console output the name of your second GameObject
        ///
        /// //This script uses the OverlapBox that creates an invisible Box Collider that detects multiple collisions with other colliders. The OverlapBox in this case is the same size and position as the GameObject you attach it to (acting as a replacement for the BoxCollider component).
        ///
        ///using UnityEngine;
        ///
        ///public class OverlapBoxExample : MonoBehaviour
        ///{
        ///    public LayerMask m_LayerMask;
        ///
        ///    void FixedUpdate()
        ///    {
        ///        MyCollisions();
        ///    }
        ///
        ///    void MyCollisions()
        ///    {
        ///        // Use the OverlapBox to detect if there are any other colliders within this box area.
        ///        // Use the GameObject's center, half the size (as a radius), and rotation. This creates an invisible box around your GameObject.
        ///        Collider[] hitColliders = Physics.OverlapBox(gameObject.transform.position, transform.localScale / 2, Quaternion.identity, m_LayerMask);
        ///        int i = 0;
        ///        // Check when there is a new collider coming into contact with the box
        ///        while (i < hitColliders.Length)
        ///        {
        ///            // Output all of the collider names
        ///            Debug.Log("Hit : " + hitColliders[i].name + i);
        ///            // Increase the number of Colliders in the array
        ///            i++;
        ///        }
        ///    }
        ///
        ///    // Draw the Box Overlap as a gizmo to show where it currently is testing. Click the Gizmos button to see this.
        ///    void OnDrawGizmos()
        ///    {
        ///        Gizmos.color = Color.red;
        ///        // Check that it is being run in Play Mode, so it doesn't try to draw this in Editor mode
        ///        if (Application.isPlaying)
        ///            // Draw a cube where the OverlapBox is (positioned where your GameObject is as well as a size)
        ///            Gizmos.DrawWireCube(transform.position, transform.localScale);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("AllLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return OverlapBox_Internal(defaultPhysicsScene, center, halfExtents, orientation, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask)
        {
            return OverlapBox(center, halfExtents, orientation, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation)
        {
            return OverlapBox(center, halfExtents, orientation, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents)
        {
            return OverlapBox(center, halfExtents, Quaternion.identity, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        ///<summary>Find all colliders touching or inside of the given box, and store them into the buffer.</summary>
        ///<param name="center">Center of the box.</param>
        ///<param name="halfExtents">Half of the size of the box in each dimension.</param>
        ///<param name="results">The buffer to store the results in.</param>
        ///<param name="orientation">Rotation of the box.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>The amount of colliders stored in <c>results</c>.</returns>
        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("AllLayers")] int mask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.OverlapBox(center, halfExtents, results, orientation, mask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results, Quaternion orientation, int mask)
        {
            return OverlapBoxNonAlloc(center, halfExtents, results, orientation, mask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results, Quaternion orientation)
        {
            return OverlapBoxNonAlloc(center, halfExtents, results, orientation, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results)
        {
            return OverlapBoxNonAlloc(center, halfExtents, results, Quaternion.identity, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        ///<summary>Cast the box along the direction, and store hits in the provided buffer.</summary>
        ///<param name="center">Center of the box.</param>
        ///<param name="halfExtents">Half the size of the box in each dimension.</param>
        ///<param name="direction">The direction in which to cast the box.</param>
        ///<param name="results">The buffer to store the results in.</param>
        ///<param name="orientation">Rotation of the box.</param>
        ///<param name="maxDistance">The max length of the cast.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a box.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>The amount of hits stored to the <c>results</c> buffer.</returns>
        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.BoxCast(center, halfExtents, direction, results, orientation, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, Quaternion orientation)
        {
            return BoxCastNonAlloc(center, halfExtents, direction, results, orientation, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, Quaternion orientation, float maxDistance)
        {
            return BoxCastNonAlloc(center, halfExtents, direction, results, orientation, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, Quaternion orientation, float maxDistance, int layerMask)
        {
            return BoxCastNonAlloc(center, halfExtents, direction, results, orientation, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results)
        {
            return BoxCastNonAlloc(center, halfExtents, direction, results, Quaternion.identity, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [FreeFunction("Physics::BoxCastAll")]
        private static extern RaycastHit[] Internal_BoxCastAll(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        ///<summary>Like <see cref="Physics.BoxCast" />, but returns all hits.</summary>
        ///<remarks>**Notes:** For colliders that overlap the box at the start of the sweep,  <see cref="RaycastHit.normal" /> is set opposite to the direction of the sweep, <see cref="RaycastHit.distance" /> is set to zero, and the zero vector gets returned in <see cref="RaycastHit.point" />. You might want to check whether this is the case in your particular query and perform additional queries to refine the result.</remarks>
        ///<param name="center">Center of the box.</param>
        ///<param name="halfExtents">Half the size of the box in each dimension.</param>
        ///<param name="direction">The direction in which to cast the box.</param>
        ///<param name="orientation">Rotation of the box.</param>
        ///<param name="maxDistance">The max length of the cast.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a box.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>All colliders that were hit.</returns>
        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("Mathf.Infinity")] float maxDistance, [DefaultValue("DefaultRaycastLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;

                return Internal_BoxCastAll(defaultPhysicsScene, center, halfExtents, normalizedDirection, orientation, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return Array.Empty<RaycastHit>();
            }
        }

        [ExcludeFromDocs]
        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, int layerMask)
        {
            return BoxCastAll(center, halfExtents, direction, orientation, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance)
        {
            return BoxCastAll(center, halfExtents, direction, orientation, maxDistance, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation)
        {
            return BoxCastAll(center, halfExtents, direction, orientation, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static RaycastHit[] BoxCastAll(Vector3 center, Vector3 halfExtents, Vector3 direction)
        {
            return BoxCastAll(center, halfExtents, direction, Quaternion.identity, Mathf.Infinity, DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        ///<summary>Check the given capsule against the physics world and return all overlapping colliders in the user-provided buffer.</summary>
        ///<remarks>Same as <see cref="Physics.OverlapCapsule" /> but does not allocate anything on the managed heap.</remarks>
        ///<param name="point0">The center of the sphere at the <c>start</c> of the capsule.</param>
        ///<param name="point1">The center of the sphere at the <c>end</c> of the capsule.</param>
        ///<param name="radius">The radius of the capsule.</param>
        ///<param name="results">The buffer to store the results into.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a capsule.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>The amount of entries written to the buffer.</returns>
        public static int OverlapCapsuleNonAlloc(Vector3 point0, Vector3 point1, float radius, Collider[] results, [DefaultValue("AllLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return defaultPhysicsScene.OverlapCapsule(point0, point1, radius, results, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public static int OverlapCapsuleNonAlloc(Vector3 point0, Vector3 point1, float radius, Collider[] results, int layerMask)
        {
            return OverlapCapsuleNonAlloc(point0, point1, radius, results, layerMask, QueryTriggerInteraction.UseGlobal);
        }

        [ExcludeFromDocs]
        public static int OverlapCapsuleNonAlloc(Vector3 point0, Vector3 point1, float radius, Collider[] results)
        {
            return OverlapCapsuleNonAlloc(point0, point1, radius, results, AllLayers, QueryTriggerInteraction.UseGlobal);
        }

        ///<summary>Rebuild the broadphase interest regions as well as set the world boundaries.</summary>
        ///<remarks>Effective only when the Multi-box Pruning Broadphase is used.
        ///
        ///In this mode, the boundaries of the world have to be set and then the physics engine would subdivide the volume into a flat grid in the XZ plane, with each cell containing a set of objects that belong to the cell. One may think that each cell contains an instance of the regular sweep-and-prune broadphase. The main benefit of having a grid is to be able to avoid the typical sweep-and-prune locality problem where in a flat world all the objects overlap each other along the Y axis thus causing excess rebuilding of the SAP projections lists along each axis even for the objects that are far apart.
        ///
        ///Note that the physics objects located outside of the world boundaries will not detect collisions at all.
        ///
        ///There is a limit of 256 on the total amount of world cells currently, so the maximum number you can set to subdivisions is 16.
        ///
        ///This function is useful to make the broadphase settings per-scene, not per-project.</remarks>
        ///<param name="worldBounds">Boundaries of the physics world.</param>
        ///<param name="subdivisions">How many cells to create along x and z axis.</param>
        [StaticAccessor("GetPhysicsManager()")]
        public static extern void RebuildBroadphaseRegions(Bounds worldBounds, int subdivisions);

        ///<summary>Prepares the mesh for use with a <see cref="MeshCollider" />.</summary>
        ///<remarks>
        ///  <para>In order for the mesh to be usable with the <see cref="MeshCollider" />, the physics system must prepare it first, by creating the spatial search acceleration structures. This process is called baking.
        ///
        ///Normally, the MeshCollider component requires the baked mesh when the user instantiates it, or when the user sets a new mesh to it with the sharedMesh property. Baking is a resource-intensive operation so you might want to run it when the moment is right (for example during a less resource-intensive part of the application), or to spread the load across all available cores if multiple meshes require baking. That said, the purpose of this function is to pre-bake the mesh for later use so that no further baking is required.
        ///
        ///The mesh instance stores the baked mesh.
        ///
        ///The baking process needs access to the mesh geometry. If the user invokes the BakeMesh method in the Player, the BakeMesh method requires the Read/Write property of the mesh to be enabled. However, using the BakeMesh method in the Editor doesn't require any extra settings, because the geometry is always available in the Editor.
        ///
        ///The MeshCollider component reuses the baked mesh if, and only if, all of the following conditions are met:
        ///
        ///- The MeshCollider's cookingOptions are exactly the same as were specified during baking, 
        ///
        ///- The MeshCollider's transform allows mesh sharing (*),
        ///
        ///- The mesh geometry hasn't been changed since the last bake.
        ///
        ///
        ///
        ///In this context, the MeshCollider's transform allows mesh sharing if:
        ///
        ///- Its scaling is not negative and is not skewed, or
        ///
        ///- Its scaling is negative but only when MeshCollider is not convex
        ///
        ///
        ///
        ///
        ///Note: When you add a <see cref="MeshCollider" /> component to a GameObject with a <see cref="MeshFilter" /> component already present, the sharedMesh property is set automatically and this might trigger a re-bake.
        ///
        ///
        ///
        ///Here is a simple example baking the mesh on the main thread:</para>
        ///  <para>BakeMesh is thread-safe, and does computations on the thread it was called from. However, don't call BakeMesh on the same mesh from multiple threads at the same time because that causes undefined behavior. You can use BakeMesh with the C# Job System.
        ///This example shows how to bake meshes across multiple threads so that MeshCollider instantiation takes less time on the main thread.</para>
        ///</remarks>
        ///<param name="convex">A flag to indicate whether to bake convex geometry or not.</param>
        ///<param name="cookingOptions">The cooking options to use when you bake the mesh.</param>
        ///<param name="meshEntityId">The EntityId of the mesh to bake collision data from.</param>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///
        ///public class MinimalTest : MonoBehaviour
        ///{
        ///    public Mesh mesh;
        ///
        ///    private MeshCollider collider;
        ///
        ///    private MeshColliderCookingOptions cookingOptions =
        ///        MeshColliderCookingOptions.UseFastMidphase | MeshColliderCookingOptions.CookForFasterSimulation;
        ///
        ///    private void OnEnable()
        ///    {
        ///        // Bake this Mesh to use later.
        ///        Physics.BakeMesh(mesh.GetEntityId(), false, cookingOptions);
        ///    }
        ///
        ///    public void FixedUpdate()
        ///    {
        ///        // If the collider wasn't yet created - create it now.
        ///        if (collider == null)
        ///        {
        ///            // No mesh baking will happen here because the mesh was pre-baked, making instantiation faster.
        ///            collider = new GameObject().AddComponent<MeshCollider>();
        ///            collider.cookingOptions = cookingOptions;
        ///            collider.sharedMesh = mesh;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[using Unity.Collections;
        ///using Unity.Jobs;
        ///using UnityEngine;
        ///
        ///public struct BakeJob : IJobParallelFor
        ///{
        ///    private NativeArray<EntityId> meshIds;
        ///
        ///    public BakeJob(NativeArray<EntityId> meshIds)
        ///    {
        ///        this.meshIds = meshIds;
        ///    }
        ///
        ///    public void Execute(int index)
        ///    {
        ///        Physics.BakeMesh(meshIds[index], false);
        ///    }
        ///}
        ///
        ///public class JobifiedBaking : MonoBehaviour
        ///{
        ///    public Mesh[] meshes;
        ///    public int meshesPerJob = 10;
        ///
        ///    // Bake all the Meshes off of the main thread, and then instantiate on the main thread.
        ///    private void OnEnable()
        ///    {
        ///        // You cannot access GameObjects and Components from other threads directly.
        ///        // As such, you need to create a native array of EntityIds that BakeMesh will accept.
        ///        NativeArray<EntityId> meshIds = new NativeArray<EntityId>(meshes.Length, Allocator.TempJob);
        ///
        ///        for (int i = 0; i < meshes.Length; ++i)
        ///        {
        ///            meshIds[i] = meshes[i].GetEntityId();
        ///        }
        ///
        ///        // This spreads the expensive operation over all cores.
        ///        var job = new BakeJob(meshIds);
        ///        job.Schedule(meshIds.Length, meshesPerJob).Complete();
        ///
        ///        meshIds.Dispose();
        ///
        ///        // Now instantiate colliders on the main thread.
        ///        for (int i = 0; i < meshes.Length; ++i)
        ///        {
        ///            new GameObject().AddComponent<MeshCollider>().sharedMesh = meshes[i];
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [StaticAccessor("GetPhysicsManager()")]
        [NativeMethod(IsThreadSafe = true)]
        public static extern void BakeMesh(EntityId meshEntityId, bool convex, MeshColliderCookingOptions cookingOptions);

        ///<summary>Prepares the mesh for use with a <see cref="MeshCollider" />.</summary>
        ///<remarks>
        ///  <para>In order for the mesh to be usable with the <see cref="MeshCollider" />, the physics system must prepare it first, by creating the spatial search acceleration structures. This process is called baking.
        ///
        ///Normally, the MeshCollider component requires the baked mesh when the user instantiates it, or when the user sets a new mesh to it with the sharedMesh property. Baking is a resource-intensive operation so you might want to run it when the moment is right (for example during a less resource-intensive part of the application), or to spread the load across all available cores if multiple meshes require baking. That said, the purpose of this function is to pre-bake the mesh for later use so that no further baking is required.
        ///
        ///The mesh instance stores the baked mesh.
        ///
        ///The baking process needs access to the mesh geometry. If the user invokes the BakeMesh method in the Player, the BakeMesh method requires the Read/Write property of the mesh to be enabled. However, using the BakeMesh method in the Editor doesn't require any extra settings, because the geometry is always available in the Editor.
        ///
        ///The MeshCollider component reuses the baked mesh if, and only if, all of the following conditions are met:
        ///
        ///- The MeshCollider's cookingOptions are exactly the same as were specified during baking, 
        ///
        ///- The MeshCollider's transform allows mesh sharing (*),
        ///
        ///- The mesh geometry hasn't been changed since the last bake.
        ///
        ///
        ///
        ///In this context, the MeshCollider's transform allows mesh sharing if:
        ///
        ///- Its scaling is not negative and is not skewed, or
        ///
        ///- Its scaling is negative but only when MeshCollider is not convex
        ///
        ///
        ///
        ///
        ///Note: When you add a <see cref="MeshCollider" /> component to a GameObject with a <see cref="MeshFilter" /> component already present, the sharedMesh property is set automatically and this might trigger a re-bake.
        ///
        ///
        ///
        ///Here is a simple example baking the mesh on the main thread:</para>
        ///  <para>BakeMesh is thread-safe, and does computations on the thread it was called from. However, don't call BakeMesh on the same mesh from multiple threads at the same time because that causes undefined behavior. You can use BakeMesh with the C# Job System.
        ///This example shows how to bake meshes across multiple threads so that MeshCollider instantiation takes less time on the main thread.</para>
        ///</remarks>
        ///<param name="meshID">The instance ID of the mesh to bake collision data from.</param>
        ///<param name="convex">A flag to indicate whether to bake convex geometry or not.</param>
        ///<param name="cookingOptions">The cooking options to use when you bake the mesh.</param>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///
        ///public class MinimalTest : MonoBehaviour
        ///{
        ///    public Mesh mesh;
        ///
        ///    private MeshCollider collider;
        ///
        ///    private MeshColliderCookingOptions cookingOptions =
        ///        MeshColliderCookingOptions.UseFastMidphase | MeshColliderCookingOptions.CookForFasterSimulation;
        ///
        ///    private void OnEnable()
        ///    {
        ///        // Bake this Mesh to use later.
        ///        Physics.BakeMesh(mesh.GetEntityId(), false, cookingOptions);
        ///    }
        ///
        ///    public void FixedUpdate()
        ///    {
        ///        // If the collider wasn't yet created - create it now.
        ///        if (collider == null)
        ///        {
        ///            // No mesh baking will happen here because the mesh was pre-baked, making instantiation faster.
        ///            collider = new GameObject().AddComponent<MeshCollider>();
        ///            collider.cookingOptions = cookingOptions;
        ///            collider.sharedMesh = mesh;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[using Unity.Collections;
        ///using Unity.Jobs;
        ///using UnityEngine;
        ///
        ///public struct BakeJob : IJobParallelFor
        ///{
        ///    private NativeArray<EntityId> meshIds;
        ///
        ///    public BakeJob(NativeArray<EntityId> meshIds)
        ///    {
        ///        this.meshIds = meshIds;
        ///    }
        ///
        ///    public void Execute(int index)
        ///    {
        ///        Physics.BakeMesh(meshIds[index], false);
        ///    }
        ///}
        ///
        ///public class JobifiedBaking : MonoBehaviour
        ///{
        ///    public Mesh[] meshes;
        ///    public int meshesPerJob = 10;
        ///
        ///    // Bake all the Meshes off of the main thread, and then instantiate on the main thread.
        ///    private void OnEnable()
        ///    {
        ///        // You cannot access GameObjects and Components from other threads directly.
        ///        // As such, you need to create a native array of EntityIds that BakeMesh will accept.
        ///        NativeArray<EntityId> meshIds = new NativeArray<EntityId>(meshes.Length, Allocator.TempJob);
        ///
        ///        for (int i = 0; i < meshes.Length; ++i)
        ///        {
        ///            meshIds[i] = meshes[i].GetEntityId();
        ///        }
        ///
        ///        // This spreads the expensive operation over all cores.
        ///        var job = new BakeJob(meshIds);
        ///        job.Schedule(meshIds.Length, meshesPerJob).Complete();
        ///
        ///        meshIds.Dispose();
        ///
        ///        // Now instantiate colliders on the main thread.
        ///        for (int i = 0; i < meshes.Length; ++i)
        ///        {
        ///            new GameObject().AddComponent<MeshCollider>().sharedMesh = meshes[i];
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [Obsolete("BakeMesh(int, bool, MeshColliderCookingOptions) is obsolete. Use BakeMesh(EntityId, bool, MeshColliderCookingOptions) instead.", true)]
        public static void BakeMesh(int meshID, bool convex, MeshColliderCookingOptions cookingOptions) => BakeMesh((EntityId)meshID, convex, cookingOptions);

        ///<summary>Prepares the mesh for use with a <see cref="MeshCollider" /> and uses default cooking options.</summary>
        ///<param name="meshID">The instance ID of the mesh to bake collision data from.</param>
        ///<param name="convex">A flag to indicate whether to bake convex geometry or not.</param>
        [Obsolete("BakeMesh(int, bool) is obsolete. Use BakeMesh(EntityId, bool) instead.", true)]
        public static void BakeMesh(int meshID, bool convex)
        {
            BakeMesh((EntityId)meshID, convex, MeshColliderCookingOptions.CookForFasterSimulation |
                                     MeshColliderCookingOptions.EnableMeshCleaning |
                                     MeshColliderCookingOptions.WeldColocatedVertices |
                                     MeshColliderCookingOptions.UseFastMidphase);
        }

        ///<summary>Prepares the mesh for use with a <see cref="MeshCollider" /> and uses default cooking options.</summary>
        ///<param name="convex">A flag to indicate whether to bake convex geometry or not.</param>
        ///<param name="meshEntityId">The EntityId of the mesh to bake collision data from.</param>
        public static void BakeMesh(EntityId meshEntityId, bool convex)
        {
            BakeMesh(meshEntityId, convex, MeshColliderCookingOptions.CookForFasterSimulation |
                                     MeshColliderCookingOptions.EnableMeshCleaning |
                                     MeshColliderCookingOptions.WeldColocatedVertices |
                                     MeshColliderCookingOptions.UseFastMidphase);
        }

        [StaticAccessor("PhysicsManager", StaticAccessorType.DoubleColon)]
        internal static extern bool ConnectPhysicsSDKVisualDebugger();

        [StaticAccessor("PhysicsManager", StaticAccessorType.DoubleColon)]
        internal static extern void DisconnectPhysicsSDKVisualDebugger();

        [StaticAccessor("PhysicsManager", StaticAccessorType.DoubleColon)]
        extern internal static Collider GetColliderByEntityId(EntityId entityId);

        [StaticAccessor("PhysicsManager", StaticAccessorType.DoubleColon)]
        internal static extern Component GetBodyByEntityId(EntityId entityId);

        [NativeMethod(IsThreadSafe = true)]
        [StaticAccessor("PhysicsManager", StaticAccessorType.DoubleColon)]
        internal static extern uint TranslateTriangleIndexFromID(EntityId entityId, uint faceIndex);

        [StaticAccessor("PhysicsManager", StaticAccessorType.DoubleColon)]
        private static extern void SendOnCollisionEnter(Component component, Collision collision);
        [StaticAccessor("PhysicsManager", StaticAccessorType.DoubleColon)]
        private static extern void SendOnCollisionStay(Component component,  Collision collision);
        [StaticAccessor("PhysicsManager", StaticAccessorType.DoubleColon)]
        private static extern void SendOnCollisionExit(Component component,  Collision collision);
    }
}
