// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine.Bindings;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Contains the overridable column properties for a <see cref="HierarchyViewColumn"/>.
    /// </summary>
    [Serializable]
    public sealed class HierarchyViewColumnState
    {
        /// <summary>
        /// The column identifier.
        /// </summary>
        public string ColumnId;

        /// <summary>
        /// Whether the column is visible.
        /// </summary>
        public bool Visible;

        /// <summary>
        /// The width of the column.
        /// </summary>
        public float Width;

        /// <summary>
        /// The position index of the column in the <see cref="HierarchyView"/>.
        /// </summary>
        public int Index = -1;

        public override string ToString()
        {
            return $"{ColumnId} Visible:{Visible} Index:{Index} Width:{Width}";
        }
    }

    /// <summary>
    /// Contains the persistable configuration for a <see cref="HierarchyView"/>.
    /// </summary>
    [Serializable]
    public sealed class HierarchyViewState
    {
        const int SerialVersion = 2;
        const UInt32 FileIdentifierToken = 0x68696572;
        const UInt32 EndOfFileToken = 0x72636879;

        [VisibleToOtherModules("UnityEditor.HierarchyModule")]
        internal static void BinarySerialization(BinaryWriter writer, HierarchyViewState value)
        {
            writer.Write(FileIdentifierToken);
            writer.Write(0x00000000);
            writer.Write(SerialVersion);
            writer.Write(0x00000000); // payload size, patched later

            int payloadStartOffset = (int)writer.BaseStream.Position;

            writer.Write((int)value.ValidContent);
            writer.Write(value.ViewModelState.Length);
            writer.Write(value.ViewModelState);
            writer.Write(value.SearchText ?? string.Empty);
            HierarchyViewColumnState[] columns = value.Columns;
            writer.Write(columns.Length);
            foreach (var col in columns)
            {
                writer.Write(col.ColumnId ?? string.Empty);
                writer.Write(col.Visible);
                writer.Write(col.Width);
                writer.Write(col.Index);
            }
            writer.Write(value.ScrollPositionX);
            writer.Write(value.ScrollPositionY);

            int payloadEndOffset = (int)writer.BaseStream.Position;
            int payloadSize = payloadEndOffset - payloadStartOffset;

            writer.Write(EndOfFileToken);

            writer.Seek(payloadStartOffset - 4, SeekOrigin.Begin);
            writer.Write(payloadSize);
        }

        [VisibleToOtherModules("UnityEditor.HierarchyModule")]
        internal static HierarchyViewState BinaryDeserialization(BinaryReader reader)
        {
            HierarchyViewState state = new HierarchyViewState();

            if (reader.ReadUInt32() != FileIdentifierToken ||
                reader.ReadInt32() != 0x00000000)
                return default;
            int fileSerialVersion = reader.ReadInt32();
            if (fileSerialVersion != SerialVersion)
                return default;
            int expectedPayloadSize = reader.ReadInt32();

            int payloadStartOffset = (int)reader.BaseStream.Position;

            state.ValidContent = (Content)reader.ReadInt32();
            state.ViewModelState = reader.ReadBytes(reader.ReadInt32());
            state.SearchText = reader.ReadString();
            int numColumns = reader.ReadInt32();
            for (int i = 0; i < numColumns; ++i)
            {
                HierarchyViewColumnState col = new HierarchyViewColumnState();
                col.ColumnId = reader.ReadString();
                col.Visible = reader.ReadBoolean();
                col.Width = reader.ReadSingle();
                col.Index = reader.ReadInt32();
            }
            state.ScrollPositionX = reader.ReadDouble();
            state.ScrollPositionY = reader.ReadDouble();

            int payloadEndOffset = (int)reader.BaseStream.Position;
            int actualPayloadSize = payloadEndOffset - payloadStartOffset;

            if (actualPayloadSize != expectedPayloadSize)
                return default;
            if (reader.ReadUInt32() != EndOfFileToken)
                return default;

            return state;
        }

        /// <summary>
        /// Specifies which content is valid in a <see cref="HierarchyViewState"/>.
        /// </summary>
        [Flags]
        public enum Content
        {
            /// <summary>
            /// The initialized content.
            /// </summary>
            Invalid = 0,

            /// <summary>
            /// Persists the selected identifiers.
            /// </summary>
            ViewModelState = 1 << 1,

            /// <summary>
            /// Persists the search text.
            /// </summary>
            SearchText = 1 << 2,

            /// <summary>
            /// Persists the column configuration.
            /// </summary>
            Columns = 1 << 3,

            /// <summary>
            /// Persists the scroll position.
            /// </summary>
            ScrollPosition = 1 << 4,

            /// <summary>
            /// Persists all content.
            /// </summary>
            All = ViewModelState | SearchText | Columns | ScrollPosition,

            /// <summary>
            /// Content to persist when saving layout.
            /// </summary>
            Layout = Content.Columns,

            /// <summary>
            /// Content to persist when saving hierarchy preferences.
            /// </summary>
            Settings = Content.Columns,

            /// <summary>
            /// Content to persist before a domain reload and restore after a domain reload.
            /// </summary>
            DomainReload = Content.Columns | Content.ViewModelState | Content.SearchText | Content.ScrollPosition,

            /// <summary>
            /// Content to persist when entering play mode.
            /// </summary>
            EnterPlayMode = Content.ViewModelState | Content.SearchText | Content.ScrollPosition,

            /// <summary>
            /// Content to persist when exiting play mode.
            /// </summary>
            ExitPlayMode = Content.ViewModelState | Content.SearchText | Content.ScrollPosition,

            /// <summary>
            /// Content to persist when changing stage. For example, when entering a Prefab.
            /// </summary>
            Stage = Content.ViewModelState | Content.ScrollPosition
        }

        /// <summary>
        /// Creates a new <see cref="HierarchyViewState"/>.
        /// </summary>
        public HierarchyViewState()
        {
            ValidContent = Content.Invalid;
        }

        /// <summary>
        /// Creates a new <see cref="HierarchyViewState"/>.
        /// </summary>
        /// <param name="content">The <see cref="Content"/> flags that specify which content to persist.</param>
        public HierarchyViewState(Content content)
        {
            ValidContent = content;
        }

        /// <summary>
        /// The valid <see cref="Content"/> flags in this <see cref="HierarchyViewState"/>.
        /// </summary>
        public Content ValidContent;

        /// <summary>
        /// The view model state encoded as a compressed byte stream.
        /// </summary>
        public byte[] ViewModelState;

        /// <summary>
        /// The search text in the <see cref="HierarchyView"/> search field.
        /// </summary>
        public string SearchText;

        /// <summary>
        /// The column layout and visibility preferences.
        /// </summary>
        public HierarchyViewColumnState[] Columns = Array.Empty<HierarchyViewColumnState>();

        /// <summary>
        /// The horizontal scroll position of the <see cref="HierarchyView"/>.
        /// </summary>
        public double ScrollPositionX = -1;

        /// <summary>
        /// The vertical scroll position of the <see cref="HierarchyView"/>.
        /// </summary>
        public double ScrollPositionY = -1;

        /// <summary>
        /// Converts this <see cref="HierarchyViewState"/> to a string.
        /// </summary>
        /// <returns>A string representation of this <see cref="HierarchyViewState"/>.</returns>
        public override string ToString()
        {
            var str = $"Content: {ValidContent}";
            if ((ValidContent & Content.SearchText) != 0)
            {
                str += $"Text:{SearchText} ";
            }

            if ((ValidContent & Content.ViewModelState) != 0)
            {
                str += $"{ViewModelState.Length}";
            }

            if ((ValidContent & Content.Columns) != 0)
            {
                str += $"ColsCount:{Columns.Length} ";
            }

            if ((ValidContent & Content.ScrollPosition) != 0)
            {
                str += $"Scroll:({ScrollPositionX},{ScrollPositionY})";
            }

            return str;
        }
    }
}
