// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;

namespace Unity.Profiling.Editor
{
    internal class CpuProfilerAssistantController : IDisposable
    {
        public const string k_ProfilerAssistantRole = "CPU Profiler Assistant";

        private IAskAssistantService[] m_CpuProfilerAssistantServices;

        private void Initialize()
        {
            if (m_CpuProfilerAssistantServices != null)
                return;

            var paTypes = TypeCache.GetTypesDerivedFrom<IAskAssistantService>();
            var painstances = new List<IAskAssistantService>();
            foreach (var painstance in paTypes)
            {
                // Ignore abstract classes and interfaces
                if (painstance.IsAbstract || painstance.IsInterface)
                    continue;

                // Read the role attribute and ignore types that don't match the CPU Profiler Assistant role
                var roleAttribute = (AskAssistantServiceRoleAttribute)Attribute.GetCustomAttribute(painstance, typeof(AskAssistantServiceRoleAttribute));
                if (roleAttribute == null || roleAttribute.Role != k_ProfilerAssistantRole)
                    continue;

                try
                {
                    var instance = (IAskAssistantService)Activator.CreateInstance(painstance);
                    if (instance.Initialize())
                        painstances.Add(instance);
                    else
                        instance.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Could not create instance of IAskAssistantService type {painstance.FullName}. Exception: {e}");
                }
            }
            m_CpuProfilerAssistantServices = painstances.ToArray();
        }

        public void Dispose()
        {
            if (m_CpuProfilerAssistantServices == null)
                return;

            foreach (var service in m_CpuProfilerAssistantServices)
                service.Dispose();
            m_CpuProfilerAssistantServices = null;
        }

        public bool Supported
        {
            get
            {
                Initialize();
                return m_CpuProfilerAssistantServices.Length > 0;
            }
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

        public void LaunchCpuProfilerAssistant(Rect screenRect, CpuProfilerContext context, string prompt)
        {
            if (!Supported)
                throw new InvalidOperationException("Profiler Assistant is not supported.");

            var serviceContext = GetServiceContext(context);
            foreach (var service in m_CpuProfilerAssistantServices)
            {
                try
                {
                    service.ShowAskAssistantPopup(screenRect, serviceContext, prompt);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Could not launch Profiler Assistant Service {service.GetType().FullName}. Exception: {e}");
                }
            }
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
