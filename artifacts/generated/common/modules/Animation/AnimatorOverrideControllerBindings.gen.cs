// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{


[System.Obsolete ("This class is not used anymore.  See AnimatorOverrideController.GetOverrides() and AnimatorOverrideController.ApplyOverrides()")]
[System.Serializable]
[StructLayout(LayoutKind.Sequential)]
public sealed partial class AnimationClipPair
{
            public AnimationClip originalClip;
            public AnimationClip overrideClip;
}

public sealed partial class AnimatorOverrideController : RuntimeAnimatorController
{
    public AnimatorOverrideController()
        {
            Internal_CreateAnimatorOverrideController(this, null);
        }
    
    
    public AnimatorOverrideController(RuntimeAnimatorController controller)
        {
            Internal_CreateAnimatorOverrideController(this, controller);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_CreateAnimatorOverrideController ([Writable] AnimatorOverrideController self, RuntimeAnimatorController controller) ;

    public extern  RuntimeAnimatorController runtimeAnimatorController
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public AnimationClip this[string name]
            {
            get { return Internal_GetClipByName(name, true); }
            set { Internal_SetClipByName(name, value); }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private AnimationClip Internal_GetClipByName (string name, bool returnEffectiveClip) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_SetClipByName (string name, AnimationClip clip) ;

    public AnimationClip this[AnimationClip clip]
            {
            get { return Internal_GetClip(clip, true); }
            set { Internal_SetClip(clip, value); }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private AnimationClip Internal_GetClip (AnimationClip originalClip, bool returnEffectiveClip) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Internal_SetClip (AnimationClip originalClip, AnimationClip overrideClip, [uei.DefaultValue("true")]  bool notify ) ;

    [uei.ExcludeFromDocs]
    private void Internal_SetClip (AnimationClip originalClip, AnimationClip overrideClip) {
        bool notify = true;
        Internal_SetClip ( originalClip, overrideClip, notify );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void SendNotification () ;

    internal delegate void OnOverrideControllerDirtyCallback();
    
    
    internal OnOverrideControllerDirtyCallback OnOverrideControllerDirty;
    
    
    [RequiredByNativeCode] internal static void OnInvalidateOverrideController(AnimatorOverrideController controller)
        {
            if (controller.OnOverrideControllerDirty != null)
                controller.OnOverrideControllerDirty();
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private AnimationClip Internal_GetOriginalClip (int index) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private AnimationClip Internal_GetOverrideClip (AnimationClip originalClip) ;

    public extern  int overridesCount
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public void GetOverrides(List<KeyValuePair<AnimationClip, AnimationClip> > overrides)
        {
            if (overrides == null)
                throw new System.ArgumentNullException("overrides");

            int count = overridesCount;
            if (overrides.Capacity < count)
                overrides.Capacity = count;

            overrides.Clear();
            for (int i = 0; i < count; ++i)
            {
                AnimationClip originalClip = Internal_GetOriginalClip(i);
                overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(originalClip, Internal_GetOverrideClip(originalClip)));
            }
        }
    
    
    public void ApplyOverrides(IList<KeyValuePair<AnimationClip, AnimationClip> > overrides)
        {
            if (overrides == null)
                throw new System.ArgumentNullException("overrides");

            for (int i = 0; i < overrides.Count; i++)
                Internal_SetClip(overrides[i].Key, overrides[i].Value, false);

            SendNotification();
        }
    
    
    [System.Obsolete ("clips property is deprecated. Use AnimatorOverrideController.GetOverrides and AnimatorOverrideController.ApplyOverrides instead.")]
    public AnimationClipPair[] clips
        {
            get
            {
                int count = overridesCount;

                AnimationClipPair[] clipPair = new AnimationClipPair[count];
                for (int i = 0; i < count; i++)
                {
                    clipPair[i] = new AnimationClipPair();
                    clipPair[i].originalClip = Internal_GetOriginalClip(i);
                    clipPair[i].overrideClip = Internal_GetOverrideClip(clipPair[i].originalClip);
                }

                return clipPair;
            }
            set
            {
                for (int i = 0; i < value.Length; i++)
                    Internal_SetClip(value[i].originalClip, value[i].overrideClip, false);

                SendNotification();
            }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal void PerformOverrideClipListCleanup () ;

}

}
