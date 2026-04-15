// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements;

// Helps associate a selector within a lookup table that merges data from an "owner" style sheet with other style sheets
// imported into it, while preserving data related to sorting matching selectors
struct StyleSelectorLookupEntry(StyleComplexSelector selector, int importedStyleSheetIndex)
{
    // The selector, which may be from the style sheet which owns the table or any style sheet imported into it
    public readonly StyleComplexSelector selector = selector;

    // -1 means it's from the owner style sheet itself
    // other this is value is the index of the source style sheet inside the flattenedRecursiveImports list
    public readonly int importedStyleSheetIndex = importedStyleSheetIndex;
}
