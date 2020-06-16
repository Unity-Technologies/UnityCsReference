// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEditorInternal.Profiling;
using UnityEngine;

namespace UnityEditor.Profiling
{
    [Serializable]
    internal class DynamicProfilerModule : ProfilerModuleBase
    {
        const string k_IconName = "Profiler.Custom";

        public DynamicProfilerModule(IProfilerWindowController profilerWindow, string name) : base(profilerWindow, name, k_IconName) {}

        public static DynamicProfilerModule CreateFromSerializedData(SerializedData serializedData, IProfilerWindowController profilerWindow)
        {
            var name = serializedData.m_Name;
            var module = new DynamicProfilerModule(profilerWindow, name);
            module.SetCounters(serializedData.m_ChartCounters, serializedData.m_DetailCounters);
            return module;
        }

        public override void DrawToolbar(Rect position)
        {
            DrawEmptyToolbar();
        }

        public override void DrawDetailsView(Rect position)
        {
            DrawDetailsViewText(position);
        }

        public SerializedData ToSerializedData()
        {
            return new SerializedData()
            {
                m_Name = name,
                m_ChartCounters = m_ChartCounters,
                m_DetailCounters = m_DetailCounters,
            };
        }

        [Serializable]
        public struct SerializedData
        {
            public string m_Name;
            public List<ProfilerCounterData> m_ChartCounters;
            public List<ProfilerCounterData> m_DetailCounters;
        }

        [Serializable]
        public class SerializedDataCollection
        {
            public List<SerializedData> m_Modules = new List<SerializedData>();

            public int Length => m_Modules.Count;
            public SerializedData this[int index]
            {
                get => m_Modules[index];
            }

            public void Add(SerializedData module)
            {
                m_Modules.Add(module);
            }

            public static SerializedDataCollection FromDynamicProfilerModulesInCollection(List<ProfilerModuleBase> modules)
            {
                var serializableCollection = new SerializedDataCollection();
                foreach (var module in modules)
                {
                    if (module is DynamicProfilerModule dynamicModule)
                    {
                        serializableCollection.Add(dynamicModule.ToSerializedData());
                    }
                }

                return serializableCollection;
            }
        }
    }
}
