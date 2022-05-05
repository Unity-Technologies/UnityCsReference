// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using System;
using UnityEditor.SceneManagement;

namespace UnityEditor
{
    public partial class PhysicsDebugWindow : EditorWindow
    {
        [System.Serializable]
        private class RenderedTransform : IEquatable<RenderedTransform>
        {
            public Transform Transform { get; private set; }
            public VisualisationState State { get; set; }
            public bool HasBody { get; private set; }

            [SerializeField] private bool m_ShowFolduot = false;

            private Rigidbody m_Rigidbody;
            private ArticulationBody m_Articulation;

            public RenderedTransform(Transform transform, VisualisationState visualizationState)
            {
                Transform = transform;
                State = visualizationState;

                Init();
            }

            public RenderedTransform(Transform transform)
            {
                Transform = transform;
                State = VisualisationState.None;

                Init();
            }

            private void Init()
            {
                if (Transform == null)
                {
                    m_Rigidbody = null;
                    m_Articulation = null;
                    return;
                }

                m_Rigidbody = Transform.GetComponent<Rigidbody>();

                if (m_Rigidbody == null)
                    m_Articulation = Transform.GetComponent<ArticulationBody>();

                HasBody = !(m_Rigidbody == null && m_Articulation == null);
            }

            public static implicit operator RenderedTransform(Transform transform) => new RenderedTransform(transform);

            public bool Equals(RenderedTransform other)
            {
                return Transform == other.Transform;
            }

            public bool IsValid()
            {
                return m_Rigidbody || m_Articulation;
            }

            public void DrawInfoTable()
            {
                if (!HasBody)
                    return;

                if (m_Rigidbody)
                    DrawRigidbodyInfo(m_Rigidbody);
                else if (m_Articulation)
                    DrawArticulationBodyInfo(m_Articulation);
            }

            public void Visualize()
            {
                if (!HasBody)
                    return;

                var showCenterOfMass = (State & VisualisationState.CenterOfMass) == VisualisationState.CenterOfMass;
                var showInertiaTensor = (State & VisualisationState.InertiaTensor) == VisualisationState.InertiaTensor;

                if (m_Rigidbody)
                    VisualizeRigidbody(m_Rigidbody, showCenterOfMass, showInertiaTensor);
                else if (m_Articulation)
                    VisualizeArticulationBody(m_Articulation, showCenterOfMass, showInertiaTensor);
            }

            private void DrawRigidbodyInfo(Rigidbody body)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.FloatField(Style.infoSpeed                  , body.velocity.magnitude);
                EditorGUILayout.Vector3Field(Style.infoVel                  , body.velocity);
                EditorGUILayout.Vector3Field(Style.infoAngVel               , body.angularVelocity);
                EditorGUILayout.Vector3Field(Style.infoInertiaTensor        , body.inertiaTensor);
                EditorGUILayout.Vector3Field(Style.infoInertiaTensorRotation, body.inertiaTensorRotation.eulerAngles);
                EditorGUILayout.Vector3Field(Style.infoLocalCenterOfMass    , body.centerOfMass);
                EditorGUILayout.Vector3Field(Style.infoWorldCenterOfMass    , body.worldCenterOfMass);
                EditorGUILayout.LabelField(Style.infoSleepState             , body.IsSleeping() ? Style.sleep : Style.awake);
                EditorGUILayout.FloatField(Style.infoSleepThreshold         , body.sleepThreshold);
                EditorGUILayout.FloatField(Style.infoMaxLinVel              , body.maxLinearVelocity);
                EditorGUILayout.FloatField(Style.infoMaxAngVel              , body.maxAngularVelocity);
                EditorGUILayout.IntField(Style.infoSolverIterations         , body.solverIterations);
                EditorGUILayout.IntField(Style.infoSolverVelIterations      , body.solverVelocityIterations);
                EditorGUI.indentLevel--;
            }

