// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// A toggle for the toolbar. For more information, refer to [[wiki:UIE-uxml-element-ToolbarToggle|UXML element ToolbarToggle]].
    /// </summary>
    [UxmlElement]
    [Icon("UIToolkit/Icons/ToolbarToggle.png")]
    public partial class ToolbarToggle : Toggle
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-toolbar-toggle";

        /// <summary>
        /// Constructor.
        /// </summary>
        public ToolbarToggle()
        {
            focusable = false;

            Toolbar.SetToolbarStyleSheet(this);
            RemoveFromClassList(Toggle.ussClassName);
            AddToClassList(ussClassName);
        }
    }
}
