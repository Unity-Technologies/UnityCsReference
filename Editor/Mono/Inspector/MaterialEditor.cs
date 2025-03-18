// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.UIElements;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using UnityEngine.Scripting;
using VirtualTexturing = UnityEngine.Rendering.VirtualTexturing;
using StackValidationResult = UnityEngine.Rendering.VirtualTexturing.EditorHelpers.StackValidationResult;
using System.IO;

namespace UnityEditor
{
    [CustomEditor(typeof(Material))]
    [CanEditMultipleObjects]
    public partial class MaterialEditor : Editor
    {
        private static class Styles
        {
            public static readonly GUIContent reflectionProbePickerIcon = EditorGUIUtility.TrIconContent("ReflectionProbeSelector");
            public static readonly GUIContent lightmapEmissiveLabelRealtimeGISupport = EditorGUIUtility.TrTextContent("Global Illumination", "Controls if the emission is Baked or Realtime.\n\nBaked only has effect in scenes where Baked Global Illumination is enabled.\n\nRealtime uses Realtime Global Illumination if enabled in the scene. Otherwise the emission won't light up other objects.");
            public static readonly GUIContent lightmapEmissiveLabel = EditorGUIUtility.TrTextContent("Global Illumination", "Controls if the emission is Baked or Realtime.\n\nBaked only has effect in scenes where Baked Global Illumination is enabled.\n\nRealtime won't light up other objects since Realtime Global Illumination is not supported.");
            public static GUIContent[] lightmapEmissiveStrings = { EditorGUIUtility.TextContent("Realtime"), EditorGUIUtility.TrTextContent("Baked"), EditorGUIUtility.TrTextContent("None") };
            public static int[]  lightmapEmissiveValues = { (int)MaterialGlobalIlluminationFlags.RealtimeEmissive, (int)MaterialGlobalIlluminationFlags.BakedEmissive, (int)MaterialGlobalIlluminationFlags.None };
            public static string propBlockInfo = EditorGUIUtility.TrTextContent("MaterialPropertyBlock is used to modify these values").text;

            public const int kNewShaderQueueValue = -1;
            public const int kCustomQueueIndex = 4;
            public static readonly GUIContent queueLabel = EditorGUIUtility.TrTextContent("Render Queue");
            public static readonly GUIContent[] queueNames =
            {
                EditorGUIUtility.TrTextContent("From Shader"),
                EditorGUIUtility.TrTextContent("Geometry", "Queue 2000"),
                EditorGUIUtility.TrTextContent("AlphaTest", "Queue 2450"),
                EditorGUIUtility.TrTextContent("Transparent", "Queue 3000"),
            };
            public static readonly int[] queueValues =
            {
                kNewShaderQueueValue,
                (int)UnityEngine.Rendering.RenderQueue.Geometry,
                (int)UnityEngine.Rendering.RenderQueue.AlphaTest,
                (int)UnityEngine.Rendering.RenderQueue.Transparent,
            };
            public static GUIContent[] customQueueNames =
            {
                queueNames[0],
                queueNames[1],
                queueNames[2],
                queueNames[3],
                EditorGUIUtility.TextContent(""), // This name will be overriden during runtime
            };
            public static int[] customQueueValues =
            {
                queueValues[0],
                queueValues[1],
                queueValues[2],
                queueValues[3],
                0, // This value will be overriden during runtime
            };

            public static readonly GUIContent enableInstancingLabel = EditorGUIUtility.TrTextContent("Enable GPU Instancing");
            public static readonly GUIContent doubleSidedGILabel = EditorGUIUtility.TrTextContent("Double Sided Global Illumination", "When enabled, the lightmapper accounts for both sides of the geometry when calculating Global Illumination. Backfaces are not rendered or added to lightmaps, but get treated as valid when seen from other objects. When using the Progressive Lightmapper backfaces bounce light using the same emission and albedo as frontfaces.");
            public static readonly GUIContent emissionLabel = EditorGUIUtility.TrTextContent("Emission");

            public const string undoAssignMaterial = "Assign Material";
            public const string undoAssignSkyboxMaterial = "Assign Skybox Material";

            public static readonly GUIContent parentContent = EditorGUIUtility.TrTextContent("Parent", "Specify the parent of this material.");
            public static readonly GUIContent hierarchyIcon = EditorGUIUtility.IconContent("UnityEditor.SceneHierarchyWindow", "|Open Material Hierarchy Popup."); // right of | means tooltip
            public static readonly GUIContent convertIcon = EditorGUIUtility.IconContent("d_RotateTool", "|This material is in a conversion process."); // right of | means tooltip

            public const string timeUpdateButtonName = "TimeUpdateButton";
            public const string selectedMeshButtonName = "SelectedMeshButton";
            public const string lightModeButtonName = "LightModeButton";
            public const string reflectionProbeDropdownName = "ReflectionProbeDropdown";

            public const int kPadding = 3;
            public const int kHierarchyIconWidth = 44;
            public const float kSpaceForFoldoutArrow = 10f;
        }

        private static readonly List<MaterialEditor> s_MaterialEditors = new List<MaterialEditor>(4);
        private int m_VariantCountCache = -1, m_HasMixedParentCache = -1;
        private bool m_CheckSetup;

        private static int s_ControlHash = "EditorTextField".GetHashCode();
        const float kSpacingUnderTexture = 6f;
        const float kMiniWarningMessageHeight = 27f;

        private MaterialPropertyBlock m_PropertyBlock;

        internal override string targetTitle
        {
            get
            {
                var typeName = AllTargetsAreVariants() ? "Material Variant" : "Material";
                return (!m_AllowMultiObjectAccess || targets.Length == 1) ? target.name + " (" + typeName + ")" : targets.Length + " " + typeName + "s";
            }
        }

        private enum PreviewType
        {
            Mesh = 0,
            Plane = 1,
            Skybox = 2
        }

        private static PreviewType GetPreviewType(Material mat)
        {
            if (mat == null)
                return PreviewType.Mesh;
            var tag = mat.GetTag("PreviewType", false, string.Empty).ToLower();
            if (tag == "plane")
                return PreviewType.Plane;
            if (tag == "skybox")
                return PreviewType.Skybox;
            if (mat.shader != null && mat.shader.name.Contains("Skybox"))
                return PreviewType.Skybox;
            return PreviewType.Mesh;
        }

        private static bool DoesPreviewAllowRotation(PreviewType type)
        {
            return type != PreviewType.Plane;
        }

        public bool isVisible { get { return firstInspectedEditor || InternalEditorUtility.GetIsInspectorExpanded(target); } }

        private Shader m_Shader;

        private string                      m_InfoMessage;
        private Vector2                     m_PreviewDir = new Vector2(0, -20);
        private int                  m_SelectedMesh;
        private int                         m_TimeUpdate;
        private int                         m_LightMode = 1;
        private static readonly GUIContent  s_TilingText = EditorGUIUtility.TrTextContent("Tiling");
        private static readonly GUIContent  s_OffsetText = EditorGUIUtility.TrTextContent("Offset");

        const string kDefaultMaterialPreviewMesh = "DefaultMaterialPreviewMesh";

        private ShaderGUI   m_CustomShaderGUI;
        string              m_CustomEditorClassName;
        public ShaderGUI customShaderGUI { get { return m_CustomShaderGUI; } }

        bool                                m_InsidePropertiesGUI;
        Renderer[]                          m_RenderersForAnimationMode;
        Component                       m_MeshRendererComp;

        internal enum ConvertAction { None, Flatten, Convert }
        internal ConvertAction convertState;

        private bool ShouldEditorBeHidden()
        {
            //When a MeshRenderer is hidden in the inspector, the MaterialEditor should also be hidden
            //as showing a Material in the inspector without its supporting component feels wrong.
            //This is more of a hack than a fix as we shouldnt be creating this editor in the first place when
            //the supporting component is hidden, as the editor creation is a part of serialization and
            //making changes at that level causes more issues this seems to be the best way to handle this case.
            //This fix is for case 1289980

            if (m_MeshRendererComp)
            {
                if (m_MeshRendererComp.hideFlags == HideFlags.HideInInspector)
                    return true;
                return false;
            }

            PropertyEditor property = propertyViewer as PropertyEditor;
            if (!property)
                return false;
            GameObject gameObject = property.tracker.activeEditors[0].target as GameObject;
            if (!gameObject)
                return false;
            foreach (Component comp in gameObject.GetComponents<MeshRenderer>())
            {
                m_MeshRendererComp = comp;
                if (comp.hideFlags == HideFlags.HideInInspector)
                    return true;
            }
            return false;
        }

        private Renderer rendererForAnimationMode
        {
            get
            {
                if (m_RenderersForAnimationMode == null)
                    return null;

                if (m_RenderersForAnimationMode.Length == 0)
                    return null;

                return m_RenderersForAnimationMode[0];
            }
        }

        private struct AnimatedCheckData
        {
            public MaterialProperty property;
            public Rect totalPosition;
            public Color color;
            public AnimatedCheckData(MaterialProperty property, Rect totalPosition, Color color)
            {
                this.property = property;
                this.totalPosition = totalPosition;
                this.color = color;
            }
        }

        private static Stack<AnimatedCheckData> s_AnimatedCheckStack = new Stack<AnimatedCheckData>();

        internal delegate void MaterialPropertyCallbackFunction(GenericMenu menu, MaterialProperty property, Renderer[] renderers);
        internal static MaterialPropertyCallbackFunction contextualPropertyMenu;

        internal class ReflectionProbePicker : PopupWindowContent
        {
            ReflectionProbe m_SelectedReflectionProbe;

            public Transform Target
            {
                get { return m_SelectedReflectionProbe != null ? m_SelectedReflectionProbe.transform : null; }
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(170, EditorGUI.kSingleLineHeight * 3f + 2f);
            }

            public void OnEnable()
            {
                m_SelectedReflectionProbe = EditorUtility.InstanceIDToObject(SessionState.GetInt("PreviewReflectionProbe", 0)) as ReflectionProbe;
            }

            public void OnDisable()
            {
                SessionState.SetInt("PreviewReflectionProbe", m_SelectedReflectionProbe ? m_SelectedReflectionProbe.GetInstanceID() : 0);
            }

            public override void OnGUI(Rect rc)
            {
                EditorGUILayout.LabelField("Select Reflection Probe", EditorStyles.boldLabel);
                EditorGUILayout.Space();
                m_SelectedReflectionProbe = EditorGUILayout.ObjectField("", m_SelectedReflectionProbe, typeof(ReflectionProbe), true) as ReflectionProbe;
            }
        }

        ReflectionProbePicker               m_ReflectionProbePicker = new ReflectionProbePicker();


        public void SetShader(Shader shader)
        {
            SetShader(shader, true);
        }

        public void SetShader(Shader newShader, bool registerUndo)
        {
            bool updateMaterialEditors = false;
            ShaderGUI customEditor = m_CustomShaderGUI;
            CreateCustomShaderEditorIfNeeded(newShader);
            m_Shader = newShader;
            if (customEditor != m_CustomShaderGUI)
            {
                if (customEditor != null)
                {
                    foreach (Material material in targets)
                        customEditor.OnClosed(material);
                }

                updateMaterialEditors = true;
            }

            foreach (Material material in targets)
            {
                Shader oldShader = material.shader;
                Undo.RecordObject(material, "Assign shader");

                BeginNoApplyMaterialPropertyDrawers();
                {
                    if (m_CustomShaderGUI != null)
                        m_CustomShaderGUI.AssignNewShaderToMaterial(material, oldShader, newShader);
                    else
                        material.shader = newShader;
                }
                EndNoApplyMaterialPropertyDrawers();

                EditorMaterialUtility.ResetDefaultTextures(material, false);
                ApplyMaterialPropertyDrawers(material);
                if (m_CustomShaderGUI != null)
                    m_CustomShaderGUI.ValidateMaterial(material);
            }

            if (updateMaterialEditors)
            {
                UpdateAllOpenMaterialEditors();
            }
            else
            {
                // ensure e.g., correctly rebuilt textures (case 879446)
                OnShaderChanged();
            }
        }

        private void UpdateAllOpenMaterialEditors()
        {
            // copy current list contents to array in case it changes during iteration
            foreach (var materialEditor in s_MaterialEditors.ToArray())
                materialEditor.DetectShaderEditorNeedsUpdate();
        }

        // Note: this is called from native code.
        internal void OnSelectedShaderPopup(object shaderNameObj)
        {
            serializedObject.Update();
            var shaderName = (string)shaderNameObj;
            if (!string.IsNullOrEmpty(shaderName))
            {
                var shader = Shader.Find(shaderName);
                if (shader != null)
                    SetShader(shader);
            }

            PropertiesChanged();
        }

        private bool HasMultipleMixedShaderValues()
        {
            bool mixed = false;
            Shader sh = (targets[0] as Material).shader;
            for (int i = 1; i < targets.Length; ++i)
            {
                if (sh != (targets[i] as Material).shader)
                {
                    mixed = true;
                    break;
                }
            }
            return mixed;
        }

        private void ShaderPopup(GUIStyle style)
        {
            bool wasEnabled = GUI.enabled;

            Rect position = EditorGUILayout.GetControlRect();
            position = EditorGUI.PrefixLabel(position, 47385, EditorGUIUtility.TempContent("Shader"));
            EditorGUI.showMixedValue = HasMultipleMixedShaderValues();

            var buttonContent = EditorGUIUtility.TempContent(m_Shader != null ? m_Shader.name : "No Shader Selected");
            if (EditorGUI.DropdownButton(position, buttonContent, FocusType.Keyboard, style))
            {
                var dropdown = new ShaderSelectionDropdown(m_Shader, OnSelectedShaderPopup);
                dropdown.Show(position);
                GUIUtility.ExitGUI();
            }

            EditorGUI.showMixedValue = false;
            GUI.enabled = wasEnabled;
        }

