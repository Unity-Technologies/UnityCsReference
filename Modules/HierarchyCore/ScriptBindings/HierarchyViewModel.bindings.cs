// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Pool;
using UnityEngine.Scripting;

namespace Unity.Hierarchy
{
    /// <summary>
    /// A hierarchy view model is a read-only filtering view of a <see cref="HierarchyFlattened"/>. 
    /// </summary>
    [NativeType(Header = "Modules/HierarchyCore/Public/HierarchyViewModel.h")]
    [NativeHeader("Modules/HierarchyCore/HierarchyViewModelBindings.h")]
    [RequiredByNativeCode(GenerateProxy = true), StructLayout(LayoutKind.Sequential)]
    public sealed class HierarchyViewModel :
        IDisposable,
        IEnumerable<HierarchyNode>,
        IReadOnlyCollection<HierarchyNode>,
        IReadOnlyList<HierarchyNode>
    {
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(HierarchyViewModel viewModel) => viewModel.m_Ptr;
        }

        [RequiredByNativeCode] IntPtr m_Ptr;
        [RequiredByNativeCode] readonly bool m_IsWrapper;
        [RequiredByNativeCode] readonly HierarchyFlattened m_HierarchyFlattened;
        [RequiredByNativeCode] readonly Hierarchy m_Hierarchy;

        [FreeFunction("HierarchyViewModelBindings::Create")]
        static extern IntPtr Internal_Create(HierarchyFlattened hierarchyFlattened);

        [FreeFunction("HierarchyViewModelBindings::Destroy")]
        static extern void Internal_Destroy(IntPtr ptr);

        /// <summary>
        /// Whether this object is valid and uses memory.
        /// </summary>
        public bool IsCreated => m_Ptr != IntPtr.Zero;

        /// <summary>
        /// The total number of nodes.
        /// </summary>
        /// <remarks>
        /// The total does not include the <see cref="Hierarchy.Root"/> node.
        /// </remarks>
        public extern int Count { [NativeMethod("Count")] get; }

        /// <summary>
        /// Whether the hierarchy view model is currently updating.
        /// </summary>
        /// <remarks>
        /// This happens when <see cref="UpdateIncremental"/> or <see cref="UpdateIncrementalTimed"/> is used.
        /// </remarks>
        public extern bool Updating { [NativeMethod("Updating")] get; }

        /// <summary>
        /// Whether the hierarchy view model requires an update.
        /// </summary>
        /// <remarks>
        /// This happens when the underlying hierarchy changes topology.
        /// </remarks>
        public extern bool UpdateNeeded { [NativeMethod("UpdateNeeded")] get; }

        /// <summary>
        /// Accesses the <see cref="HierarchyFlattened"/>.
        /// </summary>
        public HierarchyFlattened HierarchyFlattened => m_HierarchyFlattened;

        /// <summary>
        /// Accesses the <see cref="Hierarchy"/>.
        /// </summary>
        public Hierarchy Hierarchy => m_Hierarchy;

        internal extern float UpdateProgress
        {
            [VisibleToOtherModules("UnityEngine.HierarchyModule")]
            [NativeMethod("UpdateProgress")]
            get;
        }

        internal IHierarchySearchQueryParser QueryParser
        {
            [VisibleToOtherModules("UnityEditor.HierarchyModule")]
            get;
            [VisibleToOtherModules("UnityEditor.HierarchyModule")]
            set;
        }

        internal extern HierarchySearchQueryDescriptor Query
        {
            [VisibleToOtherModules("UnityEngine.HierarchyModule")]
            get;
            [VisibleToOtherModules("UnityEngine.HierarchyModule")]
            set;
        }

        /// <summary>
        /// Creates a new HierarchyViewModel from a <see cref="HierarchyFlattened"/>.
        /// </summary>
        /// <param name="hierarchyFlattened">The flattened hierarchy that serves as the hierarchy model.</param>
        public HierarchyViewModel(HierarchyFlattened hierarchyFlattened)
        {
            m_Ptr = Internal_Create(hierarchyFlattened);
            m_HierarchyFlattened = hierarchyFlattened;
            m_Hierarchy = hierarchyFlattened.Hierarchy;
            QueryParser = new DefaultHierarchySearchQueryParser();
        }

