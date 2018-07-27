// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.IMGUI.Controls
{
    class BreadcrumbBar
    {
        List<Item> m_Breadcrumbs = new List<Item>();

        public static class DefaultStyles
        {
            public static GUIStyle label;
            public static GUIStyle labelMissing;
            public static GUIStyle labelBold;
            public static GUIStyle labelBoldMissing;
            public static GUIStyle background;
            public static GUIStyle separator = "BreadcrumbsSeparator";

            static DefaultStyles()
            {
                label = new GUIStyle(EditorStyles.label);
                label.alignment = TextAnchor.MiddleLeft;
                label.margin = new RectOffset(0, 0, 0, 0);

                labelMissing = new GUIStyle(label);
                labelMissing.normal.textColor = GameObjectTreeViewGUI.GameObjectStyles.brokenPrefabLabel.normal.textColor;

                labelBold = new GUIStyle(label);
                labelBold.fontStyle = FontStyle.Bold;

                labelBoldMissing = new GUIStyle(labelMissing);
                labelBoldMissing.fontStyle = FontStyle.Bold;

                background = new GUIStyle("ProjectBrowserTopBarBg");
                background.padding = new RectOffset(4, 4, 0, 0);
                background.border = new RectOffset(3, 3, 3, 3);
                background.fixedHeight = 25f;
            }
        }

        public class Item
        {
            public GUIContent content { get; set; }
            public GUIStyle guistyle { get; set; }
            public object userdata { get; set; }
        }

        public event Action<Item> onBreadCrumbClicked = null;
        public List<Item> breadcrumbs { get { return m_Breadcrumbs; } }

        public void SetBreadCrumbs(List<Item> breadCrumbItems)
        {
            m_Breadcrumbs = breadCrumbItems;

            // Set default style if needed
            foreach (var item in m_Breadcrumbs)
                if (item.guistyle == null)
                    item.guistyle = DefaultStyles.label;
        }

        public void OnGUI()
        {
            EditorGUIUtility.SetIconSize(new Vector2(16, 16));
            for (int i = 0; i < m_Breadcrumbs.Count; ++i)
            {
                Item item = m_Breadcrumbs[i];

                if (GUILayout.Button(item.content, item.guistyle, GUILayout.MinWidth(32)))
                {
                    if (onBreadCrumbClicked != null)
                        onBreadCrumbClicked(item);
                }

                bool lastElement = i == m_Breadcrumbs.Count - 1;
                if (!lastElement)
                {
                    GUILayout.Label(GUIContent.none, DefaultStyles.separator);
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUIUtility.SetIconSize(new Vector2(0, 0));
        }
    }
}
