// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace UnityEngine.Experimental.UIElements
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
            Type baseType =
                t.BaseType;

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

        [Obsolete("Use uxmlName and uxmlNamespace instead.")]
        Type CreatesType { get; }
    }

    public class UxmlFactory<TCreatedType, TTraits> : IUxmlFactory where TCreatedType : VisualElement where TTraits : UxmlTraits, new()
    {
        protected TTraits m_Traits;

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
            get { return typeof(TCreatedType).Namespace != null ? typeof(TCreatedType).Namespace : String.Empty; }
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

                    return typeof(VisualElement).Namespace != null ? typeof(VisualElement).Namespace : String.Empty;
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

        [Obsolete("Call or override AcceptsAttributeBag(IUxmlAttributes bag, CreationContext cc) instaed")]
        public virtual bool AcceptsAttributeBag(IUxmlAttributes bag)
        {
            return AcceptsAttributeBag(bag, new CreationContext());
        }

        public virtual bool AcceptsAttributeBag(IUxmlAttributes bag, CreationContext cc)
        {
            return true;
        }

        static bool s_WarningLogged = false;

        public virtual VisualElement Create(IUxmlAttributes bag, CreationContext cc)
        {
            TCreatedType ve;

            // Ideally, we would simply do
            //   TCreatedType ve = new TCreatedType()
            // since we now have a default constructor on all VisualElement classes.
            // However, we want to preserve compatibility with 2018.1, which did not
            // have this requirement, but required overriding DoCreate() instead.
            //
            // The reason we need to preserve compatibility with 2018.1, despite our
            // experimental status, is that we cannot at the moment force users to
            // upgrade their packages, and package-manager-ui package would need to
            // be upgraded to use the new UxmlFactory.
            //
            // We examine the factory type to see if it contains a
            // protected override TCreatedType DoCreate(IUxmlAttributes bag, CreationContext cc)
            // method and call it if we find one. Otherwise, we try calling TCreatedType default
            // constructor.
            Type[] parameterTypes = { typeof(IUxmlAttributes), typeof(CreationContext) };
            bool doCreateIsOverridden = GetType().GetMethod("DoCreate",
                BindingFlags.DeclaredOnly | BindingFlags.ExactBinding | BindingFlags.Instance | BindingFlags.NonPublic,
                null, parameterTypes, null) != null;

            if (doCreateIsOverridden)
            {
                if (!s_WarningLogged)
                {
                    Debug.LogWarning("Calling obsolete method " + GetType().FullName + ".DoCreate(IUxmlAttributes bag, CreationContext cc). Remove and implemenent a default constructor for the created type instead.");
                    s_WarningLogged = true;
                }
#pragma warning disable 618
                // Disable warning for call to obsolete method.
                ve = DoCreate(bag, cc);
#pragma warning restore
            }
            else
            {
                try
                {
                    // Calling the default constructor would require a constraint on TCreatedType,
                    // which cannot be added to preserve compatibility with 2018.1
                    // (we want types without default constructor to still be accepted as a parameter
                    // to the generic UxmlFactory<>).
                    ve = (TCreatedType)Activator.CreateInstance(typeof(TCreatedType));
                }
                catch (MemberAccessException)
                {
                    if (!s_WarningLogged)
                    {
                        Debug.LogError("No accessible default constructor for " + typeof(TCreatedType));
                        s_WarningLogged = true;
                    }
                    ve = null;
                }
            }

            if (ve != null)
            {
                m_Traits.Init(ve, bag, cc);
            }

            return ve;
        }

        [Obsolete("Remove and implemenent a default constructor for the created type instead.")]
        protected virtual TCreatedType DoCreate(IUxmlAttributes bag, CreationContext cc)
        {
            return null;
        }

        [Obsolete("Use uxmlName and uxmlNamespace instead.")]
        public Type CreatesType { get { return typeof(TCreatedType); } }
    }

    public class UxmlFactory<TCreatedType> : UxmlFactory<TCreatedType, VisualElement.UxmlTraits> where TCreatedType : VisualElement {}
}
