// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.TextCore.Text;

namespace UnityEngine
{
    /// <summary>
    /// Represents text rendering settings for IMGUI runtime
    /// </summary>
    internal class RuntimeTextSettings : TextSettings
    {
        private static RuntimeTextSettings s_DefaultTextSettings;

        internal static RuntimeTextSettings defaultTextSettings
        {
            get
            {
                if (s_DefaultTextSettings == null)
                {
                    s_DefaultTextSettings = ScriptableObject.CreateInstance<RuntimeTextSettings>();
                }

                return s_DefaultTextSettings;
            }
        }

        private static List<FontAsset> s_FallbackOSFontAssetIMGUIInternal;

        internal override Shader GetFontShader()
        {
            return TextShaderUtilities.ShaderRef_MobileSDF_IMGUI;
        }

        internal override List<FontAsset> GetStaticFallbackOSFontAsset()
        {
            return s_FallbackOSFontAssetIMGUIInternal;
        }

        internal override void SetStaticFallbackOSFontAsset(List<FontAsset> fontAssets)
        {
            s_FallbackOSFontAssetIMGUIInternal = fontAssets;
        }
    }
}
