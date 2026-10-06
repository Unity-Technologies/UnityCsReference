// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    // Shared shift-click/range selection arithmetic, used by InternalEditorUtility.HandleMultiSelection
    // and the UI Toolkit collection views so both resolve ranges with the same semantics.
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal static class RangeSelectionHelper
    {
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal readonly struct Result
        {
            [VisibleToOtherModules("UnityEngine.UIElementsModule")]
            public readonly bool unchanged;
            [VisibleToOtherModules("UnityEngine.UIElementsModule")]
            public readonly bool addToExisting;
            [VisibleToOtherModules("UnityEngine.UIElementsModule")]
            public readonly int from;
            [VisibleToOtherModules("UnityEngine.UIElementsModule")]
            public readonly int to;

            public Result(bool unchanged, bool addToExisting, int from, int to)
            {
                this.unchanged = unchanged;
                this.addToExisting = addToExisting;
                this.from = from;
                this.to = to;
            }
        }

        // Computes the inclusive index range [from, to] selected by a range-selection on clickedIndex.
        // anchorIndex is the index of the last clicked item, or -1 when unknown; firstIndex/lastIndex
        // are the current selection's min/max indices; when addToExisting is false the range replaces
        // the selection, otherwise it is added to it. forceBoundsFallback reproduces arrow-key
        // navigation, which always ranges over the selection bounds and never adds.
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static Result ComputeRangeSelection(int clickedIndex, int anchorIndex, int firstIndex,
            int lastIndex, int selectedCount, bool clickedIndexIsSelected, bool forceBoundsFallback = false)
        {
            if (selectedCount <= 0)
                return new Result(false, false, clickedIndex, clickedIndex);

            if (anchorIndex != -1 && clickedIndex == anchorIndex)
                return new Result(true, false, clickedIndex, clickedIndex);

            var dir = 0;
            if (anchorIndex != -1)
                dir = clickedIndex > anchorIndex ? 1 : -1;

            var clickedInTheMiddle = lastIndex > clickedIndex && firstIndex < clickedIndex;
            var from = 0;
            var to = 0;
            var addToExisting = false;

            if (selectedCount > 1)
            {
                if (clickedIndexIsSelected || clickedInTheMiddle)
                {
                    // Range from the selection end behind the direction of travel to the clicked item,
                    // e.g. select item 1, shift-select item 5, then shift-select item 3 -> items 1 to 3.
                    from = dir > 0 ? firstIndex : clickedIndex;
                    to = dir > 0 ? clickedIndex : lastIndex;

                    if (clickedInTheMiddle && !clickedIndexIsSelected)
                        addToExisting = true;
                }
                else if (dir > 0)
                {
                    if (clickedIndex > lastIndex)
                    {
                        from = lastIndex + 1;
                        to = clickedIndex;
                        addToExisting = true;
                    }
                    else
                    {
                        from = clickedIndex;
                        to = lastIndex;
                    }
                }
                else if (dir < 0)
                {
                    if (clickedIndex < firstIndex)
                    {
                        from = clickedIndex;
                        to = firstIndex - 1;
                        addToExisting = true;
                    }
                    else
                    {
                        from = firstIndex;
                        to = clickedIndex;
                    }
                }
            }

            if (!addToExisting || forceBoundsFallback)
            {
                if (clickedIndex > lastIndex)
                {
                    from = firstIndex;
                    to = clickedIndex;
                }
                else if (clickedIndex >= firstIndex && clickedIndex < lastIndex)
                {
                    if (dir > 0)
                    {
                        from = clickedIndex;
                        to = lastIndex;
                    }
                    else
                    {
                        from = firstIndex;
                        to = clickedIndex;
                    }
                }
                else
                {
                    from = clickedIndex;
                    to = lastIndex;
                }
            }

            return new Result(false, addToExisting && !forceBoundsFallback, from, to);
        }
    }
}
