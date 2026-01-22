// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Scripting;

namespace Unity.Properties.Internal
{
    class ReflectedPropertyBagProvider
    {
        readonly MethodInfo m_CreatePropertyMethod;
        readonly MethodInfo m_CreatePropertyBagMethod;
        readonly MethodInfo m_CreateIndexedCollectionPropertyBagMethod;
        readonly MethodInfo m_CreateSetPropertyBagMethod;
        readonly MethodInfo m_CreateKeyValueCollectionPropertyBagMethod;
        readonly MethodInfo m_CreateKeyValuePairPropertyBagMethod;
        readonly MethodInfo m_CreateArrayPropertyBagMethod;
        readonly MethodInfo m_CreateListPropertyBagMethod;
        readonly MethodInfo m_CreateHashSetPropertyBagMethod;
        readonly MethodInfo m_CreateDictionaryPropertyBagMethod;

        public ReflectedPropertyBagProvider()
        {
            m_CreatePropertyMethod = typeof(ReflectedPropertyBagProvider).GetMethod(nameof(CreateProperty), BindingFlags.Instance | BindingFlags.NonPublic);
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_CreatePropertyBagMethod = typeof(ReflectedPropertyBagProvider).GetMethods(BindingFlags.Instance | BindingFlags.Public).First(x => x.Name == nameof(CreatePropertyBag) && x.IsGenericMethod);
#pragma warning restore RS0030

            // Generic interface property bag types (e.g. IList<T>, ISet<T>, IDictionary<K, V>)
            m_CreateIndexedCollectionPropertyBagMethod = typeof(ReflectedPropertyBagProvider).GetMethod(nameof(CreateIndexedCollectionPropertyBag), BindingFlags.Instance | BindingFlags.NonPublic);
            m_CreateSetPropertyBagMethod = typeof(ReflectedPropertyBagProvider).GetMethod(nameof(CreateSetPropertyBag), BindingFlags.Instance | BindingFlags.NonPublic);
            m_CreateKeyValueCollectionPropertyBagMethod = typeof(ReflectedPropertyBagProvider).GetMethod(nameof(CreateKeyValueCollectionPropertyBag), BindingFlags.Instance | BindingFlags.NonPublic);
            m_CreateKeyValuePairPropertyBagMethod = typeof(ReflectedPropertyBagProvider).GetMethod(nameof(CreateKeyValuePairPropertyBag), BindingFlags.Instance | BindingFlags.NonPublic);

            // Concrete collection property bag types (e.g. List<T>, HashSet<T>, Dictionary<K, V>
            m_CreateArrayPropertyBagMethod = typeof(ReflectedPropertyBagProvider).GetMethod(nameof(CreateArrayPropertyBag), BindingFlags.Instance | BindingFlags.NonPublic);
            m_CreateListPropertyBagMethod = typeof(ReflectedPropertyBagProvider).GetMethod(nameof(CreateListPropertyBag), BindingFlags.Instance | BindingFlags.NonPublic);
            m_CreateHashSetPropertyBagMethod = typeof(ReflectedPropertyBagProvider).GetMethod(nameof(CreateHashSetPropertyBag), BindingFlags.Instance | BindingFlags.NonPublic);
            m_CreateDictionaryPropertyBagMethod = typeof(ReflectedPropertyBagProvider).GetMethod(nameof(CreateDictionaryPropertyBag), BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public IPropertyBag CreatePropertyBag(Type type)
        {
            if (type.IsGenericTypeDefinition) return null;
            return (IPropertyBag) m_CreatePropertyBagMethod.MakeGenericMethod(type).Invoke(this, null);
        }

        public IPropertyBag<TContainer> CreatePropertyBag<TContainer>()
        {
            if (!TypeTraits<TContainer>.IsContainer || TypeTraits<TContainer>.IsObject)
            {
                throw new InvalidOperationException("Invalid container type.");
            }

            if (typeof(TContainer).IsArray)
            {
                if (typeof(TContainer).GetArrayRank() != 1)
                {
                    throw new InvalidOperationException("Properties does not support multidimensional arrays.");
                }

                return (IPropertyBag<TContainer>) m_CreateArrayPropertyBagMethod.MakeGenericMethod(typeof(TContainer).GetElementType()).Invoke(this, Array.Empty<object>());
            }

            if (typeof(TContainer).IsGenericType && typeof(TContainer).GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>)))
            {
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                return (IPropertyBag<TContainer>) m_CreateListPropertyBagMethod.MakeGenericMethod(typeof(TContainer).GetGenericArguments().First()).Invoke(this, Array.Empty<object>());
#pragma warning restore RS0030
            }

            if (typeof(TContainer).IsGenericType && typeof(TContainer).GetGenericTypeDefinition().IsAssignableFrom(typeof(HashSet<>)))
            {
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                return (IPropertyBag<TContainer>) m_CreateHashSetPropertyBagMethod.MakeGenericMethod(typeof(TContainer).GetGenericArguments().First()).Invoke(this, Array.Empty<object>());
#pragma warning restore RS0030
            }

            if (typeof(TContainer).IsGenericType && typeof(TContainer).GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>)))
            {
                var args = typeof(TContainer).GetGenericArguments();
                return (IPropertyBag<TContainer>) m_CreateDictionaryPropertyBagMethod.MakeGenericMethod(args[0], args[1]).Invoke(this, Array.Empty<object>());
            }

