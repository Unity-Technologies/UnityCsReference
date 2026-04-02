// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class FilterListChangedEvent : EventBase<FilterListChangedEvent>
    {
        public List<FilterFunction> newFilterList;
        public bool refreshField;
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class FilterFunctionReorderedEvent : EventBase<FilterFunctionReorderedEvent>
    {
        public int fromIndex;
        public int toIndex;
    }
}
