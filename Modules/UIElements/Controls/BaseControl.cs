// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Internal;

namespace UnityEngine.Experimental.UIElements
{
    public abstract class BaseControl<T> : VisualElement, INotifyValueChanged<T>
    {
        public class BaseControlUxmlTraits : VisualElementUxmlTraits
        {
            public BaseControlUxmlTraits()
            {
                m_FocusIndex.defaultValue = 0;
            }

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }

        public BaseControl()
        {
            // Controls are focusable by default
            focusIndex = 0;
        }

        public abstract T value { get; set; }

        public virtual void OnValueChanged(EventCallback<ChangeEvent<T>> callback)
        {
            RegisterCallback(callback);
        }

        public virtual void SetValueAndNotify(T newValue)
        {
            if (!EqualityComparer<T>.Default.Equals(value, newValue))
            {
                using (ChangeEvent<T> evt = ChangeEvent<T>.GetPooled(value, newValue))
                {
                    evt.target = this;
                    value = newValue;
                    UIElementsUtility.eventDispatcher.DispatchEvent(evt, panel);
                }
            }
        }
    }

    public abstract class BaseTextControl<T> : BaseTextElement, INotifyValueChanged<T>
    {
        public class BaseTextControlUxmlTraits : BaseTextElementUxmlTraits
        {
            public BaseTextControlUxmlTraits()
            {
                m_FocusIndex.defaultValue = 0;
            }
        }

        public BaseTextControl()
        {
            // Controls are focusable by default
            focusIndex = 0;
        }

        // Hide text setter for controls, values must be set with the value property
        public new virtual string text
        {
            get { return base.text; }
            protected set { base.text = value; }
        }

        public abstract T value { get; set; }

        public virtual void OnValueChanged(EventCallback<ChangeEvent<T>> callback)
        {
            RegisterCallback(callback);
        }

        public virtual void SetValueAndNotify(T newValue)
        {
            if (!EqualityComparer<T>.Default.Equals(value, newValue))
            {
                using (ChangeEvent<T> evt = ChangeEvent<T>.GetPooled(value, newValue))
                {
                    evt.target = this;
                    value = newValue;
                    UIElementsUtility.eventDispatcher.DispatchEvent(evt, panel);
                }
            }
        }
    }
}
