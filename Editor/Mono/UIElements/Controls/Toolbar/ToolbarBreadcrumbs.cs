// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Creates a breadcrumb UI element for the toolbar to help users navigate a hierarchy. For example, the visual scripting breadcrumb toolbar makes it easier to explore scripts because users can jump to any level of the script by clicking a breadcrumb item. For more information, refer to [[wiki:UIE-uxml-element-ToolbarBreadcrumbs|UXML element ToolbarBreadcrumbs]].
    /// </summary>
    /// <remarks>
    /// Represents a breadcrumb trail to facilitate navigation between related items in a hierarchy.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using UnityEngine;
    /// using UnityEngine.UIElements;
    /// using UnityEditor.UIElements;
    ///
    /// public class CreateBreadcrumbsHelper
    /// {
    ///     ToolbarBreadcrumbs breadcrumbs;
    ///     public void CreateBreadcrumbs(VisualElement root)
    ///     {
    ///         var toolbar = new Toolbar();
    ///         root.Add(toolbar);
    ///         breadcrumbs = new ToolbarBreadcrumbs();
    ///         toolbar.Add(breadcrumbs);
    ///         breadcrumbs.PushItem("myItemGrandParent", GoToRoot);
    ///         breadcrumbs.PushItem("myItemParent", () => breadcrumbs.PopItem());
    ///         breadcrumbs.PushItem("myItem");
    ///     }
    ///
    ///     void GoToRoot()
    ///     {
    ///         while (breadcrumbs.childCount > 1)
    ///             breadcrumbs.PopItem();
    ///    }
    /// }
    /// ]]>
    /// </code>
    /// </example>
    public class ToolbarBreadcrumbs : VisualElement
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new ToolbarBreadcrumbs();
        }

        /// <summary>
        /// Instantiates a <see cref="ToolbarBreadcrumbs"/> using the data read from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<ToolbarBreadcrumbs> {}

        /// <summary>
        /// A Unity style sheet (USS) class for the main ToolbarBreadcrumbs container.
        /// </summary>
        public static readonly string ussClassName = "unity-toolbar-breadcrumbs";

        /// <summary>
        /// A Unity style sheet (USS) class for individual items in a breadcrumb toolbar.
        /// </summary>
        public static readonly string itemClassName = ussClassName + "__item";
        /// <summary>
        /// A Unity style sheet (USS) class for the first element or item in a breadcrumb toolbar.
        /// </summary>
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

        /// <summary>
        /// Constructs a breadcrumb UI element for the toolbar to help users navigate a hierarchy.
        /// </summary>
        public ToolbarBreadcrumbs()
        {
            Toolbar.SetToolbarStyleSheet(this);
            AddToClassList(ussClassName);
        }

        /// <summary>
        /// Adds an item to the end of the breadcrumbs, which makes that item the deepest item in the hierarchy.
        /// </summary>
        /// <param name="label">The text to display for the item in the breadcrumb toolbar.</param>
        /// <param name="clickedEvent">The action to perform when the a users clicks the item in the toolbar.</param>
        public void PushItem(string label, Action clickedEvent = null)
        {
            BreadcrumbItem breadcrumbItem = new BreadcrumbItem(clickedEvent) { text = label };
            breadcrumbItem.EnableInClassList(firstItemClassName, childCount == 0);
            Add(breadcrumbItem);
        }

        /// <summary>
        /// Removes the last item in the breadcrumb toolbar, which is the deepest item in the hierarchy.
        /// </summary>
        public void PopItem()
        {
            if (Children().Any() && Children().Last() is BreadcrumbItem)
                RemoveAt(childCount - 1);
            else
                throw new InvalidOperationException("Last child isn't a BreadcrumbItem");
        }
    }
}
