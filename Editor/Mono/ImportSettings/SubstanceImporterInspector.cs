// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// SUBSTANCE HOOK

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;
using System.Collections.Generic;

#pragma warning disable CS0618  // Due to Obsolete attribute on Predural classes

namespace UnityEditor
{
    [CustomEditor(typeof(SubstanceArchive))]
    internal class SubstanceImporterInspector : Editor
    {
        private const float kPreviewWidth = 60;
        private const float kPreviewHeight = kPreviewWidth + 16;
        private const int kMaxRows = 2;
        private const string kDeprecationWarning = "Built-in support for Substance Designer materials has been deprecated and will be removed in Unity 2018.1. To continue using Substance Designer materials in Unity 2018.1, you will need to install Allegorithmic's external importer from the Asset Store.";

        private static SubstanceArchive s_LastSelectedPackage = null;
        private static string s_CachedSelectedMaterialInstanceName = null;
        private string m_SelectedMaterialInstanceName = null;
        private Vector2 m_ListScroll = Vector2.zero;

        private EditorCache m_EditorCache;

        [System.NonSerialized]
        private string[] m_PrototypeNames = null;

        Editor m_MaterialInspector;

        // Static preview rendering
        protected bool m_IsVisible = false;
        public Vector2 previewDir = new Vector2(0, -20);
        public int selectedMesh = 0, lightMode = 1;
        private PreviewRenderUtility m_PreviewUtility;
        static Mesh[] s_Meshes = { null, null, null, null };
        static GUIContent[] s_MeshIcons = { null, null, null, null };
        static GUIContent[] s_LightIcons = { null, null };

        // Styles used in the SubstanceImporterInspector
        class SubstanceStyles
        {
            public GUIContent iconToolbarPlus = EditorGUIUtility.IconContent("Toolbar Plus", "|Add substance from prototype.");
            public GUIContent iconToolbarMinus = EditorGUIUtility.IconContent("Toolbar Minus", "|Remove selected substance.");
            public GUIContent iconDuplicate = EditorGUIUtility.IconContent("TreeEditor.Duplicate", "|Duplicate selected substance.");
            public GUIStyle resultsGridLabel = "ObjectPickerResultsGridLabel";
            public GUIStyle resultsGrid = "ObjectPickerResultsGrid";
            public GUIStyle gridBackground = "TE NodeBackground";
            public GUIStyle background = "ObjectPickerBackground";
            public GUIStyle toolbar = "TE Toolbar";
            public GUIStyle toolbarButton = "TE toolbarbutton";
            public GUIStyle toolbarDropDown = "TE toolbarDropDown";
        }
        SubstanceStyles m_SubstanceStyles;

        public void OnEnable()
        {
            if (target == s_LastSelectedPackage)
                m_SelectedMaterialInstanceName = s_CachedSelectedMaterialInstanceName;
            else
                s_LastSelectedPackage = target as SubstanceArchive;
        }

        public void OnDisable()
        {
            if (m_EditorCache != null)
                m_EditorCache.Dispose();

            if (m_MaterialInspector != null)
            {
                ProceduralMaterialInspector pmInsp = (ProceduralMaterialInspector)m_MaterialInspector;
                pmInsp.ReimportSubstancesIfNeeded();
                DestroyImmediate(m_MaterialInspector);
            }

            s_CachedSelectedMaterialInstanceName = m_SelectedMaterialInstanceName;

            if (m_PreviewUtility != null)
            {
                m_PreviewUtility.Cleanup();
                m_PreviewUtility = null;
            }
        }

        ProceduralMaterial GetSelectedMaterial()
        {
            SubstanceImporter importer = GetImporter();
            if (importer == null)
                return null;
            ProceduralMaterial[] materials = GetSortedMaterials();

            // In some cases, m_SelectedMaterialInstanceName is not valid (for instance after ProceduralMaterial renaming).
            // So we first try to find it in the materials so we know if we should return a valid material instead (case 840177).
            ProceduralMaterial selectedMaterial = System.Array.Find<ProceduralMaterial>(materials, element => element.name == m_SelectedMaterialInstanceName);
            if (m_SelectedMaterialInstanceName == null || selectedMaterial == null)
            {
                if (materials.Length > 0)
                {
                    selectedMaterial = materials[0];
                    m_SelectedMaterialInstanceName = selectedMaterial.name;
                }
            }
            return selectedMaterial;
        }

