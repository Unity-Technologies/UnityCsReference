// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using scm = System.ComponentModel;

namespace UnityEngine
{
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct Rect
    {
        private float m_XMin, m_YMin, m_Width, m_Height;

        public Rect(float x, float y, float width, float height)
        {
            m_XMin = x;
            m_YMin = y;
            m_Width = width;
            m_Height = height;
        }

        public Rect(Vector2 position, Vector2 size)
        {
            m_XMin = position.x;
            m_YMin = position.y;
            m_Width = size.x;
            m_Height = size.y;
        }

        public Rect(Rect source)
        {
            m_XMin = source.m_XMin;
            m_YMin = source.m_YMin;
            m_Width = source.m_Width;
            m_Height = source.m_Height;
        }

        static public Rect zero { get { return new Rect(0.0f, 0.0f, 0.0f, 0.0f); } }

        static public Rect MinMaxRect(float xmin, float ymin, float xmax, float ymax)
        {
            return new Rect(xmin, ymin, xmax - xmin, ymax - ymin);
        }

        public void Set(float x, float y, float width, float height)
        {
            m_XMin = x;
            m_YMin = y;
            m_Width = width;
            m_Height = height;
        }

        public float x { get { return m_XMin; } set { m_XMin = value; } }

        public float y { get { return m_YMin; } set { m_YMin = value; } }

        public Vector2 position
        {
            get { return new Vector2(m_XMin, m_YMin); }
            set { m_XMin = value.x; m_YMin = value.y; }
        }

        public Vector2 center
        {
            get { return new Vector2(x + m_Width / 2f, y + m_Height / 2f); }
            set { m_XMin = value.x - m_Width / 2f; m_YMin = value.y - m_Height / 2f; }
        }

        public Vector2 min { get { return new Vector2(xMin, yMin); } set { xMin = value.x; yMin = value.y; } }

        public Vector2 max { get { return new Vector2(xMax, yMax); } set { xMax = value.x; yMax = value.y; } }

        public float width { get { return m_Width; } set { m_Width = value; } }

        public float height { get { return m_Height; } set { m_Height = value; } }

        public Vector2 size { get { return new Vector2(m_Width, m_Height); } set { m_Width = value.x; m_Height = value.y; } }

        public float xMin { get { return m_XMin; } set { float oldxmax = xMax; m_XMin = value; m_Width = oldxmax - m_XMin; } }
        public float yMin { get { return m_YMin; } set { float oldymax = yMax; m_YMin = value; m_Height = oldymax - m_YMin; } }
        public float xMax { get { return m_Width + m_XMin; } set { m_Width = value - m_XMin; } }
        public float yMax { get { return m_Height + m_YMin; } set { m_Height = value - m_YMin; } }

        public bool Contains(Vector2 point)
        {
            return (point.x >= xMin) && (point.x < xMax) && (point.y >= yMin) && (point.y < yMax);
        }

        // Returns true if the /x/ and /y/ components of /point/ is a point inside this rectangle.
        public bool Contains(Vector3 point)
        {
            return (point.x >= xMin) && (point.x < xMax) && (point.y >= yMin) && (point.y < yMax);
        }

        public bool Contains(Vector3 point, bool allowInverse)
        {
            if (!allowInverse)
            {
                return Contains(point);
            }
            bool xAxis = false;
            if (width < 0f && (point.x <= xMin) && (point.x > xMax) || width >= 0f && (point.x >= xMin) && (point.x < xMax))
                xAxis = true;
            if (xAxis && (height < 0f && (point.y <= yMin) && (point.y > yMax) || height >= 0f && (point.y >= yMin) && (point.y < yMax)))
                return true;
            return false;
        }

        // Swaps min and max if min was greater than max.
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

        public bool Overlaps(Rect other)
        {
            return (other.xMax > xMin &&
                    other.xMin < xMax &&
                    other.yMax > yMin &&
                    other.yMin < yMax);
        }

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

        public static Vector2 NormalizedToPoint(Rect rectangle, Vector2 normalizedRectCoordinates)
        {
            return new Vector2(
                Mathf.Lerp(rectangle.x, rectangle.xMax, normalizedRectCoordinates.x),
                Mathf.Lerp(rectangle.y, rectangle.yMax, normalizedRectCoordinates.y)
                );
        }

        public static Vector2 PointToNormalized(Rect rectangle, Vector2 point)
        {
            return new Vector2(
                Mathf.InverseLerp(rectangle.x, rectangle.xMax, point.x),
                Mathf.InverseLerp(rectangle.y, rectangle.yMax, point.y)
                );
        }

        // Returns true if the rectangles are different.
        public static bool operator!=(Rect lhs, Rect rhs)
        {
            // Returns true in the presence of NaN values.
            return !(lhs == rhs);
        }

        // Returns true if the rectangles are the same.
        public static bool operator==(Rect lhs, Rect rhs)
        {
            // Returns false in the presence of NaN values.
            return lhs.x == rhs.x && lhs.y == rhs.y && lhs.width == rhs.width && lhs.height == rhs.height;
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ (width.GetHashCode() << 2) ^ (y.GetHashCode() >> 2) ^ (height.GetHashCode() >> 1);
        }

        public override bool Equals(object other)
        {
            if (!(other is Rect)) return false;

            Rect rhs = (Rect)other;
            return x.Equals(rhs.x) && y.Equals(rhs.y) && width.Equals(rhs.width) && height.Equals(rhs.height);
        }

        public override string ToString()
        {
            return UnityString.Format("(x:{0:F2}, y:{1:F2}, width:{2:F2}, height:{3:F2})", x, y, width, height);
        }

        public string ToString(string format)
        {
            return UnityString.Format("(x:{0}, y:{1}, width:{2}, height:{3})", x.ToString(format), y.ToString(format), width.ToString(format), height.ToString(format));
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
