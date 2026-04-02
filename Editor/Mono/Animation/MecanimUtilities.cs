// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace UnityEditor.Animations
{
    internal class MecanimUtilities
    {
        public static bool StateMachineRelativePath(AnimatorStateMachine parent, AnimatorStateMachine toFind,
            ref List<AnimatorStateMachine> hierarchy)
        {
            hierarchy.Add(parent);
            if (parent == toFind)
                return true;
            var childStateMachines = AnimatorStateMachine.StateMachineCache.GetChildStateMachines(parent);
            for (int i = 0; i < childStateMachines.Length; i++)
            {
                if (StateMachineRelativePath(childStateMachines[i].stateMachine, toFind, ref hierarchy))
                    return true;
            }
            hierarchy.Remove(parent);
            return false;
        }

        internal static bool AreSameAsset(Object obj1, Object obj2)
        {
            return AssetDatabase.GetAssetPath(obj1) == AssetDatabase.GetAssetPath(obj2);
        }

        internal static void DestroyBlendTreeRecursive(BlendTree blendTree)
        {
            for (int i = 0; i < blendTree.children.Length; i++)
            {
                BlendTree childBlendTree = blendTree.children[i].motion as BlendTree;
                if (childBlendTree != null && AreSameAsset(blendTree, childBlendTree))
                    DestroyBlendTreeRecursive(childBlendTree);
            }

            Undo.DestroyObjectImmediate(blendTree);
        }

        // What is the first animation player component (Animator or Animation) when recursing parent tree toward root
        public static Component GetClosestAnimationPlayerComponentInParents(Transform tr)
        {
            while (true)
            {
                if (tr.TryGetComponent(out Animator animator))
                {
                    return animator;
                }

                if (tr.TryGetComponent(out Animation animation))
                {
                    return animation;
                }

                if (tr.TryGetComponent(out IAnimationClipSource clipPlayer))
                {
                    if (clipPlayer is Component clipPlayerComponent)
                    {
                        return clipPlayerComponent;
                    }
                }

                if (tr == tr.root)
                    break;

                tr = tr.parent;
            }
            return null;
        }

        // Add animator, controller and clip to gameobject if they are missing to make this gameobject animatable
        public static bool InitializeGameObjectForAnimation(GameObject animatedObject)
        {
            Component animationPlayer = GetClosestAnimationPlayerComponentInParents(animatedObject.transform);
            if (animationPlayer == null)
            {
                var newClip = CreateNewClip(animatedObject.name);

                if (newClip == null)
                    return false;

                animationPlayer = EnsureActiveAnimationPlayer(animatedObject);
                Undo.RecordObject(animationPlayer, "Add animation clip");
                bool success = AddClipToAnimationPlayerComponent(animationPlayer, newClip);

                if (!success)
                    Object.DestroyImmediate(animationPlayer);

                return success;
            }

            return EnsureAnimationPlayerHasClip(animationPlayer);
        }

        // Ensures that the gameobject or it's parents have an animation player component. If not try to create one.
        public static Component EnsureActiveAnimationPlayer(GameObject animatedObject)
        {
            Component closestAnimator = GetClosestAnimationPlayerComponentInParents(animatedObject.transform);
            if (closestAnimator == null)
            {
                return Undo.AddComponent<Animator>(animatedObject);
            }
            return closestAnimator;
        }

        // Ensures that animator has at least one clip and controller to go with it
        private static bool EnsureAnimationPlayerHasClip(Component animationPlayer)
        {
            if (animationPlayer == null)
                return false;

            if (AnimationUtility.GetAnimationClips(animationPlayer.gameObject).Length > 0)
                return true;

            // At this point we know that we can create a clip
            var newClip = CreateNewClip(animationPlayer.gameObject.name);

            if (newClip == null)
                return false;

            // End animation mode before adding or changing animation component to object
            AnimationMode.StopAnimationMode();

            // By default add it the animation to the Animator component.
            return AddClipToAnimationPlayerComponent(animationPlayer, newClip);
        }

        public static bool AddClipToAnimationPlayerComponent(Component animationPlayer, AnimationClip newClip)
        {
            if (animationPlayer is Animator)
                return AddClipToAnimatorComponent(animationPlayer as Animator, newClip);
            else if (animationPlayer is Animation)
                return AddClipToAnimationComponent(animationPlayer as Animation, newClip);
            return false;
        }

        public static bool AddClipToAnimatorComponent(Animator animator, AnimationClip newClip)
        {
            UnityEditor.Animations.AnimatorController controller = UnityEditor.Animations.AnimatorController.GetEffectiveAnimatorController(animator);
            if (controller == null)
            {
                controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerForClip(newClip, animator.gameObject);

                Undo.RecordObject(animator, "Set Controller");
                UnityEditor.Animations.AnimatorController.SetAnimatorController(animator, controller);

                if (controller != null)
                    return true;
            }
            else
            {
                // Do we already have a state with the clips name?
                ChildAnimatorState childAnimatorState = controller.layers[0].stateMachine.FindState(newClip.name);

                if (childAnimatorState.Equals(default(ChildAnimatorState)))
                    controller.AddMotion(newClip);

                // Assign clip if state already present, but without a motion
                else if (childAnimatorState.state && childAnimatorState.state.motion == null)
                    childAnimatorState.state.motion = newClip;

                // State present, but with some other clip
                else if (childAnimatorState.state && childAnimatorState.state.motion != newClip)
                    controller.AddMotion(newClip);

                return true;
            }
            return false;
        }

        public static bool AddClipToAnimationComponent(Animation animation, AnimationClip newClip)
        {
            SetClipAsLegacy(newClip);
            animation.AddClip(newClip, newClip.name);
            return true;
        }

        internal static string s_LastPathUsedForNewClip;
        internal static AnimationClip CreateNewClip(string gameObjectName)
        {
            // Go forward with presenting user a save clip dialog
            string message = string.Format(L10n.Tr("Create a new animation for the game object '{0}':"), gameObjectName);
            string newClipDirectory = ProjectWindowUtil.GetActiveFolderPath();
            if (s_LastPathUsedForNewClip != null)
            {
                string directoryPath = Path.GetDirectoryName(s_LastPathUsedForNewClip);
                if (directoryPath != null && Directory.Exists(directoryPath))
                {
                    newClipDirectory = directoryPath;
                }
            }
            string newClipPath = EditorUtility.SaveFilePanelInProject(L10n.Tr("Create New Animation"), "New Animation", "anim", message, newClipDirectory);

            // If user canceled or save path is invalid, we can't create a clip
            if (newClipPath == "")
                return null;

            return CreateNewClipAtPath(newClipPath);
        }

        // Create a new animation clip asset for gameObject at a certain asset path.
        // The clipPath parameter must be a full asset path ending with '.anim'. e.g. "Assets/Animations/New Clip.anim"
        // This function will overwrite existing .anim files.
        internal static AnimationClip CreateNewClipAtPath(string clipPath)
        {
            s_LastPathUsedForNewClip = clipPath;

            var newClip = new AnimationClip();

            var info = AnimationUtility.GetAnimationClipSettings(newClip);
            info.loopTime = true;
            AnimationUtility.SetAnimationClipSettingsNoDirty(newClip, info);

            AnimationClip asset = AssetDatabase.LoadMainAssetAtPath(clipPath) as AnimationClip;

            if (asset)
            {
                newClip.name = asset.name;
                EditorUtility.CopySerialized(newClip, asset);
                AssetDatabase.SaveAssets();
                Object.DestroyImmediate(newClip);
                return asset;
            }

            AssetDatabase.CreateAsset(newClip, clipPath);
            return newClip;
        }

        private static void SetClipAsLegacy(AnimationClip clip)
        {
            SerializedObject s = new SerializedObject(clip);
            s.FindProperty("m_Legacy").boolValue = true;
            s.ApplyModifiedProperties();
        }

        // What is the first animator component when recursing parent tree toward root
        public static Animator GetClosestAnimatorInParents(Transform tr)
        {
            while (true)
            {
                if (tr.TryGetComponent(out Animator animator))
                {
                    return animator;
                }
                if (tr == tr.root) break;
                tr = tr.parent;
            }
            return null;
        }

        internal static AnimationClip AllocateAndSetupClip(bool useAnimator)
        {
            // At this point we know that we can create a clip
            AnimationClip newClip = new AnimationClip();
            if (useAnimator)
            {
                AnimationClipSettings info = AnimationUtility.GetAnimationClipSettings(newClip);
                info.loopTime = true;
                AnimationUtility.SetAnimationClipSettingsNoDirty(newClip, info);
            }
            return newClip;
        }
    }
}
