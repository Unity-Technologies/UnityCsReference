// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor
{
    internal class FrameWarningsOverlayWidget
    {
        const string k_UxmlIdentifier_WarningBox = "profiler-chart-view__chart__warning-box";

        readonly ChartModel m_Model;
        readonly VisualElement m_Root;
        readonly Func<int, string> m_MessagesFactory;

        VisualElement m_GroupRoot;
        List<Label> m_Cached;

        public FrameWarningsOverlayWidget(ChartModel model, VisualElement root, Func<int, string> messagesFactory)
        {
            m_Model = model;
            m_Root = root;
            m_MessagesFactory = messagesFactory;

            m_GroupRoot = new VisualElement();
            m_GroupRoot.name = GetType().Name;
            m_GroupRoot.StretchToParentSize();
            m_Root.Add(m_GroupRoot);
            m_Cached = new List<Label>();
        }

        public void Update()
        {
            int totalWarningBoxes = 0;
            if (m_Model?.dataAvailable != null)
            {
                int lastFrameBeforeStatisticsAvailabilityChanged = 0;
                int lastStatisticsState = 1;
                int frameDataLength = m_Model.dataAvailable.Length;
                for (var frame = 0; frame < frameDataLength; frame++)
                {
                    bool hasDataForFrame = (m_Model.dataAvailable[frame] & ChartModel.dataAvailableBit) == ChartModel.dataAvailableBit;
                    if (hasDataForFrame)
                    {
                        if (lastFrameBeforeStatisticsAvailabilityChanged < frame - 1)
                        {
                            AddOverlayBox(lastFrameBeforeStatisticsAvailabilityChanged, frame, lastStatisticsState, ref totalWarningBoxes);
                        }
                        lastStatisticsState = ChartModel.dataAvailableBit;
                        lastFrameBeforeStatisticsAvailabilityChanged = frame;
                    }
                    else if (lastStatisticsState != m_Model.dataAvailable[frame])
                    {
                        // Not bitwise comparison because this here just checks that the previous frame didn't just contain normal available data
                        if (lastStatisticsState != ChartModel.dataAvailableBit)
                        {
                            // a new reason for missing data has started here, flush out the old one
                            AddOverlayBox(lastFrameBeforeStatisticsAvailabilityChanged, frame, lastStatisticsState, ref totalWarningBoxes);
                        }
                        lastFrameBeforeStatisticsAvailabilityChanged = frame;
                        lastStatisticsState = m_Model.dataAvailable[frame];
                    }
                }
                if (lastFrameBeforeStatisticsAvailabilityChanged < frameDataLength - 1)
                {
                    AddOverlayBox(lastFrameBeforeStatisticsAvailabilityChanged, frameDataLength - 1, lastStatisticsState, ref totalWarningBoxes);
                }
            }

            // Hide extra boxes
            for (var i = totalWarningBoxes; i < m_Cached.Count; i++)
            {
                var element = m_Cached[i];
                if (element.style.visibility == Visibility.Visible)
                {
                    element.style.visibility = Visibility.Hidden;
                    element.style.left = 0;
                    element.style.width = 0;
                }
            }
        }

        void AddOverlayBox(int startFrame, int endFrame, int statisticsAvailabilityState, ref int totalOverlayBoxes)
        {
            Label warningBox;
            if (totalOverlayBoxes >= m_Cached.Count)
            {
                warningBox = new Label();
                warningBox.AddToClassList(k_UxmlIdentifier_WarningBox);
                m_GroupRoot.Add(warningBox);
                m_Cached.Add(warningBox);
            }
            else
                warningBox = m_Cached[totalOverlayBoxes];

            totalOverlayBoxes++;

            var domainSize = m_Model.GetDataDomainLength();
            var pos = (float)startFrame / domainSize;
            var width = (float)(endFrame - startFrame) / domainSize;
            warningBox.style.left = new Length(100.0f * pos, LengthUnit.Percent);
            warningBox.style.width = new Length(100.0f * width, LengthUnit.Percent);
            warningBox.style.visibility = Visibility.Visible;

            if (m_MessagesFactory != null)
            {
                var message = m_MessagesFactory(statisticsAvailabilityState);

                if (message != null)
                {
                    // Remove hyperlinks for the player settings from tooltip, since they can't be clicked.
                    message = Regex.Replace(message, @"\(<a playersettingslink=.*<\/a>\)", "");
                }
                warningBox.text = message;
            }
            else
                warningBox.text = string.Empty;
        }
    }
}
