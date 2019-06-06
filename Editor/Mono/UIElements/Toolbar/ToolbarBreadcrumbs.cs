// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public class ToolbarBreadcrumbs : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<ToolbarBreadcrumbs> {}

        public static readonly string ussClassName = "unity-toolbar-breadcrumbs";

        public static readonly string itemClassName = ussClassName + "__item";
        public static readonly string firstItemClassName = ussClassName + "__first-item";

        private class BreadcrumbItem : ToolbarButton
        {
            public BreadcrumbItem(Action clickEvent) :
                base(clickEvent)
            {
                Toolbar.SetToolbarStyleSheet(this);
                RemoveFromClassList(ussClassName);
                AddToClassList(itemClassName);
            }
        }

        public ToolbarBreadcrumbs()
        {
            Toolbar.SetToolbarStyleSheet(this);
            AddToClassList(ussClassName);
        }

        public void PushItem(string label, Action clickedEvent = null)
        {
            BreadcrumbItem breadcrumbItem = new BreadcrumbItem(clickedEvent) { text = label };
            breadcrumbItem.EnableInClassList(firstItemClassName, childCount == 0);
            Add(breadcrumbItem);
        }

        public void PopItem()
        {
            if (Children().Any() && Children().Last() is BreadcrumbItem)
                RemoveAt(childCount - 1);
            else
                throw new InvalidOperationException("Last child isn't a BreadcrumbItem");
        }
    }
}
