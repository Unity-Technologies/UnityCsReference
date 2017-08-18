// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Playables;

using UnityObject = UnityEngine.Object;

namespace UnityEngine.Animations
{
    public interface IAnimatorControllerPlayable
    {
        float GetFloat(string name);
        float GetFloat(int id);
        void SetFloat(string name, float value);
        void SetFloat(int id, float value);

        bool GetBool(string name);
        bool GetBool(int id);
        void SetBool(string name, bool value);
        void SetBool(int id, bool value);

        int GetInteger(string name);
        int GetInteger(int id);
        void SetInteger(string name, int value);
        void SetInteger(int id, int value);

        void SetTrigger(string name);
        void SetTrigger(int id);

        void ResetTrigger(string name);
        void ResetTrigger(int id);

        bool IsParameterControlledByCurve(string name);
        bool IsParameterControlledByCurve(int id);

        int GetLayerCount();
        string GetLayerName(int layerIndex);
        int GetLayerIndex(string layerName);
        float GetLayerWeight(int layerIndex);
        void SetLayerWeight(int layerIndex, float weight);

        AnimatorStateInfo GetCurrentAnimatorStateInfo(int layerIndex);
        AnimatorStateInfo GetNextAnimatorStateInfo(int layerIndex);

        AnimatorTransitionInfo GetAnimatorTransitionInfo(int layerIndex);
        AnimatorClipInfo[] GetCurrentAnimatorClipInfo(int layerIndex);
        AnimatorClipInfo[] GetNextAnimatorClipInfo(int layerIndex);

        int GetCurrentAnimatorClipInfoCount(int layerIndex);
        void GetCurrentAnimatorClipInfo(int layerIndex, List<AnimatorClipInfo> clips);

        int GetNextAnimatorClipInfoCount(int layerIndex);
        void GetNextAnimatorClipInfo(int layerIndex, List<AnimatorClipInfo> clips);

        bool IsInTransition(int layerIndex);
        int GetParameterCount();
        AnimatorControllerParameter GetParameter(int index);

        void CrossFadeInFixedTime(string stateName, float transitionDuration, int layer, float fixedTime);
        void CrossFadeInFixedTime(int stateNameHash, float transitionDuration, int layer, float fixedTime);
        void CrossFade(string stateName, float transitionDuration, int layer, float normalizedTime);
        void CrossFade(int stateNameHash, float transitionDuration, int layer, float normalizedTime);

        void PlayInFixedTime(string stateName, int layer, float fixedTime);
        void PlayInFixedTime(int stateNameHash, int layer, float fixedTime);
        void Play(string stateName, int layer, float normalizedTime);
        void Play(int stateNameHash, int layer, float normalizedTime);

        bool HasState(int layerIndex, int stateID);
    };

    [NativeHeader("Runtime/Animation/ScriptBindings/AnimatorControllerPlayable.bindings.h")]
    [NativeHeader("Runtime/Animation/ScriptBindings/Animator.bindings.h")]
    [NativeHeader("Runtime/Animation/Director/AnimatorControllerPlayable.h")]
    [NativeHeader("Runtime/Animation/RuntimeAnimatorController.h")]
    [NativeHeader("Runtime/Animation/AnimatorInfo.h")]
    [StaticAccessor("AnimatorControllerPlayableBindings", StaticAccessorType.DoubleColon)]
    [RequiredByNativeCode]
    public partial struct AnimatorControllerPlayable : IPlayable, IEquatable<AnimatorControllerPlayable>
    {
        PlayableHandle m_Handle;

        static readonly AnimatorControllerPlayable m_NullPlayable = new AnimatorControllerPlayable(PlayableHandle.Null);
        public static AnimatorControllerPlayable Null { get { return m_NullPlayable; } }

        public static AnimatorControllerPlayable Create(PlayableGraph graph, RuntimeAnimatorController controller)
        {
            var handle = CreateHandle(graph, controller);
            return new AnimatorControllerPlayable(handle);
        }

        private static PlayableHandle CreateHandle(PlayableGraph graph, RuntimeAnimatorController controller)
        {
            PlayableHandle handle = PlayableHandle.Null;
            if (!CreateHandleInternal(graph, controller, ref handle))
                return PlayableHandle.Null;

            return handle;
        }

