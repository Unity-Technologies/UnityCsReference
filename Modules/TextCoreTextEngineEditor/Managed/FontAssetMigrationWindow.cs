// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using ObjectField = UnityEditor.UIElements.ObjectField;

namespace UnityEditor.TextCore.Text
{
    internal partial class FontAssetMigrationWindow : EditorWindow
    {
        internal enum EntryStatus
        {
            Pending,
            PendingEmpty,
            Blocked,
            Done,
            DoneEmpty,
        }

        internal class Entry
        {
            public UnityEngine.Object asset;
            public string path;
            public EntryStatus status;
            public string message;
            public KeepBakedData keepBakedData;
            // Loads a font face, so cached rather than computed per refresh; invalidated on focus and before conversion.
            public bool? staleGlyphIndices;
        }

        internal const string CaptionMessage =
            "Static font assets aren't supported by the Advanced Text Generator. Converting to a dynamic " +
            "font asset subsets the source font so only the baked characters ship in builds.";

        internal const string WillConvertEmptyMessage =
            "This font asset has no reference to its source font file. It will be converted to an empty " +
            "dynamic font asset with no font data.";

        internal const string ConvertedEmptyMessage =
            "This font asset had no reference to its source font file, so it was converted to an empty " +
            "dynamic font asset with no font data. Assign a source font in the Inspector to populate it.";

        internal const string LicenseAckKey = "TextCore.FontAssetMigration.LicenseAcknowledged";
        const string k_UxmlPath = "UXML/FontAssetMigration/FontAssetMigrationWindow.uxml";
        internal const string UssPath = "StyleSheets/FontAssetMigration/FontAssetMigrationWindow.uss";

        readonly List<Entry> m_Entries = new List<Entry>();
        // On a first open, GetWindow runs CreateGUI synchronously; the pending scope makes its
        // initial scan use the requested provider instead of scanning twice.
        [AutoStaticsCleanupOnCodeReload]
        static FontAssetMigrationProvider s_PendingScope;
        UnityEngine.Object m_PendingSelection;
        // Providers are reload-scoped statics, so the window holds only the type name and
        // resolves the registry instance on each access.
        [SerializeField] string m_ProviderName;

        FontAssetMigrationProvider ActiveProvider => FontAssetMigrationProvider.FindProvider(m_ProviderName);

        bool SetProvider(FontAssetMigrationProvider provider)
        {
            if (ActiveProvider.GetType() == provider.GetType())
                return false;
            m_ProviderName = provider.GetType().FullName;
            return true;
        }

        internal KeepBakedData? selectedKeepBakedData => SelectedEntry?.keepBakedData;
        internal IReadOnlyList<Entry> entries => m_Entries;

        Label m_Caption;
        ListView m_ListView;
        Label m_ListHeader;
        Label m_FooterCount;
        Button m_ConvertAllButton;
        Label m_DetailEmpty;
        VisualElement m_DetailContent;
        Label m_DetailName;
        Label m_DetailPath;
        ObjectField m_SourceFontField;
        TextField m_GlyphsField;
        TextField m_AtlasField;
        RadioButtonGroup m_KeepBakedDataGroup;
        HelpBox m_DetailMessage;
        Button m_ConvertButton;

        // The priority gap from Sprite Importer (2026) draws a separator above this item.
        [MenuItem("Window/Text/Font Asset Migration", false, 2050)]
        public static void ShowWindow()
        {
            ShowWindow(FontAssetMigrationProvider.Default, null);
        }

        internal static void ShowWindow(FontAssetMigrationProvider provider)
        {
            ShowWindow(provider, null);
        }

        internal static void ShowWindow(UnityEngine.Object selection)
        {
            ShowWindow(FontAssetMigrationProvider.FindProvider(selection), selection);
        }

        static void ShowWindow(FontAssetMigrationProvider provider, UnityEngine.Object selection)
        {
            s_PendingScope = provider;
            var window = GetWindow<FontAssetMigrationWindow>();
            s_PendingScope = null;

            bool scopeChanged = window.SetProvider(provider);
            window.m_PendingSelection = selection;
            if (window.m_ListView == null)
                return; // The layout failed to load; there is no list to scan.

            // The asset may have been created after the last scan.
            if (scopeChanged || (selection != null && !window.ApplyPendingSelection()))
            {
                window.m_PendingSelection = selection;
                window.Rescan();
            }
        }

