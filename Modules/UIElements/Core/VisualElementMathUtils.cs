// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace UnityEngine.UIElements
{
    public partial class VisualElement
    {
        Vector3 positionWithLayout
        {
            get { return ResolveTranslate() + (Vector3)layout.min; }
        }

        internal void GetPivotedMatrixWithLayout(out Matrix4x4 result)
        {
            var transformOrigin = ResolveTransformOrigin();
            result = Matrix4x4.TRS(positionWithLayout + transformOrigin, ResolveRotation(), ResolveScale());
            TranslateMatrix34InPlace(ref result, -transformOrigin);
        }

        // Used to determine whether this VisualElement can use simplified maths for its transform calculations.
        // Most elements will return true from this method, which should save a lot more time than it costs to evaluate.
        internal bool hasDefaultRotationAndScale
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get
            {
                return computedStyle.rotate.angle.value == 0f &&
                    computedStyle.scale.value == Vector3.one;
            }
        }

        internal bool has3DTransform => has3DTranslation || has3DRotation;

        private bool has3DTranslation => computedStyle.translate.z != 0;
        private bool has3DRotation
        {
            get
            {
                var r = computedStyle.rotate;
                return r.angle != 0 && r.axis != Vector3.forward;
            }
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        internal static float Min(float a, float b, float c, float d)
        {
            return Mathf.Min(Mathf.Min(a, b), Mathf.Min(c, d));
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        internal static float Max(float a, float b, float c, float d)
        {
            return Mathf.Max(Mathf.Max(a, b), Mathf.Max(c, d));
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        internal void TransformAlignedBoundsToParentSpace(ref Bounds bounds)
        {
            if (hasDefaultRotationAndScale)
            {
                bounds.center += positionWithLayout;
            }
            else
            {
                GetPivotedMatrixWithLayout(out var m);
                bounds = CalculateConservativeBounds(ref m, bounds);
            }
        }

        // Returns the same result as TransformAlignedRect(pivotedMatrixWithLayout, rec), but will try to use
        // simplified calculations for elements with no scale or rotation.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        void TransformAlignedRectToParentSpace(ref Rect rect)
        {
            if (hasDefaultRotationAndScale)
            {
                rect.position += (Vector2)positionWithLayout;
            }
            else
            {
                GetPivotedMatrixWithLayout(out var m);
                rect = CalculateConservativeRect(ref m, rect);
            }
        }

        internal static Rect CalculateConservativeRect(ref Matrix4x4 matrix, Rect rect)
        {
            //Mathf.Min does not check for NAN
            if (float.IsNaN(rect.height) | float.IsNaN(rect.width) | float.IsNaN(rect.x) | float.IsNaN(rect.y))
            {
                //fall back to old algorithm
                rect = new Rect(MultiplyMatrix44Point2(ref matrix, rect.position),
                    MultiplyVector2(ref matrix, rect.size));
                OrderMinMaxRect(ref rect);
                return rect;
            }

            var topLeft = new Vector2(rect.xMin, rect.yMin);
            var bottomRight = new Vector2(rect.xMax, rect.yMax);
            var topRight = new Vector2(rect.xMax, rect.yMin);
            var bottomLeft = new Vector2(rect.xMin, rect.yMax);

            var transformedTL = matrix.MultiplyPoint3x4(topLeft);
            var transformedBR = matrix.MultiplyPoint3x4(bottomRight);
            var transformedRL = matrix.MultiplyPoint3x4(topRight);
            var transformedBL = matrix.MultiplyPoint3x4(bottomLeft);

            Vector2 min = new Vector2(
                Min(transformedTL.x, transformedBR.x, transformedRL.x, transformedBL.x),
                Min(transformedTL.y, transformedBR.y, transformedRL.y, transformedBL.y));

            Vector2 max = new Vector2(
                Max(transformedTL.x, transformedBR.x, transformedRL.x, transformedBL.x),
                Max(transformedTL.y, transformedBR.y, transformedRL.y, transformedBL.y));

            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        internal static Bounds CalculateConservativeBounds(ref Matrix4x4 matrix, Bounds bounds)
        {
            bool IsNaN(Vector3 v) => float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z);

            //Mathf.Min does not check for NAN
            if (IsNaN(bounds.center) | IsNaN(bounds.extents))
            {
                //fall back to old algorithm
                bounds = new Bounds(matrix.MultiplyPoint3x4(bounds.center), matrix.MultiplyVector(bounds.size));
                OrderMinMaxBounds(ref bounds);
                return bounds;
            }

            var bMin = bounds.min;
            var bMax = bounds.max;
            var rMin = Vector3.zero;
            var rMax = Vector3.zero;

            // For all 8 vertices of the cube
            for (var i = 0; i < 8; i++)
            {
                var vertex = new Vector3(
                    (i & 1) != 0 ? bMax.x : bMin.x,
                    (i & 2) != 0 ? bMax.y : bMin.y,
                    (i & 4) != 0 ? bMax.z : bMin.z
                );

                vertex = matrix.MultiplyPoint3x4(vertex);
                rMin = i == 0 ? vertex : Vector3.Min(rMin, vertex);
                rMax = i == 0 ? vertex : Vector3.Max(rMax, vertex);
            }

            bounds.SetMinMax(rMin, rMax);
            return bounds;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        internal static void TransformAlignedRect(ref Matrix4x4 matrix, ref Rect rect)
        {
            rect =  CalculateConservativeRect(ref matrix, rect);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        internal static void TransformAlignedBounds(ref Matrix4x4 matrix, ref Bounds bounds)
        {
            bounds = CalculateConservativeBounds(ref matrix, bounds);
        }

        internal static void OrderMinMaxRect(ref Rect rect)
        {
            if (rect.width < 0)
            {
                rect.x += rect.width;
                rect.width = -rect.width;
            }
            if (rect.height < 0)
            {
                rect.y += rect.height;
                rect.height = -rect.height;
            }
        }

        internal static void OrderMinMaxBounds(ref Bounds bounds)
        {
            var e = bounds.extents;
            bounds.extents = new Vector3(Mathf.Abs(e.x), Mathf.Abs(e.y), Mathf.Abs(e.z));
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        internal static Vector2 MultiplyMatrix44Point2(ref Matrix4x4 lhs, Vector2 point)
        {
            Vector2 res;
            res.x = lhs.m00 * point.x + lhs.m01 * point.y + lhs.m03;
            res.y = lhs.m10 * point.x + lhs.m11 * point.y + lhs.m13;
            return res;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        internal static Vector3 MultiplyMatrix44Point2ToPoint3(ref Matrix4x4 lhs, Vector2 point)
        {
            Vector3 res;
            res.x = lhs.m00 * point.x + lhs.m01 * point.y + lhs.m03;
            res.y = lhs.m10 * point.x + lhs.m11 * point.y + lhs.m13;
            res.z = lhs.m20 * point.x + lhs.m21 * point.y + lhs.m23;
            return res;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        internal static Vector2 MultiplyMatrix44Point3ToPoint2(ref Matrix4x4 lhs, Vector3 point)
        {
            Vector2 res;
            res.x = lhs.m00 * point.x + lhs.m01 * point.y + lhs.m02 * point.z + lhs.m03;
            res.y = lhs.m10 * point.x + lhs.m11 * point.y + lhs.m12 * point.z + lhs.m13;
            return res;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        internal static Vector2 MultiplyVector2(ref Matrix4x4 lhs, Vector2 vector)
        {
            Vector2 res;
            res.x = lhs.m00 * vector.x + lhs.m01 * vector.y;
            res.y = lhs.m10 * vector.x + lhs.m11 * vector.y;
            return res;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        internal static Rect MultiplyMatrix44Rect2(ref Matrix4x4 lhs, Rect r)
        {
            r.position = MultiplyMatrix44Point2(ref lhs, r.position);
            r.size = MultiplyVector2(ref lhs, r.size);
            return r;
        }

        internal static void MultiplyMatrix34(ref Matrix4x4 lhs, ref Matrix4x4 rhs, out Matrix4x4 res)
        {
            res.m00 = lhs.m00 * rhs.m00 + lhs.m01 * rhs.m10 + lhs.m02 * rhs.m20;
            res.m01 = lhs.m00 * rhs.m01 + lhs.m01 * rhs.m11 + lhs.m02 * rhs.m21;
            res.m02 = lhs.m00 * rhs.m02 + lhs.m01 * rhs.m12 + lhs.m02 * rhs.m22;
            res.m03 = lhs.m00 * rhs.m03 + lhs.m01 * rhs.m13 + lhs.m02 * rhs.m23 + lhs.m03;

            res.m10 = lhs.m10 * rhs.m00 + lhs.m11 * rhs.m10 + lhs.m12 * rhs.m20;
            res.m11 = lhs.m10 * rhs.m01 + lhs.m11 * rhs.m11 + lhs.m12 * rhs.m21;
            res.m12 = lhs.m10 * rhs.m02 + lhs.m11 * rhs.m12 + lhs.m12 * rhs.m22;
            res.m13 = lhs.m10 * rhs.m03 + lhs.m11 * rhs.m13 + lhs.m12 * rhs.m23 + lhs.m13;

            res.m20 = lhs.m20 * rhs.m00 + lhs.m21 * rhs.m10 + lhs.m22 * rhs.m20;
            res.m21 = lhs.m20 * rhs.m01 + lhs.m21 * rhs.m11 + lhs.m22 * rhs.m21;
            res.m22 = lhs.m20 * rhs.m02 + lhs.m21 * rhs.m12 + lhs.m22 * rhs.m22;
            res.m23 = lhs.m20 * rhs.m03 + lhs.m21 * rhs.m13 + lhs.m22 * rhs.m23 + lhs.m23;

            res.m30 = 0;
            res.m31 = 0;
            res.m32 = 0;
            res.m33 = 1;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        static void TranslateMatrix34(ref Matrix4x4 lhs, Vector3 rhs, out Matrix4x4 res)
        {
            res = lhs;
            TranslateMatrix34InPlace(ref res, rhs);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        static void TranslateMatrix34InPlace(ref Matrix4x4 lhs, Vector3 rhs)
        {
            lhs.m03 += lhs.m00 * rhs.x + lhs.m01 * rhs.y + lhs.m02 * rhs.z;
            lhs.m13 += lhs.m10 * rhs.x + lhs.m11 * rhs.y + lhs.m12 * rhs.z;
            lhs.m23 += lhs.m20 * rhs.x + lhs.m21 * rhs.y + lhs.m22 * rhs.z;
        }
    }

    public static partial class VisualElementExtensions
    {
        /// <summary>
        /// Transforms a point from the world space to the local space of the element.
        /// </summary>
        /// <remarks>
        /// This element needs to be attached to a panel and must have a valid <see cref="VisualElement.layout"/>.
        /// Otherwise, this method might return invalid results.
        /// </remarks>
        /// <remarks>
        /// If the element's transform contains 3-D information, use
        /// ele.worldTransform.inverse.MultiplyPoint3x4(p) to obtain a proper 3-D transformation.
        /// </remarks>
        /// <param name="ele">The element to use as a reference for the local space.</param>
        /// <param name="p">The point to transform, in world space.</param>
        /// <returns>A point in the local space of the element.</returns>
        public static Vector2 WorldToLocal(this VisualElement ele, Vector2 p)
        {
            if (ele == null)
            {
                throw new ArgumentNullException(nameof(ele));
            }

            return VisualElement.MultiplyMatrix44Point2(ref ele.worldTransformInverse, p);
        }

        /// <summary>
        /// Transforms a point from the world space to the local space of the element.
        /// The result is stored in a Vector3, such that the original point can be reobtained
        /// by applying LocalToWorld3D with the same element on the returned point.
        /// </summary>
        /// <remarks>
        /// This element needs to be attached to a panel and must have a valid <see cref="VisualElement.layout"/>.
        /// Otherwise, this method might return invalid results.
        /// </remarks>
        /// <param name="ele">The element to use as a reference for the local space.</param>
        /// <param name="p">The point to transform, in world space.</param>
        /// <returns>A point in the local space of the element.</returns>
        // IMPORTANT:
        // This method can't be called WorldToLocal because it would break existing code
        // for example:
        // Vector2 GetLocalPointWithOffset(VisualElement ve, Vector3 worldPoint)
        // {
        //     return ve.WorldToLocal(worldPoint) - new Vector2(10, 10);
        // }
        // Such examples can be found in some packages, for instance:
        // Packages/com.unity.graphtoolsauthoringframework/GraphToolsAuthoringFrameworkEditor/UI/Manipulators/WireManipulator.cs
        internal static Vector3 WorldToLocal3D(this VisualElement ele, Vector3 p)
        {
            if (ele == null)
            {
                throw new ArgumentNullException(nameof(ele));
            }

            return ele.worldTransformInverse.MultiplyPoint3x4(p);
        }

        /// <summary>
        /// Transforms a point from the local space of the element to the world space.
        /// </summary>
        /// <remarks>
        /// This element needs to be attached to a panel and must receive a valid <see cref="VisualElement.layout"/>.
        /// Otherwise, this method may return invalid results.
        /// </remarks>
        /// <remarks>
        /// If the element's transform contains 3-D information, use
        /// ele.worldTransform.MultiplyPoint3x4(p) to obtain a proper 3-D transformation.
        /// </remarks>
        /// <param name="ele">The element to use as a reference for the local space.</param>
        /// <param name="p">The point to transform, in local space.</param>
        /// <returns>A point in the world space.</returns>
        public static Vector2 LocalToWorld(this VisualElement ele, Vector2 p)
        {
            if (ele == null)
            {
                throw new ArgumentNullException(nameof(ele));
            }

            return VisualElement.MultiplyMatrix44Point2(ref ele.worldTransformRef, p);
        }

        /// <summary>
        /// Transforms a point from the local space of the element to the world space.
        /// The result is stored in a Vector3, such that the original point can be reobtained
        /// by applying WorldToLocal3D with the same element on the returned point.
        /// </summary>
        /// <remarks>
        /// This element needs to be attached to a panel and must receive a valid <see cref="VisualElement.layout"/>.
        /// Otherwise, this method may return invalid results.
        /// </remarks>
        /// <param name="ele">The element to use as a reference for the local space.</param>
        /// <param name="p">The point to transform, in local space.</param>
        /// <returns>A point in the world space.</returns>
        internal static Vector3 LocalToWorld3D(this VisualElement ele, Vector3 p)
        {
            if (ele == null)
            {
                throw new ArgumentNullException(nameof(ele));
            }

            return ele.worldTransformRef.MultiplyPoint3x4(p);
        }

        /// <summary>
        /// Transforms a rectangle from the world space to the local space of the element.
        /// </summary>
        /// <remarks>
        /// This element needs to be attached to a panel and must receive a valid <see cref="VisualElement.layout"/>.
        /// Otherwise, this method may return invalid results.
        /// </remarks>
        /// <param name="ele">The element to use as a reference for the local space.</param>
        /// <param name="r">The rectangle to transform, in world space.</param>
        /// <returns>A rectangle in the local space of the element.</returns>
        public static Rect WorldToLocal(this VisualElement ele, Rect r)
        {
            if (ele == null)
            {
                throw new ArgumentNullException(nameof(ele));
            }

            return VisualElement.CalculateConservativeRect(ref ele.worldTransformInverse, r);
        }

        /// <summary>
        /// Transforms a rectangle from the local space of the element to the world space.
        /// </summary>
        /// <remarks>
        /// This element needs to be attached to a panel and must receive a valid <see cref="VisualElement.layout"/>.
        /// Otherwise, this method may return invalid results.
        /// </remarks>
        /// <param name="ele">The element to use as a reference for the local space.</param>
        /// <param name="r">The rectangle to transform, in local space.</param>
        /// <returns>A rectangle in the world space.</returns>
        public static Rect LocalToWorld(this VisualElement ele, Rect r)
        {
            if (ele == null)
            {
                throw new ArgumentNullException(nameof(ele));
            }

            return VisualElement.CalculateConservativeRect(ref ele.worldTransformRef, r);
        }

        internal static Ray LocalToWorld([NotNull] this VisualElement ele, Ray r)
        {
            ref var m = ref ele.worldTransformRef;
            return new Ray(m.MultiplyPoint3x4(r.origin), m.MultiplyVector(r.direction));
        }

        internal static Ray WorldToLocal([NotNull] this VisualElement ele, Ray r)
        {
            ref var m = ref ele.worldTransformInverse;
            return new Ray(m.MultiplyPoint3x4(r.origin), m.MultiplyVector(r.direction));
        }

        /// <summary>
        /// Transforms a point from the local space of an element to the local space of another element.
        /// </summary>
        /// <remarks>
        /// The elements both need to be attached to a panel and must receive a valid <see cref="VisualElement.layout"/>.
        /// Otherwise, this method may return invalid results.
        /// </remarks>
        /// <param name="src">The element to use as a reference as the source local space.</param>
        /// <param name="dest">The element to use as a reference as the destination local space.</param>
        /// <param name="point">The point to transform, in the local space of the source element.</param>
        /// <returns>A point in the local space of destination element.</returns>
        public static Vector2 ChangeCoordinatesTo(this VisualElement src, VisualElement dest, Vector2 point)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));
            if (dest == null)
                throw new ArgumentNullException(nameof(dest));

            return src.elementPanel is { isFlat: false }
                ? ChangeCoordinatesTo_3D(src, dest, point)
                : ChangeCoordinatesTo_2D(src, dest, point);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector2 ChangeCoordinatesTo_2D([NotNull] this VisualElement src, [NotNull] VisualElement dest, Vector2 point)
        {
            Vector2 worldPoint = VisualElement.MultiplyMatrix44Point2(ref src.worldTransformRef, point);
            return VisualElement.MultiplyMatrix44Point2(ref dest.worldTransformInverse, worldPoint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector2 ChangeCoordinatesTo_3D([NotNull] this VisualElement src, [NotNull] VisualElement dest, Vector2 point)
        {
            // Even though we transform a 2-D point into another 2-D point, if the visual tree contains some 3-D
            // transformations, we need to preserve the intermediary world-point as a proper 3-D point since
            // the z-coordinate value of the world-point can contribute to (x, y) offsets on the final result.
            Vector3 worldPoint = VisualElement.MultiplyMatrix44Point2ToPoint3(ref src.worldTransformRef, point);
            return VisualElement.MultiplyMatrix44Point3ToPoint2(ref dest.worldTransformInverse, worldPoint);
        }

        /// <summary>
        /// Transforms a rectangle from the local space of an element to the local space of another element.
        /// </summary>
        /// <remarks>
        /// The elements both need to be attached to a panel and have received a valid <see cref="VisualElement.layout"/>.
        /// Otherwise, this method may return invalid results.
        /// </remarks>
        /// <param name="src">The element to use as a reference as the source local space.</param>
        /// <param name="dest">The element to use as a reference as the destination local space.</param>
        /// <param name="rect">The rectangle to transform, in the local space of the source element.</param>
        /// <returns>A rectangle in the local space of destination element.</returns>
        public static Rect ChangeCoordinatesTo(this VisualElement src, VisualElement dest, Rect rect)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));
            if (dest == null)
                throw new ArgumentNullException(nameof(dest));

            // Use the CalculateConservativeRect algo but with ChangeCoordinatesTo.

            if (float.IsNaN(rect.height) || float.IsNaN(rect.width) || float.IsNaN(rect.x) || float.IsNaN(rect.y))
            {
                return new Rect(float.NaN, float.NaN, float.NaN, float.NaN);
            }

            var topLeft = new Vector2(rect.xMin, rect.yMin);
            var bottomRight = new Vector2(rect.xMax, rect.yMax);
            var topRight = new Vector2(rect.xMax, rect.yMin);
            var bottomLeft = new Vector2(rect.xMin, rect.yMax);

            Vector2 transformedTL, transformedTR, transformedBL, transformedBR;

            // Even though we transform a 2-D rect into another 2-D rect, if the visual tree contains some 3-D
            // transformations, we need to preserve the intermediary world-space points since their z-coordinate
            // can contribute to (x, y) offsets on the final result.
            if (src.elementPanel is { isFlat: false })
            {
                transformedTL = ChangeCoordinatesTo_3D(src, dest, topLeft);
                transformedTR = ChangeCoordinatesTo_3D(src, dest, topRight);
                transformedBL = ChangeCoordinatesTo_3D(src, dest, bottomLeft);
                transformedBR = ChangeCoordinatesTo_3D(src, dest, bottomRight);
            }
            else
            {
                transformedTL = ChangeCoordinatesTo_2D(src, dest, topLeft);
                transformedTR = ChangeCoordinatesTo_2D(src, dest, topRight);
                transformedBL = ChangeCoordinatesTo_2D(src, dest, bottomLeft);
                transformedBR = ChangeCoordinatesTo_2D(src, dest, bottomRight);
            }

            Vector2 min = new Vector2(
                VisualElement.Min(transformedTL.x, transformedTR.x, transformedBL.x, transformedBR.x),
                VisualElement.Min(transformedTL.y, transformedTR.y, transformedBL.y, transformedBR.y));

            Vector2 max = new Vector2(
                VisualElement.Max(transformedTL.x, transformedTR.x, transformedBL.x, transformedBR.x),
                VisualElement.Max(transformedTL.y, transformedTR.y, transformedBL.y, transformedBR.y));

            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        internal static Ray ChangeCoordinatesTo([NotNull] this VisualElement src, [NotNull] VisualElement dest, Ray ray)
        {
            return dest.WorldToLocal(src.LocalToWorld(ray));
        }

        internal static bool IntersectWorldRay([NotNull] this VisualElement ve, Ray worldRay, out float distance, out Vector3 localPoint)
        {
            ref var wti = ref ve.worldTransformInverse;
            var localRay = new Ray(wti.MultiplyPoint3x4(worldRay.origin), wti.MultiplyVector(worldRay.direction));
            var intersects = IntersectLocalRay(ve, localRay, out localPoint);
            var worldPoint = ve.worldTransformRef.MultiplyPoint3x4(localPoint);
            distance = Vector3.Distance(worldRay.origin, worldPoint);
            return intersects;
        }

        internal static bool IntersectLocalRay([NotNull] this VisualElement ve, Ray localRay, out Vector3 localPoint)
        {
            // Intersect local ray with the Z=0 plane.
            var distance = -localRay.origin.z / localRay.direction.z;
            localPoint = localRay.origin + localRay.direction * distance;

            // If the ray is going away from the Z=0 plane (or is NaN), we need to reject the point.
            if (!(distance > 0))
                return false;

            // Otherwise, accept the ray if the projected point falls within the element rect.
            // Note that we accept the ray even if it hits the back side of the element, since the element is rendered
            // from behind too. That result may be rejected later but the raycast process should at least be stopped.
            return ve.rect.Contains(localPoint);
        }

        internal static Ray TransformRay(this Matrix4x4 m, Ray ray)
        {
            return new Ray(m.MultiplyPoint3x4(ray.origin), m.MultiplyVector(ray.direction));
        }
    }

    static class MathUtils
    {
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        internal static Matrix4x4 PreApply2DOffset(ref Matrix4x4 m, Vector2 p)
        {
            // This is a fast-path for Offset * Matrix
            // [1 0 0 px]   [m00 m01 m02 m03]   [m00 m01 m02 m03+px]
            // [0 1 0 py] * [m10 m11 m12 m13] = [m10 m11 m12 m13+py]
            // [0 0 1 0 ]   [m20 m21 m22 m23]   [m20 m21 m22 m23   ]
            // [0 0 0 1 ]   [0   0   0   1  ]   [0   0   0   1     ]

            m.m03 += p.x;
            m.m13 += p.y;
            return m;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        internal static Matrix4x4 PostApply2DOffset(ref Matrix4x4 m, Vector2 p)
        {
            // This is a fast-path for Matrix * Offset
            // [m00 m01 m02 m03]   [1 0 0 px]   [m00 m01 m02 m00*px+m01*py+m03]
            // [m10 m11 m12 m13] * [0 1 0 py] = [m10 m11 m12 m10*px+m11*py+m13]
            // [m20 m21 m22 m23]   [0 0 1 0 ]   [m20 m21 m22 m20*px+m21*py+m23]
            // [0   0   0   1  ]   [0 0 0 1 ]   [0   0   0   1                ]

            m.m03 += m.m00 * p.x + m.m01 * p.y;
            m.m13 += m.m10 * p.x + m.m11 * p.y;
            m.m23 += m.m20 * p.x + m.m21 * p.y;
            return m;
        }
    }
}
