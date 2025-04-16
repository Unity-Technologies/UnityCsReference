// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;
using UnityObject = UnityEngine.Object;
using UnityEditor.Overlays;


namespace UnityEditor
{
    class ClothInspectorState : ScriptableSingleton<ClothInspectorState>
    {
        [SerializeField] public ClothInspector.DrawMode DrawMode = ClothInspector.DrawMode.MaxDistance;
        [SerializeField] public bool ManipulateBackfaces = false;
        [SerializeField] public bool PaintMaxDistanceEnabled = true;
        [SerializeField] public bool PaintCollisionSphereDistanceEnabled = false;
        [SerializeField] public float PaintMaxDistance = 0.2f;
        [SerializeField] public float PaintCollisionSphereDistance = 0.0f;
        [SerializeField] public bool SetMaxDistance = false;
        [SerializeField] public bool SetCollisionSphereDistance = false;
        [SerializeField] public ClothInspector.ToolMode ToolMode = ClothInspector.ToolMode.Paint;
        [SerializeField] public ClothInspector.CollToolMode CollToolMode = ClothInspector.CollToolMode.Select;
        [SerializeField] public float BrushRadius = 0.075f;
        [SerializeField] public bool SetSelfAndInterCollision = false;
        [SerializeField] public ClothInspector.CollisionVisualizationMode VisualizeSelfOrInterCollision = ClothInspector.CollisionVisualizationMode.SelfCollision;
        [SerializeField] public float SelfCollisionDistance = 0.1f;
        [SerializeField] public float SelfCollisionStiffness = 0.2f;
        [SerializeField] public float InterCollisionDistance = 0.1f;
        [SerializeField] public float InterCollisionStiffness = 0.2f;
        [SerializeField] public float ConstraintSize = 0.05f;
        [SerializeField] public float GradientStartValue = 0.0f;
        [SerializeField] public float GradientEndValue = 1.0f;
    }

    [CustomEditor(typeof(Cloth))]
    [CanEditMultipleObjects]
    class ClothInspector : Editor
    {
        public enum DrawMode { MaxDistance = 1, CollisionSphereDistance }
        public enum ToolMode { Select, Paint, GradientTool }
        public enum CollToolMode { Select, Paint, Erase }
        enum RectSelectionMode { Replace, Add, Substract }
        public enum CollisionVisualizationMode { SelfCollision, InterCollision }

        bool[] m_ParticleSelection;
        bool[] m_ParticleRectSelection;
        bool[] m_SelfAndInterCollisionSelection;

        Vector3[] m_ClothParticlesInWorldSpace;
        Vector3[] m_ClothNormalsInWorldSpace;

        Vector3 m_BrushPos;
        Vector3 m_BrushNorm;
        int m_BrushFace = -1;
        int m_ClothToolControlID;

        Vector3[] m_LastVertices;
        Vector2 m_SelectStartPoint;
        Vector2 m_SelectMousePoint;
        bool m_RectSelecting = false;
        bool m_DidSelect = false;
        float[] m_MaxVisualizedValue = new float[3];
        float[] m_MinVisualizedValue = new float[3];
        RectSelectionMode m_RectSelectionMode = RectSelectionMode.Add;
        int m_NumVerts = 0;

        Vector3 m_GradientStartPoint;
        Vector3 m_GradientEndPoint;

        const float kDisabledValue = float.MaxValue;

        static Texture2D s_ColorTexture = null;
        static ClothInspector s_Inspector;

        public static PrefColor s_BrushColor = new PrefColor("Cloth/Brush Color 2", 0.0f / 255.0f, 0.0f / 255.0f, 0.0f / 255.0f, 51.0f / 255.0f);
        public static PrefColor s_SelfAndInterCollisionParticleColor = new PrefColor("Cloth/Self or Inter Collision Particle Color 2", 145.0f / 255.0f, 244.0f / 255.0f, 139.0f / 255.0f, 0.5f);
        public static PrefColor s_UnselectedSelfAndInterCollisionParticleColor = new PrefColor("Cloth/Unselected Self or Inter Collision Particle Color 2", 0.1f, 0.1f, 0.1f, 0.5f);
        public static PrefColor s_SelectedParticleColor = new PrefColor("Cloth/Selected Self or Inter Collision Particle Color 2", 64.0f / 255.0f, 160.0f / 255.0f, 255.0f / 255.0f, 0.5f);

        public static ToolMode[] s_ToolMode =
        {
            ToolMode.Paint,
            ToolMode.Select,
            ToolMode.GradientTool
        };

        SerializedProperty m_SelfCollisionDistance;
        SerializedProperty m_SelfCollisionStiffness;

        int m_NumSelection = 0;

        SkinnedMeshRenderer m_Smr;
        Transform m_TransformOverride;
        WeakReference m_CachedMesh;
        private static class Styles
        {
            public static readonly GUIContent editConstraintsLabel = EditorGUIUtility.TrTextContent("Edit Constraints");
            public static readonly GUIContent editSelfInterCollisionLabel = EditorGUIUtility.TrTextContent("Edit Collision Particles");
            public static readonly GUIContent selfInterCollisionParticleColor = EditorGUIUtility.TrTextContent("Visualization Color");
            public static readonly GUIContent selfInterCollisionBrushColor = EditorGUIUtility.TrTextContent("Brush Color");
            public static readonly GUIContent clothSelfCollisionAndInterCollision = EditorGUIUtility.TrTextContent("Cloth Self-Collision and Inter-Collision");
            public static readonly GUIContent paintCollisionParticles = EditorGUIUtility.TrTextContent("Paint Collision Particles");
            public static readonly GUIContent selectCollisionParticles = EditorGUIUtility.TrTextContent("Select Collision Particles");
            public static readonly GUIContent brushRadiusString = EditorGUIUtility.TrTextContent("Brush Radius");
            public static readonly GUIContent constraintSizeString = EditorGUIUtility.TrTextContent("Constraint Size");
            public static readonly GUIContent gradientStartString = EditorGUIUtility.TrTextContent("Gradient Start");
            public static readonly GUIContent gradientEndString = EditorGUIUtility.TrTextContent("Gradient End");
            public static readonly GUIContent setMaxDistanceString = EditorGUIUtility.TrTextContent("Max Distance");
            public static readonly GUIContent setCollisionSphereDistanceString = EditorGUIUtility.TrTextContent("Surface Penetration");
            public static readonly GUIContent selfAndInterCollisionMode = EditorGUIUtility.TrTextContent("Paint or Select Particles");
            public static readonly GUIContent backFaceManipulationMode = EditorGUIUtility.TrTextContent("Back Face Manipulation");
            public static readonly GUIContent manipulateBackFaceString = EditorGUIUtility.TrTextContent("Manipulate Backfaces");
            public static readonly GUIContent selfCollisionString = EditorGUIUtility.TrTextContent("Self-Collision");
            public static readonly GUIContent setSelfAndInterCollisionString = EditorGUIUtility.TrTextContent("Self-Collision and Inter-Collision");

            public static readonly int clothEditorWindowWidth = 300;

            public static GUIContent[] toolContents =
            {
                EditorGUIUtility.IconContent("editconstraints_16"),
                EditorGUIUtility.IconContent("editCollision_16")
            };

            public static GUIContent[] toolIcons =
            {
                EditorGUIUtility.TrTextContent("Select"),
                EditorGUIUtility.TrTextContent("Paint"),
                EditorGUIUtility.TrTextContent("Gradient Tool")
            };

            public static GUIContent[] drawModeStrings =
            {
                EditorGUIUtility.TrTextContent("Fixed"),
                EditorGUIUtility.TrTextContent("Max Distance"),
                EditorGUIUtility.TrTextContent("Surface Penetration")
            };

            public static GUIContent[] toolModeStrings =
            {
                EditorGUIUtility.TrTextContent("Select"),
                EditorGUIUtility.TrTextContent("Paint"),
                EditorGUIUtility.TrTextContent("Erase")
            };

            public static GUIContent[] collToolModeIcons =
            {
                EditorGUIUtility.TrTextContent("Select"),
                EditorGUIUtility.TrTextContent("Paint"),
                EditorGUIUtility.TrTextContent("Erase")
            };

            public static GUIContent[] collVisModeStrings =
            {
                EditorGUIUtility.TrTextContent("Self-Collision"),
                EditorGUIUtility.TrTextContent("Inter-Collision"),
            };

            public static GUIContent paintIcon = EditorGUIUtility.TrIconContent("ClothInspector.PaintValue", "Change this vertex coefficient value by painting in the scene view.");

