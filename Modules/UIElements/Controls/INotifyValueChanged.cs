// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.UIElements
{
    public interface INotifyValueChanged<T>
    {
        T value { get; set; }

        [Obsolete("This method is replaced by simply using this.value. The default behaviour has been changed to notify when changed. If the behaviour is not to be notified, SetValueWithoutNotify() must be used.", false)]
        void SetValueAndNotify(T newValue);

        void SetValueWithoutNotify(T newValue);

        void OnValueChanged(EventCallback<ChangeEvent<T>> callback);
        void RemoveOnValueChanged(EventCallback<ChangeEvent<T>> callback);
    }
}
