// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Shaders
{
    [Serializable]
    internal class ShaderKeywordDeclarationOverridesClipboard
    {
        public ShaderBuildSettings.KeywordDeclarationOverride[] overrides;

        internal static bool CanPaste()
        {
            return Clipboard.HasCustomValue<ShaderKeywordDeclarationOverridesClipboard>();
        }
    }

    [Serializable]
    internal class ShaderConstantDefinesClipboard
    {
        public string[] defines;

        internal static bool CanPaste()
        {
            return Clipboard.HasCustomValue<ShaderConstantDefinesClipboard>();
        }
    }

    [Serializable]
    internal class ShaderCompilerBackendClipboard
    {
        public ShaderBuildSettings.ShaderCompilerSettings[] compilerSettings;

        internal static bool CanPaste()
        {
            return Clipboard.HasCustomValue<ShaderCompilerBackendClipboard>();
        }
    }
}
