// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine.Rendering;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Rendering
{
    [HelpURL("https://docs.unity3d.com/ScriptReference/Experimental.Rendering.GraphicsStateCollection.html")]
    [ScriptedImporter(version: 1, ext: k_FileExtension)]
    [ExcludeFromPreset]
    class GraphicsStateCollectionImporter : ScriptedImporter
    {
        private const string k_FileExtension = "graphicsstate";
        private const string k_DefaultFileName = "New Graphics State Collection." + k_FileExtension;

        public RuntimePlatform runtimePlatform;
        public GraphicsDeviceType graphicsDeviceType;
        public int version;
        public string qualityLevelName;
        public int shaderVariantCount;
        public int graphicsStateCount;

        [MenuItem("Assets/Create/Shader/Graphics State Collection", false, 309)]
        internal static void CreateGraphicsStateCollectionAsset()
        {
            var action = ScriptableObject.CreateInstance<DoCreateGraphicsStateCollection>();
            Texture2D icon = EditorGUIUtility.FindTexture(typeof(DefaultAsset));
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                EntityId.None,
                action,
                k_DefaultFileName,
                icon,
                null
            );
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            GraphicsStateCollection asset = new GraphicsStateCollection(ctx.assetPath);
            Texture2D icon = EditorGUIUtility.FindTexture(typeof(DefaultAsset));
            ctx.AddObjectToAsset("graphicsstatecollection", asset, icon);
            ctx.SetMainObject(asset);

            // Gather information about collection to display in inspector
            runtimePlatform = asset.runtimePlatform;
            graphicsDeviceType = asset.graphicsDeviceType;
            version = asset.version;
            qualityLevelName = asset.qualityLevelName;
            shaderVariantCount = asset.variantCount;
            graphicsStateCount = asset.totalGraphicsStateCount;
        }
    }

    internal class DoCreateGraphicsStateCollection : ProjectWindowCallback.AssetCreationEndAction
    {
        public override void Action(EntityId entityId, string pathName, string resourceFile)
        {
            File.WriteAllText(pathName, "{}");
            AssetDatabase.ImportAsset(pathName);
            Object asset = AssetDatabase.LoadAssetAtPath<GraphicsStateCollection>(pathName);
            if (asset != null)
            {
                ProjectWindowUtil.ShowCreatedAsset(asset);
            }
            else
            {
                File.Delete(pathName);
                Debug.LogError($"Failed to create Graphics State Collection asset at path: {pathName}");
            }
        }
    }

    [CustomEditor(typeof(GraphicsStateCollectionImporter))]
    class GraphicsStateCollectionImporterEditor : ScriptedImporterEditor
    {
        Dictionary<Shader, List<GraphicsStateCollection.ShaderVariant>> m_GroupedByShader = new Dictionary<Shader, List<GraphicsStateCollection.ShaderVariant>>();
        List<GraphicsStateCollection.GraphicsState> m_GraphicsStatesList = new List<GraphicsStateCollection.GraphicsState>();
        HashSet<EntityId> m_ExpandedShaderKeys = new HashSet<EntityId>();
        Dictionary<string, bool> m_ExpandedVariantKeys = new Dictionary<string, bool>();
        Dictionary<string, (bool vertexAttributes, bool renderState)> m_ExpandedPsoKeys = new Dictionary<string, (bool, bool)>();
        bool m_ShaderVariantsExpanded = true;
        GraphicsStateCollection m_LastCollection;
        int m_LastVariantCount = -1;

        const float k_GraphicsStateDetailsLabelWidth = 240f;

        SerializedProperty m_RuntimePlatform;
        SerializedProperty m_GraphicsDeviceType;
        SerializedProperty m_Version;
        SerializedProperty m_QualityLevelName;
        SerializedProperty m_ShaderVariantCount;
        SerializedProperty m_GraphicsStateCount;

        static class Styles
        {
            public static readonly GUIContent shaderVariantsHeader = new GUIContent("Shader Variants", "Tree view of variants in this collection. Double-click shader asset to open it.");
            [NoAutoStaticsCleanup] // lazy GUIStyle cache; no embedded Texture2D, re-initialises itself on null check
            static GUIStyle s_GreyMiniLabel;
            public static GUIStyle greyMiniLabel
            {
                get
                {
                    if (s_GreyMiniLabel == null)
                    {
                        s_GreyMiniLabel = new GUIStyle(EditorStyles.miniLabel);
                        s_GreyMiniLabel.normal.textColor = Color.grey;
                        s_GreyMiniLabel.hover.textColor = Color.grey;
                        s_GreyMiniLabel.active.textColor = Color.grey;
                        s_GreyMiniLabel.focused.textColor = Color.grey;
                    }
                    return s_GreyMiniLabel;
                }
            }
            [NoAutoStaticsCleanup] // lazy GUIStyle cache; no embedded Texture2D, re-initialises itself on null check
            static GUIStyle s_VariantBoxStyle;
            [NoAutoStaticsCleanup] // lazy GUIStyle cache; no embedded Texture2D, re-initialises itself on null check
            static GUIStyle s_GreyFoldoutStyle;
            public static GUIStyle variantBoxStyle
            {
                get
                {
                    if (s_VariantBoxStyle == null)
                    {
                        s_VariantBoxStyle = new GUIStyle(EditorStyles.helpBox);
                        s_VariantBoxStyle.padding = new RectOffset(5, 5, 5, 5);
                        s_VariantBoxStyle.margin = new RectOffset(5, 10, 5, 5);
                        s_VariantBoxStyle.clipping = TextClipping.Clip;
                    }
                    return s_VariantBoxStyle;
                }
            }
            public static GUIStyle greyFoldoutStyle
            {
                get
                {
                    if (s_GreyFoldoutStyle == null)
                    {
                        s_GreyFoldoutStyle = new GUIStyle(EditorStyles.foldout);
                        s_GreyFoldoutStyle.fontSize = 10;
                        s_GreyFoldoutStyle.normal.textColor = Color.grey;
                        s_GreyFoldoutStyle.hover.textColor = Color.grey;
                        s_GreyFoldoutStyle.onNormal.textColor = Color.grey;
                    }
                    return s_GreyFoldoutStyle;
                }
            }
        }

        public override bool showImportedObject => false;
        protected override bool needsApplyRevert => false;

        public override void OnEnable()
        {
            base.OnEnable();

            m_RuntimePlatform = serializedObject.FindProperty("runtimePlatform");
            m_GraphicsDeviceType = serializedObject.FindProperty("graphicsDeviceType");
            m_Version = serializedObject.FindProperty("version");
            m_QualityLevelName = serializedObject.FindProperty("qualityLevelName");
            m_ShaderVariantCount = serializedObject.FindProperty("shaderVariantCount");
            m_GraphicsStateCount = serializedObject.FindProperty("graphicsStateCount");
        }

        static string VariantKey(Shader shader, GraphicsStateCollection.ShaderVariant v)
        {
            EntityId shaderEntityId = shader.GetEntityId();
            string kw = "";
            if (v.keywords != null && v.keywords.Length > 0)
            {
                kw = string.Join(" ", System.Array.ConvertAll(v.keywords, k => k.name ?? ""));
            }
            return $"{shaderEntityId}/{v.passId.SubshaderIndex}/{v.passId.PassIndex}/{kw}";
        }

        static string VariantLabelShort(GraphicsStateCollection.ShaderVariant v, GraphicsStateCollection collection)
        {
            return $"PassID (Subshader {v.passId.SubshaderIndex}, Pass {v.passId.PassIndex})";
        }

        static string GraphicsStateSummary(GraphicsStateCollection.GraphicsState gs)
        {
            int attachmentCount = gs.attachments != null ? gs.attachments.Length : 0;
            return $"{attachmentCount} attachment(s) | Subpass {gs.subPassIndex.ToString()} | {gs.sampleCount} sample(s) pp | {gs.topology}";
        }

        void DrawGraphicsStateDetails(GraphicsStateCollection.GraphicsState gs, string key, float windowWidth)
        {
            int attachmentCount = gs.attachments != null ? gs.attachments.Length : 0;
            int subpassCount = gs.subPasses != null ? gs.subPasses.Length : 0;
            var grey = Styles.greyMiniLabel;

            var originalColor = EditorStyles.label.normal.textColor;
            var originalSize = EditorStyles.label.fontSize;
            EditorStyles.label.normal.textColor = Color.grey;
            EditorStyles.label.hover.textColor = Color.grey;
            EditorStyles.label.fontSize = 10;
            EditorStyles.label.clipping = TextClipping.Ellipsis;
            EditorGUIUtility.labelWidth = k_GraphicsStateDetailsLabelWidth;

            EditorGUILayout.LabelField("Render Pass", grey);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Attachment Count", attachmentCount.ToString(), grey);
            EditorGUILayout.LabelField("Subpass Count", subpassCount.ToString(), grey);
            EditorGUILayout.LabelField("Depth Attachment Index", gs.depthAttachmentIndex.ToString(), grey);
            EditorGUILayout.LabelField("Shading Rate Index", gs.shadingRateIndex.ToString(), grey);
            EditorGUILayout.LabelField("Sample Count", gs.sampleCount.ToString(), grey);
            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField("Subpass Index", gs.subPassIndex.ToString(), grey);
            EditorGUILayout.LabelField("Topology", gs.topology.ToString(), grey);
            EditorGUILayout.LabelField("Forced Cull Mode", gs.forceCullMode.ToString(), grey);
            EditorGUILayout.LabelField("Primitive Shading Rate Combiner", gs.shadingRateCombinerPrimitive.ToString(), grey);
            EditorGUILayout.LabelField("Fragment Shading Rate Combiner", gs.shadingRateCombinerFragment.ToString(), grey);
            EditorGUILayout.LabelField("Base Shading Rate Fragment Size", gs.baseShadingRate.ToString(), grey);
            EditorGUILayout.LabelField("Depth Bias", gs.depthBias.ToString(), grey);
            EditorGUILayout.LabelField("Slope Depth Bias", gs.slopeDepthBias.ToString(), grey);
            EditorGUILayout.LabelField("User Backface (Invert Culling)", gs.invertCulling.ToString(), grey);
            EditorGUILayout.LabelField("App Backface (Negative Scale)", gs.negativeScale.ToString(), grey);
            EditorGUILayout.LabelField("Wireframe", gs.wireframe.ToString(), grey);
            EditorGUILayout.LabelField("Invert Projection Matrix", gs.invertProjection.ToString(), grey);

            var (vertexAttrsExpanded, renderStateExpanded) = m_ExpandedPsoKeys[key];

            EditorGUI.indentLevel++;
            vertexAttrsExpanded = EditorGUILayout.Foldout(vertexAttrsExpanded, "Vertex Attributes:", true, Styles.greyFoldoutStyle);
            m_ExpandedPsoKeys[key] = (vertexAttrsExpanded, renderStateExpanded);
            if (vertexAttrsExpanded)
            {
                EditorGUI.indentLevel++;
                if (gs.vertexAttributes == null || gs.vertexAttributes.Length == 0)
                    EditorGUILayout.LabelField("<none>", grey);
                else
                {
                    for (int i = 0; i < gs.vertexAttributes.Length; i++)
                        EditorGUILayout.LabelField($"[{i}]", gs.vertexAttributes[i].ToString(), grey);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;

            EditorGUI.indentLevel++;
            renderStateExpanded = EditorGUILayout.Foldout(renderStateExpanded, "Render State:", true, Styles.greyFoldoutStyle);
            m_ExpandedPsoKeys[key] = (vertexAttrsExpanded, renderStateExpanded);
            if (renderStateExpanded)
            {
                EditorGUI.indentLevel++;
                var rs = gs.renderState;
                EditorGUILayout.LabelField("Mask", rs.mask.ToString(), grey);
                EditorGUILayout.LabelField("Raster", $"{rs.rasterState.cullingMode}, depthClip={rs.rasterState.depthClip}, offsetUnits={rs.rasterState.offsetUnits}", grey);
                EditorGUILayout.LabelField("Depth", $"write={rs.depthState.writeEnabled}, compare={rs.depthState.compareFunction}", grey);
                EditorGUILayout.LabelField("Stencil", $"enabled={rs.stencilState.enabled}, readMask={rs.stencilState.readMask}, writeMask={rs.stencilState.writeMask}", grey);
                EditorGUILayout.LabelField("Stencil Reference", rs.stencilReference.ToString(), grey);
                var b0 = rs.blendState.blendState0;
                EditorGUILayout.LabelField("Blend (RT0)", $"{b0.writeMask} src={b0.sourceColorBlendMode}/{b0.destinationColorBlendMode} alpha={b0.sourceAlphaBlendMode}/{b0.destinationAlphaBlendMode}", grey);
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;

            EditorStyles.label.normal.textColor = originalColor;
            EditorStyles.label.hover.textColor = originalColor;
            EditorStyles.label.fontSize = originalSize;
            EditorStyles.label.clipping = TextClipping.Clip;
            EditorGUIUtility.labelWidth = 0; // Resets to default
        }

        void RefreshVariantData(GraphicsStateCollection collection)
        {
            int count = collection.variantCount;
            if (collection == m_LastCollection && count == m_LastVariantCount)
                return;
            m_LastCollection = collection;
            m_LastVariantCount = count;

            m_GroupedByShader.Clear();
            var variants = new List<GraphicsStateCollection.ShaderVariant>();
            collection.GetVariants(variants);
            foreach (var v in variants)
            {
                Shader key = v.shader;
                if (key == null)
                    continue;
                if (!m_GroupedByShader.TryGetValue(key, out var list))
                {
                    list = new List<GraphicsStateCollection.ShaderVariant>();
                    m_GroupedByShader[key] = list;
                }
                list.Add(v);
            }
        }

        void DrawVariantTree(GraphicsStateCollection collection)
        {
            RefreshVariantData(collection);

            m_ShaderVariantsExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShaderVariantsExpanded, Styles.shaderVariantsHeader);
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (!m_ShaderVariantsExpanded)
                return;

            if (m_GroupedByShader.Count == 0)
            {
                EditorGUILayout.LabelField("No shader variants in this collection.", Styles.greyMiniLabel);
                return;
            }

            EditorGUILayout.Space();
            foreach (var (shader, variants) in m_GroupedByShader)
            {
                EntityId shaderEntityId = shader.GetEntityId();
                bool expanded = m_ExpandedShaderKeys.Contains(shaderEntityId);

                // Shader asset and foldout
                {
                    EditorGUI.indentLevel++;
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(shader, typeof(Shader), false, GUILayout.MaxWidth(1000));
                    EditorGUI.EndDisabledGroup();
                    EditorGUI.indentLevel--;

                    Rect foldoutRect = GUILayoutUtility.GetLastRect();
                    foldoutRect = new Rect(foldoutRect.x + 5, foldoutRect.y, 35, 15);
                    expanded = EditorGUI.Foldout(foldoutRect, expanded, "", true);
                    if (expanded)
                        m_ExpandedShaderKeys.Add(shaderEntityId);
                    else
                        m_ExpandedShaderKeys.Remove(shaderEntityId);
                    GUILayout.FlexibleSpace();
                }

                if (expanded)
                {
                    const int k_ListPaddingTopBottom = 4;
                    GUILayout.Space(k_ListPaddingTopBottom);

                    Rect windowRect = EditorGUILayout.BeginVertical(Styles.variantBoxStyle);
                    EditorGUI.indentLevel++;

                    foreach (var v in variants)
                    {
                        string variantKey = VariantKey(shader, v);
                        bool variantExpanded = m_ExpandedVariantKeys.TryGetValue(variantKey, out bool ve) && ve;
                        string label = VariantLabelShort(v, collection);
                        int stateCount = collection.GetGraphicsStateCountForVariant(v.shader, v.passId, v.keywords);
                        string psoLabel = $"{stateCount} PSO(s)";

                        EditorGUILayout.LabelField(label, Styles.greyMiniLabel);
                        EditorGUI.indentLevel++;

                        // Keywords and PSO count foldouts
                        {
                            int keywordCount = v.keywords?.Length ?? 0;
                            if (keywordCount > 0)
                            {
                                string keywordKey = variantKey + "_kw";
                                bool keywordsExpanded = m_ExpandedVariantKeys.TryGetValue(keywordKey, out bool ke) && ke;
                                keywordsExpanded = EditorGUILayout.Foldout(keywordsExpanded, $"Keywords ({keywordCount}):", true, Styles.greyFoldoutStyle);
                                if (keywordsExpanded)
                                {
                                    m_ExpandedVariantKeys[keywordKey] = true;
                                    EditorGUI.indentLevel++;
                                    GUIStyle wrapLabel = new GUIStyle(Styles.greyMiniLabel);
                                    wrapLabel.wordWrap = true;
                                    string fullKeywords = string.Join("  ", System.Array.ConvertAll(v.keywords, k => k.name ?? ""));
                                    EditorGUILayout.LabelField(fullKeywords, wrapLabel);
                                    EditorGUI.indentLevel--;
                                }
                                else
                                    m_ExpandedVariantKeys[keywordKey] = false;
                            }
                            else
                                EditorGUILayout.LabelField("Keywords: <no keywords>", Styles.greyMiniLabel);

                            variantExpanded = EditorGUILayout.Foldout(variantExpanded, psoLabel, true, Styles.greyFoldoutStyle);
                            m_ExpandedVariantKeys[variantKey] = variantExpanded;
                        }

                        // Graphics States details
                        if (variantExpanded && v.shader != null)
                        {
                            EditorGUI.indentLevel++;

                            m_GraphicsStatesList.Clear();
                            collection.GetGraphicsStatesForVariant(v.shader, v.passId, v.keywords, m_GraphicsStatesList);
                            if (m_GraphicsStatesList.Count == 0)
                                EditorGUILayout.LabelField("No graphics states recorded", Styles.greyMiniLabel);
                            else
                            {
                                for (int i = 0; i < m_GraphicsStatesList.Count; i++)
                                {
                                    string psoKey = $"{variantKey}|{i}";
                                    bool psoExpanded = m_ExpandedPsoKeys.ContainsKey(psoKey);
                                    string psoSummary = GraphicsStateSummary(m_GraphicsStatesList[i]);
                                    GUIStyle clipped = new GUIStyle(Styles.greyFoldoutStyle);
                                    clipped.fixedWidth = windowRect.width - 50;
                                    clipped.clipping = TextClipping.Ellipsis;

                                    psoExpanded = EditorGUILayout.Foldout(psoExpanded, $"{i.ToString()}.)  {psoSummary}", true, clipped);
                                    if (psoExpanded)
                                    {
                                        if (!m_ExpandedPsoKeys.ContainsKey(psoKey))
                                            m_ExpandedPsoKeys[psoKey] = (false, false);
                                    }
                                    else
                                        m_ExpandedPsoKeys.Remove(psoKey);

                                    if (psoExpanded)
                                        DrawGraphicsStateDetails(m_GraphicsStatesList[i], psoKey, windowRect.width);
                                }
                            }
                            EditorGUI.indentLevel--;
                        }
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndVertical();
                    GUILayout.Space(k_ListPaddingTopBottom);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(m_ShaderVariantCount, new GUIContent("Total Variant Count"));
                EditorGUILayout.PropertyField(m_GraphicsStateCount, new GUIContent("Total GraphicsState Count"));
                EditorGUILayout.PropertyField(m_GraphicsDeviceType, new GUIContent("Runtime API"));
            }

            serializedObject.ApplyModifiedProperties();

            var importer = (GraphicsStateCollectionImporter)target;
            string path = importer.assetPath;
            GraphicsStateCollection collection = AssetDatabase.LoadMainAssetAtPath(path) as GraphicsStateCollection;

            if (collection != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Additional Information", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(m_RuntimePlatform);
                    EditorGUILayout.PropertyField(m_QualityLevelName, new GUIContent("Quality Level"));
                    EditorGUILayout.PropertyField(m_Version);
                }
                serializedObject.ApplyModifiedProperties();

                EditorGUILayout.Space();
                DrawVariantTree(collection);
            }
        }
    }
}
