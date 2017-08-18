// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;
using System.IO;
using AnimatedBool = UnityEditor.AnimatedValues.AnimBool;

namespace UnityEditor
{

    [CustomEditor(typeof(CustomRenderTexture))]
    [CanEditMultipleObjects]
    internal class CustomRenderTextureEditor : RenderTextureEditor
    {
        private class Styles
        {
            public readonly GUIStyle    separator           = "sv_iconselector_sep";
            public readonly GUIContent  materials           = EditorGUIUtility.TextContent("Materials");
            public readonly GUIContent  shaderPass          = EditorGUIUtility.TextContent("Shader Pass|Shader Pass used to update the Custom Render Texture.");
            public readonly GUIContent  needSwap            = EditorGUIUtility.TextContent("Swap (Double Buffer)|If ticked, and if the texture is double buffered, a request is made to swap the buffers before the next update. If this is not ticked, the buffers will not be swapped.");
            public readonly GUIContent  updateMode          = EditorGUIUtility.TextContent("Update Mode|Specify how the texture should be updated.");
            public readonly GUIContent  updatePeriod        = EditorGUIUtility.TextContent("Period|Period in seconds at which real-time textures are updated (0.0 will update every frame).");
            public readonly GUIContent  doubleBuffered      = EditorGUIUtility.TextContent("Double Buffered|If ticked, the Custom Render Texture is double buffered so that you can access it during its own update. If unticked, the Custom Render Texture will be not be double buffered.");
            public readonly GUIContent  initializationMode  = EditorGUIUtility.TextContent("Initialization Mode|Specify how the texture should be initialized.");
            public readonly GUIContent  initSource          = EditorGUIUtility.TextContent("Source|Specify if the texture is initialized by a Material or by a Texture and a Color.");
            public readonly GUIContent  initColor           = EditorGUIUtility.TextContent("Color|Color with which the Custom Render Texture is initialized.");
            public readonly GUIContent  initTexture         = EditorGUIUtility.TextContent("Texture|Texture with which the Custom Render Texture is initialized (multiplied by the initialization color).");
            public readonly GUIContent  initMaterial        = EditorGUIUtility.TextContent("Material|Material with which the Custom Render Texture is initialized.");
            public readonly GUIContent  updateZoneSpace     = EditorGUIUtility.TextContent("Update Zone Space|Space in which the update zones are expressed (Normalized or Pixel space).");
            public readonly GUIContent  updateZoneList      = EditorGUIUtility.TextContent("Update Zones|List of partial update zones.");
            public readonly GUIContent  cubemapFacesLabel   = EditorGUIUtility.TextContent("Cubemap Faces|Enable or disable rendering on each face of the cubemap.");
            public readonly GUIContent  updateZoneCenter    = EditorGUIUtility.TextContent("Center|Center of the partial update zone.");
            public readonly GUIContent  updateZoneSize      = EditorGUIUtility.TextContent("Size|Size of the partial update zone.");
            public readonly GUIContent  updateZoneRotation  = EditorGUIUtility.TextContent("Rotation|Rotation of the update zone.");
            public readonly GUIContent  wrapUpdateZones     = EditorGUIUtility.TextContent("Wrap Update Zones|If ticked, Update zones will wrap around the border of the Custom Render Texture. If unticked, Update zones will be clamped at the border of the Custom Render Texture.");
            public readonly GUIContent  saveButton          = EditorGUIUtility.TextContent("Save Texture|Save the content of the Custom Render Texture to an EXR or PNG file.");

            public readonly GUIContent[] updateModeStrings = { EditorGUIUtility.TextContent("OnLoad"), EditorGUIUtility.TextContent("Realtime"), EditorGUIUtility.TextContent("OnDemand") };
            public readonly int[] updateModeValues = { (int)CustomRenderTextureUpdateMode.OnLoad, (int)CustomRenderTextureUpdateMode.Realtime, (int)CustomRenderTextureUpdateMode.OnDemand };

