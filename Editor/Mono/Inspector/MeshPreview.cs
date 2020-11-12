// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UObject = UnityEngine.Object;

namespace UnityEditor
{
    public class MeshPreview : IDisposable
    {
        internal static class Styles
        {
            public static readonly GUIContent wireframeToggle = EditorGUIUtility.TrTextContent("Wireframe", "Show wireframe");
            public static GUIContent displayModeDropdown = EditorGUIUtility.TrTextContent("", "Change display mode");
            public static GUIContent uvChannelDropdown = EditorGUIUtility.TrTextContent("", "Change active UV channel");

            public static GUIStyle preSlider = "preSlider";
            public static GUIStyle preSliderThumb = "preSliderThumb";
        }

        internal class Settings : IDisposable
        {
            public DisplayMode displayMode = DisplayMode.Shaded;
            public int activeUVChannel = 0;
            public int activeBlendshape = 0;
            public bool drawWire = true;

            public Vector3 orthoPosition = new Vector3(0.0f, 0.0f, 0.0f);
            public Vector2 previewDir = new Vector2(0, 0);
            public Vector2 lightDir = new Vector2(0, 0);
            public Vector3 pivotPositionOffset = Vector3.zero;
            public float zoomFactor = 1.0f;
            public int checkerTextureMultiplier = 10;

            public Material shadedPreviewMaterial;
            public Material activeMaterial;
            public Material meshMultiPreviewMaterial;
            public Material wireMaterial;
            public Material lineMaterial;
            public Texture2D checkeredTexture;

            public bool[] availableDisplayModes = Enumerable.Repeat(true, 7).ToArray();
            public bool[] availableUVChannels = Enumerable.Repeat(true, 8).ToArray();

            public Settings()
            {
                shadedPreviewMaterial = new Material(Shader.Find("Standard"));
                wireMaterial = CreateWireframeMaterial();
                meshMultiPreviewMaterial = CreateMeshMultiPreviewMaterial();
                lineMaterial = CreateLineMaterial();
                checkeredTexture = EditorGUIUtility.LoadRequired("Previews/Textures/textureChecker.png") as Texture2D;
                activeMaterial = shadedPreviewMaterial;

                orthoPosition = new Vector3(0.5f, 0.5f, -1);
                previewDir = new Vector2(130, 0);
                lightDir = new Vector2(-40, -40);
                zoomFactor = 1.0f;
            }

            public void Dispose()
            {
                if (shadedPreviewMaterial != null)
                    UObject.DestroyImmediate(shadedPreviewMaterial);
                if (wireMaterial != null)
                    UObject.DestroyImmediate(wireMaterial);
                if (meshMultiPreviewMaterial != null)
                    UObject.DestroyImmediate(meshMultiPreviewMaterial);
                if (lineMaterial != null)
                    UObject.DestroyImmediate(lineMaterial);
            }
        }

        static string[] m_DisplayModes =
        {
            "Shaded", "UV Checker", "UV Layout",
            "Vertex Color", "Normals", "Tangents", "Blendshapes"
        };

        static string[] m_UVChannels =
        {
            "Channel 0", "Channel 1", "Channel 2", "Channel 3", "Channel 4", "Channel 5", "Channel 6", "Channel 7"
        };

        internal enum DisplayMode
        {
            Shaded = 0,
            UVChecker = 1,
            UVLayout = 2,
            VertexColor = 3,
            Normals = 4,
            Tangent = 5,
            Blendshapes = 6
        }

        Mesh m_Target;

        public Mesh mesh
        {
            get => m_Target;
            set => m_Target = value;
        }

        PreviewRenderUtility m_PreviewUtility;
        Settings m_Settings;

        Mesh m_BakedSkinnedMesh;
        List<string> m_BlendShapes;

        public MeshPreview(Mesh target)
        {
            m_Target = target;

            m_PreviewUtility = new PreviewRenderUtility();
            m_PreviewUtility.camera.fieldOfView = 30.0f;
            m_PreviewUtility.camera.transform.position = new Vector3(5, 5, 0);

            m_Settings = new Settings();
            m_BlendShapes = new List<string>();
            CheckAvailableAttributes();
        }

        public void Dispose()
        {
            DestroyBakedSkinnedMesh();
            m_PreviewUtility.Cleanup();
            m_Settings.Dispose();
        }

