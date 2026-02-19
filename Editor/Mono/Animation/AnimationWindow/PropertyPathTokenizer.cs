// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using UnityEngine.Pool;

namespace UnityEditorInternal
{
    /// <summary>
    /// Utility for tokenizing slash-delimited property display names into hierarchical segments.
    /// Used by the Animation Window's "Add Property" menu to create nested tree views for a panel renderer hierarhcy (encoded in the binding property).
    /// </summary>
    internal static class PropertyPathTokenizer
    {
        static ObjectPool<StringBuilder> s_stringBuilderPool = null;

        /// <summary>
        /// Tokenizes a slash-delimited property path name into hierarchical menu entries.
        /// </summary>
        /// <param name="propertyPath">The path for the property</param>
        /// <returns>Array of path segments from root to leaf</returns>
        /// <remarks>
        /// Examples:
        /// - "#RootElementname/Color" -> [#RootElementname,Color]
        /// - "#RootElementname/#TheFirstChild/Color" -> [#RootElementname,#TheFirstChild,Color]
        /// - "#RootElementname//Invalid" -> ["Material", "Invalid"] (empty segments filtered)
        /// - "" -> [] (empty array)
        /// - null -> [] (empty array)
        /// </remarks>
        public static string[] TokenizePath(string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath))
                return Array.Empty<string>();

            // Split on '/' and filter empty entries
            return propertyPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Gets the full path up to a specific depth (for creating intermediate nodes).
        /// </summary>
        /// <param name="segments">The tokenized path segments</param>
        /// <param name="depth">The depth to generate the path for (0-indexed)</param>
        /// <returns>Full path string with slashes, e.g., "Material/Color"</returns>
        public static string GetPathUpToDepth(string[] segments, int depth)
        {
            if (segments == null || segments.Length == 0 || depth < 0)
                return string.Empty;

            int count = Math.Min(depth + 1, segments.Length);
            s_stringBuilderPool ??= new( () => new StringBuilder() );
            var sb = s_stringBuilderPool.Get();
            sb.Clear();

            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                    sb.Append('/');
                sb.Append(segments[i]);
            }

            var result = sb.ToString();
            sb.Clear();
            s_stringBuilderPool.Release(sb);
            return result;
        }
    }
}
