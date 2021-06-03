// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

using UnityEditor.ShortcutManagement;

namespace UnityEditor.Search
{
    static class Styles
    {
        static Styles()
        {
            if (!isDarkTheme)
            {
                selectedItemLabel.normal.textColor = Color.white;
                selectedItemDescription.normal.textColor = Color.white;
            }

            statusWheel = new GUIContent[12];
            for (int i = 0; i < 12; i++)
                statusWheel[i] = EditorGUIUtility.IconContent("WaitSpin" + i.ToString("00"));

            var syncShortcut = ShortcutManager.instance.GetShortcutBinding(QuickSearch.k_TogleSyncShortcutName);
            var tooltip = $"Synchronize search fields ({syncShortcut})";
            syncSearchButtonContent = new GUIContent(string.Empty, EditorGUIUtility.LoadIcon("QuickSearch/SyncSearch"), tooltip);
            syncSearchOnButtonContent = new GUIContent(string.Empty, EditorGUIUtility.LoadIcon("QuickSearch/SyncSearch On"), tooltip);
        }

        private const int itemRowPadding = 4;
        public const float actionButtonSize = 16f;
        public const float itemPreviewSize = 32f;
        public const float descriptionPadding = 2f;
        public const float itemRowHeight = itemPreviewSize + itemRowPadding * 2f;

        private static bool isDarkTheme => EditorGUIUtility.isProSkin;

        private static readonly RectOffset marginNone = new RectOffset(0, 0, 0, 0);
        private static readonly RectOffset paddingNone = new RectOffset(0, 0, 0, 0);
        private static readonly RectOffset defaultPadding = new RectOffset(itemRowPadding, itemRowPadding, itemRowPadding, itemRowPadding);

        public static readonly string highlightedTextColorFormat = isDarkTheme ? "<color=#F6B93F>{0}</color>" : "<b>{0}</b>";
        public static readonly string tabCountTextColorFormat = isDarkTheme ? "<color=#7B7B7B>{0}</color>" : "<color=#6A6A6A>{0}</color>";

        public static readonly GUIStyle panelBorder = new GUIStyle("grey_border")
        {
            name = "quick-search-border",
            padding = new RectOffset(1, 1, 1, 1),
            margin = new RectOffset(0, 0, 0, 0)
        };

        public static readonly GUIStyle autoCompleteBackground = new GUIStyle("grey_border")
        {
            name = "quick-search-auto-complete-background",
            padding = new RectOffset(1, 1, 1, 1),
            margin = new RectOffset(0, 0, 0, 0)
        };

        public static readonly GUIContent moreActionsContent = new GUIContent("", Icons.more, "Open actions menu");
        public static readonly GUIContent moreProviderFiltersContent = new GUIContent("", Icons.more, "Display search provider filter ids and toggle their activate state.");

        public static readonly GUIStyle scrollbar = new GUIStyle("VerticalScrollbar");
        public static readonly float scrollbarWidth = scrollbar.fixedWidth + scrollbar.margin.horizontal;

        public static readonly GUIStyle itemBackground1 = new GUIStyle
        {
            name = "quick-search-item-background1",
            fixedHeight = 0,

            margin = marginNone,
            padding = defaultPadding
        };

        public static readonly GUIStyle itemBackground2 = new GUIStyle(itemBackground1) { name = "quick-search-item-background2" };
        public static readonly GUIStyle selectedItemBackground = new GUIStyle(itemBackground1) { name = "quick-search-item-selected-background" };

        public static readonly GUIStyle gridItemBackground = new GUIStyle()
        {
            name = "quick-search-grid-item-background",
            alignment = TextAnchor.MiddleCenter,
            imagePosition = ImagePosition.ImageOnly
        };

        public static readonly GUIStyle gridItemLabel = new GUIStyle("ProjectBrowserGridLabel")
        {
            wordWrap = true,
            fixedWidth = 0,
            fixedHeight = 0,
            alignment = TextAnchor.MiddleCenter,
            margin = marginNone,
            padding = new RectOffset(2, 1, 1, 1)
        };

