// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    // UIAnimationClip's only serialized field is its inner AnimationClip (m_AnimationClip). That clip is
    // an implementation detail authored through the Animation Window, not a reference users should
    // reassign or clear from the Inspector (and the sub-asset itself is hidden via HideInHierarchy).
    // Hide the field so the asset shows an empty body instead of an editable reference to a hidden clip.
    [CustomEditor(typeof(UIAnimationClip))]
    internal class UIAnimationClipEditor : Editor
    {
        internal const string k_HiddenPropertyPath = "m_AnimationClip";

        public override VisualElement CreateInspectorGUI()
        {
            // Default inspector minus the inner-clip field. Today that leaves an empty body, but it
            // stays correct if fields are ever added. FillDefaultInspector binds the fields itself.
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this, k_HiddenPropertyPath);
            return root;
        }
    }
}
