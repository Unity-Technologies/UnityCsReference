// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class BoundsField : BaseValueField<Bounds>
    {
        public class BoundsFieldFactory : UxmlFactory<BoundsField, BoundsFieldUxmlTraits> {}

        public class BoundsFieldUxmlTraits : BaseValueFieldUxmlTraits
        {
            UxmlFloatAttributeDescription m_CenterXValue;
            UxmlFloatAttributeDescription m_CenterYValue;
            UxmlFloatAttributeDescription m_CenterZValue;

            UxmlFloatAttributeDescription m_ExtentsXValue;
            UxmlFloatAttributeDescription m_ExtentsYValue;
            UxmlFloatAttributeDescription m_ExtentsZValue;

            public BoundsFieldUxmlTraits()
            {
                m_CenterXValue = new UxmlFloatAttributeDescription { name = "cx" };
                m_CenterYValue = new UxmlFloatAttributeDescription { name = "cy" };
                m_CenterZValue = new UxmlFloatAttributeDescription { name = "cz" };

                m_ExtentsXValue = new UxmlFloatAttributeDescription { name = "ex" };
                m_ExtentsYValue = new UxmlFloatAttributeDescription { name = "ey" };
                m_ExtentsZValue = new UxmlFloatAttributeDescription { name = "ez" };
            }

            public override IEnumerable<UxmlAttributeDescription> uxmlAttributesDescription
            {
                get
                {
                    foreach (var attr in base.uxmlAttributesDescription)
                    {
                        yield return attr;
                    }

                    yield return m_CenterXValue;
                    yield return m_CenterYValue;
                    yield return m_CenterZValue;
                    yield return m_ExtentsXValue;
                    yield return m_ExtentsYValue;
                    yield return m_ExtentsZValue;
                }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                BoundsField f = (BoundsField)ve;
                f.value = new Bounds(
                        new Vector3(m_CenterXValue.GetValueFromBag(bag), m_CenterYValue.GetValueFromBag(bag), m_CenterZValue.GetValueFromBag(bag)),
                        new Vector3(m_ExtentsXValue.GetValueFromBag(bag), m_ExtentsYValue.GetValueFromBag(bag), m_ExtentsZValue.GetValueFromBag(bag)));
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
                    SetValueAndNotify(current);
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
                    SetValueAndNotify(current);
                });

            var extentsGroup = new VisualElement();
            extentsGroup.AddToClassList("group");
            extentsGroup.contentContainer.Add(new Label("Extents"));
            extentsGroup.contentContainer.Add(m_ExtentsField);
            this.shadow.Add(extentsGroup);
        }

        protected override void UpdateDisplay()
        {
            m_CenterField.value = m_Value.center;
            m_ExtentsField.value = m_Value.extents;
        }
    }
}
