// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal
    abstract class BaseTypeFactory<TKey, TValue>
    {
        private readonly Dictionary<Type, Type> m_Mappings = new Dictionary<Type, Type>();
        private readonly Type m_FallbackType;
        private static readonly Type k_KeyType;
        private static readonly Type k_ValueType;

        static BaseTypeFactory()
        {
            k_KeyType = typeof(TKey);
            k_ValueType = typeof(TValue);
        }

        public Type this[Type t]
        {
            get
            {
                try
                {
                    return m_Mappings[t];
                }
                catch (KeyNotFoundException e)
                {
                    throw new KeyNotFoundException("Type " + t.Name + " is not registered in the factory.", e);
                }
            }
            set
            {
                if (!t.IsSubclassOf(k_KeyType) && !t.GetInterfaces().Contains(k_KeyType))
                {
                    throw new ArgumentException("The type passed as key (" + t.Name + ") does not implement or derive from " + k_KeyType.Name + ".");
                }

                if (!value.IsSubclassOf(k_ValueType))
                {
                    throw new ArgumentException("The type passed as value (" + value.Name + ") does not derive from " + k_ValueType.Name + ".");
                }

                m_Mappings[t] = value;
            }
        }

        public virtual TValue Create(TKey key)
        {
            Type valueType = null;
            Type keyType = key.GetType();

            while (valueType == null && keyType != null && keyType != typeof(TKey))
            {
                if (!m_Mappings.TryGetValue(keyType, out valueType))
                {
                    keyType = keyType.BaseType;
                }
            }

            if (valueType == null)
            {
                valueType = m_FallbackType;
            }

            return InternalCreate(valueType);
        }

        protected BaseTypeFactory()
            : this(typeof(TValue))
        {}

        protected BaseTypeFactory(Type fallbackType)
        {
            m_FallbackType = fallbackType;
        }

        protected virtual TValue InternalCreate(Type valueType)
        {
            return (TValue)Activator.CreateInstance(valueType);
        }
    }
}
