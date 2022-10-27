// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// The supported sizes for the text of the sticky notes.
    /// </summary>
    enum StickyNoteTextSize
    {
        Small,
        Medium,
        Large,
        Huge
    }

    /// <summary>
    /// The supported color themes for the sticky notes.
    /// </summary>
    enum StickyNoteColorTheme
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
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    class StickyNoteModel : GraphElementModel, IMovable, IHasTitle, IRenamable, IResizable
    {
        [SerializeField]
        string m_Title;

        [SerializeField]
        string m_Contents;

        [SerializeField]
        string m_ThemeName = String.Empty;

        [SerializeField]
        string m_TextSizeName = String.Empty;

        [SerializeField]
        Rect m_Position;

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

                m_Position = r;
            }
        }

        /// <inheritdoc />
        public virtual Vector2 Position
        {
            get => PositionAndSize.position;
            set => PositionAndSize = new Rect(value, PositionAndSize.size);
        }

        /// <inheritdoc />
        public virtual string Title
        {
            get => m_Title;
            set { if (value != null && m_Title != value) m_Title = value; }
        }

        /// <inheritdoc />
        public virtual string DisplayTitle => Title;

        /// <summary>
        /// The text content of the note.
        /// </summary>
        public virtual string Contents
        {
            get => m_Contents;
            set { if (value != null && m_Contents != value) m_Contents = value; }
        }

        /// <summary>
        /// The theme to use to display the note.
        /// </summary>
        public virtual string Theme
        {
            get => m_ThemeName;
            set => m_ThemeName = value;
        }

        /// <summary>
        /// The size of the text used to display the note.
        /// </summary>
        public virtual string TextSize
        {
            get => m_TextSizeName;
            set => m_TextSizeName = value;
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
            if (!this.IsMovable())
                return;

            Position += delta;
        }

        /// <inheritdoc />
        public virtual void Rename(string newName)
        {
            if (!this.IsRenamable())
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
    }
}
