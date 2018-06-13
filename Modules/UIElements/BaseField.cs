// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public abstract class BaseField<T> : VisualElement, IBindable, INotifyValueChanged<T>
    {
        [SerializeField]
        protected T m_Value;
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_PropertyPath;

            public UxmlTraits()
            {
                m_PropertyPath = new UxmlStringAttributeDescription { name = "binding-path" };
                m_FocusIndex.defaultValue = 0;
            }

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get
                {
                    yield break;
                }
            }


            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                string propPath = m_PropertyPath.GetValueFromBag(bag);

                if (!string.IsNullOrEmpty(propPath))
                {
                    BaseField<T> control = ve as BaseField<T>;
                    if (control != null)
                    {
                        control.bindingPath = propPath;
                    }
                }
            }
        }

        public BaseField()
        {
            // Fields are focusable by default
            focusIndex = 0;
            m_Value = default(T);
        }

        public virtual T value
        {
            get { return m_Value; }
            set
            {
                if (!EqualityComparer<T>.Default.Equals(m_Value, value))
                {
                    using (ChangeEvent<T> evt = ChangeEvent<T>.GetPooled(m_Value, value))
                    {
                        evt.target = this;
                        SetValueWithoutNotify(value);
                        UIElementsUtility.eventDispatcher.DispatchEvent(evt, panel);
                    }
                    MarkDirtyRepaint();
                }
            }
        }

        [Obsolete("This method is replaced by simply using this.value. The default behaviour has been changed to notify when changed. If the behaviour is not to be notified, SetValueWithoutNotify() must be used.", false)]
        public virtual void SetValueAndNotify(T newValue)
        {
            value = newValue;
        }

        public void OnValueChanged(EventCallback<ChangeEvent<T>> callback)
        {
            RegisterCallback(callback);
        }

        public void RemoveOnValueChanged(EventCallback<ChangeEvent<T>> callback)
        {
            UnregisterCallback(callback);
        }

        public virtual void SetValueWithoutNotify(T newValue)
        {
            m_Value = newValue;

            if (!string.IsNullOrEmpty(persistenceKey))
                SavePersistentData();
        }

        public IBinding binding { get; set; }
        public string bindingPath { get; set; }
    }
}
