// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.EditorTools;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    public interface ISupportsOverlays {}

    static class OverlayUtilities
    {
        class OverlayEditorWindowAssociation
        {
            public Type overlay;
            public Type editorWindowType;
        }

        static OverlayEditorWindowAssociation[] s_Overlays;
        static readonly Dictionary<Type, List<Type>> s_OverlaysTypeAssociations = new Dictionary<Type, List<Type>>();
        internal const string nullWindowTypeErrorMsg = "{0} editor window type cannot be null.";

        static OverlayEditorWindowAssociation[] overlays
        {
            get
            {
                if (s_Overlays == null)
                {
                    Type[] ovrls = TypeCache.GetTypesWithAttribute<OverlayAttribute>()
                        .Where(x => !x.IsAbstract)
                        .ToArray();

                    int len = ovrls.Length;
                    var overlayWindows = new List<OverlayEditorWindowAssociation>(len);

                    for (int i = 0; i < len; i++)
                    {
                        var overlayAttribute = (OverlayAttribute)ovrls[i].GetCustomAttributes(typeof(OverlayAttribute), false).FirstOrDefault();

                        // Overlays that are implemented as instances don't need to define a target editor window.
                        if (overlayAttribute?.editorWindowType == null)
                            continue;

                        overlayWindows.Add(new OverlayEditorWindowAssociation
                        {
                            overlay = ovrls[i],
                            editorWindowType = overlayAttribute.editorWindowType
                        });
                    }

                    s_Overlays = overlayWindows.ToArray();
                }

                return s_Overlays;
            }
        }

        internal static List<Type> GetOverlaysForType(Type type)
        {
            List<Type> res;

            if (s_OverlaysTypeAssociations.TryGetValue(type, out res))
                return res;

            s_OverlaysTypeAssociations.Add(type, res = new List<Type>());

            for (int i = 0, c = overlays.Length; i < c; i++)
            {
                if (overlays[i].editorWindowType != null
                    && (overlays[i].editorWindowType.IsAssignableFrom(type)
                        || type.IsAssignableFrom(overlays[i].editorWindowType)))
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
            if(type == typeof(EditorToolSettingsOverlay))
                overlay = new EditorToolSettingsOverlay();
            else if(type == typeof(SearchToolBar))
                overlay = new SearchToolBar();
            else if(type == typeof(TransformToolsOverlayToolBar))
                overlay = new TransformToolsOverlayToolBar();
            else if(type == typeof(SceneViewToolBar))
                overlay = new SceneViewToolBar();
            else if(type == typeof(GridAndSnapToolBar))
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

        internal static string GetSignificantLettersForIcon(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            var folders = s.Split('/');
            var last = folders[folders.Length - 1];
            var words = last.Trim().Split(' ');

            if (words.Length == 1)
            {
                return words[0].Length > 1 ? words[0].Substring(0, 2) : words[0][0].ToString();
            }

            return words[0].Substring(0, 1) + words[1].Substring(0, 1);
        }

        internal static bool EnsureValidId(IEnumerable<Overlay> existing, Overlay overlay)
        {
            if (overlay == null)
                return false;
            var id = string.IsNullOrEmpty(overlay.id) ? $"{overlay.GetType()}" : overlay.id;
            var ret = EnsureUniqueId(existing.Select(x => x.id), id);
            if (string.IsNullOrEmpty(ret))
                return false;
            overlay.id = ret;
            return true;
        }

        static string EnsureUniqueId(IEnumerable<string> existing, string name)
        {
            if (!existing.Contains(name))
                return name;

            // 256 has no special meaning, it's just a failsafe to prevent this method from locking up the editor
            // in the event that someone is incorrectly using AddOverlay. We'll throw an exception in EnsureValidId
            // to let the user know that they're doing it wrong (there aren't many legitimate cases for more than a
            // couple overlays of the same type, let alone 100+).
            for (int n = 0; n < 256; ++n)
            {
                var inc = $"{name} ({n})";
                if (!existing.Contains(inc))
                    return inc;
            }

            return null;
        }
    }
}
