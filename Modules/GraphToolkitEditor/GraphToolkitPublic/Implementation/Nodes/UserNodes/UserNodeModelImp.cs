// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//Do not edit UserNodeModelImp.cs directly : this file is auto-generated. Do not edit it directly. Make changes in UserNodeModelImp.inc.cs.t4 and re-run the template. ( Right click on .tt file in Rider).

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.Implementation
{

    [Serializable]
    class UserNodeModelImp : NodeModel, IUserNodeModelImp
    {
        [SerializeReference]
        Node m_Node;

        Node IUserNodeModelImp.Node => m_Node;
        public Node Node => m_Node;

        public override string Title => m_Node?.GetType().Name ?? "Missing Node";

        string m_CustomTooltip;
        Color m_CustomDefaultColor;

        public override string Tooltip
        {
            get => !string.IsNullOrEmpty(m_CustomTooltip) ? m_CustomTooltip : base.Tooltip;
            set
            {
                m_CustomTooltip = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Style);
            }
        }

        public override string IconPath => m_Node?.GetType().GetAttribute<NodeAttribute>()?.IconPath ?? base.IconPath;

        public override Color DefaultColor
        {
            get => m_CustomDefaultColor;
            set
            {
                m_CustomDefaultColor = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Style);
            }
        }

        protected override void OnDefineNode(NodeDefinitionScope definitionScope)
        {
            ((IUserNodeModelImp)this).CustomOnDefineNode(definitionScope);
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            m_Node?.SetImplementation(this);
        }

        public void InitCustomNode(Node node)
        {
            m_Node = node;
            Node.SetImplementation(this);
        }

        public override void OnDuplicateNode(AbstractNodeModel sourceNode)
        {
            ((IUserNodeModelImp)this).CallOnEnable();
            base.OnDuplicateNode(sourceNode);
        }

        public override void OnCreateNode()
        {
            ((IUserNodeModelImp)this).CallOnEnable();
            base.OnCreateNode();
        }

        protected override PortModel CreatePort(PortDirection direction, PortOrientation orientation, string portName, PortType portType, TypeHandle dataType, string portId, PortModelOptions options, Attribute[] attributes, PortModel parentPort)
        {
			return new PortModelImp(this, direction, orientation, portName, portType, dataType, portId, options, attributes, parentPort);
        }

    }

    partial class UserBlockNodeModelImp
    {
        [SerializeReference]
        BlockNode m_Node;

        Node IUserNodeModelImp.Node => m_Node;
        public BlockNode Node => m_Node;

        public override string Title => m_Node?.GetType().Name ?? "Missing Node";

        string m_CustomTooltip;
        Color m_CustomDefaultColor;

        public override string Tooltip
        {
            get => !string.IsNullOrEmpty(m_CustomTooltip) ? m_CustomTooltip : base.Tooltip;
            set
            {
                m_CustomTooltip = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Style);
            }
        }

        public override string IconPath => m_Node?.GetType().GetAttribute<NodeAttribute>()?.IconPath ?? base.IconPath;

        public override Color DefaultColor
        {
            get => m_CustomDefaultColor;
            set
            {
                m_CustomDefaultColor = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Style);
            }
        }

        protected override void OnDefineNode(NodeDefinitionScope definitionScope)
        {
            ((IUserNodeModelImp)this).CustomOnDefineNode(definitionScope);
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            m_Node?.SetImplementation(this);
        }

        public void InitCustomNode(BlockNode node)
        {
            m_Node = node;
            Node.SetImplementation(this);
        }

        public override void OnDuplicateNode(AbstractNodeModel sourceNode)
        {
            ((IUserNodeModelImp)this).CallOnEnable();
            base.OnDuplicateNode(sourceNode);
        }

        public override void OnCreateNode()
        {
            ((IUserNodeModelImp)this).CallOnEnable();
            base.OnCreateNode();
        }

        protected override PortModel CreatePort(PortDirection direction, PortOrientation orientation, string portName, PortType portType, TypeHandle dataType, string portId, PortModelOptions options, Attribute[] attributes, PortModel parentPort)
        {
			if (orientation == PortOrientation.Vertical)
				Debug.LogWarning($"The {(direction == PortDirection.Input ? "input" : "output")} port {portId} in {m_Node.GetType()} is configured as a vertical port. Vertical ports are not supported on block nodes.");
			return new PortModelImp(this, direction, PortOrientation.Horizontal, portName, portType, dataType, portId, options, attributes, parentPort);
        }

    }

    partial class UserContextNodeModelImp
    {
        [SerializeReference]
        ContextNode m_Node;

        Node IUserNodeModelImp.Node => m_Node;
        public ContextNode Node => m_Node;

        public override string Title => m_Node?.GetType().Name ?? "Missing Node";

        string m_CustomTooltip;
        Color m_CustomDefaultColor = Color.darkGreen;

        public override string Tooltip
        {
            get => !string.IsNullOrEmpty(m_CustomTooltip) ? m_CustomTooltip : base.Tooltip;
            set
            {
                m_CustomTooltip = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Style);
            }
        }

        public override string IconPath => m_Node?.GetType().GetAttribute<NodeAttribute>()?.IconPath ?? base.IconPath;

        public override Color DefaultColor
        {
            get => m_CustomDefaultColor;
            set
            {
                m_CustomDefaultColor = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Style);
            }
        }

        protected override void OnDefineNode(NodeDefinitionScope definitionScope)
        {
            ((IUserNodeModelImp)this).CustomOnDefineNode(definitionScope);
			base.OnDefineNode(definitionScope);
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            m_Node?.SetImplementation(this);
        }

        public void InitCustomNode(ContextNode node)
        {
            m_Node = node;
            Node.SetImplementation(this);
        }

        public override void OnDuplicateNode(AbstractNodeModel sourceNode)
        {
            ((IUserNodeModelImp)this).CallOnEnable();
            base.OnDuplicateNode(sourceNode);
        }

        public override void OnCreateNode()
        {
            ((IUserNodeModelImp)this).CallOnEnable();
            base.OnCreateNode();
        }

        protected override PortModel CreatePort(PortDirection direction, PortOrientation orientation, string portName, PortType portType, TypeHandle dataType, string portId, PortModelOptions options, Attribute[] attributes, PortModel parentPort)
        {
			return new PortModelImp(this, direction, orientation, portName, portType, dataType, portId, options, attributes, parentPort);
        }

    }
}
