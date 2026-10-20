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
    unsafe class BinaryPropertyReader : BinaryPropertyVisitor,
        ISerializedTypeProvider,
        IPropertyBagVisitor,
        IListPropertyBagVisitor,
        ISetPropertyBagVisitor,
        IDictionaryPropertyBagVisitor,
        IPropertyVisitor,
        IBinaryPropertyReader
    {
        UnsafeAppendBuffer.Reader* m_Stream;
        Type m_SerializedType;
        bool m_DisableRootAdapters;
        BinaryAdapterCollection m_Adapters;
        SerializedReferences m_SerializedReferences;

        public UnsafeAppendBuffer.Reader* Reader => m_Stream;

        public void SetStream(UnsafeAppendBuffer.Reader* stream)
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

        public BinaryPropertyReader()
        {
            m_Adapters.Internal = this;
        }

        void IPropertyBagVisitor.Visit<TContainer>(IPropertyBag<TContainer> properties, ref TContainer container)
        {
            m_SerializedReferences?.AddDeserializedReference(container);

            foreach (var property in properties.GetProperties(ref container))
            {
                if (PropertyChecks.IsPropertyExcludedFromSerialization(property))
                    continue;

                property.Accept(this, ref container);
            }
        }

        void IListPropertyBagVisitor.Visit<TList, TElement>(IListPropertyBag<TList, TElement> properties, ref TList container)
        {
            m_SerializedReferences?.AddDeserializedReference(container);

            var count = m_Stream->ReadNext<int>();

            if (typeof(TList).IsArray)
            {
                for (var i = 0; i < count; i++)
                {
                    container[i] = ReadValue<TElement>();
                }
            }
            else
            {
                container.Clear();
                for (var i = 0; i < count; i++)
                {
                    container.Add(ReadValue<TElement>());
                }
            }
        }

        void ISetPropertyBagVisitor.Visit<TSet, TValue>(ISetPropertyBag<TSet, TValue> properties, ref TSet container)
        {
            m_SerializedReferences?.AddDeserializedReference(container);

            container.Clear();
            var count = m_Stream->ReadNext<int>();

            for (var i = 0; i < count; i++)
            {
                container.Add(ReadValue<TValue>());
            }
        }

        void IDictionaryPropertyBagVisitor.Visit<TDictionary, TKey, TValue>(IDictionaryPropertyBag<TDictionary, TKey, TValue> properties, ref TDictionary container)
        {
            m_SerializedReferences?.AddDeserializedReference(container);

            container.Clear();
            var count = m_Stream->ReadNext<int>();

            for (var i = 0; i < count; i++)
            {
                container.Add(ReadValue<TKey>(), ReadValue<TValue>());
            }
        }

        void IPropertyVisitor.Visit<TContainer, TValue>(Property<TContainer, TValue> property, ref TContainer container)
        {
            var value = property.GetValue(ref container);
            var isRoot = property is IPropertyWrapper;

            ReadValue(ref value, isRoot);

            if (!property.IsReadOnly)
            {
                property.SetValue(ref container, value);
            }
            else if (PropertyChecks.CheckReadOnlyPropertyForDeserialization(property, ref container, ref value, out var error))
            {
                throw new SerializationException(error);
            }
        }

        TValue ReadValue<TValue>()
        {
            var value = default(TValue);
            ReadValue(ref value);
            return value;
        }

        public void ReadValue<TValue>(ref TValue value, bool isRoot = false)
        {
            if (!(isRoot && m_DisableRootAdapters))
                ReadValueWithAdapters(ref value, m_Adapters.GetEnumerator(), isRoot);
            else
                ReadValueWithoutAdapters(ref value, true);
        }

        public void ReadValueWithAdapters<TValue>(ref TValue value, BinaryAdapterCollection.Enumerator adapters, bool isRoot)
        {
            while (adapters.MoveNext())
            {
                switch (adapters.Current)
                {
                    case IBinaryAdapter<TValue> typed:
                        value = typed.Deserialize(new BinaryDeserializationContext<TValue>(this, adapters, isRoot));
                        return;
                    case IContravariantBinaryAdapter<TValue> typedContravariant:
                        // NOTE: Boxing
                        value = (TValue) typedContravariant.Deserialize((IBinaryDeserializationContext) new BinaryDeserializationContext<TValue>(this, adapters, isRoot));
                        return;
                }
            }

            ReadValueWithoutAdapters(ref value, isRoot);
        }

        public void ReadValueWithoutAdapters<TValue>(ref TValue value, bool isRoot)
        {
            if (TypeTraits<TValue>.IsEnum)
            {
                BinarySerialization.ReadPrimitiveUnsafe(m_Stream, ref value, Enum.GetUnderlyingType(typeof(TValue)));
                return;
            }

            var token = default(byte);

            if (TypeTraits<TValue>.CanBeNull)
            {
                token = m_Stream->ReadNext<byte>();

                switch (token)
                {
                    case k_TokenNull:
                        value = default;
                        return;
                    case k_TokenSerializedReference:
                        var id = m_Stream->ReadNext<int>();
                        if (null == m_SerializedReferences)
                            throw new Exception("Deserialization encountered a serialized object reference while running with DisableSerializedReferences.");
                        value = (TValue) m_SerializedReferences.GetDeserializedReference(id);
                        return;
                }
            }

            if (TypeTraits<TValue>.IsNullable)
            {
                var underlyingType = Nullable.GetUnderlyingType(typeof(TValue));

                if (TypeTraits.IsContainer(underlyingType))
                {
                    m_SerializedTypeProviderSerializedType = underlyingType;
                    DefaultTypeConstruction.Construct(ref value, this);

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

                    // Repack the T as Nullable<T>
                    value = (TValue) underlyingValue;
                }
                else
                {
                    BinarySerialization.ReadPrimitiveBoxed(m_Stream, ref value, Nullable.GetUnderlyingType(typeof(TValue)));
                }
                return;
            }

            if (!(isRoot && m_DisableRootAdapters) && token == k_TokenUnityEngineObjectReference)
            {
                var adapters = m_Adapters.GetEnumerator();

                while (adapters.MoveNext())
                {
                    if (adapters.Current is IContravariantBinaryAdapter<UnityEngine.Object> unityObjectAdapter)
                    {
                        // Special path for polymorphic unity object references.
                        value = (TValue) unityObjectAdapter.Deserialize(new BinaryDeserializationContext<TValue>(this, default, isRoot));
                        break;
                    }
                }
                
                return;
            }

            if (token == k_TokenPolymorphic)
            {
                m_Stream->ReadNextNBC(out var assemblyQualifiedTypeName);

                if (string.IsNullOrEmpty(assemblyQualifiedTypeName))
                {
                    throw new ArgumentException();
                }

                var concreteType = Type.GetType(assemblyQualifiedTypeName);

                if (null == concreteType)
                {
                    if (FormerNameAttribute.TryGetCurrentTypeName(assemblyQualifiedTypeName, out var currentAssemblyQualifiedTypeName))
                    {
                        concreteType = Type.GetType(currentAssemblyQualifiedTypeName);
                    }

                    if (null == concreteType)
                    {
                        throw new ArgumentException();
                    }
                }

                m_SerializedTypeProviderSerializedType = concreteType;
            }
            else
            {
                // If we have a user provided root type pass it to the type construction.
                m_SerializedTypeProviderSerializedType = isRoot ? m_SerializedType : null;
            }

            DefaultTypeConstruction.Construct(ref value, this);

            if (TypeTraits<TValue>.IsObject && !TypeTraits.IsContainer(value.GetType()))
            {
                BinarySerialization.ReadPrimitiveBoxed(m_Stream, ref value, value.GetType());
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

        Type m_SerializedTypeProviderSerializedType;

        Type ISerializedTypeProvider.GetSerializedType()
        {
            return m_SerializedTypeProviderSerializedType;
        }

        int ISerializedTypeProvider.GetArrayLength()
        {
            var pos = m_Stream->Offset;
            var count = m_Stream->ReadNext<int>();
            m_Stream->Offset = pos;
            return count;
        }

        object ISerializedTypeProvider.GetDefaultObject()
        {
            throw new InvalidOperationException();
        }
    }
}
