// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class ToolbarButton : Button
    {
        public new class UxmlFactory : UxmlFactory<ToolbarButton, UxmlTraits> {}
        public new class UxmlTraits : Button.UxmlTraits {}

        const string k_ClassName = "toolbarButton";
        public ToolbarButton(Action clickEvent) :
            base(clickEvent)
        {
            Toolbar.SetToolbarStyleSheet(this);
            AddToClassList(k_ClassName);
        }

        public ToolbarButton() : this(() => {})
        {
        }
    }
}