        static Material CreateWireframeMaterial()
        {
            var shader = Shader.FindBuiltin("Internal-Colored.shader");
            if (!shader)
            {
                Debug.LogWarning("Could not find the built-in Colored shader");
                return null;
            }
            var mat = new Material(shader);
            mat.hideFlags = HideFlags.HideAndDontSave;
            mat.SetColor("_Color", new Color(0, 0, 0, 0.3f));
            mat.SetFloat("_ZWrite", 0.0f);
            mat.SetFloat("_ZBias", -1.0f);
            return mat;
        }

        static Material CreateMeshMultiPreviewMaterial()
        {
            var shader = EditorGUIUtility.LoadRequired("Previews/MeshPreviewShader.shader") as Shader;
            if (!shader)
            {
                Debug.LogWarning("Could not find the built in Mesh preview shader");
                return null;
            }
            var mat = new Material(shader);
            mat.hideFlags = HideFlags.HideAndDontSave;
            return mat;
        }

        static Material CreateLineMaterial()
        {
            Shader shader = Shader.FindBuiltin("Internal-Colored.shader");
            if (!shader)
            {
                Debug.LogWarning("Could not find the built-in Colored shader");
                return null;
            }
            var mat = new Material(shader);
            mat.hideFlags = HideFlags.HideAndDontSave;
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_Cull", (float)CullMode.Off);
            mat.SetFloat("_ZWrite", 0.0f);
            return mat;
        }

        void ResetView()
        {
            m_Settings.zoomFactor = 1.0f;
            m_Settings.orthoPosition = new Vector3(0.5f, 0.5f, -1);
            m_Settings.pivotPositionOffset = Vector3.zero;

            m_Settings.activeUVChannel = 0;

            m_Settings.meshMultiPreviewMaterial.SetFloat("_UVChannel", (float)m_Settings.activeUVChannel);
            m_Settings.meshMultiPreviewMaterial.SetTexture("_MainTex", null);

            m_Settings.activeBlendshape = 0;
        }

        void FrameObject()
        {
            m_Settings.zoomFactor = 1.0f;
            m_Settings.orthoPosition = new Vector3(0.5f, 0.5f, -1);
            m_Settings.pivotPositionOffset = Vector3.zero;
        }

        void CheckAvailableAttributes()
        {
            if (!mesh.HasVertexAttribute(VertexAttribute.Color))
                m_Settings.availableDisplayModes[(int)DisplayMode.VertexColor] = false;
            if (!mesh.HasVertexAttribute(VertexAttribute.Normal))
                m_Settings.availableDisplayModes[(int)DisplayMode.Normals] = false;
            if (!mesh.HasVertexAttribute(VertexAttribute.Tangent))
                m_Settings.availableDisplayModes[(int)DisplayMode.Tangent] = false;

            int index = 0;
            for (int i = 4; i < 12; i++)
            {
                if (!mesh.HasVertexAttribute((VertexAttribute)i))
                    m_Settings.availableUVChannels[index] = false;
                index++;
            }

            var blendShapeCount = mesh.blendShapeCount;

            if (blendShapeCount > 0)
            {
                for (int i = 0; i < blendShapeCount; i++)
                {
                    m_BlendShapes.Add(mesh.GetBlendShapeName(i));
                }
            }
            else
            {
                m_Settings.availableDisplayModes[(int)DisplayMode.Blendshapes] = false;
            }
        }

        void DoPopup(Rect popupRect, string[] elements, int selectedIndex, GenericMenu.MenuFunction2 func, bool[] disabledItems)
        {
            GenericMenu menu = new GenericMenu();
            for (int i = 0; i < elements.Length; i++)
            {
                var element = elements[i];
                if (element == m_DisplayModes[(int)DisplayMode.Blendshapes] && Selection.count > 1)
                    continue;

                if (disabledItems == null || disabledItems[i])
                    menu.AddItem(new GUIContent(element), i == selectedIndex, func, i);
                else
                    menu.AddDisabledItem(new GUIContent(element));
            }
            menu.DropDown(popupRect);
        }

        void SetUVChannel(object data)
        {
            int popupIndex = (int)data;
            if (popupIndex < 0 || popupIndex >= m_Settings.availableUVChannels.Length)
                return;

            m_Settings.activeUVChannel = popupIndex;

            if (m_Settings.displayMode == DisplayMode.UVLayout || m_Settings.displayMode == DisplayMode.UVChecker)
                m_Settings.activeMaterial.SetFloat("_UVChannel", (float)popupIndex);
        }

