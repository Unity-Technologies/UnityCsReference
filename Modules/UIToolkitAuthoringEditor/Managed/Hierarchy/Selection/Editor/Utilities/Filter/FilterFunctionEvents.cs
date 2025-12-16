// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class FilterFunctionAddedEvent : EventBase<FilterFunctionAddedEvent>
    {
        public FilterFunction filterFunction;
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class FilterFunctionRemovedEvent : EventBase<FilterFunctionRemovedEvent>
    {
        public List<int> indices;
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class FilterFunctionChangedEvent : EventBase<FilterFunctionChangedEvent>
    {
        public FilterFunctionListViewItem item;
        public FilterFunction filterFunction;
        public int index;
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class FilterFunctionValueChangedEvent : EventBase<FilterFunctionValueChangedEvent>
    {
        public FilterFunctionListViewItem item;
        public FilterFunction filterFunction;
        public int index;
        public int paramIndex;
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class FilterFunctionReorderedEvent : EventBase<FilterFunctionReorderedEvent>
    {
        public int fromIndex;
        public int toIndex;
    }
}
