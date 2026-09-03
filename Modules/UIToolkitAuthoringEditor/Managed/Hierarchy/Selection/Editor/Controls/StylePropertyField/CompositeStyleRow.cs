// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Groups two style fields on a single row under one aligned label. The inner fields keep their own
    /// <see cref="StylePropertyBinding"/>s; this control only provides the aligned label and affordance gutter so the
    /// row lines up with the single-field rows in the inspector.
    /// </summary>
    [UxmlElement]
    internal partial class CompositeStyleRow : BaseField<CompositeStyleRow.EmptyValue>
    {
        internal readonly struct EmptyValue { }
        internal class EmptyValueAttributeConverter : UxmlAttributeConverter<EmptyValue>
        {
            public override EmptyValue FromString(string value)
            {
                throw new NotImplementedException("No support for editing this.");
            }

            public override string ToString(EmptyValue value)
            {
                throw new NotImplementedException("No support for editing this.");
            }
        }

        public new static readonly string ussClassName = "unity-composite-field-row";
        public static readonly string containerUssClassName = ussClassName + "__container";

        readonly VisualElement m_Container;

        public override VisualElement contentContainer => m_Container ?? this;

        public CompositeStyleRow() : this(null) { }

        public CompositeStyleRow(string label) : base(label, null)
        {
            AddToClassList(ussClassName);
            AddToClassList(alignedFieldUssClassName);

            visualInput.style.display = DisplayStyle.None;

            m_Container = new VisualElement();
            m_Container.AddToClassList(containerUssClassName);
            hierarchy.Add(m_Container);
        }
    }
}
