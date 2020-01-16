// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Samples
{
    internal class StylesExplorer
    {
        private static readonly string s_StylesExplorerName = "StylesExplorer";
        private static readonly string s_StylesExplorerClassName = "unity-samples-window__styles-explorer";
        private static readonly string s_ScrollViewContainerClassName = "unity-styles-explorer__scroll_view_container";

        private static readonly string s_StylePanelDarkClassName = "unity-samples-window__style-panel--dark";
        private static readonly string s_StylePanelLightClassName = "unity-samples-window__style-panel--light";

        private static readonly string s_StylePanelClassName = "unity-samples-window__style-panel";
        private static readonly string s_StyleSectionClassName = "unity-samples-window__style-section";
        private static readonly string s_StyleSectionLabelClassName = "unity-samples-window__style-section-label";
        private static readonly string s_StyleSectionInnerContainerClassName = "unity-samples-window__style-section-inner-container";
        private static readonly string s_StyleSectionMainContainerClassName = "unity-samples-window__style-section-main-container";
        private static readonly string s_StyleSectionSecondaryContainerClassName = "unity-samples-window__style-section-secondary-container";
        private static readonly string s_StyleFoldoutClassName = "unity-samples-window__style-foldout";

        private static readonly int s_MinWidthForDualColumn = 690;

        internal static VisualElement Create(UIElementsSamples.SampleTreeItem item)
        {
            var panelContainer = new ScrollView();
            panelContainer.name = s_StylesExplorerName;
            panelContainer.AddToClassList(s_StylesExplorerClassName);

            var innerContainer = new VisualElement();
            panelContainer.Add(innerContainer);
            innerContainer.AddToClassList(s_ScrollViewContainerClassName);
            innerContainer.RegisterCallback<GeometryChangedEvent>(ContainerGeometryChange);
            ContainerSetColumnCount(innerContainer);

            var leftPanel = CreatePanel();
            var rightPanel = CreatePanel();

            leftPanel.AddToClassList(s_StylePanelDarkClassName);
            rightPanel.AddToClassList(s_StylePanelLightClassName);

            leftPanel.styleSheets.Add(UIElementsEditorUtility.s_DefaultCommonDarkStyleSheet);
            rightPanel.styleSheets.Add(UIElementsEditorUtility.s_DefaultCommonLightStyleSheet);

            innerContainer.Add(leftPanel);
            innerContainer.Add(rightPanel);

            return panelContainer;
        }

        private static void ContainerSetColumnCount(VisualElement container)
        {
            if (container.resolvedStyle.width > s_MinWidthForDualColumn)
                container.style.flexDirection = FlexDirection.Row;
            else
                container.style.flexDirection = FlexDirection.Column;
        }

        private static void ContainerGeometryChange(GeometryChangedEvent evt)
        {
            var container = evt.target as VisualElement;
            ContainerSetColumnCount(container);
        }

        private static VisualElement CreatePanel()
        {
            var container = new VisualElement();
            IgnoreAllInputs(container);
            container.AddToClassList(s_StylePanelClassName);

            container.Add(CreateStandardSection<Button>());
            container.Add(CreateStandardSection<Toggle>());
            container.Add(CreateStandardSection<Slider>());
            container.Add(CreateStandardSection<TextField>());
            container.Add(CreateStandardSection<HelpBox>());
            container.Add(CreateStandardSection<Vector3Field>());
            container.Add(CreateStandardSection<RectField>());

            { // Foldout
                var section = CreateSection("Foldout");
                Add<Foldout>(section);
                Add<Foldout>(section, PseudoStates.Checked);
                container.Add(section);
            }

            return container;
        }

        private static VisualElement CreateSection(string sectionName)
        {
            var section = new VisualElement();

            section.AddToClassList(s_StyleSectionClassName);

            var label = new Label(sectionName);
            label.AddToClassList(s_StyleSectionLabelClassName);
            section.Add(label);

            return section;
        }

        private static VisualElement CreateStandardSection<T>() where T : VisualElement, new()
        {
            var tname = typeof(T).Name;

            var section = CreateSection(tname);

            var innerContainer = new VisualElement();
            innerContainer.AddToClassList(s_StyleSectionInnerContainerClassName);

            var main = new VisualElement();
            main.AddToClassList(s_StyleSectionMainContainerClassName);
            innerContainer.Add(main);

            {
                Add<T>(main);
                Add<T>(main, PseudoStates.Disabled);

                Add<T>(main, PseudoStates.Hover);
                Add<T>(main, PseudoStates.Hover | PseudoStates.Active);
                Add<T>(main, PseudoStates.Focus | PseudoStates.Active);
            }

            if (tname == "Toggle" || tname == "Button")
            {
                var secondary = new VisualElement();
                secondary.AddToClassList(s_StyleSectionSecondaryContainerClassName);
                innerContainer.Add(secondary);

                Add<T>(secondary, PseudoStates.Checked);
                Add<T>(secondary, PseudoStates.Disabled | PseudoStates.Checked);
                Add<T>(secondary, PseudoStates.Hover | PseudoStates.Checked);
                Add<T>(secondary, PseudoStates.Hover | PseudoStates.Active | PseudoStates.Checked);
                Add<T>(secondary, PseudoStates.Focus | PseudoStates.Active | PseudoStates.Checked);
            }

            section.Add(innerContainer);

            return section;
        }

        private static void IgnoreAllInputs(VisualElement element)
        {
            element.pickingMode = PickingMode.Ignore;
            element.focusable = false;
            element.RegisterCallback<MouseDownEvent>(e =>
            {
                e.PreventDefault();
                e.StopImmediatePropagation();
            });
            element.RegisterCallback<MouseUpEvent>(e =>
            {
                e.PreventDefault();
                e.StopImmediatePropagation();
            });
            element.RegisterCallback<MouseMoveEvent>(e =>
            {
                e.PreventDefault();
                e.StopImmediatePropagation();
            });
        }

        private static void IgnoreAllInputsRecursive(VisualElement element)
        {
            IgnoreAllInputs(element);
            foreach (var child in element.Children())
                IgnoreAllInputsRecursive(child);
        }

        private static T Add<T>(VisualElement parent, PseudoStates pseudo = 0) where T : VisualElement, new()
        {
            var element = new T();
            element.pseudoStates |= pseudo;
            if ((pseudo & PseudoStates.Disabled) == PseudoStates.Disabled)
                element.SetEnabled(false);

            ApplyModificationsToInputs(element, BaseField<int>.inputUssClassName, pseudo);

            IgnoreAllInputsRecursive(element);

            var description = pseudo.ToString();
            if (pseudo == 0)
                description = "Normal";

            if (element is TextElement) (element as TextElement).text = description;
            else if (element is BaseField<bool>) (element as BaseField<bool>).label = description;
            else if (element is BaseField<float>) (element as BaseField<float>).label = description;
            else if (element is BaseField<string>) (element as BaseField<string>).label = description;
            else if (element is BaseField<Vector3>) (element as BaseField<Vector3>).label = description;
            else if (element is BaseField<Rect>) (element as BaseField<Rect>).label = description;
            else if (element is Foldout)
            {
                var foldout = element as Foldout;
                foldout.text = description;
                foldout.value = (pseudo & PseudoStates.Checked) == PseudoStates.Checked;

                var contents = new Label("Content with Border");
                contents.AddToClassList(s_StyleFoldoutClassName);
                foldout.Add(contents);
            }
            else if (element is HelpBox)
            {
                var helpBox = element as HelpBox;
                helpBox.text = description;
                helpBox.messageType = HelpBoxMessageType.Info;
            }

            parent.Add(element);
            return element;
        }

        private static void ApplyModificationsToInputs(VisualElement element, string className, PseudoStates pseudo)
        {
            element.Query(classes: new string[] { className }).ForEach(e =>
            {
                e.pseudoStates |= pseudo;
            });
        }
    }
}
