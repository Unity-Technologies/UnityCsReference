// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
using System;

namespace UnityEngine.UIElements.Internal
{
    /// <summary>
    /// Handle to resize columns interactively.
    /// </summary>
    class MultiColumnHeaderColumnResizeHandle : VisualElement
    {
        public static readonly string ussClassName = MultiColumnCollectionHeader.ussClassName + "__column-resize-handle";
        internal static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        public static readonly string dragAreaUssClassName = ussClassName + "__drag-area";
        internal static readonly UniqueStyleString dragAreaUssClassNameUnique = new(dragAreaUssClassName);

        public VisualElement dragArea { get; }

        public MultiColumnHeaderColumnResizeHandle()
        {
            AddToClassList(ussClassNameUnique);

            dragArea = new VisualElement() { focusable = true, tabIndex = -1  };
            dragArea.AddToClassList(dragAreaUssClassNameUnique);
            Add(dragArea);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
