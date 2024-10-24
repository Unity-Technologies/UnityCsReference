// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using UnityEditor;

namespace Unity.UI.Builder
{
    internal static class BuilderStyleSheetsNewSelectorHelpTips
    {
        static readonly string k_HelpTooltipPath = BuilderConstants.UIBuilderPackagePath + "/Explorer/BuilderStyleSheetsNewSelectorHelpTips.uxml";
        internal const string k_HelpTooltipLabel = "uss-selector-precedence-link";

        public static VisualElement Create()
        {
            var helpTooltipTemplate = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(k_HelpTooltipPath);
            var helpTooltipContainer = helpTooltipTemplate.CloneTree();
            UpdateHelpUrl(helpTooltipContainer);
            return helpTooltipContainer;
        }

        static void UpdateHelpUrl(VisualElement root)
        {
            var url = Help.FindHelpNamed("UIE-uss-selector-precedence");
            var label = root.Q<Label>(k_HelpTooltipLabel);
            label.text = $"<a href=\"{url}\"><u>Learn more</u></a>";
        }
    }
}
