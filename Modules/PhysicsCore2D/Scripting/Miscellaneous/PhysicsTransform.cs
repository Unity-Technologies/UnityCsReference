// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    /// <summary>
    /// Represents a 2D transformation combining a translation and a <see cref="PhysicsRotate"/>.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public struct PhysicsTransform
    {
        /// <summary>
        /// Create an identity transform.
        /// </summary>
        public PhysicsTransform() { position = Vector2.zero; rotation = PhysicsRotate.identity; }

        /// <summary>
        /// Create a transformation with the specified translation and no rotation.
        /// </summary>
        /// <param name="position">The translation for the transformation.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PhysicsTransform(Vector2 position) { this.position = position; rotation = PhysicsRotate.identity; }

        /// <summary>
        /// Create a transformation with the specified translation and rotation.
        /// </summary>
        /// <param name="position">The translation for the transformation.</param>
        /// <param name="rotation">The rotation for the transformation.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PhysicsTransform(Vector2 position, PhysicsRotate rotation) { this.position = position; this.rotation = rotation; }

        /// <summary>
        /// Check if the PhysicsTransform is valid (position is not NaN and <see cref="PhysicsRotate.isValid"/>).
        /// </summary>
        public readonly bool isValid => PhysicsTransform_IsValid(this);

        /// <summary>
        /// The translation for the transformation.
        /// </summary>
        /// <remarks>
        /// This is exposed directly as a field rather than a property as it is extremely unlikely to ever change and causes usability issues as a property.
        /// </remarks>
        public Vector2 position;

        /// <summary>
        /// The rotation for the transformation.
        /// </summary>
        /// <remarks>
        /// This is exposed directly as a field rather than a property as it is extremely unlikely to ever change and causes usability issues as a property.
        /// </remarks>
        public PhysicsRotate rotation;

        /// <summary>
        /// Get both the position and rotation.
        /// </summary>
        /// <param name="position">The translation for the transformation.</param>
        /// <param name="rotation">The rotation for the transformation.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void GetPositionAndRotation(out Vector2 position, out PhysicsRotate rotation) { position = this.position ; rotation = this.rotation; }

        /// <summary>
        /// Transform a point using the current transform translation and rotation.
        /// </summary>
        /// <param name="point">The point to transform.</param>
        /// <returns>The transformed point.</returns>
        public readonly Vector2 TransformPoint(Vector2 point) => PhysicsTransform_TransformPoint(this, point);

        /// <summary>
        /// Inverse Transform a point using the current transform translation and rotation.
        /// </summary>
        /// <param name="point">The point to inverse transform.</param>
        /// <returns>The inverse transformed point.</returns>
        public readonly Vector2 InverseTransformPoint(Vector2 point) => PhysicsTransform_InverseTransformPoint(this, point);

        /// <summary>
        /// Multiply the specified transform with the current transform.
        /// </summary>
        /// <param name="transform">The transform to multiply with.</param>
        /// <returns>The resultant multiplied transform.</returns>
        public readonly PhysicsTransform MultiplyTransform(PhysicsTransform transform) => PhysicsTransform_MultiplyTransform(this, transform);

        /// <summary>
        /// Inverse Multiply the specified transform with the current transform.
        /// </summary>
        /// <param name="transform">The transform to inverse multiply with.</param>
        /// <returns>The resultant multiplied transform.</returns>
        public readonly PhysicsTransform InverseMultiplyTransform(PhysicsTransform transform) => PhysicsTransform_InverseMultiplyTransform(this, transform);

        /// <summary>
        /// The identity transformation i.e. a transformation with no translation or rotation.
        /// </summary>
        public static PhysicsTransform identity { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new(); }

        /// <summary>
        /// Implicit conversion of a <see cref="UnityEngine.Vector2"/> that represents a translation transformation with no rotation.
        /// </summary>
        /// <param name="position">The translation for the transformation.</param>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] public static implicit operator PhysicsTransform(Vector2 position) => new(position: position, rotation: PhysicsRotate.identity);

        /// <summary>
        /// Implicit conversion of a <see cref="PhysicsRotate"/> that represents a rotation transformation with no translation.
        /// </summary>
        /// <param name="rotation">The translation for the transformation.</param>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static implicit operator PhysicsTransform(PhysicsRotate rotation) => new(position: Vector2.zero, rotation: rotation);

        /// <undoc/>
        public override readonly string ToString() => $"position={position}, rotation={rotation}";
    }
}
