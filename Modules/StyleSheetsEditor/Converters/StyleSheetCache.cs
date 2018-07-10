// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.StyleSheets;

namespace UnityEditor.StyleSheets
{
    internal class StyleSheetCache
    {
        public StyleSheet sheet { get; private set; }
        public Dictionary<string, StyleComplexSelector> customStyleSelectors { get; private set; }
        public Dictionary<string, StyleComplexSelector> selectors { get; private set; }
        public Dictionary<string, StyleComplexSelector> typeStyleSelectors { get; private set; }
        public Dictionary<string, StyleComplexSelector> abstractStyleSelectors { get; private set; }

        public StyleSheetCache(StyleSheet sheet)
        {
            this.sheet = sheet;
            customStyleSelectors = new Dictionary<string, StyleComplexSelector>();
            selectors = new Dictionary<string, StyleComplexSelector>();
            typeStyleSelectors = new Dictionary<string, StyleComplexSelector>();
            abstractStyleSelectors = new Dictionary<string, StyleComplexSelector>();
            IndexSheet();
        }

        public void AddSelector(StyleComplexSelector selector)
        {
            var selectorStr = StyleSheetToUss.ToUssSelector(selector);
            if (ConverterUtils.IsCustomStyleSelector(selectorStr))
            {
                customStyleSelectors.TryAdd(selectorStr, selector);
            }
            else if (ConverterUtils.IsTypeStyleSelector(selectorStr.ToLower()))
            {
                typeStyleSelectors.TryAdd(selectorStr, selector);
            }
            else if (ConverterUtils.IsAbstractStyleSelector(selectorStr.ToLower()))
            {
                abstractStyleSelectors.TryAdd(selectorStr, selector);
            }

            selectors.TryAdd(selectorStr, selector);
        }

        private void IndexSheet()
        {
            foreach (var complexSelector in sheet.complexSelectors)
            {
                AddSelector(complexSelector);
            }
        }
    }
}
