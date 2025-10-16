// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.CSO;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    class ExpandablePortPropertyField : BaseModelPropertyField
    {
        /// <summary>
        /// The USS class name added to a <see cref="ExpandablePortPropertyField"/>.
        /// </summary>
        public new static readonly string ussClassName = "ge-expandable-port-property-field";

        /// <summary>
        /// The USS class name added to the title container
        /// </summary>
        public static readonly string titleContainerUssClassName = ussClassName.WithUssElement(GraphElementHelper.titleContainerName);

        /// <summary>
        /// The USS class name added to the sub ports fields container.
        /// </summary>
        public static readonly string subPortFieldsUssClassName = ussClassName.WithUssElement("sub-port-fields");


        /// <summary>
        /// The USS class name added to the expand toggle.
        /// </summary>
        public static readonly string expandTogglesUssClassName = ussClassName.WithUssElement("expand-toggle");

        /// <summary>
        /// The USS class name added to this sub ports fields container if collapsed
        /// </summary>
        public static readonly string collapsedSubPortFieldsUssClassName = subPortFieldsUssClassName.WithUssModifier(GraphElementHelper.collapsedUssModifier);

        NodePortsInspector m_NodePortsInspector;
        Toggle m_ExpandToggle;
        VisualElement m_SubPortFieldContainer;

        List<BaseModelPropertyField> m_SubPortFields = new();

        /// <summary>
        /// The list of expandable ports displayed by this field.
        /// </summary>
        public IReadOnlyList<PortModel> ExpandablePorts { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomizableModelPropertyField"/> class.
        /// </summary>
        /// <param name="commandTarget">The view to use to dispatch commands when the field is edited.</param>
        /// <param name="nodePortsInspector">The containing node inspector.</param>
        /// <param name="expandablePorts">The ports displayed by this field.</param>
        public ExpandablePortPropertyField(ICommandTarget commandTarget, NodePortsInspector nodePortsInspector, IReadOnlyList<PortModel> expandablePorts)
            : base(commandTarget)
        {
            m_NodePortsInspector = nodePortsInspector;
            ExpandablePorts = new List<PortModel>(expandablePorts);
            AddToClassList(ussClassName);
            var titleContainer = new VisualElement();

            m_ExpandToggle = new Toggle();
            m_ExpandToggle.AddToClassList(Foldout.toggleUssClassName);
            m_ExpandToggle.AddToClassList(expandTogglesUssClassName);

            if (ExpandablePorts.Count > 0)
            {
                m_ExpandToggle.RegisterCallback<ChangeEvent<bool>>(
                    t => commandTarget.Dispatch(new ExpandExpandablePortInInspectorCommand(ExpandablePorts[0].UniqueName, t.newValue))); //TODO use the ModelInspectorStateComponent to store the state of the foldout
            }
            LabelElement = new Label(GraphElementHelper.titleName);
            LabelElement.AddToClassList(labelUssClassName);
            LabelElement.tooltip = expandablePorts[0].ToolTip;
            titleContainer.Add(m_ExpandToggle);
            titleContainer.Add(LabelElement);
            titleContainer.AddToClassList(titleContainerUssClassName);
            Add(titleContainer);

            m_SubPortFieldContainer = new VisualElement();
            m_SubPortFieldContainer.AddToClassList(subPortFieldsUssClassName);
            Add(m_SubPortFieldContainer);

            using var dispose = ListPool<List<PortModel>>.Get(out var portGroups);

            var firstSubPorts = expandablePorts[0].SubPorts;
            for (int i = 0; i < firstSubPorts.Count; ++i)
            {
                var list = new List<PortModel>(expandablePorts.Count);
                list.Add(firstSubPorts[i]);
                portGroups.Add(list);
            }

            // For now, if the SubPorts in each port don't match, we don't display anything.
            for (int i = 1; i < expandablePorts.Count; --i)
            {
                var subPorts = expandablePorts[i].SubPorts;
                if (subPorts.Count != portGroups.Count)
                    return;
                for (int j = 0; j < subPorts.Count; ++j)
                {
                    var firstSubPort = firstSubPorts[j];
                    var subPort = subPorts[j];
                    if (firstSubPort.PortId != subPort.PortId || firstSubPort.DataTypeHandle == subPort.DataTypeHandle)
                        return;
                    portGroups[j].Add(subPorts[j]);
                }
            }

            bool empty = true;

            foreach (var ports in portGroups)
            {
                var field = m_NodePortsInspector.CreatePortEditor(ports);

                m_SubPortFields.Add(field);
                if (field != null)
                {
                    if (empty)
                    {
                        empty = field is ExpandablePortPropertyField subExpandablePortField && subExpandablePortField.style.display == DisplayStyle.None;
                    }
                    m_SubPortFieldContainer.Add(field);
                }
            }
            this.AddPackageStylesheet("ExpandablePortPropertyField.uss");
            SyncExpandToggle();

            // In the case where we couldn't create any field, we hide ourself.
            if (empty)
            {
                style.display = DisplayStyle.None;
            }
        }

        /// <inheritdoc />
        public override void UpdateDisplayedValue()
        {
            if (ExpandablePorts.Count == 0)
                return;

            LabelElement.text = ExpandablePorts[0].Title;

            foreach (var subPortElement in m_SubPortFieldContainer.Children())
            {
                if (subPortElement is BaseModelPropertyField subPortField)
                {
                    subPortField.UpdateDisplayedValue();
                }
            }
        }

        internal void RefreshCollapse(HashSet<string> changeSetPortUniqueNameChanged)
        {
            if (ExpandablePorts.Count == 0)
                return;

            if (changeSetPortUniqueNameChanged.Contains(ExpandablePorts[0].UniqueName))
            {
                SyncExpandToggle();
            }
        }

        void SyncExpandToggle()
        {
            if (ExpandablePorts.Count == 0)
                return;

            if (m_ExpandToggle != null && m_NodePortsInspector.ModelInspectorState != null)
            {
                var expanded = m_NodePortsInspector.ModelInspectorState.IsExpandablePortExpanded(ExpandablePorts[0].UniqueName);

                m_ExpandToggle.SetValueWithoutNotify(expanded);
                m_SubPortFieldContainer.EnableInClassList(collapsedSubPortFieldsUssClassName, !expanded);
            }
        }

        /// <summary>
        /// Whether the fields should be rebuilt based on the current model.
        /// </summary>
        /// <returns>Whether the fields should be rebuilt based on the current model.</returns>
        public bool ShouldRebuildFields()
        {
            if (ExpandablePorts.Count == 0)
                return false;

            if (ExpandablePorts[0].SubPorts.Count != m_SubPortFields.Count)
                return false;

            using var pool = ListPool<PortModel>.Get(out var subPorts);
            for (int i = 0; i < m_SubPortFields.Count; ++i)
            {
                subPorts.Clear();

                foreach (var port in ExpandablePorts)
                {
                    subPorts.Add(port.SubPorts[i]);
                }

                if (NodePortsInspector.ShouldRebuildField(m_SubPortFields[i], subPorts))
                    return true;
            }

            return false;
        }
    }
}
