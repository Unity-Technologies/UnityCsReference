// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace UnityEngine.U2D.UTess
{

    // Constrained Delaunay Triangulation.
    struct Tessellator
    {

        // For Processing.
        NativeArray<int2> m_Edges;
        NativeArray<UStar> m_Stars;
        Array<int3> m_Cells;
        int m_CellCount;

        // For Storage.
        NativeArray<int> m_ILArray;
        NativeArray<int> m_IUArray;
        NativeArray<int> m_SPArray;
        int m_NumEdges;
        int m_NumHulls;
        int m_NumPoints;
        int m_StarCount;

        // Intermediates.
        NativeArray<int> m_Flags;
        NativeArray<int> m_Neighbors;
        NativeArray<int> m_Constraints;
        Allocator m_Allocator;

        struct TestHullPointL : ICondition2<UHull, float2>
        {
            public bool Test(UHull h, float2 p, ref float t)
            {
                t = ModuleHandle.OrientFast(h.a, h.b, p);
                return t < 0;
            }
        }

        struct TestHullPointU : ICondition2<UHull, float2>
        {
            public bool Test(UHull h, float2 p, ref float t)
            {
                t = ModuleHandle.OrientFast(h.a, h.b, p);
                return t > 0;
            }
        }

        static float FindSplit(UHull hull, UEvent edge)
        {
            float d = 0;
            if (hull.a.x < edge.a.x)
            {
                d = ModuleHandle.OrientFast(hull.a, hull.b, edge.a);
            }
            else
            {
                d = ModuleHandle.OrientFast(edge.b, edge.a, hull.a);
            }

            if (0 != d)
            {
                return d;
            }

            if (edge.b.x < hull.b.x)
            {
                d = ModuleHandle.OrientFast(hull.a, hull.b, edge.b);
            }
            else
            {
                d = ModuleHandle.OrientFast(edge.b, edge.a, hull.b);
            }

            if (0 != d)
            {
                return d;
            }
            return hull.idx - edge.idx;
        }

        struct TestHullEventLe : ICondition2<UHull, UEvent>
        {
            public bool Test(UHull h, UEvent p, ref float t)
            {
                t = FindSplit(h, p);
                return t <= 0;
            }
        }

        struct TestHullEventE : ICondition2<UHull, UEvent>
        {
            public bool Test(UHull h, UEvent p, ref float t)
            {
                t = FindSplit(h, p);
                return t == 0;
            }
        }

        void SetAllocator(Allocator allocator)
        {
            m_Allocator = allocator;
        }

        bool AddPoint(NativeArray<UHull> hulls, int hullCount, NativeArray<float2> points, float2 p, int idx)
        {
            int l = ModuleHandle.GetLower(hulls, hullCount, p, new TestHullPointL());
            int u = ModuleHandle.GetUpper(hulls, hullCount, p, new TestHullPointU());
            if (l < 0 || u < 0)
                return false;
            for (int i = l; i < u; ++i)
            {
                UHull hull = hulls[i];

                int m = hull.ilcount;
                while (m > 1 && ModuleHandle.OrientFast(points[hull.ilarray[m - 2]], points[hull.ilarray[m - 1]], p) > 0)
                {
                    int3 c = new int3();
                    c.x = hull.ilarray[m - 1];
                    c.y = hull.ilarray[m - 2];
                    c.z = idx;
                    m_Cells[m_CellCount++] = c;
                    m -= 1;
                }

                hull.ilcount = m + 1;
                if (hull.ilcount > hull.ilarray.Length)
                    return false;
                hull.ilarray[m] = idx;

                m = hull.iucount;
                while (m > 1 && ModuleHandle.OrientFast(points[hull.iuarray[m - 2]], points[hull.iuarray[m - 1]], p) < 0)
                {
                    int3 c = new int3();
                    c.x = hull.iuarray[m - 2];
                    c.y = hull.iuarray[m - 1];
                    c.z = idx;
                    m_Cells[m_CellCount++] = c;
                    m -= 1;
                }

                hull.iucount = m + 1;
                if (hull.iucount > hull.iuarray.Length)
                    return false;
                hull.iuarray[m] = idx;

                hulls[i] = hull;
            }
            return true;
        }

        static void InsertHull(NativeArray<UHull> Hulls, int Pos, ref int Count, UHull Value)
        {
            if (Count < Hulls.Length - 1)
            {
                for (int i = Count; i > Pos; --i)
                    Hulls[i] = Hulls[i - 1];
                Hulls[Pos] = Value;
                Count++;
            }
        }

        static void EraseHull(NativeArray<UHull> Hulls, int Pos, ref int Count)
        {
            if (Count < Hulls.Length)
            {
                for (int i = Pos; i < Count - 1; ++i)
                    Hulls[i] = Hulls[i + 1];
                Count--;
            }
        }

        bool SplitHulls(NativeArray<UHull> hulls, ref int hullCount, NativeArray<float2> points, UEvent evt)
        {
            int index = ModuleHandle.GetLower(hulls, hullCount, evt, new TestHullEventLe());
            if (index < 0)
                return false;

            UHull hull = hulls[index];

            UHull newHull;
            newHull.a = evt.a;
            newHull.b = evt.b;
            newHull.idx = evt.idx;

            int y = hull.iuarray[hull.iucount - 1];
            newHull.iuarray = new ArraySlice<int>(m_IUArray, newHull.idx * m_NumHulls, m_NumHulls);
            newHull.iucount = hull.iucount;
            for (int i = 0; i < newHull.iucount; ++i)
                newHull.iuarray[i] = hull.iuarray[i];
            hull.iuarray[0] = y;
            hull.iucount = 1;
            hulls[index] = hull;

            newHull.ilarray = new ArraySlice<int>(m_ILArray, newHull.idx * m_NumHulls, m_NumHulls);
            newHull.ilarray[0] = y;
            newHull.ilcount = 1;

            InsertHull(hulls, index + 1, ref hullCount, newHull);
            return true;
        }

        bool MergeHulls(NativeArray<UHull> hulls, ref int hullCount, NativeArray<float2> points, UEvent evt)
        {
            float2 temp = evt.a;
            evt.a = evt.b;
            evt.b = temp;
            int index = ModuleHandle.GetEqual(hulls, hullCount, evt, new TestHullEventE());
            if (index < 0)
                return false;

            UHull upper = hulls[index];
            UHull lower = hulls[index - 1];

            lower.iucount = upper.iucount;
            for (int i = 0; i < lower.iucount; ++i)
                lower.iuarray[i] = upper.iuarray[i];

            hulls[index - 1] = lower;
            EraseHull(hulls, index, ref hullCount);
            return true;
        }

        static void InsertUniqueEdge(NativeArray<int2> edges, int2 e, ref int edgeCount)
        {
            TessEdgeCompare edgeComparer = new TessEdgeCompare();
            var validEdge = true;
            for (int j = 0; validEdge && j < edgeCount; ++j)
                if (edgeComparer.Compare(e, edges[j]) == 0)
                    validEdge = false;
            if (validEdge)
                edges[edgeCount++] = e;
        }

        void PrepareDelaunay(NativeArray<int2> edges, int edgeCount)
        {
            m_StarCount = m_CellCount * 3;
            m_Stars = new NativeArray<UStar>(m_StarCount, m_Allocator);
            m_SPArray = new NativeArray<int>(m_StarCount * m_StarCount, m_Allocator, NativeArrayOptions.UninitializedMemory);

            var UEdgeCount = 0;
            var UEdges = new NativeArray<int2>(m_StarCount, m_Allocator);

            // Input Edges.
            for (int i = 0; i < edgeCount; ++i)
            {
                int2 e = edges[i];
                e.x = (edges[i].x < edges[i].y) ? edges[i].x : edges[i].y;
                e.y = (edges[i].x > edges[i].y) ? edges[i].x : edges[i].y;
                edges[i] = e;
                InsertUniqueEdge(UEdges, e, ref UEdgeCount);
            }

            m_Edges = new NativeArray<int2>(UEdgeCount, m_Allocator);
            for (int i = 0; i < UEdgeCount; ++i)
                m_Edges[i] = UEdges[i];
            UEdges.Dispose();

            unsafe
            {
                ModuleHandle.InsertionSort<int2, TessEdgeCompare>(
                    NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(m_Edges), 0, m_Edges.Length - 1,
                    new TessEdgeCompare());
            }

            // Init Stars.
            for (int i = 0; i < m_StarCount; ++i)
            {
                UStar s = m_Stars[i];
                s.points = new ArraySlice<int>(m_SPArray, i * m_StarCount, m_StarCount);
                s.pointCount = 0;
                m_Stars[i] = s;
            }

            // Fill stars.
            for (int i = 0; i < m_CellCount; ++i)
            {
                int a = m_Cells[i].x;
                int b = m_Cells[i].y;
                int c = m_Cells[i].z;
                UStar sa = m_Stars[a];
                UStar sb = m_Stars[b];
                UStar sc = m_Stars[c];
                sa.points[sa.pointCount++] = b;
                sa.points[sa.pointCount++] = c;
                sb.points[sb.pointCount++] = c;
                sb.points[sb.pointCount++] = a;
                sc.points[sc.pointCount++] = a;
                sc.points[sc.pointCount++] = b;
                m_Stars[a] = sa;
                m_Stars[b] = sb;
                m_Stars[c] = sc;
            }

        }

        int OppositeOf(int a, int b)
        {
            ArraySlice<int> points = m_Stars[b].points;
            for (int k = 1, n = m_Stars[b].pointCount; k < n; k += 2)
                if (points[k] == a)
                    return points[k - 1];
            return -1;
        }

        struct TestEdgePointE : ICondition2<int2, int2>
        {
            public bool Test(int2 h, int2 p, ref float t)
            {
                TessEdgeCompare tc = new TessEdgeCompare();
                t = tc.Compare(h, p);
                return t == 0;
            }
        }

        int FindConstraint(int a, int b)
        {
            int2 e;
            e.x = a < b ? a : b;
            e.y = a > b ? a : b;
            return ModuleHandle.GetEqual(m_Edges, m_Edges.Length, e, new TestEdgePointE());
        }

        void AddTriangle(int i, int j, int k)
        {
            UStar si = m_Stars[i];
            UStar sj = m_Stars[j];
            UStar sk = m_Stars[k];
            si.points[si.pointCount++] = j;
            si.points[si.pointCount++] = k;
            sj.points[sj.pointCount++] = k;
            sj.points[sj.pointCount++] = i;
            sk.points[sk.pointCount++] = i;
            sk.points[sk.pointCount++] = j;
            m_Stars[i] = si;
            m_Stars[j] = sj;
            m_Stars[k] = sk;
        }

        void RemovePair(int r, int j, int k)
        {
            UStar s = m_Stars[r];
            ArraySlice<int> points = s.points;
            for (int i = 1, n = s.pointCount; i < n; i += 2)
            {
                if (points[i - 1] == j && points[i] == k)
                {
                    points[i - 1] = points[n - 2];
                    points[i] = points[n - 1];
                    s.points = points;
                    s.pointCount = s.pointCount - 2;
                    m_Stars[r] = s;
                    return;
                }
            }
        }

        void RemoveTriangle(int i, int j, int k)
        {
            RemovePair(i, j, k);
            RemovePair(j, k, i);
            RemovePair(k, i, j);
        }

        void EdgeFlip(int i, int j)
        {
            int a = OppositeOf(i, j);
            int b = OppositeOf(j, i);
            RemoveTriangle(i, j, a);
            RemoveTriangle(j, i, b);
            AddTriangle(i, b, a);
            AddTriangle(j, a, b);
        }

        bool Flip(NativeArray<float2> points, ref Array<int> stack, ref int stackCount, int a, int b, int x)
        {
            int y = OppositeOf(a, b);

            if (y < 0)
            {
                return true;
            }

            if (b < a)
            {
                int tmp = a;
                a = b;
                b = tmp;
                tmp = x;
                x = y;
                y = tmp;
            }

            if (FindConstraint(a, b) != -1)
            {
                return true;
            }

            if (ModuleHandle.IsInsideCircle(points[a], points[b], points[x], points[y]))
            {
                if ((2 + stackCount) >= stack.Length)
                    return false;
                stack[stackCount++] = a;
                stack[stackCount++] = b;
            }

            return true;
        }

        Array<int3> GetCells(ref int count)
        {
            var cellsOut = new Array<int3>(m_NumPoints * 4, m_NumPoints * (m_NumPoints + 1), m_Allocator, NativeArrayOptions.UninitializedMemory);
            count = 0;
            for (int i = 0, n = m_Stars.Length; i < n; ++i)
            {
                ArraySlice<int> points = m_Stars[i].points;
                for (int j = 0, m = m_Stars[i].pointCount; j < m; j += 2)
                {
                    int s = points[j];
                    int t = points[j + 1];
                    if (i < math.min(s, t))
                    {
                        int3 c = new int3();
                        c.x = i;
                        c.y = s;
                        c.z = t;
                        cellsOut[count++] = c;
                    }
                }
            }

            return cellsOut;
        }

        internal bool ApplyDelaunay(NativeArray<float2> points, NativeArray<int2> edges)
        {

            // Early out if cannot find any valid cells.
            if (0 == m_CellCount)
                return false;

            var stack = new Array<int>(m_NumPoints * 4, m_NumPoints * (m_NumPoints + 1), m_Allocator, NativeArrayOptions.UninitializedMemory);
            int stackCount = 0;
            var valid = true;

            PrepareDelaunay(edges, m_NumEdges);
            for (int a = 0; valid && (a < m_NumPoints); ++a)
            {
                UStar star = m_Stars[a];
                for (int j = 1; j < star.pointCount; j += 2)
                {
                    int b = star.points[j];

                    if (b < a)
                    {
                        continue;
                    }

                    if (FindConstraint(a, b) >= 0)
                    {
                        continue;
                    }

                    int x = star.points[j - 1], y = -1;
                    for (int k = 1; k < star.pointCount; k += 2)
                    {
                        if (star.points[k - 1] == b)
                        {
                            y = star.points[k];
                            break;
                        }
                    }

                    if (y < 0)
                    {
                        continue;
                    }

                    if (ModuleHandle.IsInsideCircle(points[a], points[b], points[x], points[y]))
                    {
                        if ((2 + stackCount) >= stack.Length)
                        {
                            valid = false;
                            break;
                        }

                        stack[stackCount++] = a;
                        stack[stackCount++] = b;
                    }
                }
            }

            var flipFlops = m_NumPoints * m_NumPoints;
            while (stackCount > 0 && valid)
            {
                int b = stack[stackCount - 1];
                stackCount--;
                int a = stack[stackCount - 1];
                stackCount--;

                int x = -1, y = -1;
                UStar star = m_Stars[a];
                for (int i = 1; i < star.pointCount; i += 2)
                {
                    int s = star.points[i - 1];
                    int t = star.points[i];
                    if (s == b)
                    {
                        y = t;
                    }
                    else if (t == b)
                    {
                        x = s;
                    }
                }

                if (x < 0 || y < 0)
                {
                    continue;
                }

                if (!ModuleHandle.IsInsideCircle(points[a], points[b], points[x], points[y]))
                {
                    continue;
                }

                EdgeFlip(a, b);

                valid = Flip(points, ref stack, ref stackCount, x, a, y);
                valid = valid && Flip(points, ref stack, ref stackCount, a, y, x);
                valid = valid && Flip(points, ref stack, ref stackCount, y, b, x);
                valid = valid && Flip(points, ref stack, ref stackCount, b, x, y);
                valid = valid && (--flipFlops > 0);
            }

            stack.Dispose();
            return valid;
        }

        struct TestCellE : ICondition2<int3, int3>
        {
            public bool Test(int3 h, int3 p, ref float t)
            {
                TessCellCompare tc = new TessCellCompare();
                t = tc.Compare(h, p);
                return t == 0;
            }
        }

        int FindNeighbor(Array<int3> cells, int count, int a, int b, int c)
        {
            int x = a, y = b, z = c;
            if (b < c)
            {
                if (b < a)
                {
                    x = b;
                    y = c;
                    z = a;
                }
            }
            else if (c < a)
            {
                x = c;
                y = a;
                z = b;
            }

            if (x < 0)
            {
                return -1;
            }

            int3 key;
            key.x = x;
            key.y = y;
            key.z = z;
            return ModuleHandle.GetEqual(cells, count, key, new TestCellE());
        }

        Array<int3> Constrain(ref int count)
        {
            var cells = GetCells(ref count);
            int nc = count;
            for (int i = 0; i < nc; ++i)
            {
                int3 c = cells[i];
                int x = c.x, y = c.y, z = c.z;
                if (y < z)
                {
                    if (y < x)
                    {
                        c.x = y;
                        c.y = z;
                        c.z = x;
                    }
                }
                else if (z < x)
                {
                    c.x = z;
                    c.y = x;
                    c.z = y;
                }

                cells[i] = c;
            }

            unsafe
            {
                ModuleHandle.InsertionSort<int3, TessCellCompare>(
                    cells.UnsafePtr, 0, count - 1,
                    new TessCellCompare());
            }

            // Out
            m_Flags = new NativeArray<int>(nc, m_Allocator);
            m_Neighbors = new NativeArray<int>(nc * 3, m_Allocator);
            m_Constraints = new NativeArray<int>(nc * 3, m_Allocator);
            var next = new NativeArray<int>(nc * 3, m_Allocator);
            var active = new NativeArray<int>(nc * 3, m_Allocator);

            int side = 1, nextCount = 0, activeCount = 0;

            for (int i = 0; i < nc; ++i)
            {
                int3 c = cells[i];
                for (int j = 0; j < 3; ++j)
                {
                    int x = j, y = (j + 1) % 3;
                    x = (x == 0) ? c.x : (j == 1) ? c.y : c.z;
                    y = (y == 0) ? c.x : (y == 1) ? c.y : c.z;

                    int o = OppositeOf(y, x);
                    int a = m_Neighbors[3 * i + j] = FindNeighbor(cells, count, y, x, o);
                    int b = m_Constraints[3 * i + j] = (-1 != FindConstraint(x, y)) ? 1 : 0;
                    if (a < 0)
                    {
                        if (0 != b)
                        {
                            next[nextCount++] = i;
                        }
                        else
                        {
                            active[activeCount++] = i;
                            m_Flags[i] = 1;
                        }
                    }
                }
            }

            while (activeCount > 0 || nextCount > 0)
            {
                while (activeCount > 0)
                {
                    int t = active[activeCount - 1];
                    activeCount--;
                    if (m_Flags[t] == -side)
                    {
                        continue;
                    }

                    m_Flags[t] = side;
                    int3 c = cells[t];
                    for (int j = 0; j < 3; ++j)
                    {
                        int f = m_Neighbors[3 * t + j];
                        if (f >= 0 && m_Flags[f] == 0)
                        {
                            if (0 != m_Constraints[3 * t + j])
                            {
                                next[nextCount++] = f;
                            }
                            else
                            {
                                active[activeCount++] = f;
                                m_Flags[f] = side;
                            }
                        }
                    }
                }

                for (int e = 0; e < nextCount; e++)
                    active[e] = next[e];
                activeCount = nextCount;
                nextCount = 0;
                side = -side;
            }

            active.Dispose();
            next.Dispose();
            return cells;
        }

        internal NativeArray<int3> RemoveExterior(ref int cellCount)
        {
            int constrainedCount = 0;
            var constrained = Constrain(ref constrainedCount);
            var cellsOut = new NativeArray<int3>(constrainedCount, m_Allocator);
            cellCount = 0;
            for (int i = 0; i < constrainedCount; ++i)
            {
                if (m_Flags[i] == -1)
                {
                    cellsOut[cellCount++] = constrained[i];
                }
            }

            constrained.Dispose();
            return cellsOut;
        }

        // Unused todo: Remove
        internal NativeArray<int3> RemoveInterior(ref int cellCount)
        {
            int constrainedCount = 0;
            var constrained = Constrain(ref constrainedCount);
            var cellsOut = new NativeArray<int3>(constrainedCount, m_Allocator);
            cellCount = 0;
            for (int i = 0; i < constrainedCount; ++i)
            {
                if (m_Flags[i] == 1)
                {
                    cellsOut[cellCount++] = constrained[i];
                }
            }

            constrained.Dispose();
            return cellsOut;
        }

        internal bool Triangulate(NativeArray<float2> points, int pointCount, NativeArray<int2> edges, int edgeCount)
        {
            m_NumEdges = edgeCount;
            m_NumHulls = edgeCount * 2;
            m_NumPoints = pointCount;
            m_CellCount = 0;
            int allocSize = m_NumHulls * (m_NumHulls + 1);
            m_Cells = new Array<int3>(allocSize, ModuleHandle.kMaxTriangleCount, m_Allocator, NativeArrayOptions.UninitializedMemory);
            m_ILArray = new NativeArray<int>(allocSize, m_Allocator); // Make room for -1 node.
            m_IUArray = new NativeArray<int>(allocSize, m_Allocator); // Make room for -1 node.

            NativeArray<UHull> hulls = new NativeArray<UHull>(m_NumPoints * 8, m_Allocator);
            int hullCount = 0;

            NativeArray<UEvent> events = new NativeArray<UEvent>(m_NumPoints + (m_NumEdges * 2), m_Allocator);
            int eventCount = 0;

            for (int i = 0; i < m_NumPoints; ++i)
            {
                UEvent evt = new UEvent();
                evt.a = points[i];
                evt.b = new float2();
                evt.idx = i;
                evt.type = (int)UEventType.EVENT_POINT;
                events[eventCount++] = evt;
            }

            for (int i = 0; i < m_NumEdges; ++i)
            {
                int2 e = edges[i];
                float2 a = points[e.x];
                float2 b = points[e.y];
                if (a.x < b.x)
                {
                    UEvent _s = new UEvent();
                    _s.a = a;
                    _s.b = b;
                    _s.idx = i;
                    _s.type = (int)UEventType.EVENT_START;

                    UEvent _e = new UEvent();
                    _e.a = b;
                    _e.b = a;
                    _e.idx = i;
                    _e.type = (int)UEventType.EVENT_END;

                    events[eventCount++] = _s;
                    events[eventCount++] = _e;
                }
                else if (a.x > b.x)
                {
                    UEvent _s = new UEvent();
                    _s.a = b;
                    _s.b = a;
                    _s.idx = i;
                    _s.type = (int)UEventType.EVENT_START;

                    UEvent _e = new UEvent();
                    _e.a = a;
                    _e.b = b;
                    _e.idx = i;
                    _e.type = (int)UEventType.EVENT_END;

                    events[eventCount++] = _s;
                    events[eventCount++] = _e;
                }
                else
                {
                    if (a.y < b.y)
                    {
                        UEvent _s = new UEvent();
                        _s.a = a;
                        _s.b = b;
                        _s.idx = i;
                        _s.type = (int)UEventType.EVENT_START;

                        UEvent _e = new UEvent();
                        _e.a = b;
                        _e.b = a;
                        _e.idx = i;
                        _e.type = (int)UEventType.EVENT_END;

                        events[eventCount++] = _s;
                        events[eventCount++] = _e;
                    }
                    else if (a.y > b.y)
                    {
                        UEvent _s = new UEvent();
                        _s.a = b;
                        _s.b = a;
                        _s.idx = i;
                        _s.type = (int)UEventType.EVENT_START;

                        UEvent _e = new UEvent();
                        _e.a = a;
                        _e.b = b;
                        _e.idx = i;
                        _e.type = (int)UEventType.EVENT_END;

                        events[eventCount++] = _s;
                        events[eventCount++] = _e;
                    }
                }
            }

            unsafe
            {
                ModuleHandle.InsertionSort<UEvent, TessEventCompare>(
                    NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(events), 0, eventCount - 1,
                    new TessEventCompare());
                ;
            }

            var hullOp = true;
            float minX = events[0].a.x - (1 + math.abs(events[0].a.x)) * math.pow(2.0f, -16.0f);
            UHull hull;
            hull.a.x = minX;
            hull.a.y = 1;
            hull.b.x = minX;
            hull.b.y = 0;
            hull.idx = -1;
            hull.ilarray = new ArraySlice<int>(m_ILArray, m_NumHulls * m_NumHulls, m_NumHulls); // Last element
            hull.iuarray = new ArraySlice<int>(m_IUArray, m_NumHulls * m_NumHulls, m_NumHulls);
            hull.ilcount = 0;
            hull.iucount = 0;
            hulls[hullCount++] = hull;


            for (int i = 0, numEvents = eventCount; i < numEvents; ++i)
            {

                switch (events[i].type)
                {
                    case (int) UEventType.EVENT_POINT:
                    {
                        hullOp = AddPoint(hulls, hullCount, points, events[i].a, events[i].idx);
                    }
                        break;

                    case (int) UEventType.EVENT_START:
                    {
                        hullOp = SplitHulls(hulls, ref hullCount, points, events[i]);
                    }
                        break;

                    default:
                    {
                        hullOp = MergeHulls(hulls, ref hullCount, points, events[i]);
                    }
                        break;
                }

                if (!hullOp)
                    break;
            }

            events.Dispose();
            hulls.Dispose();
            return hullOp;
        }

        internal static bool Tessellate(Allocator allocator, NativeArray<float2> pgPoints, int pgPointCount, NativeArray<int2> pgEdges, int pgEdgeCount, ref NativeArray<float2> outputVertices, ref int vertexCount, ref NativeArray<int> outputIndices, ref int indexCount)
        {
            // Process.
            Tessellator tess = new Tessellator();
            tess.SetAllocator(allocator);
            int maxCount = 0, triCount = 0;
            var valid = true;

            valid = tess.Triangulate(pgPoints, pgPointCount, pgEdges, pgEdgeCount);
            valid = valid && tess.ApplyDelaunay(pgPoints, pgEdges);

            if (valid)
            {
                // Output.
                NativeArray<int3> cells = tess.RemoveExterior(ref triCount);
                for (var i = 0; i < triCount; ++i)
                {
                    var a = (UInt16)cells[i].x;
                    var b = (UInt16)cells[i].y;
                    var c = (UInt16)cells[i].z;
                    if (a != b && b != c && a != c)
                    {
                        outputIndices[indexCount++] = a;
                        outputIndices[indexCount++] = c;
                        outputIndices[indexCount++] = b;
                    }
                    maxCount = math.max(math.max(math.max(cells[i].x, cells[i].y), cells[i].z), maxCount);
                }
                maxCount = (maxCount != 0) ? (maxCount + 1) : 0;
                for (var i = 0; i < maxCount; ++i)
                    outputVertices[vertexCount++] = pgPoints[i];
                cells.Dispose();
            }

            tess.Cleanup();
            return valid;
        }

        internal static bool TessellateMainThread(Allocator allocator, ref NativeArray<float2> pgPoints, ref NativeArray<int2> pgEdges, out NativeArray<float2> outputVertices, out NativeArray<int> outputIndices)
        {
            // Process.
            Tessellator tess = new Tessellator();
            tess.SetAllocator(allocator);
            int maxCount = 0, triCount = 0, indexCount = 0;
            var valid = true;

            valid = tess.Triangulate(pgPoints, pgPoints.Length, pgEdges, pgEdges.Length);
            valid = valid && tess.ApplyDelaunay(pgPoints, pgEdges);

            if (valid)
            {
                // Output.
                NativeArray<int3> cells = tess.RemoveExterior(ref triCount);
                NativeArray<int> intermediate = new NativeArray<int>(triCount * 3, allocator);
                for (var i = 0; i < triCount; ++i)
                {
                    var a = (UInt16)cells[i].x;
                    var b = (UInt16)cells[i].y;
                    var c = (UInt16)cells[i].z;
                    if (a != b && b != c && a != c)
                    {
                        intermediate[indexCount++] = a;
                        intermediate[indexCount++] = c;
                        intermediate[indexCount++] = b;
                    }
                    maxCount = math.max(math.max(math.max(cells[i].x, cells[i].y), cells[i].z), maxCount);
                }
                maxCount = (maxCount != 0) ? (maxCount + 1) : 0;

                outputIndices = new NativeArray<int>(indexCount, allocator);
                outputVertices = new NativeArray<float2>(maxCount, allocator);

                NativeArray<int>.Copy(intermediate, 0, outputIndices, 0, indexCount);
                NativeArray<float2>.Copy(pgPoints, 0, outputVertices, 0, maxCount);
                cells.Dispose();
                intermediate.Dispose();
            }
            else
            {
                outputIndices = new NativeArray<int>(1, allocator);
                outputVertices = new NativeArray<float2>(1, allocator);
            }

            tess.Cleanup();
            return valid;
        }

        internal void Cleanup()
        {
            if (m_Edges.IsCreated) m_Edges.Dispose();
            if (m_Stars.IsCreated) m_Stars.Dispose();
            if (m_SPArray.IsCreated) m_SPArray.Dispose();
            if (m_Cells.IsCreated) m_Cells.Dispose();
            if (m_ILArray.IsCreated) m_ILArray.Dispose();
            if (m_IUArray.IsCreated) m_IUArray.Dispose();
            if (m_Flags.IsCreated) m_Flags.Dispose();
            if (m_Neighbors.IsCreated) m_Neighbors.Dispose();
            if (m_Constraints.IsCreated) m_Constraints.Dispose();
        }

    }

}
