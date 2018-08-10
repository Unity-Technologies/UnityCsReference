// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditorInternal;
using UnityEditor.IMGUI.Controls;

using UnityObject = UnityEngine.Object;


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
    }

    [CustomEditor(typeof(Cloth))]
    [CanEditMultipleObjects]
    class ClothInspector : Editor
    {
        public enum DrawMode { MaxDistance = 1, CollisionSphereDistance };
        public enum ToolMode { Select, Paint };
        public enum CollToolMode { Select, Paint, Erase };
        enum RectSelectionMode { Replace, Add, Substract };
        public enum CollisionVisualizationMode { SelfCollision, InterCollision };

        bool[] m_ParticleSelection;
        bool[] m_ParticleRectSelection;
        bool[] m_SelfAndInterCollisionSelection;

        Vector3[] m_ClothParticlesInWorldSpace;

        Vector3 m_BrushPos;
        Vector3 m_BrushNorm;
        int m_BrushFace = -1;

        int m_MouseOver = -1;
        Vector3[] m_LastVertices;
        Vector2 m_SelectStartPoint;
        Vector2 m_SelectMousePoint;
        bool m_RectSelecting = false;
        bool m_DidSelect = false;
        float[] m_MaxVisualizedValue = new float[3];
        float[] m_MinVisualizedValue = new float[3];
        RectSelectionMode m_RectSelectionMode = RectSelectionMode.Add;
        int m_NumVerts = 0;

        const float kDisabledValue = float.MaxValue;

        static Texture2D s_ColorTexture = null;
        static bool s_BrushCreated = false;

        public static PrefColor s_BrushColor = new PrefColor("Cloth/Brush Color 2", 0.0f / 255.0f, 0.0f / 255.0f, 0.0f / 255.0f, 51.0f / 255.0f);
        public static PrefColor s_SelfAndInterCollisionParticleColor = new PrefColor("Cloth/Self or Inter Collision Particle Color 2", 145.0f / 255.0f, 244.0f / 255.0f, 139.0f / 255.0f, 0.5f);
        public static PrefColor s_UnselectedSelfAndInterCollisionParticleColor = new PrefColor("Cloth/Unselected Self or Inter Collision Particle Color 2", 0.1f, 0.1f, 0.1f, 0.5f);
        public static PrefColor s_SelectedParticleColor = new PrefColor("Cloth/Selected Self or Inter Collision Particle Color 2", 64.0f / 255.0f, 160.0f / 255.0f, 255.0f / 255.0f, 0.5f);

        public static ToolMode[] s_ToolMode =
        {
            ToolMode.Paint,
            ToolMode.Select
        };

        SerializedProperty m_SelfCollisionDistance;
        SerializedProperty m_SelfCollisionStiffness;

        int m_NumSelection = 0;

        SkinnedMeshRenderer m_SkinnedMeshRenderer;

        private static class Styles
        {
            public static readonly GUIContent editConstraintsLabel = EditorGUIUtility.TextContent("Edit Constraints");
            public static readonly GUIContent editSelfInterCollisionLabel = EditorGUIUtility.TextContent("Edit Collision Particles");
            public static readonly GUIContent selfInterCollisionParticleColor = EditorGUIUtility.TextContent("Visualization Color");
            public static readonly GUIContent selfInterCollisionBrushColor = EditorGUIUtility.TextContent("Brush Color");
            public static readonly GUIContent clothSelfCollisionAndInterCollision = EditorGUIUtility.TextContent("Cloth Self-Collision And Inter-Collision");
            public static readonly GUIContent paintCollisionParticles = EditorGUIUtility.TextContent("Paint Collision Particles");
            public static readonly GUIContent selectCollisionParticles = EditorGUIUtility.TextContent("Select Collision Particles");
            public static readonly GUIContent brushRadiusString = EditorGUIUtility.TextContent("Brush Radius");
            public static readonly GUIContent selfAndInterCollisionMode = EditorGUIUtility.TextContent("Paint or Select Particles");
            public static readonly GUIContent backFaceManipulationMode = EditorGUIUtility.TextContent("Back Face Manipulation");
            public static readonly GUIContent manipulateBackFaceString = EditorGUIUtility.TextContent("Manipulate Backfaces");
            public static readonly GUIContent selfCollisionString = EditorGUIUtility.TextContent("Self Collision");
            public static readonly GUIContent setSelfAndInterCollisionString = EditorGUIUtility.TextContent("Self-Collision and Inter-Collision");

            public static readonly int clothEditorWindowWidth = 300;

            public static GUIContent[] toolContents =
            {
                EditorGUIUtility.IconContent("EditCollider"),
                EditorGUIUtility.IconContent("EditCollider")
            };

            public static GUIContent[] toolIcons =
            {
                EditorGUIUtility.TextContent("Select"),
                EditorGUIUtility.TextContent("Paint")
            };

            public static GUIContent[] drawModeStrings =
            {
                EditorGUIUtility.TextContent("Fixed"),
                EditorGUIUtility.TextContent("Max Distance"),
                EditorGUIUtility.TextContent("Surface Penetration")
            };

            public static GUIContent[] toolModeStrings =
            {
                EditorGUIUtility.TextContent("Select"),
                EditorGUIUtility.TextContent("Paint"),
                EditorGUIUtility.TextContent("Erase")
            };

            public static GUIContent[] collToolModeIcons =
            {
                EditorGUIUtility.TextContent("Select"),
                EditorGUIUtility.TextContent("Paint"),
                EditorGUIUtility.TextContent("Erase")
            };

            public static GUIContent[] collVisModeStrings =
            {
                EditorGUIUtility.TextContent("Self Collision"),
                EditorGUIUtility.TextContent("Inter Collision"),
            };

            public static GUIContent paintIcon = EditorGUIUtility.IconContent("ClothInspector.PaintValue", "Change this vertex coefficient value by painting in the scene view.");

            public static EditMode.SceneViewEditMode[] sceneViewEditModes = new[]
            {
                EditMode.SceneViewEditMode.ClothConstraints,
                EditMode.SceneViewEditMode.ClothSelfAndInterCollisionParticles
            };

            public static GUIContent selfCollisionDistanceGUIContent = EditorGUIUtility.TextContent("Self Collision Distance");
            public static GUIContent selfCollisionStiffnessGUIContent = EditorGUIUtility.TextContent("Self Collision Stiffness");

            static Styles()
            {
                toolContents[0].tooltip = EditorGUIUtility.TextContent("Edit cloth constraints").text;
                toolContents[1].tooltip = EditorGUIUtility.TextContent("Edit cloth self or inter collision").text;

                toolIcons[0].tooltip = EditorGUIUtility.TextContent("Select cloth particles for use in self or inter collision").text;
                toolIcons[1].tooltip = EditorGUIUtility.TextContent("Paint cloth particles for use in self or inter collision").text;

                collToolModeIcons[0].tooltip = EditorGUIUtility.TextContent("Select cloth particles.").text;
                collToolModeIcons[1].tooltip = EditorGUIUtility.TextContent("Paint cloth particles.").text;
                collToolModeIcons[2].tooltip = EditorGUIUtility.TextContent("Erase cloth particles.").text;
            }
        }

        ClothInspectorState state
        {
            get
            {
                return ClothInspectorState.instance;
            }
        }

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

        Cloth cloth
        {
            get
            {
                return (Cloth)target;
            }
        }

        public bool editingConstraints
        {
            get { return EditMode.editMode == EditMode.SceneViewEditMode.ClothConstraints && EditMode.IsOwner(this); }
        }

        public bool editingSelfAndInterCollisionParticles
        {
            get { return EditMode.editMode == EditMode.SceneViewEditMode.ClothSelfAndInterCollisionParticles && EditMode.IsOwner(this); }
        }

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
            if (targets.Length <= 1)
            {
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
                    if (cloth.selfCollisionDistance > 0.0f)
                    {
                        state.SelfCollisionDistance = cloth.selfCollisionDistance;
                        m_SelfCollisionDistance.floatValue = cloth.selfCollisionDistance;
                    }
                    else
                    {
                        cloth.selfCollisionDistance = state.SelfCollisionDistance;
                        m_SelfCollisionDistance.floatValue = state.SelfCollisionDistance;
                    }

                    if (cloth.selfCollisionStiffness > 0.0f)
                    {
                        state.SelfCollisionStiffness = cloth.selfCollisionStiffness;
                        m_SelfCollisionStiffness.floatValue = cloth.selfCollisionStiffness;
                    }
                    else
                    {
                        cloth.selfCollisionStiffness = state.SelfCollisionStiffness;
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

                if (Physics.interCollisionDistance > 0.0f)
                {
                    state.InterCollisionDistance = Physics.interCollisionDistance;
                }
                else
                {
                    Physics.interCollisionDistance = state.InterCollisionDistance;
                }

                if (Physics.interCollisionStiffness > 0.0f)
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

        public bool Raycast(out Vector3 pos, out Vector3 norm, out int face)
        {
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

            GameObject go = cloth.gameObject;
            MeshCollider meshCollider = go.GetComponent<MeshCollider>();

            RaycastHit hit;
            if (meshCollider.Raycast(mouseRay, out hit, Mathf.Infinity))
            {
                norm = hit.normal;
                pos = hit.point;
                face = hit.triangleIndex;
                return true;
            }

            norm = Vector2.zero;
            pos = Vector3.zero;
            face = -1;
            return false;
        }

        void UpdatePreviewBrush()
        {
            Raycast(out m_BrushPos, out m_BrushNorm, out m_BrushFace);
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
                Vector3[] vertices = cloth.vertices;
                Transform t = m_SkinnedMeshRenderer.actualRootBone;
                if (m_LastVertices.Length != vertices.Length)
                    return true;
                for (int i = 0; i < m_LastVertices.Length; i++)
                {
                    var v = t.rotation * vertices[i] + t.position;
                    // we use !(==) here instead of !=, since Vector3 != is incorrectly written to return false if one component is NaN.
                    if (!(m_LastVertices[i] == v))
                        return true;
                }
            }

            return false;
        }

        void GenerateSelectionMesh()
        {
            if (!IsMeshValid())
            {
                return;
            }

            Vector3[] vertices = cloth.vertices;
            int length = vertices.Length;
            m_ParticleSelection = new bool[length];
            m_ParticleRectSelection = new bool[length];

            m_LastVertices = new Vector3[length];
            Transform t = m_SkinnedMeshRenderer.actualRootBone;
            for (int i = 0; i < length; i++)
            {
                m_LastVertices[i] = t.rotation * vertices[i] + t.position;
            }
        }

        void InitSelfAndInterCollisionSelection()
        {
            int length = cloth.vertices.Length;
            m_SelfAndInterCollisionSelection = new bool[length];
            for (int i = 0; i < length; i++)
            {
                m_SelfAndInterCollisionSelection[i] = false;
            }

            List<UInt32> selfAndInterCollisionIndices = new List<UInt32>(length);
            selfAndInterCollisionIndices.Clear();

            cloth.GetSelfAndInterCollisionIndices(selfAndInterCollisionIndices);

            length = selfAndInterCollisionIndices.Count;
            for (int i = 0; i < length; i++)
            {
                m_SelfAndInterCollisionSelection[selfAndInterCollisionIndices[i]] = true;
            }
        }

        void InitClothParticlesInWorldSpace()
        {
            Vector3[] vertices = cloth.vertices;
            int length = vertices.Length;
            m_ClothParticlesInWorldSpace = new Vector3[length];

            Transform t = m_SkinnedMeshRenderer.actualRootBone;
            Quaternion rotation = t.rotation;
            Vector3 position = t.position;
            for (int i = 0; i < length; i++)
            {
                m_ClothParticlesInWorldSpace[i] = rotation * vertices[i] + position;
            }
        }

        void DrawSelfAndInterCollisionParticles()
        {
            Transform t = m_SkinnedMeshRenderer.actualRootBone;
            Vector3[] vertices = cloth.vertices;

            int id = GUIUtility.GetControlID(FocusType.Passive);
            float size = state.SelfCollisionDistance;
            if (state.VisualizeSelfOrInterCollision == CollisionVisualizationMode.SelfCollision)
            {
                size = state.SelfCollisionDistance;
            }
            else if (state.VisualizeSelfOrInterCollision == CollisionVisualizationMode.InterCollision)
            {
                size = state.InterCollisionDistance;
            }

            int length = m_SelfAndInterCollisionSelection.Length;
            for (int i = 0; i < length; i++)
            {
                Vector3 distanceBetween = m_ClothParticlesInWorldSpace[i] - m_BrushPos;
                bool forwardFacing = Vector3.Dot(t.rotation * cloth.normals[i], Camera.current.transform.forward) <= 0;
                if (forwardFacing || state.ManipulateBackfaces)
                {
                    if ((m_SelfAndInterCollisionSelection[i] == true) && !(m_ParticleSelection[i] == true))
                    {
                        Handles.color = s_SelfAndInterCollisionParticleColor;
                    }
                    else if (!(m_SelfAndInterCollisionSelection[i] == true) && !(m_ParticleSelection[i] == true))
                    {
                        Handles.color = s_UnselectedSelfAndInterCollisionParticleColor;
                    }

                    if ((m_ParticleSelection[i] == true) && (m_NumSelection > 0) && (state.CollToolMode == CollToolMode.Select))
                    {
                        Handles.color = s_SelectedParticleColor;
                    }

                    if ((distanceBetween.magnitude < state.BrushRadius) && forwardFacing && ((state.CollToolMode == CollToolMode.Paint) || (state.CollToolMode == CollToolMode.Erase)))
                    {
                        Handles.color = s_SelectedParticleColor;
                    }

                    Handles.SphereHandleCap(id, m_ClothParticlesInWorldSpace[i], t.rotation, size, EventType.Repaint);
                }
            }
        }

        void InitInspector()
        {
            InitBrushCollider();
            InitSelfAndInterCollisionSelection();
            InitClothParticlesInWorldSpace();

            m_NumVerts = cloth.vertices.Length;
        }

        void OnEnable()
        {
            if (s_ColorTexture == null)
                s_ColorTexture = GenerateColorTexture(100);

            m_SkinnedMeshRenderer = cloth.GetComponent<SkinnedMeshRenderer>();

            InitInspector();

            GenerateSelectionMesh();

            m_SelfCollisionDistance = serializedObject.FindProperty("m_SelfCollisionDistance");
            m_SelfCollisionStiffness = serializedObject.FindProperty("m_SelfCollisionStiffness");

            SceneView.onPreSceneGUIDelegate += OnPreSceneGUICallback;
        }

        void InitBrushCollider()
        {
            if (cloth != null)
            {
                GameObject go = cloth.gameObject;
                MeshCollider oldMeshCollider = go.GetComponent<MeshCollider>();
                if (oldMeshCollider != null)
                {
                    if (((oldMeshCollider.hideFlags & HideFlags.HideInHierarchy) != 0) || ((oldMeshCollider.hideFlags & HideFlags.HideInInspector) != 0))
                    {
                        DestroyImmediate(oldMeshCollider, true);
                    }
                }

                MeshCollider meshCollider = go.AddComponent<MeshCollider>();
                meshCollider.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector |
                    HideFlags.DontSaveInEditor | HideFlags.NotEditable;

                meshCollider.sharedMesh = m_SkinnedMeshRenderer.sharedMesh;

                s_BrushCreated = true;
            }
        }

        public void OnDestroy()
        {
            SceneView.onPreSceneGUIDelegate -= OnPreSceneGUICallback;

            if (s_BrushCreated == true)
            {
                if (cloth != null)
                {
                    GameObject go = cloth.gameObject;
                    MeshCollider meshCollider = go.GetComponent<MeshCollider>();
                    if (meshCollider != null)
                    {
                        if (((meshCollider.hideFlags & HideFlags.HideInHierarchy) != 0) || ((meshCollider.hideFlags & HideFlags.HideInInspector) != 0))
                        {
                            DestroyImmediate(meshCollider, true);
                        }
                    }
                    s_BrushCreated = false;
                }
            }
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

        void OnDisable()
        {
            SceneView.onPreSceneGUIDelegate -= OnPreSceneGUICallback;
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
            ClothSkinningCoefficient[] coefficients = cloth.coefficients;

            float maxDistance = 0;
            float useMaxDistance = 0;
            float collisionSphereDistance = 0;
            float useCollisionSphereDistance = 0;
            int numSelection = 0;
            bool firstVertex = true;

            for (int i = 0; i < m_ParticleSelection.Length; i++)
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

            float maxDistanceNew = CoefficientField(maxDistance, useMaxDistance, numSelection > 0, DrawMode.MaxDistance);
            if (maxDistanceNew != maxDistance)
            {
                for (int i = 0; i < coefficients.Length; i++)
                {
                    if (m_ParticleSelection[i])
                        coefficients[i].maxDistance = maxDistanceNew;
                }
                cloth.coefficients = coefficients;
                Undo.RegisterCompleteObjectUndo(target, "Change Cloth Coefficients");
            }

            float collisionSphereDistanceNew = CoefficientField(collisionSphereDistance, useCollisionSphereDistance, numSelection > 0, DrawMode.CollisionSphereDistance);
            if (collisionSphereDistanceNew != collisionSphereDistance)
            {
                for (int i = 0; i < coefficients.Length; i++)
                {
                    if (m_ParticleSelection[i])
                        coefficients[i].collisionSphereDistance = collisionSphereDistanceNew;
                }
                cloth.coefficients = coefficients;
                Undo.RegisterCompleteObjectUndo(target, "Change Cloth Coefficients");
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
                for (int i = 0; i < coefficients.Length; i++)
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
            {
                return;
            }

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

            Vector3[] normals = cloth.normals;
            ClothSkinningCoefficient[] coefficients = cloth.coefficients;
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            float minDistance = 1000;
            int found = -1;
            Quaternion rotation = m_SkinnedMeshRenderer.actualRootBone.rotation;
            for (int i = 0; i < coefficients.Length; i++)
            {
                Vector3 dir = m_LastVertices[i] - mouseRay.origin;
                float sqrDistance = Vector3.Cross(dir, mouseRay.direction).sqrMagnitude;
                bool forwardFacing = Vector3.Dot(rotation * normals[i], Camera.current.transform.forward) <= 0;
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

            Transform t = m_SkinnedMeshRenderer.actualRootBone;
            int id = GUIUtility.GetControlID(FocusType.Passive);
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

            Vector3[] normals = cloth.normals;
            for (int i = 0; i < length; i++)
            {
                bool forwardFacing = Vector3.Dot(t.rotation * normals[i], Camera.current.transform.forward) <= 0;
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
                        Handles.color = s_SelectedParticleColor;
                    }

                    Handles.SphereHandleCap(id, m_ClothParticlesInWorldSpace[i], t.rotation, state.ConstraintSize, EventType.Repaint);
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

            Vector3[] normals = cloth.normals;
            ClothSkinningCoefficient[] coefficients = cloth.coefficients;

            float minX = Mathf.Min(m_SelectStartPoint.x, m_SelectMousePoint.x);
            float maxX = Mathf.Max(m_SelectStartPoint.x, m_SelectMousePoint.x);
            float minY = Mathf.Min(m_SelectStartPoint.y, m_SelectMousePoint.y);
            float maxY = Mathf.Max(m_SelectStartPoint.y, m_SelectMousePoint.y);
            Ray topLeft = HandleUtility.GUIPointToWorldRay(new Vector2(minX, minY));
            Ray topRight = HandleUtility.GUIPointToWorldRay(new Vector2(maxX, minY));
            Ray botLeft = HandleUtility.GUIPointToWorldRay(new Vector2(minX, maxY));
            Ray botRight = HandleUtility.GUIPointToWorldRay(new Vector2(maxX, maxY));

            Plane top = new Plane(topRight.origin + topRight.direction, topLeft.origin + topLeft.direction, topLeft.origin);
            Plane bottom = new Plane(botLeft.origin + botLeft.direction, botRight.origin + botRight.direction, botRight.origin);
            Plane left = new Plane(topLeft.origin + topLeft.direction, botLeft.origin + botLeft.direction, botLeft.origin);
            Plane right = new Plane(botRight.origin + botRight.direction, topRight.origin + topRight.direction, topRight.origin);

            Quaternion rotation = m_SkinnedMeshRenderer.actualRootBone.rotation;

            int length = coefficients.Length;
            for (int i = 0; i < length; i++)
            {
                Vector3 v = m_LastVertices[i];
                bool forwardFacing = Vector3.Dot(rotation * normals[i], Camera.current.transform.forward) <= 0;
                bool selected = top.GetSide(v) && bottom.GetSide(v) && left.GetSide(v) && right.GetSide(v);
                selected = selected && (state.ManipulateBackfaces || forwardFacing);
                if (m_ParticleRectSelection[i] != selected)
                {
                    m_ParticleRectSelection[i] = selected;
                    selectionChanged = true;
                }
            }
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
            SceneView.lastActiveSceneView.SendEvent(EditorGUIUtility.CommandEvent("ModifierKeysChanged"));
        }

        void SelectionPreSceneGUI(int id)
        {
            Event e = Event.current;
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (e.alt || e.control || e.command || e.button != 0)
                        break;
                    GUIUtility.hotControl = id;
                    int found = GetMouseVertex(e);
                    if (found != -1)
                    {
                        if (e.shift)
                            m_ParticleSelection[found] = !m_ParticleSelection[found];
                        else
                        {
                            for (int i = 0; i < m_ParticleSelection.Length; i++)
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
                    if (m_RectSelecting && e.commandName == "ModifierKeysChanged")
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
                                for (int i = 0; i < coefficients.Length; i++)
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

            Vector3[] vertices = cloth.vertices;
            Vector3[] normals = cloth.normals;
            ClothSkinningCoefficient[] coefficients = cloth.coefficients;
            Quaternion rotation = m_SkinnedMeshRenderer.actualRootBone.rotation;
            int length = vertices.Length;
            for (int i = 0; i < length; i++)
            {
                Vector3 distanceBetween = m_ClothParticlesInWorldSpace[i] - m_BrushPos;
                bool forwardFacing = Vector3.Dot(rotation * normals[i], Camera.current.transform.forward) <= 0;
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

            Vector3[] vertices = cloth.vertices;
            Vector3[] normals = cloth.normals;
            Quaternion rotation = m_SkinnedMeshRenderer.actualRootBone.rotation;
            int length = vertices.Length;
            for (int i = 0; i < length; i++)
            {
                Vector3 distanceBetween = m_ClothParticlesInWorldSpace[i] - m_BrushPos;
                bool forwardFacing = Vector3.Dot(rotation * normals[i], Camera.current.transform.forward) <= 0;
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

                    int id = GUIUtility.GetControlID(FocusType.Passive);
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
                    Handles.SphereHandleCap(id, m_ClothParticlesInWorldSpace[i], rotation, size, EventType.Repaint);

                    Repaint();
                }
            }

            Undo.RegisterCompleteObjectUndo(target, "Paint Collision");
        }

        void PaintPreSceneGUI(int id)
        {
            if (!IsMeshValid())
            {
                return;
            }

            Event e = Event.current;
            EventType type = e.GetTypeForControl(id);
            if (type == EventType.MouseDown || type == EventType.MouseDrag)
            {
                ClothSkinningCoefficient[] coefficients = cloth.coefficients;
                if (GUIUtility.hotControl != id && (e.alt || e.control || e.command || e.button != 0))
                    return;
                if (type == EventType.MouseDown)
                    GUIUtility.hotControl = id;

                if (editingSelfAndInterCollisionParticles)
                {
                    GetBrushedParticles(e);
                }

                if (editingConstraints)
                {
                    GetBrushedConstraints(e);
                }
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

        private void OnPreSceneGUICallback(SceneView sceneView)
        {
            // Multi-editing in scene not supported
            if (targets.Length > 1)
                return;

            if (editingConstraints || editingSelfAndInterCollisionParticles)
            {
                OnPreSceneGUI();
            }
        }

        void OnPreSceneGUI()
        {
            if (!IsMeshValid())
            {
                return;
            }

            // Disable Scene view tools, so we can use our own.
            Tools.current = Tool.None;
            if (state.ToolMode == (ToolMode)(-1))
                state.ToolMode = ToolMode.Select;

            if ((m_ParticleSelection == null) || (m_LastVertices.Length != cloth.vertices.Length))
            {
                GenerateSelectionMesh();
            }
            else
            {
                ClothSkinningCoefficient[] coefficients = cloth.coefficients;
                if (m_ParticleSelection.Length != coefficients.Length)
                    InitInspector();
            }

            Handles.BeginGUI();
            int id = GUIUtility.GetControlID(FocusType.Passive);

            Event e = Event.current;
            switch (e.GetTypeForControl(id))
            {
                case EventType.Layout:
                    HandleUtility.AddDefaultControl(id);
                    break;

                case EventType.MouseMove:
                case EventType.MouseDrag:
                    int oldMouseOver = m_MouseOver;
                    m_MouseOver = GetMouseVertex(e);
                    if (m_MouseOver != oldMouseOver)
                        SceneView.RepaintAll();
                    break;
            }

            if (editingConstraints)
            {
                switch (state.ToolMode)
                {
                    case ToolMode.Select:
                        SelectionPreSceneGUI(id);
                        break;

                    case ToolMode.Paint:
                        PaintPreSceneGUI(id);
                        break;
                }
            }

            if (editingSelfAndInterCollisionParticles)
            {
                switch (state.CollToolMode)
                {
                    case CollToolMode.Select:
                        SelectionPreSceneGUI(id);
                        break;

                    case CollToolMode.Paint:
                    case CollToolMode.Erase:
                        PaintPreSceneGUI(id);
                        break;
                }
            }

            Handles.EndGUI();
        }

        public void OnSceneGUI()
        {
            if (editingConstraints)
            {
                OnSceneEditConstraintsGUI();
                return;
            }

            if (editingSelfAndInterCollisionParticles)
            {
                OnSceneEditSelfAndInterCollisionParticlesGUI();
                return;
            }
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

            if (Event.current.type == EventType.Repaint)
                DrawConstraints();

            var evt = Event.current;
            if (evt.commandName == "SelectAll")
            {
                if (evt.type == EventType.ValidateCommand)
                    evt.Use();

                if (evt.type == EventType.ExecuteCommand)
                {
                    int numVertices = cloth.vertices.Length;
                    for (int i = 0; i < numVertices; i++)
                        m_ParticleSelection[i] = true;
                    SceneView.RepaintAll();
                    state.ToolMode = ToolMode.Select;
                    evt.Use();
                }
            }

            Handles.BeginGUI();
            if (m_RectSelecting && state.ToolMode == ToolMode.Select && Event.current.type == EventType.Repaint)
                EditorStyles.selectionRect.Draw(EditorGUIExt.FromToRect(m_SelectStartPoint, m_SelectMousePoint), GUIContent.none, false, false, false, false);
            Handles.EndGUI();

            SceneViewOverlay.Window(new GUIContent("Cloth Constraints"), ConstraintEditing, (int)SceneViewOverlay.Ordering.ClothConstraints, SceneViewOverlay.WindowDisplayOption.OneWindowPerTarget);
        }

        void OnSceneEditSelfAndInterCollisionParticlesGUI()
        {
            // Multi-editing in scene not supported
            if (Selection.gameObjects.Length > 1)
                return;

            DrawSelfAndInterCollisionParticles();

            if ((Event.current.type == EventType.Repaint) && ((state.CollToolMode == CollToolMode.Paint) || ((state.CollToolMode == CollToolMode.Erase))))
            {
                UpdatePreviewBrush();
                DrawBrush();
            }

            Handles.BeginGUI();
            if (m_RectSelecting && state.CollToolMode == CollToolMode.Select && Event.current.type == EventType.Repaint)
                EditorStyles.selectionRect.Draw(EditorGUIExt.FromToRect(m_SelectStartPoint, m_SelectMousePoint), GUIContent.none, false, false, false, false);
            Handles.EndGUI();

            SceneViewOverlay.Window(Styles.clothSelfCollisionAndInterCollision, SelfAndInterCollisionEditing, (int)SceneViewOverlay.Ordering.ClothSelfAndInterCollision, SceneViewOverlay.WindowDisplayOption.OneWindowPerTarget);
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

        void ConstraintEditing(UnityObject unused, SceneView sceneView)
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
                menu.AddItem(new GUIContent("Manipulate Backfaces"), state.ManipulateBackfaces , VisualizationMenuToggleManipulateBackfaces);
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
            }

            if (!IsConstrained())
                EditorGUILayout.HelpBox("No constraints have been set up, so the cloth will move freely. Set up vertex constraints here to restrict it.", MessageType.Info);

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
            int lengthSelection = m_ParticleRectSelection.Length;
            for (int i = 0; i < lengthSelection; i++)
            {
                m_ParticleRectSelection[i] = false;
                m_ParticleSelection[i] = false;
            }
        }

        void SelfAndInterCollisionEditing(UnityObject unused, SceneView sceneView)
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

            if (countIndices > 0)
            {
                List<UInt32> selfAndInterCollisionIndices = new List<UInt32>(countIndices);
                selfAndInterCollisionIndices.Clear();
                for (UInt32 i = 0; i < length; i++)
                {
                    if (m_SelfAndInterCollisionSelection[i] == true)
                    {
                        selfAndInterCollisionIndices.Add(i);
                    }
                }

                cloth.SetSelfAndInterCollisionIndices(selfAndInterCollisionIndices);
            }

            GUILayout.EndVertical();
        }
    }
}

