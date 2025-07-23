// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using UnityEngine.Bindings;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    [Serializable]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class StyleRule
    {
        [SerializeField]
        StyleProperty[] m_Properties = Array.Empty<StyleProperty>();

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [SerializeField]
        internal int line;

        public StyleProperty[] properties
        {
            get
            {
                return m_Properties;
            }
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            internal set
            {
                m_Properties = value;
            }
        }

        public StyleProperty AddProperty(StyleSheet styleSheet, string propertyName)
        {
            var property = new StyleProperty { name = propertyName };
            CollectionExtensions.AddToArray(ref m_Properties, property);

            if (property.isCustomProperty)
                ++customPropertiesCount;

            styleSheet.SetTemporaryContentHash();
            return property;
        }

        public bool RemoveProperty(StyleSheet styleSheet, StyleProperty property)
        {
            if (!CollectionExtensions.RemoveFromArray(ref m_Properties, property))
                return false;

            if (property.isCustomProperty)
                --customPropertiesCount;

            styleSheet.SetTemporaryContentHash();
            return true;
        }

        public StyleProperty FindLastProperty(string propertyName)
        {
            for (var i = properties.Length - 1; i >= 0; --i)
            {
                if (properties[i].name == propertyName)
                    return properties[i];
            }
            return null;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [NonSerialized]
        internal int customPropertiesCount;
    }
}
