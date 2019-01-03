// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
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

        public new static readonly string ussClassName = "unity-bounds-int-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public static readonly string positionUssClassName = ussClassName + "__position-field";
        public static readonly string sizeUssClassName = ussClassName + "__size-field";

        public BoundsIntField()
            : this(null) {}

        public BoundsIntField(string label)
            : base(label, null)
        {
            delegatesFocus = false;
            visualInput.focusable = false;

            AddToClassList(ussClassName);
            visualInput.AddToClassList(inputUssClassName);
            labelElement.AddToClassList(labelUssClassName);

            m_PositionField = new Vector3IntField("Position");
            m_PositionField.name = "unity-m_Position-input";
            m_PositionField.delegatesFocus = true;
            m_PositionField.AddToClassList(positionUssClassName);
            m_PositionField.RegisterValueChangedCallback(e =>
            {
                var current = value;
                current.position = e.newValue;
                value = current;
            });
            visualInput.hierarchy.Add(m_PositionField);


            m_SizeField = new Vector3IntField("Size");
            m_SizeField.name = "unity-m_Size-input";
            m_SizeField.delegatesFocus = true;
            m_SizeField.AddToClassList(sizeUssClassName);
            m_SizeField.RegisterValueChangedCallback(e =>
            {
                var current = value;
                current.size = e.newValue;
                value = current;
            });
            visualInput.hierarchy.Add(m_SizeField);
        }

        public override void SetValueWithoutNotify(BoundsInt newValue)
        {
            base.SetValueWithoutNotify(newValue);
            m_PositionField.SetValueWithoutNotify(rawValue.position);
            m_SizeField.SetValueWithoutNotify(rawValue.size);
        }
    }
}
