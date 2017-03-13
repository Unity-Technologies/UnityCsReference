// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
    public class PreviewRenderUtility
    {
        public Camera m_Camera;
        public float m_CameraFieldOfView = 15.0f;
        public Light[] m_Light = { null, null };
        internal RenderTexture m_RenderTexture;
        private Rect m_TargetRect;
        private SavedRenderTargetState m_SavedState;

        public PreviewRenderUtility() : this(false)
        {
        }
        public PreviewRenderUtility(bool renderFullScene)
        {
            GameObject cameraGO = EditorUtility.CreateGameObjectWithHideFlags("PreRenderCamera", HideFlags.HideAndDontSave, typeof(Camera));
            m_Camera = cameraGO.GetComponent<Camera>();
            m_Camera.cameraType = CameraType.Preview;
            m_Camera.fieldOfView = m_CameraFieldOfView;
            m_Camera.enabled = false;
            m_Camera.clearFlags = CameraClearFlags.Depth;
            m_Camera.farClipPlane = 10.0f;
            m_Camera.nearClipPlane = 2.0f;
            m_Camera.backgroundColor = new Color(49.0f / 255.0f, 49.0f / 255.0f, 49.0f / 255.0f, 1.0f);
            // Explicitly use forward rendering for all previews
            // (deferred fails when generating some static previews at editor launch; and we never want
            // vertex lit previews if that is chosen in the player settings)
            m_Camera.renderingPath = RenderingPath.Forward;
            m_Camera.useOcclusionCulling = false;

            if (!renderFullScene)
                Handles.SetCameraOnlyDrawMesh(m_Camera);
            for (int i = 0; i < 2; i++)
            {
                GameObject lightGO = EditorUtility.CreateGameObjectWithHideFlags("PreRenderLight", HideFlags.HideAndDontSave, typeof(Light));
                m_Light[i] = lightGO.GetComponent<Light>();
                m_Light[i].type = LightType.Directional;
                m_Light[i].intensity = 1.0f;
                m_Light[i].enabled = false;
            }
            m_Light[0].color = SceneView.kSceneViewFrontLight;
            m_Light[1].transform.rotation = Quaternion.Euler(340, 218, 177);
            m_Light[1].color = new Color(.4f, .4f, .45f, 0f) * .7f;
        }

        public void Cleanup()
        {
            if (m_Camera)
                Object.DestroyImmediate(m_Camera.gameObject, true);
            if (m_RenderTexture)
            {
                Object.DestroyImmediate(m_RenderTexture);
                m_RenderTexture = null;
            }
            foreach (Light l in m_Light)
            {
                if (l)
                    Object.DestroyImmediate(l.gameObject, true);
            }
        }

        private void BeginPreview(Rect r, GUIStyle previewBackground, bool hdr)
        {
            InitPreview(r, hdr);

            if (previewBackground == null || previewBackground == GUIStyle.none)
                return;

            Graphics.DrawTexture(
                previewBackground.overflow.Add(new Rect(0, 0, m_RenderTexture.width, m_RenderTexture.height)),
                previewBackground.normal.background,
                new Rect(0, 0, 1, 1),
                previewBackground.border.left, previewBackground.border.right, previewBackground.border.top,
                previewBackground.border.bottom,
                new Color(.5f, .5f, .5f, 1),
                null
                );
        }

        private void BeginStaticPreview(Rect r, bool hdr)
        {
            InitPreview(r, hdr);
            var color = new Color(82 / 255f, 82 / 255f, 82 / 255f, 1.0f);
            var darkGreyBackground = new Texture2D(1, 1, TextureFormat.RGBA32, true, true);
            darkGreyBackground.SetPixel(0, 0, color);
            darkGreyBackground.Apply();
            Graphics.DrawTexture(new Rect(0, 0, m_RenderTexture.width, m_RenderTexture.height), darkGreyBackground);
            Object.DestroyImmediate(darkGreyBackground);
        }

        private void InitPreview(Rect r, bool hdr)
        {
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
                m_RenderTexture = new RenderTexture(rtWidth, rtHeight, 16, hdr ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
                m_RenderTexture.hideFlags = HideFlags.HideAndDontSave;

                m_Camera.targetTexture = m_RenderTexture;
            }
            // Calculate a view multiplier to avoid clipping when the preview width is smaller than the height.
            float viewMultiplier = (m_RenderTexture.width <= 0 ? 1.0f : Mathf.Max(1.0f, (float)m_RenderTexture.height / m_RenderTexture.width));
            // Multiply the viewing area by the viewMultiplier - it requires some conversions since the camera view is expressed as an angle.
            m_Camera.fieldOfView = Mathf.Atan(viewMultiplier * Mathf.Tan(m_CameraFieldOfView * 0.5f * Mathf.Deg2Rad)) * Mathf.Rad2Deg * 2.0f;

            m_SavedState = new SavedRenderTargetState();
            EditorGUIUtility.SetRenderTextureNoViewport(m_RenderTexture);
            GL.LoadOrtho();
            GL.LoadPixelMatrix(0, m_RenderTexture.width, m_RenderTexture.height, 0);
            ShaderUtil.rawViewportRect = new Rect(0, 0, m_RenderTexture.width, m_RenderTexture.height);
            ShaderUtil.rawScissorRect = new Rect(0, 0, m_RenderTexture.width, m_RenderTexture.height);
            GL.Clear(true, true, new Color(0, 0, 0, 0));
        }

        public float GetScaleFactor(float width, float height)
        {
            float scaleFacX = Mathf.Max(Mathf.Min(width * 2, 1024), width) / width;
            float scaleFacY = Mathf.Max(Mathf.Min(height * 2, 1024), height) / height;
            return Mathf.Min(scaleFacX, scaleFacY) * EditorGUIUtility.pixelsPerPoint;
        }

        public void BeginStaticPreview(Rect r)
        {
            BeginStaticPreview(r, false);
        }

        public void BeginStaticPreviewHDR(Rect r)
        {
            BeginStaticPreview(r, true);
        }

        public void BeginPreview(Rect r, GUIStyle previewBackground)
        {
            BeginPreview(r, previewBackground, false);
        }

        public void BeginPreviewHDR(Rect r, GUIStyle previewBackground)
        {
            BeginPreview(r, previewBackground, true);
        }

        public Texture EndPreview()
        {
            m_SavedState.Restore();
            return m_RenderTexture;
        }

        public void EndAndDrawPreview(Rect r)
        {
            Texture t = EndPreview();
            GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            GUI.DrawTexture(r, t, ScaleMode.StretchToFill, false);
            GL.sRGBWrite = false;
        }

        public Texture2D EndStaticPreview()
        {
            var tmp = RenderTexture.GetTemporary((int)m_TargetRect.width, (int)m_TargetRect.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);

            GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            Graphics.Blit(m_RenderTexture, tmp);
            GL.sRGBWrite = false;

            RenderTexture.active = tmp;
            var copy = new Texture2D((int)m_TargetRect.width, (int)m_TargetRect.height, TextureFormat.RGB24, false, true);
            copy.ReadPixels(new Rect(0, 0, m_TargetRect.width, m_TargetRect.height), 0, 0);
            copy.Apply();
            RenderTexture.ReleaseTemporary(tmp);
            m_SavedState.Restore();
            return copy;
        }

        public void DrawMesh(Mesh mesh, Vector3 pos, Quaternion rot, Material mat, int subMeshIndex)
        {
            DrawMesh(mesh, pos, rot, mat, subMeshIndex, null);
        }

        public void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material mat, int subMeshIndex)
        {
            DrawMesh(mesh, matrix, mat, subMeshIndex, null);
        }

        public void DrawMesh(Mesh mesh, Vector3 pos, Quaternion rot, Material mat, int subMeshIndex, MaterialPropertyBlock customProperties)
        {
            Graphics.DrawMesh(mesh, pos, rot, mat, 1, m_Camera, subMeshIndex, customProperties);
        }

        public void DrawMesh(Mesh mesh, Vector3 pos, Quaternion rot, Material mat, int subMeshIndex, MaterialPropertyBlock customProperties, Transform probeAnchor)
        {
            Graphics.DrawMesh(mesh, pos, rot, mat, 1, m_Camera, subMeshIndex, customProperties, UnityEngine.Rendering.ShadowCastingMode.Off, false, probeAnchor);
        }

        public void DrawMesh(Mesh mesh, Vector3 pos, Quaternion rot, Material mat, int subMeshIndex, MaterialPropertyBlock customProperties, Transform probeAnchor, bool useLightProbe)
        {
            Graphics.DrawMesh(mesh, pos, rot, mat, 1, m_Camera, subMeshIndex, customProperties, UnityEngine.Rendering.ShadowCastingMode.Off, false, probeAnchor, useLightProbe);
        }

        public void DrawMesh(Mesh mesh, Matrix4x4 matrix, Material mat, int subMeshIndex, MaterialPropertyBlock customProperties)
        {
            Graphics.DrawMesh(mesh, matrix, mat, 1, m_Camera, subMeshIndex, customProperties);
        }

        static internal Mesh GetPreviewSphere()
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
