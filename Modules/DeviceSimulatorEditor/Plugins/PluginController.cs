// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.DeviceSimulation
{
    internal class PluginController : IDisposable
    {
        private readonly List<DeviceSimulatorPlugin> plugins = new List<DeviceSimulatorPlugin>();

        public PluginController(SimulatorState serializedState, DeviceSimulator deviceSimulator)
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom<DeviceSimulatorPlugin>().Where(type => !type.IsAbstract))
            {
                try
                {
                    var plugin = (DeviceSimulatorPlugin)Activator.CreateInstance(type);
                    plugin.deviceSimulator = deviceSimulator;
                    if (serializedState.plugins.TryGetValue(plugin.GetType().ToString(), out var serializedPlugin))
                        JsonUtility.FromJsonOverwrite(serializedPlugin, plugin);
                    try
                    {
                        plugin.OnCreate();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                    try
                    {
                        plugin.resolvedTitle = plugin.title;
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                    plugins.Add(plugin);
                }
                catch (MissingMethodException)
                {
                    Debug.LogError($"Failed instantiating Device Simulator plug-in {type.Name}. It does not have a public default constructor.");
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            plugins.Sort(ComparePluginOrder);
        }

        public void StoreSerializationStates(ref SimulatorState states)
        {
            foreach (var plugin in plugins)
            {
                var serializedPlugin = JsonUtility.ToJson(plugin);
                states.plugins.Add(plugin.GetType().ToString(), serializedPlugin);
            }
        }

        private static int ComparePluginOrder(DeviceSimulatorPlugin ext1, DeviceSimulatorPlugin ext2)
        {
            return string.CompareOrdinal(ext1.resolvedTitle, ext2.resolvedTitle);
        }

        public List<(VisualElement ui, string title, string serializationKey)> CreateUI()
        {
            var pluginUI = new List<(VisualElement ui, string title, string serializationKey)>();
            foreach (var plugin in plugins)
            {
                try
                {
                    var ui = plugin.OnCreateUI();
                    if (ui == null)
                        continue;

                    if (!string.IsNullOrEmpty(plugin.resolvedTitle))
                        pluginUI.Add((ui, plugin.title, plugin.GetType().ToString()));
                    else
                        Debug.LogError($"Device Simulator plug-in {plugin.GetType()} returned an empty title. It will not be added to the Control Panel.");
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            return pluginUI;
        }

        public string[] GetPluginNames()
        {
            return plugins.Select(p => p.resolvedTitle).ToArray();
        }

        public void Dispose()
        {
            foreach (var plugin in plugins)
            {
                try
                {
                    plugin.OnDestroy();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
