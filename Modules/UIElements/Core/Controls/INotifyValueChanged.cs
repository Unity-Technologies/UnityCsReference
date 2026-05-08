// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Interface for controls that hold a value and can notify when it is changed by user input.
    /// </summary>
    public interface INotifyValueChanged<T>
    {
        /// <summary>
        /// The value the control holds.
        /// </summary>
        T value { get; set; }

        /// <summary>
        /// Sets the value and, even if different, doesn't notify registers callbacks with a <see cref="ChangeEvent{T}"/>
        /// </summary>
        /// <param name="newValue">The new value to be set.</param>
        void SetValueWithoutNotify(T newValue);
    }


    /// <summary>
    /// INotifyValueChangedExtensions is a set of extension methods useful for objects implementing <see cref="INotifyValueChanged{T}"/>.
    /// </summary>
    public static class INotifyValueChangedExtensions
    {
        /// <summary>
        /// Registers this callback to receive <see cref="ChangeEvent{T}"/> when the value is changed.
        /// </summary>
        /// <remarks>
        /// <para>This calls <see cref="CallbackEventHandler.RegisterCallback{TEventType}(EventCallback{TEventType},TrickleDown)"/> on the same control (equivalent to registering a <see cref="ChangeEvent{T}"/> callback directly). <see cref="ChangeEvent{T}"/> participates in propagation; handlers on an ancestor receive bubbled events from descendant controls of the same event type.</para>
        /// <para>Use <see cref="EventBase.target"/> to identify which element originated the change, and <see cref="EventBase.currentTarget"/> for the element on which the callback was registered. Refer to the [[wiki:UIE-Change-Events|Change events]] manual page for guidance on filtering and composite controls.</para>
        /// </remarks>
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

        /// <summary>
        /// Unregisters this callback from receiving <see cref="ChangeEvent{T}"/> when the value is changed.
        /// </summary>
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
