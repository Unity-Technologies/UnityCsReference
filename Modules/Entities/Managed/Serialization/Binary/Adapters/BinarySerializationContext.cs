// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
﻿using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities.Serialization.Binary
{
    /// <summary>
    /// The visitor behind <see cref="BinarySerializationContext{T}"/>. PropertiesModule implements it, because writing a value walks it with Unity.Properties.
    /// </summary>
    [VisibleToOtherModules("UnityEngine.EntitiesPropertiesModule")]
    internal unsafe interface IBinaryPropertyWriter
    {
        UnsafeAppendBuffer* Writer { get; }
        void WriteValue<TValue>(TValue value, bool isRoot = false);
        void WriteValueWithAdapters<TValue>(TValue value, BinaryAdapterCollection.Enumerator adapters, bool isRoot);
        void WriteValueWithoutAdapters<TValue>(TValue value, bool isRoot);
    }

    /// <summary>
    /// The visitor behind <see cref="BinaryDeserializationContext{T}"/>. PropertiesModule implements it, because reading a value walks it with Unity.Properties.
    /// </summary>
    [VisibleToOtherModules("UnityEngine.EntitiesPropertiesModule")]
    internal unsafe interface IBinaryPropertyReader
    {
        UnsafeAppendBuffer.Reader* Reader { get; }
        void ReadValue<TValue>(ref TValue value, bool isRoot = false);
        void ReadValueWithAdapters<TValue>(ref TValue value, BinaryAdapterCollection.Enumerator adapters, bool isRoot);
        void ReadValueWithoutAdapters<TValue>(ref TValue value, bool isRoot);
    }

    /// <summary>
    /// The <see cref="IBinarySerializationContext"/> provides an untyped context for contravariant serialization adapters.
    /// </summary>
    [VisibleToOtherModules("UnityEngine.EntitiesPropertiesModule")]
    internal unsafe interface IBinarySerializationContext
    {
        /// <summary>
        /// Gets the underlying <see cref="UnsafeAppendBuffer"/> which can be used to output data.
        /// </summary>
        UnsafeAppendBuffer* Writer { get; }

        /// <summary>
        /// Continues serialization for the current value. This will run the next adapter in the sequence, or the default behaviour.
        /// </summary>
        void ContinueVisitation();

        /// <summary>
        /// Continues serialization for the current type without running any more adapters. This will perform the default behaviour.
        /// </summary>
        void ContinueVisitationWithoutAdapters();
        
        /// <summary>
        /// Writes the given <paramref name="value"/> to the stream. This will run all adapters.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <typeparam name="T">The value type to write.</typeparam>
        void SerializeValue<T>(T value);
    }

    /// <summary>
    /// The <see cref="BinarySerializationContext{T}"/> is available from adapters. It provides access to the current adapter enumerator and allows for control of serialization for a given type. 
    /// </summary>
    /// <typeparam name="TValue">The value type being serialized.</typeparam>
    [VisibleToOtherModules("UnityEngine.EntitiesPropertiesModule")]
    internal readonly unsafe struct BinarySerializationContext<TValue> : IBinarySerializationContext
    {
        readonly IBinaryPropertyWriter m_Visitor;
        readonly BinaryAdapterCollection.Enumerator m_Adapters;
        readonly TValue m_Value;
        readonly bool m_IsRoot;

        /// <summary>
        /// Gets the underlying <see cref="UnsafeAppendBuffer"/> which can be used to output data.
        /// </summary>
        public UnsafeAppendBuffer* Writer => m_Visitor.Writer;

        [VisibleToOtherModules("UnityEngine.EntitiesPropertiesModule")]
        internal BinarySerializationContext(IBinaryPropertyWriter visitor, BinaryAdapterCollection.Enumerator adapters, TValue value, bool isRoot)
        {
            m_Visitor = visitor;
            m_Adapters = adapters;
            m_Value = value;
            m_IsRoot = isRoot;
        }

        /// <summary>
        /// Continues visitation for the current type. This will run the next adapter in the sequence, or the default behaviour.
        /// </summary>
        public void ContinueVisitation()
            => m_Visitor.WriteValueWithAdapters(m_Value, m_Adapters, m_IsRoot);

        /// <summary>
        /// Continues visitation for the current type without running any more adapters. This will perform the default behaviour.
        /// </summary>
        public void ContinueVisitationWithoutAdapters()
            => m_Visitor.WriteValueWithoutAdapters(m_Value, m_IsRoot);

        /// <summary>
        /// Writes the given <paramref name="value"/> to the stream. This will run all adapters.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <typeparam name="T">The value type to write.</typeparam>
        public void SerializeValue<T>(T value)
            => m_Visitor.WriteValue(value);
    }

    /// <summary>
    /// The <see cref="IBinaryDeserializationContext"/> provides an untyped context for contravariant serialization adapters.
    /// </summary>
    [VisibleToOtherModules("UnityEngine.EntitiesPropertiesModule")]
    internal unsafe interface IBinaryDeserializationContext
    {
        /// <summary>
        /// Gets the serialized view for value being deserialized. 
        /// </summary>
        UnsafeAppendBuffer.Reader* Reader { get; }

        /// <summary>
        /// Continues de-serialization for the current value. This will run the next adapter in the sequence, or the default behaviour.
        /// </summary>
        /// <returns>The deserialized value.</returns>
        object ContinueVisitation();
        
        /// <summary>
        /// Continues de-serialization for the current type without running any more adapters. This will perform the default behaviour.
        /// </summary>
        /// <returns>The deserialized value.</returns>
        object ContinueVisitationWithoutAdapters();

        /// <summary>
        /// Reads the next value in the stream as <typeparamref name="T"/> and returns it. This will run all adapters.
        /// </summary>
        /// <typeparam name="T">The type of the value being deserialized.</typeparam>
        /// <returns>The instance of type <see cref="T"/> deserialized.</returns>
        T DeserializeValue<T>();
    }

    /// <summary>
    /// The <see cref="BinaryDeserializationContext{T}"/> is available from adapters. It provides access to the current adapter enumerator and allows for control of deserialization for a given type. 
    /// </summary>
    /// <typeparam name="TValue">The value type being deserialized.</typeparam>
    [VisibleToOtherModules("UnityEngine.EntitiesPropertiesModule")]
    internal readonly unsafe struct BinaryDeserializationContext<TValue> : IBinaryDeserializationContext
    {
        readonly IBinaryPropertyReader m_Visitor;
        readonly BinaryAdapterCollection.Enumerator m_Adapters;
        readonly bool m_IsRoot;

        /// <summary>
        /// The in-memory representation of the value being deserialized.
        /// </summary>
        public UnsafeAppendBuffer.Reader* Reader => m_Visitor.Reader;

        [VisibleToOtherModules("UnityEngine.EntitiesPropertiesModule")]
        internal BinaryDeserializationContext(IBinaryPropertyReader visitor, BinaryAdapterCollection.Enumerator adapters, bool isRoot)
        {
            m_Visitor = visitor;
            m_Adapters = adapters;
            m_IsRoot = isRoot;
        }

        /// <summary>
        /// Continues visitation for the current type. This will run the next adapter in the sequence, or the default behaviour and return the deserialized value.
        /// </summary>
        /// <returns>The deserialized value.</returns>
        public TValue ContinueVisitation()
        {
            var value = default(TValue);
            m_Visitor.ReadValueWithAdapters(ref value, m_Adapters, m_IsRoot);
            return value;
        }

        /// <summary>
        /// Continues visitation for the current type. This will run the next adapter in the sequence, or the default behaviour and return the deserialized value.
        /// </summary>
        /// <param name="value">The value being deserialized.</param>
        public void ContinueVisitation(ref TValue value)
        {
            m_Visitor.ReadValueWithAdapters(ref value, m_Adapters, m_IsRoot);
        }

        /// <summary>
        /// Continues visitation for the current type. This will run the next adapter in the sequence, or the default behaviour and return the deserialized value.
        /// </summary>
        /// <returns>The deserialized value.</returns>
        public TValue ContinueVisitationWithoutAdapters()
        {
            var value = default(TValue);
            m_Visitor.ReadValueWithoutAdapters(ref value, m_IsRoot);
            return value;
        }
        
        /// <summary>
        /// Continues visitation for the current type. This will invoke the default behaviour and return the deserialized value.
        /// </summary>
        /// <param name="value">The value being deserialized.</param>
        public void ContinueVisitationWithoutAdapters(ref TValue value)
        {
            m_Visitor.ReadValueWithoutAdapters(ref value, m_IsRoot);
        }

        /// <inheritdoc cref="IBinaryDeserializationContext.ContinueVisitation"/>
        object IBinaryDeserializationContext.ContinueVisitation() => ContinueVisitation();
        
        /// <inheritdoc cref="IBinaryDeserializationContext.ContinueVisitationWithoutAdapters"/>
        object IBinaryDeserializationContext.ContinueVisitationWithoutAdapters() => ContinueVisitationWithoutAdapters();
        
        /// <summary>
        /// Reads the next value in the stream as <typeparamref name="T"/> and returns it. This will run all adapters.
        /// </summary>
        /// <typeparam name="T">The type of the value being deserialized.</typeparam>
        /// <returns>The instance of type <see cref="T"/> deserialized.</returns>
        public T DeserializeValue<T>()
        {
            var value = default(T);
            m_Visitor.ReadValue(ref value);
            return value;
        }
    }
}
