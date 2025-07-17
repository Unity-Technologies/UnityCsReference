// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine.Bindings;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Class containing all the overridable columns properties.
    /// </summary>
    [Serializable]
    internal sealed class HierarchyViewColumnState
    {
        /// <summary>
        /// ColumnId of the column.
        /// </summary>
        public string ColumnId;

        /// <summary>
        /// Is the column visible
        /// </summary>
        public bool Visible;

        /// <summary>
        /// Width of the column
        /// </summary>
        public float Width;

        /// <summary>
        /// Which index was the column positioned in the HierarchyView.
        /// </summary>
        public int Index = -1;

        public override string ToString()
        {
            return $"{ColumnId} Visible:{Visible} Index:{Index} Width:{Width}";
        }
    }

    /// <summary>
    /// Class containing all persistable configuration for a HierarchyView.
    /// </summary>
    [Serializable]
    internal sealed class HierarchyViewState
    {
        const int SerialVersion = 1;
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
            state.ScrollPositionX = reader.ReadSingle();
            state.ScrollPositionY = reader.ReadSingle();

            int payloadEndOffset = (int)reader.BaseStream.Position;
            int actualPayloadSize = payloadEndOffset - payloadStartOffset;

            if (actualPayloadSize != expectedPayloadSize)
                return default;
            if (reader.ReadUInt32() != EndOfFileToken)
                return default;

            return state;
        }

        /// <summary>
        /// Valid Content in the HierarchyViewState
        /// </summary>
        [Flags]
        public enum Content
        {
            /// <summary>
            /// Initialized content
            /// </summary>
            Invalid = 0,

            /// <summary>
            /// Persist SelectedIds
            /// </summary>
            ViewModelState = 1 << 1,

            /// <summary>
            /// Persist search text
            /// </summary>
            SearchText = 1 << 2,

            /// <summary>
            /// Persist Column configuration
            /// </summary>
            Columns = 1 << 3,

            /// <summary>
            /// Persist Scroll Position
            /// </summary>
            ScrollPosition = 1 << 4,

            /// <summary>
            /// Persist all content
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
            /// Content to persist when before a domain reload and to restore after domain reload.
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
            /// Content to persist when changing stage (ex: digging into Prefab).
            /// </summary>
            Stage = Content.ViewModelState | Content.ScrollPosition
        }

        /// <summary>
        /// Create a new HierarchyViewState
        /// </summary>
        public HierarchyViewState()
        {
            ValidContent = Content.Invalid;
        }

        /// <summary>
        /// Create a new HierarchyViewState
        /// </summary>
        /// <param name="content">Content we want to actually persist in the HierarchyViewState.</param>
        public HierarchyViewState(Content content)
        {
            ValidContent = content;
        }

        /// <summary>
        /// Valid Content in the HierarchyViewState.
        /// </summary>
        public Content ValidContent;

        /// <summary>
        /// HierarchyViewModelState encoded as a compressed stream.
        /// </summary>
        public byte[] ViewModelState;

        /// <summary>
        /// Current search text in the HierarchyView searchfield.
        /// </summary>
        public string SearchText;

        /// <summary>
        /// Current column preferences
        /// </summary>
        public HierarchyViewColumnState[] Columns = Array.Empty<HierarchyViewColumnState>();

        /// <summary>
        /// Current View scroll position.
        /// </summary>
        public float ScrollPositionX = -1;

        /// <summary>
        /// Current View scroll position.
        /// </summary>
        public float ScrollPositionY = -1;

        /// <summary>
        /// Convert a HierarchyViewState to a string.
        /// </summary>
        /// <returns>return the string representation of a HierarchyViewState</returns>
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
