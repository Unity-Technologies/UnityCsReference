// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.StyleSheets;
using UnityEngine;

namespace UnityEditor
{
    [Serializable]
    internal class ScalableGUIContent
    {
        [Serializable]
        struct TextureResource
        {
            public float pixelsPerPoint;
            public string resourcePath;
        }

        [SerializeField]
        private List<TextureResource> m_TextureResources = new List<TextureResource>(2);

        [SerializeField]
        private string m_CurrentResourcePath;

        [SerializeField]
        private GUIContent m_GuiContent;

        public ScalableGUIContent(string resourceName) : this(string.Empty, string.Empty, resourceName)
        {
        }

        public ScalableGUIContent(string text, string tooltip, string resourceName)
        {
            m_GuiContent = !string.IsNullOrEmpty(text) || !string.IsNullOrEmpty(tooltip) ?
                EditorGUIUtility.TextContent(string.Format("{0}|{1}", text, tooltip)) :
                new GUIContent();

            // TODO: make this more sophisticated when/if we have more granular support for different DPI levels
            m_TextureResources.Add(new TextureResource { pixelsPerPoint = 1f, resourcePath = resourceName});
            m_TextureResources.Add(new TextureResource { pixelsPerPoint = 2f, resourcePath = string.Format("{0}@2x", resourceName)});
        }

        public static implicit operator GUIContent(ScalableGUIContent gc)
        {
            var dpi = EditorGUIUtility.pixelsPerPoint;
            var resourcePath = gc.m_CurrentResourcePath;
            for (int i = 0, count = gc.m_TextureResources.Count; i < count; ++i)
            {
                if (gc.m_TextureResources[i].pixelsPerPoint > dpi)
                    break;
                resourcePath = gc.m_TextureResources[i].resourcePath;
            }
            if (resourcePath != gc.m_CurrentResourcePath)
            {
                gc.m_GuiContent.image =
                    StyleSheetResourceUtil.LoadResource(resourcePath, typeof(Texture2D), false) as Texture2D;
                gc.m_CurrentResourcePath = resourcePath;
            }
            return gc.m_GuiContent;
        }
    }
}