        internal AnimatorControllerPlayable(PlayableHandle handle)
        {
            m_Handle = PlayableHandle.Null;
            SetHandle(handle);
        }

        public PlayableHandle GetHandle()
        {
            return m_Handle;
        }

        public void SetHandle(PlayableHandle handle)
        {
            if (m_Handle.IsValid())
                throw new InvalidOperationException("Cannot call IPlayable.SetHandle on an instance that already contains a valid handle.");

            if (handle.IsValid())
            {
                if (!handle.IsPlayableOfType<AnimatorControllerPlayable>())
                    throw new InvalidCastException("Can't set handle: the playable is not an AnimatorControllerPlayable.");
            }

            m_Handle = handle;
        }

        public static implicit operator Playable(AnimatorControllerPlayable playable)
        {
            return new Playable(playable.GetHandle());
        }

        public static explicit operator AnimatorControllerPlayable(Playable playable)
        {
            return new AnimatorControllerPlayable(playable.GetHandle());
        }

        public bool Equals(AnimatorControllerPlayable other)
        {
            return GetHandle() == other.GetHandle();
        }

        // Gets the value of a float parameter
        public float GetFloat(string name)
        {
            return GetFloatString(ref m_Handle, name);
        }

        // Gets the value of a float parameter
        public float GetFloat(int id)
        {
            return GetFloatID(ref m_Handle, id);
        }

        // Sets the value of a float parameter
        public void SetFloat(string name, float value)
        {
            SetFloatString(ref m_Handle, name, value);
        }

        // Sets the value of a float parameter
        public void SetFloat(int id, float value)
        {
            SetFloatID(ref m_Handle, id, value);
        }

        // Gets the value of a bool parameter
        public bool GetBool(string name)
        {
            return GetBoolString(ref m_Handle, name);
        }

        // Gets the value of a bool parameter
        public bool GetBool(int id)
        {
            return GetBoolID(ref m_Handle, id);
        }

        // Sets the value of a bool parameter
        public void SetBool(string name, bool value)
        {
            SetBoolString(ref m_Handle, name, value);
        }

        // Sets the value of a bool parameter
        public void SetBool(int id, bool value)
        {
            SetBoolID(ref m_Handle, id, value);
        }

        // Gets the value of an integer parameter
        public int GetInteger(string name)
        {
            return GetIntegerString(ref m_Handle, name);
        }

        // Gets the value of an integer parameter
        public int GetInteger(int id)
        {
            return GetIntegerID(ref m_Handle, id);
        }

        // Sets the value of an integer parameter
        public void SetInteger(string name, int value)
        {
            SetIntegerString(ref m_Handle, name, value);
        }

        // Sets the value of an integer parameter
        public void SetInteger(int id, int value)
        {
            SetIntegerID(ref m_Handle, id, value);
        }

        // Sets the trigger parameter on
        public void SetTrigger(string name)
        {
            SetTriggerString(ref m_Handle, name);
        }

        // Sets the trigger parameter at on
        public void SetTrigger(int id)
        {
            SetTriggerID(ref m_Handle, id);
        }

        // Resets the trigger parameter at off
        public void ResetTrigger(string name)
        {
            ResetTriggerString(ref m_Handle, name);
        }

        // Resets the trigger parameter at off
        public void ResetTrigger(int id)
        {
            ResetTriggerID(ref m_Handle, id);
        }

        // Returns true if a parameter is controlled by an additional curve on an animation
        public bool IsParameterControlledByCurve(string name)
        {
            return IsParameterControlledByCurveString(ref m_Handle, name);
        }

        // Returns true if a parameter is controlled by an additional curve on an animation
        public bool IsParameterControlledByCurve(int id)
        {
            return IsParameterControlledByCurveID(ref m_Handle, id);
        }

        // The AnimatorController layer count
        public int GetLayerCount()
        {
            return GetLayerCountInternal(ref m_Handle);
        }

        public string GetLayerName(int layerIndex)
        {
            return GetLayerNameInternal(ref m_Handle, layerIndex);
        }

        public int GetLayerIndex(string layerName)
        {
            return GetLayerIndexInternal(ref m_Handle, layerName);
        }

        public float GetLayerWeight(int layerIndex)
        {
            return GetLayerWeightInternal(ref m_Handle, layerIndex);
        }