            public readonly GUIContent[] initSourceStrings = { EditorGUIUtility.TextContent("Texture and Color"), EditorGUIUtility.TextContent("Material") };
            public readonly int[] initSourceValues = { (int)CustomRenderTextureInitializationSource.TextureAndColor, (int)CustomRenderTextureInitializationSource.Material };

            public readonly GUIContent[] updateZoneSpaceStrings = { EditorGUIUtility.TextContent("Normalized"), EditorGUIUtility.TextContent("Pixel") };
            public readonly int[] updateZoneSpaceValues = { (int)CustomRenderTextureUpdateZoneSpace.Normalized, (int)CustomRenderTextureUpdateZoneSpace.Pixel };

            public readonly GUIContent[] cubemapFaces = { EditorGUIUtility.TextContent("+X"), EditorGUIUtility.TextContent("-X"), EditorGUIUtility.TextContent("+Y"), EditorGUIUtility.TextContent("-Y"), EditorGUIUtility.TextContent("+Z"), EditorGUIUtility.TextContent("-Z") };
        }

        static Styles s_Styles = null;
        private static Styles styles { get { if (s_Styles == null) s_Styles = new Styles(); return s_Styles; } }

        private SerializedProperty m_Material;
        private SerializedProperty m_ShaderPass;
        private SerializedProperty m_InitializationMode;
        private SerializedProperty m_InitSource;
        private SerializedProperty m_InitColor;
        private SerializedProperty m_InitTexture;
        private SerializedProperty m_InitMaterial;
        private SerializedProperty m_UpdateMode;
        private SerializedProperty m_UpdatePeriod;
        private SerializedProperty m_UpdateZoneSpace;
        private SerializedProperty m_UpdateZones;
        private SerializedProperty m_WrapUpdateZones;
        private SerializedProperty m_DoubleBuffered;
        private SerializedProperty m_CubeFaceMask;

        private UnityEditorInternal.ReorderableList m_RectList;

        private const float kCubefaceToggleWidth = 70.0f;
        private const float kRListAddButtonOffset = 16.0f;
        private const float kIndentSize = 15.0f;
        private const float kToggleWidth = 100.0f;

        readonly AnimatedBool m_ShowInitSourceAsMaterial = new AnimatedBool();

        private bool multipleEditing { get { return targets.Length > 1; } }

        void UpdateZoneVec3PropertyField(Rect rect, SerializedProperty prop, GUIContent label, bool as2D)
        {
            EditorGUI.BeginProperty(rect, label, prop);
            if (!as2D)
            {
                prop.vector3Value = EditorGUI.Vector3Field(rect, label, prop.vector3Value);
            }
            else
            {
                Vector2 newValue = EditorGUI.Vector2Field(rect, label, new Vector2(prop.vector3Value.x, prop.vector3Value.y));
                prop.vector3Value = new Vector3(newValue.x, newValue.y, prop.vector3Value.z);
            }
            EditorGUI.EndProperty();
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            CustomRenderTexture customRenderTexture = target as CustomRenderTexture;
            bool is3DTexture = customRenderTexture.dimension == UnityEngine.Rendering.TextureDimension.Tex3D;
            bool isDoubleBuffer = customRenderTexture.doubleBuffered;

            var element = m_RectList.serializedProperty.GetArrayElementAtIndex(index);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            rect.y += EditorGUIUtility.standardVerticalSpacing;
            rect.height = lineHeight;
            EditorGUI.LabelField(rect, string.Format("Update Zone {0}", index));
            rect.y += lineHeight;
            SerializedProperty centerProp = element.FindPropertyRelative("updateZoneCenter");
            UpdateZoneVec3PropertyField(rect, centerProp, styles.updateZoneCenter, !is3DTexture);

            rect.y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
            SerializedProperty sizeProp = element.FindPropertyRelative("updateZoneSize");
            UpdateZoneVec3PropertyField(rect, sizeProp, styles.updateZoneSize, !is3DTexture);

            if (!is3DTexture)
            {
                rect.y += (EditorGUIUtility.standardVerticalSpacing + lineHeight);
                EditorGUI.PropertyField(rect, element.FindPropertyRelative("rotation"), styles.updateZoneRotation);
            }

            // Shader pass
            List<GUIContent> shaderPassNames = new List<GUIContent>();
            List<int> shaderPassValues = new List<int>();
            Material material = m_Material.objectReferenceValue as Material;
            if (material != null)
            {
                BuildShaderPassPopup(material, shaderPassNames, shaderPassValues, true);
            }

            using (new EditorGUI.DisabledScope(shaderPassNames.Count == 0))
            {
                SerializedProperty passIndexProperty = element.FindPropertyRelative("passIndex");
                rect.y += (EditorGUIUtility.standardVerticalSpacing + lineHeight);
                EditorGUI.IntPopup(rect, passIndexProperty, shaderPassNames.ToArray(), shaderPassValues.ToArray(), styles.shaderPass);
            }

            if (isDoubleBuffer)
            {
                rect.y += (EditorGUIUtility.standardVerticalSpacing + lineHeight);
                EditorGUI.PropertyField(rect, element.FindPropertyRelative("needSwap"), styles.updateZoneRotation);
            }
        }

