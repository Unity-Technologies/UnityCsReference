// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public class ToolbarButton : Button
    {
        public new class UxmlFactory : UxmlFactory<ToolbarButton, UxmlTraits> {}
        public new class UxmlTraits : Button.UxmlTraits {}

        public new static readonly string ussClassName = "unity-toolbar-button";

        public ToolbarButton(Action clickEvent) :
            base(clickEvent)
        {
            Toolbar.SetToolbarStyleSheet(this);
            RemoveFromClassList(Button.ussClassName);
            AddToClassList(ussClassName);
        }

        public ToolbarButton() : this(() => {})
        {
        }
    }
}
