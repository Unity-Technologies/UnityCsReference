// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Accessibility;
using UnityEditorInternal.Profiling;
using UnityEngine;
using UnityEngine.Accessibility;

namespace UnityEditorInternal
{
    internal class ProfilerColors
    {
        static ProfilerColors()
        {
            s_DefaultColors = new Color[]
            {
                FrameDataView.GetMarkerCategoryColor(0), // "Render"
                FrameDataView.GetMarkerCategoryColor(1), // "Scripts"
                FrameDataView.GetMarkerCategoryColor(2), // "Managed Jobs"
                FrameDataView.GetMarkerCategoryColor(3), // "Burst Jobs"
                FrameDataView.GetMarkerCategoryColor(4), // "GUI"
                FrameDataView.GetMarkerCategoryColor(5), // "Physics"
                FrameDataView.GetMarkerCategoryColor(6), // "Animation"
                FrameDataView.GetMarkerCategoryColor(7), // "AI"
                FrameDataView.GetMarkerCategoryColor(8), // "Audio"
                FrameDataView.GetMarkerCategoryColor(9), // "Video"
                FrameDataView.GetMarkerCategoryColor(10), // "Particles"
                FrameDataView.GetMarkerCategoryColor(11), // "Gi"
                FrameDataView.GetMarkerCategoryColor(12), // "Network"
                FrameDataView.GetMarkerCategoryColor(13), // "Loading"
                FrameDataView.GetMarkerCategoryColor(14), // "Other"
                FrameDataView.GetMarkerCategoryColor(15), // "GC"
                FrameDataView.GetMarkerCategoryColor(16), // "VSync"
                FrameDataView.GetMarkerCategoryColor(17), // "Overhead"
                FrameDataView.GetMarkerCategoryColor(18), // "PlayerLoop"
                FrameDataView.GetMarkerCategoryColor(19), // "Director"
                FrameDataView.GetMarkerCategoryColor(20), // "VR"
                FrameDataView.GetMarkerCategoryColor(21), // "NativeMem"
                FrameDataView.GetMarkerCategoryColor(22), // "Internal"
                FrameDataView.GetMarkerCategoryColor(23), // "FileIO"
                FrameDataView.GetMarkerCategoryColor(24), // "UI Layout"
                FrameDataView.GetMarkerCategoryColor(25), // "UI Render"
                FrameDataView.GetMarkerCategoryColor(26), // "VFX"
                FrameDataView.GetMarkerCategoryColor(27), // "Build Interface"
                FrameDataView.GetMarkerCategoryColor(28), // "Input"
            };
            s_DefaultColorsLuminanceValues  = new float[s_DefaultColors.Length];
            VisionUtility.GetLuminanceValuesForPalette(s_DefaultColors, ref s_DefaultColorsLuminanceValues);
            // Areas are defined by stats in ProfilerStats.cpp file.
            // Color are driven by CPU profiler chart area colors and must be consistent with CPU timeline sample colors.
            // Sample color is defined by ProfilerGroup (category) and defined in s_ProfilerGroupInfos table.
            s_DefaultChartColors = new Color[]
            {
                s_DefaultColors[0],                   // "Rendering"
                s_DefaultColors[1],                   // "Scripts"
                s_DefaultColors[5],                   // "Physics"
                s_DefaultColors[6],                   // "Animation"
                s_DefaultColors[15],                  // "GarbageCollector"
                s_DefaultColors[16],                  // "VSync"
                s_DefaultColors[11],                  // "Global Illumination"
                s_DefaultColors[24],                  // "UI"
                s_DefaultColors[14],                  // "Others"
                // Colors below are currently only used in Timeline view
                s_DefaultColors[8],                   // "Audio"
                s_DefaultColors[21],                  // "Memory Alloc"
                s_DefaultColors[22],                  // "Internal"
                s_DefaultColors[27],                  // "Build Interface"
                s_DefaultColors[28],                  // "Input"
            };
            s_ColorBlindSafeChartColors = new Color[s_DefaultChartColors.Length];
            VisionUtility.GetColorBlindSafePalette(s_ColorBlindSafeChartColors, 0.3f, 1f);

            s_ColorBlindSafeColors  = new Color[]
            {
                s_ColorBlindSafeChartColors[0], // "Render"
                s_ColorBlindSafeChartColors[1], // "Scripts"
                s_ColorBlindSafeChartColors[1], // "Managed Jobs"
                s_ColorBlindSafeChartColors[1], // "Burst Jobs"
                s_ColorBlindSafeChartColors[8], // "GUI"
                s_ColorBlindSafeChartColors[3], // "Physics"
                s_ColorBlindSafeChartColors[4], // "Animation"
                s_ColorBlindSafeChartColors[8], // "AI"
                s_ColorBlindSafeChartColors[9], // "Audio"
                s_ColorBlindSafeChartColors[8], // "Video"
                s_ColorBlindSafeChartColors[8], // "Particles"
                s_ColorBlindSafeChartColors[6], // "Gi"
                s_ColorBlindSafeChartColors[8], // "Network"
                s_ColorBlindSafeChartColors[8], // "Loading"
                s_ColorBlindSafeChartColors[8], // "Other"
                s_ColorBlindSafeChartColors[4], // "GC"
                s_ColorBlindSafeChartColors[5], // "VSync"
                s_ColorBlindSafeChartColors[8], // "Overhead"
                s_ColorBlindSafeChartColors[8], // "PlayerLoop"
                s_ColorBlindSafeChartColors[8], // "Director"
                s_ColorBlindSafeChartColors[8], // "VR"
                s_ColorBlindSafeChartColors[10], // "NativeMem"
                s_ColorBlindSafeChartColors[11], // "Internal"
                s_ColorBlindSafeChartColors[8], // "FileIO"
                s_ColorBlindSafeChartColors[7], // "UI Layout"
                s_ColorBlindSafeChartColors[7], // "UI Render"
                s_ColorBlindSafeChartColors[8], // "VFX"
                s_ColorBlindSafeChartColors[12], // "Build Interface"
                s_ColorBlindSafeChartColors[13], // "Input"
            };
            s_ColorBlindSafeColorsLuminanceValues = new float[s_ColorBlindSafeColors.Length];
            VisionUtility.GetLuminanceValuesForPalette(s_ColorBlindSafeColors, ref s_ColorBlindSafeColorsLuminanceValues);
        }

        public static Color[] chartAreaColors
        {
            get { return UserAccessiblitySettings.colorBlindCondition == ColorBlindCondition.Default ? s_DefaultChartColors : s_ColorBlindSafeChartColors; }
        }

        public static Color[] timelineColors
        {
            get { return UserAccessiblitySettings.colorBlindCondition == ColorBlindCondition.Default ? s_DefaultColors : s_ColorBlindSafeColors; }
        }

        public static float[] timelineColorsLuminance
        {
            get { return UserAccessiblitySettings.colorBlindCondition == ColorBlindCondition.Default ? s_DefaultColorsLuminanceValues : s_ColorBlindSafeColorsLuminanceValues; }
        }

        static readonly Color[] s_DefaultColors;
        static readonly float[] s_DefaultColorsLuminanceValues;
        static readonly Color[] s_ColorBlindSafeColors;
        static readonly float[] s_ColorBlindSafeColorsLuminanceValues;
        static readonly Color[] s_DefaultChartColors;
        static readonly Color[] s_ColorBlindSafeChartColors;
    }
}
