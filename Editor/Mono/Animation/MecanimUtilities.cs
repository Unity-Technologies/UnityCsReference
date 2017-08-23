// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;


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
    }
}
