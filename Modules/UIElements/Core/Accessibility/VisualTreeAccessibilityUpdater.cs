// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Profiling;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// The dedicated update phase that keeps the generated accessibility hierarchy in sync with the
    /// visual tree while <see cref="UITKAccessibilityBridge"/> has a live hierarchy.
    /// </summary>
    /// <remarks>
    /// The phase runs after layout and transforms are settled and before repaint, so node frames and
    /// structural snapshots read final geometry. It only carries the version-bit-driven part of the
    /// sync — structure (<see cref="VersionChangeType.Hierarchy"/>), name-derived labels
    /// (<see cref="VersionChangeType.Name"/>), frame refreshes (layout/transform/size) and inclusion
    /// rechecks (style/picking); leaf data (value, caption, text) rides per-element event hooks and
    /// disabled/displayed flips their single engine code paths, applied by the bridge as they happen.
    /// Everything funnels into the bridge, which owns the hierarchy and the element-node mapping;
    /// the updater is per-panel plumbing that the bridge installs when it starts tracking a panel and
    /// removes when tracking stops (see <see cref="UITKAccessibilityBridge"/>'s RefreshTrackedPanels)
    ///  — a panel the bridge does not track carries no updater at all. The live/tracked checks below
    /// remain as the correctness guard: an installed updater still ticks before the first hierarchy
    /// exists, and must stay inert until then.
    /// </remarks>
    internal class VisualTreeAccessibilityUpdater : BaseVisualTreeUpdater
    {
        static readonly string s_Description = "UIElements.UpdateAccessibility";
        static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker(ProfilerCategory.UIToolkit, s_Description);
        public override ProfilerMarker profilerMarker => s_ProfilerMarker;

        const VersionChangeType k_StructureChanged = VersionChangeType.Hierarchy;
        const VersionChangeType k_NameChanged = VersionChangeType.Name;

        // Style and picking changes can flip an element's inclusion in the hierarchy (visibility;
        // pickingMode decides whether unknown elements are kept); the flush re-checks inclusion
        // cheaply and only regenerates on an actual flip.
        const VersionChangeType k_InclusionMayHaveChanged =
            VersionChangeType.StyleSheet | VersionChangeType.Picking;

        const VersionChangeType k_GeometryChanged =
            VersionChangeType.Layout | VersionChangeType.Transform | VersionChangeType.Size;

        const VersionChangeType k_AnythingChanged =
            k_StructureChanged | k_NameChanged | k_InclusionMayHaveChanged | k_GeometryChanged;

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if (!UITKAccessibilityBridge.isLive || (versionChangeType & k_AnythingChanged) == 0)
                return;

            if (!UITKAccessibilityBridge.IsTrackedPanel(panel))
                return;

            if ((versionChangeType & k_StructureChanged) != 0)
                UITKAccessibilityBridge.OnElementStructureChanged(ve);

            if ((versionChangeType & k_NameChanged) != 0)
                UITKAccessibilityBridge.OnElementLabelSourceChanged(ve);

            if ((versionChangeType & k_InclusionMayHaveChanged) != 0)
                UITKAccessibilityBridge.OnElementInclusionMayHaveChanged(ve);

            if ((versionChangeType & k_GeometryChanged) != 0)
            {
                UITKAccessibilityBridge.OnElementGeometryChanged(ve);

                // Text is a label source, and a text change re-measures (Layout). A mapped text
                // element relabels through its property hook, but an unmapped one whose text just
                // became derivable has no hook yet — the inclusion recheck (a flush-time no-op
                // for elements whose representation didn't flip) covers it.
                if (ve is TextElement)
                    UITKAccessibilityBridge.OnElementInclusionMayHaveChanged(ve);
            }
        }

        public override void Update()
        {
            if (!UITKAccessibilityBridge.isLive || !UITKAccessibilityBridge.IsTrackedPanel(panel))
                return;

            UITKAccessibilityBridge.FlushPanelUpdates(panel);
        }
    }
}
