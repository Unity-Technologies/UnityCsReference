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
    public partial class VisualElement
    {
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal class TypeData
        {
            public Type type { get; }

            private readonly DefaultEventInterests m_DefaultEventInterests;
            internal DefaultEventInterests defaultEventInterests => m_DefaultEventInterests;

            internal bool hasContainsPoint { get; }

            internal IEventInvoker selfEventInvoker { get; }

            public TypeData(Type type)
            {
                this.type = type;

                EventInterestReflectionUtils.GetDefaultEventInterests(type, out m_DefaultEventInterests, out var parentTypeData);

                hasContainsPoint =
                    type.GetMethod(nameof(ContainsPoint), BindingFlags.Instance | BindingFlags.Public)
                        ?.DeclaringType != typeof(VisualElement);

                try
                {
                    selfEventInvoker = (IEventInvoker)typeof(EventSelfArg<>).MakeGenericType(type)
                                           .GetMethod(nameof(EventSelfArg<VisualElement>.GetSelfInvoker))
                                           ?.Invoke(null, Array.Empty<object>()) ??
                                       parentTypeData?.selfEventInvoker ??
                                       EventSelfArg<VisualElement>.GetSelfInvoker();
                }
                catch (Exception)
                {
                    // Fall back on parent type. If we're compiling for il2cpp, we assume that the static analysis
                    // will have found all the valid EventSelfArg<TElement> in the wild, which will make this invoker
                    // use the most derived invoker that could be used to register a callback (hence, a valid one).
                    selfEventInvoker = parentTypeData?.selfEventInvoker ?? EventSelfArg<VisualElement>.GetSelfInvoker();
                }
            }

            private string m_FullTypeName = string.Empty;
            private string m_TypeName = string.Empty;
            private int m_TypeNameId = -1;

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

            public int typeNameId
            {
                get
                {
                    if (m_TypeNameId == -1)
                    {
                        // Access typeName property to ensure it's initialized first
                        var name = typeName;
                        m_TypeNameId = new UniqueStyleString(name).id;
                    }

                    return m_TypeNameId;
                }
            }
        }

        private readonly TypeData m_TypeData;
        internal TypeData typeData => m_TypeData;

        private class TypeReferenceComparer : IEqualityComparer<Type>
        {
            public bool Equals(Type x, Type y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(Type obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
        private static readonly Dictionary<Type, TypeData> s_TypeData = new(new TypeReferenceComparer());

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static TypeData GetOrCreateTypeData(Type t)
        {
            if (!s_TypeData.TryGetValue(t, out var data))
            {
                data = new TypeData(t);
                s_TypeData.Add(t, data);
            }

            return data;
        }
    }
}
