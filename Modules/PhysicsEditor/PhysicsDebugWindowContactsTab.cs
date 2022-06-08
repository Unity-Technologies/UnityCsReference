// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine.Profiling;
using System;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEditor
{
    public partial class PhysicsDebugWindow : EditorWindow
    {
        private Camera m_Camera;
        private readonly Dictionary<PhysicsScene, SceneContacts> m_ContactsToDraw = new Dictionary<PhysicsScene, SceneContacts>();

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

            foreach (var (scene, sceneContacts) in m_ContactsToDraw)
                sceneContacts.CompleteAndDispose();

            m_ContactsToDraw.Clear();
        }

        #region Contact retrieval and filtering

        private struct VisContactPoint
        {
            public Vector3 point;
            public Vector3 normal;
            public float separation;
            public float impulse;
            public int thisColliderId;
            public int otherColliderId;

            public Collider thisCollider => thisColliderId == 0 ? null : UnityEngine.Object.FindObjectFromInstanceID(thisColliderId) as Collider;
            public Collider otherCollider => otherColliderId == 0 ? null : UnityEngine.Object.FindObjectFromInstanceID(otherColliderId) as Collider;

            public VisContactPoint(Vector3 point, Vector3 normal, float separation, float impulse, int col0, int col1)
            {
                this.point = point;
                this.normal = normal;
                this.separation = separation;
                this.impulse = impulse;
                this.thisColliderId = col0;
                this.otherColliderId = col1;
            }
        }

        private struct ReadContactsJob : IJob
        {
            public ContactArrayWrapper contactBuffer;

            [ReadOnly]
            public NativeArray<ContactPairHeader>.ReadOnly pairHeaders;

            public ReadContactsJob(ContactArrayWrapper contactBuffer, NativeArray<ContactPairHeader>.ReadOnly pairHeaders)
            {
                this.contactBuffer = contactBuffer;
                this.pairHeaders = pairHeaders;
            }

            public void Execute()
            {
                for (int i = 0; i < pairHeaders.Length; i++)
                {
                    var n = pairHeaders[i].PairCount;

                    for (int j = 0; j < n; j++)
                    {
                        ref readonly var pair = ref pairHeaders[i].GetContactPair(j);

                        if (pair.IsCollisionExit)
                            continue;

                        var shape0 = pair.ColliderInstanceID;
                        var shape1 = pair.OtherColliderInstanceID;

                        for(int k = 0; k < pair.ContactCount; k++)
                        {
                            ref readonly var contact = ref pair.GetContactPoint(k);

                            contactBuffer.PushBackNoResize(new VisContactPoint(
                                contact.m_Position,
                                contact.m_Normal,
                                contact.m_Separation,
                                contact.m_Impulse.magnitude,
                                shape0,
                                shape1
                            ));
                        }
                    }
                }
            }
        }

        // This whole mess is here because we have no access to DynamicArrays in the Core module
        // and the NativeArray safety doesn't allow array resizing when running a single thread job
        private struct ContactArrayWrapper
        {
            private NativeArray<VisContactPoint> m_Buffer;
            private int m_Count;

            public int Capacity => m_Buffer.Length;
            public int Count { get { return m_Count; } set { m_Count = value; } }

            public VisContactPoint this[int index]
            {
                get { return m_Buffer[index]; }
                set { m_Buffer[index] = value; }
            }

            public ContactArrayWrapper(int size)
            {
                m_Buffer = new NativeArray<VisContactPoint>(size, Allocator.Persistent);
                m_Count = 0;
            }

            public void Reserve(int size)
            {
                if (size <= Capacity)
                    return;

                var newArray = new NativeArray<VisContactPoint>(size, Allocator.Persistent);
                NativeArray<VisContactPoint>.Copy(m_Buffer, newArray, m_Count);

                m_Buffer.Dispose();
                m_Buffer = newArray;
            }

            public void PushBackNoResize(VisContactPoint value)
            {
                if (m_Count >= Capacity)
                    return;

                m_Buffer[m_Count++] = value;
            }

            public void Clear()
            {
                m_Count = 0;
            }

            public void Dispose()
            {
                m_Count = 0;
                m_Buffer.Dispose();
            }
        }

        private struct SceneContacts
        {
            private ContactArrayWrapper m_ContactArray;
            private JobHandle m_JobHandle;

            public ContactArrayWrapper ContactArray { get { return m_ContactArray; } set { m_ContactArray = value; } }
            public JobHandle Handle { get { return m_JobHandle; } set { m_JobHandle = value; } }

            public SceneContacts(int size)
            {
                m_ContactArray = new ContactArrayWrapper(size);
                m_JobHandle = new JobHandle();
            }

            public void CompleteJob()
            {
                m_JobHandle.Complete();
            }

            public void CompleteAndDispose()
            {
                m_JobHandle.Complete();
                m_ContactArray.Dispose();
            }
        }

        private void ReadContacts_Internal(PhysicsScene scene, NativeArray<ContactPairHeader>.ReadOnly pairHeaders)
        {
            Profiler.BeginSample("PhysicsDebugWindow.ReadContacts");

            try
            {
                var sceneContacts = m_ContactsToDraw.ContainsKey(scene) ? m_ContactsToDraw[scene] : new SceneContacts(32);

                int nbContacts = 0;

                for (int i = 0; i < pairHeaders.Length; i++)
                {
                    var header = pairHeaders[i];
                    for (int j = 0; j < header.PairCount; j++)
                    {
                        ref readonly var pair = ref header.GetContactPair(j);

                        if (!pair.IsCollisionExit)
                            nbContacts += pair.ContactCount;
                    }
                }

                var array = sceneContacts.ContactArray;

                array.Reserve(nbContacts);

                var job = new ReadContactsJob(array, pairHeaders);

                array.Count = nbContacts;
                sceneContacts.ContactArray = array;
                sceneContacts.Handle = job.Schedule();

                m_ContactsToDraw[scene] = sceneContacts;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            Profiler.EndSample();
        }

        private void OnPhysicsSceneDestoryed(PhysicsScene sceneHandle)
        {
            if (!m_ContactsToDraw.ContainsKey(sceneHandle))
                return;

            m_ContactsToDraw[sceneHandle].CompleteAndDispose();
            m_ContactsToDraw.Remove(sceneHandle);
        }

        #endregion

        #region Gizmos

        private void DrawContacts()
        {
            if (!PhysicsVisualizationSettings.showContacts)
                return;

            var useRandomColor = PhysicsVisualizationSettings.useVariedContactColors;
            var impulseColor = useRandomColor ? Color.black : PhysicsVisualizationSettings.contactImpulseColor;

            foreach(var (scene, sceneContacts) in m_ContactsToDraw)
            {
                sceneContacts.CompleteJob();

                var array = sceneContacts.ContactArray;
                for(int j = 0; j < array.Count; j++)
                {
                    bool passedFiltering = (!PhysicsVisualizationSettings.useContactFiltering || PhysicsDebugDraw.IsColliderVisualised(array[j].otherCollider));
                    if (passedFiltering && IsContactSeenByCamera(array[j].point))
                        DrawSingleCollision(array[j], impulseColor, useRandomColor);
                }
            }
        }

        private void DrawSingleCollision(VisContactPoint contactPoint, Color impulseColor, bool useRandomColor)
        {
            var primaryColor = useRandomColor ? GetHashedColor(contactPoint.thisColliderId) : PhysicsVisualizationSettings.contactColor;
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

                var discFillingColor = inverseColor;
                discFillingColor.a = 0.2f;

                Handles.color = discFillingColor;
                Handles.DrawSolidDisc(contactPoint.point, contactPoint.normal, contactPoint.separation / 2f);
            }

            // Impulse arrow
            if (PhysicsVisualizationSettings.showContactImpulse && contactPoint.impulse > 0.032f)
            {
                Handles.color = impulseColor;
                Handles.ArrowHandleCap(0, contactPoint.point, Quaternion.LookRotation(contactPoint.normal), contactPoint.impulse, EventType.Repaint);
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
