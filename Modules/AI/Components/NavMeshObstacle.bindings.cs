// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.AI
{
    // Keep this enum in sync with the one defined in "NavMeshBindingTypes.h"
    [MovedFrom("UnityEngine")]
    public enum NavMeshObstacleShape
    {
        // Capsule shaped obstacle.
        Capsule = 0,
        // Box shaped obstacle.
        Box = 1,
    }

    // Navigation mesh obstacle.
    [MovedFrom("UnityEngine")]
    [NativeHeader("Modules/AI/Components/NavMeshObstacle.bindings.h")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.ai.navigation@1.1/manual/NavMeshObstacle.html")]
    public sealed class NavMeshObstacle : Behaviour
    {
        // Obstacle height.
        public extern float height { get; set; }

        // Obstacle radius.
        public extern float radius { get; set; }

        // Obstacle velocity.
        public extern Vector3 velocity { get; set; }

        // Enable carving
        public extern bool carving { get; set; }

        // When carving enabled, carve only when obstacle is stationary, moving obstacles are avoided dynamically.
        public extern bool carveOnlyStationary { get; set; }

        // Update carving if moved at least this distance, or if carveWhenStationary if moved at least this distance, the obstacle is considered moving.
        [NativeProperty("MoveThreshold")]
        public extern float carvingMoveThreshold { get; set; }

        // If carveWhenStationary is set, the obstacle is considered stationary if it has not moved during this long period.
        [NativeProperty("TimeToStationary")]
        public extern float carvingTimeToStationary { get; set; }

        // Shape of the obstacle, NavMeshObstacleShape.Box or NavMeshObstacleShape.Capsule.
        public extern NavMeshObstacleShape shape { get; set; }

        public extern Vector3 center { get; set; }

        public extern Vector3 size
        {
            [FreeFunction("NavMeshObstacleScriptBindings::GetSize", HasExplicitThis = true)]
            get;
            [FreeFunction("NavMeshObstacleScriptBindings::SetSize", HasExplicitThis = true)]
            set;
        }

        [FreeFunction("NavMeshObstacleScriptBindings::FitExtents", HasExplicitThis = true)]
        internal extern void FitExtents();
    }
}
