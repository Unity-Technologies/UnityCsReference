// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public class BoundsField : BaseField<Bounds>
    {
        public new class UxmlFactory : UxmlFactory<BoundsField, UxmlTraits> {}

        public new class UxmlTraits : BaseField<Bounds>.UxmlTraits
        {
            UxmlFloatAttributeDescription m_CenterXValue = new UxmlFloatAttributeDescription { name = "cx" };
            UxmlFloatAttributeDescription m_CenterYValue = new UxmlFloatAttributeDescription { name = "cy" };
            UxmlFloatAttributeDescription m_CenterZValue = new UxmlFloatAttributeDescription { name = "cz" };

            UxmlFloatAttributeDescription m_ExtentsXValue = new UxmlFloatAttributeDescription { name = "ex" };
            UxmlFloatAttributeDescription m_ExtentsYValue = new UxmlFloatAttributeDescription { name = "ey" };
            UxmlFloatAttributeDescription m_ExtentsZValue = new UxmlFloatAttributeDescription { name = "ez" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                BoundsField f = (BoundsField)ve;
                f.SetValueWithoutNotify(new Bounds(
                    new Vector3(m_CenterXValue.GetValueFromBag(bag, cc), m_CenterYValue.GetValueFromBag(bag, cc), m_CenterZValue.GetValueFromBag(bag, cc)),
                    new Vector3(m_ExtentsXValue.GetValueFromBag(bag, cc), m_ExtentsYValue.GetValueFromBag(bag, cc), m_ExtentsZValue.GetValueFromBag(bag, cc))));
            }
        }

        public new static readonly string ussClassName = "unity-bounds-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";
        public static readonly string centerFieldUssClassName = ussClassName + "__center-field";
        public static readonly string extentsFieldUssClassName = ussClassName + "__extents-field";

        private Vector3Field m_CenterField;
        private Vector3Field m_ExtentsField;

        public BoundsField()
            : this(null) {}

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
    }
}
