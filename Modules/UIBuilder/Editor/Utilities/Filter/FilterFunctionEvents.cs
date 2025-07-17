// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class FilterFunctionAddedEvent : EventBase<FilterFunctionAddedEvent>
    {
        public FilterFunction filterFunction;
    }

    class FilterFunctionRemovedEvent : EventBase<FilterFunctionRemovedEvent>
    {
        public List<int> indices;
    }

    class FilterFunctionChangedEvent : EventBase<FilterFunctionChangedEvent>
    {
        public FilterFunctionListViewItem item;
        public FilterFunction filterFunction;
        public int index;
    }

    class FilterFunctionValueChangedEvent : EventBase<FilterFunctionValueChangedEvent>
    {
        public FilterFunctionListViewItem item;
        public FilterFunction filterFunction;
        public int index;
        public int paramIndex;
    }

    class FilterFunctionReorderedEvent : EventBase<FilterFunctionReorderedEvent>
    {
        public int fromIndex;
        public int toIndex;
    }
}