        public static readonly GUIStyle itemGridBackground1 = new GUIStyle(itemBackground1) { fixedHeight = 0, };
        public static readonly GUIStyle itemGridBackground2 = new GUIStyle(itemBackground2) { fixedHeight = 0 };

        public static readonly GUIStyle preview = new GUIStyle
        {
            name = "quick-search-item-preview",
            fixedWidth = 0,
            fixedHeight = 0,
            alignment = TextAnchor.MiddleCenter,
            imagePosition = ImagePosition.ImageOnly,
            margin = new RectOffset(8, 2, 2, 2),
            padding = paddingNone
        };

        public static readonly Vector2 previewSize = new Vector2(256, 256);
        public static readonly GUIStyle largePreview = new GUIStyle
        {
            name = "quick-search-item-large-preview",
            alignment = TextAnchor.MiddleCenter,
            imagePosition = ImagePosition.ImageOnly,
            margin = new RectOffset(8, 8, 2, 2),
            padding = paddingNone,
            stretchWidth = true,
            stretchHeight = true
        };

        public static readonly GUIStyle itemLabel = new GUIStyle(EditorStyles.label)
        {
            name = "quick-search-item-label",
            richText = true,
            wordWrap = false,
            margin = new RectOffset(8, 4, 4, 2),
            padding = paddingNone
        };

        public static readonly GUIStyle itemLabelLeftAligned = new GUIStyle(itemLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(2, 2, 0, 0)
        };
        public static readonly GUIStyle itemLabelCenterAligned = new GUIStyle(itemLabelLeftAligned) { alignment = TextAnchor.MiddleCenter };
        public static readonly GUIStyle itemLabelrightAligned = new GUIStyle(itemLabelLeftAligned) { alignment = TextAnchor.MiddleRight };

        public static readonly GUIStyle itemLabelCompact = new GUIStyle(itemLabel)
        {
            name = "quick-search-item-compact-label",
            margin = new RectOffset(8, 4, 2, 2)
        };

        public static readonly GUIStyle itemLabelGrid = new GUIStyle(itemLabel)
        {
            fontSize = itemLabel.fontSize - 1,
            wordWrap = true,
            alignment = TextAnchor.UpperCenter,
            margin = marginNone,
            padding = new RectOffset(1, 1, 1, 1)
        };

        public static readonly GUIStyle selectedItemLabel = new GUIStyle(itemLabel)
        {
            name = "quick-search-item-selected-label",
            padding = paddingNone
        };

        public static readonly GUIStyle selectedItemLabelCompact = new GUIStyle(selectedItemLabel)
        {
            name = "quick-search-item-selected-compact-label",
            margin = new RectOffset(8, 4, 2, 2)
        };

        public static readonly GUIStyle autoCompleteItemLabel = new GUIStyle(EditorStyles.label)
        {
            richText = true,
            name = "quick-search-auto-complete-item-label",
            fixedHeight = EditorStyles.toolbarButton.fixedHeight,
            padding = new RectOffset(9, 10, 0, 1)
        };

        public static readonly GUIStyle autoCompleteSelectedItemLabel = new GUIStyle(autoCompleteItemLabel)
        {
            name = "quick-search-auto-complete-item-selected-label"
        };

        public static readonly GUIStyle autoCompleteTooltip = new GUIStyle(EditorStyles.label)
        {
            richText = true,
            alignment = TextAnchor.MiddleRight,
            padding = new RectOffset(2, 6, 0, 2)
        };

        public static readonly GUIStyle noResult = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        {
            name = "quick-search-no-result",
            fontSize = 20,
            fixedHeight = 0,
            fixedWidth = 0,
            wordWrap = true,
            richText = true,
            alignment = TextAnchor.MiddleCenter,
            margin = marginNone,
            padding = paddingNone
        };

        public static readonly GUIStyle itemDescription = new GUIStyle(EditorStyles.label)
        {
            name = "quick-search-item-description",
            richText = true,
            wordWrap = false,
            margin = new RectOffset(4, 4, 1, 4),
            padding = paddingNone,
            fontSize = Math.Max(9, itemLabel.fontSize - 2)
        };

