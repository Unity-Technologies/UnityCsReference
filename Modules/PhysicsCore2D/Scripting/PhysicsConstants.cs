// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Scripting.APIUpdating;

namespace Unity.U2D.Physics
{
    /// <summary>
    /// Constants used throughout the 2D physics system.
    /// </summary>
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public readonly partial struct PhysicsConstants
    {
        /// <summary>
        /// A constant defining the maximum number of worker threads supported by physics simulation.
        /// The current device may support fewer or more than this.
        /// </summary>
        public const int MaxWorkers = 64;

        /// <summary>
        /// The maximum number of supported vertices in <see cref="PolygonGeometry"/>.
        /// </summary>
        public const int MaxPolygonVertices = 8;

        /// <summary>
        /// The number of "colors" used for contact and joint constraints when solving the simulation.
        /// </summary>
        internal const int SolverGraphColorCount = 24;
    }
}
