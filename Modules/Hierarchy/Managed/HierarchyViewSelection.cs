// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Pool;
using UnityEngine.UIElements.HierarchyV2;

namespace Unity.Hierarchy
{
    class HierarchyViewSelection : ICollectionViewSelectionContainer
    {
        HierarchyViewModel m_HierarchyViewModel;

        public void SetSourceViewModel(HierarchyViewModel viewModel)
        {
            m_HierarchyViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        public int indexCount => m_HierarchyViewModel.HasFlagsCount(HierarchyNodeFlags.Selected);

        public int minIndex => m_HierarchyViewModel.GetFirstIndexWithFlags(HierarchyNodeFlags.Selected);

        public int maxIndex => m_HierarchyViewModel.GetLastIndexWithFlags(HierarchyNodeFlags.Selected);

        public int selectedIndex
        {
            get => minIndex;
            set => Select(stackalloc int[] { value });
        }

        public bool MatchesExistingSelection(ReadOnlySpan<int> selectedIndices)
        {
            var currentSelectionCount = m_HierarchyViewModel.HasFlagsCount(HierarchyNodeFlags.Selected);
            if (selectedIndices.Length != currentSelectionCount)
                return false;

            using var viewModelSelectedNodes = new RentSpanUnmanaged<int>(selectedIndices.Length);
            m_HierarchyViewModel.GetIndicesWithFlags(HierarchyNodeFlags.Selected, viewModelSelectedNodes.Span);

            return selectedIndices.SequenceEqual(viewModelSelectedNodes.Span);
        }

        public bool ContainsIndex(int index)
        {
            if (index < 0 || index >= m_HierarchyViewModel.Count)
                return false;

            ref readonly var node = ref m_HierarchyViewModel[index];
            return m_HierarchyViewModel.HasFlags(in node, HierarchyNodeFlags.Selected);
        }

        public void Add(int index)
        {
            if (index < 0 || index >= m_HierarchyViewModel.Count)
                return;

            m_HierarchyViewModel.SetFlags(stackalloc int[] { index }, HierarchyNodeFlags.Selected);
        }

        public void AddRange(ReadOnlySpan<int> newIndices)
        {
            m_HierarchyViewModel.SetFlags(newIndices, HierarchyNodeFlags.Selected);
        }

        public void Remove(int index)
        {
            m_HierarchyViewModel.ClearFlags(stackalloc int[] { index }, HierarchyNodeFlags.Selected);
        }

        public void Clear()
        {
            m_HierarchyViewModel.ClearFlags(HierarchyNodeFlags.Selected);
        }

        public void SelectAll()
        {
            m_HierarchyViewModel.SetFlags(HierarchyNodeFlags.Selected);
        }

        public void Select(ReadOnlySpan<int> indices)
        {
            using var _ = new HierarchyViewModelFlagsChangeScope(m_HierarchyViewModel);
            m_HierarchyViewModel.ClearFlags(HierarchyNodeFlags.Selected);
            m_HierarchyViewModel.SetFlags(indices, HierarchyNodeFlags.Selected);
        }
    }
}