        private void SelectNextMaterial()
        {
            SubstanceImporter importer = GetImporter();
            if (importer == null)
                return;
            string selected = null;
            ProceduralMaterial[] materials = GetSortedMaterials();
            for (int i = 0; i < materials.Length; ++i)
            {
                if (materials[i].name == m_SelectedMaterialInstanceName)
                {
                    int id = System.Math.Min(i + 1, materials.Length - 1);
                    if (id == i) --id;
                    if (id >= 0) selected = materials[id].name;
                    break;
                }
            }
            m_SelectedMaterialInstanceName = selected;
        }

        Editor GetSelectedMaterialInspector()
        {
            // Check if the cached editor is still valid
            ProceduralMaterial material = GetSelectedMaterial();
            if (material && m_MaterialInspector != null && m_MaterialInspector.target == material)
                return m_MaterialInspector;

            // In case the user was editing the name of the material, but didn't apply the changes yet, end text editing
            // Case 535718
            EditorGUI.EndEditingActiveTextField();

            // Build a new editor and return it
            DestroyImmediate(m_MaterialInspector);
            m_MaterialInspector = null;

            if (material)
            {
                m_MaterialInspector = Editor.CreateEditor(material);

                if (!(m_MaterialInspector is ProceduralMaterialInspector) && m_MaterialInspector != null)
                {
                    if (material.shader != null)
                        Debug.LogError("The shader: '" + material.shader.name + "' is using a custom editor deriving from MaterialEditor, please derive from ShaderGUI instead. Only the ShaderGUI approach works with Procedural Materials. Search the docs for 'ShaderGUI'");

                    DestroyImmediate(m_MaterialInspector);
                    m_MaterialInspector = Editor.CreateEditor(material, typeof(ProceduralMaterialInspector));
                }

                // This should never fail, but will throw if the unexpected occurs
                ProceduralMaterialInspector pmInsp = (ProceduralMaterialInspector)m_MaterialInspector;
                pmInsp.DisableReimportOnDisable();
            }

            return m_MaterialInspector;
        }

        public override void OnInspectorGUI()
        {
            // Initialize styles
            if (m_SubstanceStyles == null)
                m_SubstanceStyles = new SubstanceStyles();

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical();
            MaterialListing();
            MaterialManagement();
            EditorGUILayout.EndVertical();

            Editor materialEditor = GetSelectedMaterialInspector();
            if (materialEditor)
            {
                materialEditor.DrawHeader();
                EditorGUILayout.HelpBox(kDeprecationWarning, MessageType.Warning);
                materialEditor.OnInspectorGUI();
            }
        }

        SubstanceImporter GetImporter()
        {
            return AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(target)) as SubstanceImporter;
        }

