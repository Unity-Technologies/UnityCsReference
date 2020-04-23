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
