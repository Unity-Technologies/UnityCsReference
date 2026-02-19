// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Profiling.Editor
{
    [VisibleToOtherModules("UnityEditor.ProjectAuditorModule")]
    internal abstract class BaseAssistantController : IDisposable
    {
        private IAskAssistantService[] m_AssistantServices;
        private readonly string m_Role;

        protected BaseAssistantController(string role)
        {
            m_Role = role;
        }

        private void Initialize()
        {
            if (m_AssistantServices != null)
                return;

            var serviceTypes = TypeCache.GetTypesDerivedFrom<IAskAssistantService>();
            var instances = new List<IAskAssistantService>();
            foreach (var serviceType in serviceTypes)
            {
                // Ignore abstract classes and interfaces
                if (serviceType.IsAbstract || serviceType.IsInterface)
                    continue;

                // Read the role attribute and ignore types that don't match the specified role
                var roleAttribute = (AskAssistantServiceRoleAttribute)Attribute.GetCustomAttribute(serviceType, typeof(AskAssistantServiceRoleAttribute));
                if (roleAttribute == null || roleAttribute.Role != m_Role)
                    continue;

                try
                {
                    var instance = (IAskAssistantService)Activator.CreateInstance(serviceType);
                    if (instance.Initialize())
                        instances.Add(instance);
                    else
                        instance.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Could not create instance of IAskAssistantService type {serviceType.FullName}. Exception: {e}");
                }
            }
            m_AssistantServices = instances.ToArray();
        }

        public void Dispose()
        {
            if (m_AssistantServices == null)
                return;

            foreach (var service in m_AssistantServices)
                service.Dispose();
            m_AssistantServices = null;
        }

        public bool Supported
        {
            get
            {
                Initialize();
                return m_AssistantServices.Length > 0;
            }
        }

        protected void LaunchAssistant(Rect parentRect, IAskAssistantService.Context serviceContext, string prompt)
        {
            if (!Supported)
                throw new InvalidOperationException($"{m_Role} is not supported.");

            foreach (var service in m_AssistantServices)
            {
                try
                {
                    service.ShowAskAssistantPopup(parentRect, serviceContext, prompt);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Could not launch {m_Role} Service {service.GetType().FullName}. Exception: {e}");
                }
            }
        }
    }
}
