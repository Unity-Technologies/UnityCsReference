// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class ColorField : VisualElement, INotifyValueChanged<Color>
    {
        private Color m_Value;

        public Color value
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

        public ColorField()
        {
            showEyeDropper = true;
            showAlpha = true;

            var colorField = new IMGUIContainer(OnGUIHandler) { name = "InternalColorField" };
            Add(colorField);
        }

        public void SetValueAndNotify(Color newValue)
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

        public void OnValueChanged(EventCallback<ChangeEvent<Color>> callback)
        {
            RegisterCallback(callback);
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.GetEventTypeId() == FocusEvent.TypeId())
                m_SetKbControl = true;
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
        }
    }
}
