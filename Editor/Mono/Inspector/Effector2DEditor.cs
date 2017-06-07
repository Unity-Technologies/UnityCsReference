// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace UnityEditor
{
    /// <summary>
    /// Prompts the end-user to add 2D colliders if non exist for 2D effector to work with.
    /// </summary>
    [CustomEditor(typeof(Effector2D), true)]
    [CanEditMultipleObjects]
    internal class Effector2DEditor : Editor
    {
        SerializedProperty m_UseColliderMask;
        SerializedProperty m_ColliderMask;
        readonly AnimBool m_ShowColliderMask = new AnimBool();

        public virtual void OnEnable()
        {
            m_UseColliderMask = serializedObject.FindProperty("m_UseColliderMask");
            m_ColliderMask = serializedObject.FindProperty("m_ColliderMask");

            m_ShowColliderMask.value = (target as Effector2D).useColliderMask;
            m_ShowColliderMask.valueChanged.AddListener(Repaint);
        }

        public virtual void OnDisable()
        {
            m_ShowColliderMask.valueChanged.RemoveListener(Repaint);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Fetch the effector.
            var effector = target as Effector2D;

            // Update collider-mask fade-group.
            m_ShowColliderMask.target = effector.useColliderMask;

            EditorGUILayout.PropertyField(m_UseColliderMask);
            if (EditorGUILayout.BeginFadeGroup(m_ShowColliderMask.faded))
                EditorGUILayout.PropertyField(m_ColliderMask);
            EditorGUILayout.EndFadeGroup();

            serializedObject.ApplyModifiedProperties();

            // Finish if any enabled 2D colliders used by the effector exist.
            if (effector.GetComponents<Collider2D>().Any(collider => collider.enabled && collider.usedByEffector))
                return;

            // Show appropriate feedback.
            if (effector.requiresCollider)
                EditorGUILayout.HelpBox("This effector will not function until there is at least one enabled 2D collider with 'Used by Effector' checked on this GameObject.", MessageType.Warning);
            else
                EditorGUILayout.HelpBox("This effector can optionally work without a 2D collider.", MessageType.Info);
        }

        /// <summary>
        /// Checks collider types for compatibility warnings.
        /// </summary>
        /// <param name="collider"></param>
        public static void CheckEffectorWarnings(Collider2D collider)
        {
            // Finish if the collider is not used by the effector.
            if (!collider.usedByEffector || collider.usedByComposite)
                return;

            // Fetch the effector.
            var effector = collider.GetComponent<Effector2D>();

            // Warning if there's no effector or it's not enabled.
            if (effector == null || !effector.enabled)
            {
                // Show warning.
                EditorGUILayout.HelpBox("This collider will not function with an effector until there is at least one enabled 2D effector on this GameObject.", MessageType.Warning);

                // Finish if there was no effector.
                if (effector == null)
                    return;
            }

            // Handle collision/trigger effector preferences.
            if (effector.designedForNonTrigger && collider.isTrigger)
            {
                // Show warning.
                EditorGUILayout.HelpBox("This collider has 'Is Trigger' checked but this should be unchecked when used with the '" + effector.GetType().Name + "' component which is designed to work with collisions.", MessageType.Warning);
            }
            else if (effector.designedForTrigger && !collider.isTrigger)
            {
                // Show warning.
                EditorGUILayout.HelpBox("This collider has 'Is Trigger' unchecked but this should be checked when used with the '" + effector.GetType().Name + "' component which is designed to work with triggers.", MessageType.Warning);
            }
        }
    }
}
