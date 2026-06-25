// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.UIElements.StyleSheets;

[NativeHeader("Modules/UIElements/Core/Native/StyleSheets/NativeSelectorMatcher.h")]
internal static class NativeSelectorMatcher
{
    [FreeFunction("UIToolkit::NativeSelectorMatcher::MatchRightToLeftFlat", IsThreadSafe = true)]
    internal static extern unsafe bool MatchRightToLeftFlat(
        VisualElementSelectorData* element,
        FlattenedSelector* selectors,
        int selectorCount,
        FlattenedSelectorPart* parts,
        bool applyPseudoMasks);
}
