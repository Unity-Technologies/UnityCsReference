// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.BuildReporting;

namespace UnityEditor.BuildReporting
{
    internal class StrippingInfo : ScriptableObject, ISerializationCallbackReceiver
    {
        public const string RequiredByScripts = "Required by Scripts";

        [System.Serializable]
        public struct SerializedDependency
        {
            [SerializeField]
            public string key;
            [SerializeField]
            public List<string> value;
            [SerializeField]
            public string icon;
            [SerializeField]
            public int size;
        }

        public List<SerializedDependency> serializedDependencies;

        public List<string> modules = new List<string>();

        // Not needed any more since we have SerializedDependency.size now, but keep it as long as the BuildReport UI uses it.
        public List<int> serializedSizes = new List<int>();

        public Dictionary<string, HashSet<string>> dependencies = new Dictionary<string, HashSet<string>>();

        public Dictionary<string, int> sizes = new Dictionary<string, int>();

        public Dictionary<string, string> icons = new Dictionary<string, string>();

        public int totalSize = 0;

        void OnEnable()
        {
            SetIcon(RequiredByScripts, "class/MonoScript");

            // We don't have real icons specifically for all the modules right now.
            // While it would be nice to have some, for now, we set up the common modules to use some
            // of their class icons.
            SetIcon(ModuleName("AI"), "class/NavMeshAgent");
            SetIcon(ModuleName("Animation"), "class/Animation");
            SetIcon(ModuleName("Audio"), "class/AudioSource");
            SetIcon(ModuleName("Core"), "class/GameManager");
            SetIcon(ModuleName("IMGUI"), "class/GUILayer");
            SetIcon(ModuleName("ParticleSystem"), "class/ParticleSystem");
            SetIcon(ModuleName("ParticlesLegacy"), "class/EllipsoidParticleEmitter");
            SetIcon(ModuleName("Physics"), "class/PhysicMaterial");
            SetIcon(ModuleName("Physics2D"), "class/PhysicsMaterial2D");
            SetIcon(ModuleName("TextRendering"), "class/Font");
            SetIcon(ModuleName("UI"), "class/CanvasGroup");
            SetIcon(ModuleName("Umbra"), "class/OcclusionCullingSettings");
            SetIcon(ModuleName("UNET"), "class/NetworkTransform");
            SetIcon(ModuleName("Vehicles"), "class/WheelCollider");
            SetIcon(ModuleName("Cloth"), "class/Cloth");
            SetIcon(ModuleName("ImageConversion"), "class/Texture");
            SetIcon(ModuleName("ScreenCapture"), "class/RenderTexture");
            SetIcon(ModuleName("Wind"), "class/WindZone");
        }

        public void OnBeforeSerialize()
        {
            serializedDependencies = new List<SerializedDependency>();
            foreach (var dep in dependencies)
            {
                var list = new List<string>();
                foreach (var nc in dep.Value)
                    list.Add(nc);
                SerializedDependency sd;
                sd.key = dep.Key;
                sd.value = list;
                sd.icon = icons.ContainsKey(dep.Key) ? icons[dep.Key] : "class/DefaultAsset";
                sd.size = sizes.ContainsKey(dep.Key) ? sizes[dep.Key] : 0;
                serializedDependencies.Add(sd);
            }
            serializedSizes = new List<int>();
            foreach (var module in modules)
            {
                serializedSizes.Add(sizes.ContainsKey(module) ? sizes[module] : 0);
            }
        }

        public void OnAfterDeserialize()
        {
            dependencies = new Dictionary<string, HashSet<string>>();
            icons = new Dictionary<string, string>();
            for (int i = 0; i < serializedDependencies.Count; i++)
            {
                HashSet<string> depends = new HashSet<string>();
                foreach (var s in serializedDependencies[i].value)
                    depends.Add(s);
                dependencies.Add(serializedDependencies[i].key, depends);
                icons[serializedDependencies[i].key] = serializedDependencies[i].icon;
                sizes[serializedDependencies[i].key] = serializedDependencies[i].size;
            }
            sizes = new Dictionary<string, int>();
            for (int i = 0; i < serializedSizes.Count; i++)
                sizes[modules[i]] = serializedSizes[i];
        }

        public void RegisterDependency(string obj, string depends)
        {
            if (!dependencies.ContainsKey(obj))
                dependencies[obj] = new HashSet<string>();
            dependencies[obj].Add(depends);
            if (!icons.ContainsKey(depends))
                SetIcon(depends, "class/" + depends);
        }

        public void AddModule(string module)
        {
            if (!modules.Contains(module))
                modules.Add(module);
            if (!sizes.ContainsKey(module))
                sizes[module] = 0;

            // Fall back to default icon for unknown modules
            if (!icons.ContainsKey(module))
                SetIcon(module, "class/DefaultAsset");
        }

        public void SetIcon(string dependency, string icon)
        {
            icons[dependency] = icon;
            if (!dependencies.ContainsKey(dependency))
                dependencies[dependency] = new HashSet<string>();
        }

        public void AddModuleSize(string module, int size)
        {
            if (modules.Contains(module))
                sizes[module] = size;
        }

        public static StrippingInfo GetBuildReportData(BuildReport report)
        {
            if (report == null)
                return null;
            var allStrippingData = (StrippingInfo[])report.GetAppendices(typeof(StrippingInfo));
            if (allStrippingData.Length > 0)
                return allStrippingData[0];

            var newData = ScriptableObject.CreateInstance<StrippingInfo>();
            report.AddAppendix(newData);
            return newData;
        }

        public static string ModuleName(string module)
        {
            return module + " Module";
        }
    }
}
