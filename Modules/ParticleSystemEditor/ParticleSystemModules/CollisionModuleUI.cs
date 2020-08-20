// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using UnityEditorInternal;

namespace UnityEditor
{
    class CollisionModuleUI : ModuleUI
    {
        enum CollisionTypes { Plane = 0, World = 1 }
        enum CollisionModes { Mode3D = 0, Mode2D = 1 }
        enum PlaneVizType { Grid, Solid }

        SerializedProperty m_Type;
        SerializedProperty m_Planes;
        SerializedMinMaxCurve m_Dampen;
        SerializedMinMaxCurve m_Bounce;
        SerializedMinMaxCurve m_LifetimeLossOnCollision;
        SerializedProperty m_MinKillSpeed;
        SerializedProperty m_MaxKillSpeed;
        SerializedProperty m_RadiusScale;
        SerializedProperty m_CollidesWith;
        SerializedProperty m_CollidesWithDynamic;
        SerializedProperty m_MaxCollisionShapes;
        SerializedProperty m_Quality;
        SerializedProperty m_VoxelSize;
        SerializedProperty m_CollisionMessages;
        SerializedProperty m_CollisionMode;
        SerializedProperty m_ColliderForce;
        SerializedProperty m_MultiplyColliderForceByCollisionAngle;
        SerializedProperty m_MultiplyColliderForceByParticleSpeed;
        SerializedProperty m_MultiplyColliderForceByParticleSize;

        ReorderableList m_PlanesList;

        List<Transform> m_ScenePlanes = new List<Transform>();
        static PlaneVizType m_PlaneVisualizationType = PlaneVizType.Solid;
        static float m_ScaleGrid = 1.0f;
        static bool s_VisualizeBounds = false;
        static Transform s_SelectedTransform; // static so to ensure only one selected Transform across multiple particle systems
        internal static PrefColor s_CollisionBoundsColor = new PrefColor("Particle System/Collision Bounds", 0.0f, 1.0f, 0.0f, 1.0f);

        static EditMode.SceneViewEditMode[] s_SceneViewEditModes = new[]
        {
            EditMode.SceneViewEditMode.ParticleSystemCollisionModulePlanesMove,
            EditMode.SceneViewEditMode.ParticleSystemCollisionModulePlanesRotate
        };

        static readonly string s_UndoCollisionPlaneString = L10n.Tr("Modified Collision Plane Transform");

