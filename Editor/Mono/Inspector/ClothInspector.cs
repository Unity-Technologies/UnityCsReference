// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditorInternal;


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
        [SerializeField] public ClothInspector.ToolMode ToolMode = ClothInspector.ToolMode.Select;
    }

    [CustomEditor(typeof(Cloth))]
    [CanEditMultipleObjects]
    class ClothInspector : Editor
    {
        public enum DrawMode { MaxDistance = 1, CollisionSphereDistance };
        public enum ToolMode { Select, Paint };
        enum RectSelectionMode { Replace, Add, Substract };

        bool[] m_Selection;
        bool[] m_RectSelection;
        int m_MouseOver = -1;
        int m_MeshVerticesPerSelectionVertex = 0;
        Mesh m_SelectionMesh;
        Mesh m_SelectedMesh;
        Mesh m_VertexMesh;
        Mesh m_VertexMeshSelected;
        Vector3[] m_LastVertices;
        Vector2 m_SelectStartPoint;
        Vector2 m_SelectMousePoint;
        bool m_RectSelecting = false;
        bool m_DidSelect = false;
        float[] m_MaxVisualizedValue = new float[3];
        float[] m_MinVisualizedValue = new float[3];
        RectSelectionMode m_RectSelectionMode = RectSelectionMode.Add;

        static Color s_SelectionColor;
        static Material s_SelectionMaterial = null;
        static Material s_SelectionMaterialBackfaces = null;
        static Material s_SelectedMaterial = null;
        static Texture2D s_ColorTexture = null;

        const float kDisabledValue = float.MaxValue;

        static GUIContent[] s_ToolIcons = null;
        static GUIContent[] s_ModeStrings = null;
        static GUIContent s_PaintIcon = null;

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
                    SetupSelectionMeshColors();
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

        public bool editing
        {
            get { return EditMode.editMode == EditMode.SceneViewEditMode.Cloth && EditMode.IsOwner(this); }
        }

        GUIContent GetModeString(DrawMode mode)
        {
            return s_ModeStrings[(int)mode];
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
            EditorGUI.BeginDisabledGroup(targets.Length > 1);
            EditMode.DoEditModeInspectorModeButton(
                EditMode.SceneViewEditMode.Cloth,
                "Edit Constraints",
                EditorGUIUtility.IconContent("EditCollider"),
                this
                );
            EditorGUI.EndDisabledGroup();
            base.OnInspectorGUI();

            MeshRenderer meshRenderer = cloth.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
                Debug.LogWarning("MeshRenderer will not work with a cloth component! Use only SkinnedMeshRenderer. Any MeshRenderer's attached to a cloth component will be deleted at runtime.");
        }

        internal override Bounds GetWorldBoundsOfTarget(Object targetObject)
        {
            Cloth cloth = (Cloth)targetObject;
            var skin = cloth.GetComponent<SkinnedMeshRenderer>();
            return skin == null ? base.GetWorldBoundsOfTarget(targetObject) : skin.bounds;
        }

        bool SelectionMeshDirty()
        {
            SkinnedMeshRenderer smr = cloth.GetComponent<SkinnedMeshRenderer>();
            Vector3[] vertices = cloth.vertices;
            Transform t = smr.actualRootBone;
            if (m_LastVertices.Length != vertices.Length)
                return true;
            for (int i = 0; i < m_LastVertices.Length; i++)
            {
                var v = t.rotation * vertices[i] + t.position;
                // we use !(==) here instead of !=, since Vector3 != is incorrectly written to return false if one component is NaN.
                if (!(m_LastVertices[i] == v))
                    return true;
            }
            return false;
        }

        void GenerateSelectionMesh()
        {
            if (cloth.vertices.Length == 0)
            {
                return;
            }

            SkinnedMeshRenderer smr = cloth.GetComponent<SkinnedMeshRenderer>();
            Vector3[] vertices = cloth.vertices;

            int length = vertices.Length;
            m_Selection = new bool[vertices.Length];
            m_RectSelection = new bool[vertices.Length];

            if (m_SelectionMesh != null)
            {
                DestroyImmediate(m_SelectionMesh);
                DestroyImmediate(m_SelectedMesh);
            }

            m_SelectionMesh = new Mesh();
            m_SelectionMesh.indexFormat = IndexFormat.UInt32;
            m_SelectionMesh.hideFlags |= HideFlags.DontSave;

            m_SelectedMesh = new Mesh();
            m_SelectedMesh.indexFormat = IndexFormat.UInt32;
            m_SelectedMesh.hideFlags |= HideFlags.DontSave;

            m_LastVertices = new Vector3[length];
            m_MeshVerticesPerSelectionVertex = m_VertexMesh.vertices.Length;
            Transform t = smr.actualRootBone;

            {
                int numVertices = length;
                CombineInstance[] combine = new CombineInstance[numVertices];
                for (int i = 0; i < numVertices; i++)
                {
                    m_LastVertices[i] = t.rotation * vertices[i] + t.position;
                    combine[i].mesh = m_VertexMesh;
                    combine[i].transform = Matrix4x4.TRS(m_LastVertices[i], Quaternion.identity, Vector3.one);
                }
                m_SelectionMesh.CombineMeshes(combine);

                for (int i = 0; i < numVertices; i++)
                    combine[i].mesh = m_VertexMeshSelected;
                m_SelectedMesh.CombineMeshes(combine);
            }

            SetupSelectionMeshColors();
        }

        void OnEnable()
        {
            if (s_SelectionMaterial == null)
            {
                s_SelectionMaterial = EditorGUIUtility.LoadRequired("SceneView/VertexSelectionMaterial.mat") as Material;
                s_SelectionMaterialBackfaces = EditorGUIUtility.LoadRequired("SceneView/VertexSelectionBackfacesMaterial.mat") as Material;
                s_SelectedMaterial = EditorGUIUtility.LoadRequired("SceneView/VertexSelectedMaterial.mat") as Material;
            }
            if (s_ColorTexture == null)
                s_ColorTexture = GenerateColorTexture(100);

            if (s_ToolIcons == null)
            {
                s_ToolIcons = new GUIContent[2];
                s_ToolIcons[0] = EditorGUIUtility.TextContent("Select|Select vertices and edit their cloth coefficients in the inspector.");
                s_ToolIcons[1] = EditorGUIUtility.TextContent("Paint|Paint cloth coefficients on to vertices.");
            }

            if (s_ModeStrings == null)
            {
                s_ModeStrings = new GUIContent[3];
                s_ModeStrings[0] = EditorGUIUtility.TextContent("Fixed");
                s_ModeStrings[1] = EditorGUIUtility.TextContent("Max Distance");
                s_ModeStrings[2] = EditorGUIUtility.TextContent("Surface Penetration");
            }

            if (s_PaintIcon == null)
            {
                s_PaintIcon = EditorGUIUtility.IconContent("ClothInspector.PaintValue", "|Change this vertex coefficient value by painting in the scene view.");
            }

            m_VertexMesh = new Mesh();
            m_VertexMesh.hideFlags |= HideFlags.DontSave;
            Mesh cubeMesh = (Mesh)Resources.GetBuiltinResource(typeof(Mesh), "Cube.fbx");
            m_VertexMesh.vertices = new Vector3[cubeMesh.vertices.Length];
            m_VertexMesh.normals = cubeMesh.normals;
            var tangents = new Vector4[cubeMesh.vertices.Length];
            var vertices = cubeMesh.vertices;
            for (int i = 0; i < cubeMesh.vertices.Length; i++)
                tangents[i] = vertices[i] * -0.01f;
            m_VertexMesh.tangents = tangents;
            m_VertexMesh.triangles = cubeMesh.triangles;

            m_VertexMeshSelected = new Mesh();
            m_VertexMeshSelected.hideFlags |= HideFlags.DontSave;
            m_VertexMeshSelected.vertices = m_VertexMesh.vertices;
            m_VertexMeshSelected.normals = m_VertexMesh.normals;
            for (int i = 0; i < cubeMesh.vertices.Length; i++)
                tangents[i] = vertices[i] * -0.02f;
            m_VertexMeshSelected.tangents = tangents;
            m_VertexMeshSelected.triangles = m_VertexMesh.triangles;

            GenerateSelectionMesh();
            SetupSelectedMeshColors();

            SceneView.onPreSceneGUIDelegate += OnPreSceneGUICallback;
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

        void SetupSelectionMeshColors()
        {
            if (cloth.vertices.Length == 0)
            {
                return;
            }

            ClothSkinningCoefficient[] coefficients = cloth.coefficients;
            int length = coefficients.Length;
            Color[] colors = new Color[length * m_MeshVerticesPerSelectionVertex];
            float min = 0;
            float max = 0;
            for (int i = 0; i < coefficients.Length; i++)
            {
                float value = GetCoefficient(coefficients[i]);
                if (value >= kDisabledValue)
                    continue;
                if (value < min)
                    min = value;
                if (value > max)
                    max = value;
            }
            for (int i = 0; i < length; i++)
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
                for (int j = 0; j < m_MeshVerticesPerSelectionVertex; j++)
                    colors[i * m_MeshVerticesPerSelectionVertex + j] = color;
            }

            m_MaxVisualizedValue[(int)drawMode] = max;
            m_MinVisualizedValue[(int)drawMode] = min;
            m_SelectionMesh.colors = colors;
        }

        void SetupSelectedMeshColors()
        {
            if (cloth.vertices.Length == 0)
            {
                return;
            }

            int length = cloth.coefficients.Length;
            Color[] colors = new Color[length * m_MeshVerticesPerSelectionVertex];
            for (int i = 0; i < length; i++)
            {
                bool selected = m_Selection[i];
                if (m_RectSelecting)
                {
                    switch (m_RectSelectionMode)
                    {
                        case RectSelectionMode.Replace:
                            selected = m_RectSelection[i];
                            break;
                        case RectSelectionMode.Add:
                            selected |= m_RectSelection[i];
                            break;
                        case RectSelectionMode.Substract:
                            selected = selected && !m_RectSelection[i];
                            break;
                    }
                }

                var color = selected ? s_SelectionColor : Color.clear;
                for (int j = 0; j < m_MeshVerticesPerSelectionVertex; j++)
                    colors[i * m_MeshVerticesPerSelectionVertex + j] = color;
            }

            m_SelectedMesh.colors = colors;
        }

        void OnDisable()
        {
            SceneView.onPreSceneGUIDelegate -= OnPreSceneGUICallback;

            if (m_SelectionMesh != null)
            {
                DestroyImmediate(m_SelectionMesh);
                DestroyImmediate(m_SelectedMesh);
            }
            DestroyImmediate(m_VertexMesh);
            DestroyImmediate(m_VertexMeshSelected);
        }

        float CoefficientField(float value, float useValue, bool enabled, DrawMode mode)
        {
            var label = GetModeString(mode);

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
            var label = GetModeString(mode);
            GUILayout.BeginHorizontal();
            enabled = GUILayout.Toggle(enabled, s_PaintIcon, "MiniButton", GUILayout.ExpandWidth(false));
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

            for (int i = 0; i < m_Selection.Length; i++)
            {
                if (m_Selection[i])
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
                    if (m_Selection[i])
                        coefficients[i].maxDistance = maxDistanceNew;
                }
                cloth.coefficients = coefficients;
                SetupSelectionMeshColors();
                Undo.RegisterCompleteObjectUndo(target, "Change Cloth Coefficients");
            }

            float collisionSphereDistanceNew = CoefficientField(collisionSphereDistance, useCollisionSphereDistance, numSelection > 0, DrawMode.CollisionSphereDistance);
            if (collisionSphereDistanceNew != collisionSphereDistance)
            {
                for (int i = 0; i < coefficients.Length; i++)
                {
                    if (m_Selection[i])
                        coefficients[i].collisionSphereDistance = collisionSphereDistanceNew;
                }
                cloth.coefficients = coefficients;
                SetupSelectionMeshColors();
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
                    if (m_Selection[i])
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
                SetupSelectionMeshColors();
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
        }

        int GetMouseVertex(Event e)
        {
            // No cloth manipulation Tool enabled -> don't interact with vertices.
            if (Tools.current != Tool.None)
                return -1;

            SkinnedMeshRenderer smr = cloth.GetComponent<SkinnedMeshRenderer>();
            Vector3[] normals = cloth.normals;
            ClothSkinningCoefficient[] coefficients = cloth.coefficients;
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            float minDistance = 1000;
            int found = -1;
            Quaternion rotation = smr.actualRootBone.rotation;
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

        void DrawVertices()
        {
            if (SelectionMeshDirty())
                GenerateSelectionMesh();

            if (state.ToolMode == ToolMode.Select)
            {
                for (int i = 0; i < s_SelectedMaterial.passCount; i++)
                {
                    s_SelectedMaterial.SetPass(i);
                    Graphics.DrawMeshNow(m_SelectedMesh, Matrix4x4.identity);
                }
            }

            Material mat = state.ManipulateBackfaces ? s_SelectionMaterialBackfaces : s_SelectionMaterial;
            for (int i = 0; i < mat.passCount; i++)
            {
                mat.SetPass(i);
                Graphics.DrawMeshNow(m_SelectionMesh, Matrix4x4.identity);
            }

            if (m_MouseOver != -1)
            {
                Matrix4x4 m = Matrix4x4.TRS(m_LastVertices[m_MouseOver], Quaternion.identity, Vector3.one * 1.2f);
                if (state.ToolMode == ToolMode.Select)
                {
                    mat = s_SelectedMaterial;
                    mat.color = new Color(s_SelectionColor.r, s_SelectionColor.g, s_SelectionColor.b, 0.5f);
                }
                else
                {
                    mat.color = m_SelectionMesh.colors[m_MouseOver];
                }

                for (int i = 0; i < mat.passCount; i++)
                {
                    mat.SetPass(i);
                    Graphics.DrawMeshNow(m_VertexMeshSelected, m);
                }
                mat.color = Color.white;
            }
        }

        bool UpdateRectSelection()
        {
            bool selectionChanged = false;

            SkinnedMeshRenderer smr = cloth.GetComponent<SkinnedMeshRenderer>();
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

            Quaternion rotation = smr.actualRootBone.rotation;

            for (int i = 0; i < coefficients.Length; i++)
            {
                Vector3 v = m_LastVertices[i];
                bool forwardFacing = Vector3.Dot(rotation * normals[i], Camera.current.transform.forward) <= 0;
                bool selected = top.GetSide(v) && bottom.GetSide(v) && left.GetSide(v) && right.GetSide(v);
                selected = selected && (state.ManipulateBackfaces || forwardFacing);
                if (m_RectSelection[i] != selected)
                {
                    m_RectSelection[i] = selected;
                    selectionChanged = true;
                }
            }
            return selectionChanged;
        }

        void ApplyRectSelection()
        {
            ClothSkinningCoefficient[] coefficients = cloth.coefficients;

            for (int i = 0; i < coefficients.Length; i++)
            {
                switch (m_RectSelectionMode)
                {
                    case RectSelectionMode.Replace:
                        m_Selection[i] = m_RectSelection[i];
                        break;

                    case RectSelectionMode.Add:
                        m_Selection[i] |= m_RectSelection[i];
                        break;

                    case RectSelectionMode.Substract:
                        m_Selection[i] = m_Selection[i] && !m_RectSelection[i];
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
                            m_Selection[found] = !m_Selection[found];
                        else
                        {
                            for (int i = 0; i < m_Selection.Length; i++)
                                m_Selection[i] = false;
                            m_Selection[found] = true;
                        }
                        m_DidSelect = true;
                        SetupSelectedMeshColors();
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
                                SetupSelectedMeshColors();
                            }
                        }
                        if (m_RectSelecting)
                        {
                            m_SelectMousePoint = new Vector2(Mathf.Max(e.mousePosition.x, 0), Mathf.Max(e.mousePosition.y, 0));
                            if (RectSelectionModeFromEvent() || UpdateRectSelection())
                                SetupSelectedMeshColors();
                            e.Use();
                        }
                    }
                    break;

                case EventType.ExecuteCommand:
                    if (m_RectSelecting && e.commandName == "ModifierKeysChanged")
                    {
                        if (RectSelectionModeFromEvent() || UpdateRectSelection())
                            SetupSelectedMeshColors();
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
                                    m_Selection[i] = false;
                            }
                        }
                        // Disable text focus when selection changes, otherwise we cannot update inspector fields
                        // if text is currently selected.
                        GUIUtility.keyboardControl = 0;
                        SetupSelectedMeshColors();
                        SceneView.RepaintAll();
                    }
                    break;
            }
        }

        void PaintPreSceneGUI(int id)
        {
            Event e = Event.current;
            EventType type = e.GetTypeForControl(id);
            if (type == EventType.MouseDown || type == EventType.MouseDrag)
            {
                ClothSkinningCoefficient[] coefficients = cloth.coefficients;
                if (GUIUtility.hotControl != id && (e.alt || e.control || e.command || e.button != 0))
                    return;
                if (type == EventType.MouseDown)
                    GUIUtility.hotControl = id;
                int found = GetMouseVertex(e);
                if (found != -1)
                {
                    bool changed = false;
                    if (state.PaintMaxDistanceEnabled && coefficients[found].maxDistance != state.PaintMaxDistance)
                    {
                        coefficients[found].maxDistance = state.PaintMaxDistance;
                        changed = true;
                    }
                    if (state.PaintCollisionSphereDistanceEnabled && coefficients[found].collisionSphereDistance != state.PaintCollisionSphereDistance)
                    {
                        coefficients[found].collisionSphereDistance = state.PaintCollisionSphereDistance;
                        changed = true;
                    }
                    if (changed)
                    {
                        Undo.RegisterCompleteObjectUndo(target, "Paint Cloth");
                        cloth.coefficients = coefficients;
                        SetupSelectionMeshColors();
                        Repaint();
                    }
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
            if (!editing)
                return;

            // Multi-editing in scene not supported
            if (targets.Length > 1)
                return;

            // Disable Scene view tools, so we can use our own.
            Tools.current = Tool.None;
            if (state.ToolMode == (ToolMode)(-1))
                state.ToolMode = ToolMode.Select;

            if (m_Selection == null)
            {
                GenerateSelectionMesh();
                SetupSelectedMeshColors();
            }

            ClothSkinningCoefficient[] coefficients = cloth.coefficients;
            if (m_Selection.Length != coefficients.Length)
                // Recreate selection if underlying mesh has changed.
                OnEnable();

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

            switch (state.ToolMode)
            {
                case ToolMode.Select:
                    SelectionPreSceneGUI(id);
                    break;

                case ToolMode.Paint:
                    PaintPreSceneGUI(id);
                    break;
            }
            Handles.EndGUI();
        }

        public void OnSceneGUI()
        {
            if (!editing)
                return;

            // Multi-editing in scene not supported
            if (Selection.gameObjects.Length > 1)
                return;

            s_SelectionColor = GUI.skin.settings.selectionColor;
            if (Event.current.type == EventType.Repaint)
                DrawVertices();

            var evt = Event.current;
            if (evt.commandName == "SelectAll")
            {
                if (evt.type == EventType.ValidateCommand)
                    evt.Use();

                if (evt.type == EventType.ExecuteCommand)
                {
                    int numVertices = cloth.vertices.Length;
                    for (int i = 0; i < numVertices; i++)
                        m_Selection[i] = true;
                    SetupSelectedMeshColors();
                    SceneView.RepaintAll();
                    state.ToolMode = ToolMode.Select;
                    evt.Use();
                }
            }

            Handles.BeginGUI();
            if (m_RectSelecting && state.ToolMode == ToolMode.Select && Event.current.type == EventType.Repaint)
                EditorStyles.selectionRect.Draw(EditorGUIExt.FromToRect(m_SelectStartPoint, m_SelectMousePoint), GUIContent.none, false, false, false, false);
            Handles.EndGUI();

            SceneViewOverlay.Window(new GUIContent("Cloth Constraints"), VertexEditing, (int)SceneViewOverlay.Ordering.Cloth, SceneViewOverlay.WindowDisplayOption.OneWindowPerTarget);
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

        void VertexEditing(Object unused, SceneView sceneView)
        {
            GUILayout.BeginVertical(GUILayout.Width(300));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Visualization: ", GUILayout.ExpandWidth(false));
            GUILayout.BeginVertical();
            if (EditorGUILayout.DropdownButton(GetModeString(drawMode), FocusType.Passive, EditorStyles.toolbarDropDown))
            {
                Rect buttonRect = GUILayoutUtility.topLevel.GetLast();
                GenericMenu menu = new GenericMenu();
                menu.AddItem(GetModeString(DrawMode.MaxDistance), drawMode == DrawMode.MaxDistance , VisualizationMenuSetMaxDistanceMode);
                menu.AddItem(GetModeString(DrawMode.CollisionSphereDistance), drawMode == DrawMode.CollisionSphereDistance , VisualizationMenuSetCollisionSphereMode);
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Manipulate Backfaces"), state.ManipulateBackfaces , VisualizationMenuToggleManipulateBackfaces);
                menu.DropDown(buttonRect);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(m_MinVisualizedValue[(int)drawMode].ToString(), GUILayout.ExpandWidth(false));
            DrawColorBox(s_ColorTexture, Color.clear);
            GUILayout.Label(m_MaxVisualizedValue[(int)drawMode].ToString(), GUILayout.ExpandWidth(false));

            GUILayout.Label("Unconstrained:");
            GUILayout.Space(-24);
            GUILayout.BeginHorizontal(GUILayout.Width(20));
            DrawColorBox(null, Color.black);
            GUILayout.EndHorizontal();

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical("Box");

            if (Tools.current != Tool.None)
                state.ToolMode = (ToolMode)(-1);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                state.ToolMode = (ToolMode)GUILayout.Toolbar((int)state.ToolMode, s_ToolIcons);
                if (check.changed)
                {
                    // delselect text, so we don't end up having a text field highlighted in the new tab
                    GUIUtility.keyboardControl = 0;
                    SceneView.RepaintAll();
                    SetupSelectionMeshColors();
                    SetupSelectedMeshColors();
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
            GUILayout.EndVertical();

            if (!IsConstrained())
                EditorGUILayout.HelpBox("No constraints have been set up, so the cloth will move freely. Set up vertex constraints here to restrict it.", MessageType.Info);

            GUILayout.EndVertical();
            GUILayout.Space(-4);
        }
    }
}

