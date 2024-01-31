// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [Serializable]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class UxmlAsset : IUxmlAttributes
    {
        public const string NullNodeType = "null";

        public UxmlAsset(string fullTypeName, UxmlNamespaceDefinition xmlNamespace = default)
        {
            m_FullTypeName = fullTypeName;
            m_XmlNamespace = xmlNamespace;
        }

        [SerializeField]
        private string m_FullTypeName;

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
            set => m_Id = value;
        }

        public bool isNull => fullTypeName == NullNodeType;

        [SerializeField]
        private int m_OrderInDocument;

        public int orderInDocument
        {
            get => m_OrderInDocument;
            set => m_OrderInDocument = value;
        }

        [SerializeField]
        private int m_ParentId;

        public int parentId
        {
            get => m_ParentId;
            set => m_ParentId = value;
        }

        [SerializeField] private List<UxmlNamespaceDefinition> m_NamespaceDefinitions;
        public List<UxmlNamespaceDefinition> namespaceDefinitions => m_NamespaceDefinitions ??= new ();

        [SerializeField]
        protected List<string> m_Properties;

        public List<string> GetProperties() => m_Properties;

        int m_DirtyCount;
        public bool HasParent() => m_ParentId != 0;

        public bool HasAttribute(string attributeName)
        {
            if (m_Properties == null || m_Properties.Count <= 0)
                return false;

            for (var i = 0; i < m_Properties.Count; i += 2)
            {
                var name = m_Properties[i];
                if (name == attributeName)
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

            for (int i = 0; i < m_Properties.Count - 1; i += 2)
            {
                if (m_Properties[i] == propertyName)
                {
                    value = m_Properties[i + 1];
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

            for (var i = 0; i < m_Properties.Count; i += 2)
            {
                var name = m_Properties[i];
                if (name != attributeName)
                    continue;

                m_Properties.RemoveAt(i); // Removing the name at i.
                m_Properties.RemoveAt(i); // Removing the value at i + 1.
                return;
            }
        }

        void SetOrAddProperty(string propertyName, string propertyValue)
        {
            if (m_Properties == null)
                m_Properties = new List<string>();

            m_DirtyCount++;

            for (var i = 0; i < m_Properties.Count - 1; i += 2)
            {
                if (m_Properties[i] == propertyName)
                {
                    m_Properties[i + 1] = propertyValue;
                    return;
                }
            }

            m_Properties.Add(propertyName);
            m_Properties.Add(propertyValue);
        }

        internal int GetPropertiesDirtyCount()
        {
            return m_DirtyCount;
        }

        public override string ToString() => $"{fullTypeName}(id:{id})";
    }

    [Serializable]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class UxmlObjectAsset : UxmlAsset
    {
        [SerializeField]
        bool m_IsField;

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

        public override string ToString() => isField ? $"Reference: {fullTypeName} (id:{id} parent:{parentId})" : base.ToString();
    }
}