        public void SetLayerWeight(int layerIndex, float weight)
        {
            SetLayerWeightInternal(ref m_Handle, layerIndex, weight);
        }

        public AnimatorStateInfo GetCurrentAnimatorStateInfo(int layerIndex)
        {
            return GetCurrentAnimatorStateInfoInternal(ref m_Handle, layerIndex);
        }

        public AnimatorStateInfo GetNextAnimatorStateInfo(int layerIndex)
        {
            return GetNextAnimatorStateInfoInternal(ref m_Handle, layerIndex);
        }

        // Gets the Transition information on a specified AnimatorController layer
        public AnimatorTransitionInfo GetAnimatorTransitionInfo(int layerIndex)
        {
            return GetAnimatorTransitionInfoInternal(ref m_Handle, layerIndex);
        }

        public AnimatorClipInfo[] GetCurrentAnimatorClipInfo(int layerIndex)
        {
            return GetCurrentAnimatorClipInfoInternal(ref m_Handle, layerIndex);
        }

        // Gets the list of AnimatorClipInfo currently played by the current state
        public void GetCurrentAnimatorClipInfo(int layerIndex, List<AnimatorClipInfo> clips)
        {
            if (clips == null) throw new ArgumentNullException("clips");

            GetAnimatorClipInfoInternal(ref m_Handle, layerIndex, true, clips);
        }

        // Gets the list of AnimatorClipInfo currently played by the next state
        public void GetNextAnimatorClipInfo(int layerIndex, List<AnimatorClipInfo> clips)
        {
            if (clips == null) throw new ArgumentNullException("clips");

            GetAnimatorClipInfoInternal(ref m_Handle, layerIndex, false, clips);
        }

        // Gets the number of AnimatorClipInfo currently played by the current state
        public int GetCurrentAnimatorClipInfoCount(int layerIndex)
        {
            return GetAnimatorClipInfoCountInternal(ref m_Handle, layerIndex, true);
        }

        // Gets the number of AnimatorClipInfo currently played by the next state
        public int GetNextAnimatorClipInfoCount(int layerIndex)
        {
            return GetAnimatorClipInfoCountInternal(ref m_Handle, layerIndex, false);
        }

        public AnimatorClipInfo[] GetNextAnimatorClipInfo(int layerIndex)
        {
            return GetNextAnimatorClipInfoInternal(ref m_Handle, layerIndex);
        }

        public bool IsInTransition(int layerIndex)
        {
            return IsInTransitionInternal(ref m_Handle, layerIndex);
        }

        public int GetParameterCount()
        {
            return GetParameterCountInternal(ref m_Handle);
        }

        public AnimatorControllerParameter GetParameter(int index)
        {
            AnimatorControllerParameter[] parameters = GetParametersArrayInternal(ref m_Handle);
            if (index < 0 && index >= parameters.Length)
                throw new IndexOutOfRangeException("index");
            return parameters[index];
        }

        public void CrossFadeInFixedTime(string stateName, float transitionDuration)
        {
            CrossFadeInFixedTimeInternal(ref m_Handle, StringToHash(stateName), transitionDuration, -1, 0.0f);
        }

        public void CrossFadeInFixedTime(string stateName, float transitionDuration, int layer)
        {
            CrossFadeInFixedTimeInternal(ref m_Handle, StringToHash(stateName), transitionDuration, layer, 0.0f);
        }

        public void CrossFadeInFixedTime(string stateName, float transitionDuration, [UnityEngine.Internal.DefaultValue("-1")] int layer, [UnityEngine.Internal.DefaultValue("0.0f")] float fixedTime)
        {
            CrossFadeInFixedTimeInternal(ref m_Handle, StringToHash(stateName), transitionDuration, layer, fixedTime);
        }

        public void CrossFadeInFixedTime(int stateNameHash, float transitionDuration)
        {
            CrossFadeInFixedTimeInternal(ref m_Handle, stateNameHash, transitionDuration, -1, 0.0f);
        }

        public void CrossFadeInFixedTime(int stateNameHash, float transitionDuration, int layer)
        {
            CrossFadeInFixedTimeInternal(ref m_Handle, stateNameHash, transitionDuration, layer, 0.0f);
        }

