// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine.UIElements
{
    internal interface IUxmlSerializedDataCustomAttributeHandler
    {
        void SerializeCustomAttributes(IUxmlAttributes bag, HashSet<string> handledAttributes);
    }

    /// <summary>
    /// Generates an instance of the declaring element when the <see cref="UxmlElementAttribute"/> is used in a custom control. 
    /// </summary>
    [Serializable]
    public abstract class UxmlSerializedData
    {
        [SerializeField, UxmlIgnore, HideInInspector]
        internal int uxmlAssetId;

        /// <summary>
        /// Returns an instance of the declaring element.
        /// </summary>
        /// <returns>The new instance of the declaring element.</returns>
        public abstract object CreateInstance();

        /// <summary>
        /// Applies serialized field values to a compatible visual element.
        /// </summary>
        /// <remarks>
        /// This is used by the code generator when a custom control is using the <see cref="UxmlElementAttribute"/>.
        /// </remarks>
        /// <param name="obj">The element to have the serialized data applied to.</param>
        public abstract void Deserialize(object obj);
    }

    abstract class UxmlSerializableAdapterBase
    {
        public abstract object dataBoxed { get; set; }
        public abstract object CopyBoxed(object value);
    }

    [Serializable]
    internal sealed class UxmlSerializableAdapter<T> : UxmlSerializableAdapterBase
    {
        public static readonly UxmlSerializableAdapter<T> SharedInstance = new UxmlSerializableAdapter<T>();

        public T data;

        public override object dataBoxed { get => data; set => data = (T)value; }

        public T CopyData(T value)
        {
            UxmlSerializableAdapter<T> copy = null;
            try
            {
                data = (T)value;
                var json = JsonUtility.ToJson(this);
                copy = JsonUtility.FromJson<UxmlSerializableAdapter<T>>(json);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                data = default;
            }

            return copy != null ? copy.data : default;
        }

        public override object CopyBoxed(object value) => CopyData((T)value);
    }

    /// <summary>
    /// This is used by the code generator when a custom control is using the <see cref="UxmlElementAttribute"/>.
    /// </summary>
    public static class UxmlSerializedDataUtility
    {
        internal static Dictionary<Type, UxmlSerializableAdapterBase> s_Adapters = new Dictionary<Type, UxmlSerializableAdapterBase>();

        /// <summary>
        /// This is used by the code generator when a custom control is using the <see cref="UxmlElementAttribute"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static object CopySerialized(object value)
        {
            if (value == null)
                return null;

            object copy = null;
            try
            {

                if (!s_Adapters.TryGetValue(value.GetType(), out var adapter))
                {
                    var adapterType = typeof(UxmlSerializableAdapter<>).MakeGenericType(value.GetType());

                    var field = adapterType.GetField("SharedInstance", BindingFlags.Static | BindingFlags.Public);
                    adapter = (UxmlSerializableAdapterBase)field.GetValue(null);
                    s_Adapters[value.GetType()] = adapter;
                }
                copy = adapter.CopyBoxed(value);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return copy;
        }

        public static T CopySerialized<T>(object value)
        {
            var adapter = UxmlSerializableAdapter<T>.SharedInstance;
            return adapter.CopyData((T)value);
        }
    }
}