        [InitializeOnLoadMethod]
        static void RegisterConsoleLinkHandler()
        {
            EditorGUI.hyperLinkClicked += OnHyperLinkClicked;
        }

        // The link value names the provider type to scope to; unknown values (UITK logs "true") open the default scope.
        static void OnHyperLinkClicked(EditorWindow window, HyperLinkClickedEventArgs args)
        {
            if (args.hyperLinkData.TryGetValue("openfontassetmigration", out string scope))
                ShowWindow(FontAssetMigrationProvider.FindProvider(scope));
        }

        internal static List<Entry> ScanStaticFontAssets() => ScanStaticFontAssets(FontAssetMigrationProvider.Default);

        internal static List<Entry> ScanStaticFontAssets(FontAssetMigrationProvider provider)
        {
            var entries = new List<Entry>();
            foreach (UnityEngine.Object asset in provider.ScanStaticAssets())
                entries.Add(new Entry { asset = asset, path = AssetDatabase.GetAssetPath(asset) });
            entries.Sort((a, b) => string.CompareOrdinal(a.path, b.path));
            return entries;
        }

        void CreateGUI()
        {
            titleContent = new GUIContent("Font Asset Migration");
            minSize = new Vector2(640, 400);

            var uxml = EditorGUIUtility.Load(k_UxmlPath) as VisualTreeAsset;
            if (uxml == null)
            {
                rootVisualElement.Add(new HelpBox("Failed to load the Font Asset Migration layout.", HelpBoxMessageType.Error));
                return;
            }

            if (EditorGUIUtility.Load(UssPath) is StyleSheet styleSheet)
                rootVisualElement.styleSheets.Add(styleSheet);

            uxml.CloneTree(rootVisualElement);
            BindUI();
            if (s_PendingScope != null)
                SetProvider(s_PendingScope);
            Rescan();
        }

        void BindUI()
        {
            var root = rootVisualElement;

            m_Caption = root.Q<Label>("caption");

            m_ListHeader = root.Q<Label>("list-header");
            m_ListView = root.Q<ListView>("asset-list");
            m_ListView.fixedItemHeight = 22;
            m_ListView.selectionType = SelectionType.Single;
            m_ListView.itemsSource = m_Entries;
            m_ListView.makeItem = MakeRow;
            m_ListView.bindItem = BindRow;
            m_ListView.selectionChanged += _ => RefreshDetails();
            m_ListView.itemsChosen += items =>
            {
                foreach (object item in items)
                {
                    if (item is Entry entry && entry.asset != null)
                        EditorGUIUtility.PingObject(entry.asset);
                }
            };

            m_DetailEmpty = root.Q<Label>("detail-empty");
            m_DetailContent = root.Q<VisualElement>("detail-content");
            m_DetailName = root.Q<Label>("detail-name");
            m_DetailPath = root.Q<Label>("detail-path");

            m_SourceFontField = root.Q<ObjectField>("detail-source-font");
            m_SourceFontField.objectType = typeof(Font);
            m_SourceFontField.allowSceneObjects = false;
            m_SourceFontField.SetEnabled(false);

            m_GlyphsField = root.Q<TextField>("detail-glyphs");
            m_GlyphsField.SetEnabled(false);
            m_AtlasField = root.Q<TextField>("detail-atlas");
            m_AtlasField.SetEnabled(false);

            // Per-entry option, defaulted from the asset's render mode; the group edits the selection.
            // Keeping the atlas restores the out-of-sync block, so entry statuses need a refresh.
            m_KeepBakedDataGroup = root.Q<RadioButtonGroup>("keep-baked-data");
            var choiceTooltips = new[]
            {
                "Drops the baked atlas, glyph table, and legacy tables. The Advanced Text Generator rasterizes glyphs on demand into a fresh atlas. Default for SDFAA font assets.",
                "Keeps the pre-baked atlas and glyph table to avoid rasterization at startup. The baked glyphs must still match the source font. Default for other render modes.",
                "Also keeps the character table and font feature table. Only the standard text generator uses them.",
            };
            int choiceIndex = 0;
            m_KeepBakedDataGroup.Query<RadioButton>().ForEach(radio =>
            {
                if (choiceIndex < choiceTooltips.Length)
                    radio.tooltip = choiceTooltips[choiceIndex++];
            });
            m_KeepBakedDataGroup.RegisterValueChangedCallback(evt =>
            {
                Entry entry = SelectedEntry;
                if (entry == null)
                    return;
                entry.keepBakedData = (KeepBakedData)evt.newValue;
                RefreshAll();
            });

            m_DetailMessage = root.Q<HelpBox>("detail-message");
            m_ConvertButton = root.Q<Button>("convert-button");
            m_ConvertButton.clicked += ConvertSelected;

            m_FooterCount = root.Q<Label>("footer-count");
            m_ConvertAllButton = root.Q<Button>("convert-all-button");
            m_ConvertAllButton.clicked += ConvertAll;
        }

