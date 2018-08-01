// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
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

        private Vector3Field m_CenterField;
        private Vector3Field m_ExtentsField;

        public BoundsField()
        {
            m_CenterField = new Vector3Field();

            m_CenterField.OnValueChanged(e =>
            {
                Bounds current = value;
                current.center = e.newValue;
                value = current;
            });

            var centerGroup = new VisualElement();
            centerGroup.AddToClassList("group");
            centerGroup.Add(new Label("Center"));
            centerGroup.Add(m_CenterField);
            this.shadow.Add(centerGroup);

            m_ExtentsField = new Vector3Field();

            m_ExtentsField.OnValueChanged(e =>
            {
                Bounds current = value;
                current.extents = e.newValue;
                value = current;
            });

            var extentsGroup = new VisualElement();
            extentsGroup.AddToClassList("group");
            extentsGroup.contentContainer.Add(new Label("Extents"));
            extentsGroup.contentContainer.Add(m_ExtentsField);
            this.shadow.Add(extentsGroup);
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            // Focus first field if any
            if (evt.GetEventTypeId() == FocusEvent.TypeId())
            {
                m_CenterField.Focus();
            }
        }

        public override void SetValueWithoutNotify(Bounds newValue)
        {
            base.SetValueWithoutNotify(newValue);
            m_CenterField.SetValueWithoutNotify(m_Value.center);
            m_ExtentsField.SetValueWithoutNotify(m_Value.extents);
        }
    }
}
