// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine.TextCore;

namespace UnityEngine.TextCore.Text
{
    internal struct GlyphProxy
    {
        public uint index;
        public GlyphRect glyphRect;
        public GlyphMetrics metrics;
        public int atlasIndex;
    }

    internal static class FontAssetEditorUtilities
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="so"></param>
        /// <param name="lookupDictionary"></param>
        internal static void PopulateGlyphProxyLookupDictionary(SerializedObject so, Dictionary<uint, GlyphProxy> lookupDictionary)
        {
            if (lookupDictionary == null)
                return;

            // Get reference to serialized property for the glyph table
            SerializedProperty glyphTable = so.FindProperty("m_GlyphTable");

            for (int i = 0; i < glyphTable.arraySize; i++)
            {
                SerializedProperty glyphProperty = glyphTable.GetArrayElementAtIndex(i);
                GlyphProxy proxy = GetGlyphProxyFromSerializedProperty(glyphProperty);

                lookupDictionary.Add(proxy.index, proxy);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        static GlyphProxy GetGlyphProxyFromSerializedProperty(SerializedProperty property)
        {
            GlyphProxy proxy = new GlyphProxy();
            proxy.index = (uint)property.FindPropertyRelative("m_Index").intValue;

            SerializedProperty glyphRectProperty = property.FindPropertyRelative("m_GlyphRect");
            proxy.glyphRect = new GlyphRect();
            proxy.glyphRect.x = glyphRectProperty.FindPropertyRelative("m_X").intValue;
            proxy.glyphRect.y = glyphRectProperty.FindPropertyRelative("m_Y").intValue;
            proxy.glyphRect.width = glyphRectProperty.FindPropertyRelative("m_Width").intValue;
            proxy.glyphRect.height = glyphRectProperty.FindPropertyRelative("m_Height").intValue;

            SerializedProperty glyphMetricsProperty = property.FindPropertyRelative("m_Metrics");
            proxy.metrics = new GlyphMetrics();
            proxy.metrics.horizontalBearingX = glyphMetricsProperty.FindPropertyRelative("m_HorizontalBearingX").floatValue;
            proxy.metrics.horizontalBearingY = glyphMetricsProperty.FindPropertyRelative("m_HorizontalBearingY").floatValue;
            proxy.metrics.horizontalAdvance = glyphMetricsProperty.FindPropertyRelative("m_HorizontalAdvance").floatValue;
            proxy.metrics.width = glyphMetricsProperty.FindPropertyRelative("m_Width").floatValue;
            proxy.metrics.height = glyphMetricsProperty.FindPropertyRelative("m_Height").floatValue;

            return proxy;
        }
    }
}
