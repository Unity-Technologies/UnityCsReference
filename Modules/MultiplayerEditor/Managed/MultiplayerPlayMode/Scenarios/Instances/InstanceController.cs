// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    abstract class InstanceController : ScriptableObject
    {
        const string k_CustomTypeName = "Custom";

        protected internal virtual void SetupExecutionGraph(ExecutionGraph graph) { }

        protected internal virtual VisualElement CreateControllerUI(Instance instance) => null;

        internal virtual string GetTypeNameForAnalytics() => k_CustomTypeName;

        internal static T CreateInstance<T>(string name) where T : InstanceController
        {
            var controller = ScriptableObject.CreateInstance<T>();
            controller.name = name;
            OrchestratedScenario.PreventScriptableObjectUnload(controller);
            return controller;
        }

        internal static new T CreateInstance<T>() where T : InstanceController
            => CreateInstance<T>(typeof(T).Name);
    }

    abstract class InstanceController<TController, TSettings> : InstanceController
        where TController : InstanceController<TController, TSettings>
        where TSettings : new()
    {
        [SerializeField] TSettings m_Settings;
        internal protected TSettings Settings
        {
            get => m_Settings;
        }

        internal static TController CreateInstance(string name, TSettings settings)
        {
            var controller = CreateInstance<TController>(name);
            controller.m_Settings = settings;
            return controller;
        }

        internal static TSettings GetDefaultSettings() => new();
    }
}
