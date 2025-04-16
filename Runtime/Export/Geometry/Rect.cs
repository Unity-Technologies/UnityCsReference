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
        public Rect(Vector2 position, Vector2 size)
        {
            m_XMin = position.x;
            m_YMin = position.y;
            m_Width = size.x;
            m_Height = size.y;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public Rect(Rect source)
        {
            m_XMin = source.m_XMin;
            m_YMin = source.m_YMin;
            m_Width = source.m_Width;
            m_Height = source.m_Height;
        }

        static public Rect zero => new Rect(0.0f, 0.0f, 0.0f, 0.0f);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        static public Rect MinMaxRect(float xmin, float ymin, float xmax, float ymax)
        {
            return new Rect(xmin, ymin, xmax - xmin, ymax - ymin);
        }

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
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return m_XMin; }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_XMin = value; }
        }

        public float y
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return m_YMin; }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_YMin = value; }
        }

        public Vector2 position
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return new Vector2(m_XMin, m_YMin); }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_XMin = value.x; m_YMin = value.y; }
        }

        public Vector2 center
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return new Vector2(x + m_Width / 2f, y + m_Height / 2f); }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_XMin = value.x - m_Width / 2f; m_YMin = value.y - m_Height / 2f; }
        }

        public Vector2 min
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return new Vector2(xMin, yMin); }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { xMin = value.x; yMin = value.y; }
        }

        public Vector2 max
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return new Vector2(xMax, yMax); }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { xMax = value.x; yMax = value.y; }
        }

        public float width
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return m_Width; }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_Width = value; }
        }

        public float height
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return m_Height; }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_Height = value; }
        }

        public Vector2 size
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return new Vector2(m_Width, m_Height); }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_Width = value.x; m_Height = value.y; }
        }

        public float xMin
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return m_XMin; }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { float oldxmax = xMax; m_XMin = value; m_Width = oldxmax - m_XMin; }
        }
        public float yMin
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return m_YMin; }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { float oldymax = yMax; m_YMin = value; m_Height = oldymax - m_YMin; }
        }
        public float xMax
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return m_Width + m_XMin; }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_Width = value - m_XMin; }
        }
        public float yMax
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return m_Height + m_YMin; }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_Height = value - m_YMin; }
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public bool Contains(Vector2 point)
        {
            return (point.x >= xMin) && (point.x < xMax) && (point.y >= yMin) && (point.y < yMax);
        }

        // Returns true if the /x/ and /y/ components of /point/ is a point inside this rectangle.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public bool Contains(Vector3 point)
        {
            return (point.x >= xMin) && (point.x < xMax) && (point.y >= yMin) && (point.y < yMax);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public bool Contains(Vector3 point, bool allowInverse)
        {
            if (!allowInverse)
            {
                return Contains(point);
            }
            bool xAxis = width < 0f && (point.x <= xMin) && (point.x > xMax) ||
                width >= 0f && (point.x >= xMin) && (point.x < xMax);
            bool yAxis = height < 0f && (point.y <= yMin) && (point.y > yMax) ||
                height >= 0f && (point.y >= yMin) && (point.y < yMax);
            return xAxis && yAxis;
        }

        // Swaps min and max if min was greater than max.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        private static Rect OrderMinMax(Rect rect)
        {
            if (rect.xMin > rect.xMax)
            {
                float temp = rect.xMin;
                rect.xMin = rect.xMax;
                rect.xMax = temp;
            }
            if (rect.yMin > rect.yMax)
            {
                float temp = rect.yMin;
                rect.yMin = rect.yMax;
                rect.yMax = temp;
            }
            return rect;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public bool Overlaps(Rect other)
        {
            return (other.xMax > xMin &&
                other.xMin < xMax &&
                other.yMax > yMin &&
                other.yMin < yMax);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public bool Overlaps(Rect other, bool allowInverse)
        {
            Rect self = this;
            if (allowInverse)
            {
                self = OrderMinMax(self);
                other = OrderMinMax(other);
            }
            return self.Overlaps(other);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2 NormalizedToPoint(Rect rectangle, Vector2 normalizedRectCoordinates)
        {
            return new Vector2(
                Mathf.Lerp(rectangle.x, rectangle.xMax, normalizedRectCoordinates.x),
                Mathf.Lerp(rectangle.y, rectangle.yMax, normalizedRectCoordinates.y)
            );
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2 PointToNormalized(Rect rectangle, Vector2 point)
        {
            return new Vector2(
                Mathf.InverseLerp(rectangle.x, rectangle.xMax, point.x),
                Mathf.InverseLerp(rectangle.y, rectangle.yMax, point.y)
            );
        }

        // Returns true if the rectangles are different.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator!=(Rect lhs, Rect rhs)
        {
            // Returns true in the presence of NaN values.
            return !(lhs == rhs);
        }

        // Returns true if the rectangles are the same.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator==(Rect lhs, Rect rhs)
        {
            // Returns false in the presence of NaN values.
            return lhs.x == rhs.x && lhs.y == rhs.y && lhs.width == rhs.width && lhs.height == rhs.height;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ (width.GetHashCode() << 2) ^ (y.GetHashCode() >> 2) ^ (height.GetHashCode() >> 1);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override bool Equals(object other)
        {
            if (other is Rect r)
                return Equals(r);
            return false;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public bool Equals(Rect other)
        {
            return x.Equals(other.x) && y.Equals(other.y) && width.Equals(other.width) && height.Equals(other.height);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override string ToString()
        {
            return ToString(null, null);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public string ToString(string format)
        {
            return ToString(format, null);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "F2";
            if (formatProvider == null)
                formatProvider = CultureInfo.InvariantCulture.NumberFormat;
            return string.Format("(x:{0}, y:{1}, width:{2}, height:{3})", x.ToString(format, formatProvider), y.ToString(format, formatProvider), width.ToString(format, formatProvider), height.ToString(format, formatProvider));
        }

        [System.Obsolete("use xMin")]
        public float left { get { return m_XMin; } }
        [System.Obsolete("use xMax")]
        public float right { get { return m_XMin + m_Width; } }
        [System.Obsolete("use yMin")]
        public float top { get { return m_YMin; } }
        [System.Obsolete("use yMax")]
        public float bottom { get { return m_YMin + m_Height; } }
    }
} //namespace
