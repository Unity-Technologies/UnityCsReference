// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

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
                return m_Rigidbody != null || m_Articulation != null;
            }

            // Draw info tables -------

            public void DrawInfoTable()
            {
                if (!HasBody)
                    return;

                if (m_Rigidbody)
                    DrawRigidbodyInfo(m_Rigidbody);
                else if (m_Articulation)
                    DrawArticulationBodyInfo(m_Articulation);
            }

            private void DrawRigidbodyInfo(Rigidbody body)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.FloatField("Speed", body.velocity.magnitude);
                EditorGUILayout.Vector3Field("Velocity", body.velocity);
                EditorGUILayout.Vector3Field("Angular Velocity", body.angularVelocity);
                EditorGUILayout.Vector3Field("Inertia Tensor", body.inertiaTensor);
                EditorGUILayout.Vector3Field("Inertia Tensor Rotation", body.inertiaTensorRotation.eulerAngles);
                EditorGUILayout.Vector3Field("Local Center of Mass", body.centerOfMass);
                EditorGUILayout.Vector3Field("World Center of Mass", body.worldCenterOfMass);
                EditorGUILayout.LabelField("Sleep State", body.IsSleeping() ? "Asleep" : "Awake");
                EditorGUILayout.FloatField("Sleep Threshold", body.sleepThreshold);
                EditorGUILayout.FloatField("Max Linear Velocity", body.maxLinearVelocity);
                EditorGUILayout.FloatField("Max Angular Velocity", body.maxAngularVelocity);
                EditorGUILayout.FloatField("Solver Iterations", body.solverIterations);
                EditorGUILayout.FloatField("Solver Velocity Iterations", body.solverVelocityIterations);
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
            }

            private void DrawArticulationBodyInfo(ArticulationBody body)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.FloatField("Speed", body.velocity.magnitude);
                EditorGUILayout.Vector3Field("Velocity", body.velocity);
                EditorGUILayout.Vector3Field("Angular Velocity", body.angularVelocity);
                EditorGUILayout.Vector3Field("Inertia Tensor", body.inertiaTensor);
                EditorGUILayout.Vector3Field("Inertia Tensor Rotation", body.inertiaTensorRotation.eulerAngles);
                EditorGUILayout.Vector3Field("Local Center of Mass", body.centerOfMass);
                EditorGUILayout.Vector3Field("World Center of Mass", body.worldCenterOfMass);
                EditorGUILayout.LabelField("Sleep State", body.IsSleeping() ? "Asleep" : "Awake");
                EditorGUILayout.FloatField("Sleep Threshold", body.sleepThreshold);
                EditorGUILayout.FloatField("Max Linear Velocity", body.maxLinearVelocity);
                EditorGUILayout.FloatField("Max Angular Velocity", body.maxAngularVelocity);
                EditorGUILayout.FloatField("Solver Iterations", body.solverIterations);
                EditorGUILayout.FloatField("Solver Velocity Iterations", body.solverVelocityIterations);
                EditorGUILayout.IntField("Body Index", body.index);
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
            }
        }

        private bool DrawInfoTabHeader()
        {
            m_Collumns.value = EditorGUILayout.IntSlider("Number of items per row:", m_Collumns.value, 1, 10);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Tracked objects: " + m_TransformsToRender.Count);
            if (GUILayout.Button("Clear locked  objects", GUILayout.MaxWidth(150f)))
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
                m_ObjectsToRemove.Add(tr.Transform);
                return;
            }

            DrawObjectHeader(tr.Transform);
            tr.DrawInfoTable();
        }

        private void DrawObjectHeader(Transform tr)
        {
            var locked = m_LockedObjects.ContainsKey(tr);
            var lockedPrev = locked;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("GameObject: " + tr.name);

            GUILayout.FlexibleSpace();

            var state = locked ? m_LockedObjects[tr] : VisualisationState.None;
            var statePrev = state;

            EditorGUIUtility.labelWidth = 50f;
            EditorGUILayout.LabelField("Draw Gizmos for:", GUILayout.ExpandWidth(false));
            EditorGUIUtility.labelWidth = 0f;
            state = (VisualisationState)EditorGUILayout.EnumPopup(state);

            if (locked)
                m_LockedObjects[tr] = state;
            else if (state != statePrev)
            {
                locked = true;
                lockedPrev = false;
            }

            if (state != statePrev)
                RepaintSceneAndGameViews();

            locked = EditorGUILayout.ToggleLeft("Lock", locked, GUILayout.MaxWidth(50f));

            EditorGUILayout.EndHorizontal();

            if (locked != lockedPrev)
            {
                if (locked)
                    m_ObjectsToAdd.Add(new RenderedTransform(tr, state));
                else m_ObjectsToRemove.Add(tr);

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

        // Manage lists ----------

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
            ClearInvalidLockedObjects();
        }

        // This is usefull when Transfrom references change and they start pointing to null Transforms
        private void ClearInvalidLockedObjects()
        {
            foreach (var lockedObject in m_LockedObjects)
                if (lockedObject.Key == null)
                    m_ObjectsToRemove.Add(lockedObject.Key);

            RemoveLockedObjects();
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

        // Gizmos

        [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
        static void DrawGizmoForRigidbody(Rigidbody rb, GizmoType gizmoType)
        {
            if (!EditorWindow.HasOpenInstances<PhysicsDebugWindow>())
                return;

            var state = s_Window.ShouldDrawObject(rb.transform);

            var showCenterOfMass = (state & VisualisationState.CenterOfMass) == VisualisationState.CenterOfMass;
            var showInertiaTensor = (state & VisualisationState.InertiaTensor) == VisualisationState.InertiaTensor;

            VisualizeRigidbody(rb, showCenterOfMass, showInertiaTensor);
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
        static void DrawGizmoForArticulationBody(ArticulationBody ab, GizmoType gizmoType)
        {
            if (!EditorWindow.HasOpenInstances<PhysicsDebugWindow>())
                return;

            var state = s_Window.ShouldDrawObject(ab.transform);

            var showCenterOfMass = (state & VisualisationState.CenterOfMass) == VisualisationState.CenterOfMass;
            var showInertiaTensor = (state & VisualisationState.InertiaTensor) == VisualisationState.InertiaTensor;

            VisualizeArticulationBody(ab, showCenterOfMass, showInertiaTensor);
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
            color.a *= PhysicsVisualizationSettings.baseAlpha;
            color.a = Mathf.Clamp01(color.a);
            return color;
        }
    }
}
