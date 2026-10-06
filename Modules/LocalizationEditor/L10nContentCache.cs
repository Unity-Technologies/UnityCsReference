// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace UnityEditor
{
    public static partial class L10n
    {
        readonly struct ContentKey : IEquatable<ContentKey>
        {
            readonly string m_Group;
            readonly string m_Text;
            readonly string m_Tooltip;
            readonly string m_IconName;
            readonly float m_PixelsPerPoint;
            readonly EntityId m_Icon;

            public ContentKey(string group, string text, string tooltip, string iconName, float pixelsPerPoint, EntityId icon)
            {
                m_Group = group;
                m_Text = text;
                m_Tooltip = tooltip;
                m_IconName = iconName;
                m_PixelsPerPoint = pixelsPerPoint;
                m_Icon = icon;
            }

            public bool Equals(ContentKey other)
            {
                return m_Group == other.m_Group
                    && m_Text == other.m_Text
                    && m_Tooltip == other.m_Tooltip
                    && m_IconName == other.m_IconName
                    && m_PixelsPerPoint == other.m_PixelsPerPoint
                    && m_Icon == other.m_Icon;
            }

            public override bool Equals(object obj)
            {
                return obj is ContentKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(m_Group, m_Text, m_Tooltip, m_IconName, m_PixelsPerPoint, m_Icon);
            }
        }

        [NoAutoStaticsCleanup] // GUIContent cache; re-populated on demand after reload
        static readonly Dictionary<ContentKey, GUIContent> s_TextContents = new Dictionary<ContentKey, GUIContent>();
        [NoAutoStaticsCleanup] // GUIContent cache; re-populated on demand after reload
        static readonly Dictionary<ContentKey, GUIContent> s_NamedIconContents = new Dictionary<ContentKey, GUIContent>();
        [NoAutoStaticsCleanup] // GUIContent cache; re-populated on demand after reload
        static readonly Dictionary<ContentKey, GUIContent> s_TextureIconContents = new Dictionary<ContentKey, GUIContent>();
        [NoAutoStaticsCleanup] // GUIContent cache; re-populated on demand after reload
        static readonly Dictionary<(string group, string key), GUIContent> s_KeyedTextContents =
            new Dictionary<(string, string), GUIContent>();

        static void ClearContentCache()
        {
            s_TextContents.Clear();
            s_NamedIconContents.Clear();
            s_TextureIconContents.Clear();
            s_KeyedTextContents.Clear();
        }

        // The ungrouped keys have never included the texture, so two icons sharing a tooltip return
        // each other's content. That is long standing, and changing it would move behaviour callers
        // may rely on, so it stays. The grouped tables are new and do not inherit it.
        static EntityId IconIdentity(string groupName, Texture icon)
        {
            return groupName == null || icon == null ? default : icon.GetEntityId();
        }

        static GUIContent BuildTextContent(string groupName, string text, string tooltip, Texture icon)
        {
            var content = new GUIContent(TrWithGroup(text, groupName));
            if (tooltip != null)
            {
                content.tooltip = TrWithGroup(tooltip, groupName);
            }
            if (icon != null)
            {
                content.image = icon;
            }
            return content;
        }

        /// <summary>
        ///     Builds text content, or returns the cached instance when the same key has been asked for before.
        /// </summary>
        internal static GUIContent CachedTextContent(string groupName, string key, string text, string tooltip, Texture icon)
        {
            if (key == null)
                return BuildTextContent(groupName, text, tooltip, icon);

            if (!s_KeyedTextContents.TryGetValue((groupName, key), out var content))
            {
                content = BuildTextContent(groupName, text, tooltip, icon);
                s_KeyedTextContents[(groupName, key)] = content;
            }
            return content;
        }

        static GUIContent TextContentByTexture(string groupName, string text, string tooltip, Texture icon)
        {
            var key = new ContentKey(groupName, text, tooltip, null, 0f, IconIdentity(groupName, icon));
            if (!s_TextContents.TryGetValue(key, out var content))
            {
                content = BuildTextContent(groupName, text, tooltip, icon);
                s_TextContents[key] = content;
            }
            return content;
        }

        static GUIContent TextContentByIconName(string groupName, string text, string tooltip, string iconName)
        {
            var key = new ContentKey(groupName, text, tooltip, iconName, EditorGUIUtility.pixelsPerPoint, default);
            if (!s_TextContents.TryGetValue(key, out var content))
            {
                content = BuildTextContent(groupName, text, tooltip, EditorGUIUtility.LoadIconRequired(iconName));
                s_TextContents[key] = content;
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
            var key = new ContentKey(groupName, null, tooltip, iconName, EditorGUIUtility.pixelsPerPoint, default);
            if (!s_NamedIconContents.TryGetValue(key, out var content))
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
                s_NamedIconContents[key] = content;
            }
            return content;
        }

        static GUIContent IconContentByTexture(string groupName, Texture icon, string tooltip)
        {
            // A tooltip is the only thing worth keying on here, so content without one is not cached.
            if (tooltip == null)
                return new GUIContent { image = icon };

            var key = new ContentKey(groupName, null, tooltip, null, 0f, IconIdentity(groupName, icon));
            if (!s_TextureIconContents.TryGetValue(key, out var content))
            {
                content = new GUIContent { image = icon, tooltip = TrWithGroup(tooltip, groupName) };
                s_TextureIconContents[key] = content;
            }
            return content;
        }
    }
}
