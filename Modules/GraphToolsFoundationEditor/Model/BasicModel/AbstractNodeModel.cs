// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Search;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base class for a model that represents a node in a graph.
    /// </summary>
    [Serializable]
    abstract class AbstractNodeModel : GraphElementModel, IHasTitle, IMovable
    {
        [SerializeField, HideInInspector]
        Vector2 m_Position;

        [SerializeField, HideInInspector]
        string m_Title;

        [SerializeField, HideInInspector]
        string m_Tooltip;

        SpawnFlags m_SpawnFlags = SpawnFlags.Default;

        [SerializeReference, HideInInspector]
        NodePreviewModel m_NodePreviewModel;

        internal static string titleFieldName_Internal = nameof(m_Title);
        internal static string positionFieldName_Internal = nameof(m_Position);

        /// <summary>
        /// Does the node allow to be connected to itself.
        /// </summary>
        public abstract bool AllowSelfConnect { get; }

        /// <summary>
        /// Type of the node icon as a string.
        /// </summary>
        public abstract string IconTypeString { get; }

        /// <summary>
        /// State of the node model.
        /// </summary>
        public abstract ModelState State { get; set; }

        /// <inheritdoc />
        public virtual string Title
        {
            get => m_Title;
            set
            {
                if (m_Title == value)
                    return;
                m_Title = value;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <inheritdoc />
        public virtual string DisplayTitle => Title.Nicify();

        /// <summary>
        /// The Subtitle of the node.
        /// </summary>
        public virtual string Subtitle => GetType().GetAttribute<LibraryItemAttribute>()?.Subtitle;

        /// <summary>
        /// Tooltip to display.
        /// </summary>
        public virtual string Tooltip
        {
            get => string.IsNullOrEmpty(m_Tooltip)?DisplayTitle:m_Tooltip;
            set
            {
                if (m_Tooltip == value)
                    return;
                m_Tooltip = value;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Style);
            }
        }

        /// <inheritdoc />
        public virtual Vector2 Position
        {
            get => m_Position;
            set
            {
                if (!this.IsMovable())
                    return;

                if (m_Position == value)
                    return;

                m_Position = value;
                GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Layout);
            }
        }

        /// <summary>
        /// The flags specifying how the node is to be spawned.
        /// </summary>
        public virtual SpawnFlags SpawnFlags
        {
            get => m_SpawnFlags;
            set => m_SpawnFlags = value;
        }

        /// <summary>
        /// Whether the object was deleted from the graph.
        /// </summary>
        public bool Destroyed { get; private set; }

        /// <summary>
        /// Whether the node has a preview or not.
        /// </summary>
        public abstract bool HasNodePreview { get; }

        /// <summary>
        /// The preview of the node.
        /// </summary>
        public NodePreviewModel NodePreviewModel => HasNodePreview ? m_NodePreviewModel : null;

        /// <inheritdoc />
        public override IEnumerable<GraphElementModel> DependentModels => m_NodePreviewModel != null ? base.DependentModels.Append(m_NodePreviewModel) : base.DependentModels;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractNodeModel"/> class.
        /// </summary>
        protected AbstractNodeModel()
        {
            m_Capabilities.AddRange(new[]
            {
                Editor.Capabilities.Deletable,
                Editor.Capabilities.Droppable,
                Editor.Capabilities.Copiable,
                Editor.Capabilities.Selectable,
                Editor.Capabilities.Movable,
                Editor.Capabilities.Collapsible,
                Editor.Capabilities.Colorable,
                Editor.Capabilities.Ascendable
            });
        }

        /// <summary>
        /// Called on deletion of the node.
        /// </summary>
        public virtual void OnDeleteNode()
        {
            Destroyed = true;

            if (m_NodePreviewModel != null)
                GraphModel.CurrentGraphChangeDescription?.AddDeletedModels(m_NodePreviewModel);
        }

        /// <summary>
        /// Gets all wires connected to this node.
        /// </summary>
        /// <returns>All <see cref="WireModel"/> connected to this node.</returns>
        public abstract IEnumerable<WireModel> GetConnectedWires();

        /// <summary>
        /// Called on creation of the node.
        /// </summary>
        public virtual void OnCreateNode()
        {
            if (HasNodePreview && SpawnFlags != SpawnFlags.Orphan)
                AddNodePreview();
        }

        /// <summary>
        /// Called on duplication of the node.
        /// </summary>
        /// <param name="sourceNode">Model of the node duplicated.</param>
        public virtual void OnDuplicateNode(AbstractNodeModel sourceNode)
        {
            Title = sourceNode.Title;
            if (sourceNode.HasNodePreview)
            {
                var nodePreview = AddNodePreview();
                nodePreview.OnDuplicateNodePreview(sourceNode.NodePreviewModel);
            }
        }

        /// <inheritdoc />
        public virtual void Move(Vector2 delta)
        {
            if (!this.IsMovable())
                return;

            Position += delta;
        }

        /// <summary>
        /// Creates a node preview.
        /// </summary>
        /// <returns>The newly created node preview.</returns>
        protected virtual NodePreviewModel CreateNodePreview()
        {
            return new NodePreviewModel();
        }

        /// <summary>
        /// Creates and adds a node preview to the node.
        /// </summary>
        /// <returns>The newly created node preview.</returns>
        protected virtual NodePreviewModel AddNodePreview()
        {
            var nodePreviewModel = CreateNodePreview();
            m_NodePreviewModel = nodePreviewModel;
            nodePreviewModel.OnCreateNodePreview(this);

            GraphModel.RegisterNodePreview(m_NodePreviewModel);
            GraphModel.CurrentGraphChangeDescription?.AddNewModels(m_NodePreviewModel);

            return nodePreviewModel;
        }
    }
}