        void DestroyBakedSkinnedMesh()
        {
            if (m_BakedSkinnedMesh)
                UObject.DestroyImmediate(m_BakedSkinnedMesh);
        }

        void SetDisplayMode(object data)
        {
            int popupIndex = (int)data;
            if (popupIndex < 0 || popupIndex >= m_DisplayModes.Length)
                return;

            m_Settings.displayMode = (DisplayMode)popupIndex;

            DestroyBakedSkinnedMesh();

            switch (m_Settings.displayMode)
            {
                case DisplayMode.Shaded:
                    OnDropDownAction(m_Settings.shadedPreviewMaterial, 0, false);
                    break;
                case DisplayMode.UVChecker:
                    OnDropDownAction(m_Settings.meshMultiPreviewMaterial, 4, false);
                    m_Settings.meshMultiPreviewMaterial.SetTexture("_MainTex", m_Settings.checkeredTexture);
                    m_Settings.meshMultiPreviewMaterial.mainTextureScale = new Vector2(m_Settings.checkerTextureMultiplier, m_Settings.checkerTextureMultiplier);
                    break;
                case DisplayMode.UVLayout:
                    OnDropDownAction(m_Settings.meshMultiPreviewMaterial, 0, true);
                    break;
                case DisplayMode.VertexColor:
                    OnDropDownAction(m_Settings.meshMultiPreviewMaterial, 1, false);
                    break;
                case DisplayMode.Normals:
                    OnDropDownAction(m_Settings.meshMultiPreviewMaterial, 2, false);
                    break;
                case DisplayMode.Tangent:
                    OnDropDownAction(m_Settings.meshMultiPreviewMaterial, 3, false);
                    break;
                case DisplayMode.Blendshapes:
                    OnDropDownAction(m_Settings.shadedPreviewMaterial, 0, false);
                    BakeSkinnedMesh();
                    break;
            }
        }

        void SetBlendshape(object data)
        {
            int popupIndex = (int)data;
            if (popupIndex < 0 || popupIndex >= m_BlendShapes.Count)
                return;

            m_Settings.activeBlendshape = popupIndex;

            DestroyBakedSkinnedMesh();
            BakeSkinnedMesh();
        }

        internal static void RenderMeshPreview(
            Mesh mesh,
            PreviewRenderUtility previewUtility,
            Settings settings,
            int meshSubset)
        {
            if (mesh == null || previewUtility == null)
                return;

            Bounds bounds = mesh.bounds;

            Transform renderCamTransform = previewUtility.camera.GetComponent<Transform>();
            previewUtility.camera.nearClipPlane = 0.0001f;
            previewUtility.camera.farClipPlane = 1000f;

            if (settings.displayMode == DisplayMode.UVLayout)
            {
                previewUtility.camera.orthographic = true;
                previewUtility.camera.orthographicSize = settings.zoomFactor;
                renderCamTransform.position = settings.orthoPosition;
                renderCamTransform.rotation = Quaternion.identity;
                DrawUVLayout(mesh, previewUtility, settings);
                return;
            }

            float halfSize = bounds.extents.magnitude;
            float distance = 4.0f * halfSize;

            previewUtility.camera.orthographic = false;
            Quaternion camRotation = Quaternion.identity;
            Vector3 camPosition = camRotation * Vector3.forward * (-distance * settings.zoomFactor) + settings.pivotPositionOffset;

            renderCamTransform.position = camPosition;
            renderCamTransform.rotation = camRotation;

            previewUtility.lights[0].intensity = 1.1f;
            previewUtility.lights[0].transform.rotation = Quaternion.Euler(-settings.lightDir.y, -settings.lightDir.x, 0);
            previewUtility.lights[1].intensity = 1.1f;
            previewUtility.lights[1].transform.rotation = Quaternion.Euler(settings.lightDir.y, settings.lightDir.x, 0);

            previewUtility.ambientColor = new Color(.1f, .1f, .1f, 0);

            RenderMeshPreviewSkipCameraAndLighting(mesh, bounds, previewUtility, settings, null, meshSubset);
        }