            public static EditMode.SceneViewEditMode[] sceneViewEditModes = new[]
            {
                EditMode.SceneViewEditMode.ClothConstraints,
                EditMode.SceneViewEditMode.ClothSelfAndInterCollisionParticles
            };

            public static GUIContent selfCollisionDistanceGUIContent = EditorGUIUtility.TrTextContent("Self-Collision Distance");
            public static GUIContent selfCollisionStiffnessGUIContent = EditorGUIUtility.TrTextContent("Self-Collision Stiffness");

            static Styles()
            {
                toolContents[0].tooltip = EditorGUIUtility.TrTextContent("Edit cloth constraints").text;
                toolContents[1].tooltip = EditorGUIUtility.TrTextContent("Edit cloth self/inter-collision").text;

                toolIcons[0].tooltip = EditorGUIUtility.TrTextContent("Select cloth particles for use in self/inter-collision").text;
                toolIcons[1].tooltip = EditorGUIUtility.TrTextContent("Paint cloth particles for use in self/inter-collision").text;

                collToolModeIcons[0].tooltip = EditorGUIUtility.TrTextContent("Select cloth particles.").text;
                collToolModeIcons[1].tooltip = EditorGUIUtility.TrTextContent("Paint cloth particles.").text;
                collToolModeIcons[2].tooltip = EditorGUIUtility.TrTextContent("Erase cloth particles.").text;
            }
        }

        ClothInspectorState state => ClothInspectorState.instance;

        DrawMode drawMode
        {
            get
            {
                return state.DrawMode;
            }
            set
            {
                if (state.DrawMode != value)
                {
                    state.DrawMode = value;
                    Repaint();
                }
            }
        }

        Cloth cloth => (Cloth)target;
        public bool editingConstraints => EditMode.editMode == EditMode.SceneViewEditMode.ClothConstraints && EditMode.IsOwner(this);
        public bool editingSelfAndInterCollisionParticles => EditMode.editMode == EditMode.SceneViewEditMode.ClothSelfAndInterCollisionParticles && EditMode.IsOwner(this);

        GUIContent GetDrawModeString(DrawMode mode)
        {
            return Styles.drawModeStrings[(int)mode];
        }

        GUIContent GetCollVisModeString(CollisionVisualizationMode mode)
        {
            return Styles.collVisModeStrings[(int)mode];
        }

        bool IsMeshValid()
        {
            if (cloth.vertices.Length != m_NumVerts)
            {
                InitInspector();
                return true;
            }
            else if (m_NumVerts == 0)
            {
                return false;
            }

            return true;
        }

