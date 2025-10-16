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
    /// A model that represents a placemat in a graph.
    /// </summary>
    /// <remarks>
    /// 'PlacematModel' represents a placemat in a graph, and serves as a container to help users visually organize their node networks.
    /// Placemats provide a way to group related nodes for better readability and workflow management, but they do not introduce additional
    /// logic or functionality beyond organization.
    /// </remarks>
    [Serializable]
    [UnityRestricted]
    internal class PlacematModel : GraphElementModel, IHasTitle, IMovable, IResizable, IRenamable, IHasElementColor
    {
        const string k_DefaultPlacematName = "Placemat";
        const int k_MinTitleFontSize = 16;
        const int k_MaxTitleFontSize = 150;

        [SerializeField, HideInInspector]
        string m_Title;

        [SerializeField, InspectorUseProperty(nameof(TitleFontSize))]
        int m_TitleFontSize;

        [SerializeField]
        TextAlignment m_TitleAlignment;

        [SerializeField, HideInInspector]
        Rect m_Position;

        [SerializeField, Multiline, Delayed, InspectorFieldOrder(0)]
        string m_Comment;

        [SerializeField, HideInInspector]
        ElementColor m_ElementColor;

        /// <summary>
        /// The color of the placemat.
        /// </summary>
        /// <remarks>Used to show the color field in the placemat inspector.</remarks>
        public Color Color => m_ElementColor.Color;

        /// <inheritdoc />
        public ElementColor ElementColor
        {
            get
            {
                // Called here because EditorGUIUtility.isProSkin cannot be called in the constructor.
                if (m_ElementColor.Color == default)
                    m_ElementColor = new ElementColor(this, DefaultColor);
                return m_ElementColor;
            }
            protected set
            {
                m_ElementColor = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Style);
            }
        }

        /// <inheritdoc />
        public void SetColor(Color color) => m_ElementColor.Color = color;

        /// <inheritdoc />
        public Color DefaultColor => EditorGUIUtility.isProSkin ? new Color(53.0f / 255.0f, 58.0f / 255.0f, 71.0f / 255.0f) : new Color(125.0f / 255.0f, 132.0f / 255.0f, 161.0f / 255.0f);

        /// <inheritdoc />
        public bool UseColorAlpha => false;

        /// <inheritdoc />
        public virtual string Title
        {
            get => m_Title;
            set
            {
                if (m_Title == value)
                    return;
                m_Title = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        public int TitleFontSize
        {
            get => m_TitleFontSize;
            set
            {
                if (m_TitleFontSize == value)
                    return;

                m_TitleFontSize = value > k_MaxTitleFontSize ? k_MaxTitleFontSize : (value < k_MinTitleFontSize ? k_MinTitleFontSize : value);
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        public TextAlignment TitleAlignment
        {
            get => m_TitleAlignment;
            set
            {
                if (m_TitleAlignment == value)
                    return;

                m_TitleAlignment = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        public string Comment
        {
            get => m_Comment;
            set
            {
                if (m_Comment == value)
                    return;

                m_Comment = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
            }
        }

        /// <inheritdoc />
        /// <remarks>Setter implementations should set the <see cref="ChangeHint.Layout"/> change hint.</remarks>
        public virtual Rect PositionAndSize
        {
            get => m_Position;
            set
            {
                var r = value;
                if (!IsResizable())
                    r.size = m_Position.size;

                if (!IsMovable())
                    r.position = m_Position.position;

                if (r != m_Position)
                {
                    GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Layout);
                    m_Position = r;
                }
            }
        }

        /// <inheritdoc />
        /// <remarks>Setter implementations should set the <see cref="ChangeHint.Layout"/> change hint.</remarks>
        public virtual Vector2 Position
        {
            get => PositionAndSize.position;
            set => PositionAndSize = new Rect(value, PositionAndSize.size);
        }

        /// <summary>
        /// Whether the object was deleted from the graph.
        /// </summary>
        public bool Destroyed { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlacematModel"/> class.
        /// </summary>
        public PlacematModel()
        {
            m_Capabilities.AddRange(new[]
            {
                Editor.Capabilities.Deletable,
                Editor.Capabilities.Copiable,
                Editor.Capabilities.Selectable,
                Editor.Capabilities.Renamable,
                Editor.Capabilities.Movable,
                Editor.Capabilities.Resizable,
                Editor.Capabilities.Collapsible,
                Editor.Capabilities.Colorable,
                Editor.Capabilities.Ascendable
            });

            m_Title = k_DefaultPlacematName;
        }

        /// <summary>
        /// Marks the object as being deleted from the graph.
        /// </summary>
        public virtual void Destroy() => Destroyed = true;

        /// <inheritdoc />
        public virtual void Move(Vector2 delta)
        {
            if (!IsMovable())
                return;

            PositionAndSize = new Rect(PositionAndSize.position + delta, PositionAndSize.size);
        }

        /// <inheritdoc />
        public virtual void Rename(string newName)
        {
            if (!IsRenamable())
                return;

            Title = newName;
        }

        /// <summary>
        /// Returns the Z-order of the placemat in the graph.
        /// </summary>
        /// <returns>The Z-order of the placemat in the graph.</returns>
        public virtual int GetZOrder() => GraphModel.PlacematModels.IndexOf(this);

        /// <summary>
        /// Copies parameters from a source placemat to a destination placemat.
        /// </summary>
        /// <param name="source">The placemat from which to copy parameters.</param>
        /// <param name="destination">The placemat to which the parameters are copied.</param>
        /// <remarks>
        /// 'CopyPlacematParameters' transfers parameters from a source placemat to a destination placemat. This ensures that the destination placemat retains the same
        /// properties as the source, such as <see cref="Title"/>, <see cref="ElementColor"/>, <see cref="TitleFontSize"/>, <see cref="TitleAlignment"/>, and <see cref="Comment"/>.
        /// Use this method when duplicating or synchronizing placemat settings between different instances.
        /// </remarks>
        public static void CopyPlacematParameters(PlacematModel source, PlacematModel destination)
        {
            destination.Title = source.Title;
            destination.ElementColor = new ElementColor(destination, source.ElementColor.Color, source.ElementColor.HasUserColor);
            destination.TitleFontSize = source.TitleFontSize;
            destination.TitleAlignment = source.TitleAlignment;
            destination.Comment = source.Comment;
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            m_ElementColor.OwnerElementModel = this;
        }

        /// <inheritdoc />
        public override IReadOnlyList<ContextualMenuItem> ContextualMenuItems
        {
            get
            {
                var graphElementModelCommonMenuItems = base.ContextualMenuItems;

                // Combine the common graph element menu items with the node menu items.
                var menuItems = new List<ContextualMenuItem>(graphElementModelCommonMenuItems);
                menuItems.AddRange(k_ContextualMenuItems);
                return menuItems;
            }
        }

        static readonly List<ContextualMenuItem> k_ContextualMenuItems = new() {
            ContextualMenuHelpers.deleteAndSelectContentsItem,
            new ContextualMenuItem(ContextualMenuHelpers.smartResizeItem, 0),
            new ContextualMenuItem(ContextualMenuHelpers.reorderPlacematItem, 1),
            ContextualMenuHelpers.selectAllPlacematContentsItem
        };
    }
}
