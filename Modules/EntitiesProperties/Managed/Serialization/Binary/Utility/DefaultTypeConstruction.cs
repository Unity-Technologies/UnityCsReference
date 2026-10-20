// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;

namespace Unity.Entities.Serialization
{
    /// <summary>
    /// Interface to allow the serialization backend to provide type information to the type construction system.
    /// </summary>
    interface ISerializedTypeProvider
    {
        /// <summary>
        /// Returns the <see cref="System.Type"/> resolved by the serialization backend.
        /// </summary>
        /// <returns>The <see cref="System.Type"/> which was serialized.</returns>
        Type GetSerializedType();
        
        /// <summary>
        /// Returns the array length. 
        /// </summary>
        /// <returns></returns>
        int GetArrayLength();
        
        object GetDefaultObject();
    }
    
    static class DefaultTypeConstruction
    {
        internal static void Construct<TValue>(ref TValue value, ISerializedTypeProvider provider)
        {
            if (TypeTraits<TValue>.IsValueType)
            {
                if (!(TypeTraits<TValue>.IsNullable && TypeTraits.IsContainer(Nullable.GetUnderlyingType(typeof(TValue)))))
                {
                    return;
                }
            }
            
            var serializedType = provider.GetSerializedType();

            if (null != serializedType) 
            {
                if (!typeof(TValue).IsAssignableFrom(serializedType))
                {
                    throw new ArgumentException($"Type mismatch. DeclaredType=[{typeof(TValue)}] SerializedType=[{serializedType}]");
                }

                ConstructFromSerializedType(ref value, serializedType, provider);
                return;
            }
            
            if (TypeTraits<TValue>.IsObject && null == value)
            {
                value = (TValue) provider.GetDefaultObject();
                return;
            }
            
            if (TypeTraits<TValue>.IsAbstractOrInterface)
            {
                throw new ArgumentException();
            }

            ConstructFromDeclaredType(ref value, provider);
        }

        static void ConstructFromSerializedType<TValue>(ref TValue value, Type type, ISerializedTypeProvider provider)
        {
            if (type.IsArray)
            {
                var count = provider.GetArrayLength();
                value = TypeUtility.InstantiateArray<TValue>(type, count);
                return;
            }
            
            if (null != value && value.GetType() == type)
            {
                return;
            }

            value = TypeUtility.Instantiate<TValue>(type);
        }

        static void ConstructFromDeclaredType<TValue>(ref TValue value, ISerializedTypeProvider provider)
        {
            if (typeof(TValue).IsArray)
            {
                var count = provider.GetArrayLength();
                
                if (null == value || (value as Array)?.Length != count)
                {
                    value = TypeUtility.InstantiateArray<TValue>(count);
                    return;
                }

                return;
            }

            if (null == value)
            {
                value = TypeUtility.Instantiate<TValue>();
            }
        }
    }
}
