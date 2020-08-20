// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using UnityEditorInternal;

namespace UnityEditor
{
    class TriggerModuleUI : ModuleUI
    {
        enum OverlapOptions { Ignore = 0, Kill = 1, Callback = 2 }

        SerializedProperty m_Primitives;
        SerializedProperty m_Inside;
        SerializedProperty m_Outside;
        SerializedProperty m_Enter;
        SerializedProperty m_Exit;
        SerializedProperty m_ColliderQueryMode;
        SerializedProperty m_RadiusScale;

        static bool s_VisualizeBounds = false;

        class Texts
        {
            public GUIContent collisionShapes = EditorGUIUtility.TrTextContent("Colliders", "The list of collision shapes to use for the trigger.");
            public GUIContent createCollisionShape = EditorGUIUtility.TrTextContent("", "Create a GameObject containing a sphere collider and assign it to the list.");
            public GUIContent inside = EditorGUIUtility.TrTextContent("Inside", "What to do for particles that are inside the collision volume.");
            public GUIContent outside = EditorGUIUtility.TrTextContent("Outside", "What to do for particles that are outside the collision volume.");
            public GUIContent enter = EditorGUIUtility.TrTextContent("Enter", "Triggered once when particles enter the collision volume.");
            public GUIContent exit = EditorGUIUtility.TrTextContent("Exit", "Triggered once when particles leave the collision volume.");
            public GUIContent colliderQueryMode = EditorGUIUtility.TrTextContent("Collider Query Mode", "Required in order to get collider information when using the ParticleSystem.GetTriggerParticles script API. Disabled by default because it has a performance cost.\nWhen set to One, the script can retrieve the first collider in the list that the particle interacts with.\nSetting to All will return all colliders that the particle interacts with.");
            public GUIContent radiusScale = EditorGUIUtility.TrTextContent("Radius Scale", "Scale particle bounds by this amount to get more precise collisions.");
            public GUIContent visualizeBounds = EditorGUIUtility.TrTextContent("Visualize Bounds", "Render the collision bounds of the particles.");

            public GUIContent[] overlapOptions = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("Ignore"),
                EditorGUIUtility.TrTextContent("Kill"),
                EditorGUIUtility.TrTextContent("Callback")
            };

            public GUIContent[] colliderQueryModeOptions = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("Disabled"),
                EditorGUIUtility.TrTextContent("One"),
                EditorGUIUtility.TrTextContent("All")
            };
        }
        private static Texts s_Texts;

        ReorderableList m_PrimitivesList;

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

            m_Primitives = GetProperty("primitives");
            m_Inside = GetProperty("inside");
            m_Outside = GetProperty("outside");
            m_Enter = GetProperty("enter");
            m_Exit = GetProperty("exit");
            m_ColliderQueryMode = GetProperty("colliderQueryMode");
            m_RadiusScale = GetProperty("radiusScale");

            m_PrimitivesList = new ReorderableList(m_Primitives.m_SerializedObject, m_Primitives, true, false, true, true);
            m_PrimitivesList.headerHeight = 0;
            m_PrimitivesList.drawElementCallback = DrawPrimitiveElementCallback;
            m_PrimitivesList.elementHeight = kReorderableListElementHeight;
            m_PrimitivesList.onAddCallback = OnAddPrimitiveElementCallback;

            s_VisualizeBounds = EditorPrefs.GetBool("VisualizeTriggerBounds", false);
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            DoListOfCollisionShapesGUI();

            GUIPopup(s_Texts.inside, m_Inside, s_Texts.overlapOptions);
            GUIPopup(s_Texts.outside, m_Outside, s_Texts.overlapOptions);
            GUIPopup(s_Texts.enter, m_Enter, s_Texts.overlapOptions);
            GUIPopup(s_Texts.exit, m_Exit, s_Texts.overlapOptions);
            GUIPopup(s_Texts.colliderQueryMode, m_ColliderQueryMode, s_Texts.colliderQueryModeOptions);
            GUIFloat(s_Texts.radiusScale, m_RadiusScale);

            if (EditorGUIUtility.comparisonViewMode == EditorGUIUtility.ComparisonViewMode.None)
            {
                EditorGUI.BeginChangeCheck();
                s_VisualizeBounds = GUIToggle(s_Texts.visualizeBounds, s_VisualizeBounds);
                if (EditorGUI.EndChangeCheck())
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
            // only allow editing in single edit mode
            if (m_ParticleSystemUI.multiEdit)
            {
                EditorGUILayout.HelpBox("Trigger editing is only available when editing a single Particle System", MessageType.Info, true);
                return;
            }

            m_PrimitivesList.DoLayoutList();
        }

        void OnAddPrimitiveElementCallback(ReorderableList list)
        {
            int index = m_Primitives.arraySize;
            m_Primitives.InsertArrayElementAtIndex(index);
            m_Primitives.GetArrayElementAtIndex(index).objectReferenceValue = null;
        }

        void DrawPrimitiveElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            var primitive = m_Primitives.GetArrayElementAtIndex(index);

            Rect objectRect = new Rect(rect.x, rect.y, rect.width - EditorGUI.kSpacing - ParticleSystemStyles.Get().plus.fixedWidth, rect.height);
            EditorGUI.ObjectField(objectRect, primitive, null, GUIContent.none, ParticleSystemStyles.Get().objectField);

            if (primitive.objectReferenceValue == null)
            {
                Rect buttonRect = new Rect(objectRect.xMax + EditorGUI.kSpacing, rect.y + 4, ParticleSystemStyles.Get().plus.fixedWidth, rect.height);
                if (GUI.Button(buttonRect, s_Texts.createCollisionShape, ParticleSystemStyles.Get().plus))
                {
                    GameObject go = CreateDefaultCollider("Collider " + (index + 1), m_ParticleSystemUI.m_ParticleSystems[0]);
                    go.transform.localPosition = new Vector3(0, 0, 10 + index); // ensure each collider is not at the same position
                    primitive.objectReferenceValue = go.GetComponent<Collider>();
                }
            }
        }

        override public void OnSceneViewGUI()
        {
            if (s_VisualizeBounds == false)
                return;

            Color oldColor = Handles.color;
            Handles.color = CollisionModuleUI.s_CollisionBoundsColor;
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
                    transform = ps.localToWorldMatrix;
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
