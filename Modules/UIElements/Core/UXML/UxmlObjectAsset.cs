// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Pool;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [Serializable]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    struct UxmlProperty
    {
        public string name;
        public string value;
    }

    [Serializable]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    struct UxmlNamespaceDefinition : IEquatable<UxmlNamespaceDefinition>
    {
        public static UxmlNamespaceDefinition Empty { get; } = default;

        public string prefix;
        public string resolvedNamespace;

        public string Export()
        {
            if (string.IsNullOrEmpty(prefix))
                return $"xmlns=\"{resolvedNamespace}\"";
            return $"xmlns:{prefix}=\"{resolvedNamespace}\"";
        }

        public static bool operator ==(UxmlNamespaceDefinition lhs, UxmlNamespaceDefinition rhs)
        {
            if (string.IsNullOrEmpty(lhs.prefix) && string.IsNullOrEmpty(rhs.prefix) &&
                string.IsNullOrEmpty(lhs.resolvedNamespace) && string.IsNullOrEmpty(rhs.resolvedNamespace))
                return true;
            return string.Compare(lhs.prefix, rhs.prefix, StringComparison.Ordinal) == 0 &&
                   string.Compare(lhs.resolvedNamespace, rhs.resolvedNamespace, StringComparison.Ordinal) == 0;
        }

        public static bool operator !=(UxmlNamespaceDefinition lhs, UxmlNamespaceDefinition rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(UxmlNamespaceDefinition other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            return obj is UxmlNamespaceDefinition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(prefix, resolvedNamespace);
        }
    }

    [Serializable]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal abstract class UxmlAsset : IUxmlAttributes
    {
        [Flags]
        enum Flags
        {
            None = 0,
            HasAuthoringId = 1 << 0
        }

        public const string AuthoringIdAttribute = "authoring-id";
        public const string NullNodeType = "null";

        public UxmlAsset(string fullTypeName, UxmlNamespaceDefinition xmlNamespace = default)
        {
            m_FullTypeName = fullTypeName;
            m_XmlNamespace = xmlNamespace;
        }

        [SerializeField] private string m_FullTypeName;

        public string fullTypeName
        {
            get => m_FullTypeName;
            set => m_FullTypeName = value;
        }

        [SerializeField]
        private UxmlNamespaceDefinition m_XmlNamespace;

        public UxmlNamespaceDefinition xmlNamespace
        {
            get => m_XmlNamespace;
            set => m_XmlNamespace = value;
        }

        [SerializeField]
        private int m_Id;

        public int id
        {
            get => m_Id;
            internal set
            {
                if (visualTreeAsset)
                    visualTreeAsset.UnregisterId(this);
                m_Id = value;
                if (visualTreeAsset)
                    visualTreeAsset.RegisterId(this);
            }
        }

        [SerializeField]
        Flags m_Flags;

        /// <summary>
        /// Indicates whether the UXML asset has a persistent authoring ID.
        /// If not, a temporary generated ID is used, which may change the next time the asset is imported.
        /// </summary>
        public bool hasAuthoringId
        {
            get => m_Flags.HasFlag(Flags.HasAuthoringId);
            set => m_Flags = value ? (m_Flags | Flags.HasAuthoringId) : (m_Flags & ~Flags.HasAuthoringId);
        }

        public bool isNull => fullTypeName == NullNodeType;
        public bool isRoot => fullTypeName.Equals("UXML", StringComparison.Ordinal) || fullTypeName.Equals("UnityEngine.UIElements.UXML", StringComparison.Ordinal) || fullTypeName.EndsWith(".UXML", StringComparison.Ordinal);

        public UxmlAsset parentAsset => m_Parent;

        [SerializeReference, HideInInspector] private UxmlAsset m_Parent;
        [SerializeReference] private List<UxmlAsset> m_Children;
        [SerializeField] private VisualTreeAsset m_VisualTreeAsset;

        internal VisualTreeAsset visualTreeAsset
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
            get => m_VisualTreeAsset;
        }

        public int childCount => m_Children?.Count ?? 0;

        public UxmlAsset this[int index] => m_Children[index];

        [SerializeField] private List<UxmlNamespaceDefinition> m_NamespaceDefinitions;
        public List<UxmlNamespaceDefinition> namespaceDefinitions => m_NamespaceDefinitions ??= new ();

        [SerializeField]
        protected List<UxmlProperty> m_Properties;

        public List<UxmlProperty> properties => m_Properties;

        int m_DirtyCount;
        public void GetChildren(List<UxmlAsset> children)
        {
            children.Clear();

            for (var i = 0; i < childCount; i++)
            {
                children.Add(this[i]);
            }
        }

        public void GetChildrenUxmlObjectAssets(List<UxmlObjectAsset> children)
        {
            children.Clear();

            for (var i = 0; i < childCount; i++)
            {
                if (this[i] is UxmlObjectAsset uxmlObjectAsset)
                {
                    children.Add(uxmlObjectAsset);
                }
            }
        }

        public bool HasAnyUxmlObjectAsset()
        {
            for (var i = 0; i < childCount; i++)
            {
                if (this[i] is UxmlObjectAsset)
                {
                    return true;
                }
            }

            return false;
        }

        public UxmlObjectAsset GetField(string fieldName)
        {
            for (var i = 0; i < childCount; i++)
            {
                if (this[i] is not UxmlObjectAsset uxmlObjectAsset)
                {
                    continue;
                }

                if (uxmlObjectAsset.isField && uxmlObjectAsset.fullTypeName == fieldName)
                    return uxmlObjectAsset;
            }

            return null;
        }

        private void RemoveNonFields()
        {
            for (var i = childCount - 1; i >= 0; i--)
            {
                if (this[i] is not UxmlObjectAsset uxmlObjectAsset)
                {
                    continue;
                }

                if (!uxmlObjectAsset.isField)
                {
                    uxmlObjectAsset.RemoveFromHierarchy();
                }
            }
        }

        public void RemoveUxmlObjectAssetChildren()
        {
            for (var i = childCount - 1; i >= 0; i--)
            {
                if (this[i] is not UxmlObjectAsset uxmlObjectAsset)
                {
                    continue;
                }

                uxmlObjectAsset.RemoveFromHierarchy();
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal void SetUxmlObjectAssets(string fieldName, List<UxmlObjectAsset> entries)
        {
            if (!string.IsNullOrEmpty(fieldName))
            {
                var fieldAsset = GetField(fieldName);
                fieldAsset?.SetUxmlObjectAssets(null, entries);
            }
            else
            {
                RemoveNonFields();

                foreach (var entry in entries)
                {
                    Add(entry);
                }
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal void CollectUxmlObjectAssets(string fieldName, List<UxmlObjectAsset> foundEntries)
        {
            for (var i = 0; i < childCount; i++)
            {
                if (this[i] is not UxmlObjectAsset uxmlObjectAsset)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(fieldName) && uxmlObjectAsset.isField &&
                    uxmlObjectAsset.fullTypeName == fieldName)
                {
                    uxmlObjectAsset.CollectUxmlObjectAssets(null, foundEntries);
                    return;
                }

                if (uxmlObjectAsset.isField)
                {
                    continue;
                }

                foundEntries.Add(uxmlObjectAsset);
            }
        }

        public virtual void GetExportTypename(out string typename, out UxmlNamespaceDefinition resolvedNamespace)
        {
            if (xmlNamespace != UxmlNamespaceDefinition.Empty)
            {
                var namespaceDefinition = m_VisualTreeAsset.FindUxmlNamespaceDefinitionFromPrefix(this, xmlNamespace.prefix);
                if (namespaceDefinition != xmlNamespace)
                {
                    xmlNamespace = m_VisualTreeAsset.FindUxmlNamespaceDefinitionForTypeName(this, fullTypeName);
                }
            }

            if (string.IsNullOrEmpty(xmlNamespace.prefix))
            {
                if (string.IsNullOrEmpty(xmlNamespace.resolvedNamespace))
                {
                    typename = fullTypeName;
                    resolvedNamespace = xmlNamespace;
                    return;
                }

                var name = fullTypeName.Substring(xmlNamespace.resolvedNamespace.Length + 1);
                typename = name;
                resolvedNamespace = xmlNamespace;
            }
            else
            {
                var name = fullTypeName.Substring(xmlNamespace.resolvedNamespace.Length + 1);
                typename = name;
                resolvedNamespace = xmlNamespace;
            }
        }

        internal void SetVisualTreeAssetWithOutNotify(VisualTreeAsset vta)
        {
            m_VisualTreeAsset = vta;
        }

        internal void SetVisualTreeAsset(VisualTreeAsset vta)
        {
            var previous = visualTreeAsset;
            SetVisualTreeAssetWithOutNotify(vta);

            if (previous != visualTreeAsset)
            {
                OnVisualTreeAssetChanged(previous, visualTreeAsset);
            }

            if (m_Children == null)
                return;

            foreach (var child in m_Children)
            {
                child.SetVisualTreeAsset(vta);
            }
        }

        public void Add(UxmlAsset asset)
        {
            if (null == asset)
                throw new ArgumentNullException(nameof(asset));

            Insert(childCount, asset);
        }

        public void Insert(int index, UxmlAsset asset)
        {
            if (null == asset)
                throw new ArgumentNullException(nameof(asset));

            if (index < 0 || index > childCount)
                throw new ArgumentOutOfRangeException("Index out of range: " + index);

            if (asset == this)
                throw new ArgumentException("Cannot insert element as its own child.");

            if (asset.IsAncestorOf(this))
                throw new ArgumentException("Cannot insert element as a child because it is an ancestor.");

            if (!Accepts(asset, out var errorMessage))
                throw new InvalidOperationException(errorMessage);

            // If it's already a children, simply update internal lists.
            if (asset.parentAsset == this)
            {
                var siblingIndex = m_Children.IndexOf(asset);
                // Already inserted in the right spot.
                if (siblingIndex == index)
                    return;

                var append = index == childCount;
                m_Children.RemoveAt(siblingIndex);
                m_Children.Insert(append ? childCount : index, asset);
                return;
            }

            InsertInChildren(index, asset);
            asset.SetParent(this);
        }

        public bool Remove(UxmlAsset asset)
        {
            if (null == asset)
                throw new ArgumentNullException(nameof(asset));

            if (asset == this)
                throw new ArgumentException("Cannot remove element from itself.");

            if (asset.m_Parent != this)
                return false;

            RemoveAt(m_Children.IndexOf(asset));
            return true;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index > childCount)
                throw new ArgumentOutOfRangeException("Index out of range: " + index);

            var child = m_Children[index];
            child.SetParent(null);
        }

        private void InsertInChildren(int index, UxmlAsset asset)
        {
            m_Children ??= new List<UxmlAsset>();
            m_Children.Insert(index, asset);
        }

        private void RemoveFromChildren(UxmlAsset child)
        {
            RemoveFromChildren(IndexOf(child));
        }

        private void RemoveFromChildren(int index)
        {
            m_Children.RemoveAt(index);
        }

        private void SetParent(UxmlAsset parent)
        {
            m_Parent?.RemoveFromChildren(this);
            m_Parent = parent;
            SetVisualTreeAsset(parent?.visualTreeAsset);
        }

        private protected virtual void OnVisualTreeAssetChanged(VisualTreeAsset previousVta, VisualTreeAsset newVta)
        {
            if (previousVta)
                previousVta.UnregisterId(this);
            if (newVta)
                newVta.RegisterId(this);
        }

        public int IndexOf(UxmlAsset asset)
        {
            return m_Children.IndexOf(asset);
        }

        public int SiblingIndex()
        {
            return parentAsset?.IndexOf(this) ?? -1;
        }

        public void RemoveFromHierarchy()
        {
            parentAsset?.Remove(this);
        }

        public bool IsAncestorOf(UxmlAsset other)
        {
            using var parentScope = HashSetPool<UxmlAsset>.Get(out var parents);
            var current = other;
            while (null != current)
            {
                if (!parents.Add(current))
                    throw new InvalidOperationException("Recursion Detected");

                if (this == current.parentAsset)
                    return true;
                current = current.parentAsset;
            }

            return false;
        }

        public virtual bool HasParent() => null != m_Parent;

        public bool HasAttribute(string attributeName)
        {
            if (m_Properties is not { Count: > 0 })
                return false;

            for (var i = 0; i < m_Properties.Count; ++i)
            {
                if (string.CompareOrdinal(m_Properties[i].name, attributeName) == 0)
                    return true;
            }

            return false;
        }

        public string GetAttributeValue(string attributeName)
        {
            TryGetAttributeValue(attributeName, out var value);
            return value;
        }

        public bool TryGetAttributeValue(string propertyName, out string value)
        {
            if (m_Properties == null)
            {
                value = null;
                return false;
            }

            for (var i = 0; i < m_Properties.Count; ++i)
            {
                var property = m_Properties[i];
                if (string.CompareOrdinal(property.name, propertyName) == 0)
                {
                    value = property.value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        public void AddUxmlNamespace(string prefix, string resolvedNamespace)
        {
            namespaceDefinitions.Add(new UxmlNamespaceDefinition
            {
                prefix = prefix,
                resolvedNamespace = resolvedNamespace
            });
        }

        public void SetAttribute(string name, string value)
        {
            SetOrAddProperty(name, value);
        }

        public void RemoveAttribute(string attributeName)
        {
            if (m_Properties == null || m_Properties.Count <= 0)
                return;

            for (var i = 0; i < m_Properties.Count; ++i)
            {
                var property = m_Properties[i];
                if (string.CompareOrdinal(property.name, attributeName) != 0)
                    continue;

                m_Properties.RemoveAt(i);
                return;
            }
        }

        void SetOrAddProperty(string propertyName, string propertyValue)
        {
            m_Properties ??= new List<UxmlProperty>();

            m_DirtyCount++;

            for (var i = 0; i < m_Properties.Count; ++i)
            {
                var property = m_Properties[i];
                if (string.CompareOrdinal(property.name, propertyName) == 0)
                {
                    property.value = propertyValue;
                    m_Properties[i] = property;
                    return;
                }
            }

            m_Properties.Add(new UxmlProperty
            {
                name = propertyName, value = propertyValue
            });
        }

        internal int GetPropertiesDirtyCount()
        {
            return m_DirtyCount;
        }

        internal abstract bool Accepts(UxmlAsset asset, out string errorMessage);

        public override string ToString() => $"{fullTypeName}(id:{id})";
    }

    [Serializable]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal class UxmlObjectAsset : UxmlAsset
    {
        [SerializeField] private int m_OrderInDocument;

        public int orderInDocument
        {
            get => m_OrderInDocument;
            set => m_OrderInDocument = value;
        }

        [SerializeField] bool m_IsField;

        /// <summary>
        /// Returns true if the field is a container for one or more UxmlObject.
        /// Returns false if it's itself a UxmlObject.
        /// </summary>
        public bool isField => m_IsField;

        public UxmlObjectAsset(string fullTypeNameOrFieldName, bool isField, UxmlNamespaceDefinition xmlNamespace = default)
            : base(fullTypeNameOrFieldName, xmlNamespace)
        {
            m_IsField = isField;
        }

        public override void GetExportTypename(out string typename, out UxmlNamespaceDefinition uxmlNamespaceDefinition)
        {
            if (isField)
            {
                typename = fullTypeName;
                uxmlNamespaceDefinition = UxmlNamespaceDefinition.Empty;
                return;
            }
            base.GetExportTypename(out typename, out uxmlNamespaceDefinition);
        }

        internal override bool Accepts(UxmlAsset asset, out string errorMessage)
        {
            var result = asset is UxmlObjectAsset;

            errorMessage = !result
                ? $"[UI Toolkit] Cannot add a UXML asset of type '{asset.fullTypeName}' to a UXML asset of type '{fullTypeName}': UXML objects can only contain other UXML objects."
                : null;
            return result;
        }

        public override string ToString() => isField ? $"Reference: {fullTypeName} (id:{id} parent:{parentAsset?.id})" : base.ToString();
    }
}
