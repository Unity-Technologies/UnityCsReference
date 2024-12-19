// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnityEngine.UIElements.UIR
{
    class EntryPreProcessor
    {
        public struct AllocSize
        {
            public int vertexCount;
            public int indexCount;
        }

        public int childrenIndex => m_ChildrenIndex;
        public List<AllocSize> headAllocs => m_HeadAllocs;
        public List<AllocSize> tailAllocs => m_TailAllocs;

        public List<Entry> flattenedEntries => m_FlattenedEntries;

        int m_ChildrenIndex;

        List<AllocSize> m_Allocs; // This is just a pointer to one of the two other lists
        List<AllocSize> m_HeadAllocs = new(1);
        List<AllocSize> m_TailAllocs = new(1);

        List<Entry> m_FlattenedEntries = new(8);

        AllocSize m_Pending;
        Stack<AllocSize> m_Mask = new(1);
        bool m_IsPushingMask;

        public void PreProcess(Entry root)
        {
            m_ChildrenIndex = -1;
            m_FlattenedEntries.Clear();
            m_HeadAllocs.Clear();
            m_TailAllocs.Clear();

            m_Allocs = m_HeadAllocs;
            DoEvaluate(root);
            Flush();

            // Because the VE is complete
            Debug.Assert(!m_IsPushingMask);
            Debug.Assert(m_Mask.Count == 0);
        }

        // Clear important references to prevent memory retention
        public void ClearReferences()
        {
            m_FlattenedEntries.Clear();
        }

        void DoEvaluate(Entry entry)
        {
            while (entry != null)
            {
                if (entry.type != EntryType.DedicatedPlaceholder)
                    m_FlattenedEntries.Add(entry);

                switch (entry.type)
                {
                    case EntryType.DrawSolidMesh:
                    case EntryType.DrawTexturedMesh:
                    case EntryType.DrawTexturedMeshSkipAtlas:
                    case EntryType.DrawTextMesh:
                    case EntryType.DrawGradients:
                        Debug.Assert(entry.vertices.Length <= UIRenderDevice.maxVerticesPerPage);
                        Add(entry.vertices.Length, entry.indices.Length);
                        break;
                    case EntryType.DrawChildren:
                        Debug.Assert(!m_IsPushingMask); // Children must not contribute to a mask
                        Debug.Assert(m_ChildrenIndex == -1); // There should only be one of such entries
                        Flush();
                        m_ChildrenIndex = m_FlattenedEntries.Count - 1;
                        m_Allocs = tailAllocs;
                        break;
                    case EntryType.BeginStencilMask:
                        Debug.Assert(!m_IsPushingMask); // We don't support nested masking within a single VE yet
                        m_IsPushingMask = true;
                        break;
                    case EntryType.EndStencilMask:
                        Debug.Assert(m_IsPushingMask);
                        m_IsPushingMask = false;
                        break;
                    case EntryType.PopStencilMask:
                        while (m_Mask.TryPop(out AllocSize size))
                            Add(size.vertexCount, size.indexCount);
                        break;
                    case EntryType.PushClippingRect:
                    case EntryType.PopClippingRect:
                    case EntryType.PushScissors:
                    case EntryType.PopScissors:
                    case EntryType.PushGroupMatrix:
                    case EntryType.PopGroupMatrix:
                    case EntryType.DrawImmediate:
                    case EntryType.DrawImmediateCull:
                    case EntryType.PushRenderTexture:
                    case EntryType.BlitAndPopRenderTexture:
                    case EntryType.PushDefaultMaterial:
                    case EntryType.PopDefaultMaterial:
                    case EntryType.CutRenderChain:
                    case EntryType.DedicatedPlaceholder:
                        break; // Keep this to differentiate from unhandled cases
                    default:
                        throw new NotImplementedException(); // Keep this juste in case of future entry types
                }

                // Iterate
                if (entry.firstChild != null)
                    DoEvaluate(entry.firstChild); // Recurse
                entry = entry.nextSibling; // Next
            }
        }

        void Add(int vertexCount, int indexCount)
        {
            if (vertexCount == 0 || indexCount == 0)
                return;

            int nextVertexCount = m_Pending.vertexCount + vertexCount;
            if (nextVertexCount <= UIRenderDevice.maxVerticesPerPage)
            {
                // Accumulate
                m_Pending.vertexCount = nextVertexCount;
                m_Pending.indexCount += indexCount;
            }
            else
            {
                // New alloc
                Flush();
                m_Pending.vertexCount = vertexCount;
                m_Pending.indexCount = indexCount;
            }

            if (m_IsPushingMask)
            {
                m_Mask.Push(new AllocSize
                {
                    vertexCount = vertexCount,
                    indexCount = indexCount
                });
            }
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        void Flush()
        {
            if (m_Pending.vertexCount > 0)
            {
                m_Allocs.Add(m_Pending);
                m_Pending = new AllocSize();
            }
        }
    }
}
