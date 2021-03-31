using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace UnityEngine.UIElements
{
    /// <summary>
    /// Describes a <see cref="VisualElement"/> derived class for the parsing of UXML files and the generation of UXML schema definition.
    /// </summary>
    /// <remarks>
    /// UxmlTraits describes the UXML attributes and children elements of a class deriving from <see cref="VisualElement"/>. It is used by <see cref="UxmlFactory"/> to map UXML attributes to the C# class properties when reading UXML documents. It is also used to generate UXML schema definitions.
    /// </remarks>
    public abstract class UxmlTraits
    {
        protected UxmlTraits()
        {
            canHaveAnyAttribute = true;
        }

        /// <summary>
        /// Must return true if the UXML element attributes are not restricted to the values enumerated by <see cref="uxmlAttributesDescription"/>.
        /// </summary>
        public bool canHaveAnyAttribute { get; protected set; }

        /// <summary>
        /// Describes the UXML attributes expected by the element. The attributes enumerated here will appear in the UXML schema.
        /// </summary>
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

        /// <summary>
        /// Describes the types of element that can appear as children of this element in a UXML file.
        /// </summary>
        public virtual IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
        {
            get { yield break; }
        }

        /// <summary>
        /// Initialize a <see cref="VisualElement"/> instance with values from the UXML element attributes.
        /// </summary>
        /// <param name="ve">The VisualElement to initialize.</param>
        /// <param name="bag">A bag of name-value pairs, one for each attribute of the UXML element.</param>
        /// <param name="cc">When the element is created as part of a template instance inserted in another document, this contains information about the insertion point.</param>
        /// <remarks>
        /// Override this function in your traits class to initialize your C# object with values read from the UXML document.
        /// </remarks>
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

    /// <summary>
    /// Interface for UXML factories. While it is not strictly required, concrete factories should derive from the generic class <see cref="UxmlFactory"/>.
    /// </summary>
    public interface IUxmlFactory
    {
        /// <summary>
        /// The name of the UXML element read by the factory.
        /// </summary>
        string uxmlName { get; }

        /// <summary>
        /// The namespace of the UXML element read by the factory.
        /// </summary>
        string uxmlNamespace { get; }

        /// <summary>
        /// The fully qualified name of the UXML element read by the factory.
        /// </summary>
        string uxmlQualifiedName { get; }

        /// <summary>
        /// Must return true if the UXML element attributes are not restricted to the values enumerated by <see cref="uxmlAttributesDescription"/>.
        /// </summary>
        bool canHaveAnyAttribute { get; }

        /// <summary>
        /// Describes the UXML attributes expected by the element. The attributes enumerated here will appear in the UXML schema.
        /// </summary>
        IEnumerable<UxmlAttributeDescription> uxmlAttributesDescription { get; }

        /// <summary>
        /// Describes the types of element that can appear as children of this element in a UXML file.
        /// </summary>
        IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription { get; }

        /// <summary>
        /// The type of element for which this element type can substitute for.
        /// </summary>
        /// <remarks>
        /// Enables the element to appear anywhere the <see cref="substituteForTypeName"/> element can appear in a UXML document.
        /// For example, if an element restricts its children to Button elements (using the <see cref="uxmlChildElementsDescription"/> property), elements that have <see cref="substitueForTypeName"/> return <c>Button</c> are accepted as children of that element.
        ///
        /// The value of this property is used for the element's substitutionGroup attribute in UXML schema definition.
        /// </remarks>
        string substituteForTypeName { get; }

        /// <summary>
        /// The UXML namespace for the type returned by <see cref="substituteForTypeName"/>.
        /// </summary>
        string substituteForTypeNamespace { get; }

        /// <summary>
        /// The fully qualified XML name for the type returned by <see cref="substituteForTypeName"/>.
        /// </summary>
        string substituteForTypeQualifiedName { get; }

        /// <summary>
        /// Returns true if the factory accepts the content of the attribute bag.
        /// </summary>
        /// <param name="bag">The attribute bag.</param>
        /// <remarks>
        /// Use this function to validate the content of the attribute bag against the requirements of your factory. If a required attribute is missing or if an attribute value is incorrect, return false. Otherwise, if the bag content is acceptable to your factory, return true.
        /// </remarks>
        /// <returns>True if the factory accepts the content of the attribute bag. False otherwise.</returns>
        bool AcceptsAttributeBag(IUxmlAttributes bag, CreationContext cc);

        /// <summary>
        /// Instantiate and initialize an object of type <c>T0</c>.
        /// </summary>
        /// <param name="bag">A bag of name-value pairs, one for each attribute of the UXML element. This can be used to initialize the properties of the created object.</param>
        /// <param name="cc">When the element is created as part of a template instance inserted in another document, this contains information about the insertion point.</param>
        /// <returns>The created object.</returns>
        VisualElement Create(IUxmlAttributes bag, CreationContext cc);
    }

    /// <summary>
    /// Generic base class for UXML factories, which instantiate a VisualElement using the data read from a UXML file.
    /// </summary>
    /// <remarks>
    /// /T0/ The type of the element that will be instantiated. It must derive from <see cref="VisualElement"/>.
    ///
    /// /T1/ The traits of the element that will be instantiated. It must derive from <see cref="UxmlTraits"/>.
    /// </remarks>
    public class UxmlFactory<TCreatedType, TTraits> : IUxmlFactory where TCreatedType : VisualElement, new() where TTraits : UxmlTraits, new()
    {
        // Make private once we get rid of PropertyControl
        internal TTraits m_Traits;

        protected UxmlFactory()
        {
            m_Traits = new TTraits();
        }

        /// <summary>
        /// Returns the type name of <c>T0</c>.
        /// </summary>
        public virtual string uxmlName
        {
            get { return typeof(TCreatedType).Name; }
        }

        /// <summary>
        /// Returns the namespace name of <c>T0</c>.
        /// </summary>
        public virtual string uxmlNamespace
        {
            get { return typeof(TCreatedType).Namespace ?? String.Empty; }
        }

        /// <summary>
        /// Returns the typefully qualified name of <c>T0</c>.
        /// </summary>
        public virtual string uxmlQualifiedName
        {
            get { return typeof(TCreatedType).FullName; }
        }

        /// <summary>
        /// Returns UxmlTraits<see cref="canHaveAnyAttribute"/> (where UxmlTraits is the argument for <c>T1</c>).
        /// </summary>
        public bool canHaveAnyAttribute
        {
            get { return m_Traits.canHaveAnyAttribute; }
        }

        /// <summary>
        /// Returns an empty enumerable.
        /// </summary>
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

        /// <summary>
        /// Returns an empty enumerable.
        /// </summary>
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

        /// <summary>
        /// Returns an empty string if <c>T0</c> is not <see cref="VisualElement"/>; otherwise, returns "VisualElement".
        /// </summary>
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

        /// <summary>
        /// Returns the namespace for <see cref="substituteForTypeName"/>.
        /// </summary>
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

        /// <summary>
        /// Returns the fully qualified name for <see cref="substituteForTypeName"/>.
        /// </summary>
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

        /// <summary>
        /// Returns true.
        /// </summary>
        /// <param name="bag">The attribute bag.</param>
        /// <remarks>
        /// By default, accepts any attribute bags. Override this function if you want to make specific checks on the attribute bag.
        /// </remarks>
        /// <returns>Always true.</returns>
        public virtual bool AcceptsAttributeBag(IUxmlAttributes bag, CreationContext cc)
        {
            return true;
        }

        /// <summary>
        /// Instantiate an object of type <c>T0</c> and initialize it by calling <c>T1</c> UxmlTraits<see cref="Init"/> method.
        /// </summary>
        /// <param name="bag">A bag of name-value pairs, one for each attribute of the UXML element. This can be used to initialize the properties of the created object.</param>
        /// <param name="cc">When the element is created as part of a template instance inserted in another document, this contains information about the insertion point.</param>
        /// <returns>The created element.</returns>
        public virtual VisualElement Create(IUxmlAttributes bag, CreationContext cc)
        {
            TCreatedType ve = new TCreatedType();
            m_Traits.Init(ve, bag, cc);
            return ve;
        }
    }

    /// <summary>
    /// UxmlFactory specialization for classes that derive from <see cref="VisualElement"/> and that shares its traits, <see cref="VisualElementTraits"/>.
    /// </summary>
    public class UxmlFactory<TCreatedType> : UxmlFactory<TCreatedType, VisualElement.UxmlTraits> where TCreatedType : VisualElement, new() {}
}
