// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [Flags]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal enum LiveReloadTrackers
    {
        None = 0,
        Document = 1 << 0,
        Text = 1 << 1,
        StyleSheet = 1 << 2,
        All = Document | Text | StyleSheet
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal interface ILiveReloadSystem
    {
        bool enable { get; set; }

        LiveReloadTrackers enabledTrackers { get; set; }

        void Update();

        void RegisterVisualTreeAssetTracker(ILiveReloadAssetTracker<VisualTreeAsset> tracker, VisualElement owner);
        void UnregisterVisualTreeAssetTracker(VisualElement owner);

        void RegisterAuthoringTrackerForAsset(IAuthoringLiveReloadAssetTracker<VisualTreeAsset> tracker, VisualTreeAsset asset);
        void UnregisterAuthoringTrackerForAsset(IAuthoringLiveReloadAssetTracker<VisualTreeAsset> tracker, VisualTreeAsset asset);

        void RegisterAuthoringTrackerForAsset(IAuthoringLiveReloadAssetTracker<StyleSheet> tracker, StyleSheet asset);
        void UnregisterAuthoringTrackerForAsset(IAuthoringLiveReloadAssetTracker<StyleSheet> tracker, StyleSheet asset);

        void StartTracking(List<VisualElement> elements);
        void StopTracking(List<VisualElement> elements);

        void OnVisualTreeAssetChanged(VisualTreeAsset visualTreeAsset);

        void StartStyleSheetAssetTracking(StyleSheet styleSheet);
        void StopStyleSheetAssetTracking(StyleSheet styleSheet);
        void OnStyleSheetChanged(List<StyleSheet> styleSheets);
        void OnStyleSheetChanged(List<string> styleSheetPaths);
        void OnStyleSheetAssetsImported(HashSet<StyleSheet> changedAssets, HashSet<string> deletedAssets);

        void OnVisualTreeAssetsImported(HashSet<VisualTreeAsset> changedAssets, HashSet<string> deletedAssets);

        bool AnyStyleSheetMarkedDirtyAfterUndo();
    }
}
