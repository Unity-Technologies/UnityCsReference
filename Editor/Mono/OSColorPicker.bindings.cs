// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Platform/Interface/ColorPicker.h")]
    internal class OSColorPicker
    {
        [FreeFunction("OSColorPickerShow")]
        static public extern void Show(bool showAlpha);

        [FreeFunction("OSColorPickerClose")]
        static public extern void Close();

        static public extern bool visible
        {
            [FreeFunction("OSColorPickerIsVisible")]
            get;
        }

        static public extern Color color
        {
            [FreeFunction("OSColorPickerGetColor")]
            get;

            [FreeFunction("OSColorPickerSetColor")]
            set;
        }
    }
}
