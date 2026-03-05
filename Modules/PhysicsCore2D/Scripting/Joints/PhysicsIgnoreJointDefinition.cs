// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    /// <summary>
    /// A joint definition used to specify properties when creating a <see cref="PhysicsIgnoreJoint"/>.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public struct PhysicsIgnoreJointDefinition
    {
        /// <summary>
        /// Create a default <see cref="PhysicsIgnoreJoint"/> definition.
        /// </summary>
        public PhysicsIgnoreJointDefinition() { this = IgnorePhysicsJoint_GetDefaultDefinition(); }

        /// <summary>
        /// Create a default <see cref="PhysicsIgnoreJoint"/> definition.
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

        /// <summary>
        /// Controls whether this joint is automatically drawn when the world is drawn.
        ///
        /// See <see cref="PhysicsJoint.worldDrawing"/>.
        /// </summary>
        public bool worldDrawing { readonly get => m_WorldDrawing; set => m_WorldDrawing = value; }

        #region Internal

        PhysicsBody m_BodyA;
        PhysicsBody m_BodyB;
        [SerializeField] bool m_WorldDrawing;

        #endregion
    }
}
