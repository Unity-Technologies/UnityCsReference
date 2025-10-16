// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor.ContextualMenuItems;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The supported sizes for the text of the sticky notes.
    /// </summary>
    [UnityRestricted]
    internal enum StickyNoteTextSize
    {
        Small,
        Medium,
        Large,
        Huge
    }

    /// <summary>
    /// The supported color themes for the sticky notes.
    /// </summary>
    [UnityRestricted]
    internal enum StickyNoteColorTheme
    {
        Classic,
        Orange,
        Green,
        Blue,
        Red,
        Purple,
        Teal,
        Pink,
        Black
    }

    /// <summary>
    /// A model that represents a sticky note in a graph.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class StickyNoteModel : GraphElementModel, IMovable, IHasTitle, IRenamable, IResizable
    {
        [SerializeField, HideInInspector]
        string m_Title;

        [SerializeField, HideInInspector]
        string m_Contents;

        [SerializeField, HideInInspector]
        string m_ThemeName = String.Empty;

        [SerializeField, HideInInspector]
        string m_TextSizeName = String.Empty;

        [SerializeField, HideInInspector]
        Rect m_Position;

        /// <inheritdoc />
        /// <remarks>Setter implementations must set the <see cref="ChangeHint.Layout"/> change hint.</remarks>
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
        /// <remarks>Setter implementations must set the <see cref="ChangeHint.Layout"/> change hint.</remarks>
        public virtual Vector2 Position
        {
            get => PositionAndSize.position;
            set => PositionAndSize = new Rect(value, PositionAndSize.size);
        }

        /// <inheritdoc />
        /// <remarks>Setter implementations must set the <see cref="ChangeHint.Data"/> change hint.</remarks>
        public virtual string Title
        {
            get => m_Title;
            set
            {
                if (value != null && m_Title != value)
                {
                    m_Title = value;
                    GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
                }
            }
        }

        /// <summary>
        /// The text content of the note.
        /// </summary>
        /// <remarks>Setter implementations must set the <see cref="ChangeHint.Data"/> change hints.</remarks>
        public virtual string Contents
        {
            get => m_Contents;
            set
            {
                if (value != null && m_Contents != value)
                {
                    m_Contents = value;
                    GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Data);
                }
            }
        }

        /// <summary>
        /// The theme to use to display the note.
        /// </summary>
        /// <remarks>Setter implementations must set the <see cref="ChangeHint.Style"/> change hints.</remarks>
        public virtual string Theme
        {
            get => m_ThemeName;
            set
            {
                if (m_ThemeName == value)
                    return;
                m_ThemeName = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Style);
            }
        }

        /// <summary>
        /// The size of the text used to display the note.
        /// </summary>
        /// <remarks>Setter implementations must set the <see cref="ChangeHint.Style"/> change hints.</remarks>
        public virtual string TextSize
        {
            get => m_TextSizeName;
            set
            {
                if (m_TextSizeName == value)
                    return;
                m_TextSizeName = value;
                GraphModel?.CurrentGraphChangeDescription.AddChangedModel(this, ChangeHint.Style);
            }
        }

        /// <summary>
        /// Whether the object was deleted from the graph.
        /// </summary>
        public bool Destroyed { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StickyNoteModel"/> class.
        /// </summary>
        public StickyNoteModel()
        {
            m_Capabilities.AddRange(new[]
            {
                Editor.Capabilities.Deletable,
                Editor.Capabilities.Copiable,
                Editor.Capabilities.Selectable,
                Editor.Capabilities.Renamable,
                Editor.Capabilities.Movable,
                Editor.Capabilities.Resizable,
                Editor.Capabilities.Ascendable
            });

            m_Title = string.Empty;
            m_Contents = string.Empty;
            m_ThemeName = StickyNoteColorTheme.Classic.ToString();
            m_TextSizeName = StickyNoteTextSize.Small.ToString();
            m_Position = Rect.zero;
        }

        /// <summary>
        /// Marks the object as being deleted from the graph.
        /// </summary>
        public void Destroy() => Destroyed = true;

        /// <inheritdoc />
        public virtual void Move(Vector2 delta)
        {
            if (!IsMovable())
                return;

            Position += delta;
        }

        /// <inheritdoc />
        public virtual void Rename(string newName)
        {
            if (!IsRenamable())
                return;

            Title = newName;
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            if (Theme == "Dark")
                Theme = StickyNoteColorTheme.Black.ToString();
        }

        /// <inheritdoc />
        public override IReadOnlyList<ContextualMenuItem> ContextualMenuItems
        {
            get
            {
                var graphElementModelCommonMenuItems = base.ContextualMenuItems;

                // Combine the common graph element menu items with the sticky note menu items.
                var menuItems = new List<ContextualMenuItem>(graphElementModelCommonMenuItems);
                menuItems.AddRange(k_ContextualMenuItems);
                return menuItems;
            }
        }

        static readonly List<ContextualMenuItem> k_ContextualMenuItems = new() {
            new ContextualMenuItem(ContextualMenuHelpers.fitToTextItem, 1),
            new ContextualMenuItem(ContextualMenuHelpers.fontSizeAndThemeItem, 2)
        };
    }
}
