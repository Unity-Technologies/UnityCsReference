// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    internal enum ReferenceTab
    {
        ReferencedBy,
        ReferencesTo,
    }

    /// <summary>
    /// The dependency half of the Asset-mode inspector: a two-tab bar under the metadata block,
    /// with a flat list per direction. Owns the loading and unavailable states, so the tab bodies
    /// and the tab-label parentheticals always agree:
    ///   (N)  a measured direct-edge count
    ///   (-)  the graph is still loading
    ///   none the count is unknowable (a terminal unavailable state, or referrers not recorded
    ///        for this asset) — "(-)" would promise a number that is never coming
    ///
    /// Purely presentational: callers push exactly one of <see cref="SetLoading"/>,
    /// <see cref="SetUnavailable"/> or <see cref="Bind"/>; nothing here is async.
    /// </summary>
    internal sealed class ReferenceSection : VisualElement
    {
        private const string k_ReferencedByLabel = "Referenced By";
        private const string k_ReferencesToLabel = "References To";
        private const string k_PendingCount = "(-)";
        private const string k_LoadingMessage = "Loading References...";

        private const string k_UnreadableMessage =
            "Dependency data couldn't be read for this build.";
        private const string k_NoContentLayoutMessage =
            "This build didn't record dependency data.";
        private const string k_UnsupportedVersionMessage =
            "Dependency data isn't available for builds made with an older Unity version.";

        // Scripts are sinks in build content (references are authored on the components and
        // ScriptableObjects that use them), so an empty References To is the correct answer.
        private const string k_ScriptNoReferencesMessage =
            "Scripts don't reference other assets.";

        // Keyed on IsAttributed, not on being a script: all scripts ship in one shared file, so
        // the build records referrers of that file, not of an individual script - and real
        // referrers appear by themselves if scripts ever get their own files.
        private const string k_ReferrersNotRecordedMessage =
            "Referenced By isn't available for scripts.";

        private enum State { None, Loading, Unavailable, Bound }

        private readonly ReferenceTabBar m_TabBar = new ReferenceTabBar();
        private readonly Label m_Message = new Label();
        private readonly FlatReferenceList m_ReferencedByList = new FlatReferenceList(withSize: false);
        private readonly FlatReferenceList m_ReferencesToList = new FlatReferenceList(withSize: true);
        private readonly LoadingOverlay m_LoadingOverlay = new LoadingOverlay();

        private State m_State;
        private DependencyGraphStatus m_UnavailableStatus;
        private bool m_ReferrersRecorded;
        private bool m_IsScript;

        public ReferenceSection()
        {
            AddToClassList("inspector__ref-section");

            Add(m_TabBar);

            var bodyHost = new VisualElement();
            bodyHost.AddToClassList("inspector__ref-body");
            m_Message.name = "ref-message";
            m_Message.AddToClassList("inspector__ref-message");
            bodyHost.Add(m_Message);
            m_ReferencedByList.name = "referenced-by-list";
            bodyHost.Add(m_ReferencedByList);
            m_ReferencesToList.name = "references-to-list";
            bodyHost.Add(m_ReferencesToList);
            m_LoadingOverlay.AddToClassList("loading-overlay--inline");
            bodyHost.Add(m_LoadingOverlay);
            Add(bodyHost);

            m_TabBar.ActiveTabChanged += _ => RefreshBody();

            // Inert until a caller pushes a state - no spinner schedule ticking behind an
            // inspector that never shows this section.
            m_TabBar.SetLabel(ReferenceTab.ReferencedBy, k_ReferencedByLabel);
            m_TabBar.SetLabel(ReferenceTab.ReferencesTo, k_ReferencesToLabel);
            RefreshBody();
        }

        internal ReferenceTab ActiveTab => m_TabBar.ActiveTab;

        /// <summary>Hidden entirely for builds that structurally never record dependency data (Player builds).</summary>
        public void SetVisible(bool visible)
        {
            style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            // The spinner schedule must not keep ticking behind a hidden section; RefreshBody
            // resumes it when a still-loading section comes back.
            if (visible)
                RefreshBody();
            else
                m_LoadingOverlay.Hide();
        }

        public void SetLoading()
        {
            m_State = State.Loading;
            m_TabBar.SetLabel(ReferenceTab.ReferencedBy, $"{k_ReferencedByLabel} {k_PendingCount}");
            m_TabBar.SetLabel(ReferenceTab.ReferencesTo, $"{k_ReferencesToLabel} {k_PendingCount}");
            RefreshBody();
        }

        public void SetUnavailable(DependencyGraphStatus status)
        {
            m_State = State.Unavailable;
            m_UnavailableStatus = status;
            m_TabBar.SetLabel(ReferenceTab.ReferencedBy, k_ReferencedByLabel);
            m_TabBar.SetLabel(ReferenceTab.ReferencesTo, k_ReferencesToLabel);
            RefreshBody();
        }

        /// <summary>Bind both directions for one asset. Rows are precomputed here so bindItem stays allocation-free.</summary>
        public void Bind(IAssetReferenceProvider references, int assetId, BuildAnalysisAsset[] assets)
        {
            if (references == null)
                throw new ArgumentNullException(nameof(references));
            if (assets == null)
                throw new ArgumentNullException(nameof(assets));

            m_State = State.Bound;

            var path = (uint)assetId < (uint)assets.Length ? assets[assetId].Path ?? string.Empty : string.Empty;
            m_IsScript = path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
            m_ReferrersRecorded = references.IsAttributed(assetId);

            m_ReferencesToList.SetRows(BuildReferencesToRows(references, assetId, assets));
            m_TabBar.SetLabel(ReferenceTab.ReferencesTo,
                $"{k_ReferencesToLabel} ({references.GetReferencesToCount(assetId)})");

            if (m_ReferrersRecorded)
            {
                m_ReferencedByList.SetRows(BuildReferencedByRows(references, assetId, assets));
                m_TabBar.SetLabel(ReferenceTab.ReferencedBy,
                    $"{k_ReferencedByLabel} ({references.GetReferencedByCount(assetId)})");
            }
            else
            {
                m_ReferencedByList.ClearRows();
                m_TabBar.SetLabel(ReferenceTab.ReferencedBy, k_ReferencedByLabel);
            }

            RefreshBody();
        }

        /// <summary>Back to the default tab. Called on a genuinely new build selection only.</summary>
        public void ResetActiveTab() => m_TabBar.SetActiveTab(ReferenceTab.ReferencedBy);

        private void RefreshBody()
        {
            var loading = m_State == State.Loading;
            if (loading)
                m_LoadingOverlay.Show(k_LoadingMessage);
            else
                m_LoadingOverlay.Hide();

            if (m_State == State.Unavailable)
            {
                ShowBody(list: null, message: UnavailableMessage(m_UnavailableStatus));
                return;
            }

            if (m_State != State.Bound)
            {
                ShowBody(list: null, message: null);
                return;
            }

            if (m_TabBar.ActiveTab == ReferenceTab.ReferencesTo)
            {
                // A script's empty list is the correct measurement; the one-liner explains why it
                // is always empty. The list stays visible so the zebra pattern fills the body.
                var explainEmpty = m_IsScript && m_ReferencesToList.Count == 0;
                ShowBody(m_ReferencesToList, explainEmpty ? k_ScriptNoReferencesMessage : null);
            }
            else if (m_ReferrersRecorded)
            {
                ShowBody(m_ReferencedByList, message: null);
            }
            else
            {
                // The message alone, no list: an asset with genuinely no referrers renders an
                // empty list (we looked, nothing points here), and an unrecorded one must not
                // render the same way.
                ShowBody(list: null, message: k_ReferrersNotRecordedMessage);
            }
        }

        private void ShowBody(FlatReferenceList list, string message)
        {
            m_ReferencedByList.style.display = list == m_ReferencedByList ? DisplayStyle.Flex : DisplayStyle.None;
            m_ReferencesToList.style.display = list == m_ReferencesToList ? DisplayStyle.Flex : DisplayStyle.None;
            m_Message.style.display = message != null ? DisplayStyle.Flex : DisplayStyle.None;
            m_Message.text = message ?? string.Empty;
            list?.RefreshZebra();
        }

        private static string UnavailableMessage(DependencyGraphStatus status)
        {
            switch (status)
            {
                case DependencyGraphStatus.NoContentLayout:
                    return k_NoContentLayoutMessage;
                case DependencyGraphStatus.UnsupportedLayoutVersion:
                    return k_UnsupportedVersionMessage;
                default:
                    return k_UnreadableMessage;
            }
        }

        private static List<ReferenceRowData> BuildReferencesToRows(
            IAssetReferenceProvider references, int assetId, BuildAnalysisAsset[] assets)
        {
            var targets = references.GetReferencesTo(assetId);
            var costs = references.GetReferenceSizes(assetId);
            var rows = new List<ReferenceRowData>(targets.Length);
            for (var i = 0; i < targets.Length; i++)
            {
                var id = targets[i];
                if ((uint)id >= (uint)assets.Length)
                    continue;
                var asset = assets[id];
                var path = asset.Path ?? string.Empty;

                // The size is the reference's cost to this asset, not the target's own size. When
                // the cost is a genuine subset (a referrer pulling one sub-object of a multi-object
                // asset), show both — the cost is the answer, the total explains the Assets-table number.
                var cost = (ulong)costs[i];
                var size = cost < asset.OutputSizeBytes
                    ? $"{FormatUtility.FormatSize(cost)} of {FormatUtility.FormatSize(asset.OutputSizeBytes)}"
                    : FormatUtility.FormatSize(cost);

                rows.Add(new ReferenceRowData
                {
                    Name = string.IsNullOrEmpty(path) ? string.Empty : Path.GetFileName(path),
                    Path = path,
                    Size = size,
                    SortSize = cost,
                    Icon = IconUtility.GetAssetIcon(path),
                });
            }

            // Largest cost first, same rationale as the root-asset ReferencesView; name then path
            // break ties so equal-cost rows keep a deterministic order (List.Sort is unstable).
            rows.Sort((x, y) =>
            {
                var bySize = y.SortSize.CompareTo(x.SortSize);
                if (bySize != 0)
                    return bySize;
                var byName = string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
                return byName != 0 ? byName : string.Compare(x.Path, y.Path, StringComparison.Ordinal);
            });
            return rows;
        }

        private static List<ReferenceRowData> BuildReferencedByRows(
            IAssetReferenceProvider references, int assetId, BuildAnalysisAsset[] assets)
        {
            var sources = references.GetReferencedBy(assetId);
            var rows = new List<ReferenceRowData>(sources.Length);
            for (var i = 0; i < sources.Length; i++)
            {
                var id = sources[i];
                if ((uint)id >= (uint)assets.Length)
                    continue;
                var path = assets[id].Path ?? string.Empty;
                rows.Add(new ReferenceRowData
                {
                    Name = string.IsNullOrEmpty(path) ? string.Empty : Path.GetFileName(path),
                    Path = path,
                    Icon = IconUtility.GetAssetIcon(path),
                });
            }

            rows.Sort((x, y) =>
            {
                var byName = string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
                return byName != 0 ? byName : string.Compare(x.Path, y.Path, StringComparison.Ordinal);
            });
            return rows;
        }
    }

    /// <summary>Two text tabs, active one underlined. Pure header state — it never sees the bodies.</summary>
    internal sealed class ReferenceTabBar : VisualElement
    {
        public event Action<ReferenceTab> ActiveTabChanged;

        private readonly Label m_ReferencedBy;
        private readonly Label m_ReferencesTo;

        public ReferenceTab ActiveTab { get; private set; } = ReferenceTab.ReferencedBy;

        public ReferenceTabBar()
        {
            AddToClassList("inspector__ref-tabs");

            m_ReferencedBy = MakeTab(ReferenceTab.ReferencedBy);
            m_ReferencesTo = MakeTab(ReferenceTab.ReferencesTo);
            SyncActiveClasses();
        }

        public void SetLabel(ReferenceTab tab, string text) => LabelFor(tab).text = text;

        internal string GetLabel(ReferenceTab tab) => LabelFor(tab).text;

        public void SetActiveTab(ReferenceTab tab)
        {
            if (tab == ActiveTab)
                return;
            ActiveTab = tab;
            SyncActiveClasses();
            ActiveTabChanged?.Invoke(tab);
        }

        private Label MakeTab(ReferenceTab tab)
        {
            var label = new Label();
            label.AddToClassList("inspector__ref-tab");
            label.RegisterCallback<ClickEvent>(_ => SetActiveTab(tab));
            Add(label);
            return label;
        }

        private Label LabelFor(ReferenceTab tab) =>
            tab == ReferenceTab.ReferencedBy ? m_ReferencedBy : m_ReferencesTo;

        private void SyncActiveClasses()
        {
            m_ReferencedBy.EnableInClassList("inspector__ref-tab--active", ActiveTab == ReferenceTab.ReferencedBy);
            m_ReferencesTo.EnableInClassList("inspector__ref-tab--active", ActiveTab == ReferenceTab.ReferencesTo);
        }
    }

    /// <summary>One precomputed reference row: everything bindItem needs, nothing it must derive.</summary>
    internal struct ReferenceRowData
    {
        public string Name;
        public string Path;
        public string Size;
        public ulong SortSize;
        public Texture Icon;
    }

    /// <summary>
    /// A bare reference list: no toolbar, no footer, zebra continuing past the last row.
    /// </summary>
    internal sealed class FlatReferenceList : VisualElement
    {
        private readonly List<ReferenceRowData> m_Rows = new List<ReferenceRowData>();
        private readonly ListView m_List;
        private readonly ZebraEmptyBody m_EmptyBody;
        private readonly bool m_WithSize;

        public FlatReferenceList(bool withSize)
        {
            AddToClassList("inspector__ref-list");
            m_WithSize = withSize;

            m_List = new ListView
            {
                itemsSource = m_Rows,
                fixedItemHeight = 20,
                selectionType = SelectionType.None,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                makeItem = () => AssetReferenceRow.Make(m_WithSize),
                bindItem = BindItem,
            };
            m_List.AddToClassList("inspector__references-list");
            Add(m_List);

            m_EmptyBody = new ZebraEmptyBody(m_List);
            Add(m_EmptyBody);
        }

        internal int Count => m_Rows.Count;
        internal IReadOnlyList<ReferenceRowData> Rows => m_Rows;

        public void SetRows(List<ReferenceRowData> rows)
        {
            m_Rows.Clear();
            m_Rows.AddRange(rows);
            m_List.RefreshItems();
            m_EmptyBody.Refresh();
        }

        public void ClearRows()
        {
            m_Rows.Clear();
            m_List.RefreshItems();
            m_EmptyBody.Refresh();
        }

        // The zebra reads live geometry, so it needs a nudge when this list is display-toggled
        // back on without its rows changing.
        public void RefreshZebra() => m_EmptyBody.Refresh();

        private void BindItem(VisualElement element, int index)
        {
            var row = m_Rows[index];
            AssetReferenceRow.Bind(element, row.Icon, row.Name, row.Path, m_WithSize ? row.Size : null);
        }
    }

    /// <summary>
    /// The one reference-row implementation: icon + name (path tooltip) + optional right-aligned
    /// size. Shared by the root-asset ReferencesView and the flat lists (and, later, the tree),
    /// so every styling fix lands once. The size arrives preformatted — edge cost, output size or
    /// nothing is the caller's business.
    /// </summary>
    internal static class AssetReferenceRow
    {
        public static VisualElement Make(bool withSize)
        {
            var row = new VisualElement();
            row.AddToClassList("inspector__references-item");
            var icon = new Image { scaleMode = ScaleMode.ScaleToFit };
            icon.AddToClassList("inspector__references-item-icon");
            row.Add(icon);
            var name = new Label();
            name.AddToClassList("inspector__references-item-name");
            row.Add(name);
            if (withSize)
            {
                var size = new Label();
                size.AddToClassList("inspector__references-item-size");
                row.Add(size);
            }
            return row;
        }

        public static void Bind(VisualElement row, Texture icon, string name, string tooltip, string size)
        {
            ((Image)row.ElementAt(0)).image = icon;
            var nameLabel = (Label)row.ElementAt(1);
            nameLabel.text = name;
            nameLabel.tooltip = tooltip;
            if (size != null)
                ((Label)row.ElementAt(2)).text = size;
        }
    }
}
