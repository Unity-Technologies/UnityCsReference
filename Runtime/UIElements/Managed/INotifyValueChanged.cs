// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public interface INotifyValueChanged<T>
    {
        T value { get; set; }

        void SetValueAndNotify(T newValue);

        void OnValueChanged(EventCallback<ChangeEvent<T>> callback);
    }
}
