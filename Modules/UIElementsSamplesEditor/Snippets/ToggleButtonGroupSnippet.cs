// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class ToggleButtonGroupSnippet : ElementSnippet<ToggleButtonGroupSnippet>
    {
        internal override void Apply(VisualElement container)
        {
            /// <sample>
            // Create custom buttons with Text value only.
            var csharpToggleButtonGroupWithButtonTextOnly = new ToggleButtonGroup("C# Toggle Button Group Buttons with Text");
            csharpToggleButtonGroupWithButtonTextOnly.Add(new Button() { text = "one", tooltip = "custom button one" });
            csharpToggleButtonGroupWithButtonTextOnly.Add(new Button() { text = "two", tooltip = "custom button two" });
            container.Add(csharpToggleButtonGroupWithButtonTextOnly);

            // Create custom buttons with IconImage value only.
            var csharpToggleButtonGroupWithButtonIconOnly = new ToggleButtonGroup("C# Toggle Button Group Buttons with Icons");
            csharpToggleButtonGroupWithButtonIconOnly.Add(new Button() { iconImage = EditorGUIUtility.FindTexture("d_PlayButton"), tooltip = "Play" });
            csharpToggleButtonGroupWithButtonIconOnly.Add(new Button() { iconImage = EditorGUIUtility.FindTexture("d_PauseButton"), tooltip = "Pause" });
            csharpToggleButtonGroupWithButtonIconOnly.Add(new Button() { iconImage = EditorGUIUtility.FindTexture("d_StepButton"), tooltip = "Step" });
            container.Add(csharpToggleButtonGroupWithButtonIconOnly);

            // Create custom buttons with IconImage and Text.
            var csharpToggleButtonGroupWithButtonIconAndText = new ToggleButtonGroup("C# Toggle Button Group Buttons with Icons and Text");
            csharpToggleButtonGroupWithButtonIconAndText.Add(new Button() { iconImage = EditorGUIUtility.FindTexture("d_PlayButton"), text = "Play", tooltip = "Play" });
            csharpToggleButtonGroupWithButtonIconAndText.Add(new Button() { iconImage = EditorGUIUtility.FindTexture("d_PauseButton"), text = "Pause", tooltip = "Pause" });
            csharpToggleButtonGroupWithButtonIconAndText.Add(new Button() { iconImage = EditorGUIUtility.FindTexture("d_StepButton"), text = "Step", tooltip = "Step" });
            container.Add(csharpToggleButtonGroupWithButtonIconAndText);

            // Create custom buttons with IconImage, Text and AllowEmptySelection.
            var csharpToggleButtonGroupSingleSelectionAndAllowEmptySelection = new ToggleButtonGroup("C# Toggle Button Group Buttons with Allow Empty Selection") { allowEmptySelection = true };
            csharpToggleButtonGroupSingleSelectionAndAllowEmptySelection.Add(new Button() { iconImage = EditorGUIUtility.FindTexture("d_PlayButton"), text = "Play", tooltip = "Play" });
            csharpToggleButtonGroupSingleSelectionAndAllowEmptySelection.Add(new Button() { iconImage = EditorGUIUtility.FindTexture("d_PauseButton"), text = "Pause", tooltip = "Pause" });
            csharpToggleButtonGroupSingleSelectionAndAllowEmptySelection.Add(new Button() { iconImage = EditorGUIUtility.FindTexture("d_StepButton"), text = "Step", tooltip = "Step" });
            container.Add(csharpToggleButtonGroupSingleSelectionAndAllowEmptySelection);

            // Create a ToggleButtonGroup that allows multiple active selections.
            var csharpToggleButtonGroupAllowMultiSelect = new ToggleButtonGroup("C# Toggle Button Group with Multiple Selection Enabled") { isMultipleSelection = true };
            csharpToggleButtonGroupAllowMultiSelect.Add(new Button() { text = "X", tooltip = "tooltip text for X" });
            csharpToggleButtonGroupAllowMultiSelect.Add(new Button() { text = "Y", tooltip = "tooltip text for Y" });
            csharpToggleButtonGroupAllowMultiSelect.Add(new Button() { text = "Z", tooltip = "tooltip text for Z" });
            container.Add(csharpToggleButtonGroupAllowMultiSelect);

            // Create a ToggleButtonGroup that allows multiple active selections and allow empty selection.
            var csharpToggleButtonGroupAllowMultiSelectWithAllowEmptySelection = new ToggleButtonGroup("C# Toggle Button Group with Multiple Selection and Allow Empty Selection") { isMultipleSelection = true, allowEmptySelection = true };
            csharpToggleButtonGroupAllowMultiSelectWithAllowEmptySelection.Add(new Button() { text = "X", tooltip = "tooltip text for X" });
            csharpToggleButtonGroupAllowMultiSelectWithAllowEmptySelection.Add(new Button() { text = "Y", tooltip = "tooltip text for Y" });
            csharpToggleButtonGroupAllowMultiSelectWithAllowEmptySelection.Add(new Button() { text = "Z", tooltip = "tooltip text for Z" });
            container.Add(csharpToggleButtonGroupAllowMultiSelectWithAllowEmptySelection);

            // Create ToggleButtonGroup with a custom class that sets the text's font style to Bold.
            var csharpToggleButtonGroupWithCustomClass = new ToggleButtonGroup("C# Toggle Button Group with a Custom Class");
            csharpToggleButtonGroupWithCustomClass.AddToClassList("my-custom-style");
            csharpToggleButtonGroupWithCustomClass.Add(new Button() { text = "Button A", tooltip = "Bolded font Button A" });
            csharpToggleButtonGroupWithCustomClass.Add(new Button() { text = "Button B", tooltip = "Bolded font Button B" });
            container.Add(csharpToggleButtonGroupWithCustomClass);
            /// </sample>
        }
    }
}
