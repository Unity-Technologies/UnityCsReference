// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // These classes are only used by native code, and only here to prevent them from being stripped.
    internal class LowerResBlitTexture : Object
    {
        [RequiredByNativeCode]
        internal void LowerResBlitTextureDontStripMe() {}
    }

    internal class PreloadData : Object
    {
        [RequiredByNativeCode]
        internal void PreloadDataDontStripMe() {}
    }

    // The LightingSettings native class needs to be preserved in player builds, even if no instance of the class is
    // present in the game data, as an instance needs to be created in code in that case. But the managed API representation
    // of that class only exists in UnityEditor code, so we add a dummy runtime version here to preserve the type
    internal class LightingSettings : Object
    {
        [RequiredByNativeCode]
        internal void LightingSettingsDontStripMe() {}
    }
}
