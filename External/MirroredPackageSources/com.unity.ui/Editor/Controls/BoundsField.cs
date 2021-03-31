using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// A <see cref="Bounds"/> editor field.
    /// </summary>
    public class BoundsField : BaseField<Bounds>
    {
        /// <summary>
        /// Instantiates a <see cref="BoundsField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<BoundsField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="BoundsField"/>.
        /// </summary>
        public new class UxmlTraits : BaseField<Bounds>.UxmlTraits
        {
            UxmlFloatAttributeDescription m_CenterXValue = new UxmlFloatAttributeDescription { name = "cx" };
            UxmlFloatAttributeDescription m_CenterYValue = new UxmlFloatAttributeDescription { name = "cy" };
            UxmlFloatAttributeDescription m_CenterZValue = new UxmlFloatAttributeDescription { name = "cz" };

            UxmlFloatAttributeDescription m_ExtentsXValue = new UxmlFloatAttributeDescription { name = "ex" };
            UxmlFloatAttributeDescription m_ExtentsYValue = new UxmlFloatAttributeDescription { name = "ey" };
            UxmlFloatAttributeDescription m_ExtentsZValue = new UxmlFloatAttributeDescription { name = "ez" };

            /// <summary>
            /// Initialize <see cref="BoundsField"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                BoundsField f = (BoundsField)ve;
                f.SetValueWithoutNotify(new Bounds(
                    new Vector3(m_CenterXValue.GetValueFromBag(bag, cc), m_CenterYValue.GetValueFromBag(bag, cc), m_CenterZValue.GetValueFromBag(bag, cc)),
                    new Vector3(m_ExtentsXValue.GetValueFromBag(bag, cc), m_ExtentsYValue.GetValueFromBag(bag, cc), m_ExtentsZValue.GetValueFromBag(bag, cc))));
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-bounds-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";
        /// <summary>
        /// USS class name of center fields in elements of this type.
        /// </summary>
        public static readonly string centerFieldUssClassName = ussClassName + "__center-field";
        /// <summary>
        /// USS class name of extents fields in elements of this type.
        /// </summary>
        public static readonly string extentsFieldUssClassName = ussClassName + "__extents-field";

        private Vector3Field m_CenterField;
        private Vector3Field m_ExtentsField;

        /// <summary>
        /// Initializes and returns an instance of BoundsField.
        /// </summary>
        public BoundsField()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of BoundsField.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public BoundsField(string label)
            : base(label, null)
        {
            delegatesFocus = false;
            visualInput.focusable = false;

            AddToClassList(ussClassName);
            visualInput.AddToClassList(inputUssClassName);
            labelElement.AddToClassList(labelUssClassName);

            m_CenterField = new Vector3Field("Center");
            m_CenterField.name = "unity-m_Center-input";
            m_CenterField.delegatesFocus = true;
            m_CenterField.AddToClassList(centerFieldUssClassName);

            m_CenterField.RegisterValueChangedCallback(e =>
            {
                Bounds current = value;
                current.center = e.newValue;
                value = current;
            });
            visualInput.hierarchy.Add(m_CenterField);

            m_ExtentsField = new Vector3Field("Extents");
            m_ExtentsField.name = "unity-m_Extent-input";
            m_ExtentsField.delegatesFocus = true;
            m_ExtentsField.AddToClassList(extentsFieldUssClassName);
            m_ExtentsField.RegisterValueChangedCallback(e =>
            {
                Bounds current = value;
                current.extents = e.newValue;
                value = current;
            });
            visualInput.hierarchy.Add(m_ExtentsField);
        }

        public override void SetValueWithoutNotify(Bounds newValue)
        {
            base.SetValueWithoutNotify(newValue);
            m_CenterField.SetValueWithoutNotify(rawValue.center);
            m_ExtentsField.SetValueWithoutNotify(rawValue.extents);
        }

        protected override void UpdateMixedValueContent()
        {
            m_CenterField.showMixedValue = showMixedValue;
            m_ExtentsField.showMixedValue = showMixedValue;
        }
    }
}
