// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.Hierarchy.Editor
{
    /// <summary>
    /// Interface that <see cref="HierarchyNodeTypeHandler"/> should implement to be able to populate the create menu in the Hierarchy.
    /// </summary>
    internal interface IHierarchyExtendCreateMenu
    {
        /// <summary>
        /// Method use to populate the create menu in the Hierarchy.
        /// </summary>
        /// <param name="menu">The <see cref="DropdownMenu"/> to add items to.</param>
        void PopulateCreateMenu(DropdownMenu menu);
    }
}
