// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    internal class RectUtils
    {
        public static bool Contains(Rect a, Rect b)
        {
            if (a.xMin > b.xMin)
                return false;
            if (a.xMax < b.xMax)
                return false;

            if (a.yMin > b.yMin)
                return false;
            if (a.yMax < b.yMax)
                return false;

            return true;
        }

        public static Rect Encompass(Rect a, Rect b)
        {
            Rect newRect = a;
            newRect.xMin = Math.Min(a.xMin, b.xMin);
            newRect.yMin = Math.Min(a.yMin, b.yMin);
            newRect.xMax = Math.Max(a.xMax, b.xMax);
            newRect.yMax = Math.Max(a.yMax, b.yMax);
            return newRect;
        }

        public static Rect Inflate(Rect a, float factor)
        {
            return Inflate(a, factor, factor);
        }

        public static Rect Inflate(Rect a, float factorX, float factorY)
        {
            float newWidth = a.width * factorX;
            float newHeight = a.height * factorY;

            float offsetWidth = (newWidth - a.width) / 2.0f;
            float offsetHeight = (newHeight - a.height) / 2.0f;

            Rect r = a;
            r.xMin -= offsetWidth;
            r.yMin -= offsetHeight;
            r.xMax += offsetWidth;
            r.yMax += offsetHeight;
            return r;
        }

        public static bool Intersects(Rect r1, Rect r2)
        {
            if (!r1.Overlaps(r2) && !r2.Overlaps(r1))
                return false;
            return true;
        }

        public static bool Intersection(Rect r1, Rect r2, out Rect intersection)
        {
            if (!r1.Overlaps(r2) && !r2.Overlaps(r1))
            {
                intersection = new Rect(0, 0, 0, 0);
                return false;
            }

            float left = Mathf.Max(r1.xMin, r2.xMin);
            float top = Mathf.Max(r1.yMin, r2.yMin);

            float right = Mathf.Min(r1.xMax, r2.xMax);
            float bottom = Mathf.Min(r1.yMax, r2.yMax);

            intersection = new Rect(left, top, right - left, bottom - top);
            return true;
        }

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

            if (Mathf.Abs(dx) > 0.0000001f)
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

        public static Rect OffsetX(Rect r, float offsetX)
        {
            return Offset(r, offsetX, 0.0f);
        }

        public static Rect Offset(Rect r, float offsetX, float offsetY)
        {
            Rect nr = r;
            nr.xMin += offsetX;
            nr.yMin += offsetY;
            return nr;
        }

        public static Rect Offset(Rect a, Rect b)
        {
            Rect nr = a;
            nr.xMin += b.xMin;
            nr.yMin += b.yMin;
            return nr;
        }

        public static Rect Move(Rect r, Vector2 delta)
        {
            Rect nr = r;
            nr.xMin += delta.x;
            nr.yMin += delta.y;
            nr.xMax += delta.x;
            nr.yMax += delta.y;
            return nr;
        }
    }

    internal interface IBounds
    {
        Rect boundingRect { get; }
    }

    internal class QuadTreeNode<T> where T : IBounds
    {
        private Rect m_BoundingRect;
        private static Color m_DebugFillColor = new Color(1.0f, 1.0f, 1.0f, 0.01f);
        private static Color m_DebugWireColor = new Color(1.0f, 0.0f, 0.0f, 0.5f);
        private static Color m_DebugBoxFillColor = new Color(1.0f, 0.0f, 0.0f, 0.01f);
        private const float kSmallestAreaForQuadTreeNode = 10.0f;

        List<T> m_Elements = new List<T>();
        List<QuadTreeNode<T>> m_ChildrenNodes = new List<QuadTreeNode<T>>(4);

        public QuadTreeNode(Rect r)
        {
            m_BoundingRect = r;
        }

        public bool IsEmpty { get { return (m_BoundingRect.width == 0 && m_BoundingRect.height == 0) || m_ChildrenNodes.Count == 0; } }
        public Rect BoundingRect { get { return m_BoundingRect; } }

        public int CountItemsIncludingChildren()
        {
            return Count(true);
        }

        public int CountLocalItems()
        {
            return Count(false);
        }

        private int Count(bool recursive)
        {
            int count = m_Elements.Count;

            if (recursive)
            {
                foreach (QuadTreeNode<T> node in m_ChildrenNodes)
                    count += node.Count(recursive);
            }
            return count;
        }

        public List<T> GetElementsIncludingChildren()
        {
            return Elements(true);
        }

        public List<T> GetElements()
        {
            return Elements(false);
        }

        private List<T> Elements(bool recursive)
        {
            List<T> results = new List<T>();

            if (recursive)
            {
                foreach (QuadTreeNode<T> node in m_ChildrenNodes)
                    results.AddRange(node.Elements(recursive));
            }

            results.AddRange(m_Elements);
            return results;
        }

        public List<T> IntersectsWith(Rect queryArea)
        {
            List<T> results = new List<T>();

            foreach (T item in m_Elements)
            {
                if (RectUtils.Intersects(item.boundingRect, queryArea))
                {
                    results.Add(item);
                }
            }

            foreach (QuadTreeNode<T> node in m_ChildrenNodes)
            {
                if (node.IsEmpty)
                    continue;

                if (RectUtils.Intersects(node.BoundingRect, queryArea))
                {
                    // the node completely contains the queryArea
                    // recurse down and stop
                    results.AddRange(node.IntersectsWith(queryArea));
                    break;
                }
            }

            return results;
        }

        public List<T> ContainedBy(Rect queryArea)
        {
            List<T> results = new List<T>();

            foreach (T item in m_Elements)
            {
                if (RectUtils.Contains(item.boundingRect, queryArea) || queryArea.Overlaps(item.boundingRect))
                {
                    results.Add(item);
                }
            }

            foreach (QuadTreeNode<T> node in m_ChildrenNodes)
            {
                if (node.IsEmpty)
                    continue;

                if (RectUtils.Contains(node.BoundingRect, queryArea))
                {
                    // the node completely contains the queryArea
                    // recurse down and stop
                    results.AddRange(node.ContainedBy(queryArea));
                    break;
                }

                if (RectUtils.Contains(queryArea, node.BoundingRect))
                {
                    // the queryArea completely contains this node
                    // just add everything under this node, recursively
                    results.AddRange(node.Elements(true));
                    continue;
                }

                if (node.BoundingRect.Overlaps(queryArea))
                {
                    // the node intersects
                    // recurse and continue iterating siblings
                    results.AddRange(node.ContainedBy(queryArea));
                }
            }

            return results;
        }

        public void Remove(T item)
        {
            m_Elements.Remove(item);
            foreach (QuadTreeNode<T> node in m_ChildrenNodes)
            {
                node.Remove(item);
            }
        }

        public void Insert(T item)
        {
            if (!RectUtils.Contains(m_BoundingRect, item.boundingRect))
            {
                Rect intersection = new Rect();
                if (!RectUtils.Intersection(item.boundingRect, m_BoundingRect, out intersection))
                {
                    // Ignore elements completely outside the quad tree
                    return;
                }
            }

            if (m_ChildrenNodes.Count == 0)
                Subdivide();

            // insert into children nodes
            foreach (QuadTreeNode<T> node in m_ChildrenNodes)
            {
                if (RectUtils.Contains(node.BoundingRect, item.boundingRect))
                {
                    node.Insert(item);
                    return;
                }
            }

            // item is not completely contained in any of the children nodes
            // insert here
            this.m_Elements.Add(item);
        }

        private void Subdivide()
        {
            if ((m_BoundingRect.height * m_BoundingRect.width) <= kSmallestAreaForQuadTreeNode)
                return;

            float halfWidth = (m_BoundingRect.width / 2f);
            float halfHeight = (m_BoundingRect.height / 2f);

            m_ChildrenNodes.Add(new QuadTreeNode<T>(new Rect(m_BoundingRect.position.x, m_BoundingRect.position.y, halfWidth, halfHeight)));
            m_ChildrenNodes.Add(new QuadTreeNode<T>(new Rect(m_BoundingRect.xMin, m_BoundingRect.yMin + halfHeight, halfWidth, halfHeight)));
            m_ChildrenNodes.Add(new QuadTreeNode<T>(new Rect(m_BoundingRect.xMin + halfWidth, m_BoundingRect.yMin, halfWidth, halfHeight)));
            m_ChildrenNodes.Add(new QuadTreeNode<T>(new Rect(m_BoundingRect.xMin + halfWidth, m_BoundingRect.yMin + halfHeight, halfWidth, halfHeight)));
        }

        public void DebugDraw(Vector2 offset)
        {
            HandleUtility.ApplyWireMaterial();
            Rect screenSpaceRect = m_BoundingRect;
            screenSpaceRect.x += offset.x;
            screenSpaceRect.y += offset.y;

            Handles.DrawSolidRectangleWithOutline(screenSpaceRect, m_DebugFillColor, m_DebugWireColor);
            foreach (QuadTreeNode<T> node in m_ChildrenNodes)
            {
                node.DebugDraw(offset);
            }

            foreach (IBounds i in Elements(false))
            {
                Rect o = i.boundingRect;
                o.x += offset.x;
                o.y += offset.y;
                Handles.DrawSolidRectangleWithOutline(o, m_DebugBoxFillColor, Color.yellow);
            }
        }
    };

    internal class QuadTree<T> where T : IBounds
    {
        private QuadTreeNode<T> m_Root = null;
        private Rect m_Rectangle;
        private Vector2 m_ScreenSpaceOffset = Vector2.zero;

        public QuadTree()
        {
            Clear();
        }

        public Vector2 screenSpaceOffset
        {
            get { return m_ScreenSpaceOffset; }
            set
            {
                m_ScreenSpaceOffset = value;
            }
        }

        public Rect rectangle
        {
            get { return m_Rectangle; }
        }

        public void Clear()
        {
            SetSize(new Rect(0, 0, 1, 1));
        }

        public void SetSize(Rect rectangle)
        {
            m_Root = null;
            m_Rectangle = rectangle;
            m_Root = new QuadTreeNode<T>(m_Rectangle);
        }

        public int Count { get { return m_Root.CountItemsIncludingChildren(); } }

        public void Insert(List<T> items)
        {
            foreach (T i in items)
            {
                Insert(i);
            }
        }

        public void Insert(T item)
        {
            m_Root.Insert(item);
        }

        public void Remove(T item)
        {
            m_Root.Remove(item);
        }

        public List<T> GetItemsAtPosition(Vector2 pos)
        {
            Rect r = new Rect(pos, Vector2.one);
            return IntersectsWith(r);
        }

        public List<T> IntersectsWith(Rect area)
        {
            return m_Root.IntersectsWith(area);
        }

        public List<T> ContainedBy(Rect area)
        {
            area.x -= m_ScreenSpaceOffset.x;
            area.y -= m_ScreenSpaceOffset.y;
            return m_Root.ContainedBy(area);
        }

        public List<T> Elements()
        {
            return m_Root.GetElementsIncludingChildren();
        }

        public void DebugDraw()
        {
            m_Root.DebugDraw(m_ScreenSpaceOffset);
        }
    }
}
