// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements.UIR
{
    static class Shaders
    {
        public static readonly string k_AtlasBlit;
        public static readonly string k_Editor;
        public static readonly string k_Runtime;
        public static readonly string k_RuntimeWorld;
        public static readonly string k_ColorConversionBlit;

        static Shaders()
        {
            k_AtlasBlit = "Hidden/Internal-UIRAtlasBlitCopy";
            k_Editor = "Hidden/UIElements/EditorUIE";
            k_Runtime = "Hidden/Internal-UIRDefault";
            k_RuntimeWorld = "Hidden/Internal-UIRDefaultWorld";
            k_ColorConversionBlit = "Hidden/Internal-UIE-ColorConversionBlit";
        }
    }
}
