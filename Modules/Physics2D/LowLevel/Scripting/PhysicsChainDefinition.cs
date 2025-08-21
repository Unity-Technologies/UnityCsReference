// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

using static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelScripting2D;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <summary>
    /// A <see cref="LowLevelPhysics2D.PhysicsChain"/> definition used to specify the chain of vertices that will produce multiple <see cref="LowLevelPhysics2D.ChainSegmentGeometry"/> shape types.
    /// Additionally, non-geometric properties can be specified here.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct PhysicsChainDefinition
    {
        /// <summary>
        /// Create a default <see cref="LowLevelPhysics2D.PhysicsChain"/> definition.
        /// </summary>
        public PhysicsChainDefinition() { this = defaultDefinition; }

        /// <summary>
        /// Create a default <see cref="LowLevelPhysics2D.PhysicsChain"/> definition.
        /// </summary>
        /// <param name="useSettings">Controls whether the default come settings from the physics settings or not.</param>
        public PhysicsChainDefinition(bool useSettings) { this = PhysicsChain_GetDefaultDefinition(useSettings); }

        /// <summary>
        /// Get a default <see cref="LowLevelPhysics2D.PhysicsChain"/> definition.
        /// </summary>
        public static PhysicsChainDefinition defaultDefinition => PhysicsChain_GetDefaultDefinition(true);

        /// <summary>
        /// The surface material for the shape comprising of many properties such as friciton, bounciness, rolling resistance etc.
        /// </summary>
        public PhysicsShape.SurfaceMaterial surfaceMaterial { readonly get => m_SurfaceMaterial; set => m_SurfaceMaterial = value; }

        /// <summary>
        /// The contact filter used to control which contacts this shape can participate in.
        /// </summary>
        public PhysicsShape.ContactFilter contactFilter { readonly get => m_ContactFilter; set => m_ContactFilter = value; }

        /// <summary>
        /// Indicates a closed chain formed by connecting the first and last vertices specified.
        /// </summary>
        public bool isLoop { readonly get => m_IsLoop; set => m_IsLoop = value; }

        /// <summary>
        /// Controls whether this chain produces trigger events which can be retrieved after the simulation has completed.
        /// This applies to triggers and non-triggers alike.
        /// </summary>
	    public bool triggerEvents { readonly get => m_TriggerEvents; set => m_TriggerEvents = value; }

        #region Internal

        [SerializeField] PhysicsShape.SurfaceMaterial m_SurfaceMaterial;
        [SerializeField] PhysicsShape.ContactFilter m_ContactFilter;
        [SerializeField] bool m_IsLoop;
	    [SerializeField] bool m_TriggerEvents;

        #endregion
    }
}
