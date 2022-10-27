// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CommandStateObserver;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    class SubgraphFieldCustomPropertyField : ICustomPropertyFieldBuilder<SubgraphPropertiesField>
    {
        public static readonly string ussClassName = "subgraph-properties";
        public static readonly string containerUssClassName = ussClassName.WithUssElement("container");
        public static readonly string toggleContainerUssClassName = ussClassName.WithUssElement("toggle-container");
        public static readonly string descriptionUssClassName = ussClassName.WithUssElement("description");
        public static readonly string placeholderUssClassName = ussClassName.WithUssElement("placeholder");
        public static readonly string hiddenUssClassName = ussClassName.WithUssElement("hidden");
        public static readonly string textAreaUssClassName = ussClassName.WithUssElement("text-area");

        const string k_Placeholder = "Click to add description";

        GraphModel m_GraphModel;
        VisualElement m_Container;
        VisualElement m_ToggleContainer;

        Toggle m_Toggle;
        HelpBox m_WarningBox;
        TextField m_PathTextField;
        TextField m_DescriptionTextField;
        string m_WarningMessage;

        public bool UpdateDisplayedValue(SubgraphPropertiesField value)
        {
            if (!string.IsNullOrEmpty(value.WarningMessage))
                m_WarningMessage = value.WarningMessage;

            if (m_Toggle != null)
            {
                m_Toggle.SetValueWithoutNotify(value.ShouldShowInLibrary);
                DisplayElement(m_WarningBox, m_Toggle.value && !m_GraphModel.CanBeSubgraph());

                if (m_WarningBox != null)
                    m_WarningBox.text = m_WarningMessage?? "The conditions for this graph to be usable as a subgraph are not met.";

                if (m_PathTextField != null && m_DescriptionTextField != null)
                {
                    DisplayElement(m_ToggleContainer, m_Toggle.value);
                    m_PathTextField.SetValueWithoutNotify(value.DisplayedPath);

                    var description = string.IsNullOrEmpty(value.Description)? k_Placeholder : value.Description;
                    m_DescriptionTextField.EnableInClassList(placeholderUssClassName, string.IsNullOrEmpty(value.Description) || value.Description == k_Placeholder);
                    m_DescriptionTextField.SetValueWithoutNotify(description);
                }

                return true;
            }

            return false;
        }


        public void SetMixed()
        {
            // This should never happen as we don't have a way to select multiple GraphModel at the same time.
        }

        public VisualElement Build(ICommandTarget commandTargetView, string label, string tooltip, IEnumerable<object> obj, string propertyName)
        {
            if (obj.First() is GraphModel graphModel)
            {
                m_GraphModel = graphModel;

                if (m_GraphModel.IsContainerGraph())
                    return null;

                m_Container = new VisualElement { tooltip = tooltip };
                m_Container.AddStylesheet_Internal("SubgraphPropertiesField.uss");
                m_Container.AddToClassList(containerUssClassName);

                m_Toggle = new Toggle { label = "Show graph in Node Library" };
                m_Toggle.RegisterCallback<ChangeEvent<bool>>(OnToggleChanged);
                m_Container.Add(m_Toggle);

                m_WarningBox = new HelpBox { messageType = HelpBoxMessageType.Warning };
                m_WarningBox.AddToClassList(textAreaUssClassName);
                DisplayElement(m_WarningBox, m_Toggle.value);
                m_Container.Add(m_WarningBox);

                m_ToggleContainer = new VisualElement();
                m_ToggleContainer.AddToClassList(toggleContainerUssClassName);
                m_Container.Add(m_ToggleContainer);

                m_PathTextField = new TextField { label = "Node Library Path", multiline = false };
                m_PathTextField.RegisterCallback<ChangeEvent<string>>(OnPathTextFieldChanged);
                m_ToggleContainer.Add(m_PathTextField);

                m_DescriptionTextField = new TextField { label = "Description", multiline = true };

                // UIElements does not provide a placeholder for text fields. This is a workaround.
                m_DescriptionTextField.RegisterCallback<FocusInEvent>(OnFocusIn);
                m_DescriptionTextField.RegisterCallback<FocusOutEvent>(OnFocusOut);

                m_DescriptionTextField.RegisterCallback<ChangeEvent<string>>(OnDescriptionFieldChanged);

                m_DescriptionTextField.AddToClassList(descriptionUssClassName);
                m_DescriptionTextField.AddToClassList(textAreaUssClassName);
                m_ToggleContainer.Add(m_DescriptionTextField);

                return m_Container;
            }

            return null;
        }

        static void DisplayElement(VisualElement element, bool shouldDisplay)
        {
            element.EnableInClassList(hiddenUssClassName, !shouldDisplay);
        }

        void OnFocusOut(FocusOutEvent evt)
        {
            if (string.IsNullOrEmpty(m_DescriptionTextField.text))
            {
                m_DescriptionTextField.SetValueWithoutNotify(k_Placeholder);
                m_DescriptionTextField.EnableInClassList(placeholderUssClassName, true);
            }
            else if (m_DescriptionTextField.ClassListContains(placeholderUssClassName))
            {
                m_DescriptionTextField.EnableInClassList(placeholderUssClassName, false);
            }
        }

        void OnFocusIn(FocusInEvent evt)
        {
            if (m_DescriptionTextField.ClassListContains(placeholderUssClassName))
            {
                m_DescriptionTextField.value = string.Empty;
                m_DescriptionTextField.RemoveFromClassList(placeholderUssClassName);
            }
        }

        void OnToggleChanged(ChangeEvent<bool> e)
        {
            var oldPropertiesField = new SubgraphPropertiesField
            {
                ShouldShowInLibrary = e.previousValue,
                DisplayedPath = m_PathTextField.value,
                Description = m_DescriptionTextField.value
            };

            var newPropertiesField = new SubgraphPropertiesField
            {
                ShouldShowInLibrary = e.newValue,
                DisplayedPath = m_PathTextField.value,
                Description = m_DescriptionTextField.value
            };

            DisplayElement(m_WarningBox, e.newValue && !m_GraphModel.CanBeSubgraph());
            DisplayElement(m_ToggleContainer, e.newValue);

            using (var ee = ChangeEvent<SubgraphPropertiesField>.GetPooled(oldPropertiesField, newPropertiesField))
            {
                ee.target = m_Container;
                m_Container.SendEvent(ee);
            }

            e.StopPropagation();
        }

        void OnPathTextFieldChanged(ChangeEvent<string> e)
        {
            var oldPropertiesField = new SubgraphPropertiesField
            {
                ShouldShowInLibrary = m_Toggle.value,
                DisplayedPath = e.previousValue,
                Description = m_DescriptionTextField.value
            };

            var newPropertiesField = new SubgraphPropertiesField
            {
                ShouldShowInLibrary = m_Toggle.value,
                DisplayedPath = e.newValue,
                Description = m_DescriptionTextField.value
            };

            using (var ee = ChangeEvent<SubgraphPropertiesField>.GetPooled(oldPropertiesField, newPropertiesField))
            {
                ee.target = m_Container;
                m_Container.SendEvent(ee);
            }

            e.StopPropagation();
        }

        void OnDescriptionFieldChanged(ChangeEvent<string> e)
        {
            var oldPropertiesField = new SubgraphPropertiesField
            {
                ShouldShowInLibrary = m_Toggle.value,
                DisplayedPath = m_PathTextField.value,
                Description = e.previousValue
            };

            var newPropertiesField = new SubgraphPropertiesField
            {
                ShouldShowInLibrary = m_Toggle.value,
                DisplayedPath = m_PathTextField.value,
                Description = e.newValue
            };

            using (var ee = ChangeEvent<SubgraphPropertiesField>.GetPooled(oldPropertiesField, newPropertiesField))
            {
                ee.target = m_Container;
                m_Container.SendEvent(ee);
            }

            e.StopPropagation();
        }
    }
}
