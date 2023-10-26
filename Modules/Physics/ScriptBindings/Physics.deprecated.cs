// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;

namespace UnityEngine
{
    public partial class Physics
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Please use Physics.IgnoreRaycastLayer instead. (UnityUpgradable) -> IgnoreRaycastLayer", true)]
        public const int kIgnoreRaycastLayer = IgnoreRaycastLayer;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Please use Physics.DefaultRaycastLayers instead. (UnityUpgradable) -> DefaultRaycastLayers", true)]
        public const int kDefaultRaycastLayers = DefaultRaycastLayers;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Please use Physics.AllLayers instead. (UnityUpgradable) -> AllLayers", true)]
        public const int kAllLayers = AllLayers;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use Physics.defaultContactOffset or Collider.contactOffset instead.", true)]
        public static float minPenetrationForPenalty { get { return 0f; } set {} }

        [Obsolete("Please use bounceThreshold instead. (UnityUpgradable) -> bounceThreshold")]
        public static float bounceTreshold { get { return bounceThreshold; } set { bounceThreshold = value; }  }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("The sleepVelocity is no longer supported. Use sleepThreshold. Note that sleepThreshold is energy but not velocity.", true)]
        public static float sleepVelocity { get { return 0f; } set {} }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("The sleepAngularVelocity is no longer supported. Use sleepThreshold. Note that sleepThreshold is energy but not velocity.", true)]
        public static float sleepAngularVelocity { get { return 0f; } set {} }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use Rigidbody.maxAngularVelocity instead.", true)]
        public static float maxAngularVelocity { get { return 0f; } set {} }

        [Obsolete("Please use Physics.defaultSolverIterations instead. (UnityUpgradable) -> defaultSolverIterations")]
        public static int solverIterationCount { get { return defaultSolverIterations; } set { defaultSolverIterations = value; } }

        [Obsolete("Please use Physics.defaultSolverVelocityIterations instead. (UnityUpgradable) -> defaultSolverVelocityIterations")]
        public static int solverVelocityIterationCount { get { return defaultSolverVelocityIterations; } set { defaultSolverVelocityIterations = value; } }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("penetrationPenaltyForce has no effect.", true)]
        public static float penetrationPenaltyForce { get { return 0f; } set {} }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Physics.autoSimulation has been replaced by Physics.simulationMode", false)]
        public static bool autoSimulation
        {
            get { return simulationMode != SimulationMode.Script; }
            set { simulationMode = value ? SimulationMode.FixedUpdate : SimulationMode.Script; }
        }

        [Obsolete("Physics.RebuildBroadphaseRegions has been deprecated alongside Multi Box Pruning. Use Automatic Box Pruning instead.", false)]
        public static void RebuildBroadphaseRegions(Bounds worldBounds, int subdivisions)
        {
            return;
        }
    }
}