        private void OnDrawHeader(Rect rect)
        {
            GUI.Label(rect, styles.updateZoneList);
        }

        private void OnAdd(ReorderableList l)
        {
            CustomRenderTexture customRenderTexture = target as CustomRenderTexture;
            var index = l.serializedProperty.arraySize;
            l.serializedProperty.arraySize++;
            l.index = index;
            var element = l.serializedProperty.GetArrayElementAtIndex(index);
            Vector3 defaultCenter = new Vector3(0.5f, 0.5f, 0.5f);
            Vector3 defaultSize = new Vector3(1.0f, 1.0f, 1.0f);
            if (customRenderTexture.updateZoneSpace == CustomRenderTextureUpdateZoneSpace.Pixel)
            {
                Vector3 size = new Vector3((float)customRenderTexture.width, (float)customRenderTexture.height, (float)customRenderTexture.volumeDepth);
                defaultCenter.Scale(size);
                defaultSize.Scale(size);
            }
            element.FindPropertyRelative("updateZoneCenter").vector3Value = defaultCenter;
            element.FindPropertyRelative("updateZoneSize").vector3Value = defaultSize;
            element.FindPropertyRelative("rotation").floatValue = 0.0f;
            element.FindPropertyRelative("passIndex").intValue = -1;
            element.FindPropertyRelative("needSwap").boolValue = false;
        }

        private void OnRemove(ReorderableList l)
        {
            l.serializedProperty.arraySize--;
            if (l.index == l.serializedProperty.arraySize)
            {
                l.index--;
            }
        }

