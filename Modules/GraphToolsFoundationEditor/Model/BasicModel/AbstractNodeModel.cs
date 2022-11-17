// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
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
            set => m_Title = value;
        }

        /// <inheritdoc />
        public virtual string DisplayTitle => Title.Nicify();

        /// <summary>
        /// Tooltip to display.
        /// </summary>
        public virtual string Tooltip
        {
            get => string.IsNullOrEmpty(m_Tooltip)?DisplayTitle:m_Tooltip;
            set => m_Tooltip = value;
        }

        /// <inheritdoc />
        public virtual Vector2 Position
        {
            get => m_Position;
            set
            {
                if (!this.IsMovable())
                    return;

                m_Position = value;
            }
        }

        /// <summary>
        /// Whether the object was deleted from the graph.
        /// </summary>
        public bool Destroyed { get; private set; }

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
        /// Marks the object as being deleted from the graph.
        /// </summary>
        public void Destroy() => Destroyed = true;

        /// <summary>
        /// Gets all wires connected to this node.
        /// </summary>
        /// <returns>All <see cref="WireModel"/> connected to this node.</returns>
        public abstract IEnumerable<WireModel> GetConnectedWires();

        /// <summary>
        /// Called on creation of the node.
        /// </summary>
        public virtual void OnCreateNode() { }

        /// <summary>
        /// Called on duplication of the node.
        /// </summary>
        /// <param name="sourceNode">Model of the node duplicated.</param>
        public virtual void OnDuplicateNode(AbstractNodeModel sourceNode)
        {
            Title = sourceNode.Title;
        }

        /// <inheritdoc />
        public virtual void Move(Vector2 delta)
        {
            if (!this.IsMovable())
                return;

            Position += delta;
        }
    }
}
