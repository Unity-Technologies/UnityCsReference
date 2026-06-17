// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.LowLevelPhysics2D
{
    /// <summary>
    /// Constants used throughout the 2D physics system.
    /// </summary>
    public readonly struct PhysicsConstants
    {
        /// <summary>
        /// The maximum number of simultaneous active <see cref="LowLevelPhysics2D.PhysicsWorld"/> allowed.
        /// </summary>
        public const int MaxWorlds = 128;

        /// <summary>
        /// A constant defining the maximum number of worker threads supported by physics simulation.
        /// The current device may support fewer or more than this.
        /// </summary>
        public const int MaxWorkers = 64;

        /// <summary>
        /// The maximum number of supported vertices in <see cref="LowLevelPhysics2D.PolygonGeometry"/>.
        /// </summary>
        public const int MaxPolygonVertices = 8;

        /// <summary>
        /// The number of "colors" used for contact and joint constraints when solving the simulation.
        /// </summary>
        internal const int SolverGraphColorCount = 24;
    }
}
