// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.AI
{
    // Keep this enum in sync with the one defined in "NavMeshBindingTypes.h"
    ///<summary>Level of obstacle avoidance.</summary>
    [MovedFrom("UnityEngine")]
    public enum ObstacleAvoidanceType
    {
        ///<summary>Disable avoidance.</summary>
        NoObstacleAvoidance = 0,

        ///<summary>Enable simple avoidance. Low performance impact.</summary>
        LowQualityObstacleAvoidance = 1,

        ///<summary>Medium avoidance. Medium performance impact.</summary>
        MedQualityObstacleAvoidance = 2,

        ///<summary>Good avoidance. High performance impact.</summary>
        GoodQualityObstacleAvoidance = 3,

        ///<summary>Enable highest precision. Highest performance impact.</summary>
        HighQualityObstacleAvoidance = 4
    }

    ///<summary>Navigation mesh agent.</summary>
    ///<remarks>Attach this component to a mobile character in the game to allow the character to use the NavMesh to navigate the scene. For more details refer to  <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/index.html">AI Navigation</see>.</remarks>
    [MovedFrom("UnityEngine")]
    [NativeHeader("Modules/AI/Components/NavMeshAgent.bindings.h")]
    [NativeHeader("Modules/AI/NavMesh/NavMesh.bindings.h")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavMeshAgent.html")]
    public sealed class NavMeshAgent : Behaviour
    {
        ///<summary>Sets or updates the destination thus triggering the calculation for a new path.</summary>
        ///<remarks>Note that the path may not become available until after a few frames later.
        ///While the path is being computed, <see cref="pathPending" /> will be true.
        ///If a valid path becomes available then the agent will resume movement.</remarks>
        ///<param name="target">The target point to navigate to.</param>
        ///<returns>True if the destination was requested successfully, otherwise false.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.AI;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    NavMeshAgent myNavMeshAgent;
        ///    void Start()
        ///    {
        ///        myNavMeshAgent = GetComponent<NavMeshAgent>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        if (Input.GetMouseButtonDown(0))
        ///        {
        ///            SetDestinationToMousePosition();
        ///        }
        ///    }
        ///
        ///    void SetDestinationToMousePosition()
        ///    {
        ///        RaycastHit hit;
        ///        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        ///        if (Physics.Raycast(ray, out hit))
        ///        {
        ///            myNavMeshAgent.SetDestination(hit.point);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern bool SetDestination(Vector3 target);

        ///<summary>Gets or attempts to set the destination of the agent in world-space units.</summary>
        ///<remarks>Getting:
        ///
        ///Returns the destination set for this agent.
        ///
        ///• If a destination is set but the path is not yet processed the position returned will be valid navmesh position that's closest to the previously set position.&lt;br&gt;
        ///• If the agent has no path or requested path - returns the agents position on the navmesh.&lt;br&gt;
        ///• If the agent is not mapped to the navmesh (e.g. Scene has no navmesh) - returns a position at infinity.
        ///
        ///Setting:
        ///
        ///Requests the agent to move to the valid navmesh position that's closest to the requested destination.
        ///
        ///• The path result may not become available until after a few frames. Use <see cref="pathPending" /> to query for outstanding results.&lt;br&gt;
        ///• If it's not possible to find a valid nearby navmesh position (e.g. Scene has no navmesh) no path is requested. Use <see cref="SetDestination" /> and check return value if you need to handle this case explicitly.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.AI;
        ///
        ///[RequireComponent(typeof(NavMeshAgent))]
        ///public class FollowTarget : MonoBehaviour
        ///{
        ///    public Transform target;
        ///    Vector3 destination;
        ///    NavMeshAgent agent;
        ///
        ///    void Start()
        ///    {
        ///        // Cache agent component and destination
        ///        agent = GetComponent<NavMeshAgent>();
        ///        destination = agent.destination;
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        // Update destination if the target moves one unit
        ///        if (Vector3.Distance(destination, target.position) > 1.0f)
        ///        {
        ///            destination = target.position;
        ///            agent.destination = destination;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern Vector3 destination { get; set; }

        ///<summary>Stop within this distance from the target position.</summary>
        ///<remarks>It is seldom possible to land exactly at the target point, so this property can be used to set an acceptable radius within which the agent should stop. A larger stopping distance will give the agent more room for manoeuvre at the end of the path and might avoid sudden braking, turning or other unconvincing AI behaviour.</remarks>
        public extern float stoppingDistance { get; set; }

        ///<summary>Access the current velocity of the <see cref="NavMeshAgent" /> component, or set a velocity to control the agent manually.</summary>
        ///<remarks>Reading the variable will return the current velocity of the agent based on the crowd simulation.
        ///
        ///Setting the variable will override the simulation (including: moving towards destination, collision avoidance, and acceleration control) and command the NavMesh Agent to move using the specific velocity directly. When the agent is controlled using a velocity, its movement is still constrained on the NavMesh.
        ///
        ///Setting the velocity directly, can be used for implementing player characters, which are moving on NavMesh and affecting the rest of the simulated crowd. In addition, setting priority to high (a small value is higher priority), will make other simulated agents to avoid the player controlled agent even more eagerly.
        ///
        ///It is recommended to set the velocity each frame when controlling the agent manually, and if releasing the control to the simulation, set the velocity to zero. If agent’s velocity is set to some value and then stopped updating it, the simulation will pick up from there and the agent will slowly decelerate (assuming no destination is set).
        ///
        ///Note that reading the velocity will always return value from the simulation. If you set the value, the effect will show up in the next update. Since the returned velocity comes from the simulation (including avoidance and collision handling), it can be different than the one you set.
        ///
        ///The velocity is specified in distance units per second (same as physics), and represented in global coordinate system.</remarks>
        public extern Vector3 velocity { get; set; }

        ///<summary>Gets or sets the simulation position of the navmesh agent.</summary>
        ///<remarks>
        ///  <para>The position vector is in world space coordinates and units.
        ///
        ///The nextPosition is coupled to <see cref="Transform.position" />. In the default case the navmesh agent's Transform position will match the internal simulation position at the time the script Update function is called. This coupling can be turned on and off by setting <see cref="updatePosition" />.
        ///
        ///When <see cref="updatePosition" /> is true, the <see cref="Transform.position" /> reflects the simulated position, when false the position of the transform and the navmesh agent is not synchronized, and you'll see a difference between the two in general. When <see cref="updatePosition" /> is turned back on, the <see cref="Transform.position" /> will be immediately move to match nextPosition.
        ///
        ///By setting nextPosition you can directly control where the internal agent position should be. The agent will be moved towards the position, but is constrained by the navmesh connectivity and boundaries. As such it will be useful only if the positions are continuously updated and assessed.
        ///</para>
        ///  <para>Additionally it can be useful to control the agent position directly - especially if the
        ///gameobject transform is controlled by something else - e.g. animator, physics, scripted or input.</para>
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.AI;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        // Update the transform position explicitly in the OnAnimatorMove callback
        ///        GetComponent<NavMeshAgent>().updatePosition = false;
        ///    }
        ///
        ///    void OnAnimatorMove()
        ///    {
        ///        transform.position = GetComponent<NavMeshAgent>().nextPosition;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.AI;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public bool agentIsControlledByOther;
        ///    void Update()
        ///    {
        ///        var agent = GetComponent<NavMeshAgent>();
        ///        agent.updatePosition = !agentIsControlledByOther;
        ///        if (agentIsControlledByOther)
        ///        {
        ///            GetComponent<NavMeshAgent>().nextPosition = transform.position;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Warp" />
        ///<seealso cref="Move" />
        [NativeProperty("Position")]
        public extern Vector3 nextPosition { get; set; }

        ///<summary>Get the current steering target along the path.</summary>
        ///<remarks>This is typically the next corner along the path or the end point of the path.
        ///
        ///Unless the agent is moving on an <see cref="OffMeshLink" />, there is a straight path between the agent and the steeringTarget.
        ///
        ///When approaching an OffMeshLink for traversal - the value is the position where the agent will enter the link.
        ///While agent is traversing an OffMeshLink the value is the position where the agent will leave the link.</remarks>
        public extern Vector3 steeringTarget { get; }

        ///<summary>The desired velocity of the agent including any potential contribution from avoidance.</summary>
        public extern Vector3 desiredVelocity { get; }

        ///<summary>The distance between the agent's position and the destination on the current path.</summary>
        ///<remarks>If the remaining distance is unknown then this will have a value of infinity.</remarks>
        public extern float remainingDistance { get; }

        ///<summary>The relative vertical displacement of the owning <see cref="GameObject" />.</summary>
        public extern float baseOffset { get; set; }

        ///<summary>Is the agent currently positioned on an OffMeshLink?</summary>
        ///<remarks>This property is useful when <see cref="autoTraverseOffMeshLink" /> is false and custom movement is needed when crossing the link.</remarks>
        ///<seealso cref="autoTraverseOffMeshLink" />
        ///<seealso cref="CompleteOffMeshLink" />
        public extern bool isOnOffMeshLink
        {
            [NativeName("IsOnOffMeshLink")]
            get;
        }

        ///<summary>Enables or disables the current off-mesh link.</summary>
        ///<remarks>This function activates or deactivates the off-mesh link
        ///where the agent is currently waiting. This is useful for
        ///granting access to newly discovered areas of the game world or
        ///simulating the creation or removal of an obstacle to an area.</remarks>
        ///<param name="activated">Is the link activated?</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.AI;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    private NavMeshAgent agent;
        ///    void Start() {
        ///        agent = GetComponent<NavMeshAgent>();
        ///    }
        ///    void OpenDiscoveredArea(Hashtable areasDiscovered) {
        ///        if (agent.isOnOffMeshLink)
        ///            if (areasDiscovered.ContainsKey(agent.currentOffMeshLinkData.offMeshLink.name))
        ///                agent.ActivateCurrentOffMeshLink(true);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern void ActivateCurrentOffMeshLink(bool activated);

        ///<summary>The current <see cref="OffMeshLinkData" />.</summary>
        ///<remarks>In the case that this agent is not on an OffMeshLink the <see cref="OffMeshLinkData" /> is marked as invalid. See also <see cref="isOnOffMeshLink" /></remarks>
        public OffMeshLinkData currentOffMeshLinkData => GetCurrentOffMeshLinkDataInternal();

        [FreeFunction("NavMeshAgentScriptBindings::GetCurrentOffMeshLinkDataInternal", HasExplicitThis = true)]
        internal extern OffMeshLinkData GetCurrentOffMeshLinkDataInternal();

        ///<summary>The next <see cref="OffMeshLinkData" /> on the current path.</summary>
        ///<remarks>In the case that the current path does not contain an OffMeshLink the <see cref="OffMeshLinkData" /> is marked as invalid.</remarks>
        public OffMeshLinkData nextOffMeshLinkData => GetNextOffMeshLinkDataInternal();

        [FreeFunction("NavMeshAgentScriptBindings::GetNextOffMeshLinkDataInternal", HasExplicitThis = true)]
        internal extern OffMeshLinkData GetNextOffMeshLinkDataInternal();

        ///<summary>Completes the movement on the current OffMeshLink.</summary>
        ///<remarks>The agent will move to the closest valid navmesh position on the other end of the current OffMeshLink.
        ///
        ///CompleteOffMeshLink has no effect unless the agent is on an OffMeshLink ().
        ///
        ///When <see cref="autoTraverseOffMeshLink" /> is disabled an agent will pause at an off-mesh link until this function is called.
        ///It is useful for implementing custom movement across OffMeshLinks.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.AI;
        ///using System.Collections;
        ///
        ///public enum OffMeshLinkMoveMethod
        ///{
        ///    Teleport,
        ///    NormalSpeed,
        ///    Parabola
        ///}
        ///
        ///[RequireComponent(typeof(NavMeshAgent))]
        ///public class AgentLinkMover : MonoBehaviour
        ///{
        ///    public OffMeshLinkMoveMethod method = OffMeshLinkMoveMethod.Parabola;
        ///    IEnumerator Start()
        ///    {
        ///        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        ///        agent.autoTraverseOffMeshLink = false;
        ///        while (true)
        ///        {
        ///            if (agent.isOnOffMeshLink)
        ///            {
        ///                if (method == OffMeshLinkMoveMethod.NormalSpeed)
        ///                    yield return StartCoroutine(NormalSpeed(agent));
        ///                else if (method == OffMeshLinkMoveMethod.Parabola)
        ///                    yield return StartCoroutine(Parabola(agent, 2.0f, 0.5f));
        ///                agent.CompleteOffMeshLink();
        ///            }
        ///            yield return null;
        ///        }
        ///    }
        ///
        ///    IEnumerator NormalSpeed(NavMeshAgent agent)
        ///    {
        ///        OffMeshLinkData data = agent.currentOffMeshLinkData;
        ///        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
        ///        while (agent.transform.position != endPos)
        ///        {
        ///            agent.transform.position = Vector3.MoveTowards(agent.transform.position, endPos, agent.speed * Time.deltaTime);
        ///            yield return null;
        ///        }
        ///    }
        ///
        ///    IEnumerator Parabola(NavMeshAgent agent, float height, float duration)
        ///    {
        ///        OffMeshLinkData data = agent.currentOffMeshLinkData;
        ///        Vector3 startPos = agent.transform.position;
        ///        Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
        ///        float normalizedTime = 0.0f;
        ///        while (normalizedTime < 1.0f)
        ///        {
        ///            float yOffset = height * 4.0f * (normalizedTime - normalizedTime * normalizedTime);
        ///            agent.transform.position = Vector3.Lerp(startPos, endPos, normalizedTime) + yOffset * Vector3.up;
        ///            normalizedTime += Time.deltaTime / duration;
        ///            yield return null;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="isOnOffMeshLink" />
        public extern void CompleteOffMeshLink();

        ///<summary>Should the agent move across OffMeshLinks automatically?</summary>
        ///<remarks>Off-mesh links are used to connect disjoint regions of the NavMesh. Usually, a character should be able to pass through or traverse a link automatically, which will happen if this property is set to true. However, it can also be set to false in cases where special control over movement is needed.</remarks>
        ///<seealso cref="isOnOffMeshLink" />
        ///<seealso cref="CompleteOffMeshLink" />
        public extern bool autoTraverseOffMeshLink { get; set; }

        ///<summary>Should the agent brake automatically to avoid overshooting the destination point?</summary>
        ///<remarks>If the agent needs to land close to the destination point then it will typically need to brake to avoid overshooting or endless "orbiting" around the target zone. If this property is set to true, the agent will brake automatically as it nears the destination.</remarks>
        public extern bool autoBraking { get; set; }

        ///<summary>Should the agent attempt to acquire a new path if the existing path becomes invalid?</summary>
        ///<remarks>A new path calculation is also attempted aquired if the agent reaches the end of a partial and stale path.</remarks>
        public extern bool autoRepath { get; set; }

        ///<summary>Does the agent currently have a path? (RO)</summary>
        ///<remarks>This property will be true if the agent has a path calculated to the desired destination and false otherwise.</remarks>
        public extern bool hasPath
        {
            [NativeName("HasPath")]
            get;
        }

        ///<summary>Is a path in the process of being computed but not yet ready? (RO)</summary>
        public extern bool pathPending
        {
            [NativeName("PathPending")]
            get;
        }

        ///<summary>Is the current path stale. (RO)</summary>
        ///<remarks>When true, the path may no longer be valid or optimal.
        ///This flag will be set if: there are any changes to the <see cref="areaMask" />, if any <see cref="OffMeshLink" /> is enabled or disabled, or if the costs for the NavMeshAreas have been changed.</remarks>
        public extern bool isPathStale
        {
            [NativeName("IsPathStale")]
            get;
        }

        ///<summary>The status of the current path (complete, partial or invalid).</summary>
        ///<remarks>Returns <see cref="NavMeshPathStatus.PathInvalid" /> if either the path is invalid, or the agent is not yet initialized. (.)</remarks>
        ///<seealso cref="NavMeshPath.status" />
        public extern NavMeshPathStatus pathStatus { get; }

        ///<exclude />
        [NativeProperty("EndPositionOfCurrentPath")]
        public extern Vector3 pathEndPosition { get; }

        ///<summary>Warps agent to the provided position.</summary>
        ///<remarks>Returns true if successful, otherwise returns false.</remarks>
        ///<param name="newPosition">New position to warp the agent to.</param>
        ///<returns>True if agent is successfully warped, otherwise false.</returns>
        public extern bool Warp(Vector3 newPosition);

        ///<summary>Apply relative movement to current position.</summary>
        ///<remarks>If the agent has a path it will be adjusted.</remarks>
        ///<param name="offset">The relative movement vector.</param>
        public extern void Move(Vector3 offset);

        ///<summary>Stop movement of this agent along its current path.</summary>
        ///<remarks>See <see cref="Resume" /> for how to resume movement after stopping.</remarks>
        [Obsolete("Set isStopped to true instead.")]
        public extern void Stop();

        [Obsolete("Set isStopped to true instead.")]
        public void Stop(bool stopUpdates) { Stop(); }

        ///<summary>Resumes the movement along the current path after a pause.</summary>
        ///<remarks>See <see cref="Stop" /> for how to pause movement along the current path.</remarks>
        [Obsolete("Set isStopped to false instead.")]
        public extern void Resume();

        ///<summary>Use this property to set, or get, whether the NavMesh agent stops or continues its movement along the current path.</summary>
        ///<remarks>If set to true, the NavMesh agent's movement stops along its current path. If set to false after the NavMesh agent has stopped, the NavMesh agent resumes its movement along the current path.</remarks>
        public extern bool isStopped
        {
            [FreeFunction("NavMeshAgentScriptBindings::GetIsStopped", HasExplicitThis = true)]
            get;
            [FreeFunction("NavMeshAgentScriptBindings::SetIsStopped", HasExplicitThis = true)]
            set;
        }

        ///<summary>Clears the current path.</summary>
        ///<remarks>When the path is cleared, the agent will not start looking for a new path until SetDestination is called.
        ///
        ///Note that if the agent is on an OffMeshLink when this function is called, it will complete the link immediately.</remarks>
        public extern void ResetPath();

        ///<summary>Assign a new path to this agent.</summary>
        ///<remarks>If you successfully assign the path, the agent resumes movement toward the new target.
        ///If the path cannot be assigned, the path is cleared (see <see cref="ResetPath" />).
        ///A path that was calculated for a different agent type than this agent's <see cref="agentTypeID" /> is ignored: the method returns false and the agent keeps its current path. Use <see cref="CalculatePath" /> or <see cref="NavMesh.CalculatePath" /> with a <c>NavMeshQueryFilter</c> to obtain a path for this agent type.</remarks>
        ///<param name="path">New path to follow.</param>
        ///<returns>True if the path is successfully assigned.</returns>
        public extern bool SetPath([NotNull] NavMeshPath path);

        ///<summary>Property to get and set the current path.</summary>
        ///<remarks>This property can be useful for GUI, debugging and other purposes to get the points of the path calculated by the navigation system. Additionally, a path created from user code can be set for the agent to follow in the usual way. An example of this might be a patrol route designed for coverage rather than optimal distance between two points.</remarks>
        public NavMeshPath path
        {
            get
            {
                NavMeshPath result = new NavMeshPath();
                CopyPathTo(result);
                return result;
            }
            set
            {
                if (value == null)
                    throw new NullReferenceException();
                SetPath(value);
            }
        }

        [NativeMethod("CopyPath")]
        internal extern void CopyPathTo([NotNull] NavMeshPath path);

        ///<summary>Locate the closest NavMesh edge.</summary>
        ///<remarks>The returned <see cref="NavMeshHit" /> object contains the position
        ///and details of the nearest point on the nearest edge of the
        ///Navmesh. Since an edge typically corresponds to a wall or
        ///other large object, this could be used to make a character
        ///take cover as close to the wall as possible.</remarks>
        ///<param name="hit">Holds the properties of the resulting location.</param>
        ///<returns>True if a nearest edge is found.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.AI;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour {
        ///    private NavMeshAgent agent;
        ///    void Start() {
        ///        agent = GetComponent<NavMeshAgent>();
        ///    }
        ///
        ///    void Update() {
        ///        if (Input.GetMouseButtonDown(0))
        ///            TakeCover();
        ///    }
        ///
        ///    void TakeCover() {
        ///        NavMeshHit hit;
        ///        if (agent.FindClosestEdge(out hit))
        ///            agent.SetDestination(hit.position);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeName("DistanceToEdge")]
        public extern bool FindClosestEdge(out NavMeshHit hit);

        ///<summary>Trace a straight path towards a target postion in the NavMesh without moving the agent.</summary>
        ///<remarks>This function follows the path of a "ray" between the agent's
        ///position and the specified target position. If an obstruction is
        ///encountered along the line then a true value is returned and
        ///the position and other details of the obstructing object are stored
        ///in the <c>hit</c> parameter. This can be used to check if there is a clear
        ///shot or line of sight between a character and a target object.
        ///This function is preferable to the similar <see cref="M:UnityEngine.Physics.Raycast" />
        ///because the line tracing is performed in a simpler way using the navmesh
        /// and has a lower processing overhead.</remarks>
        ///<param name="targetPosition">The desired end position of movement.</param>
        ///<param name="hit">Properties of the obstacle detected by the ray (if any).</param>
        ///<returns>True if there is an obstacle between the agent and the target position, otherwise false.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.AI;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Transform target;
        ///    private NavMeshAgent agent;
        ///
        ///    void Start()
        ///    {
        ///        agent = GetComponent<NavMeshAgent>();
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        NavMeshHit hit;
        ///        if (!agent.Raycast(target.position, out hit))
        ///        {
        ///            // Target is "visible" from our position.
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern bool Raycast(Vector3 targetPosition, out NavMeshHit hit);

        ///<summary>Calculate a path to a specified point and store the resulting path.</summary>
        ///<remarks>Use this function to avoid gameplay delays by planning a path before it is needed. You can also use this function to check if a target position is reachable before moving the agent. The function takes into account the agent's <see cref="areaMask" />, <see cref="agentTypeID" /> and <see cref="NavMeshAgent.GetAreaCost">area costs</see> properties when it searches for a matching path.
        ///
        ///This function is synchronous. It performs path finding immediately, which can adversely affect the frame rate when processing very long paths. It is recommended to only perform a few path finds per frame when, for example, evaluating distances to cover points.
        ///
        ///Use the returned path to set the path for this agent, or an agent of the same type, with <see cref="NavMeshAgent.SetPath" />. For SetPath to work, the agent must be close to the starting point and be allowed to move through the <see cref="NavMeshAgent.areaMask">area type</see> where the start point is.</remarks>
        ///<param name="targetPosition">The final position of the path requested.</param>
        ///<param name="path">The resulting path.</param>
        ///<returns>True if either a complete or partial path is found. False otherwise.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.AI;
        ///
        ///[RequireComponent(typeof(NavMeshAgent))]
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Transform target;
        ///
        ///    void Start()
        ///    {
        ///        if (target == null)
        ///            return;
        ///
        ///        var agent = GetComponent<NavMeshAgent>();
        ///        var path = new NavMeshPath();
        ///        agent.CalculatePath(target.position, path);
        ///        switch (path.status)
        ///        {
        ///            case NavMeshPathStatus.PathComplete:
        ///                Debug.Log($"{agent.name} will be able to reach {target.name}.");
        ///                break;
        ///            case NavMeshPathStatus.PathPartial:
        ///                Debug.LogWarning($"{agent.name} will only be able to move partway to {target.name}.");
        ///                break;
        ///            default:
        ///                Debug.LogError($"There is no valid path for {agent.name} to reach {target.name}.");
        ///                break;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public bool CalculatePath(Vector3 targetPosition, NavMeshPath path)
        {
            path.ClearCorners();
            return CalculatePathInternal(targetPosition, path);
        }

        [FreeFunction("NavMeshAgentScriptBindings::CalculatePathInternal", HasExplicitThis = true)]
        extern bool CalculatePathInternal(Vector3 targetPosition, [NotNull] NavMeshPath path);

        ///<summary>Sample a position along the current path.</summary>
        ///<remarks>This function looks ahead the specified <c>maxDistance</c> along the current path, up to the third
        ///                    <see cref="NavMeshPath.corners">corner</see>. It returns details of the mesh
        ///                    at that position in a <see cref="NavMeshHit" /> object. You can use this
        ///                    to check the type of surface that lies ahead before the character gets there. For example, characters could
        ///                    raise their guns above their heads if they are about to wade through water.
        ///
        ///                    If the path sampling terminates on an outer edge, <c>hit.mask</c> is 0. If the path sampling terminates at an area not specified by <c>areaMask</c>, <c>hit.mask</c> contains the area mask of the blocking polygon. If the sampling reaches the end of the path, or the limit at the path's third corner, <c>hit.mask</c> contains the area mask at that position on the NavMesh.</remarks>
        ///<param name="areaMask">A bitfield mask specifying which NavMesh areas can be passed when tracing the path.</param>
        ///<param name="maxDistance">Terminate scanning the path at this distance.</param>
        ///<param name="hit">Holds the properties of the resulting location.</param>
        ///<returns>True if terminated before reaching the position at <c>maxDistance</c>, false otherwise.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.AI;
        ///using System.Collections;
        ///
        ///public class ExampleClass : MonoBehaviour
        ///{
        ///    public Transform target;
        ///    private NavMeshAgent agent;
        ///    private int waterMask;
        ///
        ///
        ///    void Start()
        ///    {
        ///        agent = GetComponent<NavMeshAgent>();
        ///        waterMask = 1 << NavMesh.GetAreaFromName("Water");
        ///        agent.SetDestination(target.position);
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        NavMeshHit hit;
        ///
        ///        // Check all areas one length unit ahead.
        ///        if (!agent.SamplePathPosition(NavMesh.AllAreas, 1.0F, out hit))
        ///            if ((hit.mask & waterMask) != 0)
        ///            {
        ///                // Water detected along the path...
        ///            }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern bool SamplePathPosition(int areaMask, float maxDistance, out NavMeshHit hit);

        ///<summary>Sets the cost for traversing over geometry of the layer type.</summary>
        ///<remarks>If you enable or disable the agent then the cost will be reset to the default layer cost.</remarks>
        ///<param name="layer">Layer index.</param>
        ///<param name="cost">New cost for the specified layer.</param>
        [Obsolete("Use SetAreaCost instead.")]
        [NativeMethod("SetAreaCost")]
        public extern void SetLayerCost(int layer, float cost);

        ///<summary>Gets the cost for crossing ground of a particular type.</summary>
        ///<remarks>The cost of a path is the amount of "difficulty" involved in following it - the shortest path may not be the quickest if it passes over difficult ground, such as mud, snow, etc. Different types of ground are denoted by navmesh layers in Unity. The cost of a particular layer is given in cost units per distance unit. Note that the cost of a path applies to the pathfinding only and does not automatically affect the movement speed of the agent when following the path. Indeed, the path's cost may denote other factors such as danger (safe but long path through a minefield) or visibility (long path that keeps a character in the shadows).</remarks>
        ///<param name="layer">Layer index.</param>
        ///<returns>Current cost of specified layer.</returns>
        [Obsolete("Use GetAreaCost instead.")]
        [NativeMethod("GetAreaCost")]
        public extern float GetLayerCost(int layer);

        ///<summary>Sets the cost for traversing over areas of the area type.</summary>
        ///<remarks>If you enable or disable the agent then the cost will be reset to the default layer cost.</remarks>
        ///<param name="areaIndex">Area cost.</param>
        ///<param name="areaCost">New cost for the specified area index.</param>
        public extern void SetAreaCost(int areaIndex, float areaCost);

        ///<summary>Gets the cost for path calculation when crossing area of a particular type.</summary>
        ///<remarks>The cost of a path is the amount of "difficulty" involved in calculating it - the shortest path may not be the best if it passes over difficult ground, such as mud, snow, etc. Different types of areas are denoted by navmesh areas in Unity. The cost of a particular area is given in cost units per distance unit. Note that the cost of a path applies to the pathfinding only and does not automatically affect the movement speed of the agent when following the path. Indeed, the path's cost may denote other factors such as danger (safe but long path through a minefield) or visibility (long path that keeps a character in the shadows).</remarks>
        ///<param name="areaIndex">Area Index.</param>
        ///<returns>Current cost for specified area index.</returns>
        public extern float GetAreaCost(int areaIndex);

        ///<summary>Returns the owning object of the NavMesh the agent is currently placed on.</summary>
        ///<remarks>If no owner is set for a NavMesh or link instance the return value is null.</remarks>
        ///<seealso cref="NavMeshDataInstance.owner" />
        ///<seealso cref="NavMesh.GetLinkOwner" />
        public Object navMeshOwner => GetOwnerInternal();

        ///<summary>The type ID for the agent.</summary>
        ///<remarks>This identifier determines which NavMeshes are available for the Agent to move on. See also <see cref="NavMeshBuildSettings.agentTypeID" />. Changing this ID will reset the Agent's current path.</remarks>
        public extern int agentTypeID { get; set; }

        [NativeName("GetCurrentPolygonOwner")]
        extern Object GetOwnerInternal();

        ///<summary>Specifies which NavMesh layers are passable (bitfield). Changing <c>walkableMask</c> will make the path stale (see <see cref="isPathStale" />).</summary>
        [Obsolete("Use areaMask instead.")]
        public int walkableMask { get { return areaMask; } set { areaMask = value; } }

        ///<summary>Specifies which NavMesh areas are passable. Changing <c>areaMask</c> will make the path stale (see <see cref="isPathStale" />).</summary>
        ///<remarks>This is a bitfield.</remarks>
        public extern int areaMask { get; set; }

        ///<summary>Maximum movement speed when following a path.</summary>
        ///<remarks>An agent will typically need to speed up and slow down as it follows a path (eg, it will slow down to make a tight turn). The speed is often limited by the length of a path segment and the time taken to accelerate and brake, but the speed will not exceed the value set by this property even on a long, straight path.</remarks>
        public extern float speed { get; set; }

        ///<summary>Maximum turning speed in (deg/s) while following a path.</summary>
        ///<remarks>This is the maximum rate at which the agent can turn as it rounds the "corner" defined by a waypoint. The actual turning circle is also influenced by the speed of the agent on approach and also the maximum acceleration.</remarks>
        public extern float angularSpeed { get; set; }

        ///<summary>The maximum acceleration of an agent as it follows a path, given in units / sec^2.</summary>
        ///<remarks>An agent does not follow precisely the line segments of the path calculated by the navigation system but rather uses the waypoints along the path as intermediate destinations. This value is the maximum amount by which the agent can accelerate while moving towards the next waypoint.</remarks>
        public extern float acceleration { get; set; }

        ///<summary>Gets or sets whether the transform position is synchronized with the simulated agent position. The default value is true.</summary>
        ///<remarks>When true: changing the transform position will affect the simulated position and vice-versa.
        ///
        ///When false: the simulated position will not be applied to the transform position and vice-versa.
        ///
        ///Setting <see cref="updatePosition" /> to false can be used to enable explicit control of the transform position via script.
        ///This allows you to use the agent's simulated position to drive another component, which in turn sets the transform position (eg. animation with root motion or physics).
        ///
        ///When enabling the <see cref="updatePosition" /> (from previously being disabled), the transform will be moved to the simulated position. This way the agent stays constrained to the navmesh surface.</remarks>
        public extern bool updatePosition { get; set; }

        ///<summary>Should the agent update the transform orientation?</summary>
        public extern bool updateRotation { get; set; }

        ///<summary>Allows you to specify whether the agent should be aligned to the up-axis of the NavMesh or link that it is placed on.</summary>
        ///<remarks>When this value is set to true, the agent will always be aligned to the local up-axis of the NavMesh or link that it is currently on. When set to false, the agent’s orientation is unaffected by the orientation of the NavMesh.</remarks>
        public extern bool updateUpAxis { get; set; }

        ///<summary>The avoidance radius for the agent.</summary>
        ///<remarks>This is the agent's "personal space" within which obstacles and other agents should not pass.</remarks>
        public extern float radius { get; set; }

        ///<summary>The height of the agent for purposes of passing under obstacles, etc.</summary>
        public extern float height { get; set; }

        ///<summary>The level of quality of avoidance.</summary>
        ///<remarks>This property lets you trade off the precision of obstacle avoidance againt the processor load required to achieve it. The exact quality/performance values will depend heavily on the complexity of the Scene but as a general rule, faster performance can be achieved at the cost of quality and vice versa.</remarks>
        public extern ObstacleAvoidanceType obstacleAvoidanceType { get; set; }

        ///<summary>The avoidance priority level.</summary>
        ///<remarks>When the agent is performing avoidance, agents of lower priority are ignored.
        ///The valid range is from 0 to 99 where:
        ///Most important = 0. Least important = 99. Default = 50.</remarks>
        public extern int avoidancePriority { get; set; }

        ///<summary>Is the agent currently bound to the navmesh?</summary>
        ///<remarks>This property is false if the agent, for some reason, could not bind to the navmesh. E.g. if Scene has no navmesh.</remarks>
        public extern bool isOnNavMesh
        {
            [NativeName("InCrowdSystem")]
            get;
        }
    }
}