            private void DrawArticulationBodyInfo(ArticulationBody body)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.FloatField(Style.infoSpeed                  , body.velocity.magnitude);
                EditorGUILayout.Vector3Field(Style.infoVel                  , body.velocity);
                EditorGUILayout.Vector3Field(Style.infoAngVel               , body.angularVelocity);
                EditorGUILayout.Vector3Field(Style.infoInertiaTensor        , body.inertiaTensor);
                EditorGUILayout.Vector3Field(Style.infoInertiaTensorRotation, body.inertiaTensorRotation.eulerAngles);
                EditorGUILayout.Vector3Field(Style.infoLocalCenterOfMass    , body.centerOfMass);
                EditorGUILayout.Vector3Field(Style.infoWorldCenterOfMass    , body.worldCenterOfMass);
                EditorGUILayout.LabelField(Style.infoSleepState             , body.IsSleeping() ? Style.sleep : Style.awake);
                EditorGUILayout.FloatField(Style.infoSleepThreshold         , body.sleepThreshold);
                EditorGUILayout.FloatField(Style.infoMaxLinVel              , body.maxLinearVelocity);
                EditorGUILayout.FloatField(Style.infoMaxAngVel              , body.maxAngularVelocity);
                EditorGUILayout.IntField(Style.infoSolverIterations         , body.solverIterations);
                EditorGUILayout.IntField(Style.infoSolverVelIterations      , body.solverVelocityIterations);
                EditorGUILayout.IntField(Style.infoBodyIndex, body.index);

                if (!body.isRoot && body.jointType != ArticulationJointType.FixedJoint)
                {
                    m_ShowFolduot = EditorGUILayout.Foldout(m_ShowFolduot, Style.infoJointInfo);
                    if (m_ShowFolduot)
                    {
                        DrawArticulationReducedSpaceField(body.jointPosition, Style.infoJointPosition);
                        DrawArticulationReducedSpaceField(body.jointVelocity, Style.infoJointVelocity);
                        DrawArticulationReducedSpaceField(body.jointForce, Style.infoJointForce);
                        DrawArticulationReducedSpaceField(body.jointAcceleration, Style.infoJointAcceleration);
                    }
                }

                EditorGUI.indentLevel--;
            }

