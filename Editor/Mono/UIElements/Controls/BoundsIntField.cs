// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class BoundsIntField : BaseField<BoundsInt>
    {
        public new class UxmlFactory : UxmlFactory<BoundsIntField, UxmlTraits> {}

        public new class UxmlTraits : BaseField<BoundsInt>.UxmlTraits
        {
            UxmlIntAttributeDescription m_PositionXValue = new UxmlIntAttributeDescription { name = "px" };
            UxmlIntAttributeDescription m_PositionYValue = new UxmlIntAttributeDescription { name = "py" };
            UxmlIntAttributeDescription m_PositionZValue = new UxmlIntAttributeDescription { name = "pz" };

            UxmlIntAttributeDescription m_SizeXValue = new UxmlIntAttributeDescription { name = "sx" };
            UxmlIntAttributeDescription m_SizeYValue = new UxmlIntAttributeDescription { name = "sy" };
            UxmlIntAttributeDescription m_SizeZValue = new UxmlIntAttributeDescription { name = "sz" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var f = (BoundsIntField)ve;
                f.SetValueWithoutNotify(new BoundsInt(
                    new Vector3Int(m_PositionXValue.GetValueFromBag(bag, cc), m_PositionYValue.GetValueFromBag(bag, cc), m_PositionZValue.GetValueFromBag(bag, cc)),
                    new Vector3Int(m_SizeXValue.GetValueFromBag(bag, cc), m_SizeYValue.GetValueFromBag(bag, cc), m_SizeZValue.GetValueFromBag(bag, cc))));
            }
        }

        private Vector3IntField m_PositionField;
        private Vector3IntField m_SizeField;

        public BoundsIntField()
        {
            m_PositionField = new Vector3IntField();

            m_PositionField.OnValueChanged(e =>
            {
                var current = value;
                current.position = e.newValue;
                value = current;
            });

            var centerGroup = new VisualElement();
            centerGroup.AddToClassList("group");
            centerGroup.Add(new Label("Position"));
            centerGroup.Add(m_PositionField);
            this.shadow.Add(centerGroup);

            m_SizeField = new Vector3IntField();

            m_SizeField.OnValueChanged(e =>
            {
                var current = value;
                current.size = e.newValue;
                value = current;
            });

            var extentsGroup = new VisualElement();
            extentsGroup.AddToClassList("group");
            extentsGroup.contentContainer.Add(new Label("Size"));
            extentsGroup.contentContainer.Add(m_SizeField);
            this.shadow.Add(extentsGroup);
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            // Focus first field if any
            if (evt.GetEventTypeId() == FocusEvent.TypeId())
            {
                m_PositionField.Focus();
            }
        }

        public override void SetValueWithoutNotify(BoundsInt newValue)
        {
            base.SetValueWithoutNotify(newValue);
            m_PositionField.SetValueWithoutNotify(m_Value.position);
            m_SizeField.SetValueWithoutNotify(m_Value.size);
        }
    }
}