        public void CrossFadeInFixedTime(int stateNameHash, float transitionDuration, [UnityEngine.Internal.DefaultValue("-1")] int layer, [UnityEngine.Internal.DefaultValue("0.0f")] float fixedTime)
        {
            CrossFadeInFixedTimeInternal(ref m_Handle, stateNameHash, transitionDuration, layer, fixedTime);
        }

        public void CrossFade(string stateName, float transitionDuration)
        {
            CrossFadeInternal(ref m_Handle, StringToHash(stateName), transitionDuration, -1, float.NegativeInfinity);
        }

        public void CrossFade(string stateName, float transitionDuration, int layer)
        {
            CrossFadeInternal(ref m_Handle, StringToHash(stateName), transitionDuration, layer, float.NegativeInfinity);
        }

        public void CrossFade(string stateName, float transitionDuration, [UnityEngine.Internal.DefaultValue("-1")] int layer, [UnityEngine.Internal.DefaultValue("float.NegativeInfinity")] float normalizedTime)
        {
            CrossFadeInternal(ref m_Handle, StringToHash(stateName), transitionDuration, layer, normalizedTime);
        }

        public void CrossFade(int stateNameHash, float transitionDuration)
        {
            CrossFadeInternal(ref m_Handle, stateNameHash, transitionDuration, -1, float.NegativeInfinity);
        }

        public void CrossFade(int stateNameHash, float transitionDuration, int layer)
        {
            CrossFadeInternal(ref m_Handle, stateNameHash, transitionDuration, layer, float.NegativeInfinity);
        }

        public void CrossFade(int stateNameHash, float transitionDuration, [UnityEngine.Internal.DefaultValue("-1")] int layer, [UnityEngine.Internal.DefaultValue("float.NegativeInfinity")] float normalizedTime)
        {
            CrossFadeInternal(ref m_Handle, stateNameHash, transitionDuration, layer, normalizedTime);
        }

        public void PlayInFixedTime(string stateName)
        {
            PlayInFixedTimeInternal(ref m_Handle, StringToHash(stateName), -1, float.NegativeInfinity);
        }

        public void PlayInFixedTime(string stateName, int layer)
        {
            PlayInFixedTimeInternal(ref m_Handle, StringToHash(stateName), layer, float.NegativeInfinity);
        }

        public void PlayInFixedTime(string stateName, [UnityEngine.Internal.DefaultValue("-1")] int layer, [UnityEngine.Internal.DefaultValue("float.NegativeInfinity")] float fixedTime)
        {
            PlayInFixedTimeInternal(ref m_Handle, StringToHash(stateName), layer, fixedTime);
        }

        public void PlayInFixedTime(int stateNameHash)
        {
            PlayInFixedTimeInternal(ref m_Handle, stateNameHash, -1, float.NegativeInfinity);
        }

        public void PlayInFixedTime(int stateNameHash, int layer)
        {
            PlayInFixedTimeInternal(ref m_Handle, stateNameHash, layer, float.NegativeInfinity);
        }

        public void PlayInFixedTime(int stateNameHash, [UnityEngine.Internal.DefaultValue("-1")] int layer, [UnityEngine.Internal.DefaultValue("float.NegativeInfinity")] float fixedTime)
        {
            PlayInFixedTimeInternal(ref m_Handle, stateNameHash, layer, fixedTime);
        }

        public void Play(string stateName)
        {
            PlayInternal(ref m_Handle, StringToHash(stateName), -1, float.NegativeInfinity);
        }

        public void Play(string stateName, int layer)
        {
            PlayInternal(ref m_Handle, StringToHash(stateName), layer, float.NegativeInfinity);
        }

        public void Play(string stateName, [UnityEngine.Internal.DefaultValue("-1")] int layer, [UnityEngine.Internal.DefaultValue("float.NegativeInfinity")] float normalizedTime)
        {
            PlayInternal(ref m_Handle, StringToHash(stateName), layer, normalizedTime);
        }

        public void Play(int stateNameHash)
        {
            PlayInternal(ref m_Handle, stateNameHash, -1, float.NegativeInfinity);
        }

        public void Play(int stateNameHash, int layer)
        {
            PlayInternal(ref m_Handle, stateNameHash, layer, float.NegativeInfinity);
        }

        public void Play(int stateNameHash, [UnityEngine.Internal.DefaultValue("-1")] int layer, [UnityEngine.Internal.DefaultValue("float.NegativeInfinity")] float normalizedTime)
        {
            PlayInternal(ref m_Handle, stateNameHash, layer, normalizedTime);
        }

