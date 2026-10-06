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

    ///<summary>Interface to control Animator Override Controller.</summary>
    ///<remarks>
    ///  <para>Animator Override Controller is used to override Animation Clips from a controller to specialize animations for a given Avatar.
    ///Swapping <see cref="P:UnityEngine.Animator.runtimeAnimatorController" /> with an <see cref="AnimatorOverrideController" /> based on the same <see cref="T:UnityEditor.Animations.AnimatorController" /> at runtime doesn't reset state machine's current state.
    ///
    ///There are three ways to use the Animator Override Controller.
    ///
    ///**1. Create an Animator Override Controller in the Editor.**
    ///
    ///**2. Change one Animation Clip per frame at runtime (Basic use case).**
    ///
    ///In this case the indexer operator <see cref="AnimatorOverrideController.this" /> could be used, but be careful as each call will trigger a reallocation of the animator's clip bindings.</para>
    ///  <para>**3. Changing many Animation Clips per frame at runtime (Advanced use case).**
    ///
    ///The <see cref="AnimatorOverrideController.ApplyOverrides" /> method is well suited for this case as it reduce the number of animator's clips bindings reallocation to only one per call.</para>
    ///</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using UnityEngine;
    ///
    ///public class SwapWeapon : MonoBehaviour
    ///{
    ///    public AnimationClip[] weaponAnimationClip;
    ///
    ///    protected Animator animator;
    ///    protected AnimatorOverrideController animatorOverrideController;
    ///
    ///    protected int weaponIndex;
    ///
    ///    public void Start()
    ///    {
    ///        animator = GetComponent<Animator>();
    ///        weaponIndex = 0;
    ///
    ///        animatorOverrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
    ///        animator.runtimeAnimatorController = animatorOverrideController;
    ///    }
    ///
    ///    public void Update()
    ///    {
    ///        if (Input.GetButtonDown("NextWeapon"))
    ///        {
    ///            weaponIndex = (weaponIndex + 1) % weaponAnimationClip.Length;
    ///            animatorOverrideController["shot"] = weaponAnimationClip[weaponIndex];
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<example>
    ///  <code><![CDATA[
    ///using UnityEngine;
    ///using System.Collections.Generic;
    ///
    ///public class AnimationClipOverrides : List<KeyValuePair<AnimationClip, AnimationClip>>
    ///{
    ///    public AnimationClipOverrides(int capacity) : base(capacity) {}
    ///
    ///    public AnimationClip this[string name]
    ///    {
    ///        get { return this.Find(x => x.Key.name.Equals(name)).Value; }
    ///        set
    ///        {
    ///            int index = this.FindIndex(x => x.Key.name.Equals(name));
    ///            if (index != -1)
    ///                this[index] = new KeyValuePair<AnimationClip, AnimationClip>(this[index].Key, value);
    ///        }
    ///    }
    ///}
    ///
    ///public class Weapon
    ///{
    ///    public AnimationClip singleAttack;
    ///    public AnimationClip comboAttack;
    ///    public AnimationClip dashAttack;
    ///    public AnimationClip smashAttack;
    ///}
    ///
    ///public class SwapWeapon : MonoBehaviour
    ///{
    ///    public Weapon[] weapons;
    ///
    ///    protected Animator animator;
    ///    protected AnimatorOverrideController animatorOverrideController;
    ///
    ///    protected int weaponIndex;
    ///
    ///    protected AnimationClipOverrides clipOverrides;
    ///    public void Start()
    ///    {
    ///        animator = GetComponent<Animator>();
    ///        weaponIndex = 0;
    ///
    ///        animatorOverrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
    ///        animator.runtimeAnimatorController = animatorOverrideController;
    ///
    ///        clipOverrides = new AnimationClipOverrides(animatorOverrideController.overridesCount);
    ///        animatorOverrideController.GetOverrides(clipOverrides);
    ///    }
    ///
    ///    public void Update()
    ///    {
    ///        if (Input.GetButtonDown("NextWeapon"))
    ///        {
    ///            weaponIndex = (weaponIndex + 1) % weapons.Length;
    ///            clipOverrides["SingleAttack"] = weapons[weaponIndex].singleAttack;
    ///            clipOverrides["ComboAttack"] = weapons[weaponIndex].comboAttack;
    ///            clipOverrides["DashAttack"] = weapons[weaponIndex].dashAttack;
    ///            clipOverrides["SmashAttack"] = weapons[weaponIndex].smashAttack;
    ///            animatorOverrideController.ApplyOverrides(clipOverrides);
    ///        }
    ///    }
    ///}
    ///]]></code>
    ///</example>
    [NativeHeader("Modules/Animation/AnimatorOverrideController.h")]
    [NativeHeader("Modules/Animation/ScriptBindings/Animation.bindings.h")]
    [UsedByNativeCode]
    [global::UnityEngine.NativeClass("AnimatorOverrideController", PersistentTypeId = 221)]
    [HelpURL("AnimatorOverrideController")]
    public class AnimatorOverrideController : RuntimeAnimatorController
    {
        internal AnimatorOverrideController(global::UnityEngine.EntityId id) : base(id) {}
        ///<summary>Creates an empty Animator Override Controller.</summary>
        public AnimatorOverrideController()
        {
            SetEntityIdFromConstructor(Internal_Create(null));
            OnOverrideControllerDirty = null;
        }

        ///<summary>Creates an Animator Override Controller that overrides **controller**.</summary>
        ///<remarks>Although the Animator Override Controller doesn't support nested Animator Override Controller, this constructor will find the right controller for you.</remarks>
        ///<param name="controller">Runtime Animator Controller to override.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class SwapWeapon : MonoBehaviour
        ///{
        ///    protected Animator animator;
        ///    protected AnimatorOverrideController animatorOverrideController;
        ///
        ///    public void Start()
        ///    {
        ///        animator = GetComponent<Animator>();
        ///
        ///        animatorOverrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
        ///        animator.runtimeAnimatorController = animatorOverrideController;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public AnimatorOverrideController(RuntimeAnimatorController controller)
        {
            SetEntityIdFromConstructor(Internal_Create(controller));
            OnOverrideControllerDirty = null;
        }

        [FreeFunction("AnimationBindings::CreateAnimatorOverrideController")]
        extern private static EntityId Internal_Create(RuntimeAnimatorController controller);

        // The runtime representation of AnimatorController that controls the Animator
        ///<summary>The Runtime Animator Controller that the Animator Override Controller overrides.</summary>
        ///<remarks>Note: Animator Override Controllers cannot be nested, which means you cannot supply an Animator Override Controller to <see cref="AnimatorOverrideController.runtimeAnimatorController" />.</remarks>
        extern public RuntimeAnimatorController runtimeAnimatorController
        {
            [NativeMethod("GetAnimatorController")]
            get;
            [NativeMethod("SetAnimatorController")]
            set;
        }

        // Returns the animation clip named /name/.
        ///<summary>Returns either the overriding Animation Clip if set or the original Animation Clip named name.</summary>
        ///<remarks>Note: You should avoid calling this function more than once per frame per Animator as each call will trigger a reallocation of the animator's clip bindings. Instead use <see cref="AnimatorOverrideController.ApplyOverrides" />.</remarks>
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
        ///<summary>Returns either the overriding Animation Clip if set or the original Animation Clip named name.</summary>
        ///<remarks>Note: You should avoid calling this function more than once per frame per Animator as each call will trigger a reallocation of the animator's clip bindings. Instead use <see cref="AnimatorOverrideController.ApplyOverrides" />.</remarks>
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

        ///<summary>Returns the count of overrides.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using System.Collections;
        ///using System.Collections.Generic;
        ///using UnityEngine;
        ///
        ///public class ResetOverrides : MonoBehaviour
        ///{
        ///    public AnimatorOverrideController overrideController;
        ///    protected List<KeyValuePair<AnimationClip, AnimationClip>> overrides;
        ///
        ///    public void ResetAllOverrides()
        ///    {
        ///        overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
        ///        overrideController.GetOverrides(overrides);
        ///        for (int i = 0; i < overrides.Count; ++i)
        ///            overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, null);
        ///        overrideController.ApplyOverrides(overrides);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        extern public int overridesCount
        {
            [NativeMethod("GetOriginalClipsCount")]
            get;
        }

        ///<summary>Gets the list of Animation Clip overrides currently defined in this Animator Override Controller.</summary>
        ///<remarks>This function is allocation-free if you pre-allocate the overrides list with <see cref="AnimatorOverrideController.overridesCount" />.</remarks>
        ///<param name="overrides">Array to receive results.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using System.Collections;
        ///using System.Collections.Generic;
        ///using UnityEngine;
        ///
        ///public class ResetOverrides : MonoBehaviour
        ///{
        ///    public AnimatorOverrideController overrideController;
        ///    protected List<KeyValuePair<AnimationClip, AnimationClip>> overrides;
        ///
        ///    public void ResetAllOverrides()
        ///    {
        ///        overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
        ///        overrideController.GetOverrides(overrides);
        ///        for (int i = 0; i < overrides.Count; ++i)
        ///            overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, null);
        ///        overrideController.ApplyOverrides(overrides);
        ///    }
        ///}
        ///]]></code>
        ///</example>
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

        ///<summary>Applies the list of overrides on this Animator Override Controller.</summary>
        ///<remarks>This function should be used as soon as you need to override more than two Animation Clips in the same frame. The function will notify the Animator to update all the internal bindings after processing the whole list.</remarks>
        ///<param name="overrides">Overrides list to apply.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using System.Collections.Generic;
        ///
        ///public class AnimationClipOverrides : List<KeyValuePair<AnimationClip, AnimationClip>>
        ///{
        ///    public AnimationClipOverrides(int capacity) : base(capacity) {}
        ///
        ///    public AnimationClip this[string name]
        ///    {
        ///        get { return this.Find(x => x.Key.name.Equals(name)).Value; }
        ///        set
        ///        {
        ///            int index = this.FindIndex(x => x.Key.name.Equals(name));
        ///            if (index != -1)
        ///                this[index] = new KeyValuePair<AnimationClip, AnimationClip>(this[index].Key, value);
        ///        }
        ///    }
        ///}
        ///
        ///public class Weapon
        ///{
        ///    public AnimationClip singleAttack;
        ///    public AnimationClip comboAttack;
        ///    public AnimationClip dashAttack;
        ///    public AnimationClip smashAttack;
        ///}
        ///
        ///public class SwapWeapon : MonoBehaviour
        ///{
        ///    public Weapon[] weapons;
        ///
        ///    protected Animator animator;
        ///    protected AnimatorOverrideController animatorOverrideController;
        ///
        ///    protected int weaponIndex;
        ///
        ///    protected AnimationClipOverrides clipOverrides;
        ///    public void Start()
        ///    {
        ///        animator = GetComponent<Animator>();
        ///        weaponIndex = 0;
        ///
        ///        animatorOverrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
        ///        animator.runtimeAnimatorController = animatorOverrideController;
        ///
        ///        clipOverrides = new AnimationClipOverrides(animatorOverrideController.overridesCount);
        ///        animatorOverrideController.GetOverrides(clipOverrides);
        ///    }
        ///
        ///    public void Update()
        ///    {
        ///        if (Input.GetButtonDown("NextWeapon"))
        ///        {
        ///            weaponIndex = (weaponIndex + 1) % weapons.Length;
        ///            clipOverrides["SingleAttack"] = weapons[weaponIndex].singleAttack;
        ///            clipOverrides["ComboAttack"] = weapons[weaponIndex].comboAttack;
        ///            clipOverrides["DashAttack"] = weapons[weaponIndex].dashAttack;
        ///            clipOverrides["SmashAttack"] = weapons[weaponIndex].smashAttack;
        ///            animatorOverrideController.ApplyOverrides(clipOverrides);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public void ApplyOverrides(IList<KeyValuePair<AnimationClip, AnimationClip>> overrides)
        {
            if (overrides == null)
                throw new System.ArgumentNullException("overrides");

            for (int i = 0; i < overrides.Count; i++)
                SetClip(overrides[i].Key, overrides[i].Value, false);

            SendNotification();
        }

        ///<summary>Returns the list of orignal Animation Clip from the controller and their override Animation Clip.</summary>
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
