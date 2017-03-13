// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(Animation))]
    [CanEditMultipleObjects]
    internal class AnimationEditor : Editor
    {
        private int m_PrePreviewAnimationArraySize = -1;

        public void OnEnable()
        {
            m_PrePreviewAnimationArraySize = -1;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty clipProperty = serializedObject.FindProperty("m_Animation");

            EditorGUILayout.PropertyField(clipProperty, true);
            int newAnimID = clipProperty.objectReferenceInstanceIDValue;

            SerializedProperty arrProperty = serializedObject.FindProperty("m_Animations");
            int arrSize = arrProperty.arraySize;

            // Remember the array size when ObjectSelector becomes visible
            if (ObjectSelector.isVisible && m_PrePreviewAnimationArraySize == -1)
                m_PrePreviewAnimationArraySize = arrSize;

            // Make sure the array is the original array size + 1 at max (+1 for the ObjectSelector preview slot)
            if (m_PrePreviewAnimationArraySize != -1)
            {
                // Always resize if the last anim element is not the current animation
                int lastAnimID = arrSize > 0 ? arrProperty.GetArrayElementAtIndex(arrSize - 1).objectReferenceInstanceIDValue : -1;
                if (lastAnimID != newAnimID)
                    arrProperty.arraySize = m_PrePreviewAnimationArraySize;
                if (!ObjectSelector.isVisible)
                    m_PrePreviewAnimationArraySize = -1;
            }

            DrawPropertiesExcluding(serializedObject, "m_Animation", "m_UserAABB");

            serializedObject.ApplyModifiedProperties();
        }

        // A minimal list of settings to be shown in the Asset Store preview inspector
        internal override void OnAssetStoreInspectorGUI()
        {
            OnInspectorGUI();
        }
    }
}
