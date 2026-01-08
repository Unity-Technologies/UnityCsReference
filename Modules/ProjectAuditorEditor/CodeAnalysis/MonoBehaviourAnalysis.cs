// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Mono.Cecil;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    static class MonoBehaviourAnalysis
    {
        static readonly int k_CoreModuleHashCode = "UnityEngine.CoreModule.dll".GetHashCode();
        static readonly int k_MonoBehaviourHashCode = "UnityEngine.MonoBehaviour".GetHashCode();
        static readonly int k_ILPostProcessorHashCode = "Unity.CompilationPipeline.Common.ILPostProcessing.ILPostProcessor".GetHashCode();

        static readonly string[] k_EventNames =
        {"Awake", "Start", "OnEnable", "OnDisable", "Update", "LateUpdate", "FixedUpdate"};

        static readonly string[] k_UpdateMethodNames = {"Update", "LateUpdate", "FixedUpdate", "OnAnimatorIK", "OnAnimatorMove", "OnWillRenderObject", "OnRenderObject"};

        public static bool IsMonoBehaviour(TypeReference typeReference)
        {
            // handle special case where Assembly will fail to be Resolved
            if (typeReference.FullName.GetHashCode() == k_ILPostProcessorHashCode)
                return false;

            try
            {
                var typeDefinition = typeReference.Resolve();

                if (typeDefinition == null)
                {
                    // temporary fix to handle case where the assembly is found but not the type
                    Debug.LogWarning(typeReference.FullName + " could not be resolved.");
                    return false;
                }

                if (typeDefinition.FullName.GetHashCode() == k_MonoBehaviourHashCode &&
                    typeDefinition.Module.Name.GetHashCode() == k_CoreModuleHashCode)
                    return true;

                if (typeDefinition.BaseType != null)
                    return IsMonoBehaviour(typeDefinition.BaseType);
            }
            catch (AssemblyResolutionException e)
            {
                Debug.LogWarningFormat("Could not resolve {0}: {1}", typeReference.Name, e.Message);
            }

            return false;
        }

        public static bool IsMonoBehaviourEvent(MethodDefinition methodDefinition)
        {
            return Array.IndexOf(k_EventNames, methodDefinition.Name) != -1;
        }

        public static bool IsMonoBehaviourUpdateMethod(MethodDefinition methodDefinition)
        {
            return Array.IndexOf(k_UpdateMethodNames, methodDefinition.Name) != -1;
        }
    }
}
