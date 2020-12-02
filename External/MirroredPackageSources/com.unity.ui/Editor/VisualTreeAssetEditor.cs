using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [CustomEditor(typeof(VisualTreeAsset))]
    internal class VisualTreeAssetEditor : ScriptableObjectAssetEditor
    {
        private Panel m_Panel;
        private VisualElement m_Tree;
        private VisualTreeAsset m_LastTree;
        protected Texture2D m_FileTypeIcon;
        protected RenderTexture m_preview_texture;
        private Event m_evt = new Event();//Dummy event to fake rendering, cached to reduce memory allocation.
        private int m_LastDirtyCount;
        private int m_LastContentHash;

        //Currently just uses a fixed size texture to minimize lag/jitter as we are not integrated in the update loop. (instead of the real preview size)
        private Vector2Int m_TextureSize = new Vector2Int(512, 512);

        protected void OnEnable()
        {
            m_FileTypeIcon = EditorGUIUtility.FindTexture(typeof(VisualTreeAsset));
            EditorApplication.update += Update;
            m_LastTree = null;//Force redraw;
        }

        protected void OnDisable()
        {
            EditorApplication.update -= Update;
            if (m_Panel != null)
            {
                m_Panel.Dispose();
                m_Panel = null;
            }
        }

        void Update()
        {
            UpdatePreviewTexture(m_TextureSize.x, m_TextureSize.y);
        }

        protected void OnDestroy()
        {
            if (m_LastTree != null)
            {
                UIElementsUtility.RemoveCachedPanel(m_LastTree.GetInstanceID());
            }

            if (m_preview_texture != null)
            {
                m_preview_texture.Release();
                m_preview_texture.DiscardContents();
                DestroyImmediate(m_preview_texture);
            }
        }

        public override bool HasPreviewGUI()
        {
            return true;
        }

        protected void RenderIcon(Rect iconRect)
        {
            Debug.Assert(m_FileTypeIcon != null);
            GUI.DrawTexture(iconRect, m_FileTypeIcon, ScaleMode.ScaleToFit);
        }

        private void RenderStaticPreview(int width, int height, ref RenderTexture tex)
        {
            //No backup of the viewport is currently done
            //It is not necessary if rendereing to a rendertexture in the editor update loop but mandatory to do an IM rendering
            //RectInt oldViewport = UnityEngine.UIElements.UIR.Utility.GetActiveViewport();

            var r = new Rect(0, 0, width, height);
            var viewportRect = GUIClip.UnclipToWindow(r); // Still in points, not pixels
            m_Panel.visualTree.SetSize(viewportRect.size); // We will draw relative to a viewport covering the preview area, so draw at 0,0
            m_Panel.visualTree.IncrementVersion(VersionChangeType.Repaint);

            var backup = RenderTexture.active;
            GL.PushMatrix();
            var oldState = SavedGUIState.Create();
            PanelClearSettings oldClearSettings = m_Panel.clearSettings;

            try
            {
                if (tex == null || tex.width != width || tex.height != height)
                {
                    if (tex != null)
                    {
                        tex.Release();
                        tex.DiscardContents();
                        DestroyImmediate(tex);
                    }

                    tex = new RenderTexture((int)viewportRect.size.x, (int)viewportRect.size.y, 24);
                }

                RenderTexture.active = tex;
                GL.LoadPixelMatrix();
                GL.Clear(true, true, Color.black, UIRUtility.k_ClearZ);

                int clips = GUIClip.Internal_GetCount();
                while (clips > 0)
                {
                    GUIClip.Pop();
                    clips--;
                }

                m_Panel.clearSettings = new PanelClearSettings();

                //Use a dummy repaint event, otherwise imgui element wont be shown when using event.current and rendered in the editor update loop
                m_evt.type = EventType.Repaint;
                m_Panel.Repaint(m_evt);
            }
            finally
            {
                m_Panel.clearSettings = oldClearSettings;
                oldState.ApplyAndForget();
                GL.PopMatrix();
                RenderTexture.active = backup;
            }

            // As stated above, viewport is not saved/restored.
            // Doing  " GL.Viewport(new Rect(oldViewport.xMin, oldViewport.yMin, oldViewport.width, oldViewport.height));" is not ennough
        }

        public void Render(VisualTreeAsset vta, Rect r, GUIStyle background)
        {
            if (Event.current.type != EventType.Repaint || r.width < 100 && r.height < 100)
                return;


            // Simon.Dufour: was not able to find what setting to revert after rendering the panel so that it does not affect the current rendering.
            // Update of the preview texture has been moved to the editor update loop;
            // In typical IMGUI fashion, updatePreviewTexture((int)r.width, (int)r.height); would be called here

            if (m_preview_texture)
            {
                Vector2 size = Mathf.Min(r.height / (float)m_preview_texture.height, r.width / (float)m_preview_texture.width) * new Vector2(m_preview_texture.width, m_preview_texture.height);

                EditorGUI.DrawPreviewTexture(new Rect(r.center - size / 2, size), m_preview_texture);
            }
        }

        private bool UpdatePreviewTexture(int width, int height)
        {
            var vta = target as VisualTreeAsset;
            bool dirty = false;
            int currentDirtyCount = EditorUtility.GetDirtyCount(target);
            if (vta != m_LastTree || !m_LastTree || currentDirtyCount != m_LastDirtyCount || vta.contentHash != m_LastContentHash)
            {
                m_LastTree = vta;
                m_Tree = vta.Instantiate();
                m_Tree.StretchToParentSize();
                m_LastDirtyCount = currentDirtyCount;
                m_LastContentHash = vta.contentHash;
                dirty = true;
            }

            if (m_Panel == null)
            {
                m_Panel = UIElementsUtility.FindOrCreateEditorPanel(m_LastTree);
                var visualTree = m_Panel.visualTree;
                UIElementsEditorUtility.AddDefaultEditorStyleSheets(visualTree);
                dirty = true;
            }

            if (dirty)
            {
                m_Panel.visualTree.Clear();
                m_Panel.visualTree.Add(m_Tree);
                RenderStaticPreview(width, height, ref m_preview_texture);
            }

            return dirty;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            const int k_IconSize = 64;

            base.OnPreviewGUI(r, background);


            if (r.width > k_IconSize || r.height > k_IconSize)
            {
                Render(target as VisualTreeAsset, r, background);
            }
            else
            {
                RenderIcon(r);
            }
        }
    }
}