        static VisualElement MakeRow()
        {
            var row = new VisualElement();
            row.AddToClassList("asset-row");
            var icon = new Image { name = "icon", scaleMode = ScaleMode.ScaleToFit };
            icon.AddToClassList("asset-row__icon");
            row.Add(icon);
            var label = new Label { name = "name" };
            label.AddToClassList("asset-row__label");
            row.Add(label);
            var check = new Image { name = "done", scaleMode = ScaleMode.ScaleToFit };
            check.image = EditorGUIUtility.IconContent("TestPassed").image;
            check.AddToClassList("asset-row__check");
            row.Add(check);
            var warn = new Image { name = "warn", scaleMode = ScaleMode.ScaleToFit };
            warn.AddToClassList("asset-row__warn");
            row.Add(warn);
            return row;
        }

        void BindRow(VisualElement row, int index)
        {
            Entry entry = m_Entries[index];
            row.Q<Image>("icon").image = AssetPreview.GetMiniThumbnail(entry.asset);
            row.Q<Label>("name").text = entry.asset != null ? entry.asset.name : "(deleted)";
            bool done = entry.status == EntryStatus.Done || entry.status == EntryStatus.DoneEmpty;
            row.Q<Image>("done").style.display = done ? DisplayStyle.Flex : DisplayStyle.None;

            var warn = row.Q<Image>("warn");
            bool showWarn = entry.status == EntryStatus.Blocked;
            warn.style.display = showWarn ? DisplayStyle.Flex : DisplayStyle.None;
            if (showWarn)
            {
                warn.image = EditorGUIUtility.IconContent("console.warnicon.sml").image;
                warn.tooltip = entry.message;
            }
        }

        internal void Rescan()
        {
            SetEntries(ScanStaticFontAssets(ActiveProvider));

            if (!ApplyPendingSelection() && m_Entries.Count > 0)
            {
                int first = m_Entries.FindIndex(e => e.status == EntryStatus.Pending || e.status == EntryStatus.PendingEmpty);
                m_ListView.SetSelection(first >= 0 ? first : 0);
            }
        }

        internal void SetEntries(List<Entry> entries)
        {
            m_ListView.ClearSelection();
            m_Entries.Clear();
            m_Entries.AddRange(entries);
            foreach (Entry entry in m_Entries)
                entry.keepBakedData = ActiveProvider.DefaultKeepBakedData(entry.asset);
            RefreshAll();
        }

        bool ApplyPendingSelection()
        {
            if (m_PendingSelection == null || m_ListView == null)
                return false;

            int index = m_Entries.FindIndex(e => e.asset == m_PendingSelection);
            m_PendingSelection = null;
            if (index < 0)
                return false;

            m_ListView.SetSelection(index);
            m_ListView.ScrollToItem(index);
            return true;
        }

        void OnEnable()
        {
            EditorApplication.projectChanged += OnProjectChanged;
        }

        void OnDisable()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        void OnFocus()
        {
            // Assets may have been converted or edited elsewhere while the window was unfocused.
            if (m_ListView == null)
                return;

            foreach (Entry entry in m_Entries)
                entry.staleGlyphIndices = null;
            RefreshAll();
        }

        // Existing entries keep their status and options so converted assets stay listed as done.
        internal void OnProjectChanged()
        {
            if (m_ListView == null)
                return;

            var known = new HashSet<UnityEngine.Object>();
            bool changed = false;
            foreach (Entry entry in m_Entries)
            {
                entry.staleGlyphIndices = null;
                if (entry.asset == null)
                    continue;
                known.Add(entry.asset);
                string path = AssetDatabase.GetAssetPath(entry.asset);
                if (path != entry.path)
                {
                    entry.path = path;
                    changed = true;
                }
            }

            foreach (UnityEngine.Object asset in ActiveProvider.ScanStaticAssets())
            {
                if (!known.Add(asset))
                    continue;
                m_Entries.Add(new Entry
                {
                    asset = asset,
                    path = AssetDatabase.GetAssetPath(asset),
                    keepBakedData = ActiveProvider.DefaultKeepBakedData(asset),
                });
                changed = true;
            }

            if (changed)
            {
                Entry selected = SelectedEntry;
                m_Entries.Sort((a, b) => string.CompareOrdinal(a.path, b.path));
                if (selected != null)
                    m_ListView.SetSelectionWithoutNotify(new[] { m_Entries.IndexOf(selected) });
            }
            RefreshAll();
        }

