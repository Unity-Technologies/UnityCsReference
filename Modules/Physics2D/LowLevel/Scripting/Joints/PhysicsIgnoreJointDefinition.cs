// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

using static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelScripting2D;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <summary>
    /// A joint definition used to specify properties when creating a <see cref="LowLevelPhysics2D.PhysicsIgnoreJoint"/>.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct PhysicsIgnoreJointDefinition
    {
        /// <summary>
        /// Create a default <see cref="LowLevelPhysics2D.PhysicsIgnoreJoint"/> definition.
        /// </summary>
        public PhysicsIgnoreJointDefinition() { this = IgnorePhysicsJoint_GetDefaultDefinition(); }

        /// <summary>
        /// Create a default <see cref="LowLevelPhysics2D.PhysicsIgnoreJoint"/> definition.
        /// </summary>
        public static PhysicsIgnoreJointDefinition defaultDefinition => IgnorePhysicsJoint_GetDefaultDefinition();

        /// <summary>
        /// The first body the joint constrains.
        /// </summary>
        public PhysicsBody bodyA { readonly get => m_BodyA; set => m_BodyA = value; }

        /// <summary>
        /// The second body the joint constrains.
        /// </summary>
        public PhysicsBody bodyB { readonly get => m_BodyB; set => m_BodyB = value; }

        #region Internal

        [SerializeField] PhysicsBody m_BodyA;
        [SerializeField] PhysicsBody m_BodyB;

        #endregion
    }
}