            private void DrawArticulationReducedSpaceField(ArticulationReducedSpace ars, GUIContent label)
            {
                if (ars.dofCount == 0)
                    return;

                if (ars.dofCount == 1)
                    EditorGUILayout.FloatField(label, ars[0]);
                else if (ars.dofCount == 2)
                    EditorGUILayout.Vector2Field(label, new Vector2(ars[0], ars[1]));
                else if (ars.dofCount == 3)
                    EditorGUILayout.Vector3Field(label, new Vector3(ars[0], ars[1], ars[2]));
            }
        }

        private bool DrawInfoTabHeader()
        {
            m_Collumns.value = EditorGUILayout.IntSlider(Style.numOfItems, m_Collumns.value, 1, 10);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Tracked objects: " + m_TransformsToRender.Count);
            if (GUILayout.Button(Style.clearLocked, Style.maxWidth150))
                ClearAllLockedObjects();

            EditorGUILayout.EndHorizontal();

            if (m_TransformsToRender.Count == 0)
            {
                EditorGUILayout.HelpBox("Select a GameObject with a Rigidbody or an Articulation Body Component to display information about it", MessageType.Info);
                return false;
            }

            return true;
        }

        private void DrawSingleInfoItem(RenderedTransform tr)
        {
            if (tr == null)
                return;

            if (!tr.IsValid())
            {
                m_ObjectsToRemove.AddLast(tr.Transform);
                return;
            }

            DrawObjectHeader(tr);
            tr.DrawInfoTable();
        }

        private void DrawObjectHeader(RenderedTransform renderedTransform)
        {
            var locked = m_LockedObjects.ContainsKey(renderedTransform.Transform);
            var lockedPrev = locked;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("GameObject: " + renderedTransform.Transform.name);

            GUILayout.FlexibleSpace();

            var state = locked ? m_LockedObjects[renderedTransform.Transform] : VisualisationState.None;
            var statePrev = state;

            EditorGUIUtility.labelWidth = 50f;
            EditorGUILayout.LabelField(Style.drawGizmosFor, Style.notExpandWidth);
            EditorGUIUtility.labelWidth = 0f;
            state = (VisualisationState)EditorGUILayout.EnumPopup(state);

            if (locked)
            {
                m_LockedObjects[renderedTransform.Transform] = state;
                renderedTransform.State = state;
            }
            else if (state != statePrev)
            {
                locked = true;
                lockedPrev = false;
            }

            if (state != statePrev)
                RepaintSceneAndGameViews();

            locked = EditorGUILayout.ToggleLeft(Style.lockToggle, locked, Style.maxWidth50);

            EditorGUILayout.EndHorizontal();

            if (locked != lockedPrev)
            {
                if (locked)
                    m_ObjectsToAdd.AddLast(new RenderedTransform(renderedTransform.Transform, state));
                else m_ObjectsToRemove.AddLast(renderedTransform.Transform);

                RepaintSceneAndGameViews();
            }
        }

        private static bool HasBody(Transform transform)
        {
            var rb = transform.GetComponent<Rigidbody>();

            if (rb != null)
                return true;

            var ab = transform.GetComponent<ArticulationBody>();

            return ab != null;
        }

        #region Manage lists

        private RenderedTransform GetNextTransform(int index)
        {
            if (index < m_TransformsToRender.Count)
                return m_TransformsToRender[index];

            return null;
        }

        private void AddLockedObjects()
        {
            foreach (var renderedTransform in m_ObjectsToAdd)
            {
                m_LockedObjects.Add(renderedTransform.Transform, renderedTransform.State);
                var existingIndex = m_TransformsToRender.IndexOf(renderedTransform);

                m_TransformsToRender[existingIndex].State = renderedTransform.State;
            }

            if (m_ObjectsToAdd.Count > 0)
                m_ObjectsToAdd.Clear();
        }

        private void RemoveLockedObjects()
        {
            foreach (var tr in m_ObjectsToRemove)
            {
                m_LockedObjects.Remove(tr);
                if (!IsSelected(tr) || !HasBody(tr))
                    m_TransformsToRender.Remove(tr);
            }

            if (m_ObjectsToRemove.Count > 0)
            {
                UpdateSelection();
                m_ObjectsToRemove.Clear();
            }
        }

        private void ClearAllLockedObjects()
        {
            m_LockedObjects.Clear();

            UpdateSelection();
            RepaintSceneAndGameViews();
        }

        private void SaveDictionary()
        {
            m_DictionaryKeys.Clear();
            m_DictionaryValues.Clear();

            foreach (var lockedObject in m_LockedObjects)
            {
                m_DictionaryKeys.Add(lockedObject.Key);
                m_DictionaryValues.Add(lockedObject.Value);
            }
        }

        private void LoadDictionary()
        {
            m_LockedObjects.Clear();

            for (int i = 0; i < m_DictionaryKeys.Count; i++)
            {
                m_LockedObjects.Add(m_DictionaryKeys[i], m_DictionaryValues[i]);
            }

            m_DictionaryKeys.Clear();
            m_DictionaryValues.Clear();
        }

        private void OnSceneClose(Scene scene)
        {
            ClearInvalidObjects();
            PhysicsDebugDraw.UpdateFilter();
        }

        private void OnSceneOpen(Scene scene, OpenSceneMode mode)
        {
            PhysicsDebugDraw.UpdateFilter();
        }

        // This is usefull when Transfrom references change and they start pointing to null Transforms
        private void ClearInvalidObjects()
        {
            foreach (var lockedObject in m_LockedObjects)
                if (lockedObject.Key == null)
                    m_ObjectsToRemove.AddLast(lockedObject.Key);

            RemoveLockedObjects();

            for(int i = 0; i < m_TransformsToRender.Count; i++)
            {
                if(m_TransformsToRender[i].Transform == null)
                {
                    m_TransformsToRender.RemoveAt(i);
                    i--;
                }
            }
        }

        private void UpdateSelection()
        {
            var selection = Selection.GetTransforms(SelectionMode.Unfiltered);

            m_TemporarySelection.Clear();

            // Adds rendered transforms that are not part of the m_LockedObjects set
            foreach (var tr in m_TransformsToRender)
                if (!m_LockedObjects.ContainsKey(tr.Transform))
                    m_TemporarySelection.Add(tr.Transform);

            var addedTransforms = selection.Except(m_TemporarySelection);
            var removedTransforms = m_TemporarySelection.Except(selection);

            foreach (var tr in addedTransforms)
            {
                var newItem = new RenderedTransform(tr);

                if (!m_TransformsToRender.Contains(newItem) && newItem.HasBody)
                    m_TransformsToRender.Add(newItem);
            }

            foreach (var tr in removedTransforms)
            {
                if (!m_LockedObjects.ContainsKey(tr))
                    m_TransformsToRender.Remove(tr);
            }

            Repaint();
        }

        internal static void UpdateSelectionOnComponentAdd()
        {
            if (s_Window)
                s_Window.UpdateSelection();
        }

        private bool IsSelected(Transform tr)
        {
            return Selection.GetTransforms(SelectionMode.Unfiltered).Contains(tr);
        }

        private VisualisationState ShouldDrawObject(Transform transform)
        {
            return m_LockedObjects.ContainsKey(transform) ? m_LockedObjects[transform] : VisualisationState.None;
        }

        private void RecalculateItemsPerRow(int totalItems, int rows)
        {
            m_NumberOfItemPerRow.Clear();

            for (int i = 0; i < rows; i++)
            {
                if (totalItems > m_Collumns)
                {
                    m_NumberOfItemPerRow.Add(m_Collumns);
                    totalItems -= m_Collumns;
                }
                else
                {
                    m_NumberOfItemPerRow.Add(totalItems);
                    totalItems = 0;
                }
            }
        }

        #endregion

        #region Gizmos

        private void DrawComAndInertia()
        {
            foreach(var lockedObject in m_TransformsToRender)
            {
                if (lockedObject.State == VisualisationState.None)
                    continue;

                lockedObject.Visualize();
            }
        }

        private static void VisualizeRigidbody(Rigidbody rigidbody, bool showCenterOfMass, bool showInertiaTensor)
        {
            var centerOfMass = rigidbody.worldCenterOfMass;
            var rotation = rigidbody.transform.rotation * rigidbody.inertiaTensorRotation;
            var inertiaTensor = rigidbody.inertiaTensor;

            if (showCenterOfMass)
            {
                DrawCenterOfMassArrows(centerOfMass, rotation);
            }

            if (showInertiaTensor)
            {
                DrawInertiaTensorWithHandles(centerOfMass, rotation, inertiaTensor);
            }
        }

        private static void VisualizeArticulationBody(ArticulationBody articulationBody, bool showCenterOfMass, bool showInertiaTensor)
        {
            var centerOfMass = articulationBody.worldCenterOfMass;
            var rotation = articulationBody.transform.rotation * articulationBody.inertiaTensorRotation;
            var inertiaTensor = articulationBody.inertiaTensor;

            if (showCenterOfMass)
            {
                DrawCenterOfMassArrows(centerOfMass, rotation);
            }

            if (showInertiaTensor)
            {
                DrawInertiaTensorWithHandles(centerOfMass, rotation, inertiaTensor);
            }
        }

        private static void DrawCenterOfMassArrows(Vector3 centerOfMass, Quaternion rotation)
        {
            var size = PhysicsVisualizationSettings.centerOfMassUseScreenSize ?
                HandleUtility.GetHandleSize(centerOfMass) :
                1f;

            Matrix4x4 matrix = Matrix4x4.TRS(centerOfMass, rotation, Vector3.one);
            using (new Handles.DrawingScope(ApplyAlphaToColor(Handles.xAxisColor), matrix))
            {
                var thickness = 3f;
                Handles.DrawLine(Vector3.zero, Vector3.right * size, thickness);
                Handles.color = ApplyAlphaToColor(Handles.yAxisColor);
                Handles.DrawLine(Vector3.zero, Vector3.up * size, thickness);
                Handles.color = ApplyAlphaToColor(Handles.zAxisColor);
                Handles.DrawLine(Vector3.zero, Vector3.forward * size, thickness);
            }
        }

        private static void DrawInertiaTensorWithHandles(Vector3 center, Quaternion rotation, Vector3 inertiaTensor)
        {
            inertiaTensor *= PhysicsVisualizationSettings.inertiaTensorScale;

            Matrix4x4 matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
            using (new Handles.DrawingScope(ApplyAlphaToColor(Handles.xAxisColor), matrix))
            {
                float thickness = 3f;
                Handles.DrawLine(Vector3.zero, new Vector3(inertiaTensor.x, 0f, 0f), thickness);
                Handles.color = ApplyAlphaToColor(Handles.yAxisColor);
                Handles.DrawLine(Vector3.zero, new Vector3(0f, inertiaTensor.y, 0f), thickness);
                Handles.color = ApplyAlphaToColor(Handles.zAxisColor);
                Handles.DrawLine(Vector3.zero, new Vector3(0f, 0f, inertiaTensor.z), thickness);
            }
        }

        private static Color ApplyAlphaToColor(Color color)
        {
            color.a += 0.4f;
            color.a *= PhysicsVisualizationSettings.baseAlpha;
            color.a = Mathf.Clamp01(color.a);
            return color;
        }

        #endregion
    }
}