        private static int previewNoDragDropHash = "PreviewWithoutDragAndDrop".GetHashCode();
        void MaterialListing()
        {
            ProceduralMaterial[] materials = GetSortedMaterials();

            foreach (ProceduralMaterial mat in materials)
            {
                if (mat.isProcessing)
                {
                    Repaint();
                    SceneView.RepaintAll();
                    GameView.RepaintAll();
                    break;
                }
            }

            int count = materials.Length;

            // The height of the content may change based on the selected material,
            // thus an inspector vertical scrollbar may appear or disappear based on selected material.
            // We don't want previews to jump around when you select them.
            // So we don't calculate the width based on the available space (because it would change due to scrollbar)
            // but rather based on total inspector width, with scrollbar width (16) always subtracted (plus margins and borders).
            float listWidth = GUIView.current.position.width - 16 - 18 - 2;

            // If the preview list gets its own scrollbar, subtract the width of that one as well
            // This won't jump around based on selection, so ok to conditionally subtract it.
            if (listWidth * kMaxRows < count * kPreviewWidth)
                listWidth -= 16;

            // Number of previews per row
            int perRow = Mathf.Max(1, Mathf.FloorToInt(listWidth / kPreviewWidth));
            // Number of preview rows
            int rows = Mathf.CeilToInt(count / (float)perRow);
            // The rect for the preview list
            Rect listRect = new Rect(0, 0, perRow * kPreviewWidth, rows * kPreviewHeight);
            // The rect for the ScrollView the preview list in shown in
            Rect listDisplayRect = GUILayoutUtility.GetRect(
                    listRect.width,
                    Mathf.Clamp(listRect.height, kPreviewHeight, kPreviewHeight * kMaxRows) + 1 // Add one for top border
                    );

            // Rect without top and side borders
            Rect listDisplayRectInner = new Rect(listDisplayRect.x + 1, listDisplayRect.y + 1, listDisplayRect.width - 2, listDisplayRect.height - 1);

            // Background
            GUI.Box(listDisplayRect, GUIContent.none, m_SubstanceStyles.gridBackground);
            GUI.Box(listDisplayRectInner, GUIContent.none, m_SubstanceStyles.background);

            m_ListScroll = GUI.BeginScrollView(listDisplayRectInner, m_ListScroll, listRect, false, false);

            if (m_EditorCache == null)
                m_EditorCache = new EditorCache(EditorFeatures.PreviewGUI);

            for (int i = 0; i < materials.Length; i++)
            {
                ProceduralMaterial mat = materials[i];
                if (mat == null)
                    continue;

                float x = (i % perRow) * kPreviewWidth;
                float y = (i / perRow) * kPreviewHeight;

                Rect r = new Rect(x, y, kPreviewWidth, kPreviewHeight);
                bool selected = (mat.name == m_SelectedMaterialInstanceName);
                Event evt = Event.current;
                int id = GUIUtility.GetControlID(previewNoDragDropHash, FocusType.Passive, r);

                switch (evt.GetTypeForControl(id))
                {
                    case EventType.Repaint:
                        Rect r2 = r;
                        r2.y = r.yMax - 16;
                        r2.height = 16;
                        m_SubstanceStyles.resultsGridLabel.Draw(r2, EditorGUIUtility.TempContent(mat.name), false, false, selected, selected);
                        break;
                    case EventType.MouseDown:
                        if (evt.button != 0)
                            break;
                        if (r.Contains(evt.mousePosition))
                        {
                            // One click selects the material
                            if (evt.clickCount == 1)
                            {
                                m_SelectedMaterialInstanceName = mat.name;
                                evt.Use();
                            }
                            // Double click opens the SBSAR
                            else if (evt.clickCount == 2)
                            {
                                AssetDatabase.OpenAsset(mat);
                                GUIUtility.ExitGUI();
                                evt.Use();
                            }
                        }
                        break;
                }

                r.height -= 16;
                EditorWrapper p = m_EditorCache[mat];
                p.OnPreviewGUI(r, m_SubstanceStyles.background);
            }

            GUI.EndScrollView();
        }

        public override bool HasPreviewGUI()
        {
            return (GetSelectedMaterialInspector() != null);
        }

        // Show the preview of the selected substance.
        public override void OnPreviewGUI(Rect position, GUIStyle style)
        {
            Editor editor = GetSelectedMaterialInspector();
            if (editor)
                editor.OnPreviewGUI(position, style);
        }

        public override string GetInfoString()
        {
            Editor editor = GetSelectedMaterialInspector();
            if (editor)
                return editor.targetTitle + "\n" + editor.GetInfoString();
            return string.Empty;
        }

        public override void OnPreviewSettings()
        {
            Editor editor = GetSelectedMaterialInspector();
            if (editor)
                editor.OnPreviewSettings();
        }