            if (typeof(TContainer).IsGenericType && typeof(TContainer).GetGenericTypeDefinition().IsAssignableFrom(typeof(IList<>)))
            {
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                return (IPropertyBag<TContainer>) m_CreateIndexedCollectionPropertyBagMethod.MakeGenericMethod(typeof(TContainer), typeof(TContainer).GetGenericArguments().First()).Invoke(this, Array.Empty<object>());
#pragma warning restore RS0030
            }

            if (typeof(TContainer).IsGenericType && typeof(TContainer).GetGenericTypeDefinition().IsAssignableFrom(typeof(ISet<>)))
            {
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                return (IPropertyBag<TContainer>) m_CreateSetPropertyBagMethod.MakeGenericMethod(typeof(TContainer), typeof(TContainer).GetGenericArguments().First()).Invoke(this, Array.Empty<object>());
#pragma warning restore RS0030
            }

            if (typeof(TContainer).IsGenericType && typeof(TContainer).GetGenericTypeDefinition().IsAssignableFrom(typeof(IDictionary<,>)))
            {
                var args = typeof(TContainer).GetGenericArguments();
                return (IPropertyBag<TContainer>) m_CreateKeyValueCollectionPropertyBagMethod.MakeGenericMethod(typeof(TContainer), args[0], args[1]).Invoke(this, Array.Empty<object>());
            }

            if (typeof(TContainer).IsGenericType && typeof(TContainer).GetGenericTypeDefinition().IsAssignableFrom(typeof(KeyValuePair<,>)))
            {
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var types = typeof(TContainer).GetGenericArguments().ToArray();
#pragma warning restore RS0030
                return (IPropertyBag<TContainer>) m_CreateKeyValuePairPropertyBagMethod.MakeGenericMethod(types[0], types[1]).Invoke(this, Array.Empty<object>());
            }

            var propertyBag = new ReflectedPropertyBag<TContainer>();

            foreach (var member in GetPropertyMembers(typeof(TContainer)))
            {
                IMemberInfo info;

                switch (member)
                {
                    case FieldInfo field:
                        info = new FieldMember(field);
                        break;
                    case PropertyInfo property:
                        info = new PropertyMember(property);
                        break;
                    default:
                        throw new InvalidOperationException();
                }

                m_CreatePropertyMethod.MakeGenericMethod(typeof(TContainer), info.ValueType).Invoke(this, new object[]
                {
                    info,
                    propertyBag
                });
            }

            return propertyBag;
        }

#pragma warning disable RS0030 // This [Preserve] usage will be addressed by https://jira.unity3d.com/browse/UUM-128402
        [Preserve]
#pragma warning restore RS0030
        void CreateProperty<TContainer, TValue>(IMemberInfo member, ReflectedPropertyBag<TContainer> propertyBag)
        {
            if (typeof(TValue).IsPointer)
            {
                return;
            }

            propertyBag.AddProperty(new ReflectedMemberProperty<TContainer, TValue>(member, member.Name));
        }

#pragma warning disable RS0030 // This [Preserve] usage will be addressed by https://jira.unity3d.com/browse/UUM-128402
        [Preserve]
#pragma warning restore RS0030
        IPropertyBag<TList> CreateIndexedCollectionPropertyBag<TList, TElement>() where TList : IList<TElement>
            => new IndexedCollectionPropertyBag<TList, TElement>();

#pragma warning disable RS0030 // This [Preserve] usage will be addressed by https://jira.unity3d.com/browse/UUM-128402
        [Preserve]
#pragma warning restore RS0030
        IPropertyBag<TSet> CreateSetPropertyBag<TSet, TValue>() where TSet : ISet<TValue>
            => new SetPropertyBagBase<TSet, TValue>();

#pragma warning disable RS0030 // This [Preserve] usage will be addressed by https://jira.unity3d.com/browse/UUM-128402
        [Preserve]
#pragma warning restore RS0030
        IPropertyBag<TDictionary> CreateKeyValueCollectionPropertyBag<TDictionary, TKey, TValue>() where TDictionary : IDictionary<TKey, TValue>
            => new KeyValueCollectionPropertyBag<TDictionary, TKey, TValue>();

