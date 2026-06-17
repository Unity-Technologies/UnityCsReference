// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
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
        public abstract object CloneInstanceBoxed(object value);
    }

    [Serializable]
    internal sealed class UxmlSerializableAdapter<T> : UxmlSerializableAdapterBase
    {
        public T data;

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

    [Serializable]
    internal sealed class UxmlSerializableDeserializeReferenceListAdapter<TElement> : UxmlSerializableAdapterBase
        where TElement : IUxmlSerializedDataDeserializeReference
    {

        public override object CloneInstanceBoxed(object value)
        {
            try
            {
                if (value is TElement[] array)
                {
                    var cloned = new TElement[array.Length];
                    for (int i = 0; i < array.Length; i++)
                        cloned[i] = array[i] is IUxmlSerializedDataDeserializeReference r ? (TElement)r.DeserializeReference() : default;
                    return cloned;
                }

                if (value is List<TElement> list)
                {
                    var cloned = new List<TElement>(list.Count);
                    foreach (var item in list)
                        cloned.Add(item is IUxmlSerializedDataDeserializeReference r ? (TElement)r.DeserializeReference() : default);
                    return cloned;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return null;
        }
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
                    adapter = CreateAdapter(value.GetType());
                copy = adapter.CloneInstanceBoxed(value);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return copy;
        }

        public static T CopySerialized<T>(object value) =>
            (T)UxmlSerializableCopyAdapter<T>.Instance.CloneInstanceBoxed(value);

        static class UxmlSerializableCopyAdapter<T>
        {
            public static readonly UxmlSerializableAdapterBase Instance = Register();

            static UxmlSerializableAdapterBase Register()
            {
                Type elementType = null;
                if (typeof(T).IsArray)
                    elementType = typeof(T).GetElementType();
                else if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
                    elementType = typeof(T).GetGenericArguments()[0];

                UxmlSerializableAdapterBase adapter;
                if (elementType != null && typeof(IUxmlSerializedDataDeserializeReference).IsAssignableFrom(elementType))
                    adapter = (UxmlSerializableAdapterBase)Activator.CreateInstance(
                        typeof(UxmlSerializableDeserializeReferenceListAdapter<>).MakeGenericType(elementType));
                else
                    adapter = new UxmlSerializableAdapter<T>();

                s_Adapters[typeof(T)] = adapter;
                return adapter;
            }
        }

        static UxmlSerializableAdapterBase CreateAdapter(Type type)
        {
            RuntimeHelpers.RunClassConstructor(typeof(UxmlSerializableCopyAdapter<>).MakeGenericType(type).TypeHandle);
            return s_Adapters[type];
        }
    }
}
