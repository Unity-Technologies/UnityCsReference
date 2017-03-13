// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

namespace UnityEditorInternal
{
    internal class AnimationWindowHierarchyNode : TreeViewItem
    {
        public string path;
        public System.Type animatableObjectType;
        public string propertyName;

        public EditorCurveBinding? binding;
        public AnimationWindowCurve[] curves;

        public float? topPixel = null;
        public int indent = 0;

        public AnimationWindowHierarchyNode(int instanceID, int depth, TreeViewItem parent, System.Type animatableObjectType, string propertyName, string path, string displayName)
            : base(instanceID, depth, parent, displayName)
        {
            this.displayName = displayName;
            this.animatableObjectType = animatableObjectType;
            this.propertyName = propertyName;
            this.path = path;
        }
    }

    internal class AnimationWindowHierarchyPropertyGroupNode : AnimationWindowHierarchyNode
    {
        public AnimationWindowHierarchyPropertyGroupNode(System.Type animatableObjectType, int setId, string propertyName, string path, TreeViewItem parent)
            : base(AnimationWindowUtility.GetPropertyNodeID(setId, path, animatableObjectType, propertyName), parent != null ? parent.depth + 1 : -1, parent, animatableObjectType, AnimationWindowUtility.GetPropertyGroupName(propertyName), path, AnimationWindowUtility.GetNicePropertyGroupDisplayName(animatableObjectType, propertyName))
        {}
    }

    internal class AnimationWindowHierarchyPropertyNode : AnimationWindowHierarchyNode
    {
        public bool isPptrNode;

        public AnimationWindowHierarchyPropertyNode(System.Type animatableObjectType, int setId, string propertyName, string path, TreeViewItem parent, EditorCurveBinding binding, bool isPptrNode)
            : base(AnimationWindowUtility.GetPropertyNodeID(setId, path, animatableObjectType, propertyName), parent != null ? parent.depth + 1 : -1, parent, animatableObjectType, propertyName, path, AnimationWindowUtility.GetNicePropertyDisplayName(animatableObjectType, propertyName))
        {
            this.binding = binding;
            this.isPptrNode = isPptrNode;
        }
    }

    internal class AnimationWindowHierarchyClipNode : AnimationWindowHierarchyNode
    {
        public AnimationWindowHierarchyClipNode(TreeViewItem parent, int setId, string name)
            : base(setId, parent != null ? parent.depth + 1 : -1, parent, null, null, null, name)
        {}
    }

    internal class AnimationWindowHierarchyMasterNode : AnimationWindowHierarchyNode
    {
        public AnimationWindowHierarchyMasterNode()
            : base(0, -1, null, null, null, null, "")
        {}
    }

    // A special node to put "Add Curve" button in bottom of the tree
    internal class AnimationWindowHierarchyAddButtonNode : AnimationWindowHierarchyNode
    {
        public AnimationWindowHierarchyAddButtonNode()
            : base(0, -1, null, null, null, null, "")
        {}
    }
}
