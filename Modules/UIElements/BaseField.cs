// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public abstract class BaseField<T> : BindableElement, INotifyValueChanged<T>
    {
        [SerializeField]
        protected T m_Value;
        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            public UxmlTraits()
            {
                m_FocusIndex.defaultValue = 0;
            }

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get
                {
                    yield break;
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
                    if (panel != null)
                    {
                        using (ChangeEvent<T> evt = ChangeEvent<T>.GetPooled(m_Value, value))
                        {
                            evt.target = this;
                            SetValueWithoutNotify(value);
                            SendEvent(evt);
                        }
                    }
                    else
                    {
                        SetValueWithoutNotify(value);
                    }
                }
            }
        }

        [Obsolete("This method is replaced by simply using this.value. The default behaviour has been changed to notify when changed. If the behaviour is not to be notified, SetValueWithoutNotify() must be used.", false)]
        public virtual void SetValueAndNotify(T newValue)
        {
            value = newValue;
        }

        public override void OnPersistentDataReady()
        {
            base.OnPersistentDataReady();
            var key = GetFullHierarchicalPersistenceKey();

            var oldValue = m_Value;
            OverwriteFromPersistedData(this, key);

            if (!EqualityComparer<T>.Default.Equals(oldValue, m_Value))
                using (ChangeEvent<T> evt = ChangeEvent<T>.GetPooled(oldValue, m_Value))
                {
                    evt.target = this;
                    SendEvent(evt);
                }
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
            MarkDirtyRepaint();
        }
    }
}
