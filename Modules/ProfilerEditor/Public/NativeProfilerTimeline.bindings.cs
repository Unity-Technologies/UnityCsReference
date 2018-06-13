// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditorInternal
{
    public struct NativeProfilerTimeline_InitializeArgs
    {
        public float ghostAlpha;
        public float nonSelectedAlpha;
        public IntPtr guiStyle;
        public float lineHeight;
        public float textFadeOutWidth;
        public float textFadeStartWidth;

        public void Reset()
        {
            ghostAlpha = 0;
            nonSelectedAlpha = 0;
            guiStyle = (IntPtr)0;
            lineHeight = 0;
            textFadeOutWidth = 0;
            textFadeStartWidth = 0;
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
    }
}
