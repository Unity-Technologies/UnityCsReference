// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Makes a style field for editing a StyleCursor.
    /// </summary>
    [UxmlElement]
    internal partial class StyleCursorField : StylePropertyField<StyleCursor, CursorField, Cursor>
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-cursor-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public StyleCursorField() : this(null) {}

        public StyleCursorField(string label) : base(label, new CursorField())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        protected override CursorField CreateValueField()
        {
            return new CursorField();
        }

        protected override StyleCursor CreateStyleValue(Cursor v)
        {
            return v;
        }

        internal override bool EqualsCurrentValue(StyleCursor v)
        {
            return value == v;
        }
    }
}