        public static readonly GUIStyle previewDescription = new GUIStyle(itemDescription)
        {
            wordWrap = true,
            margin = new RectOffset(4, 4, 10, 4),
            padding = new RectOffset(4, 4, 4, 4),
            fontSize = Math.Max(11, itemLabel.fontSize),
            alignment = TextAnchor.MiddleLeft
        };

        public static readonly GUIStyle statusLabel = new GUIStyle(itemDescription)
        {
            name = "quick-search-status-label",
            margin = new RectOffset(4, 4, 1, 1),
            fontSize = Math.Max(9, itemLabel.fontSize - 1),
            clipping = TextClipping.Clip,
            imagePosition = ImagePosition.TextOnly
        };

        public static readonly GUIStyle selectedItemDescription = new GUIStyle(itemDescription)
        {
            name = "quick-search-item-selected-description"
        };

        public static readonly GUIStyle actionButton = new GUIStyle("IconButton")
        {
            name = "quick-search-action-button",

            fixedWidth = actionButtonSize,
            fixedHeight = actionButtonSize,

            imagePosition = ImagePosition.ImageOnly,
            alignment = TextAnchor.MiddleCenter,

            margin = new RectOffset(4, 4, 4, 4),
            padding = paddingNone
        };

        public static readonly GUIStyle tabMoreButton = new GUIStyle(actionButton)
        {
            margin = new RectOffset(4, 4, 6, 0)
        };

        public static readonly GUIStyle syncButton = new GUIStyle("IconButton")
        {
            margin = new RectOffset(4, 4, 6, 0),
            padding = paddingNone,
            fixedWidth = actionButtonSize,
            fixedHeight = actionButtonSize,
            imagePosition = ImagePosition.ImageOnly,
            alignment = TextAnchor.MiddleCenter
        };

        public static readonly GUIStyle actionButtonHovered = new GUIStyle(actionButton)
        {
            name = "quick-search-action-button-hovered"
        };

        private static readonly GUIStyleState clear = new GUIStyleState()
        {
            background = null,
            scaledBackgrounds = new Texture2D[] { null },
            textColor = isDarkTheme ? new Color(210 / 255f, 210 / 255f, 210 / 255f) : Color.black
        };

        public static readonly GUIStyle toolbar = new GUIStyle("Toolbar")
        {
            name = "quick-search-bar",
            margin = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(4, 8, 4, 4),
            border = new RectOffset(0, 0, 0, 0),
            fixedHeight = 0f
        };


        const int k_SearchFieldFontSize = 15;

        public static readonly GUIStyle searchField = new GUIStyle("ToolbarSeachTextFieldPopup")
        {
            name = "quick-search-search-field",
            wordWrap = true,
            fontSize = k_SearchFieldFontSize,
            fixedHeight = 0f,
            fixedWidth = 0f,
            alignment = TextAnchor.MiddleLeft,
            margin = new RectOffset(4, 0, 4, 4),
            padding = new RectOffset(8, 20, 0, 0),
            border = new RectOffset(0, 0, 0, 0),
            normal = clear,
            focused = clear,
            hover = clear,
            active = clear,
            onNormal = clear,
            onHover = clear,
            onFocused = clear,
            onActive = clear,
        };

        public static readonly GUIStyle placeholderTextStyle = new GUIStyle(searchField)
        {
            name = "quick-search-search-field-placeholder",
            fontSize = k_SearchFieldFontSize,
            padding = new RectOffset(0, 0, 0, 0),
            alignment = TextAnchor.MiddleCenter,
            normal = clear,
            focused = clear,
            hover = clear,
            active = clear,
            onNormal = clear,
            onHover = clear,
            onFocused = clear,
            onActive = clear
        };

        public static readonly GUIStyle searchFieldBtn = new GUIStyle()
        {
            name = "quick-search-search-field-clear",
            richText = false,
            fixedHeight = 0,
            fixedWidth = 0,
            margin = new RectOffset(0, 4, 0, 0),
            padding = new RectOffset(0, 0, 0, 0),
            normal = clear,
            focused = clear,
            hover = clear,
            active = clear,
            onNormal = clear,
            onHover = clear,
            onFocused = clear,
            onActive = clear,
            alignment = TextAnchor.MiddleRight,
            imagePosition = ImagePosition.ImageOnly
        };

