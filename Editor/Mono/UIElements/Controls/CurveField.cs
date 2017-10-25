// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.Experimental.UIElements
{
    public class CurveField : VisualElement, INotifyValueChanged<AnimationCurve>
    {
        private const string k_CurveColorProperty = "curve-color";

        private bool m_SetKbControl;

        public AnimationCurve value { get; set; }
        public Rect ranges { get; set; }

        StyleValue<Color> m_CurveColor;
        private Color curveColor
        {
            get
            {
                return m_CurveColor.GetSpecifiedValueOrDefault(Color.green);
            }
        }

        public CurveField()
        {
            var curveField = new IMGUIContainer(OnGUIHandler) { name = "InternalCurveField" };
            Add(curveField);
        }

        public void SetValueAndNotify(AnimationCurve newValue)
        {
            if (value != newValue)
            {
                SendChangeEvent(newValue);
            }
        }

        public void OnValueChanged(EventCallback<ChangeEvent<AnimationCurve>> callback)
        {
            RegisterCallback(callback);
        }

        protected override void OnStyleResolved(ICustomStyle style)
        {
            base.OnStyleResolved(style);

            style.ApplyCustomProperty(k_CurveColorProperty, ref m_CurveColor);
        }

        private void SendChangeEvent(AnimationCurve newValue)
        {
            // Since the animation curve is an object, it's possible that it changed internally but that the comparison
            // "value != newValue" returns false;
            using (ChangeEvent<AnimationCurve> evt = ChangeEvent<AnimationCurve>.GetPooled(value, newValue))
            {
                evt.target = this;
                value = newValue;
                UIElementsUtility.eventDispatcher.DispatchEvent(evt, panel);
            }
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.GetEventTypeId() == FocusEvent.TypeId())
                m_SetKbControl = true;
        }

        private static int s_CurveHash = "s_CurveHash".GetHashCode();
        private void OnGUIHandler()
        {
            Rect r = EditorGUILayout.GetControlRect(false, EditorGUI.kSingleLineHeight, EditorStyles.colorField);
            int id = GUIUtility.GetControlID(s_CurveHash, FocusType.Keyboard, r);
            AnimationCurve newCurve = EditorGUI.DoCurveField(r, id, value, m_CurveColor, ranges, null);

            Event evt = Event.current;
            if (evt.GetTypeForControl(id) == EventType.ExecuteCommand &&
                (evt.commandName == "CurveChangeCompleted" || evt.commandName == "CurveChanged"))
            {
                if (value == CurveEditorWindow.curve)
                    SendChangeEvent(newCurve);
            }

            if (m_SetKbControl)
            {
                GUIUtility.SetKeyboardControlToFirstControlId();
                m_SetKbControl = false;
            }
        }
    }
}
