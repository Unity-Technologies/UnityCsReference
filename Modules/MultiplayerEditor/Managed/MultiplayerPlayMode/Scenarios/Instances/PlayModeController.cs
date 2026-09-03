// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    abstract class PlayModeController : ScriptableObject
    {
        const string k_CustomTypeName = "Custom";

        [SerializeReference] IPlayModeControllerItem m_InstanceItem;
        [SerializeField] OrchestratedScenario m_Owner;

        internal IPlayModeControllerItem GetInstanceItem() => m_InstanceItem;

        protected internal virtual void SetupExecutionGraph(ExecutionGraphBuilder graph) { }

        protected internal virtual VisualElement CreateControllerUI(Instance instance) => null;
        protected internal virtual VisualElement CreateTitleBarUI(Instance instance) => null;

        // The Instance is passed because the active state can live on it (e.g. the free-run
        // token), not on the controller — mirroring CreateControllerUI(Instance).
        internal virtual bool NeedsTearDown(Instance instance, out string reason)
        {
            reason = null;
            return false;
        }

        // Must be idempotent and safe to call when there is nothing to release.
        internal virtual void TearDown(Instance instance) { }

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

        private protected SerializedProperty GetControllerItemProperty()
            => m_Owner == null ? null : m_Owner.GetControllerItemProperty(m_InstanceItem.GetId());

        internal virtual string GetTypeNameForAnalytics() => k_CustomTypeName;

        internal static bool IsSubclassOfGenericDefinition(Type type, Type genericDefinition)
        {
            var currentType = type;
            while (currentType != null && currentType != typeof(object))
            {
                if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == genericDefinition)
                    return true;

                currentType = currentType.BaseType;
            }

            return false;
        }

        internal static PlayModeController CreateInstance(
            Type controllerType,
            IPlayModeControllerItem instanceItem,
            OrchestratedScenario owner)
        {
            if (!typeof(PlayModeController).IsAssignableFrom(controllerType))
                throw new ArgumentException($"Type {controllerType.Name} must derive from PlayModeController.");

            var controller = (PlayModeController)CreateInstance(controllerType);
            controller.name = instanceItem != null ? instanceItem.GetName() : controllerType.Name;
            controller.m_InstanceItem = instanceItem;
            controller.m_Owner = owner;
            OrchestratedScenario.PreventScriptableObjectUnload(controller);
            return controller;
        }

        internal static T CreateInstance<T>(IPlayModeControllerItem instanceItem, OrchestratedScenario owner) where T : PlayModeController
            => (T)CreateInstance(typeof(T), instanceItem, owner);


        internal static new T CreateInstance<T>() where T : PlayModeController
            => CreateInstance<T>(default, null);
    }

    abstract class PlayModeController<TSettings> : PlayModeController
        where TSettings : struct
    {
        internal protected TSettings Settings => GetInstanceItem().GetSettings<TSettings>();

        internal static TSettings GetDefaultSettings() => new();
    }
}