        public static readonly GUIStyle searchFieldTabToFilterBtn = new GUIStyle()
        {
            richText = true,
            fontSize = k_SearchFieldFontSize,
            fixedHeight = 0,
            fixedWidth = 0,
            margin = new RectOffset(0, 25, 0, 0),
            padding = new RectOffset(0, 0, 0, 2),
            normal = clear,
            focused = clear,
            hover = clear,
            active = clear,
            onNormal = clear,
            onHover = clear,
            onFocused = clear,
            onActive = clear,
            alignment = TextAnchor.MiddleRight,
            imagePosition = ImagePosition.TextOnly
        };

        public static readonly GUIContent searchFavoriteButtonContent = new GUIContent(string.Empty, EditorGUIUtility.FindTexture("Favorite Icon"), "Mark as Favorite");
        public static readonly GUIContent searchFavoriteOnButtonContent = new GUIContent(string.Empty, EditorGUIUtility.FindTexture("Favorite On Icon"), "Remove as Favorite");
        public static readonly GUIContent saveQueryButtonContent = new GUIContent(string.Empty, EditorGUIUtility.FindTexture("SaveAs"), "Save search query as an asset.");
        public static readonly GUIContent previewInspectorContent = new GUIContent("Inspector", EditorGUIUtility.FindTexture("UnityEditor.InspectorWindow"), "Open Inspector");
        public static readonly GUIContent previewInspectorButtonContent = new GUIContent(string.Empty, EditorGUIUtility.FindTexture("UnityEditor.InspectorWindow"), "Open Inspector");
        public static readonly GUIContent sortButtonContent = new GUIContent(string.Empty, EditorGUIUtility.FindTexture("UnityEditor/Filter Icon"), "Change Saved Searches sorting order");

        public static readonly GUIContent syncSearchButtonContent = new GUIContent(string.Empty, EditorGUIUtility.LoadIcon("QuickSearch/SyncSearch"), "Synchronize search fields (Ctrl + K)");
        public static readonly GUIContent syncSearchOnButtonContent = new GUIContent(string.Empty, EditorGUIUtility.LoadIcon("QuickSearch/SyncSearch On"), "Synchronize search fields (Ctrl + K)");
        public static readonly GUIContent syncSearchAllGroupTabContent = new GUIContent(string.Empty, EditorGUIUtility.LoadIcon("QuickSearch/SyncSearch"), "Choose a specific search tab (eg. Project) to enable synchronization.");
        public static readonly GUIContent syncSearchProviderNotSupportedContent = new GUIContent(string.Empty, EditorGUIUtility.LoadIcon("QuickSearch/SyncSearch"), "Search provider doesn't support synchronization");
        public static readonly GUIContent syncSearchViewNotEnabledContent = new GUIContent(string.Empty, EditorGUIUtility.LoadIcon("QuickSearch/SyncSearch"), "Search provider uses a search engine\nthat cannot be synchronized.\nSee Preferences -> Search.");
        public static readonly GUIContent searchTipsHelp = new GUIContent("Type '?' for help", EditorGUIUtility.LoadIcon("QuickSearch/Help"));
        public static readonly GUIContent searchTipsDrag = new GUIContent("Drag from search results to Scene, Hierarchy or Inspector", EditorGUIUtility.LoadIcon("QuickSearch/DragArrow"));
        public static readonly GUIContent searchTipsSaveSearches = new GUIContent("Save Searches you use often", EditorGUIUtility.FindTexture("SaveAs"));
        public static readonly GUIContent searchTipsPreviewInspector = new GUIContent("Enable the Preview Inspector to edit search results in place", EditorGUIUtility.LoadIcon("UnityEditor.InspectorWindow"));
        public static readonly GUIContent searchTipsSync = new GUIContent("Enable sync to keep other Editor search fields populated ", EditorGUIUtility.LoadIcon("QuickSearch/SyncSearch On"));
        public static readonly GUIContent sideBarPanelContent = new GUIContent(string.Empty, Utils.LoadIcon("ShowPanels"), "Open Saved Searches Panel");

