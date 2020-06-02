// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
