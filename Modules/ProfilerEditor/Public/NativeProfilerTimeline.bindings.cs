// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Accessibility;

namespace UnityEditorInternal
{
    public struct ProfilerColorDescriptor
    {
        public readonly Color color;
        public readonly bool isBright;
        const float k_LuminanceThreshold = 0.7f;
        public ProfilerColorDescriptor(Color color)
        {
            this.color = color;
            float lum = VisionUtility.ComputePerceivedLuminance(color);
            isBright = lum >= k_LuminanceThreshold;
        }
    }

    public struct NativeProfilerTimeline_InitializeArgs
    {
        public float ghostAlpha;
        public float nonSelectedAlpha;
        public float lineHeight;
        public float textFadeOutWidth;
        public float textFadeStartWidth;
        public IntPtr guiStyle;
        public ProfilerColorDescriptor[] profilerColorDescriptors;
        public int showFullScriptingMethodNames;

        public void Reset()
        {
            ghostAlpha = 0;
            nonSelectedAlpha = 0;
            guiStyle = IntPtr.Zero;
            lineHeight = 0;
            textFadeOutWidth = 0;
            textFadeStartWidth = 0;
            profilerColorDescriptors = null;
            showFullScriptingMethodNames = 1;
        }
    }

    public struct NativeProfilerTimeline_DrawArgs
    {
        public int frameIndex;
        public int threadIndex;
        public float timeOffset;
        public Rect threadRect;
        public Rect shownAreaRect;
        public int selectedEntryIndex;
        public int mousedOverEntryIndex;

        public void Reset()
        {
            frameIndex = -1;
            threadIndex = -1;
            timeOffset = 0;
            threadRect = Rect.zero;
            shownAreaRect = Rect.zero;
            selectedEntryIndex = -1;
            mousedOverEntryIndex = -1;
        }
    }

    public struct NativeProfilerTimeline_GetEntryAtPositionArgs
    {
        public int frameIndex;
        public int threadIndex;
        public float timeOffset;
        public Rect threadRect;
        public Rect shownAreaRect;
        public Vector2 position;

        public int out_EntryIndex;
        public float out_EntryYMaxPos;
        public string out_EntryName;

        public void Reset()
        {
            frameIndex = -1;
            threadIndex = -1;
            timeOffset = 0;
            threadRect = Rect.zero;
            shownAreaRect = Rect.zero;
            position = Vector2.zero;

            out_EntryIndex = -1;
            out_EntryYMaxPos = 0.0f;
            out_EntryName = string.Empty;
        }
    }

    public struct NativeProfilerTimeline_GetEntryInstanceInfoArgs
    {
        public int frameIndex;
        public int threadIndex;
        public int entryIndex;

        public int out_Id;
        public string out_Path;
        public string out_CallstackInfo;
        public string out_MetaData;

        public void Reset()
        {
            frameIndex = -1;
            threadIndex = -1;
            entryIndex = -1;

            out_Id = 0;
            out_Path = string.Empty;
            out_CallstackInfo = string.Empty;
            out_MetaData = string.Empty;
        }
    }

    public struct NativeProfilerTimeline_GetEntryTimingInfoArgs
    {
        public int frameIndex;
        public int threadIndex;
        public int entryIndex;
        public bool calculateFrameData;

        public float out_LocalStartTime;
        public float out_Duration;
        public float out_TotalDurationForFrame;
        public int out_InstanceCountForFrame;

        public void Reset()
        {
            frameIndex = -1;
            threadIndex = -1;
            entryIndex = -1;
            calculateFrameData = false;

            out_LocalStartTime = -1.0f;
            out_Duration = -1.0f;
            out_TotalDurationForFrame = -1.0f;
            out_InstanceCountForFrame = -1;
        }
    }

    public struct NativeProfilerTimeline_GetEntryPositionInfoArgs
    {
        public int frameIndex;
        public int threadIndex;
        public int sampleIndex;
        public float timeOffset;
        public Rect threadRect;
        public Rect shownAreaRect;

        public Vector2 out_Position;
        public Vector2 out_Size;
        public int out_Depth;

        public void Reset()
        {
            frameIndex = -1;
            threadIndex = -1;
            sampleIndex = -1;
            timeOffset = 0;
            threadRect = Rect.zero;
            shownAreaRect = Rect.zero;

            out_Position = Vector2.zero;
            out_Size = Vector2.zero;
            out_Depth = 0;
        }
    }

    [NativeHeader("Modules/ProfilerEditor/Timeline/NativeProfilerTimeline.h")]
    public class NativeProfilerTimeline
    {
        [FreeFunction]
        public static extern void Initialize(ref NativeProfilerTimeline_InitializeArgs args);

        [FreeFunction]
        public static extern void Draw(ref NativeProfilerTimeline_DrawArgs args);

        [FreeFunction]
        public static extern bool GetEntryAtPosition(ref NativeProfilerTimeline_GetEntryAtPositionArgs args);

        [FreeFunction]
        public static extern bool GetEntryInstanceInfo(ref NativeProfilerTimeline_GetEntryInstanceInfoArgs args);

        [FreeFunction]
        public static extern bool GetEntryTimingInfo(ref NativeProfilerTimeline_GetEntryTimingInfoArgs args);

        [FreeFunction]
        public static extern bool GetEntryPositionInfo(ref NativeProfilerTimeline_GetEntryPositionInfoArgs args);
    }
}
