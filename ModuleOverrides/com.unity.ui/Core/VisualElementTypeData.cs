// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    public partial class VisualElement
    {
        internal class TypeData
        {
            public Type type { get; }

            public TypeData(Type type)
            {
                this.type = type;
            }

            private string m_FullTypeName = string.Empty;
            private string m_TypeName = string.Empty;
            private string m_TypeNamespace = string.Empty;

            public string fullTypeName
            {
                get
                {
                    if (string.IsNullOrEmpty(m_FullTypeName))
                        m_FullTypeName = type.FullName;
                    return m_FullTypeName;
                }
            }

            public string typeName
            {
                get
                {
                    if (string.IsNullOrEmpty(m_TypeName))
                    {
                        bool isGeneric = type.IsGenericType;
                        m_TypeName = type.Name;

                        if (isGeneric)
                        {
                            int genericTypeIndex = m_TypeName.IndexOf('`');
                            if (genericTypeIndex >= 0)
                            {
                                m_TypeName = m_TypeName.Remove(genericTypeIndex);
                            }
                        }
                    }

                    return m_TypeName;
                }
            }
            public string typeNamespace
            {
                get
                {
                    if (string.IsNullOrEmpty(m_TypeNamespace))
                        m_TypeNamespace = type.Namespace;
                    return m_TypeNamespace;
                }
            }
        }

        internal static TypeData GetOrCreateTypeData(Type t)
        {
            if (!s_TypeData.TryGetValue(t, out var data))
            {
                data = new TypeData(t);
                s_TypeData.Add(t, data);
            }

            return data;
        }

        private static readonly Dictionary<Type, TypeData> s_TypeData = new Dictionary<Type, TypeData>();
        private TypeData m_TypeData;

        private TypeData typeData
        {
            get
            {
                if (m_TypeData == null)
                {
                    var type = GetType();
                    if (!s_TypeData.TryGetValue(type, out m_TypeData))
                    {
                        m_TypeData = new TypeData(type);
                        s_TypeData.Add(type, m_TypeData);
                    }
                }

                return m_TypeData;
            }
        }
    }
}
