// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    // The grammar of UI Toolkit animation binding names. A UI Toolkit curve keeps
    // EditorCurveBinding.path empty and encodes its element inside propertyName:
    //
    //   [elementPath/]baseName[.suffix]
    //
    // where elementPath is "#name" segments joined by '/' - empty for the animation root - and the
    // tail is everything after it. Parsing and building live together because the two must agree
    // byte-for-byte, or the Animation Window shows two rows driving one property.
    //
    // Everything here is string shape; what a segment or base name *means* stays with the callers.
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal static class UIAnimationPath
    {
        // The dope sheet asks for the element path of every row on every repaint and never wants the tail,
        // which the full split would allocate regardless.
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static bool TrySplitElementPath(string propertyName, out string elementPath)
        {
            elementPath = null;

            if (string.IsNullOrEmpty(propertyName))
                return false;

            var lastSlash = propertyName.LastIndexOf('/');
            if (lastSlash < 0)
            {
                elementPath = string.Empty;
                return true;
            }

            if (lastSlash == propertyName.Length - 1)
                return false;

            elementPath = propertyName.Substring(0, lastSlash);
            return true;
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static bool TrySplitPropertyName(string propertyName, out string elementPath, out string tail)
        {
            tail = null;
            if (!TrySplitElementPath(propertyName, out elementPath))
                return false;

            tail = elementPath.Length == 0 ? propertyName : propertyName.Substring(elementPath.Length + 1);
            return true;
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static string BuildPropertyName(string elementPath, string propName, string channelSuffix = null)
        {
            // Empty path produces "Opacity", not "/Opacity", to match Add Property and stored curves.
            var baseName = string.IsNullOrEmpty(elementPath)
                ? propName
                : $"{elementPath}/{propName}";

            return string.IsNullOrEmpty(channelSuffix)
                ? baseName
                : baseName + channelSuffix;
        }

        /// <summary>
        /// Where <paramref name="elementPath"/> lands when <paramref name="oldElementPath"/> is remapped
        /// onto <paramref name="newElementPath"/>, or null when the move does not reach it.
        /// </summary>
        // Matching anchors on a trailing separator so "#a" cannot claim "#ab". The empty path addresses the
        // animation root, which every other path hangs off; cascading from it would re-root the whole clip
        // instead of repairing one element's subtree.
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static string RebaseElementPath(string elementPath, string oldElementPath, string newElementPath)
        {
            if (string.Equals(elementPath, oldElementPath, StringComparison.Ordinal))
                return newElementPath;

            if (oldElementPath.Length == 0)
                return null;

            var prefix = oldElementPath + "/";
            return elementPath.StartsWith(prefix, StringComparison.Ordinal)
                ? BuildPropertyName(newElementPath, elementPath.Substring(prefix.Length))
                : null;
        }

        // Lenient by design: malformed input ("a/") yields an empty tail rather than a refusal, because
        // its callers guard with their own lookups and enum parses. Validation is TrySplitPropertyName.
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static string Tail(string propertyName) =>
            propertyName.Substring(propertyName.LastIndexOf('/') + 1);

        // The tail up to its first sub-channel separator: "#a/Opacity.x" -> "Opacity". Lenient like Tail.
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static string BaseName(string propertyName)
        {
            var start = propertyName.LastIndexOf('/') + 1;
            var dot = propertyName.IndexOf('.', start);
            var end = dot < 0 ? propertyName.Length : dot;
            return propertyName.Substring(start, end - start);
        }

        // The suffix starting at the first '.' after the trailing path segment
        // (e.g. ".offset.value"), or null when there is no sub-channel.
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static string SubChannelSuffix(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return null;

            var pathEnd = propertyName.LastIndexOf('/');
            var firstDot = propertyName.IndexOf('.', pathEnd + 1);
            if (firstDot < 0 || firstDot == propertyName.Length - 1)
                return null;

            return propertyName.Substring(firstDot);
        }


        // The one place the segment shape is decided; GatherAnimatableElements registers what these
        // build, so every producer and consumer of a path shares the same bytes.
        const string k_SegmentPrefix = "#";

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static string Segment(string elementName) => k_SegmentPrefix + elementName;

        // A Segment call in the chain would break the compile-time "/#" fold and cost a transient string.
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static string AppendSegment(string elementPath, string elementName) =>
            string.IsNullOrEmpty(elementPath)
                ? Segment(elementName)
                : elementPath + "/" + k_SegmentPrefix + elementName;

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static string LastSegment(string elementPath)
        {
            var lastSlash = elementPath.LastIndexOf('/');
            return lastSlash < 0 ? elementPath : elementPath.Substring(lastSlash + 1);
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static string ReplaceLastSegment(string elementPath, string segment)
        {
            var lastSlash = elementPath.LastIndexOf('/');
            return lastSlash < 0 ? segment : elementPath.Substring(0, lastSlash + 1) + segment;
        }
    }
}
