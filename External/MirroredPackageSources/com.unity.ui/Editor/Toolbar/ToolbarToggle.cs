using System;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public class ToolbarToggle : Toggle
    {
        public new class UxmlFactory : UxmlFactory<ToolbarToggle, UxmlTraits> {}
        public new class UxmlTraits : Toggle.UxmlTraits {}

        public new static readonly string ussClassName = "unity-toolbar-toggle";

        public ToolbarToggle()
        {
            Toolbar.SetToolbarStyleSheet(this);
            RemoveFromClassList(Toggle.ussClassName);
            AddToClassList(ussClassName);
        }
    }
}
