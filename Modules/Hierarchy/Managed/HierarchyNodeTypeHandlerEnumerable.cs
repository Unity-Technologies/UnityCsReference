// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Unity.Hierarchy
{
    /// <summary>
    /// A read-only collection of <see cref="HierarchyNodeTypeHandler"/> instances.
    /// </summary>
    public readonly struct HierarchyNodeTypeHandlerEnumerable
    {
        readonly Hierarchy m_Hierarchy;

        internal HierarchyNodeTypeHandlerEnumerable(Hierarchy hierarchy)
        {
            m_Hierarchy = hierarchy;
        }

        /// <summary>
        /// Gets an enumerator for the <see cref="HierarchyNodeTypeHandler"/>.
        /// </summary>
        /// <returns>An enumerator.</returns>
        public Enumerator GetEnumerator() => new Enumerator(m_Hierarchy);

        /// <summary>
        /// An enumerator for <see cref="HierarchyNodeTypeHandler"/> instances.
        /// </summary>
        public struct Enumerator : IDisposable
        {
            readonly IntPtr[] m_Handlers;
            readonly int m_Count;
            int m_Index;

            internal Enumerator(Hierarchy hierarchy)
            {
                var count = hierarchy.GetNodeTypeHandlersBaseCount();
                m_Handlers = ArrayPool<IntPtr>.Shared.Rent(count);
                m_Count = hierarchy.GetNodeTypeHandlersBaseSpan(m_Handlers.AsSpan()[..count]);
                m_Index = -1;
            }

            /// <summary>
            /// Releases the resources used by the enumerator.
            /// </summary>
            public void Dispose()
            {
                ArrayPool<IntPtr>.Shared.Return(m_Handlers);
            }

            /// <summary>
            /// Gets the current <see cref="HierarchyNodeTypeHandler"/> being enumerated.
            /// </summary>
            public HierarchyNodeTypeHandler Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => HierarchyNodeTypeHandlerBase.FromIntPtr(m_Handlers[m_Index]) as HierarchyNodeTypeHandler;
            }

            /// <summary>
            /// Moves to the next iterable value in the enumerator.
            /// </summary>
            /// <returns><see langword="true"/> if the current item is valid, <see langword="false"/> otherwise.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++m_Index < m_Count)
                {
                    if (HierarchyNodeTypeHandlerBase.FromIntPtr(m_Handlers[m_Index]) is HierarchyNodeTypeHandler)
                        return true;
                }
                return false;
            }
        }
    }
}
