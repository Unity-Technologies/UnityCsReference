// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe.NotBurstCompatible;
using Unity.Properties;

namespace Unity.Entities.Serialization.Binary
{
    unsafe class BinaryPropertyWriter : BinaryPropertyVisitor,
        IPropertyBagVisitor,
        IListPropertyBagVisitor,
        ISetPropertyBagVisitor,
        IDictionaryPropertyBagVisitor,
        IPropertyVisitor,
        IBinaryPropertyWriter
    {
        UnsafeAppendBuffer* m_Stream;
        Type m_SerializedType;
        bool m_DisableRootAdapters;
        BinaryAdapterCollection m_Adapters;
        SerializedReferences m_SerializedReferences;

        public UnsafeAppendBuffer* Writer => m_Stream;

        public void SetStream(UnsafeAppendBuffer* stream)
            => m_Stream = stream;

        public void SetSerializedType(Type type)
            => m_SerializedType = type;

        public void SetDisableRootAdapters(bool disableRootAdapters)
            => m_DisableRootAdapters = disableRootAdapters;

        public void SetGlobalAdapters(List<IBinaryAdapter> adapters)
            => m_Adapters.Global = adapters;

        public void SetUserDefinedAdapters(List<IBinaryAdapter> adapters)
            => m_Adapters.UserDefined = adapters;

        public void SetSerializedReferences(SerializedReferences serializedReferences)
            => m_SerializedReferences = serializedReferences;

        public BinaryPropertyWriter()
        {
            m_Adapters.Internal = this;
        }

        void IPropertyBagVisitor.Visit<TContainer>(IPropertyBag<TContainer> properties, ref TContainer container)
        {
            foreach (var property in properties.GetProperties(ref container))
            {
                if (PropertyChecks.IsPropertyExcludedFromSerialization(property))
                    continue;

                property.Accept(this, ref container);
            }
        }

        void IListPropertyBagVisitor.Visit<TList, TElement>(IListPropertyBag<TList, TElement> properties, ref TList container)
        {
            m_Stream->Add(container.Count);

            for (var i = 0; i < container.Count; i++)
            {
                WriteValue(container[i]);
            }
        }

        void ISetPropertyBagVisitor.Visit<TSet, TValue>(ISetPropertyBag<TSet, TValue> properties, ref TSet container)
        {
            m_Stream->Add(container.Count);

            foreach (var value in container)
            {
                WriteValue(value);
            }
        }

        void IDictionaryPropertyBagVisitor.Visit<TDictionary, TKey, TValue>(IDictionaryPropertyBag<TDictionary, TKey, TValue> properties, ref TDictionary container)
        {
            m_Stream->Add(container.Count);

            foreach (var kvp in container)
            {
                WriteValue(kvp.Key);
                WriteValue(kvp.Value);
            }
        }

        void IPropertyVisitor.Visit<TContainer, TValue>(Property<TContainer, TValue> property, ref TContainer container)
        {
            var isRootProperty = property is IPropertyWrapper;
            WriteValue(property.GetValue(ref container), isRootProperty);
        }

        public void WriteValue<TValue>(TValue value, bool isRoot = false)
        {
            if (!(isRoot && m_DisableRootAdapters))
                WriteValueWithAdapters(value, m_Adapters.GetEnumerator(), isRoot);
            else
                WriteValueWithoutAdapters(value, true);
        }

        public void WriteValueWithAdapters<TValue>(TValue value, BinaryAdapterCollection.Enumerator adapters, bool isRoot)
        {
            while (adapters.MoveNext())
            {
                switch (adapters.Current)
                {
                    case IBinaryAdapter<TValue> typed:
                        typed.Serialize(new BinarySerializationContext<TValue>(this, adapters, value, isRoot), value);
                        return;
                    case IContravariantBinaryAdapter<TValue> typedContravariant:
                        // NOTE: Boxing
                        typedContravariant.Serialize((IBinarySerializationContext) new BinarySerializationContext<TValue>(this, adapters, value, isRoot), value);
                        return;
                }
            }

            // Do the default thing.
            WriteValueWithoutAdapters(value, isRoot);
        }

        public void WriteValueWithoutAdapters<TValue>(TValue value, bool isRoot)
        {
            if (TypeTraits<TValue>.IsEnum)
            {
                BinarySerialization.WritePrimitiveUnsafe(m_Stream, ref value, Enum.GetUnderlyingType(typeof(TValue)));
                return;
            }

            if (TypeTraits<TValue>.CanBeNull && null == value)
            {
                m_Stream->Add(k_TokenNull);
                return;
            }

            if (TypeTraits<TValue>.IsMultidimensionalArray)
            {
                // No support for multidimensional arrays yet. This can be done using adapters for now.
                m_Stream->Add(k_TokenNull);
                return;
            }

            if (TypeTraits<TValue>.IsNullable)
            {
                m_Stream->Add(k_TokenNone);

                var underlyingType = Nullable.GetUnderlyingType(typeof(TValue));

                if (TypeTraits.IsContainer(underlyingType))
                {
                    // IMPORTANT: Do NOT add a token to the stream since we are triggering a re-entrance on the same object here.
                    // Unpack Nullable<T> as T
                    var underlyingValue = Convert.ChangeType(value, underlyingType);

                    if (!PropertyContainer.TryAccept(this, ref underlyingValue, out var errorCode))
                    {
                        switch (errorCode)
                        {
                            case VisitReturnCode.NullContainer:
                                throw new ArgumentNullException(nameof(value));
                            case VisitReturnCode.InvalidContainerType:
                                throw new InvalidContainerTypeException(value.GetType());
                            case VisitReturnCode.MissingPropertyBag:
                                throw new MissingPropertyBagException(value.GetType());
                            default:
                                throw new Exception($"Unexpected {nameof(VisitReturnCode)}=[{errorCode}]");
                        }
                    }
                }
                else
                {
                    BinarySerialization.WritePrimitiveBoxed(m_Stream, value, underlyingType);
                }

                return;
            }

            if (!TypeTraits<TValue>.IsValueType)
            {
                if (!(isRoot && m_DisableRootAdapters) && value is UnityEngine.Object unityEngineObject)
                {
                    // Special path for polymorphic unity object references.
                    m_Stream->Add(k_TokenUnityEngineObjectReference);
                    
                    var adapters = m_Adapters.GetEnumerator();

                    while (adapters.MoveNext())
                    {
                        if (adapters.Current is IContravariantBinaryAdapter<UnityEngine.Object> unityObjectAdapter)
                        {
                            unityObjectAdapter.Serialize(new BinarySerializationContext<TValue>(this, default, value, isRoot), unityEngineObject);
                            break;
                        }
                    }
                    
                    return;
                }

                if (null != m_SerializedReferences && !value.GetType().IsValueType)
                {
                    // At this point we don't know if an object will reference this value.
                    // To avoid a second visitation pass we always create an entry for each managed object reference.
                    var id = m_SerializedReferences.AddSerializedReference(value);

                    if (!m_SerializedReferences.SetSerialized(value))
                    {
                        // This is the second time encountering this object during serialization.
                        // We instead of writing out the object we simply write the id.
                        m_Stream->Add(k_TokenSerializedReference);
                        m_Stream->Add(id);
                        return;
                    }
                }

                // This is a very common case. At serialize time we are serializing something that is polymorphic or an object
                // However at deserialize time the user known the System.Type, we can avoid writing out the fully qualified type name in this case.
                var isRootAndTypeWasGiven = isRoot && null != m_SerializedType;

                if (typeof(TValue) != value.GetType() && !isRootAndTypeWasGiven)
                {
                    m_Stream->Add(k_TokenPolymorphic);
                    m_Stream->AddNBC(value.GetType().AssemblyQualifiedName);
                }
                else
                {
                    m_Stream->Add(k_TokenNone);
                }
            }

            if (TypeTraits<TValue>.IsObject && !TypeTraits.IsContainer(value.GetType()))
            {
                BinarySerialization.WritePrimitiveBoxed(m_Stream, value, value.GetType());
            }
            else
            {
                if (!PropertyContainer.TryAccept(this, ref value, out var errorCode))
                {
                    switch (errorCode)
                    {
                        case VisitReturnCode.NullContainer:
                            throw new ArgumentNullException(nameof(value));
                        case VisitReturnCode.InvalidContainerType:
                            throw new InvalidContainerTypeException(value.GetType());
                        case VisitReturnCode.MissingPropertyBag:
                            throw new MissingPropertyBagException(value.GetType());
                        default:
                            throw new Exception($"Unexpected {nameof(VisitReturnCode)}=[{errorCode}]");
                    }
                }
            }
        }
    }
}
