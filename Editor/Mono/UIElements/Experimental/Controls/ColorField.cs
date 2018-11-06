// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class ColorField : BaseField<Color>
    {
        public new class UxmlFactory : UxmlFactory<ColorField, UxmlTraits> {}

        public new class UxmlTraits : BaseField<Color>.UxmlTraits
        {
            UxmlColorAttributeDescription m_Value = new UxmlColorAttributeDescription { name = "value" };
            UxmlBoolAttributeDescription m_ShowEyeDropper = new UxmlBoolAttributeDescription { name = "show-eye-dropper", obsoleteNames = new[] { "showEyeDropper" }, defaultValue = true };
            UxmlBoolAttributeDescription m_ShowAlpha = new UxmlBoolAttributeDescription { name = "show-alpha", obsoleteNames = new[] { "showAlpha" }, defaultValue = true };
            UxmlBoolAttributeDescription m_Hdr = new UxmlBoolAttributeDescription { name = "hdr" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                ((ColorField)ve).SetValueWithoutNotify(m_Value.GetValueFromBag(bag, cc));
                ((ColorField)ve).showEyeDropper = m_ShowEyeDropper.GetValueFromBag(bag, cc);
                ((ColorField)ve).showAlpha = m_ShowAlpha.GetValueFromBag(bag, cc);
                ((ColorField)ve).hdr = m_Hdr.GetValueFromBag(bag, cc);
            }
        }

        public bool showEyeDropper { get; set; }
        public bool showAlpha { get; set; }
        public bool hdr { get; set; }

        private bool m_SetKbControl;
        private bool m_ResetKbControl;

        private IMGUIContainer m_ColorField;

        // Since the ColorField is containing a child in the focus ring,
        //     it must make sure the child focus follow the focusIndex
        public override int focusIndex
        {
            get { return base.focusIndex; }
            set
            {
                base.focusIndex = value;
                if (m_ColorField != null)
                {
                    m_ColorField.focusIndex = value;
                }
            }
        }


        public ColorField()
        {
            showEyeDropper = true;
            showAlpha = true;

            // The focus on a color field is implemented like a BaseCompoundField : the ColorField and its inner child
            // are both put in the focus ring. When the ColorField is receiving the Focus, it is "delegating" it to the inner child,
            // which is, in this case, the IMGUIContainer.
            m_ColorField = new IMGUIContainer(OnGUIHandler) { name = "InternalColorField", useUIElementsFocusStyle = true };
            Add(m_ColorField);
        }

        [Obsolete("This method is replaced by simply using this.value. The default behaviour has been changed to notify when changed. If the behaviour is not to be notified, SetValueWithoutNotify() must be used.", false)]
        public override void SetValueAndNotify(Color newValue)
        {
            if (value != newValue)
            {
                value = newValue;
            }
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.GetEventTypeId() == FocusEvent.TypeId())
            {
                m_SetKbControl = true;
                // Make sure the inner IMGUIContainer is receiving the focus
                m_ColorField.Focus();
            }

            if (evt.GetEventTypeId() == BlurEvent.TypeId())
            {
                m_ResetKbControl = true;
            }
        }

        protected internal override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            if (evt.GetEventTypeId() == KeyDownEvent.TypeId())
            {
                m_ColorField.HandleEvent(evt);
            }
        }

        private void OnGUIHandler()
        {
            // Dirty repaint on eye dropper update to preview the color under the cursor
            if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == EventCommandNames.EyeDropperUpdate)
            {
                IncrementVersion(VersionChangeType.Repaint);
            }

            Color newColor = EditorGUILayout.ColorField(GUIContent.none, value, showEyeDropper, showAlpha, hdr);
            value = newColor;
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
