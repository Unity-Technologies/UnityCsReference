// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;


namespace UnityEditor
{
    class TriggerModuleUI : ModuleUI
    {
        // Keep in sync with TriggerModule.h
        const int k_MaxNumCollisionShapes = 6;
        enum OverlapOptions { Ignore = 0, Kill = 1, Callback = 2 };

        SerializedProperty[] m_CollisionShapes = new SerializedProperty[k_MaxNumCollisionShapes];
        SerializedProperty m_Inside;
        SerializedProperty m_Outside;
        SerializedProperty m_Enter;
        SerializedProperty m_Exit;
        SerializedProperty m_RadiusScale;
        SerializedProperty[] m_ShownCollisionShapes;

        static bool s_VisualizeBounds = false;

        class Texts
        {
            public GUIContent collisionShapes = EditorGUIUtility.TextContent("Colliders|The list of collision shapes to use for the trigger.");
            public GUIContent createCollisionShape = EditorGUIUtility.TextContent("|Create a GameObject containing a sphere collider and assigns it to the list.");
            public GUIContent inside = EditorGUIUtility.TextContent("Inside|What to do for particles that are inside the collision volume.");
            public GUIContent outside = EditorGUIUtility.TextContent("Outside|What to do for particles that are outside the collision volume.");
            public GUIContent enter = EditorGUIUtility.TextContent("Enter|Triggered once when particles enter the collison volume.");
            public GUIContent exit = EditorGUIUtility.TextContent("Exit|Triggered once when particles leave the collison volume.");
            public GUIContent radiusScale = EditorGUIUtility.TextContent("Radius Scale|Scale particle bounds by this amount to get more precise collisions.");
            public GUIContent visualizeBounds = EditorGUIUtility.TextContent("Visualize Bounds|Render the collision bounds of the particles.");

            public GUIContent[] overlapOptions = new GUIContent[]
            {
                EditorGUIUtility.TextContent("Ignore"),
                EditorGUIUtility.TextContent("Kill"),
                EditorGUIUtility.TextContent("Callback")
            };
        }
        private static Texts s_Texts;

        public TriggerModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "TriggerModule", displayName)
        {
            m_ToolTip = "Allows you to execute script code based on whether particles are inside or outside the collision shapes.";
        }