        class Texts
        {
            public GUIContent lifetimeLoss = EditorGUIUtility.TrTextContent("Lifetime Loss", "When particle collides, it will lose this fraction of its Start Lifetime");
            public GUIContent planes = EditorGUIUtility.TrTextContent("Planes", "Planes are defined by assigning a reference to a transform. This transform can be any transform in the scene and can be animated. Multiple planes can be used. Note: the Y-axis is used as the plane normal.");
            public GUIContent createPlane = EditorGUIUtility.TrTextContent("", "Create an empty GameObject and assign it as a plane.");
            public GUIContent minKillSpeed = EditorGUIUtility.TrTextContent("Min Kill Speed", "When particles collide and their speed is lower than this value, they are killed.");
            public GUIContent maxKillSpeed = EditorGUIUtility.TrTextContent("Max Kill Speed", "When particles collide and their speed is higher than this value, they are killed.");
            public GUIContent dampen = EditorGUIUtility.TrTextContent("Dampen", "When particle collides, it will lose this fraction of its speed. Unless this is set to 0.0, particle will become slower after collision.");
            public GUIContent bounce = EditorGUIUtility.TrTextContent("Bounce", "When particle collides, the bounce is scaled with this value. The bounce is the upwards motion in the plane normal direction.");
            public GUIContent radiusScale = EditorGUIUtility.TrTextContent("Radius Scale", "Scale particle bounds by this amount to get more precise collisions.");
            public GUIContent visualization = EditorGUIUtility.TrTextContent("Visualization", "Only used for visualizing the planes: Wireframe or Solid.");
            public GUIContent scalePlane = EditorGUIUtility.TrTextContent("Scale Plane", "Resizes the visualization planes.");
            public GUIContent visualizeBounds = EditorGUIUtility.TrTextContent("Visualize Bounds", "Render the collision bounds of the particles.");
            public GUIContent collidesWith = EditorGUIUtility.TrTextContent("Collides With", "Collides the particles with colliders included in the layermask.");
            public GUIContent collidesWithDynamic = EditorGUIUtility.TrTextContent("Enable Dynamic Colliders", "Should particles collide with dynamic objects?");
            public GUIContent maxCollisionShapes = EditorGUIUtility.TrTextContent("Max Collision Shapes", "How many collision shapes can be considered for particle collisions. Excess shapes will be ignored. Terrains take priority.");
            public GUIContent quality = EditorGUIUtility.TrTextContent("Collision Quality", "Quality of world collisions. Medium and low quality are approximate and may leak particles.");
            public GUIContent voxelSize = EditorGUIUtility.TrTextContent("Voxel Size", "Size of voxels in the collision cache. Smaller values improve accuracy but require higher memory usage and are less efficient.");
            public GUIContent collisionMessages = EditorGUIUtility.TrTextContent("Send Collision Messages", "Send collision callback messages.");
            public GUIContent collisionType = EditorGUIUtility.TrTextContent("Type", "Collide with a list of Planes, or the Physics World.");
            public GUIContent collisionMode = EditorGUIUtility.TrTextContent("Mode", "Use 3D Physics or 2D Physics.");
            public GUIContent colliderForce = EditorGUIUtility.TrTextContent("Collider Force", "Control the strength of particle forces on colliders.");
            public GUIContent multiplyColliderForceByCollisionAngle = EditorGUIUtility.TrTextContent("Multiply by Collision Angle", "Should the force be proportional to the angle of the particle collision?  A particle collision directly along the collision normal produces all the specified force whilst collisions away from the collision normal produce less force.");
            public GUIContent multiplyColliderForceByParticleSpeed = EditorGUIUtility.TrTextContent("Multiply by Particle Speed", "Should the force be proportional to the particle speed?");
            public GUIContent multiplyColliderForceByParticleSize = EditorGUIUtility.TrTextContent("Multiply by Particle Size", "Should the force be proportional to the particle size?");

            public GUIContent[] collisionTypes = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("Planes"),
                EditorGUIUtility.TrTextContent("World")
            };

            public GUIContent[] collisionModes = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("3D"),
                EditorGUIUtility.TrTextContent("2D")
            };

