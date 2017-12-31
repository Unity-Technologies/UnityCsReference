// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngineInternal;
using UnityEditor;
using System.Runtime.InteropServices;

namespace UnityEditor.Animations
{
    [NativeHeader("Runtime/Animation/Animator.h")]
    [NativeHeader("Editor/Src/Animation/StateMachineBehaviourScripting.h")]
    [NativeHeader("Editor/Src/Animation/AnimatorController.bindings.h")]
    [NativeHeader("Runtime/Animation/AnimatorController.h")]
    public partial class AnimatorController : RuntimeAnimatorController
    {
        public AnimatorController()
        {
            Internal_Create(this);
        }

        [FreeFunction("AnimatorControllerBindings::Internal_Create")]
        extern private static void Internal_Create([Writable] AnimatorController self);

        extern public AnimatorControllerLayer[] layers
        {
            [FreeFunction(Name = "AnimatorControllerBindings::GetLayers", HasExplicitThis = true)]
            get;
            [FreeFunction(Name = "AnimatorControllerBindings::SetLayers", HasExplicitThis = true)]
            set;
        }

        extern public AnimatorControllerParameter[] parameters
        {
            [FreeFunction(Name = "AnimatorControllerBindings::GetParameters", HasExplicitThis = true)]
            get;
            [FreeFunction(Name = "AnimatorControllerBindings::SetParameters", HasExplicitThis = true)]
            set;
        }

        [FreeFunction(Name = "AnimatorControllerBindings::GetEffectiveAnimatorController")]
        extern internal static AnimatorController GetEffectiveAnimatorController(Animator animator);


        internal static AnimatorControllerPlayable FindAnimatorControllerPlayable(Animator animator, AnimatorController controller)
        {
            PlayableHandle handle = new PlayableHandle();
            Internal_FindAnimatorControllerPlayable(ref handle, animator, controller);
            if (!handle.IsValid())
                return AnimatorControllerPlayable.Null;
            return new AnimatorControllerPlayable(handle);
        }

        [FreeFunction(Name = "AnimatorControllerBindings::Internal_FindAnimatorControllerPlayable")]
        extern internal static void Internal_FindAnimatorControllerPlayable(ref PlayableHandle ret, Animator animator, AnimatorController controller);

        public static void SetAnimatorController(Animator animator, AnimatorController controller)
        {
            animator.runtimeAnimatorController = controller;
        }

        extern internal int IndexOfParameter(string name);
        extern internal void RenameParameter(string prevName, string newName);
        extern public string MakeUniqueParameterName(string name);
        extern public string MakeUniqueLayerName(string name);

        static public StateMachineBehaviourContext[] FindStateMachineBehaviourContext(StateMachineBehaviour behaviour)
        {
            return Internal_FindStateMachineBehaviourContext(behaviour);
        }

        [FreeFunction("FindStateMachineBehaviourContext")]
        extern internal static StateMachineBehaviourContext[] Internal_FindStateMachineBehaviourContext(ScriptableObject behaviour);

        [FreeFunction("AnimatorControllerBindings::Internal_CreateStateMachineBehaviour")]
        extern public static int CreateStateMachineBehaviour(MonoScript script);

        [FreeFunction("AnimatorControllerBindings::CanAddStateMachineBehaviours")]
        extern internal static  bool CanAddStateMachineBehaviours();

        extern internal MonoScript GetBehaviourMonoScript(AnimatorState state, int layerIndex, int behaviourIndex);

        [FreeFunction]
        extern private static ScriptableObject ScriptingAddStateMachineBehaviourWithType(Type stateMachineBehaviourType, AnimatorController controller, AnimatorState state, int layerIndex);


        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        public StateMachineBehaviour AddEffectiveStateMachineBehaviour(Type stateMachineBehaviourType, AnimatorState state, int layerIndex)
        {
            return (StateMachineBehaviour)ScriptingAddStateMachineBehaviourWithType(stateMachineBehaviourType, this, state, layerIndex);
        }

        public T AddEffectiveStateMachineBehaviour<T>(AnimatorState state, int layerIndex) where T : StateMachineBehaviour
        {
            return AddEffectiveStateMachineBehaviour(typeof(T), state, layerIndex) as T;
        }

        public T[] GetBehaviours<T>() where T : StateMachineBehaviour
        {
            return ConvertStateMachineBehaviour<T>(InternalGetBehaviours(typeof(T)));
        }

        [FreeFunction(Name = "AnimatorControllerBindings::Internal_GetBehaviours", HasExplicitThis = true)]
        extern internal ScriptableObject[] InternalGetBehaviours([NotNull] Type type);

        internal static T[] ConvertStateMachineBehaviour<T>(ScriptableObject[] rawObjects) where T : StateMachineBehaviour
        {
            if (rawObjects == null) return null;
            T[] typedObjects = new T[rawObjects.Length];
            for (int i = 0; i < typedObjects.Length; i++)
                typedObjects[i] = (T)rawObjects[i];
            return typedObjects;
        }

        extern internal UnityEngine.Object[] CollectObjectsUsingParameter(string parameterName);

        internal extern  bool isAssetBundled
        {
            [NativeName("IsAssetBundled")]
            get;
        }

        extern internal void AddStateEffectiveBehaviour([NotNull] AnimatorState state, int layerIndex, int instanceID);
        extern internal void RemoveStateEffectiveBehaviour([NotNull] AnimatorState state, int layerIndex, int behaviourIndex);

        [FreeFunction(Name = "AnimatorControllerBindings::Internal_GetEffectiveBehaviours", HasExplicitThis = true)]
        extern internal ScriptableObject[] Internal_GetEffectiveBehaviours([NotNull] AnimatorState state, int layerIndex);

        [FreeFunction(Name = "AnimatorControllerBindings::Internal_SetEffectiveBehaviours", HasExplicitThis = true)]
        extern internal void Internal_SetEffectiveBehaviours([NotNull] AnimatorState state, int layerIndex, ScriptableObject[] behaviours);
    }
}
