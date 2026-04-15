// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
sealed partial class UxmlCodePreview : CodePreview<VisualTreeAsset>
{
    [Serializable]
    public new class UxmlSerializedData : CodePreview<VisualTreeAsset>.UxmlSerializedData
    {
        [Conditional("UNITY_EDITOR")]
        public new static void Register()
        {
            CodePreview<VisualTreeAsset>.UxmlSerializedData.Register();
            UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), [], true);
        }

        public override object CreateInstance() => new UxmlCodePreview();
    }

    class Tracker(UxmlCodePreview owner)
        : BaseLiveReloadVisualTreeAssetTracker, IAuthoringLiveReloadAssetTracker<VisualTreeAsset>
    {
        internal override void OnVisualTreeAssetChanged() => owner.Refresh();
    }

    protected override string GetTitle() => "UXML Preview";
    protected override string GetExtension() => ".uxml";

    protected override IAuthoringLiveReloadAssetTracker<VisualTreeAsset> CreateTracker() => new Tracker(this);
    protected override void RegisterTracker(ILiveReloadSystem liveReloadSystem, IAuthoringLiveReloadAssetTracker<VisualTreeAsset> tracker, VisualTreeAsset asset)
        => liveReloadSystem.RegisterAuthoringTrackerForAsset(tracker, asset);

    protected override void UnregisterTracker(ILiveReloadSystem liveReloadSystem, IAuthoringLiveReloadAssetTracker<VisualTreeAsset> tracker, VisualTreeAsset asset)
        => liveReloadSystem.UnregisterAuthoringTrackerForAsset(tracker, asset);

    protected override string GenerateCodePreview()
        => Asset ? VisualTreeAssetExporter.Default.ToUxmlString(Asset) : string.Empty;
}