        public static readonly GUIContent[] searchTipIcons =
        {
            new GUIContent("", EditorGUIUtility.LoadIcon("QuickSearch/Help")),
            new GUIContent("", EditorGUIUtility.LoadIcon("QuickSearch/DragArrow")),
            new GUIContent("", EditorGUIUtility.FindTexture("SaveAs")),
            new GUIContent("", EditorGUIUtility.LoadIcon("UnityEditor.InspectorWindow")),
            new GUIContent("", EditorGUIUtility.LoadIcon("QuickSearch/SyncSearch On"))
        };

        public static readonly GUIContent[] searchTipLabels =
        {
            new GUIContent("Type '?' for help"),
            new GUIContent("Drag from search results to Scene, Hierarchy or Inspector"),
            new GUIContent("Save Searches you use often"),
            new GUIContent("Enable the Preview Inspector to edit search results in place"),
            new GUIContent("Enable sync to keep other Editor search fields populated")
        };

        public static readonly GUIStyle tipIcon = new GUIStyle("Label")
        {
            margin = new RectOffset(4, 4, 2, 2),
            stretchWidth = false
        };
        public static readonly GUIStyle tipText = new GUIStyle("Label")
        {
            richText = true,
            wordWrap = true
        };

        public static readonly GUIStyle tipsSection = Utils.FromUSS("quick-search-tips-section");

        public static readonly GUIStyle statusError = new GUIStyle("CN StatusError") { padding = new RectOffset(2, 2, 1, 1) };
        public static readonly GUIStyle statusWarning = new GUIStyle("CN StatusWarn") { padding = new RectOffset(2, 2, 1, 1) };

        public static readonly GUIStyle toolbarButton = new GUIStyle("IconButton")
        {
            margin = new RectOffset(2, 2, 0, 0),
            padding = new RectOffset(0, 0, 0, 0),
            fixedWidth = 24f,
            fixedHeight = 24f,
            imagePosition = ImagePosition.ImageOnly,
            alignment = TextAnchor.MiddleCenter
        };

        public static readonly GUIStyle savedSearchesHeaderButton = new GUIStyle("IconButton")
        {
            margin = new RectOffset(2, 2, 4, 4),
            padding = new RectOffset(0, 0, 0, 0),
            fixedWidth = 23f,
            fixedHeight = 23f,
            imagePosition = ImagePosition.ImageOnly,
            alignment = TextAnchor.MiddleCenter
        };

        public static readonly GUIStyle toolbarDropdownButton = new GUIStyle("ToolbarCreateAddNewDropDown")
        {
            margin = new RectOffset(2, 2, 0, 0),
            padding = new RectOffset(2, 0, 0, 0),
            fixedWidth = 32f,
            fixedHeight = 24f,
            imagePosition = ImagePosition.ImageOnly,
            alignment = TextAnchor.MiddleLeft
        };

        public static GUIStyle toolbarSearchField = new GUIStyle(EditorStyles.toolbarSearchField)
        {
            margin = new RectOffset(4, 4, 4, 4)
        };

        public static readonly GUIStyle panelHeader = Utils.FromUSS(new GUIStyle() {
            margin = new RectOffset(4, 4, 2, 3),
            padding = new RectOffset(4, 4, 6, 2),
            wordWrap = false,
            stretchWidth = true,
            clipping = TextClipping.Clip
        }, "quick-search-panel-header");

        public static readonly GUIStyle reportHeader = new GUIStyle(panelHeader)
        {
            stretchWidth = false,
            margin = new RectOffset(10, 10, 0, 0)
        };

        public static readonly GUIStyle reportButton = new GUIStyle("IconButton")
        {
            fixedHeight = 20,
            margin = new RectOffset(2, 2, 0, 0),
            padding = new RectOffset(0, 0, 0, 0)
        };