#pragma warning disable RS0030 // This [Preserve] usage will be addressed by https://jira.unity3d.com/browse/UUM-128402
        [Preserve]
#pragma warning restore RS0030
        IPropertyBag<KeyValuePair<TKey, TValue>> CreateKeyValuePairPropertyBag<TKey, TValue>()
            => new KeyValuePairPropertyBag<TKey, TValue>();

#pragma warning disable RS0030 // This [Preserve] usage will be addressed by https://jira.unity3d.com/browse/UUM-128402
        [Preserve]
#pragma warning restore RS0030
        IPropertyBag<TElement[]> CreateArrayPropertyBag<TElement>()
            => new ArrayPropertyBag<TElement>();

#pragma warning disable RS0030 // This [Preserve] usage will be addressed by https://jira.unity3d.com/browse/UUM-128402
        [Preserve]
#pragma warning restore RS0030
        IPropertyBag<List<TElement>> CreateListPropertyBag<TElement>()
            => new ListPropertyBag<TElement>();

#pragma warning disable RS0030 // This [Preserve] usage will be addressed by https://jira.unity3d.com/browse/UUM-128402
        [Preserve]
#pragma warning restore RS0030
        IPropertyBag<HashSet<TElement>> CreateHashSetPropertyBag<TElement>()
            => new HashSetPropertyBag<TElement>();

#pragma warning disable RS0030 // This [Preserve] usage will be addressed by https://jira.unity3d.com/browse/UUM-128402
        [Preserve]
#pragma warning restore RS0030
        IPropertyBag<Dictionary<TKey, TValue>> CreateDictionaryPropertyBag<TKey, TValue>()
            => new DictionaryPropertyBag<TKey, TValue>();
    
        static IEnumerable<MemberInfo> GetPropertyMembers(Type type)
        {
            do
            {
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic).OrderBy(x => x.MetadataToken);
#pragma warning restore RS0030

                foreach (var member in members)
                {
                    if (member.MemberType != MemberTypes.Field && member.MemberType != MemberTypes.Property)
                    {
                        continue;
                    }

                    if (member.DeclaringType != type)
                    {
                        continue;
                    }

                    if (!IsValidMember(member))
                    {
                        continue;
                    }

                    // Gather all possible attributes we care about.
                    var hasDontCreatePropertyAttribute = member.GetCustomAttribute<DontCreatePropertyAttribute>() != null;
                    var hasCreatePropertyAttribute = member.GetCustomAttribute<CreatePropertyAttribute>() != null;
                    var hasNonSerializedAttribute = member.GetCustomAttribute<NonSerializedAttribute>() != null;
                    var hasSerializedFieldAttribute = member.GetCustomAttribute<SerializeField>() != null;
                    var hasSerializeReferenceAttribute = member.GetCustomAttribute<SerializeReference>() != null;

                    if (hasDontCreatePropertyAttribute)
                    {
                        // This attribute trumps all others. No matter what a property should NOT be generated.
                        continue;
                    }

                    if (hasCreatePropertyAttribute)
                    {
                        // The user explicitly requests an attribute, one will be generated, regardless of serialization attributes.
                        yield return member;
                        continue;
                    }

                    if (hasNonSerializedAttribute)
                    {
                        // If property generation was not explicitly specified lets keep behaviour consistent with Unity.
                        continue;
                    }

                    if (hasSerializedFieldAttribute)
                    {
                        // If property generation was not explicitly specified lets keep behaviour consistent with Unity.
                        yield return member;
                        continue;
                    }

                    if (hasSerializeReferenceAttribute)
                    {
                        // If property generation was not explicitly specified lets keep behaviour consistent with Unity.
                        yield return member;
                        continue;
                    }

                    // No attributes were specified, if this is a public field we will generate one by implicitly.
                    if (member is FieldInfo field && field.IsPublic)
                    {
                        yield return member;
                    }
                }

                type = type.BaseType;
            }
            while (type != null && type != typeof(object));
        }

        static bool IsValidMember(MemberInfo memberInfo)
        {
            switch (memberInfo)
            {
                case FieldInfo fieldInfo:
                    return !fieldInfo.IsStatic && IsValidPropertyType(fieldInfo.FieldType);
                case PropertyInfo propertyInfo:
                    return null != propertyInfo.GetMethod && !propertyInfo.GetMethod.IsStatic && IsValidPropertyType(propertyInfo.PropertyType);
            }

            return false;
        }

        static bool IsValidPropertyType(Type type)
        {
            if (type.IsPointer)
                return false;

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return !type.IsGenericType || type.GetGenericArguments().All(IsValidPropertyType);
#pragma warning restore RS0030
        }
    }
}
