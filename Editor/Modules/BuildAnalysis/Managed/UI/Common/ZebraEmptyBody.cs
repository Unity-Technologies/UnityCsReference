// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    // Drop-in overlay that continues a vertical collection view's alternating-row "zebra" pattern
    // past the last visible row, filling any leftover body space. Reads body geometry and row
    // height live from the list so it works equally for fixed-height tables and ones hosted
    // inside a TwoPaneSplitView (where the body height is user-driven). Works with any
    // BaseVerticalCollectionView (MultiColumnListView, ListView, ...).
    //
    //   m_EmptyBody = new ZebraEmptyBody(m_ListView);
    //   hostContainer.Add(m_EmptyBody);   // any ancestor that visually overlaps the list body
    //   // ...then after every m_ListView.RefreshItems():
    //   m_EmptyBody.Refresh();
    internal sealed class ZebraEmptyBody : VisualElement
    {
        public static readonly string ussClassName = "zebra-empty-body";
        public static readonly string stripeUssClassName = ussClassName + "__stripe";
        public static readonly string stripeAltUssClassName = stripeUssClassName + "--alt";

        // Public USS class name is part of MCLV's stable surface; the C# header type is internal.
        // A headerless list (e.g. a plain ListView) simply has no element matching this class.
        private const string k_HeaderClassName = "unity-multi-column-header";
        private const string k_UssPath = "BuildAnalysis/StyleSheets/ZebraEmptyBody.uss";

        private readonly BaseVerticalCollectionView m_ListView;

        internal ZebraEmptyBody(BaseVerticalCollectionView listView)
        {
            m_ListView = listView;

            var styleSheet = EditorGUIUtility.LoadRequired(k_UssPath) as StyleSheet;
            styleSheets.Add(styleSheet);
            AddToClassList(ussClassName);

            pickingMode = PickingMode.Ignore;
            style.display = DisplayStyle.None;

            // Splitter drags / window resizes change the body without changing the row count;
            // catch those here so callers don't need to wire layout listeners themselves.
            m_ListView.RegisterCallback<GeometryChangedEvent>(_ => Refresh());
        }

        internal void Refresh()
        {
            if (parent == null)
                return;

            var rowHeight = m_ListView.fixedItemHeight;
            if (rowHeight <= 0f)
                return;

            var bodyRect = ComputeBodyRectInOwnParent();
            if (float.IsNaN(bodyRect.height) || bodyRect.height <= 0f)
                return; // pre-layout — the GeometryChangedEvent will re-call us

            var rowCount = m_ListView.itemsSource?.Count ?? 0;
            var rowsPx = rowCount * rowHeight;
            var freePx = bodyRect.height - rowsPx;
            if (freePx <= 0f)
            {
                style.display = DisplayStyle.None;
                return;
            }

            style.display = DisplayStyle.Flex;
            style.left = bodyRect.x;
            style.width = bodyRect.width;
            style.top = bodyRect.y + rowsPx;
            style.height = freePx;

            var stripeCount = Mathf.CeilToInt(freePx / rowHeight);
            SyncStripeChildren(stripeCount, rowCount, rowHeight);
        }

        // Returns the MCLV's body region (where rows actually render) in this overlay's parent's
        // coordinate space. Body top = the header's bottom-margin edge — header.worldBound.yMax
        // already encodes its marginTop offset, so we add only marginBottom. Reading the margin
        // explicitly matters because header.layout.height is the box height, not the full layout
        // slot — without it the overlay overlaps the bottom of the header area and the first row.
        private Rect ComputeBodyRectInOwnParent()
        {
            var listWorld = m_ListView.worldBound;
            var resolved = m_ListView.resolvedStyle;

            var bodyLeftWorld = listWorld.x + resolved.borderLeftWidth;
            var bodyRightWorld = listWorld.xMax - resolved.borderRightWidth;
            var bodyBottomWorld = listWorld.yMax - resolved.borderBottomWidth;

            var bodyTopWorld = listWorld.y + resolved.borderTopWidth;
            var header = m_ListView.Q<VisualElement>(className: k_HeaderClassName);
            if (header != null)
                bodyTopWorld = header.worldBound.yMax + header.resolvedStyle.marginBottom;

            // CSS position:absolute is relative to the parent's padding-box (inside its borders),
            // but worldBound is the border-box (outside). Add the parent's border widths so the
            // offset lands on the padding-box origin — otherwise a host with borders (e.g.
            // MessagesConsole's content-area) shifts the overlay onto its own right/bottom border.
            var parentWorld = parent.worldBound;
            var parentResolved = parent.resolvedStyle;
            return new Rect(
                bodyLeftWorld - (parentWorld.x + parentResolved.borderLeftWidth),
                bodyTopWorld - (parentWorld.y + parentResolved.borderTopWidth),
                bodyRightWorld - bodyLeftWorld,
                bodyBottomWorld - bodyTopWorld);
        }

        private void SyncStripeChildren(int count, int rowOffset, float rowHeightPx)
        {
            while (childCount < count)
            {
                var stripe = new VisualElement();
                stripe.AddToClassList(stripeUssClassName);
                Add(stripe);
            }
            while (childCount > count)
                RemoveAt(childCount - 1);

            for (int i = 0; i < count; i++)
            {
                var stripe = this[i];
                stripe.style.height = rowHeightPx;
                var alt = ((rowOffset + i) & 1) == 1;
                stripe.EnableInClassList(stripeAltUssClassName, alt);
            }
        }
    }
}