        Entry SelectedEntry => m_ListView.selectedItem as Entry;

        void UpdateEntryStatus(Entry entry, bool computeStale = false)
        {
            if (entry.asset == null)
            {
                entry.status = EntryStatus.Blocked;
                entry.message = "The font asset was deleted.";
                return;
            }

            if (!ActiveProvider.IsStatic(entry.asset))
            {
                if (entry.status != EntryStatus.DoneEmpty)
                {
                    entry.status = EntryStatus.Done;
                    entry.message = null;
                }
                return;
            }

            if (!ActiveProvider.CanMigrate(entry.asset, out string reason))
            {
                entry.status = EntryStatus.Blocked;
                entry.message = reason;
                return;
            }

            bool hasBakedGlyphs = ActiveProvider.GetGlyphCount(entry.asset) > 0;
            if (!hasBakedGlyphs && ActiveProvider.GetSourceFont(entry.asset) == null)
            {
                entry.status = EntryStatus.PendingEmpty;
                entry.message = WillConvertEmptyMessage;
                return;
            }

            // Glyph-id drift only matters when the baked atlas is kept.
            if (hasBakedGlyphs && entry.keepBakedData != KeepBakedData.None)
            {
                if (computeStale && entry.staleGlyphIndices == null)
                    entry.staleGlyphIndices = ActiveProvider.HasStaleGlyphIndices(entry.asset);
                if (entry.staleGlyphIndices == true)
                {
                    entry.status = EntryStatus.Blocked;
                    entry.message = FontAssetStaticMigrator.OutOfSyncMessage;
                    return;
                }
            }

            entry.status = EntryStatus.Pending;
            entry.message = null;
        }

        void RefreshAll()
        {
            m_Caption.text = $"{CaptionMessage} <a href=\"{ActiveProvider.DocsUrl}\">Learn more</a>";

            foreach (Entry entry in m_Entries)
                UpdateEntryStatus(entry);

            int done = 0;
            bool anyPending = false;
            foreach (Entry entry in m_Entries)
            {
                if (entry.status == EntryStatus.Done || entry.status == EntryStatus.DoneEmpty)
                    done++;
                else if (entry.status != EntryStatus.Blocked)
                    anyPending = true;
            }

            m_ListHeader.text = $"Static Font Assets ({done}/{m_Entries.Count})";
            m_FooterCount.text = $"{done} of {m_Entries.Count} converted";
            m_ConvertAllButton.SetEnabled(anyPending);
            m_ListView.RefreshItems();
            RefreshDetails();
        }

        void RefreshDetails()
        {
            Entry entry = SelectedEntry;
            m_DetailEmpty.style.display = entry == null ? DisplayStyle.Flex : DisplayStyle.None;
            m_DetailContent.style.display = entry == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (entry == null)
                return;

            UpdateEntryStatus(entry, computeStale: true);
            // The stale check can flip the entry to Blocked; repaint the row so its icon matches.
            m_ListView.RefreshItem(m_ListView.selectedIndex);

            bool hasAsset = entry.asset != null;
            m_DetailName.text = hasAsset ? entry.asset.name : "(deleted)";
            m_DetailPath.text = entry.path;
            m_DetailPath.tooltip = entry.path;
            m_SourceFontField.SetValueWithoutNotify(hasAsset ? ActiveProvider.GetSourceFont(entry.asset) : null);
            m_GlyphsField.SetValueWithoutNotify(hasAsset ? ActiveProvider.GetGlyphCount(entry.asset).ToString() : "0");
            m_AtlasField.SetValueWithoutNotify(hasAsset ? ActiveProvider.GetAtlasDescription(entry.asset) : "-");
            m_KeepBakedDataGroup.SetValueWithoutNotify((int)entry.keepBakedData);

            bool showMessage = entry.message != null;
            m_DetailMessage.style.display = showMessage ? DisplayStyle.Flex : DisplayStyle.None;
            if (showMessage)
            {
                m_DetailMessage.text = entry.message;
                m_DetailMessage.messageType = entry.status == EntryStatus.DoneEmpty ? HelpBoxMessageType.Info : HelpBoxMessageType.Warning;
            }

            // The button keeps its action label; the row icon and detail message carry the status.
            m_ConvertButton.SetEnabled(entry.status is EntryStatus.Pending or EntryStatus.PendingEmpty);
        }

