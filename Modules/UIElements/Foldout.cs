// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    public class Foldout : BindableElement, INotifyValueChanged<bool>
    {
        public new class UxmlFactory : UxmlFactory<Foldout, UxmlTraits> {}

        Toggle m_Toggle;
        VisualElement m_Container;

        public override VisualElement contentContainer
        {
            get
            {
                return m_Container;
            }
        }

        public string text
        {
            get
            {
                return m_Toggle.text;
            }
            set
            {
                m_Toggle.text = value;
            }
        }

        private bool m_Value = true;
        public bool value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (m_Value == value)
                    return;

                using (ChangeEvent<bool> evt = ChangeEvent<bool>.GetPooled(m_Value, value))
                {
                    evt.target = this;
                    SetValueWithoutNotify(value);
                    SendEvent(evt);
                }
            }
        }

        public void SetValueWithoutNotify(bool newValue)
        {
            m_Value = newValue;
            m_Toggle.value = m_Value;

            if (m_Value)
            {
                contentContainer.visible = true;
                contentContainer.style.position = Position.Relative;
            }
            else
            {
                contentContainer.visible = false;
                contentContainer.style.position = Position.Absolute;
            }
        }

        public static readonly string ussClassName = "unity-foldout";
        public static readonly string toggleUssClassName = ussClassName + "__toggle";
        public static readonly string contentUssClassName = ussClassName + "__content";

        public Foldout()
        {
            AddToClassList(ussClassName);

            m_Toggle = new Toggle
            {
                value = true
            };
            m_Toggle.RegisterValueChangedCallback((evt) =>
            {
                value = m_Toggle.value;
                evt.StopPropagation();
            });
            m_Toggle.AddToClassList(toggleUssClassName);
            hierarchy.Add(m_Toggle);

            m_Container = new VisualElement()
            {
                name = "unity-content",
            };
            m_Container.AddToClassList(contentUssClassName);
            hierarchy.Add(m_Container);
        }
    }
}
