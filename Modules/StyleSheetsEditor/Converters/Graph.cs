// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.StyleSheets
{
    internal class Graph
    {
        private int m_NbVertices;
        private List<int>[] m_Adjacents;

        //Constructor
        public Graph(int v)
        {
            m_NbVertices = v;
            m_Adjacents = new List<int>[v];
            for (int i = 0; i < v; ++i)
                m_Adjacents[i] = new List<int>();
        }

        public void AddEdge(int v, int w)
        {
            // w is adjacent to v. edge is directed v -> w
            m_Adjacents[v].Add(w);
        }

        public List<int> DepthFirstTraversal(int initialVertexIndex = -1)
        {
            var dsfTraversal = new List<int>();
            var visited = new bool[m_NbVertices];

            if (initialVertexIndex == -1)
            {
                for (var vertexIndex = 0; vertexIndex < m_Adjacents.Length; ++vertexIndex)
                {
                    var adj = m_Adjacents[vertexIndex];
                    for (var childrenIndex = 0; childrenIndex < adj.Count; ++childrenIndex)
                    {
                        visited[adj[childrenIndex]] = true;
                    }
                }

                // The roots are have no ingoing edges:
                var roots = new List<int>();
                for (var vertexIndex = 0; vertexIndex < m_Adjacents.Length; ++vertexIndex)
                    if (!visited[vertexIndex]) roots.Add(vertexIndex);

                // We have our roots, reset the visited array:
                for (var i = 0; i < visited.Length; ++i) visited[i] = false;

                // DFs traversal according to the roots:
                for (int i = 0; i < roots.Count; i++)
                {
                    var rootIndex = roots[i];
                    if (!visited[rootIndex])
                        Visit(rootIndex, visited, dsfTraversal);
                }
            }
            else
            {
                Visit(initialVertexIndex, visited, dsfTraversal);
            }

            return dsfTraversal;
        }

        public int[] TopologicalSort()
        {
            var stack = new Stack<int>();

            var visited = new bool[m_NbVertices];
            for (int i = 0; i < m_NbVertices; i++)
                if (!visited[i])
                    Visit(i, visited, stack);

            return stack.ToArray();
        }

        private void Visit(int v, bool[] visited, List<int> dsfTraversal)
        {
            // Mark the current node as visited.
            visited[v] = true;
            dsfTraversal.Add(v);
            foreach (var i in m_Adjacents[v])
            {
                if (!visited[i])
                    Visit(i, visited, dsfTraversal);
            }
        }

        private void Visit(int v, bool[] visited, Stack<int> stack)
        {
            // Mark the current node as visited.
            visited[v] = true;

            // Recur for all the vertices adjacent to this vertex
            foreach (var i in m_Adjacents[v])
            {
                if (!visited[i])
                    Visit(i, visited, stack);
            }

            // Push current vertex to stack which stores result
            stack.Push(v);
        }
    }
}
