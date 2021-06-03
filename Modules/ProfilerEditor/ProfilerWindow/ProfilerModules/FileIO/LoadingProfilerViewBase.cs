// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Profiling;
using Unity.Profiling;
using System.IO;
using UnityEditorInternal.Profiling;

namespace UnityEditor
{
    [Serializable]
    internal class LoadingProfilerViewBase
    {
        private int m_CurrentFrame = -1;
        public int GetSelectedFrame() { return m_CurrentFrame; }
        public void SetSelectedFrame(int frameIndex) { m_CurrentFrame = frameIndex; }

        public bool NewData { get; set; }

        [NonSerialized] public bool DataPulled = false;

        ProfilerWindow m_ProfilerWindow;
        IProfilerFrameTimeViewSampleSelectionController m_CpuProfilerModule;

        public LoadingProfilerViewBase() {}

        public void OnEnable()
        {
            DataPulled = false;
        }

        private void GetProfiler()
        {
            if (m_ProfilerWindow == null)
            {
                m_ProfilerWindow = EditorWindow.GetWindow<ProfilerWindow>();
            }
            if (m_CpuProfilerModule == null)
            {
                m_CpuProfilerModule = m_ProfilerWindow.GetFrameTimeViewSampleSelectionController(ProfilerWindow.cpuModuleIdentifier);
            }
        }

        public void OpenMarkerInCpuModule(long frameIndex, string threadGroupName, string threadName, ulong threadId, int threadIndex, int sampleIndex)
        {
            GetProfiler();
            if (frameIndex < m_ProfilerWindow.firstAvailableFrameIndex || frameIndex > m_ProfilerWindow.lastAvailableFrameIndex)
            {
                Debug.Log($"Frame index {frameIndex} out of range ({m_ProfilerWindow.firstAvailableFrameIndex} - { m_ProfilerWindow.lastAvailableFrameIndex}");
                return;
            }
            m_ProfilerWindow.selectedModule = m_ProfilerWindow.GetProfilerModuleByType<CPUProfilerModule>();
            var cpuModule = m_ProfilerWindow.selectedModule as CPUOrGPUProfilerModule;
            cpuModule.ViewType = ProfilerViewType.Timeline;
            m_CpuProfilerModule.SetSelection(new ProfilerTimeSampleSelection(frameIndex, threadGroupName, threadName, threadId, sampleIndex));
            // m_CpuProfilerModule.focusedThreadIndex = (int)threadIndex; // I'm hoping this could autoexpand but currently gives a null ref exception
        }

        public void GoToFrameInModule(int frameIndex)
        {
            GetProfiler();
            m_ProfilerWindow.selectedFrameIndex = frameIndex;
            NewData = true;
        }

        static public void UpdateDepthStack(ref Stack<int> depthStack, int childCount)
        {
            if (childCount > 0)
            {
                depthStack.Push(childCount);
            }
            else
            {
                while (depthStack.Count > 0)
                {
                    int remainingChildren = depthStack.Pop();
                    if (remainingChildren > 1)
                    {
                        depthStack.Push(remainingChildren - 1);
                        break;
                    }
                }
            }
        }

        static public string GetFullThreadName(string groupName, string threadName)
        {
            return string.IsNullOrEmpty(groupName) ? threadName : string.Format("{0}.{1}", groupName, threadName);
        }

        static public ulong AbsDiff(ulong val1, ulong val2)
        {
            if (val1 >= val2)
                return val1 - val2;
            else
                return val2 - val1;
        }
    }
}
