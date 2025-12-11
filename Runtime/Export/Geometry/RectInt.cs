// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct RectInt : IEquatable<RectInt>, IFormattable
    {
        private int m_XMin, m_YMin, m_Width, m_Height;

        // Left coordinate of the rectangle.
        public int x
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_XMin;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_XMin = value;
        }

        // Top coordinate of the rectangle.
        public int y
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_YMin;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_YMin = value;
        }

        // Center coordinate of the rectangle.
        public readonly Vector2 center
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new Vector2() { x = m_XMin + m_Width * 0.5f, y = m_YMin + m_Height * 0.5f };
        }

        // Top left corner of the rectangle.
        public Vector2Int min
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => new Vector2Int(xMin, yMin);
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { xMin = value.x; yMin = value.y; }
        }

        // Bottom right corner of the rectangle.
        public Vector2Int max
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => new Vector2Int(xMax, yMax);
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { xMax = value.x; yMax = value.y; }
        }

        // Width of the rectangle.
        public int width
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_Width;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_Width = value;
        }

        // Height of the rectangle.
        public int height
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_Height;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_Height = value;
        }

        public int xMin
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => Mathf.Min(m_XMin, m_XMin + m_Width);
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { int oldxmax = xMax; m_XMin = value; m_Width = oldxmax - m_XMin; }
        }
        public int yMin
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => Mathf.Min(m_YMin, m_YMin + m_Height);
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { int oldymax = yMax; m_YMin = value; m_Height = oldymax - m_YMin; }
        }
        public int xMax
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => Mathf.Max(m_XMin, m_XMin + m_Width);
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_Width = value - m_XMin;
        }
        public int yMax
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => Mathf.Max(m_YMin, m_YMin + m_Height);
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_Height = value - m_YMin;
        }

        public Vector2Int position
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => new Vector2Int(m_XMin, m_YMin);
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_XMin = value.x; m_YMin = value.y; }
        }
        public Vector2Int size
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => new Vector2Int(m_Width, m_Height);
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_Width = value.x; m_Height = value.y; }
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void SetMinMax(Vector2Int minPosition, Vector2Int maxPosition)
        {
            min = minPosition;
            max = maxPosition;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void SetMinMax(in Vector2Int minPosition, in Vector2Int maxPosition)
        {
            min = minPosition;
            max = maxPosition;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public RectInt(int xMin, int yMin, int width, int height)
        {
            m_XMin = xMin;
            m_YMin = yMin;
            m_Width = width;
            m_Height = height;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public RectInt(Vector2Int position, Vector2Int size)
        {
            m_XMin = position.x;
            m_YMin = position.y;
            m_Width = size.x;
            m_Height = size.y;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public RectInt(in Vector2Int position, in Vector2Int size)
        {
            m_XMin = position.x;
            m_YMin = position.y;
            m_Width = size.x;
            m_Height = size.y;
        }

        private static readonly RectInt kZero = new RectInt(0, 0, 0, 0);

        // Shorthand for writing new RectInt(0,0,0,0).
        static public RectInt zero
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => kZero;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void ClampToBounds(RectInt bounds)
        {
            int xmin = bounds.xMin;
            int xmax = bounds.xMax;
            int ymin = bounds.yMin;
            int ymax = bounds.yMax;

            m_XMin =   Math.Max(Math.Min(xmax, m_XMin), xmin);
            m_YMin =   Math.Max(Math.Min(ymax, m_YMin), ymin);
            m_Width =  Math.Min(xmax - m_XMin, m_Width);
            m_Height = Math.Min(ymax - m_YMin, m_Height);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void ClampToBounds(in RectInt bounds)
        {
            int xmin = bounds.xMin;
            int xmax = bounds.xMax;
            int ymin = bounds.yMin;
            int ymax = bounds.yMax;

            m_XMin =   Math.Max(Math.Min(xmax, m_XMin), xmin);
            m_YMin =   Math.Max(Math.Min(ymax, m_YMin), ymin);
            m_Width =  Math.Min(xmax - m_XMin, m_Width);
            m_Height = Math.Min(ymax - m_YMin, m_Height);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Contains(Vector2Int position)
        {
            int px = position.x;
            int py = position.y;
            return px >= xMin
                && py >= yMin
                && px < xMax
                && py < yMax;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Contains(in Vector2Int position)
        {
            int px = position.x;
            int py = position.y;
            return px >= xMin
                && py >= yMin
                && px < xMax
                && py < yMax;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Overlaps(RectInt other) => other.xMin < xMax
                && other.xMax > xMin
                && other.yMin < yMax
                && other.yMax > yMin;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Overlaps(in RectInt other) => other.xMin < xMax
                && other.xMax > xMin
                && other.yMin < yMax
                && other.yMax > yMin;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly string ToString() => ToString(null, null);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly string ToString(string format) => ToString(format, null);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly string ToString(string format, IFormatProvider formatProvider)
        {
            if (formatProvider == null)
                formatProvider = CultureInfo.InvariantCulture.NumberFormat;
            return string.Format("(x:{0}, y:{1}, width:{2}, height:{3})", x.ToString(format, formatProvider), y.ToString(format, formatProvider), width.ToString(format, formatProvider), height.ToString(format, formatProvider));
        }

        // Returns true if the rectangles are different.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator!=(RectInt lhs, RectInt rhs) => !(lhs == rhs);

        // Returns true if the rectangles are the same.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator==(RectInt lhs, RectInt rhs) => lhs.m_XMin == rhs.m_XMin && lhs.m_YMin == rhs.m_YMin && lhs.m_Width == rhs.m_Width && lhs.m_Height == rhs.m_Height;


        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly int GetHashCode()
        {
            var xHash = m_XMin.GetHashCode();
            var yHash = m_YMin.GetHashCode();
            var wHash = m_Width.GetHashCode();
            var hHash = m_Height.GetHashCode();
            return xHash ^
                (yHash << 4) ^ (yHash >> 28) ^
                (wHash >> 4) ^ (wHash << 28) ^
                (hHash >> 4) ^ (hHash << 28);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly bool Equals(object other)
        {
            if (other is RectInt rect)
                return Equals(in rect);

            return false;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Equals(RectInt other) => m_XMin == other.m_XMin &&
                m_YMin == other.m_YMin &&
                m_Width == other.m_Width &&
                m_Height == other.m_Height;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Equals(in RectInt other) => m_XMin == other.m_XMin &&
                m_YMin == other.m_YMin &&
                m_Width == other.m_Width &&
                m_Height == other.m_Height;

        public readonly PositionEnumerator allPositionsWithin
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => new PositionEnumerator(min, max);
        }

        public struct PositionEnumerator : IEnumerator<Vector2Int>
        {
            private readonly Vector2Int _min, _max;
            private Vector2Int _current;

            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            public PositionEnumerator(in Vector2Int min, in Vector2Int max)
            {
                _min = min;
                _max = max;
                _current = _min;
                _current.x--;
            }

            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            public PositionEnumerator GetEnumerator()
            {
                return this;
            }

            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            public bool MoveNext()
            {
                if (_current.y >= _max.y)
                    return false;

                _current.x++;
                if (_current.x >= _max.x)
                {
                    _current.x = _min.x;
                    if (_current.x >= _max.x)
                        return false;

                    _current.y++;
                    if (_current.y >= _max.y)
                    {
                        return false;
                    }
                }

                return true;
            }

            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            public void Reset()
            {
                _current = _min;
                _current.x--;
            }

            public readonly Vector2Int Current { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => _current; }

            readonly object IEnumerator.Current { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => Current; }

            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            void IDisposable.Dispose() {}
        }
    }
} //namespace
