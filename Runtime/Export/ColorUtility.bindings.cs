// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Export/ColorUtility.bindings.h")]
    public partial class ColorUtility
    {
        [FreeFunction]
        extern internal static bool DoTryParseHtmlColor(string htmlString, out Color32 color);
    }
}