        bool HasMixedParent()
        {
            if (m_HasMixedParentCache != -1)
                return m_HasMixedParentCache == 1;

            m_HasMixedParentCache = 0;
            if (targets.Length != 0)
            {
                var parent = ((Material)targets[0]).parent;
                bool isVariant = ((Material)targets[0]).isVariant;
                for (int i = 1; i < targets.Length; i++)
                {
                    if (((Material)targets[i]).parent != parent || ((Material)targets[i]).isVariant != isVariant)
                    {
                        m_HasMixedParentCache = 1;
                        break;
                    }
                }
            }
            return m_HasMixedParentCache == 1;
        }

        int GetVariantCount()
        {
            if (m_VariantCountCache == -1)
                m_VariantCountCache = GetVariantCount(targets);

            return m_VariantCountCache;
        }

        bool AllTargetsAreVariants()
        {
            return GetVariantCount() == targets.Length;
        }

        // returns true if mat is a child of any element of the targets array
        private static bool IsChildOfAnyTarget(Material mat, Object[] targets)
        {
            foreach (var target in targets)
            {
                if (mat == target || mat.IsChildOf(target as Material))
                    return true;
            }
            return false;
        }

        private static Material HandleParentDragAndDrop(Rect rect, int controlID, EventType eventType, Object[] targets)
        {
            Material parent = (targets[0] as Material).parent;

            // We handle drag and drop ourselves because we accept assets containing a material artifact
            if (eventType == EventType.DragUpdated || eventType == EventType.DragPerform)
            {
                if (rect.Contains(Event.current.mousePosition) && GUI.enabled)
                {
                    Object[] references = DragAndDrop.objectReferences;
                    Material validatedObject = EditorGUI.ValidateObjectFieldAssignment(references, typeof(Material), null, EditorGUI.ObjectFieldValidatorOptions.None) as Material;
                    if (validatedObject == null)
                    {
                        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(validatedObject)))
                        {
                            validatedObject = asset as Material;
                            if (validatedObject)
                                break;
                        }
                    }
                    if (validatedObject != null && !IsChildOfAnyTarget(validatedObject, targets))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                        if (eventType == EventType.DragPerform)
                        {
                            GUI.changed = true;
                            DragAndDrop.AcceptDrag();
                            DragAndDrop.activeControlID = 0;
                            parent = validatedObject;
                        }
                        else
                            DragAndDrop.activeControlID = controlID;
                    }
                    Event.current.Use();
                }
            }
            return parent;
        }

        private static bool HasMissingParent(Material material)
        {
            return material.isVariant && material.parent == null;
        }

        private static Material DoParentObjectField(Rect rect, Object[] targets)
        {
            // This is an augmented object field, preventing cyclic dependencies in the material hierarchy, handling missing parent, and with custom drap and drop rejection

            int controlID = GUIUtility.GetControlID(FocusType.Keyboard, rect);
            var eventType = Event.current.type;

            Material parent = HandleParentDragAndDrop(rect, controlID, eventType, targets);

            if (eventType == EventType.Repaint && HasMissingParent(targets[0] as Material))
            {
                GUIContent content = EditorGUIUtility.TempContent("Missing (Material)");

                var mousePos = Event.current.mousePosition;
                Rect buttonRect = EditorStyles.objectFieldButton.margin.Remove(new Rect(rect.xMax - 19, rect.y, 19, rect.height));

                EditorGUI.BeginHandleMixedValueContentColor();
                EditorStyles.objectField.Draw(rect, content, controlID, DragAndDrop.activeControlID == controlID, rect.Contains(mousePos));
                EditorStyles.objectFieldButton.Draw(buttonRect, GUIContent.none, controlID, DragAndDrop.activeControlID == controlID, buttonRect.Contains(mousePos));
                EditorGUI.EndHandleMixedValueContentColor();
                return parent;
            }

            return EditorGUI.DoObjectField(rect, rect, controlID, parent, null, typeof(Material), null, true) as Material;
        }

        internal void ParentField(Rect rect, bool showMixedValue = false)
        {
            rect = EditorGUI.PrefixLabel(rect, Styles.parentContent);

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = showMixedValue;
            var parent = DoParentObjectField(rect, targets);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                convertState = ConvertAction.None;
                Undo.RecordObjects(targets, "Assign parent");
                foreach (Material target in targets)
                    target.parent = parent;
            }
        }

        private bool DoHierarchyButton()
        {
            bool clicked = EditorGUILayout.DropdownButton(GUIContent.none, FocusType.Passive, GUILayout.MaxWidth(Styles.kHierarchyIconWidth));

            Rect rect = GUILayoutUtility.topLevel.GetLast();
            rect.x += 6;
            if (convertState == ConvertAction.Convert)
            {
                float height = rect.height;
                rect.height *= 0.8f;
                rect.y += (height - rect.height) * 0.5f;
                EditorGUI.LabelField(rect, Styles.convertIcon);
            }
            else
                EditorGUI.LabelField(rect, Styles.hierarchyIcon);

            return clicked;
        }

        private class ShaderSelectionDropdown : AdvancedDropdown
        {
            Action<object> m_OnSelectedShaderPopup;
            Shader m_CurrentShader;

            public ShaderSelectionDropdown(Shader shader, Action<object> onSelectedShaderPopup)
                : base(new AdvancedDropdownState())
            {
                minimumSize = new Vector2(270, 308);
                m_CurrentShader = shader;
                m_OnSelectedShaderPopup = onSelectedShaderPopup;
                m_DataSource = new ShaderDropdownDataSource(m_CurrentShader);
                m_Gui = new MaterialDropdownGUI(m_DataSource);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                return m_DataSource.mainTree;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                m_OnSelectedShaderPopup(((ShaderDropdownItem)item).fullName);
            }

            internal class ShaderDropdownDataSource : AdvancedDropdownDataSource
            {
                Shader m_CurrentShader;
                internal ShaderDropdownDataSource(Shader currentShader) { m_CurrentShader = currentShader; }

                protected override AdvancedDropdownItem FetchData()
                {
                    m_SearchableElements = new List<AdvancedDropdownItem>();
                    var root = new AdvancedDropdownItem("Shaders");

                    var shaders = ShaderUtil.GetAllShaderInfo();
                    var shaderList = new List<string>();
                    var legacyList = new List<string>();
                    var notSupportedList = new List<string>();
                    var failedCompilationList = new List<string>();

                    foreach (var shader in shaders)
                    {
                        if (shader.name.StartsWith("Deprecated") || shader.name.StartsWith("Hidden"))
                        {
                            continue;
                        }
                        if (shader.hasErrors)
                        {
                            failedCompilationList.Add(shader.name);
                            continue;
                        }
                        if (!shader.supported)
                        {
                            notSupportedList.Add(shader.name);
                            continue;
                        }
                        if (shader.name.StartsWith("Legacy Shaders/"))
                        {
                            legacyList.Add(shader.name);
                            continue;
                        }
                        shaderList.Add(shader.name);
                    }

                    shaderList.Sort();
                    var unnestedList =  shaderList.Where(s => s.Count(c => c == '/') == 0).ToList();
                    shaderList = shaderList.Where(s => s.Count(c => c == '/') > 0).ToList();
                    shaderList.AddRange(unnestedList);

                    legacyList.Sort();
                    notSupportedList.Sort();
                    failedCompilationList.Sort();

                    shaderList.ForEach(s => AddShaderToMenu("", root, s, s));
                    if (legacyList.Any() || notSupportedList.Any() || failedCompilationList.Any())
                        root.AddSeparator();
                    legacyList.ForEach(s => AddShaderToMenu("", root, s, s));
                    notSupportedList.ForEach(s => AddShaderToMenu("Not supported/", root, s, "Not supported/" + s));
                    failedCompilationList.ForEach(s => AddShaderToMenu("Failed to compile/", root, s, "Failed to compile/" + s));

                    return root;
                }

                private void AddShaderToMenu(string prefix, AdvancedDropdownItem parent, string fullShaderName, string shaderName)
                {
                    var shaderNameParts = shaderName.Split('/');
                    if (shaderNameParts.Length > 1)
                    {
                        AddShaderToMenu(prefix, FindOrCreateChild(parent, shaderName), fullShaderName, shaderName.Substring(shaderNameParts[0].Length + 1));
                    }
                    else
                    {
                        var item = new ShaderDropdownItem(prefix, fullShaderName, shaderName);
                        parent.AddChild(item);
                        m_SearchableElements.Add(item);
                        if (m_CurrentShader != null && m_CurrentShader.name == fullShaderName)
                        {
                            selectedIDs.Add(item.id);
                        }
                    }
                }

                private AdvancedDropdownItem FindOrCreateChild(AdvancedDropdownItem parent, string path)
                {
                    var shaderNameParts = path.Split('/');
                    var group = shaderNameParts[0];
                    foreach (var child in parent.children)
                    {
                        if (child.name == group)
                            return child;
                    }

                    var item = new AdvancedDropdownItem(group);
                    parent.AddChild(item);
                    return item;
                }

                protected override AdvancedDropdownItem Search(string searchString)
                {
                    if (string.IsNullOrEmpty(searchString) || m_SearchableElements == null)
                        return null;

                    // Support multiple search words separated by spaces.
                    var searchWords = searchString.ToLower().Split(' ');
                    var matched = new List<List<AdvancedDropdownItem>>();
                    matched.Add(new List<AdvancedDropdownItem>());

                    bool found = false;
                    foreach (var e in m_SearchableElements)
                    {
                        if (e.children.Any())
                            continue;

                        var menuItem = (ShaderDropdownItem)e;
                        var name = menuItem.fullName.ToLower().Replace(" ", "");
                        if (AddMatchItem(e, name, searchWords, matched))
                            found = true;
                    }
                    if (!found)
                    {
                        foreach (var e in m_SearchableElements)
                        {
                            var menuItem = (ShaderDropdownItem)e;
                            var name = menuItem.fullName.Replace(" ", "");
                            AddMatchItem(e, name, searchWords, matched);
                        }
                    }

                    var searchTree = new AdvancedDropdownItem("Shaders");
                    for (int i = 0; i < matched.Count; i++)
                    {
                        matched[i].Sort((s2, s1) =>
                        {
                            var order = (int)((s2 as ShaderDropdownItem)?.fullName.Count(c => c == '/') - (s1 as ShaderDropdownItem)?.fullName.Count(c => c == '/'));
                            if (order == 0)
                                order = s2.name.CompareTo(s1.name);
                            return order;
                        });


                        foreach (var element in matched[i])
                        {
                            searchTree.AddChild(element);
                        }
                    }

                    return searchTree;
                }

                protected bool AddMatchItem(AdvancedDropdownItem e, string fullName, string[] searchWords, List<List<AdvancedDropdownItem>> matched)
                {
                    int index = -1;
                    var splitName = searchWords[0].Contains('/') && searchWords.Length == 1 ? new string[1] { fullName } : fullName.Split('/');

                    // See if we match ALL the search words.
                    for (var w = 0; w < searchWords.Length; w++)
                    {
                        index = -1;

                        for (int i = 0; i < splitName.Length; i++)
                        {
                            if (splitName[i].Contains(searchWords[w]))
                            {
                                if(i >= index)
                                    index = i;
                            }
                            else if(i == splitName.Length - 1 && index == -1)
                                return false;
                        }
                    }

                    // index 0 is reserved for Shader name matches thus the actual index is always higher by one
                    index++;

                    if (index == splitName.Length)
                        index = 0;

                    if (index >= matched.Count)
                    {
                        for (int i = matched.Count; i <= index; i++)
                        {
                            matched.Add(new List<AdvancedDropdownItem>());
                        }
                    }

                    matched[index].Add(e);
                    return true;
                }
            }

            private class ShaderDropdownItem : AdvancedDropdownItem
            {
                string m_FullName;
                string m_Prefix;
                public string fullName => m_FullName;
                public string prefix => m_Prefix;

                public ShaderDropdownItem(string prefix, string fullName, string shaderName)
                    : base(shaderName)
                {
                    m_FullName = fullName;
                    m_Prefix = prefix;
                    id = (prefix + fullName + shaderName).GetHashCode();
                }
            }

            private class MaterialDropdownGUI : AdvancedDropdownGUI
            {
                public MaterialDropdownGUI(AdvancedDropdownDataSource dataSource)
                    : base(dataSource) { m_DataSource = dataSource; }

                AdvancedDropdownDataSource m_DataSource;

                private static class Styles
                {
                    internal static GUIStyle lineStyleFaint = new GUIStyle("DD ItemStyle");
                    internal static GUIStyle checkMark = "DD ItemCheckmark";
                    public static GUIContent checkMarkContent = new GUIContent("✔");

                    static Styles()
                    {
                        float val = EditorGUIUtility.isProSkin ? 0.5f : 0.25f;
                        lineStyleFaint.active.textColor = new Color(val, val, val, 1f);
                        lineStyleFaint.focused.textColor = new Color(val, val, val, 1f);
                        lineStyleFaint.hover.textColor = new Color(val, val, val, 1f);
                        lineStyleFaint.normal.textColor = new Color(val, val, val, 1f);
                    }
                }

                internal override void DrawItem(AdvancedDropdownItem item, string name, Texture2D icon, bool enabled, bool drawArrow, bool selected, bool hasSearch)
                {
                    var newScriptItem = item as ShaderDropdownItem;
                    string text;
                    if (hasSearch && newScriptItem != null)
                    {
                        DrawSearchItem(item, newScriptItem.name, newScriptItem.prefix + newScriptItem.fullName, selected);
                        text = newScriptItem.fullName.Contains('/') ? $"{name} ({newScriptItem.fullName})" : name;
                    }
                    else
                    {
                        text = name;
                        base.DrawItem(item, name, icon, enabled, drawArrow, selected, hasSearch);
                    }

                    if (Event.current.type == EventType.Repaint && hasSearch)
                    {
                        var tooltipRect = GUILayoutUtility.GetLastRect();
                        GUI.Label(tooltipRect, new GUIContent("", text));
                    }
                }

                private void DrawSearchItem(AdvancedDropdownItem item, string name, string path, bool selected)
                {
                    path = $" ({path.Substring(0, path.Length - name.Length).Trim('/')})";
                    path = path.Equals(" ()") ? "" : path; // if the path we're left with is equal to " ()" - scrap it

                    var contentWithIcon = new GUIContent(name, path);

                    var rect = GUILayoutUtility.GetRect(contentWithIcon, Styles.lineStyleFaint, GUILayout.ExpandWidth(true));
                    var fullRect = new Rect(rect);

                    if (Event.current.type != EventType.Repaint)
                        return;

                    bool checkMark = false;
                    if (m_DataSource.selectedIDs.Any() && m_DataSource.selectedIDs.Contains(item.id))
                    {
                        checkMark = true;
                        var checkMarkRect = new Rect(fullRect);
                        checkMarkRect.width = iconSize.x + 1;
                        Styles.checkMark.Draw(checkMarkRect, Styles.checkMarkContent, selected, selected, selected, selected);
                    }

                    var emptySpace = Styles.lineStyleFaint.CalcSize(contentWithIcon);
                    rect.x += emptySpace.x + iconSize.x + 1;

                    if (!checkMark)
                        lineStyle.Draw(fullRect, new GUIContent(), selected, selected, selected, selected);

                    fullRect.x += iconSize.x + 1;
                    lineStyle.Draw(fullRect, contentWithIcon, selected & checkMark, selected & checkMark, selected & checkMark, selected & checkMark);

                    Styles.lineStyleFaint.Draw(rect, new GUIContent(path, path), selected, false, false, false);
                }
            }
        }

        public virtual void Awake()
        {
            if (GetPreviewType(target as Material) == PreviewType.Skybox)
                m_PreviewDir = new Vector2(0, 50);

            m_SelectedMesh = EditorPrefs.GetInt(kDefaultMaterialPreviewMesh);
        }

        private void DetectShaderEditorNeedsUpdate()
        {
            var material = target as Material;
            bool shaderChanged = material && material.shader != m_Shader;
            bool customEditorChanged = material && material.shader && m_CustomEditorClassName != ShaderUtil.GetCurrentCustomEditor(material.shader);
            if (shaderChanged || customEditorChanged)
            {
                CreateCustomShaderEditorIfNeeded(material.shader);
                if (shaderChanged)
                {
                    m_Shader = material.shader;
                    OnShaderChanged();
                }
                InspectorWindow.RepaintAllInspectors();
            }
        }

        private string ParseValidationResult(StackValidationResult validationResult)
        {
            string[] errorMessages = validationResult.errorMessage.Split('\n');

            string result = "'" + validationResult.stackName + "' is invalid";
            if (errorMessages.Length == 1)
                result += " (1 issue)\n";
            else
                result += " (" + errorMessages.Length + " issues)\n";

            for (int i = 0; i < errorMessages.Length; ++i)
            {
                result += " - " + errorMessages[i] + '\n';
            }

            return result;
        }

        private void DetectTextureStackValidationIssues()
        {
            if (PlayerSettings.GetVirtualTexturingSupportEnabled())
            {
                if (isVisible && m_Shader != null && !HasMultipleMixedShaderValues())
                {
                    // We want additional spacing, but only when the material properties are visible
                    EditorGUILayout.Space(EditorGUIUtility.singleLineHeight / 2.0f);
                }

                // We don't want these message boxes to be indented
                EditorGUI.indentLevel--;

                var material = target as Material;
                StackValidationResult[] stackValidationResults = VirtualTexturing.EditorHelpers.ValidateMaterialTextureStacks(material);
                if (stackValidationResults.Length == 0)
                    return;

                foreach (StackValidationResult validationResult in stackValidationResults)
                {
                    string errorBoxText = ParseValidationResult(validationResult);
                    EditorGUILayout.HelpBox(errorBoxText, MessageType.Error);
                }

                // Reset the original indentation level
                EditorGUI.indentLevel++;
            }
        }

        public override void OnInspectorGUI()
        {
            if (ShouldEditorBeHidden())
                return;

            serializedObject.Update();

            CheckSetup();
            DetectShaderEditorNeedsUpdate();

            if (isVisible && m_Shader != null && !HasMultipleMixedShaderValues())
            {
                // Show Material properties
                if (PropertiesGUI())
                {
                    foreach (Material material in targets)
                    {
                        if (m_CustomShaderGUI != null)
                            m_CustomShaderGUI.ValidateMaterial(material);
                    }
                    PropertiesChanged();
                }
            }

            DetectTextureStackValidationIssues();
        }

        void CheckSetup()
        {
            if (!m_CheckSetup || m_Shader == null)
                return;

            m_CheckSetup = false;
            if (m_CustomShaderGUI == null && !IsMaterialEditor(ShaderUtil.GetCurrentCustomEditor(m_Shader)))
            {
                Debug.LogWarningFormat("Could not create a custom UI for the shader '{0}'. The shader has the following: 'CustomEditor = {1}'. Does the custom editor specified include its namespace? And does the class either derive from ShaderGUI or MaterialEditor?", m_Shader.name, ShaderUtil.GetCurrentCustomEditor(m_Shader));
            }
        }

        // A minimal list of settings to be shown in the Asset Store preview inspector
        internal override void OnAssetStoreInspectorGUI()
        {
            OnInspectorGUI();
        }

        public void PropertiesChanged()
        {
            // @TODO: Show performance warnings for multi-selections too?
            m_InfoMessage = null;
            if (targets.Length == 1)
                m_InfoMessage = Utils.PerformanceChecks.CheckMaterial(target as Material, EditorUserBuildSettings.activeBuildTarget);
        }

        protected virtual void OnShaderChanged()
        {
        }

        protected override void OnHeaderGUI()
        {
            if (ShouldEditorBeHidden())
                return;

            Rect titleRect = DrawHeaderGUI(this, targetTitle, firstInspectedEditor ? 0 : Styles.kSpaceForFoldoutArrow);
            int id = GUIUtility.GetControlID(45678, FocusType.Passive);

            if (!firstInspectedEditor)
            {
                Rect renderRect = EditorGUI.GetInspectorTitleBarObjectFoldoutRenderRect(titleRect);
                renderRect.y = titleRect.yMax - 17f; // align with bottom
                bool oldVisible = InternalEditorUtility.GetIsInspectorExpanded(target);
                bool newVisible = EditorGUI.DoObjectFoldout(oldVisible, titleRect, renderRect, targets, id);

                // Toggle visibility
                if (newVisible != oldVisible)
                    InternalEditorUtility.SetIsInspectorExpanded(target, newVisible);
            }
        }

        internal override void OnHeaderControlsGUI()
        {
            // Clear cache in case material is modified externally
            m_VariantCountCache = -1;
            m_HasMixedParentCache = -1;

            if (ShouldEditorBeHidden())
                return;

            serializedObject.Update();

            var oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 50;

            // Shader selection dropdown
            using (new EditorGUI.DisabledScope(!IsEnabled() || GetVariantCount() != 0))
                ShaderPopup("MiniPulldown");

            // Edit button for custom shaders
            if (m_Shader != null && !HasMultipleMixedShaderValues() && (m_Shader.hideFlags & HideFlags.DontSave) == 0)
            {
                if (GUILayout.Button("Edit...", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    AssetDatabase.OpenAsset(m_Shader);
            }

            using (new EditorGUI.DisabledScope(targets.Length != 1))
            {
                if (DoHierarchyButton())
                {
                    var rect = GUILayoutUtility.topLevel.GetLast();
                    PopupWindow.Show(rect, new MaterialHierarchyPopup(target as Material, IsEnabled(), this, rect));
                    GUIUtility.ExitGUI();
                }
            }

            if (AllTargetsAreVariants() || convertState == ConvertAction.Convert)
            {
                // Start a new line for parent
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                if (!firstInspectedEditor)
                    GUILayout.Space(Styles.kSpaceForFoldoutArrow);
                using (new EditorGUI.DisabledScope(!IsEnabled()))
                    ParentField(EditorGUILayout.GetControlRect(), HasMixedParent());
            }

            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        // -------- obsolete helper functions to get/set material values

        [System.Obsolete("Use GetMaterialProperty instead.")]
        public float GetFloat(string propertyName, out bool hasMixedValue)
        {
            hasMixedValue = false;
            float f = ((Material)targets[0]).GetFloat(propertyName);
            for (int i = 1; i < targets.Length; i++)
            {
                if (((Material)targets[i]).GetFloat(propertyName) != f)
                {
                    hasMixedValue = true;
                    break;
                }
            }
            return f;
        }

        [System.Obsolete("Use MaterialProperty instead.")]
        public void SetFloat(string propertyName, float value)
        {
            foreach (Material material in targets)
                material.SetFloat(propertyName, value);
        }

        [System.Obsolete("Use GetMaterialProperty instead.")]
        public Color GetColor(string propertyName, out bool hasMixedValue)
        {
            hasMixedValue = false;
            Color f = ((Material)targets[0]).GetColor(propertyName);
            for (int i = 1; i < targets.Length; i++)
                if (((Material)targets[i]).GetColor(propertyName) != f) { hasMixedValue = true; break; }
            return f;
        }

        [System.Obsolete("Use MaterialProperty instead.")]
        public void SetColor(string propertyName, Color value)
        {
            foreach (Material material in targets)
                material.SetColor(propertyName, value);
        }

        [System.Obsolete("Use GetMaterialProperty instead.")]
        public Vector4 GetVector(string propertyName, out bool hasMixedValue)
        {
            hasMixedValue = false;
            Vector4 f = ((Material)targets[0]).GetVector(propertyName);
            for (int i = 1; i < targets.Length; i++)
                if (((Material)targets[i]).GetVector(propertyName) != f) { hasMixedValue = true; break; }
            return f;
        }

        [System.Obsolete("Use MaterialProperty instead.")]
        public void SetVector(string propertyName, Vector4 value)
        {
            foreach (Material material in targets)
                material.SetVector(propertyName, value);
        }

        [System.Obsolete("Use GetMaterialProperty instead.")]
        public Texture GetTexture(string propertyName, out bool hasMixedValue)
        {
            hasMixedValue = false;
            Texture f = ((Material)targets[0]).GetTexture(propertyName);
            for (int i = 1; i < targets.Length; i++)
                if (((Material)targets[i]).GetTexture(propertyName) != f) { hasMixedValue = true; break; }
            return f;
        }

        [System.Obsolete("Use MaterialProperty instead.")]
        public void SetTexture(string propertyName, Texture value)
        {
            foreach (Material material in targets)
                material.SetTexture(propertyName, value);
        }

        [System.Obsolete("Use MaterialProperty instead.")]
        public Vector2 GetTextureScale(string propertyName, out bool hasMixedValueX, out bool hasMixedValueY)
        {
            hasMixedValueX = false;
            hasMixedValueY = false;
            Vector2 f = ((Material)targets[0]).GetTextureScale(propertyName);
            for (int i = 1; i < targets.Length; i++)
            {
                Vector2 f2 = ((Material)targets[i]).GetTextureScale(propertyName);
                if (f2.x != f.x) { hasMixedValueX = true; }
                if (f2.y != f.y) { hasMixedValueY = true; }
                if (hasMixedValueX && hasMixedValueY)
                    break;
            }
            return f;
        }

        [System.Obsolete("Use MaterialProperty instead.")]
        public Vector2 GetTextureOffset(string propertyName, out bool hasMixedValueX, out bool hasMixedValueY)
        {
            hasMixedValueX = false;
            hasMixedValueY = false;
            Vector2 f = ((Material)targets[0]).GetTextureOffset(propertyName);
            for (int i = 1; i < targets.Length; i++)
            {
                Vector2 f2 = ((Material)targets[i]).GetTextureOffset(propertyName);
                if (f2.x != f.x) { hasMixedValueX = true; }
                if (f2.y != f.y) { hasMixedValueY = true; }
                if (hasMixedValueX && hasMixedValueY)
                    break;
            }
            return f;
        }

        [System.Obsolete("Use MaterialProperty instead.")]
        public void SetTextureScale(string propertyName, Vector2 value, int coord)
        {
            foreach (Material material in targets)
            {
                Vector2 f = material.GetTextureScale(propertyName);
                f[coord] = value[coord];
                material.SetTextureScale(propertyName, f);
            }
        }

        [System.Obsolete("Use MaterialProperty instead.")]
        public void SetTextureOffset(string propertyName, Vector2 value, int coord)
        {
            foreach (Material material in targets)
            {
                Vector2 f = material.GetTextureOffset(propertyName);
                f[coord] = value[coord];
                material.SetTextureOffset(propertyName, f);
            }
        }

        // -------- helper functions to handle material variant overrides

        internal static int GetVariantCount(Object[] targets)
        {
            int count = 0;
            foreach (Material target in targets)
                count += target.isVariant ? 1 : 0;
            return count;
        }

        static bool AllTargetsAreVariants(Object[] targets)
        {
            return GetVariantCount(targets) == targets.Length;
        }

        static MaterialSerializedProperty GetMaterialSerializedProperty(SerializedProperty property)
        {
            if (property.propertyPath == "m_LightmapFlags")
                return MaterialSerializedProperty.LightmapFlags;
            if (property.propertyPath == "m_EnableInstancingVariants")
                return MaterialSerializedProperty.EnableInstancingVariants;
            if (property.propertyPath == "m_DoubleSidedGI")
                return MaterialSerializedProperty.DoubleSidedGI;
            if (property.propertyPath == "m_CustomRenderQueue")
                return MaterialSerializedProperty.CustomRenderQueue;
            throw new ArgumentException(string.Format("The SerializedProperty '{0}' is not supported by BeginProperty.", property.propertyPath));
        }

        internal static void BeginProperty(MaterialSerializedProperty property, Object[] targets)
        {
            MaterialProperty.BeginProperty(property, targets);
        }

        internal static void BeginProperty(Rect rect, MaterialSerializedProperty property, Object[] targets)
        {
            MaterialProperty.BeginProperty(rect, null, property, targets);
        }

        public static void BeginProperty(SerializedProperty property)
        {
            MaterialProperty.BeginProperty(GetMaterialSerializedProperty(property), property.serializedObject.targetObjects);
        }

        public static void BeginProperty(Rect rect, SerializedProperty property)
        {
            MaterialProperty.BeginProperty(rect, null, GetMaterialSerializedProperty(property), property.serializedObject.targetObjects);
        }

        public static void BeginProperty(MaterialProperty property)
        {
            MaterialProperty.BeginProperty(property, property.targets);
        }

        public static void BeginProperty(Rect rect, MaterialProperty property)
        {
            MaterialProperty.BeginProperty(rect, property, 0, property.targets);
        }

        public static void EndProperty()
        {
            MaterialProperty.EndProperty();
        }

        // -------- helper functions to display common material controls

        // The 'Property' methods that accept GUIContent are internal with different name to avoid
        // breaking backwards compatibility caused by adding overloads:
        // 'RangeProperty(prop, null);' would cause a compile error because of ambigious overloads.

        public float RangeProperty(MaterialProperty prop, string label)
        {
            return RangePropertyInternal(prop, new GUIContent(label));
        }

        internal float RangePropertyInternal(MaterialProperty prop, GUIContent label)
        {
            Rect r = GetPropertyRect(prop, label, true);
            return RangePropertyInternal(r, prop, label);
        }

        public float RangeProperty(Rect position, MaterialProperty prop, string label)
        {
            return RangePropertyInternal(position, prop, new GUIContent(label));
        }

        internal static float RangePropertyInternal(Rect position, MaterialProperty prop, GUIContent label)
        {
            return DoPowerRangeProperty(position, prop, label, 1f);
        }

        internal static float DoPowerRangeProperty(Rect position, MaterialProperty prop, GUIContent label, float power)
        {
            BeginProperty(position, prop);

            EditorGUI.BeginChangeCheck();

            // For range properties we want to show the slider so we adjust label width to use default width (setting it to 0)
            // See SetDefaultGUIWidths where we set: EditorGUIUtility.labelWidth = GUIClip.visibleRect.width - EditorGUIUtility.fieldWidth - 17;
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 0f;

            // fix for case 1245429 where we sometimes get a rounding issue when converting between gamma and linear, which causes us to break the slider
            // we need to check if the range is inverted before clamping (case 1351151)
            bool invert = prop.rangeLimits.x > prop.rangeLimits.y;
            float value = Mathf.Clamp(prop.floatValue, invert ? prop.rangeLimits.y : prop.rangeLimits.x, invert ? prop.rangeLimits.x : prop.rangeLimits.y);

            float newValue = EditorGUI.PowerSlider(position, label, value, prop.rangeLimits.x, prop.rangeLimits.y, power);

            EditorGUIUtility.labelWidth = oldLabelWidth;

            if (EditorGUI.EndChangeCheck())
                prop.floatValue = newValue;

            EndProperty();

            return prop.floatValue;
        }

        internal static int DoIntRangeProperty(Rect position, MaterialProperty prop, GUIContent label)
        {
            BeginProperty(position, prop);

            EditorGUI.BeginChangeCheck();

            // For range properties we want to show the slider so we adjust label width to use default width (setting it to 0)
            // See SetDefaultGUIWidths where we set: EditorGUIUtility.labelWidth = GUIClip.visibleRect.width - EditorGUIUtility.fieldWidth - 17;
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 0f;

            int newValue = EditorGUI.IntSlider(position, label, (int)prop.floatValue, (int)prop.rangeLimits.x, (int)prop.rangeLimits.y);

            EditorGUIUtility.labelWidth = oldLabelWidth;

            if (EditorGUI.EndChangeCheck())
                prop.floatValue = (float)newValue;

            EndProperty();

            return (int)prop.floatValue;
        }

        public int IntegerProperty(MaterialProperty prop, string label)
        {
            return IntegerPropertyInternal(prop, new GUIContent(label));
        }

        internal int IntegerPropertyInternal(MaterialProperty prop, GUIContent label)
        {
            Rect r = GetPropertyRect(prop, label, true);
            return IntegerPropertyInternal(r, prop, label);
        }

        public int IntegerProperty(Rect position, MaterialProperty prop, string label)
        {
            return IntegerPropertyInternal(position, prop, new GUIContent(label));
        }

        internal int IntegerPropertyInternal(Rect position, MaterialProperty prop, GUIContent label)
        {
            BeginProperty(position, prop);

            EditorGUI.BeginChangeCheck();
            int newValue = EditorGUI.IntField(position, label, prop.intValue);
            if (EditorGUI.EndChangeCheck())
                prop.intValue = newValue;

            EndProperty();

            return prop.intValue;
        }

        public float FloatProperty(MaterialProperty prop, string label)
        {
            return FloatPropertyInternal(prop, new GUIContent(label));
        }

        internal float FloatPropertyInternal(MaterialProperty prop, GUIContent label)
        {
            Rect r = GetPropertyRect(prop, label, true);
            return FloatPropertyInternal(r, prop, label);
        }

        public float FloatProperty(Rect position, MaterialProperty prop, string label)
        {
            return FloatPropertyInternal(position, prop, new GUIContent(label));
        }

        internal static float FloatPropertyInternal(Rect position, MaterialProperty prop, GUIContent label)
        {
            BeginProperty(position, prop);

            EditorGUI.BeginChangeCheck();
            float newValue = EditorGUI.FloatField(position, label, prop.floatValue);
            if (EditorGUI.EndChangeCheck())
                prop.floatValue = newValue;

            EndProperty();

            return prop.floatValue;
        }

        public Color ColorProperty(MaterialProperty prop, string label)
        {
            return ColorPropertyInternal(prop, new GUIContent(label));
        }

        internal Color ColorPropertyInternal(MaterialProperty prop, GUIContent label)
        {
            Rect r = GetPropertyRect(prop, label, true);
            return ColorPropertyInternal(r, prop, label);
        }

        public Color ColorProperty(Rect position, MaterialProperty prop, string label)
        {
            return ColorPropertyInternal(position, prop, new GUIContent(label));
        }

        internal static Color ColorPropertyInternal(Rect position, MaterialProperty prop, GUIContent label)
        {
            BeginProperty(position, prop);

            EditorGUI.BeginChangeCheck();
            bool isHDR = ((prop.propertyFlags & ShaderPropertyFlags.HDR) != 0);
            bool showAlpha = true;
            Color newValue = EditorGUI.ColorField(position, label, prop.colorValue, true, showAlpha, isHDR);
            if (EditorGUI.EndChangeCheck())
                prop.colorValue = newValue;

            EndProperty();

            return prop.colorValue;
        }

        public Vector4 VectorProperty(MaterialProperty prop, string label)
        {
            return VectorProperty(prop, new GUIContent(label));
        }

        public Vector4 VectorProperty(MaterialProperty prop, GUIContent label)
        {
            Rect r = GetPropertyRect(prop, label, true);
            return VectorProperty(r, prop, label);
        }

        public Vector4 VectorProperty(Rect position, MaterialProperty prop, string label)
        {
            return VectorPropertyInternal(position, prop, new GUIContent(label));
        }

        public Vector4 VectorProperty(Rect position, MaterialProperty prop, GUIContent label)
        {
            return VectorPropertyInternal(position, prop, label);
        }

        internal static Vector4 VectorPropertyInternal(in Rect position, in MaterialProperty prop, in GUIContent label)
        {
            BeginProperty(position, prop);

            EditorGUI.BeginChangeCheck();

            // We want to make room for the field in case it's drawn on the same line as the label
            // Set label width to default width (zero) temporarily
            var oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 0f;

            Vector4 newValue = EditorGUI.Vector4Field(position, label, prop.vectorValue);

            EditorGUIUtility.labelWidth = oldLabelWidth;

            if (EditorGUI.EndChangeCheck())
                prop.vectorValue = newValue;

            EndProperty();

            return prop.vectorValue;
        }

        // Using GUILayout
        public void TextureScaleOffsetProperty(MaterialProperty property)
        {
            Rect rect = EditorGUILayout.GetControlRect(true, 2 * (kLineHeight + EditorGUI.kVerticalSpacingMultiField), EditorStyles.layerMaskField);
            TextureScaleOffsetProperty(rect, property, false);
        }

        // Returns height used
        public float TextureScaleOffsetProperty(Rect position, MaterialProperty property)
        {
            return TextureScaleOffsetProperty(position, property, true);
        }

        // Returns height used
        public float TextureScaleOffsetProperty(Rect position, MaterialProperty property, bool partOfTexturePropertyControl)
        {
            BeginAnimatedCheck(position, property);

            BeginProperty(position, property);

            EditorGUI.BeginChangeCheck();
            // Mixed value mask is 4 bits for the uv offset & scale (First bit is for the texture itself)
            int mixedValuemask = property.mixedValueMask >> 1;
            Vector4 scaleAndOffset = TextureScaleOffsetProperty(position, property.textureScaleAndOffset, mixedValuemask, partOfTexturePropertyControl);

            if (EditorGUI.EndChangeCheck())
                property.textureScaleAndOffset = scaleAndOffset;

            EndProperty();

            EndAnimatedCheck();
            return 2 * kLineHeight;
        }

        private Texture TexturePropertyBody(Rect position, MaterialProperty prop)
        {
            if (prop.propertyType != ShaderPropertyType.Texture)
            {
                throw new ArgumentException(string.Format("The MaterialProperty '{0}' should be of type 'Texture' (its type is '{1})'", prop.name, prop.propertyType));
            }

            m_DesiredTexdim = prop.textureDimension;
            System.Type t = MaterialEditor.GetTextureTypeFromDimension(m_DesiredTexdim);

            // Why are we disabling the GUI in Animation Mode here?
            // If it's because object references can't be changed, shouldn't it be done in ObjectField instead?
            bool wasEnabled = GUI.enabled;

            EditorGUI.BeginChangeCheck();
            if ((prop.propertyFlags & ShaderPropertyFlags.NonModifiableTextureData) != 0)
                GUI.enabled = false;

            EditorGUI.showMixedValue = prop.hasMixedValue;
            int controlID = GUIUtility.GetControlID(12354, FocusType.Keyboard, position);
            var newValue = EditorGUI.DoObjectField(position, position, controlID, prop.textureValue, target, t, TextureValidator, false) as Texture;
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                prop.textureValue = newValue;

            GUI.enabled = wasEnabled;

            return prop.textureValue;
        }

        public Texture TextureProperty(MaterialProperty prop, string label)
        {
            bool scaleOffset = ((prop.propertyFlags & ShaderPropertyFlags.NoScaleOffset) == 0);
            return TextureProperty(prop, label, scaleOffset);
        }

        public Texture TextureProperty(MaterialProperty prop, string label, bool scaleOffset)
        {
            Rect r = GetPropertyRect(prop, label, true);
            return TextureProperty(r, prop, label, scaleOffset);
        }

        public bool HelpBoxWithButton(GUIContent messageContent, GUIContent buttonContent)
        {
            const float kButtonWidth = 60f;
            const float kSpacing = 5f;
            const float kButtonHeight = 20f;

            // Reserve size of wrapped text
            Rect contentRect = GUILayoutUtility.GetRect(messageContent, EditorStyles.helpBox);
            // Reserve size of button
            GUILayoutUtility.GetRect(1, kButtonHeight + kSpacing);

            // Render background box with text at full height
            contentRect.height += kButtonHeight + kSpacing;
            GUI.Label(contentRect, messageContent, EditorStyles.helpBox);

            // Button (align lower right)
            Rect buttonRect = new Rect(contentRect.xMax - kButtonWidth - 4f, contentRect.yMax - kButtonHeight - 4f, kButtonWidth, kButtonHeight);
            return GUI.Button(buttonRect, buttonContent);
        }

        public void TextureCompatibilityWarning(MaterialProperty prop)
        {
            if (InternalEditorUtility.BumpMapTextureNeedsFixing(prop))
            {
                if (HelpBoxWithButton(
                    EditorGUIUtility.TrTextContent("This texture is not marked as a normal map"),
                    EditorGUIUtility.TrTextContent("Fix Now")))
                {
                    InternalEditorUtility.FixNormalmapTexture(prop);
                }
            }
        }

        public Texture TexturePropertyMiniThumbnail(Rect position, MaterialProperty prop, string label, string tooltip)
        {
            BeginProperty(position, prop);

            BeginAnimatedCheck(position, prop);
            Rect thumbRect, labelRect;
            EditorGUI.GetRectsForMiniThumbnailField(position, out thumbRect, out labelRect);
            EditorGUI.HandlePrefixLabel(position, labelRect, new GUIContent(label, tooltip), 0, EditorStyles.label);
            EndAnimatedCheck();

            Texture retValue = TexturePropertyBody(thumbRect, prop);

            Rect warningPosition = position;
            warningPosition.y += position.height;
            warningPosition.height = kMiniWarningMessageHeight;

            TextureCompatibilityWarning(prop);

            EndProperty();

            return retValue;
        }

        public Rect GetTexturePropertyCustomArea(Rect position)
        {
            EditorGUI.indentLevel++;
            position.height = GetTextureFieldHeight();
            Rect scaleOffsetRect = position;
            scaleOffsetRect.yMin += EditorGUI.kSingleLineHeight;
            scaleOffsetRect.xMax -= EditorGUIUtility.fieldWidth + 2;
            scaleOffsetRect = EditorGUI.IndentedRect(scaleOffsetRect);
            EditorGUI.indentLevel--;
            return scaleOffsetRect;
        }

        public Texture TextureProperty(Rect position, MaterialProperty prop, string label)
        {
            return TextureProperty(position, prop, new GUIContent(label, string.Empty));
        }

        public Texture TextureProperty(Rect position, MaterialProperty prop, GUIContent label)
        {
            bool scaleOffset = ((prop.propertyFlags & ShaderPropertyFlags.NoScaleOffset) == 0);
            return TextureProperty(position, prop, label, scaleOffset);
        }

        public Texture TextureProperty(Rect position, MaterialProperty prop, string label, bool scaleOffset)
        {
            return TextureProperty(position, prop, label, string.Empty, scaleOffset);
        }

        public Texture TextureProperty(Rect position, MaterialProperty prop, string label, string tooltip, bool scaleOffset)
        {
            return TextureProperty(position, prop, new GUIContent(label, tooltip), scaleOffset);
        }

        public Texture TextureProperty(Rect position, MaterialProperty prop, GUIContent label, bool scaleOffset)
        {
            Rect scopeRect = new Rect(position.x, position.y, position.width, EditorGUI.lineHeight);
            BeginProperty(scopeRect, prop);

            // Label
            EditorGUI.PrefixLabel(position, label);

            // Texture slot
            position.height = GetTextureFieldHeight();
            Rect texPos = position;
            texPos.xMin = texPos.xMax - EditorGUIUtility.fieldWidth;
            Texture value = TexturePropertyBody(texPos, prop);

            EndProperty();

            // UV scale and offset
            if (scaleOffset)
            {
                TextureScaleOffsetProperty(GetTexturePropertyCustomArea(position), prop);
            }

            // Potential warning help boxes
            {
                GUILayout.Space(-kSpacingUnderTexture);
                TextureCompatibilityWarning(prop);
                GUILayout.Space(kSpacingUnderTexture);
            }
            return value;
        }

        public static Vector4 TextureScaleOffsetProperty(Rect position, Vector4 scaleOffset)
        {
            return TextureScaleOffsetProperty(position, scaleOffset, 0, false);
        }

        public static Vector4 TextureScaleOffsetProperty(Rect position, Vector4 scaleOffset, bool partOfTexturePropertyControl)
        {
            return TextureScaleOffsetProperty(position, scaleOffset, 0, partOfTexturePropertyControl);
        }

        internal static Vector4 TextureScaleOffsetProperty(Rect position, Vector4 scaleOffset, int mixedValueMask, bool partOfTexturePropertyControl)
        {
            Vector2 tiling = new Vector2(scaleOffset.x, scaleOffset.y);
            Vector2 offset = new Vector2(scaleOffset.z, scaleOffset.w);

            float labelWidth = EditorGUIUtility.labelWidth;
            float controlStartX = position.x + labelWidth;
            float labelStartX = position.x + EditorGUI.indent;

            // Temporarily reset the indent level as it was already used above to compute the positions of the label and control. See issue 946082.
            int oldIndentLevel = EditorGUI.indentLevel;

            EditorGUI.indentLevel = 0;

            if (partOfTexturePropertyControl)
            {
                labelWidth = 65;
                controlStartX = position.x + labelWidth;
                labelStartX = position.x;
                position.y = position.yMax - 2 * kLineHeight; // align with large texture thumb bottom
            }

            // Tiling
            Rect labelRect = new Rect(labelStartX, position.y, labelWidth, kLineHeight);
            Rect valueRect = new Rect(controlStartX, position.y, position.width - labelWidth, kLineHeight);
            EditorGUI.PrefixLabel(labelRect, s_TilingText);
            tiling = EditorGUI.Vector2Field(valueRect, GUIContent.none, tiling);

            // Offset
            labelRect.y += kLineHeight + EditorGUI.kVerticalSpacingMultiField;
            valueRect.y += kLineHeight + EditorGUI.kVerticalSpacingMultiField;
            EditorGUI.PrefixLabel(labelRect, s_OffsetText);
            offset = EditorGUI.Vector2Field(valueRect, GUIContent.none, offset);

            // Restore the indent level
            EditorGUI.indentLevel = oldIndentLevel;

            return new Vector4(tiling.x, tiling.y, offset.x, offset.y);
        }

        public float GetPropertyHeight(MaterialProperty prop)
        {
            return GetPropertyHeight(prop, prop.displayName);
        }

        public float GetPropertyHeight(MaterialProperty prop, string label)
        {
            // has custom drawers?
            float handlerHeight = 0f;
            MaterialPropertyHandler handler = MaterialPropertyHandler.GetHandler(((Material)target).shader, prop.name);
            if (handler != null)
            {
                handlerHeight = handler.GetPropertyHeight(prop, label ?? prop.displayName, this);
                // if we have a property drawer (and not just decorators), exit now and don't add default height
                if (handler.propertyDrawer != null)
                    return handlerHeight;
            }

            // otherwise, return default height
            return handlerHeight + GetDefaultPropertyHeight(prop);
        }

        private static float GetTextureFieldHeight()
        {
            return EditorGUI.kObjectFieldThumbnailHeight;
        }

        public static float GetDefaultPropertyHeight(MaterialProperty prop)
        {
            if (prop.propertyType == ShaderPropertyType.Vector)
                return EditorGUI.kStructHeaderLineHeight + EditorGUI.kSingleLineHeight;

            if (prop.propertyType == ShaderPropertyType.Texture)
                return GetTextureFieldHeight() + kSpacingUnderTexture;

            return EditorGUI.kSingleLineHeight;
        }

        private Rect GetPropertyRect(MaterialProperty prop, GUIContent label, bool ignoreDrawer)
        {
            return GetPropertyRect(prop, label.text, ignoreDrawer);
        }

        private Rect GetPropertyRect(MaterialProperty prop, string label, bool ignoreDrawer)
        {
            float handlerHeight = 0f;
            if (!ignoreDrawer)
            {
                MaterialPropertyHandler handler = MaterialPropertyHandler.GetHandler(((Material)target).shader, prop.name);
                if (handler != null)
                {
                    handlerHeight = handler.GetPropertyHeight(prop, label ?? prop.displayName, this);
                    // if we have a property drawer (and not just decorators), exit now and don't add default height
                    if (handler.propertyDrawer != null)
                        return EditorGUILayout.GetControlRect(true, handlerHeight, EditorStyles.layerMaskField);
                }
            }

            return EditorGUILayout.GetControlRect(true, handlerHeight + GetDefaultPropertyHeight(prop), EditorStyles.layerMaskField);
        }

        public void BeginAnimatedCheck(Rect totalPosition, MaterialProperty prop)
        {
            if (rendererForAnimationMode == null)
                return;

            s_AnimatedCheckStack.Push(new AnimatedCheckData(prop, totalPosition, GUI.backgroundColor));

            Color overrideColor;
            if (MaterialAnimationUtility.OverridePropertyColor(prop, rendererForAnimationMode, out overrideColor))
                GUI.backgroundColor = overrideColor;
        }

        public void BeginAnimatedCheck(MaterialProperty prop)
        {
            BeginAnimatedCheck(Rect.zero, prop);
        }

        public void EndAnimatedCheck()
        {
            if (rendererForAnimationMode == null)
                return;

            AnimatedCheckData data = s_AnimatedCheckStack.Pop();
            if (Event.current.type == EventType.ContextClick && data.totalPosition.Contains(Event.current.mousePosition))
            {
                DoPropertyContextMenu(data.property);
            }

            GUI.backgroundColor = data.color;
        }

        private void DoPropertyContextMenu(MaterialProperty prop)
        {
            if (contextualPropertyMenu != null)
            {
                GenericMenu pm = new GenericMenu();
                contextualPropertyMenu(pm, prop, m_RenderersForAnimationMode);

                if (pm.GetItemCount() > 0)
                    pm.ShowAsContext();
            }
        }

        public void ShaderProperty(MaterialProperty prop, string label)
        {
            ShaderProperty(prop, new GUIContent(label));
        }

        public void ShaderProperty(MaterialProperty prop, GUIContent label)
        {
            ShaderProperty(prop, label, 0);
        }

        public void ShaderProperty(MaterialProperty prop, string label, int labelIndent)
        {
            ShaderProperty(prop, new GUIContent(label), labelIndent);
        }

        public void ShaderProperty(MaterialProperty prop, GUIContent label, int labelIndent)
        {
            Rect r = GetPropertyRect(prop, label, false);
            ShaderProperty(r, prop, label, labelIndent);
        }

        public void ShaderProperty(Rect position, MaterialProperty prop, string label)
        {
            ShaderProperty(position, prop, new GUIContent(label));
        }

        public void ShaderProperty(Rect position, MaterialProperty prop, GUIContent label)
        {
            ShaderProperty(position, prop, label, 0);
        }

        public void ShaderProperty(Rect position, MaterialProperty prop, string label, int labelIndent)
        {
            ShaderProperty(position, prop, new GUIContent(label), labelIndent);
        }

        public void ShaderProperty(Rect position, MaterialProperty prop, GUIContent label, int labelIndent)
        {
            BeginAnimatedCheck(position, prop);
            EditorGUI.indentLevel += labelIndent;

            // [PerRendererData] material properties are read-only as they are meant to be set in code on a per-renderer basis.
            using (new EditorGUI.DisabledScope((prop.propertyFlags & ShaderPropertyFlags.PerRendererData) != 0))
            {
                ShaderPropertyInternal(position, prop, label);
            }

            EditorGUI.indentLevel -= labelIndent;
            EndAnimatedCheck();
        }

        public void LightmapEmissionProperty()
        {
            LightmapEmissionProperty(0);
        }

        public void LightmapEmissionProperty(int labelIndent)
        {
            Rect r = EditorGUILayout.GetControlRect(true, EditorGUI.kSingleLineHeight, EditorStyles.layerMaskField);
            LightmapEmissionProperty(r, labelIndent);
        }

        static MaterialGlobalIlluminationFlags GetGlobalIlluminationFlags(MaterialGlobalIlluminationFlags flags)
        {
            MaterialGlobalIlluminationFlags newFlags = MaterialGlobalIlluminationFlags.None;
            if ((flags & MaterialGlobalIlluminationFlags.RealtimeEmissive) != 0)
                newFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            else if ((flags & MaterialGlobalIlluminationFlags.BakedEmissive) != 0)
                newFlags = MaterialGlobalIlluminationFlags.BakedEmissive;

            return newFlags;
        }

        public void LightmapEmissionProperty(Rect position, int labelIndent)
        {
            EditorGUI.indentLevel += labelIndent;

            Object[] materials = targets;
            Material firstMaterial = (Material)target;

            // Calculate isMixed
            MaterialGlobalIlluminationFlags giFlags = GetGlobalIlluminationFlags(firstMaterial.globalIlluminationFlags);
            bool isMixed = false;
            for (int i = 1; i < materials.Length; i++)
            {
                Material material = (Material)materials[i];

                if (GetGlobalIlluminationFlags(material.globalIlluminationFlags) != giFlags)
                    isMixed = true;
            }

            BeginProperty(position, MaterialSerializedProperty.LightmapFlags, targets);
            EditorGUI.BeginChangeCheck();

            bool realtimeGISupported = SupportedRenderingFeatures.IsLightmapBakeTypeSupported(LightmapBakeType.Realtime);

            // Show popup
            EditorGUI.showMixedValue = isMixed;
            giFlags = (MaterialGlobalIlluminationFlags)EditorGUI.IntPopup(position, realtimeGISupported ? Styles.lightmapEmissiveLabelRealtimeGISupport : Styles.lightmapEmissiveLabel, (int)giFlags, Styles.lightmapEmissiveStrings, Styles.lightmapEmissiveValues);
            EditorGUI.showMixedValue = false;

            // Apply flags. But only the part that this tool modifies (RealtimeEmissive, BakedEmissive, None)
            if (EditorGUI.EndChangeCheck())
            {
                foreach (Material material in materials)
                {
                    MaterialGlobalIlluminationFlags flags = material.globalIlluminationFlags;

                    flags &= ~(MaterialGlobalIlluminationFlags.RealtimeEmissive | MaterialGlobalIlluminationFlags.BakedEmissive);
                    flags |= giFlags;

                    material.globalIlluminationFlags = flags;
                }
            }

            EndProperty();
            EditorGUI.indentLevel -= labelIndent;
        }

        public bool EmissionEnabledProperty()
        {
            Material[] materials = Array.ConvertAll(targets, (Object o) => { return (Material)o; });

            var settings = Lightmapping.GetLightingSettingsOrDefaultsFallback();

            MaterialGlobalIlluminationFlags defaultEnabled = settings.realtimeGI ? MaterialGlobalIlluminationFlags.RealtimeEmissive
                : (settings.bakedGI ? MaterialGlobalIlluminationFlags.BakedEmissive : MaterialGlobalIlluminationFlags.None);

            // Calculate isMixed
            bool enabled = materials[0].globalIlluminationFlags != MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            bool isMixed = false;
            for (int i = 1; i < materials.Length; i++)
            {
                if ((materials[i].globalIlluminationFlags != MaterialGlobalIlluminationFlags.EmissiveIsBlack) != enabled)
                {
                    isMixed = true;
                    break;
                }
            }

            BeginProperty(MaterialSerializedProperty.LightmapFlags, targets);

            // initial checkbox for enabling/disabling emission
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = isMixed;
            enabled = EditorGUILayout.Toggle(Styles.emissionLabel, enabled);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                RegisterPropertyChangeUndo("Emission Flags");
                foreach (Material mat in materials)
                {
                    mat.globalIlluminationFlags = enabled ? defaultEnabled : MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                }
                EndProperty();
                return enabled;
            }

            EndProperty();
            return !isMixed && enabled;
        }

        public static void FixupEmissiveFlag(Material mat)
        {
            if (mat == null)
                throw new ArgumentNullException("mat");

            mat.globalIlluminationFlags = FixupEmissiveFlag(mat.GetColor("_EmissionColor"), mat.globalIlluminationFlags);
        }

        public static MaterialGlobalIlluminationFlags FixupEmissiveFlag(Color col, MaterialGlobalIlluminationFlags flags)
        {
            if ((flags & MaterialGlobalIlluminationFlags.BakedEmissive) != 0 && col.maxColorComponent == 0.0f) // flag black baked
                flags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            else if (flags != MaterialGlobalIlluminationFlags.EmissiveIsBlack) // clear baked flag on everything else, unless it's explicity disabled
                flags &= MaterialGlobalIlluminationFlags.AnyEmissive;
            return flags;
        }

        public void LightmapEmissionFlagsProperty(int indent, bool enabled)
        {
            LightmapEmissionFlagsProperty(indent, enabled, false);
        }

        public void LightmapEmissionFlagsProperty(int indent, bool enabled, bool ignoreEmissionColor)
        {
            Material[] materials = Array.ConvertAll(targets, (Object o) => { return (Material)o; });

            // Calculate isMixed
            MaterialGlobalIlluminationFlags any_em = MaterialGlobalIlluminationFlags.AnyEmissive;
            MaterialGlobalIlluminationFlags giFlags = materials[0].globalIlluminationFlags & any_em;
            bool isMixed = false;
            for (int i = 1; i < materials.Length; i++)
            {
                isMixed = isMixed || (materials[i].globalIlluminationFlags & any_em) != giFlags;
            }

            BeginProperty(MaterialSerializedProperty.LightmapFlags, targets);
            EditorGUI.BeginChangeCheck();

            bool realtimeGISupported = SupportedRenderingFeatures.IsLightmapBakeTypeSupported(LightmapBakeType.Realtime);

            // Show popup
            EditorGUI.showMixedValue = isMixed;
            EditorGUI.indentLevel += indent;
            giFlags = (MaterialGlobalIlluminationFlags)EditorGUILayout.IntPopup(realtimeGISupported ? Styles.lightmapEmissiveLabelRealtimeGISupport : Styles.lightmapEmissiveLabel, (int)giFlags, Styles.lightmapEmissiveStrings, Styles.lightmapEmissiveValues);
            EditorGUI.indentLevel -= indent;
            EditorGUI.showMixedValue = false;

            // Apply flags. But only the part that this tool modifies (RealtimeEmissive, BakedEmissive, None)
            RegisterPropertyChangeUndo("Emission Flags");
            bool applyFlags = EditorGUI.EndChangeCheck();
            foreach (Material mat in materials)
            {
                mat.globalIlluminationFlags = applyFlags ? giFlags : mat.globalIlluminationFlags;
                if (!ignoreEmissionColor)
                {
                    FixupEmissiveFlag(mat);
                }
            }

            EndProperty();
        }

        void ShaderPropertyInternal(Rect position, MaterialProperty prop, GUIContent label)
        {
            // Use custom property drawers if needed
            MaterialPropertyHandler handler = MaterialPropertyHandler.GetHandler(((Material)target).shader, prop.name);
            if (handler != null)
            {
                handler.OnGUI(ref position, prop, label.text != null ? label : new GUIContent(prop.displayName), this);
                // If we had a property drawer (and not just decorators), exit now and don't draw default UI.
                if (handler.propertyDrawer != null)
                    return;
            }

            DefaultShaderPropertyInternal(position, prop, label);
        }

        public void DefaultShaderProperty(MaterialProperty prop, string label)
        {
            DefaultShaderPropertyInternal(prop, new GUIContent(label));
        }

        internal void DefaultShaderPropertyInternal(MaterialProperty prop, GUIContent label)
        {
            Rect r = GetPropertyRect(prop, label, true);
            DefaultShaderPropertyInternal(r, prop, label);
        }

        public void DefaultShaderProperty(Rect position, MaterialProperty prop, string label)
        {
            DefaultShaderPropertyInternal(position, prop, new GUIContent(label));
        }

        internal void DefaultShaderPropertyInternal(Rect position, MaterialProperty prop, GUIContent label)
        {
            switch (prop.propertyType)
            {
                case ShaderPropertyType.Range: // float ranges
                    RangePropertyInternal(position, prop, label);
                    break;
                case ShaderPropertyType.Float: // floats
                    FloatPropertyInternal(position, prop, label);
                    break;
                case ShaderPropertyType.Int: // ints
                    IntegerPropertyInternal(position, prop, label);
                    break;
                case ShaderPropertyType.Color: // colors
                    ColorPropertyInternal(position, prop, label);
                    break;
                case ShaderPropertyType.Texture: // textures
                    TextureProperty(position, prop, label);
                    break;
                case ShaderPropertyType.Vector: // vectors
                    VectorProperty(position, prop, label);
                    break;
                default:
                    GUI.Label(position, "Unknown property type: " + prop.name + ": " + (int)prop.propertyType);
                    break;
            }
        }

        // -------- obsolete versions of common controls


        [System.Obsolete("Use RangeProperty with MaterialProperty instead.")]
        public float RangeProperty(string propertyName, string label, float v2, float v3)
        {
            MaterialProperty prop = GetMaterialProperty(targets, propertyName);
            return RangeProperty(prop, label);
        }

        [System.Obsolete("Use FloatProperty with MaterialProperty instead.")]
        public float FloatProperty(string propertyName, string label)
        {
            MaterialProperty prop = GetMaterialProperty(targets, propertyName);
            return FloatProperty(prop, label);
        }

        [System.Obsolete("Use ColorProperty with MaterialProperty instead.")]
        public Color ColorProperty(string propertyName, string label)
        {
            MaterialProperty prop = GetMaterialProperty(targets, propertyName);
            return ColorProperty(prop, label);
        }

        [System.Obsolete("Use VectorProperty with MaterialProperty instead.")]
        public Vector4 VectorProperty(string propertyName, string label)
        {
            MaterialProperty prop = GetMaterialProperty(targets, propertyName);
            return VectorProperty(prop, label);
        }

        [System.Obsolete("Use TextureProperty with MaterialProperty instead.")]
        public Texture TextureProperty(string propertyName, string label, ShaderUtil.ShaderPropertyTexDim texDim)
        {
            MaterialProperty prop = GetMaterialProperty(targets, propertyName);
            return TextureProperty(prop, label);
        }

        [System.Obsolete("Use TextureProperty with MaterialProperty instead.")]
        public Texture TextureProperty(string propertyName, string label, ShaderUtil.ShaderPropertyTexDim texDim, bool scaleOffset)
        {
            MaterialProperty prop = GetMaterialProperty(targets, propertyName);
            return TextureProperty(prop, label, scaleOffset);
        }

        [System.Obsolete("Use ShaderProperty that takes MaterialProperty parameter instead.")]
        public void ShaderProperty(Shader shader, int propertyIndex)
        {
            MaterialProperty prop = GetMaterialProperty(targets, propertyIndex);
            ShaderProperty(prop, prop.displayName);
        }

        // -------- other functionality

        public static MaterialProperty[] GetMaterialProperties(Object[] mats)
        {
            if (mats == null)
                throw new ArgumentNullException("mats");
            if (Array.IndexOf(mats, null) >= 0)
                throw new ArgumentException("List of materials contains null");
            return ShaderUtil.GetMaterialProperties(mats);
        }

        public static string[] GetMaterialPropertyNames(Object[] mats)
        {
            if (mats == null)
                throw new ArgumentNullException("mats");
            if (Array.IndexOf(mats, null) >= 0)
                throw new ArgumentException("List of materials contains null");
            return ShaderUtil.GetMaterialPropertyNames(mats);
        }

        public static MaterialProperty GetMaterialProperty(Object[] mats, string name)
        {
            if (mats == null)
                throw new ArgumentNullException("mats");
            if (Array.IndexOf(mats, null) >= 0)
                throw new ArgumentException("List of materials contains null");
            return ShaderUtil.GetMaterialProperty(mats, name);
        }

        public static MaterialProperty GetMaterialProperty(Object[] mats, int propertyIndex)
        {
            if (mats == null)
                throw new ArgumentNullException("mats");
            if (Array.IndexOf(mats, null) >= 0)
                throw new ArgumentException("List of materials contains null");
            return ShaderUtil.GetMaterialProperty(mats, propertyIndex);
        }

        class ForwardApplyMaterialModification
        {
            readonly Renderer[] renderers;
            bool        isMaterialEditable;

            public ForwardApplyMaterialModification(Renderer[] r, bool inIsMaterialEditable)
            {
                renderers = r;
                isMaterialEditable = inIsMaterialEditable;
            }

            public bool DidModifyAnimationModeMaterialProperty(MaterialProperty property, int changedMask, object previousValue)
            {
                bool didModify = false;
                foreach (Renderer renderer in renderers)
                {
                    didModify = didModify | MaterialAnimationUtility.ApplyMaterialModificationToAnimationRecording(property, changedMask, renderer, previousValue);
                }

                if (didModify)
                    return true;

                // If the material is not editable,
                // then we explicitly make sure that things that these properties arecan not be recorded are not going to be applied to the material
                return !isMaterialEditable;
            }
        }

        static Renderer[] GetAssociatedRenderersFromInspector()
        {
            var imguicontainer = UIElementsUtility.GetCurrentIMGUIContainer();
            if (imguicontainer != null)
            {
                var editorElement = imguicontainer.GetFirstAncestorOfType<IEditorElement>();
                if (editorElement != null)
                {
                    return GetAssociatedRenderersFromEditors(editorElement.Editors);
                }
            }

            return new Renderer[0];
        }

        internal static Renderer[] GetAssociatedRenderersFromEditors(IEnumerable<Editor> editors)
        {
            List<Renderer> renderers = new List<Renderer>();
            foreach (var editor in editors)
            {
                foreach (Object target in editor.targets)
                {
                    var renderer = target as Renderer;
                    if (renderer)
                        renderers.Add(renderer);
                }
            }
            return renderers.ToArray();
        }

        public static Renderer PrepareMaterialPropertiesForAnimationMode(MaterialProperty[] properties, bool isMaterialEditable)
        {
            Renderer[] renderers = PrepareMaterialPropertiesForAnimationMode(properties, GetAssociatedRenderersFromInspector(), isMaterialEditable);
            return (renderers != null && renderers.Length > 0) ? renderers[0] : null;
        }

        internal static Renderer PrepareMaterialPropertiesForAnimationMode(MaterialProperty[] properties, Renderer renderer, bool isMaterialEditable)
        {
            Renderer[] renderers = PrepareMaterialPropertiesForAnimationMode(properties, new Renderer[] {renderer}, isMaterialEditable);
            return (renderers != null && renderers.Length > 0) ? renderers[0] : null;
        }

        internal static Renderer[] PrepareMaterialPropertiesForAnimationMode(MaterialProperty[] properties, Renderer[] renderers, bool isMaterialEditable)
        {
            bool isInAnimationMode = AnimationMode.InAnimationMode();

            if (renderers != null && renderers.Length > 0)
            {
                var callback = new ForwardApplyMaterialModification(renderers, isMaterialEditable);
                var block = new MaterialPropertyBlock();

                renderers[0].GetPropertyBlock(block);
                foreach (MaterialProperty prop in properties)
                {
                    prop.ReadFromMaterialPropertyBlock(block);
                    if (isInAnimationMode)
                        prop.applyPropertyCallback = callback.DidModifyAnimationModeMaterialProperty;
                }
            }

            if (isInAnimationMode)
                return renderers;
            else
                return null;
        }

        public void SetDefaultGUIWidths()
        {
            EditorGUIUtility.fieldWidth = EditorGUI.kObjectFieldThumbnailHeight;
            EditorGUIUtility.labelWidth = GUIClip.visibleRect.width - EditorGUIUtility.fieldWidth - 25 - EditorGUI.kIndentPerLevel;
        }

        internal override bool GetOptimizedGUIBlock(bool isDirty, bool isVisible, out float height)
        {
            // Shift UI to the right to leave space for locks
            // Done here because it's the only place between the creation of the vertical group and the call ot OnInspectorGUI
            // And because OnInspectorGUI might be overriden by a user editor
            var style = GUILayoutUtility.topLevel.style = new GUIStyle(GUILayoutUtility.topLevel.style);
            style.padding.left += (int)EditorGUI.kIndentPerLevel;
            return base.GetOptimizedGUIBlock(isDirty, isVisible, out height);
        }

        private bool IsMaterialEditor(string customEditorName)
        {
            string unityEditorFullName = "UnityEditor." + customEditorName; // for convenience: adding UnityEditor namespace is not needed in the shader

            foreach (var type in TypeCache.GetTypesDerivedFrom<MaterialEditor>())
            {
                if (type.FullName.Equals(customEditorName, StringComparison.Ordinal) ||
                    type.FullName.Equals(unityEditorFullName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        void CreateCustomShaderEditorIfNeeded(Shader shader)
        {
            var customEditorName = ShaderUtil.GetCurrentCustomEditor(shader);
            if (string.IsNullOrEmpty(customEditorName))
            {
                m_CustomEditorClassName = "";
                m_CustomShaderGUI = null;
                return;
            }
            if (m_CustomEditorClassName == customEditorName)
                return;

            m_CustomEditorClassName = customEditorName;
            m_CustomShaderGUI = ShaderGUIUtility.CreateShaderGUI(m_CustomEditorClassName);
            // We need to delay checking setup because we need all loaded editor assemblies which is not ready
            // during package import. During package import we create an Editor to generate a asset preview. (case 707328)
            m_CheckSetup = true;
        }

        public bool PropertiesGUI()
        {
            // Material Editor is the first inspected editor when accessed through the Project panel
            // and this is the scenario where we do not want to redraw the VC status bar
            // since InspectorWindow already takes care of that. Otherwise, the Material Editor
            // is not the first inspected editor (e.g. when it's a part of a GO Inspector)
            // thus we draw the VC status bar
            if (!firstInspectedEditor)
            {
                // OnInspectorGUI is wrapped inside a BeginVertical/EndVertical block that adds padding,
                // which we don't want here so we could have the VC bar span the entire Material Editor width
                // we stop the vertical block, draw the VC bar, and then start a new vertical block with the same style.
                var style = GUILayoutUtility.topLevel.style;
                EditorGUILayout.EndVertical();

                // setting the GUI to enabled where the VC status bar is drawn because it gets disabled by the parent inspector
                // for non-checked out materials, and we need the version control status bar to be always active
                bool wasGUIEnabled = GUI.enabled;
                GUI.enabled = true;

                PropertyEditor.VersionControlBar(this);

                GUI.enabled = wasGUIEnabled;
                EditorGUILayout.BeginVertical(style);
            }

            var eventType = Event.current.type;
            bool isRunningCommand = eventType == EventType.ExecuteCommand || eventType == EventType.ValidateCommand;
            if (m_InsidePropertiesGUI && !isRunningCommand)
            {
                Debug.LogWarning("PropertiesGUI() is being called recursively. If you want to render the default gui for shader properties then call PropertiesDefaultGUI() instead");
                return false;
            }

            EditorGUI.BeginChangeCheck();

            MaterialProperty[] props = GetMaterialProperties(targets);

            // In animation mode we are actually animating the Renderer instead of the material.
            // Thus all properties are editable even if the material is not editable.
            m_RenderersForAnimationMode = PrepareMaterialPropertiesForAnimationMode(props, GetAssociatedRenderersFromInspector(), GUI.enabled);
            bool wasEnabled = GUI.enabled;
            if (m_RenderersForAnimationMode != null)
                GUI.enabled = true;

            if (!isRunningCommand)
                m_InsidePropertiesGUI = true;

            // Since ExitGUI is called when showing the Object Picker we wrap
            // properties gui in try/catch to catch the ExitGUIException thrown by ExitGUI()
            // to ensure our m_InsidePropertiesGUI flag is reset
            try
            {
                if (m_CustomShaderGUI != null)
                    m_CustomShaderGUI.OnGUI(this, props);
                else
                    PropertiesDefaultGUI(props);

                Renderer[] renderers = GetAssociatedRenderersFromInspector();
                if (renderers != null && renderers.Length > 0)
                {
                    if (eventType == EventType.Layout)
                    {
                        renderers[0].GetPropertyBlock(m_PropertyBlock);
                    }

                    if (m_PropertyBlock != null && !m_PropertyBlock.isEmpty)
                        EditorGUILayout.HelpBox(Styles.propBlockInfo, MessageType.Info);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                GUI.enabled = wasEnabled;
                if (!isRunningCommand)
                    m_InsidePropertiesGUI = false;
                m_RenderersForAnimationMode = null;
            }

            return EditorGUI.EndChangeCheck();
        }

        public void PropertiesDefaultGUI(MaterialProperty[] props)
        {
            SetDefaultGUIWidths();

            if (m_InfoMessage != null)
                EditorGUILayout.HelpBox(m_InfoMessage, MessageType.Info);
            else
                // Hack to make sure that control IDs stay the same when the help box is there or is not there.
                // Otherwise, open color picker windows will not keep synched to the same properties when the help box
                // shows up or disappears (case 566958)
                GUIUtility.GetControlID(s_ControlHash, FocusType.Passive, new Rect(0, 0, 0, 0));

            for (var i = 0; i < props.Length; i++)
            {
                if ((props[i].propertyFlags & ShaderPropertyFlags.HideInInspector) != 0)
                    continue;

                float h = GetPropertyHeight(props[i], props[i].displayName);
                Rect r = EditorGUILayout.GetControlRect(true, h, EditorStyles.layerMaskField);

                ShaderProperty(r, props[i], props[i].displayName);
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            if (UnityEngine.Rendering.SupportedRenderingFeatures.active.editableMaterialRenderQueue)
                RenderQueueField();
            EnableInstancingField();
            DoubleSidedGIField();
        }

        internal static void BeginNoApplyMaterialPropertyDrawers()
        {
            EditorMaterialUtility.disableApplyMaterialPropertyDrawers = true;
        }

        internal static void EndNoApplyMaterialPropertyDrawers()
        {
            EditorMaterialUtility.disableApplyMaterialPropertyDrawers = false;
        }

        public static void ApplyMaterialPropertyDrawers(Material material)
        {
            var objs = new Object[] { material };
            ApplyMaterialPropertyDrawers(objs);
        }

        [RequiredByNativeCode]
        internal static void ApplyMaterialPropertyDrawersFromNative(Material material)
        {
            var objs = new Object[] { material };
            ApplyMaterialPropertyDrawers(objs);
        }

        public static void ApplyMaterialPropertyDrawers(Object[] targets)
        {
            if (!EditorMaterialUtility.disableApplyMaterialPropertyDrawers)
            {
                if (targets == null || targets.Length == 0)
                    return;
                var target = targets[0] as Material;
                if (target == null)
                    return;

                var shader = target.shader;
                var propNames = GetMaterialPropertyNames(targets);
                for (var i = 0; i < propNames.Length; i++)
                {
                    MaterialPropertyHandler handler = MaterialPropertyHandler.GetHandler(shader, propNames[i]);
                    if (handler != null && handler.propertyDrawer != null)
                    {
                        var prop = GetMaterialProperty(targets, i);
                        handler.propertyDrawer.Apply(prop);
                    }
                }
            }
        }

        public void RegisterPropertyChangeUndo(string label)
        {
            Undo.RecordObjects(targets, "Modify " + label + " of " + targetTitle);
        }

        private UnityEngine.Rendering.TextureDimension m_DesiredTexdim;

        private Object TextureValidator(Object[] references, System.Type objType, SerializedProperty property, EditorGUI.ObjectFieldValidatorOptions options)
        {
            foreach (Object i in references)
            {
                var t = i as Texture;
                if (t)
                {
                    if (t.dimension == m_DesiredTexdim || m_DesiredTexdim == UnityEngine.Rendering.TextureDimension.Any)
                        return t;
                }
            }
            return null;
        }

        private static readonly Mesh[] s_Meshes = {null, null, null, null, null };
        private static Mesh s_PlaneMesh;
        private static readonly GUIContent[] s_MeshIcons = { null, null, null, null, null };
        private static readonly GUIContent[] s_LightIcons = { null, null };
        private static readonly GUIContent[] s_TimeIcons = { null, null };

        private void Init()
        {
            if (s_Meshes[0] == null)
            {
                var handleGo = (GameObject)EditorGUIUtility.LoadRequired("Previews/PreviewMaterials.fbx");
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

                s_MeshIcons[0] = EditorGUIUtility.TrIconContent("PreMatSphere");
                s_MeshIcons[1] = EditorGUIUtility.TrIconContent("PreMatCube");
                s_MeshIcons[2] = EditorGUIUtility.TrIconContent("PreMatCylinder");
                s_MeshIcons[3] = EditorGUIUtility.TrIconContent("PreMatTorus");
                s_MeshIcons[4] = EditorGUIUtility.TrIconContent("PreMatQuad");

                s_LightIcons[0] = EditorGUIUtility.TrIconContent("PreMatLight0");
                s_LightIcons[1] = EditorGUIUtility.TrIconContent("PreMatLight1");

                s_TimeIcons[0] = EditorGUIUtility.TrIconContent("PlayButton");
                s_TimeIcons[1] = EditorGUIUtility.TrIconContent("PauseButton");

                Mesh quadMesh = Resources.GetBuiltinResource(typeof(Mesh), "Quad.fbx") as Mesh;
                s_Meshes[4] = quadMesh;
                s_PlaneMesh = quadMesh;
            }
        }

        public override void OnPreviewSettings()
        {
            if (m_CustomShaderGUI != null)
                m_CustomShaderGUI.OnMaterialPreviewSettingsGUI(this);
            else
                DefaultPreviewSettingsGUI();
        }

        private bool DoReflectionProbePicker(out Rect buttonRect)
        {
            buttonRect = GUILayoutUtility.GetRect(Styles.reflectionProbePickerIcon, EditorStyles.toolbarDropDownRight);

            if (EditorGUI.DropdownButton(buttonRect, Styles.reflectionProbePickerIcon, FocusType.Passive, EditorStyles.toolbarDropDownRight))
                return true;

            return false;
        }

        public void DefaultPreviewSettingsGUI()
        {
            var mat = target as Material;
            if (!SupportsRenderingPreview(mat))
                return;

            Init();

            var viewType = GetPreviewType(mat);
            if (targets.Length > 1 || viewType == PreviewType.Mesh)
            {
                var oldSelectedMeshVal = m_SelectedMesh;
                m_TimeUpdate = PreviewGUI.CycleButton(m_TimeUpdate, s_TimeIcons);

                m_SelectedMesh = PreviewGUI.CycleButton(m_SelectedMesh, s_MeshIcons);

                if (oldSelectedMeshVal != m_SelectedMesh)
                    EditorPrefs.SetInt(kDefaultMaterialPreviewMesh, m_SelectedMesh);

                m_LightMode = PreviewGUI.CycleButton(m_LightMode, s_LightIcons);

                Rect settingsButton;
                if (DoReflectionProbePicker(out settingsButton))
                    PopupWindow.Show(settingsButton, m_ReflectionProbePicker);
            }
        }

        public sealed override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            var mat = target as Material;
            if (!SupportsRenderingPreview(mat))
                return null;

            Init();

            var previewRenderUtility = GetPreviewRendererUtility();
            EditorUtility.SetCameraAnimateMaterials(previewRenderUtility.camera, true);

            previewRenderUtility.BeginStaticPreview(new Rect(0, 0, width, height));
            StreamRenderResources();
            DoRenderPreview(previewRenderUtility, true);
            return previewRenderUtility.EndStaticPreview();
        }

        private void StreamRenderResources()
        {
            //Streaming texture tiles if the material uses VT
            if (PlayerSettings.GetVirtualTexturingSupportEnabled())
            {
                foreach (var t in targets)
                {
                    var mat = t as Material;
                    var shader = mat.shader;

                    //Find all texture stacks and the maximum texture dimension per stack
                    var stackTextures = new Dictionary<int, int>();

                    int count = shader.GetPropertyCount();
                    for (int i = 0; i < count; i++)
                    {
                        if (shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture)
                        {
                            string stackName;
                            int dummy;

                            if (shader.FindTextureStack(i, out stackName, out dummy))
                            {
                                var stackId = Shader.PropertyToID(stackName);

                                if (!stackTextures.ContainsKey(stackId))
                                {
                                    //Get the dimension of the texture stack. This can be different from the texture dimensions.
                                    try
                                    {
                                        int width, height;
                                        VirtualTexturing.Streaming.GetTextureStackSize(mat, stackId, out width, out height);
                                        stackTextures[stackId] = Math.Max(width, height);
                                    }
                                    catch
                                    {
                                    }
                                }
                            }
                        }
                    }

                    if (stackTextures.Count != 0)
                    {
                        //@TODO Poor mans prefetching. Remove once we request the mips synchronously and are guaranteed that they are in the cache.
                        //Now we need to update the VT system. We sleep to make sure any VT threads (transcoder?) can pick up the work.

                        const int numberOfVTUpdates = 3; // We assume the texture data will be in the texture tile cache after this number of updates
                        //Streaming texture mips for all the stacks so we have texture data to render with
                        for (int i = 0; i < numberOfVTUpdates; i++)
                        {
                            foreach (var item in stackTextures)
                            {
                                var stackId = item.Key;
                                var maxDimension = item.Value;

                                //Requesting the 256x256 mip and 128x128 mip so that their is content in the cache to render with
                                const int mipResolutionToRequest = 256;
                                int mipToRequest = 0;

                                if (maxDimension > mipResolutionToRequest)
                                {
                                    float factor = (float)maxDimension / (float)mipResolutionToRequest;
                                    mipToRequest = (int)Math.Log(factor, 2);
                                }

                                //@TODO use synchronous requesting once it is available.
                                VirtualTexturing.Streaming.RequestRegion(mat, stackId, new Rect(0, 0, 1, 1), mipToRequest, 2);
                            }

                            //2 system updates per sleep to make sure we flush the VT system while limiting sleeping.
                            VirtualTexturing.System.Update();
                            System.Threading.Thread.Sleep(1);
                            VirtualTexturing.System.Update();
                        }
                    }
                }
            }
        }

        private void DoRenderPreview(PreviewRenderUtility previewRenderUtility, bool overridePreviewMesh = false)
        {
            if (previewRenderUtility.renderTexture.width <= 0 || previewRenderUtility.renderTexture.height <= 0)
                return;

            var mat = target as Material;
            var viewType = GetPreviewType(mat);

            previewRenderUtility.camera.transform.position = -Vector3.forward * 5;
            previewRenderUtility.camera.transform.rotation = Quaternion.identity;
            if (m_LightMode == 0)
            {
                previewRenderUtility.lights[0].intensity = 1.0f;
                previewRenderUtility.lights[0].transform.rotation = Quaternion.Euler(30f, 30f, 0);
                previewRenderUtility.lights[1].intensity = 0;
            }
            else
            {
                previewRenderUtility.lights[0].intensity = 1.0f;
                previewRenderUtility.lights[0].transform.rotation = Quaternion.Euler(50f, 50f, 0);
                previewRenderUtility.lights[1].intensity = 1.0f;
            }

            previewRenderUtility.ambientColor = new Color(0.2f, 0.2f, 0.2f, 0);

            Quaternion rot = Quaternion.identity;
            if (DoesPreviewAllowRotation(viewType))
                rot = Quaternion.Euler(m_PreviewDir.y, 0, 0) * Quaternion.Euler(0, m_PreviewDir.x, 0);
            Mesh mesh = overridePreviewMesh ? s_Meshes[0] : s_Meshes[m_SelectedMesh];

            switch (viewType)
            {
                case PreviewType.Plane:
                    mesh = s_PlaneMesh;
                    break;
                case PreviewType.Mesh:
                    // We need to rotate camera, so we can see different reflections from different angles
                    // If we would only rotate object, the reflections would stay the same
                    previewRenderUtility.camera.transform.position = Quaternion.Inverse(rot) * previewRenderUtility.camera.transform.position;
                    previewRenderUtility.camera.transform.LookAt(Vector3.zero);
                    rot = Quaternion.identity;
                    break;
                case PreviewType.Skybox:
                    mesh = null;
                    previewRenderUtility.camera.transform.rotation = Quaternion.Inverse(rot);
                    previewRenderUtility.camera.fieldOfView = 120.0f;
                    break;
            }

            if (mesh != null)
            {
                previewRenderUtility.DrawMesh(mesh, Vector3.zero, rot, mat, 0, null, m_ReflectionProbePicker.Target, false);
            }

            previewRenderUtility.Render(true);
            if (viewType == PreviewType.Skybox)
            {
                InternalEditorUtility.DrawSkyboxMaterial(mat, previewRenderUtility.camera);
            }
        }

        public sealed override bool HasPreviewGUI()
        {
            return true;
        }

        public override bool RequiresConstantRepaint()
        {
            return m_TimeUpdate == 1;
        }

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            if (m_CustomShaderGUI != null)
                m_CustomShaderGUI.OnMaterialInteractivePreviewGUI(this, r, background);
            else
                base.OnInteractivePreviewGUI(r, background);
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (m_CustomShaderGUI != null)
                m_CustomShaderGUI.OnMaterialPreviewGUI(this, r, background);
            else
                DefaultPreviewGUI(r, background);
        }

        // TO DO: remove IMGUI code after fully finished with UITK one
        public override VisualElement CreatePreview(VisualElement inspectorPreviewWindow)
        {
            var window = inspectorPreviewWindow as InspectorPreviewWindow;
            var toolbar = window?.GetButtonPane();

            var mat = target as Material;
            if (!SupportsRenderingPreview(mat))
                return null;

            Init();

            var viewType = GetPreviewType(mat);
            if (toolbar != null && (targets.Length > 1 || viewType == PreviewType.Mesh))
            {
                window.AddButton(Styles.timeUpdateButtonName, (Texture2D)s_TimeIcons[m_TimeUpdate].image, () => OnTimeUpdateClick(window));
                window.AddButton(Styles.selectedMeshButtonName, (Texture2D)s_MeshIcons[m_SelectedMesh].image, () => OnSelectedMeshClick(window));
                window.AddButton(Styles.lightModeButtonName, (Texture2D)s_LightIcons[m_LightMode].image, () => OnLightModeClick(window));
                window.AddDropdownWithIcon(Styles.reflectionProbeDropdownName, (Texture2D)Styles.reflectionProbePickerIcon.image, () => OnReflectionProbeClick(toolbar));
            }

            return window;
        }

        void OnTimeUpdateClick(InspectorPreviewWindow window) { m_TimeUpdate = OnButtonClick(Styles.timeUpdateButtonName, m_TimeUpdate, s_TimeIcons, window); }

        void OnLightModeClick(InspectorPreviewWindow window) { m_LightMode = OnButtonClick(Styles.lightModeButtonName, m_LightMode, s_LightIcons, window); }

        void OnSelectedMeshClick(InspectorPreviewWindow window)
        {
            m_SelectedMesh = OnButtonClick(Styles.selectedMeshButtonName, m_SelectedMesh, s_MeshIcons, window);
            EditorPrefs.SetInt(kDefaultMaterialPreviewMesh, m_SelectedMesh);
        }

        void OnReflectionProbeClick(VisualElement toolbar)
        {
            var button = toolbar.Q(Styles.reflectionProbeDropdownName);
            if (button != null)
            {
                PopupWindow.Show(button.worldBound, m_ReflectionProbePicker);
            }
        }

        int OnButtonClick(string name, int state, GUIContent[] icons, InspectorPreviewWindow window)
        {
            state++;
            if (state == icons.Length)
                state = 0;

            window.UpdateButtonIcon(name, (Texture2D)icons[state].image);

            return state;
        }

        private static bool SupportsRenderingPreview(Material material)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
                return false;

            if (material == null)
                return false;

            var assetPath = AssetDatabase.GetAssetPath(material);
            if (assetPath.EndsWith(".vfx", StringComparison.InvariantCultureIgnoreCase))
                return false;

            return true;
        }

        public void DefaultPreviewGUI(Rect r, GUIStyle background)
        {
            var mat = target as Material;
            if (!SupportsRenderingPreview(mat))
            {
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 40), "Material preview \nnot available");
                return;
            }

            Init();

            var viewType = GetPreviewType(mat);

            if (DoesPreviewAllowRotation(viewType))
                m_PreviewDir = PreviewGUI.Drag2D(m_PreviewDir, r);

            if (Event.current.type != EventType.Repaint)
                return;

            var previewRenderUtility = GetPreviewRendererUtility();
            previewRenderUtility.BeginPreview(r,  background);
            DoRenderPreview(previewRenderUtility, !firstInspectedEditor);
            previewRenderUtility.EndAndDrawPreview(r);
        }

        private static PreviewRenderUtility s_PreviewRenderUtility;
        private static PreviewRenderUtility GetPreviewRendererUtility()
        {
            if (s_PreviewRenderUtility == null)
            {
                s_PreviewRenderUtility = new PreviewRenderUtility();
                EditorUtility.SetCameraAnimateMaterials(s_PreviewRenderUtility.camera, true);
            }
            return s_PreviewRenderUtility;
        }

        private static void CleanUpPreviewRenderUtility()
        {
            if (s_PreviewRenderUtility == null)
                return;

            s_PreviewRenderUtility.Cleanup();
            s_PreviewRenderUtility = null;
        }

        private static int s_NumberOfEditors = 0;

        public virtual void OnEnable()
        {
            s_NumberOfEditors++;

            if (!target)
                return;
            m_Shader = serializedObject.FindProperty("m_Shader").objectReferenceValue as Shader;
            m_CustomEditorClassName = "";
            CreateCustomShaderEditorIfNeeded(m_Shader);

            s_MaterialEditors.Add(this);
            Undo.undoRedoEvent += UndoRedoPerformed;
            PropertiesChanged();

            m_PropertyBlock = new MaterialPropertyBlock();

            m_ReflectionProbePicker.OnEnable();
        }

        [Obsolete("MaterialEditor.UndoRedoPerformed() has been deprecated. Use MaterialEditor.UndoRedoPerformed(in UndoRedoInfo) instead", false)]
        public virtual void UndoRedoPerformed()
        {
            // Undo could've restored old shader which might lead to change in custom editor class
            // therefore we need to rebuild inspector
            UpdateAllOpenMaterialEditors();

            PropertiesChanged();
        }

        public virtual void UndoRedoPerformed(in UndoRedoInfo info)
        {
            // Undo could've restored old shader which might lead to change in custom editor class
            // therefore we need to rebuild inspector
            UpdateAllOpenMaterialEditors();

            PropertiesChanged();
        }

        public virtual void OnDisable()
        {
            s_NumberOfEditors--;
            if (s_NumberOfEditors == 0)
                CleanUpPreviewRenderUtility();

            m_ReflectionProbePicker.OnDisable();
            s_MaterialEditors.Remove(this);
            Undo.undoRedoEvent -= UndoRedoPerformed;
        }

        // Handle dragging of material onto renderers
        internal void OnSceneDrag(SceneView sceneView, int index)
        {
            Event evt = Event.current;

            if (evt.type == EventType.Repaint)
                return;

            var materialIndex = -1;
            var go = HandleUtility.PickGameObject(evt.mousePosition, out materialIndex);

            if (EditorMaterialUtility.IsBackgroundMaterial((target as Material)))
            {
                HandleSkybox(go, evt);
                ClearDragMaterialRendering();
            }
            else if (go && go.GetComponent<Renderer>())
                HandleRenderer(go.GetComponent<Renderer>(), materialIndex, target as Material, evt.type, evt.alt);
            else
                ClearDragMaterialRendering();
        }

        private static void TryRevertDragChanges()
        {
            if (s_previousDraggedUponRenderer != null)
            {
                bool hasRevert = false;
                if (!s_previousAlreadyHadPrefabModification &&
                    PrefabUtility.GetPrefabInstanceStatus(s_previousDraggedUponRenderer) == PrefabInstanceStatus.Connected)
                {
                    var materialRendererSerializedObject = new SerializedObject(s_previousDraggedUponRenderer).FindProperty("m_Materials");
                    PrefabUtility.RevertPropertyOverride(materialRendererSerializedObject, InteractionMode.AutomatedAction, false);
                    hasRevert = true;

                    if (!hasRevert)
                        s_previousDraggedUponRenderer.sharedMaterials = s_previousMaterialValue;
                }
            }
        }

        private static void ClearDragMaterialRendering()
        {
            TryRevertDragChanges();
            s_previousDraggedUponRenderer = null;
            s_previousMaterialValue = null;
        }

        Material s_OriginalMaterial;
        internal void HandleSkybox(GameObject go, Event evt)
        {
            bool draggingOverBackground = !go;
            var applyAndConsumeEvent = false;

            if (!draggingOverBackground || evt.type == EventType.DragExited)
            {
                if (s_OriginalMaterial != null)
                {
                    RenderSettings.skybox = s_OriginalMaterial;
                    s_OriginalMaterial = null;
                }
                evt.Use();
            }
            else
            {
                switch (evt.type)
                {
                    case EventType.DragUpdated:
                        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        applyAndConsumeEvent = true;
                        break;

                    case EventType.DragPerform:
                        DragAndDrop.AcceptDrag();
                        applyAndConsumeEvent = true;
                        break;
                }
            }

            if (applyAndConsumeEvent)
            {
                Undo.RecordObject(RenderSettings.GetRenderSettings(), Styles.undoAssignSkyboxMaterial);
                if (s_OriginalMaterial == null) s_OriginalMaterial = RenderSettings.skybox;

                RenderSettings.skybox = target as Material;
                if (evt.type == EventType.DragPerform) s_OriginalMaterial = null;

                evt.Use();
            }
        }

        static Renderer s_previousDraggedUponRenderer;
        static Material[] s_previousMaterialValue;
        static bool s_previousAlreadyHadPrefabModification;
        internal static void HandleRenderer(Renderer r, int materialIndex, Material dragMaterial, EventType eventType, bool alt)
        {
            if (r.GetType().GetCustomAttributes(typeof(RejectDragAndDropMaterial), true).Length > 0)
                return;

            var applyMaterial = false;
            switch (eventType)
            {
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    applyMaterial = true;
                    break;

                case EventType.DragPerform:
                    DragAndDrop.AcceptDrag();
                    applyMaterial = true;

                    ClearDragMaterialRendering();
                    break;
            }
            if (applyMaterial)
            {
                if (eventType != EventType.DragPerform)
                {
                    ClearDragMaterialRendering();
                    s_previousDraggedUponRenderer = r;
                    s_previousMaterialValue = r.sharedMaterials;

                    // Update prefab modification status cache
                    s_previousAlreadyHadPrefabModification = false;
                    if (PrefabUtility.GetPrefabInstanceStatus(s_previousDraggedUponRenderer) == PrefabInstanceStatus.Connected)
                    {
                        var materialRendererSerializedObject = new SerializedObject(s_previousDraggedUponRenderer).FindProperty("m_Materials");
                        s_previousAlreadyHadPrefabModification = materialRendererSerializedObject.prefabOverride;
                    }
                }

                Undo.RecordObject(r, Styles.undoAssignMaterial);
                var materials = r.sharedMaterials;

                bool isValidMaterialIndex = (materialIndex >= 0 && materialIndex < r.sharedMaterials.Length);
                if (!alt && isValidMaterialIndex)
                {
                    materials[materialIndex] = dragMaterial;
                }
                else
                {
                    for (int q = 0; q < materials.Length; ++q)
                        materials[q] = dragMaterial;
                }

                r.sharedMaterials = materials;
                // Since we can handle multiple objects being dragged, we cannot use the event here.
                // This will fall under respective view message processing responsibilities.
            }
        }

        internal override bool HasLargeHeader()
        {
            return true;
        }

        internal override void OnHeaderIconGUI(Rect iconRect)
        {
            OnPreviewGUI(iconRect, "IN BigTitle inner");
        }

        [MenuItem("CONTEXT/Material/Flatten Material Variant", true)]
        static bool FlattenMaterialValidate(MenuCommand command)
        {
            Material mat = command.context as Material;
            return mat.isVariant;
        }

        [MenuItem("CONTEXT/Material/Flatten Material Variant", false, 502)]
        static void FlattenMaterial(MenuCommand command)
        {
            Material mat = command.context as Material;

            Undo.RecordObject(mat, "Flatten Material Variant");
            mat.parent = null;
        }

        [MenuItem("CONTEXT/Material/Remove Unused Properties...", false, 602)]
        static void RemoveUnusedProperties(MenuCommand command)
        {
            Material mat = command.context as Material;

            Undo.RegisterCompleteObjectUndo(mat, "Remove unused properties");
            mat.RemoveUnusedProperties();
        }

        // We need to access renderer data in the GenericMenu callback, which is called from a static function
        // So we backup these variables during header rendering in case context menu is openned
        static Renderer[] renderersForContextMenu = null;
        static Material materialForContextMenu = null;

        internal override Rect DrawHeaderHelpAndSettingsGUI(Rect r)
        {
            if (!firstInspectedEditor && targets.Length == 1)
            {
                renderersForContextMenu = GetAssociatedRenderersFromInspector();
                materialForContextMenu = target as Material;
            }
            return base.DrawHeaderHelpAndSettingsGUI(r);
        }

        internal static void AddAdditionalMaterialMenuItems(GenericMenu menu)
        {
            if (renderersForContextMenu == null || renderersForContextMenu.Length == 0)
                return;

            // Capture local copies for lambda, and clear static values
            var renderers = renderersForContextMenu;
            var material = materialForContextMenu;
            renderersForContextMenu = null;
            materialForContextMenu = null;

            menu.AddItem(new GUIContent("Create Variant for Renderer"), false, () => {

                var directory = "Assets/Materials";
                Directory.CreateDirectory(directory);
                var assetPath = AssetDatabase.GetAssetPath(material);
                var assetName = Path.GetFileNameWithoutExtension(assetPath) + " Variant";
                var package = PackageManager.PackageInfo.FindForAssetPath(assetPath);
                if (package == null || package.source == PackageManager.PackageSource.Local)
                    directory = Path.GetDirectoryName(assetPath);

                var path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(directory, assetName + ".mat"));
                var variant = new Material(material) { name = assetName, parent = material };
                AssetDatabase.CreateAsset(variant, path);

                foreach (var renderer in renderers)
                {
                    Undo.RegisterCompleteObjectUndo(renderer, "Assign Material");
                    var materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (materials[i] == material)
                            materials[i] = variant;
                    }
                    renderer.sharedMaterials = materials;
                }
            });
        }
    }
} // namespace UnityEditor
