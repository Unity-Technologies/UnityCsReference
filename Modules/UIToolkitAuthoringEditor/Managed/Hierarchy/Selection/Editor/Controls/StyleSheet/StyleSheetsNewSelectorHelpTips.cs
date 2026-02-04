// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine.Bindings;

namespace Unity.UIToolkit.Editor
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class StyleSheetsNewSelectorHelpTips : VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new StyleSheetsNewSelectorHelpTips();
        }

        static readonly string s_UxmlPath = "UIToolkitAuthoring/Inspector/StyleSheet/StyleSheetsNewSelectorHelpTips.uxml";
        internal const string k_HelpTooltipLabel = "uss-selector-precedence-link";

        public StyleSheetsNewSelectorHelpTips()
        {
            var helpTooltipTemplate = EditorGUIUtility.Load(s_UxmlPath) as VisualTreeAsset;
            helpTooltipTemplate.CloneTree(this);
            UpdateHelpUrl();
        }

        void UpdateHelpUrl()
        {
            var url = Help.FindHelpNamed("UIE-uss-selector-precedence");
            var label = this.Q<Label>(k_HelpTooltipLabel);
            label.text = $"<a href=\"{url}\"><u>Learn more</u></a>";
        }
    }
}
