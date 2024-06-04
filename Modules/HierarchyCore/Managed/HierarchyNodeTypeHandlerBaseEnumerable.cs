// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Unity.Hierarchy
{
    /// <summary>
    /// A read-only collection of hierarchy node type handler base.
    /// </summary>
    public readonly struct HierarchyNodeTypeHandlerBaseEnumerable
    {
        readonly Hierarchy m_Hierarchy;

        internal HierarchyNodeTypeHandlerBaseEnumerable(Hierarchy hierarchy)
        {
            m_Hierarchy = hierarchy;
        }

        /// <summary>
        /// Gets an enumerator for the node type handlers.
        /// </summary>
        /// <returns>An enumerator.</returns>
        public Enumerator GetEnumerator() => new Enumerator(m_Hierarchy);

        /// <summary>
        /// An enumerator for hierarchy node type handlers base.
        /// </summary>
        public struct Enumerator : IDisposable
        {
            readonly IMemoryOwner<IntPtr> m_Handlers;
            readonly int m_Count;
            int m_Index;

            internal Enumerator(Hierarchy hierarchy)
            {
                m_Handlers = MemoryPool<IntPtr>.Shared.Rent(hierarchy.GetNodeTypeHandlersBaseCount());
                m_Count = hierarchy.GetNodeTypeHandlersBaseSpan(m_Handlers.Memory.Span);
                m_Index = -1;
            }

            /// <summary>
            /// Dispose of the enumerator.
            /// </summary>
            public void Dispose()
            {
                m_Handlers.Dispose();
            }

            /// <summary>
            /// Get the current item being enumerated.
            /// </summary>
            public HierarchyNodeTypeHandlerBase Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => HierarchyNodeTypeHandlerBase.FromIntPtr(m_Handlers.Memory.Span[m_Index]);
            }

            /// <summary>
            /// Move to next iterable value.
            /// </summary>
            /// <returns><see langword="true"/> if Current item is valid, <see langword="false"/> otherwise.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++m_Index < m_Count;
        }
    }
}
