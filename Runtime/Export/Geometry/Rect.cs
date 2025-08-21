// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using scm = System.ComponentModel;

namespace UnityEngine
{
    [NativeHeader("Runtime/Math/Rect.h")]
    [NativeClass("Rectf", "template<typename T> class RectT; typedef RectT<float> Rectf;")]
    [RequiredByNativeCode(Optional = true, GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct Rect : IEquatable<Rect>, IFormattable
    {
        [NativeName("x")]
        private float m_XMin;
        [NativeName("y")]
        private float m_YMin;
        [NativeName("width")]
        private float m_Width;
        [NativeName("height")]
        private float m_Height;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public Rect(float x, float y, float width, float height)
        {
            m_XMin = x;
            m_YMin = y;
            m_Width = width;
            m_Height = height;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public Rect(in Vector2 position, in Vector2 size)
        {
            m_XMin = position.x;
            m_YMin = position.y;
            m_Width = size.x;
            m_Height = size.y;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public Rect(in Rect source)
        {
            m_XMin = source.m_XMin;
            m_YMin = source.m_YMin;
            m_Width = source.m_Width;
            m_Height = source.m_Height;
        }

        private static readonly Rect kZero = new Rect(0.0f, 0.0f, 0.0f, 0.0f);

        static public Rect zero
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => kZero;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        static public Rect MinMaxRect(float xmin, float ymin, float xmax, float ymax) => new Rect(xmin, ymin, xmax - xmin, ymax - ymin);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Set(float x, float y, float width, float height)
        {
            m_XMin = x;
            m_YMin = y;
            m_Width = width;
            m_Height = height;
        }

        public float x
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_XMin;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_XMin = value;
        }

        public float y
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_YMin;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_YMin = value;
        }

        public Vector2 position
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => new Vector2(m_XMin, m_YMin);
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_XMin = value.x; m_YMin = value.y; }
        }

        public Vector2 center
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => new Vector2(x + m_Width / 2f, y + m_Height / 2f);
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_XMin = value.x - m_Width / 2f; m_YMin = value.y - m_Height / 2f; }
        }

        public Vector2 min
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => new Vector2(xMin, yMin);
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { xMin = value.x; yMin = value.y; }
        }

        public Vector2 max
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => new Vector2(xMax, yMax);
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { xMax = value.x; yMax = value.y; }
        }

        public float width
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_Width;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_Width = value;
        }

        public float height
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_Height;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_Height = value;
        }

        public Vector2 size
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => new Vector2(m_Width, m_Height);
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_Width = value.x; m_Height = value.y; }
        }

        public float xMin
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_XMin;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { float oldxmax = xMax; m_XMin = value; m_Width = oldxmax - m_XMin; }
        }
        public float yMin
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_YMin;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { float oldymax = yMax; m_YMin = value; m_Height = oldymax - m_YMin; }
        }
        public float xMax
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_Width + m_XMin;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_Width = value - m_XMin;
        }
        public float yMax
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_Height + m_YMin;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_Height = value - m_YMin;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Contains(Vector2 point) => Contains(in point);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Contains(in Vector2 point) => (point.x >= xMin) && (point.x < xMax) && (point.y >= yMin) && (point.y < yMax);

        // Returns true if the /x/ and /y/ components of /point/ is a point inside this rectangle.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Contains(Vector3 point) => Contains(in point);

        // Returns true if the /x/ and /y/ components of /point/ is a point inside this rectangle.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Contains(in Vector3 point) => (point.x >= xMin) && (point.x < xMax) && (point.y >= yMin) && (point.y < yMax);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Contains(Vector3 point, bool allowInverse) => Contains(in point, allowInverse);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Contains(in Vector3 point, bool allowInverse)
        {
            if (!allowInverse)
            {
                return Contains(in point);
            }
            bool xAxis = width < 0f && (point.x <= xMin) && (point.x > xMax) ||
                width >= 0f && (point.x >= xMin) && (point.x < xMax);
            bool yAxis = height < 0f && (point.y <= yMin) && (point.y > yMax) ||
                height >= 0f && (point.y >= yMin) && (point.y < yMax);
            return xAxis && yAxis;
        }

        // Swaps min and max if min was greater than max.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        private static Rect OrderMinMax(Rect rect) => OrderMinMax(in rect);

        // Swaps min and max if min was greater than max.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        private static Rect OrderMinMax(in Rect rect)
        {
            Rect orderedRect = rect;
            if (orderedRect.xMin > orderedRect.xMax)
            {
                float temp = orderedRect.xMin;
                orderedRect.xMin = orderedRect.xMax;
                orderedRect.xMax = temp;
            }
            if (orderedRect.yMin > orderedRect.yMax)
            {
                float temp = orderedRect.yMin;
                orderedRect.yMin = orderedRect.yMax;
                orderedRect.yMax = temp;
            }
            return orderedRect;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Overlaps(Rect other) => Overlaps(in other);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Overlaps(in Rect other) => other.xMax > xMin && other.xMin < xMax && other.yMax > yMin && other.yMin < yMax;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Overlaps(Rect other, bool allowInverse) => Overlaps(in other, allowInverse);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Overlaps(in Rect other, bool allowInverse)
        {
            Rect self;
            Rect otherRect;
            if (allowInverse)
            {
                self = OrderMinMax(in this);
                otherRect = OrderMinMax(in other);
            }
            else
            {
                self = this;
                otherRect = other;
            }
            return self.Overlaps(in otherRect);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2 NormalizedToPoint(Rect rectangle, Vector2 normalizedRectCoordinates) => NormalizedToPoint(in rectangle, in normalizedRectCoordinates);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2 NormalizedToPoint(in Rect rectangle, in Vector2 normalizedRectCoordinates) => new Vector2(
                Mathf.Lerp(rectangle.x, rectangle.xMax, normalizedRectCoordinates.x),
                Mathf.Lerp(rectangle.y, rectangle.yMax, normalizedRectCoordinates.y)
            );

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2 PointToNormalized(Rect rectangle, Vector2 point) => PointToNormalized(in rectangle, in point);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2 PointToNormalized(in Rect rectangle, in Vector2 point) => new Vector2(
                Mathf.InverseLerp(rectangle.x, rectangle.xMax, point.x),
                Mathf.InverseLerp(rectangle.y, rectangle.yMax, point.y)
            );

        // Returns true if the rectangles are different.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator!=(in Rect lhs, in Rect rhs) =>
            // Returns true in the presence of NaN values.
            !(lhs == rhs);

        // Returns true if the rectangles are the same.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator==(in Rect lhs, in Rect rhs) =>
            // Returns false in the presence of NaN values.
            lhs.m_XMin == rhs.m_XMin && lhs.m_YMin == rhs.m_YMin && lhs.m_Width == rhs.m_Width && lhs.m_Height == rhs.m_Height;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly int GetHashCode() => m_XMin.GetHashCode() ^ (m_Width.GetHashCode() << 2) ^ (m_YMin.GetHashCode() >> 2) ^ (m_Height.GetHashCode() >> 1);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly bool Equals(object other)
        {
            if (other is Rect r)
                return Equals(r);
            return false;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Equals(Rect other) => x.Equals(other.x) && y.Equals(other.y) && width.Equals(other.width) && height.Equals(other.height);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly string ToString() => ToString(null, null);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly string ToString(string format) => ToString(format, null);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "F2";
            if (formatProvider == null)
                formatProvider = CultureInfo.InvariantCulture.NumberFormat;
            return string.Format("(x:{0}, y:{1}, width:{2}, height:{3})", x.ToString(format, formatProvider), y.ToString(format, formatProvider), width.ToString(format, formatProvider), height.ToString(format, formatProvider));
        }

        [System.Obsolete("use xMin")]
        public readonly float left { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => m_XMin; }
        [System.Obsolete("use xMax")]
        public readonly float right { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => m_XMin + m_Width; }
        [System.Obsolete("use yMin")]
        public readonly float top { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => m_YMin; }
        [System.Obsolete("use yMax")]
        public readonly float bottom { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => m_YMin + m_Height; }
    }
} //namespace
