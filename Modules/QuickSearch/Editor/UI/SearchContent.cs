// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.Search
{
    static class SearchContent
    {
        private static int s_GUIContentPoolIndex = 0;
        private static readonly GUIContent[] s_GUIContentPool = new GUIContent[32];

        public static GUIContent Take(string text = null, string tooltip = null, Texture2D thumbnail = null)
        {
            GUIContent content = s_GUIContentPool[s_GUIContentPoolIndex];
            if (content == null)
                s_GUIContentPool[s_GUIContentPoolIndex] = content = new GUIContent(text, thumbnail, tooltip);
            else
            {
                content.text = text;
                content.tooltip = tooltip;
                content.image = thumbnail;
            }

            s_GUIContentPoolIndex = Utils.Wrap(s_GUIContentPoolIndex + 1, s_GUIContentPool.Length);
            return content;
        }

        public static GUIContent FormatDescription(SearchItem item, SearchContext context, float availableSpace, bool useColor = true)
        {
            var desc = item.GetDescription(context);
            if (String.IsNullOrEmpty(desc))
                return Styles.emptyContent;
            var content = Take(desc);
            if (item.options == SearchItemOptions.None || Event.current.type != EventType.Repaint)
                return content;

            var truncatedDesc = desc;
            var truncated = false;
            if (useColor)
            {
                if (item.options.HasAny(SearchItemOptions.Ellipsis))
                {
                    int maxCharLength = Utils.GetNumCharactersThatFitWithinWidth(Styles.itemDescription, truncatedDesc + "...", availableSpace);
                    if (maxCharLength < 0)
                        maxCharLength = truncatedDesc.Length;
                    truncated = desc.Length > maxCharLength;
                    if (truncated)
                    {
                        if (item.options.HasAny(SearchItemOptions.RightToLeft))
                        {
                            truncatedDesc = "..." + desc.Replace("<b>", "").Replace("</b>", "");
                            truncatedDesc = truncatedDesc.Substring(Math.Max(0, truncatedDesc.Length - maxCharLength));
                        }
                        else
                            truncatedDesc = desc.Substring(0, Math.Min(maxCharLength, desc.Length)) + "...";
                    }
                }

                if (context != null)
                {
                    if (item.options.HasAny(SearchItemOptions.Highlight))
                    {
                        var parts = context.searchQuery.Split('*', ' ', '.').Where(p => p.Length > 2);
                        foreach (var p in parts)
                            truncatedDesc = Regex.Replace(truncatedDesc, Regex.Escape(p), string.Format(Styles.highlightedTextColorFormat, "$0"), RegexOptions.IgnoreCase);
                    }
                    else if (item.options.HasAny(SearchItemOptions.FuzzyHighlight))
                    {
                        long score = 1;
                        var matches = new List<int>();
                        var sq = Utils.CleanString(context.searchQuery.ToLowerInvariant());
                        if (FuzzySearch.FuzzyMatch(sq, Utils.CleanString(truncatedDesc), ref score, matches))
                            truncatedDesc = RichTextFormatter.FormatSuggestionTitle(truncatedDesc, matches);
                    }
                }
            }

            content.text = truncatedDesc;
            if (truncated)
                content.tooltip = Utils.StripHTML(desc);

            return content;
        }
    }
}