        static void DrawUVLayout(Mesh mesh, PreviewRenderUtility previewUtility, Settings settings)
        {
            GL.PushMatrix();

            // draw UV grid
            settings.lineMaterial.SetPass(0);

            GL.LoadProjectionMatrix(previewUtility.camera.projectionMatrix);
            GL.MultMatrix(previewUtility.camera.worldToCameraMatrix);

            GL.Begin(GL.LINES);
            const float step = 0.125f;
            for (var g = -2.0f; g <= 3.0f; g += step)
            {
                var majorLine = Mathf.Abs(g - Mathf.Round(g)) < 0.01f;
                if (majorLine)
                {
                    // major grid lines: larger area than [0..1] range, more opaque
                    GL.Color(new Color(0.6f, 0.6f, 0.7f, 1.0f));
                    GL.Vertex3(-2, g, 0);
                    GL.Vertex3(+3, g, 0);
                    GL.Vertex3(g, -2, 0);
                    GL.Vertex3(g, +3, 0);
                }
                else if (g >= 0 && g <= 1)
                {
                    // minor grid lines: only within [0..1] area, more transparent
                    GL.Color(new Color(0.6f, 0.6f, 0.7f, 0.5f));
                    GL.Vertex3(0, g, 0);
                    GL.Vertex3(1, g, 0);
                    GL.Vertex3(g, 0, 0);
                    GL.Vertex3(g, 1, 0);
                }
            }
            GL.End();

            // draw the mesh
            GL.LoadIdentity();
            settings.meshMultiPreviewMaterial.SetPass(0);
            GL.wireframe = true;
            Graphics.DrawMeshNow(mesh, previewUtility.camera.worldToCameraMatrix);
            GL.wireframe = false;

            GL.PopMatrix();
        }

        internal static Color GetSubMeshTint(int index)
        {
            // color palette generator based on "golden ratio" idea, like in
            // https://martin.ankerl.com/2009/12/09/how-to-create-random-colors-programmatically/
            var hue = Mathf.Repeat(index * 0.618f, 1);
            var sat = index == 0 ? 0f : 0.3f;
            var val = 1f;
            return Color.HSVToRGB(hue, sat, val);
        }

        internal static void RenderMeshPreviewSkipCameraAndLighting(
            Mesh mesh,
            Bounds bounds,
            PreviewRenderUtility previewUtility,
            Settings settings,
            MaterialPropertyBlock customProperties,
            int meshSubset) // -1 for whole mesh
        {
            if (mesh == null || previewUtility == null)
                return;

            Quaternion rot = Quaternion.Euler(settings.previewDir.y, 0, 0) * Quaternion.Euler(0, settings.previewDir.x, 0);
            Vector3 pos = rot * (-bounds.center);

            bool oldFog = RenderSettings.fog;
            Unsupported.SetRenderSettingsUseFogNoDirty(false);

            int submeshes = mesh.subMeshCount;
            var tintSubmeshes = false;
            var colorPropID = 0;
            if (submeshes > 1 && settings.displayMode == DisplayMode.Shaded && customProperties == null && meshSubset == -1)
            {
                tintSubmeshes = true;
                customProperties = new MaterialPropertyBlock();
                colorPropID = Shader.PropertyToID("_Color");
            }

            if (settings.activeMaterial != null)
            {
                previewUtility.camera.clearFlags = CameraClearFlags.Nothing;
                if (meshSubset < 0 || meshSubset >= submeshes)
                {
                    for (int i = 0; i < submeshes; ++i)
                    {
                        if (tintSubmeshes)
                            customProperties.SetColor(colorPropID, GetSubMeshTint(i));
                        previewUtility.DrawMesh(mesh, pos, rot, settings.activeMaterial, i, customProperties);
                    }
                }
                else
                    previewUtility.DrawMesh(mesh, pos, rot, settings.activeMaterial, meshSubset, customProperties);
                previewUtility.Render();
            }

            if (settings.wireMaterial != null && settings.drawWire)
            {
                previewUtility.camera.clearFlags = CameraClearFlags.Nothing;
                GL.wireframe = true;
                if (tintSubmeshes)
                    customProperties.SetColor(colorPropID, settings.wireMaterial.color);
                if (meshSubset < 0 || meshSubset >= submeshes)
                {
                    for (int i = 0; i < submeshes; ++i)
                    {
                        // lines/points already are wire-like; it does not make sense to overdraw
                        // them again with dark wireframe color
                        var topology = mesh.GetTopology(i);
                        if (topology == MeshTopology.Lines || topology == MeshTopology.LineStrip || topology == MeshTopology.Points)
                            continue;
                        previewUtility.DrawMesh(mesh, pos, rot, settings.wireMaterial, i, customProperties);
                    }
                }
                else
                    previewUtility.DrawMesh(mesh, pos, rot, settings.wireMaterial, meshSubset, customProperties);
                previewUtility.Render();

                GL.wireframe = false;
            }

            Unsupported.SetRenderSettingsUseFogNoDirty(oldFog);
        }

