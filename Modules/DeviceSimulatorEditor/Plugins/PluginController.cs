// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.DeviceSimulation
{
    internal class PluginController : IDisposable
    {
        public List<DeviceSimulatorPlugin> Plugins { get; } = new List<DeviceSimulatorPlugin>();

        public PluginController(SimulatorState serializedState, DeviceSimulator deviceSimulator)
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom<DeviceSimulatorPlugin>().Where(type => !type.IsAbstract))
            {
                var plugin = (DeviceSimulatorPlugin)Activator.CreateInstance(type);
                plugin.deviceSimulator = deviceSimulator;
                if (serializedState.plugins.TryGetValue(plugin.GetType().ToString(), out var serializedPlugin))
                    JsonUtility.FromJsonOverwrite(serializedPlugin, plugin);
                plugin.OnCreate();
                Plugins.Add(plugin);
            }
            Plugins.Sort(ComparePluginOrder);
        }

        public void StoreSerializationStates(ref SimulatorState states)
        {
            foreach (var plugin in Plugins)
            {
                var serializedPlugin = JsonUtility.ToJson(plugin);
                states.plugins.Add(plugin.GetType().ToString(), serializedPlugin);
            }
        }

        private static int ComparePluginOrder(DeviceSimulatorPlugin ext1, DeviceSimulatorPlugin ext2)
        {
            return string.CompareOrdinal(ext1.title, ext2.title);
        }

        public void Dispose()
        {
            foreach (var plugin in Plugins)
            {
                plugin.OnDestroy();
            }
        }
    }
}
