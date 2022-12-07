// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A model that represents a placemat in a graph.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    class PlacematModel : GraphElementModel, IHasTitle, IMovable, IResizable, IRenamable
    {
        const string k_DefaultPlacematName = "Placemat";
        const int k_MinTitleFontSize = 16;
        const int k_MaxTitleFontSize = 150;

        [SerializeField]
        string m_Title;

        [SerializeField, InspectorUseProperty(nameof(TitleFontSize))]
        int m_TitleFontSize;

        [SerializeField]
        TextAlignment m_TitleAlignment;

        [SerializeField, HideInInspector]
        Rect m_Position;

        [SerializeField, Multiline]
        string m_Comment;

        public override Color DefaultColor => new Color(74.0f/255.0f, 88.0f/255.0f, 91.0f / 255.0f);

        /// <inheritdoc />
        public override bool UseColorAlpha => false;

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

        public int TitleFontSize
        {
            get => m_TitleFontSize;
            set => m_TitleFontSize = value > k_MaxTitleFontSize ? k_MaxTitleFontSize : (value < k_MinTitleFontSize ? k_MinTitleFontSize : value);
        }

        public TextAlignment TitleAlignment
        {
            get => m_TitleAlignment;
            set => m_TitleAlignment = value;
        }

        public string Comment
        {
            get => m_Comment;
            set => m_Comment = value;
        }

        /// <inheritdoc />
        public virtual string DisplayTitle => Title;

        /// <inheritdoc />
        public virtual Rect PositionAndSize
        {
            get => m_Position;
            set
            {
                var r = value;
                if (!this.IsResizable())
                    r.size = m_Position.size;

                if (!this.IsMovable())
                    r.position = m_Position.position;

                if (r != m_Position)
                    GraphModel?.CurrentGraphChangeDescription?.AddChangedModel(this, ChangeHint.Layout);

                m_Position = r;
            }
        }

        /// <inheritdoc />
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
            if (!this.IsMovable())
                return;

            PositionAndSize = new Rect(PositionAndSize.position + delta, PositionAndSize.size);
        }

        /// <inheritdoc />
        public virtual void Rename(string newName)
        {
            if (!this.IsRenamable())
                return;

            Title = newName;
        }

        /// <summary>
        /// Returns the Z-order of the placemat in the graph.
        /// </summary>
        /// <returns>The Z-order of the placemat in the graph.</returns>
        public virtual int GetZOrder() => GraphModel.PlacematModels.IndexOf_Internal(this);

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            if (Version <= SerializationVersion.GTF_V_0_8_2)
            {
                if (DefaultColor != SerializedColor_Internal)
                {
                    // sets HasUserColor properly
                    Color = SerializedColor_Internal;
                }
            }
        }

        public static void CopyPlacematParameters(PlacematModel source, PlacematModel destination)
        {
            destination.Title = source.Title;
            destination.Color = source.Color;
            destination.TitleFontSize = source.TitleFontSize;
            destination.TitleAlignment = source.TitleAlignment;
            destination.Comment = source.Comment;
        }
    }
}