        public bool HasState(int layerIndex, int stateID)
        {
            return HasStateInternal(ref m_Handle, layerIndex, stateID);
        }

        internal string ResolveHash(int hash)
        {
            return ResolveHashInternal(ref m_Handle, hash);
        }

        // Methods bindings.
        extern private static bool CreateHandleInternal(PlayableGraph graph, RuntimeAnimatorController controller, ref PlayableHandle handle);
        extern private static RuntimeAnimatorController GetAnimatorControllerInternal(ref PlayableHandle handle);
        extern private static int GetLayerCountInternal(ref PlayableHandle handle);
        extern private static string GetLayerNameInternal(ref PlayableHandle handle, int layerIndex);
        extern private static int GetLayerIndexInternal(ref PlayableHandle handle, string layerName);
        extern private static float GetLayerWeightInternal(ref PlayableHandle handle, int layerIndex);
        extern private static void SetLayerWeightInternal(ref PlayableHandle handle,  int layerIndex, float weight);
        extern private static AnimatorStateInfo GetCurrentAnimatorStateInfoInternal(ref PlayableHandle handle, int layerIndex);
        extern private static AnimatorStateInfo GetNextAnimatorStateInfoInternal(ref PlayableHandle handle, int layerIndex);
        extern private static AnimatorTransitionInfo GetAnimatorTransitionInfoInternal(ref PlayableHandle handle, int layerIndex);
        extern private static AnimatorClipInfo[] GetCurrentAnimatorClipInfoInternal(ref PlayableHandle handle, int layerIndex);
        extern private static int GetAnimatorClipInfoCountInternal(ref PlayableHandle handle, int layerIndex, bool current);
        extern private static AnimatorClipInfo[] GetNextAnimatorClipInfoInternal(ref PlayableHandle handle, int layerIndex);
        extern private static string ResolveHashInternal(ref PlayableHandle handle, int hash);
        extern private static bool IsInTransitionInternal(ref PlayableHandle handle, int layerIndex);
        extern private static int GetParameterCountInternal(ref PlayableHandle handle);
        [ThreadSafe]
        extern private static int StringToHash(string name);
        extern private static void CrossFadeInFixedTimeInternal(ref PlayableHandle handle, int stateNameHash, float transitionDuration, int layer, float fixedTime);
        extern private static void CrossFadeInternal(ref PlayableHandle handle, int stateNameHash, float transitionDuration, int layer, float normalizedTime);
        extern private static void PlayInFixedTimeInternal(ref PlayableHandle handle, int stateNameHash, int layer, float fixedTime);
        extern private static void PlayInternal(ref PlayableHandle handle, int stateNameHash, int layer, float normalizedTime);
        extern private static bool HasStateInternal(ref PlayableHandle handle, int layerIndex, int stateID);

        extern private static void SetFloatString(ref PlayableHandle handle, string name, float value);
        extern private static void SetFloatID(ref PlayableHandle handle, int id, float value);
        extern private static float GetFloatString(ref PlayableHandle handle, string name);
        extern private static float GetFloatID(ref PlayableHandle handle, int id);
        extern private static void SetBoolString(ref PlayableHandle handle, string name, bool value);
        extern private static void SetBoolID(ref PlayableHandle handle, int id, bool value);
        extern private static bool GetBoolString(ref PlayableHandle handle, string name);
        extern private static bool GetBoolID(ref PlayableHandle handle, int id);
        extern private static void SetIntegerString(ref PlayableHandle handle, string name, int value);
        extern private static void SetIntegerID(ref PlayableHandle handle, int id, int value);
        extern private static int GetIntegerString(ref PlayableHandle handle, string name);
        extern private static int GetIntegerID(ref PlayableHandle handle, int id);
        extern private static void SetTriggerString(ref PlayableHandle handle, string name);
        extern private static void SetTriggerID(ref PlayableHandle handle, int id);
        extern private static void ResetTriggerString(ref PlayableHandle handle, string name);
        extern private static void ResetTriggerID(ref PlayableHandle handle, int id);
        extern private static bool IsParameterControlledByCurveString(ref PlayableHandle handle, string name);
        extern private static bool IsParameterControlledByCurveID(ref PlayableHandle handle, int id);
    }
}
