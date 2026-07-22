// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.AI
{
    // Keep this enum in sync with the one defined in "NavMeshBindingTypes.h"
    ///<summary>Shape of the obstacle.</summary>
    [MovedFrom("UnityEngine")]
    public enum NavMeshObstacleShape
    {
        ///<summary>Capsule shaped obstacle.</summary>
        Capsule = 0,
        ///<summary>Box shaped obstacle.</summary>
        Box = 1,
    }

    ///<summary>An obstacle for NavMeshAgents to avoid.</summary>
    ///<remarks>A NavMeshObstacle is cylindrical in shape and can move around the surface of the NavMesh with a specified velocity. By default, the obstacle will only affect the agent's avoidance behaviour rather than the pathfinding. This means that the agent will ignore the obstacle when plotting a path but will sidestep around it while moving along the path. If carving is enabled, the obstacle will create a temporary "hole" in the NavMesh. The hole will be recognised by the pathfinding, so paths will be plotted to avoid the obstacle. This means that if, say, an obstacle blocks a narrow gap, the pathfinding will seek an alternative route to the target. Without carving, the agent will head for the gap but won't be able to pass until the obstacle is clear.</remarks>
    ///<seealso cref="NavMeshAgent" />
    [MovedFrom("UnityEngine")]
    [NativeHeader("Modules/AI/Components/NavMeshObstacle.bindings.h")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavMeshObstacle.html")]
    public sealed class NavMeshObstacle : Behaviour
    {
        ///<summary>Height of the obstacle's cylinder shape.</summary>
        ///<seealso cref="radius" />
        public extern float height { get; set; }

        ///<summary>Radius of the obstacle's capsule shape.</summary>
        ///<seealso cref="height" />
        public extern float radius { get; set; }

        ///<summary>Velocity at which the obstacle moves around the NavMesh.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///                        // Name this file (ManualObstacleVelocityUpdater.cs) to match the class name.
        ///                        using System;
        ///                        using UnityEngine;
        ///                        using UnityEngine.AI;
        ///
        ///                        /// <summary>
        ///                        /// Update the current GameObject's NavMesh Obstacle velocity according to its position changes.
        ///                        /// Useful when the position of an object is controlled by script.
        ///                        /// </summary>
        ///                        public class ManualObstacleVelocityUpdater : MonoBehaviour
        ///                        {
        ///                            NavMeshObstacle m_Obstacle;
        ///                            Vector3 m_LastPosition;
        ///
        ///                            void Start()
        ///                            { 
        ///                                m_Obstacle = GetComponent<NavMeshObstacle>(); 
        ///                                m_LastPosition = transform.position; 
        ///                            }
        ///
        ///                            void Update()
        ///                            {
        ///                                var deltaTime = Time.deltaTime;
        ///                                if (m_Obstacle && deltaTime > Mathf.Epsilon)
        ///                                { 
        ///                                    // Compute this frame's velocity 
        ///                                    var newPosition = transform.position; 
        ///                                    var velocity = (newPosition - m_LastPosition) / deltaTime; 
        ///                                    m_Obstacle.velocity = velocity; 
        ///                                    
        ///                                    // Keep track of the last considered position 
        ///                                    m_LastPosition = newPosition; 
        ///                                }
        ///                            }
        ///                        }
        ///]]></code>
        ///</example>
        public extern Vector3 velocity { get; set; }

        ///<summary>Should this obstacle make a cut-out in the navmesh.</summary>
        ///<remarks>When enabled, this changes the navmesh by cutting out a hole. The shape of the hole is based on the size and shape set on <see cref="NavMeshObstacle" /> and the navmesh bake settings for radius and height.
        ///
        ///When the obstacle moves, the carved hole will also move but to reduce CPU overhead the hole is only recalculated when necessary. The recalculation logic has two options: 1) carve when stationary, 2) carve when moved.
        ///
        ///"Carve when stationary" is the default behavior and is used when <see cref="carveOnlyStationary" /> is set to true. The obstacle is treated as moving when it has moved more than the distance set by <see cref="carvingMoveThreshold" />. At this time, the carved hole is removed. When the obstacle has stopped moving, and has been stationary more than <see cref="carvingTimeToStationary" /> seconds, the obstacles is treated stationary and carving is updated again. While the obstacle is moving, the agents will avoid it using the collision avoidance, but will not plan paths around it. This mode is generally the best choice in terms of performance. It is good match when the game object is controlled by physics (i.e. crates and barrels).
        ///
        ///"Carve when moved" behavior is used when <see cref="carveOnlyStationary" /> is set to false. In this mode the carved hole is updated when the obstacle has moved more than the distance set by <see cref="carvingMoveThreshold" />. This mode is well suited for large slowly moving obstacles, for example a tank that is being avoided by infantry.</remarks>
        public extern bool carving { get; set; }

        ///<summary>Should this obstacle be carved when it is constantly moving?</summary>
        ///<remarks>When this property is enabled, the obstacle will carve a hole only when it is stationary. There will be no hole carved when the object is moving. See <see cref="carving" /> for full description of different carving behaviors.</remarks>
        public extern bool carveOnlyStationary { get; set; }

        ///<summary>Threshold distance for updating a moving carved hole (when carving is enabled).</summary>
        ///<remarks>If the <see cref="NavMeshObstacle" /> has moved a distance shorter than the threshold since last carving then the navmesh will not be updated.</remarks>
        ///<seealso cref="carving" />
        [NativeProperty("MoveThreshold")]
        public extern float carvingMoveThreshold { get; set; }

        ///<summary>Time to wait until obstacle is treated as stationary (when carving and carveOnlyStationary are enabled).</summary>
        ///<remarks>If the <see cref="NavMeshObstacle" /> has been moving, and becomes still, We wait <c>carvingTimeToStationary</c> time until the obstacle is treated stationary by the carving system. See <see cref="carving" /> for full description of different carving behaviors.</remarks>
        [NativeProperty("TimeToStationary")]
        public extern float carvingTimeToStationary { get; set; }

        ///<summary>The shape of the obstacle.</summary>
        ///<remarks>
        ///  <para>Set or get the shape of the <see cref="NavMeshObstacle" />.</para>
        ///  <para>A newly created <see cref="NavMeshObstacle" /> has a shape of the <see cref="AI.NavMeshObstacleShape.Box" /> shape.
        ///          The obstacle shapes are listed in <see cref="AI.NavMeshObstacleShape" />.</para>
        ///  <para>**Note:** When the shape is changed the <see cref="center" /> is set back to zero.</para>
        ///</remarks>
        public extern NavMeshObstacleShape shape { get; set; }

        ///<summary>The center of the obstacle, measured in the object's local space.</summary>
        ///<remarks>**Note:** When a <see cref="NavMeshObstacle" /> is created the <see cref="center" /> is set to zero.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Default center is (0, 0, 0)
        ///using UnityEngine;
        ///using UnityEngine.AI;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    void Start()
        ///    {
        ///        NavMeshObstacle navMeshObstacle = gameObject.AddComponent<NavMeshObstacle>();
        ///        Debug.Log(navMeshObstacle.center);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern Vector3 center { get; set; }

        ///<summary>The size of the obstacle, measured in the object's local space.</summary>
        ///<remarks>The size will be scaled by the transform's scale.</remarks>
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
        ///        NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();
        ///        Mesh mesh = GetComponent<MeshFilter>().mesh;
        ///        obstacle.shape = NavMeshObstacleShape.Box;
        ///        obstacle.size = mesh.bounds.size;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public extern Vector3 size
        {
            [FreeFunction("NavMeshObstacleScriptBindings::GetSize", HasExplicitThis = true)]
            get;
            [FreeFunction("NavMeshObstacleScriptBindings::SetSize", HasExplicitThis = true)]
            set;
        }

        [VisibleToOtherModules("UnityEditor.AIModule")]
        [FreeFunction("NavMeshObstacleScriptBindings::FitExtents", HasExplicitThis = true)]
        internal extern void FitExtents();
    }
}
