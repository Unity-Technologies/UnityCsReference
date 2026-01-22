// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    public interface ISupportsOverlays {}

    interface ISupportsOverlaysCustomMode : ISupportsOverlays
    {
        OverlayCanvasMode overlayCanvasMode { get; }
    }

    public interface ISupportsOverlaysWithFilter : ISupportsOverlays
    {
        bool IsOverlaySupported(string overlayId);
    }

    static class OverlayUtilities
    {
        internal const string k_StyleCommon = "StyleSheets/Overlays/OverlayCommon.uss";
        internal const string k_StyleLight = "StyleSheets/Overlays/OverlayLight.uss";
        internal const string k_StyleDark = "StyleSheets/Overlays/OverlayDark.uss";

        internal class OverlayEditorWindowAssociation
        {
            public Type overlay;
            public Type editorWindowType;
            public string overlayId;
        }

        static OverlayEditorWindowAssociation[] s_Overlays;
        static readonly Dictionary<Type, List<Type>> s_OverlaysTypeAssociations = new Dictionary<Type, List<Type>>();
        internal const string nullWindowTypeErrorMsg = "{0} editor window type cannot be null.";
        // used by tests
        internal const string invalidWindowErrorMsg = "Overlay {0}'s window type {1} does not support overlays. The attribute's window must inherit from EditorWindow and implement ISupportsOverlays.";

        const float k_ClampOffset = 0.001f; //Used to make sure we're not clamping exactly on the bounds (which was causing issues)

        static OverlayEditorWindowAssociation[] overlays
        {
            get
            {
                if (s_Overlays == null)
                {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    Type[] ovrls = TypeCache.GetTypesWithAttribute<OverlayAttribute>()
#pragma warning restore RS0030
                        .Where(x => !x.IsAbstract)
                        .ToArray();

                    int len = ovrls.Length;
                    var overlayWindows = new List<OverlayEditorWindowAssociation>(len);

                    for (int i = 0; i < len; i++)
                    {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        var overlayAttribute = (OverlayAttribute)ovrls[i].GetCustomAttributes(typeof(OverlayAttribute), false).FirstOrDefault();
#pragma warning restore RS0030

                        // Overlays that are implemented as instances don't need to define a target editor window.
                        if (overlayAttribute?.editorWindowType == null)
                            continue;

                        if (!IsOverlayWindowValid(overlayAttribute))
                        {
                            Debug.LogErrorFormat(invalidWindowErrorMsg, overlayAttribute.displayName, overlayAttribute.editorWindowType);
                            continue;
                        }

                        overlayWindows.Add(new OverlayEditorWindowAssociation
                        {
                            overlay = ovrls[i],
                            editorWindowType = overlayAttribute.editorWindowType,
                            overlayId = overlayAttribute.id
                        });
                    }

                    s_Overlays = overlayWindows.ToArray();
                }

                return s_Overlays;
            }
        }

        // This ensures that the rect is at least partly contained within the boundary. Use ClampRectToRect if the rect needs to be fully within.
        public static Rect EnsureRectOverlapsRect(Rect rect, Rect boundary)
        {
            if (rect.x > boundary.xMax)
                rect.x = boundary.xMax;

            if (rect.xMax < boundary.xMin)
                rect.x = (boundary.xMin) - rect.width;

            if (rect.y > boundary.yMax)
                rect.y = boundary.yMax;

            if (rect.y < boundary.yMin)
                rect.y = boundary.yMin;

            return rect;
        }

        public static Rect ClampRectToRect(Rect rect, Rect clampingRect)
        {
            rect.position -= rect.max - ClampPositionToRect(rect.max, clampingRect);
            rect.position = ClampPositionToRect(rect.position, clampingRect);
            return rect;
        }

        public static Vector2 ClampPositionToRect(Vector2 position, Rect clampingRect)
        {
            //keep mouse position within bounds for picking, mathf.epsilon is too small
            position.x = Mathf.Clamp(position.x, clampingRect.xMin + k_ClampOffset,
                clampingRect.xMax - k_ClampOffset);
            position.y = Mathf.Clamp(position.y, clampingRect.yMin + k_ClampOffset,
                clampingRect.yMax - k_ClampOffset);
            return position;
        }

        public static bool IsResizable(Overlay overlay)
        {
            bool canUserResize = overlay.IsResizeCompatible() && overlay.container.resizingAllowed;
            canUserResize &= !Mathf.Approximately(overlay.minSize.x, overlay.maxSize.x);
            canUserResize &= !Mathf.Approximately(overlay.minSize.y, overlay.maxSize.y);
            return canUserResize;
        }

        static bool OverlayWindowTypeEquatesTo(Type windowType, Type overlayWindowType)
        {
            return overlayWindowType != null && overlayWindowType.IsAssignableFrom(windowType);
        }

        internal static List<Type> GetOverlaysForType(Type windowType, Func<string, bool> filter = null)
        {
            List<Type> res;

            if (s_OverlaysTypeAssociations.TryGetValue(windowType, out res))
                return res;

            s_OverlaysTypeAssociations.Add(windowType, res = new List<Type>());

            for (int i = 0, c = overlays.Length; i < c; i++)
            {
                var shouldAddOverlayType = filter == null ?
                    OverlayWindowTypeEquatesTo(windowType, overlays[i].editorWindowType) :
                    OverlayWindowTypeEquatesTo(windowType, overlays[i].editorWindowType) && filter(overlays[i].overlayId);
                if (shouldAddOverlayType)
                    res.Add(overlays[i].overlay);
            }

            return res;
        }

        public static OverlayAttribute GetAttribute(Type window, Type overlay)
        {
            return overlay.GetCustomAttribute<OverlayAttribute>() ?? new OverlayAttribute(window, overlay.Name);
        }

        public static Overlay CreateOverlay(Type type)
        {
            Overlay overlay = null;

            // Reflection is super slow, avoid it if this this a known type.
            if (type == typeof(EditorToolSettingsOverlay))
                overlay = new EditorToolSettingsOverlay();
            else if (type == typeof(SearchToolBar))
                overlay = new SearchToolBar();
            else if (type == typeof(TransformToolsOverlayToolBar))
                overlay = new TransformToolsOverlayToolBar();
            else if (type == typeof(SceneViewToolBar))
                overlay = new SceneViewToolBar();
            else if (type == typeof(SceneViewCameraModeToolbar))
                overlay = new SceneViewCameraModeToolbar();
            else if (type == typeof(GridAndSnapToolBar))
                overlay = new GridAndSnapToolBar();
            else
            {
                var ctor = type.GetConstructor(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    Type.EmptyTypes, null);

                overlay = ctor?.Invoke(null) as Overlay;

                if (overlay == null)
                {
                    Debug.LogWarning($"Overlay of type {type} can not be instantiated. Make sure this type contains a " +
                        "parameter-less constructor and inherits the Overlay type.");
                    return null;
                }
            }

            overlay.Initialize(GetAttribute(null, type));
            return overlay;
        }

        internal static bool GetIsDefaultDisplayFromAttribute(Type type)
        {
            if (Attribute.IsDefined(type, typeof(OverlayAttribute)))
            {
                var attributes = type.GetCustomAttributes(typeof(OverlayAttribute), true);
                for (int i = 0, c = attributes.Length; i < c; i++)
                    if (attributes[i] is OverlayAttribute)
                        return ((OverlayAttribute)attributes[i]).defaultDisplay;
            }
            return false;
        }

        internal static string GetDisplayNameFromAttribute(Type type)
        {
            if (Attribute.IsDefined(type, typeof(OverlayAttribute)))
            {
                var attributes = type.GetCustomAttributes(typeof(OverlayAttribute), true);
                for (int i = 0, c = attributes.Length; i < c; i++)
                    if (attributes[i] is OverlayAttribute)
                        return ((OverlayAttribute)attributes[i]).displayName;
            }
            return string.Empty;
        }

        internal static string GetSignificantLettersForIcon(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            var folders = s.Split('/');
            var last = folders[folders.Length - 1];
            if (string.IsNullOrEmpty(last))
                return string.Empty;

            var words = last.Trim().Split(' ');
            if (words.Length == 1)
            {
                var regex = new Regex(@"[A-Z][^A-Z]*", RegexOptions.Compiled);
                var matches = regex.Matches(words[0]);
                if (matches == null || matches.Count == 0)
                    return words[0].Length > 1 ? words[0].Substring(0, 2) : words[0][0].ToString();

                if (matches.Count == 1)
                    return matches[0].Length > 1 ? matches[0].Value.Substring(0, 2) : matches[0].Value[0].ToString();

                return matches[0].Value.Substring(0, 1) + matches[1].Value.Substring(0, 1);
            }

            return words[0].Substring(0, 1) + words[1].Substring(0, 1);
        }

        internal static void ValidateName(Overlay overlay)
        {
            if (overlay == null || !string.IsNullOrEmpty(overlay.displayName))
                return;
            overlay.displayName = $"{overlay.GetType().Name}";
        }

        internal static bool EnsureValidId(IEnumerable<Overlay> existing, Overlay overlay)
        {
            if (overlay == null)
                return false;
            var id = string.IsNullOrEmpty(overlay.id) ? $"{overlay.GetType()}" : overlay.id;
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var ret = EnsureUniqueId(existing.Select(x => x.id), id);
#pragma warning restore RS0030
            if (string.IsNullOrEmpty(ret))
                return false;
            overlay.id = ret;
            return true;
        }

        static string EnsureUniqueId(IEnumerable<string> existing, string name)
        {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (!existing.Contains(name))
#pragma warning restore RS0030
                return name;

            // 256 has no special meaning, it's just a failsafe to prevent this method from locking up the editor
            // in the event that someone is incorrectly using AddOverlay. We'll throw an exception in EnsureValidId
            // to let the user know that they're doing it wrong (there aren't many legitimate cases for more than a
            // couple overlays of the same type, let alone 100+).
            for (int n = 0; n < 256; ++n)
            {
                var inc = $"{name} ({n})";
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                if (!existing.Contains(inc))
#pragma warning restore RS0030
                    return inc;
            }

            return null;
        }

        internal static void AddStyleSheets(VisualElement ve)
        {
            StyleSheet sheet;
            sheet = EditorGUIUtility.Load(k_StyleCommon) as StyleSheet;
            ve.styleSheets.Add(sheet);

            if (EditorGUIUtility.isProSkin)
                sheet = EditorGUIUtility.Load(k_StyleDark) as StyleSheet;
            else
                sheet = EditorGUIUtility.Load(k_StyleLight) as StyleSheet;

            ve.styleSheets.Add(sheet);
        }

        public static bool IsOverlayWindowValid(OverlayAttribute attribute)
        {
            return !(!typeof(ISupportsOverlays).IsAssignableFrom(attribute.editorWindowType)
                     && attribute.editorWindowType != typeof(EditorWindow)
                     && !attribute.editorWindowType.IsInterface);
        }

        internal static OverlayDropZoneBase FindNearestValidDockZoneHorizontally(IEnumerable<OverlayDropZoneBase> dropZones, Overlay targetOverlay, Vector2 mousePosition)
        {
            float mx = mousePosition.x;
            float closestDist = float.MaxValue;
            OverlayDropZoneBase nearestDropZone = null;
            foreach (var dz in dropZones)
            {
                if (!dz.visible)
                    continue;
                
                if (!dz.CanAcceptTarget(targetOverlay))
                    continue;

                float l = dz.worldBoundingBox.xMin;
                float r = dz.worldBoundingBox.xMax;
                float dist = Mathf.Min(Mathf.Abs(l - mx), Mathf.Abs(r - mx));
                if (dist < closestDist)
                {
                    closestDist = dist;
                    nearestDropZone = dz;
                }
            }
            return nearestDropZone;
        }

        // Can be a costly operation, shouldn't be used frequently
        // Does not support comparing with default presets (empty array)
        internal static bool IsCanvasStateDifferent(OverlayCanvasSaveState a, OverlayCanvasSaveState b)
        {
            if (a.overlays != b.overlays && (a.overlays == null || b.overlays == null))
                return true;

            if (a.dynamicPanels != b.dynamicPanels && (a.dynamicPanels == null || b.dynamicPanels == null))
                return true;

            if (a.overlays != null)
            {
                foreach (var data in a.overlays)
                {
                    var index = Array.FindIndex(b.overlays, (save) => save.id == data.id);
                    if (index < 0)
                        return true;

                    if (!data.Equals(b.overlays[index]))
                        return true;
                }
            }

            if (a.dynamicPanels != null)
            {
                foreach (var data in a.dynamicPanels)
                {
                    var index = Array.FindIndex(b.dynamicPanels, (save) => save.containerId == data.containerId);
                    if (index < 0)
                        return true;

                    if (!data.Equals(b.dynamicPanels[index]))
                        return true;
                }
            }

            return false;
        }
    }
}
