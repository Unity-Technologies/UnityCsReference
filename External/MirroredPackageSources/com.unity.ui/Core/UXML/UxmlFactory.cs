using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace UnityEngine.UIElements
{
    public abstract class UxmlTraits
    {
        protected UxmlTraits()
        {
            canHaveAnyAttribute = true;
        }

        public bool canHaveAnyAttribute { get; protected set; }

        public virtual IEnumerable<UxmlAttributeDescription> uxmlAttributesDescription
        {
            get
            {
                foreach (UxmlAttributeDescription attributeDescription in GetAllAttributeDescriptionForType(GetType()))
                {
                    yield return attributeDescription;
                }
            }
        }

        public virtual IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
        {
            get { yield break; }
        }

        public virtual void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc) {}

        IEnumerable<UxmlAttributeDescription> GetAllAttributeDescriptionForType(Type t)
        {
            Type baseType = t.BaseType;

            if (baseType != null)
            {
                foreach (UxmlAttributeDescription ident in GetAllAttributeDescriptionForType(baseType))
                {
                    yield return ident;
                }
            }

            foreach (FieldInfo fieldInfo in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                     .Where(f => typeof(UxmlAttributeDescription).IsAssignableFrom(f.FieldType)))
            {
                yield return (UxmlAttributeDescription)fieldInfo.GetValue(this);
            }
        }
    }

    public interface IUxmlFactory
    {
        string uxmlName { get; }

        string uxmlNamespace { get; }

        string uxmlQualifiedName { get; }

        bool canHaveAnyAttribute { get; }

        IEnumerable<UxmlAttributeDescription> uxmlAttributesDescription { get; }

        IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription { get; }

        string substituteForTypeName { get; }

        string substituteForTypeNamespace { get; }

        string substituteForTypeQualifiedName { get; }

        bool AcceptsAttributeBag(IUxmlAttributes bag, CreationContext cc);

        VisualElement Create(IUxmlAttributes bag, CreationContext cc);
    }

    public class UxmlFactory<TCreatedType, TTraits> : IUxmlFactory where TCreatedType : VisualElement, new() where TTraits : UxmlTraits, new()
    {
        // Make private once we get rid of PropertyControl
        internal TTraits m_Traits;

        protected UxmlFactory()
        {
            m_Traits = new TTraits();
        }

        public virtual string uxmlName
        {
            get { return typeof(TCreatedType).Name; }
        }

        public virtual string uxmlNamespace
        {
            get { return typeof(TCreatedType).Namespace ?? String.Empty; }
        }

        public virtual string uxmlQualifiedName
        {
            get { return typeof(TCreatedType).FullName; }
        }

        public bool canHaveAnyAttribute
        {
            get { return m_Traits.canHaveAnyAttribute; }
        }

        public virtual IEnumerable<UxmlAttributeDescription> uxmlAttributesDescription
        {
            get
            {
                foreach (var attr in m_Traits.uxmlAttributesDescription)
                {
                    yield return attr;
                }
            }
        }

        public virtual IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
        {
            get
            {
                foreach (var child in m_Traits.uxmlChildElementsDescription)
                {
                    yield return child;
                }
            }
        }

        public virtual string substituteForTypeName
        {
            get
            {
                if (typeof(TCreatedType) == typeof(VisualElement))
                {
                    return String.Empty;
                }

                return typeof(VisualElement).Name;
            }
        }

        public virtual string substituteForTypeNamespace
        {
            get
            {
                {
                    if (typeof(TCreatedType) == typeof(VisualElement))
                    {
                        return String.Empty;
                    }

                    return typeof(VisualElement).Namespace ?? String.Empty;
                }
            }
        }

        public virtual string substituteForTypeQualifiedName
        {
            get
            {
                {
                    if (typeof(TCreatedType) == typeof(VisualElement))
                    {
                        return String.Empty;
                    }

                    return typeof(VisualElement).FullName;
                }
            }
        }

        public virtual bool AcceptsAttributeBag(IUxmlAttributes bag, CreationContext cc)
        {
            return true;
        }

        public virtual VisualElement Create(IUxmlAttributes bag, CreationContext cc)
        {
            TCreatedType ve = new TCreatedType();
            m_Traits.Init(ve, bag, cc);
            return ve;
        }
    }

    public class UxmlFactory<TCreatedType> : UxmlFactory<TCreatedType, VisualElement.UxmlTraits> where TCreatedType : VisualElement, new() {}
}
