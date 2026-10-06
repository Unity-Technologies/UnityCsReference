// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    ///<summary>Stores a reference to an animation asset associated with a [State Machine](xref:AnimationStateMachines) state.</summary>
    ///<remarks>The <see cref="Motion" /> class acts as an abstraction for APIs that accept either the <see cref="AnimationClip" /> or <see cref="T:UnityEditor.Animations.BlendTree" /> animation classes.
    ///
    ///
    ///
    ///This example demonstrates how to create a <see cref="T:UnityEditor.Animations.BlendTree" /> from a selection of <see cref="AnimationClip">AnimationClips</see>. This example also demonstrates how a <see cref="T:UnityEditor.Animations.BlendTree" /> is composed of two or more child <see cref="Motion" />s, and how to use a <see cref="T:UnityEditor.Animations.BlendTree" /> to instantiate an <see cref="T:UnityEditor.Animations.AnimatorState" />.</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Tests/EditModeAndPlayModeTests/Animation/Assets/Editor/DocumentationExamples/MotionClassExample.cs}]]></code>
    ///</example>
    ///<seealso cref="P:UnityEditor.Animations.AnimatorState.motion" />
    ///<seealso cref="M:UnityEditor.Animations.AnimatorController.AddMotion" />
    ///<seealso cref="M:UnityEditor.Animations.BlendTree.AddChild" />
    [global::UnityEngine.NativeClass("Motion", PersistentTypeId = 207)]
    [NativeHeader("Modules/Animation/Motion.h")]
    public partial class Motion : Object
    {
        protected Motion() {}
        protected internal Motion(global::UnityEngine.EntityId id) : base(id) {}

        extern public float averageDuration { get; }
        extern public float averageAngularSpeed { get; }
        extern public Vector3 averageSpeed { get; }
        extern public float apparentSpeed { get; }

        extern public bool isLooping
        {
            [NativeMethod("IsLooping")]
            get;
        }

        ///<exclude />
        extern public bool legacy
        {
            [NativeMethod("IsLegacy")]
            get;
        }

        extern public bool isHumanMotion
        {
            [NativeMethod("IsHumanMotion")]
            get;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("ValidateIfRetargetable is not supported anymore, please use isHumanMotion instead.", true)]
        public bool ValidateIfRetargetable(bool val) { return false; }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("isAnimatorMotion is not supported anymore, please use !legacy instead.", true)]
        public bool isAnimatorMotion { get; }
    }
}
