// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class OverlayCanvasSettingsAttribute : Attribute
    {
        internal const string invalidColorFormatError = "Invalid color format in OverlayCanvasSettings. Color array should have 3 or 4 elements, it currently has {0}.";

        public float[] defaultColor { get; set; } = null;
        public float[] defaultColorLight { get; set; } = null;
        public float[] defaultColorDark { get; set; } = null;
        public DynamicPanelBehavior dynamicPanelBehavior { get; set; } = DynamicPanelBehavior.None;
        public bool allowDynamicPanelBehaviorChanges { get; set; } = true;

        internal bool TryGetDefaultColor(out Color color)
        {
            var themeColor = EditorGUIUtility.isProSkin ? defaultColorDark : defaultColorLight;
            if (themeColor != null && themeColor.Length > 0)
            {
                return TryCastToColor(themeColor, out color);
            }

            if (defaultColor != null && defaultColor.Length > 0)
            {
                return TryCastToColor(defaultColor, out color);
            }

            color = default;
            return false;
        }

        internal static bool TryCastToColor(float[] raw, out Color color)
        {
            if (raw.Length != 3 && raw.Length != 4)
            {
                Debug.LogErrorFormat(invalidColorFormatError, raw.Length);
                color = default;
                return false;
            }

            color = new Color(raw[0], raw[1], raw[2], raw.Length > 3 ? raw[3] : 1);
            return true;
        }
    }

    // An overlay background color preference is expected to persist across sessions and domain reloads, but it will NOT persist across theme
    // changes, since the overlay text color will change and likely be incompatible with the previous background color, hindering accessibility.
    // All floating (displayed OVER window) overlays are expected to take on the custom color.
    // All docked (displacing window) overlays are expected to maintain the default toolbar color, since they do not obstruct the view.
    internal class OverlayPrefs : ScriptableSingleton<OverlayPrefs>
    {
        class WindowSettings
        {
            public Type type { get; private set; }

            public Color defaultBackgroundColor { get; private set; }

            public PrefColor backgroundColor { get; private set; }

            public DynamicPanelBehavior dynamicPanelBehavior { get; private set; }
            public bool allowDynamicPanelBehaviorChanges { get; private set; }

            public bool enabled
            {
                get => EditorPrefs.GetBool($"OverlayEnabled.{type.AssemblyQualifiedName}", true);
                set
                {
                    EditorPrefs.SetBool($"OverlayEnabled.{type.AssemblyQualifiedName}", value);
                    enabledChanged?.Invoke(type, value);
                }
            }

            public WindowSettings(Type type, Color defaultBackgroundColor, DynamicPanelBehavior dynamicPanelBehavior, bool allowDynamicPanelBehaviorChanges)
            {
                this.dynamicPanelBehavior = dynamicPanelBehavior;
                this.allowDynamicPanelBehaviorChanges = allowDynamicPanelBehaviorChanges;
                this.type = type;
                this.defaultBackgroundColor = defaultBackgroundColor;
                backgroundColor = new PrefColor(k_BackgroundColorPrefKey + type.Name,
                    defaultBackgroundColor.r,
                    defaultBackgroundColor.g,
                    defaultBackgroundColor.b,
                    defaultBackgroundColor.a);
            }
        }

        const string k_BackgroundColorPrefKey = "Overlays Background/";
        const string k_WasProSkinPrefKey = k_BackgroundColorPrefKey + "WasProSkin";
        const float k_DefaultPopupAlpha = 0.95f;
        readonly static Color k_DefaultDarkBackgroundColor = new Color(0, 0, 0, 0.8f);
        readonly static Color k_DefaultLightBackgroundColor = new Color(182/255f, 182/255f, 182/255f, 0.9f);

        [SerializeField]
        internal StyleSheet styleSheet;
        List<Type> m_SupportedTypes = new List<Type>();
        Dictionary<Type, WindowSettings> m_Windows = new Dictionary<Type, WindowSettings>();
        HashSet<string> m_ColorPrefKeys = new HashSet<string>();
        bool m_StyleSheetDirty = false;

        public static event Action styleSheetChanged;
        public static event Action<Type, bool> enabledChanged;

        internal static Color GetDefaultColor(Type windowType)
        {
            if (instance.m_Windows.TryGetValue(windowType, out var settings))
                return settings.defaultBackgroundColor;

            return Color.pink;
        }

        public static bool GetEnabled(Type windowType)
        {
            if (instance.m_Windows.TryGetValue(windowType, out var settings))
                return settings.enabled;

            return false;
        }

        public static void SetEnabled(Type windowType, bool enabled)
        {
            if (instance.m_Windows.TryGetValue(windowType, out var settings))
                settings.enabled = enabled;
        }

        public static Color GetBackgroundColor(Type windowType)
        {
            if (instance.m_Windows.TryGetValue(windowType, out var settings))
                return settings.backgroundColor;

            return Color.pink;
        }

        public static void SetBackgroundColor(Type windowType, Color color)
        {
            if (instance.m_Windows.TryGetValue(windowType, out var settings))
            {
                settings.backgroundColor.Color = color;
                PrefSettings.Set(settings.backgroundColor.Name, settings.backgroundColor);
            }
        }

        public static void RevertToDefaultColor(Type windowType)
        {
            if (instance.m_Windows.TryGetValue(windowType, out var settings))
            {
                settings.backgroundColor.ResetToDefault();
                WriteStylesheetFromPrefs();
            }
        }

        public static bool IsDynamicPanelBehaviorChangesAllowed(Type windowType)
        {
            if (instance.m_Windows.TryGetValue(windowType, out var settings))
            {
                return settings.allowDynamicPanelBehaviorChanges;
            }

            return true;
        }

        public static DynamicPanelBehavior GetDefaultDynamicPanelBehavior(Type windowType)
        {
            if (instance.m_Windows.TryGetValue(windowType, out var settings))
            {
                return settings.dynamicPanelBehavior;
            }

            return DynamicPanelBehavior.None;
        }

        internal static string GetPreferenceCanvasClass(Type windowType)
        {
            return $"unity-overlay-canvas-{windowType.Name.ToLowerInvariant()}";
        }

        public static IEnumerable<Type> GetSupportedWindowTypes()
        {
            return instance.m_SupportedTypes;
        }

        public static void WriteStylesheetFromPrefs()
        {
            var styleSheetString = BuildOverlayStylesheetString();
            var importer = new StyleSheetImporterImpl();
            importer.Import(instance.styleSheet, styleSheetString);
            styleSheetChanged?.Invoke();
            instance.m_StyleSheetDirty = false;
        }

        static void RequestStyleSheetRebuild()
        {
            if (!instance.m_StyleSheetDirty)
            {
                instance.m_StyleSheetDirty = true;
                EditorApplication.delayCall += () =>
                {
                    WriteStylesheetFromPrefs();
                };
            }
        }

        // String building refactored for performance test.
        public static string BuildOverlayStylesheetString()
        {
            var sb = new StringBuilder();

            foreach (var type in GetSupportedWindowTypes())
            {
                var color = GetBackgroundColor(type);

                int r = Mathf.RoundToInt(color.r * 255f);
                int g = Mathf.RoundToInt(color.g * 255f);
                int b = Mathf.RoundToInt(color.b * 255f);
                float a = color.a;
                float popupAlpha = Mathf.Max(a, k_DefaultPopupAlpha); // Ensure we use the most opaque version for the popup

                var selector = GetPreferenceCanvasClass(type);

                sb.AppendLine($".{selector} {{");
                sb.AppendLine($"    --unity-overlay-background-color: rgba({r}, {g}, {b}, {a.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)});");
                sb.AppendLine($"    --unity-overlay-popup-background-color: rgba({r}, {g}, {b}, {popupAlpha.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)});");
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        void OnSettingsChanged(string key, Type type)
        {
            if (m_ColorPrefKeys.Contains(key))
                RequestStyleSheetRebuild();
        }

        public static void DeleteOverlayKey(Type windowType)
        {
            instance.m_Windows.TryGetValue(windowType, out var settings);
            EditorPrefs.DeleteKey(settings.backgroundColor.Name);
        }

        void OnEnable()
        {
            // Build supported types array
            var found = TypeCache.GetTypesDerivedFrom<ISupportsOverlays>();
            foreach (var type in found)
            {
                if (typeof(EditorWindow).IsAssignableFrom(type) && !type.IsAbstract && type != typeof(MainToolbarWindow))
                {
                    // Get default background color override (if any)
                    var attr = type.GetCustomAttribute<OverlayCanvasSettingsAttribute>();
                    var defaultBackgroundColor = EditorGUIUtility.isProSkin ? k_DefaultDarkBackgroundColor : k_DefaultLightBackgroundColor;
                    var dynamicPanelBehavior = DynamicPanelBehavior.None;
                    bool allowDynamicPanelBehaviorChanges = true;
                    if (attr != null)
                    {
                        if (attr.TryGetDefaultColor(out var color))
                            defaultBackgroundColor = color;
                        dynamicPanelBehavior = attr.dynamicPanelBehavior;
                        allowDynamicPanelBehaviorChanges = attr.allowDynamicPanelBehaviorChanges;
                    }

                    var settings = new WindowSettings(type, defaultBackgroundColor, dynamicPanelBehavior, allowDynamicPanelBehaviorChanges);
                    m_Windows.Add(type, settings);
                    m_SupportedTypes.Add(type);
                    m_ColorPrefKeys.Add(settings.backgroundColor.Name);
                }
            }

            m_SupportedTypes.Sort((a, b) => a.Name.CompareTo(b.Name));

            // Initialize Stylesheet
            if (styleSheet == null)
            {
                styleSheet = CreateInstance<StyleSheet>();
                styleSheet.name = "OverlayPreferences";
                styleSheet.hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;
                RequestStyleSheetRebuild();
            }

            PrefSettings.settingsReverted += () =>
            {
                foreach (var windowType in GetSupportedWindowTypes())
                {
                    RevertToDefaultColor(windowType);
                    DeleteOverlayKey(windowType);
                }
            };

            PrefSettings.settingChanged += OnSettingsChanged;

            if (EditorPrefs.HasKey(k_WasProSkinPrefKey) && EditorPrefs.GetBool(k_WasProSkinPrefKey) != EditorGUIUtility.isProSkin)
            {
                foreach (var windowType in GetSupportedWindowTypes())
                {
                    RevertToDefaultColor(windowType);
                    DeleteOverlayKey(windowType);
                }
            }
            EditorPrefs.SetBool(k_WasProSkinPrefKey, EditorGUIUtility.isProSkin);
        }

        void OnDisable()
        {
            PrefSettings.settingChanged -= OnSettingsChanged;
        }
    }
}

