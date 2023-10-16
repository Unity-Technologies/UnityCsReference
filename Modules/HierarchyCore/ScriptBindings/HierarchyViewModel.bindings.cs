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
        /// Gets the number of child nodes that a hierarchy node has, including children of children.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The number of child nodes, including children of children.</returns>
        [NativeThrows]
        public extern int GetChildrenCountRecursive(in HierarchyNode node);

        /// <summary>
        /// Determines the depth of a node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The depth of the hierarchy node.</returns>
        [NativeThrows]
        public extern int GetDepth(in HierarchyNode node);

        /// <summary>
        /// Sets the specified flags on all hierarchy nodes.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        public void SetFlags(HierarchyNodeFlags flags) => SetFlagsAll(flags);

        /// <summary>
        /// Sets the specified flags on the hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="recurse">Whether or not to set the flags on all children recursively for that hierarchy node.</param>
        public void SetFlags(in HierarchyNode node, HierarchyNodeFlags flags, bool recurse = false) => SetFlagsNode(in node, flags, recurse);

        /// <summary>
        /// Sets the specified flags on the hierarchy nodes.
        /// </summary>
        /// <remarks>
        /// Null or invalid nodes are ignored.
        /// </remarks>
        /// <param name="nodes">The hierarchy nodes.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The number of nodes that had their flags set.</returns>
        public int SetFlags(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags) => SetFlagsNodes(nodes, flags);

        /// <summary>
        /// Sets the specified flags on the hierarchy node indices.
        /// </summary>
        /// <remarks>
        /// Invalid node indices are ignored.
        /// </remarks>
        /// <param name="indices">The hierarchy node indices.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The number of nodes that had their flags set.</returns>
        public int SetFlags(ReadOnlySpan<int> indices, HierarchyNodeFlags flags) => SetFlagsIndices(indices, flags);

        /// <summary>
        /// Gets whether or not the specified flags is set on any hierarchy node.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns><see langword="true"/> if any node have the flags set, otherwise <see langword="false"/>.</returns>
        public bool HasFlags(HierarchyNodeFlags flags) => HasFlagsAny(flags);

        /// <summary>
        /// Gets whether or not the specified flags is set on the hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns><see langword="true"/> if the flags is set, otherwise <see langword="false"/>.</returns>
        public bool HasFlags(in HierarchyNode node, HierarchyNodeFlags flags) => HasFlagsNode(in node, flags);

        /// <summary>
        /// Gets the number of nodes that have the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The number of nodes that have the flags set.</returns>
        public extern int HasFlagsCount(HierarchyNodeFlags flags);

        /// <summary>
        /// Gets whether or not the specified flags is not set on any hierarchy node.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns><see langword="true"/> if any node have the flags set, otherwise <see langword="false"/>.</returns>
        public bool DoesNotHaveFlags(HierarchyNodeFlags flags) => DoesNotHaveFlagsAny(flags);

        /// <summary>
        /// Gets whether or not the specified flags is not set on the hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns><see langword="true"/> if the flags is not set, otherwise <see langword="false"/>.</returns>
        public bool DoesNotHaveFlags(in HierarchyNode node, HierarchyNodeFlags flags) => DoesNotHaveFlagsNode(in node, flags);

        /// <summary>
        /// Gets the number of nodes that does not have the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The number of nodes that does not have the flags set.</returns>
        public extern int DoesNotHaveFlagsCount(HierarchyNodeFlags flags);

        /// <summary>
        /// Clears the specified flags on all hierarchy nodes.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        public void ClearFlags(HierarchyNodeFlags flags) => ClearFlagsAll(flags);

        /// <summary>
        /// Clears the specified flags on the hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="recurse">Whether or not to clear the flags on all children recursively for that hierarchy node.</param>
        public void ClearFlags(in HierarchyNode node, HierarchyNodeFlags flags, bool recurse = false) => ClearFlagsNode(in node, flags, recurse);

        /// <summary>
        /// Clears the specified flags on the hierarchy nodes.
        /// </summary>
        /// <remarks>
        /// Null or invalid nodes are ignored.
        /// </remarks>
        /// <param name="nodes">The hierarchy nodes.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The number of nodes that had their flags cleared.</returns>
        public int ClearFlags(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags) => ClearFlagsNodes(nodes, flags);

        /// <summary>
        /// Clears the specified flags on the hierarchy node indices.
        /// </summary>
        /// <remarks>
        /// Invalid node indices are ignored.
        /// </remarks>
        /// <param name="indices">The hierarchy node indices.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The number of nodes that had their flags cleared.</returns>
        public int ClearFlags(ReadOnlySpan<int> indices, HierarchyNodeFlags flags) => ClearFlagsIndices(indices, flags);

        /// <summary>
        /// Toggles the specified flags on all hierarchy nodes.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        public void ToggleFlags(HierarchyNodeFlags flags) => ToggleFlagsAll(flags);

        /// <summary>
        /// Toggles the specified flags on the hierarchy node.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="recurse">Whether or not to clear the flags on all children recursively for that hierarchy node.</param>
        public void ToggleFlags(in HierarchyNode node, HierarchyNodeFlags flags, bool recurse = false) => ToggleFlagsNode(in node, flags, recurse);

        /// <summary>
        /// Toggles the specified flags on the hierarchy nodes.
        /// </summary>
        /// <remarks>
        /// Null or invalid nodes are ignored.
        /// </remarks>
        /// <param name="nodes">The hierarchy nodes.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The number of nodes that had their flags cleared.</returns>
        public int ToggleFlags(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags) => ToggleFlagsNodes(nodes, flags);

        /// <summary>
        /// Toggles the specified flags on the hierarchy node indices.
        /// </summary>
        /// <remarks>
        /// Invalid node indices are ignored.
        /// </remarks>
        /// <param name="indices">The hierarchy node indices.</param>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The number of nodes that had their flags cleared.</returns>
        public int ToggleFlags(ReadOnlySpan<int> indices, HierarchyNodeFlags flags) => ToggleFlagsIndices(indices, flags);

        /// <summary>
        /// Gets all hierarchy nodes that have the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="outNodes">The hierarchy nodes.</param>
        /// <returns>The number of nodes written in the <paramref name="outNodes"/> span.</returns>
        public int GetNodesWithFlags(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes) => GetNodesWithFlagsSpan(flags, outNodes);

        /// <summary>
        /// Gets all hierarchy nodes that have the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The hierarchy nodes.</returns>
        public HierarchyNode[] GetNodesWithFlags(HierarchyNodeFlags flags)
        {
            var count = HasFlagsCount(flags);
            var nodes = new HierarchyNode[count];
            GetNodesWithFlagsSpan(flags, nodes);
            return nodes;
        }

        /// <summary>
        /// Gets an enumerable of all hierarchy nodes that have the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>An enumerable of hierarchy node.</returns>
        public HierarchyViewNodesEnumerable EnumerateNodesWithFlags(HierarchyNodeFlags flags) => new HierarchyViewNodesEnumerable(this, flags, HasFlags);

        /// <summary>
        /// Gets all hierarchy node indices that have the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="outIndices">The hierarchy node indices.</param>
        /// <returns>The number of indices written in the <paramref name="outIndices"/> span.</returns>
        public int GetIndicesWithFlags(HierarchyNodeFlags flags, Span<int> outIndices) => GetIndicesWithFlagsSpan(flags, outIndices);

        /// <summary>
        /// Gets all hierarchy node indices that have the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The hierarchy node indices.</returns>
        public int[] GetIndicesWithFlags(HierarchyNodeFlags flags)
        {
            var count = HasFlagsCount(flags);
            var indices = new int[count];
            GetIndicesWithFlagsSpan(flags, indices);
            return indices;
        }

        /// <summary>
        /// Gets all hierarchy nodes that does not have the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="outNodes">The hierarchy nodes.</param>
        /// <returns>The number of nodes written in the <paramref name="outNodes"/> span.</returns>
        public int GetNodesWithoutFlags(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes) => GetNodesWithoutFlagsSpan(flags, outNodes);

        /// <summary>
        /// Gets all hierarchy nodes that does not have the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The hierarchy nodes.</returns>
        public HierarchyNode[] GetNodesWithoutFlags(HierarchyNodeFlags flags)
        {
            var count = DoesNotHaveFlagsCount(flags);
            var nodes = new HierarchyNode[count];
            GetNodesWithoutFlagsSpan(flags, nodes);
            return nodes;
        }

        /// <summary>
        /// Gets an enumerable of all hierarchy nodes that does not have the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>An enumerable of hierarchy node.</returns>
        public HierarchyViewNodesEnumerable EnumerateNodesWithoutFlags(HierarchyNodeFlags flags) => new HierarchyViewNodesEnumerable(this, flags, DoesNotHaveFlags);

        /// <summary>
        /// Gets all hierarchy node indices that does not have the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <param name="outIndices">The hierarchy node indices.</param>
        /// <returns>The number of indices written in the <paramref name="outIndices"/> span.</returns>
        public int GetIndicesWithoutFlags(HierarchyNodeFlags flags, Span<int> outIndices) => GetIndicesWithoutFlagsSpan(flags, outIndices);

        /// <summary>
        /// Gets all hierarchy node indices that does not have the specified flags set.
        /// </summary>
        /// <param name="flags">The hierarchy node flags.</param>
        /// <returns>The hierarchy node indices.</returns>
        public int[] GetIndicesWithoutFlags(HierarchyNodeFlags flags)
        {
            var count = DoesNotHaveFlagsCount(flags);
            var indices = new int[count];
            GetIndicesWithoutFlagsSpan(flags, indices);
            return indices;
        }

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
            /// Get the current item being enumerated.
            /// </summary>
            public HierarchyNode Current => m_HierarchyViewModel[m_Index];

            object IEnumerator.Current => Current;

            internal Enumerator(HierarchyViewModel hierarchyViewModel)
            {
                m_HierarchyViewModel = hierarchyViewModel;
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
            /// Reset iteration at the beginning.
            /// </summary>
            public void Reset() => m_Index = -1;

            /// <summary>
            /// Check if iteration is done.
            /// </summary>
            /// <returns>Returns true if iteration is done.</returns>
            public bool Done() => m_Index >= m_HierarchyViewModel.Count;
        }

        // Note: called from native to avoid passing Query as a parameter
        [RequiredByNativeCode]
        internal void SearchBegin()
        {
            using var _ = ListPool<HierarchyNodeTypeHandlerBase>.Get(out var handlers);
            m_Hierarchy.GetAllNodeTypeHandlersBase(handlers);
            foreach (var handler in handlers)
                handler.Internal_SearchBegin(Query);
        }

        IEnumerator<HierarchyNode> IEnumerable<HierarchyNode>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [NativeThrows]
        extern HierarchyNode ElementAt(int index);

        [NativeThrows, FreeFunction("HierarchyViewModelBindings::SetFlagsAll", HasExplicitThis = true)]
        extern void SetFlagsAll(HierarchyNodeFlags flags);

        [NativeThrows, FreeFunction("HierarchyViewModelBindings::SetFlagsNode", HasExplicitThis = true)]
        extern void SetFlagsNode(in HierarchyNode node, HierarchyNodeFlags flags, bool recurse = false);

        [FreeFunction("HierarchyViewModelBindings::SetFlagsNodes", HasExplicitThis = true)]
        extern int SetFlagsNodes(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::SetFlagsIndices", HasExplicitThis = true)]
        extern int SetFlagsIndices(ReadOnlySpan<int> indices, HierarchyNodeFlags flags);

        [NativeThrows, FreeFunction("HierarchyViewModelBindings::HasFlagsAny", HasExplicitThis = true)]
        extern bool HasFlagsAny(HierarchyNodeFlags flags);

        [NativeThrows, FreeFunction("HierarchyViewModelBindings::HasFlagsNode", HasExplicitThis = true)]
        extern bool HasFlagsNode(in HierarchyNode node, HierarchyNodeFlags flags);

        [NativeThrows, FreeFunction("HierarchyViewModelBindings::DoesNotHaveFlagsAny", HasExplicitThis = true)]
        extern bool DoesNotHaveFlagsAny(HierarchyNodeFlags flags);

        [NativeThrows, FreeFunction("HierarchyViewModelBindings::DoesNotHaveFlagsNode", HasExplicitThis = true)]
        extern bool DoesNotHaveFlagsNode(in HierarchyNode node, HierarchyNodeFlags flags);

        [NativeThrows, FreeFunction("HierarchyViewModelBindings::ClearFlagsAll", HasExplicitThis = true)]
        extern void ClearFlagsAll(HierarchyNodeFlags flags);

        [NativeThrows, FreeFunction("HierarchyViewModelBindings::ClearFlagsNode", HasExplicitThis = true)]
        extern void ClearFlagsNode(in HierarchyNode node, HierarchyNodeFlags flags, bool recurse = false);

        [FreeFunction("HierarchyViewModelBindings::ClearFlagsNodes", HasExplicitThis = true)]
        extern int ClearFlagsNodes(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::ClearFlagsIndices", HasExplicitThis = true)]
        extern int ClearFlagsIndices(ReadOnlySpan<int> indices, HierarchyNodeFlags flags);

        [NativeThrows, FreeFunction("HierarchyViewModelBindings::ToggleFlagsAll", HasExplicitThis = true)]
        extern void ToggleFlagsAll(HierarchyNodeFlags flags);

        [NativeThrows, FreeFunction("HierarchyViewModelBindings::ToggleFlagsNode", HasExplicitThis = true)]
        extern void ToggleFlagsNode(in HierarchyNode node, HierarchyNodeFlags flags, bool recurse = false);

        [FreeFunction("HierarchyViewModelBindings::ToggleFlagsNodes", HasExplicitThis = true)]
        extern int ToggleFlagsNodes(ReadOnlySpan<HierarchyNode> nodes, HierarchyNodeFlags flags);

        [FreeFunction("HierarchyViewModelBindings::ToggleFlagsIndices", HasExplicitThis = true)]
        extern int ToggleFlagsIndices(ReadOnlySpan<int> indices, HierarchyNodeFlags flags);

        [NativeThrows, FreeFunction("HierarchyViewModelBindings::GetNodesWithFlagsSpan", HasExplicitThis = true)]
        extern int GetNodesWithFlagsSpan(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes);

        [NativeThrows, FreeFunction("HierarchyViewModelBindings::GetIndicesWithFlagsSpan", HasExplicitThis = true)]
        extern int GetIndicesWithFlagsSpan(HierarchyNodeFlags flags, Span<int> outIndices);

        [NativeThrows, FreeFunction("HierarchyViewModelBindings::GetNodesWithoutFlagsSpan", HasExplicitThis = true)]
        extern int GetNodesWithoutFlagsSpan(HierarchyNodeFlags flags, Span<HierarchyNode> outNodes);

        [NativeThrows, FreeFunction("HierarchyViewModelBindings::GetIndicesWithoutFlagsSpan", HasExplicitThis = true)]
        extern int GetIndicesWithoutFlagsSpan(HierarchyNodeFlags flags, Span<int> outIndices);

        #region Obsolete code to remove in 2024.1
        [Obsolete("GetExpanded will be removed, please use DoesNotHaveFlags(in HierarchyNode node, HierarchyNodeFlags.Collapsed) instead.")]
        public bool GetExpanded(in HierarchyNode node) => DoesNotHaveFlagsNode(in node, HierarchyNodeFlags.Collapsed);

        [Obsolete("GetSelected will be removed, please use HasFlags(in HierarchyNode node, HierarchyNodeFlags.Selected) instead.")]
        public bool GetSelected(in HierarchyNode node) => HasFlagsNode(in node, HierarchyNodeFlags.Selected);

        [Obsolete("SetExpanded will be removed, please use ClearFlags(in HierarchyNode node, HierarchyNodeFlags.Collapsed) instead.")]
        public void SetExpanded(in HierarchyNode node, bool expanded) => ClearFlagsNode(in node, HierarchyNodeFlags.Collapsed);

        [Obsolete("SetSelected will be removed, please use SetFlags(in HierarchyNode node, HierarchyNodeFlags.Selected) instead.")]
        public void SetSelected(in HierarchyNode node, bool selected) => SetFlagsNode(in node, HierarchyNodeFlags.Selected, selected);
        #endregion
    }
}
