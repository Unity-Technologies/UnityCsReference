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
    public struct BoundsInt
    {
        private Vector3Int m_Position;
        private Vector3Int m_Size;

        public int x { get { return m_Position.x; } set { m_Position.x = value; } }
        public int y { get { return m_Position.y; } set { m_Position.y = value; } }
        public int z { get { return m_Position.z; } set { m_Position.z = value; } }

        public Vector3 center { get { return new Vector3(x + m_Size.x / 2f, y + m_Size.y / 2f, z + m_Size.z / 2f); } }
        public Vector3Int min { get { return new Vector3Int(xMin, yMin, zMin); } set { xMin = value.x; yMin = value.y; zMin = value.z; } }
        public Vector3Int max { get { return new Vector3Int(xMax, yMax, zMax); } set { xMax = value.x; yMax = value.y; zMax = value.z; } }

        public int xMin { get { return Math.Min(m_Position.x, m_Position.x + m_Size.x); } set { int oldxmax = xMax; m_Position.x = value; m_Size.x = oldxmax - m_Position.x; } }
        public int yMin { get { return Math.Min(m_Position.y, m_Position.y + m_Size.y); } set { int oldymax = yMax; m_Position.y = value; m_Size.y = oldymax - m_Position.y; } }
        public int zMin { get { return Math.Min(m_Position.z, m_Position.z + m_Size.z); } set { int oldzmax = zMax; m_Position.z = value; m_Size.z = oldzmax - m_Position.z; } }
        public int xMax { get { return Math.Max(m_Position.x, m_Position.x + m_Size.x); } set { m_Size.x = value - m_Position.x; } }
        public int yMax { get { return Math.Max(m_Position.y, m_Position.y + m_Size.y); } set { m_Size.y = value - m_Position.y; } }
        public int zMax { get { return Math.Max(m_Position.z, m_Position.z + m_Size.z); } set { m_Size.z = value - m_Position.z; } }

        public Vector3Int position { get { return m_Position; } set { m_Position = value; } }
        public Vector3Int size { get { return m_Size; } set { m_Size = value; } }

        public BoundsInt(int xMin, int yMin, int zMin, int sizeX, int sizeY, int sizeZ)
        {
            m_Position = new Vector3Int(xMin, yMin, zMin);
            m_Size = new Vector3Int(sizeX, sizeY, sizeZ);
        }

        public BoundsInt(Vector3Int position, Vector3Int size)
        {
            m_Position = position;
            m_Size = size;
        }

        public void SetMinMax(Vector3Int minPosition, Vector3Int maxPosition)
        {
            min = minPosition;
            max = maxPosition;
        }

        public void ClampToBounds(BoundsInt bounds)
        {
            position = new Vector3Int(
                    Math.Max(Math.Min(bounds.xMax, position.x), bounds.xMin),
                    Math.Max(Math.Min(bounds.yMax, position.y), bounds.yMin),
                    Math.Max(Math.Min(bounds.zMax, position.z), bounds.zMin)
                    );
            size = new Vector3Int(
                    Math.Min(bounds.xMax - position.x, size.x),
                    Math.Min(bounds.yMax - position.y, size.y),
                    Math.Min(bounds.zMax - position.z, size.z)
                    );
        }

        public bool Contains(Vector3Int position)
        {
            return position.x >= m_Position.x
                && position.y >= m_Position.y
                && position.z >= m_Position.z
                && position.x < m_Position.x + m_Size.x
                && position.y < m_Position.y + m_Size.y
                && position.z < m_Position.z + m_Size.z;
        }

        public override string ToString()
        {
            return UnityString.Format("Position: {0}, Size: {1}", m_Position, m_Size);
        }

        public static bool operator==(BoundsInt lhs, BoundsInt rhs)
        {
            return lhs.m_Position == rhs.m_Position && lhs.m_Size == rhs.m_Size;
        }

        public static bool operator!=(BoundsInt lhs, BoundsInt rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object other)
        {
            if (!(other is BoundsInt)) return false;

            BoundsInt rhs = (BoundsInt)other;
            return m_Position.Equals(rhs.m_Position) && m_Size.Equals(rhs.m_Size);
        }

        public override int GetHashCode()
        {
            return m_Position.GetHashCode() ^ (m_Size.GetHashCode() << 2);
        }

        public PositionEnumerator allPositionsWithin
        {
            get { return new PositionEnumerator(min, max); }
        }

        public struct PositionEnumerator : IEnumerator<Vector3Int>
        {
            private readonly Vector3Int _min, _max;
            private Vector3Int _current;

            public PositionEnumerator(Vector3Int min, Vector3Int max)
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
                if (_current.z >= _max.z)
                    return false;

                _current.x++;
                if (_current.x >= _max.x)
                {
                    _current.x = _min.x;
                    _current.y++;
                    if (_current.y >= _max.y)
                    {
                        _current.y = _min.y;
                        _current.z++;
                        if (_current.z >= _max.z)
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

            public Vector3Int Current { get { return _current; } }

            object IEnumerator.Current { get { return Current; } }

            void IDisposable.Dispose() {}
        }
    }
} //namespace
