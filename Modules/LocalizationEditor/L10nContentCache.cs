// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace UnityEditor
{
    public static partial class L10n
    {
        [NoAutoStaticsCleanup] // GUIContent cache; re-populated on demand after reload
        static readonly Dictionary<string, GUIContent> s_TextContents = new Dictionary<string, GUIContent>();
        [NoAutoStaticsCleanup] // GUIContent cache; re-populated on demand after reload
        static readonly Dictionary<string, GUIContent> s_IconContents = new Dictionary<string, GUIContent>();

        // Named groups get their own tables. The ungrouped ones are keyed on caller-supplied strings,
        // including raw icon names and tooltips, so no prefix scheme can be proven not to collide.
        [NoAutoStaticsCleanup] // GUIContent cache; re-populated on demand after reload
        static readonly Dictionary<(string group, string key), GUIContent> s_GroupTextContents =
            new Dictionary<(string, string), GUIContent>();
        [NoAutoStaticsCleanup] // GUIContent cache; re-populated on demand after reload
        static readonly Dictionary<(string group, string key), GUIContent> s_GroupIconContents =
            new Dictionary<(string, string), GUIContent>();

        static void ClearContentCache()
        {
            s_TextContents.Clear();
            s_IconContents.Clear();
            s_GroupTextContents.Clear();
            s_GroupIconContents.Clear();
        }

        static GUIContent GetCached(Dictionary<string, GUIContent> ungrouped,
            Dictionary<(string, string), GUIContent> grouped, string groupName, string key)
        {
            if (key == null)
                return null;

            if (groupName == null)
                return ungrouped.TryGetValue(key, out var content) ? content : null;

            return grouped.TryGetValue((groupName, key), out var groupedContent) ? groupedContent : null;
        }

        static void SetCached(Dictionary<string, GUIContent> ungrouped,
            Dictionary<(string, string), GUIContent> grouped, string groupName, string key, GUIContent content)
        {
            if (key == null)
                return;

            if (groupName == null)
                ungrouped[key] = content;
            else
                grouped[(groupName, key)] = content;
        }

        /// <summary>
        ///     Builds text content, or returns the cached instance when the same key has been asked for before.
        /// </summary>
        internal static GUIContent CachedTextContent(string groupName, string key, string text, string tooltip, Texture icon)
        {
            GUIContent content = GetCached(s_TextContents, s_GroupTextContents, groupName, key);
            if (content == null)
            {
                content = new GUIContent(TrWithGroup(text, groupName));
                if (tooltip != null)
                {
                    content.tooltip = TrWithGroup(tooltip, groupName);
                }
                if (icon != null)
                {
                    content.image = icon;
                }
                SetCached(s_TextContents, s_GroupTextContents, groupName, key, content);
            }
            return content;
        }

        /// <summary>
        ///     Builds icon content from an icon name, or returns the cached instance.
        /// </summary>
        /// <remarks>
        ///     The key does not include <paramref name="lightenTexture"/>, so asking for the same icon both
        ///     ways returns whichever was built first. That is long standing and left as it is.
        /// </remarks>
        internal static GUIContent IconContentNamed(string groupName, string iconName, string tooltip, bool lightenTexture)
        {
            string key = IconKey(iconName, tooltip);
            GUIContent content = GetCached(s_IconContents, s_GroupIconContents, groupName, key);
            if (content == null)
            {
                content = new GUIContent();
                if (tooltip != null)
                {
                    content.tooltip = TrWithGroup(tooltip, groupName);
                }
                content.image = EditorGUIUtility.LoadIconRequired(iconName);
                if (lightenTexture && content.image is Texture2D lightened)
                {
                    content.image = EditorGUIUtility.LightenTexture(lightened);
                }
                SetCached(s_IconContents, s_GroupIconContents, groupName, key, content);
            }
            return content;
        }

        static GUIContent CachedIconContent(string groupName, string key, Texture icon, string tooltip)
        {
            GUIContent content = GetCached(s_IconContents, s_GroupIconContents, groupName, key);
            if (content == null)
            {
                content = new GUIContent { image = icon };
                if (tooltip != null)
                {
                    content.tooltip = TrWithGroup(tooltip, groupName);
                }
                SetCached(s_IconContents, s_GroupIconContents, groupName, key, content);
            }
            return content;
        }

        // Each part is written with its length in front. Joining on a separator alone would let
        // ("a|b", "c") and ("a", "b|c") build the same key and share an entry, and both halves are
        // user-visible text, so neither can be assumed free of the separator.
        static void AppendPart(System.Text.StringBuilder key, string part)
        {
            if (part == null)
            {
                key.Append("-|");
                return;
            }
            key.Append(part.Length).Append('|').Append(part);
        }

        static string TextKey(string text, string tooltip)
        {
            var key = new System.Text.StringBuilder(64);
            AppendPart(key, text);
            AppendPart(key, tooltip);
            return key.ToString();
        }

        static string TextKey(string text, string tooltip, string iconName)
        {
            var key = new System.Text.StringBuilder(96);
            AppendPart(key, text);
            AppendPart(key, tooltip);
            AppendPart(key, iconName);
            key.Append(EditorGUIUtility.pixelsPerPoint);
            return key.ToString();
        }

        static string IconKey(string iconName, string tooltip)
        {
            var key = new System.Text.StringBuilder(64);
            AppendPart(key, iconName);
            AppendPart(key, tooltip);
            key.Append(EditorGUIUtility.pixelsPerPoint);
            return key.ToString();
        }

        // The ungrouped keys have never included the texture, so two icons sharing a tooltip return
        // each other's content. That is long standing, and changing it would move behaviour callers
        // may rely on, so it stays. The grouped tables are new and do not inherit it.
        static string WithIconIdentity(string groupName, string key, Texture icon)
        {
            if (groupName == null || icon == null)
                return key;
            return string.Format("{0}|{1}", key, icon.GetEntityId());
        }

        // A tooltip on its own is a whole key here, so it is length-prefixed like any other part.
        static string TooltipKey(string tooltip)
        {
            var key = new System.Text.StringBuilder(32);
            AppendPart(key, tooltip);
            return key.ToString();
        }
    }
}