        public void InstanciatePrototype(object prototypeName)
        {
            m_SelectedMaterialInstanceName = GetImporter().InstantiateMaterial(prototypeName as string);
            ApplyAndRefresh(false);
        }

        public class SubstanceNameComparer : IComparer
        {
            public int Compare(object o1, object o2)
            {
                Object O1 = o1 as Object;
                Object O2 = o2 as Object;
                return EditorUtility.NaturalCompare(O1.name, O2.name);
            }
        }

        private ProceduralMaterial[] GetSortedMaterials()
        {
            SubstanceImporter importer = GetImporter();
            ProceduralMaterial[] materials = importer.GetMaterials();
            System.Array.Sort(materials, new SubstanceNameComparer());
            return materials;
        }

        void MaterialManagement()
        {
            // Get selected material
            SubstanceImporter importer = GetImporter();

            if (m_PrototypeNames == null)
                m_PrototypeNames = importer.GetPrototypeNames();

            ProceduralMaterial selectedMaterial = GetSelectedMaterial();

            GUILayout.BeginHorizontal(m_SubstanceStyles.toolbar);
            {
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                {
                    // Instantiate prototype
                    if (m_PrototypeNames.Length > 1)
                    {
                        Rect dropdownRect = EditorGUILayoutUtilityInternal.GetRect(m_SubstanceStyles.iconToolbarPlus, m_SubstanceStyles.toolbarDropDown);
                        if (EditorGUI.DropdownButton(dropdownRect, m_SubstanceStyles.iconToolbarPlus, FocusType.Passive, m_SubstanceStyles.toolbarDropDown))
                        {
                            GenericMenu menu = new GenericMenu();
                            for (int i = 0; i < m_PrototypeNames.Length; i++)
                            {
                                menu.AddItem(new GUIContent(m_PrototypeNames[i]), false, InstanciatePrototype, m_PrototypeNames[i] as object);
                            }
                            menu.DropDown(dropdownRect);
                        }
                    }
                    else if (m_PrototypeNames.Length == 1)
                    {
                        if (GUILayout.Button(m_SubstanceStyles.iconToolbarPlus, m_SubstanceStyles.toolbarButton))
                        {
                            m_SelectedMaterialInstanceName = GetImporter().InstantiateMaterial(m_PrototypeNames[0]);
                            ApplyAndRefresh(true);
                        }
                    }

                    using (new EditorGUI.DisabledScope(selectedMaterial == null))
                    {
                        // Delete selected instance
                        if (GUILayout.Button(m_SubstanceStyles.iconToolbarMinus, m_SubstanceStyles.toolbarButton))
                        {
                            if (GetSortedMaterials().Length > 1)
                            {
                                SelectNextMaterial();
                                importer.DestroyMaterial(selectedMaterial);
                                ApplyAndRefresh(true);
                            }
                        }

                        // Clone selected instance
                        if (GUILayout.Button(m_SubstanceStyles.iconDuplicate, m_SubstanceStyles.toolbarButton))
                        {
                            string cloneName = importer.CloneMaterial(selectedMaterial);
                            if (cloneName != "")
                            {
                                m_SelectedMaterialInstanceName = cloneName;
                                ApplyAndRefresh(true);
                            }
                        }
                    }
                }
            } EditorGUILayout.EndHorizontal();
        }

