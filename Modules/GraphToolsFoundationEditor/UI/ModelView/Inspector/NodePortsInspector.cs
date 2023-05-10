// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Inspector for node port default values.
    /// </summary>
    class NodePortsInspector : FieldsInspector
    {
        /// <summary>
        /// Creates a new instance of the <see cref="NodePortsInspector"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="models">The models displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="NodePortsInspector"/>.</returns>
        public static NodePortsInspector Create(string name, IReadOnlyList<Model> models, ChildView ownerElement, string parentClassName)
        {
            return new NodePortsInspector(name, models, ownerElement, parentClassName);
        }

        /// <inheritdoc />
        public NodePortsInspector(string name, IReadOnlyList<Model> models, ChildView ownerElement, string parentClassName)
            : base(name, models, ownerElement, parentClassName)
        {
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if (ShouldRebuildFields())
            {
                BuildFields();
            }

            foreach (var modelField in m_Fields)
            {
                modelField.UpdateDisplayedValue();
            }
        }

        /// <summary>
        /// Returns true if the fields should be completely rebuilt.
        /// </summary>
        /// <returns>True if the fields should be rebuilt.</returns>
        protected virtual bool ShouldRebuildFields()
        {
            var portsToDisplay = GetPortsToDisplay().ToList();

            if (portsToDisplay.Count != m_Fields.Count)
                return true;

            for (var i = 0; i < portsToDisplay.Count; i++)
            {
                if (m_Fields[i] is ConstantField constantField)
                {
                    if (!Enumerable.SequenceEqual(portsToDisplay[i].Select(t => t.NodeModel), constantField.Owners))
                        return true;

                    if (portsToDisplay[i].First().EmbeddedValue.Type != constantField.ConstantModels.First().Type)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the ports that should be displayed in the inspector.
        /// </summary>
        /// <returns>An enumerable of list of ports to display, each list contains ports for the same property in case of a multi selection.</returns>
        protected virtual IEnumerable<List<PortModel>> GetPortsToDisplay()
        {
            var portNodeModel = m_Models.OfType<PortNodeModel>().FirstOrDefault();

            if (portNodeModel == null)
                return Enumerable.Empty<List<PortModel>>();

            var portList = portNodeModel.Ports.Where(p => p.Options != PortModelOptions.IsNodeOption && p.Direction == PortDirection.Input && p.PortType == PortType.Data && p.EmbeddedValue != null)
                .Select(t => new List<PortModel>(new[] { t })).ToList();

            for (int i = 0; i < portList.Count; ++i)
            {
                var ports = portList[i];
                var firstPort = ports.First();
                // Only keep ports that are common to all the models.
                foreach (var other in m_Models.OfType<PortNodeModel>().Skip(1))
                {
                    var otherPort = other.Ports.FirstOrDefault(t => t.Direction == firstPort.Direction && t.UniqueName == firstPort.UniqueName && t.Orientation == firstPort.Orientation && t.DataTypeHandle == firstPort.DataTypeHandle);
                    if (otherPort == null)
                    {
                        portList.RemoveAt(i);
                        --i;
                        break;
                    }
                    else
                    {
                        ports.Add(otherPort);
                    }
                }
            }

            return portList;
        }

        /// <inheritdoc />
        protected override IEnumerable<BaseModelPropertyField> GetFields()
        {
            var ports = GetPortsToDisplay();

            if (ports == null)
                yield break;

            foreach (var port in ports)
            {
                yield return InlineValueEditor.CreateEditorForConstants(
                    OwnerRootView, port, port.Select(t => t.EmbeddedValue), false,
                    port.First().DisplayTitle ?? "");
            }
        }
    }
}
