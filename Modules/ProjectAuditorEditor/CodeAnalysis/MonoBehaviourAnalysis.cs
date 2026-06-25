// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Mono.Cecil;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    static class MonoBehaviourAnalysis
    {
        static readonly int k_CoreModuleHashCode = "UnityEngine.CoreModule.dll".GetHashCode();
        static readonly int k_MonoBehaviourHashCode = "UnityEngine.MonoBehaviour".GetHashCode();
        static readonly int k_ILPostProcessorHashCode = "Unity.CompilationPipeline.Common.ILPostProcessing.ILPostProcessor".GetHashCode();

        [NoAutoStaticsCleanup] // Constant lookup table of MonoBehaviour event names; safe to persist
        static readonly HashSet<string> k_EventNames = new HashSet<string>(
        [
            "Awake", "Start", "OnEnable", "OnDisable",
            "OnControllerColliderHit", "OnCollisionEnter2D", "OnCollisionExit2D", "OnCollisionStay2D", "OnTriggerEnter2D", "OnTriggerExit2D", "OnTriggerStay2D",
            "OnJointBreak2D", "OnTerrainChanged", "OnCanvasHierarchyChanged", "OnCanvasGroupChanged",
            "OnBecameVisible", "OnBecameInvisible", "OnParticleCollision", "OnParticleTrigger","OnParticleSystemStopped","OnParticleUpdateJobScheduled",
            "OnTriggerEnter","OnTriggerExit","OnTriggerStay","OnCollisionEnter", "OnCollisionExit", "OnCollisionStay", "OnJointBreak", "RigidbodyAdded",
            "OnApplicationPause", "OnApplicationFocus", "OnApplicationQuit", "OnLevelWasLoaded", "OnRectTransformRemoved","OnRectTransformDimensionsChange","OnChildRectTransformDimensionsChange",
            "OnBeforeTransformParentChanged","OnTransformParentChanged","OnTransformChildrenChanged"
        ]);

        [NoAutoStaticsCleanup] // Constant lookup table of MonoBehaviour update method names; safe to persist
        static readonly HashSet<string> k_UpdateMethodNames = new HashSet<string>(
        [
            "Update", "LateUpdate", "FixedUpdate", "OnAnimatorIK", "OnAnimatorMove", "OnWillRenderObject", "OnRenderObject",
            "OnPreCull", "OnPostRender", "OnPreRender"
        ]);

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
            return IsMonoBehaviourUpdateMethod(methodDefinition) || k_EventNames.Contains(methodDefinition.Name);
        }

        public static bool IsMonoBehaviourUpdateMethod(MethodDefinition methodDefinition)
        {
            return k_UpdateMethodNames.Contains(methodDefinition.Name);
        }
    }
}
