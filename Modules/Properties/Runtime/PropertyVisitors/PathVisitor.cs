// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace Unity.Properties
{
    /// <summary>
    /// Helper visitor to visit a single property using a specified <see cref="PropertyPath"/>.
    /// </summary>
    public abstract class PathVisitor : IPropertyBagVisitor, IPropertyVisitor
    {
        readonly struct PropertyScope : IDisposable
        {
            readonly PathVisitor m_Visitor;
            readonly IProperty m_Property;

            public PropertyScope(PathVisitor visitor, IProperty property)
            {
                m_Visitor = visitor;
                m_Property = m_Visitor.Property;
                m_Visitor.Property = property;
            }

            public void Dispose() => m_Visitor.Property = m_Property;
        }

        int m_PathIndex;

        /// <summary>
        /// The path to visit.
        /// </summary>
        public PropertyPath Path { get; set; }

        /// <summary>
        /// Resets the state of the visitor.
        /// </summary>
        public virtual void Reset()
        {
            m_PathIndex = 0;
            Path = default;
            ReturnCode = VisitReturnCode.Ok;
            ReadonlyVisit = false;
        }

        /// <summary>
        /// Returns the property for the currently visited container.
        /// </summary>
        IProperty Property { get; set; }

        /// <summary>
        /// Returns whether or not the visitor will write back values along the path.
        /// </summary>
        public bool ReadonlyVisit { get; set; }

        /// <summary>
        /// Returns the error code encountered while visiting the provided path.
        /// </summary>
        public VisitReturnCode ReturnCode { get; protected set; }

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
                        ReturnCode = VisitReturnCode.InvalidPath;
                    }
                }
                    break;

                case PropertyPathPartKind.Index:
                {
                    if (properties is IIndexedProperties<TContainer> indexable && indexable.TryGetProperty(ref container, part.Index, out property))
                    {
                        using ((property as Internal.IAttributes).CreateAttributesScope(Property as Internal.IAttributes))
                        {
                            property.Accept(this, ref container);
                        }
                    }
                    else
                    {
                        ReturnCode = VisitReturnCode.InvalidPath;
                    }
                }
                    break;

                case PropertyPathPartKind.Key:
                {
                    if (properties is IKeyedProperties<TContainer, object> keyable && keyable.TryGetProperty(ref container, part.Key, out property))
                    {
                        using ((property as Internal.IAttributes).CreateAttributesScope(Property as Internal.IAttributes))
                        {
                            property.Accept(this, ref container);
                        }
                    }
                    else
                    {
                        ReturnCode = VisitReturnCode.InvalidPath;
                    }
                }
                    break;

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
                VisitPath(property, ref container, ref value);
            }
            else if (PropertyBag.TryGetPropertyBagForValue(ref value, out _))
            {
                if (TypeTraits<TValue>.CanBeNull && EqualityComparer<TValue>.Default.Equals(value, default))
                {
                    ReturnCode = VisitReturnCode.InvalidPath;
                    return;
                }
                using (new PropertyScope(this, property))
                {
                    PropertyContainer.Accept(this, ref value);
                }

                if (!property.IsReadOnly && !ReadonlyVisit)
                    property.SetValue(ref container, value);
            }
            else
            {
                ReturnCode = VisitReturnCode.InvalidPath;
            }
        }

        /// <summary>
        /// Method called when the visitor has successfully visited the provided path.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="container"></param>
        /// <param name="value"></param>
        /// <typeparam name="TContainer"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        protected virtual void VisitPath<TContainer, TValue>(Property<TContainer, TValue> property, ref TContainer container, ref TValue value)
        {
        }
    }
}
