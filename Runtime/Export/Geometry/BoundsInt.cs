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
    [Serializable]
    public struct BoundsInt : IEquatable<BoundsInt>, IFormattable
    {
        [SerializeField]
        private Vector3Int m_Position;
        [SerializeField]
        private Vector3Int m_Size;

        public int x
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_Position.x;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_Position.x = value;
        }
        public int y
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_Position.y;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_Position.y = value;
        }
        public int z
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_Position.z;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_Position.z = value;
        }

        public readonly Vector3 center
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => new Vector3() { x = m_Position.x + m_Size.x * 0.5f, y = m_Position.y + m_Size.y * 0.5f, z = m_Position.z + m_Size.z * 0.5f };
        }
        public Vector3Int min
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => new Vector3Int(xMin, yMin, zMin);
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { xMin = value.x; yMin = value.y; zMin = value.z; }
        }
        public Vector3Int max
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => new Vector3Int(xMax, yMax, zMax);
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { xMax = value.x; yMax = value.y; zMax = value.z; }
        }

        public int xMin
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => Math.Min(m_Position.x, m_Position.x + m_Size.x);
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { int oldxmax = xMax; m_Position.x = value; m_Size.x = oldxmax - m_Position.x; }
        }
        public int yMin
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => Math.Min(m_Position.y, m_Position.y + m_Size.y);
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { int oldymax = yMax; m_Position.y = value; m_Size.y = oldymax - m_Position.y; }
        }
        public int zMin
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => Math.Min(m_Position.z, m_Position.z + m_Size.z);
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { int oldzmax = zMax; m_Position.z = value; m_Size.z = oldzmax - m_Position.z; }
        }
        public int xMax
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => Math.Max(m_Position.x, m_Position.x + m_Size.x);
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_Size.x = value - m_Position.x;
        }
        public int yMax
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => Math.Max(m_Position.y, m_Position.y + m_Size.y);
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_Size.y = value - m_Position.y;
        }
        public int zMax
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => Math.Max(m_Position.z, m_Position.z + m_Size.z);
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_Size.z = value - m_Position.z;
        }

        public Vector3Int position
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_Position;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_Position = value;
        }
        public Vector3Int size
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_Size;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_Size = value;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public BoundsInt(int xMin, int yMin, int zMin, int sizeX, int sizeY, int sizeZ)
        {
            m_Position = new Vector3Int(xMin, yMin, zMin);
            m_Size = new Vector3Int(sizeX, sizeY, sizeZ);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public BoundsInt(Vector3Int position, Vector3Int size)
        {
            m_Position = position;
            m_Size = size;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public BoundsInt(in Vector3Int position, in Vector3Int size)
        {
            m_Position = position;
            m_Size = size;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void SetMinMax(Vector3Int minPosition, Vector3Int maxPosition)
        {
            xMin = minPosition.x;
            yMin = minPosition.y;
            zMin = minPosition.z;

            xMax = maxPosition.x;
            yMax = maxPosition.y;
            zMax = maxPosition.z;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void SetMinMax(in Vector3Int minPosition, in Vector3Int maxPosition)
        {
            xMin = minPosition.x;
            yMin = minPosition.y;
            zMin = minPosition.z;

            xMax = maxPosition.x;
            yMax = maxPosition.y;
            zMax = maxPosition.z;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void ClampToBounds(BoundsInt bounds)
        {
            m_Position.x = Math.Max(Math.Min(bounds.xMax, m_Position.x), bounds.xMin);
            m_Position.y = Math.Max(Math.Min(bounds.yMax, m_Position.y), bounds.yMin);
            m_Position.z = Math.Max(Math.Min(bounds.zMax, m_Position.z), bounds.zMin);

            m_Size.x = Math.Min(bounds.xMax - m_Position.x, m_Size.x);
            m_Size.y = Math.Min(bounds.yMax - m_Position.y, m_Size.y);
            m_Size.z = Math.Min(bounds.zMax - m_Position.z, m_Size.z);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void ClampToBounds(in BoundsInt bounds)
        {
            m_Position.x = Math.Max(Math.Min(bounds.xMax, m_Position.x), bounds.xMin);
            m_Position.y = Math.Max(Math.Min(bounds.yMax, m_Position.y), bounds.yMin);
            m_Position.z = Math.Max(Math.Min(bounds.zMax, m_Position.z), bounds.zMin);

            m_Size.x = Math.Min(bounds.xMax - m_Position.x, m_Size.x);
            m_Size.y = Math.Min(bounds.yMax - m_Position.y, m_Size.y);
            m_Size.z = Math.Min(bounds.zMax - m_Position.z, m_Size.z);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public bool Contains(Vector3Int position) => position.x >= xMin
                && position.y >= yMin
                && position.z >= zMin
                && position.x < xMax
                && position.y < yMax
                && position.z < zMax;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public bool Contains(in Vector3Int position) => position.x >= xMin
                && position.y >= yMin
                && position.z >= zMin
                && position.x < xMax
                && position.y < yMax
                && position.z < zMax;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly string ToString() => ToString(null, null);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly string ToString(string format) => ToString(format, null);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly string ToString(string format, IFormatProvider formatProvider)
        {
            if (formatProvider == null)
                formatProvider = CultureInfo.InvariantCulture.NumberFormat;
            return string.Format("Position: {0}, Size: {1}", m_Position.ToString(format, formatProvider), m_Size.ToString(format, formatProvider));
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator==(BoundsInt lhs, BoundsInt rhs) => lhs.m_Position == rhs.m_Position && lhs.m_Size == rhs.m_Size;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator!=(BoundsInt lhs, BoundsInt rhs) => !(lhs == rhs);


        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly bool Equals(object other)
        {
            if (other is BoundsInt bounds)
                return Equals(in bounds);

            return false;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Equals(BoundsInt other) => m_Position.Equals(in other.m_Position) && m_Size.Equals(in other.m_Size);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Equals(in BoundsInt other) => m_Position.Equals(in other.m_Position) && m_Size.Equals(in other.m_Size);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly int GetHashCode() => m_Position.GetHashCode() ^ (m_Size.GetHashCode() << 2);

        public readonly PositionEnumerator allPositionsWithin
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => new PositionEnumerator(min, max);
        }

        public struct PositionEnumerator : IEnumerator<Vector3Int>
        {
            private readonly Vector3Int _min, _max;
            private Vector3Int _current;

            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            public PositionEnumerator(in Vector3Int min, in Vector3Int max)
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
                if (_current.z >= _max.z || _current.y >= _max.y)
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
                        _current.y = _min.y;

                        _current.z++;
                        if (_current.z >= _max.z)
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

            public readonly Vector3Int Current { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => _current; }

            readonly object IEnumerator.Current { [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get => Current; }

            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            void IDisposable.Dispose() {}
        }
    }
} //namespace
