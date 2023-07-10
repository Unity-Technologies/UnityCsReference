// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    [Flags]
    internal enum LiveReloadTrackers
    {
        Document = 1 << 0,
        Text = 1 << 1,
    }

    internal interface ILiveReloadSystem
    {
        bool enable { get; set; }

        LiveReloadTrackers enabledTrackers { get; set; }

        void Update();

        void RegisterVisualTreeAssetTracker(ILiveReloadAssetTracker<VisualTreeAsset> tracker, VisualElement owner);
        void UnregisterVisualTreeAssetTracker(VisualElement owner);

        void StartTracking(List<VisualElement> elements);
        void StopTracking(List<VisualElement> elements);

        void StartStyleSheetAssetTracking(StyleSheet styleSheet);
        void StopStyleSheetAssetTracking(StyleSheet styleSheet);
        void OnStyleSheetAssetsImported(HashSet<StyleSheet> changedAssets, HashSet<string> deletedAssets);

        void OnVisualTreeAssetsImported(HashSet<VisualTreeAsset> changedAssets, HashSet<string> deletedAssets);

        void RegisterTextElement(TextElement element);
        void UnregisterTextElement(TextElement element);
    }
}
