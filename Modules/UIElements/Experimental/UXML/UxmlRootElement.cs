// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public class UxmlRootElementFactory : UxmlFactory<VisualElement, UxmlRootElementTraits>
    {
        public override string uxmlName
        {
            get { return "UXML"; }
        }

        public override string uxmlQualifiedName
        {
            get { return uxmlNamespace + "." + uxmlName; }
        }

        public override string substituteForTypeName
        {
            get { return String.Empty; }
        }

        public override string substituteForTypeNamespace
        {
            get { return String.Empty; }
        }

        public override string substituteForTypeQualifiedName
        {
            get { return String.Empty; }
        }

        public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
        {
            return null;
        }
    }

    public class UxmlRootElementTraits : UxmlTraits
    {
        public UxmlRootElementTraits()
        {
            canHaveAnyAttribute = false;
        }

        public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
        {
            get { return new[] { new UxmlChildElementDescription(typeof(VisualElement)) }; }
        }
    }
}
