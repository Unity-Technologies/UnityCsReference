// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Hierarchy;

namespace Unity.UIToolkit.Editor;

// [TODO] MP: Convenience struct until APIs are added to retrieve the selection from the HierarchyView
internal readonly ref struct SelectionContext
{
    public enum SelectionType
    {
        None,
        Mixed,
        All
    }

    public SelectionContext(
        Span<HierarchyNode> selection,
        int selectionCount,
        SelectionType handlerType)
    {
        Selection = selection;
        SelectionCount = selectionCount;
        Type = handlerType;
    }

    public readonly Span<HierarchyNode> Selection;
    public readonly int SelectionCount;
    public readonly SelectionType Type;
}
