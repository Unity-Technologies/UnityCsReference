// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace Unity.Hierarchy
{
    [NativeType(Header = "Modules/HierarchyCore/HierarchyProperties.h")]
    static class HierarchyProperties
    {
        [FreeFunction("HierarchyProperties::GetItemClassListPropertyId")]
        static extern HierarchyPropertyId GetItemClassListPropertyId(Hierarchy hierarchy);

        public static HierarchyPropertyString GetItemClassListProperty(Hierarchy hierarchy)
            => new(hierarchy, GetItemClassListPropertyId(hierarchy));
    }
}