        private float OnElementHeight(int index)
        {
            CustomRenderTexture customRenderTexture = target as CustomRenderTexture;
            bool is3DTexture = customRenderTexture.dimension == UnityEngine.Rendering.TextureDimension.Tex3D;
            bool isDoubleBuffer = customRenderTexture.doubleBuffered;
            int lineCount = 4;  // 4 lines : Index, Zone Origin, Zone Size, Shader Pass
            if (!is3DTexture) // We don't support rotation for 3D textures so we don't show it.
                lineCount++;
            if (isDoubleBuffer) // "Swap" only shown for double buffered custom textures
                lineCount++;

            return (EditorGUIUtility.singleLineHeight + 2.0f) * lineCount;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_Material = serializedObject.FindProperty("m_Material");
            m_ShaderPass = serializedObject.FindProperty("m_ShaderPass");
            m_InitializationMode = serializedObject.FindProperty("m_InitializationMode");
            m_InitSource = serializedObject.FindProperty("m_InitSource");
            m_InitColor = serializedObject.FindProperty("m_InitColor");
            m_InitTexture = serializedObject.FindProperty("m_InitTexture");
            m_InitMaterial = serializedObject.FindProperty("m_InitMaterial");
            m_UpdateMode = serializedObject.FindProperty("m_UpdateMode");
            m_UpdatePeriod = serializedObject.FindProperty("m_UpdatePeriod");
            m_UpdateZoneSpace = serializedObject.FindProperty("m_UpdateZoneSpace");
            m_UpdateZones = serializedObject.FindProperty("m_UpdateZones");
            m_WrapUpdateZones = serializedObject.FindProperty("m_WrapUpdateZones");
            m_DoubleBuffered = serializedObject.FindProperty("m_DoubleBuffered");
            m_CubeFaceMask = serializedObject.FindProperty("m_CubemapFaceMask");

            m_RectList = new UnityEditorInternal.ReorderableList(serializedObject, m_UpdateZones);
            m_RectList.drawElementCallback = OnDrawElement;
            m_RectList.drawHeaderCallback = OnDrawHeader;
            m_RectList.onAddCallback = OnAdd;
            m_RectList.onRemoveCallback = OnRemove;
            m_RectList.elementHeightCallback = OnElementHeight;
            m_RectList.footerHeight = 0;

            m_ShowInitSourceAsMaterial.value = !m_InitSource.hasMultipleDifferentValues && (m_InitSource.intValue == (int)CustomRenderTextureInitializationSource.Material);
            m_ShowInitSourceAsMaterial.valueChanged.AddListener(Repaint);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            m_ShowInitSourceAsMaterial.valueChanged.RemoveListener(Repaint);
        }

        void DisplayRenderTextureGUI()
        {
            OnRenderTextureGUI(0);
            GUILayout.Space(10.0f);
        }

        void BuildShaderPassPopup(Material material, List<GUIContent> names, List<int> values, bool addDefaultPass)
        {
            names.Clear();
            values.Clear();

            int shaderPassCount = material.passCount;
            for (int i = 0; i < shaderPassCount; ++i)
            {
                string name = material.GetPassName(i);
                if (name.Length == 0)
                {
                    name = string.Format("Unnamed Pass {0}", i);
                }
                names.Add(EditorGUIUtility.TextContent(name));
                values.Add(i);
            }

            if (addDefaultPass)
            {
                CustomRenderTexture customRenderTexture = target as CustomRenderTexture;

                GUIContent defaultName = EditorGUIUtility.TextContent(string.Format("Default ({0})", names[customRenderTexture.shaderPass].text));
                names.Insert(0, defaultName);
                values.Insert(0, -1);
            }
        }

        void DisplayMaterialGUI()
        {
            EditorGUILayout.PropertyField(m_Material, true);
            EditorGUI.indentLevel++;

            List<GUIContent> shaderPassNames = new List<GUIContent>();
            List<int> shaderPassValues = new List<int>();
            Material material = m_Material.objectReferenceValue as Material;
            if (material != null)
            {
                BuildShaderPassPopup(material, shaderPassNames, shaderPassValues, false);
            }

            using (new EditorGUI.DisabledScope(shaderPassNames.Count == 0 || m_Material.hasMultipleDifferentValues)) // Different materials can have widely different passes, so there's no point trying to edit that when multiple editing.
            {
                if (material != null)
                    EditorGUILayout.IntPopup(m_ShaderPass, shaderPassNames.ToArray(), shaderPassValues.ToArray(), styles.shaderPass);
            }

            EditorGUI.indentLevel--;
        }

