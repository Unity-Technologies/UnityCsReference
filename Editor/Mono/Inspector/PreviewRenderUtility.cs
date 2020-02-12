// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class PreviewScene : IDisposable
    {
        private readonly Scene m_Scene;
        private readonly List<GameObject> m_GameObjects = new List<GameObject>();
        private readonly Camera m_Camera;

        public PreviewScene(string sceneName)
        {
            m_Scene = EditorSceneManager.NewPreviewScene();
            if (!m_Scene.IsValid())
                throw new InvalidOperationException("Preview scene could not be created");

            m_Scene.name = sceneName;

            var camGO = EditorUtility.CreateGameObjectWithHideFlags("Preview Scene Camera", HideFlags.HideAndDontSave, typeof(Camera));
            AddGameObject(camGO);
            m_Camera = camGO.GetComponent<Camera>();
            camera.cameraType = CameraType.Preview;
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.Depth;
            camera.fieldOfView = 15;
            camera.farClipPlane = 10.0f;
            camera.nearClipPlane = 2.0f;

            // Explicitly use forward rendering for all previews
            // (deferred fails when generating some static previews at editor launch; and we never want
            // vertex lit previews if that is chosen in the player settings)
            camera.renderingPath = RenderingPath.Forward;
            camera.useOcclusionCulling = false;
            camera.scene = m_Scene;
        }

        public Camera camera
        {
            get { return m_Camera; }
        }

        public Scene scene
        {
            get { return m_Scene; }
        }

        public void AddGameObject(GameObject go)
        {
            if (m_GameObjects.Contains(go))
                return;

            SceneManager.MoveGameObjectToScene(go, m_Scene);
            m_GameObjects.Add(go);
        }

        public void AddManagedGO(GameObject go)
        {
            SceneManager.MoveGameObjectToScene(go, m_Scene);
        }

        public void Dispose()
        {
            EditorSceneManager.ClosePreviewScene(m_Scene);

            foreach (var go in m_GameObjects)
                Object.DestroyImmediate(go);

            m_GameObjects.Clear();
        }
    }

    public class PreviewRenderUtility
    {
        private readonly PreviewScene m_PreviewScene;

        private RenderTexture m_RenderTexture;
        private Rect m_TargetRect;
        private SavedRenderTargetState m_SavedState;
        private bool m_PixelPerfect;
        private Material m_InvisibleMaterial;

        private string m_Type;

        // This is used to track colour space changes
        // and try to keep colour values in sync
        private ColorSpace colorSpace;
        private Color defaultBackgroundColor;

        public PreviewRenderUtility(bool renderFullScene) : this()
        {}

        public PreviewRenderUtility(bool renderFullScene, bool pixelPerfect) : this()
        {
            m_PixelPerfect = pixelPerfect;
        }

        public PreviewRenderUtility()
        {
            m_PreviewScene = new PreviewScene("Preview Scene");

            var l0 = CreateLight();
            previewScene.AddGameObject(l0);
            Light0 = l0.GetComponent<Light>();

            var l1 = CreateLight();
            previewScene.AddGameObject(l1);
            Light1 = l1.GetComponent<Light>();

            Light0.color = SceneView.kSceneViewFrontLight;
            Light1.transform.rotation = Quaternion.Euler(340, 218, 177);
            Light1.color = new Color(.4f, .4f, .45f, 0f) * .7f;

            m_PixelPerfect = false;

            // Set a default background color
            defaultBackgroundColor = new Color(49.0f / 255.0f, 49.0f / 255.0f, 49.0f / 255.0f, 1.0f);
            colorSpace = QualitySettings.activeColorSpace;
            camera.backgroundColor = colorSpace == ColorSpace.Gamma ? defaultBackgroundColor : defaultBackgroundColor.linear;

            if (Unsupported.IsDeveloperMode())
            {
                var stackTrace = new StackTrace();
                for (int i = 0; i < stackTrace.FrameCount; i++)
                {
                    var frame = stackTrace.GetFrame(i);
                    var type = frame.GetMethod().DeclaringType;
                    if (type != null && (type.IsSubclassOf(typeof(Editor)) || type.IsSubclassOf(typeof(EditorWindow))))
                    {
                        m_Type = type.Name;
                        break;
                    }
                }
            }
        }

        ~PreviewRenderUtility()
        {
            if (m_Type != null)
            {
                Debug.LogErrorFormat("{0} created a PreviewRenderUtility but didn't call its Cleanup() during OnDisable. This is leaking the Preview scene in the Editor and should be fixed.", m_Type);
            }
            else
            {
                Debug.LogError("A PreviewRenderUtility was not clean up properly before assembly reloading which lead to leaking this scene in the Editor. " +
                    "This can be caused by not calling Cleanup() during the OnDisable of an Editor or an EditorWindow.");
            }
        }

        internal static void SetEnabledRecursive(GameObject go, bool enabled)
        {
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>())
                renderer.enabled = enabled;
        }

        [Obsolete("Use the property camera instead (UnityUpgradable) -> camera", false)]
        public Camera m_Camera;

        public Camera camera
        {
            get { return previewScene.camera; }
        }

        [Obsolete("Use the property cameraFieldOfView (UnityUpgradable) -> cameraFieldOfView", false)]
        public float m_CameraFieldOfView;

        public float cameraFieldOfView
        {
            get { return camera.fieldOfView; }
            set { camera.fieldOfView = value; }
        }

        public Color ambientColor { get; set; }

        [Obsolete("Use the property lights (UnityUpgradable) -> lights", false)]
        public Light[] m_Light;

        public Light[] lights
        {
            get
            {
                return new[] { Light0, Light1 };
            }
        }

        private Light Light0 { get; set; }

        private Light Light1 { get; set; }

        internal RenderTexture renderTexture
        {
            get { return m_RenderTexture; }
        }

        internal PreviewScene previewScene
        {
            get { return m_PreviewScene; }
        }

        public void Cleanup()
        {
            if (m_RenderTexture)
            {
                Object.DestroyImmediate(m_RenderTexture);
                m_RenderTexture = null;
            }

            if (m_InvisibleMaterial != null)
            {
                Object.DestroyImmediate(m_InvisibleMaterial);
                m_InvisibleMaterial = null;
            }

            previewScene.Dispose();
            GC.SuppressFinalize(this);
        }

        public void BeginPreview(Rect r, GUIStyle previewBackground)
        {
            Texture defaultEnvTexture = ReflectionProbe.defaultTexture;

            if (Unsupported.SetOverrideLightingSettings(previewScene.scene))
            {
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = ambientColor;

                RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
                RenderSettings.customReflection = defaultEnvTexture as Cubemap;
            }

            InitPreview(r);

            if (previewBackground == null || previewBackground == GUIStyle.none || previewBackground.normal.background == null)
                return;

            Graphics.DrawTexture(
                previewBackground.overflow.Add(new Rect(0, 0, m_RenderTexture.width, m_RenderTexture.height)),
                previewBackground.normal.background, new Rect(0, 0, 1, 1), previewBackground.border.left,
                previewBackground.border.right, previewBackground.border.top, previewBackground.border.bottom,
                new Color(.5f, .5f, .5f, 0.5f), null
            );
        }

        public void BeginStaticPreview(Rect r)
        {
            InitPreview(r);
            var color = new Color(82 / 255f, 82 / 255f, 82 / 255f, 1.0f);
            var darkGreyBackground = new Texture2D(1, 1, TextureFormat.RGBA32, true);
            darkGreyBackground.SetPixel(0, 0, color);
            darkGreyBackground.Apply();
            Graphics.DrawTexture(new Rect(0, 0, m_RenderTexture.width, m_RenderTexture.height), darkGreyBackground);
            Object.DestroyImmediate(darkGreyBackground);

            if (!EditorApplication.isUpdating)
            {
                var oldProbe = RenderSettings.ambientProbe;
                Texture defaultEnvTexture = ReflectionProbe.defaultTexture;
                if (Unsupported.SetOverrideLightingSettings(previewScene.scene))
                {
                    // Most preview windows just want the light probe from the main scene so by default we copy it here. It can then be overridden if user wants.
                    RenderSettings.ambientProbe = oldProbe;
                    RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
                    RenderSettings.customReflection = defaultEnvTexture as Cubemap;
                }
            }
        }

        private void InitPreview(Rect r)
        {
            // If the background colour has changed then we can't make any assumptions
            // about colour space, otherwise flip to the background colour to the correct one
            if (colorSpace != QualitySettings.activeColorSpace
                && (camera.backgroundColor == defaultBackgroundColor || camera.backgroundColor.linear == defaultBackgroundColor.linear))
            {
                camera.backgroundColor = QualitySettings.activeColorSpace == ColorSpace.Linear
                    ? defaultBackgroundColor.linear
                    : defaultBackgroundColor;
            }

            m_TargetRect = r;
            float scaleFac = GetScaleFactor(r.width, r.height);

            int rtWidth = (int)(r.width * scaleFac);
            int rtHeight = (int)(r.height * scaleFac);
            if (!m_RenderTexture || m_RenderTexture.width != rtWidth || m_RenderTexture.height != rtHeight)
            {
                if (m_RenderTexture)
                {
                    Object.DestroyImmediate(m_RenderTexture);
                    m_RenderTexture = null;
                }

                // Do not use GetTemporary to manage render textures. Temporary RTs are only
                // garbage collected each N frames, and in the editor we might be wildly resizing
                // the inspector, thus using up tons of memory.
                GraphicsFormat format = camera.allowHDR ? GraphicsFormat.R16G16B16A16_SFloat : GraphicsFormat.R8G8B8A8_UNorm;
                m_RenderTexture = new RenderTexture(rtWidth, rtHeight, 32, format);
                m_RenderTexture.hideFlags = HideFlags.HideAndDontSave;

                camera.targetTexture = m_RenderTexture;

                foreach (var light in lights)
                    light.enabled = true;
            }

            m_SavedState = new SavedRenderTargetState();
            EditorGUIUtility.SetRenderTextureNoViewport(m_RenderTexture);
            GL.LoadOrtho();
            GL.LoadPixelMatrix(0, m_RenderTexture.width, m_RenderTexture.height, 0);
            ShaderUtil.rawViewportRect = new Rect(0, 0, m_RenderTexture.width, m_RenderTexture.height);
            ShaderUtil.rawScissorRect = new Rect(0, 0, m_RenderTexture.width, m_RenderTexture.height);
            GL.Clear(true, true, camera.backgroundColor);

            foreach (var light in lights)
                light.enabled = true;
        }

        public float GetScaleFactor(float width, float height)
        {
            float scaleFacX = Mathf.Max(Mathf.Min(width * 2, 1024), width) / width;
            float scaleFacY = Mathf.Max(Mathf.Min(height * 2, 1024), height) / height;
            float result = Mathf.Min(scaleFacX, scaleFacY) * EditorGUIUtility.pixelsPerPoint;
            if (m_PixelPerfect)
                result = Mathf.Max(Mathf.Round(result), 1f);
            return result;
        }

        [Obsolete("This method has been marked obsolete, use BeginStaticPreview() instead (UnityUpgradable) -> BeginStaticPreview(*)", false)]
        public void BeginStaticPreviewHDR(Rect r)
        {
            BeginStaticPreview(r);
        }

        [Obsolete("This method has been marked obsolete, use BeginPreview() instead (UnityUpgradable) -> BeginPreview(*)", false)]
        public void BeginPreviewHDR(Rect r, GUIStyle previewBackground)
        {
            BeginPreview(r, previewBackground);
        }

        public Texture EndPreview()
        {
            Unsupported.RestoreOverrideLightingSettings();

            m_SavedState.Restore();
            FinishFrame();
            return m_RenderTexture;
        }

        private void FinishFrame()
        {
            Unsupported.RestoreOverrideLightingSettings();
            foreach (var light in lights)
                light.enabled = false;
        }

        public void EndAndDrawPreview(Rect r)
        {
            var texture = EndPreview();
            DrawPreview(r, texture);
        }

        internal static void DrawPreview(Rect r, Texture texture)
        {
            GUI.DrawTexture(r, texture, ScaleMode.StretchToFill, false);
        }

        public Texture2D EndStaticPreview()
        {
            if (!EditorApplication.isUpdating)
                Unsupported.RestoreOverrideLightingSettings();

            var tmp = RenderTexture.GetTemporary((int)m_TargetRect.width, (int)m_TargetRect.height, 0, GraphicsFormat.R8G8B8A8_UNorm);

            Graphics.Blit(m_RenderTexture, tmp, EditorGUIUtility.GUITextureBlit2SRGBMaterial);

            RenderTexture.active = tmp;
            var copy = new Texture2D((int)m_TargetRect.width, (int)m_TargetRect.height, TextureFormat.RGB24, false, false);
            copy.ReadPixels(new Rect(0, 0, m_TargetRect.width, m_TargetRect.height), 0, 0);
            copy.Apply();
            RenderTexture.ReleaseTemporary(tmp);
            m_SavedState.Restore();
            FinishFrame();
            return copy;
        }

        [Obsolete("AddSingleGO(GameObject go, bool instantiateAtZero) has been deprecated, use AddSingleGo(GameObject go) instead. instantiateAtZero has no effect and is not supported.")]
        public void AddSingleGO(GameObject go, bool instantiateAtZero)
        {
            AddSingleGO(go);
        }

        public void AddSingleGO(GameObject go)
        {
            previewScene.AddGameObject(go);
        }

        public GameObject InstantiatePrefabInScene(GameObject prefab)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, m_PreviewScene.scene);
            return instance;
        }

        internal void AddManagedGO(GameObject go)
        {
            m_PreviewScene.AddManagedGO(go);
        }

        public void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material mat, int subMeshIndex)
        {
            DrawMesh(mesh, matrix, mat, subMeshIndex, null, null, false);
        }

        public void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material mat, int subMeshIndex, MaterialPropertyBlock customProperties)
        {
            DrawMesh(mesh, matrix, mat, subMeshIndex, customProperties, null, false);
        }

        public void DrawMesh(Mesh mesh, Matrix4x4 m, Material mat, int subMeshIndex, MaterialPropertyBlock customProperties, Transform probeAnchor, bool useLightProbe)
        {
            var quat = Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
            var pos = m.GetColumn(3);
            var scale = new Vector3(
                m.GetColumn(0).magnitude,
                m.GetColumn(1).magnitude,
                m.GetColumn(2).magnitude
            );

            DrawMesh(mesh, pos, scale, quat, mat, subMeshIndex, customProperties, probeAnchor, useLightProbe);
        }

        public void DrawMesh(Mesh mesh, Vector3 pos, Quaternion rot, Material mat, int subMeshIndex)
        {
            DrawMesh(mesh, pos, rot, mat, subMeshIndex, null, null, false);
        }

        public void DrawMesh(Mesh mesh, Vector3 pos, Quaternion rot, Material mat, int subMeshIndex, MaterialPropertyBlock customProperties)
        {
            DrawMesh(mesh, pos, rot, mat, subMeshIndex, customProperties, null, false);
        }

        public void DrawMesh(Mesh mesh, Vector3 pos, Quaternion rot, Material mat, int subMeshIndex, MaterialPropertyBlock customProperties, Transform probeAnchor)
        {
            DrawMesh(mesh, pos, rot, mat, subMeshIndex, customProperties, probeAnchor, false);
        }

        public void DrawMesh(Mesh mesh, Vector3 pos, Quaternion rot, Material mat, int subMeshIndex, MaterialPropertyBlock customProperties, Transform probeAnchor, bool useLightProbe)
        {
            DrawMesh(mesh, pos, Vector3.one, rot, mat, subMeshIndex, customProperties, probeAnchor, useLightProbe);
        }

        public void DrawMesh(Mesh mesh, Vector3 pos, Vector3 scale, Quaternion rot, Material mat, int subMeshIndex, MaterialPropertyBlock customProperties, Transform probeAnchor, bool useLightProbe)
        {
            Graphics.DrawMesh(mesh, Matrix4x4.TRS(pos, rot, scale), mat, 1, camera, subMeshIndex, customProperties, ShadowCastingMode.Off, false, probeAnchor, useLightProbe);
        }

        internal static Mesh GetPreviewSphere()
        {
            var handleGo = (GameObject)EditorGUIUtility.LoadRequired("Previews/PreviewMaterials.fbx");
            // Temp workaround to make it not render in the scene
            handleGo.SetActive(false);
            foreach (Transform t in handleGo.transform)
            {
                if (t.name == "sphere")
                    return t.GetComponent<MeshFilter>().sharedMesh;
            }
            return null;
        }

        protected static GameObject CreateLight()
        {
            GameObject lightGO = EditorUtility.CreateGameObjectWithHideFlags("PreRenderLight", HideFlags.HideAndDontSave, typeof(Light));
            var light = lightGO.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            light.enabled = false;
            return lightGO;
        }

        public void Render(bool allowScriptableRenderPipeline = false, bool updatefov = true)
        {
            foreach (var light in lights)
                light.enabled = true;
            var oldAllowPipes = Unsupported.useScriptableRenderPipeline;
            Unsupported.useScriptableRenderPipeline = allowScriptableRenderPipeline;

            float saveFieldOfView = camera.fieldOfView;

            if (updatefov)
            {
                // Calculate a view multiplier to avoid clipping when the preview width is smaller than the height.
                float viewMultiplier = (m_RenderTexture.width <= 0 ? 1.0f : Mathf.Max(1.0f, (float)m_RenderTexture.height / m_RenderTexture.width));
                // Multiply the viewing area by the viewMultiplier - it requires some conversions since the camera view is expressed as an angle.
                camera.fieldOfView = Mathf.Atan(viewMultiplier * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad)) * Mathf.Rad2Deg * 2.0f;
            }

            camera.Render();

            camera.fieldOfView = saveFieldOfView;
            Unsupported.useScriptableRenderPipeline = oldAllowPipes;
        }
    }

    internal class SavedRenderTargetState
    {
        RenderTexture renderTexture;
        Rect viewport;
        Rect scissor;

        internal SavedRenderTargetState()
        {
            GL.PushMatrix();
            if (ShaderUtil.hardwareSupportsRectRenderTexture)
                renderTexture = RenderTexture.active;
            viewport = ShaderUtil.rawViewportRect;
            scissor = ShaderUtil.rawScissorRect;
        }

        internal void Restore()
        {
            if (ShaderUtil.hardwareSupportsRectRenderTexture)
                EditorGUIUtility.SetRenderTextureNoViewport(renderTexture);
            ShaderUtil.rawViewportRect = viewport;
            ShaderUtil.rawScissorRect = scissor;
            GL.PopMatrix();
        }
    }
}