        void ConvertSelected()
        {
            Entry entry = SelectedEntry;
            if (entry == null || !FontLicenseNotice.RequestConsent())
                return;

            ConvertEntry(entry);
            RefreshAll();
        }

        void ConvertAll()
        {
            if (!FontLicenseNotice.RequestConsent())
                return;

            foreach (Entry entry in m_Entries)
                ConvertEntry(entry);

            RefreshAll();
        }

        bool ConvertEntry(Entry entry)
        {
            entry.staleGlyphIndices = null;
            UpdateEntryStatus(entry, computeStale: true);
            if (entry.status != EntryStatus.Pending && entry.status != EntryStatus.PendingEmpty)
                return false;

            bool empty = entry.status == EntryStatus.PendingEmpty;
            bool dropAtlas = entry.keepBakedData == KeepBakedData.None;
            bool dropLegacyData = entry.keepBakedData != KeepBakedData.AtlasAndLegacyTables;
            if (!ActiveProvider.Convert(entry.asset, out string error, dropLegacyData, dropAtlas))
            {
                entry.status = EntryStatus.Blocked;
                entry.message = error;
                Debug.LogError($"Font asset conversion failed: {error}", entry.asset);
                return false;
            }

            entry.status = empty ? EntryStatus.DoneEmpty : EntryStatus.Done;
            entry.message = empty ? ConvertedEmptyMessage : null;
            return true;
        }

    }

    // Modal license confirmation shown before the first conversion; embedding font data has licensing implications.
    internal partial class FontLicenseNotice : EditorWindow
    {
        internal const string Message =
            "Converting to a dynamic font asset embeds actual font data in your build. This is different from " +
            "static font assets, which shipped no font data. By proceeding, you confirm that the applicable " +
            "font license(s) permit subsetting and distribution of embedded font data in your application.";

        const string k_UxmlPath = "UXML/FontAssetMigration/FontLicenseNotice.uxml";

        // Test seam: the modal blocks until closed, so tests stub the answer instead.
        [AutoStaticsCleanupOnCodeReload]
        internal static Func<bool> consentOverride;

        bool m_Accepted;

        // Blocks until the dialog closes. Returns true when the user confirmed; converting with
        // "Don't show again" checked persists the confirmation for this project and user.
        public static bool RequestConsent()
        {
            if (consentOverride != null)
                return consentOverride();

            if (EditorUserSettings.GetConfigValue(FontAssetMigrationWindow.LicenseAckKey) == "1")
                return true;

            var window = CreateInstance<FontLicenseNotice>();
            window.titleContent = new GUIContent("Font License Notice");
            window.minSize = window.maxSize = new Vector2(420, 160);
            window.ShowModalUtility();
            return window.m_Accepted;
        }

        void CreateGUI()
        {
            var uxml = EditorGUIUtility.Load(k_UxmlPath) as VisualTreeAsset;
            if (uxml == null)
            {
                rootVisualElement.Add(new HelpBox("Failed to load the Font License Notice layout.", HelpBoxMessageType.Error));
                return;
            }

            if (EditorGUIUtility.Load(FontAssetMigrationWindow.UssPath) is StyleSheet styleSheet)
                rootVisualElement.styleSheets.Add(styleSheet);

            uxml.CloneTree(rootVisualElement);

            rootVisualElement.Q<Label>("license-message").text = Message;

            var dontShowToggle = rootVisualElement.Q<Toggle>("license-dont-show");
            rootVisualElement.Q<Button>("license-cancel").clicked += Close;
            rootVisualElement.Q<Button>("license-convert").clicked += () =>
            {
                // Only an accepted notice is worth remembering; a cancelled one must show again.
                if (dontShowToggle.value)
                    EditorUserSettings.SetConfigValue(FontAssetMigrationWindow.LicenseAckKey, "1");
                m_Accepted = true;
                Close();
            };
        }
    }
}
