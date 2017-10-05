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
        // Keep in sync with CollisionModule.h
        const int k_MaxNumPlanes = 6;
        enum CollisionTypes { Plane = 0, World = 1 };
        enum CollisionModes { Mode3D = 0, Mode2D = 1 };
        enum ForceModes { None = 0, Constant = 1, SizeBased = 2 };
        enum PlaneVizType { Grid, Solid };

        SerializedProperty m_Type;
        SerializedProperty[] m_Planes = new SerializedProperty[k_MaxNumPlanes];
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

        SerializedProperty[] m_ShownPlanes;

        List<Transform> m_ScenePlanes = new List<Transform>();
        static PlaneVizType m_PlaneVisualizationType = PlaneVizType.Solid;
        static float m_ScaleGrid = 1.0f;
        static bool s_VisualizeBounds = false;
        static Transform s_SelectedTransform; // static so to ensure only one selected Transform across multiple particle systems

        class Texts
        {
            public GUIContent lifetimeLoss = EditorGUIUtility.TextContent("Lifetime Loss|When particle collides, it will lose this fraction of its Start Lifetime");
            public GUIContent planes = EditorGUIUtility.TextContent("Planes|Planes are defined by assigning a reference to a transform. This transform can be any transform in the scene and can be animated. Multiple planes can be used. Note: the Y-axis is used as the plane normal.");
            public GUIContent createPlane = EditorGUIUtility.TextContent("|Create an empty GameObject and assign it as a plane.");
            public GUIContent minKillSpeed = EditorGUIUtility.TextContent("Min Kill Speed|When particles collide and their speed is lower than this value, they are killed.");
            public GUIContent maxKillSpeed = EditorGUIUtility.TextContent("Max Kill Speed|When particles collide and their speed is higher than this value, they are killed.");
            public GUIContent dampen = EditorGUIUtility.TextContent("Dampen|When particle collides, it will lose this fraction of its speed. Unless this is set to 0.0, particle will become slower after collision.");
            public GUIContent bounce = EditorGUIUtility.TextContent("Bounce|When particle collides, the bounce is scaled with this value. The bounce is the upwards motion in the plane normal direction.");
            public GUIContent radiusScale = EditorGUIUtility.TextContent("Radius Scale|Scale particle bounds by this amount to get more precise collisions.");
            public GUIContent visualization = EditorGUIUtility.TextContent("Visualization|Only used for visualizing the planes: Wireframe or Solid.");
            public GUIContent scalePlane = EditorGUIUtility.TextContent("Scale Plane|Resizes the visualization planes.");
            public GUIContent visualizeBounds = EditorGUIUtility.TextContent("Visualize Bounds|Render the collision bounds of the particles.");
            public GUIContent collidesWith = EditorGUIUtility.TextContent("Collides With|Collides the particles with colliders included in the layermask.");
            public GUIContent collidesWithDynamic = EditorGUIUtility.TextContent("Enable Dynamic Colliders|Should particles collide with dynamic objects?");
            public GUIContent maxCollisionShapes = EditorGUIUtility.TextContent("Max Collision Shapes|How many collision shapes can be considered for particle collisions. Excess shapes will be ignored. Terrains take priority.");
            public GUIContent quality = EditorGUIUtility.TextContent("Collision Quality|Quality of world collisions. Medium and low quality are approximate and may leak particles.");
            public GUIContent voxelSize = EditorGUIUtility.TextContent("Voxel Size|Size of voxels in the collision cache. Smaller values improve accuracy, but require higher memory usage and are less efficient.");
            public GUIContent collisionMessages = EditorGUIUtility.TextContent("Send Collision Messages|Send collision callback messages.");
            public GUIContent collisionType = EditorGUIUtility.TextContent("Type|Collide with a list of Planes, or the Physics World.");
            public GUIContent collisionMode = EditorGUIUtility.TextContent("Mode|Use 3D Physics or 2D Physics.");
            public GUIContent colliderForce = EditorGUIUtility.TextContent("Collider Force|Control the strength of particle forces on colliders.");
            public GUIContent multiplyColliderForceByCollisionAngle = EditorGUIUtility.TextContent("Multiply by Collision Angle|Should the force be proportional to the angle of the particle collision?  A particle collision directly along the collision normal produces all the specified force whilst collisions away from the collision normal produce less force.");
            public GUIContent multiplyColliderForceByParticleSpeed = EditorGUIUtility.TextContent("Multiply by Particle Speed|Should the force be proportional to the particle speed?");
            public GUIContent multiplyColliderForceByParticleSize = EditorGUIUtility.TextContent("Multiply by Particle Size|Should the force be proportional to the particle size?");

            public GUIContent[] collisionTypes = new GUIContent[]
            {
                EditorGUIUtility.TextContent("Planes"),
                EditorGUIUtility.TextContent("World")
            };

            public GUIContent[] collisionModes = new GUIContent[]
            {
                EditorGUIUtility.TextContent("3D"),
                EditorGUIUtility.TextContent("2D")
            };

            public GUIContent[] qualitySettings = new GUIContent[]
            {
                EditorGUIUtility.TextContent("High"),
                EditorGUIUtility.TextContent("Medium (Static Colliders)"),
                EditorGUIUtility.TextContent("Low (Static Colliders)")
            };

            public GUIContent[] planeVizTypes = new GUIContent[]
            {
                EditorGUIUtility.TextContent("Grid"),
                EditorGUIUtility.TextContent("Solid")
            };

            public GUIContent[] toolContents =
            {
                EditorGUIUtility.IconContent("MoveTool", "|Move plane editing mode."),
                EditorGUIUtility.IconContent("RotateTool", "|Rotate plane editing mode.")
            };

            public EditMode.SceneViewEditMode[] sceneViewEditModes = new[]
            {
                EditMode.SceneViewEditMode.ParticleSystemCollisionModulePlanesMove,
                EditMode.SceneViewEditMode.ParticleSystemCollisionModulePlanesRotate
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
                {
                    EditMode.QuitEditMode();
                }
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

            List<SerializedProperty> shownPlanes = new List<SerializedProperty>();
            for (int i = 0; i < m_Planes.Length; ++i)
            {
                m_Planes[i] = GetProperty("plane" + i); // Keep name in sync with transfer func in CollisionModule.h
                System.Diagnostics.Debug.Assert(m_Planes[i] != null);

                // Always show the first plane
                if (i == 0 || m_Planes[i].objectReferenceValue != null)
                    shownPlanes.Add(m_Planes[i]);
            }

            m_ShownPlanes = shownPlanes.ToArray();

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

        private Bounds GetBounds()
        {
            var combinedBounds = new Bounds();
            bool initialized = false;

            foreach (ParticleSystem ps in m_ParticleSystemUI.m_ParticleSystems)
            {
                ParticleSystemRenderer particleSystemRenderer = ps.GetComponent<ParticleSystemRenderer>();
                if (!initialized)
                    combinedBounds = particleSystemRenderer.bounds;
                combinedBounds.Encapsulate(particleSystemRenderer.bounds);

                initialized = true;
            }

            return combinedBounds;
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

                GUIButtonGroup(s_Texts.sceneViewEditModes, s_Texts.toolContents, GetBounds, m_ParticleSystemUI.m_ParticleEffectUI.m_Owner.customEditor);
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

            EditorGUI.BeginChangeCheck();
            s_VisualizeBounds = GUIToggle(s_Texts.visualizeBounds, s_VisualizeBounds);
            if (EditorGUI.EndChangeCheck())
            {
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

            if (m_ParticleSystemUI.multiEdit)
            {
                foreach (ParticleSystem ps in m_ParticleSystemUI.m_ParticleSystems)
                {
                    if (ps.collision.type != ParticleSystemCollisionType.Planes)
                        continue;

                    for (int planeIndex = 0; planeIndex < k_MaxNumPlanes; planeIndex++)
                    {
                        var transform = ps.collision.GetPlane(planeIndex);
                        if (transform != null && !m_ScenePlanes.Contains(transform))
                            m_ScenePlanes.Add(transform);
                    }
                }
            }
            else
            {
                CollisionTypes type = (CollisionTypes)m_Type.intValue;
                if (type != CollisionTypes.Plane)
                {
                    editingPlanes = false;
                    return;
                }
                for (int i = 0; i < m_ShownPlanes.Length; ++i)
                {
                    Transform transform = m_ShownPlanes[i].objectReferenceValue as Transform;
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

        private bool IsListOfPlanesValid()
        {
            if (m_ParticleSystemUI.multiEdit)
            {
                for (int planeIndex = 0; planeIndex < k_MaxNumPlanes; planeIndex++)
                {
                    int hasPlane = -1;
                    foreach (ParticleSystem ps in m_ParticleSystemUI.m_ParticleSystems)
                    {
                        int planeValue = (ps.collision.GetPlane(planeIndex) != null) ? 1 : 0;
                        if (hasPlane == -1)
                        {
                            hasPlane = planeValue;
                        }
                        else if (planeValue != hasPlane)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private void DoListOfPlanesGUI()
        {
            // only show the list of planes when we can edit them safely
            if (!IsListOfPlanesValid())
            {
                EditorGUILayout.HelpBox("Plane list editing is only available when all selected systems contain the same number of planes", MessageType.Info, true);
                return;
            }

            EditorGUI.BeginChangeCheck();
            int buttonPressedIndex = GUIListOfFloatObjectToggleFields(s_Texts.planes, m_ShownPlanes, null, s_Texts.createPlane, !m_ParticleSystemUI.multiEdit);
            bool listChanged = EditorGUI.EndChangeCheck();

            if (buttonPressedIndex >= 0 && !m_ParticleSystemUI.multiEdit)
            {
                GameObject go = CreateEmptyGameObject("Plane Transform " + (buttonPressedIndex + 1), m_ParticleSystemUI.m_ParticleSystems[0]);
                go.transform.localPosition = new Vector3(0, 0, 10 + buttonPressedIndex); // ensure each plane is not at same pos
                go.transform.localEulerAngles =  (new Vector3(-90, 0, 0));           // make the plane normal point towards the forward axis of the particle system

                m_ShownPlanes[buttonPressedIndex].objectReferenceValue = go;
                listChanged = true;
            }

            // Minus button
            Rect rect = GUILayoutUtility.GetRect(0, EditorGUI.kSingleLineHeight); //GUILayoutUtility.GetLastRect();
            rect.x = rect.xMax - kPlusAddRemoveButtonWidth * 2 - kPlusAddRemoveButtonSpacing;
            rect.width = kPlusAddRemoveButtonWidth;
            if (m_ShownPlanes.Length > 1)
            {
                if (MinusButton(rect))
                {
                    m_ShownPlanes[m_ShownPlanes.Length - 1].objectReferenceValue = null;
                    List<SerializedProperty> shownPlanes = new List<SerializedProperty>(m_ShownPlanes);
                    shownPlanes.RemoveAt(shownPlanes.Count - 1);
                    m_ShownPlanes = shownPlanes.ToArray();
                    listChanged = true;
                }
            }

            // Plus button
            if (m_ShownPlanes.Length < k_MaxNumPlanes && !m_ParticleSystemUI.multiEdit)
            {
                rect.x += kPlusAddRemoveButtonWidth + kPlusAddRemoveButtonSpacing;
                if (PlusButton(rect))
                {
                    List<SerializedProperty> shownPlanes = new List<SerializedProperty>(m_ShownPlanes);
                    shownPlanes.Add(m_Planes[shownPlanes.Count]);
                    m_ShownPlanes = shownPlanes.ToArray();
                }
            }

            if (listChanged)
                SyncVisualization();
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
                                Handles.ShowStaticLabel(position);
                            if (EditMode.editMode == EditMode.SceneViewEditMode.ParticleSystemCollisionModulePlanesMove)
                                newPosition = Handles.PositionHandle(position, rotation);
                            else if (EditMode.editMode == EditMode.SceneViewEditMode.ParticleSystemCollisionModulePlanesRotate)
                                newRotation = Handles.RotationHandle(rotation, position);
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(transform, "Modified Collision Plane Transform");
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
                if (!ps.collision.enabled)
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
