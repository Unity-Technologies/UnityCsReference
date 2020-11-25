// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.DeviceSimulation
{
    [Serializable]
    internal class SimulatorState : ISerializationCallbackReceiver
    {
        public bool controlPanelVisible;
        public float controlPanelWidth;

        public Dictionary<string, bool> controlPanelFoldouts = new Dictionary<string, bool>();
        [SerializeField] private List<string> controlPanelFoldoutKeys = new List<string>();
        [SerializeField] private List<bool> controlPanelFoldoutValues = new List<bool>();

        public Dictionary<string, string> plugins = new Dictionary<string, string>();
        [SerializeField] private List<string> pluginNames = new List<string>();
        [SerializeField] private List<string> pluginStates = new List<string>();

        public int scale;
        public bool fitToScreenEnabled = true;
        public int rotationDegree;
        public bool highlightSafeAreaEnabled;
        public string friendlyName = string.Empty;

        public void OnBeforeSerialize()
        {
            foreach (var plugin in plugins)
            {
                pluginNames.Add(plugin.Key);
                pluginStates.Add(plugin.Value);
            }

            foreach (var foldout in controlPanelFoldouts)
            {
                controlPanelFoldoutKeys.Add(foldout.Key);
                controlPanelFoldoutValues.Add(foldout.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            for (int index = 0; index < pluginNames.Count; ++index)
            {
                plugins.Add(pluginNames[index], pluginStates[index]);
            }
            for (int index = 0; index < controlPanelFoldoutKeys.Count; ++index)
            {
                controlPanelFoldouts.Add(controlPanelFoldoutKeys[index], controlPanelFoldoutValues[index]);
            }
        }
    }
}
