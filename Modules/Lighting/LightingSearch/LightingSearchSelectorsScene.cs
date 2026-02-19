// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Lighting.LightingSearch
{
    static class LightingSearchSelectorsScene
    {
        [SearchSelector(LightingSearchSelectors.k_SceneLightingSettingsPath, provider: LightingSearchSelectors.k_AssetProvider, priority: 99)]
        static object SceneLightingSettingsSearchSelector(SearchSelectorArgs args)
        {
            var sceneAsset = args.current.ToObject<SceneAsset>();
            if (sceneAsset == null)
                return null;

            return LightingSearchDataAccessors.GetSceneLightingSettingsFromAsset(sceneAsset);
        }

        [SearchColumnProvider(LightingSearchSelectors.k_SceneLightingSettingsPath)]
        internal static void SceneLightingSettingsSearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var sceneAsset = args.item.data as SceneAsset ?? args.item.ToObject<SceneAsset>();
                if (sceneAsset == null)
                    return null;

                return LightingSearchDataAccessors.GetSceneLightingSettingsFromAsset(sceneAsset);
            };
            column.setter = args =>
            {
                if (args.value is not LightingSettings lightingSettings)
                    return;

                var sceneAsset = args.item.data as SceneAsset ?? args.item.ToObject<SceneAsset>();
                if (sceneAsset == null)
                    return;

                LightingSearchDataAccessors.SetSceneLightingSettingsFromAsset(sceneAsset, lightingSettings);
            };
            column.cellCreator = _ => new UnityEditor.UIElements.ObjectField { objectType = typeof(LightingSettings), style = { flexGrow = 1 } };
            column.binder = (args, ve) =>
            {
                var objectField = (UnityEditor.UIElements.ObjectField)ve;
                var lightingSettings = args.value as LightingSettings;

                ve.visible = true;
                objectField.SetValueWithoutNotify(lightingSettings);
            };
        }

        [SearchColumnProvider(LightingSearchSelectors.k_SceneLightingGeneratedPath)]
        internal static void SceneLightingGeneratedSearchColumnProvider(SearchColumn column)
        {
            column.getter = args =>
            {
                var go = args.item.data as GameObject ?? args.item.ToObject<GameObject>();
                if (go == null)
                    return null;

                return LightingSearchDataAccessors.IsLightingGenerated(go);
            };
            column.cellCreator = _ => new Label { style = { flexGrow = 1 } };
            column.binder = (args, ve) =>
            {
                var label = (Label)ve;
                if (args.value != null && args.value is bool isGenerated)
                {
                    label.visible = true;
                    label.text = isGenerated ? "Yes" : "No";
                }
                else
                {
                    label.visible = false;
                }
            };
        }
    }
}
