// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Playables;
using UnityEngine.Internal;

namespace UnityEngine.Experimental.Animations
{
    [NativeHeader("Runtime/Animation/ScriptBindings/AnimatorJobExtensions.bindings.h")]
    [NativeHeader("Runtime/Animation/Animator.h")]
    [NativeHeader("Runtime/Animation/Director/AnimationStreamHandles.h")]
    [NativeHeader("Runtime/Animation/Director/AnimationSceneHandles.h")]
    [NativeHeader("Runtime/Animation/Director/AnimationStream.h")]
    [StaticAccessor("AnimatorJobExtensionsBindings", StaticAccessorType.DoubleColon)]
    public static class AnimatorJobExtensions
    {
        public static TransformStreamHandle BindStreamTransform(this Animator animator, Transform transform)
        {
            TransformStreamHandle transformStreamHandle = new TransformStreamHandle();
            InternalBindStreamTransform(animator, transform, out transformStreamHandle);
            return transformStreamHandle;
        }

        public static PropertyStreamHandle BindStreamProperty(this Animator animator, Transform transform, Type type, string property)
        {
            return BindStreamProperty(animator, transform, type, property, false);
        }

        public static PropertyStreamHandle BindStreamProperty(this Animator animator, Transform transform, Type type, string property, [DefaultValue("false")] bool isObjectReference)
        {
            PropertyStreamHandle propertyStreamHandle = new PropertyStreamHandle();
            InternalBindStreamProperty(animator, transform, type, property, isObjectReference, out propertyStreamHandle);
            return propertyStreamHandle;
        }

        public static TransformSceneHandle BindSceneTransform(this Animator animator, Transform transform)
        {
            TransformSceneHandle transformSceneHandle = new TransformSceneHandle();
            InternalBindSceneTransform(animator, transform, out transformSceneHandle);
            return transformSceneHandle;
        }

        public static PropertySceneHandle BindSceneProperty(this Animator animator, Transform transform, Type type, string property)
        {
            return BindSceneProperty(animator, transform, type, property, false);
        }

        public static PropertySceneHandle BindSceneProperty(this Animator animator, Transform transform, Type type, string property, [DefaultValue("false")] bool isObjectReference)
        {
            PropertySceneHandle propertySceneHandle = new PropertySceneHandle();
            InternalBindSceneProperty(animator, transform, type, property, isObjectReference, out propertySceneHandle);
            return propertySceneHandle;
        }

        public static bool OpenAnimationStream(this Animator animator, ref AnimationStream stream)
        {
            return InternalOpenAnimationStream(animator, ref stream);
        }

        public static void CloseAnimationStream(this Animator animator, ref AnimationStream stream)
        {
            InternalCloseAnimationStream(animator, ref stream);
        }

        public static void ResolveAllStreamHandles(this Animator animator)
        {
            InternalResolveAllStreamHandles(animator);
        }

        public static void ResolveAllSceneHandles(this Animator animator)
        {
            InternalResolveAllSceneHandles(animator);
        }

        extern private static void InternalBindStreamTransform([NotNull] Animator animator, [NotNull] Transform transform, out TransformStreamHandle transformStreamHandle);

        extern private static void InternalBindStreamProperty([NotNull] Animator animator, [NotNull] Transform transform, [NotNull] Type type, [NotNull] string property, bool isObjectReference, out PropertyStreamHandle propertyStreamHandle);

        extern private static void InternalBindSceneTransform([NotNull] Animator animator, [NotNull] Transform transform, out TransformSceneHandle transformSceneHandle);

        extern private static void InternalBindSceneProperty([NotNull] Animator animator, [NotNull] Transform transform, [NotNull] Type type, [NotNull] string property, bool isObjectReference, out PropertySceneHandle propertySceneHandle);

        extern private static bool InternalOpenAnimationStream([NotNull] Animator animator, ref AnimationStream stream);

        extern private static void InternalCloseAnimationStream([NotNull] Animator animator, ref AnimationStream stream);

        extern private static void InternalResolveAllStreamHandles([NotNull] Animator animator);

        extern private static void InternalResolveAllSceneHandles([NotNull] Animator animator);
    }
}
