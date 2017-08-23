// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    class RectUtils
    {
        public static bool IntersectsSegment(Rect rect, Vector2 p1, Vector2 p2)
        {
            float minX = Mathf.Min(p1.x, p2.x);
            float maxX = Mathf.Max(p1.x, p2.x);

            if (maxX > rect.xMax)
            {
                maxX = rect.xMax;
            }

            if (minX < rect.xMin)
            {
                minX = rect.xMin;
            }

            if (minX > maxX)
            {
                return false;
            }

            float minY = Mathf.Min(p1.y, p2.y);
            float maxY = Mathf.Max(p1.y, p2.y);

            float dx = p2.x - p1.x;

            if (Mathf.Abs(dx) > float.Epsilon)
            {
                float a = (p2.y - p1.y) / dx;
                float b = p1.y - a * p1.x;
                minY = a * minX + b;
                maxY = a * maxX + b;
            }

            if (minY > maxY)
            {
                float tmp = maxY;
                maxY = minY;
                minY = tmp;
            }

            if (maxY > rect.yMax)
            {
                maxY = rect.yMax;
            }

            if (minY < rect.yMin)
            {
                minY = rect.yMin;
            }

            if (minY > maxY)
            {
                return false;
            }

            return true;
        }

        public static Rect Encompass(Rect a, Rect b)
        {
            return new Rect
            {
                xMin = Math.Min(a.xMin, b.xMin),
                yMin = Math.Min(a.yMin, b.yMin),
                xMax = Math.Max(a.xMax, b.xMax),
                yMax = Math.Max(a.yMax, b.yMax)
            };
        }
    }
}