        Texture2D GenerateColorTexture(int width)
        {
            var tex = new Texture2D(width, 1, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.hideFlags = HideFlags.DontSave;

            Color[] colors = new Color[width];
            for (int i = 0; i < width; i++)
                colors[i] = GetGradientColor(i / (float)(width - 1));
            tex.SetPixels(colors);
            tex.Apply();
            return tex;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Multi-editing in scene not supported
            if (targets.Length < 2)
            {
                bool reinitInspector = false;
                //sync transform override from smr
                var actualRootBone = m_Smr.actualRootBone;
                if (m_TransformOverride != actualRootBone)
                {
                    reinitInspector = true;
                    m_TransformOverride = actualRootBone;
                }
                else if (actualRootBone.hasChanged)
                {
                    reinitInspector = true;
                }

                if (m_Smr.sharedMesh != m_CachedMesh.Target as Mesh)
                {
                    m_CachedMesh.Target = m_Smr.sharedMesh;
                    reinitInspector = true;
                }

                if (reinitInspector)
                    InitInspector();

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditMode.DoInspectorToolbar(Styles.sceneViewEditModes, Styles.toolContents, this);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            if (editingSelfAndInterCollisionParticles)
            {
                if ((state.SetSelfAndInterCollision) || ((state.CollToolMode == CollToolMode.Paint) || (state.CollToolMode == CollToolMode.Erase)))
                {
                    if ((cloth.selfCollisionDistance >= 0.0f) && (state.SelfCollisionDistance != cloth.selfCollisionDistance))
                    {
                        state.SelfCollisionDistance = cloth.selfCollisionDistance;
                        m_SelfCollisionDistance.floatValue = cloth.selfCollisionDistance;
                    }

                    if ((cloth.selfCollisionStiffness >= 0.0f) && (state.SelfCollisionStiffness != cloth.selfCollisionStiffness))
                    {
                        state.SelfCollisionStiffness = cloth.selfCollisionStiffness;
                        m_SelfCollisionStiffness.floatValue = cloth.selfCollisionStiffness;
                    }

                    Rect rect = GUILayoutUtility.GetRect(new GUIContent(), GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.Height(17));
                    EditorGUI.LabelField(rect, Styles.selfCollisionString, EditorStyles.boldLabel);
                    Rect rectDist = GUILayoutUtility.GetRect(new GUIContent(), GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.Height(17));
                    EditorGUI.PropertyField(rectDist, m_SelfCollisionDistance, Styles.selfCollisionDistanceGUIContent);
                    Rect rectStiff = GUILayoutUtility.GetRect(new GUIContent(), GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.Height(17));
                    EditorGUI.PropertyField(rectStiff, m_SelfCollisionStiffness, Styles.selfCollisionStiffnessGUIContent);
                    GUILayout.Space(10);
                }

                if (Physics.interCollisionDistance >= 0.0f)
                {
                    state.InterCollisionDistance = Physics.interCollisionDistance;
                }
                else
                {
                    Physics.interCollisionDistance = state.InterCollisionDistance;
                }

                if (Physics.interCollisionStiffness >= 0.0f)
                {
                    state.InterCollisionStiffness = Physics.interCollisionStiffness;
                }
                else
                {
                    Physics.interCollisionStiffness = state.InterCollisionStiffness;
                }
            }

            DrawPropertiesExcluding(serializedObject, "m_SelfAndInterCollisionIndices", "m_VirtualParticleIndices", "m_SelfCollisionDistance", "m_SelfCollisionStiffness");

            serializedObject.ApplyModifiedProperties();

            MeshRenderer meshRenderer = cloth.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
                Debug.LogWarning("MeshRenderer will not work with a cloth component! Use only SkinnedMeshRenderer. Any MeshRenderer's attached to a cloth component will be deleted at runtime.");
        }

        void UpdatePreviewBrush()
        {
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

            bool hasHit = false;
            RaycastHit hit = cloth.Raycast(mouseRay, Mathf.Infinity, ref hasHit);

            if (!hasHit)
            {
                m_BrushPos = new Vector3(Mathf.Infinity, Mathf.Infinity, Mathf.Infinity);
                m_BrushNorm = m_BrushPos;
                m_BrushFace = -1; // set invalid face index in case of no hit
                return;
            }

            m_BrushPos = hit.point;
            m_BrushNorm = hit.normal;
            m_BrushFace = hit.triangleIndex;
        }

        void DrawBrush()
        {
            if (m_BrushFace >= 0)
            {
                Handles.color = s_BrushColor;
                Handles.DrawSolidDisc(m_BrushPos, m_BrushNorm, state.BrushRadius);
            }
        }

        internal override Bounds GetWorldBoundsOfTarget(UnityObject targetObject)
        {
            Cloth cloth = (Cloth)targetObject;
            var skin = cloth.GetComponent<SkinnedMeshRenderer>();
            return skin == null ? base.GetWorldBoundsOfTarget(targetObject) : skin.bounds;
        }

        bool SelectionMeshDirty()
        {
            if (m_LastVertices != null)
            {
                int numLastVertices = m_LastVertices.Length;
                if (numLastVertices != m_NumVerts)
                    return true;
                for (int i = 0; i < numLastVertices; i++)
                {
                    // we use !(==) here instead of !=, since Vector3 != is incorrectly written to return false if one component is NaN.
                    if (!(m_LastVertices[i] == m_ClothParticlesInWorldSpace[i]))
                        return true;
                }
            }

            return false;
        }

        void GenerateSelectionMesh()
        {
            if (!IsMeshValid())
                return;

            m_ParticleSelection = new bool[m_NumVerts];
            m_ParticleRectSelection = new bool[m_NumVerts];

            m_LastVertices = new Vector3[m_NumVerts];
            for (int i = 0; i < m_NumVerts; i++)
                m_LastVertices[i] = m_ClothParticlesInWorldSpace[i];
        }

        void InitSelfAndInterCollisionSelection()
        {
            m_SelfAndInterCollisionSelection = new bool[m_NumVerts];
            for (int i = 0; i < m_NumVerts; i++)
            {
                m_SelfAndInterCollisionSelection[i] = false;
            }

            List<UInt32> selfAndInterCollisionIndices = new List<UInt32>(m_NumVerts);
            selfAndInterCollisionIndices.Clear();

            cloth.GetSelfAndInterCollisionIndices(selfAndInterCollisionIndices);

            int length = selfAndInterCollisionIndices.Count;
            for (int i = 0; i < length; i++)
            {
                m_SelfAndInterCollisionSelection[selfAndInterCollisionIndices[i]] = true;
            }
        }

        void InitClothParticlesInWorldSpace()
        {
            Vector3[] vertices = cloth.vertices;
            m_ClothParticlesInWorldSpace = new Vector3[m_NumVerts];

            Quaternion rotation = m_TransformOverride.rotation;
            Vector3 position = m_TransformOverride.position;
            for (int i = 0; i < m_NumVerts; i++)
            {
                m_ClothParticlesInWorldSpace[i] = (rotation * vertices[i]) + position;
            }
        }

        void InitClothNormalsInWorldSpace()
        {
            Vector3[] normals = cloth.normals;
            int length = normals.Length;
            m_ClothNormalsInWorldSpace = new Vector3[length];

            for (int i = 0; i < length; i++)
            {
                m_ClothNormalsInWorldSpace[i] = (m_TransformOverride.localToWorldMatrix * normals[i]).normalized;
            }
        }

        void DrawSelfAndInterCollisionParticles()
        {
            float size = state.SelfCollisionDistance;
            if (state.VisualizeSelfOrInterCollision == CollisionVisualizationMode.SelfCollision)
                size = state.SelfCollisionDistance;
            else if (state.VisualizeSelfOrInterCollision == CollisionVisualizationMode.InterCollision)
                size = state.InterCollisionDistance;

            int length = m_SelfAndInterCollisionSelection.Length;
            for (int i = 0; i < length; i++)
            {
                Vector3 distanceBetween = m_ClothParticlesInWorldSpace[i] - m_BrushPos;
                bool forwardFacing = Vector3.Dot(m_ClothNormalsInWorldSpace[i], SceneView.GetLastActiveSceneViewCamera().transform.forward) <= 0;
                if (forwardFacing || state.ManipulateBackfaces)
                {
                    if (m_SelfAndInterCollisionSelection[i] && !m_ParticleSelection[i])
                    {
                        Handles.color = s_SelfAndInterCollisionParticleColor;
                    }
                    else if (!m_SelfAndInterCollisionSelection[i] && !m_ParticleSelection[i])
                    {
                        Handles.color = s_UnselectedSelfAndInterCollisionParticleColor;
                    }

                    if ((m_ParticleSelection[i] == true) && (m_NumSelection > 0) && (state.CollToolMode == CollToolMode.Select))
                    {
                        Handles.color = s_SelectedParticleColor;
                    }

                    if ((distanceBetween.magnitude < state.BrushRadius) && forwardFacing && ((state.CollToolMode == CollToolMode.Paint) || (state.CollToolMode == CollToolMode.Erase)))
                    {
                        if (m_BrushFace > -1) // check mouse pointer is over a valid mesh face
                        {
                            Handles.color = s_SelectedParticleColor;
                        }
                    }

                    Handles.SphereHandleCap(-1, m_ClothParticlesInWorldSpace[i], m_TransformOverride.rotation, size, EventType.Repaint);
                }
            }
        }

        void InitInspector()
        {
            m_NumVerts = cloth.vertices.Length;

            InitSelfAndInterCollisionSelection();
            InitClothParticlesInWorldSpace();
            InitClothNormalsInWorldSpace();
        }

        void OnEnable()
        {
            if (s_ColorTexture == null)
                s_ColorTexture = GenerateColorTexture(100);

            m_Smr = cloth.GetComponent<SkinnedMeshRenderer>();
            m_TransformOverride = m_Smr.actualRootBone;
            m_CachedMesh = new WeakReference(m_Smr.sharedMesh);
            InitInspector();

            GenerateSelectionMesh();

            m_SelfCollisionDistance = serializedObject.FindProperty("m_SelfCollisionDistance");
            m_SelfCollisionStiffness = serializedObject.FindProperty("m_SelfCollisionStiffness");
        }

        float GetCoefficient(ClothSkinningCoefficient coefficient)
        {
            switch (drawMode)
            {
                case DrawMode.MaxDistance:
                    return coefficient.maxDistance;
                case DrawMode.CollisionSphereDistance:
                    return coefficient.collisionSphereDistance;
            }
            return 0;
        }

        Color GetGradientColor(float val)
        {
            if (val < 0.3f)
                return Color.Lerp(Color.red, Color.magenta, val / 0.2f);
            else if (val < 0.7f)
                return Color.Lerp(Color.magenta, Color.yellow, (val - 0.2f) / 0.5f);
            else
                return Color.Lerp(Color.yellow, Color.green, (val - 0.7f) / 0.3f);
        }

        float CoefficientField(float value, float useValue, bool enabled, DrawMode mode)
        {
            var label = GetDrawModeString(mode);

            using (new EditorGUI.DisabledScope(!enabled))
            {
                GUILayout.BeginHorizontal();
                EditorGUI.showMixedValue = useValue < 0;
                EditorGUI.BeginChangeCheck();
                useValue = EditorGUILayout.Toggle(GUIContent.none, useValue != 0) ? 1 : 0;
                if (EditorGUI.EndChangeCheck())
                {
                    if (useValue > 0)
                        value = 0;
                    else
                        value = kDisabledValue;
                    drawMode = mode;
                }
                GUILayout.Space(-152);
                EditorGUI.showMixedValue = false;

                using (new EditorGUI.DisabledScope(useValue != 1))
                {
                    float fieldValue = value;
                    EditorGUI.showMixedValue = value < 0;
                    EditorGUI.BeginChangeCheck();

                    int oldHotControl = GUIUtility.keyboardControl;
                    if (useValue > 0)
                        fieldValue = EditorGUILayout.FloatField(label, value);
                    else
                        EditorGUILayout.FloatField(label, 0);

                    bool changed = EditorGUI.EndChangeCheck();
                    if (changed)
                    {
                        value = fieldValue;
                        if (value < 0)
                            value = 0;
                    }

                    if (changed || oldHotControl != GUIUtility.keyboardControl)
                        // if we got focus or changed values, set draw mode.
                        drawMode = mode;
                }
            }

            if (useValue > 0)
            {
                float min = m_MinVisualizedValue[(int)mode];
                float max = m_MaxVisualizedValue[(int)mode];
                if (max - min > 0)
                    DrawColorBox(null, GetGradientColor((value - min) / (max - min)));
                else
                    DrawColorBox(null, GetGradientColor(value <= min ? 0 : 1));
            }
            else
                DrawColorBox(null, Color.black);

            EditorGUI.showMixedValue = false;
            GUILayout.EndHorizontal();
            return value;
        }

        float PaintField(float value, ref bool enabled, DrawMode mode)
        {
            var label = GetDrawModeString(mode);
            GUILayout.BeginHorizontal();
            enabled = GUILayout.Toggle(enabled, Styles.paintIcon, "MiniButton", GUILayout.ExpandWidth(false));
            bool useValue;
            float retVal;
            using (new EditorGUI.DisabledScope(!enabled))
            {
                EditorGUI.BeginChangeCheck();
                useValue = EditorGUILayout.Toggle(GUIContent.none, value < kDisabledValue);
                if (EditorGUI.EndChangeCheck())
                {
                    if (useValue)
                        value = 0;
                    else
                        value = kDisabledValue;
                    drawMode = mode;
                }
                GUILayout.Space(-162);

                using (new EditorGUI.DisabledScope(!useValue))
                {
                    retVal = value;
                    int oldHotControl = GUIUtility.keyboardControl;
                    EditorGUI.BeginChangeCheck();
                    if (useValue)
                        retVal = EditorGUILayout.FloatField(label, value);
                    else
                        EditorGUILayout.FloatField(label, 0);

                    if (retVal < 0)
                        retVal = 0;

                    if (EditorGUI.EndChangeCheck() || oldHotControl != GUIUtility.keyboardControl)
                        // if we got focus or changed values, set draw mode.
                        drawMode = mode;
                }
            }

            if (useValue)
            {
                float min = m_MinVisualizedValue[(int)mode];
                float max = m_MaxVisualizedValue[(int)mode];
                if (max - min > 0)
                    DrawColorBox(null, GetGradientColor((value - min) / (max - min)));
                else
                    DrawColorBox(null, GetGradientColor(value <= min ? 0 : 1));
            }
            else
                DrawColorBox(null, Color.black);


            GUILayout.EndHorizontal();
            return retVal;
        }

        void SelectionGUI()
        {
            if (m_ParticleSelection == null)
                return;

            ClothSkinningCoefficient[] coefficients = cloth.coefficients;

            float maxDistance = 0;
            float useMaxDistance = 0;
            float collisionSphereDistance = 0;
            float useCollisionSphereDistance = 0;
            int numSelection = 0;
            bool firstVertex = true;

            int numParticleSelection = m_ParticleSelection.Length;
            for (int i = 0; i < numParticleSelection; i++)
            {
                if (m_ParticleSelection[i])
                {
                    if (firstVertex)
                    {
                        maxDistance = coefficients[i].maxDistance;
                        useMaxDistance = (maxDistance < kDisabledValue) ? 1 : 0;
                        collisionSphereDistance = coefficients[i].collisionSphereDistance;
                        useCollisionSphereDistance = (collisionSphereDistance < kDisabledValue) ? 1 : 0;
                        firstVertex = false;
                    }
                    if (coefficients[i].maxDistance != maxDistance)
                        maxDistance = -1;
                    if (coefficients[i].collisionSphereDistance != collisionSphereDistance)
                        collisionSphereDistance = -1;
                    if (useMaxDistance != ((coefficients[i].maxDistance < kDisabledValue) ? 1 : 0))
                        useMaxDistance = -1;
                    if (useCollisionSphereDistance != ((coefficients[i].collisionSphereDistance < kDisabledValue) ? 1 : 0))
                        useCollisionSphereDistance = -1;
                    numSelection++;
                }
            }

            int numCoefficients = coefficients.Length;
            float maxDistanceNew = CoefficientField(maxDistance, useMaxDistance, numSelection > 0, DrawMode.MaxDistance);
            if (maxDistanceNew != maxDistance)
            {
                for (int i = 0; i < numCoefficients; i++)
                {
                    if (m_ParticleSelection[i])
                        coefficients[i].maxDistance = maxDistanceNew;
                }
                Undo.RegisterCompleteObjectUndo(target, "Change Cloth Coefficients");
                cloth.coefficients = coefficients;
            }

            float collisionSphereDistanceNew = CoefficientField(collisionSphereDistance, useCollisionSphereDistance, numSelection > 0, DrawMode.CollisionSphereDistance);
            if (collisionSphereDistanceNew != collisionSphereDistance)
            {
                for (int i = 0; i < numCoefficients; i++)
                {
                    if (m_ParticleSelection[i])
                        coefficients[i].collisionSphereDistance = collisionSphereDistanceNew;
                }
                Undo.RegisterCompleteObjectUndo(target, "Change Cloth Coefficients");
                cloth.coefficients = coefficients;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                GUILayout.BeginHorizontal();
                if (numSelection > 0)
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(numSelection + " selected");
                }
                else
                {
                    GUILayout.Label("Select cloth vertices to edit their constraints.");
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Backspace)
            {
                for (int i = 0; i < numCoefficients; i++)
                {
                    if (m_ParticleSelection[i])
                    {
                        switch (drawMode)
                        {
                            case DrawMode.MaxDistance:
                                coefficients[i].maxDistance = kDisabledValue;
                                break;
                            case DrawMode.CollisionSphereDistance:
                                coefficients[i].collisionSphereDistance = kDisabledValue;
                                break;
                        }
                    }
                }
                Undo.RegisterCompleteObjectUndo(target, "Change Cloth Coefficients");
                cloth.coefficients = coefficients;
            }

            EditConstraintSize();
        }

        void GradientToolGUI()
        {
            if (m_ParticleSelection == null)
                return;

            ClothSkinningCoefficient[] coefficients = cloth.coefficients;

            int numSelection = 0;
            int numParticleSelection = m_ParticleSelection.Length;
            for (int i = 0; i < numParticleSelection; i++)
            {
                if (m_ParticleSelection[i])
                {
                    numSelection++;
                }
            }

            EditGradientStart();
            EditGradientEnd();

            if (numSelection == 0)
            {
                state.SetMaxDistance = false;
                state.SetCollisionSphereDistance = false;
            }

            var lineLength = Vector3.Distance(m_GradientStartPoint, m_GradientEndPoint);

            using (new EditorGUI.DisabledScope(numSelection == 0))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                bool setMaxDistance = EditorGUILayout.Toggle(GUIContent.none, state.SetMaxDistance);
                if (EditorGUI.EndChangeCheck())
                {
                    state.SetMaxDistance = setMaxDistance;
                    int numCoefficients = coefficients.Length;
                    for (int i = 0; i < numCoefficients; i++)
                    {
                        if (m_ParticleSelection[i])
                        {
                            Vector3 nearestPointToLine = HandleUtility.ProjectPointLine(m_ClothParticlesInWorldSpace[i], m_GradientStartPoint, m_GradientEndPoint);
                            float lerp = Vector3.Distance(nearestPointToLine, m_GradientEndPoint) / lineLength;
                            float distanceNew = Mathf.Lerp(state.GradientStartValue, state.GradientEndValue, 1.0f - lerp);
                            coefficients[i].maxDistance = distanceNew;
                        }
                    }
                    Undo.RegisterCompleteObjectUndo(target, "Change Cloth Coefficients");
                    cloth.coefficients = coefficients;
                }

                EditorGUILayout.LabelField(Styles.setMaxDistanceString);
                EditorGUILayout.EndHorizontal();
            }

            using (new EditorGUI.DisabledScope(numSelection == 0))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                bool setCollisionSphereDistance = EditorGUILayout.Toggle(GUIContent.none, state.SetCollisionSphereDistance);
                if (EditorGUI.EndChangeCheck())
                {
                    state.SetCollisionSphereDistance = setCollisionSphereDistance;
                    int numCoefficients = coefficients.Length;
                    for (int i = 0; i < numCoefficients; i++)
                    {
                        if (m_ParticleSelection[i])
                        {
                            Vector3 nearestPointToLine = HandleUtility.ProjectPointLine(m_ClothParticlesInWorldSpace[i], m_GradientStartPoint, m_GradientEndPoint);
                            float lerp = Vector3.Distance(nearestPointToLine, m_GradientEndPoint) / lineLength;
                            float distanceNew = Mathf.Lerp(state.GradientStartValue, state.GradientEndValue, 1.0f - lerp);
                            coefficients[i].collisionSphereDistance = distanceNew;
                        }
                    }
                    cloth.coefficients = coefficients;
                    Undo.RegisterCompleteObjectUndo(target, "Change Cloth Coefficients");
                }

                EditorGUILayout.LabelField(Styles.setCollisionSphereDistanceString);
                EditorGUILayout.EndHorizontal();
            }

            using (new EditorGUI.DisabledScope(true))
            {
                GUILayout.BeginHorizontal();
                if (numSelection > 0)
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(numSelection + " selected");
                }
                else
                {
                    GUILayout.Label("Select cloth vertices to edit their constraints.");
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
            }

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Backspace)
            {
                int numCoefficients = coefficients.Length;
                for (int i = 0; i < numCoefficients; i++)
                {
                    if (m_ParticleSelection[i])
                    {
                        switch (drawMode)
                        {
                            case DrawMode.MaxDistance:
                                coefficients[i].maxDistance = kDisabledValue;
                                break;
                            case DrawMode.CollisionSphereDistance:
                                coefficients[i].collisionSphereDistance = kDisabledValue;
                                break;
                        }
                    }
                }
                cloth.coefficients = coefficients;
            }
        }

