// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Assemblies;

namespace Unity.Multiplayer.Editor
{
    [Serializable]
    internal struct AutomaticSelectionOptions
    {
        private Dictionary<Type, MultiplayerRoleFlags> m_CompleteComponentsList;
        internal Dictionary<Type, MultiplayerRoleFlags> CompleteComponentsList
        {
            get
            {
                if (m_CompleteComponentsList == null)
                    BakeCompleteComponentsList();

                return m_CompleteComponentsList;
            }
        }

        [SerializeField] private bool m_StripRenderComponents;
        [SerializeField] private bool m_StripUIComponents;
        [SerializeField] private bool m_StripAudioComponents;

        public bool StripRenderingComponents
        {
            get => m_StripRenderComponents;
            set
            {
                // Don't bake if value didn't change
                if (m_StripRenderComponents == value)
                    return;

                m_StripRenderComponents = value;
                BakeCompleteComponentsList();
            }
        }
        public bool StripUIComponents
        {
            get => m_StripUIComponents;
            set
            {
                // Don't bake if value didn't change
                if (m_StripUIComponents == value)
                    return;

                m_StripUIComponents = value;
                BakeCompleteComponentsList();
            }
        }
        public bool StripAudioComponents
        {
            get => m_StripAudioComponents;
            set
            {
                // Don't bake if value didn't change
                if (m_StripAudioComponents == value)
                    return;

                m_StripAudioComponents = value;
                BakeCompleteComponentsList();
            }
        }

        [SerializeField] private SerializedDictionary<SerializedType, MultiplayerRoleFlags> m_CustomComponentsList;
        private SerializedDictionary<SerializedType, MultiplayerRoleFlags> CustomComponentsList
        {
            get
            {
                if (m_CustomComponentsList == null)
                    m_CustomComponentsList = new();

                return m_CustomComponentsList;
            }
            set
            {
                m_CustomComponentsList = value;
                BakeCompleteComponentsList();
            }
        }

        public Dictionary<Type, MultiplayerRoleFlags> GetCustomComponents()
        {
            var dictionary = new Dictionary<Type, MultiplayerRoleFlags>();

            foreach (var kvp in CustomComponentsList)
            {
                // This will happen if the script was deleted.
                if (kvp.Key.Value == null)
                    continue;

                dictionary[kvp.Key.Value] = kvp.Value;
            }

            return dictionary;
        }

        public void SetCustomComponents(Dictionary<Type, MultiplayerRoleFlags> customComponents)
        {
            CustomComponentsList.Clear();

            if (customComponents != null)
                foreach (var kvp in customComponents)
                    SetCustomComponentMultiplayerRoleFlags(kvp.Key, kvp.Value, false);

            BakeCompleteComponentsList();
        }

        public MultiplayerRoleFlags GetMultiplayerRoleMaskForComponentType(Type type)
            => GetMultiplayerRoleFlagsForType(type);

        public void SetMultiplayerRoleMaskForComponentType(Type type, MultiplayerRoleFlags mask)
            => SetCustomComponentMultiplayerRoleFlags(type, mask, true);

        internal void SetCustomComponentMultiplayerRoleFlags(Type type, MultiplayerRoleFlags target, bool bake = true)
        {
            if (!type.IsSubclassOf(typeof(Component)))
                throw new ArgumentException("Only subclasses of Component can be selected.");

            if (target == MultiplayerRoleFlags.ClientAndServer)
                CustomComponentsList.Remove(new SerializedType(type));
            else
                CustomComponentsList[new SerializedType(type)] = target;

            if (bake)
                BakeCompleteComponentsList();
        }

        internal AutomaticSelectionOptions Clone()
        {
            var options = this;
            options.m_CustomComponentsList = null;
            options.m_CompleteComponentsList = null;
            options.SetCustomComponents(this.GetCustomComponents());

            return options;
        }

