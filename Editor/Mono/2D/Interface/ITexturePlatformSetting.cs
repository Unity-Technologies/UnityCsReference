// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.U2D.Interface
{
    internal interface ITexturePlatformSettingsView
    {
        string buildPlatformTitle { get; set; }
        TextureImporterCompression DrawCompression(TextureImporterCompression defaultValue, bool isMixedValue, out bool changed);
        bool DrawUseCrunchedCompression(bool defaultValue, bool isMixedValue, out bool changed);
        bool DrawAlphaSplit(bool defaultValue, bool isMixedValue, bool isDisabled, out bool changed);
        bool DrawOverride(bool defaultValue, bool isMixedValue, out bool changed);
        int DrawMaxSize(int defaultValue, bool isMixedValue, out bool changed);
        TextureImporterFormat DrawFormat(TextureImporterFormat defaultValue, int[] displayValues, string[] displayStrings, bool isMixedValue, bool isDisabled, out bool changed);
        int DrawCompressionQualityPopup(int defaultValue, bool isMixedValue, out bool changed);
        int DrawCompressionQualitySlider(int defaultValue, bool isMixedValue, out bool changed);
    }

    internal interface ITexturePlatformSettingsFormatHelper
    {
        void AcquireTextureFormatValuesAndStrings(BuildTarget buildTarget, out int[] displayValues, out string[] displayStrings);

        bool TextureFormatRequireCompressionQualityInput(TextureImporterFormat format);

        bool IsETC1SupportedByBuildTarget(BuildTarget buildTarget);

        bool IsTextureFormatETC1Compression(TextureFormat format);
    }

    internal interface ITexturePlatformSettingsController
    {
        bool HandleDefaultSettings(List<TextureImporterPlatformSettings> platformSettings, ITexturePlatformSettingsView view);
        bool HandlePlatformSettings(BuildTarget buildTarget, List<TextureImporterPlatformSettings> platformSettings, ITexturePlatformSettingsView view, ITexturePlatformSettingsFormatHelper formatHelper);
    }
}
