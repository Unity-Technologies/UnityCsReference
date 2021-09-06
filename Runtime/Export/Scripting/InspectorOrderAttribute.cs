// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Enum)]
    public sealed class InspectorOrderAttribute : PropertyAttribute
    {
        internal InspectorSort m_inspectorSort { get; private set; }
        internal InspectorSortDirection m_sortDirection { get; private set; }

        public InspectorOrderAttribute(InspectorSort inspectorSort = InspectorSort.ByName, InspectorSortDirection sortDirection = InspectorSortDirection.Ascending)
        {
            m_inspectorSort = inspectorSort;
            m_sortDirection = sortDirection;
        }
    }

    public enum InspectorSort
    {
        ByName,
        ByValue
    }

    public enum InspectorSortDirection
    {
        Ascending,
        Descending
    }
}
