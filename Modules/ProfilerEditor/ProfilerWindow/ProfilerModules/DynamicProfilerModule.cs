// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling.Editor;
using UnityEditorInternal.Profiling;
using UnityEngine;

namespace UnityEditor.Profiling
{
    [Serializable]
    internal class DynamicProfilerModule : ProfilerModuleBase
    {
        public const string iconPath = "Profiler.Custom";

        Action m_LegacyInitialization;

        public void Initialize(InitializationArgs args, List<ProfilerCounterData> chartCounters, List<ProfilerCounterData> detailCounters)
        {
            m_LegacyInitialization = () =>
            {
                SetCounters(chartCounters, detailCounters);
            };
            Initialize(args);
        }

        internal override void LegacyModuleInitialize()
        {
            base.LegacyModuleInitialize();
            m_LegacyInitialization.Invoke();
            m_LegacyInitialization = null;
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
                m_Name = DisplayName,
                m_ChartCounters = m_LegacyChartCounters,
                m_DetailCounters = m_LegacyDetailCounters,
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

            public static SerializedDataCollection FromDynamicProfilerModulesInCollection(List<ProfilerModule> modules)
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
