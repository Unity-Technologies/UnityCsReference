// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEngine.Experimental.UIElements
{
    public class Foldout : BindableElement, INotifyValueChanged<bool>
    {
        private static readonly string s_FoldoutClassName = "unity-foldout";
        private static readonly string s_ToggleClassName = "unity-foldout-toggle";
        private static readonly string s_ContentContainerClassName = "unity-foldout-content";
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

        // This method is obsolete in the declaring interface and will be removed soon.
        [UnityEngine.Internal.ExcludeFromDocs]
        public void SetValueAndNotify(bool newValue)
        {
            value = newValue;
        }

        public void SetValueWithoutNotify(bool newValue)
        {
            m_Value = newValue;
            m_Toggle.value = m_Value;

            if (m_Value)
            {
                contentContainer.visible = true;
                contentContainer.style.positionType = PositionType.Relative;
            }
            else
            {
                contentContainer.visible = false;
                contentContainer.style.positionType = PositionType.Absolute;
            }
        }

        public void OnValueChanged(EventCallback<ChangeEvent<bool>> callback)
        {
            RegisterCallback(callback);
        }

        public void RemoveOnValueChanged(EventCallback<ChangeEvent<bool>> callback)
        {
            UnregisterCallback(callback);
        }

        public Foldout()
        {
            m_Toggle = new Toggle();
            m_Toggle.value = true;
            m_Toggle.OnValueChanged((evt) =>
            {
                value = m_Toggle.value;
                evt.StopPropagation();
            });
            m_Toggle.AddToClassList(s_ToggleClassName);
            shadow.Add(m_Toggle);

            m_Container = new VisualElement();
            m_Container.clippingOptions = ClippingOptions.ClipContents;
            m_Container.AddToClassList(s_ContentContainerClassName);
            shadow.Add(m_Container);

            AddToClassList(s_FoldoutClassName);
        }
    }
}
