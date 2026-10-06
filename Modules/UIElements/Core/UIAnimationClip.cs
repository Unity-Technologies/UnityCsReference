// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// An asset used to animate <see cref="VisualElement"/> hierarchies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Assign a <c>UIAnimationClip</c> to a visual element through the USS property
    /// <c>animation-name</c> or the <see cref="IStyle.unityAnimationClip"/> API.
    /// The element becomes the animation root and all animation paths resolve relative to it.
    /// </para>
    /// </remarks>
    [NativeHeader("Modules/UIElements/Core/Native/UIAnimationClip.h")]
    [NativeHeader("Modules/UIElements/Core/Native/UIAnimationClip.bindings.h")]
    [NativeClass("UIAnimationClip", PersistentTypeId = 0x175EDA4B)]
    public sealed class UIAnimationClip : Object
    {
        internal UIAnimationClip(global::UnityEngine.EntityId id) : base(id) {}
        /// <summary>
        /// Creates a new empty UIAnimationClip.
        /// </summary>
        public UIAnimationClip()
        {
            SetEntityIdFromConstructor(Internal_Create());
        }

        [FreeFunction("UIAnimationClipBindings::Internal_Create")]
        extern private static EntityId Internal_Create();

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule", "UnityEditor.UIBuilderModule")]
        internal extern AnimationClip animationClip
        {
            [NativeMethod("GetAnimationClip")]
            get;
            [NativeMethod("SetAnimationClip")]
            set;
        }
    }
}
