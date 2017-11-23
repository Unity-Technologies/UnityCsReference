// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class BoundsField : BaseValueField<Bounds>
    {
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
