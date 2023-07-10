// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements.Internal
{
    /// <summary>
    /// Handle to resize columns interactively.
    /// </summary>
    class MultiColumnHeaderColumnResizeHandle : VisualElement
    {
        public static readonly string ussClassName = MultiColumnCollectionHeader.ussClassName + "__column-resize-handle";
        public static readonly string dragAreaUssClassName = ussClassName + "__drag-area";

        public VisualElement dragArea { get; }

        public MultiColumnHeaderColumnResizeHandle()
        {
            AddToClassList(ussClassName);

            dragArea = new VisualElement() { focusable = true };
            dragArea.AddToClassList(dragAreaUssClassName);
            Add(dragArea);
        }
    }
}
