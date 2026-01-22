// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    abstract class InstanceController : ScriptableObject
    {
        protected internal virtual void SetupExecutionGraph(ExecutionGraph graph) { }

        protected internal virtual Task<Scenario.ValidationResult> ValidateForRunningAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new Scenario.ValidationResult(true, string.Empty));
        }

        protected internal virtual VisualElement CreateControllerUI() => null;

        internal static new T CreateInstance<T>() where T : InstanceController
        {
            var controller = ScriptableObject.CreateInstance<T>();
            OrchestratedScenario.PreventScriptableObjectUnload(controller);
            return controller;
        }
    }

    abstract class InstanceController<TController, TSettings> : InstanceController
        where TController : InstanceController<TController, TSettings>
    {
        [SerializeField] TSettings m_Settings;
        internal protected TSettings Settings
        {
            get => m_Settings;
        }

        internal static TController CreateInstance(TSettings settings)
        {
            var controller = CreateInstance<TController>();
            controller.m_Settings = settings;
            return controller;
        }

        internal static TSettings GetDefaultSettings()
        {
            return Activator.CreateInstance<TSettings>();
        }
    }
}
