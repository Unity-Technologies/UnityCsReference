// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEditor.Profiling;
using System.Text;

namespace UnityEditor
{
    [Serializable]
    internal class VirtualTexturingProfilerView
    {
        [SerializeField]
        Vector2 m_ScrollAggregate, m_ScrollDemand, m_ScrollBias;
        [SerializeField]
        static readonly Guid m_VTProfilerGuid = new Guid("AC871613E8884327B4DD29D7469548DC");

        //+-----------------------------+-------------------------+
        //|                             |                         |
        //|     aggregated info         |                         |
        //|                             |                         |
        //|                             |                         |
        //+----------------+------------+----+                    |
        //|  cache demand  |     cache bias  |                    |
        //|                |                 |                    |
        //|                |                 |                    |
        //|                |                 |                    |
        //+----------------+-----------------+--------------------+

        internal void DrawUIPane(IProfilerWindowController win)
        {
            EditorGUILayout.BeginVertical();
            var rect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            Rect aggregatedInfoBox = rect;
            aggregatedInfoBox.height *= 0.6f;
            aggregatedInfoBox.width *= 0.4f;


            if (ProfilerDriver.GetStatisticsAvailabilityState(ProfilerArea.VirtualTexturing, win.GetActiveVisibleFrameIndex()) == 0)
            {
                GUI.Label(aggregatedInfoBox, "No Virtual Texturing data was collected.\n Virtual Texturing can be enabled in the player settings.");
                EditorGUILayout.EndVertical();
                return;
            }

            Rect cacheDemandTitle = new Rect();
            cacheDemandTitle.width = 400;
            cacheDemandTitle.height = EditorGUIUtility.singleLineHeight;
            cacheDemandTitle.x = rect.x;
            cacheDemandTitle.y += aggregatedInfoBox.height + rect.y;

            Rect cacheFormatBox = new Rect();
            cacheFormatBox.y = 0;
            cacheFormatBox.x = 0;
            cacheFormatBox.height = rect.height * 0.3f;
            cacheFormatBox.width = cacheDemandTitle.width * 0.5f;
            Rect cacheValueBox = cacheFormatBox;
            cacheValueBox.x += cacheFormatBox.width;

            Rect cacheDemandScrollBox = cacheFormatBox;
            cacheDemandScrollBox.y = cacheDemandTitle.y + cacheDemandTitle.height;
            cacheDemandScrollBox.width = cacheDemandTitle.width;

            Rect cacheBiasTitle = cacheDemandTitle;
            cacheBiasTitle.x += cacheDemandTitle.width;

            Rect cacheBiasScollBox = cacheBiasTitle;
            cacheBiasScollBox.height = rect.height * 0.3f;
            cacheBiasScollBox.y += cacheDemandTitle.height;

            var f = ProfilerDriver.GetRawFrameDataView(win.GetActiveVisibleFrameIndex(), 0);
            var frameDataIterator = new ProfilerFrameDataIterator();

            if (f.valid)
            {
                Assert.IsTrue(f.threadName == "Main Thread");

                //Find render thread
                var threadCount = frameDataIterator.GetThreadCount(f.frameIndex);
                RawFrameDataView fRender = null;
                for (int i = 0; i < threadCount; ++i)
                {
                    var frameData = ProfilerDriver.GetRawFrameDataView(f.frameIndex, i);
                    if (frameData.threadName == "Render Thread")
                    {
                        fRender = frameData;
                        break;
                    }
                }
                Assert.IsTrue(fRender != null);

                var stringBuilder = new StringBuilder(1024);
                stringBuilder.AppendLine($"Tiles required this frame: {GetCounterValue(f, "Required Tiles")}");
                stringBuilder.AppendLine($"Atlases: {GetCounterValue(f, "Atlases")}");
                stringBuilder.AppendLine($"Max GPU mip bias: {GetCounterValue(f, "Max Cache Mip Bias")}");
                stringBuilder.AppendLine($"Max GPU cache demand: {GetCounterValue(f, "Max Cache Demand")}%");
                stringBuilder.AppendLine($"Total CPU Cache Size: {EditorUtility.FormatBytes(GetCounterValue(f, "Total Cpu Cache Size"))}");
                stringBuilder.AppendLine($"Total GPU Cache Size: {EditorUtility.FormatBytes(GetCounterValue(f, "Total Gpu Cache size"))}");
                stringBuilder.AppendLine("\nFOLLOWING STATISTICS ARE ONLY AVAILABLE IN A PLAYER BUILD");
                stringBuilder.AppendLine($"Missing Disk Data: {EditorUtility.FormatBytes(GetCounterValue(f, "Missing Disk Data"))}");
                stringBuilder.AppendLine($"Missing Streaming tiles: {GetCounterValue(f, "Missing Streaming Tiles")}");
                stringBuilder.AppendLine($"Read From Disk: {EditorUtility.FormatBytes(GetCounterValue(f, "Read From Disk"))}");

                //AGGREGATED DATA
                string aggregatedText = stringBuilder.ToString();
                float aggregateHeight = EditorStyles.wordWrappedLabel.CalcHeight(GUIContent.Temp(aggregatedText), rect.width);
                m_ScrollAggregate = GUI.BeginScrollView(aggregatedInfoBox, m_ScrollAggregate, new Rect(0, 0, 200, aggregateHeight));
                GUI.Label(new Rect(0, 0, aggregatedInfoBox.width * 0.9f, aggregateHeight), aggregatedText);
                GUI.EndScrollView();

                //CACHE DEMANDS
                StringBuilder formats = new StringBuilder();
                StringBuilder demands = new StringBuilder();
                var demandData = fRender.GetFrameMetaData<int>(m_VTProfilerGuid, 1);
                for (int i = 0; i < demandData.Length; i += 2)
                {
                    formats.AppendLine(((GraphicsFormat)demandData[i]).ToString());
                    demands.AppendLine(demandData[i + 1].ToString() + '%');
                }

                GUI.Label(cacheDemandTitle, "% Cache demands");
                float demandHeight = EditorStyles.wordWrappedLabel.CalcHeight(GUIContent.Temp(formats.ToString()), rect.width);
                m_ScrollDemand = GUI.BeginScrollView(cacheDemandScrollBox, m_ScrollDemand, new Rect(0, 0, 200, demandHeight));
                cacheFormatBox.height = demandHeight;
                cacheValueBox.height = demandHeight;
                GUI.Label(cacheFormatBox, formats.ToString());
                GUI.Label(cacheValueBox, demands.ToString());
                GUI.EndScrollView();

                //CACHE BIAS
                formats.Clear();
                demands.Clear();
                var biasData = fRender.GetFrameMetaData<int>(m_VTProfilerGuid, 0);
                for (int i = 0; i < biasData.Length; i += 2)
                {
                    formats.AppendLine(((GraphicsFormat)biasData[i]).ToString());
                    demands.AppendLine(biasData[i + 1].ToString());
                }

                GUI.Label(cacheBiasTitle, "Mipmap bias per format");
                m_ScrollBias = GUI.BeginScrollView(cacheBiasScollBox, m_ScrollBias, new Rect(0, 0, 200, demandHeight));
                GUI.Label(cacheFormatBox, formats.ToString());
                GUI.Label(cacheValueBox, demands.ToString());
                GUI.EndScrollView();
            }
            else GUI.Label(aggregatedInfoBox, "No frame data available");
            EditorGUILayout.EndVertical();
        }

        static long GetCounterValue(FrameDataView frameData, string name)
        {
            var id = frameData.GetMarkerId(name);
            if (id == FrameDataView.invalidMarkerId)
                return -1;

            return frameData.GetCounterValueAsInt(id);
        }
    }
}