        ~HierarchyViewModel()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes this object and releases its memory.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (m_Ptr != IntPtr.Zero)
            {
                if (!m_IsWrapper)
                    Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Gets the <see cref="HierarchyNode"/> at a specified index.
        /// </summary>
        /// <param name="index">The node index.</param>
        /// <returns>A hierarchy node.</returns>
        public HierarchyNode this[int index] => ElementAt(index);

        /// <summary>
        /// Gets the zero-based index of a specified node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>A zero-based index of the node if found, -1 otherwise.</returns>
        [NativeThrows]
        public extern int IndexOf(in HierarchyNode node);

        /// <summary>
        /// Determines if a specified node is in the hierarchy view model.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns><see langword="true"/> if the node is found, <see langword="false"/> otherwise.</returns>
        [NativeThrows]
        public extern bool Contains(in HierarchyNode node);

        /// <summary>
        /// Gets the parent of a hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>A hierarchy node.</returns>
        [NativeThrows]
        public extern HierarchyNode GetParent(in HierarchyNode node);

        /// <summary>
        /// Gets the next sibling of a node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>A hierarchy node.</returns>
        [NativeThrows]
        public extern HierarchyNode GetNextSibling(in HierarchyNode node);

        /// <summary>
        /// Gets the number of child nodes that a hierarchy node has.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The number of children.</returns>
        [NativeThrows]
        public extern int GetChildrenCount(in HierarchyNode node);

        /// <summary>
        /// Determines the depth of a node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The depth of the hierarchy node.</returns>
        [NativeThrows]
        public extern int GetDepth(in HierarchyNode node);

        /// <summary>
        /// Sets the expanded state of a node.
        /// </summary>
        /// <remarks>
        /// Nodes are expanded by default.
        /// </remarks>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="expanded">The expanded state.</param>
        [NativeThrows]
        public extern void SetExpanded(in HierarchyNode node, bool expanded);

        /// <summary>
        /// Gets the expanded state of a node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns><see langword="true"/> if the node is expanded, <see langword="false"/> otherwise.</returns>
        [NativeThrows]
        public extern bool GetExpanded(in HierarchyNode node);

        /// <summary>
        /// Sets the selected state of a node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="selected">The selected state.</param>
        [NativeThrows]
        public extern void SetSelected(in HierarchyNode node, bool selected);

        /// <summary>
        /// Gets the selected state of a node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns><see langword="true"/> if the node is selected, <see langword="false"/> otherwise.</returns>
        [NativeThrows]
        public extern bool GetSelected(in HierarchyNode node);

        /// <summary>
        /// Sets the search query.
        /// </summary>
        /// <param name="query">The search query.</param>
        public void SetQuery(string query)
        {
            var newQuery = QueryParser.ParseQuery(query);
            if (newQuery == Query)
                return;
            Query = newQuery;
        }

        /// <summary>
        /// Updates the hierarchy view model and requests a rebuild of the list of <see cref="HierarchyNode"/> that filters the <see cref="HierarchyFlattened"/>.
        /// </summary>
        public extern void Update();

        /// <summary>
        /// Updates the hierarchy view model incrementally. 
        /// </summary>
        /// <returns><see langword="true"/> if additional invocations are needed to complete the update, <see langword="false"/> otherwise.</returns>
        public extern bool UpdateIncremental();

        /// <summary>
        /// Updates the hierarchy view model incrementally until a time limit is reached.
        /// </summary>
        /// <param name="milliseconds">The time period in milliseconds.</param>
        /// <returns><see langword="true"/> if additional invocations are needed to complete the update, <see langword="false"/> otherwise.</returns>
        public extern bool UpdateIncrementalTimed(double milliseconds);

        /// <summary>
        /// Gets the <see cref="HierarchyNode"/> enumerator.
        /// </summary>
        /// <returns>An enumerator.</returns>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// An enumerator of <see cref="HierarchyNode"/>. Enumerates and filters items at the same time.
        /// </summary>
        public struct Enumerator : IEnumerator<HierarchyNode>
        {
            readonly HierarchyViewModel m_HierarchyViewModel;
            int m_Index;

            /// <summary>
            /// Gets the progress of an iteration as a percentage.
            /// </summary>
            public float Progress => m_HierarchyViewModel.Count == 0 ? 0 : (float)m_Index / m_HierarchyViewModel.Count;
            /// <summary>
            /// Gets the current item being enumerated.
            /// </summary>
            public HierarchyNode Current => m_HierarchyViewModel[m_Index];

            object IEnumerator.Current => Current;

            internal Enumerator(HierarchyViewModel hierarchyBaked)
            {
                m_HierarchyViewModel = hierarchyBaked;
                m_Index = -1;
            }

            [ExcludeFromDocs]
            public void Dispose() { }
            /// <summary>
            /// Moves to the next iterable value.
            /// </summary>
            /// <returns>Returns true if Current item is valid</returns>
            public bool MoveNext() => ++m_Index < m_HierarchyViewModel.Count;
            /// <summary>
            /// Checks if the iteration is done.
            /// </summary>
            /// <returns>Returns true if the iteration is done.</returns>
            public bool Done() => m_Index >= m_HierarchyViewModel.Count;
            /// <summary>
            /// Resets iteration to the beginning.
            /// </summary>
            public void Reset() => m_Index = -1;
        }

        // Note: called from native to avoid passing Query as a parameter
        [RequiredByNativeCode]
        internal void SearchBegin()
        {
            using var _ = ListPool<HierarchyNodeTypeHandlerBase>.Get(out var handlers);
            m_Hierarchy.GetAllNodeTypeHandlersBase(handlers);
            foreach (var handler in handlers)
                handler.SearchBegin(Query);
        }

        IEnumerator<HierarchyNode> IEnumerable<HierarchyNode>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [NativeThrows]
        extern HierarchyNode ElementAt(int index);
    }
}
