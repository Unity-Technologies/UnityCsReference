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
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal class StyleRule
    {
        [SerializeField]
        StyleComplexSelector[] m_ComplexSelectors = Array.Empty<StyleComplexSelector>();

        [SerializeField]
        StyleProperty[] m_Properties = Array.Empty<StyleProperty>();

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [SerializeField]
        internal int line;

        // This reference is set at runtime as convenience, but is not serialized
        [field:NonSerialized]
        internal StyleSheet styleSheet { get; set; }

        internal StyleRule(StyleSheet styleSheet)
        {
            this.styleSheet = styleSheet;
        }

        public StyleComplexSelector[] complexSelectors => m_ComplexSelectors;

        // Called by the StyleSheetBuilder when importing the style sheet.
        // This sets the selectors without needing to rebuild the StyleSheet
        // references right away.
        internal void SetSelectors(StyleComplexSelector[] selectors)
        {
            m_ComplexSelectors = selectors;
        }

        public StyleProperty[] properties => m_Properties;

        // Called by the StyleSheetBuilder when importing the style sheet.
        // This sets the selectors without needing to rebuild the StyleSheet
        // references right away.
        internal void SetProperties(StyleProperty[] props)
        {
            m_Properties = props;
        }

        public bool TryAddSelector(string selectorStr, out StyleComplexSelector selector)
            => TryAddSelector(selectorStr, out selector, out _);

        public bool TryAddSelector(string selectorStr, out StyleComplexSelector selector, out string error)
        {
            if (!SelectorUtility.ExtractSelectorsAndSpecificityFromString(
                    selectorStr,
                    out var selectors,
                    out var specificity,
                    out error))
            {
                selector = null;
                return false;
            }

            selector = new StyleComplexSelector { selectors = selectors, specificity = specificity };
            CollectionExtensions.AddToArray(ref m_ComplexSelectors, selector);
            selector.rule = this;

            styleSheet.RequestRebuild();
            return true;
        }

        public StyleComplexSelector AddSelector(string selectorStr)
        {
            if (!TryAddSelector(selectorStr, out var selector, out var error))
            {
                throw new InvalidOperationException(error);
            }

            return selector;
        }

        public bool RemoveSelector(StyleComplexSelector selector)
        {
            var index = Array.IndexOf(m_ComplexSelectors, selector);
            if (index < 0)
                return false;
            RemoveSelector(index);
            return true;
        }

        public bool RemoveSelector(int index)
        {
            if (index < 0 || index >= m_ComplexSelectors.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            var selector = m_ComplexSelectors[index];
            CollectionExtensions.RemoveFromArray(ref m_ComplexSelectors, index);
            selector.ruleIndex = -1;
            selector.rule = null;
            selector.nextInTable = null;
            styleSheet.RequestRebuild();
            return true;
        }

        public StyleProperty AddProperty(string propertyName)
        {
            var property = new StyleProperty { name = propertyName };
            AddPropertyToArray(property);
            return property;
        }

        public StyleProperty AddProperty(StylePropertyId id)
        {
            var property = new StyleProperty { id = id };
            AddPropertyToArray(property);
            return property;
        }

        private void AddPropertyToArray(StyleProperty property)
        {
            CollectionExtensions.AddToArray(ref m_Properties, property);

            if (property.isCustomProperty)
                ++customPropertiesCount;

            styleSheet.RequestRebuild();
        }

        public bool RemoveProperty(StyleProperty property)
        {
            var index = Array.IndexOf(m_Properties, property);
            if (index < 0)
                return false;
            RemoveProperty(index);
            return true;
        }

        public bool RemoveProperty(int index)
        {
            if (index < 0 || index >= m_Properties.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            var property = m_Properties[index];
            CollectionExtensions.RemoveFromArray(ref m_Properties, index);

            if (property.isCustomProperty)
                --customPropertiesCount;

            styleSheet.RequestRebuild();
            return true;
        }

        public void ClearProperties()
        {
            m_Properties = Array.Empty<StyleProperty>();
            customPropertiesCount = 0;
            styleSheet.RequestRebuild();
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

        public StyleProperty FindLastProperty(StylePropertyId id)
        {
            for (var i = properties.Length - 1; i >= 0; --i)
            {
                if (properties[i].id == id)
                    return properties[i];
            }
            return null;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [NonSerialized]
        internal int customPropertiesCount;
    }
}
