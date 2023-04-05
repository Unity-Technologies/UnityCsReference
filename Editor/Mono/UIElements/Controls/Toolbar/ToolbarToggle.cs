// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// A toggle for the toolbar.
    /// </summary>
    public class ToolbarToggle : Toggle
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : Toggle.UxmlSerializedData
        {
            public override object CreateInstance() => new ToolbarToggle();
        }

        /// <summary>
        /// Instantiates a <see cref="ToolbarToggle"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<ToolbarToggle, UxmlTraits> {}
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="ToolbarToggle"/>.
        /// </summary>
        public new class UxmlTraits : Toggle.UxmlTraits
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            public UxmlTraits()
            {
                focusable.defaultValue = false;
            }
        }

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