        void DisplayInitializationGUI()
        {
            m_ShowInitSourceAsMaterial.target = !m_InitSource.hasMultipleDifferentValues && (m_InitSource.intValue == (int)CustomRenderTextureInitializationSource.Material);

            EditorGUILayout.IntPopup(m_InitializationMode, styles.updateModeStrings, styles.updateModeValues, styles.initializationMode);
            EditorGUI.indentLevel++;

            EditorGUILayout.IntPopup(m_InitSource, styles.initSourceStrings, styles.initSourceValues, styles.initSource);
            if (!m_InitSource.hasMultipleDifferentValues)
            {
                EditorGUI.indentLevel++;
                if (EditorGUILayout.BeginFadeGroup(m_ShowInitSourceAsMaterial.faded))
                {
                    EditorGUILayout.PropertyField(m_InitMaterial, styles.initMaterial);
                }
                EditorGUILayout.EndFadeGroup();

                if (EditorGUILayout.BeginFadeGroup(1 - m_ShowInitSourceAsMaterial.faded))
                {
                    EditorGUILayout.PropertyField(m_InitColor, styles.initColor);
                    EditorGUILayout.PropertyField(m_InitTexture, styles.initTexture);
                }
                EditorGUILayout.EndFadeGroup();
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }

        void DisplayUpdateGUI()
        {
            EditorGUILayout.IntPopup(m_UpdateMode, styles.updateModeStrings, styles.updateModeValues, styles.updateMode);

            EditorGUI.indentLevel++;

            if (m_UpdateMode.intValue == (int)CustomRenderTextureUpdateMode.Realtime)
            {
                EditorGUILayout.PropertyField(m_UpdatePeriod, styles.updatePeriod);
            }

            EditorGUILayout.PropertyField(m_DoubleBuffered, styles.doubleBuffered);
            EditorGUILayout.PropertyField(m_WrapUpdateZones, styles.wrapUpdateZones);

            bool isCubemap = true;
            foreach (Object o in targets)
            {
                CustomRenderTexture customRenderTexture = o as CustomRenderTexture;
                if (customRenderTexture != null && customRenderTexture.dimension != UnityEngine.Rendering.TextureDimension.Cube)
                    isCubemap = false;
            }

            if (isCubemap)
            {
                int newFaceMask = 0;
                int currentFaceMask = m_CubeFaceMask.intValue;

                var AllRects = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight * 3 + EditorGUIUtility.standardVerticalSpacing * 2);
                EditorGUI.BeginProperty(AllRects, GUIContent.none, m_CubeFaceMask);

                Rect toggleRect = AllRects;
                toggleRect.width = kToggleWidth;
                toggleRect.height = EditorGUIUtility.singleLineHeight;
                int faceIndex = 0;
                {
                    Rect labelRect = AllRects;
                    EditorGUI.LabelField(labelRect, styles.cubemapFacesLabel);

                    EditorGUI.BeginChangeCheck();
                    for (int i = 0; i < 3; ++i)
                    {
                        toggleRect.x = AllRects.x + EditorGUIUtility.labelWidth - kIndentSize;

                        {
                            for (int j = 0; j < 2; ++j)
                            {
                                bool value = EditorGUI.ToggleLeft(toggleRect, styles.cubemapFaces[faceIndex], (currentFaceMask & (1 << faceIndex)) != 0);
                                if (value)
                                    newFaceMask |= (int)(1 << faceIndex);
                                faceIndex++;

                                toggleRect.x += kToggleWidth;
                            }
                        }

                        toggleRect.y += EditorGUIUtility.singleLineHeight;
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_CubeFaceMask.intValue = newFaceMask;
                    }
                }
                EditorGUI.EndProperty();
            }


            EditorGUILayout.IntPopup(m_UpdateZoneSpace, styles.updateZoneSpaceStrings, styles.updateZoneSpaceValues, styles.updateZoneSpace);

            if (!multipleEditing)
            {
                Rect listRect = GUILayoutUtility.GetRect(0.0f, m_RectList.GetHeight() + kRListAddButtonOffset, GUILayout.ExpandWidth(true)); // kRListAddButtonOffset because reorderable list does not take the  +/- button at the bottom when computing its Rects making it half occulted by other GUI elements.
                // Reorderable list seems to not take indentLevel into account properly.
                float indentSize = kIndentSize;
                listRect.x += indentSize;
                listRect.width -= indentSize;
                m_RectList.DoList(listRect);
            }
            else
            {
                EditorGUILayout.HelpBox("Update Zones cannot be changed while editing multiple Custom Textures.", MessageType.Info);
            }

            EditorGUI.indentLevel--;
        }