        static void SetTransformMatrix(Transform tr, Matrix4x4 mat)
        {
            // extract position
            var pos = new Vector3(mat.m03, mat.m13, mat.m23);

            // extract scale
            var scale = mat.lossyScale;

            // now remove scale from the matrix axes,
            var invScale = new Vector3(1.0f / scale.x, 1.0f / scale.y, 1.0f / scale.z);
            mat.m00 *= invScale.x; mat.m10 *= invScale.x; mat.m20 *= invScale.x;
            mat.m01 *= invScale.y; mat.m11 *= invScale.y; mat.m21 *= invScale.y;
            mat.m02 *= invScale.z; mat.m12 *= invScale.z; mat.m22 *= invScale.z;

            // and extract rotation
            var rot = mat.rotation;
            tr.localPosition = pos;
            tr.localRotation = rot;
            tr.localScale = scale;
        }

        void BakeSkinnedMesh()
        {
            if (mesh == null)
                return;

            var baseGameObjectForSkinnedMeshRenderer = new GameObject { hideFlags = HideFlags.HideAndDontSave };
            SkinnedMeshRenderer skinnedMeshRenderer = baseGameObjectForSkinnedMeshRenderer.AddComponent<SkinnedMeshRenderer>();
            skinnedMeshRenderer.hideFlags = HideFlags.HideAndDontSave;

            m_BakedSkinnedMesh = new Mesh() { hideFlags = HideFlags.HideAndDontSave };

            var isRigid = mesh.blendShapeCount > 0 && mesh.bindposes.Length == 0;

            Transform[] boneTransforms = new Transform[mesh.bindposes.Length];

            if (!isRigid)
            {
                for (int i = 0; i < boneTransforms.Length; i++)
                {
                    var bindPoseInverse = mesh.bindposes[i].inverse;
                    boneTransforms[i] = new GameObject().transform;
                    boneTransforms[i].gameObject.hideFlags = HideFlags.HideAndDontSave;
                    SetTransformMatrix(boneTransforms[i], bindPoseInverse);
                }

                skinnedMeshRenderer.bones = boneTransforms;
            }

            skinnedMeshRenderer.sharedMesh = mesh;
            skinnedMeshRenderer.SetBlendShapeWeight(m_Settings.activeBlendshape, 100f);
            skinnedMeshRenderer.BakeMesh(m_BakedSkinnedMesh);

            if (isRigid)
                m_BakedSkinnedMesh.RecalculateBounds();

            skinnedMeshRenderer.sharedMesh = null;

            UObject.DestroyImmediate(skinnedMeshRenderer);
            UObject.DestroyImmediate(baseGameObjectForSkinnedMeshRenderer);

            if (!isRigid)
            {
                for (int i = 0; i < boneTransforms.Length; i++)
                    UObject.DestroyImmediate(boneTransforms[i].gameObject);
            }
        }

        public Texture2D RenderStaticPreview(int width, int height)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
                return null;
            m_PreviewUtility.BeginStaticPreview(new Rect(0, 0, width, height));
            DoRenderPreview();
            return m_PreviewUtility.EndStaticPreview();
        }

        void DoRenderPreview()
        {
            if (m_Settings.displayMode == DisplayMode.Blendshapes)
                RenderMeshPreview(m_BakedSkinnedMesh, m_PreviewUtility, m_Settings, -1);
            else
                RenderMeshPreview(mesh, m_PreviewUtility, m_Settings, -1);
        }

        public void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            var evt = Event.current;

            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
            {
                if (evt.type == EventType.Repaint)
                    EditorGUI.DropShadowLabel(new Rect(rect.x, rect.y, rect.width, 40),
                        "Mesh preview requires\nrender texture support");
                return;
            }

