using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    public partial class VisualElement
    {
        private class TypeData
        {
            public Type type { get; }

            public TypeData(Type type)
            {
                this.type = type;
            }

            private string m_FullTypeName = string.Empty;
            private string m_TypeName = string.Empty;

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