        void ApplyAndRefresh(bool exitGUI)
        {
            string path = AssetDatabase.GetAssetPath(target);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUncompressedImport);
            if (exitGUI)
                EditorGUIUtility.ExitGUI();
            Repaint();
        }

        // Init used for static preview rendering
        void Init()
        {
            if (m_PreviewUtility == null)
                m_PreviewUtility = new PreviewRenderUtility();

            if (s_Meshes[0] == null)
            {
                GameObject handleGo = (GameObject)EditorGUIUtility.LoadRequired("Previews/PreviewMaterials.fbx");
                // @TODO: temp workaround to make it not render in the scene
                handleGo.SetActive(false);
                foreach (Transform t in handleGo.transform)
                {
                    var meshFilter = t.GetComponent<MeshFilter>();
                    switch (t.name)
                    {
                        case "sphere":
                            s_Meshes[0] = meshFilter.sharedMesh;
                            break;
                        case "cube":
                            s_Meshes[1] = meshFilter.sharedMesh;
                            break;
                        case "cylinder":
                            s_Meshes[2] = meshFilter.sharedMesh;
                            break;
                        case "torus":
                            s_Meshes[3] = meshFilter.sharedMesh;
                            break;
                        default:
                            Debug.Log("Something is wrong, weird object found: " + t.name);
                            break;
                    }
                }

                s_MeshIcons[0] = EditorGUIUtility.IconContent("PreMatSphere");
                s_MeshIcons[1] = EditorGUIUtility.IconContent("PreMatCube");
                s_MeshIcons[2] = EditorGUIUtility.IconContent("PreMatCylinder");
                s_MeshIcons[3] = EditorGUIUtility.IconContent("PreMatTorus");

                s_LightIcons[0] = EditorGUIUtility.IconContent("PreMatLight0");
                s_LightIcons[1] = EditorGUIUtility.IconContent("PreMatLight1");
            }
        }

        // Note that the static preview for the package is completely different than the dynamic preview.
        // The static preview makes a single image of all the substances, while the dynamic preview
        // redirects to the MaterialEditor dynamic preview for the selected substance.
        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
                return null;

            Init();

            m_PreviewUtility.BeginStaticPreview(new Rect(0, 0, width, height));

            DoRenderPreview(subAssets);

            return m_PreviewUtility.EndStaticPreview();
        }

        // Used for static preview only. See note above.
        protected void DoRenderPreview(Object[] subAssets)
        {
            if (m_PreviewUtility.renderTexture.width <= 0 || m_PreviewUtility.renderTexture.height <= 0)
                return;

            List<ProceduralMaterial> materials = new List<ProceduralMaterial>();
            foreach (Object obj in subAssets)
                if (obj is ProceduralMaterial)
                    materials.Add(obj as ProceduralMaterial);

            int rows = 1;
            int cols = 1;
            while (cols * cols < materials.Count)
                cols++;
            rows = Mathf.CeilToInt(materials.Count / (float)cols);

            m_PreviewUtility.camera.transform.position = -Vector3.forward * 5 * cols;
            m_PreviewUtility.camera.transform.rotation = Quaternion.identity;
            m_PreviewUtility.camera.farClipPlane = 5 * cols + 5.0f;
            m_PreviewUtility.camera.nearClipPlane = 5 * cols - 3.0f;
            m_PreviewUtility.ambientColor = new Color(.2f, .2f, .2f, 0);
            if (lightMode == 0)
            {
                m_PreviewUtility.lights[0].intensity = 1.0f;
                m_PreviewUtility.lights[0].transform.rotation = Quaternion.Euler(30f, 30f, 0);
                m_PreviewUtility.lights[1].intensity = 0;
            }
            else
            {
                m_PreviewUtility.lights[0].intensity = 1.0f;
                m_PreviewUtility.lights[0].transform.rotation = Quaternion.Euler(50f, 50f, 0);
                m_PreviewUtility.lights[1].intensity = 1.0f;
            }

            for (int i = 0; i < materials.Count; i++)
            {
                ProceduralMaterial mat = materials[i];
                Vector3 pos = new Vector3(i % cols - (cols - 1) * 0.5f, -i / cols + (rows - 1) * 0.5f, 0);
                pos *= Mathf.Tan(m_PreviewUtility.camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 5 * 2;
                m_PreviewUtility.DrawMesh(s_Meshes[selectedMesh], pos, Quaternion.Euler(previewDir.y, 0, 0) * Quaternion.Euler(0, previewDir.x, 0), mat, 0);
            }

            m_PreviewUtility.Render();
        }
    }
}

#pragma warning restore CS0618  // Due to Obsolete attribute on Predural classes
