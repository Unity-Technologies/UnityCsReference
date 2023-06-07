// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;

namespace UnityEngine.UIElements.Internal
{
    class AutoCompletePathVisitor : ITypeVisitor, IPropertyVisitor, IPropertyBagVisitor, IListPropertyVisitor
    {
        class VisitContext
        {
            public List<PropertyPathInfo> propertyPathInfos { get; set; }
            public HashSet<Type> types { get; } = new();
            public PropertyPath current { get; set; }
            public int currentDepth { get; set; }
        }

        VisitContext m_VisitContext = new();

        public List<PropertyPathInfo> propertyPathList
        {
            set => m_VisitContext.propertyPathInfos = value;
        }

        public int maxDepth { get; set; }

        bool HasReachedEnd(Type containerType) => m_VisitContext.currentDepth >= maxDepth || m_VisitContext.types.Contains(containerType);

        public void Reset()
        {
            m_VisitContext.current = new PropertyPath();
            m_VisitContext.propertyPathInfos = null;
            m_VisitContext.types.Clear();
            m_VisitContext.currentDepth = 0;
        }

        /// <inheritdoc/>
        void ITypeVisitor.Visit<TContainer>()
        {
            if (HasReachedEnd(typeof(TContainer)))
                return;

            using var scope = new InspectedTypeScope<TContainer>(m_VisitContext);
            var bag = PropertyBag.GetPropertyBag<TContainer>();
            if (bag == null)
                return;

            foreach (var property in bag.GetProperties())
            {
                using var propertyScope = new VisitedPropertyScope(m_VisitContext, property);
                VisitPropertyType(property.DeclaredValueType());
            }
        }

        /// <inheritdoc/>
        void IPropertyBagVisitor.Visit<TContainer>(IPropertyBag<TContainer> properties, ref TContainer container)
        {
            if (HasReachedEnd(typeof(TContainer)))
                return;

            using var scope = new InspectedTypeScope<TContainer>(m_VisitContext);

            switch (properties)
            {
                case IIndexedProperties<TContainer> indexedProperties:
                    if (indexedProperties.TryGetProperty(ref container, 0, out var indexProperty))
                    {
                        using var propertyScope = new VisitedPropertyScope(m_VisitContext, 0, indexProperty.DeclaredValueType());
                        indexProperty.Accept(this, ref container);
                    }
                    else
                    {
                        // Fallback to type visitation in this branch.
                        VisitPropertyType(typeof(TContainer));
                    }
                    break;
                case IKeyedProperties<TContainer, object>:
                    // Dictionaries are unsupported.
                    break;
                default:
                {
                    foreach (var property in properties.GetProperties(ref container))
                    {
                        using var propertyScope = new VisitedPropertyScope(m_VisitContext, property);
                        property.Accept(this, ref container);
                    }

                    break;
                }
            }
        }

        /// <inheritdoc/>
        void IPropertyVisitor.Visit<TContainer, TValue>(Property<TContainer, TValue> property, ref TContainer container)
        {
            if (!TypeTraits.IsContainer(typeof(TValue)))
                return;

            var value = property.GetValue(ref container);

            var isValueNull = TypeTraits<TValue>.CanBeNull && EqualityComparer<TValue>.Default.Equals(value, default);
            if (!isValueNull && PropertyBag.TryGetPropertyBagForValue(ref value, out var valuePropertyBag))
            {
                switch (valuePropertyBag)
                {
                    case IListPropertyAccept<TValue> accept:
                        accept.Accept(this, property, ref container, ref value);
                        break;
                    default:
                        PropertyContainer.TryAccept(this, ref value);
                        break;
                }
            }
            else
            {
                // Fallback to type visitation in this branch.
                VisitPropertyType(property.DeclaredValueType());
            }
        }

        void IListPropertyVisitor.Visit<TContainer, TList, TElement>(Property<TContainer, TList> property, ref TContainer container, ref TList list)
        {
            PropertyContainer.TryAccept(this, ref list);
        }

        void VisitPropertyType(Type type)
        {
            if (HasReachedEnd(type))
                return;

            if (type.IsArray)
            {
                if (type.GetArrayRank() != 1)
                    return;

                var elementType = type.GetElementType();
                var untypedBag = PropertyBag.GetPropertyBag(elementType);
                using var propertyScope = new VisitedPropertyScope(m_VisitContext, 0, elementType);
                untypedBag?.Accept(this);
            }
            else if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>)) ||
                    type.GetGenericTypeDefinition().IsAssignableFrom(typeof(IList<>)))
                {
                    var elementType = type.GenericTypeArguments[0];
                    var untypedBag = PropertyBag.GetPropertyBag(elementType);
                    using var propertyScope = new VisitedPropertyScope(m_VisitContext, 0, elementType);
                    untypedBag?.Accept(this);
                }
            }
            else
            {
                var bag = PropertyBag.GetPropertyBag(type);
                bag?.Accept(this);
            }
        }

        struct InspectedTypeScope<TContainer> : IDisposable
        {
            VisitContext m_VisitContext;

            public InspectedTypeScope(VisitContext context)
            {
                m_VisitContext = context;
                m_VisitContext.types.Add(typeof(TContainer));
            }

            public void Dispose()
            {
                m_VisitContext.types.Remove(typeof(TContainer));
            }
        }

        struct VisitedPropertyScope : IDisposable
        {
            VisitContext m_VisitContext;

            public VisitedPropertyScope(VisitContext context, IProperty property)
            {
                m_VisitContext = context;
                m_VisitContext.current = PropertyPath.AppendProperty(m_VisitContext.current, property);

                var propertyPathInfo = new PropertyPathInfo(m_VisitContext.current, property.DeclaredValueType());
                m_VisitContext.propertyPathInfos?.Add(propertyPathInfo);
                m_VisitContext.currentDepth++;
            }

            public VisitedPropertyScope(VisitContext context, int index, Type type)
            {
                m_VisitContext = context;
                m_VisitContext.current = PropertyPath.AppendIndex(m_VisitContext.current, index);

                var propertyPathInfo = new PropertyPathInfo(m_VisitContext.current, type);
                m_VisitContext.propertyPathInfos?.Add(propertyPathInfo);
                m_VisitContext.currentDepth++;
            }

            public void Dispose()
            {
                m_VisitContext.current = PropertyPath.Pop(m_VisitContext.current);
                m_VisitContext.currentDepth--;
            }
        }
    }
}
