// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.GraphToolkit.CSO;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal class SubgraphFieldCustomPropertyField : ICustomPropertyFieldBuilder<SubgraphPropertiesField>
    {
        /// <summary>
        /// The USS class name added to a <see cref="SubgraphFieldCustomPropertyField"/>.
        /// </summary>
        public static readonly string ussClassName = "subgraph-properties";

        /// <summary>
        /// The USS class name added to the container of a <see cref="SubgraphFieldCustomPropertyField"/>.
        /// </summary>
        public static readonly string containerUssClassName = ussClassName.WithUssElement(GraphElementHelper.containerName);

        /// <summary>
        /// The USS class name added to the toggle of a <see cref="SubgraphFieldCustomPropertyField"/>.
        /// </summary>
        public static readonly string toggleContainerUssClassName = ussClassName.WithUssElement("toggle-container");

        /// <summary>
        /// The USS class name added to the description of a <see cref="SubgraphFieldCustomPropertyField"/>.
        /// </summary>
        public static readonly string descriptionUssClassName = ussClassName.WithUssElement("description");

        /// <summary>
        /// The USS class name added to hide the content of <see cref="SubgraphFieldCustomPropertyField"/>.
        /// </summary>
        public static readonly string hiddenUssClassName = ussClassName.WithUssElement(GraphElementHelper.hiddenUssModifier);

        /// <summary>
        /// The USS class name added to the text area of a <see cref="SubgraphFieldCustomPropertyField"/>.
        /// </summary>
        public static readonly string textAreaUssClassName = ussClassName.WithUssElement(GraphElementHelper.textAreaName);

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
                var shouldDisplayWarning = value.ShouldShowInLibrary && !m_GraphModel.CanBeSubgraph();
                m_Toggle.SetValueWithoutNotify(!shouldDisplayWarning && value.ShouldShowInLibrary);
                m_Toggle.SetEnabled(!shouldDisplayWarning);
                DisplayElement(m_WarningBox, shouldDisplayWarning);

                if (m_WarningBox != null)
                    m_WarningBox.text = m_WarningMessage ?? "The conditions for this graph to be usable as a subgraph are not met.";

                if (m_PathTextField != null && m_DescriptionTextField != null)
                {
                    DisplayElement(m_ToggleContainer, value.ShouldShowInLibrary);

                    if (shouldDisplayWarning)
                    {
                        DisplayElement(m_PathTextField, false);
                        DisplayElement(m_DescriptionTextField, false);
                    }
                    else
                    {
                        DisplayElement(m_PathTextField, true);
                        DisplayElement(m_DescriptionTextField, true);

                        m_PathTextField.SetValueWithoutNotify(value.DisplayedPath);
                        var description = value.Description;
                        if (string.IsNullOrEmpty(description))
                        {
                            m_DescriptionTextField.textEdition.placeholder = SubgraphPropertiesField.descriptionPlaceholder;
                        }
                        m_DescriptionTextField.SetValueWithoutNotify(description);
                    }
                }
            }

            return true;
        }

        public void SetMixed()
        {
            // This should never happen as we don't have a way to select multiple GraphModel at the same time.
        }

        public (Label label, VisualElement field) Build(ICommandTarget commandTargetView, string label, string tooltip, IReadOnlyList<object> obj, string propertyName)
        {
            if (obj[0] is GraphModel graphModel)
            {
                m_GraphModel = graphModel;

                if (m_GraphModel.IsContainerGraph())
                    return (null, null);

                m_Container = new VisualElement();
                m_Container.AddPackageStylesheet("SubgraphPropertiesField.uss");
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

                m_DescriptionTextField = new TextField
                {
                    label = "Description",
                    multiline = true
                };

                m_DescriptionTextField.RegisterCallback<ChangeEvent<string>>(OnDescriptionFieldChanged);
                m_DescriptionTextField.AddToClassList(descriptionUssClassName);
                m_DescriptionTextField.AddToClassList(textAreaUssClassName);
                m_ToggleContainer.Add(m_DescriptionTextField);

                return (null, m_Container);
            }

            return (null, null);
        }

        static void DisplayElement(VisualElement element, bool shouldDisplay)
        {
            element.EnableInClassList(hiddenUssClassName, !shouldDisplay);
        }

        void OnToggleChanged(ChangeEvent<bool> e)
        {
            var oldPropertiesField = new SubgraphPropertiesField(m_WarningMessage)
            {
                ShouldShowInLibrary = e.previousValue,
                DisplayedPath = m_PathTextField.value,
                Description = m_DescriptionTextField.value
            };

            var newPropertiesField = new SubgraphPropertiesField(m_WarningMessage)
            {
                ShouldShowInLibrary = e.newValue,
                DisplayedPath = m_PathTextField.value,
                Description = m_DescriptionTextField.value
            };

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
