// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEditor.Accessibility;
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
                ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.Render),
                ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.Scripts),
                ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.BurstJobs),
                ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.Other),
                ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.Physics),
                ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.Animation),
                ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.Audio),
                ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.AudioJob),
                ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.AudioUpdateJob),
                ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.Lighting),
                ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.GC),
                ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.VSync),
                ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.Memory),
                ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.Internal),
                ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.UI),
                ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.Build),
                ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.Input),
            };

            s_DefaultColorsLuminanceValues = new float[s_DefaultColors.Length];
            VisionUtility.GetLuminanceValuesForPalette(s_DefaultColors, ref s_DefaultColorsLuminanceValues);

            // Chart Areas are defined by stats in ProfilerStats.cpp file.
            // Color are driven by CPU profiler chart area colors and must be consistent with CPU timeline sample colors.
            ProfilerCategoryColor[] defaultChartColors = new ProfilerCategoryColor[]
            {
                ProfilerCategoryColor.Render,
                ProfilerCategoryColor.Scripts,
                ProfilerCategoryColor.Physics,
                ProfilerCategoryColor.Animation,
                ProfilerCategoryColor.GC,
                ProfilerCategoryColor.VSync,
                ProfilerCategoryColor.Lighting,
                ProfilerCategoryColor.UI,
                ProfilerCategoryColor.Other,
                // Colors below are currently only used in Timeline view
                ProfilerCategoryColor.Audio,
                ProfilerCategoryColor.AudioJob,
                ProfilerCategoryColor.AudioUpdateJob,
                ProfilerCategoryColor.Memory,
                ProfilerCategoryColor.Internal,
                ProfilerCategoryColor.Build,
                ProfilerCategoryColor.Input,
            };

            s_DefaultChartColors = new Color[defaultChartColors.Length];
            for (int i = 0; i < defaultChartColors.Length; i++)
            {
                var colorIndex = (int)defaultChartColors[i];
                s_DefaultChartColors[i] = s_DefaultColors[colorIndex];
            }

            s_ColorBlindSafeChartColors = new Color[s_DefaultChartColors.Length];
            VisionUtility.GetColorBlindSafePalette(s_ColorBlindSafeChartColors, 0.3f, 1f);

            s_ColorBlindSafeColors = new Color[]
            {
                s_ColorBlindSafeChartColors[0],
                s_ColorBlindSafeChartColors[1],
                s_ColorBlindSafeChartColors[1],
                s_ColorBlindSafeChartColors[8],
                s_ColorBlindSafeChartColors[2],
                s_ColorBlindSafeChartColors[3],
                s_ColorBlindSafeChartColors[9],
                s_ColorBlindSafeChartColors[10],
                s_ColorBlindSafeChartColors[11],
                s_ColorBlindSafeChartColors[6],
                s_ColorBlindSafeChartColors[4],
                s_ColorBlindSafeChartColors[5],
                s_ColorBlindSafeChartColors[12],
                s_ColorBlindSafeChartColors[13],
                s_ColorBlindSafeChartColors[7],
                s_ColorBlindSafeChartColors[14],
                s_ColorBlindSafeChartColors[15],
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