        public static readonly GUIStyle sidebarDropdown = new GUIStyle(EditorStyles.popup)
        {
            fixedHeight = 20,
            margin = new RectOffset(10, 10, 8, 4)
        };

        public static readonly GUIStyle sidebarActionDropdown = new GUIStyle(EditorStyles.miniButton)
        {
            fixedHeight = 20,
            margin = new RectOffset(0, 0, 8, 4),
            padding = new RectOffset(2, 2, 1, 1)
        };

        public static readonly GUIStyle sidebarToggle = new GUIStyle(EditorStyles.toggle)
        {
            margin = new RectOffset(10, 2, 4, 2)
        };

        public static readonly GUIStyle readOnlyObjectField = new GUIStyle("ObjectField")
        {
            padding = new RectOffset(3, 3, 2, 2)
        };

        public static readonly GUIContent prefButtonContent = new GUIContent(Icons.settings, "Open search preferences...");
        public static readonly GUIStyle statusBarButton = new GUIStyle("IconButton")
        {
            fixedWidth = 16,
            fixedHeight = 16,
            margin = new RectOffset(0, 2, 3, 2),
            padding = new RectOffset(0, 0, 0, 0),
            alignment = TextAnchor.MiddleCenter,
            imagePosition = ImagePosition.ImageOnly,
        };

        public static readonly GUIStyle statusBarPrefsButton = new GUIStyle(statusBarButton)
        {
            margin = new RectOffset(0, 2, 2, 2),
        };

        public static readonly GUIStyle searchInProgressButton = new GUIStyle(statusBarButton)
        {
            alignment = TextAnchor.MiddleLeft,
            contentOffset = new Vector2(-1, 0),
            padding = new RectOffset(2, 2, 2, 2),
            richText = false,
            stretchHeight = false,
            stretchWidth = false
        };

        public static readonly GUILayoutOption[] searchInProgressLayoutOptions = new[] { GUILayout.MaxWidth(searchInProgressButton.fixedWidth) };
        public static readonly GUIContent emptyContent = new GUIContent("", "No content");

        public static readonly GUIContent[] statusWheel;

        public static readonly GUIStyle statusBarBackground = new GUIStyle()
        {
            name = "quick-search-status-bar-background",
            fixedHeight = 21f
        };

        public static readonly GUIStyle resultview = new GUIStyle()
        {
            name = "quick-search-result-view",
            padding = new RectOffset(1, 1, 1, 1)
        };

        public static readonly GUIStyle panelBackground = new GUIStyle() { name = "quick-search-panel-background" };
        public static readonly GUIStyle panelBackgroundLeft = new GUIStyle() { name = "quick-search-panel-background-left" };
        public static readonly GUIStyle panelBackgroundRight = new GUIStyle() { name = "quick-search-panel-background-right" };
        public static readonly GUIStyle searchTabBackground = new GUIStyle() { name = "quick-search-tab-background" };
        public static readonly GUIStyle searchTab = Utils.FromUSS(new GUIStyle() { richText = true }, "quick-search-tab");
        public static readonly GUIStyle searchTabMoreButton = new GUIStyle("IN Foldout")
        {
            margin = new RectOffset(10, 2, 0, 0)
        };

        public static readonly GUIContent pressToFilterContent = new GUIContent("Press Tab \u21B9 to filter");
        public static readonly float pressToFilterContentWidth = searchFieldTabToFilterBtn.CalcSize(pressToFilterContent).x;
        public static readonly GUIStyle searchReportField = new GUIStyle(searchTabBackground)
        {
            padding = toolbar.padding
        };

        public static readonly GUIStyle inspector = new GUIStyle()
        {
            name = "quick-search-inspector",
            margin = new RectOffset(1, 0, 0, 0),
            padding = new RectOffset(0, 0, 0, 0)
        };

        public static readonly GUIStyle inpsectorMargins = new GUIStyle(EditorStyles.inspectorDefaultMargins)
        {
            padding = new RectOffset(8, 8, 4, 4)
        };

        public static readonly GUIStyle inpsectorWideMargins = new GUIStyle(inpsectorMargins)
        {
            padding = new RectOffset(18, 8, 4, 4)
        };

