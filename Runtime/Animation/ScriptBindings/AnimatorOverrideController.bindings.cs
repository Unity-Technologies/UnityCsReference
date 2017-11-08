// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    [Obsolete("This class is not used anymore. See AnimatorOverrideController.GetOverrides() and AnimatorOverrideController.ApplyOverrides()")]
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public class AnimationClipPair
    {
        public AnimationClip originalClip;
        public AnimationClip overrideClip;
    }

    [NativeHeader("Runtime/Animation/AnimatorOverrideController.h")]
    [NativeHeader("Runtime/Animation/ScriptBindings/Animation.bindings.h")]
    [UsedByNativeCode]
    public class AnimatorOverrideController : RuntimeAnimatorController
    {
        public AnimatorOverrideController()
        {
            Internal_Create(this, null);
            OnOverrideControllerDirty = null;
        }

        public AnimatorOverrideController(RuntimeAnimatorController controller)
        {
            Internal_Create(this, controller);
            OnOverrideControllerDirty = null;
        }

        [FreeFunction("AnimationBindings::CreateAnimatorOverrideController")]
        extern private static void Internal_Create([Writable] AnimatorOverrideController self, RuntimeAnimatorController controller);

        // The runtime representation of AnimatorController that controls the Animator
        extern public RuntimeAnimatorController runtimeAnimatorController
        {
            [NativeMethod("GetAnimatorController")]
            get;
            [NativeMethod("SetAnimatorController")]
            set;
        }

        // Returns the animation clip named /name/.
        public AnimationClip this[string name]
        {
            get { return Internal_GetClipByName(name, true); }
            set { Internal_SetClipByName(name, value); }
        }

        [NativeMethod("GetClip")]
        extern private AnimationClip Internal_GetClipByName(string name, bool returnEffectiveClip);

        [NativeMethod("SetClip")]
        extern private void Internal_SetClipByName(string name, AnimationClip clip);

        // Returns the animation clip named /name/.
        public AnimationClip this[AnimationClip clip]
        {
            get { return GetClip(clip, true); }
            set { SetClip(clip, value, true); }
        }

        extern private AnimationClip GetClip(AnimationClip originalClip, bool returnEffectiveClip);

        extern private void SetClip(AnimationClip originalClip, AnimationClip overrideClip, bool notify);

        extern private void SendNotification();

        extern private AnimationClip GetOriginalClip(int index);
        extern private AnimationClip GetOverrideClip(AnimationClip originalClip);

        extern public int overridesCount
        {
            [NativeMethod("GetOriginalClipsCount")]
            get;
        }

        public void GetOverrides(List<KeyValuePair<AnimationClip, AnimationClip>> overrides)
        {
            if (overrides == null)
                throw new System.ArgumentNullException("overrides");

            int count = overridesCount;
            if (overrides.Capacity < count)
                overrides.Capacity = count;

            overrides.Clear();
            for (int i = 0; i < count; ++i)
            {
                AnimationClip originalClip = GetOriginalClip(i);
                overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(originalClip, GetOverrideClip(originalClip)));
            }
        }

        public void ApplyOverrides(IList<KeyValuePair<AnimationClip, AnimationClip>> overrides)
        {
            if (overrides == null)
                throw new System.ArgumentNullException("overrides");

            for (int i = 0; i < overrides.Count; i++)
                SetClip(overrides[i].Key, overrides[i].Value, false);

            SendNotification();
        }

        [Obsolete("AnimatorOverrideController.clips property is deprecated. Use AnimatorOverrideController.GetOverrides and AnimatorOverrideController.ApplyOverrides instead.")]
        public AnimationClipPair[] clips
        {
            get
            {
                int count = overridesCount;

                AnimationClipPair[] clipPair = new AnimationClipPair[count];
                for (int i = 0; i < count; i++)
                {
                    clipPair[i] = new AnimationClipPair();
                    clipPair[i].originalClip = GetOriginalClip(i);
                    clipPair[i].overrideClip = GetOverrideClip(clipPair[i].originalClip);
                }

                return clipPair;
            }
            set
            {
                for (int i = 0; i < value.Length; i++)
                    SetClip(value[i].originalClip, value[i].overrideClip, false);

                SendNotification();
            }
        }

        [NativeConditional("UNITY_EDITOR")]
        extern internal void PerformOverrideClipListCleanup();

        internal delegate void OnOverrideControllerDirtyCallback();

        internal OnOverrideControllerDirtyCallback OnOverrideControllerDirty;

        [NativeConditional("UNITY_EDITOR")]
        [RequiredByNativeCode]
        internal static void OnInvalidateOverrideController(AnimatorOverrideController controller)
        {
            if (controller.OnOverrideControllerDirty != null)
                controller.OnOverrideControllerDirty();
        }
    }
}
