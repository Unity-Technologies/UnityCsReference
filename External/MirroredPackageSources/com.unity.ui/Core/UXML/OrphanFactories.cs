using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal class UxmlGenericAttributeNames
    {
        internal const string k_NameAttributeName = "name";
        internal const string k_PathAttributeName = "path";
        internal const string k_SrcAttributeName = "src";
    }

    /// <summary>
    /// Factory for the root <c>UXML</c> element.
    /// </summary>
    /// <remarks>
    /// This factory does not generate VisualElements. UIElements uses it to generate schemas.
    /// </remarks>
    public class UxmlRootElementFactory : UxmlFactory<VisualElement, UxmlRootElementTraits>
    {
        internal const string k_ElementName = "UXML";

        /// <summary>
        /// Returns <c>"UXML"</c>.
        /// </summary>
        public override string uxmlName => k_ElementName;

        /// <summary>
        /// Returns the qualified name for this element.
        /// </summary>
        public override string uxmlQualifiedName => uxmlNamespace + "." + uxmlName;

        /// <summary>
        /// Returns the empty string, as the root element can not appear anywhere else bit at the root of the document.
        /// </summary>
        public override string substituteForTypeName => String.Empty;

        /// <summary>
        /// Returns the empty string, as the root element can not appear anywhere else bit at the root of the document.
        /// </summary>
        public override string substituteForTypeNamespace => String.Empty;

        /// <summary>
        /// Returns the empty string, as the root element can not appear anywhere else bit at the root of the document.
        /// </summary>
        public override string substituteForTypeQualifiedName => String.Empty;

        /// <summary>
        /// Returns null.
        /// </summary>
        public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
        {
            return null;
        }
    }

    /// <summary>
    /// Defines <see cref="UxmlTraits"/> for the UXML root element.
    /// </summary>
    public class UxmlRootElementTraits : UxmlTraits
    {
        protected UxmlStringAttributeDescription m_Name = new UxmlStringAttributeDescription
        {
            name = UxmlGenericAttributeNames.k_NameAttributeName
        };
#pragma warning disable 414
        // These variables are used by reflection.
        UxmlStringAttributeDescription m_Class = new UxmlStringAttributeDescription {name = "class"};
#pragma warning restore

        /// <summary>
        /// Returns an enumerable containing <c>UxmlChildElementDescription(typeof(VisualElement))</c>, since the root element can contain VisualElements.
        /// </summary>
        public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
        {
            get { yield return new UxmlChildElementDescription(typeof(VisualElement)); }
        }
    }

    /// <summary>
    /// Factory for the root <c>Style</c> element.
    /// </summary>
    /// <remarks>
    /// This factory does not generate VisualElements. UIElements uses it to generate schemas.
    /// </remarks>
    public class UxmlStyleFactory : UxmlFactory<VisualElement, UxmlStyleTraits>
    {
        internal const string k_ElementName = "Style";

        /// <summary>
        ///
        /// </summary>
        public override string uxmlName => k_ElementName;

        /// <summary>
        ///
        /// </summary>
        public override string uxmlQualifiedName => uxmlNamespace + "." + uxmlName;

        /// <summary>
        ///
        /// </summary>
        public override string substituteForTypeName => typeof(VisualElement).Name;

        public override string substituteForTypeNamespace => typeof(VisualElement).Namespace ?? String.Empty;

        public override string substituteForTypeQualifiedName => typeof(VisualElement).FullName;

        public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
        {
            return null;
        }
    }

    /// <summary>
    /// Defines <see cref="UxmlTraits"/> for the Style tag.
    /// </summary>
    public class UxmlStyleTraits : UxmlTraits
    {
        UxmlStringAttributeDescription m_Name = new UxmlStringAttributeDescription { name = UxmlGenericAttributeNames.k_NameAttributeName };
        UxmlStringAttributeDescription m_Path = new UxmlStringAttributeDescription { name = UxmlGenericAttributeNames.k_PathAttributeName };
        UxmlStringAttributeDescription m_Src = new UxmlStringAttributeDescription { name = UxmlGenericAttributeNames.k_SrcAttributeName };

        /// <undoc/>
        public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
        {
            get { yield break; }
        }
    }

    /// <summary>
    /// Factory for the root <c>Template</c> element.
    /// </summary>
    /// <remarks>
    /// This factory does not generate VisualElements. UIElements uses it to generate schemas.
    /// </remarks>
    public class UxmlTemplateFactory : UxmlFactory<VisualElement, UxmlTemplateTraits>
    {
        internal const string k_ElementName = "Template";

        /// <summary>
        ///
        /// </summary>
        public override string uxmlName => k_ElementName;

        /// <summary>
        ///
        /// </summary>
        public override string uxmlQualifiedName => uxmlNamespace + "." + uxmlName;

        /// <summary>
        ///
        /// </summary>
        public override string substituteForTypeName => typeof(VisualElement).Name;

        public override string substituteForTypeNamespace => typeof(VisualElement).Namespace ?? String.Empty;

        public override string substituteForTypeQualifiedName => typeof(VisualElement).FullName;

        public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
        {
            return null;
        }
    }

    /// <summary>
    /// Defines <see cref="UxmlTraits"/> for the Template tag.
    /// </summary>
    public class UxmlTemplateTraits : UxmlTraits
    {
        UxmlStringAttributeDescription m_Name = new UxmlStringAttributeDescription { name = UxmlGenericAttributeNames.k_NameAttributeName, use = UxmlAttributeDescription.Use.Required };
        UxmlStringAttributeDescription m_Path = new UxmlStringAttributeDescription { name = UxmlGenericAttributeNames.k_PathAttributeName };
        UxmlStringAttributeDescription m_Src = new UxmlStringAttributeDescription { name = UxmlGenericAttributeNames.k_SrcAttributeName };

        /// <undoc/>
        public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
        {
            get { yield break; }
        }
    }

    /// <summary>
    /// Factory for the root <c>AttributeOverrides</c> element.
    /// </summary>
    /// <remarks>
    /// This factory does not generate VisualElements. UIElements uses it to generate schemas.
    /// </remarks>
    public class UxmlAttributeOverridesFactory : UxmlFactory<VisualElement, UxmlAttributeOverridesTraits>
    {
        internal const string k_ElementName = "AttributeOverrides";

        /// <summary>
        ///
        /// </summary>
        public override string uxmlName => k_ElementName;

        /// <summary>
        ///
        /// </summary>
        public override string uxmlQualifiedName => uxmlNamespace + "." + uxmlName;

        public override string substituteForTypeName => typeof(VisualElement).Name;

        public override string substituteForTypeNamespace => typeof(VisualElement).Namespace ?? String.Empty;

        public override string substituteForTypeQualifiedName => typeof(VisualElement).FullName;

        public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
        {
            return null;
        }
    }

    /// <summary>
    /// Defines <see cref="UxmlTraits"/> for the AttributeOverrides tag.
    /// </summary>
    public class UxmlAttributeOverridesTraits : UxmlTraits
    {
        internal const string k_ElementNameAttributeName = "element-name";
        UxmlStringAttributeDescription m_ElementName = new UxmlStringAttributeDescription { name = k_ElementNameAttributeName, use = UxmlAttributeDescription.Use.Required };

        /// <undoc/>
        public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
        {
            get { yield break; }
        }
    }
}
