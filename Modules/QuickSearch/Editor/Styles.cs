// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.StyleSheets;
using UnityEngine;

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

        private static readonly Color darkColor4 = new Color(111 / 255f, 111 / 255f, 111 / 255f);
        private static readonly Color darkColor5 = new Color(71 / 255f, 71 / 255f, 71 / 255f);
        private static readonly Color darkColor6 = new Color(63 / 255f, 63 / 255f, 63 / 255f);

        private static readonly Color lightColor4 = new Color(111 / 255f, 111 / 255f, 111 / 255f);
        private static readonly Color lightColor5 = new Color(181 / 255f, 181 / 255f, 181 / 255f);
        private static readonly Color lightColor6 = new Color(214 / 255f, 214 / 255f, 214 / 255f);

        public static readonly string highlightedTextColorFormat = isDarkTheme ? "<color=#F6B93F>{0}</color>" : "<b>{0}</b>";

        private static readonly Texture2D buttonPressedBackgroundImage = GenerateSolidColorTexture(isDarkTheme ? darkColor4 : lightColor4);
        private static readonly Texture2D buttonHoveredBackgroundImage = GenerateSolidColorTexture(isDarkTheme ? darkColor5 : lightColor5);

        private static readonly Texture2D searchFieldBg = GenerateSolidColorTexture(isDarkTheme ? darkColor6 : lightColor6);

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

        public static readonly GUIContent moreActionsContent = new GUIContent("", Icons.more, "Open actions menu (Alt + Right)");
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

        public static readonly GUIStyle selectedGridItemBackground = new GUIStyle(selectedItemBackground)
        {
            name = "quick-search-selected-item-grid-background",
            fixedHeight = 0,
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
            margin = new RectOffset(2, 2, 2, 2),
            padding = paddingNone
        };

        public static readonly Vector2 previewSize = new Vector2(256, 256);
        public static readonly GUIStyle largePreview = new GUIStyle
        {
            name = "quick-search-item-large-preview",
            alignment = TextAnchor.MiddleCenter,
            imagePosition = ImagePosition.ImageOnly,
            margin = new RectOffset(2, 2, 2, 2),
            padding = paddingNone,
            stretchWidth = true,
            stretchHeight = true
        };

        public static readonly GUIStyle itemLabel = new GUIStyle(EditorStyles.label)
        {
            name = "quick-search-item-label",
            richText = true,
            wordWrap = false,
            margin = new RectOffset(4, 4, 4, 2),
            padding = paddingNone
        };

        public static readonly GUIStyle itemLabelCompact = new GUIStyle(itemLabel)
        {
            margin = new RectOffset(4, 4, 2, 2)
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
            margin = new RectOffset(4, 4, 2, 2)
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
            fontSize = Math.Max(11, itemLabel.fontSize + 2),
            alignment = TextAnchor.MiddleLeft
        };

        public static readonly GUIStyle statusLabel = new GUIStyle(itemDescription)
        {
            name = "quick-search-status-label",
            margin = new RectOffset(4, 4, 4, 4),
            fontSize = Math.Max(9, itemLabel.fontSize - 1)
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
            padding = paddingNone,

            active = new GUIStyleState { background = buttonPressedBackgroundImage, scaledBackgrounds = new[] { buttonPressedBackgroundImage } },
            hover = new GUIStyleState { background = buttonHoveredBackgroundImage, scaledBackgrounds = new[] { buttonHoveredBackgroundImage } }
        };

        public static readonly GUIStyle tabMoreButton = new GUIStyle(actionButton)
        {
            margin = new RectOffset(4, 4, 6, 0)
        };

        public static readonly GUIStyle actionButtonHovered = new GUIStyle(actionButton)
        {
            name = "quick-search-action-button-hovered"
        };

        private const float k_ToolbarHeight = 40.0f;

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
            padding = new RectOffset(0, 0, 0, 0),
            border = new RectOffset(0, 0, 0, 0),
            fixedHeight = k_ToolbarHeight,

            normal = new GUIStyleState() { background = searchFieldBg, scaledBackgrounds = new Texture2D[] { null } }
        };

        public static readonly GUIStyle searchField = new GUIStyle("ToolbarSeachTextFieldPopup")
        {
            name = "quick-search-search-field",
            fontSize = 19,
            fixedHeight = 32f,
            alignment = TextAnchor.MiddleLeft,
            margin = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(8, 20, 0, 2),
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
            fontSize = 19,
            padding = new RectOffset(0, 0, 0, 0),
            alignment = TextAnchor.MiddleCenter
        };

        public static readonly GUIStyle placeholderTextStyleLeft = new GUIStyle(searchField)
        {
            fontSize = 19,
            padding = new RectOffset(10, 0, 0, 0),
            alignment = TextAnchor.MiddleLeft
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
            fontSize = 18,
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

        public static readonly GUIContent saveQueryButtonContent = new GUIContent(string.Empty, EditorGUIUtility.LoadIcon("SaveAs"), "Save search query as an asset.");

        public static readonly GUIStyle toolbarButton = new GUIStyle(EditorStyles.iconButton)
        {
            margin = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(0, 0, 0, 0),
            fixedWidth = 32f,
            fixedHeight = 32f,
            imagePosition = ImagePosition.ImageOnly,
            alignment = TextAnchor.MiddleCenter
        };

        public static readonly GUIStyle savedSearchItem = GUIStyleExtensions.FromUSS("quick-search-saved-search-item");
        public static readonly GUIStyle savedSearchItemSelected = GUIStyleExtensions.FromUSS("quick-search-saved-search-item-selected");
        public static readonly GUIStyle panelHeader = GUIStyleExtensions.FromUSS(new GUIStyle() {
            wordWrap = true,
            stretchWidth = true,
            clipping = TextClipping.Clip
        }, "quick-search-panel-header");

        public static readonly GUIStyle sidebarDropdown = new GUIStyle(EditorStyles.popup)
        {
            fixedHeight = 20,
            margin = new RectOffset(10, 10, 8, 4)
        };

        public static readonly GUIContent prefButtonContent = new GUIContent(Icons.settings, "Open search preferences...");
        public static readonly GUIStyle prefButton = new GUIStyle("IconButton")
        {
            fixedWidth = 16,
            fixedHeight = 16,
            margin = new RectOffset(0, 2, 2, 2),
            padding = new RectOffset(0, 0, 0, 0)
        };

        public static readonly GUIStyle searchInProgressButton = new GUIStyle(prefButton)
        {
            imagePosition = ImagePosition.ImageOnly,
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
        public static readonly GUIStyle searchTab = GUIStyleExtensions.FromUSS(GUIStyle.none, "quick-search-tab");

        public static readonly GUIContent pressToFilterContent = new GUIContent("Press Tab \u21B9 to filter");
        public static readonly float pressToFilterContentWidth = searchFieldTabToFilterBtn.CalcSize(pressToFilterContent).x;

        public static readonly GUIStyle inspector = new GUIStyle()
        {
            name = "quick-search-inspector",
            margin = new RectOffset(8, 8, 1, 1),
            padding = new RectOffset(0, 0, 0, 0)
        };
    }
}
