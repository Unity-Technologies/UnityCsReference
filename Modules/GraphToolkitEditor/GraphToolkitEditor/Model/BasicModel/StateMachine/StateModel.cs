// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor.ContextualMenuItems;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for nodes that represent a state in a state machine.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class StateModel : PortNodeModel, IRenamable
    {
        const string k_DefaultName = "State";

        const string k_OutgoingPortId = "Outgoing";
        const string k_IncomingPortId = "Incoming";

        // Don't serialize these ports when entering play mode, they are created on demand.
        [NonSerialized]
        StatePortModel m_OutPort;
        [NonSerialized]
        StatePortModel m_InPort;
        [SerializeField]
        ElementColor m_ElementColor;

        /// <inheritdoc />
        public override bool AllowSelfConnect => true;

        /// <inheritdoc />
        public override string IconTypeString { get; set; } = string.Empty;

        /// <inheritdoc />
        public override ElementColor ElementColor => m_ElementColor;

        /// <inheritdoc />
        public override void SetColor(Color color) => m_ElementColor.Color = color;

        /// <inheritdoc />
        public override Color DefaultColor => default;

        /// <inheritdoc />
        public override bool UseColorAlpha => true;

        /// <inheritdoc />
        public override bool HasNodePreview => false;

        /// <summary>
        /// Gets the port on which to connect the outgoing connections of this state.
        /// </summary>
        /// <returns>The port.</returns>
        public StatePortModel GetOutPort()
        {
            if (m_OutPort == null)
                DefinePorts();

            return m_OutPort;
        }

        /// <summary>
        /// Gets the port on which to connect the incoming connections of this state.
        /// </summary>
        /// <returns>The port.</returns>
        public StatePortModel GetInPort()
        {
            if (m_InPort == null)
                DefinePorts();

            return m_InPort;
        }

        /// <inheritdoc />
        public override IReadOnlyCollection<PortModel> GetPorts() => new[] { GetOutPort(), GetInPort() };

        /// <summary>
        /// Whether this state is the default entry state of the graph.
        /// </summary>
        public bool IsEntryPoint => this == GraphModel?.EntryPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateModel"/> class.
        /// </summary>
        public StateModel()
        {
            m_Capabilities = new List<Capabilities>()
            {
                Unity.GraphToolkit.Editor.Capabilities.Selectable,
                Unity.GraphToolkit.Editor.Capabilities.Deletable,
                Unity.GraphToolkit.Editor.Capabilities.Copiable,
                Unity.GraphToolkit.Editor.Capabilities.Renamable,
                Unity.GraphToolkit.Editor.Capabilities.Movable
            };
            m_ElementColor = new ElementColor(this);
        }

        void DefinePorts()
        {
            if (m_OutPort == null)
            {
                m_OutPort = new StatePortModel(PortDirection.Output, this, k_OutgoingPortId);

                GraphModel?.RegisterPort(m_OutPort);
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.GraphTopology);
            }

            if (m_InPort == null)
            {
                m_InPort = new StatePortModel(PortDirection.Input, this, k_IncomingPortId);

                GraphModel?.RegisterPort(m_InPort);
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.GraphTopology);
            }
        }

        /// <inheritdoc />
        public override void OnCreateNode()
        {
            base.OnCreateNode();
            DefinePorts();
        }

        /// <inheritdoc />
        public override void OnDuplicateNode(AbstractNodeModel sourceNode)
        {
            base.OnDuplicateNode(sourceNode);

            var newName = (sourceNode as IHasTitle)?.Title ?? "New State";
            Title = GetUniqueName(newName);

            DefinePorts();
        }

        /// <inheritdoc />
        public override PortModel GetPortFitToConnectTo(PortModel portModel)
        {
            if (portModel.PortType == PortType.State)
            {
                switch (portModel.Direction)
                {
                    case PortDirection.Input:
                        return GetOutPort();
                    case PortDirection.Output:
                        return GetInPort();
                    case PortDirection.None:
                    default:
                        break;
                }
            }

            return null;
        }

        /// <inheritdoc />
        public override bool RemoveUnusedMissingPort(PortModel portModel) => false;

        /// <inheritdoc />
        public virtual void Rename(string newName)
        {
            var setName = newName;
            if (string.IsNullOrEmpty(setName))
            {
                setName = k_DefaultName;
            }

            Title = GetUniqueName(setName);
        }

        string GetUniqueName(string name)
        {
            if (GraphModel == null)
                return name;

            var otherNames = new List<string>();
            foreach (var node in GraphModel.NodeModels)
            {
                if (node is IHasTitle hasTitle && !ReferenceEquals(hasTitle, this))
                {
                    otherNames.Add(hasTitle.Title);
                }
            }
            return ObjectNames.GetUniqueName(otherNames.ToArray(), name);
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            if (m_OutPort is { NodeModel : null })
                m_OutPort.NodeModel = this;
            if (m_InPort is { NodeModel : null })
                m_InPort.NodeModel = this;

            m_ElementColor.OwnerElementModel = this;
        }

        /// <inheritdoc />
        public override IReadOnlyList<ContextualMenuItem> ContextualMenuItems => k_ContextualMenuItems;

        static readonly List<ContextualMenuItem> k_ContextualMenuItems = new() {
            ContextualMenuHelpers.createTransitionMenuItem,
            ContextualMenuHelpers.createLocalTransitionMenuItem,
            ContextualMenuHelpers.createOnEnterTransitionMenuItem,
            ContextualMenuHelpers.createSelfTransitionMenuItem,
            ContextualMenuHelpers.createPlacematItem,

            ContextualMenuHelpers.cutItem,
            ContextualMenuHelpers.copyItem,
            ContextualMenuHelpers.pasteItem,
            ContextualMenuHelpers.pasteAsNewMenuItem,

            ContextualMenuHelpers.renameItem,
            ContextualMenuHelpers.duplicateItem,
            ContextualMenuHelpers.deleteItem,

            ContextualMenuHelpers.setAsDefaultStateMenuItem,
            ContextualMenuHelpers.colorItem
        };
    }
}
