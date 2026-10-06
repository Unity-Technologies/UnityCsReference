// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;
using Unity.Properties;

namespace Unity.Multiplayer.Center.Editor
{
    /// <summary>
    /// Custom GridView display to display a list of packages based on <see cref="PackageRecommendation"/>.
    /// </summary>
    [UxmlElement]
    partial class RecommendationsGrid : VisualElement
    {
        public RecommendationsGrid()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanelEvent);
        }

        [UxmlAttribute] public VisualTreeAsset ItemTemplate { get; set; }

        List<string> m_Recommendations = new();

        [UxmlAttribute, CreateProperty]
        public List<string> Recommendations
        {
            get => m_Recommendations;
            set
            {
                if (m_Recommendations.Count != value.Count)
                {
                    while (m_Recommendations.Count < value.Count)
                    {
                        m_Recommendations.Add(string.Empty);
                        // The TemplateContainer is adding its own styling that breaks the grid layout.
                        // This is why we're adding the first child instead of the whole instantiated element.
                        var element = ItemTemplate.Instantiate().ElementAt(0);
                        Add(element);
                    }

                    while (m_Recommendations.Count > value.Count)
                    {
                        RemoveAt(m_Recommendations.Count - 1);
                        m_Recommendations.RemoveAt(m_Recommendations.Count - 1);
                    }
                }

                for (int i = 0; i < m_Recommendations.Count; i++)
                {
                    if (m_Recommendations[i] != value[i])
                    {
                        m_Recommendations[i] = value[i];
                        var element = ElementAt(i);
                        element.dataSource = value[i];
                    }
                }
            }
        }

        void OnDetachFromPanelEvent(DetachFromPanelEvent evt)
        {
            UnregisterCallback<GeometryChangedEvent>(HandleGeometryChange);
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            RegisterCallback<GeometryChangedEvent>(HandleGeometryChange);
        }

        void HandleGeometryChange(GeometryChangedEvent evt)
        {
            if (hierarchy.childCount == 0)
                return;

            // if we can't display 2 items side by side at least
            var shouldAdd = hierarchy.childCount < 2
                            || evt.newRect.width < hierarchy[0].resolvedStyle.minWidth.value * 2;

            foreach (var child in hierarchy.Children())
            {
                child.EnableInClassList(StyleClasses.RecommendationGridCollapsed, shouldAdd);
            }

            evt.StopImmediatePropagation();
        }
    }
}
