// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Presets
{
    class PresetManagerPostProcessor : AssetPostprocessor
    {
        public override int GetPostprocessOrder()
        {
            return -1000;
        }

        public void OnPreprocessAsset()
        {
            if (assetImporter.importSettingsMissing)
            {
                var preset = Preset.GetDefaultForObject(assetImporter);
                if (preset != null)
                {
                    preset.ApplyTo(assetImporter);
                }
            }
        }
    }
}
