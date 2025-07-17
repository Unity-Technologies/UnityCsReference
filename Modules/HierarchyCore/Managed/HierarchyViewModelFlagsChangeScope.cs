// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Hierarchy
{
    /// <summary>
    /// Auto dispose struct for HierarchyViewModel flags change scope.
    /// </summary>
    public ref struct HierarchyViewModelFlagsChangeScope
    {
        readonly HierarchyViewModel m_HierarchyViewModel;

        /// <summary>
        /// Begins the flags change scope.
        /// </summary>
        /// <param name="hierarchyViewModel">The hierarchy view model.</param>
        public HierarchyViewModelFlagsChangeScope(HierarchyViewModel hierarchyViewModel)
        {
            m_HierarchyViewModel = hierarchyViewModel;
            m_HierarchyViewModel.BeginFlagsChange();
        }

        /// <summary>
        /// Ends the flags change scope.
        /// </summary>
        public void Dispose()
        {
            if (m_HierarchyViewModel != null && m_HierarchyViewModel.IsCreated)
                m_HierarchyViewModel.EndFlagsChange();
        }
    }
}
