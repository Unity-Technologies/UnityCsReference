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
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return m_XMin; }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_XMin = value; }
        }

        // Top coordinate of the rectangle.
        public int y
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return m_YMin; }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_YMin = value; }
        }

        // Center coordinate of the rectangle.
        public Vector2 center
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return new Vector2(x + m_Width / 2f, y + m_Height / 2f); }
        }

        // Top left corner of the rectangle.
        public Vector2Int min
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return new Vector2Int(xMin, yMin); }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { xMin = value.x; yMin = value.y; }
        }

        // Bottom right corner of the rectangle.
        public Vector2Int max
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return new Vector2Int(xMax, yMax); }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { xMax = value.x; yMax = value.y; }
        }

        // Width of the rectangle.
        public int width
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return m_Width; }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_Width = value; }
        }

        // Height of the rectangle.
        public int height
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return m_Height; }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_Height = value; }
        }

        public int xMin
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return Math.Min(m_XMin, m_XMin + m_Width); }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { int oldxmax = xMax; m_XMin = value; m_Width = oldxmax - m_XMin; }
        }
        public int yMin
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return Math.Min(m_YMin, m_YMin + m_Height); }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { int oldymax = yMax; m_YMin = value; m_Height = oldymax - m_YMin; }
        }
        public int xMax
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return Math.Max(m_XMin, m_XMin + m_Width); }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_Width = value - m_XMin; }
        }
        public int yMax
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return Math.Max(m_YMin, m_YMin + m_Height); }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_Height = value - m_YMin; }
        }

        public Vector2Int position
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return new Vector2Int(m_XMin, m_YMin); }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_XMin = value.x; m_YMin = value.y; }
        }
        public Vector2Int size
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return new Vector2Int(m_Width, m_Height); }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_Width = value.x; m_Height = value.y; }
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void SetMinMax(Vector2Int minPosition, Vector2Int maxPosition)
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

        // Shorthand for writing new RectInt(0,0,0,0).
        static public RectInt zero => new RectInt(0, 0, 0, 0);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void ClampToBounds(RectInt bounds)
        {
            position = new Vector2Int(
                Math.Max(Math.Min(bounds.xMax, position.x), bounds.xMin),
                Math.Max(Math.Min(bounds.yMax, position.y), bounds.yMin)
            );
            size = new Vector2Int(
                Math.Min(bounds.xMax - position.x, size.x),
                Math.Min(bounds.yMax - position.y, size.y)
            );
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public bool Contains(Vector2Int position)
        {
            return position.x >= xMin
                && position.y >= yMin
                && position.x < xMax
                && position.y < yMax;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public bool Overlaps(RectInt other)
        {
            return other.xMin < xMax
                && other.xMax > xMin
                && other.yMin < yMax
                && other.yMax > yMin;
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

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (formatProvider == null)
                formatProvider = CultureInfo.InvariantCulture.NumberFormat;
            return string.Format("(x:{0}, y:{1}, width:{2}, height:{3})", x.ToString(format, formatProvider), y.ToString(format, formatProvider), width.ToString(format, formatProvider), height.ToString(format, formatProvider));
        }

        // Returns true if the rectangles are different.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator!=(RectInt lhs, RectInt rhs)
        {
            return !(lhs == rhs);
        }

        // Returns true if the rectangles are the same.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator==(RectInt lhs, RectInt rhs)
        {
            return lhs.x == rhs.x && lhs.y == rhs.y && lhs.width == rhs.width && lhs.height == rhs.height;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override int GetHashCode()
        {
            var xHash = x.GetHashCode();
            var yHash = y.GetHashCode();
            var wHash = width.GetHashCode();
            var hHash = height.GetHashCode();
            return xHash ^
                (yHash << 4) ^ (yHash >> 28) ^
                (wHash >> 4) ^ (wHash << 28) ^
                (hHash >> 4) ^ (hHash << 28);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override bool Equals(object other)
        {
            if (!(other is RectInt)) return false;

            return Equals((RectInt)other);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public bool Equals(RectInt other)
        {
            return m_XMin == other.m_XMin &&
                m_YMin == other.m_YMin &&
                m_Width == other.m_Width &&
                m_Height == other.m_Height;
        }

        public PositionEnumerator allPositionsWithin
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get { return new PositionEnumerator(min, max); }
        }

        public struct PositionEnumerator : IEnumerator<Vector2Int>
        {
            private readonly Vector2Int _min, _max;
            private Vector2Int _current;

            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            public PositionEnumerator(Vector2Int min, Vector2Int max)
            {
                _min = _current = min;
                _max = max;
                Reset();
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

            public Vector2Int Current { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return _current; } }

            object IEnumerator.Current { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return Current; } }

            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            void IDisposable.Dispose() {}
        }
    }
} //namespace
