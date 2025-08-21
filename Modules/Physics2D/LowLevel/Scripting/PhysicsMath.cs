// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;

using static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelScripting2D;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <summary>
    /// A set of mathematical operations that are useful for physics.
    /// These operations do not form a fully comprehensive mathematics library, they simply provide operations that are usually required when interacting with physics.
    /// </summary>
    public readonly struct PhysicsMath
    {
        /// <summary>
        /// Get the value of PI used internally by the physics system. Using this will help with determinism.
        /// </summary>
        public static float PI => PhysicsMath_PI();

        /// <summary>
        /// Get the value of tau (2 * PI) used internally by the physics system. Using this will help with determinism.
        /// </summary>
        public static float TAU => PhysicsMath_TAU();

        /// <summary>
        /// Convert radians to degrees.
        /// This operates as deterministically as possible across platforms.
        /// See <see cref="LowLevelPhysics2D.PhysicsMath.ToRadians(float)"/>.
        /// </summary>
        /// <param name="radians">The radian value to convert to degrees.</param>
        /// <returns>The radian value converted to degrees.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToDegrees(float radians) => PhysicsMath_ToDegrees(radians);

        /// <summary>
        /// Convert degrees to radians.
        /// This operates as deterministically as possible across platforms.
        /// See <see cref="LowLevelPhysics2D.PhysicsMath.ToDegrees(float)"/>.
        /// </summary>
        /// <param name="degrees">The degree value to convert to radians.</param>
        /// <returns>The value converted to radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToRadians(float degrees) => PhysicsMath_ToRadians(degrees);

        /// <summary>
        /// Calculate the arc-tangent i.e. the angle of the provided slope y/x.
        /// This operates as deterministically as possible across platforms.
        /// </summary>
        /// <param name="y">The Y axis.</param>
        /// <param name="x">The X axis.</param>
        /// <returns>The angle in radians.</returns>
        public static float Atan2(float y, float x) => PhysicsMath_Atan2(y, x);

        /// <summary>
        /// Calculate both the Cosine and Sine of the specified angle.
        /// </summary>
        /// <param name="angle">The angle to calculate, in radians.</param>
        /// <param name="cosine">The Cosine of the specified angle.</param>
        /// <param name="sine">The Sine of the specified angle.</param>
        public static void CosSin(float angle, out float cosine, out float sine) => PhysicsMath_CosSin(angle, out cosine, out sine);

        /// <summary>
        /// Calculate a one-dimensional mass-spring-damper simulation which drives towards a zero translation.
        /// You can then compute the new position using: "translation += newSpeed * deltaTime;"
        /// </summary>
        /// <param name="frequency">The frequency of the spring, in cycles per second.</param>
        /// <param name="damping">The damping of the spring. Must be >= zero.</param>
        /// <param name="translation">The current translation of the spring.</param>
        /// <param name="speed">The current speed of the spring.</param>
        /// <param name="deltaTime">The time over which to simulate the spring.</param>
        /// <returns>The new calculated spring speed.</returns>
        public static float SpringDamper(float frequency, float damping, float translation, float speed, float deltaTime) => PhysicsMath_SpringDamper(frequency, damping, translation, speed, deltaTime);

        /// <summary>
        /// Get the minimum absolute value component from the specified vector.
        /// </summary>
        /// <param name="vector">The vector to examine.</param>
        /// <returns>The calculated component.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float MinAbsComponent(Vector2 vector)
        {
            return Math.Min(Math.Abs(vector.x), Math.Abs(vector.y));
        }

        /// <summary>
        /// Get the minimum absolute value component from the specified vector.
        /// </summary>
        /// <param name="vector">The vector to examine.</param>
        /// <returns>The calculated component.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float MinAbsComponent(Vector3 vector)
        {
            return Math.Min(Math.Min(Math.Abs(vector.x), Math.Abs(vector.y)), Math.Abs(vector.z));
        }

        /// <summary>
        /// Get the maximum absolute value component from the specified vector.
        /// </summary>
        /// <param name="vector">The vector to examine.</param>
        /// <returns>The calculated component.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float MaxAbsComponent(Vector2 vector)
        {
            return Math.Max(Math.Abs(vector.x), Math.Abs(vector.y));
        }

        /// <summary>
        /// Get the maximum absolute value component from the specified vector.
        /// </summary>
        /// <param name="vector">The vector to examine.</param>
        /// <returns>The calculated component.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float MaxAbsComponent(Vector3 vector)
        {
            return Math.Max(Math.Max(Math.Abs(vector.x), Math.Abs(vector.y)), Math.Abs(vector.z));
        }

        /// <summary>
        /// Get the used translation axes, given the specified transform plane.
        /// This is the inverse of <see cref="LowLevelPhysics2D.PhysicsMath.GetTranslationIgnoredAxes(PhysicsWorld.TransformPlane)"/>.
        /// </summary>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <returns>The used translation axes. 0 indicates an axis is ignored whereas 1 indicates the axis is used.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the TransformPlane is unknown.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetTranslationAxes(PhysicsWorld.TransformPlane transformPlane = PhysicsWorld.TransformPlane.XY)
        {
            return transformPlane switch
            {
                PhysicsWorld.TransformPlane.XY => Vector3.right + Vector3.up,
                PhysicsWorld.TransformPlane.XZ => Vector3.right + Vector3.forward,
                PhysicsWorld.TransformPlane.ZY => Vector3.up + Vector3.forward,
                _ => throw new InvalidOperationException("Invalid Transform Plane."),
            };
        }

        /// <summary>
        /// Get the ignored translation axes, given the specified transform plane.
        /// This is the inverse of <see cref="LowLevelPhysics2D.PhysicsMath.GetTranslationAxes(PhysicsWorld.TransformPlane)"/>.
        /// </summary>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <returns>The ignored translation axes. 0 indicates an axis is used whereas 1 indicates the axis is ignored.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the TransformPlane is unknown.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetTranslationIgnoredAxes(PhysicsWorld.TransformPlane transformPlane = PhysicsWorld.TransformPlane.XY)
        {
            return transformPlane switch
            {
                PhysicsWorld.TransformPlane.XY => Vector3.forward,
                PhysicsWorld.TransformPlane.XZ => Vector3.up,
                PhysicsWorld.TransformPlane.ZY => Vector3.right,
                _ => throw new InvalidOperationException("Invalid Transform Plane."),
            };
        }

        /// <summary>
        /// Get the ignored translation axis, given the specified transform plane.
        /// </summary>
        /// <param name="position">The position to extra the axis from.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <returns>The ignored translation axis value.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the TransformPlane is unknown.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetTranslationIgnoredAxis(Vector3 position, PhysicsWorld.TransformPlane transformPlane = PhysicsWorld.TransformPlane.XY)
        {
            return transformPlane switch
            {
                PhysicsWorld.TransformPlane.XY => position.z,
                PhysicsWorld.TransformPlane.XZ => position.y,
                PhysicsWorld.TransformPlane.ZY => position.x,
                _ => throw new InvalidOperationException("Invalid Transform Plane."),
            };
        }

        /// <summary>
        /// Get the used rotation axes, given the specified transform plane.
        /// This is the inverse of <see cref="LowLevelPhysics2D.PhysicsMath.GetRotationIgnoredAxes(PhysicsWorld.TransformPlane)"/>.
        /// </summary>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <returns>The used rotation axes. 0 indicates an axis is ignored whereas 1 indicates the axis is used.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the TransformPlane is unknown.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetRotationAxes(PhysicsWorld.TransformPlane transformPlane = PhysicsWorld.TransformPlane.XY)
        {
            return transformPlane switch
            {
                PhysicsWorld.TransformPlane.XY => Vector3.forward,
                PhysicsWorld.TransformPlane.XZ => Vector3.up,
                PhysicsWorld.TransformPlane.ZY => Vector3.right,
                _ => throw new InvalidOperationException("Invalid Transform Plane."),
            };
        }

        /// <summary>
        /// Get the ignored rotation axes, given the specified transform plane.
        /// This is the inverse of <see cref="LowLevelPhysics2D.PhysicsMath.GetRotationAxes(PhysicsWorld.TransformPlane)"/>.
        /// </summary>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <returns>The ignored rotation axes. 0 indicates an axis is used whereas 1 indicates the axis is ignored.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the TransformPlane is unknown.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetRotationIgnoredAxes(PhysicsWorld.TransformPlane transformPlane = PhysicsWorld.TransformPlane.XY)
        {
            return transformPlane switch
            {
                PhysicsWorld.TransformPlane.XY => Vector3.right + Vector3.up,
                PhysicsWorld.TransformPlane.XZ => Vector3.right + Vector3.forward,
                PhysicsWorld.TransformPlane.ZY => Vector3.up + Vector3.forward,
                _ => throw new InvalidOperationException("Invalid Transform Plane."),
            };
        }

        /// <summary>
        /// Get the relative transformation matrix between the two specified transforms using the specified transform plane.
        /// </summary>
        /// <param name="transformFrom">The transform used as a reference to transform from.</param>
        /// <param name="transformTo">The transform used as a reference to transform to.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <param name="useScale">If the returned matrix should include scale.</param>
        /// <returns>The calculated relative transformation matrix.</returns>
        public static Matrix4x4 GetRelativeMatrix(Transform transformFrom, Transform transformTo, PhysicsWorld.TransformPlane transformPlane = PhysicsWorld.TransformPlane.XY, bool useScale = true)
        {
            // The same transforms use identity with scaling.
            if (transformFrom == transformTo)
            {
                if (useScale)
                    return Matrix4x4.TRS(Vector3.zero, Quaternion.identity, transformTo.lossyScale);
                else
                    return Matrix4x4.identity;
            }

            // Calculate the relative transform using the selected transform plane.            
            var inverseRotation = Quaternion.Inverse(ToRotationFast3D(ToRotation2D(transformFrom.rotation, transformPlane), transformPlane));
            var inversePosition = inverseRotation * -Swizzle(transformFrom.position, transformPlane);
            var inverseMatrix = Matrix4x4.TRS(inversePosition, inverseRotation, Vector3.one);

            if (useScale)
                return inverseMatrix * Swizzle(transformTo.localToWorldMatrix, transformPlane);

            return inverseMatrix * Swizzle(transformTo.localToWorldMatrix * Matrix4x4.Scale(transformTo.localScale).inverse, transformPlane);
        }

        /// <summary>
        /// Transform a 3D position into a 3D position using the selected transform plane.
        /// </summary>
        /// <param name="position">The 3D position to transform.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <returns>The transformed position.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the TransformPlane is unknown.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Swizzle(Vector3 position, PhysicsWorld.TransformPlane transformPlane = PhysicsWorld.TransformPlane.XY)
        {
            return transformPlane switch
            {
                PhysicsWorld.TransformPlane.XY => position,
                PhysicsWorld.TransformPlane.XZ => new Vector3(position.x, position.z, position.y),
                PhysicsWorld.TransformPlane.ZY => new Vector3(position.z, position.y, position.x),
                _ => throw new InvalidOperationException("Invalid Transform Plane."),
            };
        }

        /// <summary>
        /// Transform a 3D position (with perspective divide) into a 3D position using the selected transform plane.
        /// </summary>
        /// <param name="position">The 3D position to transform.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <returns>The transformed position.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the TransformPlane is unknown.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Swizzle(Vector4 position, PhysicsWorld.TransformPlane transformPlane = PhysicsWorld.TransformPlane.XY)
        {
            var swizzledPosition = Swizzle(new Vector3(position.x, position.y, position.z), transformPlane);
            return new Vector4(swizzledPosition.x, swizzledPosition.y, swizzledPosition.z, position.w);
        }

        /// <summary>
        /// Transform a Matrix position (with perspective divide) into a Matrix position using the selected transform plane.
        /// </summary>
        /// <param name="position">The Matrix position to transform.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <returns>The transformed matrix.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the TransformPlane is unknown.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 Swizzle(Matrix4x4 matrix, PhysicsWorld.TransformPlane transformPlane = PhysicsWorld.TransformPlane.XY)
        {
            matrix.SetColumn(3, Swizzle(matrix.GetColumn(3), transformPlane));
            return matrix;
        }

        /// <summary>
        /// Transform a 2D position into a 3D position using the selected transform plane.
        /// </summary>
        /// <param name="position">The 2D position to transform.</param>
        /// <param name="reference">The 3D position used as a reference.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <returns>The transformed position.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the TransformPlane is unknown.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToPosition3D(Vector2 position, Vector3 reference, PhysicsWorld.TransformPlane transformPlane = PhysicsWorld.TransformPlane.XY)
        {
            return transformPlane switch
            {
                PhysicsWorld.TransformPlane.XY => new Vector3(position.x, position.y, reference.z),
                PhysicsWorld.TransformPlane.XZ => new Vector3(position.x, reference.y, position.y),
                PhysicsWorld.TransformPlane.ZY => new Vector3(reference.x, position.y, position.x),
                _ => throw new InvalidOperationException("Invalid Transform Plane."),
            };
        }

        /// <summary>
        /// Transform a 3D position into a 2D position using the selected transform plane.
        /// </summary>
        /// <param name="position">The 3D position to transform.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <returns>The transformed position.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the TransformPlane is unknown.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToPosition2D(Vector3 position, PhysicsWorld.TransformPlane transformPlane = PhysicsWorld.TransformPlane.XY)
        {
            return transformPlane switch
            {
                PhysicsWorld.TransformPlane.XY => position,
                PhysicsWorld.TransformPlane.XZ => new Vector2(position.x, position.z),
                PhysicsWorld.TransformPlane.ZY => new Vector2(position.z, position.y),
                _ => throw new InvalidOperationException("Invalid Transform Plane."),
            };
        }

        /// <summary>
        /// Transform a 3D rotation into a 2D angle using the selected transform plane.
        /// </summary>
        /// <param name="quaternion">The 3D rotation to transform.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <returns>The transformed rotation in radians.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the TransformPlane is unknown.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToRotation2D(Quaternion quaternion, PhysicsWorld.TransformPlane transformPlane = PhysicsWorld.TransformPlane.XY)
        {
            // Ensure positive Quaternion.
            if (quaternion.w < 0.0f)
                quaternion = new Quaternion(-quaternion.x, -quaternion.y, -quaternion.z, -quaternion.w);

            return transformPlane switch
            {
                PhysicsWorld.TransformPlane.XY => 2.0f * Atan2(quaternion.z, quaternion.w),
                PhysicsWorld.TransformPlane.XZ => -2.0f * Atan2(quaternion.y, quaternion.w),
                PhysicsWorld.TransformPlane.ZY => -2.0f * Atan2(quaternion.x, quaternion.w),
                _ => throw new InvalidOperationException("Invalid Transform Plane."),
            };
        }

        /// <summary>
        /// Transform a 3D <see cref="UnityEngine.Transform"/> position and rotation to a 2D <see cref="LowLevelPhysics2D.PhysicsTransform"/>.
        /// Scale is not part of a <see cref="LowLevelPhysics2D.PhysicsTransform"/> therefore it is ignored.
        /// </summary>
        /// <param name="transform">The 3D transform to use.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <returns>The 2D transform.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the TransformPlane is unknown.</exception>
        public static PhysicsTransform ToPhysicsTransform(Transform transform, PhysicsWorld.TransformPlane transformPlane = PhysicsWorld.TransformPlane.XY)
        {
            return new PhysicsTransform
            {
                position = ToPosition2D(transform.position, transformPlane),
                rotation = new PhysicsRotate(ToRotation2D(transform.rotation, transformPlane))
            };
        }

        /// <summary>
        /// Calculate a <see cref="UnityEngine.Quaternion"/> given a 2D angular velocity and a time to integrate over using the selected transform plane.
        /// </summary>
        /// <param name="angularVelocity">The 2D angular velocity, in radians.</param>
        /// <param name="deltaTime">The time over which to apply the angular velocity, in seconds.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <returns>The transformed rotation.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the TransformPlane is unknown.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion AngularVelocityToQuaternion(float angularVelocity, float deltaTime, PhysicsWorld.TransformPlane transformPlane)
        {
            // Calculate the angular speed.
            var angularSpeed = Mathf.Abs(angularVelocity);

            // Use Identity if no appreciable angular velocity is present.
            if (angularSpeed < 0.00001f)
                return Quaternion.identity;

            // Calculate rotation.
            var rotate = new PhysicsRotate(angularSpeed * deltaTime * 0.5f);

            // Ensure the angular velocity is used in the correct plane.
            var transformedAxis = Swizzle(new Vector3(0f, 0f, angularVelocity * (rotate.sin / angularSpeed)), transformPlane);

            // Calculate the normalized, integrated quaternion.
            return new Quaternion(transformedAxis.x, transformedAxis.y, transformedAxis.z, rotate.cos).normalized;
        }

        /// <summary>
        /// Transform a 2D angle into a 3D rotation using the selected transform plane (Fast).
        /// The transformation is fast because the rotation is simplified by the fact that only a single axis of rotation is handled. All other axis rotations are reset to zero.
        /// </summary>
        /// <param name="angle">The 2D angle to transform in radians.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <returns>The transformed rotation.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the TransformPlane is unknown.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ToRotationFast3D(float angle, PhysicsWorld.TransformPlane transformPlane = PhysicsWorld.TransformPlane.XY)
        {
            switch (transformPlane)
            {
                case PhysicsWorld.TransformPlane.XY:
                {
                    var rotate = new PhysicsRotate(angle * 0.5f);
                    return new Quaternion(0.0f, 0.0f, rotate.sin, rotate.cos);
                }

                case PhysicsWorld.TransformPlane.XZ:
                {
                    var rotate = new PhysicsRotate(angle * -0.5f);
                    return new Quaternion(0.0f, rotate.sin, 0.0f, rotate.cos);
                }

                case PhysicsWorld.TransformPlane.ZY:
                {
                    var rotate = new PhysicsRotate(angle * -0.5f);
                    return new Quaternion(rotate.sin, 0.0f, 0.0f, rotate.cos);
                }

                default:
                    throw new InvalidOperationException("Invalid Transform Plane.");
            }
        }

        /// <summary>
        /// Transform a 2D angle into a 3D rotation using the selected transform plane (Slow).
        /// The transformation is slower because the rotation is more complex due to the fact that changing a single axis of rotation requires it to not affect any other axis rotations.
        /// </summary>
        /// <param name="angle">The 2D angle to transform in radians.</param>
        /// <param name="reference">The 3D rotation used as a reference.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ToRotationSlow3D(float angle, Quaternion reference, PhysicsWorld.TransformPlane transformPlane = PhysicsWorld.TransformPlane.XY)
        {
            // Ensure positive Quaternion.
            if (reference.w < 0.0f)
                reference = new Quaternion(-reference.x, -reference.y, -reference.z, -reference.w);

            // Calculate the final rotation.
            var targetPlaneRotation = ToRotationFast3D(angle, transformPlane);
            var planeRotation = Quaternion.Inverse(ToRotationFast3D(ToRotation2D(reference, transformPlane), transformPlane));
            return targetPlaneRotation * planeRotation * reference;
        }
    }
}
