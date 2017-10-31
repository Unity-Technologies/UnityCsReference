// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct RectInt
    {
        private int m_XMin, m_YMin, m_Width, m_Height;

        // Left coordinate of the rectangle.
        public int x { get { return m_XMin; } set { m_XMin = value; } }

        // Top coordinate of the rectangle.
        public int y { get { return m_YMin; } set { m_YMin = value; } }

        // Center coordinate of the rectangle.
        public Vector2 center { get { return new Vector2(x + m_Width / 2f, y + m_Height / 2f); } }

        // Top left corner of the rectangle.
        public Vector2Int min { get { return new Vector2Int(xMin, yMin); } set { xMin = value.x; yMin = value.y; } }

        // Bottom right corner of the rectangle.
        public Vector2Int max { get { return new Vector2Int(xMax, yMax); } set { xMax = value.x; yMax = value.y; } }

        // Width of the rectangle.
        public int width { get { return m_Width; } set { m_Width = value; } }

        // Height of the rectangle.
        public int height { get { return m_Height; } set { m_Height = value; } }

        public int xMin { get { return Math.Min(m_XMin, m_XMin + m_Width); } set { int oldxmax = xMax; m_XMin = value; m_Width = oldxmax - m_XMin; } }
        public int yMin { get { return Math.Min(m_YMin, m_YMin + m_Height); } set { int oldymax = yMax; m_YMin = value; m_Height = oldymax - m_YMin; } }
        public int xMax { get { return Math.Max(m_XMin, m_XMin + m_Width); } set { m_Width = value - m_XMin; } }
        public int yMax { get { return Math.Max(m_YMin, m_YMin + m_Height); } set { m_Height = value - m_YMin; } }

        public Vector2Int position { get { return new Vector2Int(m_XMin, m_YMin); } set { m_XMin = value.x; m_YMin = value.y; } }
        public Vector2Int size { get { return new Vector2Int(m_Width, m_Height); } set { m_Width = value.x; m_Height = value.y; } }

        public void SetMinMax(Vector2Int minPosition, Vector2Int maxPosition)
        {
            min = minPosition;
            max = maxPosition;
        }

        public RectInt(int xMin, int yMin, int width, int height)
        {
            m_XMin = xMin;
            m_YMin = yMin;
            m_Width = width;
            m_Height = height;
        }

        public RectInt(Vector2Int position, Vector2Int size)
        {
            m_XMin = position.x;
            m_YMin = position.y;
            m_Width = size.x;
            m_Height = size.y;
        }

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

        public bool Contains(Vector2Int position)
        {
            return position.x >= m_XMin
                && position.y >= m_YMin
                && position.x < m_XMin + m_Width
                && position.y < m_YMin + m_Height;
        }

        public override string ToString()
        {
            return UnityString.Format("(x:{0}, y:{1}, width:{2}, height:{3})", x, y, width, height);
        }

        public PositionEnumerator allPositionsWithin
        {
            get { return new PositionEnumerator(min, max); }
        }

        public struct PositionEnumerator : IEnumerator<Vector2Int>
        {
            private readonly Vector2Int _min, _max;
            private Vector2Int _current;

            public PositionEnumerator(Vector2Int min, Vector2Int max)
            {
                _min = _current = min;
                _max = max;
                Reset();
            }

            public PositionEnumerator GetEnumerator()
            {
                return this;
            }

            public bool MoveNext()
            {
                if (_current.y >= _max.y)
                    return false;

                _current.x++;
                if (_current.x >= _max.x)
                {
                    _current.x = _min.x;
                    _current.y++;
                    if (_current.y >= _max.y)
                    {
                        return false;
                    }
                }

                return true;
            }

            public void Reset()
            {
                _current = _min;
                _current.x--;
            }

            public Vector2Int Current { get { return _current; } }

            object IEnumerator.Current { get { return Current; } }

            void IDisposable.Dispose() {}
        }
    }
} //namespace
