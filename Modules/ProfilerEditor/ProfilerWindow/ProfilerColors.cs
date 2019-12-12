// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Accessibility;
using UnityEditor.Profiling;
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
                HierarchyFrameDataView.GetMarkerCategoryColor(0), // "Render"
                HierarchyFrameDataView.GetMarkerCategoryColor(1), // "Scripts"
                HierarchyFrameDataView.GetMarkerCategoryColor(2), // "Managed Jobs"
                HierarchyFrameDataView.GetMarkerCategoryColor(3), // "Burst Jobs"
                HierarchyFrameDataView.GetMarkerCategoryColor(4), // "GUI"
                HierarchyFrameDataView.GetMarkerCategoryColor(5), // "Physics"
                HierarchyFrameDataView.GetMarkerCategoryColor(6), // "Animation"
                HierarchyFrameDataView.GetMarkerCategoryColor(7), // "AI"
                HierarchyFrameDataView.GetMarkerCategoryColor(8), // "Audio"
                HierarchyFrameDataView.GetMarkerCategoryColor(9), // "Audio Job"
                HierarchyFrameDataView.GetMarkerCategoryColor(10), // "Audio Update Job
                HierarchyFrameDataView.GetMarkerCategoryColor(11), // "Video"
                HierarchyFrameDataView.GetMarkerCategoryColor(12), // "Particles"
                HierarchyFrameDataView.GetMarkerCategoryColor(13), // "Gi"
                HierarchyFrameDataView.GetMarkerCategoryColor(14), // "Network"
                HierarchyFrameDataView.GetMarkerCategoryColor(15), // "Loading"
                HierarchyFrameDataView.GetMarkerCategoryColor(16), // "Other"
                HierarchyFrameDataView.GetMarkerCategoryColor(17), // "GC"
                HierarchyFrameDataView.GetMarkerCategoryColor(18), // "VSync"
                HierarchyFrameDataView.GetMarkerCategoryColor(19), // "Overhead"
                HierarchyFrameDataView.GetMarkerCategoryColor(20), // "PlayerLoop"
                HierarchyFrameDataView.GetMarkerCategoryColor(21), // "Director"
                HierarchyFrameDataView.GetMarkerCategoryColor(22), // "VR"
                HierarchyFrameDataView.GetMarkerCategoryColor(23), // "NativeMem"
                HierarchyFrameDataView.GetMarkerCategoryColor(24), // "Internal"
                HierarchyFrameDataView.GetMarkerCategoryColor(25), // "FileIO"
                HierarchyFrameDataView.GetMarkerCategoryColor(26), // "UI Layout"
                HierarchyFrameDataView.GetMarkerCategoryColor(27), // "UI Render"
                HierarchyFrameDataView.GetMarkerCategoryColor(28), // "VFX"
                HierarchyFrameDataView.GetMarkerCategoryColor(29), // "Build Interface"
                HierarchyFrameDataView.GetMarkerCategoryColor(30), // "Input"
                HierarchyFrameDataView.GetMarkerCategoryColor(31), // "Virtual Texturing"
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
                s_DefaultColors[17],                  // "GarbageCollector"
                s_DefaultColors[18],                  // "VSync"
                s_DefaultColors[13],                  // "Global Illumination"
                s_DefaultColors[26],                  // "UI"
                s_DefaultColors[16],                  // "Others"
                // Colors below are currently only used in Timeline view
                s_DefaultColors[8],                   // "Audio"
                s_DefaultColors[9],                   // "Audio Job"
                s_DefaultColors[10],                  // "Audio Update Job"
                s_DefaultColors[23],                  // "Memory Alloc"
                s_DefaultColors[24],                  // "Internal"
                s_DefaultColors[29],                  // "Build Interface"
                s_DefaultColors[30],                  // "Input"
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
                s_ColorBlindSafeChartColors[10], // "Audio Job"
                s_ColorBlindSafeChartColors[11], // "Audio Update Job
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
                s_ColorBlindSafeChartColors[12], // "NativeMem"
                s_ColorBlindSafeChartColors[13], // "Internal"
                s_ColorBlindSafeChartColors[8], // "FileIO"
                s_ColorBlindSafeChartColors[7], // "UI Layout"
                s_ColorBlindSafeChartColors[7], // "UI Render"
                s_ColorBlindSafeChartColors[8], // "VFX"
                s_ColorBlindSafeChartColors[14], // "Build Interface"
                s_ColorBlindSafeChartColors[15], // "Input"
                s_ColorBlindSafeChartColors[8], // "Virtual Texturing"
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