            public GUIContent[] qualitySettings = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("High"),
                EditorGUIUtility.TrTextContent("Medium (Static Colliders)"),
                EditorGUIUtility.TrTextContent("Low (Static Colliders)")
            };

            public GUIContent[] planeVizTypes = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("Grid"),
                EditorGUIUtility.TrTextContent("Solid")
            };

            public GUIContent[] toolContents =
            {
                EditorGUIUtility.TrIconContent("MoveTool", "Move plane editing mode."),
                EditorGUIUtility.TrIconContent("RotateTool", "Rotate plane editing mode.")
            };
        }
        private static Texts s_Texts;

        public CollisionModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "CollisionModule", displayName)
        {
            m_ToolTip = "Allows you to specify multiple collision planes that the particle can collide with.";
        }

        private bool editingPlanes
        {
            get
            {
                return ((EditMode.editMode == EditMode.SceneViewEditMode.ParticleSystemCollisionModulePlanesMove ||
                    EditMode.editMode == EditMode.SceneViewEditMode.ParticleSystemCollisionModulePlanesRotate) &&
                    EditMode.IsOwner(m_ParticleSystemUI.m_ParticleEffectUI.m_Owner.customEditor));
            }
            set
            {
                if (!value && editingPlanes)
                    EditMode.QuitEditMode();
                SceneView.RepaintAll();
            }
        }

        protected override void Init()
        {
            // Already initialized?
            if (m_Type != null)
                return;
            if (s_Texts == null)
                s_Texts = new Texts();

            m_Type = GetProperty("type");

            m_Planes = GetProperty("m_Planes");

            m_Dampen = new SerializedMinMaxCurve(this, s_Texts.dampen, "m_Dampen");
            m_Dampen.m_AllowCurves = false;

            m_Bounce = new SerializedMinMaxCurve(this, s_Texts.bounce, "m_Bounce");
            m_Bounce.m_AllowCurves = false;

            m_LifetimeLossOnCollision = new SerializedMinMaxCurve(this, s_Texts.lifetimeLoss, "m_EnergyLossOnCollision");
            m_LifetimeLossOnCollision.m_AllowCurves = false;

            m_MinKillSpeed = GetProperty("minKillSpeed");
            m_MaxKillSpeed = GetProperty("maxKillSpeed");
            m_RadiusScale = GetProperty("radiusScale");

            m_PlaneVisualizationType = (PlaneVizType)EditorPrefs.GetInt("PlaneColisionVizType", (int)PlaneVizType.Solid);
            m_ScaleGrid = EditorPrefs.GetFloat("ScalePlaneColision", 1f);
            s_VisualizeBounds = EditorPrefs.GetBool("VisualizeBounds", false);

            m_CollidesWith = GetProperty("collidesWith");
            m_CollidesWithDynamic = GetProperty("collidesWithDynamic");
            m_MaxCollisionShapes = GetProperty("maxCollisionShapes");

            m_Quality = GetProperty("quality");

            m_VoxelSize = GetProperty("voxelSize");

            m_CollisionMessages = GetProperty("collisionMessages");
            m_CollisionMode = GetProperty("collisionMode");

            m_ColliderForce = GetProperty("colliderForce");
            m_MultiplyColliderForceByCollisionAngle = GetProperty("multiplyColliderForceByCollisionAngle");
            m_MultiplyColliderForceByParticleSpeed = GetProperty("multiplyColliderForceByParticleSpeed");
            m_MultiplyColliderForceByParticleSize = GetProperty("multiplyColliderForceByParticleSize");

            m_PlanesList = new ReorderableList(m_Planes.m_SerializedObject, m_Planes, true, false, true, true);
            m_PlanesList.headerHeight = 0;
            m_PlanesList.drawElementCallback = DrawPlaneElementCallback;
            m_PlanesList.elementHeight = kReorderableListElementHeight;
            m_PlanesList.onAddCallback = OnAddPlaneElementCallback;

            SyncVisualization();
        }

        public override void UndoRedoPerformed()
        {
            base.UndoRedoPerformed();
            SyncVisualization();
        }

        protected override void SetVisibilityState(VisibilityState newState)
        {
            base.SetVisibilityState(newState);

            // Show tools again when module is not visible
            if (newState != VisibilityState.VisibleAndFoldedOut)
            {
                s_SelectedTransform = null;
                editingPlanes = false;
            }
            else
            {
                SyncVisualization();
            }
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            EditorGUI.BeginChangeCheck();
            CollisionTypes type = (CollisionTypes)GUIPopup(s_Texts.collisionType, m_Type, s_Texts.collisionTypes);
            if (EditorGUI.EndChangeCheck())
                SyncVisualization();

            if (type == CollisionTypes.Plane)
            {
                DoListOfPlanesGUI();

                EditorGUI.BeginChangeCheck();
                m_PlaneVisualizationType = (PlaneVizType)GUIPopup(s_Texts.visualization, (int)m_PlaneVisualizationType, s_Texts.planeVizTypes);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetInt("PlaneColisionVizType", (int)m_PlaneVisualizationType);
                }

                EditorGUI.BeginChangeCheck();
                m_ScaleGrid = GUIFloat(s_Texts.scalePlane, m_ScaleGrid, "f2");
                if (EditorGUI.EndChangeCheck())
                {
                    m_ScaleGrid = Mathf.Max(0f, m_ScaleGrid);
                    EditorPrefs.SetFloat("ScalePlaneColision", m_ScaleGrid);
                }

                if (EditorGUIUtility.comparisonViewMode == EditorGUIUtility.ComparisonViewMode.None)
                    GUIButtonGroup(s_SceneViewEditModes, s_Texts.toolContents, m_ParticleSystemUI.GetBounds, m_ParticleSystemUI.m_ParticleEffectUI.m_Owner.customEditor);
            }
            else
            {
                GUIPopup(s_Texts.collisionMode, m_CollisionMode, s_Texts.collisionModes);
            }

            GUIMinMaxCurve(s_Texts.dampen, m_Dampen);
            GUIMinMaxCurve(s_Texts.bounce, m_Bounce);
            GUIMinMaxCurve(s_Texts.lifetimeLoss, m_LifetimeLossOnCollision);
            GUIFloat(s_Texts.minKillSpeed, m_MinKillSpeed);
            GUIFloat(s_Texts.maxKillSpeed, m_MaxKillSpeed);
            GUIFloat(s_Texts.radiusScale, m_RadiusScale);

            if (type == CollisionTypes.World)
            {
                GUIPopup(s_Texts.quality, m_Quality, s_Texts.qualitySettings);
                EditorGUI.indentLevel++;
                GUILayerMask(s_Texts.collidesWith, m_CollidesWith);
                GUIInt(s_Texts.maxCollisionShapes, m_MaxCollisionShapes);

                if (m_Quality.intValue == 0)
                    GUIToggle(s_Texts.collidesWithDynamic, m_CollidesWithDynamic);
                else
                    GUIFloat(s_Texts.voxelSize, m_VoxelSize);

                EditorGUI.indentLevel--;

                GUIFloat(s_Texts.colliderForce, m_ColliderForce);
                EditorGUI.indentLevel++;
                GUIToggle(s_Texts.multiplyColliderForceByCollisionAngle, m_MultiplyColliderForceByCollisionAngle);
                GUIToggle(s_Texts.multiplyColliderForceByParticleSpeed, m_MultiplyColliderForceByParticleSpeed);
                GUIToggle(s_Texts.multiplyColliderForceByParticleSize, m_MultiplyColliderForceByParticleSize);
                EditorGUI.indentLevel--;
            }

            GUIToggle(s_Texts.collisionMessages, m_CollisionMessages);

            if (EditorGUIUtility.comparisonViewMode == EditorGUIUtility.ComparisonViewMode.None)
            {
                EditorGUI.BeginChangeCheck();
                s_VisualizeBounds = GUIToggle(s_Texts.visualizeBounds, s_VisualizeBounds);
                if (EditorGUI.EndChangeCheck())
                    EditorPrefs.SetBool("VisualizeBounds", s_VisualizeBounds);
            }
        }

        protected override void OnModuleEnable()
        {
            base.OnModuleEnable();
            SyncVisualization();
        }

        protected override void OnModuleDisable()
        {
            base.OnModuleDisable();
            editingPlanes = false;
        }

        private void SyncVisualization()
        {
            m_ScenePlanes.Clear();

            if (m_Type.hasMultipleDifferentValues || (CollisionTypes)m_Type.intValue != CollisionTypes.Plane)
            {
                editingPlanes = false;
                return;
            }

            foreach (ParticleSystem ps in m_ParticleSystemUI.m_ParticleSystems)
            {
                if (ps.collision.type != ParticleSystemCollisionType.Planes)
                    continue;

                for (int planeIndex = 0; planeIndex < ps.collision.planeCount; planeIndex++)
                {
                    var transform = ps.collision.GetPlane(planeIndex);
                    if (transform != null && !m_ScenePlanes.Contains(transform))
                        m_ScenePlanes.Add(transform);
                }
            }
        }

        private static GameObject CreateEmptyGameObject(string name, ParticleSystem parentOfGameObject)
        {
            GameObject go = new GameObject(name);
            if (go)
            {
                if (parentOfGameObject)
                    go.transform.parent = parentOfGameObject.transform;
                Undo.RegisterCreatedObjectUndo(go, "Created `" + name + "`");
                return go;
            }
            return null;
        }

        private void DoListOfPlanesGUI()
        {
            // only allow editing in single edit mode
            if (m_ParticleSystemUI.multiEdit)
            {
                EditorGUILayout.HelpBox("Trigger editing is only available when editing a single Particle System", MessageType.Info, true);
                return;
            }

            EditorGUI.BeginChangeCheck();

            m_PlanesList.DoLayoutList();

            if (EditorGUI.EndChangeCheck())
                SyncVisualization();
        }

        void OnAddPlaneElementCallback(ReorderableList list)
        {
            int index = m_Planes.arraySize;
            m_Planes.InsertArrayElementAtIndex(index);
            m_Planes.GetArrayElementAtIndex(index).objectReferenceValue = null;
        }

        void DrawPlaneElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            var plane = m_Planes.GetArrayElementAtIndex(index);

            Rect objectRect = new Rect(rect.x, rect.y, rect.width - EditorGUI.kSpacing - ParticleSystemStyles.Get().plus.fixedWidth, rect.height);
            EditorGUI.ObjectField(objectRect, plane, null, GUIContent.none, ParticleSystemStyles.Get().objectField);

            if (plane.objectReferenceValue == null)
            {
                Rect buttonRect = new Rect(objectRect.xMax + EditorGUI.kSpacing, rect.y + 4, ParticleSystemStyles.Get().plus.fixedWidth, rect.height);
                if (GUI.Button(buttonRect, s_Texts.createPlane, ParticleSystemStyles.Get().plus))
                {
                    GameObject go = CreateEmptyGameObject("Plane Transform " + (index + 1), m_ParticleSystemUI.m_ParticleSystems[0]);
                    go.transform.localPosition = new Vector3(0, 0, 10 + index); // ensure each plane is not at same pos
                    go.transform.localEulerAngles = (new Vector3(-90, 0, 0));           // make the plane normal point towards the forward axis of the particle system

                    plane.objectReferenceValue = go.GetComponent<Transform>();
                }
            }
        }

        override public void OnSceneViewGUI()
        {
            RenderCollisionBounds();
            CollisionPlanesSceneGUI();
        }

        private void CollisionPlanesSceneGUI()
        {
            if (m_ScenePlanes.Count == 0)
                return;

            Event evt = Event.current;

            Color origCol = Handles.color;
            Color col = new Color(1, 1, 1, 0.5F);

            for (int i = 0; i < m_ScenePlanes.Count; ++i)
            {
                if (m_ScenePlanes[i] == null)
                    continue;

                Transform transform = m_ScenePlanes[i];
                Vector3 position = transform.position;
                Quaternion rotation = transform.rotation;
                Vector3 right = rotation * Vector3.right;
                Vector3 up = rotation * Vector3.up;
                Vector3 forward = rotation * Vector3.forward;
                bool isPlayingAndStatic = EditorApplication.isPlaying && transform.gameObject.isStatic;
                if (editingPlanes)
                {
                    if (Object.ReferenceEquals(s_SelectedTransform, transform))
                    {
                        EditorGUI.BeginChangeCheck();
                        var newPosition = transform.position;
                        var newRotation = transform.rotation;

                        using (new EditorGUI.DisabledScope(isPlayingAndStatic))
                        {
                            if (isPlayingAndStatic)
                                Handles.ShowSceneViewLabel(position, Handles.s_StaticLabel);
                            if (EditMode.editMode == EditMode.SceneViewEditMode.ParticleSystemCollisionModulePlanesMove)
                                newPosition = Handles.PositionHandle(position, rotation);
                            else if (EditMode.editMode == EditMode.SceneViewEditMode.ParticleSystemCollisionModulePlanesRotate)
                                newRotation = Handles.RotationHandle(rotation, position);
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(transform, s_UndoCollisionPlaneString);
                            transform.position = newPosition;
                            transform.rotation = newRotation;
                            ParticleSystemEditorUtils.PerformCompleteResimulation();
                        }
                    }
                    else
                    {
                        float handleSize = HandleUtility.GetHandleSize(position) * 0.6f;

                        EventType oldEventType = evt.type;

                        // we want ignored mouse up events to check for dragging off of scene view
                        if (evt.type == EventType.Ignore && evt.rawType == EventType.MouseUp)
                            oldEventType = evt.rawType;

                        Handles.FreeMoveHandle(position, Quaternion.identity, handleSize, Vector3.zero, Handles.RectangleHandleCap);

                        // Detect selected plane (similar to TreeEditor)
                        if (oldEventType == EventType.MouseDown && evt.type == EventType.Used)
                        {
                            s_SelectedTransform = transform;
                            oldEventType = EventType.Used;
                            GUIUtility.hotControl = 0; // Reset hot control or the FreeMoveHandle will prevent input to the new Handles. (case 873514)
                        }
                    }
                }

                Handles.color = col;
                Color color = Handles.s_ColliderHandleColor * 0.9f;
                if (isPlayingAndStatic)
                    color.a *= 0.2f;

                if (m_PlaneVisualizationType == PlaneVizType.Grid)
                {
                    DrawGrid(position, right, forward, up, color);
                }
                else
                {
                    DrawSolidPlane(position, rotation, color, Color.yellow);
                }
            }

            Handles.color = origCol;
        }

        void RenderCollisionBounds()
        {
            if (s_VisualizeBounds == false)
                return;

            Color oldColor = Handles.color;
            Handles.color = s_CollisionBoundsColor;
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
                if (!ps.collision.enabled)
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

                    float radius = System.Math.Max(size.x, System.Math.Max(size.y, size.z)) * 0.5f * ps.collision.radiusScale;
                    Handles.matrix = transform * Matrix4x4.TRS(particle.position, Quaternion.identity, new Vector3(radius, radius, radius));
                    Handles.DrawPolyLine(points);
                }
            }

            Handles.color = oldColor;
            Handles.matrix = oldMatrix;
        }

        private static void DrawSolidPlane(Vector3 pos, Quaternion rot, Color faceColor, Color edgeColor)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            var oldMatrix = Handles.matrix;
            float scale = 10 * m_ScaleGrid;
            Handles.matrix = Matrix4x4.TRS(pos, rot, new Vector3(scale, scale, scale)) * Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(90, 0, 0), Vector3.one); // Rotate plane by 90
            Handles.DrawSolidRectangleWithOutline(new Rect(-0.5f, -0.5f, 1, 1), faceColor, edgeColor);
            Handles.DrawLine(Vector3.zero, Vector3.back / scale);
            Handles.matrix = oldMatrix;
        }

        private static void DrawGrid(Vector3 pos, Vector3 axis1, Vector3 axis2, Vector3 normal, Color color)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            HandleUtility.ApplyWireMaterial();

            if (color.a > 0)
            {
                GL.Begin(GL.LINES);

                float lineLength = 10f;
                int numLines = 11;

                lineLength *= m_ScaleGrid;
                numLines = (int)lineLength;
                numLines = Mathf.Clamp(numLines, 10, 40);
                if (numLines % 2 == 0)
                    numLines++;

                float halfLength = lineLength * 0.5f;

                float distBetweenLines = lineLength / (numLines - 1);
                Vector3 v1 = axis1 * lineLength;
                Vector3 v2 = axis2 * lineLength;
                Vector3 dist1 = axis1 * distBetweenLines;
                Vector3 dist2 = axis2 * distBetweenLines;
                Vector3 startPos = pos - axis1 * halfLength - axis2 * halfLength;

                for (int i = 0; i < numLines; i++)
                {
                    if (i % 2 == 0)
                        GL.Color(color * 0.7f);
                    else
                        GL.Color(color);

                    // Axis1
                    GL.Vertex(startPos + i * dist1);
                    GL.Vertex(startPos + i * dist1 + v2);

                    // Axis2
                    GL.Vertex(startPos + i * dist2);
                    GL.Vertex(startPos + i * dist2 + v1);
                }

                GL.Color(color);
                GL.Vertex(pos);
                GL.Vertex(pos + normal);


                GL.End();
            }
        }

        override public void UpdateCullingSupportedString(ref string text)
        {
            text += "\nCollision module is enabled.";
        }
    }
} // namespace UnityEditor
