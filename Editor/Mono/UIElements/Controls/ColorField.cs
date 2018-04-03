// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class ColorField : BaseControl<Color>
    {
        public class ColorFieldFactory : UxmlFactory<ColorField, ColorFieldUxmlTraits> {}

        public class ColorFieldUxmlTraits : BaseControlUxmlTraits
        {
            UxmlColorAttributeDescription m_Value;
            UxmlBoolAttributeDescription m_ShowEyeDropper;
            UxmlBoolAttributeDescription m_ShowAlpha;
            UxmlBoolAttributeDescription m_Hdr;

            public ColorFieldUxmlTraits()
            {
                m_Value = new UxmlColorAttributeDescription { name = "value" };
                m_ShowEyeDropper = new UxmlBoolAttributeDescription { name = "showEyeDropper", defaultValue = true };
                m_ShowAlpha = new UxmlBoolAttributeDescription { name = "showAlpha", defaultValue = true };
                m_Hdr = new UxmlBoolAttributeDescription { name = "hdr" };
            }

            public override IEnumerable<UxmlAttributeDescription> uxmlAttributesDescription
            {
                get
                {
                    foreach (var attr in base.uxmlAttributesDescription)
                    {
                        yield return attr;
                    }

                    yield return m_Value;
                    yield return m_ShowEyeDropper;
                    yield return m_ShowAlpha;
                    yield return m_Hdr;
                }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                ((ColorField)ve).value = m_Value.GetValueFromBag(bag);
                ((ColorField)ve).showEyeDropper = m_ShowEyeDropper.GetValueFromBag(bag);
                ((ColorField)ve).showAlpha = m_ShowAlpha.GetValueFromBag(bag);
                ((ColorField)ve).hdr = m_Hdr.GetValueFromBag(bag);
            }
        }

        private Color m_Value;

        public override Color value
        {
            get { return m_Value; }
            set
            {
                m_Value = value;
                Dirty(ChangeType.Repaint);
            }
        }

        public bool showEyeDropper { get; set; }
        public bool showAlpha { get; set; }
        public bool hdr { get; set; }

        private bool m_SetKbControl;
        private bool m_ResetKbControl;

        private IMGUIContainer m_ColorField;

        public ColorField()
        {
            showEyeDropper = true;
            showAlpha = true;

            m_ColorField = new IMGUIContainer(OnGUIHandler) { name = "InternalColorField" };
            // Disable focus on the IMGUIContainer, it's handled by the parent VisualElement
            m_ColorField.focusIndex = -1;
            Add(m_ColorField);
        }

        public override void SetValueAndNotify(Color newValue)
        {
            if (value != newValue)
            {
                using (ChangeEvent<Color> evt = ChangeEvent<Color>.GetPooled(value, newValue))
                {
                    evt.target = this;
                    value = newValue;
                    UIElementsUtility.eventDispatcher.DispatchEvent(evt, panel);
                }
            }
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.GetEventTypeId() == FocusEvent.TypeId())
                m_SetKbControl = true;
            if (evt.GetEventTypeId() == BlurEvent.TypeId())
                m_ResetKbControl = true;
        }

        protected internal override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            if (evt.GetEventTypeId() == KeyDownEvent.TypeId())
                m_ColorField.HandleEvent(evt);
        }

        private void OnGUIHandler()
        {
            // Dirty repaint on eye dropper update to preview the color under the cursor
            if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == EventCommandNames.EyeDropperUpdate)
            {
                Dirty(ChangeType.Repaint);
            }

            Color newColor = EditorGUILayout.ColorField(GUIContent.none, value, showEyeDropper, showAlpha, hdr);
            SetValueAndNotify(newColor);
            if (m_SetKbControl)
            {
                GUIUtility.SetKeyboardControlToFirstControlId();
                m_SetKbControl = false;
            }
            if (m_ResetKbControl)
            {
                GUIUtility.keyboardControl = 0;
                m_ResetKbControl = false;
            }
        }
    }
}
