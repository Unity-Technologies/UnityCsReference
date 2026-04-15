// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement(visibility = LibraryVisibility.Hidden)]
sealed partial class UssCodePreview : CodePreview<StyleSheet>
{
    [Serializable]
    public new class UxmlSerializedData : CodePreview<StyleSheet>.UxmlSerializedData
    {
        [Conditional("UNITY_EDITOR")]
        public new static void Register()
        {
            CodePreview<StyleSheet>.UxmlSerializedData.Register();
            UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), [], true);
        }

        public override object CreateInstance() => new UssCodePreview();
    }

    class Tracker(UssCodePreview owner) : LiveReloadStyleSheetAssetTracker, IAuthoringLiveReloadAssetTracker<StyleSheet>
    {
        public override void OnTrackedAssetChanged() => owner.Refresh();
    }

    protected override string GetTitle() => "USS Preview";
    protected override string GetExtension() => ".uss";

    protected override IAuthoringLiveReloadAssetTracker<StyleSheet> CreateTracker() => new Tracker(this);
    protected override void RegisterTracker(ILiveReloadSystem liveReloadSystem, IAuthoringLiveReloadAssetTracker<StyleSheet> tracker, StyleSheet asset)
        => liveReloadSystem.RegisterAuthoringTrackerForAsset(tracker, asset);

    protected override void UnregisterTracker(ILiveReloadSystem liveReloadSystem, IAuthoringLiveReloadAssetTracker<StyleSheet> tracker, StyleSheet asset)
        => liveReloadSystem.UnregisterAuthoringTrackerForAsset(tracker, asset);

    protected override string GenerateCodePreview()
        => Asset ? StyleSheetExporter.Default.ToUssString(Asset) : string.Empty;
}