        protected override void Init()
        {
            // Already initialized?
            if (m_Inside != null)
                return;
            if (s_Texts == null)
                s_Texts = new Texts();

            List<SerializedProperty> shownCollisionShapes = new List<SerializedProperty>();
            for (int i = 0; i < m_CollisionShapes.Length; ++i)
            {
                m_CollisionShapes[i] = GetProperty("collisionShape" + i); // Keep name in sync with transfer func in TriggerModule.h
                System.Diagnostics.Debug.Assert(m_CollisionShapes[i] != null);

                // Always show the first collision shape
                if (i == 0 || m_CollisionShapes[i].objectReferenceValue != null)
                    shownCollisionShapes.Add(m_CollisionShapes[i]);
            }

            m_ShownCollisionShapes = shownCollisionShapes.ToArray();

            m_Inside = GetProperty("inside");
            m_Outside = GetProperty("outside");
            m_Enter = GetProperty("enter");
            m_Exit = GetProperty("exit");
            m_RadiusScale = GetProperty("radiusScale");

            s_VisualizeBounds = EditorPrefs.GetBool("VisualizeTriggerBounds", false);
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            DoListOfCollisionShapesGUI();

            GUIPopup(s_Texts.inside, m_Inside, s_Texts.overlapOptions);
            GUIPopup(s_Texts.outside, m_Outside, s_Texts.overlapOptions);
            GUIPopup(s_Texts.enter, m_Enter, s_Texts.overlapOptions);
            GUIPopup(s_Texts.exit, m_Exit, s_Texts.overlapOptions);
            GUIFloat(s_Texts.radiusScale, m_RadiusScale);

            EditorGUI.BeginChangeCheck();
            s_VisualizeBounds = GUIToggle(s_Texts.visualizeBounds, s_VisualizeBounds);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool("VisualizeTriggerBounds", s_VisualizeBounds);
            }
        }

        private static GameObject CreateDefaultCollider(string name, ParticleSystem parentOfGameObject)
        {
            GameObject go = new GameObject(name);
            if (go)
            {
                if (parentOfGameObject)
                    go.transform.parent = parentOfGameObject.transform;
                go.AddComponent<SphereCollider>();
                return go;
            }
            return null;
        }

        private void DoListOfCollisionShapesGUI()
        {
            // only show the list of colliders when we can edit them safely
            if (m_ParticleSystemUI.multiEdit)
            {
                for (int shapeIndex = 0; shapeIndex < k_MaxNumCollisionShapes; shapeIndex++)
                {
                    int hasShape = -1;
                    foreach (ParticleSystem ps in m_ParticleSystemUI.m_ParticleSystems)
                    {
                        int shapeValue = (ps.trigger.GetCollider(shapeIndex) != null) ? 1 : 0;
                        if (hasShape == -1)
                        {
                            hasShape = shapeValue;
                        }
                        else if (shapeValue != hasShape)
                        {
                            EditorGUILayout.HelpBox("Collider list editing is only available when all selected systems contain the same number of colliders", MessageType.Info, true);
                            return;
                        }
                    }
                }
            }

            int buttonPressedIndex = GUIListOfFloatObjectToggleFields(s_Texts.collisionShapes, m_ShownCollisionShapes, null, s_Texts.createCollisionShape, !m_ParticleSystemUI.multiEdit);
            if (buttonPressedIndex >= 0 && !m_ParticleSystemUI.multiEdit)
            {
                GameObject go = CreateDefaultCollider("Collider " + (buttonPressedIndex + 1), m_ParticleSystemUI.m_ParticleSystems[0]);
                go.transform.localPosition = new Vector3(0, 0, 10 + buttonPressedIndex); // ensure each collider is not at same pos
                m_ShownCollisionShapes[buttonPressedIndex].objectReferenceValue = go;
            }

            // Minus button
            Rect rect = GUILayoutUtility.GetRect(0, EditorGUI.kSingleLineHeight); //GUILayoutUtility.GetLastRect();
            rect.x = rect.xMax - kPlusAddRemoveButtonWidth * 2 - kPlusAddRemoveButtonSpacing;
            rect.width = kPlusAddRemoveButtonWidth;
            if (m_ShownCollisionShapes.Length > 1)
            {
                if (MinusButton(rect))
                {
                    m_ShownCollisionShapes[m_ShownCollisionShapes.Length - 1].objectReferenceValue = null;

                    List<SerializedProperty> shownCollisionShapes = new List<SerializedProperty>(m_ShownCollisionShapes);
                    shownCollisionShapes.RemoveAt(shownCollisionShapes.Count - 1);
                    m_ShownCollisionShapes = shownCollisionShapes.ToArray();
                }
            }

            // Plus button
            if (m_ShownCollisionShapes.Length < k_MaxNumCollisionShapes && !m_ParticleSystemUI.multiEdit)
            {
                rect.x += kPlusAddRemoveButtonWidth + kPlusAddRemoveButtonSpacing;
                if (PlusButton(rect))
                {
                    List<SerializedProperty> shownCollisionShapes = new List<SerializedProperty>(m_ShownCollisionShapes);
                    shownCollisionShapes.Add(m_CollisionShapes[shownCollisionShapes.Count]);
                    m_ShownCollisionShapes = shownCollisionShapes.ToArray();
                }
            }
        }

        override public void OnSceneViewGUI()
        {
            if (s_VisualizeBounds == false)
                return;

            Color oldColor = Handles.color;
            Handles.color = Color.green;
            Matrix4x4 oldMatrix = Handles.matrix;

            Vector3[] points0 = new Vector3[20];
            Vector3[] points1 = new Vector3[20];
            Vector3[] points2 = new Vector3[20];

            Handles.SetDiscSectionPoints(points0, Vector3.zero, Vector3.forward, Vector3.right, 360, 1.0f);
            Handles.SetDiscSectionPoints(points1, Vector3.zero, Vector3.up, -Vector3.right, 360, 1.0f);
            Handles.SetDiscSectionPoints(points2, Vector3.zero, Vector3.right, Vector3.up, 360, 1.0f);

            Vector3[] points = new Vector3[points0.Length + points1.Length + points2.Length];
            points0.CopyTo(points, 0);
            points1.CopyTo(points, 20);
            points2.CopyTo(points, 40);

            foreach (ParticleSystem ps in m_ParticleSystemUI.m_ParticleSystems)
            {
                if (!ps.trigger.enabled)
                    continue;

                ParticleSystem.Particle[] particles = new ParticleSystem.Particle[ps.particleCount];
                int count = ps.GetParticles(particles);

                Matrix4x4 transform = Matrix4x4.identity;
                if (ps.main.simulationSpace == ParticleSystemSimulationSpace.Local)
                {
                    transform = ps.GetLocalToWorldMatrix();
                }

                for (int i = 0; i < count; i++)
                {
                    ParticleSystem.Particle particle = particles[i];
                    Vector3 size = particle.GetCurrentSize3D(ps);

                    float radius = System.Math.Max(size.x, System.Math.Max(size.y, size.z)) * 0.5f * ps.trigger.radiusScale;
                    Handles.matrix = transform * Matrix4x4.TRS(particle.position, Quaternion.identity, new Vector3(radius, radius, radius));
                    Handles.DrawPolyLine(points);
                }
            }

            Handles.color = oldColor;
            Handles.matrix = oldMatrix;
        }

        public override void UpdateCullingSupportedString(ref string text)
        {
            text += "\nTriggers module is enabled.";
        }
    }
} // namespace UnityEditor