        void CollSelectionGUI()
        {
            if (!IsMeshValid())
                return;

            bool firstFound = false;
            bool mixedValue = false;
            int numSelection = 0;
            int lengthSelection = m_ParticleRectSelection.Length;
            for (int i = 0; i < lengthSelection; i++)
            {
                if (m_ParticleRectSelection[i])
                {
                    if (firstFound == false)
                    {
                        state.SetSelfAndInterCollision = m_SelfAndInterCollisionSelection[i];
                        firstFound = true;
                    }
                    else
                    {
                        if (state.SetSelfAndInterCollision != m_SelfAndInterCollisionSelection[i])
                        {
                            mixedValue = true;
                            state.SetSelfAndInterCollision = false;
                        }
                    }
                    numSelection++;
                }
            }

            m_NumSelection = numSelection;
            if (m_NumSelection == 0)
            {
                state.SetSelfAndInterCollision = false;
            }

            using (new EditorGUI.DisabledScope(m_NumSelection == 0))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.showMixedValue = mixedValue;
                EditorGUI.BeginChangeCheck();
                bool setSelfAndInterCollision = EditorGUILayout.Toggle(GUIContent.none, state.SetSelfAndInterCollision);
                if (EditorGUI.EndChangeCheck())
                {
                    state.SetSelfAndInterCollision = setSelfAndInterCollision;
                    for (int i = 0; i < lengthSelection; i++)
                    {
                        if (m_ParticleRectSelection[i])
                        {
                            m_SelfAndInterCollisionSelection[i] = state.SetSelfAndInterCollision;
                        }
                    }

                    Undo.RegisterCompleteObjectUndo(target, "Change Cloth Particles Selected for self or inter collision");
                }

                EditorGUILayout.LabelField(Styles.setSelfAndInterCollisionString);
                EditorGUI.showMixedValue = false;
                EditorGUILayout.EndHorizontal();
            }
        }

        void EditBrushSize()
        {
            EditorGUI.BeginChangeCheck();
            float fieldValue = EditorGUILayout.FloatField(Styles.brushRadiusString, state.BrushRadius);
            bool changed = EditorGUI.EndChangeCheck();
            if (changed)
            {
                state.BrushRadius = fieldValue;
                if (state.BrushRadius < 0.0f)
                    state.BrushRadius = 0.0f;
            }
        }

        void EditConstraintSize()
        {
            EditorGUI.BeginChangeCheck();
            float fieldValue = EditorGUILayout.FloatField(Styles.constraintSizeString, state.ConstraintSize);
            bool changed = EditorGUI.EndChangeCheck();
            if (changed)
            {
                state.ConstraintSize = fieldValue;
                if (state.ConstraintSize < 0.0f)
                    state.ConstraintSize = 0.0f;
            }
        }

        void EditGradientStart()
        {
            EditorGUI.BeginChangeCheck();
            float fieldValue = EditorGUILayout.FloatField(Styles.gradientStartString, state.GradientStartValue);
            bool changed = EditorGUI.EndChangeCheck();
            if (changed)
            {
                state.GradientStartValue = fieldValue;
                if (state.GradientStartValue < 0.0f)
                    state.GradientStartValue = 0.0f;
            }
        }

        void EditGradientEnd()
        {
            EditorGUI.BeginChangeCheck();
            float fieldValue = EditorGUILayout.FloatField(Styles.gradientEndString, state.GradientEndValue);
            bool changed = EditorGUI.EndChangeCheck();
            if (changed)
            {
                state.GradientEndValue = fieldValue;
                if (state.GradientEndValue < 0.0f)
                    state.GradientEndValue = 0.0f;
            }
        }

        void PaintGUI()
        {
            state.PaintMaxDistance = PaintField(state.PaintMaxDistance, ref state.PaintMaxDistanceEnabled, DrawMode.MaxDistance);

            state.PaintCollisionSphereDistance = PaintField(state.PaintCollisionSphereDistance, ref state.PaintCollisionSphereDistanceEnabled, DrawMode.CollisionSphereDistance);

            // Automatically switch visualization mode if we are only painting a coefficient we are not visualizing
            if (state.PaintMaxDistanceEnabled && !state.PaintCollisionSphereDistanceEnabled)
                drawMode = DrawMode.MaxDistance;
            else if (!state.PaintMaxDistanceEnabled && state.PaintCollisionSphereDistanceEnabled)
                drawMode = DrawMode.CollisionSphereDistance;

            using (new EditorGUI.DisabledScope(true))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Set constraints to paint onto cloth vertices.");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            EditBrushSize();
            EditConstraintSize();
        }

        int GetMouseVertex(Event e)
        {
            // No cloth manipulation Tool enabled -> don't interact with vertices.
            if (Tools.current != Tool.None)
                return -1;

            if (m_LastVertices == null)
            {
                return -1;
            }

            Ray mouseRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            float minDistance = 1000;
            int found = -1;
            for (int i = 0; i < m_NumVerts; i++)
            {
                Vector3 dir = m_LastVertices[i] - mouseRay.origin;
                float sqrDistance = Vector3.Cross(dir, mouseRay.direction).sqrMagnitude;
                bool forwardFacing = Vector3.Dot(m_ClothNormalsInWorldSpace[i], SceneView.GetLastActiveSceneViewCamera().transform.forward) <= 0;
                if ((forwardFacing || state.ManipulateBackfaces) && sqrDistance < minDistance && sqrDistance < 0.05f * 0.05f)
                {
                    minDistance = sqrDistance;
                    found = i;
                }
            }
            return found;
        }

        void DrawConstraints()
        {
            if (SelectionMeshDirty())
                GenerateSelectionMesh();

            ClothSkinningCoefficient[] coefficients = cloth.coefficients;
            int length = coefficients.Length;
            float min = 0;
            float max = 0;
            for (int i = 0; i < length; i++)
            {
                float value = GetCoefficient(coefficients[i]);
                if (value >= kDisabledValue)
                    continue;
                if (value < min)
                    min = value;
                if (value > max)
                    max = value;
            }

            m_MaxVisualizedValue[(int)drawMode] = max;
            m_MinVisualizedValue[(int)drawMode] = min;

            for (int i = 0; i < length; i++)
            {
                bool forwardFacing = Vector3.Dot(m_ClothNormalsInWorldSpace[i], SceneView.GetLastActiveSceneViewCamera().transform.forward) <= 0;
                if (forwardFacing || state.ManipulateBackfaces)
                {
                    float val = GetCoefficient(coefficients[i]);
                    Color color;
                    if (val >= kDisabledValue)
                        color = Color.black;
                    else
                    {
                        if (max - min != 0)
                            val = (val - min) / (max - min);
                        else
                            val = 0.0f;
                        color = GetGradientColor(val);
                    }

                    Handles.color = color;

                    Vector3 distanceBetween = m_ClothParticlesInWorldSpace[i] - m_BrushPos;
                    if ((m_ParticleSelection[i] == true) && (state.CollToolMode == CollToolMode.Select))
                    {
                        Handles.color = s_SelectedParticleColor;
                    }

                    if ((distanceBetween.magnitude < state.BrushRadius) && forwardFacing && (state.ToolMode == ToolMode.Paint))
                    {
                        if (m_BrushFace > -1) // check mouse pointer is over a valid mesh face
                        {
                            Handles.color = s_SelectedParticleColor;
                        }
                    }

                    Handles.SphereHandleCap(-1, m_ClothParticlesInWorldSpace[i], m_TransformOverride.rotation, state.ConstraintSize, EventType.Repaint);
                }
            }
        }

        bool UpdateRectParticleSelection()
        {
            if (!IsMeshValid())
            {
                return false;
            }

            bool selectionChanged = false;

            ClothSkinningCoefficient[] coefficients = cloth.coefficients;

            float minX = Mathf.Min(m_SelectStartPoint.x, m_SelectMousePoint.x);
            float maxX = Mathf.Max(m_SelectStartPoint.x, m_SelectMousePoint.x);
            float minY = Mathf.Min(m_SelectStartPoint.y, m_SelectMousePoint.y);
            float maxY = Mathf.Max(m_SelectStartPoint.y, m_SelectMousePoint.y);

            var topLeftVec = new Vector2(minX, minY);
            var topRightVec = new Vector2(maxX, minY);
            var botLeftVec = new Vector2(minX, maxY);
            var botRightVec = new Vector2(maxX, maxY);

            Ray topLeft = HandleUtility.GUIPointToWorldRay(topLeftVec);
            Ray topRight = HandleUtility.GUIPointToWorldRay(topRightVec);
            Ray botLeft = HandleUtility.GUIPointToWorldRay(botLeftVec);
            Ray botRight = HandleUtility.GUIPointToWorldRay(botRightVec);

            Plane top = new Plane(topRight.origin + topRight.direction, topLeft.origin + topLeft.direction, topLeft.origin);
            Plane bottom = new Plane(botLeft.origin + botLeft.direction, botRight.origin + botRight.direction, botRight.origin);
            Plane left = new Plane(topLeft.origin + topLeft.direction, botLeft.origin + botLeft.direction, botLeft.origin);
            Plane right = new Plane(botRight.origin + botRight.direction, topRight.origin + topRight.direction, topRight.origin);

            int length = coefficients.Length;
            for (int i = 0; i < length; i++)
            {
                Vector3 v = m_LastVertices[i];
                bool forwardFacing = Vector3.Dot(m_ClothNormalsInWorldSpace[i], SceneView.GetLastActiveSceneViewCamera().transform.forward) <= 0;
                bool selected = top.GetSide(v) && bottom.GetSide(v) && left.GetSide(v) && right.GetSide(v);
                selected = selected && (state.ManipulateBackfaces || forwardFacing);
                if (m_ParticleRectSelection[i] != selected)
                {
                    m_ParticleRectSelection[i] = selected;
                    selectionChanged = true;
                }
            }

            var curCamm = SceneView.GetLastActiveSceneViewCamera();
            bool wasOrtho = curCamm.orthographic;
            curCamm.orthographic = true;
            topLeft = HandleUtility.GUIPointToWorldRay(topLeftVec);
            topRight = HandleUtility.GUIPointToWorldRay(topRightVec);
            botLeft = HandleUtility.GUIPointToWorldRay(botLeftVec);
            botRight = HandleUtility.GUIPointToWorldRay(botRightVec);
            curCamm.orthographic = wasOrtho;
            m_GradientStartPoint = (topLeft.origin + botLeft.origin) * 0.5f;
            m_GradientEndPoint = (topRight.origin + botRight.origin) * 0.5f;

            return selectionChanged;
        }

        void ApplyRectSelection()
        {
            if (!IsMeshValid())
            {
                return;
            }

            int length = cloth.coefficients.Length;
            for (int i = 0; i < length; i++)
            {
                switch (m_RectSelectionMode)
                {
                    case RectSelectionMode.Replace:
                        m_ParticleSelection[i] = m_ParticleRectSelection[i];
                        break;

                    case RectSelectionMode.Add:
                        m_ParticleSelection[i] |= m_ParticleRectSelection[i];
                        break;

                    case RectSelectionMode.Substract:
                        m_ParticleSelection[i] = m_ParticleSelection[i] && !m_ParticleRectSelection[i];
                        break;
                }
            }
        }

        bool RectSelectionModeFromEvent()
        {
            Event e = Event.current;
            RectSelectionMode mode = RectSelectionMode.Replace;
            if (e.shift)
                mode = RectSelectionMode.Add;
            if (e.alt)
                mode = RectSelectionMode.Substract;
            if (m_RectSelectionMode != mode)
            {
                m_RectSelectionMode = mode;
                return true;
            }
            return false;
        }

        internal void SendCommandsOnModifierKeys()
        {
            SceneView.lastActiveSceneView.SendEvent(EditorGUIUtility.CommandEvent(EventCommandNames.ModifierKeysChanged));
        }

        void SelectionPreSceneGUI(int id)
        {
            if (m_ParticleSelection == null)
                return;

            Event e = Event.current;
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (HandleUtility.nearestControl != id || e.alt || e.control || e.command || e.button != 0)
                        break;
                    GUIUtility.hotControl = id;
                    int found = GetMouseVertex(e);
                    if (found != -1)
                    {
                        if (e.shift)
                        {
                            m_ParticleSelection[found] = !m_ParticleSelection[found];
                        }
                        else
                        {
                            int length = m_ParticleSelection.Length;
                            for (int i = 0; i < length; i++)
                                m_ParticleSelection[i] = false;
                            m_ParticleSelection[found] = true;
                        }
                        m_DidSelect = true;
                        Repaint();
                    }
                    else
                        m_DidSelect = false;

                    m_SelectStartPoint = e.mousePosition;
                    e.Use();
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        if (!m_RectSelecting && (e.mousePosition - m_SelectStartPoint).magnitude > 2f)
                        {
                            if (!(e.alt || e.control || e.command))
                            {
                                EditorApplication.modifierKeysChanged += SendCommandsOnModifierKeys;
                                m_RectSelecting = true;
                                RectSelectionModeFromEvent();
                            }
                        }
                        if (m_RectSelecting)
                        {
                            m_SelectMousePoint = new Vector2(Mathf.Max(e.mousePosition.x, 0), Mathf.Max(e.mousePosition.y, 0));
                            RectSelectionModeFromEvent();
                            UpdateRectParticleSelection();
                            e.Use();
                        }
                    }
                    break;

                case EventType.ExecuteCommand:
                    if (m_RectSelecting && e.commandName == EventCommandNames.ModifierKeysChanged)
                    {
                        RectSelectionModeFromEvent();
                        UpdateRectParticleSelection();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && e.button == 0)
                    {
                        GUIUtility.hotControl = 0;

                        if (m_RectSelecting)
                        {
                            EditorApplication.modifierKeysChanged -= SendCommandsOnModifierKeys;
                            m_RectSelecting = false;
                            RectSelectionModeFromEvent();
                            ApplyRectSelection();
                        }
                        else if (!m_DidSelect)
                        {
                            if (!(e.alt || e.control || e.command))
                            {
                                // If nothing was clicked, deselect all
                                ClothSkinningCoefficient[] coefficients = cloth.coefficients;
                                int length = coefficients.Length;
                                for (int i = 0; i < length; i++)
                                    m_ParticleSelection[i] = false;
                            }
                        }
                        // Disable text focus when selection changes, otherwise we cannot update inspector fields
                        // if text is currently selected.
                        GUIUtility.keyboardControl = 0;
                        SceneView.RepaintAll();
                    }
                    break;
            }
        }

        void GetBrushedConstraints(Event e)
        {
            if (!IsMeshValid())
            {
                return;
            }

            ClothSkinningCoefficient[] coefficients = cloth.coefficients;
            for (int i = 0; i < m_NumVerts; i++)
            {
                Vector3 distanceBetween = m_ClothParticlesInWorldSpace[i] - m_BrushPos;
                bool forwardFacing = Vector3.Dot(m_ClothNormalsInWorldSpace[i], SceneView.GetLastActiveSceneViewCamera().transform.forward) <= 0;
                if ((distanceBetween.magnitude < state.BrushRadius) && (forwardFacing || state.ManipulateBackfaces))
                {
                    bool changed = false;
                    if (state.PaintMaxDistanceEnabled && coefficients[i].maxDistance != state.PaintMaxDistance)
                    {
                        coefficients[i].maxDistance = state.PaintMaxDistance;
                        changed = true;
                    }
                    if (state.PaintCollisionSphereDistanceEnabled && coefficients[i].collisionSphereDistance != state.PaintCollisionSphereDistance)
                    {
                        coefficients[i].collisionSphereDistance = state.PaintCollisionSphereDistance;
                        changed = true;
                    }
                    if (changed)
                    {
                        Undo.RegisterCompleteObjectUndo(target, "Paint Cloth Constraints");
                        cloth.coefficients = coefficients;
                        Repaint();
                    }
                }
            }
        }

        void GetBrushedParticles(Event e)
        {
            if (!IsMeshValid())
            {
                return;
            }

            Quaternion rotation = m_TransformOverride.rotation;
            for (int i = 0; i < m_NumVerts; i++)
            {
                Vector3 distanceBetween = m_ClothParticlesInWorldSpace[i] - m_BrushPos;
                bool forwardFacing = Vector3.Dot(m_ClothNormalsInWorldSpace[i], SceneView.GetLastActiveSceneViewCamera().transform.forward) <= 0;
                if ((distanceBetween.magnitude < state.BrushRadius) && (forwardFacing || state.ManipulateBackfaces))
                {
                    if (e.button == 0)
                    {
                        if (state.CollToolMode == CollToolMode.Paint)
                        {
                            m_SelfAndInterCollisionSelection[i] = true;
                        }
                        else if (state.CollToolMode == CollToolMode.Erase)
                        {
                            m_SelfAndInterCollisionSelection[i] = false;
                        }
                    }

                    float size = cloth.selfCollisionDistance;
                    if (state.VisualizeSelfOrInterCollision == CollisionVisualizationMode.SelfCollision)
                    {
                        size = cloth.selfCollisionDistance;
                    }
                    else if (state.VisualizeSelfOrInterCollision == CollisionVisualizationMode.InterCollision)
                    {
                        size = Physics.interCollisionDistance;
                    }

                    Handles.color = s_SelectedParticleColor;
                    if (Event.current.type == EventType.Repaint)
                        Handles.SphereHandleCap(-1, m_ClothParticlesInWorldSpace[i], rotation, size, EventType.Repaint);
                    Repaint();
                }
            }

            Undo.RegisterCompleteObjectUndo(target, "Paint Collision");
        }

        void PaintPreSceneGUI(int id)
        {
            if (!IsMeshValid())
                return;

            Event e = Event.current;
            EventType type = e.GetTypeForControl(id);

            if (type == EventType.MouseDown || type == EventType.MouseDrag)
            {
                if (GUIUtility.hotControl != id && (e.alt || e.control || e.command || e.button != 0))
                    return;

                if (HandleUtility.nearestControl != id)
                    return;

                if (type == EventType.MouseDown)
                    GUIUtility.hotControl = id;

                if (editingSelfAndInterCollisionParticles)
                    GetBrushedParticles(e);
                else if (editingConstraints)
                    GetBrushedConstraints(e);

                e.Use();
            }
            else if (type == EventType.MouseUp)
            {
                if (GUIUtility.hotControl == id && e.button == 0)
                {
                    GUIUtility.hotControl = 0;
                    e.Use();
                }
            }
        }

        void GradientToolPreScenGUI(int id)
        {
            Event e = Event.current;

            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (HandleUtility.nearestControl != id || e.alt || e.control || e.command || e.button != 0)
                        break;
                    GUIUtility.hotControl = id;
                    int found = GetMouseVertex(e);
                    if (found != -1)
                    {
                        if (e.shift)
                            m_ParticleSelection[found] = !m_ParticleSelection[found];
                        else
                        {
                            int length = m_ParticleSelection.Length;
                            for (int i = 0; i < length; i++)
                                m_ParticleSelection[i] = false;
                            m_ParticleSelection[found] = true;
                        }
                        m_DidSelect = true;
                        Repaint();
                    }
                    else
                        m_DidSelect = false;

                    m_SelectStartPoint = e.mousePosition;
                    e.Use();
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        if (!m_RectSelecting && (e.mousePosition - m_SelectStartPoint).magnitude > 2f)
                        {
                            if (!(e.alt || e.control || e.command))
                            {
                                EditorApplication.modifierKeysChanged += SendCommandsOnModifierKeys;
                                m_RectSelecting = true;
                                RectSelectionModeFromEvent();
                            }
                        }
                        if (m_RectSelecting)
                        {
                            m_SelectMousePoint = new Vector2(Mathf.Max(e.mousePosition.x, 0), Mathf.Max(e.mousePosition.y, 0));
                            RectSelectionModeFromEvent();
                            UpdateRectParticleSelection();
                            e.Use();
                        }
                    }
                    break;

                case EventType.ExecuteCommand:
                    if (m_RectSelecting && e.commandName == EventCommandNames.ModifierKeysChanged)
                    {
                        RectSelectionModeFromEvent();
                        UpdateRectParticleSelection();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && e.button == 0)
                    {
                        GUIUtility.hotControl = 0;

                        if (m_RectSelecting)
                        {
                            EditorApplication.modifierKeysChanged -= SendCommandsOnModifierKeys;
                            m_RectSelecting = false;
                            RectSelectionModeFromEvent();
                            ApplyRectSelection();
                        }
                        else if (!m_DidSelect)
                        {
                            if (!(e.alt || e.control || e.command))
                            {
                                // If nothing was clicked, deselect all
                                ClothSkinningCoefficient[] coefficients = cloth.coefficients;
                                int length = coefficients.Length;
                                for (int i = 0; i < length; i++)
                                    m_ParticleSelection[i] = false;
                            }
                        }
                        // Disable text focus when selection changes, otherwise we cannot update inspector fields
                        // if text is currently selected.
                        GUIUtility.keyboardControl = 0;
                        SceneView.RepaintAll();
                    }
                    break;
            }
        }

        void DoOnPreSceneGUI()
        {
            if (!IsMeshValid())
                return;

            if (state.ToolMode == (ToolMode)(-1))
                state.ToolMode = ToolMode.Select;

            if (m_ParticleSelection == null || m_LastVertices.Length != cloth.vertices.Length)
            {
                GenerateSelectionMesh();
            }
            else
            {
                ClothSkinningCoefficient[] coefficients = cloth.coefficients;
                if (m_ParticleSelection.Length != coefficients.Length)
                    InitInspector();
            }

            m_ClothToolControlID = GUIUtility.GetControlID(FocusType.Passive);

            Handles.BeginGUI();

            Event e = Event.current;

            switch (e.GetTypeForControl(m_ClothToolControlID))
            {
                case EventType.Layout:
                    HandleUtility.AddDefaultControl(m_ClothToolControlID);
                    break;

                case EventType.MouseMove:
                case EventType.MouseDrag:
                    GetMouseVertex(e);
                    SceneView.RepaintAll();
                    break;
            }

            if (editingConstraints)
            {
                switch (state.ToolMode)
                {
                    case ToolMode.Select:
                        SelectionPreSceneGUI(m_ClothToolControlID);
                        break;

                    case ToolMode.Paint:
                        PaintPreSceneGUI(m_ClothToolControlID);
                        break;

                    case ToolMode.GradientTool:
                        GradientToolPreScenGUI(m_ClothToolControlID);
                        break;
                }
            }

            if (editingSelfAndInterCollisionParticles)
            {
                switch (state.CollToolMode)
                {
                    case CollToolMode.Select:
                        SelectionPreSceneGUI(m_ClothToolControlID);
                        break;

                    case CollToolMode.Paint:
                    case CollToolMode.Erase:
                        PaintPreSceneGUI(m_ClothToolControlID);
                        break;
                }
            }

            Handles.EndGUI();
        }

        public void OnSceneGUI()
        {
            if (!editingConstraints && !editingSelfAndInterCollisionParticles)
                return;

            DoOnPreSceneGUI();

            if (editingConstraints)
                OnSceneEditConstraintsGUI();
            else if (editingSelfAndInterCollisionParticles)
                OnSceneEditSelfAndInterCollisionParticlesGUI();
        }

        void OnSceneEditConstraintsGUI()
        {
            if ((Event.current.type == EventType.Repaint) && (state.ToolMode == ToolMode.Paint))
            {
                UpdatePreviewBrush();
                DrawBrush();
            }

            // Multi-editing in scene not supported
            if (Selection.gameObjects.Length > 1)
                return;

            if (!IsMeshValid())
                return;

            s_Inspector = this;

            if (Event.current.type == EventType.Repaint)
                DrawConstraints();

            var evt = Event.current;
            if (evt.commandName == EventCommandNames.SelectAll)
            {
                if (evt.type == EventType.ValidateCommand)
                    evt.Use();

                if (evt.type == EventType.ExecuteCommand)
                {
                    for (int i = 0; i < m_NumVerts; i++)
                        m_ParticleSelection[i] = true;
                    SceneView.RepaintAll();
                    state.ToolMode = ToolMode.Select;
                    evt.Use();
                }
            }

            Handles.BeginGUI();
            if (m_RectSelecting && (state.ToolMode == ToolMode.Select || state.ToolMode == ToolMode.GradientTool) &&
                Event.current.type == EventType.Repaint)
                EditorStyles.selectionRect.Draw(EditorGUIExt.FromToRect(m_SelectStartPoint, m_SelectMousePoint),
                    GUIContent.none, false, false, false, false);
            Handles.EndGUI();
        }

        void OnSceneEditSelfAndInterCollisionParticlesGUI()
        {
            // Multi-editing in scene not supported
            if (Selection.gameObjects.Length > 1)
                return;

            if (!IsMeshValid())
                return;

            s_Inspector = this;
            Event evt = Event.current;

            if (evt.type == EventType.Repaint)
                DrawSelfAndInterCollisionParticles();

            if (evt.type == EventType.Repaint && (state.CollToolMode == CollToolMode.Paint || state.CollToolMode == CollToolMode.Erase))
            {
                UpdatePreviewBrush();
                DrawBrush();
            }

            Handles.BeginGUI();
            if (m_RectSelecting && state.CollToolMode == CollToolMode.Select && evt.type == EventType.Repaint)
                EditorStyles.selectionRect.Draw(EditorGUIExt.FromToRect(m_SelectStartPoint, m_SelectMousePoint), GUIContent.none, false, false, false, false);
            Handles.EndGUI();
        }

        public void VisualizationMenuSetMaxDistanceMode()
        {
            drawMode = DrawMode.MaxDistance;
            if (!state.PaintMaxDistanceEnabled)
            {
                state.PaintCollisionSphereDistanceEnabled = false;
                state.PaintMaxDistanceEnabled = true;
            }
        }

        public void VisualizationMenuSetCollisionSphereMode()
        {
            drawMode = DrawMode.CollisionSphereDistance;
            if (!state.PaintCollisionSphereDistanceEnabled)
            {
                state.PaintCollisionSphereDistanceEnabled = true;
                state.PaintMaxDistanceEnabled = false;
            }
        }

        public void VisualizationMenuToggleManipulateBackfaces()
        {
            state.ManipulateBackfaces = !state.ManipulateBackfaces;
        }

        public void VisualizationMenuSelfCollision()
        {
            state.VisualizeSelfOrInterCollision = CollisionVisualizationMode.SelfCollision;
        }

        public void VisualizationMenuInterCollision()
        {
            state.VisualizeSelfOrInterCollision = CollisionVisualizationMode.InterCollision;
        }

        public void DrawColorBox(Texture gradientTex, Color col)
        {
            if (!GUI.enabled)
            {
                col = new Color(0.3f, 0.3f, 0.3f, 1.0f);
                EditorGUI.showMixedValue = false;
            }
            GUILayout.BeginVertical();
            GUILayout.Space(5);
            Rect r = GUILayoutUtility.GetRect(new GUIContent(), GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.Height(10));
            GUI.Box(r, GUIContent.none);
            r = new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2);
            if (gradientTex)
                GUI.DrawTexture(r, gradientTex);
            else
                EditorGUIUtility.DrawColorSwatch(r, col, false);
            GUILayout.EndVertical();
        }

        bool IsConstrained()
        {
            ClothSkinningCoefficient[] coefficients = cloth.coefficients;
            foreach (var c in coefficients)
            {
                if (c.maxDistance < kDisabledValue)
                    return true;
                if (c.collisionSphereDistance < kDisabledValue)
                    return true;
            }
            return false;
        }

        void ConstraintEditing()
        {
            GUILayout.BeginVertical(GUILayout.Width(Styles.clothEditorWindowWidth));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Visualization ", GUILayout.ExpandWidth(false));
            GUILayout.BeginVertical();
            if (EditorGUILayout.DropdownButton(GetDrawModeString(drawMode), FocusType.Passive, EditorStyles.toolbarDropDown))
            {
                Rect buttonRect = GUILayoutUtility.topLevel.GetLast();
                GenericMenu menu = new GenericMenu();
                menu.AddItem(GetDrawModeString(DrawMode.MaxDistance), drawMode == DrawMode.MaxDistance , VisualizationMenuSetMaxDistanceMode);
                menu.AddItem(GetDrawModeString(DrawMode.CollisionSphereDistance), drawMode == DrawMode.CollisionSphereDistance , VisualizationMenuSetCollisionSphereMode);
                menu.AddSeparator("");
                menu.AddItem(EditorGUIUtility.TrTextContent("Manipulate Backfaces"), state.ManipulateBackfaces , VisualizationMenuToggleManipulateBackfaces);
                menu.DropDown(buttonRect);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(m_MinVisualizedValue[(int)drawMode].ToString(), GUILayout.ExpandWidth(false));
            DrawColorBox(s_ColorTexture, Color.clear);
            GUILayout.Label(m_MaxVisualizedValue[(int)drawMode].ToString(), GUILayout.ExpandWidth(false));

            GUILayout.Label("Unconstrained ");
            GUILayout.Space(-24);
            GUILayout.BeginHorizontal(GUILayout.Width(20));
            DrawColorBox(null, Color.black);
            GUILayout.EndHorizontal();

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (Tools.current != Tool.None)
                state.ToolMode = (ToolMode)(-1);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            state.ToolMode = (ToolMode)GUILayout.Toolbar((int)state.ToolMode, Styles.toolIcons);
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                if (check.changed)
                {
                    // delselect text, so we don't end up having a text field highlighted in the new tab
                    GUIUtility.keyboardControl = 0;
                    SceneView.RepaintAll();
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (cloth != null)
            {
                switch (state.ToolMode)
                {
                    case ToolMode.Select:
                        Tools.current = Tool.None;
                        SelectionGUI();
                        break;

                    case ToolMode.Paint:
                        Tools.current = Tool.None;
                        PaintGUI();
                        break;

                    case ToolMode.GradientTool:
                        Tools.current = Tool.None;
                        GradientToolGUI();
                        break;
                }

                if (m_CachedMesh.Target == null)
                {
                    EditorGUILayout.HelpBox("No mesh has been selected to use with cloth, please select a mesh for the skinned mesh renderer.", MessageType.Info);
                }
                else if (!IsConstrained())
                {
                    EditorGUILayout.HelpBox("No constraints have been set up, so the cloth will move freely. Set up vertex constraints here to restrict it.", MessageType.Info);
                }
            }

            GUILayout.EndVertical();
        }

        void SelectManipulateBackFaces()
        {
            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            bool manipulateBackfaces = EditorGUILayout.Toggle(GUIContent.none, state.ManipulateBackfaces);
            if (EditorGUI.EndChangeCheck())
            {
                state.ManipulateBackfaces = manipulateBackfaces;
            }
            EditorGUILayout.LabelField(Styles.manipulateBackFaceString);
            GUILayout.EndHorizontal();
        }

        void ResetParticleSelection()
        {
            if (m_ParticleRectSelection == null)
            {
                return;
            }

            int lengthSelection = m_ParticleRectSelection.Length;
            for (int i = 0; i < lengthSelection; i++)
            {
                m_ParticleRectSelection[i] = false;
                m_ParticleSelection[i] = false;
            }
        }

        void SelfAndInterCollisionEditing()
        {
            GUILayout.BeginVertical(GUILayout.Width(Styles.clothEditorWindowWidth));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Visualization ", GUILayout.ExpandWidth(false));
            GUILayout.BeginVertical();
            if (EditorGUILayout.DropdownButton(GetCollVisModeString(state.VisualizeSelfOrInterCollision), FocusType.Passive, EditorStyles.toolbarDropDown))
            {
                Rect buttonRect = GUILayoutUtility.topLevel.GetLast();
                GenericMenu menu = new GenericMenu();
                menu.AddItem(GetCollVisModeString(CollisionVisualizationMode.SelfCollision), state.VisualizeSelfOrInterCollision == CollisionVisualizationMode.SelfCollision, VisualizationMenuSelfCollision);
                menu.AddItem(GetCollVisModeString(CollisionVisualizationMode.InterCollision), state.VisualizeSelfOrInterCollision == CollisionVisualizationMode.InterCollision, VisualizationMenuInterCollision);
                menu.DropDown(buttonRect);
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (Tools.current != Tool.None)
                state.ToolMode = (ToolMode)(-1);

            CollToolMode oldCollToolMode = state.CollToolMode;
            state.CollToolMode = (CollToolMode)GUILayout.Toolbar((int)state.CollToolMode, Styles.collToolModeIcons);
            if (state.CollToolMode != oldCollToolMode)
            {
                // delselect text, so we don't end up having a text field highlighted in the new tab
                GUIUtility.keyboardControl = 0;
                SceneView.RepaintAll();
            }

            if (cloth != null)
            {
                switch (state.CollToolMode)
                {
                    case CollToolMode.Select:
                        Tools.current = Tool.None;
                        CollSelectionGUI();
                        break;

                    case CollToolMode.Paint:
                    case CollToolMode.Erase:
                        Tools.current = Tool.None;
                        ResetParticleSelection();
                        EditBrushSize();
                        break;
                }

                SelectManipulateBackFaces();
                int countIndices = 0;
                int length = m_SelfAndInterCollisionSelection.Length;

                for (int i = 0; i < length; i++)
                {
                    if (m_SelfAndInterCollisionSelection[i] == true)
                    {
                        countIndices++;
                    }
                }

                List<UInt32> selfAndInterCollisionIndices = new List<UInt32>();
                if (countIndices > 0)
                {
                    selfAndInterCollisionIndices.Capacity = countIndices;
                    for (uint i = 0; i < length; ++i)
                    {
                        if (m_SelfAndInterCollisionSelection[i] == true)
                        {
                            selfAndInterCollisionIndices.Add(i);
                        }
                    }
                }

                Undo.RecordObject(cloth, "SetSelfAndInterCollisionIndices");
                cloth.SetSelfAndInterCollisionIndices(selfAndInterCollisionIndices);

                if (m_CachedMesh.Target == null)
                {
                    EditorGUILayout.HelpBox("No mesh has been selected to use with cloth, please select a mesh for the skinned mesh renderer.", MessageType.Info);
                }
            }

            GUILayout.EndVertical();
        }

        [Overlay(typeof(SceneView), "Scene View/Cloth Constraints", "Cloth Constraints", "unity-sceneview-clothconstraints", priority = (int)OverlayPriority.ClothCollisions, defaultDisplay = false, defaultDockIndex = 0)]
        [Icon("Icons/editconstraints_16.png")]
        class SceneViewClothConstraintsOverlay : TransientSceneViewOverlay
        {
            public override bool visible
            {
                get { return s_Inspector != null && s_Inspector.editingConstraints; }
            }

            public override void OnGUI()
            {
                if(s_Inspector != null)
                    s_Inspector.ConstraintEditing();
            }
        }

        [Overlay(typeof(SceneView), "Scene View/Cloth Collisions", "Cloth Self-Collision and Inter-Collision", "unity-sceneview-clothcollision",  priority = (int)OverlayPriority.ClothCollisions, defaultDisplay = false, defaultDockIndex = 0)]
        [Icon("Icons/editCollision_16.png")]
        class SceneViewClothCollisionsOverlay : TransientSceneViewOverlay
        {
            public override bool visible
            {
                get { return s_Inspector != null && s_Inspector.editingSelfAndInterCollisionParticles; }
            }

            public override void OnGUI()
            {
                if(s_Inspector != null)
                    s_Inspector.SelfAndInterCollisionEditing();
            }
        }
    }
}

