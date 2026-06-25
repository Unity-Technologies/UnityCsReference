// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.ProjectAuditor.Editor.Utils;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.PackageManager;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    internal static partial class Utility
    {
        public enum IconType
        {
            Info,
            Warning,
            Error,

            Critical,
            Major,
            Moderate,
            Minor,
            Ignored,

            Help,
            Refresh,
            Settings,

            StatusWheel,
            Hierarchy,
            ZoomTool,
            Fix,
            Download,
            Load,
            Save,
            Trash,
            View,
            WhiteCheckMark,
            GreenCheckMark,
            AdditionalAnalysis,
            FoldoutExpanded,
            FoldoutFolded
        }

        // Log level
        static readonly string k_InfoIconName = "console.infoicon";
        static readonly string k_WarningIconName = "console.warnicon";
        static readonly string k_ErrorIconName = "console.erroricon";

        // Severity
        static readonly string k_CriticalIconName = "Critical";
        static readonly string k_MajorIconName = "Major";
        static readonly string k_ModerateIconName = "Moderate";
        static readonly string k_MinorIconName = "Minor";
        static readonly string k_IgnoredIconName = "Ignored";

        static readonly string k_HelpIconName = "_Help";
        static readonly string k_RefreshIconName = "Refresh";
        static readonly string k_SettingsIconName = "Settings";

        static readonly string k_WhiteCheckMarkIconName = "FilterSelectedOnly";
        static readonly string k_GreenCheckMarkIconName = "TestPassed";
        static readonly string k_HierarchyIconName = "UnityEditor.SceneHierarchyWindow";
        static readonly string k_ZoomToolIconName = "ViewToolZoom";
        static readonly string k_FixIconName = "Profiler.Custom";
        static readonly string k_DownloadIconName = "Download-Available";
        static readonly string k_LoadIconName = "Import";
        static readonly string k_SaveIconName = "SaveAs";
        static readonly string k_TrashIconName = "TreeEditor.Trash";
        static readonly string k_ViewIconName = "ViewToolOrbit";
        static readonly string k_AdditionalAnalysisIconName = "AdditionalAnalysis";
        static readonly string k_FoldoutExpandedIconName = "ClassicFoldoutArrow-Open";
        static readonly string k_FoldoutFoldedIconName = "ClassicFoldoutArrow-Close";

        [NoAutoStaticsCleanup] // Lazily loaded from asset database by fixed name; survives code reload
        static Texture2D s_CriticalIcon;
        [NoAutoStaticsCleanup] // Lazily loaded from asset database by fixed name; survives code reload
        static Texture2D s_MajorIcon;
        [NoAutoStaticsCleanup] // Lazily loaded from asset database by fixed name; survives code reload
        static Texture2D s_ModerateIcon;
        [NoAutoStaticsCleanup] // Lazily loaded from asset database by fixed name; survives code reload
        static Texture2D s_MinorIcon;
        [NoAutoStaticsCleanup] // Lazily loaded from asset database by fixed name; survives code reload
        static Texture2D s_IgnoredIcon;

        [NoAutoStaticsCleanup] // Lazily loaded from asset database by fixed name; survives code reload
        static Texture2D s_AdditionalAnalysisIcon;
        [NoAutoStaticsCleanup] // Lazily loaded from asset database by fixed name; survives code reload
        static Texture2D s_FoldoutExpandedIcon;
        [NoAutoStaticsCleanup] // Lazily loaded from asset database by fixed name; survives code reload
        static Texture2D s_FoldoutFoldedIcon;

        [AutoStaticsCleanupOnCodeReload]
        static GUIContent[] s_StatusWheel;

        [AutoStaticsCleanupOnCodeReload]
        static byte[] s_LetterWidths;
        [AutoStaticsCleanupOnCodeReload]
        static GUIStyle s_Style;
        [AutoStaticsCleanupOnCodeReload]
        static GUIContent s_TempContent;

        public static readonly GUIContent ClearSelection = new GUIContent("Clear Selection");
        public static readonly GUIContent CopyRowToClipboard = new GUIContent("Copy Row(s) to Clipboard");
        public static readonly GUIContent CopyCellToClipboard = new GUIContent("Copy Column Item(s) to Clipboard");
        public static readonly GUIContent OpenIssue = new GUIContent("Open Issue");
        public static readonly GUIContent OpenScriptReference = new GUIContent("Open Script Reference");

        internal class DropdownItem
        {
            public GUIContent Content;
            public GUIContent SelectionContent;
            public bool Enabled;
            public object UserData;
        }

        public static bool BoldFoldout(bool toggle, GUIContent content)
        {
            var style = SharedStyles.Foldout;
            Vector2 textSize = style.CalcSize(content);
            // Reserve the full available width for layout so parent containers don't shrink horizontally
            // when the foldout is the only visible child, but limit the foldout's hit area to just the
            // arrow + label so clicks in the empty space to the right don't toggle it.
            Rect fullRect = GUILayoutUtility.GetRect(content, style, GUILayout.ExpandWidth(true));
            Rect hitRect = new Rect(fullRect.x, fullRect.y, textSize.x, fullRect.height);

            return EditorGUI.Foldout(hitRect, toggle, content, true, style);
        }

        public static void ToolbarDropdownList(DropdownItem[] items, int selectionIndex,
            GenericMenu.MenuFunction2 callback, params GUILayoutOption[] options)
        {
            var selectionContent = items[selectionIndex].SelectionContent;
            var r = GUILayoutUtility.GetRect(selectionContent, EditorStyles.toolbarButton, options);
            if (EditorGUI.DropdownButton(r, selectionContent, FocusType.Passive, EditorStyles.toolbarDropDown))
            {
                var menu = new GenericMenu();

                for (var i = 0; i != items.Length; i++)
                    if (items[i].Enabled)
                        menu.AddItem(items[i].Content, i == selectionIndex, callback, items[i].UserData);
                    else
                        menu.AddDisabledItem(items[i].Content);
                menu.DropDown(r);
            }
        }

        internal static bool ToolbarButtonWithDropdownList(GUIContent content, string[] buttonNames,
            GenericMenu.MenuFunction2 callback, params GUILayoutOption[] options)
        {
            var rect = GUILayoutUtility.GetRect(content, EditorStyles.toolbarDropDown, options);
            var dropDownRect = rect;

            if (Event.current.type == EventType.MouseDown && dropDownRect.Contains(Event.current.mousePosition))
            {
                var menu = new GenericMenu();
                for (var i = 0; i != buttonNames.Length; i++)
                    menu.AddItem(new GUIContent(buttonNames[i]), false, callback, i);

                menu.DropDown(rect);
                Event.current.Use();

                return false;
            }

            return GUI.Button(rect, content, EditorStyles.toolbarDropDown);
        }

        public static void DrawHelpButton(GUIContent content, string url)
        {
            if (GUILayout.Button(content, EditorStyles.toolbarButton, GUILayout.MaxWidth(25)))
            {
                Application.OpenURL(url);
            }
        }

        public static void DrawSelectedText(string text)
        {
            const int kBorder = 4;

            var treeViewSelectionStyle = SharedStyles.TextBoxBackground;
            var textStyle = SharedStyles.IconLabel;

            var content = GetTempContent(text);
            var size = textStyle.CalcSize(content);
            var rect = EditorGUILayout.GetControlRect(GUILayout.MaxWidth(size.x + kBorder), GUILayout.Height(size.y + kBorder));

            GUI.Box(rect, GUIContent.none, treeViewSelectionStyle);
            GUI.Label(rect, content, textStyle);
        }

        public static string GetTreeViewSelectedSummary(TreeViewSelection selection, string[] names)
        {
            var selectedStrings = selection.GetSelectedStrings(names, true, false);
            var numStrings = selectedStrings.Length;

            if (numStrings == 0)
                return "None";

            if (numStrings == 1)
                return selectedStrings[0];

            return Formatting.CombineStrings(selectedStrings);
        }

        static string GetPlatformIconName(BuildTargetGroup buildTargetGroup)
        {
            string platformName;
            if (buildTargetGroup == BuildTargetGroup.Unknown)
                return "BuildSettings.Broadcom";

            switch (buildTargetGroup)
            {
                case BuildTargetGroup.WSA:
                    platformName = "Metro";
                    break;
                case BuildTargetGroup.GameCoreXboxSeries:
                    platformName = "GameCoreScarlett";
                    break;
                default:
                    platformName = buildTargetGroup.ToString();
                    break;
            }

            return $"BuildSettings.{platformName}.Small";
        }

        public static GUIContent GetPlatformIconWithName(BuildTarget buildTarget)
        {
            var iconName = GetPlatformIconName(BuildPipeline.GetBuildTargetGroup(buildTarget));

            return EditorGUIUtility.TrTextContentWithIcon(Formatting.GetModernBuildTargetName(buildTarget), iconName);
        }

        public static GUIContent GetIcon(IconType iconType, string tooltip = null)
        {
            switch (iconType)
            {
                // log level icons
                case IconType.Info:
                    if (string.IsNullOrEmpty(tooltip))
                        tooltip = "Info";
                    return EditorGUIUtility.TrIconContent(k_InfoIconName, tooltip);
                case IconType.Warning:
                    if (string.IsNullOrEmpty(tooltip))
                        tooltip = "Warning";
                    return EditorGUIUtility.TrIconContent(k_WarningIconName, tooltip);
                case IconType.Error:
                    if (string.IsNullOrEmpty(tooltip))
                        tooltip = "Error";
                    return EditorGUIUtility.TrIconContent(k_ErrorIconName, tooltip);

                // Severity icons
                case IconType.Critical:
                    if (string.IsNullOrEmpty(tooltip))
                        tooltip = "Critical";
                    if (s_CriticalIcon == null)
                        s_CriticalIcon = LoadIcon(k_CriticalIconName);
                    return EditorGUIUtility.TrIconContent(s_CriticalIcon, tooltip);
                case IconType.Major:
                    if (string.IsNullOrEmpty(tooltip))
                        tooltip = "Major";
                    if (s_MajorIcon == null)
                        s_MajorIcon = LoadIcon(k_MajorIconName);
                    return EditorGUIUtility.TrIconContent(s_MajorIcon, tooltip);
                case IconType.Moderate:
                    if (string.IsNullOrEmpty(tooltip))
                        tooltip = "Moderate";
                    if (s_ModerateIcon == null)
                        s_ModerateIcon = LoadIcon(k_ModerateIconName);
                    return EditorGUIUtility.TrIconContent(s_ModerateIcon, tooltip);
                case IconType.Minor:
                    if (string.IsNullOrEmpty(tooltip))
                        tooltip = "Minor";
                    if (s_MinorIcon == null)
                        s_MinorIcon = LoadIcon(k_MinorIconName);
                    return EditorGUIUtility.TrIconContent(s_MinorIcon, tooltip);
                case IconType.Ignored:
                    if (string.IsNullOrEmpty(tooltip))
                        tooltip = "Ignored";
                    if (s_IgnoredIcon == null)
                        s_IgnoredIcon = LoadIcon(k_IgnoredIconName);
                    return EditorGUIUtility.TrIconContent(s_IgnoredIcon, tooltip);

                case IconType.AdditionalAnalysis:
                    if (string.IsNullOrEmpty(tooltip))
                        tooltip = "Not Analyzed";
                    if (s_AdditionalAnalysisIcon == null)
                        s_AdditionalAnalysisIcon = LoadIcon(k_AdditionalAnalysisIconName, "d_");
                    return EditorGUIUtility.TrIconContent(s_AdditionalAnalysisIcon, tooltip);
                case IconType.FoldoutExpanded:
                    if (s_FoldoutExpandedIcon == null)
                        s_FoldoutExpandedIcon = LoadIcon(k_FoldoutExpandedIconName);
                    return EditorGUIUtility.TrIconContent(s_FoldoutExpandedIcon);
                case IconType.FoldoutFolded:
                    if (s_FoldoutFoldedIcon == null)
                        s_FoldoutFoldedIcon = LoadIcon(k_FoldoutFoldedIconName);
                    return EditorGUIUtility.TrIconContent(s_FoldoutFoldedIcon);

                case IconType.Hierarchy:
                    return EditorGUIUtility.TrIconContent(k_HierarchyIconName, tooltip);
                case IconType.ZoomTool:
                    return EditorGUIUtility.TrIconContent(k_ZoomToolIconName, tooltip);
                case IconType.Fix:
                    return EditorGUIUtility.TrIconContent(k_FixIconName, tooltip);
                case IconType.Download:
                    return EditorGUIUtility.TrIconContent(k_DownloadIconName, tooltip);
                case IconType.View:
                    return EditorGUIUtility.TrIconContent(k_ViewIconName, tooltip);
                case IconType.Help:
                    return EditorGUIUtility.TrIconContent(k_HelpIconName, tooltip);
                case IconType.Refresh:
                    return EditorGUIUtility.TrIconContent(k_RefreshIconName, tooltip);
                case IconType.Settings:
                    return EditorGUIUtility.TrIconContent(k_SettingsIconName, tooltip);
                case IconType.Load:
                    return EditorGUIUtility.TrIconContent(k_LoadIconName, tooltip);
                case IconType.Save:
                    return EditorGUIUtility.TrIconContent(k_SaveIconName, tooltip);
                case IconType.Trash:
                    return EditorGUIUtility.TrIconContent(k_TrashIconName, tooltip);
                case IconType.StatusWheel:
                    return GetStatusWheel();
                case IconType.WhiteCheckMark:
                    return EditorGUIUtility.TrIconContent(k_WhiteCheckMarkIconName, tooltip);
                case IconType.GreenCheckMark:
                    return EditorGUIUtility.TrIconContent(k_GreenCheckMarkIconName, tooltip);
            }

            return null;
        }

        public static GUIContent GetIconWithText(IconType iconType, string displayName, string tooltip = null)
        {
            switch (iconType)
            {
                case IconType.Refresh:
                    return EditorGUIUtility.TrTextContentWithIcon(displayName, tooltip, k_RefreshIconName);
            }

            return null;
        }

        public static GUIContent GetLogLevelIcon(LogLevel logLevel, string tooltip = null)
        {
            switch (logLevel)
            {
                case LogLevel.Info:
                    return GetIcon(IconType.Info, tooltip);
                case LogLevel.Warning:
                    return GetIcon(IconType.Warning, tooltip);
                case LogLevel.Error:
                    return GetIcon(IconType.Error, tooltip);
                default:
                    return GetIcon(IconType.Help, tooltip);
            }
        }

        public static GUIContent GetSeverityIcon(Severity severity)
        {
            switch (severity)
            {
                case Severity.Error:
                    return GetIcon(IconType.Error);
                case Severity.Critical:
                    return GetIcon(IconType.Critical);
                case Severity.Major:
                    return GetIcon(IconType.Major);
                case Severity.Moderate:
                case Severity.Default:
                    return GetIcon(IconType.Moderate);
                default:
                    return GetIcon(IconType.Minor);
            }
        }

        public static GUIContent GetSeverityIconWithText(Severity severity)
        {
            switch (severity)
            {
                case Severity.Minor:
                    if (s_MinorIcon == null)
                        s_MinorIcon = LoadIcon(k_MinorIconName);
                    return EditorGUIUtility.TrTextContentWithIcon("Minor", s_MinorIcon);
                case Severity.Moderate:
                case Severity.Default:
                    if (s_ModerateIcon == null)
                        s_ModerateIcon = LoadIcon(k_ModerateIconName);
                    return EditorGUIUtility.TrTextContentWithIcon("Moderate", s_ModerateIcon);
                case Severity.Major:
                    if (s_MajorIcon == null)
                        s_MajorIcon = LoadIcon(k_MajorIconName);
                    return EditorGUIUtility.TrTextContentWithIcon("Major", s_MajorIcon);
                case Severity.Critical:
                    if (s_CriticalIcon == null)
                        s_CriticalIcon = LoadIcon(k_CriticalIconName);
                    return EditorGUIUtility.TrTextContentWithIcon("Critical", s_CriticalIcon);
                case Severity.Error:
                    return EditorGUIUtility.TrTextContentWithIcon("Error", k_ErrorIconName);
                default:
                    return EditorGUIUtility.TrTextContentWithIcon("Unknown", MessageType.None);
            }
        }

        public static GUIContent GetSeverityIconWithCustomText(Severity severity, string text)
        {
            switch (severity)
            {
                case Severity.Minor:
                    if (s_MinorIcon == null)
                        s_MinorIcon = LoadIcon(k_MinorIconName);
                    return EditorGUIUtility.TrTextContentWithIcon(text, s_MinorIcon);
                case Severity.Moderate:
                case Severity.Default:
                    if (s_ModerateIcon == null)
                        s_ModerateIcon = LoadIcon(k_ModerateIconName);
                    return EditorGUIUtility.TrTextContentWithIcon(text, s_ModerateIcon);
                case Severity.Major:
                    if (s_MajorIcon == null)
                        s_MajorIcon = LoadIcon(k_MajorIconName);
                    return EditorGUIUtility.TrTextContentWithIcon(text, s_MajorIcon);
                case Severity.Critical:
                    if (s_CriticalIcon == null)
                        s_CriticalIcon = LoadIcon(k_CriticalIconName);
                    return EditorGUIUtility.TrTextContentWithIcon(text, s_CriticalIcon);
                case Severity.Error:
                    return EditorGUIUtility.TrTextContentWithIcon(text, k_ErrorIconName);
                default:
                    return EditorGUIUtility.TrTextContentWithIcon("Unknown", MessageType.None);
            }
        }

        public static int GetStatusWheelFrame()
        {
            return (int)Mathf.Repeat(Time.realtimeSinceStartup * 10, 11.99f);
        }

        static GUIContent GetStatusWheel()
        {
            if (s_StatusWheel == null)
            {
                s_StatusWheel = new GUIContent[12];
                for (int i = 0; i < 12; i++)
                    s_StatusWheel[i] = EditorGUIUtility.IconContent("WaitSpin" + i.ToString("00"));
            }

            int frame = GetStatusWheelFrame();
            return s_StatusWheel[frame];
        }

        public static GUIContent GetTextContentWithAssetIcon(string displayName, string assetPath)
        {
            var icon = AssetDatabase.GetCachedIcon(assetPath);
            return EditorGUIUtility.TrTextContentWithIcon(displayName, assetPath, icon);
        }

        static Texture2D LoadIcon(string iconName, string darkModePrefix = "")
        {
            if (SharedStyles.IsDarkMode)
                iconName = darkModePrefix + iconName;
            return EditorResources.Load<Texture2D>($"{ProjectAuditor.s_DataPath}/Icons/{iconName}.png");
        }

        public static Texture2D MakeColorTexture(Color col)
        {
            var pix = new Color[1];
            pix[0] = col;

            var result = new Texture2D(1, 1);
            result.SetPixels(pix);
            result.Apply();

            return result;
        }

        // A quick and dirty way to get a rough width of a string, in comparison to other strings that also get passed to this method.
        // Used to find the widest string in a column. Pass that string to GetWidth_SlowButAccurate to get an actual width that includes kerning.
        public static float EstimateWidth(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            if (s_LetterWidths == null)
                s_LetterWidths = new byte[256];

            var style = GUI.skin.box;
            int totalWidth = 0;
            int len = text.Length;
            for (int i = 0; i < len; ++i)
            {
                var currChar = text[i];
                // Yes, we are crunching a 16-bit Unicode character down to a single byte, and will likely end up with
                // the wrong widths for non-English characters as a result. Why? Because the error will probably be
                // comparatively small, and because we want s_LetterWidths to fit into a cache-friendly 64 bytes rather than
                // a whole 16KB. We're in an extremely hot code path here, and speed is more important than accuracy.
                var charByte = (byte)currChar;
                byte charWidth = s_LetterWidths[charByte];
                if (charWidth == 0)
                {
                    var content = new GUIContent(currChar.ToString());
                    charWidth = (byte)((int)style.CalcSize(content).x);
                    s_LetterWidths[charByte] = charWidth;
                }

                totalWidth += charWidth;
            }

            return totalWidth;
        }

        public static float GetWidth_SlowButAccurate(string text, int fontSize)
        {
            if (s_Style == null)
                s_Style = EditorStyles.label;
            s_Style.fontSize = fontSize;

            var content = GetTempContent(text);
            var width = s_Style.CalcSize(content).x;
            return width;
        }

        private static GUIContent GetTempContent(string text)
        {
            if (s_TempContent == null)
                s_TempContent = new GUIContent();
            s_TempContent.text = text;
            return s_TempContent;
        }

        public static bool IsInternalDocsLink(string url)
        {
            return url.Contains("docs.unity3d.com", StringComparison.Ordinal);
        }

        // Transforms versionless https://docs.unity3d.com/Manual/... and .../ScriptReference/... URLs to
        // include the current editor version, matching how the rest of the editor constructs help links.
        public static string GetVersionedDocsUrl(string url)
        {
            const string baseUrl = "https://docs.unity3d.com/";
            if (!url.StartsWith(baseUrl, StringComparison.Ordinal))
                return url;

            var path = url.Substring(baseUrl.Length);

            // Leave already-versioned URLs ("6000.0/...") and package URLs ("Packages/...") unchanged.
            if (path.Length > 0 && (char.IsDigit(path[0]) || path.StartsWith("Packages/", StringComparison.Ordinal)))
                return url;

            var version = InternalEditorUtility.GetUnityVersion();
            return $"{baseUrl}{version.Major}.{version.Minor}/Documentation/{path}";
        }

        public static int VersionToInt(string version)
        {
            var parts = version.Split('.');
            return int.Parse(parts[0]) * 100 + int.Parse(parts[1]); // Just any integer that can be used for comparison
        }
    }
}
