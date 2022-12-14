// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.UIElements;

using UnityEditor.Connect;
using UnityEditor.StyleSheets;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("com.unity.quicksearch.tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.Environment.Core.Editor")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.ProceduralGraph.Editor")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.Rendering.Hybrid")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.VisualEffectGraph.Editor")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.Localization.Editor")]

namespace UnityEditor.Search
{

    static class EventModifiersExtensions
    {
        public static bool HasAny(this EventModifiers flags, EventModifiers f) => (flags & f) != 0;
        public static bool HasAll(this EventModifiers flags, EventModifiers all) => (flags & all) == all;
    }

    /// <summary>
    /// This utility class mainly contains proxy to internal API that are shared between the version in trunk and the package version.
    /// </summary>
    static class Utils
    {
        const int k_MaxRegexTimeout = 25;


        internal static readonly bool isDeveloperBuild = false;
        internal static bool runningTests { get; set; }

        struct RootDescriptor
        {
            public RootDescriptor(string root)
            {
                this.root = root;
                absPath = CleanPath(new FileInfo(root).FullName);
            }

            public string root;
            public string absPath;

            public override string ToString()
            {
                return $"{root} -> {absPath}";
            }
        }

        static RootDescriptor[] s_RootDescriptors;

        static RootDescriptor[] rootDescriptors
        {
            get { return s_RootDescriptors ?? (s_RootDescriptors = GetAssetRootFolders().Select(root => new RootDescriptor(root)).OrderByDescending(desc => desc.absPath.Length).ToArray()); }
        }

        private static UnityEngine.Object[] s_LastDraggedObjects;


        public static GUIStyle objectFieldButton
        {
            get
            {
                return EditorStyles.objectFieldButton;
            }
        }

        static Utils()
        {
            isDeveloperBuild = Unsupported.IsSourceBuild();
        }

        internal static void OpenInBrowser(string baseUrl, List<Tuple<string, string>> query = null)
        {
            var url = baseUrl;

            if (query != null)
            {
                url += "?";
                for (var i = 0; i < query.Count; ++i)
                {
                    var item = query[i];
                    url += item.Item1 + "=" + item.Item2;
                    if (i < query.Count - 1)
                    {
                        url += "&";
                    }
                }
            }

            var uri = new Uri(url);
            Process.Start(uri.AbsoluteUri);
        }

        internal static SettingsProvider[] FetchSettingsProviders()
        {
            return SettingsService.FetchSettingsProviders();
        }

        internal static string GetNameFromPath(string path)
        {
            var lastSep = path.LastIndexOf('/');
            if (lastSep == -1)
                return path;

            return path.Substring(lastSep + 1);
        }

        internal static Hash128 GetSourceAssetFileHash(string guid)
        {
            return AssetDatabase.GetSourceAssetFileHash(guid);
        }

        public static Texture2D GetAssetThumbnailFromPath(SearchContext ctx, string path)
        {
            var thumbnail = GetAssetPreviewFromGUID(ctx, AssetDatabase.AssetPathToGUID(path));
            if (thumbnail)
                return thumbnail;
            thumbnail = AssetDatabase.GetCachedIcon(path) as Texture2D;
            return thumbnail ?? InternalEditorUtility.FindIconForFile(path);
        }

        private static Texture2D GetAssetPreviewFromGUID(SearchContext ctx, string guid)
        {
            return AssetPreview.GetAssetPreviewFromGUID(guid, GetClientId(ctx));
        }

        public static Texture2D GetAssetPreviewFromPath(SearchContext ctx, string path, FetchPreviewOptions previewOptions)
        {
            return GetAssetPreviewFromPath(ctx, path, new Vector2(128, 128), previewOptions);
        }

        public static Texture2D GetAssetPreviewFromPath(SearchContext ctx, string path, Vector2 previewSize, FetchPreviewOptions previewOptions)
        {
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
            if (assetType == typeof(SceneAsset))
                return AssetDatabase.GetCachedIcon(path) as Texture2D;

            if (previewOptions.HasAny(FetchPreviewOptions.Normal))
            {
                if (assetType == typeof(AudioClip))
                    return GetAssetThumbnailFromPath(ctx, path);

                try
                {
                    var fi = new FileInfo(path);
                    if (!fi.Exists)
                        return null;
                    if (fi.Length > 16 * 1024 * 1024)
                        return GetAssetThumbnailFromPath(ctx, path);
                }
                catch
                {
                    return null;
                }
            }

            if (!typeof(Texture).IsAssignableFrom(assetType))
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex)
                    return tex;
            }

            var obj = AssetDatabase.LoadMainAssetAtPath(path);
            if (obj == null)
                return null;

            if (previewOptions.HasAny(FetchPreviewOptions.Large))
            {
                var tex = AssetPreviewUpdater.CreatePreview(obj, null, path, (int)previewSize.x, (int)previewSize.y);
                if (tex)
                    return tex;
            }

