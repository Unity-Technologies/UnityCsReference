// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using System.Globalization;
using System.Text;
using Unity.Profiling.LowLevel;
using System.Runtime.CompilerServices;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal class SampleSelection
    {
        public static readonly SampleSelection InvalidSampleSelection = new SampleSelection(FrameDataView.invalidOrCurrentFrameIndex, "Invalid", "Invalid", FrameDataView.invalidThreadId, RawFrameDataView.invalidSampleIndex, "Invalid");

        [SerializeField]
        bool m_Valid;
        // most indicators for validity are readonly, except for the rawSampleIndices, where only the reference is readonly
        // there must however always be a rawIndex
        public bool valid { get { return m_Valid && rawSampleIndex != RawFrameDataView.invalidSampleIndex; } }

        // The initially set frame index. When the Profiler data that the selection was made in was unloaded
        [field: SerializeField]
        public long unsafeFrameIndex { get; private set; }
        // err on the side of caution, don't serialize this.
        // This bool and the safe frameIndex are used for a performance optimization and precise selection of one particular instance of a sample when switching views on the same frame.
        [NonSerialized]
        internal bool frameIndexIsSafe = false;
        public long frameIndex => frameIndexIsSafe ? unsafeFrameIndex : FrameDataView.invalidOrCurrentFrameIndex;


        [field: SerializeField]
        public string threadGroupName { get; private set; }
        [field: SerializeField]
        public string threadName { get; private set; }
        // if multiple threads with the same name exist and one matches this id, that one is chosen, otherwise the first one is chosen
        [field: SerializeField]
        public ulong threadId { get; private set; }
        [field: SerializeField]
        public string sampleName { get; private set; }
        // this is only used for ProfilerDriver.selectedPropertyPath
        [field: SerializeField]
        internal string legacyMarkerPath { get; private set; }
        [SerializeField]
        List<string> m_MarkerNamePath;
        public ReadOnlyCollection<string> markerNamePath => m_MarkerNamePath?.AsReadOnly();
        [SerializeField]
        List<int> m_MarkerIdPath;
        public ReadOnlyCollection<int> markerIdPath => m_MarkerIdPath?.AsReadOnly();
        [field: SerializeField]
        public int markerPathDepth { get; private set; }
        [SerializeField]
        List<int> m_RawSampleIndices;
        public ReadOnlyCollection<int> rawSampleIndices => m_RawSampleIndices?.AsReadOnly();
        public int rawSampleIndex
        {
            get
            {
                if (rawSampleIndices == null || rawSampleIndices.Count <= 0)
                    return RawFrameDataView.invalidSampleIndex;
                return rawSampleIndices[0];
            }
            private set
            {
                if (rawSampleIndices == null)
                    m_RawSampleIndices = new List<int>(1);
                m_RawSampleIndices[0] = value;
            }
        }

        public SampleSelection(int frameIndex, string threadGroupName, string threadName, ulong threadId, int rawSampleIndex, string sampleName = null)
        {
            this.unsafeFrameIndex = frameIndex;
            // frame index is safe until proven otherwise by Integrity checks in CPUOrGPUProfilerModule.SetSelection
            frameIndexIsSafe = false;
            if (threadGroupName == null)
                threadGroupName = string.Empty; // simplify this to empty
            this.threadGroupName = threadGroupName;
            this.threadName = threadName;
            this.threadId = threadId;
            if (rawSampleIndex >= 0)
                m_RawSampleIndices = new List<int> { rawSampleIndex };
            else
                m_RawSampleIndices = null;
            this.sampleName = sampleName;
            legacyMarkerPath = null;
            m_MarkerNamePath = null;
            m_MarkerIdPath = null;
            markerPathDepth = 0;
            m_Valid = frameIndex >= 0 && !string.IsNullOrEmpty(threadName) && rawSampleIndex >= 0;
        }

        public SampleSelection(int frameIndex, string threadGroupName, string threadName, ulong threadId, IList<int> rawSampleIndices, string sampleName = null)
        {
            this.unsafeFrameIndex = frameIndex;
            // frame index is safe until proven otherwise by Integrity checks in CPUOrGPUProfilerModule.SetSelection
            frameIndexIsSafe = false;
            if (threadGroupName == null)
                threadGroupName = string.Empty; // simplify this to empty
            this.threadGroupName = threadGroupName;
            this.threadName = threadName;
            this.threadId = threadId;
            this.m_RawSampleIndices = new List<int>(rawSampleIndices);
            this.sampleName = sampleName;
            legacyMarkerPath = null;
            m_MarkerNamePath = null;
            m_MarkerIdPath = null;
            markerPathDepth = 0;
            m_Valid = frameIndex >= 0 && !string.IsNullOrEmpty(threadName) && rawSampleIndices.Count > 0 && rawSampleIndices[0] > 0;
        }

        // Deep copy constructor
        public SampleSelection(SampleSelection selection)
        {
            unsafeFrameIndex = selection.unsafeFrameIndex;
            // frame index is safe until proven otherwise by Integrity checks in CPUOrGPUProfilerModule.SetSelection
            // creating a copy means the CPUOrGPUProfilerModule no longer has control over this, so assume its no longer safe
            frameIndexIsSafe = false;
            threadName = selection.threadName;
            threadGroupName = selection.threadGroupName;
            threadId = selection.threadId;
            sampleName = selection.sampleName;
            legacyMarkerPath = selection.legacyMarkerPath;

            if (selection.rawSampleIndices != null)
            {
                m_RawSampleIndices = new List<int>(selection.rawSampleIndices.Count);
                m_RawSampleIndices.AddRange(selection.rawSampleIndices);
            }
            else
            {
                m_RawSampleIndices = new List<int>() { selection.rawSampleIndex };
            }
            markerPathDepth = 0;

            if (selection.markerNamePath != null)
            {
                m_MarkerNamePath = new List<string>(selection.markerNamePath);
                markerPathDepth = m_MarkerNamePath.Count;
            }
            else
            {
                m_MarkerNamePath = null;
            }

            if (selection.markerIdPath != null)
            {
                m_MarkerIdPath = new List<int>(selection.markerIdPath);
                if (markerPathDepth != m_MarkerIdPath.Count)
                    throw new ArgumentException($"The Selection had a different marker path depth for {nameof(markerIdPath)} than for {nameof(markerNamePath)}, but they must be in sync");
                markerPathDepth = m_MarkerIdPath.Count;
            }
            else
            {
                if (markerPathDepth > 0)
                    throw new ArgumentException($"The Selection had a different marker path depth for {nameof(markerIdPath)} than for {nameof(markerNamePath)}, but they must be in sync");
                m_MarkerIdPath = null;
            }

            m_Valid = selection.valid;
        }

        // NOTE: Only pass legacyMarkerNamePath if you already got it anyways (e.g. through Native Timeline selection API), otherwise leave it null, it will be generated here.
        internal void GenerateMarkerNamePath(FrameDataView frameDataView, string sampleName, List<int> markerIdPath, string legacyMarkerNamePath = null)
        {
            this.sampleName = sampleName;
            GenerateMarkerNamePath(frameDataView, markerIdPath, legacyMarkerNamePath);
        }

        const string k_EditorOnlyPrefix = "EditorOnly [";
        // NOTE: Only pass legacyMarkerNamePath if you already got it anyways (e.g. through Native Timeline selection API), otherwise leave it null, it will be generated here.
        internal void GenerateMarkerNamePath(FrameDataView frameDataView, List<int> markerIdPath, string legacyMarkerNamePath = null)
        {
            if (markerIdPath == null || markerIdPath.Count <= 0)
                throw new ArgumentException($"{nameof(markerIdPath)} can't be null or empty", nameof(markerIdPath));
            m_MarkerIdPath = markerIdPath;
            if (legacyMarkerNamePath == null)
            {
                // TODO: replace with MutableString once available
                var propertyPathBuilder = new StringBuilder();
                markerPathDepth = markerIdPath.Count;
                m_MarkerNamePath = new List<string>(markerPathDepth);
                for (int i = 0; i < markerPathDepth - 1; i++)
                {
                    var name = frameDataView.GetMarkerName(markerIdPath[i]);
                    if (IsEditorOnlyMarker(frameDataView, markerIdPath[i]))
                    {
                        // if this was an Editor Only sample, get the proper id and name
                        markerIdPath[i] = GetNonEditorOnlyMarkerNameAndId(frameDataView, ref name, markerIdPath[i]);
                    }
                    propertyPathBuilder.Append(name);
                    propertyPathBuilder.Append('/');
                    m_MarkerNamePath.Add(name);
                }
                var lastMarkerId = markerIdPath[markerPathDepth - 1];
                // note: not necessarily the same as this.sampleName because that could be a deep profiling scripting marker name with the assembly name stripped
                var lastSampleName = frameDataView.GetMarkerName(lastMarkerId);

                if (IsEditorOnlyMarker(frameDataView, lastMarkerId))
                {
                    // if this was an Editor Only sample, get the proper id and name
                    markerIdPath[markerPathDepth - 1] = GetNonEditorOnlyMarkerNameAndId(frameDataView, ref lastSampleName, lastMarkerId);
                    // if it isn't a Deep Profiling sample, update this.sampleName too
                    if ((int)(frameDataView.GetMarkerFlags(markerIdPath[markerPathDepth - 1]) & MarkerFlags.ScriptDeepProfiler) == 0)
                        sampleName = lastSampleName;
                }
                propertyPathBuilder.Append(lastSampleName);
                m_MarkerNamePath.Add(lastSampleName);
                this.legacyMarkerPath = propertyPathBuilder.ToString();
            }
            else
            {
                // TODO: replace with MutableString once available
                markerPathDepth = markerIdPath.Count;
                m_MarkerNamePath = new List<string>(markerPathDepth);
                for (int i = 0; i < markerPathDepth - 1; i++)
                {
                    var name = frameDataView.GetMarkerName(markerIdPath[i]);
                    if (IsEditorOnlyMarker(frameDataView, markerIdPath[i]))
                    {
                        // if this was an Editor Only sample, get the proper id and name
                        markerIdPath[i] = GetNonEditorOnlyMarkerNameAndId(frameDataView, ref name, markerIdPath[i]);
                    }
                    m_MarkerNamePath.Add(name);
                }
                var lastMarkerId = markerIdPath[markerPathDepth - 1];
                // note: not necessarily the same as this.sampleName because that could be a deep profiling scripting marker name with the assembly name stripped
                var lastSampleName = frameDataView.GetMarkerName(markerIdPath[markerPathDepth - 1]);

                if (IsEditorOnlyMarker(frameDataView, lastMarkerId))
                {
                    // if this was an Editor Only sample, get the proper id and name
                    markerIdPath[markerPathDepth - 1] = GetNonEditorOnlyMarkerNameAndId(frameDataView, ref lastSampleName, lastMarkerId);
                    // if it isn't a Deep Profiling sample, update this.sampleName too
                    if ((int)(frameDataView.GetMarkerFlags(markerIdPath[markerPathDepth - 1]) & MarkerFlags.ScriptDeepProfiler) == 0)
                        sampleName = lastSampleName;
                }
                m_MarkerNamePath.Add(lastSampleName);
                this.legacyMarkerPath = legacyMarkerNamePath;
            }
        }

        [MethodImpl(256 /*MethodImplOptions.AggressiveInlining*/)]
        static bool IsEditorOnlyMarker(FrameDataView frameDataView, int markerId)
        {
            return markerId < 0 && (int)(frameDataView.GetMarkerFlags(markerId) & MarkerFlags.AvailabilityEditor) != 1;
        }

        [MethodImpl(256 /*MethodImplOptions.AggressiveInlining*/)]
        static int GetNonEditorOnlyMarkerNameAndId(FrameDataView frameDataView, ref string name, int markerId)
        {
            if (name.StartsWith(k_EditorOnlyPrefix))
            {
                name = name.Substring(k_EditorOnlyPrefix.Length, name.Length - (k_EditorOnlyPrefix.Length + 1)); // + for the closing ]
                return frameDataView.GetMarkerId(name);
            }
            return markerId;
        }

        internal static void GetCleanMarkerIdsFromSampleIds(HierarchyFrameDataView frameData, List<int> sampleIdPath, List<int> markerIdPath)
        {
            for (int i = 0; i < sampleIdPath.Count; i++)
            {
                var markerId = frameData.GetItemMarkerID(sampleIdPath[i]);
                if (IsEditorOnlyMarker(frameData, markerId))
                {
                    // if this was an Editor Only sample, get the proper id and name
                    var name = frameData.GetItemName(sampleIdPath[i]);
                    markerId = GetNonEditorOnlyMarkerNameAndId(frameData, ref name, markerId);
                }
                markerIdPath.Add(markerId);
            }
        }

        internal int GetThreadIndex(int frameIndex)
        {
            return GetThreadIndex(frameIndex, threadGroupName, threadName, threadId);
        }

        internal static int GetThreadIndex(int frameIndex, string threadGroupName, string threadName, ulong threadId)
        {
            if (frameIndex > ProfilerDriver.lastFrameIndex || frameIndex < ProfilerDriver.firstFrameIndex)
                return FrameDataView.invalidThreadIndex;
            if (string.IsNullOrEmpty(threadGroupName))
                threadGroupName = string.Empty;
            var foundIndex = FrameDataView.invalidThreadIndex;
            using (var frameDataIterator = new ProfilerFrameDataIterator())
            {
                var threadCount = frameDataIterator.GetThreadCount(frameIndex);
                for (int i = 0; i < threadCount; i++)
                {
                    using (var frameData = ProfilerDriver.GetRawFrameDataView(frameIndex, i))
                    {
                        // only string compare if both names aren't null or empty, i.e. don't compare ` null != "" ` but treat null the same as empty
                        if (((string.IsNullOrEmpty(frameData.threadGroupName) && string.IsNullOrEmpty(threadGroupName)) || frameData.threadGroupName == threadGroupName) &&
                            frameData.threadName == threadName)
                        {
                            // do we have a valid thread id to check against and a direct match?
                            if (threadId == FrameDataView.invalidThreadId || frameData.threadId == threadId)
                                return frameData.threadIndex;
                            // else store the first found thread index as a fallback
                            else if (foundIndex == FrameDataView.invalidThreadIndex)
                                foundIndex = frameData.threadIndex;
                        }
                    }
                }
            }
            return foundIndex;
        }
    }
}
