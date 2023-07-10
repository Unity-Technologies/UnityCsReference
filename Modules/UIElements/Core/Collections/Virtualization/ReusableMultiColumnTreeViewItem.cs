// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    class ReusableMultiColumnTreeViewItem : ReusableTreeViewItem
    {
        public override VisualElement rootElement => bindableElement;

        public override void Init(VisualElement item)
        {
            // Do nothing here, we are using the other Init.
        }

        public void Init(VisualElement container, Columns columns)
        {
            var i = 0;
            bindableElement = container;
            foreach (var column in columns.visibleList)
            {
                if (columns.IsPrimary(column))
                {
                    var cellContainer = container[i];
                    var cellItem = cellContainer.GetProperty(MultiColumnController.bindableElementPropertyName) as VisualElement;
                    InitExpandHierarchy(cellContainer, cellItem);
                    break;
                }

                i++;
            }
        }
    }
}
