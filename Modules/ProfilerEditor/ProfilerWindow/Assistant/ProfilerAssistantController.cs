// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;

namespace Unity.Profiling.Editor
{
    internal class CpuProfilerAssistantController : BaseAssistantController
    {
        public const string k_ProfilerAssistantRole = "CPU Profiler Assistant";

        public CpuProfilerAssistantController() : base(k_ProfilerAssistantRole)
        {
        }

        public struct CpuProfilerContext
        {
            public CpuProfilerContext(string capturePath, Range frameRange, string threadName = null, string markerIdPath = null, string markerName = null, float targetFrameTime = -1f)
            {
                CapturePath = capturePath;
                FrameRange = frameRange;
                ThreadName = threadName;
                MarkerIdPath = markerIdPath;
                MarkerName = markerName;
                TargetFrameTime = targetFrameTime;
            }

            public string CapturePath { get; private set; }
            public Range FrameRange { get; private set; }
            public string ThreadName { get; private set; }
            public string MarkerIdPath { get; private set; }
            public string MarkerName { get; private set; }
            public float TargetFrameTime { get; private set; }
        }

        public void LaunchAssistant(Rect screenRect, CpuProfilerContext context, string prompt)
        {
            var serviceContext = GetServiceContext(context);
            LaunchAssistant(screenRect, serviceContext, prompt);
        }

        static IAskAssistantService.Context GetServiceContext(CpuProfilerContext context)
        {
            var displayName = "Current Profiler Capture";

            // Build payload
            var payloadSb = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(context.CapturePath))
            {
                var fileName = Path.GetFileNameWithoutExtension(context.CapturePath);
                payloadSb.AppendLine($"Capture Name: {fileName}, ");

                // Use capture file name as display name
                displayName = fileName;
            }
            payloadSb.AppendLine($"Frame Range: [{context.FrameRange.Start.Value},{context.FrameRange.End}]");
            if (!string.IsNullOrEmpty(context.ThreadName))
            {
                payloadSb.AppendLine($", Thread Name: {context.ThreadName}");
            }
            if (!string.IsNullOrEmpty(context.MarkerIdPath))
            {
                payloadSb.AppendLine($", Marker Id Path: {context.MarkerIdPath}");
            }
            if (context.TargetFrameTime > 0f)
            {
                payloadSb.AppendLine($", Target Frame Time: {context.TargetFrameTime} ms");
            }

            // Override display name if marker name is provided
            if (!string.IsNullOrEmpty(context.MarkerName))
            {
                displayName = context.MarkerName;
            }

            return new IAskAssistantService.Context(payloadSb.ToString(), "Profiler Data", displayName, context);
        }
    }
}
