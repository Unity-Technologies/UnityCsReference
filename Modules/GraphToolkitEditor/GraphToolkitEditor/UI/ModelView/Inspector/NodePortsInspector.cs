// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Inspector for node port default values.
    /// </summary>
    [UnityRestricted]
    internal class NodePortsInspector : FieldsInspector
    {
        List<BaseModelPropertyField> m_PortFields = new();
        List<ExpandablePortPropertyField> m_ExpandablePortPropertyFields = new();

        internal ModelInspectorStateComponent ModelInspectorState => (m_OwnerElement.RootView as ModelInspectorView)?.ModelInspectorViewModel.ModelInspectorState;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="NodePortsInspector"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="models">The models displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected NodePortsInspector(string name, IReadOnlyList<Model> models, ChildView ownerElement, string parentClassName)
            : base(name, models, ownerElement, parentClassName)
        {
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
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
            var portsToDisplay = GetPortsToDisplay();

            if (portsToDisplay.Count != m_PortFields.Count)
                return true;

            for (var i = 0; i < portsToDisplay.Count; i++)
            {
                var field = m_PortFields[i];
                var ports = portsToDisplay[i];
                if (ShouldRebuildField(field, ports)) return true;
            }

            return false;
        }

        static internal bool ShouldRebuildField(BaseModelPropertyField field, List<PortModel> ports)
        {
            if (field is ConstantField constantField)
            {
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                if (!ports.SequenceEqual(constantField.Owners))
#pragma warning restore RS0030
                    return true;

                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                if (ports[0].EmbeddedValue.Type != constantField.ConstantModels.First().Type)
#pragma warning restore RS0030
                    return true;
            }
            else if (field is ExpandablePortPropertyField expandablePortPropertyField)
            {
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                if (!ports.SequenceEqual(expandablePortPropertyField.ExpandablePorts))
#pragma warning restore RS0030
                    return true;

                if (expandablePortPropertyField.ShouldRebuildFields())
                    return true;
            }
            else if (field == null)
            {
                //we assume null fields will still be null for a given port otherwise any null field will result in a rebuild
                //The only case we want to manage is if the field was null because the sub ports were empty and now they are not.
                if (ports[0].IsExpandable && ports[0].SubPorts.Count > 0)
                    return true;
            }
            else
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the ports that should be displayed in the inspector.
        /// </summary>
        /// <returns>A list of lists of ports to display, each list of ports contains ports for the same property in case of a multi selection.</returns>
        protected virtual IReadOnlyList<List<PortModel>> GetPortsToDisplay()
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var portNodeModel = m_Models.OfType<PortNodeModel>().FirstOrDefault();
#pragma warning restore RS0030

            if (portNodeModel == null)
                return Array.Empty<List<PortModel>>();

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var portList = portNodeModel.GetPorts().Where(p => p.Options != PortModelOptions.IsNodeOption && p.Direction == PortDirection.Input && p.EmbeddedValue != null && p.ParentPort == null)
#pragma warning restore RS0030
                .Select(t => new List<PortModel>(new[] { t })).ToList();

            for (int i = 0; i < portList.Count; ++i)
            {
                var ports = portList[i];
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var firstPort = ports.First();
#pragma warning restore RS0030

                // Only keep ports that are common to all the models.
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                foreach (var other in m_Models.OfType<PortNodeModel>().Skip(1))
#pragma warning restore RS0030
                {
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    var otherPort = other.GetPorts().FirstOrDefault(t => t.Direction == firstPort.Direction && t.UniqueName == firstPort.UniqueName && t.Orientation == firstPort.Orientation && t.DataTypeHandle == firstPort.DataTypeHandle);
#pragma warning restore RS0030
                    if (otherPort == null)
                    {
                        portList.RemoveAt(i);
                        --i;
                        break;
                    }

                    ports.Add(otherPort);
                }
            }

            return portList;
        }
        /// <inheritdoc />
        protected override IReadOnlyList<BaseModelPropertyField> GetFields()
        {
            m_ExpandablePortPropertyFields.Clear();
            var fieldList = new List<BaseModelPropertyField>();
            var portGroups = GetPortsToDisplay();

            if (portGroups == null)
                return fieldList;

            m_PortFields.Clear();

            for (var i = 0; i < portGroups.Count; i++)
            {
                var ports = portGroups[i];

                var field = CreatePortEditor(ports);

                if (field != null)
                {
                    fieldList.Add(field);
                    m_PortFields.Add(field);
                }
                else
                    m_PortFields.Add(null);
            }

            return fieldList;
        }

        internal BaseModelPropertyField CreatePortEditor(List<PortModel> ports)
        {
            var constants = new List<Constant>();
            for (var j = 0; j < ports.Count; j++)
            {
                constants.Add(ports[j].EmbeddedValue);
            }

            var field = InlineValueEditor.CreateEditorForConstants(
                OwnerRootView, ports, constants, ports[0].Title ?? "");


            // We will always display a field for Expandable ports.
            // If we have a valid field (for instance for a Vector3), we will display it.
            // If we don't we will display a field with a foldout that will contain the sub port fields. (for instance for a Sphere struct)
            if (field.childCount > 0)
            {
                return field;
            }
            else if (ports[0].IsExpandable && ports[0].SubPorts.Count > 0)
            {
                var expandableField = new ExpandablePortPropertyField(OwnerRootView, this, ports);
                m_ExpandablePortPropertyFields.Add(expandableField);
                return expandableField;
            }

            return null;
        }

        internal void RefreshExpandablePortsFieldCollapse(HashSet<string> changeSetPortUniqueNameChanged)
        {
            foreach (var field in m_ExpandablePortPropertyFields)
            {
                field.RefreshCollapse(changeSetPortUniqueNameChanged);
            }
        }
    }
}
