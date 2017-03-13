// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

namespace UnityEditor.Animations
{
    public partial class BlendTree : Motion
    {
        public void AddChild(Motion motion)
        {
            AddChild(motion, Vector2.zero, 0);
        }

        public void AddChild(Motion motion, Vector2 position)
        {
            AddChild(motion, position, 0);
        }

        public void AddChild(Motion motion, float threshold)
        {
            AddChild(motion, Vector2.zero, threshold);
        }

        public void RemoveChild(int index)
        {
            Undo.RecordObject(this, "Remove Child");
            ChildMotion[] childMotions = children;
            ArrayUtility.RemoveAt(ref childMotions, index);
            children = childMotions;
        }

        internal void AddChild(Motion motion, Vector2 position, float threshold)
        {
            Undo.RecordObject(this, "Added BlendTree Child");
            ChildMotion[] childMotions = children;
            ChildMotion newMotion = new ChildMotion();
            newMotion.timeScale = 1;
            newMotion.motion = motion;
            newMotion.position = position;
            newMotion.threshold = threshold;
            newMotion.directBlendParameter = "Blend";
            ArrayUtility.Add(ref childMotions, newMotion);
            children = childMotions;
        }

        public BlendTree CreateBlendTreeChild(float threshold)
        {
            return CreateBlendTreeChild(Vector2.zero, threshold);
        }

        public BlendTree CreateBlendTreeChild(Vector2 position)
        {
            return CreateBlendTreeChild(position, 0);
        }

        internal bool HasChild(BlendTree childTree, bool recursive)
        {
            foreach (ChildMotion child in children)
            {
                if (child.motion == childTree)
                {
                    return true;
                }

                if (recursive && child.motion is BlendTree && (child.motion as BlendTree).HasChild(childTree, true))
                {
                    return true;
                }
            }

            return false;
        }

        internal BlendTree CreateBlendTreeChild(Vector2 position, float threshold)
        {
            Undo.RecordObject(this, "Created BlendTree Child");

            BlendTree tree = new BlendTree();
            tree.name = "BlendTree";
            tree.hideFlags = HideFlags.HideInHierarchy;
            if (AssetDatabase.GetAssetPath(this) != "")
                AssetDatabase.AddObjectToAsset(tree, AssetDatabase.GetAssetPath(this));

            AddChild(tree, position, threshold);
            return tree;
        }
    }
}
