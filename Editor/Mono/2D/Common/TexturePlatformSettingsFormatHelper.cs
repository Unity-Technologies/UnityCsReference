// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.U2D.Interface;

namespace UnityEditor.U2D.Common
{
    internal class TexturePlatformSettingsFormatHelper : ITexturePlatformSettingsFormatHelper
    {
        public void AcquireTextureFormatValuesAndStrings(BuildTarget buildTarget, out int[] formatValues, out string[] formatStrings)
        {
            TextureImportValidFormats.GetPlatformTextureFormatValuesAndStrings(TextureImporterType.Sprite, buildTarget,
                out formatValues, out formatStrings);
        }

        public void AcquireDefaultTextureFormatValuesAndStrings(out int[] formatValues, out string[] formatStrings)
        {
            TextureImportValidFormats.GetDefaultTextureFormatValuesAndStrings(TextureImporterType.Sprite,
                out formatValues, out formatStrings);
        }

        public bool TextureFormatRequireCompressionQualityInput(TextureImporterFormat format)
        {
            return TextureImporterInspector.IsFormatRequireCompressionSetting(format);
        }
    }
}
