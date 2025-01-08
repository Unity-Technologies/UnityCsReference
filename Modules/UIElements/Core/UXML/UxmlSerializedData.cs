// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    internal interface IUxmlSerializedDataCustomAttributeHandler
    {
        void SerializeCustomAttributes(IUxmlAttributes bag, HashSet<string> handledAttributes);
    }

    /// <summary>
    /// Controls how a serialized class instance is copied to a UxmlAttribute field.
    /// </summary>
    /// <remarks>
    /// By default, Unity creates a deep copy of the class instance by serializing it into a JSON string using <see cref="JsonUtility.ToJson(object)"/> 
    /// and then deserializing it back to an instance with <see cref="JsonUtility.FromJson{T}(string)"/>. 
    /// The default process involves the Unity serializer and triggers callbacks such as <see cref="ISerializationCallbackReceiver"/>, which can be inefficient, especially for large objects or frequent copying.
    /// To optimize the process and performance, implement this interface which provides a more efficient copying mechanism.
    /// </remarks>
    internal interface IUxmlSerializedDataDeserializeReference
    {
        /// <summary>
        /// Returns a copy of a serialized class instance during deserialization for use in a UxmlAttribute field.
        /// </summary>
        /// <remarks>
        /// You can define the type of copy based on your usage scenarios:
        ///- Deep Copy: Returns independent copies, ideal for scenarios where you need to modify the clone.
        ///- Shallow Copy: For performance optimization when internal state changes are unnecessary. It creates a copy of some parts of an instance while sharing others. 
        ///- Shared Instance: Returns the original instance for read-only scenarios with no modifications.
        /// </remarks>
        /// <returns>A clone of the instance.</returns>
        object DeserializeReference();
    }

    /// <summary>
    /// Generates an instance of the declaring element when the <see cref="UxmlElementAttribute"/> is used in a custom control.
    /// </summary>
    [Serializable]
    public abstract class UxmlSerializedData
    {
        /// <summary>
        /// This is used by the code generator when a custom control is using the <see cref="UxmlElementAttribute"/>. You should not need to call it.
        /// </summary>
        public static void Register()
        {
        }

        internal const string AttributeFlagSuffix = "_UxmlAttributeFlags";
        const UxmlAttributeFlags k_DefaultFlags = UxmlAttributeFlags.OverriddenInUxml;

        /// <summary>
        /// Used to indicate if a field is overridden in a UXML file.
        /// </summary>
        [Flags]
        public enum UxmlAttributeFlags : byte
        {
            /// <summary>
            /// The serialized field is ignored and not applied during <see cref="Deserialize(object)"/>.
            /// </summary>
            Ignore = 0,

            /// <summary>
            /// The serialized field is overridden in a UXML file and will be applied during <see cref="Deserialize(object)"/>.
            /// </summary>
            OverriddenInUxml = 1,

            /// <summary>
            /// The serialized field contains the default value, it will be applied when editing in the UI Builder.
            /// </summary>
            DefaultValue = 2,
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [SerializeField, UxmlIgnore, HideInInspector]
        internal int uxmlAssetId;

        static UxmlAttributeFlags s_CurrentDeserializeFlags = k_DefaultFlags;

        /// <summary>
        /// Determines if an attribute should be applied during <see cref="Deserialize(object)"/>.
        /// </summary>
        /// <param name="attributeFlags"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ShouldWriteAttributeValue(UxmlAttributeFlags attributeFlags)
        {
            return (attributeFlags & s_CurrentDeserializeFlags) != 0;
        }

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

        /// <summary>
        /// Deserializes using custom attribute flags.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="flags"></param>
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal void Deserialize(object obj, UxmlAttributeFlags flags)
        {
            try
            {
                s_CurrentDeserializeFlags = flags;
                Deserialize(obj);
            }
            finally
            {
                s_CurrentDeserializeFlags = k_DefaultFlags;
            }
        }
    }

    abstract class UxmlSerializableAdapterBase
    {
        public abstract object dataBoxed { get; set; }
        public abstract object CloneInstanceBoxed(object value);
    }

    [Serializable]
    internal sealed class UxmlSerializableAdapter<T> : UxmlSerializableAdapterBase
    {
        public static readonly UxmlSerializableAdapter<T> SharedInstance = new UxmlSerializableAdapter<T>();

        public T data;

        public override object dataBoxed { get => data; set => data = (T)value; }

        public T CloneInstance(T value)
        {
            UxmlSerializableAdapter<T> copy = null;
            try
            {
                if (value is IUxmlSerializedDataDeserializeReference uxmlSerializedDataCopy)
                    return (T)uxmlSerializedDataCopy.DeserializeReference();

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

        public override object CloneInstanceBoxed(object value) => CloneInstance((T)value);
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
                copy = adapter.CloneInstanceBoxed(value);
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
            return adapter.CloneInstance((T)value);
        }
    }
}
