// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Look Dev", useTypeNameAsIconName = true)]
    internal class LookDevView : EditorWindow, IHasCustomMenu
    {
        static readonly Vector2 s_MinWindowSize = new Vector2(300, 60);

        // Note: Color init in OnEnable
        public static Color32 m_FirstViewGizmoColor;
        public static Color32 m_SecondViewGizmoColor;

        private static string m_configAssetPath = "Library/LookDevConfig.asset";

        bool m_IsSaveRegistered = false;

        public class Styles
        {
            public readonly GUIStyle sBigTitleInnerStyle    = "IN BigTitle inner";
            public readonly GUIStyle sToolBarButton         = "toolbarbutton";

            public readonly GUIContent sSingleMode1         = EditorGUIUtility.IconContent("LookDevSingle1", "Single1|Single1 object view");
            public readonly GUIContent sSingleMode2         = EditorGUIUtility.IconContent("LookDevSingle2", "Single2|Single2 object view");
            public readonly GUIContent sSideBySideMode      = EditorGUIUtility.IconContent("LookDevSideBySide", "Side|Side by side comparison view");
            public readonly GUIContent sSplitMode           = EditorGUIUtility.IconContent("LookDevSplit", "Split|Single object split comparison view");
            public readonly GUIContent sZoneMode            = EditorGUIUtility.IconContent("LookDevZone", "Zone|Single object zone comparison view");
            public readonly GUIContent sLinkActive          = EditorGUIUtility.IconContent("LookDevMirrorViewsActive", "Link|Links the property between the different views");
            public readonly GUIContent sLinkInactive        = EditorGUIUtility.IconContent("LookDevMirrorViewsInactive", "Link|Links the property between the different views");
            public readonly GUIContent sDragAndDropObjsText = EditorGUIUtility.TextContent("Drag and drop Prefabs here.");


            public readonly GUIStyle[] sPropertyLabelStyle =
            {
                new GUIStyle(EditorStyles.miniLabel),
                new GUIStyle(EditorStyles.miniLabel),
                new GUIStyle(EditorStyles.miniLabel)
            };

            public Styles()
            {
                sPropertyLabelStyle[0].normal.textColor = LookDevView.m_FirstViewGizmoColor;
                sPropertyLabelStyle[1].normal.textColor = LookDevView.m_SecondViewGizmoColor;
            }
        }

        static Styles s_Styles = null;
        public static Styles styles { get { if (s_Styles == null) s_Styles = new Styles(); return s_Styles; } }

        internal class PreviewContextCB
        {
            public CommandBuffer m_drawBallCB;
            public CommandBuffer m_patchGBufferCB;
            public MaterialPropertyBlock m_drawBallPB;

            public PreviewContextCB()
            {
                m_drawBallCB = new CommandBuffer();
                m_drawBallCB.name = "draw ball";
                m_patchGBufferCB = new CommandBuffer();
                m_patchGBufferCB.name = "patch gbuffer";
                m_drawBallPB = new MaterialPropertyBlock();
            }
        };

        internal class PreviewContext
        {
            public enum PreviewContextPass
            {
                kView = 0,
                kViewWithShadow,
                kShadow,
                kCount
            };

            public PreviewRenderUtility[]   m_PreviewUtility = new PreviewRenderUtility[(int)PreviewContextPass.kCount];
            public Texture[]                m_PreviewResult = new Texture[(int)PreviewContextPass.kCount];
            public PreviewContextCB[]       m_PreviewCB = new PreviewContextCB[(int)PreviewContextPass.kCount];


            public PreviewContext()
            {
                for (int i = 0; i < (int)PreviewContextPass.kCount; ++i)
                {
                    m_PreviewUtility[i] = new PreviewRenderUtility();
                    m_PreviewUtility[i].camera.fieldOfView = 30.0f;
                    m_PreviewUtility[i].camera.cullingMask = 1 << Camera.PreviewCullingLayer;
                    m_PreviewCB[i] = new PreviewContextCB();
                }
            }

            public void Cleanup()
            {
                for (int contextIndex = 0; contextIndex < (int)PreviewContext.PreviewContextPass.kCount; ++contextIndex)
                {
                    if (m_PreviewUtility[contextIndex] != null)
                    {
                        m_PreviewUtility[contextIndex].Cleanup();
                        m_PreviewUtility[contextIndex] = null;
                    }
                }
            }
        }

        public static void DrawFullScreenQuad(Rect previewRect)
        {
            GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            GL.PushMatrix();
            GL.LoadOrtho();
            GL.Viewport(previewRect);

            GL.Begin(GL.QUADS);
            GL.TexCoord2(0, 0);
            GL.Vertex3(0.0F, 0.0F, 0);
            GL.TexCoord2(0, 1);
            GL.Vertex3(0.0F, 1.0F, 0);
            GL.TexCoord2(1, 1);
            GL.Vertex3(1.0F, 1.0F, 0);
            GL.TexCoord2(1, 0);
            GL.Vertex3(1.0F, 0.0F, 0);
            GL.End();
            GL.PopMatrix();
            GL.sRGBWrite = false;
        }

        private PreviewContext[]                        m_PreviewUtilityContexts = new PreviewContext[2];
        private GUIContent                              m_RenderdocContent;
        private GUIContent                              m_SyncLightVertical;
        private GUIContent                              m_ResetEnvironment;
        private Rect[]                                  m_PreviewRects = new Rect[3];
        private Rect                                    m_DisplayRect;
        private Vector4                                 m_ScreenRatio;
        private Vector2                                 m_OnMouseDownOffsetToGizmo;
        private LookDevEditionContext                   m_CurrentDragContext = LookDevEditionContext.None;
        private LookDevOperationType                    m_LookDevOperationType = LookDevOperationType.None;
        private RenderTexture                           m_FinalCompositionTexture = null;
        private LookDevEnvironmentWindow                m_LookDevEnvWindow = null;
        private bool                                    m_ShowLookDevEnvWindow = false;
        private bool                                    m_CaptureRD = false;

        private bool[]                                  m_LookDevModeToggles = new bool[(int)LookDevMode.Count];

        private float                                   m_GizmoThickness = 0.0028f;
        private float                                   m_GizmoThicknessSelected = 0.015f;
        private float                                   m_GizmoCircleRadius = 0.014f;
        private float                                   m_GizmoCircleRadiusSelected = 0.03f;
        private bool                                    m_ForceGizmoRenderSelector = false;
        private LookDevOperationType                    m_GizmoRenderMode = LookDevOperationType.None;
        private float                                   m_BlendFactorCircleSelectionRadius = 0.03f;
        private float                                   m_BlendFactorCircleRadius = 0.01f;

        private Rect                                    m_ControlWindowRect;
        private float                                   kLineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        private LookDevConfig                           m_LookDevConfig = null;// Configuration of the 3D view. This will be saved in the library folder (it should not be versionned)
        private LookDevEnvironmentLibrary               m_LookDevEnvLibrary = null;// This is the working copy of the library in use. It's always an instance of the chosen library in order not to alter the original one.
        [SerializeField]
        private LookDevEnvironmentLibrary               m_LookDevUserEnvLibrary = null;// Current user library. This is the asset we are going to serialize in the end.

        private bool                                    m_DisplayDebugGizmo = false;
        private float                                   kReferenceScale = 1080.0f;

        private int                                     m_hotControlID = 0;

        private float                                   m_DirBias = 0.01f;
        private float                                   m_DirNormalBias = 0.4f;
        private float                                   m_CurrentObjRotationOffset = 0.0f;
        private float                                   m_ObjRotationAcc = 0.0f;
        private float                                   m_EnvRotationAcc = 0.0f;

        // This a workaround: Currently the edition scene and any preview scenes are not distinct. They share the same world space
        // So if there are reflection probes in the main edition scene, they will affect the lookdev.
        // Since there is now good way yet to separate the two scenes properly, we just render far under ground and hope that no reflections probes are there...
        private float                                   kDefaultSceneHeight = -500.0f;


        private CameraControllerStandard                m_CameraController = new CameraControllerStandard();
        internal PreviewContext[] previewUtilityContexts { get { return m_PreviewUtilityContexts; } }


        public int hotControl
        {
            get { return m_hotControlID; }
        }

        public LookDevConfig config
        {
            get { return m_LookDevConfig; }
        }

        public LookDevEnvironmentLibrary envLibrary
        {
            get { return m_LookDevEnvLibrary; }
            set
            {
                if (value == null)
                {
                    // "None" is selected, we have to reset to the default library
                    m_LookDevEnvLibrary = ScriptableObject.CreateInstance<LookDevEnvironmentLibrary>();
                    m_LookDevUserEnvLibrary = null;
                }
                else
                {
                    // Update the current user library
                    if (value != m_LookDevUserEnvLibrary)
                    {
                        m_LookDevUserEnvLibrary = value;
                        m_LookDevEnvLibrary = ScriptableObject.Instantiate<LookDevEnvironmentLibrary>(value);
                        m_LookDevEnvLibrary.SetLookDevView(this);
                    }
                }

                int hdriCount = m_LookDevEnvLibrary.hdriCount;
                if (m_LookDevConfig.GetIntProperty(LookDevProperty.HDRI, LookDevEditionContext.Left) >= hdriCount || m_LookDevConfig.GetIntProperty(LookDevProperty.HDRI, LookDevEditionContext.Right) >= hdriCount)
                {
                    // When switching library, the new one can have fewer HDRIs so we reset the HDRI property to zero to make sure we don't get out of range.
                    m_LookDevConfig.UpdatePropertyLink(LookDevProperty.HDRI, true);
                    m_LookDevConfig.UpdateIntProperty(LookDevProperty.HDRI, 0);
                }
            }
        }
        public LookDevEnvironmentLibrary userEnvLibrary
        {
            get { return m_LookDevUserEnvLibrary; }
        }

        public void CreateNewLibrary(string assetPath)
        {
            // Create a new library based on the state of the current one.
            LookDevEnvironmentLibrary newLibrary = ScriptableObject.Instantiate(envLibrary) as LookDevEnvironmentLibrary;
            AssetDatabase.CreateAsset(newLibrary, assetPath);
            envLibrary = AssetDatabase.LoadAssetAtPath(assetPath, typeof(LookDevEnvironmentLibrary)) as LookDevEnvironmentLibrary;
        }

        public static void OpenInLookDevTool(UnityEngine.Object go)
        {
            LookDevView lookDev = EditorWindow.GetWindow<LookDevView>();
            lookDev.m_LookDevConfig.SetCurrentPreviewObject(go as GameObject, LookDevEditionContext.Left);
            lookDev.m_LookDevConfig.SetCurrentPreviewObject(go as GameObject, LookDevEditionContext.Right);
            lookDev.Frame(LookDevEditionContext.Left, false);
            lookDev.Repaint();
        }

        public LookDevView()
        {
            for (int i = 0; i < (int)LookDevMode.Count; ++i)
            {
                m_LookDevModeToggles[i] = false;
            }

            wantsMouseMove = true;

            minSize = s_MinWindowSize;
        }

        private void Initialize()
        {
            LookDevResources.Initialize();

            InitializePreviewUtilities();

            LoadLookDevConfig();

            // When we reload the scene the pointer like m_DefaultHDRI can become null but the list of HDRI is not null
            // so test it to be sure we don't add two time default HDRI.
            if (m_LookDevEnvLibrary.hdriList.Count == 0)
            {
                UpdateContextWithCurrentHDRI(LookDevResources.m_DefaultHDRI);
            }

            if (m_LookDevEnvWindow == null)
            {
                m_LookDevEnvWindow = new LookDevEnvironmentWindow(this);
            }
        }

        void InitializePreviewUtilities()
        {
            if (m_PreviewUtilityContexts[0] == null)
            {
                // Do this check only the first time
                if (QualitySettings.activeColorSpace == ColorSpace.Gamma)
                    Debug.LogWarning("Look Dev is designed for linear color space. Currently project is set to gamma color space. This can be changed in player settings.");
                if (Rendering.EditorGraphicsSettings.GetCurrentTierSettings().renderingPath != RenderingPath.DeferredShading)
                    Debug.LogWarning("Look Dev switched rendering mode to deferred shading for display.");
                if (Camera.main.allowHDR == false)
                    Debug.LogWarning("Look Dev switched HDR mode on for display.");

                for (int i = 0; i < 2; ++i)
                    m_PreviewUtilityContexts[i] = new PreviewContext();
            }
        }

        private void Cleanup()
        {
            LookDevResources.Cleanup();

            m_LookDevConfig.Cleanup();

            for (int i = 0; i < 2; ++i)
            {
                if (m_PreviewUtilityContexts[i] != null)
                    m_PreviewUtilityContexts[i].Cleanup();
                m_PreviewUtilityContexts[i] = null;
            }

            if (m_FinalCompositionTexture)
            {
                UnityEngine.Object.DestroyImmediate(m_FinalCompositionTexture);
                m_FinalCompositionTexture = null;
            }
        }

        public void OnDestroy()
        {
            SaveLookDevConfig();
            Cleanup();
        }

        private void UpdateRenderTexture(Rect rect)
        {
            int rtWidth = (int)rect.width;
            int rtHeight = (int)rect.height;
            if (!m_FinalCompositionTexture || m_FinalCompositionTexture.width != rtWidth || m_FinalCompositionTexture.height != rtHeight)
            {
                if (m_FinalCompositionTexture)
                {
                    UnityEngine.Object.DestroyImmediate(m_FinalCompositionTexture);
                    m_FinalCompositionTexture = null;
                }

                m_FinalCompositionTexture = new RenderTexture(rtWidth, rtHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                m_FinalCompositionTexture.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        private void GetRenderableBoundsRecurse(ref Bounds bounds, GameObject go)
        {
            // Do we have a mesh?
            MeshRenderer renderer = go.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
            MeshFilter filter = go.GetComponent(typeof(MeshFilter)) as MeshFilter;
            if (renderer && filter && filter.sharedMesh)
            {
                // To prevent origo from always being included in bounds we initialize it
                // with renderer.bounds. This ensures correct bounds for meshes with origo outside the mesh.
                if (bounds.extents == Vector3.zero)
                    bounds = renderer.bounds;
                else
                    bounds.Encapsulate(renderer.bounds);
            }

            // Do we have a skinned mesh?
            SkinnedMeshRenderer skin = go.GetComponent(typeof(SkinnedMeshRenderer)) as SkinnedMeshRenderer;
            if (skin && skin.sharedMesh)
            {
                if (bounds.extents == Vector3.zero)
                    bounds = skin.bounds;
                else
                    bounds.Encapsulate(skin.bounds);
            }

            // Do we have a Sprite?
            SpriteRenderer sprite = go.GetComponent(typeof(SpriteRenderer)) as SpriteRenderer;
            if (sprite && sprite.sprite)
            {
                if (bounds.extents == Vector3.zero)
                    bounds = sprite.bounds;
                else
                    bounds.Encapsulate(sprite.bounds);
            }

            // Recurse into children
            foreach (Transform t in go.transform)
            {
                GetRenderableBoundsRecurse(ref bounds, t.gameObject);
            }
        }

        private void RenderScene(Rect previewRect, LookDevContext lookDevContext, PreviewContext previewUtilityContext, GameObject[] currentObjectByPasses, CameraState originalCameraState, bool secondView)
        {
            // Explanation of how this work:
            // To simulate the shadow of a directional light, we want to interpolate between two environments. One with a skybox without sun for shadowed area and the other with the sun.
            // To create the lerp mask we render the scene with a white diffuse material (by patching the GBuffer so object can used their regular material) and a single shadow casting directional light.
            // This will create a mask where the shadowed area is 0 and the lit area is 1 with a smooth NDotL transition in-between.
            // Then we render the scene twice, once with the sunless environment and once with the original environment.
            // At last we composite everything in the lookdev compositing pass.

            // Do a render with a white material and no GI at all. This will result in a shadow mask
            // If shadows are disabled or if we need a debug view mode, we don't send an object to be rendered so that the mask stays neutral white.
            bool needNeutralMask = !m_LookDevConfig.enableShadowCubemap || (m_LookDevConfig.enableShadowCubemap && ((DrawCameraMode)lookDevContext.shadingMode != DrawCameraMode.Normal) && ((DrawCameraMode)lookDevContext.shadingMode != DrawCameraMode.TexturedWire));
            previewUtilityContext.m_PreviewResult[(int)PreviewContext.PreviewContextPass.kShadow] = needNeutralMask ?
                Texture2D.whiteTexture :
                RenderScene(previewRect, lookDevContext, previewUtilityContext, currentObjectByPasses[(int)PreviewContext.PreviewContextPass.kShadow], originalCameraState, null, PreviewContext.PreviewContextPass.kShadow, secondView);
            // Render the scene normally.
            CubemapInfo cubemapInfo = m_LookDevEnvLibrary.hdriList[lookDevContext.currentHDRIIndex];
            previewUtilityContext.m_PreviewResult[(int)PreviewContext.PreviewContextPass.kView] = RenderScene(previewRect, lookDevContext, previewUtilityContext, currentObjectByPasses[(int)PreviewContext.PreviewContextPass.kView], originalCameraState, cubemapInfo, PreviewContext.PreviewContextPass.kView, secondView);
            // Render the scene with the environment without the sun
            previewUtilityContext.m_PreviewResult[(int)PreviewContext.PreviewContextPass.kViewWithShadow] = RenderScene(previewRect, lookDevContext, previewUtilityContext, currentObjectByPasses[(int)PreviewContext.PreviewContextPass.kViewWithShadow], originalCameraState, cubemapInfo.cubemapShadowInfo, PreviewContext.PreviewContextPass.kViewWithShadow, secondView);
        }

        private Texture RenderScene(Rect previewRect, LookDevContext lookDevContext, PreviewContext previewUtilityContext, GameObject currentObject, CameraState originalCameraState, CubemapInfo cubemapInfo, PreviewContext.PreviewContextPass contextPass, bool secondView)
        {
            PreviewRenderUtility previewUtility = previewUtilityContext.m_PreviewUtility[(int)contextPass];
            PreviewContextCB contextCB          = previewUtilityContext.m_PreviewCB[(int)contextPass];

            // Save several lighting panel parameter as they are not unique to our lookdev but share with the scene view :(
            UnityEngine.Rendering.DefaultReflectionMode oldReflectionMode = RenderSettings.defaultReflectionMode;
            UnityEngine.Rendering.AmbientMode oldAmbientMode = RenderSettings.ambientMode;
            Cubemap oldCubeMap = RenderSettings.customReflection;
            Material oldSkybox = RenderSettings.skybox;
            float oldAmbientIntensity = RenderSettings.ambientIntensity;
            SphericalHarmonicsL2 oldAmbientProbe = RenderSettings.ambientProbe;
            float oldReflectionIntensity = RenderSettings.reflectionIntensity;

            previewUtility.BeginPreview(previewRect, styles.sBigTitleInnerStyle);
            bool shadowPass = contextPass == PreviewContext.PreviewContextPass.kShadow;

            DrawCameraMode shadingMode = (DrawCameraMode)lookDevContext.shadingMode;
            bool needDebugMode = shadingMode != DrawCameraMode.Normal && shadingMode != DrawCameraMode.TexturedWire;

            float oldShadowDistance = QualitySettings.shadowDistance;
            Vector3 oldShadowCascade4Split = QualitySettings.shadowCascade4Split;

            float cubemapOffset = m_LookDevEnvLibrary.hdriList[lookDevContext.currentHDRIIndex].angleOffset;
            // We need to invert the sign of the envRotation because we move the camera and not the cubemap itself
            float envRotation = -(lookDevContext.envRotation + cubemapOffset);

            // Here is a little trick. The lookdev allows us to rotate the environment.
            // The problem is that we can't properly rotate it in the engine right now.
            // So to simulate this, we always rotate the camera around the center of the world and compensate by rotating and moving the observed object by the inverse transform.
            CameraState cameraState = originalCameraState.Clone();
            Vector3 angles = cameraState.rotation.value.eulerAngles;

            cameraState.rotation.value = Quaternion.Euler(angles + new Vector3(0.0f, envRotation, 0.0f)); // Add environment rotation to current camera orientation
            cameraState.pivot.value = new Vector3(0.0f, kDefaultSceneHeight, 0.0f); // Look at the center of the world
            cameraState.UpdateCamera(previewUtility.camera);

            previewUtility.camera.renderingPath = RenderingPath.DeferredShading;
            previewUtility.camera.clearFlags = shadowPass ? CameraClearFlags.Color : CameraClearFlags.Skybox; // We need to clear to white for the shadow mask to work properly
            previewUtility.camera.backgroundColor = Color.white;
            previewUtility.camera.allowHDR = true;

            for (int lightIndex = 0; lightIndex < 2; lightIndex++)
            {
                previewUtility.lights[lightIndex].enabled = false;
                previewUtility.lights[lightIndex].intensity = 0.0f;
                previewUtility.lights[lightIndex].shadows = LightShadows.None;
            }

            // If shadows are disable or if we are in a Debug view mode (albedo, normal, etc) we don't want to have shadows in the mask
            if (currentObject != null && shadowPass && m_LookDevConfig.enableShadowCubemap && !needDebugMode) // The default shadow flag serves as a switch to completely remove the shadows, even those specific to different environments.
            {
                Bounds bounds = new Bounds(currentObject.transform.position, Vector3.zero);
                GetRenderableBoundsRecurse(ref bounds, currentObject);

                float maxBound = Mathf.Max(bounds.max.x, Mathf.Max(bounds.max.y, bounds.max.z)); // Compute the approximate size of the object
                float shadowMaxDistance = m_LookDevConfig.shadowDistance > 0.0f ? m_LookDevConfig.shadowDistance : 25.0f * maxBound; // We want to see shadows until a distance of 25 times the size of the object.
                float splitBaseDistance = Mathf.Min(maxBound * 2.0f, 20.0f) / shadowMaxDistance; // Try to conserve a bit of shadow precision for big objects

                QualitySettings.shadowDistance = shadowMaxDistance;
                QualitySettings.shadowCascade4Split = new Vector3(Mathf.Clamp(splitBaseDistance, 0.0f, 1.0f), Mathf.Clamp(splitBaseDistance * 2.0f, 0.0f, 1.0f), Mathf.Clamp(splitBaseDistance * 6.0f, 0.0f, 1.0f));

                ShadowInfo shadowInfo = m_LookDevEnvLibrary.hdriList[lookDevContext.currentHDRIIndex].shadowInfo;
                // previewUtility.m_Light[0].enabled = true;  // Apparently doing this will add the light to the global manager, making it visible in the scene view... so don't do that.
                previewUtility.lights[0].intensity = 1.0f;
                previewUtility.lights[0].color = Color.white;
                previewUtility.lights[0].shadows = LightShadows.Soft;
                previewUtility.lights[0].shadowBias = m_DirBias;
                previewUtility.lights[0].shadowNormalBias = m_DirNormalBias;
                previewUtility.lights[0].transform.rotation = Quaternion.Euler(shadowInfo.latitude, shadowInfo.longitude, 0.0f);

                // Can't pre-record the command buffer :( because to make MaterialPropertyBlock working, it need to be setup each frame with DrawMesh. Calling DrawMesh mean we need to call clear
                // else DrawMesh accumulate each frame. Calling clear mean we can't pre-record command buffer.

                // We need a command buffer to patch the Gbuffer to generate the 'fake' screen space shadow map
                // Patch diffuse+specular+smoothness into two MRTs
                contextCB.m_patchGBufferCB.Clear();
                RenderTargetIdentifier[] mrt = { BuiltinRenderTextureType.GBuffer0, BuiltinRenderTextureType.GBuffer1 };
                contextCB.m_patchGBufferCB.SetRenderTarget(mrt, BuiltinRenderTextureType.CameraTarget);
                contextCB.m_patchGBufferCB.DrawMesh(LookDevResources.m_ScreenQuadMesh, Matrix4x4.identity, LookDevResources.m_GBufferPatchMaterial);

                // set this command buffer to be executed just before deferred lighting pass
                previewUtility.camera.AddCommandBuffer(CameraEvent.AfterGBuffer, contextCB.m_patchGBufferCB);

                if (m_LookDevConfig.showBalls)
                {
                    // We need to draw the balls in order that they are not shadowed
                    contextCB.m_drawBallCB.Clear();
                    // Patch lighting buffer - This will write the shape of the screen space balls after the render of the shadow map to be sure we display only the normal view 0
                    RenderTargetIdentifier[] lightingBuffer = { BuiltinRenderTextureType.CameraTarget };
                    contextCB.m_drawBallCB.SetRenderTarget(lightingBuffer, BuiltinRenderTextureType.CameraTarget);
                    contextCB.m_drawBallPB.SetVector("_WindowsSize", new Vector4(previewUtility.camera.pixelWidth, previewUtility.camera.pixelHeight, secondView ? 1.0f : 0.0f, 0));
                    contextCB.m_drawBallCB.DrawMesh(LookDevResources.m_ScreenQuadMesh, Matrix4x4.identity, LookDevResources.m_DrawBallsMaterial, 0, 1, contextCB.m_drawBallPB);

                    // Draw ball in screen space by patching the GBuffer
                    previewUtility.camera.AddCommandBuffer(CameraEvent.AfterLighting, contextCB.m_drawBallCB);
                }
            }

            previewUtility.ambientColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);

            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;

            Cubemap skyboxCubemap = cubemapInfo != null ? cubemapInfo.cubemap : null;
            LookDevResources.m_SkyboxMaterial.SetTexture("_Tex", skyboxCubemap);
            LookDevResources.m_SkyboxMaterial.SetFloat("_Exposure", 1.0f); // Exposure handled in the compositing shader

            RenderSettings.customReflection = skyboxCubemap;

            // Cache computation of ambient probe
            if (cubemapInfo != null && !cubemapInfo.alreadyComputed && !shadowPass)
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox; // Force skybox for our HDRI
                RenderSettings.skybox = LookDevResources.m_SkyboxMaterial;
                DynamicGI.UpdateEnvironment();
                cubemapInfo.ambientProbe = RenderSettings.ambientProbe;
                RenderSettings.skybox = oldSkybox;
                cubemapInfo.alreadyComputed = true;
            }

            RenderSettings.ambientProbe = cubemapInfo != null ? cubemapInfo.ambientProbe : LookDevResources.m_ZeroAmbientProbe;
            RenderSettings.skybox = LookDevResources.m_SkyboxMaterial;
            RenderSettings.ambientIntensity = 1.0f; // fix this to 1, this parameter should not exist!
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox; // Force skybox for our HDRI
            RenderSettings.reflectionIntensity = 1.0f;

            // Note: This test must be all after the setup of the RenderSettings.ambientProbe
            if (contextPass == PreviewContext.PreviewContextPass.kView && m_LookDevConfig.showBalls)
            {
                // To display greyball we need ambient probe information

                // Copy/paste from SHContantCache.cpp and convert to C#. Yes we should expose C++ impl to c #
                const int kSHCoefficients = 7;
                Vector4[] shaderConstants = new Vector4[kSHCoefficients];

                GetShaderConstantsFromNormalizedSH(RenderSettings.ambientProbe, shaderConstants);

                // Can't pre-record the command buffer :( because to make MaterialPropertyBlock working, it need to be setup each frame with DrawMesh. Calling DrawMesh mean we need to call clear
                // else DrawMesh accumulate each frame. Calling clear mean we can't pre-record command buffer.

                // We modify the Gbuffer to draw chrome/Grey balls
                contextCB.m_drawBallCB.Clear();
                // Patch diffuse+occlusion+specular+smoothness into three MRTs
                // See https://issuetracker.unity3d.com/issues/camera-slash-commandbuffer-commandbuffer-dot-blit-texture2d-dot-blacktexture-builtinrendertexturetype-dot-gbuffer3-fails-when-hdr-is-enabled
                // for why we do not use BuiltinRenderTextureType.GBuffer3 but the camera target instead (LookDev is always HDR)
                RenderTargetIdentifier[] mrt2 = { BuiltinRenderTextureType.GBuffer0, BuiltinRenderTextureType.GBuffer1, BuiltinRenderTextureType.GBuffer2, BuiltinRenderTextureType.CameraTarget };
                contextCB.m_drawBallCB.SetRenderTarget(mrt2, BuiltinRenderTextureType.CameraTarget);

                contextCB.m_drawBallPB.SetVector("_SHAr", shaderConstants[0]);
                contextCB.m_drawBallPB.SetVector("_SHAg", shaderConstants[1]);
                contextCB.m_drawBallPB.SetVector("_SHAb", shaderConstants[2]);
                contextCB.m_drawBallPB.SetVector("_SHBr", shaderConstants[3]);
                contextCB.m_drawBallPB.SetVector("_SHBg", shaderConstants[4]);
                contextCB.m_drawBallPB.SetVector("_SHBb", shaderConstants[5]);
                contextCB.m_drawBallPB.SetVector("_SHC", shaderConstants[6]);

                contextCB.m_drawBallPB.SetVector("_WindowsSize", new Vector4(previewUtility.camera.pixelWidth, previewUtility.camera.pixelHeight, secondView ? 1.0f : 0.0f, 0));
                contextCB.m_drawBallCB.DrawMesh(LookDevResources.m_ScreenQuadMesh, Matrix4x4.identity, LookDevResources.m_DrawBallsMaterial, 0, 0, contextCB.m_drawBallPB);

                // Draw ball in screen space by patching the GBuffer
                previewUtility.camera.AddCommandBuffer(CameraEvent.AfterGBuffer, contextCB.m_drawBallCB);
            }

            Vector3 oldAngles = Vector3.zero;
            Vector3 oldTranslation = Vector3.zero;

            if (currentObject != null)
            {
                // Setup object property
                LODGroup lodGroup = currentObject.GetComponent(typeof(LODGroup)) as LODGroup;
                if (lodGroup != null)
                {
                    lodGroup.ForceLOD(lookDevContext.lodIndex);
                }

                PreviewRenderUtility.SetEnabledRecursive(currentObject, true);
                oldAngles = currentObject.transform.eulerAngles;
                oldTranslation = currentObject.transform.localPosition;

                currentObject.transform.position = new Vector3(0.0f, kDefaultSceneHeight, 0.0f);
                currentObject.transform.rotation = Quaternion.identity;

                // Applies inverse transform from camera pivot + environment rotation.
                currentObject.transform.Rotate(0.0f, envRotation, 0.0f);
                currentObject.transform.Translate(-originalCameraState.pivot.value);
                currentObject.transform.Rotate(0.0f, m_CurrentObjRotationOffset, 0.0f);
            }

            // Specific drawing pass for TexturedWire
            if (shadingMode == DrawCameraMode.TexturedWire && !shadowPass)
            {
                Handles.ClearCamera(previewRect, previewUtility.camera);
                Handles.DrawCamera(previewRect, previewUtility.camera, shadingMode);
            }
            else
            {
                previewUtility.Render(true, false);
            }

            if (currentObject != null)
            {
                // restore initial transform
                currentObject.transform.eulerAngles = oldAngles;
                currentObject.transform.position = oldTranslation;
                PreviewRenderUtility.SetEnabledRecursive(currentObject, false);
            }

            if (needDebugMode && !shadowPass) // Even with the debug modes we need to keep the shadow mask white.
            {
                if (Event.current.type == EventType.Repaint)
                {
                    float scaleFac = previewUtility.GetScaleFactor(previewRect.width, previewRect.height);
                    LookDevResources.m_DeferredOverlayMaterial.SetInt("_DisplayMode", (int)shadingMode - (int)DrawCameraMode.DeferredDiffuse);
                    Graphics.DrawTexture(new Rect(0, 0, previewRect.width * scaleFac, previewRect.height * scaleFac), EditorGUIUtility.whiteTexture, LookDevResources.m_DeferredOverlayMaterial);
                }
            }

            if (shadowPass)
            {
                previewUtility.camera.RemoveCommandBuffer(CameraEvent.AfterGBuffer, contextCB.m_patchGBufferCB);
                if (m_LookDevConfig.showBalls)
                {
                    previewUtility.camera.RemoveCommandBuffer(CameraEvent.AfterLighting, contextCB.m_drawBallCB);
                }
            }
            else if (contextPass == PreviewContext.PreviewContextPass.kView && m_LookDevConfig.showBalls)
            {
                // Draw ball in screen space by patching the GBuffer
                previewUtility.camera.RemoveCommandBuffer(CameraEvent.AfterGBuffer, contextCB.m_drawBallCB);
            }

            QualitySettings.shadowCascade4Split = oldShadowCascade4Split;
            QualitySettings.shadowDistance = oldShadowDistance;

            // Restore saved render settings
            RenderSettings.defaultReflectionMode    = oldReflectionMode;
            RenderSettings.ambientMode              = oldAmbientMode;
            RenderSettings.customReflection         = oldCubeMap;
            RenderSettings.skybox                   = oldSkybox;
            RenderSettings.ambientIntensity         = oldAmbientIntensity;
            RenderSettings.reflectionIntensity      = oldReflectionIntensity;
            RenderSettings.ambientProbe             = oldAmbientProbe;

            return previewUtility.EndPreview();
        }

        public void UpdateLookDevModeToggle(LookDevMode lookDevMode, bool value)
        {
            LookDevMode newLookDevMode = lookDevMode;

            if (value)
            {
                m_LookDevModeToggles[(int)lookDevMode] = value;

                // Disable all others
                for (int i = 0; i < (int)LookDevMode.Count; ++i)
                {
                    if (i != (int)lookDevMode)
                    {
                        m_LookDevModeToggles[i] = false;
                    }
                }

                newLookDevMode = lookDevMode;
            }
            else
            {
                for (int i = 0; i < (int)LookDevMode.Count; ++i)
                {
                    if (m_LookDevModeToggles[i])
                    {
                        newLookDevMode = (LookDevMode)i;
                    }
                }

                // all are false, keep current mode
                m_LookDevModeToggles[(int)lookDevMode] = true;
                newLookDevMode = lookDevMode;
            }

            m_LookDevConfig.lookDevMode = newLookDevMode;
            Repaint();
        }

        void OnUndoRedo()
        {
            Repaint();
        }

        private void DoAdditionalGUI()
        {
            if (m_LookDevConfig.lookDevMode == LookDevMode.SideBySide)
            {
                // Camera link icon
                int linkIconSize = 32;
                GUILayout.BeginArea(new Rect((m_PreviewRects[2].width - linkIconSize) / 2, (m_PreviewRects[2].height - linkIconSize) / 2, linkIconSize, linkIconSize));
                {
                    EditorGUI.BeginChangeCheck();
                    bool isLink = m_LookDevConfig.sideBySideCameraLinked;
                    bool linked = GUILayout.Toggle(isLink, GetGUIContentLink(isLink), styles.sToolBarButton);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_LookDevConfig.sideBySideCameraLinked = linked;

                        // When re-linking camera we need to copy the current context into the other one
                        if (linked)
                        {
                            CameraState currentState = m_LookDevConfig.currentEditionContext == LookDevEditionContext.Left ? m_LookDevConfig.cameraStateLeft : m_LookDevConfig.cameraStateRight;
                            CameraState otherState = m_LookDevConfig.currentEditionContext == LookDevEditionContext.Left ? m_LookDevConfig.cameraStateRight : m_LookDevConfig.cameraStateLeft;

                            otherState.Copy(currentState);
                        }
                    }
                }
                GUILayout.EndArea();
            }
        }

        GUIStyle GetPropertyLabelStyle(LookDevProperty property)
        {
            if (m_LookDevConfig.IsPropertyLinked(property) || m_LookDevConfig.lookDevMode == LookDevMode.Single1 || m_LookDevConfig.lookDevMode == LookDevMode.Single2)
                return styles.sPropertyLabelStyle[2];
            else
                return styles.sPropertyLabelStyle[m_LookDevConfig.currentEditionContextIndex];
        }

        GUIContent GetGUIContentLink(bool active)
        {
            return active ? styles.sLinkActive : styles.sLinkInactive;
        }

        private void DoControlWindow()
        {
            if (!m_LookDevConfig.showControlWindows)
                return;

            float sliderLabelWidth = 68.0f;
            float sliderWidth = 150.0f;
            float fieldWidth = 30.0f;


            GUILayout.BeginArea(m_ControlWindowRect, styles.sBigTitleInnerStyle);
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.BeginHorizontal(GUILayout.Height(kLineHeight));
                    {
                        GUILayout.FlexibleSpace();

                        bool value = false;

                        EditorGUI.BeginChangeCheck();
                        value = GUILayout.Toggle(m_LookDevModeToggles[(int)LookDevMode.Single1], styles.sSingleMode1, styles.sToolBarButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            UpdateLookDevModeToggle(LookDevMode.Single1, value);
                            m_LookDevConfig.UpdateFocus(LookDevEditionContext.Left);
                            Repaint();
                        }

                        EditorGUI.BeginChangeCheck();
                        value = GUILayout.Toggle(m_LookDevModeToggles[(int)LookDevMode.Single2], styles.sSingleMode2, styles.sToolBarButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            UpdateLookDevModeToggle(LookDevMode.Single2, value);
                            m_LookDevConfig.UpdateFocus(LookDevEditionContext.Right);
                            Repaint();
                        }

                        EditorGUI.BeginChangeCheck();
                        value = GUILayout.Toggle(m_LookDevModeToggles[(int)LookDevMode.SideBySide], styles.sSideBySideMode, styles.sToolBarButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            UpdateLookDevModeToggle(LookDevMode.SideBySide, value);
                        }

                        EditorGUI.BeginChangeCheck();
                        value = GUILayout.Toggle(m_LookDevModeToggles[(int)LookDevMode.Split], styles.sSplitMode, styles.sToolBarButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            UpdateLookDevModeToggle(LookDevMode.Split, value);
                        }

                        EditorGUI.BeginChangeCheck();
                        value = GUILayout.Toggle(m_LookDevModeToggles[(int)LookDevMode.Zone], styles.sZoneMode, styles.sToolBarButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            UpdateLookDevModeToggle(LookDevMode.Zone, value);
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(GUILayout.Height(kLineHeight));
                    {
                        GUILayout.Label(LookDevViewsWindow.styles.sExposure, GetPropertyLabelStyle(LookDevProperty.ExposureValue), GUILayout.Width(sliderLabelWidth));

                        float fExposureValue = m_LookDevConfig.currentLookDevContext.exposureValue;
                        EditorGUI.BeginChangeCheck();
                        // Display in the float field is rounded for display. To 1 decimal in case of negative number to account for the '-' character.
                        float roundedExposureRange = Mathf.Round(m_LookDevConfig.exposureRange);
                        fExposureValue = Mathf.Clamp(GUILayout.HorizontalSlider((float)Math.Round(fExposureValue, fExposureValue < 0.0f ? 1 : 2), -roundedExposureRange, roundedExposureRange, GUILayout.Width(sliderWidth)), -roundedExposureRange, roundedExposureRange);
                        fExposureValue = Mathf.Clamp(EditorGUILayout.FloatField(fExposureValue, GUILayout.Width(fieldWidth)), -roundedExposureRange, roundedExposureRange);
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_LookDevConfig.UpdateFloatProperty(LookDevProperty.ExposureValue, fExposureValue);
                        }
                        bool linked = false;
                        EditorGUI.BeginChangeCheck();
                        bool isLink = m_LookDevConfig.IsPropertyLinked(LookDevProperty.ExposureValue);
                        linked = GUILayout.Toggle(isLink, GetGUIContentLink(isLink), styles.sToolBarButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_LookDevConfig.UpdatePropertyLink(LookDevProperty.ExposureValue, linked);
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(GUILayout.Height(kLineHeight));
                    {
                        using (new EditorGUI.DisabledScope(m_LookDevEnvLibrary.hdriList.Count <= 1))
                        {
                            GUILayout.Label(LookDevViewsWindow.styles.sEnvironment, GetPropertyLabelStyle(LookDevProperty.HDRI), GUILayout.Width(sliderLabelWidth));

                            int iHDRIIndex = -1;
                            if (m_LookDevEnvLibrary.hdriList.Count > 1)
                            {
                                int maxHDRIIndex = m_LookDevEnvLibrary.hdriList.Count - 1;
                                iHDRIIndex = m_LookDevConfig.currentLookDevContext.currentHDRIIndex;
                                EditorGUI.BeginChangeCheck();
                                iHDRIIndex = (int)GUILayout.HorizontalSlider(iHDRIIndex, 0.0f, (float)maxHDRIIndex, GUILayout.Width(sliderWidth));
                                iHDRIIndex = Mathf.Clamp(EditorGUILayout.IntField(iHDRIIndex, GUILayout.Width(fieldWidth)), 0, maxHDRIIndex);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    m_LookDevConfig.UpdateIntProperty(LookDevProperty.HDRI, iHDRIIndex);
                                }
                            }
                            else
                            {
                                GUILayout.HorizontalSlider(0.0f, 0.0f, 0.0f, GUILayout.Width(sliderWidth));
                                GUILayout.Label(LookDevViewsWindow.styles.sZero, EditorStyles.miniLabel, GUILayout.Width(fieldWidth));
                            }
                        }

                        bool linked = false;
                        EditorGUI.BeginChangeCheck();
                        bool isLink = m_LookDevConfig.IsPropertyLinked(LookDevProperty.HDRI);
                        linked = GUILayout.Toggle(isLink, GetGUIContentLink(isLink), styles.sToolBarButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_LookDevConfig.UpdatePropertyLink(LookDevProperty.HDRI, linked);
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal(GUILayout.Height(kLineHeight));
                    {
                        GUILayout.Label(LookDevViewsWindow.styles.sShadingMode, GetPropertyLabelStyle(LookDevProperty.ShadingMode), GUILayout.Width(sliderLabelWidth));

                        int shadingMode = m_LookDevConfig.currentLookDevContext.shadingMode;
                        EditorGUI.BeginChangeCheck();
                        shadingMode = EditorGUILayout.IntPopup("", shadingMode, LookDevViewsWindow.styles.sShadingModeStrings, LookDevViewsWindow.styles.sShadingModeValues, GUILayout.Width(fieldWidth + sliderWidth + 4.0f));
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_LookDevConfig.UpdateIntProperty(LookDevProperty.ShadingMode, shadingMode);
                        }

                        bool linked = false;
                        EditorGUI.BeginChangeCheck();
                        bool isLink = m_LookDevConfig.IsPropertyLinked(LookDevProperty.ShadingMode);
                        linked = GUILayout.Toggle(isLink, GetGUIContentLink(isLink), styles.sToolBarButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            m_LookDevConfig.UpdatePropertyLink(LookDevProperty.ShadingMode, linked);
                        }
                    }
                    GUILayout.EndHorizontal();

                    /*
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label(LookDevViewsWindow.styles.sRotation, GetPropertyLabelStyle(LookDevProperty.kEnvRotation), GUILayout.Width(sliderLabelWidth));

                        float envRotation = m_LookDevConfig.lookDevContexts[m_LookDevConfig.lookDevContextIndex].envRotation;
                        EditorGUI.BeginChangeCheck();
                        envRotation = GUILayout.HorizontalSlider(envRotation, 0.0f, 720.0f, GUILayout.Width(sliderWidth));
                        envRotation = Mathf.Clamp(EditorGUILayout.FloatField(float.Parse(envRotation.ToString("F0")), GUILayout.Width(fieldWidth)), 0.0f, 720.0f);
                        if (EditorGUI.EndChangeCheck())
                        {
                            UpdateFloatProperty(LookDevProperty.kEnvRotation, envRotation);
                        }
                        bool linked = false;
                        EditorGUI.BeginChangeCheck();
                        bool isLink = m_LookDevConfig.IsPropertyLinked(LookDevProperty.kEnvRotation);
                        linked = GUILayout.Toggle(isLink, GetGUIContentLink(isLink), styles.sToolBarButton);
                        if (EditorGUI.EndChangeCheck())
                        {
                            UpdatePropertyLink(LookDevProperty.kEnvRotation, linked);
                        }
                    }
                    GUILayout.EndHorizontal();
                    */
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndArea();
        }

        public void Update()
        {
            // Force refresh of the frame to make the object turn
            if (m_ObjRotationAcc > 0.0f || m_EnvRotationAcc > 0.0f)
            {
                // This is necessary to make the framerate normal for the editor window.
                Repaint();
            }
        }

        private void LoadRenderDoc()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                RenderDoc.Load();
                ShaderUtil.RecreateGfxDevice();
            }
        }

        private float ComputeLookDevEnvWindowWidth()
        {
            bool needVerticalScrollBar = (m_DisplayRect.height - 5.0f) < LookDevEnvironmentWindow.m_HDRIHeight * m_LookDevEnvLibrary.hdriCount;
            return LookDevEnvironmentWindow.m_HDRIWidth + (needVerticalScrollBar ? 19.0f : 5.0f);
        }

        private float ComputeLookDevEnvWindowHeight()
        {
            return m_DisplayRect.height;
        }

        private void DoToolbarGUI()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                Rect settingsRect = GUILayoutUtility.GetRect(LookDevSettingsWindow.styles.sTitle, EditorStyles.toolbarDropDown, GUILayout.Width(120));
                if (EditorGUI.DropdownButton(settingsRect, LookDevSettingsWindow.styles.sTitle, FocusType.Passive, EditorStyles.toolbarDropDown))
                {
                    Rect rect = GUILayoutUtility.topLevel.GetLast();
                    PopupWindow.Show(rect, new LookDevSettingsWindow(this));
                    GUIUtility.ExitGUI();
                }

                settingsRect = GUILayoutUtility.GetRect(LookDevViewsWindow.styles.sTitle, EditorStyles.toolbarDropDown, GUILayout.Width(120));
                if (EditorGUI.DropdownButton(settingsRect, LookDevViewsWindow.styles.sTitle, FocusType.Passive, EditorStyles.toolbarDropDown))
                {
                    Rect rect = GUILayoutUtility.topLevel.GetLast();
                    PopupWindow.Show(rect, new LookDevViewsWindow(this));
                    GUIUtility.ExitGUI();
                }

                m_LookDevConfig.enableShadowCubemap = GUILayout.Toggle(m_LookDevConfig.enableShadowCubemap, LookDevSettingsWindow.styles.sEnableShadowIcon, styles.sToolBarButton);
                m_LookDevConfig.rotateObjectMode = GUILayout.Toggle(m_LookDevConfig.rotateObjectMode, LookDevSettingsWindow.styles.sEnableObjRotationIcon, styles.sToolBarButton);
                m_LookDevConfig.rotateEnvMode = GUILayout.Toggle(m_LookDevConfig.rotateEnvMode, LookDevSettingsWindow.styles.sEnableEnvRotationIcon, styles.sToolBarButton);

                GUILayout.FlexibleSpace();

                if (m_ShowLookDevEnvWindow)
                {
                    if (GUILayout.Button(m_SyncLightVertical, EditorStyles.toolbarButton))
                    {
                        Undo.RecordObject(m_LookDevEnvLibrary, "Synchronize lights");

                        int currentHDRIIndex = m_LookDevConfig.currentLookDevContext.currentHDRIIndex;

                        for (int i = 0; i < m_LookDevEnvLibrary.hdriList.Count; ++i)
                        {
                            // Get offset to apply to cubemap to align (offset will be -360..360, so can be apply directly on cubemap angleOffset).
                            // code in PositionToLatLong / LatLongToPosition ensure that we are 0..360 for longitude.
                            m_LookDevEnvLibrary.hdriList[i].angleOffset += (m_LookDevEnvLibrary.hdriList[currentHDRIIndex].shadowInfo.longitude + m_LookDevEnvLibrary.hdriList[currentHDRIIndex].angleOffset) - (m_LookDevEnvLibrary.hdriList[i].shadowInfo.longitude + m_LookDevEnvLibrary.hdriList[i].angleOffset);
                            m_LookDevEnvLibrary.hdriList[i].angleOffset = (m_LookDevEnvLibrary.hdriList[i].angleOffset + 360.0f) % 360.0f;
                        }

                        m_LookDevEnvLibrary.dirty = true;

                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button(m_ResetEnvironment, EditorStyles.toolbarButton))
                    {
                        Undo.RecordObject(m_LookDevEnvLibrary, "Reset environment");

                        for (int i = 0; i < m_LookDevEnvLibrary.hdriList.Count; ++i)
                        {
                            m_LookDevEnvLibrary.hdriList[i].angleOffset = 0;
                        }

                        m_LookDevEnvLibrary.dirty = true;

                        GUIUtility.ExitGUI();
                    }
                }

                if (RenderDoc.IsLoaded())
                {
                    using (new EditorGUI.DisabledScope(!RenderDoc.IsSupported()))
                    {
                        if (GUILayout.Button(m_RenderdocContent, EditorStyles.toolbarButton))
                        {
                            m_CaptureRD = true;
                            GUIUtility.ExitGUI();
                        }
                    }
                }

                settingsRect = GUILayoutUtility.GetRect(LookDevEnvironmentWindow.styles.sTitle, EditorStyles.iconButton);
                if (EditorGUI.DropdownButton(settingsRect, LookDevEnvironmentWindow.styles.sTitle, FocusType.Passive, EditorStyles.iconButton))
                {
                    m_ShowLookDevEnvWindow = !m_ShowLookDevEnvWindow;
                }

                if (m_ShowLookDevEnvWindow)
                {
                    Rect rect = new Rect();
                    rect.x = 0;
                    rect.y = 0;
                    rect.width = ComputeLookDevEnvWindowWidth();
                    rect.height = ComputeLookDevEnvWindowHeight();

                    Rect lookDevEnvWindowPos = new Rect();
                    lookDevEnvWindowPos.x = m_DisplayRect.width - ComputeLookDevEnvWindowWidth();
                    lookDevEnvWindowPos.y = m_DisplayRect.y;
                    lookDevEnvWindowPos.width = ComputeLookDevEnvWindowWidth();
                    lookDevEnvWindowPos.height = ComputeLookDevEnvWindowHeight();

                    m_LookDevEnvWindow.SetRects(lookDevEnvWindowPos, rect, m_DisplayRect);
                    GUILayout.Window(0, lookDevEnvWindowPos, m_LookDevEnvWindow.OnGUI, "", styles.sBigTitleInnerStyle);
                }
            }
            GUILayout.EndHorizontal();
        }

        void UpdateContextWithCurrentHDRI(Cubemap cubemap)
        {
            bool recordUndo = cubemap != LookDevResources.m_DefaultHDRI;
            int iIndex = m_LookDevEnvLibrary.hdriList.FindIndex(x => x.cubemap == cubemap);
            if (iIndex == -1)
            {
                // if the HDRI is not in the list insert it (allow to handle both drag and drop from explorer and from ShadowCubemap
                m_LookDevEnvLibrary.InsertHDRI(cubemap);
                iIndex = m_LookDevEnvLibrary.hdriList.Count - 1;
            }

            m_LookDevConfig.UpdateIntProperty(LookDevProperty.HDRI, iIndex, recordUndo);
        }

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            if (RenderDoc.IsInstalled() && !RenderDoc.IsLoaded())
            {
                menu.AddItem(new GUIContent("Load RenderDoc"), false, LoadRenderDoc);
            }
        }

        public void ResetView()
        {
            Undo.RecordObject(m_LookDevConfig, "Reset View");
            ScriptableObject.DestroyImmediate(m_LookDevConfig);
            m_LookDevConfig = ScriptableObject.CreateInstance<LookDevConfig>();
            m_LookDevConfig.SetLookDevView(this);
            UpdateLookDevModeToggle(m_LookDevConfig.lookDevMode, true);
        }

        private void LoadLookDevConfig()
        {
            if (m_LookDevConfig == null)
            {
                var saveLoadHelper = new ScriptableObjectSaveLoadHelper<LookDevConfig>("asset", SaveType.Text);
                LookDevConfig config = saveLoadHelper.Load(m_configAssetPath);
                if (config == null)
                {
                    m_LookDevConfig = ScriptableObject.CreateInstance<LookDevConfig>();
                }
                else
                {
                    m_LookDevConfig = config;
                }
                m_IsSaveRegistered = false;
            }

            m_LookDevConfig.SetLookDevView(this);
            m_LookDevConfig.UpdateCurrentObjectArray();

            if (m_LookDevEnvLibrary == null)
            {
                if (m_LookDevUserEnvLibrary != null)
                {
                    m_LookDevEnvLibrary = ScriptableObject.Instantiate<LookDevEnvironmentLibrary>(m_LookDevUserEnvLibrary);
                }
                else
                {
                    envLibrary = null; // Will create and initialize properly the library
                }
            }

            m_LookDevEnvLibrary.SetLookDevView(this);
        }

        public void SaveLookDevConfig()
        {
            // Save view config
            var saveLoadHelperConfig = new ScriptableObjectSaveLoadHelper<LookDevConfig>("asset", SaveType.Text);
            if (m_LookDevConfig != null)
            {
                saveLoadHelperConfig.Save(m_LookDevConfig, m_configAssetPath);
            }
        }

        public bool SaveLookDevLibrary()
        {
            // Save current library
            if (m_LookDevUserEnvLibrary != null)
            {
                // Copy the current working library into the actual asset object and set it to dirty for saving.
                EditorUtility.CopySerialized(m_LookDevEnvLibrary, m_LookDevUserEnvLibrary);
                EditorUtility.SetDirty(m_LookDevEnvLibrary);
                return true;
            }
            else
            {
                string assetPath = EditorUtility.SaveFilePanelInProject("Save New Environment Library", "New Env Library", "asset", "");
                if (!string.IsNullOrEmpty(assetPath))
                {
                    CreateNewLibrary(assetPath);
                    return true;
                }
            }

            return false;
        }

        public void OnEnable()
        {
            InitializePreviewUtilities();

            m_FirstViewGizmoColor = EditorGUIUtility.isProSkin ? new Color32(0, 204, 204, 255) : new Color32(0, 127, 255, 255);
            m_SecondViewGizmoColor = EditorGUIUtility.isProSkin ? new Color32(255, 107, 33, 255) : new Color32(255, 127, 0, 255);

            LoadLookDevConfig();

            // Required as we want that our material force a refresh update in the Lookdev
            autoRepaintOnSceneChange = true;

            titleContent = GetLocalizedTitleContent();
            m_RenderdocContent = EditorGUIUtility.IconContent("renderdoc", "Capture|Capture the current view and open in RenderDoc");
            m_SyncLightVertical = EditorGUIUtility.IconContent("LookDevCenterLight", "Sync|Sync all light vertically with current light position in current selected HDRI");
            m_ResetEnvironment = EditorGUIUtility.IconContent("LookDevResetEnv", "Reset|Reset all environment");

            UpdateLookDevModeToggle(m_LookDevConfig.lookDevMode, true);

            m_LookDevConfig.cameraStateCommon.rotation.valueChanged.AddListener(Repaint);
            m_LookDevConfig.cameraStateCommon.pivot.valueChanged.AddListener(Repaint);
            m_LookDevConfig.cameraStateCommon.viewSize.valueChanged.AddListener(Repaint);

            m_LookDevConfig.cameraStateLeft.rotation.valueChanged.AddListener(Repaint);
            m_LookDevConfig.cameraStateLeft.pivot.valueChanged.AddListener(Repaint);
            m_LookDevConfig.cameraStateLeft.viewSize.valueChanged.AddListener(Repaint);

            m_LookDevConfig.cameraStateRight.rotation.valueChanged.AddListener(Repaint);
            m_LookDevConfig.cameraStateRight.pivot.valueChanged.AddListener(Repaint);
            m_LookDevConfig.cameraStateRight.viewSize.valueChanged.AddListener(Repaint);

            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.editorApplicationQuit += OnQuit;
            EditorApplication.update += EditorUpdate;
        }

        public void OnDisable()
        {
            SaveLookDevConfig();

            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.editorApplicationQuit -= OnQuit;
            EditorApplication.update -= EditorUpdate;
        }

        void OnQuit()
        {
            SaveLookDevConfig();
        }

        void DelayedSaveLookDevConfig()
        {
            if (!m_IsSaveRegistered)
            {
                m_IsSaveRegistered = true;
                EditorApplication.delayCall += DoDelayedSaveLookDevConfig;
            }
        }

        void DoDelayedSaveLookDevConfig()
        {
            m_IsSaveRegistered = false;
            SaveLookDevConfig();
        }

        private void RenderPreviewSingle()
        {
            int index = m_LookDevConfig.lookDevMode == LookDevMode.Single1 ? 0 : 1;

            UpdateRenderTexture(m_PreviewRects[2]);

            RenderScene(m_PreviewRects[2], m_LookDevConfig.lookDevContexts[index], m_PreviewUtilityContexts[index], m_LookDevConfig.currentObjectInstances[index], m_LookDevConfig.cameraState[index], false);
            RenderCompositing(m_PreviewRects[2], m_PreviewUtilityContexts[index], m_PreviewUtilityContexts[index], false);
        }

        private void RenderPreviewSideBySide()
        {
            UpdateRenderTexture(m_PreviewRects[2]);

            RenderScene(m_PreviewRects[0], m_LookDevConfig.lookDevContexts[0], m_PreviewUtilityContexts[0], m_LookDevConfig.currentObjectInstances[0], m_LookDevConfig.cameraState[0], false);
            RenderScene(m_PreviewRects[1], m_LookDevConfig.lookDevContexts[1], m_PreviewUtilityContexts[1], m_LookDevConfig.currentObjectInstances[1], m_LookDevConfig.cameraState[1], true);

            RenderCompositing(m_PreviewRects[2], m_PreviewUtilityContexts[0], m_PreviewUtilityContexts[1], true);
        }

        private void RenderPreviewDualView()
        {
            UpdateRenderTexture(m_PreviewRects[2]);

            RenderScene(m_PreviewRects[2], m_LookDevConfig.lookDevContexts[0], m_PreviewUtilityContexts[0], m_LookDevConfig.currentObjectInstances[0], m_LookDevConfig.cameraState[0], false);
            RenderScene(m_PreviewRects[2], m_LookDevConfig.lookDevContexts[1], m_PreviewUtilityContexts[1], m_LookDevConfig.currentObjectInstances[1], m_LookDevConfig.cameraState[1], false);

            RenderCompositing(m_PreviewRects[2], m_PreviewUtilityContexts[0], m_PreviewUtilityContexts[1], true);
        }

        void RenderCompositing(Rect previewRect, PreviewContext previewContext0, PreviewContext previewContext1, bool dualView)
        {
            if (m_FinalCompositionTexture.width < 1 || m_FinalCompositionTexture.height < 1)
                return;

            Vector4 gizmoPosition = new Vector4(m_LookDevConfig.gizmo.center.x, m_LookDevConfig.gizmo.center.y, 0.0f, 0.0f);
            Vector4 gizmoZoneCenter = new Vector4(m_LookDevConfig.gizmo.point2.x, m_LookDevConfig.gizmo.point2.y, 0.0f, 0.0f);
            Vector4 gizmoThickness = new Vector4(m_GizmoThickness, m_GizmoThicknessSelected, 0.0f, 0.0f);
            Vector4 gizmoCircleRadius = new Vector4(m_GizmoCircleRadius, m_GizmoCircleRadiusSelected, 0.0f, 0.0f);

            // When we render in single view, map the parameters on same context.
            int index0 = (m_LookDevConfig.lookDevMode == LookDevMode.Single2) ? 1 : 0;
            int index1 = (m_LookDevConfig.lookDevMode == LookDevMode.Single1) ? 0 : 1;

            float exposureValue0 = (DrawCameraMode)m_LookDevConfig.lookDevContexts[index0].shadingMode == DrawCameraMode.Normal || (DrawCameraMode)m_LookDevConfig.lookDevContexts[index0].shadingMode == DrawCameraMode.TexturedWire ? m_LookDevConfig.lookDevContexts[index0].exposureValue : 0.0f;
            float exposureValue1 = (DrawCameraMode)m_LookDevConfig.lookDevContexts[index1].shadingMode == DrawCameraMode.Normal || (DrawCameraMode)m_LookDevConfig.lookDevContexts[index1].shadingMode == DrawCameraMode.TexturedWire ? m_LookDevConfig.lookDevContexts[index1].exposureValue : 0.0f;

            float dragAndDropContext = m_CurrentDragContext == LookDevEditionContext.Left ? 1.0f : (m_CurrentDragContext == LookDevEditionContext.Right ? -1.0f : 0.0f);

            CubemapInfo envInfo0 = m_LookDevEnvLibrary.hdriList[m_LookDevConfig.lookDevContexts[index0].currentHDRIIndex];
            CubemapInfo envInfo1 = m_LookDevEnvLibrary.hdriList[m_LookDevConfig.lookDevContexts[index1].currentHDRIIndex];

            // Prepare shadow information
            float shadowMultiplier0 = envInfo0.shadowInfo.shadowIntensity;
            float shadowMultiplier1 = envInfo1.shadowInfo.shadowIntensity;
            Color shadowColor0 = envInfo0.shadowInfo.shadowColor;
            Color shadowColor1 = envInfo1.shadowInfo.shadowColor;

            Texture texNormal0 = previewContext0.m_PreviewResult[(int)PreviewContext.PreviewContextPass.kView];
            Texture texWithoutSun0 = previewContext0.m_PreviewResult[(int)PreviewContext.PreviewContextPass.kViewWithShadow];
            Texture texShadows0 = previewContext0.m_PreviewResult[(int)PreviewContext.PreviewContextPass.kShadow];

            Texture texNormal1 = previewContext1.m_PreviewResult[(int)PreviewContext.PreviewContextPass.kView];
            Texture texWithoutSun1 = previewContext1.m_PreviewResult[(int)PreviewContext.PreviewContextPass.kViewWithShadow];
            Texture texShadows1 = previewContext1.m_PreviewResult[(int)PreviewContext.PreviewContextPass.kShadow];

            Vector4 compositingParams = new Vector4(m_LookDevConfig.dualViewBlendFactor, exposureValue0, exposureValue1, m_LookDevConfig.currentEditionContext == LookDevEditionContext.Left ? 1.0f : -1.0f);
            Vector4 compositingParams2 = new Vector4(dragAndDropContext, m_LookDevConfig.enableToneMap ? 1.0f : -1.0f, shadowMultiplier0, shadowMultiplier1);

            // Those could be tweakable for the neutral tonemapper, but in the case of the LookDev we don't need that
            const float BlackIn = 0.02f;
            const float WhiteIn = 10.0f;
            const float BlackOut = 0.0f;
            const float WhiteOut = 10.0f;
            const float WhiteLevel = 5.3f;
            const float WhiteClip = 10.0f;
            const float DialUnits = 20.0f;
            const float HalfDialUnits = DialUnits * 0.5f;

            // converting from artist dial units to easy shader-lerps (0-1)
            Vector4 tonemapCoeff1 = new Vector4((BlackIn * DialUnits) + 1.0f, (BlackOut * HalfDialUnits) + 1.0f, (WhiteIn / DialUnits), (1.0f - (WhiteOut / DialUnits)));
            Vector4 tonemapCoeff2 = new Vector4(0.0f, 0.0f, WhiteLevel, WhiteClip / HalfDialUnits);

            RenderTexture oldActive = RenderTexture.active;
            RenderTexture.active = m_FinalCompositionTexture;
            LookDevResources.m_LookDevCompositing.SetTexture("_Tex0Normal", texNormal0);
            LookDevResources.m_LookDevCompositing.SetTexture("_Tex0WithoutSun", texWithoutSun0);
            LookDevResources.m_LookDevCompositing.SetTexture("_Tex0Shadows", texShadows0);
            LookDevResources.m_LookDevCompositing.SetColor("_ShadowColor0", shadowColor0);
            LookDevResources.m_LookDevCompositing.SetTexture("_Tex1Normal", texNormal1);
            LookDevResources.m_LookDevCompositing.SetTexture("_Tex1WithoutSun", texWithoutSun1);
            LookDevResources.m_LookDevCompositing.SetTexture("_Tex1Shadows", texShadows1);
            LookDevResources.m_LookDevCompositing.SetColor("_ShadowColor1", shadowColor1);
            LookDevResources.m_LookDevCompositing.SetVector("_CompositingParams", compositingParams);
            LookDevResources.m_LookDevCompositing.SetVector("_CompositingParams2", compositingParams2);
            LookDevResources.m_LookDevCompositing.SetColor("_FirstViewColor", m_FirstViewGizmoColor);
            LookDevResources.m_LookDevCompositing.SetColor("_SecondViewColor", m_SecondViewGizmoColor);
            LookDevResources.m_LookDevCompositing.SetVector("_GizmoPosition", gizmoPosition);
            LookDevResources.m_LookDevCompositing.SetVector("_GizmoZoneCenter", gizmoZoneCenter);
            LookDevResources.m_LookDevCompositing.SetVector("_GizmoSplitPlane", m_LookDevConfig.gizmo.plane);
            LookDevResources.m_LookDevCompositing.SetVector("_GizmoSplitPlaneOrtho", m_LookDevConfig.gizmo.planeOrtho);
            LookDevResources.m_LookDevCompositing.SetFloat("_GizmoLength", m_LookDevConfig.gizmo.length);
            LookDevResources.m_LookDevCompositing.SetVector("_GizmoThickness", gizmoThickness);
            LookDevResources.m_LookDevCompositing.SetVector("_GizmoCircleRadius", gizmoCircleRadius);
            LookDevResources.m_LookDevCompositing.SetFloat("_BlendFactorCircleRadius", m_BlendFactorCircleRadius);
            LookDevResources.m_LookDevCompositing.SetFloat("_GetBlendFactorMaxGizmoDistance", GetBlendFactorMaxGizmoDistance());
            LookDevResources.m_LookDevCompositing.SetFloat("_GizmoRenderMode", m_ForceGizmoRenderSelector ? (float)LookDevOperationType.GizmoAll : (float)m_GizmoRenderMode);
            LookDevResources.m_LookDevCompositing.SetVector("_ScreenRatio", m_ScreenRatio);
            LookDevResources.m_LookDevCompositing.SetVector("_ToneMapCoeffs1", tonemapCoeff1);
            LookDevResources.m_LookDevCompositing.SetVector("_ToneMapCoeffs2", tonemapCoeff2);
            LookDevResources.m_LookDevCompositing.SetPass((int)m_LookDevConfig.lookDevMode);

            DrawFullScreenQuad(new Rect(0, 0, previewRect.width, previewRect.height));
            RenderTexture.active = oldActive;

            GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            GUI.DrawTexture(previewRect, m_FinalCompositionTexture, ScaleMode.StretchToFill, false);
            GL.sRGBWrite = false;
        }

        void EditorUpdate()
        {
            for (var i = 0; i < 2; ++i)
            {
                for (var j = 0; j < m_LookDevConfig.previewObjects[i].Length; j++)
                {
                    var currentObject = m_LookDevConfig.previewObjects[i][j];
                    if (currentObject == null)
                        continue;

                    EditorUtility.InitInstantiatedPreviewRecursive(currentObject); // hide objects in hierarchy
                    LookDevConfig.DisableRendererProperties(currentObject);
                    PreviewRenderUtility.SetEnabledRecursive(currentObject, false);
                }
            }
        }

        private void RenderPreview()
        {
            if (Event.current.type == EventType.Repaint)
            {
                if (m_LookDevConfig.rotateObjectMode)
                    m_ObjRotationAcc = Math.Min(m_ObjRotationAcc + Time.deltaTime * 0.5f, 1.0f);
                else
                    // Do brutal stop because weoften want to stop at a particular position
                    m_ObjRotationAcc = 0.0f; // Math.Max(m_ObjRotationAcc - Time.deltaTime * 0.5f, 0.0f);

                if (m_LookDevConfig.rotateEnvMode)
                    m_EnvRotationAcc = Math.Min(m_EnvRotationAcc + Time.deltaTime * 0.5f, 1.0f);
                else
                    // Do brutal stop because weoften want to stop at a particular position
                    m_EnvRotationAcc = 0.0f; // Math.Max(m_EnvRotationAcc - Time.deltaTime * 0.5f, 0.0f);

                // Handle objects/env rotation
                // speed control (in degree) - Time.deltaTime is in seconds
                m_CurrentObjRotationOffset = (m_CurrentObjRotationOffset + Time.deltaTime * 360.0f * 0.3f * m_LookDevConfig.objRotationSpeed * m_ObjRotationAcc) % 360.0f;
                m_LookDevConfig.lookDevContexts[0].envRotation = (m_LookDevConfig.lookDevContexts[0].envRotation + Time.deltaTime * 360.0f * 0.03f * m_LookDevConfig.envRotationSpeed * m_EnvRotationAcc) % 720.0f; // 720 to match GUI
                m_LookDevConfig.lookDevContexts[1].envRotation = (m_LookDevConfig.lookDevContexts[1].envRotation + Time.deltaTime * 360.0f * 0.03f * m_LookDevConfig.envRotationSpeed * m_EnvRotationAcc) % 720.0f; // 720 to match GUI

                switch (m_LookDevConfig.lookDevMode)
                {
                    case LookDevMode.Single1:
                    case LookDevMode.Single2:
                        RenderPreviewSingle();
                        break;
                    case LookDevMode.SideBySide:
                        RenderPreviewSideBySide();
                        break;
                    case LookDevMode.Zone:
                    case LookDevMode.Split:
                        RenderPreviewDualView();
                        break;
                }
            }
        }

        private void DoGizmoDebug()
        {
            if (m_DisplayDebugGizmo)
            {
                int lineCount = 7;
                float controlWindowWith = 150;
                float controlWindowHeight = kLineHeight * lineCount;
                float fieldWidth = 60.0f;
                float labelWidth = 90.0f;

                GUILayout.BeginArea(new Rect(position.width - controlWindowWith - 10, position.height - controlWindowHeight - 10, controlWindowWith, controlWindowHeight), styles.sBigTitleInnerStyle);
                {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("Thickness", EditorStyles.miniLabel, GUILayout.Width(labelWidth));
                            m_GizmoThickness = Mathf.Clamp(EditorGUILayout.FloatField(m_GizmoThickness, GUILayout.Width(fieldWidth)), 0.0f, 1.0f);
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("ThicknessSelected", EditorStyles.miniLabel, GUILayout.Width(labelWidth));
                            m_GizmoThicknessSelected = Mathf.Clamp(EditorGUILayout.FloatField(m_GizmoThicknessSelected, GUILayout.Width(fieldWidth)), 0.0f, 1.0f);
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("Radius", EditorStyles.miniLabel, GUILayout.Width(labelWidth));
                            m_GizmoCircleRadius = Mathf.Clamp(EditorGUILayout.FloatField(m_GizmoCircleRadius, GUILayout.Width(fieldWidth)), 0.0f, 1.0f);
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("RadiusSelected", EditorStyles.miniLabel, GUILayout.Width(labelWidth));
                            m_GizmoCircleRadiusSelected = Mathf.Clamp(EditorGUILayout.FloatField(m_GizmoCircleRadiusSelected, GUILayout.Width(fieldWidth)), 0.0f, 1.0f);
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("BlendRadius", EditorStyles.miniLabel, GUILayout.Width(labelWidth));
                            m_BlendFactorCircleRadius = Mathf.Clamp(EditorGUILayout.FloatField(m_BlendFactorCircleRadius, GUILayout.Width(fieldWidth)), 0.0f, 1.0f);
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("Selected", EditorStyles.miniLabel, GUILayout.Width(labelWidth));
                            m_ForceGizmoRenderSelector = GUILayout.Toggle(m_ForceGizmoRenderSelector, "");
                        }
                        GUILayout.EndHorizontal();

                        if (GUILayout.Button("Reset Gizmo"))
                        {
                            m_LookDevConfig.gizmo.Update(new Vector2(0.0f, 0.0f), 0.2f, 0.0f);
                        }
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndArea();
            }
        }

        private void UpdateViewSpecific()
        {
            UpdatePreviewRects(m_DisplayRect);

            // The reference scale is here to give the gizmo a fix scale regardless of LookDev window size.
            // 1080 is arbitrary but it may be good to tie it somehow to the desktop resolution? (Should not be a problem in practice as the LookDev is an edition tool that should be used with enough screen real estate anyway)
            m_ScreenRatio.Set(m_PreviewRects[2].width / kReferenceScale, m_PreviewRects[2].height / kReferenceScale, m_PreviewRects[2].width, m_PreviewRects[2].height);

            int lineCount = 4; // Title - Exposure - Environment - Rotation
            float controlWindowWith = 292.0f;
            float controlWindowHeight = kLineHeight * lineCount + EditorGUIUtility.standardVerticalSpacing;

            m_ControlWindowRect = new Rect(m_PreviewRects[2].width / 2 - controlWindowWith / 2, m_PreviewRects[2].height - controlWindowHeight - 10, controlWindowWith, controlWindowHeight);
        }

        private void UpdatePreviewRects(Rect previewRect)
        {
            m_PreviewRects[2] = new Rect(previewRect);
            if (m_ShowLookDevEnvWindow)
            {
                m_PreviewRects[2].width = m_PreviewRects[2].width - ComputeLookDevEnvWindowWidth();
            }
            m_PreviewRects[(int)LookDevEditionContext.Left] = new Rect(m_PreviewRects[2].x, m_PreviewRects[2].y, m_PreviewRects[2].width / 2, m_PreviewRects[2].height);
            m_PreviewRects[(int)LookDevEditionContext.Right] = new Rect(m_PreviewRects[2].width / 2, m_PreviewRects[2].y, m_PreviewRects[2].width / 2, m_PreviewRects[2].height);
        }

        private void HandleCamera()
        {
            if (m_LookDevOperationType == LookDevOperationType.None && !m_ControlWindowRect.Contains(Event.current.mousePosition))
            {
                int currentContextIndex = m_LookDevConfig.currentEditionContextIndex;
                int otherContextIndex = (currentContextIndex + 1) % 2;

                // Camera controller updates Camera States according to inputs
                m_CameraController.Update(m_LookDevConfig.cameraState[currentContextIndex], m_PreviewUtilityContexts[m_LookDevConfig.currentEditionContextIndex].m_PreviewUtility[0].camera); // We can use m_PreviewUtility[0] because all of them should be always synchronized anyway

                // If single or side by side mode and camera are linked we need to update both cameras
                if ((m_LookDevConfig.lookDevMode == LookDevMode.Single1 || m_LookDevConfig.lookDevMode == LookDevMode.Single2 || m_LookDevConfig.lookDevMode == LookDevMode.SideBySide) && m_LookDevConfig.sideBySideCameraLinked)
                {
                    m_LookDevConfig.cameraState[otherContextIndex].Copy(m_LookDevConfig.cameraState[currentContextIndex]);
                }

                if (m_CameraController.currentViewTool == ViewTool.None)
                {
                    if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.F)
                    {
                        if (!EditorGUIUtility.editingTextField)
                        {
                            Frame(m_LookDevConfig.currentEditionContext, true);
                            Event.current.Use();
                        }
                    }
                }

                // Update the actual Cameras with CameraState infos
                for (int i = 0; i < (int)PreviewContext.PreviewContextPass.kCount; ++i)
                {
                    m_LookDevConfig.cameraState[0].UpdateCamera(m_PreviewUtilityContexts[0].m_PreviewUtility[i].camera);
                    m_LookDevConfig.cameraState[1].UpdateCamera(m_PreviewUtilityContexts[1].m_PreviewUtility[i].camera);
                }

                m_LookDevConfig.cameraStateLeft.Copy(m_LookDevConfig.cameraState[0]);
                m_LookDevConfig.cameraStateRight.Copy(m_LookDevConfig.cameraState[1]);

                DelayedSaveLookDevConfig();
            }
        }

        public void HandleKeyboardShortcut()
        {
            // Only query camera when no in Layout event
            if (Event.current.type == EventType.Layout || EditorGUIUtility.editingTextField)
                return;

            if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.RightArrow)
            {
                m_LookDevConfig.UpdateIntProperty(LookDevProperty.HDRI, Math.Min(m_LookDevConfig.currentLookDevContext.currentHDRIIndex + 1, m_LookDevEnvLibrary.hdriList.Count - 1));
                Event.current.Use();
            }
            else if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.LeftArrow)
            {
                m_LookDevConfig.UpdateIntProperty(LookDevProperty.HDRI, Math.Max(m_LookDevConfig.currentLookDevContext.currentHDRIIndex - 1, 0));
                Event.current.Use();
            }

            if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.R)
            {
                m_LookDevConfig.ResynchronizeObjects();
                Event.current.Use();
            }
        }

        public void Frame()
        {
            Frame(true);
        }

        public void Frame(bool animate)
        {
            Frame(LookDevEditionContext.Left, animate);
            Frame(LookDevEditionContext.Right, animate);
        }

        private void Frame(LookDevEditionContext context, bool animate)
        {
            GameObject currentObject = m_LookDevConfig.currentObjectInstances[(int)context][0];
            if (currentObject != null)
            {
                Bounds bounds = new Bounds(currentObject.transform.position, Vector3.zero);
                GetRenderableBoundsRecurse(ref bounds, currentObject);

                float newSize = bounds.extents.magnitude * 1.5f;
                if (newSize == 0)
                    newSize = 10;

                CameraState cameraState = m_LookDevConfig.cameraState[(int)context];
                if (animate)
                {
                    cameraState.pivot.target = bounds.center;
                    cameraState.viewSize.target = Mathf.Abs(newSize * 2.2f);
                }
                else
                {
                    cameraState.pivot.value = bounds.center;
                    cameraState.viewSize.value = Mathf.Abs(newSize * 2.2f);
                }
            }
            m_CurrentObjRotationOffset = 0.0f;
        }

        private void HandleDragging()
        {
            Event evt = Event.current;

            switch (evt.type)
            {
                case EventType.DragPerform:
                    bool hasAddedRenderableObject = false;
                    if (m_PreviewRects[2].Contains(evt.mousePosition))
                    {
                        foreach (UnityEngine.Object o in DragAndDrop.objectReferences)
                        {
                            Cubemap cubemap = o as Cubemap;
                            if (cubemap)
                            {
                                UpdateFocus(Event.current.mousePosition);
                                UpdateContextWithCurrentHDRI(cubemap);
                            }

                            // If we have a skybox material, then try to get the cubemap texture.
                            Material material = o as Material;
                            if (material && material.shader.name.Contains("Skybox/Cubemap"))
                            {
                                Cubemap cubemap2 = material.GetTexture("_Tex") as Cubemap;
                                if (cubemap2)
                                {
                                    UpdateFocus(Event.current.mousePosition);
                                    UpdateContextWithCurrentHDRI(cubemap2);
                                }
                            }

                            GameObject go = o as GameObject;
                            if (go)
                            {
                                if (!hasAddedRenderableObject && GameObjectInspector.HasRenderableParts(go))
                                {
                                    UpdateFocus(Event.current.mousePosition);
                                    Undo.RecordObject(m_LookDevConfig, "Set current preview object");

                                    // Set current window
                                    bool bothViewUpdated = m_LookDevConfig.SetCurrentPreviewObject(go);
                                    DelayedSaveLookDevConfig();

                                    Frame(m_LookDevConfig.currentEditionContext, false);
                                    // Frame the other view if required
                                    if (bothViewUpdated)
                                    {
                                        Frame(m_LookDevConfig.currentEditionContext == LookDevEditionContext.Left ? LookDevEditionContext.Right : LookDevEditionContext.Left, false);
                                    }
                                    hasAddedRenderableObject = true;
                                }
                            }

                            LookDevEnvironmentLibrary lib = o as LookDevEnvironmentLibrary;
                            if (lib)
                            {
                                envLibrary = lib;
                            }
                        }
                    }
                    DragAndDrop.AcceptDrag();
                    m_CurrentDragContext = LookDevEditionContext.None;
                    m_LookDevEnvWindow.CancelSelection();
                    evt.Use();
                    break;
                case EventType.DragUpdated:
                {
                    bool canDrag = false;
                    foreach (UnityEngine.Object o in DragAndDrop.objectReferences)
                    {
                        Cubemap cubemap = o as Cubemap;
                        if (cubemap)
                        {
                            canDrag = true;
                        }

                        // If we have a skybox material, then try to get the cubemap texture.
                        Material material = o as Material;
                        if (material && material.shader.name.Contains("Skybox/Cubemap"))
                        {
                            canDrag = true;
                        }

                        GameObject go = o as GameObject;
                        if (go && EditorUtility.IsPersistent(go) && PrefabUtility.GetPrefabObject(go) != null)
                        {
                            if (GameObjectInspector.HasRenderableParts(go))
                            {
                                canDrag = true;
                            }
                        }

                        LookDevEnvironmentLibrary lib = o as LookDevEnvironmentLibrary;
                        if (lib)
                        {
                            canDrag = true;
                        }
                    }
                    DragAndDrop.visualMode = canDrag ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
                    m_CurrentDragContext = GetEditionContext(Event.current.mousePosition);

                    evt.Use();
                }
                break;
                case EventType.DragExited:
                    m_CurrentDragContext = LookDevEditionContext.None;
                    break;
                case EventType.Repaint:
                    break;
            }
        }

        private Vector2 GetNormalizedCoordinates(Vector2 mousePosition, Rect previewRect)
        {
            Vector2 normalizedCoord = new Vector3((mousePosition.x - previewRect.x) / previewRect.width, (mousePosition.y - previewRect.y) / previewRect.height);
            normalizedCoord.x = (normalizedCoord.x * 2.0f - 1.0f) * m_ScreenRatio.x;
            normalizedCoord.y = -(normalizedCoord.y * 2.0f - 1.0f) * m_ScreenRatio.y;

            return normalizedCoord;
        }

        LookDevEditionContext GetEditionContext(Vector2 position)
        {
            if (!m_PreviewRects[2].Contains(position)) // This may happen if the library is open
            {
                return LookDevEditionContext.None;
            }

            LookDevEditionContext context;
            switch (m_LookDevConfig.lookDevMode)
            {
                case LookDevMode.Single1:
                {
                    context = LookDevEditionContext.Left;
                    break;
                }
                case LookDevMode.Single2:
                {
                    context = LookDevEditionContext.Right;
                    break;
                }
                case LookDevMode.SideBySide:
                {
                    if (m_PreviewRects[(int)LookDevEditionContext.Left].Contains(position))
                        context = LookDevEditionContext.Left;
                    else
                        context = LookDevEditionContext.Right;
                    break;
                }
                case LookDevMode.Split:
                {
                    Vector2 normalizedCoord = GetNormalizedCoordinates(position, m_PreviewRects[2]);

                    if (Vector3.Dot(new Vector3(normalizedCoord.x, normalizedCoord.y, 1.0f), m_LookDevConfig.gizmo.plane) > 0.0f)
                        context = LookDevEditionContext.Left;
                    else
                        context = LookDevEditionContext.Right;
                    break;
                }
                case LookDevMode.Zone:
                {
                    Vector2 normalizedCoord = GetNormalizedCoordinates(position, m_PreviewRects[2]);

                    if ((Vector2.Distance(normalizedCoord, m_LookDevConfig.gizmo.point2) - m_LookDevConfig.gizmo.length * 2.0f) > 0.0f)
                        context = LookDevEditionContext.Left;
                    else
                        context = LookDevEditionContext.Right;
                    break;
                }
                default:
                    context = LookDevEditionContext.Left;
                    break;
            }

            return context;
        }

        public void UpdateFocus(Vector2 position)
        {
            m_LookDevConfig.UpdateFocus(GetEditionContext(position));
        }

        private LookDevOperationType GetGizmoZoneOperation(Vector2 mousePosition, Rect previewRect)
        {
            Vector2 normalizedCoord = GetNormalizedCoordinates(mousePosition, previewRect);
            Vector3 normalizedCoordZ1 = new Vector3(normalizedCoord.x, normalizedCoord.y, 1.0f);

            float distanceToPlane = Vector3.Dot(normalizedCoordZ1, m_LookDevConfig.gizmo.plane);
            float absDistanceToPlane = Mathf.Abs(distanceToPlane);
            float distanceFromCenter = Vector2.Distance(normalizedCoord, m_LookDevConfig.gizmo.center);
            float distanceToOrtho = Vector3.Dot(normalizedCoordZ1, m_LookDevConfig.gizmo.planeOrtho);
            float side = (distanceToOrtho > 0.0f) ? 1.0f : -1.0f;
            Vector2 orthoPlaneNormal = new Vector2(m_LookDevConfig.gizmo.planeOrtho.x, m_LookDevConfig.gizmo.planeOrtho.y);

            LookDevOperationType result = LookDevOperationType.None;
            if (absDistanceToPlane < m_GizmoCircleRadiusSelected && (distanceFromCenter < (m_LookDevConfig.gizmo.length + m_GizmoCircleRadiusSelected)))
            {
                if (absDistanceToPlane < m_GizmoThicknessSelected)
                {
                    result = LookDevOperationType.GizmoTranslation;
                }

                Vector2 circleCenter = m_LookDevConfig.gizmo.center + side * orthoPlaneNormal * m_LookDevConfig.gizmo.length;
                float d = Vector2.Distance(normalizedCoord, circleCenter);
                if (d <= m_GizmoCircleRadiusSelected)
                {
                    result = side > 0.0f ? LookDevOperationType.GizmoRotationZone1 : LookDevOperationType.GizmoRotationZone2;
                }

                float maxBlendCircleDistanceToCenter = GetBlendFactorMaxGizmoDistance();
                float minBlendCircleDistanceToCenter = GetBlendFactorMaxGizmoDistance() + m_BlendFactorCircleRadius - m_BlendFactorCircleSelectionRadius;

                float blendCircleDistanceToCenter = m_LookDevConfig.dualViewBlendFactor * GetBlendFactorMaxGizmoDistance();
                Vector2 blendCircleCenter = m_LookDevConfig.gizmo.center - orthoPlaneNormal * blendCircleDistanceToCenter;
                float blendCircleSelectionRadius = Mathf.Lerp(m_BlendFactorCircleRadius, m_BlendFactorCircleSelectionRadius, Mathf.Clamp((maxBlendCircleDistanceToCenter - Mathf.Abs(blendCircleDistanceToCenter)) / (maxBlendCircleDistanceToCenter - minBlendCircleDistanceToCenter), 0.0f, 1.0f));
                if ((normalizedCoord - blendCircleCenter).magnitude < blendCircleSelectionRadius)
                {
                    result = LookDevOperationType.BlendFactor;
                }
            }

            return result;
        }

        bool IsOperatingGizmo()
        {
            return m_LookDevOperationType == LookDevOperationType.BlendFactor ||
                m_LookDevOperationType == LookDevOperationType.GizmoRotationZone1 ||
                m_LookDevOperationType == LookDevOperationType.GizmoRotationZone2 ||
                m_LookDevOperationType == LookDevOperationType.GizmoTranslation;
        }

        private void HandleMouseInput()
        {
            Event evt = Event.current;

            m_hotControlID = GUIUtility.GetControlID(FocusType.Passive);

            switch (evt.GetTypeForControl(m_hotControlID))
            {
                case EventType.MouseDown:
                {
                    if ((m_LookDevConfig.lookDevMode == LookDevMode.Split || m_LookDevConfig.lookDevMode == LookDevMode.Zone) && evt.button == 0)
                    {
                        m_LookDevOperationType = GetGizmoZoneOperation(Event.current.mousePosition, m_PreviewRects[2]);
                        m_OnMouseDownOffsetToGizmo = GetNormalizedCoordinates(Event.current.mousePosition, m_PreviewRects[2]) - m_LookDevConfig.gizmo.center;
                    }

                    // Gizmo manipulation takes precedence over other types of operations
                    if (m_LookDevOperationType == LookDevOperationType.None)
                    {
                        if (evt.shift && evt.button == 0)
                        {
                            m_LookDevOperationType = LookDevOperationType.RotateLight;
                        }
                        else if (evt.control && evt.button == 0)
                        {
                            m_LookDevOperationType = LookDevOperationType.RotateEnvironment;
                        }
                    }

                    if (!IsOperatingGizmo() && !m_ControlWindowRect.Contains(Event.current.mousePosition))
                    {
                        UpdateFocus(Event.current.mousePosition);
                    }

                    GUIUtility.hotControl = m_hotControlID;
                    break;
                }
                case EventType.MouseMove:
                {
                    m_GizmoRenderMode = GetGizmoZoneOperation(Event.current.mousePosition, m_PreviewRects[2]);
                    Repaint();
                    break;
                }
                case EventType.MouseUp:
                {
                    // Snap back to 0 when close to the center
                    if (m_LookDevOperationType == LookDevOperationType.BlendFactor)
                    {
                        if (Mathf.Abs(m_LookDevConfig.dualViewBlendFactor) < m_GizmoCircleRadiusSelected / (m_LookDevConfig.gizmo.length - m_GizmoCircleRadius))
                        {
                            m_LookDevConfig.dualViewBlendFactor = 0.0f;
                        }
                    }

                    m_LookDevOperationType = LookDevOperationType.None;

                    if (m_LookDevEnvWindow != null)
                    {
                        Cubemap currentEnvSelection = m_LookDevEnvWindow.GetCurrentSelection();
                        if (currentEnvSelection != null)
                        {
                            UpdateFocus(Event.current.mousePosition);
                            UpdateContextWithCurrentHDRI(currentEnvSelection);
                            m_LookDevEnvWindow.CancelSelection();
                            m_CurrentDragContext = LookDevEditionContext.None;
                            Repaint();
                        }
                    }

                    GUIUtility.hotControl = 0;

                    break;
                }
                case EventType.MouseDrag:
                {
                    if (m_LookDevOperationType == LookDevOperationType.RotateEnvironment)
                    {
                        float currentRotation = m_LookDevConfig.currentLookDevContext.envRotation;
                        currentRotation = (currentRotation + evt.delta.x / Mathf.Min(position.width, position.height) * 140.0f + 720.0f) % 720.0f; // 720 to match GUI in main control panel
                        m_LookDevConfig.UpdateFloatProperty(LookDevProperty.EnvRotation, currentRotation);
                        Event.current.Use();
                    }
                    else if (m_LookDevOperationType == LookDevOperationType.RotateLight && m_LookDevConfig.enableShadowCubemap)
                    {
                        ShadowInfo shadowInfo = m_LookDevEnvLibrary.hdriList[m_LookDevConfig.currentLookDevContext.currentHDRIIndex].shadowInfo;
                        shadowInfo.latitude = shadowInfo.latitude - (evt.delta.y * 0.6f);
                        shadowInfo.longitude = shadowInfo.longitude - (evt.delta.x * 0.6f);

                        Repaint();
                    }
                    break;
                }
            }

            // Mouse Up outside the window: we need to cancel the selection
            if (Event.current.rawType == EventType.MouseUp)
            {
                if (m_LookDevEnvWindow.GetCurrentSelection() != null)
                {
                    m_LookDevEnvWindow.CancelSelection();
                }
            }

            if (m_LookDevOperationType == LookDevOperationType.GizmoTranslation)
            {
                Vector2 newPosition = GetNormalizedCoordinates(Event.current.mousePosition, m_PreviewRects[2]) - m_OnMouseDownOffsetToGizmo;

                Vector2 minXY = GetNormalizedCoordinates(new Vector2(m_DisplayRect.x, m_PreviewRects[2].y + m_DisplayRect.height), m_PreviewRects[2]);
                Vector2 maxXY = GetNormalizedCoordinates(new Vector2(m_DisplayRect.x + m_DisplayRect.width, m_PreviewRects[2].y), m_PreviewRects[2]);

                // We clamp the center of the gizmo to the border of the screen in order to avoid being able to put it out of the screen.
                // The safe band is here to ensure that you always see at least part of the gizmo in order to be able to grab it again.
                float fSafeBand = 0.05f;
                newPosition.x = Mathf.Clamp(newPosition.x, minXY.x + fSafeBand, maxXY.x - fSafeBand);
                newPosition.y = Mathf.Clamp(newPosition.y, minXY.y + fSafeBand, maxXY.y - fSafeBand);

                m_LookDevConfig.gizmo.Update(newPosition, m_LookDevConfig.gizmo.length, m_LookDevConfig.gizmo.angle);
                Repaint();
            }

            if (m_LookDevOperationType == LookDevOperationType.GizmoRotationZone1 || m_LookDevOperationType == LookDevOperationType.GizmoRotationZone2)
            {
                Vector2 normalizedCoord = GetNormalizedCoordinates(Event.current.mousePosition, m_PreviewRects[2]);
                Vector2 basePoint, newPoint;
                float angleSnapping = Mathf.Deg2Rad * 45.0f * 0.5f;

                if (m_LookDevOperationType == LookDevOperationType.GizmoRotationZone1)
                {
                    newPoint = normalizedCoord;
                    basePoint = m_LookDevConfig.gizmo.point2;
                }
                else
                {
                    newPoint = normalizedCoord;
                    basePoint = m_LookDevConfig.gizmo.point1;
                }

                // Clamp the gizmo size to the smaller window dimension to avoid an out of window unresizable gizmo
                float gizmoLength = (basePoint - newPoint).magnitude;
                float minWindowSize = Mathf.Min(position.width, position.height);
                float smallestNormalizedWindowSize = minWindowSize / kReferenceScale * 2.0f * 0.9f;// Mathf.Min(position.width / minWindowSize, position.height / minWindowSize) * 2.0f * 0.9f;
                if (gizmoLength > smallestNormalizedWindowSize)
                {
                    Vector2 direction = newPoint - basePoint;
                    direction.Normalize();
                    newPoint = basePoint + direction * smallestNormalizedWindowSize;
                }

                // Snap to a multiple of "angleSnapping"
                if (Event.current.shift)
                {
                    Vector3 verticalPlane = new Vector3(-1.0f, 0.0f, basePoint.x);
                    float side = Vector3.Dot(new Vector3(normalizedCoord.x, normalizedCoord.y, 1.0f), verticalPlane);

                    float angle = Mathf.Deg2Rad * Vector2.Angle(new Vector2(0.0f, 1.0f), normalizedCoord - basePoint);
                    if (side > 0.0f)
                        angle = 2.0f * Mathf.PI - angle;
                    angle = (int)(angle / angleSnapping) * angleSnapping;
                    Vector2 dir = normalizedCoord - basePoint;
                    float length = dir.magnitude; // we want to keep the length of the gizmo where it should be given the mouse position
                    newPoint = basePoint + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * length;
                }

                if (m_LookDevOperationType == LookDevOperationType.GizmoRotationZone1)
                {
                    m_LookDevConfig.gizmo.Update(newPoint, basePoint);
                }
                else
                {
                    m_LookDevConfig.gizmo.Update(basePoint, newPoint);
                }

                Repaint();
            }

            if (m_LookDevOperationType == LookDevOperationType.BlendFactor)
            {
                Vector2 mousePosition = GetNormalizedCoordinates(Event.current.mousePosition, m_PreviewRects[2]);
                float distanceToOrthoPlane = -Vector3.Dot(new Vector3(mousePosition.x, mousePosition.y, 1.0f), m_LookDevConfig.gizmo.planeOrtho) / GetBlendFactorMaxGizmoDistance();
                m_LookDevConfig.dualViewBlendFactor = Mathf.Clamp(distanceToOrthoPlane, -1.0f, 1.0f);
                Repaint();
            }
        }

        private float GetBlendFactorMaxGizmoDistance()
        {
            return m_LookDevConfig.gizmo.length - m_GizmoCircleRadius - m_BlendFactorCircleRadius;
        }

        void CleanupDeletedHDRI()
        {
            m_LookDevEnvLibrary.CleanupDeletedHDRI();
        }

        private void OnGUI()
        {
            // Debug code to load a custom shader for development
            /*
            bool toto = false;

            if (toto)
            {
                Shader devShader = Shader.Find("Custom/DevShader") as Shader;
                if( devShader != null)
                    LookDevResources.m_LookDevCompositing = new Material(devShader);
            }
            */


            // To enable renderdoc in lookdev it require to expose BeginCaptureRenderDoc and EndCaptureRenderDoc to script.
            if (Event.current.type == EventType.Repaint && m_CaptureRD)
            {
                //m_Parent.BeginCaptureRenderDoc();
            }

            Initialize();
            CleanupDeletedHDRI();
            BeginWindows();
            m_DisplayRect = new Rect(0, kLineHeight, position.width, (position.height - kLineHeight));

            UpdateViewSpecific();

            DoToolbarGUI();

            HandleDragging();

            RenderPreview();

            DoControlWindow();
            DoAdditionalGUI();
            DoGizmoDebug();

            HandleMouseInput();
            HandleCamera();
            HandleKeyboardShortcut();

            // Draw text to say that we need to drag and drop an object
            if (m_LookDevConfig.currentObjectInstances[0][0] == null && m_LookDevConfig.currentObjectInstances[1][0] == null)
            {
                Color oldColor = GUI.color;
                GUI.color = Color.gray;
                Vector2 textSize = GUI.skin.label.CalcSize(styles.sDragAndDropObjsText);
                Rect labelRect = new Rect(m_DisplayRect.width * .5f - textSize.x * .5f, m_DisplayRect.height * .2f - textSize.y * .5f, textSize.x, textSize.y);
                GUI.Label(labelRect, styles.sDragAndDropObjsText);
                GUI.color = oldColor;
            }

            EndWindows();

            // this is a bit tricky and actually depends heavily on the LookDevEnvironmentWindow
            // When the user drags an HDRI from the libray, we display it half transparent where the mouse cursor is.
            // The problem is that we can't render that in the LookDevEnvironmentWindow outsite of its Rect, so we render it once more here from the main LookDev window
            if (Event.current.type == EventType.Repaint)
            {
                if (m_LookDevEnvWindow != null && m_LookDevEnvWindow.GetCurrentSelection() != null)
                {
                    m_CurrentDragContext = GetEditionContext(Event.current.mousePosition);
                    GUI.DrawTexture(new Rect(Event.current.mousePosition.x - m_LookDevEnvWindow.GetSelectedPositionOffset().x, Event.current.mousePosition.y - m_LookDevEnvWindow.GetSelectedPositionOffset().y, LookDevEnvironmentWindow.m_HDRIWidth, LookDevEnvironmentWindow.m_latLongHeight), LookDevResources.m_SelectionTexture, ScaleMode.ScaleToFit, true);
                }
                else
                {
                    m_CurrentDragContext = LookDevEditionContext.None;
                }
            }


            if (Event.current.type == EventType.Repaint && m_CaptureRD)
            {
                //m_Parent.EndCaptureRenderDoc();
                m_CaptureRD = false;
            }
        }

        void GetShaderConstantsFromNormalizedSH(SphericalHarmonicsL2 ambientProbe, Vector4[] outCoefficients)
        {
            for (int channelIdx = 0; channelIdx < 3; ++channelIdx)
            {
                // Constant + Linear
                // In the shader we multiply the normal is not swizzled, so it's normal.xyz.
                // Swizzle the coefficients to be in { x, y, z, DC } order.
                outCoefficients[channelIdx].x = ambientProbe[channelIdx, 3];
                outCoefficients[channelIdx].y = ambientProbe[channelIdx, 1];
                outCoefficients[channelIdx].z = ambientProbe[channelIdx, 2];
                outCoefficients[channelIdx].w = ambientProbe[channelIdx, 0] - ambientProbe[channelIdx, 6];

                // Quadratic polynomials
                outCoefficients[channelIdx + 3].x = ambientProbe[channelIdx, 4];
                outCoefficients[channelIdx + 3].y = ambientProbe[channelIdx, 5];
                outCoefficients[channelIdx + 3].z = ambientProbe[channelIdx, 6] * 3.0f;
                outCoefficients[channelIdx + 3].w = ambientProbe[channelIdx, 7];
            }

            // Final quadratic polynomial
            outCoefficients[6].x = ambientProbe[0, 8];
            outCoefficients[6].y = ambientProbe[1, 8];
            outCoefficients[6].z = ambientProbe[2, 8];
            outCoefficients[6].w = 1.0f;
        }
    }
}
