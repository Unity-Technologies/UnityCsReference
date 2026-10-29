// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    /// <summary>
    /// A set of mathematical operations that are useful for physics.
    /// These operations do not form a fully comprehensive mathematics library, they simply provide operations that are usually required when interacting with physics.
    /// </summary>
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public readonly partial record struct PhysicsMath
    {
        #region General Mathematics

        /// <summary>
        /// Get the value of PI used internally by the physics system. Using this will help with determinism.
        /// </summary>
        public static float PI => PhysicsMath_PI();

        /// <summary>
        /// Get the value of tau (2 * PI) used internally by the physics system. Using this will help with determinism.
        /// </summary>
        public static float TAU => PhysicsMath_TAU();

        /// <summary>
        /// A small tolerance value used to test whether a value is close enough to zero to be treated as zero.
        /// </summary>
        public const float Epsilon = 1e-5f;

        /// <summary>
        /// Convert radians to degrees.
        /// This operates as deterministically as possible across platforms.
        /// See <see cref="PhysicsMath.ToRadians(float)"/>.
        /// </summary>
        /// <param name="radians">The radian value to convert to degrees.</param>
        /// <returns>The radian value converted to degrees.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToDegrees(float radians) => PhysicsMath_ToDegrees(radians);

        /// <summary>
        /// Convert degrees to radians.
        /// This operates as deterministically as possible across platforms.
        /// See <see cref="PhysicsMath.ToDegrees(float)"/>.
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
        /// Calculate both the Cosine and Sine of the specified angle.
        /// </summary>
        /// <param name="angle">The angle to calculate, in radians.</param>
        /// <returns>A 2D vector where X is the Cosine and Y is the Sine of the specified angle.</returns>
        public static Vector2 CosSin(float angle)
        {
            CosSin(angle, out var cosine, out var sine);
            return new Vector2(cosine, sine);
        }

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
        /// Get whether both components of the specified vector are finite i.e. neither NaN nor infinite.
        /// </summary>
        /// <param name="value">The vector to check.</param>
        /// <returns>Whether the vector is finite.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFinite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);

        #endregion

        #region General Transform Plane

        /// <summary>
        /// Get the used translation axes, given the specified transform plane.
        /// This is the inverse of <see cref="PhysicsMath.GetTranslationIgnoredAxes(PhysicsWorld.TransformPlane)"/>.
        /// If <see cref="PhysicsWorld.TransformPlane.Custom"/> is used,  <see cref="PhysicsWorld.TransformPlane.XY"/> will be used instead which may not provide the correct results.
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
                PhysicsWorld.TransformPlane.Custom => Vector3.right + Vector3.up,
                _ => throw new InvalidOperationException("Invalid Transform Plane."),
            };
        }

        /// <summary>
        /// Get the ignored translation axes, given the specified transform plane.
        /// This is the inverse of <see cref="PhysicsMath.GetTranslationAxes(PhysicsWorld.TransformPlane)"/>.
        /// If <see cref="PhysicsWorld.TransformPlane.Custom"/> is used,  <see cref="PhysicsWorld.TransformPlane.XY"/> will be used instead which may not provide the correct results.
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
                PhysicsWorld.TransformPlane.Custom => Vector3.forward,
                _ => throw new InvalidOperationException("Invalid Transform Plane."),
            };
        }

        /// <summary>
        /// Get the ignored translation axis, given the specified transform plane.
        /// If <see cref="PhysicsWorld.TransformPlane.Custom"/> is used,  <see cref="PhysicsWorld.TransformPlane.XY"/> will be used instead which may not provide the correct results.
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
                PhysicsWorld.TransformPlane.Custom => position.z,
                _ => throw new InvalidOperationException("Invalid Transform Plane."),
            };
        }

        /// <summary>
        /// Get the used rotation axes, given the specified transform plane.
        /// This is the inverse of <see cref="PhysicsMath.GetRotationIgnoredAxes(PhysicsWorld.TransformPlane)"/>.
        /// If <see cref="PhysicsWorld.TransformPlane.Custom"/> is used,  <see cref="PhysicsWorld.TransformPlane.XY"/> will be used instead which may not provide the correct results.
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
                PhysicsWorld.TransformPlane.Custom => Vector3.forward,
                _ => throw new InvalidOperationException("Invalid Transform Plane."),
            };
        }

        /// <summary>
        /// Get the ignored rotation axes, given the specified transform plane.
        /// This is the inverse of <see cref="PhysicsMath.GetRotationAxes(PhysicsWorld.TransformPlane)"/>.
        /// If <see cref="PhysicsWorld.TransformPlane.Custom"/> is used,  <see cref="PhysicsWorld.TransformPlane.XY"/> will be used instead which may not provide the correct results.
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
                PhysicsWorld.TransformPlane.Custom => Vector3.right + Vector3.up,
                _ => throw new InvalidOperationException("Invalid Transform Plane."),
            };
        }

        /// <summary>
        /// Get the relative transformation matrix between the two specified transform matrix using the specified transform plane to transform into 2D space i.e. XY position and Z rotation.
        /// The transform to transform from is reduced to its position on the plane and its rotation about the plane normal, the two values a physics body is built from, so its scale and any rotation off the plane do not affect the result.
        /// The transform to transform to is placed as it is seen on the plane, so geometry placed with the result lands where a renderer on that transform draws it.
        /// </summary>
        /// <param name="transformFrom">The transform used as a reference to transform from.</param>
        /// <param name="transformTo">The transform used as a reference to transform to.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <param name="transformPlaneCustom">The custom transform plane to use (if required).</param>
        /// <returns>The calculated relative transformation matrix. Its in-plane axes are those of transformTo projected onto the plane, so its scale is not relative.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the transform matrices are not a valid TRS matrix.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 GetRelativeMatrix2D(in Matrix4x4 transformFrom, in Matrix4x4 transformTo, PhysicsWorld.TransformPlane transformPlane, in PhysicsWorld.TransformPlaneCustom transformPlaneCustom)
        {
            PrepareRelative2D(transformFrom, transformTo, ref transformPlane, transformPlaneCustom, out var fromMatrix, out var toMatrix);
            RelativeAxes2D(fromMatrix, toMatrix, transformPlane, out var translation, out var axisX, out var axisY);

            // The axes are written as they are, with no split into a rotation and a scale: a tilted "to", or one under a non-uniform parent scale, projects to axes no rotation and scale can describe.
            // The matrix is 2D on every plane: an XY translation and two in-plane axes, with the transform plane applied once by whatever uses it.
            // The out-of-plane axis is left at one: it has no meaning in 2D, and anything else would reach whatever reads the largest scale component, such as scaling a shape radius.
            var relative = Matrix4x4.identity;
            relative.m00 = axisX.x;
            relative.m10 = axisX.y;
            relative.m01 = axisY.x;
            relative.m11 = axisY.y;
            relative.m03 = translation.x;
            relative.m13 = translation.y;

            return relative;
        }

        /// <summary>
        /// Get the relative transformation pose (translation, rotation and scale) between the two specified transform matrix using the specified transform plane to transform into 2D space.
        /// The transform to transform from is reduced to its position on the plane and its rotation about the plane normal, the two values a physics body is built from, so its scale and any rotation off the plane do not affect the result.
        /// The transform to transform to is described as it is seen on the plane, the same way <see cref="GetRelativeMatrix2D(in Matrix4x4, in Matrix4x4, PhysicsWorld.TransformPlane, in PhysicsWorld.TransformPlaneCustom)"/> places it.
        /// </summary>
        /// <param name="transformFrom">The transform used as a reference to transform from.</param>
        /// <param name="transformTo">The transform used as a reference to transform to.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <param name="transformPlaneCustom">The custom transform plane to use (if required).</param>
        /// <param name="translation">The 2D relative translation.</param>
        /// <param name="rotation">The direction transformTo's X axis points on the plane, relative to transformFrom, in radians.</param>
        /// <param name="scale">The lengths of transformTo's X and Y axes as seen on the plane, with a negative Y when it appears mirrored. Scale is not relative.</param>
        /// <exception cref="InvalidOperationException">Thrown if the transform matrices are not a valid TRS matrix.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetRelativePose2D(in Matrix4x4 transformFrom, in Matrix4x4 transformTo, PhysicsWorld.TransformPlane transformPlane, in PhysicsWorld.TransformPlaneCustom transformPlaneCustom, out Vector2 translation, out float rotation, out Vector2 scale)
        {
            PrepareRelative2D(transformFrom, transformTo, ref transformPlane, transformPlaneCustom, out var fromMatrix, out var toMatrix);
            RelativeAxes2D(fromMatrix, toMatrix, transformPlane, out translation, out var axisX, out var axisY);

            // The rotation and scale are read from the axes as seen, so they compose back to the same placement as the relative matrix.
            // A mirror is placed on the Y scale, decided by whether the two axes appear the other way round.
            var mirrored = (axisX.x * axisY.y) - (axisX.y * axisY.x) < 0f;
            rotation = Atan2(axisX.y, axisX.x);
            scale = new Vector2(axisX.magnitude, mirrored ? -axisY.magnitude : axisY.magnitude);
        }

        // The position of "to" and its two in-plane axes as seen on the plane, relative to the body built from "from".
        // The body turns a local point through the "from" angle alone, so the "to" position and axes are projected and turned back by that angle.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RelativeAxes2D(in Matrix4x4 fromMatrix, in Matrix4x4 toMatrix, PhysicsWorld.TransformPlane transformPlane, out Vector2 translation, out Vector2 axisX, out Vector2 axisY)
        {
            var fromRotation = PhysicsRotate.FromRadians(FromAngle2D(fromMatrix, transformPlane));
            translation = fromRotation.InverseRotateVector(ToPosition2D(toMatrix.GetPosition(), transformPlane) - ToPosition2D(fromMatrix.GetPosition(), transformPlane));
            axisX = fromRotation.InverseRotateVector(Swizzle(toMatrix.MultiplyVector(PlaneRightAxis(transformPlane)), transformPlane));
            axisY = fromRotation.InverseRotateVector(Swizzle(toMatrix.MultiplyVector(PlaneUpAxis(transformPlane)), transformPlane));
        }

        // Validates both matrices and brings them into the plane's own space, reducing a custom plane to XY for the rest of the calculation.
        // The custom plane is a change of world basis, so its inverse is applied on the left of each world matrix.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PrepareRelative2D(in Matrix4x4 transformFrom, in Matrix4x4 transformTo, ref PhysicsWorld.TransformPlane transformPlane, in PhysicsWorld.TransformPlaneCustom transformPlaneCustom, out Matrix4x4 fromMatrix, out Matrix4x4 toMatrix)
        {
            // Validate.
            if (!transformFrom.ValidTRS() || !transformTo.ValidTRS())
                throw new InvalidOperationException("Transform matrix are not valid TRS matrices. Ensure that scaling is non-zero.");

            if (transformPlane != PhysicsWorld.TransformPlane.Custom)
            {
                fromMatrix = transformFrom;
                toMatrix = transformTo;
                return;
            }

            var fromCustom = transformPlaneCustom.fromCustom;
            fromMatrix = fromCustom * transformFrom;
            toMatrix = fromCustom * transformTo;
            transformPlane = PhysicsWorld.TransformPlane.XY;
        }

        // The angle of the "from" transform about the plane normal, in radians, read the way a body built from it reads it: from its rotation whenever one can be extracted.
        // Only a negative determinant has no rotation to extract, and that falls back to the in-plane basis.
        // The in-plane mirror test is deliberately not used here: a half turn about an axis in the plane projects like a mirror, but it is a rotation, and the body reads its twist as one.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float FromAngle2D(in Matrix4x4 matrix, PhysicsWorld.TransformPlane transformPlane)
            => matrix.determinant < 0f ? ToRotation2D(matrix, transformPlane) : ToRotation2D(matrix.rotation, transformPlane);

        // The world axis the specified transform plane ignores.
        // A custom plane is brought into XY space before this is reached, so it uses the same axis as XY.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3 PlaneNormalAxis(PhysicsWorld.TransformPlane transformPlane)
        {
            return transformPlane switch
            {
                PhysicsWorld.TransformPlane.XZ => Vector3.up,
                PhysicsWorld.TransformPlane.ZY => Vector3.right,
                _ => Vector3.forward,
            };
        }

        // The in-plane angle of a transform matrix, in radians, read from the plane's own X axis.
        // Matrix4x4.rotation cannot be used for a mirrored transform: no rotation represents a reflection, so the
        // decomposition falls back to a nearest-rotation that differs between a matrix and its inverse and can leave the
        // result NaN.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ToRotation2D(in Matrix4x4 matrix, PhysicsWorld.TransformPlane transformPlane)
        {
            var planeRight = Swizzle(matrix.MultiplyVector(PlaneRightAxis(transformPlane)), transformPlane);

            return Atan2(planeRight.y, planeRight.x);
        }

        // The world axis the specified transform plane reads its own X axis from.
        // A custom plane is brought into XY space before this is reached, so it uses the same axis as XY.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3 PlaneRightAxis(PhysicsWorld.TransformPlane transformPlane)
            => transformPlane == PhysicsWorld.TransformPlane.ZY ? Vector3.forward : Vector3.right;

        // The world axis the specified transform plane reads its own Y axis from.
        // A custom plane is brought into XY space before this is reached, so it uses the same axis as XY.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3 PlaneUpAxis(PhysicsWorld.TransformPlane transformPlane)
            => transformPlane == PhysicsWorld.TransformPlane.XZ ? Vector3.forward : Vector3.up;

        /// <summary>
        /// Try to get the relative transformation matrix between the two specified transform matrix using the specified transform plane to transform into 2D space, the same as <see cref="GetRelativeMatrix2D"/> but never returning a NaN result.
        /// Each matrix is checked first: if its scale on the plane's ignored axis is at or near zero, such as a matrix built from flattened 2D data, that axis is treated as one instead of leaving the matrix singular.
        /// Returns false, instead of a matrix with a NaN rotation, when either matrix has too small an in-plane scale to work with, or the relative transform itself is too close to singular to invert cleanly.
        /// </summary>
        /// <param name="transformFrom">The transform used as a reference to transform from.</param>
        /// <param name="transformTo">The transform used as a reference to transform to.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <param name="transformPlaneCustom">The custom transform plane to use (if required).</param>
        /// <param name="relative">The calculated relative transformation matrix, or the identity matrix if this returns false.</param>
        /// <returns>Whether a valid relative transformation matrix was calculated.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetRelativeMatrix2D(in Matrix4x4 transformFrom, in Matrix4x4 transformTo, PhysicsWorld.TransformPlane transformPlane, in PhysicsWorld.TransformPlaneCustom transformPlaneCustom, out Matrix4x4 relative)
        {
            relative = Matrix4x4.identity;

            // Build a usable matrix for each side, tolerating a flattened out-of-plane scale on either one.
            if (!TryGetPlaneMatrix2D(transformFrom, transformPlane, out var fromMatrix) ||
                !TryGetPlaneMatrix2D(transformTo, transformPlane, out var toMatrix))
                return false;

            // Reject a relative transform that is too close to singular, which would otherwise produce a NaN rotation.
            GetRelativePose2D(fromMatrix, toMatrix, transformPlane, transformPlaneCustom,
                out var relativeTranslation, out var relativeRotation, out var relativeScale);
            if (!IsFinite(relativeTranslation) || !float.IsFinite(relativeRotation) || !IsFinite(relativeScale))
                return false;

            // Both matrices are valid, so calculate the actual relative matrix.
            var candidate = GetRelativeMatrix2D(fromMatrix, toMatrix, transformPlane, transformPlaneCustom);

            // Reject a "to" that covers no area on the plane, such as one turned edge-on to it: its projected axes are finite but span nothing, so there is nowhere to place geometry.
            var area = (candidate.m00 * candidate.m11) - (candidate.m01 * candidate.m10);
            if (Mathf.Abs(area) < Epsilon)
                return false;

            relative = candidate;
            return true;
        }

        /// <summary>
        /// Try to get a matrix suitable for use with <see cref="GetRelativeMatrix2D"/> or <see cref="GetRelativePose2D"/> from the specified matrix, ignoring its scale on the transform plane's out-of-plane axis.
        /// </summary>
        /// <remarks>
        /// A matrix whose out-of-plane axis is at or near zero length, such as one built from flattened 2D data, has that axis replaced by the plane normal; left as zero it makes the whole matrix singular, and inverting it later would produce a NaN result.
        /// The in-plane axes are never touched, so a mirror or a scale on the plane is kept exactly as it is.
        /// </remarks>
        /// <param name="matrix">The matrix to build from.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <param name="planeMatrix">The resulting matrix, or the default matrix if this returns false.</param>
        /// <returns>Whether a valid matrix was calculated. Returns false when the matrix has an in-plane axis too small to build from.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetPlaneMatrix2D(Matrix4x4 matrix, PhysicsWorld.TransformPlane transformPlane, out Matrix4x4 planeMatrix)
        {
            planeMatrix = default;

            // The axes are measured by their length rather than through lossyScale, which carries the sign of a mirror on whichever axis it chooses and so cannot tell which axis is actually mirrored or flattened.
            var planeRightLength = matrix.MultiplyVector(PlaneRightAxis(transformPlane)).magnitude;
            var planeUpLength = matrix.MultiplyVector(PlaneUpAxis(transformPlane)).magnitude;
            if (planeRightLength < Epsilon || planeUpLength < Epsilon)
                return false;

            // A safely non-zero out-of-plane axis leaves the matrix invertible as-is, so it can be used directly.
            var planeNormal = PlaneNormalAxis(transformPlane);
            if (matrix.MultiplyVector(planeNormal).magnitude >= Epsilon)
            {
                planeMatrix = matrix;
                return planeMatrix.ValidTRS();
            }

            // A flattened out-of-plane axis is replaced by the plane normal, leaving the position and the in-plane axes exactly as they are.
            planeMatrix = matrix;
            planeMatrix.SetColumn(PlaneNormalColumn(transformPlane), planeNormal);
            return true;
        }

        // The column of a matrix that holds the axis the specified transform plane ignores.
        // A custom plane is brought into XY space before this is reached, so it uses the same column as XY.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int PlaneNormalColumn(PhysicsWorld.TransformPlane transformPlane)
        {
            return transformPlane switch
            {
                PhysicsWorld.TransformPlane.XZ => 1,
                PhysicsWorld.TransformPlane.ZY => 0,
                _ => 2,
            };
        }

        /// <summary>
        /// Transform a 3D vector into a 3D vector using the selected transform plane.
        /// If <see cref="PhysicsWorld.TransformPlane.Custom"/> is used,  <see cref="PhysicsWorld.TransformPlane.XY"/> will be used instead which may not provide the correct results.
        /// </summary>
        /// <param name="position">The 3D vector to transform.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <returns>The transformed vector.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the TransformPlane is unknown.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Swizzle(Vector3 position, PhysicsWorld.TransformPlane transformPlane = PhysicsWorld.TransformPlane.XY)
        {
            return transformPlane switch
            {
                PhysicsWorld.TransformPlane.XY => position,
                PhysicsWorld.TransformPlane.XZ => new Vector3(position.x, position.z, position.y),
                PhysicsWorld.TransformPlane.ZY => new Vector3(position.z, position.y, position.x),
                PhysicsWorld.TransformPlane.Custom => position,
                _ => throw new InvalidOperationException("Invalid Transform Plane."),
            };
        }

        /// <summary>
        /// Transform a 3D position (with perspective divide) into a 3D position using the selected transform plane.
        /// If <see cref="PhysicsWorld.TransformPlane.Custom"/> is used,  <see cref="PhysicsWorld.TransformPlane.XY"/> will be used instead which may not provide the correct results.
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
        /// If <see cref="PhysicsWorld.TransformPlane.Custom"/> is used,  <see cref="PhysicsWorld.TransformPlane.XY"/> will be used instead which may not provide the correct results.
        /// </summary>
        /// <param name="matrix">The Matrix position to transform.</param>
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
        /// <param name="transformPlaneCustom">The custom transform plane (if required).</param>
        /// <returns>The transformed position.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the TransformPlane is unknown.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToPosition3D(Vector2 position, Vector3 reference, PhysicsWorld.TransformPlane transformPlane, in PhysicsWorld.TransformPlaneCustom transformPlaneCustom)
        {
            // If not using a custom transform plane then use the standard conversion.
            if (transformPlane != PhysicsWorld.TransformPlane.Custom)
                return ToPosition3D(position, reference, transformPlane);

            // The reference preserves the depth off the plane, so it is brought into the plane's own space to read that depth before mapping back out.
            var depth = transformPlaneCustom.fromCustom.MultiplyPoint(reference).z;
            return transformPlaneCustom.toCustom.MultiplyPoint(new Vector3(position.x, position.y, depth));
        }

        /// <summary>
        /// Transform a 3D position into a 2D position using the selected transform plane.
        /// This can also be used for a 3D scale vector as it undergoes the same transformation.
        /// </summary>
        /// <param name="position">The 3D position to transform.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <param name="transformPlaneCustom">The custom transform plane, used when transformPlane is TransformPlane.Custom.</param>
        /// <returns>The transformed position.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the TransformPlane is unknown.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToPosition2D(Vector3 position, PhysicsWorld.TransformPlane transformPlane, in PhysicsWorld.TransformPlaneCustom transformPlaneCustom)
        {
            // If not using a custom transform plane then use the standard conversion.
            if (transformPlane != PhysicsWorld.TransformPlane.Custom)
                return ToPosition2D(position, transformPlane);

            // Transform by custom transform plane.
            return transformPlaneCustom.FromPosition(position);
        }

        /// <summary>
        /// Transform a 3D rotation into a 2D angle using the selected transform plane.
        /// </summary>
        /// <param name="rotation">The 3D rotation to transform.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <param name="transformPlaneCustom">The custom transform plane, used when transformPlane is TransformPlane.Custom.</param>
        /// <returns>The transformed rotation in radians.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the TransformPlane is unknown.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToRotation2D(Quaternion rotation, PhysicsWorld.TransformPlane transformPlane, in PhysicsWorld.TransformPlaneCustom transformPlaneCustom)
        {
            // If not using a custom transform plane then use the standard conversion.
            if (transformPlane != PhysicsWorld.TransformPlane.Custom)
                return ToRotation2D(rotation, transformPlane);

            // Transform by custom transform plane.
            return ToRotation2D(transformPlaneCustom.fromCustom.rotation * rotation, PhysicsWorld.TransformPlane.XY);
        }

        /// <summary>
        /// Transform a 2D angle into a 3D rotation using the selected transform plane and the custom transform plane (Fast).
        /// The transformation is fast because the rotation is simplified by the fact that only a single axis of rotation is handled. All other axis rotations are reset to zero.
        /// </summary>
        /// <param name="angle">The 2D angle to transform in radians.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <param name="transformPlaneCustom">The custom transform plane (if required).</param>
        /// <returns>The transformed rotation.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the TransformPlane is unknown.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ToRotationFast3D(float angle, PhysicsWorld.TransformPlane transformPlane, in PhysicsWorld.TransformPlaneCustom transformPlaneCustom)
        {
            // If not using a custom transform plane then use the standard conversion.
            if (transformPlane != PhysicsWorld.TransformPlane.Custom)
                return ToRotationFast3D(angle, transformPlane);

            // The angle is in the plane's own frame, so the plane's rotation is applied on top of it.
            return transformPlaneCustom.toCustom.rotation * ToRotationFast3D(angle, PhysicsWorld.TransformPlane.XY);
        }

        /// <summary>
        /// Transform a 2D angle into a 3D rotation using the selected transform plane and the custom transform plane (Slow).
        /// The transformation is slower because the rotation is more complex due to the fact that changing a single axis of rotation requires it to not affect any other axis rotations.
        /// </summary>
        /// <param name="angle">The 2D angle to transform in radians.</param>
        /// <param name="reference">The 3D rotation used as a reference.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <param name="transformPlaneCustom">The custom transform plane (if required).</param>
        /// <returns>The transformed rotation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ToRotationSlow3D(float angle, Quaternion reference, PhysicsWorld.TransformPlane transformPlane, in PhysicsWorld.TransformPlaneCustom transformPlaneCustom)
        {
            // Ensure positive Quaternion.
            if (reference.w < 0.0f)
                reference = new Quaternion(-reference.x, -reference.y, -reference.z, -reference.w);

            // Calculate the final rotation, keeping the reference's rotation off the plane.
            var targetPlaneRotation = ToRotationFast3D(angle, transformPlane, transformPlaneCustom);
            var planeRotation = Quaternion.Inverse(ToRotationFast3D(ToRotation2D(reference, transformPlane, transformPlaneCustom), transformPlane, transformPlaneCustom));
            return targetPlaneRotation * planeRotation * reference;
        }

        #endregion

        #region Non-Custom Transform Plane

        /// <summary>
        /// Transform a 3D position into a 2D position using the selected transform plane.
        /// </summary>
        /// <param name="position">The 3D position to transform.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <returns>The transformed position.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the TransformPlane is unknown.</exception>
        /// <exception cref="System.NotSupportedException">Thrown if TransformPlane.Custom is used.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToPosition2D(Vector3 position, PhysicsWorld.TransformPlane transformPlane = PhysicsWorld.TransformPlane.XY)
        {
            return transformPlane switch
            {
                PhysicsWorld.TransformPlane.XY => position,
                PhysicsWorld.TransformPlane.XZ => new Vector2(position.x, position.z),
                PhysicsWorld.TransformPlane.ZY => new Vector2(position.z, position.y),
                PhysicsWorld.TransformPlane.Custom => throw new NotSupportedException("TransformPlane.Custom is not supported by this method."),
                _ => throw new InvalidOperationException("Invalid Transform Plane."),
            };
        }

        /// <summary>
        /// Transform a 2D position into a 3D position using the selected transform plane.
        /// </summary>
        /// <param name="position">The 2D position to transform.</param>
        /// <param name="reference">The 3D position used as a reference.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <returns>The transformed position.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the TransformPlane is unknown.</exception>
        /// <exception cref="System.NotSupportedException">Thrown if TransformPlane.Custom is used.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToPosition3D(Vector2 position, Vector3 reference, PhysicsWorld.TransformPlane transformPlane = PhysicsWorld.TransformPlane.XY)
        {
            return transformPlane switch
            {
                PhysicsWorld.TransformPlane.XY => new Vector3(position.x, position.y, reference.z),
                PhysicsWorld.TransformPlane.XZ => new Vector3(position.x, reference.y, position.y),
                PhysicsWorld.TransformPlane.ZY => new Vector3(reference.x, position.y, position.x),
                PhysicsWorld.TransformPlane.Custom => throw new NotSupportedException("TransformPlane.Custom is not supported by this method."),
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
        /// <exception cref="System.NotSupportedException">Thrown if TransformPlane.Custom is used.</exception>
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
                PhysicsWorld.TransformPlane.Custom => throw new NotSupportedException("TransformPlane.Custom is not supported by this method."),
                _ => throw new InvalidOperationException("Invalid Transform Plane."),
            };
        }

        /// <summary>
        /// Transform a 2D angle into a 3D rotation using the selected transform plane (Fast).
        /// The transformation is fast because the rotation is simplified by the fact that only a single axis of rotation is handled. All other axis rotations are reset to zero.
        /// </summary>
        /// <param name="angle">The 2D angle to transform in radians.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <returns>The transformed rotation.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the TransformPlane is unknown.</exception>
        /// <exception cref="System.NotSupportedException">Thrown if TransformPlane.Custom is used.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ToRotationFast3D(float angle, PhysicsWorld.TransformPlane transformPlane = PhysicsWorld.TransformPlane.XY)
        {
            switch (transformPlane)
            {
                case PhysicsWorld.TransformPlane.XY:
                {
                    var rotate = PhysicsRotate.FromRadians(angle * 0.5f);
                    return new Quaternion(0.0f, 0.0f, rotate.sin, rotate.cos);
                }

                case PhysicsWorld.TransformPlane.Custom:
                    throw new NotSupportedException("TransformPlane.Custom is not supported by this method.");

                case PhysicsWorld.TransformPlane.XZ:
                {
                    var rotate = PhysicsRotate.FromRadians(angle * -0.5f);
                    return new Quaternion(0.0f, rotate.sin, 0.0f, rotate.cos);
                }

                case PhysicsWorld.TransformPlane.ZY:
                {
                    var rotate = PhysicsRotate.FromRadians(angle * -0.5f);
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
        /// <returns>The transformed rotation.</returns>
        /// <exception cref="System.NotSupportedException">Thrown if TransformPlane.Custom is used.</exception>
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

        /// <summary>
        /// Transform a 3D <see cref="UnityEngine.Transform"/> position and rotation to a 2D <see cref="PhysicsTransform"/>.
        /// Scale is not part of a <see cref="PhysicsTransform"/> therefore it is ignored.
        /// </summary>
        /// <remarks>
        /// For a custom transform plane use <see cref="ToPhysicsTransform(Transform, PhysicsWorld)"/> instead.
        /// </remarks>
        /// <param name="transform">The 3D transform to use.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <returns>The 2D transform.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the TransformPlane is unknown.</exception>
        /// <exception cref="System.NotSupportedException">Thrown if TransformPlane.Custom is used.</exception>
        public static PhysicsTransform ToPhysicsTransform(Transform transform, PhysicsWorld.TransformPlane transformPlane = PhysicsWorld.TransformPlane.XY)
        {
            return new PhysicsTransform
            {
                position = ToPosition2D(transform.position, transformPlane),
                rotation = PhysicsRotate.FromRadians(ToRotation2D(transform.rotation, transformPlane))
            };
        }

        /// <summary>
        /// Set the Transform position and rotation using the specified <see cref="PhysicsWorld.TransformPlane"/>.
        /// For position, only two axis will be updated with the others remaining unchanged.
        /// For rotation, only a single rotation axis will be changed with the others set to zero.
        ///
        /// See: <see cref="PhysicsMath.ToRotationFast3D(float, PhysicsWorld.TransformPlane)"/> and <see cref="PhysicsWorld.SetTransform(Transform, ref Vector3, ref Quaternion, bool)"/>.
        /// </summary>
        /// <param name="physicsTransform">The physics transform to use as the source of the pose.</param>
        /// <param name="transform">The Transform to set.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <param name="transformChangedEvent">By default, no transform changed event will be produced however this behaviour can be overridden with this argument.</param>
        /// <exception cref="System.NotSupportedException">Thrown if TransformPlane.Custom is used. Use <see cref="SetTransformFast2D(PhysicsTransform, Transform, PhysicsWorld, bool)"/> instead.</exception>
        public static void SetTransformFast2D(PhysicsTransform physicsTransform, Transform transform, PhysicsWorld.TransformPlane transformPlane = PhysicsWorld.TransformPlane.XY, bool transformChangedEvent = false)
        {
            var position = ToPosition3D(physicsTransform.position, transform.position, transformPlane);
            var rotation = ToRotationFast3D(physicsTransform.rotation.radians, transformPlane);

            PhysicsWorld.SetTransform(transform, ref position, ref rotation, transformChangedEvent);
        }

        /// <summary>
        /// Set the Transform position and rotation using the specified <see cref="PhysicsWorld.TransformPlane"/>.
        /// For position, only two axis will be updated with the others remaining unchanged.
        /// For rotation, only a single rotation axis will be changed with the others remaining unchanged.
        ///
        /// See: <see cref="PhysicsMath.ToRotationSlow3D(float, Quaternion, PhysicsWorld.TransformPlane)"/> and <see cref="PhysicsWorld.SetTransform(Transform, ref Vector3, ref Quaternion, bool)"/>.
        /// </summary>
        /// <param name="physicsTransform">The physics transform to use as the source of the pose.</param>
        /// <param name="transform">The Transform to set.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <param name="transformChangedEvent">By default, no transform changed event will be produced however this behaviour can be overridden with this argument.</param>
        /// <exception cref="System.NotSupportedException">Thrown if TransformPlane.Custom is used. Use <see cref="SetTransformSlow3D(PhysicsTransform, Transform, PhysicsWorld, bool)"/> instead.</exception>
        public static void SetTransformSlow3D(PhysicsTransform physicsTransform, Transform transform, PhysicsWorld.TransformPlane transformPlane = PhysicsWorld.TransformPlane.XY, bool transformChangedEvent = false)
        {
            var position = ToPosition3D(physicsTransform.position, transform.position, transformPlane);
            var rotation = ToRotationSlow3D(physicsTransform.rotation.radians, transform.rotation, transformPlane);

            PhysicsWorld.SetTransform(transform, ref position, ref rotation, transformChangedEvent);
        }

        /// <summary>
        /// Calculate a <see cref="UnityEngine.Quaternion"/> given a 2D angular velocity and a time to integrate over using the selected transform plane.
        /// </summary>
        /// <param name="angularVelocity">The 2D angular velocity, in radians.</param>
        /// <param name="deltaTime">The time over which to apply the angular velocity, in seconds.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <returns>The transformed rotation.</returns>
        /// <exception cref="System.InvalidOperationException">Thrown if the TransformPlane is unknown.</exception>
        /// <exception cref="System.NotSupportedException">Thrown if TransformPlane.Custom is used.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion AngularVelocityToQuaternion(float angularVelocity, float deltaTime, PhysicsWorld.TransformPlane transformPlane)
        {
            if (transformPlane == PhysicsWorld.TransformPlane.Custom)
                throw new NotSupportedException("TransformPlane.Custom is not supported by this method.");

            // The rotation over the time is a turn through the angle travelled, built the same way as every other 2D angle so it turns in the same direction on every plane.
            return ToRotationFast3D(angularVelocity * deltaTime, transformPlane);
        }

        /// <summary>
        /// Get the relative transformation matrix between the two specified transforms using the specified transform plane.
        /// </summary>
        /// <remarks>
        /// For a custom transform plane use <see cref="GetRelativeMatrix2D(in Matrix4x4, in Matrix4x4, PhysicsWorld)"/> with each transform's local-to-world matrix instead.
        /// </remarks>
        /// <param name="transformFrom">The transform used as a reference to transform from. Only its position and rotation are used, the two values a physics body is built from, so its scale and any mirror above it do not affect the result.</param>
        /// <param name="transformTo">The transform used as a reference to transform to.</param>
        /// <param name="transformPlane">The transform plane to use.</param>
        /// <param name="useScale">If the returned matrix should include the scale of transformTo itself. When false, only its own local scale is removed; scale from its parents is kept.</param>
        /// <returns>The calculated relative transformation matrix, the same as <see cref="GetRelativeMatrix2D(in Matrix4x4, in Matrix4x4, PhysicsWorld.TransformPlane, in PhysicsWorld.TransformPlaneCustom)"/> gives for a matrix built from transformFrom's position and rotation and transformTo's local-to-world matrix.</returns>
        /// <exception cref="System.NotSupportedException">Thrown if TransformPlane.Custom is used.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 GetRelativeMatrix(Transform transformFrom, Transform transformTo, PhysicsWorld.TransformPlane transformPlane = PhysicsWorld.TransformPlane.XY, bool useScale = true)
        {
            if (transformPlane == PhysicsWorld.TransformPlane.Custom)
                throw new NotSupportedException("TransformPlane.Custom is not supported by this method.");

            // The body for transformFrom is built from its position and rotation alone, so that is what it is passed as.
            // Its local-to-world matrix cannot be used: a mirror in it, from its own scale or a parent's, has no single rotation to read and turns the result a half turn.
            var fromMatrix = Matrix4x4.TRS(transformFrom.position, transformFrom.rotation, Vector3.one);
            var toMatrix = useScale ? transformTo.localToWorldMatrix : transformTo.localToWorldMatrix * Matrix4x4.Scale(transformTo.localScale).inverse;

            return GetRelativeMatrix2D(fromMatrix, toMatrix, transformPlane, default);
        }

        #endregion

        #region World Transform Plane

        /// <summary>
        /// Transform a 3D position into a 2D position using the specified world's transform plane.
        /// </summary>
        /// <param name="position">The 3D position to transform.</param>
        /// <param name="world">The world whose transform plane is used.</param>
        /// <returns>The transformed position.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToPosition2D(Vector3 position, PhysicsWorld world) => ToPosition2D(position, world.transformPlane, world.transformPlaneCustom);

        /// <summary>
        /// Transform a 2D position into a 3D position using the specified world's transform plane.
        /// </summary>
        /// <param name="position">The 2D position to transform.</param>
        /// <param name="reference">The 3D position used as a reference.</param>
        /// <param name="world">The world whose transform plane is used.</param>
        /// <returns>The transformed position.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToPosition3D(Vector2 position, Vector3 reference, PhysicsWorld world) => ToPosition3D(position, reference, world.transformPlane, world.transformPlaneCustom);

        /// <summary>
        /// Transform a 3D rotation into a 2D angle using the specified world's transform plane.
        /// </summary>
        /// <param name="rotation">The 3D rotation to transform.</param>
        /// <param name="world">The world whose transform plane is used.</param>
        /// <returns>The transformed rotation in radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToRotation2D(Quaternion rotation, PhysicsWorld world) => ToRotation2D(rotation, world.transformPlane, world.transformPlaneCustom);

        /// <summary>
        /// Transform a Transform's world position into a 2D position using the specified world's transform plane.
        /// </summary>
        /// <param name="transform">The Transform whose world position is transformed.</param>
        /// <param name="world">The world whose transform plane is used.</param>
        /// <returns>The transformed position.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToPosition2D(Transform transform, PhysicsWorld world) => ToPosition2D(transform.position, world);

        /// <summary>
        /// Transform a Transform's world rotation into a 2D angle using the specified world's transform plane.
        /// </summary>
        /// <param name="transform">The Transform whose world rotation is transformed.</param>
        /// <param name="world">The world whose transform plane is used.</param>
        /// <returns>The transformed rotation in radians.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToRotation2D(Transform transform, PhysicsWorld world) => ToRotation2D(transform.rotation, world);

        /// <summary>
        /// Transform a 2D position into a 3D position using the specified world's transform plane, taking the out-of-plane depth from a Transform.
        /// </summary>
        /// <param name="position">The 2D position to transform.</param>
        /// <param name="reference">The Transform whose world position is used as a reference.</param>
        /// <param name="world">The world whose transform plane is used.</param>
        /// <returns>The transformed position.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToPosition3D(Vector2 position, Transform reference, PhysicsWorld world) => ToPosition3D(position, reference.position, world);

        /// <summary>
        /// Transform a 2D angle into a 3D rotation using the specified world's transform plane, keeping the rotation off the plane from a Transform (Slow).
        /// </summary>
        /// <param name="angle">The 2D angle to transform in radians.</param>
        /// <param name="reference">The Transform whose world rotation is used as a reference.</param>
        /// <param name="world">The world whose transform plane is used.</param>
        /// <returns>The transformed rotation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ToRotationSlow3D(float angle, Transform reference, PhysicsWorld world) => ToRotationSlow3D(angle, reference.rotation, world);

        /// <summary>
        /// Transform a 2D angle into a 3D rotation using the specified world's transform plane (Fast).
        /// The transformation is fast because the rotation is simplified by the fact that only a single axis of rotation is handled. All other axis rotations are reset to zero.
        /// </summary>
        /// <param name="angle">The 2D angle to transform in radians.</param>
        /// <param name="world">The world whose transform plane is used.</param>
        /// <returns>The transformed rotation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ToRotationFast3D(float angle, PhysicsWorld world) => ToRotationFast3D(angle, world.transformPlane, world.transformPlaneCustom);

        /// <summary>
        /// Transform a 2D angle into a 3D rotation using the specified world's transform plane (Slow).
        /// The transformation is slower because the rotation is more complex due to the fact that changing a single axis of rotation requires it to not affect any other axis rotations.
        /// </summary>
        /// <param name="angle">The 2D angle to transform in radians.</param>
        /// <param name="reference">The 3D rotation used as a reference.</param>
        /// <param name="world">The world whose transform plane is used.</param>
        /// <returns>The transformed rotation.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ToRotationSlow3D(float angle, Quaternion reference, PhysicsWorld world) => ToRotationSlow3D(angle, reference, world.transformPlane, world.transformPlaneCustom);

        /// <summary>
        /// Transform a 3D <see cref="UnityEngine.Transform"/> position and rotation to a 2D <see cref="PhysicsTransform"/> using the specified world's transform plane.
        /// Scale is not part of a <see cref="PhysicsTransform"/> therefore it is ignored.
        /// </summary>
        /// <param name="transform">The 3D transform to use.</param>
        /// <param name="world">The world whose transform plane is used.</param>
        /// <returns>The 2D transform.</returns>
        public static PhysicsTransform ToPhysicsTransform(Transform transform, PhysicsWorld world)
        {
            return new PhysicsTransform
            {
                position = ToPosition2D(transform.position, world),
                rotation = PhysicsRotate.FromRadians(ToRotation2D(transform.rotation, world))
            };
        }

        /// <summary>
        /// Set the Transform position and rotation using the specified world's transform plane.
        /// For position, only two axis will be updated with the others remaining unchanged.
        /// For rotation, only a single rotation axis will be changed with the others set to zero.
        /// </summary>
        /// <param name="physicsTransform">The physics transform to use as the source of the pose.</param>
        /// <param name="transform">The Transform to set.</param>
        /// <param name="world">The world whose transform plane is used.</param>
        /// <param name="transformChangedEvent">By default, no transform changed event will be produced however this behaviour can be overridden with this argument.</param>
        public static void SetTransformFast2D(PhysicsTransform physicsTransform, Transform transform, PhysicsWorld world, bool transformChangedEvent = false)
        {
            var position = ToPosition3D(physicsTransform.position, transform.position, world);
            var rotation = ToRotationFast3D(physicsTransform.rotation.radians, world);

            PhysicsWorld.SetTransform(transform, ref position, ref rotation, transformChangedEvent);
        }

        /// <summary>
        /// Set the Transform position and rotation using the specified world's transform plane.
        /// For position, only two axis will be updated with the others remaining unchanged.
        /// For rotation, only a single rotation axis will be changed with the others remaining unchanged.
        /// </summary>
        /// <param name="physicsTransform">The physics transform to use as the source of the pose.</param>
        /// <param name="transform">The Transform to set.</param>
        /// <param name="world">The world whose transform plane is used.</param>
        /// <param name="transformChangedEvent">By default, no transform changed event will be produced however this behaviour can be overridden with this argument.</param>
        public static void SetTransformSlow3D(PhysicsTransform physicsTransform, Transform transform, PhysicsWorld world, bool transformChangedEvent = false)
        {
            var position = ToPosition3D(physicsTransform.position, transform.position, world);
            var rotation = ToRotationSlow3D(physicsTransform.rotation.radians, transform.rotation, world);

            PhysicsWorld.SetTransform(transform, ref position, ref rotation, transformChangedEvent);
        }

        /// <summary>
        /// Get the relative transformation matrix between the two specified transform matrix using the specified world's transform plane to transform into 2D space.
        /// </summary>
        /// <param name="transformFrom">The transform used as a reference to transform from.</param>
        /// <param name="transformTo">The transform used as a reference to transform to.</param>
        /// <param name="world">The world whose transform plane is used.</param>
        /// <returns>The calculated relative transformation matrix. Its scale is not relative and always uses the in-plane scale of transformTo.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 GetRelativeMatrix2D(in Matrix4x4 transformFrom, in Matrix4x4 transformTo, PhysicsWorld world)
            => GetRelativeMatrix2D(transformFrom, transformTo, world.transformPlane, world.transformPlaneCustom);

        /// <summary>
        /// Get the relative transformation pose (translation, rotation and scale) between the two specified transform matrix using the specified world's transform plane to transform into 2D space.
        /// </summary>
        /// <param name="transformFrom">The transform used as a reference to transform from.</param>
        /// <param name="transformTo">The transform used as a reference to transform to.</param>
        /// <param name="world">The world whose transform plane is used.</param>
        /// <param name="translation">The 2D relative translation.</param>
        /// <param name="rotation">The 2D relative rotation.</param>
        /// <param name="scale">The 2D relative scale. Scale is not relative and always uses the transformTo lossyScale.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetRelativePose2D(in Matrix4x4 transformFrom, in Matrix4x4 transformTo, PhysicsWorld world, out Vector2 translation, out float rotation, out Vector2 scale)
            => GetRelativePose2D(transformFrom, transformTo, world.transformPlane, world.transformPlaneCustom, out translation, out rotation, out scale);

        /// <summary>
        /// Try to get the relative transformation matrix between the two specified transform matrix using the specified world's transform plane, never returning a NaN result.
        /// </summary>
        /// <param name="transformFrom">The transform used as a reference to transform from.</param>
        /// <param name="transformTo">The transform used as a reference to transform to.</param>
        /// <param name="world">The world whose transform plane is used.</param>
        /// <param name="relative">The calculated relative transformation matrix, or the identity matrix if this returns false.</param>
        /// <returns>Whether a valid relative transformation matrix was calculated.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetRelativeMatrix2D(in Matrix4x4 transformFrom, in Matrix4x4 transformTo, PhysicsWorld world, out Matrix4x4 relative)
            => TryGetRelativeMatrix2D(transformFrom, transformTo, world.transformPlane, world.transformPlaneCustom, out relative);

        #endregion
    }
}
