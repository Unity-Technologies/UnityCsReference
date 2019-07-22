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
            if (assetImporter != null && assetImporter.importSettingsMissing)
            {
                foreach (var preset in Preset.GetDefaultPresetsForObject(assetImporter))
                {
                    preset.ApplyTo(assetImporter);
                }
            }
        }
    }
}
