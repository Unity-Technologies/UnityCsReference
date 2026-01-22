// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor
{
    internal class ChartGuidesWidget
    {
        const string k_UxmlIdentifier_Uxml = "ProfilerChartViewGuide.uxml";
        const string k_UxmlIdentifier_Chart_Grid = "profiler-chart-view__chart__grid";
        const string k_UxmlIdentifier_Chart_Grid_Label = "profiler-chart-view__chart__grid-label";
        const int kMaxGuidesCount = 3;

        readonly ChartModel m_Model;
        readonly VisualElement m_Root;
        readonly VisualTreeAsset m_Template;

        VisualElement[] m_Guides;

        public ChartGuidesWidget(ChartModel model, VisualElement root)
        {
            m_Model = model;
            m_Root = root;

            m_Template = EditorGUIUtility.Load(k_UxmlIdentifier_Uxml) as VisualTreeAsset;

            MakeGuides();
        }

        public void Update()
        {
            if (!IsValidModel())
            {
                HideAll();
                return;
            }

            var range = m_Model.series[0].rangeAxis;
            float rangeScale = 100.0f / (range.y - range.x);
            for (int i = 0; i < m_Guides.Length; ++i)
            {
                var gridElement = m_Guides[i];

                if (i < m_Model.grid.Length)
                {
                    var gridValue = m_Model.grid[i];
                    float y = 100.0f - (gridValue - range.x) * rangeScale;
                    if (Mathf.Approximately(y, gridElement.style.top.value.value))
                        continue;

                    if (y >= 0)
                    {
                        var gridElementLabel = gridElement.Q<Label>(k_UxmlIdentifier_Chart_Grid_Label);
                        gridElementLabel.text = m_Model.gridLabels[i];
                        gridElement.style.top = new Length(y, LengthUnit.Percent);
                        gridElement.style.visibility = Visibility.Visible;
                    }
                    else
                    {
                        HideElement(gridElement);
                    }
                }
                else
                {
                    HideElement(gridElement);
                }
            }
        }

        bool IsValidModel()
        {
            if (m_Model.numSeries == 0)
                return false;
            if ((m_Model.grid == null) || (m_Model.gridLabels == null))
                return false;

            return true;
        }

        void MakeGuides()
        {
            m_Guides = new VisualElement[kMaxGuidesCount];

            var groupElem = new VisualElement();
            groupElem.name = GetType().Name;
            groupElem.StretchToParentSize();
            m_Root.Add(groupElem);
            for (int i = 0; i < m_Guides.Length; ++i)
            {
                var gridElement = m_Template.Instantiate();
                gridElement.AddToClassList(k_UxmlIdentifier_Chart_Grid);
                HideElement(gridElement);
                groupElem.Add(gridElement);
                m_Guides[i] = gridElement;
            }
        }

        void HideAll()
        {
            for (int i = 0; i < m_Guides.Length; ++i)
            {
                var gridElement = m_Guides[i];
                HideElement(gridElement);
            }
        }

        void HideElement(VisualElement elem)
        {
            if (elem == null)
                return;
            if (elem.style.visibility == Visibility.Hidden)
                return;

            // Move to top, to make sure they aren't affecting scroll area
            elem.style.top = 0;
            elem.style.visibility = Visibility.Hidden;
        }
    }
}