        void DisplayCustomRenderTextureGUI()
        {
            CustomRenderTexture customRenderTexture = target as CustomRenderTexture;

            DisplayMaterialGUI();
            EditorGUILayout.Space();
            DisplayInitializationGUI();
            EditorGUILayout.Space();
            DisplayUpdateGUI();

            EditorGUILayout.Space();

            if (customRenderTexture.updateMode != CustomRenderTextureUpdateMode.Realtime && customRenderTexture.initializationMode == CustomRenderTextureUpdateMode.Realtime)
                EditorGUILayout.HelpBox("Initialization Mode is set to Realtime but Update Mode is not. This will result in update never being visible.", MessageType.Warning);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DisplayRenderTextureGUI();
            DisplayCustomRenderTextureGUI();

            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("CONTEXT/CustomRenderTexture/Export", false)]
        static void SaveToDisk(MenuCommand command)
        {
            CustomRenderTexture texture = command.context as CustomRenderTexture;
            int width = texture.width;
            int height = texture.height;
            int depth = texture.volumeDepth;

            // This has its TextureFormat helper equivalent in C++ but since we are going to try to refactor TextureFormat/RenderTextureFormat into a single type so let's not bloat Scripting APIs with stuff that will get useless soon(tm).
            bool isFormatHDR = IsHDRFormat(texture.format);
            bool isFloatFormat = (texture.format == RenderTextureFormat.ARGBFloat || texture.format == RenderTextureFormat.RFloat);

            TextureFormat format = isFormatHDR ? TextureFormat.RGBAFloat : TextureFormat.RGBA32;
            int finalWidth = width;
            if (texture.dimension == UnityEngine.Rendering.TextureDimension.Tex3D)
                finalWidth = width * depth;
            else if (texture.dimension == UnityEngine.Rendering.TextureDimension.Cube)
                finalWidth = width * 6;

            Texture2D tex = new Texture2D(finalWidth, height, format, false);

            // Read screen contents into the texture
            if (texture.dimension == UnityEngine.Rendering.TextureDimension.Tex2D)
            {
                Graphics.SetRenderTarget(texture);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
            }
            else if (texture.dimension == UnityEngine.Rendering.TextureDimension.Tex3D)
            {
                int offset = 0;
                for (int i = 0; i < depth; ++i)
                {
                    Graphics.SetRenderTarget(texture, 0, CubemapFace.Unknown, i);
                    tex.ReadPixels(new Rect(0, 0, width, height), offset, 0);
                    tex.Apply();
                    offset += width;
                }
            }
            else
            {
                int offset = 0;
                for (int i = 0; i < 6; ++i)
                {
                    Graphics.SetRenderTarget(texture, 0, (CubemapFace)i);
                    tex.ReadPixels(new Rect(0, 0, width, height), offset, 0);
                    tex.Apply();
                    offset += width;
                }
            }

            // Encode texture into PNG
            byte[] bytes = null;
            if (isFormatHDR)
                bytes = tex.EncodeToEXR(Texture2D.EXRFlags.CompressZIP | (isFloatFormat ? Texture2D.EXRFlags.OutputAsFloat : 0));
            else
                bytes = tex.EncodeToPNG();

            Object.DestroyImmediate(tex);

            var extension = isFormatHDR ? "exr" : "png";

            var directory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(texture.GetInstanceID()));
            string assetPath = EditorUtility.SaveFilePanel("Save Custom Render Texture", directory, texture.name, extension);
            if (!string.IsNullOrEmpty(assetPath))
            {
                File.WriteAllBytes(assetPath, bytes);
                AssetDatabase.Refresh();
            }
        }

        override public string GetInfoString()
        {
            return base.GetInfoString();
        }
    }
}