            if ((evt.type == EventType.ValidateCommand || evt.type == EventType.ExecuteCommand) && evt.commandName == EventCommandNames.FrameSelected)
            {
                FrameObject();
                evt.Use();
            }

            if (evt.button <= 0 && m_Settings.displayMode != DisplayMode.UVLayout)
                m_Settings.previewDir = PreviewGUI.Drag2D(m_Settings.previewDir, rect);

            if (evt.button == 1 && m_Settings.displayMode != DisplayMode.UVLayout)
                m_Settings.lightDir = PreviewGUI.Drag2D(m_Settings.lightDir, rect);

            if (evt.type == EventType.ScrollWheel)
                MeshPreviewZoom(rect, evt);

            if (evt.type == EventType.MouseDrag && (m_Settings.displayMode == DisplayMode.UVLayout || evt.button == 2))
                MeshPreviewPan(rect, evt);

            if (evt.type != EventType.Repaint)
                return;

            m_PreviewUtility.BeginPreview(rect, background);

            DoRenderPreview();

            m_PreviewUtility.EndAndDrawPreview(rect);
        }

        public void OnPreviewSettings()
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
                return;

            GUI.enabled = true;

            if (m_Settings.displayMode == DisplayMode.UVChecker)
            {
                int oldVal = m_Settings.checkerTextureMultiplier;

                float sliderWidth = EditorStyles.label.CalcSize(new GUIContent("--------")).x;
                Rect sliderRect = EditorGUILayout.GetControlRect(GUILayout.Width(sliderWidth));
                sliderRect.x += 3;

                m_Settings.checkerTextureMultiplier = (int)GUI.HorizontalSlider(sliderRect, m_Settings.checkerTextureMultiplier, 30, 1, Styles.preSlider, Styles.preSliderThumb);
                if (oldVal != m_Settings.checkerTextureMultiplier)
                    m_Settings.activeMaterial.mainTextureScale = new Vector2(m_Settings.checkerTextureMultiplier, m_Settings.checkerTextureMultiplier);
            }

            if (m_Settings.displayMode == DisplayMode.UVLayout || m_Settings.displayMode == DisplayMode.UVChecker)
            {
                float channelDropDownWidth = EditorStyles.toolbarDropDown.CalcSize(new GUIContent("Channel 6")).x;
                Rect channelDropdownRect = EditorGUILayout.GetControlRect(GUILayout.Width(channelDropDownWidth));
                channelDropdownRect.y -= 1;
                channelDropdownRect.x += 5;
                GUIContent channel = new GUIContent("Channel " + m_Settings.activeUVChannel, Styles.uvChannelDropdown.tooltip);

                if (EditorGUI.DropdownButton(channelDropdownRect, channel, FocusType.Passive, EditorStyles.toolbarDropDown))
                    DoPopup(channelDropdownRect, m_UVChannels,
                        m_Settings.activeUVChannel, SetUVChannel, m_Settings.availableUVChannels);
            }

            if (m_Settings.displayMode == DisplayMode.Blendshapes)
            {
                float blendshapesDropDownWidth = EditorStyles.toolbarDropDown.CalcSize(new GUIContent("Blendshapes")).x;
                Rect blendshapesDropdownRect = EditorGUILayout.GetControlRect(GUILayout.Width(blendshapesDropDownWidth));
                blendshapesDropdownRect.y -= 1;
                blendshapesDropdownRect.x += 5;
                GUIContent blendshape = new GUIContent(m_BlendShapes[m_Settings.activeBlendshape], Styles.uvChannelDropdown.tooltip);

                if (EditorGUI.DropdownButton(blendshapesDropdownRect, blendshape, FocusType.Passive, EditorStyles.toolbarDropDown))
                    DoPopup(blendshapesDropdownRect, m_BlendShapes.ToArray(),
                        m_Settings.activeBlendshape, SetBlendshape, null);
            }

            // calculate width based on the longest value in display modes
            float displayModeDropDownWidth = EditorStyles.toolbarDropDown.CalcSize(new GUIContent(m_DisplayModes[(int)DisplayMode.VertexColor])).x;
            Rect displayModeDropdownRect = EditorGUILayout.GetControlRect(GUILayout.Width(displayModeDropDownWidth));
            displayModeDropdownRect.y -= 1;
            displayModeDropdownRect.x += 2;
            GUIContent displayModeDropdownContent = new GUIContent(m_DisplayModes[(int)m_Settings.displayMode], Styles.displayModeDropdown.tooltip);

            if (EditorGUI.DropdownButton(displayModeDropdownRect, displayModeDropdownContent, FocusType.Passive, EditorStyles.toolbarDropDown))
                DoPopup(displayModeDropdownRect, m_DisplayModes, (int)m_Settings.displayMode, SetDisplayMode, m_Settings.availableDisplayModes);

            using (new EditorGUI.DisabledScope(m_Settings.displayMode == DisplayMode.UVLayout))
            {
                m_Settings.drawWire = GUILayout.Toggle(m_Settings.drawWire, Styles.wireframeToggle, EditorStyles.toolbarButton);
            }
        }

        void OnDropDownAction(Material mat, int mode, bool flatUVs)
        {
            ResetView();

            m_Settings.activeMaterial = mat;

            m_Settings.activeMaterial.SetFloat("_Mode", (float)mode);
            m_Settings.activeMaterial.SetFloat("_UVChannel", 0.0f);
        }

        void MeshPreviewZoom(Rect rect, Event evt)
        {
            float zoomDelta = -(HandleUtility.niceMouseDeltaZoom * 0.5f) * 0.05f;
            var newZoom = m_Settings.zoomFactor + m_Settings.zoomFactor * zoomDelta;
            newZoom = Mathf.Clamp(newZoom, 0.1f, 10.0f);

            // we want to zoom around current mouse position
            var mouseViewPos = new Vector2(
                evt.mousePosition.x / rect.width,
                1 - evt.mousePosition.y / rect.height);
            var mouseWorldPos = m_PreviewUtility.camera.ViewportToWorldPoint(mouseViewPos);
            var mouseToCamPos = m_Settings.orthoPosition - mouseWorldPos;
            var newCamPos = mouseWorldPos + mouseToCamPos * (newZoom / m_Settings.zoomFactor);

            if (m_Settings.displayMode != DisplayMode.UVLayout)
            {
                m_PreviewUtility.camera.transform.position = new Vector3(newCamPos.x, newCamPos.y, newCamPos.z);
            }
            else
            {
                m_Settings.orthoPosition.x = newCamPos.x;
                m_Settings.orthoPosition.y = newCamPos.y;
            }

            m_Settings.zoomFactor = newZoom;
            evt.Use();
        }

        void MeshPreviewPan(Rect rect, Event evt)
        {
            var cam = m_PreviewUtility.camera;

            // event delta is in "screen" units of the preview rect, but the
            // preview camera is rendering into a render target that could
            // be different size; have to adjust drag position to match
            var delta = new Vector3(
                -evt.delta.x * cam.pixelWidth / rect.width,
                evt.delta.y * cam.pixelHeight / rect.height,
                0);

            Vector3 screenPos;
            Vector3 worldPos;
            if (m_Settings.displayMode == DisplayMode.UVLayout)
            {
                screenPos = cam.WorldToScreenPoint(m_Settings.orthoPosition);
                screenPos += delta;
                worldPos = cam.ScreenToWorldPoint(screenPos);
                m_Settings.orthoPosition.x = worldPos.x;
                m_Settings.orthoPosition.y = worldPos.y;
            }
            else
            {
                screenPos = cam.WorldToScreenPoint(m_Settings.pivotPositionOffset);
                screenPos += delta;
                worldPos = cam.ScreenToWorldPoint(screenPos) - m_Settings.pivotPositionOffset;
                m_Settings.pivotPositionOffset += worldPos;
            }

            evt.Use();
        }

        public static string GetInfoString(Mesh mesh)
        {
            if (mesh == null)
                return "";

            string info = $"{mesh.vertexCount} Vertices, {InternalMeshUtil.GetPrimitiveCount(mesh)} Triangles";

            int submeshes = mesh.subMeshCount;
            if (submeshes > 1)
                info += $", {submeshes} Sub Meshes";

            int blendShapeCount = mesh.blendShapeCount;
            if (blendShapeCount > 0)
                info += $", {blendShapeCount} Blend Shapes";

            info += " | " + InternalMeshUtil.GetVertexFormat(mesh);
            return info;
        }
    }
}
