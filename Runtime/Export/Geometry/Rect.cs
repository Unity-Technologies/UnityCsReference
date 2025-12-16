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
    [Serializable]
    public partial struct Rect : IEquatable<Rect>, IFormattable
    {
        [SerializeField]
        [NativeName("x")]
        private float m_XMin;
        [SerializeField]
        [NativeName("y")]
        private float m_YMin;
        [SerializeField]
        [NativeName("width")]
        private float m_Width;
        [SerializeField]
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
        public Rect(in Vector2 position, in Vector2 size)
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
        static public Rect MinMaxRect(float xmin, float ymin, float xmax, float ymax) => new Rect() { m_XMin = xmin, m_YMin = ymin, m_Width = xmax - xmin, m_Height = ymax - ymin };

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
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => new Vector2() { x = m_XMin, y = m_YMin };
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_XMin = value.x; m_YMin = value.y; }
        }

        public Vector2 center
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => new Vector2() { x = m_XMin + m_Width * 0.5f, y = m_YMin + m_Height * 0.5f };
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_XMin = value.x - m_Width * 0.5f; m_YMin = value.y - m_Height * 0.5f; }
        }

        public Vector2 min
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => new Vector2() { x = m_XMin, y = m_YMin };
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { xMin = value.x; yMin = value.y; }
        }

        public Vector2 max
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => new Vector2() { x = xMax, y = yMax };
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
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => new Vector2() { x = m_Width, y = m_Height };
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
        public readonly bool Contains(Vector2 point) => (point.x >= m_XMin) && (point.x < xMax) && (point.y >= m_YMin) && (point.y < yMax);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Contains(in Vector2 point) => (point.x >= m_XMin) && (point.x < xMax) && (point.y >= m_YMin) && (point.y < yMax);

        // Returns true if the /x/ and /y/ components of /point/ is a point inside this rectangle.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Contains(Vector3 point) => (point.x >= m_XMin) && (point.x < xMax) && (point.y >= m_YMin) && (point.y < yMax);

        // Returns true if the /x/ and /y/ components of /point/ is a point inside this rectangle.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Contains(in Vector3 point) => (point.x >= m_XMin) && (point.x < xMax) && (point.y >= m_YMin) && (point.y < yMax);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Contains(Vector3 point, bool allowInverse)
        {
            if (!allowInverse)
            {
                return Contains(in point);
            }

            float xmax = xMax;
            float ymax = yMax;

            bool xAxis = m_Width < 0f && (point.x <= m_XMin) && (point.x > xmax) ||
                m_Width >= 0f && (point.x >= m_XMin) && (point.x < xmax);
            bool yAxis = m_Height < 0f && (point.y <= m_YMin) && (point.y > ymax) ||
                m_Height >= 0f && (point.y >= m_YMin) && (point.y < ymax);
            return xAxis && yAxis;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Contains(in Vector3 point, bool allowInverse)
        {
            if (!allowInverse)
            {
                return Contains(in point);
            }

            float xmax = xMax;
            float ymax = yMax;

            bool xAxis = m_Width < 0f && (point.x <= m_XMin) && (point.x > xmax) ||
                m_Width >= 0f && (point.x >= m_XMin) && (point.x < xmax);
            bool yAxis = m_Height < 0f && (point.y <= m_YMin) && (point.y > ymax) ||
                m_Height >= 0f && (point.y >= m_YMin) && (point.y < ymax);
            return xAxis && yAxis;
        }

        // Swaps min and max if min was greater than max.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        private static Rect OrderMinMax(Rect rect)
        {
            float xmax = rect.xMax;
            float ymax = rect.yMax;

            return MinMaxRect(
                Mathf.Min(rect.m_XMin, xmax),
                Mathf.Min(rect.m_YMin, ymax),
                Mathf.Max(rect.m_XMin, xmax),
                Mathf.Max(rect.m_YMin, ymax)
            );
        }

        // Swaps min and max if min was greater than max.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        private static Rect OrderMinMax(in Rect rect)
        {
            float xmax = rect.xMax;
            float ymax = rect.yMax;

            return MinMaxRect(
                Mathf.Min(rect.m_XMin, xmax),
                Mathf.Min(rect.m_YMin, ymax),
                Mathf.Max(rect.m_XMin, xmax),
                Mathf.Max(rect.m_YMin, ymax)
            );
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Overlaps(Rect other) => other.xMax > m_XMin && other.m_XMin < xMax && other.yMax > m_YMin && other.m_YMin < yMax;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Overlaps(in Rect other) => other.xMax > m_XMin && other.m_XMin < xMax && other.yMax > m_YMin && other.m_YMin < yMax;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Overlaps(Rect other, bool allowInverse)
        {
            if (allowInverse)
            {
                other = OrderMinMax(in other);
                return OrderMinMax(in this).Overlaps(in other);
            }
            else
            {
                return Overlaps(in other);
            }
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Overlaps(in Rect other, bool allowInverse)
        {
            if (allowInverse)
            {
                Rect otherRect = OrderMinMax(in other);
                return OrderMinMax(in this).Overlaps(in otherRect);
            }
            else
            {
                return Overlaps(in other);
            }
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2 NormalizedToPoint(Rect rectangle, Vector2 normalizedRectCoordinates) => new Vector2() {
                x = Mathf.Lerp(rectangle.m_XMin, rectangle.xMax, normalizedRectCoordinates.x),
                y = Mathf.Lerp(rectangle.m_YMin, rectangle.yMax, normalizedRectCoordinates.y)
            };

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2 NormalizedToPoint(in Rect rectangle, in Vector2 normalizedRectCoordinates) => new Vector2() {
                x = Mathf.Lerp(rectangle.m_XMin, rectangle.xMax, normalizedRectCoordinates.x),
                y = Mathf.Lerp(rectangle.m_YMin, rectangle.yMax, normalizedRectCoordinates.y)
            };

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2 PointToNormalized(Rect rectangle, Vector2 point) => new Vector2() {
                x = Mathf.InverseLerp(rectangle.m_XMin, rectangle.xMax, point.x),
                y = Mathf.InverseLerp(rectangle.m_YMin, rectangle.yMax, point.y)
            };

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Vector2 PointToNormalized(in Rect rectangle, in Vector2 point) => new Vector2() {
                x = Mathf.InverseLerp(rectangle.m_XMin, rectangle.xMax, point.x),
                y = Mathf.InverseLerp(rectangle.m_YMin, rectangle.yMax, point.y)
            };

        // Returns true if the rectangles are different.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator!=(Rect lhs, Rect rhs) =>
            // Returns true in the presence of NaN values.
            !(lhs == rhs);

        // Returns true if the rectangles are the same.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator==(Rect lhs, Rect rhs) =>
            // Returns false in the presence of NaN values.
            lhs.m_XMin == rhs.m_XMin && lhs.m_YMin == rhs.m_YMin && lhs.m_Width == rhs.m_Width && lhs.m_Height == rhs.m_Height;


        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly int GetHashCode() => m_XMin.GetHashCode() ^ (m_Width.GetHashCode() << 2) ^ (m_YMin.GetHashCode() >> 2) ^ (m_Height.GetHashCode() >> 1);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly bool Equals(object other)
        {
            if (other is Rect r)
                return Equals(in r);
            return false;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Equals(Rect other) => m_XMin.Equals(other.m_XMin) && m_YMin.Equals(other.m_YMin) && m_Width.Equals(other.m_Width) && m_Height.Equals(other.m_Height);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Equals(in Rect other) => m_XMin.Equals(other.m_XMin) && m_YMin.Equals(other.m_YMin) && m_Width.Equals(other.m_Width) && m_Height.Equals(other.m_Height);

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
            return string.Format("(x:{0}, y:{1}, width:{2}, height:{3})", m_XMin.ToString(format, formatProvider), m_YMin.ToString(format, formatProvider), m_Width.ToString(format, formatProvider), m_Height.ToString(format, formatProvider));
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
