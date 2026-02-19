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
    /// Represents a 2D plane.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public struct PhysicsPlane
    {
        /// <summary>
        /// The plane normal. This must be normalized for the plane be valid.
        /// </summary>
        /// <remarks>
        /// This is exposed directly as a field rather than a property as it is extremely unlikely to ever change and causes usability issues as a property.
        /// </remarks>
        public Vector2 normal;

        /// <summary>
        ///  The plane offset.
        /// </summary>
        /// <remarks>
        /// This is exposed directly as a field rather than a property as it is extremely unlikely to ever change and causes usability issues as a property.
        /// </remarks>
        public float offset;

        /// <summary>
        /// Check if the plane is valid. To be valid, the <see cref="PhysicsPlane.normal"/> must be normalized.
        /// </summary>
        public readonly bool isValid => PhysicsPlane_IsValid(this);

        /// <summary>
        /// Get the signed separation of a point from a plane.
        /// </summary>
        /// <param name="point">The point to check the separation from the plane.</param>
        /// <returns>The signed separation of the point from the plan.</returns>
        public readonly float GetSeparation(Vector2 point) => PhysicsPlane_GetSeparation(this, point);

        /// <undoc/>
        public override readonly string ToString() => $"normal={normal}, offset={offset}";

        #region Internal
        #endregion
    }
}
