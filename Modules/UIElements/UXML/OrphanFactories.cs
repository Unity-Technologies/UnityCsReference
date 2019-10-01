// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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

    public class UxmlRootElementFactory : UxmlFactory<VisualElement, UxmlRootElementTraits>
    {
        internal const string k_ElementName = "UXML";

        public override string uxmlName => k_ElementName;

        public override string uxmlQualifiedName => uxmlNamespace + "." + uxmlName;

        public override string substituteForTypeName => String.Empty;

        public override string substituteForTypeNamespace => String.Empty;

        public override string substituteForTypeQualifiedName => String.Empty;

        public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
        {
            return null;
        }
    }

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

        public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
        {
            get { yield return new UxmlChildElementDescription(typeof(VisualElement)); }
        }
    }

    public class UxmlStyleFactory : UxmlFactory<VisualElement, UxmlStyleTraits>
    {
        internal const string k_ElementName = "Style";

        public override string uxmlName => k_ElementName;

        public override string uxmlQualifiedName => uxmlNamespace + "." + uxmlName;

        public override string substituteForTypeName => typeof(VisualElement).Name;

        public override string substituteForTypeNamespace => typeof(VisualElement).Namespace ?? String.Empty;

        public override string substituteForTypeQualifiedName => typeof(VisualElement).FullName;

        public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
        {
            return null;
        }
    }

    public class UxmlStyleTraits : UxmlTraits
    {
        UxmlStringAttributeDescription m_Name = new UxmlStringAttributeDescription { name = UxmlGenericAttributeNames.k_NameAttributeName };
        UxmlStringAttributeDescription m_Path = new UxmlStringAttributeDescription { name = UxmlGenericAttributeNames.k_PathAttributeName };
        UxmlStringAttributeDescription m_Src = new UxmlStringAttributeDescription { name = UxmlGenericAttributeNames.k_SrcAttributeName };

        public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
        {
            get { yield break; }
        }
    }

    public class UxmlTemplateFactory : UxmlFactory<VisualElement, UxmlTemplateTraits>
    {
        internal const string k_ElementName = "Template";

        public override string uxmlName => k_ElementName;

        public override string uxmlQualifiedName => uxmlNamespace + "." + uxmlName;

        public override string substituteForTypeName => typeof(VisualElement).Name;

        public override string substituteForTypeNamespace => typeof(VisualElement).Namespace ?? String.Empty;

        public override string substituteForTypeQualifiedName => typeof(VisualElement).FullName;

        public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
        {
            return null;
        }
    }

    public class UxmlTemplateTraits : UxmlTraits
    {
        UxmlStringAttributeDescription m_Name = new UxmlStringAttributeDescription { name = UxmlGenericAttributeNames.k_NameAttributeName, use = UxmlAttributeDescription.Use.Required };
        UxmlStringAttributeDescription m_Path = new UxmlStringAttributeDescription { name = UxmlGenericAttributeNames.k_PathAttributeName };
        UxmlStringAttributeDescription m_Src = new UxmlStringAttributeDescription { name = UxmlGenericAttributeNames.k_SrcAttributeName };

        public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
        {
            get { yield break; }
        }
    }

    public class UxmlAttributeOverridesFactory : UxmlFactory<VisualElement, UxmlAttributeOverridesTraits>
    {
        internal const string k_ElementName = "AttributeOverrides";

        public override string uxmlName => k_ElementName;

        public override string uxmlQualifiedName => uxmlNamespace + "." + uxmlName;

        public override string substituteForTypeName => typeof(VisualElement).Name;

        public override string substituteForTypeNamespace => typeof(VisualElement).Namespace ?? String.Empty;

        public override string substituteForTypeQualifiedName => typeof(VisualElement).FullName;

        public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
        {
            return null;
        }
    }

    public class UxmlAttributeOverridesTraits : UxmlTraits
    {
        internal const string k_ElementNameAttributeName = "element-name";
        UxmlStringAttributeDescription m_ElementName = new UxmlStringAttributeDescription { name = k_ElementNameAttributeName, use = UxmlAttributeDescription.Use.Required };

        public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
        {
            get { yield break; }
        }
    }
}
