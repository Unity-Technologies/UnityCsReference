// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal enum StyleSelectorType
    {
        Unknown,
        Wildcard,
        Type,
        Class,
        PseudoClass,
        RecursivePseudoClass,
        ID,
        Predicate
    }
}