        public static class Wiggle
        {
            private static Texture2D GenerateSolidColorTexture(Color fillColor)
            {
                Texture2D texture = new Texture2D(1, 1);
                var fillColorArray = texture.GetPixels();

                for (var i = 0; i < fillColorArray.Length; ++i)
                    fillColorArray[i] = fillColor;

                texture.hideFlags = HideFlags.HideAndDontSave;
                texture.SetPixels(fillColorArray);
                texture.Apply();

                return texture;
            }

            public static readonly GUIStyle wiggle = new GUIStyle()
            {
                name = "quick-search-wiggle",
                fixedHeight = 1f,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                alignment = TextAnchor.LowerCenter,
                normal = new GUIStyleState { background = GenerateSolidColorTexture(Color.red), scaledBackgrounds = new[] { GenerateSolidColorTexture(Color.red) } },
            };

            public static readonly GUIStyle wiggleWarning = new GUIStyle()
            {
                name = "quick-search-wiggle",
                fixedHeight = 1f,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                alignment = TextAnchor.LowerCenter,
                normal = new GUIStyleState { background = GenerateSolidColorTexture(Color.yellow), scaledBackgrounds = new[] { GenerateSolidColorTexture(Color.yellow) } },
            };

            public static readonly GUIStyle wiggleTooltip = new GUIStyle()
            {
                name = "quick-search-wiggle-tooltip",
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };
        }

        public static readonly GUIStyle dropdownItem = Utils.FromUSS("quick-search-dropdown-item");
        public static readonly GUIStyle sidebarButtons = new GUIStyle()
        {
            fixedHeight = 20,
            margin = new RectOffset(0, 8, 8, 4),
        };

        public static readonly GUIStyle dropdownItemButton = new GUIStyle(actionButton)
        {
            margin = new RectOffset(4, 4, 0, 0)
        };

        public static readonly GUIContent importReport = EditorGUIUtility.IconContent("Profiler.Open", "|Import Report...");

        public static readonly GUIContent addMoreColumns = EditorGUIUtility.IconContent("CreateAddNew", "|Add column...");
        public static readonly GUIContent resetColumns = EditorGUIUtility.IconContent("Animation.FilterBySelection", "|Reset Columns Layout");

        public static readonly GUIContent listModeContent = new GUIContent("", EditorGUIUtility.LoadIconRequired("ListView"), "List View");
        public static readonly GUIContent gridModeContent = new GUIContent("", EditorGUIUtility.LoadIconRequired("GridView"), $"Grid View ({DisplayMode.Grid}x{DisplayMode.Grid})");
        public static readonly GUIContent tableModeContent = new GUIContent("", EditorGUIUtility.LoadIconRequired("TableView"), "Table View");

        public static readonly GUIContent tableSaveButtonContent = new GUIContent("Save", EditorGUIUtility.LoadIconRequired("SaveAs"), "Save current table configuration");
        public static readonly GUIContent tableDeleteButtonContent = EditorGUIUtility.IconContent("Grid.EraserTool", "|Delete table configuration");

        public static class QueryBuilder
        {
            public static readonly Color labelColor;
            public static readonly Color splitterColor;
            public static readonly GUIStyle label;

            public static GUIContent createContent = EditorGUIUtility.IconContent("CreateAddNew");
            public static GUIStyle addNewDropDown = new GUIStyle("ToolbarCreateAddNewDropDown")
            {
                fixedWidth = 32f,
                fixedHeight = 0,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(2, 2, 4, 4)
            };

            static QueryBuilder()
            {
                ColorUtility.TryParseHtmlString("#202427", out labelColor);
                splitterColor = new Color(labelColor.r, labelColor.g, labelColor.b, 0.5f);

                label = new GUIStyle("ToolbarLabel")
                {
                    richText = true,
                    alignment = TextAnchor.MiddleLeft,
                    margin = new RectOffset(6, 6, 0, 0),
                    normal = new GUIStyleState { textColor = labelColor },
                    hover = new GUIStyleState { textColor = labelColor }
                };
            }
        }
    }
}
