// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    public interface INotifyValueChanged<T>
    {
        T value { get; set; }

        void SetValueWithoutNotify(T newValue);
    }


    public static class INotifyValueChangedExtensions
    {
        public static bool RegisterValueChangedCallback<T>(this INotifyValueChanged<T> control, EventCallback<ChangeEvent<T>> callback)
        {
            var handler =  control as CallbackEventHandler;

            if (handler != null)
            {
                handler.RegisterCallback(callback);
                return true;
            }
            return false;
        }

        public static bool UnregisterValueChangedCallback<T>(this INotifyValueChanged<T> control, EventCallback<ChangeEvent<T>> callback)
        {
            var handler = control as CallbackEventHandler;

            if (handler != null)
            {
                handler.UnregisterCallback(callback);
                return true;
            }
            return false;
        }
    }
}
