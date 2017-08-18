// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Runtime/BaseClasses/GameObject.h")]
    [NativeType(Header = "Modules/TilemapEditor/Editor/TilemapEditorUserSettings.h")]
    internal sealed partial class TilemapEditorUserSettings
    {
        public enum FocusMode
        {
            None = 0,
            Tilemap = 1,
            Grid = 2
        }

        [NativeProperty(Name = "LastUsedPaletteFromInstance")]
        public extern static GameObject lastUsedPalette
        {
            get;
            set;
        }

        [NativeProperty(Name = "FocusModeFromInstance")]
        public extern static TilemapEditorUserSettings.FocusMode focusMode
        {
            get;
            set;
        }
    }
}
