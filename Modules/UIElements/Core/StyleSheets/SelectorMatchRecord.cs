// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements.StyleSheets
{
    // Each struct represents on match for a visual element against a complex
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal readonly struct SelectorMatchRecord : IEquatable<SelectorMatchRecord>
    {
        public readonly StyleSheet sheet;
        public readonly int styleSheetIndexInStack;
        public readonly int importedStyleSheetIndex;
        public readonly StyleComplexSelector complexSelector;

        // Effective tier of this match, resolved per use (an imported rule inherits the tier of
        // the stack entry that pulled it in; see StyleSelectorMatch). Derived data: not part of
        // equality.
        public readonly StyleSheetPriority tier;

        public SelectorMatchRecord(StyleSheet sheet, int styleSheetIndexInStack, int importedStyleSheetIndex, StyleComplexSelector complexSelector)
        {
            this.sheet = sheet;
            this.styleSheetIndexInStack = styleSheetIndexInStack;
            this.importedStyleSheetIndex = importedStyleSheetIndex;
            this.complexSelector = complexSelector;

            // No stack context here (UI Builder and test paths): the sheet's own tier is the
            // best available answer.
            tier = sheet != null ? sheet.priority : StyleSheetPriority.Default;
        }

        // Copies fields out of the hot-path representation so the rest of the system can keep
        // using SelectorMatchRecord safely once StyleSelectorMatch becomes unsafe.
        public SelectorMatchRecord(in StyleSelectorMatch src)
        {
            this.sheet = src.sheet;
            this.styleSheetIndexInStack = src.styleSheetIndexInStack;
            this.importedStyleSheetIndex = src.importedStyleSheetIndex;
            this.complexSelector = src.complexSelector;
            this.tier = src.tier;
        }

        public bool Equals(SelectorMatchRecord other)
        {
            return Equals(sheet, other.sheet)
                   && styleSheetIndexInStack == other.styleSheetIndexInStack
                   && importedStyleSheetIndex == other.importedStyleSheetIndex
                   && Equals(complexSelector, other.complexSelector);
        }

        public override bool Equals(object obj)
        {
            return obj is SelectorMatchRecord other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(sheet, styleSheetIndexInStack, importedStyleSheetIndex, complexSelector);
        }
    }
}
