// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;

namespace UnityEditorInternal
{
    class UnityLinkerArgumentValueProvider
    {
        private readonly UnityLinkerRunInformation m_RunInformation;

        public UnityLinkerArgumentValueProvider(UnityLinkerRunInformation runInformation)
        {
            this.m_RunInformation = runInformation;
        }

        public string Runtime
        {
            get
            {
                var backend = PlayerSettings.GetScriptingBackend(m_RunInformation.buildTargetGroup);
                switch (backend)
                {
                    case ScriptingImplementation.IL2CPP:
                        return "il2cpp";
                    case ScriptingImplementation.Mono2x:
                        return "mono";
                    default:
                        throw new NotImplementedException($"Don't know the backend value to pass to UnityLinker for {backend}");
                }
            }
        }

        public string Profile => IL2CPPUtils.ApiCompatibilityLevelToDotNetProfileArgument(PlayerSettings.GetApiCompatibilityLevel(m_RunInformation.buildTargetGroup), m_RunInformation.target);

        public string RuleSet
        {
            get
            {
                switch (m_RunInformation.managedStrippingLevel)
                {
                    case ManagedStrippingLevel.Low:
                        return "Conservative";
                    case ManagedStrippingLevel.Medium:
                        return "Aggressive";
                    case ManagedStrippingLevel.High:
                        return "Experimental";
                }

                throw new ArgumentException($"Unhandled {nameof(ManagedStrippingLevel)} value of {m_RunInformation.managedStrippingLevel}");
            }
        }
    }
}
