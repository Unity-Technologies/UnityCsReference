using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.UIElements
{
    public partial class VisualElement
    {
        Vector3 positionWithLayout
        {
            get { return m_Position + (Vector3)layout.min; }
        }

        Matrix4x4 matrixWithLayout
        {
            get { return Matrix4x4.TRS(positionWithLayout, m_Rotation, m_Scale); }
        }

        void TransformAlignedRect(ref Rect r)
        {
            Vector2 pos = r.position;
            Vector2 size = r.size;

            pos.x *= m_Scale.x;
            pos.y *= m_Scale.y;
            size.x *= m_Scale.x;
            size.y *= m_Scale.y;

            m_Rotation.Multiply2D(ref pos);
            m_Rotation.Multiply2D(ref size);

            pos.x += m_Position.x;
            pos.y += m_Position.y;

            r = new Rect(pos, size);
            OrderMinMaxRect(ref r);
        }

        internal static void TransformAlignedRect(ref Matrix4x4 matrix, ref Rect rect)
        {
            // We assume that the transform performs translation/scaling without rotation.
            rect = new Rect(
                MultiplyMatrix44Point2(ref matrix, rect.position),
                MultiplyVector2(ref matrix, rect.size));
            OrderMinMaxRect(ref rect);
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

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        internal static Vector2 MultiplyMatrix44Point2(ref Matrix4x4 lhs, Vector2 point)
        {
            Vector2 res;
            res.x = lhs.m00 * point.x + lhs.m01 * point.y + lhs.m03;
            res.y = lhs.m10 * point.x + lhs.m11 * point.y + lhs.m13;
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
    }

    public static partial class VisualElementExtensions
    {
        // transforms a point assumed in Panel space to the referential inside of the element bound (local)
        public static Vector2 WorldToLocal(this VisualElement ele, Vector2 p)
        {
            if (ele == null)
            {
                throw new ArgumentNullException(nameof(ele));
            }

            return VisualElement.MultiplyMatrix44Point2(ref ele.worldTransformInverse, p);
        }

        // transforms a point to Panel space referential
        public static Vector2 LocalToWorld(this VisualElement ele, Vector2 p)
        {
            if (ele == null)
            {
                throw new ArgumentNullException(nameof(ele));
            }

            return VisualElement.MultiplyMatrix44Point2(ref ele.worldTransformRef, p);
        }

        // transforms a rect assumed in Panel space to the referential inside of the element bound (local)
        public static Rect WorldToLocal(this VisualElement ele, Rect r)
        {
            if (ele == null)
            {
                throw new ArgumentNullException(nameof(ele));
            }

            return VisualElement.MultiplyMatrix44Rect2(ref ele.worldTransformInverse, r);
        }

        // transforms a rect to Panel space referential
        public static Rect LocalToWorld(this VisualElement ele, Rect r)
        {
            if (ele == null)
            {
                throw new ArgumentNullException(nameof(ele));
            }

            return VisualElement.MultiplyMatrix44Rect2(ref ele.worldTransformRef, r);
        }

        // transform point from the local space of one element to to the local space of another
        public static Vector2 ChangeCoordinatesTo(this VisualElement src, VisualElement dest, Vector2 point)
        {
            return dest.WorldToLocal(src.LocalToWorld(point));
        }

        // transform rect from the local space of one element to to the local space of another
        public static Rect ChangeCoordinatesTo(this VisualElement src, VisualElement dest, Rect rect)
        {
            return dest.WorldToLocal(src.LocalToWorld(rect));
        }
    }
}
