// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.AI
{
    // Keep this enum in sync with the one defined in "NavMeshBindingTypes.h"
    [MovedFrom("UnityEngine")]
    public enum ObstacleAvoidanceType
    {
        // Disable avoidance.
        NoObstacleAvoidance = 0,

        // Enable simple avoidance. Low performance impact.
        LowQualityObstacleAvoidance = 1,

        // Medium avoidance. Medium performance impact
        MedQualityObstacleAvoidance = 2,

        // Good avoidance. High performance impact
        GoodQualityObstacleAvoidance = 3,

        // Enable highest precision. Highest performance impact.
        HighQualityObstacleAvoidance = 4
    }

    // Navigation mesh agent.
    [MovedFrom("UnityEngine")]
    [NativeHeader("Modules/AI/Components/NavMeshAgent.bindings.h")]
    [NativeHeader("Modules/AI/NavMesh/NavMesh.bindings.h")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavMeshAgent.html")]
    public sealed class NavMeshAgent : Behaviour
    {
        // Sets or updates the destination. This triggers calculation for a new path.
        public extern bool SetDestination(Vector3 target);

        // Destination to navigate towards.
        public extern Vector3 destination { get; set; }

        // Stop within this distance from the target position.
        public extern float stoppingDistance { get; set; }

        // The current velocity of the [[NavMeshAgent]] component.
        public extern Vector3 velocity { get; set; }

        // The next position on the path.
        [NativeProperty("Position")]
        public extern Vector3 nextPosition { get; set; }

        // The current steering target - usually the next corner or end point of the current path. (RO)
        public extern Vector3 steeringTarget { get; }

        // The desired velocity of the agent including any potential contribution from avoidance. (RO)
        public extern Vector3 desiredVelocity { get; }

        // Remaining distance along the current path - or infinity when not known. (RO)
        public extern float remainingDistance { get; }

        // The relative vertical displacement of the owning [[GameObject]].
        public extern float baseOffset { get; set; }

        // Is agent currently positioned on an OffMeshLink. (RO)
        public extern bool isOnOffMeshLink
        {
            [NativeName("IsOnOffMeshLink")]
            get;
        }

        // Enables or disables the current link.
        public extern void ActivateCurrentOffMeshLink(bool activated);

        // The current [[OffMeshLinkData]].
        public OffMeshLinkData currentOffMeshLinkData => GetCurrentOffMeshLinkDataInternal();

        [FreeFunction("NavMeshAgentScriptBindings::GetCurrentOffMeshLinkDataInternal", HasExplicitThis = true)]
        internal extern OffMeshLinkData GetCurrentOffMeshLinkDataInternal();

        // The next [[OffMeshLinkData]] on the current path.
        public OffMeshLinkData nextOffMeshLinkData => GetNextOffMeshLinkDataInternal();

        [FreeFunction("NavMeshAgentScriptBindings::GetNextOffMeshLinkDataInternal", HasExplicitThis = true)]
        internal extern OffMeshLinkData GetNextOffMeshLinkDataInternal();

        // Terminate OffMeshLink occupation and transfer the agent to the closest point on other side.
        public extern void CompleteOffMeshLink();

        // Automate movement onto and off of OffMeshLinks.
        public extern bool autoTraverseOffMeshLink { get; set; }

        // Automate braking of NavMeshAgent to avoid overshooting the destination.
        public extern bool autoBraking { get; set; }

        // Attempt to acquire a new path if the existing path becomes invalid
        public extern bool autoRepath { get; set; }

        // Does this agent currently have a path. (RO)
        public extern bool hasPath
        {
            [NativeName("HasPath")]
            get;
        }

        // A path is being computed, but not yet ready. (RO)
        public extern bool pathPending
        {
            [NativeName("PathPending")]
            get;
        }

        // Is the current path stale. (RO)
        public extern bool isPathStale
        {
            [NativeName("IsPathStale")]
            get;
        }

        // Query the state of the current path.
        public extern NavMeshPathStatus pathStatus { get; }

        //*undocumented*
        [NativeProperty("EndPositionOfCurrentPath")]
        public extern Vector3 pathEndPosition { get; }

        //*undocumented*
        public extern bool Warp(Vector3 newPosition);

        // Apply relative movement to current position.
        public extern void Move(Vector3 offset);

        [Obsolete("Set isStopped to true instead.")]
        public extern void Stop();

        // Stop movement of this agent along its current path.
        [Obsolete("Set isStopped to true instead.")]
        public void Stop(bool stopUpdates) { Stop(); }

        // Resumes the movement along the current path.
        [Obsolete("Set isStopped to false instead.")]
        public extern void Resume();

        public extern bool isStopped
        {
            [FreeFunction("NavMeshAgentScriptBindings::GetIsStopped", HasExplicitThis = true)]
            get;
            [FreeFunction("NavMeshAgentScriptBindings::SetIsStopped", HasExplicitThis = true)]
            set;
        }

        // Clears the current path. Note that this agent will not start looking for a new path until SetDestination is called.
        public extern void ResetPath();

        // Assign path to this agent.
        public extern bool SetPath([NotNull] NavMeshPath path);

        // Set or get a copy of the current path.
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

        // Locate the closest NavMesh edge.
        [NativeName("DistanceToEdge")]
        public extern bool FindClosestEdge(out NavMeshHit hit);

        // Trace movement towards a target position in the NavMesh. Without moving the agent.
        public extern bool Raycast(Vector3 targetPosition, out NavMeshHit hit);

        // Calculate a path to a specified point and store the resulting path.
        public bool CalculatePath(Vector3 targetPosition, NavMeshPath path)
        {
            path.ClearCorners();
            return CalculatePathInternal(targetPosition, path);
        }

        [FreeFunction("NavMeshAgentScriptBindings::CalculatePathInternal", HasExplicitThis = true)]
        extern bool CalculatePathInternal(Vector3 targetPosition, [NotNull] NavMeshPath path);

        // Sample a position along the current path.
        public extern bool SamplePathPosition(int areaMask, float maxDistance, out NavMeshHit hit);

        [Obsolete("Use SetAreaCost instead.")]
        [NativeMethod("SetAreaCost")]
        public extern void SetLayerCost(int layer, float cost);

        [Obsolete("Use GetAreaCost instead.")]
        [NativeMethod("GetAreaCost")]
        public extern float GetLayerCost(int layer);

        public extern void SetAreaCost(int areaIndex, float areaCost);

        public extern float GetAreaCost(int areaIndex);

        public Object navMeshOwner => GetOwnerInternal();

        public extern int agentTypeID { get; set; }

        [NativeName("GetCurrentPolygonOwner")]
        extern Object GetOwnerInternal();

        [Obsolete("Use areaMask instead.")]
        public int walkableMask { get { return areaMask; } set { areaMask = value; } }

        public extern int areaMask { get; set; }

        // Maximum movement speed.
        public extern float speed { get; set; }

        // Maximum rotation speed in (deg/s).
        public extern float angularSpeed { get; set; }

        // Maximum acceleration.
        public extern float acceleration { get; set; }

        // Should the agent update the transform position.
        public extern bool updatePosition { get; set; }

        // Should the agent update the transform orientation.
        public extern bool updateRotation { get; set; }

        public extern bool updateUpAxis { get; set; }

        // Agent avoidance radius.
        public extern float radius { get; set; }

        // Agent height.
        public extern float height { get; set; }

        // The level of quality of avoidance.
        public extern ObstacleAvoidanceType obstacleAvoidanceType { get; set; }

        // The avoidance priority level.
        public extern int avoidancePriority { get; set; }

        // Is agent mapped to navmesh
        public extern bool isOnNavMesh
        {
            [NativeName("InCrowdSystem")]
            get;
        }
    }
}
