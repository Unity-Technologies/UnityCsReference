// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelScripting2D;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <summary>
    /// Represents a 2D rotation.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct PhysicsRotate : ISerializationCallbackReceiver
    {
        /// <summary>
        /// The rotation direction where X = cos(angle) and Y = sin(angle).
        /// This should always be normalised otherwise warnings will be produced when used, however this is not enforced.
        /// See <see cref="LowLevelPhysics2D.PhysicsRotate.isNormalized"/> and <see cref="LowLevelPhysics2D.PhysicsRotate.Normalized"/>.
        /// </summary>
        /// <remarks>
        /// This is exposed directly as a field rather than a property as it is extremely unlikely to ever change and causes usability issues as a property.
        /// </remarks>
        public Vector2 direction;

        /// <summary>
        /// The cosine of the rotation angle.
        /// </summary>
        public readonly float cos { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => direction.x; }

        /// <summary>
        /// The sine of the rotation angle.
        /// </summary>
        public readonly float sin { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => direction.y; }

        /// <summary>
        /// Create an identity rotation.
        /// </summary>
        public PhysicsRotate() { direction = Vector2.right; }

        /// <summary>
        /// Create a rotation with the specified direction.
        /// </summary>
        /// <param name="direction">The direction to use. This cannot be zero length.</param>
        public PhysicsRotate(Vector2 direction) { this = PhysicsRotate_CreateDirection(in direction); }

        /// <summary>
        /// Create a rotation with the specified angle.
        /// </summary>
        /// <param name="angle">The rotation angle specified, in radians.</param>
        /// <returns>The rotation represented by the specified angle.</returns>
        public PhysicsRotate(float angle) { this = PhysicsRotate_CreateAngle(angle); }

        /// <summary>
        /// Create a rotation with the specified <see cref="UnityEngine.Quaternion"/>.
        /// </summary>
        /// <param name="rotation">The Quaternion rotation to use.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <returns>The 2D rotation extracted from the specified Quaternion.</returns>
        public PhysicsRotate(Quaternion rotation, PhysicsWorld.TransformPlane transformPlane) { this = new PhysicsRotate(PhysicsMath.ToRotation2D(rotation, transformPlane)); }

        /// <summary>
        /// Get the relative angle between this rotation and the specified rotation.
        /// The limits of this are +/- <see cref="LowLevelPhysics2D.PhysicsMath.PI"/>.
        /// </summary>
        /// <param name="rotation">The rotation to calculate the relative angle against.</param>
        /// <returns>The relative angle, in radians.</returns>
        public readonly float GetRelativeAngle(PhysicsRotate rotation) => PhysicsRotate_GetRelativeAngle(this, rotation);

        /// <summary>
        /// Convert any angle into the range [-pi, pi].
        /// </summary>
        /// <param name="angle">The angle to convert, in radians.</param>
        /// <returns>The angle converted into the range [-pi, pi].</returns>
        public static float UnwindAngle(float angle) => PhysicsRotate_UnwindAngle(angle);

        /// <summary>
        /// Integrate the rotation using the specified angle change.
        /// </summary>
        /// <param name="deltaAngle">The angle to integrate the rotation with, in radians.</param>
        /// <returns>The integrated rotation.</returns>
        public readonly PhysicsRotate IntegrateRotation(float deltaAngle) => PhysicsRotate_IntegrateRotation(this, deltaAngle);

        /// <summary>
        /// Calculate the normalized linear interpolation of this rotation and the specified rotation using the specified interval.
        /// </summary>
        /// <param name="rotation">The rotation to lerp with against the current rotation.</param>
        /// <param name="interval">The lerp interval, typically in the range [0, 1]. A value outside of this range performs normalized linear extrapolation.</param>
        /// <returns>The normalized linear interpolation/extrapolation.</returns>
        public readonly PhysicsRotate LerpRotation(PhysicsRotate rotation, float interval) => PhysicsRotate_LerpRotation(this, rotation, interval);

        /// <summary>
        /// Calculate the normalized linear interpolation between two rotations using the specified fraction.
        /// </summary>
        /// <param name="rotationA">The rotation to lerp from.</param>
        /// <param name="rotationB">The rotation to lerp to.</param>
        /// <param name="interval">The lerp interval, typically in the range [0, 1]. A value outside of this range performs normalized linear extrapolation.</param>
        /// <returns>The normalized linear interpolation/extrapolation.</returns>
        public static PhysicsRotate LerpRotation(PhysicsRotate rotationA, PhysicsRotate rotationB, float interval) => PhysicsRotate_LerpRotation(rotationA, rotationB, interval);

        /// <summary>
        /// Calculate the angular velocity necessary to rotate the current rotation and the specified rotation over a give time.
        /// </summary>
        /// <param name="rotation">The rotation used as a calculation to rotate to.</param>
        /// <param name="deltaTime">The delta time over which the rotation would take place.</param>
        /// <returns>The angular velocity required to rotate to the specified rotation.</returns>
        public readonly float AngularVelocity(PhysicsRotate rotation, float deltaTime) => PhysicsRotate_AngularVelocity(this, rotation, deltaTime);

        /// <summary>
        /// Calculate the angular velocity necessary to rotate between two rotations over a give time.
        /// </summary>
        /// <param name="rotationA">The rotation to rotate from.</param>
        /// <param name="rotationB">The rotation to rotate to.</param>
        /// <param name="deltaTime">The delta time over which the rotation would take place.</param>
        /// <returns>The angular velocity required to rotate between the specified rotations.</returns>
        public static float AngularVelocity(PhysicsRotate rotationA, PhysicsRotate rotationB, float deltaTime) => PhysicsRotate_AngularVelocity(rotationA, rotationB, deltaTime);

        /// <summary>
        /// Multiply a rotation with this rotation.
        /// </summary>
        /// <param name="rotation">The rotation to multiply by.</param>
        /// <returns>The result of the multiply rotation.</returns>
        public readonly PhysicsRotate MultiplyRotation(PhysicsRotate rotation) => PhysicsRotate_MultiplyRotation(this, rotation);

        /// <summary>
        /// Inverse Multiply a rotation with this rotation.
        /// </summary>
        /// <param name="rotation">The rotation to inverse multiply by.</param>
        /// <returns>The result of the inverse multiply rotation.</returns>
        public readonly PhysicsRotate InverseMultiplyRotation(PhysicsRotate rotation) => PhysicsRotate_InverseMultiplyRotation(this, rotation);

        /// <summary>
        /// Rotate a vector.
        /// </summary>
        /// <param name="vector">The vector to rotate.</param>
        /// <returns>The result of the vector rotation.</returns>
        public readonly Vector2 RotateVector(Vector2 vector) => PhysicsRotate_RotateVector(this, vector);

        /// <summary>
        /// Inverse Rotate a vector.
        /// </summary>
        /// <param name="vector">The vector to inverse rotate.</param>
        /// <returns>The result of the inverse vector rotation.</returns>
        public readonly Vector2 InverseRotateVector(Vector2 vector) => PhysicsRotate_InverseRotateVector(this, vector);

        /// <summary>
        /// Rotate the rotation by the specified angle.
        /// </summary>
        /// <param name="deltaAngle">The change in angle, in radians.</param>
        /// <returns></returns>
        public readonly PhysicsRotate Rotate(float deltaAngle) => PhysicsRotate_Rotate(this, deltaAngle);

        /// <summary>
        /// Calculate a rotation <see cref="UnityEngine.Matrix4x4"/> using the specified transform plane.
        /// </summary>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <returns>The rotation matrix.</returns>
        public readonly Matrix4x4 GetMatrix(PhysicsWorld.TransformPlane transformPlane) => Matrix4x4.Rotate(PhysicsMath.ToRotationFast3D(angle, transformPlane));

        /// <summary>
        /// Create a normalized rotation.
        /// </summary>
        public readonly PhysicsRotate Normalized() => PhysicsRotate_CreateDirection(in direction);

        /// <summary>
        /// Is the rotation normalized? If not, it should be normalized using <see cref="LowLevelPhysics2D.PhysicsRotate.Normalized"/>.
        /// </summary>
        public readonly bool isNormalized => PhysicsRotate_IsNormalized(this);

        /// <summary>
        /// Check if the rotation is valid (not NaN and Normalized).
        /// </summary>
        public readonly bool isValid => PhysicsRotate_IsValid(this);

        /// <summary>
        /// Get the angle which this rotation represents, in radians.
        /// </summary>
        public readonly float angle => PhysicsRotate_GetAngle(this);

        /// <summary>
        /// The identity rotation i.e. no rotation.
        /// </summary>
        public static PhysicsRotate identity { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => identityRotation; }
        private static readonly PhysicsRotate identityRotation = new(Vector2.right);

        /// <summary>
        /// A rotation of zero Radians.
        /// This is the same as identity.
        /// See <see cref="LowLevelPhysics2D.PhysicsRotate.identity"/>.
        /// </summary>
        public static PhysicsRotate right { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => identityRotation; }

        /// <summary>
        /// A rotation of +PI Radians (+/- 180 Degrees).
        /// </summary>
        public static PhysicsRotate left { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => leftRotation; }
        private static readonly PhysicsRotate leftRotation = new(Vector2.left);

        /// <summary>
        /// A rotation of +PI/2 Radians (+90 Degrees).
        /// </summary>
        public static PhysicsRotate up { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => upRotation; }
        private static readonly PhysicsRotate upRotation = new(Vector2.up);

        /// <summary>
        /// A rotation of -PI/2 Radians (-90 Degrees or +270 Degrees).
        /// </summary>
        public static PhysicsRotate down { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => downRotation; }
        private static readonly PhysicsRotate downRotation = new(Vector2.down);

        /// <undoc/>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static explicit operator PhysicsRotate(Vector2 direction) => new(direction);

        /// <undoc/>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static implicit operator Vector2(PhysicsRotate rotate2) => rotate2.direction;

        /// <undoc/>
        public void OnBeforeSerialize()
        {
            // Ensure it's valid.
            if (!isValid)
                this = identity;
        }

        /// <undoc/>
        public void OnAfterDeserialize()
        {
            // Ensure it's valid.
            if (!isValid)
                this = identity;
        }

        /// <undoc/>
        public override readonly string ToString() => $"angle={angle} (rad), cos={cos}, sin={sin}";
    }
}
