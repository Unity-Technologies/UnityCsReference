// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;
using UnityEngine.Internal;
using UnityEngine.SceneManagement;
using System.Runtime.CompilerServices;

namespace UnityEngine
{
    ///<summary>Represents a single instance of a 3D physics Scene.</summary>
    [NativeHeader("Modules/Physics/PhysicsQuery.h")]
    [NativeHeader("Modules/Physics/Public/PhysicsSceneHandle.h")]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct PhysicsScene : IEquatable<PhysicsScene>
    {
        private uint m_index;
        private uint m_version;

        ///<exclude />
        public override string ToString() { return string.Format("PhysicsScene(Index: {0}, Version: {1})", m_index, m_version); }
        ///<exclude />
        public static bool operator ==(PhysicsScene lhs, PhysicsScene rhs) { return lhs.m_index == rhs.m_index && lhs.m_version == rhs.m_version; }

        ///<exclude />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(PhysicsScene lhs, PhysicsScene rhs) { return !(lhs == rhs); }
        ///<exclude />
        public override int GetHashCode() { return HashCode.Combine(m_index, m_version); }
        ///<exclude />
        public override bool Equals(object other)
        {
            if (!(other is PhysicsScene))
                return false;

            PhysicsScene rhs = (PhysicsScene)other;
            return this == rhs;
        }

        public bool Equals(PhysicsScene other)
        {
            return this == other;
        }

        ///<summary>Gets whether the physics Scene is valid or not.</summary>
        ///<remarks>If the physics Scene is associated with a specific <see cref="UnityEngine.SceneManagement.Scene" /> which has been destroyed then the physics Scene is no longer valid.  Note that the <see cref="Physics.defaultPhysicsScene" /> is always valid.</remarks>
        ///<returns>Is the physics scene valid?</returns>
        public bool IsValid() { return IsValid_Internal(this); }
        [StaticAccessor("GetPhysicsManager()", StaticAccessorType.Dot)]
        [NativeMethod("IsPhysicsSceneValid")]
        extern private static bool IsValid_Internal(PhysicsScene physicsScene);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal PhysicsScene GetDefaultScene()
        {
            //!!!Keep this in sync with the declaration of kDefaultPhysicsSceneHandle inside PhysicsSceneHandle.h!!!
            var scene = new PhysicsScene();
            scene.m_index = 0;
            scene.m_version = 0;

            return scene;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static internal PhysicsScene Invalid()
        {
            //!!!Keep this in sync with the declaration of kInvalidPhysicsSceneHandle inside PhysicsSceneHandle.h!!!
            var scene = new PhysicsScene();
            scene.m_index = uint.MaxValue;
            scene.m_version = 0;

            return scene;
        }

        ///<summary>Gets whether the physics Scene is empty or not.</summary>
        ///<returns>Is the physics Scene is empty?</returns>
        public bool IsEmpty()
        {
            if (IsValid())
                return IsEmpty_Internal(this);

            throw new InvalidOperationException("Cannot check if physics scene is empty as it is invalid.");
        }

        [StaticAccessor("GetPhysicsManager()", StaticAccessorType.Dot)]
        [NativeMethod("IsPhysicsWorldEmpty")]
        extern private static bool IsEmpty_Internal(PhysicsScene physicsScene);

        // Perform a manual simulation step.
        ///<summary>Simulate physics associated with this <see cref="PhysicsScene" />.</summary>
        ///<remarks>
        ///  <para>Calling this method causes the physics to be simulated over the specified <c>step</c> time.  Only the physics associated with this <see cref="PhysicsScene" /> will be simulated.  If this <see cref="PhysicsScene" /> is not the default physics Scene (see <see cref="Physics.defaultPhysicsScene" />) then it is associated with a specific <see cref="UnityEngine.SceneManagement.Scene" /> and as such, only components added to that <see cref="UnityEngine.SceneManagement.Scene" /> are affected when running the simulation.
        ///
        ///If you pass framerate-dependent step values (such as <see cref="Time.deltaTime" />) to the physics engine, your simulation will be less deterministic because of the unpredictable fluctuations in framerate that can arise. To achieve more deterministic physics results, you should pass a fixed step value to <see cref="PhysicsScene.Simulate" /> every time you call it.
        ///
        ///You can call <see cref="PhysicsScene.Simulate" /> in the Editor outside of play mode however caution is advised as this will cause the simulation to move GameObject that have a <see cref="Rigidbody" /> component attached.  When simulating in the Editor outside of play mode, a full simulation occurs for all physics components including <see cref="Rigidbody" />, <see cref="Collider" /> and <see cref="Joint" /> including the generation of contacts however contacts are not reported via the standard script callbacks.  This is a safety measure to prevent allowing callbacks to delete objects in the Scene which would not be an undoable operation.
        ///Here is an example of a basic simulation that implements what's being done in the automatic simulation mode.</para>
        ///  <para />
        ///</remarks>
        ///<param name="step">The time to advance physics by.</param>
        ///<returns>Whether the simulation was run or not.  Running the simulation during physics callbacks will always fail.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.SceneManagement;
        ///
        ///public class MultiScenePhysics : MonoBehaviour
        ///{
        ///    private Scene extraScene;
        ///
        ///    public void Start()
        ///    {
        ///        // First create an extra scene with local physics
        ///        extraScene = SceneManager.CreateScene("Scene", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        ///
        ///        // Mark the scene active, so that all the new GameObjects end up in the newly created scene
        ///        SceneManager.SetActiveScene(extraScene);
        ///
        ///        PopulateExtraSceneWithObjects();
        ///    }
        ///
        ///    public void FixedUpdate()
        ///    {
        ///        // All of the non-default physics scenes need to be simulated manually
        ///        var physicsScene = extraScene.GetPhysicsScene();
        ///        physicsScene.Simulate(Time.fixedDeltaTime);
        ///    }
        ///
        ///    public void PopulateExtraSceneWithObjects()
        ///    {
        ///        // Create GameObjects for physics simulation
        ///        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ///        sphere.AddComponent<Rigidbody>();
        ///        sphere.transform.position = Vector3.up * 4;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Physics.Simulate" />
        public void Simulate(float step)
        {
            if (IsValid())
            {
                // Only check auto-simulation if simulating the default physics scene.
                if (this == GetDefaultScene() && Physics.simulationMode != SimulationMode.Script)
                {
                    Debug.LogWarning("PhysicsScene.Simulate(...) was called but simulation mode is not set to Script. You should set simulation mode to Script first before calling this function therefore the simulation was not run.");
                    return;
                }

                Physics.Simulate_Internal(this, step, SimulationStage.All, SimulationOption.All);
                return;
            }

            throw new InvalidOperationException("Cannot simulate the physics scene as it is invalid.");
        }

        ///<summary>Runs specified physics simulation stages on this physics scene.</summary>
        ///<remarks>Stages are processed in this order:
        ///
        ///1. <see cref="SimulationStage.PrepareSimulation" />
        ///2. <see cref="SimulationStage.RunSimulation" />
        ///3. <see cref="SimulationStage.PublishSimulationResults" />
        ///
        ///step argument can be any number if <see cref="SimulationStage.RunSimulation" /> stage is not specified.</remarks>
        ///<param name="step">The time to advance physics by.</param>
        ///<param name="stages">An enum to specify which stages to run.</param>
        ///<param name="options">A flag enum to specify any additional simulation options.</param>
        ///<seealso cref="PhysicsScene.Simulate" />
        ///<seealso cref="Physics.Simulate" />
        public void RunSimulationStages(float step, SimulationStage stages, [DefaultValue("SimulationOption.All")] SimulationOption options = SimulationOption.All)
        {
            if (!IsValid())
                throw new InvalidOperationException("Cannot simulate the physics scene as it is invalid.");

            // Only check auto-simulation if simulating the default physics scene.
            if (this == GetDefaultScene() && Physics.simulationMode != SimulationMode.Script)
            {
                Debug.LogWarning("PhysicsScene.Simulate(...) was called but simulation mode is not set to Script. You should set simulation mode to Script first before calling this function therefore the simulation was not run.");
                return;
            }

            Physics.Simulate_Internal(this, step, stages, options);
        }


        [StaticAccessor("GetPhysicsManager()", StaticAccessorType.Dot)]
        [NativeMethod("ReleasePhysicsSceneSimulationBuffers")]
        private extern static void ReleasePhysicsSceneSimulationBuffers_Internal(PhysicsScene handle);

        ///<summary>Clear and deallocate the simulation results.</summary>
        ///<remarks>Calling this method will result in all simulation result buffers used by this PhysicsScene to be deallocated. The method offers a manual alternative to the result clearing options present in the physics settings pane.</remarks>
        public void ReleaseLastSimulationStepBuffers()
        {
            ReleasePhysicsSceneSimulationBuffers_Internal(this);
        }

        ///<summary>Interpolates Rigidbodies in this <see cref="PhysicsScene" />.</summary>
        ///<remarks>
        ///  <para>Interpolates all Rigidbodies in this <see cref="PhysicsScene" /> with <see cref="Rigidbody.interpolation" /> set to either <see cref="RigidbodyInterpolation.Interpolate" /> or <see cref="RigidbodyInterpolation.Extrapolate" /> with the current <see cref="Time.time" /> value.
        ///
        ///This method is called automatically for the default <see cref="PhysicsScene" /> and therefore any manual calls on the <see cref="Physics.defaultPhysicsScene" /> will fail.
        ///
        ///</para>
        ///  <para>Simulates and interpolates a non-default <see cref="PhysicsScene" />.</para>
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class SimpleSimulator : MonoBehaviour
        ///{
        ///    private PhysicsScene m_PhysicsScene;
        ///
        ///    private void Update()
        ///    {
        ///        m_PhysicsScene.InterpolateBodies();
        ///    }
        ///
        ///    private void FixedUpdate()
        ///    {
        ///        m_PhysicsScene.ResetInterpolationPoses();
        ///        m_PhysicsScene.Simulate(Time.fixedDeltaTime);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="PhysicsScene.Simulate" />
        ///<seealso cref="PhysicsScene.ResetInterpolationPoses" />
        public void InterpolateBodies()
        {
            if (!IsValid())
                throw new InvalidOperationException("Cannot interpolate the physics scene as it is invalid.");

            if (this == Physics.defaultPhysicsScene)
            {
                Debug.LogWarning("PhysicsScene.InterpolateBodies() was called on the default Physics Scene. This is done automatically and the call will be ignored");
                return;
            }

            Physics.InterpolateBodies_Internal(this);
        }

        ///<summary>Resets the <see cref="Transform" /> positions of interpolated and extrapolated Rigidbodies in this <see cref="PhysicsScene" /> to <see cref="Rigidbody.position" /> and <see cref="Transform" /> rotations to <see cref="Rigidbody.rotation" />.</summary>
        ///<remarks>Call this method before simulating to prevent Transform poses of interpolated and extrapolated Rigidbodies from getting synced to the physics system. If multiple <see cref="PhysicsScene.Simulate" /> calls are to be made this frame, it's enough to call this method only once, before the first simulation.
        ///
        ///This method is called automatically for the default <see cref="PhysicsScene" /> and therefore any manual calls on the <see cref="Physics.defaultPhysicsScene" /> will fail.</remarks>
        ///<seealso cref="PhysicsScene.InterpolateBodies" />
        public void ResetInterpolationPoses()
        {
            if (!IsValid())
                throw new InvalidOperationException("Cannot reset poses of the physics scene as it is invalid.");

            if (this == Physics.defaultPhysicsScene)
            {
                Debug.LogWarning("PhysicsScene.ResetInterpolationPoses() was called on the default Physics Scene. This is done automatically and the call will be ignored");
                return;
            }

            Physics.ResetInterpolationPoses_Internal(this);
        }

        // Hit Test.
        ///<summary>Casts a ray, from point <c>origin</c>, in direction <c>direction</c>, of length <c>maxDistance</c>, against all colliders in the Scene.</summary>
        ///<remarks>You may optionally provide a <see cref="LayerMask" />, to filter out any Colliders you aren't interested in generating collisions with.
        ///Specifying <c>queryTriggerInteraction</c> allows you to control whether or not Trigger colliders generate a hit, or whether to use the global <see cref="Physics.queriesHitTriggers" /> setting.
        ///
        ///This example creates a simple Raycast, projecting forwards from the position of the object's current position, extending for 10 units.</remarks>
        ///<param name="origin">The starting point of the ray in world coordinates.</param>
        ///<param name="direction">The direction of the ray.</param>
        ///<param name="maxDistance">The max distance the ray should check for collisions.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layer mask) that is used to selectively filter which colliders are considered when casting a ray.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>True if the ray intersects with a Collider, otherwise false.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void FixedUpdate()
        ///    {
        ///        // Get the current PhysicsScene
        ///        PhysicsScene physicsScene = gameObject.scene.GetPhysicsScene();
        ///
        ///        // Define ray direction and origin
        ///        Vector3 origin = transform.position;
        ///        Vector3 direction = transform.TransformDirection(Vector3.forward);
        ///
        ///        // Max ray distance
        ///        float maxDistance = 10f;
        ///
        ///        // RaycastAll in the current physics scene
        ///        RaycastHit[] hits = new RaycastHit[10]; // Pre-allocate for performance
        ///        int hitCount = physicsScene.Raycast(origin, direction, hits, maxDistance);
        ///
        ///        if (hitCount > 0)
        ///        {
        ///            Debug.Log($"Detected {hitCount} hit(s) in front of the object:");
        ///            for (int i = 0; i < hitCount; i++)
        ///            {
        ///                Debug.Log($"Hit {i}: {hits[i].collider.name} at {hits[i].point}");
        ///            }
        ///        }
        ///    }
        ///}]]></code>
        ///</example>
        public bool Raycast(Vector3 origin, Vector3 direction, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("Physics.DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;
                Ray ray = new Ray(origin, normalizedDirection);
                return Internal_RaycastTest(this, ray, maxDistance, layerMask, queryTriggerInteraction);
            }

            return false;
        }

        [FreeFunction("Physics::RaycastTest")]
        extern private static bool Internal_RaycastTest(PhysicsScene physicsScene, Ray ray, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        // Single hit.
        ///<summary>Casts a ray, from point <c>origin</c>, in direction <c>direction</c>, of length <c>maxDistance</c>, against all colliders in the Scene.</summary>
        ///<remarks>This method generates no garbage.</remarks>
        ///<param name="origin">The starting point of the ray in world coordinates.</param>
        ///<param name="direction">The direction of the ray.</param>
        ///<param name="hitInfo">If true is returned, <c>hitInfo</c> will contain more information about where the collider was hit. ().</param>
        ///<param name="maxDistance">The max distance the ray should check for collisions.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layer mask) that is used to selectively filter which colliders are considered when casting a ray.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>True if the ray intersects with a Collider, otherwise false.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///public class RaycastExample : MonoBehaviour
        ///{
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
        ///<seealso cref="RaycastHit" />
        public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("Physics.DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            hitInfo = new RaycastHit();

            float dirLength = direction.magnitude;
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;
                Ray ray = new Ray(origin, normalizedDirection);

                return Internal_Raycast(this, ray, maxDistance, ref hitInfo, layerMask, queryTriggerInteraction);
            }
            else
                return false;
        }

        [FreeFunction("Physics::Raycast")]
        extern private static bool Internal_Raycast(PhysicsScene physicsScene, Ray ray, float maxDistance, ref RaycastHit hit, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        // Multiple hits.
        ///<summary>Casts a ray, from point <c>origin</c>, in direction <c>direction</c>, of length <c>maxDistance</c>, against all colliders in the Scene.</summary>
        ///<remarks>This method generates no garbage.</remarks>
        ///<param name="origin">The starting point and direction of the ray.</param>
        ///<param name="direction">The direction of the ray.</param>
        ///<param name="raycastHits">The buffer to store the hits into.</param>
        ///<param name="maxDistance">The max distance the rayhit is allowed to be from the start of the ray.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layer mask) that is used to selectively filter which colliders are considered when casting a ray.</param>
        ///<param name="queryTriggerInteraction">The amount of hits stored into the <c>results</c> buffer.</param>
        ///<returns>True if the ray intersects with a Collider, otherwise false.</returns>
        public int Raycast(Vector3 origin, Vector3 direction, RaycastHit[] raycastHits, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("Physics.DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            float dirLength = direction.magnitude;

            if (dirLength > float.Epsilon)
            {
                Ray ray = new Ray(origin, direction.normalized);
                return Internal_RaycastNonAlloc(this, ray, raycastHits, maxDistance, layerMask, queryTriggerInteraction);
            }

            return 0;
        }

        [FreeFunction("Physics::RaycastNonAlloc")]
        extern private static int Internal_RaycastNonAlloc(PhysicsScene physicsScene, Ray ray, RaycastHit[] raycastHits, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        [FreeFunction("Physics::CapsuleCast")]
        extern private static bool Query_CapsuleCast(PhysicsScene physicsScene, Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, ref RaycastHit hitInfo, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        private static bool Internal_CapsuleCast(PhysicsScene physicsScene, Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            hitInfo = new RaycastHit();
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;

                return Query_CapsuleCast(physicsScene, point1, point2, radius, normalizedDirection, maxDistance, ref hitInfo, layerMask, queryTriggerInteraction);
            }
            else
                return false;
        }

        ///<summary>Casts a capsule against all colliders in this physics scene and returns detailed information on what was hit.</summary>
        ///<param name="point1">The center of the sphere at the <c>start</c> of the capsule.</param>
        ///<param name="point2">The center of the sphere at the <c>end</c> of the capsule.</param>
        ///<param name="radius">The radius of the capsule.</param>
        ///<param name="direction">The direction into which to sweep the capsule.</param>
        ///<param name="hitInfo">If true is returned, <c>hitInfo</c> will contain more information about where the collider was hit. ().</param>
        ///<param name="maxDistance">The max length of the sweep.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a capsule.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>True when the capsule sweep intersects any collider, otherwise false.</returns>
        ///<seealso cref="Physics.CapsuleCast" />
        ///<seealso cref="RaycastHit" />
        public bool CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return Internal_CapsuleCast(this, point1, point2, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [FreeFunction("Physics::CapsuleCastNonAlloc")]
        extern private static int Internal_CapsuleCastNonAlloc(PhysicsScene physicsScene, Vector3 p0, Vector3 p1, float radius, Vector3 direction, RaycastHit[] raycastHits, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        ///<summary>Casts a capsule against all colliders in this physics scene and returns detailed information on what was hit.</summary>
        ///<param name="point1">The center of the sphere at the <c>start</c> of the capsule.</param>
        ///<param name="point2">The center of the sphere at the <c>end</c> of the capsule.</param>
        ///<param name="radius">The radius of the capsule.</param>
        ///<param name="direction">The direction into which to sweep the capsule.</param>
        ///<param name="results">The buffer to store the results in.</param>
        ///<param name="maxDistance">The max length of the sweep.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a capsule.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>The amount of hits stored to the <c>results</c> buffer.</returns>
        ///<seealso cref="Physics.CapsuleCastNonAlloc" />
        public int CapsuleCast(Vector3 point1, Vector3 point2, float radius, Vector3 direction, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            float dirLength = direction.magnitude;

            if (dirLength > float.Epsilon)
            {
                return Internal_CapsuleCastNonAlloc(this, point1, point2, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return 0;
            }
        }

        [FreeFunction("Physics::OverlapCapsuleNonAlloc")]
        extern private static int OverlapCapsuleNonAlloc_Internal(PhysicsScene physicsScene, Vector3 point0, Vector3 point1, float radius, [UnityMarshalAs(NativeType.ScriptingObjectPtr)] Collider[] results, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        ///<summary>Check the given capsule against the physics world and return all overlapping colliders in the user-provided buffer.</summary>
        ///<param name="point0">The center of the sphere at the <c>start</c> of the capsule.</param>
        ///<param name="point1">The center of the sphere at the <c>end</c> of the capsule.</param>
        ///<param name="radius">The radius of the capsule.</param>
        ///<param name="results">The buffer to store the results into.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a capsule.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>The amount of entries written to the buffer.</returns>
        ///<seealso cref="Physics.OverlapCapsuleNonAlloc" />
        public int OverlapCapsule(Vector3 point0, Vector3 point1, float radius, Collider[] results, [DefaultValue("AllLayers")] int layerMask = Physics.AllLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return OverlapCapsuleNonAlloc_Internal(this, point0, point1, radius, results, layerMask, queryTriggerInteraction);
        }

        [FreeFunction("Physics::SphereCast")]
        extern private static bool Query_SphereCast(PhysicsScene physicsScene, Vector3 origin, float radius, Vector3 direction, float maxDistance, ref RaycastHit hitInfo, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        private static bool Internal_SphereCast(PhysicsScene physicsScene, Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            hitInfo = new RaycastHit();
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;

                return Query_SphereCast(physicsScene, origin, radius, normalizedDirection, maxDistance, ref hitInfo, layerMask, queryTriggerInteraction);
            }
            else
                return false;
        }

        ///<summary>Casts a sphere along a ray and returns detailed information on what was hit.</summary>
        ///<param name="origin">The center of the sphere at the start of the sweep.</param>
        ///<param name="radius">The radius of the sphere.</param>
        ///<param name="direction">The direction into which to sweep the sphere.</param>
        ///<param name="hitInfo">If true is returned, <c>hitInfo</c> will contain more information about where the collider was hit. ().</param>
        ///<param name="maxDistance">The max length of the cast.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a sphere.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>True when the sphere sweep intersects any collider, otherwise false.</returns>
        ///<seealso cref="Physics.SphereCast" />
        ///<seealso cref="RaycastHit" />
        public bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return Internal_SphereCast(this, origin, radius, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [FreeFunction("Physics::SphereCastNonAlloc")]
        extern private static int Internal_SphereCastNonAlloc(PhysicsScene physicsScene, Vector3 origin, float radius, Vector3 direction, RaycastHit[] raycastHits, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        ///<summary>Cast sphere along the direction and store the results into buffer.</summary>
        ///<param name="origin">The center of the sphere at the start of the sweep.</param>
        ///<param name="radius">The radius of the sphere.</param>
        ///<param name="direction">The direction into which to sweep the sphere.</param>
        ///<param name="results">The buffer to save the results to.</param>
        ///<param name="maxDistance">The max length of the cast.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a sphere.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>The amount of hits stored to the <c>results</c> buffer.</returns>
        ///<seealso cref="Physics.SphereCastNonAlloc" />
        public int SphereCast(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            float dirLength = direction.magnitude;

            if (dirLength > float.Epsilon)
            {
                return Internal_SphereCastNonAlloc(this, origin, radius, direction, results, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return 0;
            }
        }

        [FreeFunction("Physics::OverlapSphereNonAlloc")]
        extern private static int OverlapSphereNonAlloc_Internal(PhysicsScene physicsScene, Vector3 position, float radius, [UnityMarshalAs(NativeType.ScriptingObjectPtr)] Collider[] results, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        ///<summary>Computes and stores colliders touching or inside the sphere into the provided buffer.</summary>
        ///<param name="position">Center of the sphere.</param>
        ///<param name="radius">Radius of the sphere.</param>
        ///<param name="results">The buffer to store the results into.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a sphere.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>The number of colliders detected that overlap with the sphere and were stored in the <c>results</c> array. The return value cannot exceed the size of the <c>results</c> array.</returns>
        ///<seealso cref="Physics.OverlapSphereNonAlloc" />
        public int OverlapSphere(Vector3 position, float radius, Collider[] results, [DefaultValue("AllLayers")] int layerMask, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction)
        {
            return OverlapSphereNonAlloc_Internal(this, position, radius, results, layerMask, queryTriggerInteraction);
        }

        [FreeFunction("Physics::BoxCast")]
        extern static private bool Query_BoxCast(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float maxDistance, ref RaycastHit outHit, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        private static bool Internal_BoxCast(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Quaternion orientation, Vector3 direction, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            float dirLength = direction.magnitude;
            hitInfo = new RaycastHit();
            if (dirLength > float.Epsilon)
            {
                Vector3 normalizedDirection = direction / dirLength;

                return Query_BoxCast(physicsScene, center, halfExtents, normalizedDirection, orientation, maxDistance, ref hitInfo, layerMask, queryTriggerInteraction);
            }
            else
                return false;
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
        ///<seealso cref="Physics.BoxCast" />
        ///<seealso cref="RaycastHit" />
        public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return Internal_BoxCast(this, center, halfExtents, orientation, direction, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public bool BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, out RaycastHit hitInfo)
        {
            return Internal_BoxCast(this, center, halfExtents, Quaternion.identity, direction, out hitInfo, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [FreeFunction("Physics::OverlapBoxNonAlloc")]
        extern private static int OverlapBoxNonAlloc_Internal(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, [UnityMarshalAs(NativeType.ScriptingObjectPtr)] Collider[] results, Quaternion orientation, int mask, QueryTriggerInteraction queryTriggerInteraction);

        ///<summary>Find all colliders touching or inside of the given box, and store them into the buffer.</summary>
        ///<param name="center">Center of the box.</param>
        ///<param name="halfExtents">Half of the size of the box in each dimension.</param>
        ///<param name="results">The buffer to store the results in.</param>
        ///<param name="orientation">Rotation of the box.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a box.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>The amount of colliders stored in <c>results</c>.</returns>
        ///<seealso cref="Physics.OverlapBoxNonAlloc" />
        public int OverlapBox(Vector3 center, Vector3 halfExtents, Collider[] results, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return OverlapBoxNonAlloc_Internal(this, center, halfExtents, results, orientation, layerMask, queryTriggerInteraction);
        }

        [ExcludeFromDocs]
        public int OverlapBox(Vector3 center, Vector3 halfExtents, Collider[] results)
        {
            return OverlapBoxNonAlloc_Internal(this, center, halfExtents, results, Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }

        [FreeFunction("Physics::BoxCastNonAlloc")]
        private static extern int Internal_BoxCastNonAlloc(PhysicsScene physicsScene, Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] raycastHits, Quaternion orientation, float maxDistance, int mask, QueryTriggerInteraction queryTriggerInteraction);

        ///<summary>Casts the box along a ray and returns detailed information on what was hit.</summary>
        ///<param name="center">Center of the box.</param>
        ///<param name="halfExtents">Half the size of the box in each dimension.</param>
        ///<param name="direction">The direction in which to cast the box.</param>
        ///<param name="results">The buffer to store the results in.</param>
        ///<param name="orientation">Rotation of the box.</param>
        ///<param name="maxDistance">The max length of the cast.</param>
        ///<param name="layerMask">A [Layer mask](xref:Layers) that is used to selectively filter which colliders are considered when casting a box.</param>
        ///<param name="queryTriggerInteraction">Specifies whether this query should hit Triggers.</param>
        ///<returns>The amount of hits stored to the <c>results</c> buffer.</returns>
        ///<seealso cref="Physics.BoxCastNonAlloc" />
        public int BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results, [DefaultValue("Quaternion.identity")] Quaternion orientation, [DefaultValue("Mathf.Infinity")] float maxDistance = Mathf.Infinity, [DefaultValue("DefaultRaycastLayers")] int layerMask = Physics.DefaultRaycastLayers, [DefaultValue("QueryTriggerInteraction.UseGlobal")] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            float dirLength = direction.magnitude;

            if (dirLength > float.Epsilon)
            {
                return Internal_BoxCastNonAlloc(this, center, halfExtents, direction, results, orientation, maxDistance, layerMask, queryTriggerInteraction);
            }
            else
            {
                return 0;
            }
        }

        [ExcludeFromDocs]
        public int BoxCast(Vector3 center, Vector3 halfExtents, Vector3 direction, RaycastHit[] results)
        {
            return BoxCast(center, halfExtents, direction, results, Quaternion.identity, Mathf.Infinity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.UseGlobal);
        }
    }


    /// <summary>
    /// Scope used during scene load operations such as <see cref="UnityEngine.SceneManagement.SceneManager.LoadSceneAsync" />, with the purpose of providing a different physics scene than the global one.
    ///<example>
    ///  <code><![CDATA[
    ///using UnityEngine;
    ///using UnityEngine.SceneManagement;
    ///
    ///public class SimpleLoader : MonoBehaviour
    ///{
    ///
    /// IEnumerator Start()
    /// {
    ///    int buildIndex0 = 0;
    ///    int buildIndex1 = 1;
    ///
    ///    var newScene = SceneManagement.SceneManager.LoadScene(buildIndex0, new LoadSceneParameters() { localPhysicsMode = LocalPhysicsMode.Physics3D });
    ///    var localPhys = newScene.GetPhysicsScene();
    ///
    ///    using(new PhysicsSceneLoadScope(localPhys))
    ///    {
    ///        SceneManagement.SceneManager.LoadSceneAsync(buildIndex1);
    ///    }
    /// }
    /// 
    ///}
    ///
    ///]]></code>
    ///</example>
    /// </summary>
    [NativeHeader("Modules/Physics/Public/PhysicsSceneHandle.h")]
    [NativeHeader("Modules/Physics/Public/PhysicsSceneLoadScope.h")]
    public struct PhysicsSceneLoadScope : IDisposable
    {
        /// <summary>
        /// The physics scene used within this scope.
        /// </summary>
        public PhysicsScene scene { get; private set; } = PhysicsScene.Invalid();

        /// <summary>
        /// Construct a new physics scene loading scope.
        /// </summary>
        /// <param name="physicsScene">A valid physics scene handle.</param>
        /// <exception cref="InvalidOperationException">An exception will be thrown if another scope already exists or the provided scene is invalid.</exception>
        public PhysicsSceneLoadScope(PhysicsScene physicsScene)
        {
            if (!SetLoadingScopeScene(physicsScene))
                throw new InvalidOperationException("Unable to push a new PhysicsSceneLoadScope. A previous scope has not been disposed yet or the handle is invalid.");

            scene = physicsScene;
        }

        /// <summary>
        /// Dispose of the current physics scene loading scope.
        /// </summary>
        public void Dispose()
        {
            PopLoadingScopeScene(scene);
            scene = PhysicsScene.Invalid();
        }

        [FreeFunction("PhysicsSceneLoadScope::SetLoadingScopeScene")]
        extern static bool SetLoadingScopeScene(PhysicsScene scene);
        [FreeFunction("PhysicsSceneLoadScope::PopLoadingScopeScene")]
        extern static void PopLoadingScopeScene(PhysicsScene scene);

        [FreeFunction("PhysicsSceneLoadScope::GetLoadingScopeScene")]
        extern static PhysicsScene GetLoadingScopeScene();
    }

    ///<summary>Scene extensions to access the underlying physics scene.</summary>
    public static class PhysicsSceneExtensions
    {
        ///<summary>An extension method that returns the 3D physics Scene from the Scene.</summary>
        ///<remarks>Returns the <see cref="PhysicsScene" /> that is assigned to the selected <see cref="UnityEngine.SceneManagement.Scene" />. The <see cref="UnityEngine.SceneManagement.Scene" /> may have created its own local 3D physics Scene in which case this provides access to that. Alternately the <see cref="UnityEngine.SceneManagement.Scene" /> may be using the default 3D physics Scene (<see cref="Physics.defaultPhysicsScene" />) in which case that will be returned instead.</remarks>
        ///<param name="scene">The Scene from which to return the 3D physics Scene.</param>
        ///<returns>The 3D physics Scene used by the Scene.</returns>
        ///<seealso cref="PhysicsScene" />
        ///<seealso cref="UnityEngine.SceneManagement.Scene" />
        public static PhysicsScene GetPhysicsScene(this Scene scene)
        {
            if (!scene.IsValid())
                throw new ArgumentException("Cannot get physics scene; Unity scene is invalid.", "scene");

            PhysicsScene physicsScene = GetPhysicsScene_Internal(scene);
            if (physicsScene.IsValid())
                return physicsScene;

            throw new Exception("The physics scene associated with the Unity scene is invalid.");
        }

        [StaticAccessor("GetPhysicsManager()", StaticAccessorType.Dot)]
        [NativeMethod("GetPhysicsSceneFromUnityScene")]
        extern private static PhysicsScene GetPhysicsScene_Internal(Scene scene);
    }
}
