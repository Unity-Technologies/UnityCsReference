// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    abstract class InstanceController : ScriptableObject
    {
        const string k_CustomTypeName = "Custom";

        [SerializeReference] IInstanceItem m_InstanceItem;
        [SerializeField] OrchestratedScenario m_Owner;

        internal IInstanceItem GetInstanceItem() => m_InstanceItem;

        protected internal virtual void SetupExecutionGraph(ExecutionGraphBuilder graph) { }

        protected internal virtual VisualElement CreateControllerUI(Instance instance) => null;
        protected internal virtual VisualElement CreateTitleBarUI(Instance instance) => null;

        // Returns controller-type-specific analytics data for the OnPlayFromScenario event,
        // or null when the controller has no extra data to report. 
        protected internal virtual ICustomInstanceAnalyticsData GetCustomAnalyticsData(ExecutionGraph graph) => null;

        internal T GetUserSettings<T>(T defaultValue = default) where T : struct
            => OrchestratedScenarioUserSettings.GetSettings<T>(m_Owner, m_InstanceItem, defaultValue);

        internal bool TryGetUserSettings<T>(out T settings) where T : struct
        {
            settings = default;
            if (m_Owner == null || !AssetDatabase.Contains(m_Owner))
                return false;
            settings = GetUserSettings<T>();
            return true;
        }

        internal void SetUserSettings<T>(T settings) where T : struct
            => OrchestratedScenarioUserSettings.SetSettings(m_Owner, m_InstanceItem, settings);

        internal SerializedProperty GetUserSettingsSerializedProperty<T>(T defaultValue = default) where T : struct
            => OrchestratedScenarioUserSettings.GetSerializedSettingsProperty<T>(m_Owner, m_InstanceItem, defaultValue);

        internal virtual string GetTypeNameForAnalytics() => k_CustomTypeName;

        internal static InstanceController CreateInstance(
            Type controllerType,
            IInstanceItem instanceItem,
            OrchestratedScenario owner)
        {
            if (!typeof(InstanceController).IsAssignableFrom(controllerType))
                throw new ArgumentException($"Type {controllerType.Name} must derive from InstanceController.");

            var controller = (InstanceController)CreateInstance(controllerType);
            controller.name = instanceItem != null ? instanceItem.GetName() : controllerType.Name;
            controller.m_InstanceItem = instanceItem;
            controller.m_Owner = owner;
            OrchestratedScenario.PreventScriptableObjectUnload(controller);
            return controller;
        }

        internal static T CreateInstance<T>(IInstanceItem instanceItem, OrchestratedScenario owner) where T : InstanceController
            => (T)CreateInstance(typeof(T), instanceItem, owner);


        internal static new T CreateInstance<T>() where T : InstanceController
            => CreateInstance<T>(default, null);
    }

    abstract class InstanceController<TSettings> : InstanceController
        where TSettings : struct
    {
        internal protected TSettings Settings => GetInstanceItem().GetSettings<TSettings>();
        internal static TSettings GetDefaultSettings() => new();
    }
}
