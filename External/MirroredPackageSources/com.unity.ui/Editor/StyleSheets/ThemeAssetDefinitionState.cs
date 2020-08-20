using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.StyleSheets
{
    class ThemeAssetDefinitionState : ScriptableObject
    {
        public List<StyleSheet> StyleSheets;
        public List<ThemeStyleSheet> InheritedThemes;
    }
}
