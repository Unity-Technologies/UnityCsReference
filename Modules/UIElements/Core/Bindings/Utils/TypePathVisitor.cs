// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;

namespace UnityEngine.UIElements.Internal
{
    class TypePathVisitor : ITypeVisitor, IPropertyBagVisitor, IPropertyVisitor
    {
        /// <summary>
        /// The path to visit.
        /// </summary>
        public PropertyPath Path { get; set; }

        /// <summary>
        /// The resulting type at this path, if found. Null if not found.
        /// </summary>
        public Type resolvedType { get; private set; }

        /// <summary>
        /// Returns the error code encountered while visiting the provided path.
        /// </summary>
        public VisitReturnCode ReturnCode { get; private set; }

        Type m_LastType;
        int m_PathIndex;

        /// <summary>
        /// Returns the index of the last property path that was visited.
        /// </summary>
        public int PathIndex => m_PathIndex;

        public void Reset()
        {
            resolvedType = null;
            m_LastType = null;
            Path = default;
            ReturnCode = VisitReturnCode.Ok;
            m_PathIndex = 0;
        }

        void IPropertyBagVisitor.Visit<TContainer>(IPropertyBag<TContainer> properties, ref TContainer container)
        {
            var part = Path[m_PathIndex++];

            IProperty<TContainer> property;

            switch (part.Kind)
            {
                case PropertyPathPartKind.Name:
                {
                    if (properties is INamedProperties<TContainer> named && named.TryGetProperty(ref container, part.Name, out property))
                    {
                        property.Accept(this, ref container);
                    }
                    else
                    {
                        foreach (var p in properties.GetProperties())
                        {
                            if (p.Name == part.Name)
                            {
                                var propertyType = m_LastType = p.DeclaredValueType();
                                var untypedBag = PropertyBag.GetPropertyBag(propertyType);
                                untypedBag?.Accept(this);
                                return;
                            }
                        }

                        ReturnCode = VisitReturnCode.InvalidPath;
                    }
                }
                    break;

                case PropertyPathPartKind.Index:
                {
                    if (properties is IIndexedProperties<TContainer> indexable && indexable.TryGetProperty(ref container, part.Index, out property))
                    {
                        property.Accept(this, ref container);
                    }
                    else
                    {
                        var elementType = GetElementType(typeof(TContainer));
                        if (elementType != null)
                        {
                            var untypedBag = PropertyBag.GetPropertyBag(elementType);
                            untypedBag?.Accept(this);
                        }
                        else
                        {
                            ReturnCode = VisitReturnCode.InvalidPath;
                        }
                    }
                }
                    break;

                case PropertyPathPartKind.Key: // Dictionaries are not supported.
                default:
                    ReturnCode = VisitReturnCode.InvalidPath;
                    break;
            }
        }

        void IPropertyVisitor.Visit<TContainer, TValue>(Property<TContainer, TValue> property, ref TContainer container)
        {
            var value = property.GetValue(ref container);

            if (m_PathIndex >= Path.Length)
            {
                resolvedType = property.DeclaredValueType();
            }
            else if (PropertyBag.TryGetPropertyBagForValue(ref value, out _))
            {
                if (TypeTraits<TValue>.CanBeNull && EqualityComparer<TValue>.Default.Equals(value, default))
                {
                    var untypedBag = PropertyBag.GetPropertyBag(property.DeclaredValueType());
                    untypedBag?.Accept(this);
                    return;
                }

                PropertyContainer.Accept(this, ref value);
            }
            else
            {
                ReturnCode = VisitReturnCode.InvalidPath;
            }
        }

        void ITypeVisitor.Visit<TContainer>()
        {
            // Stop visitation when we reach the expected property.
            if (IsLastPartReached())
                return;

            var part = Path[m_PathIndex++];
            m_LastType = null;

            switch (part.Kind)
            {
                case PropertyPathPartKind.Name:
                {
                    var propertyBag = PropertyBag.GetPropertyBag<TContainer>();
                    if (propertyBag == null)
                        return;

                    foreach (var prop in propertyBag.GetProperties())
                    {
                        if (prop.Name != part.Name)
                        {
                            continue;
                        }

                        var type = m_LastType = prop.DeclaredValueType();
                        var bag = PropertyBag.GetPropertyBag(type);
                        if (bag != null)
                        {
                            bag.Accept(this);
                            return;
                        }

                        var elementType = GetElementType(type);
                        if (elementType != null)
                        {
                            if (IsLastPartReached())
                                return;

                            part = Path[m_PathIndex++];

                            if (part.IsIndex)
                            {
                                m_LastType = elementType;
                                var untypedBag = PropertyBag.GetPropertyBag(elementType);
                                untypedBag?.Accept(this);
                                return;
                            }
                        }

                        break;
                    }
                }
                    break;

                case PropertyPathPartKind.Index:
                {
                    var type = typeof(TContainer);
                    var elementType = GetElementType(type);
                    if (elementType != null)
                    {
                        m_LastType = elementType;
                        var untypedBag = PropertyBag.GetPropertyBag(elementType);
                        if (untypedBag != null)
                        {
                            untypedBag.Accept(this);
                            return;
                        }
                    }
                }
                    break;
            }

            // Part was not handled, look if we reached the end, otherwise the path is invalid
            if (IsLastPartReached())
                return;

            if (ReturnCode == VisitReturnCode.Ok)
            {
                // Unsupported validation.
                ReturnCode = VisitReturnCode.InvalidPath;
            }
        }

        bool IsLastPartReached()
        {
            if (m_PathIndex < Path.Length)
            {
                return false;
            }

            if (m_LastType == null)
            {
                ReturnCode = VisitReturnCode.InvalidPath;
            }

            resolvedType = m_LastType;
            return true;
        }

        static Type GetElementType(Type type)
        {
            Type elementType = null;
            if (type.IsArray && type.GetArrayRank() == 1)
            {
                elementType = type.GetElementType();
            }
            else if (type.IsGenericType && (type.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>)) || type.GetGenericTypeDefinition().IsAssignableFrom(typeof(IList<>))))
            {
                elementType = type.GenericTypeArguments[0];
            }

            return elementType;
        }
    }
}
