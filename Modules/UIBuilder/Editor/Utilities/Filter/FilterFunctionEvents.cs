// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class FilterListChangedEvent : EventBase<FilterListChangedEvent>
    {
        public List<FilterFunction> newFilterList;
        public bool refreshField;
    }

    class FilterFunctionReorderedEvent : EventBase<FilterFunctionReorderedEvent>
    {
        public int fromIndex;
        public int toIndex;
    }
}
