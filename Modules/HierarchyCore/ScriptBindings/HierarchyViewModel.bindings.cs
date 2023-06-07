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
    /// A read-only array of <see cref="HierarchyNode"/>, from a filtered view over an <see cref="HierarchyFlattened"/>.
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
        /// Whether or not this object is still valid and uses memory.
        /// </summary>
        public bool IsCreated => m_Ptr != IntPtr.Zero;

        /// <summary>
        /// The total number of nodes.
        /// </summary>
        /// <remarks>
        /// Does not include the <see cref="Hierarchy.Root"/> node.
        /// </remarks>
        public extern int Count { [NativeMethod("Count")] get; }

        /// <summary>
        /// Whether the hierarchy view model is currently updating.
        /// </summary>
        /// <remarks>
        /// Happens during use of <see cref="UpdateIncremental"/> or <see cref="UpdateIncrementalTimed"/>.
        /// </remarks>
        public extern bool Updating { [NativeMethod("Updating")] get; }

        /// <summary>
        /// Whether the hierarchy view model requires an update.
        /// </summary>
        /// <remarks>
        /// Happens when the underlying hierarchy changes topology.
        /// </remarks>
        public extern bool UpdateNeeded { [NativeMethod("UpdateNeeded")] get; }

        /// <summary>
        /// Access the HierarchyFlattened
        /// </summary>
        public HierarchyFlattened HierarchyFlattened => m_HierarchyFlattened;

        /// <summary>
        /// Access the Hierarchy
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
        /// Create a new HierarchyViewModel from a HierarchyFlattened.
        /// </summary>
        /// <param name="hierarchyFlattened">Flattened hierarchy that serve as the hierarchy model.</param>
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
        /// Dispose this object, releasing its memory.
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
        /// Retrieve the <see cref="HierarchyNode"/> at the specified index.
        /// </summary>
        /// <param name="index">The node index.</param>
        /// <returns>An hierarchy node.</returns>
        public HierarchyNode this[int index] => ElementAt(index);

        /// <summary>
        /// Returns the zero-based index of the specified node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>A zero-based index of the node if found, -1 otherwise.</returns>
        [NativeThrows]
        public extern int IndexOf(in HierarchyNode node);

        /// <summary>
        /// Determine if the specified node is found in the hierarchy view model.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns><see langword="true"/> if the node is found, <see langword="false"/> otherwise.</returns>
        [NativeThrows]
        public extern bool Contains(in HierarchyNode node);

        /// <summary>
        /// Retrieve the parent of an hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>An hierarchy node.</returns>
        [NativeThrows]
        public extern HierarchyNode GetParent(in HierarchyNode node);

        /// <summary>
        /// Retrieve the next sibling of a node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>An hierarchy node.</returns>
        [NativeThrows]
        public extern HierarchyNode GetNextSibling(in HierarchyNode node);

        /// <summary>
        /// Retrieve the number of children of an hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The number of children.</returns>
        [NativeThrows]
        public extern int GetChildrenCount(in HierarchyNode node);

        /// <summary>
        /// Determine the depth of a node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The depth of the hierarchy node.</returns>
        [NativeThrows]
        public extern int GetDepth(in HierarchyNode node);

        /// <summary>
        /// Set the expanded state of a node.
        /// </summary>
        /// <remarks>
        /// Nodes are expanded by default.
        /// </remarks>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="expanded">The expanded state.</param>
        [NativeThrows]
        public extern void SetExpanded(in HierarchyNode node, bool expanded);

        /// <summary>
        /// Get the expanded state of a node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns><see langword="true"/> if the node is expanded, <see langword="false"/> otherwise.</returns>
        [NativeThrows]
        public extern bool GetExpanded(in HierarchyNode node);

        /// <summary>
        /// Set the selected state of a node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="selected">The selected state.</param>
        [NativeThrows]
        public extern void SetSelected(in HierarchyNode node, bool selected);

        /// <summary>
        /// Get the selected state of a node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns><see langword="true"/> if the node is selected, <see langword="false"/> otherwise.</returns>
        [NativeThrows]
        public extern bool GetSelected(in HierarchyNode node);

        /// <summary>
        /// Set the search query.
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
        /// Update the hierarchy view model, requesting to rebuild the list of <see cref="HierarchyNode"/> filtering the <see cref="HierarchyFlattened"/>.
        /// </summary>
        public extern void Update();

        /// <summary>
        /// Incrementally update the hierarchy view model.
        /// </summary>
        /// <returns><see langword="true"/> if additional invocations are needed to complete the update, <see langword="false"/> otherwise.</returns>
        public extern bool UpdateIncremental();

        /// <summary>
        /// Incrementally update the hierarchy view model until the time limit is reached.
        /// </summary>
        /// <param name="milliseconds">Time limit in milliseconds.</param>
        /// <returns><see langword="true"/> if additional invocations are needed to complete the update, <see langword="false"/> otherwise.</returns>
        public extern bool UpdateIncrementalTimed(double milliseconds);

        /// <summary>
        /// Get the <see cref="HierarchyNode"/> enumerator.
        /// </summary>
        /// <returns>An enumerator.</returns>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// And enumerator of <see cref="HierarchyNode"/>. This enumerate and filter items at the same time.
        /// </summary>
        public struct Enumerator : IEnumerator<HierarchyNode>
        {
            readonly HierarchyViewModel m_HierarchyViewModel;
            int m_Index;

            /// <summary>
            /// Get progress of iteration as a percentage.
            /// </summary>
            public float Progress => m_HierarchyViewModel.Count == 0 ? 0 : (float)m_Index / m_HierarchyViewModel.Count;
            /// <summary>
            /// Get the current item being enumerated.
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
            /// Move to next iterable value.
            /// </summary>
            /// <returns>Returns true if Current item is valid</returns>
            public bool MoveNext() => ++m_Index < m_HierarchyViewModel.Count;
            /// <summary>
            /// Check if iteration is done.
            /// </summary>
            /// <returns>Returns true if iteration is done.</returns>
            public bool Done() => m_Index >= m_HierarchyViewModel.Count;
            /// <summary>
            /// Reset iteration at the beginning.
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