        internal static void GetBuiltinStrippingComponentsForServer(in AutomaticSelectionOptions options, List<Type> types)
        {
            if (options.StripRenderingComponents)
            {
                var components = new List<Type>();
                foreach (var type in TypeCache.GetTypesDerivedFrom<Renderer>())
                {
                    components.Add(type);
                }
                components.Add(typeof(Camera));
                components.Add(typeof(Light));

                components.Add(typeof(ReflectionProbe));
                components.Add(typeof(LightProbeGroup));
                #pragma warning disable CS0618  // Type or member is obsolete
                components.Add(typeof(LightProbeProxyVolume));
                #pragma warning restore CS0618  // Type or member is obsolete

                var additionalDataType = Type.GetType("UnityEngine.Rendering.IAdditionalData,Unity.RenderPipelines.Core.Runtime");
                if (additionalDataType != null)
                {
                    foreach(var type in TypeCache.GetTypesDerivedFrom(additionalDataType))
                    {
                        components.Add(type);
                    }
                }

                foreach (var component in components)
                    types.Add(component);
            }

            if (options.StripUIComponents)
            {
                var assemblies = new List<System.Reflection.Assembly>();
                foreach(var a in CurrentAssemblies.GetLoadedAssemblies())
                {
                    if(
                        a.GetName().Name == "UnityEngine.UI" ||
                        a.GetName().Name == "UnityEngine.Canvas" ||
                        a.GetName().Name == "UnityEngine.UIElementsModule"
                    )
                    {
                        assemblies.Add(a);
                    }
                }

                var components = new List<Type>();
                foreach (var a in assemblies)
                {
                    foreach (var t in a.GetTypes())
                    {
                        if (IsClassOrSubclassOf(t, typeof(Component)))
                        {
                            components.Add(t);
                        }
                    }
                }

                foreach (var component in components)
                    types.Add(component);
            }

            if (options.StripAudioComponents)
            {
                var assembly = typeof(AudioClip).Assembly;
                foreach (var t in assembly.GetTypes())
                {
                    if(IsClassOrSubclassOf(t, typeof(Component)))
                    {
                        types.Add(t);
                    }
                }
            }
        }

        private void BakeCompleteComponentsList()
        {
            if (m_CompleteComponentsList == null)
                m_CompleteComponentsList = new();

            m_CompleteComponentsList.Clear();

            var builtinServerTypes = new List<Type>();
            GetBuiltinStrippingComponentsForServer(this, builtinServerTypes);

            foreach (var type in builtinServerTypes)
            {
                m_CompleteComponentsList[type] = MultiplayerRoleFlags.ClientAndServer & ~MultiplayerRoleFlags.Server;
            }

            foreach (var component in CustomComponentsList)
            {
                if (component.Key.Value == null)
                    continue;

                if (!m_CompleteComponentsList.TryGetValue(component.Key.Value, out var maskValue))
                    maskValue = MultiplayerRoleFlags.ClientAndServer;

                maskValue &= component.Value;
                m_CompleteComponentsList[component.Key.Value] = maskValue;
            }
        }

        internal static bool IsClassOrSubclassOf(Type type, Type parentType)
            => type == parentType || type.IsSubclassOf(parentType);

        internal bool IsComponentSelected(Type type)
        {
            foreach (var selectedType in CompleteComponentsList.Keys)
            {
                if (IsClassOrSubclassOf(type, selectedType))
                {
                    return true;
                }
            }
            return false;
        }

        internal IEnumerable<KeyValuePair<Type, MultiplayerRoleFlags>> GetSelectedParentComponents(Type type)
        {
            var ret = new List<KeyValuePair<Type, MultiplayerRoleFlags>>();
            foreach (var selectionValue in CompleteComponentsList)
            {
                if (AutomaticSelectionOptions.IsClassOrSubclassOf(type, selectionValue.Key))
                {
                    ret.Add(selectionValue);
                }
            }
            return ret;
        }

        internal MultiplayerRoleFlags GetMultiplayerRoleFlagsForType(Type type)
        {
            if (!CompleteComponentsList.TryGetValue(type, out var target))
                target = MultiplayerRoleFlags.ClientAndServer;

            return target;
        }

        internal MultiplayerRoleFlags GetInheritMultiplayerRoleFlagsForType(System.Type type)
        {
            var parentTypes = new List<KeyValuePair<Type, MultiplayerRoleFlags>>();
            foreach (var kvp in CompleteComponentsList)
            {
                if (IsClassOrSubclassOf(type, kvp.Key))
                {
                    parentTypes.Add(kvp);
                }
            }
            var target = MultiplayerRoleFlags.ClientAndServer;

            foreach (var item in parentTypes)
                target &= item.Value;

            return target;
        }
    }
}
