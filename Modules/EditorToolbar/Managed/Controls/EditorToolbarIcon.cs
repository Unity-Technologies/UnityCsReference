// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Toolbars
{
    struct EditorToolbarIcon : IEquatable<EditorToolbarIcon>
    {
        public string textIcon { get; }
        public Texture2D textureIcon { get; }

        public EditorToolbarIcon(string text) : this(text, null) { }

        public EditorToolbarIcon(Texture2D texture) : this(null, texture) {}

        internal EditorToolbarIcon(string text, Texture2D texture)
        {
            textIcon = text;
            textureIcon = texture;
        }

        public bool Equals(EditorToolbarIcon other)
        {
            return textIcon == other.textIcon && textureIcon == other.textureIcon;
        }
    }
}
