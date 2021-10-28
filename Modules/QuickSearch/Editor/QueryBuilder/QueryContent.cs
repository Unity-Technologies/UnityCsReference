// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Search
{
    class QueryContent : GUIContent
    {
        public readonly float width;
        public readonly GUIStyle style;

        public static readonly QueryContent DownArrow;
        public static readonly QueryContent UpArrow;

        public static readonly new QueryContent none = new QueryContent(string.Empty, null, Styles.QueryBuilder.label);

        public float expandedWidth => width + style.margin.horizontal;

        static QueryContent()
        {
            DownArrow = Styles.QueryBuilder.label.CreateContent("", EditorGUIUtility.LoadGeneratedIconOrNormalIcon("icon dropdown"));
            UpArrow = Styles.QueryBuilder.label.CreateContent("", EditorGUIUtility.LoadGeneratedIconOrNormalIcon("icon dropdown open"));
        }

        public QueryContent(string text, Texture2D image, GUIStyle style) : base(text, image)
        {
            this.style = style;
            width = style.CalcSize(this).x;
        }

        public Rect Draw(in Rect rect, in Vector2 mousePosition)
        {
            if (Event.current.type == EventType.Repaint)
                style.Draw(rect, this, rect.Contains(mousePosition), false, false, false);
            return rect;
        }
    }

    // TODO: Move the content generator in the QueryBuilder?
    static class QueryContentGenerator
    {
        // TODO: manage the pool size
        static readonly Dictionary<int, QueryContent> s_ContentPool = new Dictionary<int, QueryContent>();
        public static QueryContent CreateContent(this GUIStyle style, string value, Texture2D image = null)
        {
            if (string.IsNullOrEmpty(value) && image == null)
                return QueryContent.none;
            var valueHash = value.GetHashCode() ^ (image?.GetHashCode() ?? 53);
            if (s_ContentPool.Count > 50)
                s_ContentPool.Clear();
            if (s_ContentPool.TryGetValue(value.GetHashCode(), out var qc))
                return qc;
            qc = new QueryContent(value, image, style);
            s_ContentPool[valueHash] = qc;
            return qc;
        }
    }
}
