// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    ///<summary>A representation of the Animator Controller, optimized for runtime.</summary>
    ///<remarks>At runtime, Unity replaces the <see cref="T:UnityEditor.Animations.AnimatorController" /> class with this optimized runtime class. Access to Editor functions, such as modifying the structure of an Animator Controller, are restricted.
    ///
    ///This optimized class provides the following different ways to access and modify an Animator Controller at runtime:
    ///
    ///* Store the reference of an Animator Controller so you can replace the Animator Controller of an <see cref="Animator" />. This is useful for modifiying the structure of an Animator Controller at runtime. Use <see cref="Animator.runtimeAnimatorController" /> to access the controller to be replaced.
    ///* Create an <see cref="AnimatorOverrideController" /> that you can use to override the Animation Clips associated with an Animator Controller. This is more efficient than replacing a controller because only the clips are updated. The Animator Override Controller is based on the Runtime Animator Controller that initializes it. .
    ///
    ///The following example demonstrates how to spawn GameObjects at runtime. Each GameObject is animated with different Animator Controllers.</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/DocumentationExamples/RandomZombieSpawner.cs}]]></code>
    ///</example>
    ///<seealso cref="AnimatorOverrideController.runtimeAnimatorController" />
    [NativeHeader("Modules/Animation/RuntimeAnimatorController.h")]
    [UsedByNativeCode]
    [global::UnityEngine.NativeClass("RuntimeAnimatorController", PersistentTypeId = 93)]
    [ExcludeFromObjectFactory]
    public partial class RuntimeAnimatorController : Object
    {
        ///<exclude />
        protected RuntimeAnimatorController() {}
        protected internal RuntimeAnimatorController(global::UnityEngine.EntityId id) : base(id) {}

        ///<summary>Retrieves all <see cref="AnimationClip" /> used by the controller.</summary>
        extern public AnimationClip[] animationClips { get; }
    }
}
