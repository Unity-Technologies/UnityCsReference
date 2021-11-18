// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using Unity.Collections;

namespace UnityEditor
{
    public partial class PhysicsDebugWindow : EditorWindow
    {
        private Camera m_Camera;

        private void DrawContactsTab()
        {
            var useVariedColorsPrevValue = PhysicsVisualizationSettings.useVariedContactColors;

            PhysicsVisualizationSettings.showContacts = EditorGUILayout.Toggle(Style.showContacts
                , PhysicsVisualizationSettings.showContacts);

            EditorGUI.BeginDisabledGroup(!PhysicsVisualizationSettings.showContacts);
            EditorGUI.indentLevel++;

            PhysicsVisualizationSettings.showAllContacts = EditorGUILayout.Toggle(Style.showAllContacts
                , PhysicsVisualizationSettings.showAllContacts);

            PhysicsVisualizationSettings.showContactImpulse = EditorGUILayout.Toggle(Style.showImpulse
                , PhysicsVisualizationSettings.showContactImpulse);

            PhysicsVisualizationSettings.showContactSeparation = EditorGUILayout.Toggle(Style.showSeparation
                , PhysicsVisualizationSettings.showContactSeparation);

            PhysicsVisualizationSettings.useContactFiltering = EditorGUILayout.Toggle(Style.useContactFiltering
                , PhysicsVisualizationSettings.useContactFiltering);

            EditorGUI.indentLevel--;
            EditorGUI.EndDisabledGroup();

            if (m_ShowAllContactsWhenEnteredPlayMode != PhysicsVisualizationSettings.showAllContacts || m_ShowContactsWhenEnteredPlayMode != PhysicsVisualizationSettings.showContacts)
                EditorGUILayout.HelpBox("This change will only fully take effect after exiting Play Mode", MessageType.Warning);

            EditorGUILayout.LabelField(Style.contactColors);
            EditorGUI.indentLevel++;
            EditorGUIUtility.labelWidth += 20f;

            PhysicsVisualizationSettings.useVariedContactColors = EditorGUILayout.Toggle(Style.useVariedColors
                , PhysicsVisualizationSettings.useVariedContactColors);

            EditorGUI.BeginDisabledGroup(PhysicsVisualizationSettings.useVariedContactColors);

            PhysicsVisualizationSettings.contactColor =
                EditorGUILayout.ColorField(Style.contactColor, PhysicsVisualizationSettings.contactColor);

            PhysicsVisualizationSettings.contactSeparationColor =
                EditorGUILayout.ColorField(Style.contactSeparationColor, PhysicsVisualizationSettings.contactSeparationColor);

            PhysicsVisualizationSettings.contactImpulseColor =
                EditorGUILayout.ColorField(Style.contactImpulseColor, PhysicsVisualizationSettings.contactImpulseColor);

            EditorGUI.EndDisabledGroup();

            EditorGUIUtility.labelWidth = 0f;
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10f);

            if (useVariedColorsPrevValue != PhysicsVisualizationSettings.useVariedContactColors)
                RepaintSceneAndGameViews();
        }

        private void PlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
                ClearAllPoolsAndStoredQueries();
        }

        private void ClearAllPoolsAndStoredQueries()
        {
            PhysicsDebugDraw.ClearAllPools();
            ClearQueryShapes();
        }

        #region Contact retrieval and filtering



        #endregion

        #region Gizmos

        private void DrawContacts()
        {
            if (PhysicsVisualizationSettings.showContacts)
                PhysicsDebugDraw.GetPooledContacts();
        }

        private void DrawContacts_Internal(NativeArray<PhysicsDebugDraw.VisContactPoint> array)
        {
            var useRandomColor = PhysicsVisualizationSettings.useVariedContactColors;
            var impulseColor = useRandomColor ? Color.black : PhysicsVisualizationSettings.contactImpulseColor;

            for (int i = 0; i < array.Length; i++)
            {
                bool passedFiltering = (!PhysicsVisualizationSettings.useContactFiltering || PhysicsDebugDraw.IsContactVisualised(array[i].otherCollider));
                if (passedFiltering && IsContactSeenByCamera(array[i].point))
                    DrawSingleCollision(array[i], impulseColor, useRandomColor);
            }
        }

        private void DrawSingleCollision(PhysicsDebugDraw.VisContactPoint contactPoint, Color impulseColor, bool useRandomColor)
        {
            var primaryColor = useRandomColor ? GetHashedColor(contactPoint.thisColliderInstanceID) : PhysicsVisualizationSettings.contactColor;
            var inverseColor = useRandomColor ? GetInverseColor(primaryColor) : PhysicsVisualizationSettings.contactSeparationColor;

            var colliderScale1 = GetColliderScale(contactPoint.thisCollider);
            var colliderScale2 = GetColliderScale(contactPoint.otherCollider);
            var colliderScale = Mathf.Min(colliderScale1, colliderScale2);

            Handles.color = primaryColor;
            Handles.ArrowHandleCap(0, contactPoint.point, Quaternion.LookRotation(contactPoint.normal), colliderScale, EventType.Repaint);

            if (PhysicsVisualizationSettings.showContactSeparation && contactPoint.separation > 0.01f)
            {
                Vector3 p2 = contactPoint.point - (contactPoint.normal * contactPoint.separation);

                Handles.color = inverseColor;
                // The line that displays the separation
                Handles.DrawLine(p2, contactPoint.point, 2f);
                Handles.Disc(Quaternion.identity, contactPoint.point
                    , contactPoint.normal, contactPoint.separation / 2f, false, 1f);

                // Looks really good but maybe computationally too expensive?
                var discFillingColor = inverseColor;
                discFillingColor.a = 0.2f;

                Handles.color = discFillingColor;
                Handles.DrawSolidDisc(contactPoint.point, contactPoint.normal, contactPoint.separation / 2f);
            }

            // Impulse arrow
            if (PhysicsVisualizationSettings.showContactImpulse && contactPoint.impulse.sqrMagnitude > 0.001f)
            {
                Handles.color = impulseColor;
                Handles.ArrowHandleCap(0, contactPoint.point, Quaternion.LookRotation(contactPoint.impulse), contactPoint.impulse.magnitude, EventType.Repaint);
            }
        }

        private bool IsContactSeenByCamera(Vector3 position)
        {
            if (!m_Camera)
                return false;

            Vector3 screenPoint = m_Camera.WorldToViewportPoint(position);

            return screenPoint.z > 0f && screenPoint.x > 0f && screenPoint.x < 1f && screenPoint.y > 0f && screenPoint.y < 1f;
        }

        private static Color GetHashedColor(int colliderHash)
        {
            colliderHash ^= colliderHash >> 6;
            colliderHash ^= colliderHash << 13;
            colliderHash ^= colliderHash >> 14;
            colliderHash *= 736338717;

            return new Color32(
                (byte)colliderHash,
                (byte)(colliderHash >> 8),
                (byte)(colliderHash >> 16),
                255
            );
        }

        private static Color GetInverseColor(Color color)
        {
            return new Color(1f - color.r, 1f - color.g, 1f - color.b, 1f);
        }

        private static float GetColliderScale(Collider collider)
        {
            if (!collider)
                return 1f;

            var bounds = collider.bounds;
            return Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        }

        #endregion
    }
}