            return GetAssetPreview(ctx, obj, previewOptions) ?? AssetDatabase.GetCachedIcon(path) as Texture2D;
        }

        internal static bool HasInvalidComponent(UnityEngine.Object obj)
        {
            return PrefabUtility.HasInvalidComponent(obj);
        }

        public static int GetMainAssetInstanceID(string assetPath)
        {
            return AssetDatabase.GetMainAssetInstanceID(assetPath);
        }

        internal static GUIContent GUIContentTemp(string text, string tooltip)
        {
            return GUIContent.Temp(text, tooltip);
        }

        internal static GUIContent GUIContentTemp(string text, Texture image)
        {
            return GUIContent.Temp(text, image);
        }

        internal static GUIContent GUIContentTemp(string text)
        {
            return GUIContent.Temp(text);
        }

        internal static Texture2D GetAssetPreview(SearchContext ctx, UnityEngine.Object obj, FetchPreviewOptions previewOptions)
        {
            var preview = AssetPreview.GetAssetPreview(obj.GetInstanceID(), GetClientId(ctx));
            if (preview == null || previewOptions.HasAny(FetchPreviewOptions.Large))
            {
                var largePreview = AssetPreview.GetMiniThumbnail(obj);
                if (preview == null || (largePreview != null && largePreview.width > preview.width))
                    preview = largePreview;
            }
            return preview;
        }

        internal static bool IsEditorValid(Editor e)
        {
            return e && e.serializedObject != null && e.serializedObject.isValid;
        }

        internal static int Wrap(int index, int n)
        {
            return ((index % n) + n) % n;
        }

        internal static void SetCurrentViewWidth(float width)
        {
            EditorGUIUtility.currentViewWidth = width;
        }

        internal static void SelectObject(UnityEngine.Object obj, bool ping = false)
        {
            if (!obj)
                return;
            Selection.activeObject = obj;
            if (ping)
            {
                EditorApplication.delayCall += () =>
                {
                    EditorWindow.FocusWindowIfItsOpen(GetProjectBrowserWindowType());
                    EditorApplication.delayCall += () => EditorGUIUtility.PingObject(obj);
                };
            }
        }

        internal static UnityEngine.Object SelectAssetFromPath(string path, bool ping = false)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            SelectObject(asset, ping);
            return asset;
        }

        internal static void SetTextEditorHasFocus(TextEditor editor, bool hasFocus)
        {
            editor.m_HasFocus = hasFocus;
        }

        public static void FrameAssetFromPath(string path)
        {
            var asset = SelectAssetFromPath(path);
            if (asset != null)
            {
                EditorApplication.delayCall += () =>
                {
                    EditorWindow.FocusWindowIfItsOpen(GetProjectBrowserWindowType());
                    EditorApplication.delayCall += () => EditorGUIUtility.PingObject(asset);
                };
            }
            else
            {
                EditorUtility.RevealInFinder(path);
            }
        }

        internal static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        internal static void GetMenuItemDefaultShortcuts(List<string> outItemNames, List<string> outItemDefaultShortcuts)
        {
            Menu.GetMenuItemDefaultShortcuts(outItemNames, outItemDefaultShortcuts);
        }

        static string PrintTabs(int level)
        {
            var tabs = string.Empty;
            for (int i = 0; i < level; ++i)
                tabs += "\t";
            return tabs;
        }

        static void Append(System.Text.StringBuilder sb, string name, object v, int level, HashSet<object> _seen)
        {
            try
            {
                var vt = v?.GetType();
                if (v == null)
                    sb.AppendLine($"{PrintTabs(level)}{name}: nil");
                else if (v is UnityEngine.Object ueo)
                    sb.AppendLine($"{PrintTabs(level)}{name}: ({ueo.GetInstanceID()}) {ueo.name} [{ueo.GetType()}]");
                else if (v is string s)
                    sb.AppendLine($"{PrintTabs(level)}{name}: {s}");
                else if (vt.IsPrimitive)
                    sb.AppendLine($"{PrintTabs(level)}{name}: {v}");
                else if (v is Enum @enum)
                    sb.AppendLine($"{PrintTabs(level)}{name}: {@enum}");
                else if (v is Delegate d)
                    sb.AppendLine($"{PrintTabs(level)}{name}: {d.Method.DeclaringType.Name}.{d.Method.Name}");
                else if (v is System.Collections.ICollection coll)
                {
                    sb.AppendLine($"{PrintTabs(level)}{name} ({coll.Count}):");
                    int i = 0;
                    foreach (var e in coll)
                        Append(sb, $"[{i++}] {e?.GetType()}", e, level + 2, _seen);
                }
                else if (vt.FullName.StartsWith("System.", StringComparison.Ordinal))
                    sb.AppendLine($"{PrintTabs(level)}{name}: {v}");
                else
                    sb.AppendLine(PrintObject(name, v, level + 1, _seen));
            }
            catch (Exception ex)
            {
                sb.AppendLine($"{PrintTabs(level)}{name}: <{ex.Message}>");
            }
        }

        static bool PrintField(FieldInfo fi)
        {
            if (!fi.DeclaringType.IsSerializable)
                return false;

            return fi.GetCustomAttribute<NonSerializedAttribute>() == null;
        }

        public static string PrintObject(string label, object obj, int level = 1, HashSet<object> seen = null)
        {
            seen = seen ?? new HashSet<object>();
            if (!seen.Contains(obj))
            {
                seen.Add(obj);
                var t = obj.GetType();
                var bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var sb = new System.Text.StringBuilder();
                foreach (var item in t.GetFields(bindingAttr).Where(p => PrintField(p)))
                    Append(sb, item.Name, item.GetValue(obj), level, seen);

                var result = sb.ToString().Trim(' ', '\r', '\n');
                if (result.Length > 0)
                    result = "\r\n" + result;
                return $"{PrintTabs(level - 1)}{label} [{obj?.GetHashCode() ?? -1:X}]: {result}";
            }

            return $"{PrintTabs(level - 1)}{label}: [{obj?.GetHashCode() ?? -1:X}]";
        }

        internal static string FormatProviderList(IEnumerable<SearchProvider> providers, bool fullTimingInfo = false, bool showFetchTime = true)
        {
            return string.Join(fullTimingInfo ? "\r\n" : ", ", providers.Select(p =>
            {
                var fetchTime = p.fetchTime;
                if (fullTimingInfo)
                    return $"{p.name} ({fetchTime:0.#} ms, Enable: {p.enableTime:0.#} ms, Init: {p.loadTime:0.#} ms)";

                var avgTimeLabel = String.Empty;
                if (showFetchTime && fetchTime > 9.99)
                    avgTimeLabel = $" ({fetchTime:#} ms)";
                return $"<b>{p.name}</b>{avgTimeLabel}";
            }));
        }

        public static string FormatBytes(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return $"{Math.Sign(byteCount) * num} {suf[place]}";
        }

        internal static string ToGuid(string assetPath)
        {
            string metaFile = $"{assetPath}.meta";
            if (!File.Exists(metaFile))
                return null;

            string line;
            using (var file = new StreamReader(metaFile))
            {
                while ((line = file.ReadLine()) != null)
                {
                    if (!line.StartsWith("guid:", StringComparison.Ordinal))
                        continue;
                    return line.Substring(6);
                }
            }

            return null;
        }

        internal static Rect GetEditorMainWindowPos()
        {
            var windows = Resources.FindObjectsOfTypeAll<ContainerWindow>();
            foreach (var win in windows)
            {
                if (win.showMode == ShowMode.MainWindow)
                    return win.position;
            }

            return new Rect(0, 0, 800, 600);
        }

        internal static Rect GetCenteredWindowPosition(Rect parentWindowPosition, Vector2 size)
        {
            var pos = new Rect
            {
                x = 0, y = 0,
                width = Mathf.Min(size.x, parentWindowPosition.width * 0.90f),
                height = Mathf.Min(size.y, parentWindowPosition.height * 0.90f)
            };
            var w = (parentWindowPosition.width - pos.width) * 0.5f;
            var h = (parentWindowPosition.height - pos.height) * 0.5f;
            pos.x = parentWindowPosition.x + w;
            pos.y = parentWindowPosition.y + h;
            return pos;
        }

        internal static Type GetProjectBrowserWindowType()
        {
            return typeof(ProjectBrowser);
        }

        public static Rect GetMainWindowCenteredPosition(Vector2 size)
        {
            var mainWindowRect = GetEditorMainWindowPos();
            return GetCenteredWindowPosition(mainWindowRect, size);
        }

        internal static void ShowDropDown(this EditorWindow window, Vector2 size)
        {
            window.maxSize = window.minSize = size;
            window.position = GetMainWindowCenteredPosition(size);
            window.ShowPopup();

            var parentView = window.m_Parent;
            parentView.AddToAuxWindowList();
            parentView.window.m_DontSaveToLayout = true;
        }

        internal static string JsonSerialize(object obj)
        {
            return Json.Serialize(obj);
        }

        internal static object JsonDeserialize(string json)
        {
            return Json.Deserialize(json);
        }

        internal static int GetNumCharactersThatFitWithinWidth(GUIStyle style, string text, float width)
        {
            return style.GetNumCharactersThatFitWithinWidth(text, width);
        }

        internal static string GetNextWord(string src, ref int index)
        {
            // Skip potential white space BEFORE the actual word we are extracting
            for (; index < src.Length; ++index)
            {
                if (!char.IsWhiteSpace(src[index]))
                {
                    break;
                }
            }

            var startIndex = index;
            for (; index < src.Length; ++index)
            {
                if (char.IsWhiteSpace(src[index]))
                {
                    break;
                }
            }

            return src.Substring(startIndex, index - startIndex);
        }

        internal static int LevenshteinDistance<T>(IEnumerable<T> lhs, IEnumerable<T> rhs) where T : System.IEquatable<T>
        {
            if (lhs == null) throw new System.ArgumentNullException("lhs");
            if (rhs == null) throw new System.ArgumentNullException("rhs");

            IList<T> first = lhs as IList<T> ?? new List<T>(lhs);
            IList<T> second = rhs as IList<T> ?? new List<T>(rhs);

            int n = first.Count, m = second.Count;
            if (n == 0) return m;
            if (m == 0) return n;

            int curRow = 0, nextRow = 1;
            int[][] rows = { new int[m + 1], new int[m + 1] };
            for (int j = 0; j <= m; ++j)
                rows[curRow][j] = j;

            for (int i = 1; i <= n; ++i)
            {
                rows[nextRow][0] = i;

                for (int j = 1; j <= m; ++j)
                {
                    int dist1 = rows[curRow][j] + 1;
                    int dist2 = rows[nextRow][j - 1] + 1;
                    int dist3 = rows[curRow][j - 1] +
                        (first[i - 1].Equals(second[j - 1]) ? 0 : 1);

                    rows[nextRow][j] = System.Math.Min(dist1, System.Math.Min(dist2, dist3));
                }
                if (curRow == 0)
                {
                    curRow = 1;
                    nextRow = 0;
                }
                else
                {
                    curRow = 0;
                    nextRow = 1;
                }
            }
            return rows[curRow][m];
        }

        internal static int LevenshteinDistance(string lhs, string rhs, bool caseSensitive = true)
        {
            if (!caseSensitive)
            {
                lhs = lhs.ToLower();
                rhs = rhs.ToLower();
            }
            char[] first = lhs.ToCharArray();
            char[] second = rhs.ToCharArray();
            return LevenshteinDistance(first, second);
        }

        internal static Texture2D GetThumbnailForGameObject(GameObject go)
        {
            var thumbnail = PrefabUtility.GetIconForGameObject(go);
            if (thumbnail)
                return thumbnail;
            return EditorGUIUtility.ObjectContent(go, go.GetType()).image as Texture2D;
        }

        internal static Texture2D FindTextureForType(Type type)
        {
            if (type == null)
                return null;
            return EditorGUIUtility.FindTexture(type);
        }

        internal static Texture2D GetIconForObject(UnityEngine.Object obj)
        {
            return EditorGUIUtility.GetIconForObject(obj);
        }

        public static void PingAsset(string assetPath)
        {
            EditorGUIUtility.PingObject(AssetDatabase.GetMainAssetInstanceID(assetPath));
        }

        internal static T ConvertValue<T>(string value)
        {
            var type = typeof(T);
            var converter = TypeDescriptor.GetConverter(type);
            if (converter.IsValid(value))
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                return (T)converter.ConvertFromString(null, CultureInfo.InvariantCulture, value);
            }
            return (T)Activator.CreateInstance(type);
        }

        internal static bool TryConvertValue<T>(string value, out T convertedValue)
        {
            var type = typeof(T);
            var converter = TypeDescriptor.GetConverter(type);
            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                convertedValue = (T)converter.ConvertFromString(null, CultureInfo.InvariantCulture, value);
                return true;
            }
            catch
            {
                convertedValue = default;
                return false;
            }
        }

        public static void StartDrag(UnityEngine.Object[] objects, string label = null)
        {
            s_LastDraggedObjects = objects;
            if (s_LastDraggedObjects == null)
                return;
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = s_LastDraggedObjects;
            DragAndDrop.StartDrag(label);
        }

        public static void StartDrag(UnityEngine.Object[] objects, string[] paths, string label = null)
        {
            s_LastDraggedObjects = objects;
            if (paths == null || paths.Length == 0)
                return;
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = s_LastDraggedObjects;
            DragAndDrop.paths = paths;
            DragAndDrop.StartDrag(label);
        }

        internal static Type GetTypeFromName(string typeName)
        {
            return TypeCache.GetTypesDerivedFrom<UnityEngine.Object>().FirstOrDefault(t => string.Equals(t.Name, typeName, StringComparison.Ordinal)) ?? typeof(UnityEngine.Object);
        }

        internal static string StripHTML(string input)
        {
            return Regex.Replace(input, "<.*?>", String.Empty);
        }

        internal static UnityEngine.Object ToObject(SearchItem item, Type filterType)
        {
            if (item == null || item.provider == null)
                return null;
            return item.provider.toObject?.Invoke(item, filterType);
        }

        internal static bool IsFocusedWindowTypeName(string focusWindowName)
        {
            return EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.GetType().ToString().EndsWith("." + focusWindowName);
        }

        internal static string CleanString(string s)
        {
            var sb = s.ToCharArray();
            for (int c = 0; c < s.Length; ++c)
            {
                var ch = s[c];
                if (ch == '_' || ch == '.' || ch == '-' || ch == '/')
                    sb[c] = ' ';
            }
            return new string(sb).ToLowerInvariant();
        }

        internal static string CleanPath(string path)
        {
            return path.Replace("\\", "/");
        }

        internal static bool IsPathUnderProject(string path)
        {
            if (!Path.IsPathRooted(path))
            {
                path = new FileInfo(path).FullName;
            }
            path = CleanPath(path);
            return rootDescriptors.Any(desc => path.StartsWith(desc.absPath));
        }

        internal static string GetPathUnderProject(string path)
        {
            path = CleanPath(path);
            if (!Path.IsPathRooted(path))
            {
                return path;
            }

            foreach (var desc in rootDescriptors)
            {
                if (path.StartsWith(desc.absPath))
                {
                    var relativePath = path.Substring(desc.absPath.Length);
                    return desc.root + relativePath;
                }
            }

            return path;
        }

        private static int GetClientId(SearchContext ctx)
        {
            return ctx != null && ctx.searchView != null ? ctx.searchView.GetViewId() : 0;
        }

        internal static Texture2D GetSceneObjectPreview(SearchContext ctx, GameObject obj, Vector2 previewSize, FetchPreviewOptions options, Texture2D defaultThumbnail)
        {
            var sr = obj.GetComponent<SpriteRenderer>();
            if (sr && sr.sprite && sr.sprite.texture)
                return sr.sprite.texture;


            
            if (!options.HasAny(FetchPreviewOptions.Large))
            {
                var preview = AssetPreview.GetAssetPreview(obj.GetInstanceID(), GetClientId(ctx));
                if (preview)
                    return preview;

                if (AssetPreview.IsLoadingAssetPreview(obj.GetInstanceID()))
                    return null;
            }

            var assetPath = SearchUtils.GetHierarchyAssetPath(obj, true);
            if (string.IsNullOrEmpty(assetPath))
                return AssetPreview.GetAssetPreview(obj.GetInstanceID(), GetClientId(ctx)) ?? defaultThumbnail;
            return GetAssetPreviewFromPath(ctx, assetPath, previewSize, options);
        }

        internal static bool TryGetNumber(object value, out double number)
        {
            if (value == null)
            {
                number = double.NaN;
                return false;
            }

            if (value is string s)
            {
                if (TryParse(s, out number))
                    return true;
                else
                {
                    number = double.NaN;
                    return false;
                }
            }

            if (value.GetType().IsPrimitive || value is decimal)
            {
                number = Convert.ToDouble(value);
                return true;
            }

            return TryParse(Convert.ToString(value), out number);
        }

        internal static bool IsRunningTests()
        {
            return runningTests;
        }

        internal static bool IsMainProcess()
        {
            if (AssetDatabaseAPI.IsAssetImportWorkerProcess())
                return false;

            if (EditorUtility.isInSafeMode)
                return false;

            if (MPE.ProcessService.level != MPE.ProcessLevel.Main)
                return false;

            return true;
        }

        internal static event EditorApplication.CallbackFunction tick
        {
            add
            {
                EditorApplication.tick -= value;
                EditorApplication.tick += value;
            }
            remove
            {
                EditorApplication.tick -= value;
            }
        }

        public static Action CallAnimated(EditorApplication.CallbackFunction callback, double seconds = 0.05d)
        {
            return CallDelayed(callback, seconds);
        }

        public static Action CallDelayed(EditorApplication.CallbackFunction callback, double seconds = 0)
        {
            return EditorApplication.CallDelayed(callback, seconds);
        }

        internal static void SetFirstInspectedEditor(Editor editor)
        {
            editor.firstInspectedEditor = true;
        }

        public static GUIStyle FromUSS(string name)
        {
            return GUIStyleExtensions.FromUSS(GUIStyle.none, name);
        }

        public static GUIStyle FromUSS(GUIStyle @base, string name)
        {
            return GUIStyleExtensions.FromUSS(@base, name);
        }

        internal static bool HasCurrentWindowKeyFocus()
        {
            return EditorGUIUtility.HasCurrentWindowKeyFocus();
        }

        internal static void AddStyleSheet(VisualElement rootVisualElement, string ussFileName)
        {
            rootVisualElement.AddStyleSheetPath($"StyleSheets/QuickSearch/{ussFileName}");
        }

        internal static InspectorWindowUtils.LayoutGroupChecker LayoutGroupChecker()
        {
            return new InspectorWindowUtils.LayoutGroupChecker();
        }



        internal static string GetConnectAccessToken()
        {
            return UnityConnect.instance.GetAccessToken();
        }

        internal static string GetPackagesKey()
        {
            return UnityConnect.instance.GetConfigurationURL(CloudConfigUrl.CloudPackagesKey);
        }

        internal static void OpenPackageManager(string packageName)
        {
            PackageManager.UI.PackageManagerWindow.SelectPackageAndFilterStatic(packageName, PackageManager.UI.Internal.PackageFilterTab.AssetStore);
        }

        internal static char FastToLower(char c)
        {
            // ASCII non-letter characters and
            // lower case letters.
            if (c < 'A' || (c > 'Z' && c <= 'z'))
            {
                return c;
            }

            if (c >= 'A' && c <= 'Z')
            {
                return (char)(c + 32);
            }

            return Char.ToLower(c, CultureInfo.InvariantCulture);
        }

        internal static string FastToLower(string str)
        {
            int length = str.Length;

            var chars = new char[length];

            for (int i = 0; i < length; ++i)
            {
                chars[i] = FastToLower(str[i]);
            }

            return new string(chars);
        }

        public static string FormatCount(ulong count)
        {
            if (count < 1000U)
                return count.ToString(CultureInfo.InvariantCulture.NumberFormat);
            if (count < 1000000U)
                return (count / 1000U).ToString(CultureInfo.InvariantCulture.NumberFormat) + "k";
            if (count < 1000000000U)
                return (count / 1000000U).ToString(CultureInfo.InvariantCulture.NumberFormat) + "M";
            return (count / 1000000000U).ToString(CultureInfo.InvariantCulture.NumberFormat) + "G";
        }

        internal static bool TryAdd<K, V>(this Dictionary<K, V> dict, K key, V value)
        {
            if (!dict.ContainsKey(key))
            {
                dict.Add(key, value);
                return true;
            }

            return false;
        }

        internal static string[] GetAssetRootFolders()
        {
            return AssetDatabase.GetAssetRootFolders();
        }

        internal static string ToString(in Vector3 v)
        {
            return $"({FormatFloatString(v.x)},{FormatFloatString(v.y)},{FormatFloatString(v.z)})";
        }

        internal static string ToString(in Vector4 v, int dim)
        {
            switch (dim)
            {
                case 2: return $"({FormatFloatString(v.x)},{FormatFloatString(v.y)})";
                case 3: return $"({FormatFloatString(v.x)},{FormatFloatString(v.y)},{FormatFloatString(v.z)})";
                case 4: return $"({FormatFloatString(v.x)},{FormatFloatString(v.y)},{FormatFloatString(v.z)},{FormatFloatString(v.w)})";
            }
            return null;
        }

        internal static string ToString(in Vector2Int v)
        {
            return $"({(int.MaxValue == v.x ? string.Empty : v.x.ToString())},{(int.MaxValue == v.y ? string.Empty : v.y.ToString())})";
        }

        internal static string ToString(in Vector3Int v)
        {
            return $"({(int.MaxValue == v.x ? string.Empty : v.x.ToString())},{(int.MaxValue == v.y ? string.Empty : v.y.ToString())},{(int.MaxValue == v.z ? string.Empty : v.z.ToString())})";
        }

        internal static string FormatFloatString(in float f)
        {
            if (float.IsNaN(f))
                return string.Empty;
            return f.ToString(CultureInfo.InvariantCulture);
        }

        internal static bool TryParseVectorValue(in object value, out Vector4 vc, out int dim)
        {
            dim = 0;
            vc = new Vector4(float.NaN, float.NaN, float.NaN, float.NaN);
            if (!(value is string arg))
                return false;
            if (arg.Length < 3 || arg[0] != '(' || arg[arg.Length - 1] != ')' || arg.IndexOf(',') == -1)
                return false;
            var ves = arg.Substring(1, arg.Length - 2);
            var values = ves.Split(',');
            if (values.Length < 2 || values.Length > 4)
                return false;

            dim = values.Length;
            if (values.Length >= 1 && values[0].Length > 0 && (values[0].Length > 1 || values[0][0] != '-') && TryParse<float>(values[0], out var f))
                vc.x = f;
            if (values.Length >= 2 && values[1].Length > 0 && (values[1].Length > 1 || values[1][0] != '-') && TryParse(values[1], out f))
                vc.y = f;
            if (values.Length >= 3 && values[2].Length > 0 && (values[2].Length > 1 || values[2][0] != '-') && TryParse(values[2], out f))
                vc.z = f;
            if (values.Length >= 4 && values[3].Length > 0 && (values[3].Length > 1 || values[3][0] != '-') && TryParse(values[3], out f))
                vc.w = f;

            return true;
        }

        public static bool TryParse<T>(string expression, out T result, bool supportNamedNumber = true)
        {
            expression = expression.Replace(',', '.');
            expression = expression.TrimEnd('f');
            expression = expression.ToLowerInvariant();

            bool success = false;
            result = default;
            if (typeof(T) == typeof(float))
            {
                if (supportNamedNumber && expression == "pi")
                {
                    success = true;
                    result = (T)(object)(float)Math.PI;
                }
                else
                {
                    success = float.TryParse(expression, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out var temp);
                    result = (T)(object)temp;
                }
            }
            else if (typeof(T) == typeof(int))
            {
                success = int.TryParse(expression, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out var temp);
                result = (T)(object)temp;
            }
            else if (typeof(T) == typeof(uint))
            {
                success = uint.TryParse(expression, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out var temp);
                result = (T)(object)temp;
            }
            else if (typeof(T) == typeof(double))
            {
                if (supportNamedNumber && expression == "pi")
                {
                    success = true;
                    result = (T)(object)Math.PI;
                }
                else
                {
                    success = double.TryParse(expression, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out var temp);
                    result = (T)(object)temp;
                }
            }
            else if (typeof(T) == typeof(long))
            {
                success = long.TryParse(expression, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out var temp);
                result = (T)(object)temp;
            }
            else if (typeof(T) == typeof(ulong))
            {
                success = ulong.TryParse(expression, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out var temp);
                result = (T)(object)temp;
            }
            return success;
        }

        private const string k_RevealInFinderLabel = "Open Containing Folder";
        internal static string GetRevealInFinderLabel() { return k_RevealInFinderLabel; }

        public static string TrimText(string text)
        {
            return text.Trim().Replace("\n", " ");
        }

        public static string TrimText(string text, int maxLength)
        {
            text = TrimText(text);
            if (text.Length > maxLength)
            {
                text = Utils.StripHTML(text);
                text = text.Substring(0, Math.Min(text.Length, maxLength) - 1) + "\u2026";
            }
            return text;
        }

        public static ulong GetHashCode64(this string strText)
        {
            if (string.IsNullOrEmpty(strText))
                return 0;
            var s1 = (ulong)strText.Substring(0, strText.Length / 2).GetHashCode();
            var s2 = (ulong)strText.Substring(strText.Length / 2).GetHashCode();
            return s1 << 32 | s2;
        }

        public static string RemoveInvalidCharsFromPath(string path, char repl = '/')
        {
            var invalidChars = Path.GetInvalidPathChars();
            foreach (var c in invalidChars)
                path = path.Replace(c, repl);
            return path;
        }

        public static Rect BeginHorizontal(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
        {
            return EditorGUILayout.BeginHorizontal(content, style, options);
        }

        public static bool IsGUIClipEnabled()
        {
            return GUIClip.enabled;
        }

        public static Rect Unclip(in Rect r)
        {
            return GUIClip.Unclip(r);
        }

        public static MonoScript MonoScriptFromScriptedObject(UnityEngine.Object obj)
        {
            return MonoScript.FromScriptedObject(obj);
        }

        public static bool SerializedPropertyIsScript(SerializedProperty property)
        {
            return property.isScript;
        }

        public static string SerializedPropertyObjectReferenceStringValue(SerializedProperty property)
        {
            return property.objectReferenceStringValue;
        }

        public static GUIContent ObjectContent(UnityEngine.Object obj, Type type, int instanceID)
        {
            return EditorGUIUtility.ObjectContent(obj, type, instanceID);
        }

        public static bool IsCommandDelete(string commandName)
        {
            return commandName == EventCommandNames.Delete || commandName == EventCommandNames.SoftDelete;
        }

        public static void PopupWindowWithoutFocus(Rect position, PopupWindowContent windowContent)
        {
            UnityEditor.PopupWindowWithoutFocus.Show(
                position,
                windowContent,
                new[] { UnityEditor.PopupLocation.Left, UnityEditor.PopupLocation.Below, UnityEditor.PopupLocation.Right });
        }

        public static void OpenPropertyEditor(UnityEngine.Object target)
        {
            PropertyEditor.OpenPropertyEditor(target);
        }

        public static bool MainActionKeyForControl(Event evt, int id)
        {
            return evt.MainActionKeyForControl(id);
        }


        public static bool IsNavigationKey(in Event evt)
        {
            if (!evt.isKey)
                return false;

            switch (evt.keyCode)
            {
                case KeyCode.UpArrow:
                case KeyCode.DownArrow:
                case KeyCode.LeftArrow:
                case KeyCode.RightArrow:
                case KeyCode.Home:
                case KeyCode.End:
                case KeyCode.PageUp:
                case KeyCode.PageDown:
                    return true;
            }

            return false;
        }

        public static Texture2D LoadIcon(string name)
        {
            return EditorGUIUtility.LoadIcon(name);
        }

        public static string GetIconSkinAgnosticName(Texture2D icon)
        {
            if (icon == null)
                return null;
            return GetIconSkinAgnosticName(icon.name);
        }

        public static string GetIconSkinAgnosticName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var oldName = Path.GetFileName(name);
            var dirName = Path.GetDirectoryName(name);
            var newName = oldName.StartsWith("d_") ? oldName.Substring(2) : oldName;
            if (!string.IsNullOrEmpty(dirName))
                newName = $"{dirName}/{newName}";
            return newName;
        }

        public static ulong GetFileIDHint(in UnityEngine.Object obj)
        {
            return Unsupported.GetFileIDHint(obj);
        }

        public static bool IsEditingTextField()
        {
            return GUIUtility.textFieldInput || EditorGUI.IsEditingTextField();
        }

        static readonly Regex trimmer = new Regex(@"(\s\s+)|(\r\n|\r|\n)+");
        public static string Simplify(string text)
        {
            return trimmer.Replace(text, " ").Replace("\r\n", " ").Replace('\n', ' ');
        }

        public static void OpenGraphViewer(in string searchQuery)
        {
        }

        internal static void WriteTextFileToDisk(in string path, in string content)
        {
            FileUtil.WriteTextFileToDisk(path, content);
        }

        internal static bool ParseRx(string pattern, bool exact, out Regex rx)
        {
            try
            {
                rx = new Regex(!exact ? pattern : $"^{pattern}$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(k_MaxRegexTimeout));
            }
            catch (ArgumentException)
            {
                rx = null;
                return false;
            }

            return true;
        }

        internal static bool ParseGlob(string pattern, bool exact, out Regex rx)
        {
            try
            {
                pattern = Regex.Escape(RemoveDuplicateAdjacentCharacters(pattern, '*')).Replace(@"\*", ".*").Replace(@"\?", ".");
                rx = new Regex(!exact ? pattern : $"^{pattern}$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(k_MaxRegexTimeout));
            }
            catch (ArgumentException)
            {
                rx = null;
                return false;
            }

            return true;
        }

        static string RemoveDuplicateAdjacentCharacters(string pattern, char c)
        {
            for (int i = pattern.Length - 1; i >= 0; --i)
            {
                if (pattern[i] != c || i == 0)
                    continue;

                if (pattern[i - 1] == c)
                    pattern = pattern.Remove(i, 1);
            }

            return pattern;
        }

        internal static T GetAttribute<T>(this MethodInfo mi) where T : System.Attribute
        {
            var attrs = mi.GetCustomAttributes(typeof(T), false);
            if (attrs == null || attrs.Length == 0)
                return null;
            return attrs[0] as T;
        }

        internal static T GetAttribute<T>(this Type mi) where T : System.Attribute
        {
            var attrs = mi.GetCustomAttributes(typeof(T), false);
            if (attrs == null || attrs.Length == 0)
                return null;
            return attrs[0] as T;
        }

        internal static bool IsBuiltInResource(UnityEngine.Object obj)
        {
            var resPath = AssetDatabase.GetAssetPath(obj);
            return IsBuiltInResource(resPath);
        }

        internal static bool IsBuiltInResource(in string resPath)
        {
            return string.Equals(resPath, "Library/unity editor resources", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(resPath, "resources/unity_builtin_extra", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(resPath, "library/unity default resources", StringComparison.OrdinalIgnoreCase);
        }

        public static int CombineHashCodes(params int[] hashCodes)
        {
            int hash1 = (5381 << 16) + 5381;
            int hash2 = hash1;

            int i = 0;
            foreach (var hashCode in hashCodes)
            {
                if (i % 2 == 0)
                    hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ hashCode;
                else
                    hash2 = ((hash2 << 5) + hash2 + (hash2 >> 27)) ^ hashCode;

                ++i;
            }

            return hash1 + (hash2 * 1566083941);
        }
    }

    static class SerializedPropertyExtension
    {
        readonly struct Cache : IEquatable<Cache>
        {
            readonly Type host;
            readonly string path;

            public Cache(Type host, string path)
            {
                this.host = host;
                this.path = path;
            }

            public bool Equals(Cache other)
            {
                return Equals(host, other.host) && string.Equals(path, other.path, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;
                return obj is Cache && Equals((Cache)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((host != null ? host.GetHashCode() : 0) * 397) ^ (path != null ? path.GetHashCode() : 0);
                }
            }
        }

        class MemberInfoCache
        {
            public MemberInfo fieldInfo;
            public Type type;
        }

        static Type s_NativePropertyAttributeType;
        static Dictionary<Cache, MemberInfoCache> s_MemberInfoFromPropertyPathCache = new Dictionary<Cache, MemberInfoCache>();

        public static Type GetManagedType(this SerializedProperty property)
        {
            var host = property.serializedObject?.targetObject?.GetType();
            if (host == null)
                return null;

            var path = property.propertyPath;
            var cache = new Cache(host, path);

            if (s_MemberInfoFromPropertyPathCache.TryGetValue(cache, out var infoCache))
                return infoCache?.type;

            const string arrayData = @"\.Array\.data\[[0-9]+\]";
            // we are looking for array element only when the path ends with Array.data[x]
            var lookingForArrayElement = Regex.IsMatch(path, arrayData + "$");
            // remove any Array.data[x] from the path because it is prevents cache searching.
            path = Regex.Replace(path, arrayData, ".___ArrayElement___");

            MemberInfo memberInfo = null;
            var type = host;
            string[] parts = path.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                string member = parts[i];
                string alternateName = null;
                if (member.StartsWith("m_", StringComparison.Ordinal))
                    alternateName = member.Substring(2);

                foreach (MemberInfo f in type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if ((f.MemberType & (MemberTypes.Property | MemberTypes.Field)) == 0)
                        continue;
                    var memberName = f.Name;
                    if (f is PropertyInfo pi)
                    {
                        if (!pi.CanRead)
                            continue;

                        s_NativePropertyAttributeType = typeof(UnityEngine.Bindings.NativePropertyAttribute);
                        var nattr = pi.GetCustomAttribute(s_NativePropertyAttributeType);
                        if (nattr != null)
                            memberName = s_NativePropertyAttributeType.GetProperty("Name").GetValue(nattr) as string ?? string.Empty;
                    }
                    if (string.Equals(member, memberName, StringComparison.Ordinal) ||
                        (alternateName != null && string.Equals(alternateName, memberName, StringComparison.OrdinalIgnoreCase)))
                    {
                        memberInfo = f;
                        break;
                    }
                }

                if (memberInfo is FieldInfo fi)
                    type = fi.FieldType;
                else if (memberInfo is PropertyInfo pi)
                    type = pi.PropertyType;
                else
                    continue;

                // we want to get the element type if we are looking for Array.data[x]
                if (i < parts.Length - 1 && parts[i + 1] == "___ArrayElement___" && type.IsArrayOrList())
                {
                    i++; // skip the "___ArrayElement___" part
                    type = type.GetArrayOrListElementType();
                }
            }

            if (memberInfo == null)
            {
                s_MemberInfoFromPropertyPathCache.Add(cache, null);
                return null;
            }

            // we want to get the element type if we are looking for Array.data[x]
            if (lookingForArrayElement && type != null && type.IsArrayOrList())
                type = type.GetArrayOrListElementType();

            s_MemberInfoFromPropertyPathCache.Add(cache, new MemberInfoCache
            {
                type = type,
                fieldInfo = memberInfo
            });
            return type;
        }
    }

}
