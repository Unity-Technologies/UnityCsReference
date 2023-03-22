// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace UnityEditor
{
    [System.Flags]
    internal enum EditorFeatures
    {
        None = 0,
        PreviewGUI = 1,
        OnSceneDrag = 4
    }

    internal class EditorWrapper : System.IDisposable
    {
        internal Editor editor;

        public delegate void VoidDelegate(SceneView sceneView, int index);

        public VoidDelegate OnSceneDrag;

        public bool HasPreviewGUI() { return editor.HasPreviewGUI(); }
        public void OnPreviewSettings() { editor.OnPreviewSettings(); }
        public void OnPreviewGUI(Rect position, GUIStyle background) { editor.OnPreviewGUI(position, background); }
        public void OnInteractivePreviewGUI(Rect r, GUIStyle background) { if (editor != null) editor.OnInteractivePreviewGUI(r, background); }
        internal void OnAssetStoreInspectorGUI() { if (editor != null) editor.OnAssetStoreInspectorGUI(); }
        public string GetInfoString() { return editor.GetInfoString(); }

        private EditorWrapper() {}

        static public EditorWrapper Make(Object obj, EditorFeatures requirements)
        {
            EditorWrapper cp = new EditorWrapper();
            if (cp.Init(obj, requirements))
                return cp;
            else
            {
                cp.Dispose();
                return null;
            }
        }

        private bool Init(Object obj, EditorFeatures requirements)
        {
            editor = Editor.CreateEditor(obj);
            if (editor == null)
                return false;

            if (requirements.HasFlag(EditorFeatures.PreviewGUI) && !editor.HasPreviewGUI())
                return false;

            const BindingFlags sceneDragReflectionFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var onSceneDragMethodInfo = editor.GetType().GetMethod("OnSceneDrag", sceneDragReflectionFlags);

            if (onSceneDragMethodInfo == null)
            {
                var nonRootEditor = RootEditorUtils.CreateNonRootEditor(new[] { obj });

                // Are we dealing with a Root Editor?
                if (editor.GetType() != nonRootEditor.GetType())
                {
                    // Destroy previous editor
                    UnityEngine.Object.DestroyImmediate(editor);
                    // Try again, with a non Root editor
                    editor = nonRootEditor;
                    onSceneDragMethodInfo = editor.GetType().GetMethod("OnSceneDrag", sceneDragReflectionFlags);
                }
                else
                {
                    // Destroy unused non Root editor
                    UnityEngine.Object.DestroyImmediate(nonRootEditor);
                }
            }

            if (onSceneDragMethodInfo != null)
                OnSceneDrag = (VoidDelegate)System.Delegate.CreateDelegate(typeof(VoidDelegate), editor, onSceneDragMethodInfo);
            else if (requirements.HasFlag(EditorFeatures.OnSceneDrag))
                return false;
            else
                OnSceneDrag = DefaultOnSceneDrag;

            return true;
        }

        private void DefaultOnSceneDrag(SceneView sceneView, int index) {}

        public string name
        {
            get { return editor.target.name; }
        }

        public void Dispose()
        {
            if (editor != null)
            {
                OnSceneDrag = null;

                UnityEngine.Object.DestroyImmediate(editor);
                editor = null;
            }

            System.GC.SuppressFinalize(this);
        }

        ~EditorWrapper()
        {
            Debug.LogError("Failed to dispose EditorWrapper.");
        }
    }

    internal class EditorCache : System.IDisposable
    {
        Dictionary<Object, EditorWrapper> m_EditorCache;
        Dictionary<Object, bool> m_UsedEditors;
        EditorFeatures m_Requirements;

        public EditorCache() : this(EditorFeatures.None) {}


        public EditorCache(EditorFeatures requirements)
        {
            m_Requirements = requirements;
            m_EditorCache = new Dictionary<Object, EditorWrapper>();
            m_UsedEditors = new Dictionary<Object, bool>();
        }

        public EditorWrapper this[Object o]
        {
            get
            {
                m_UsedEditors[o] = true;
                if (m_EditorCache.ContainsKey(o))
                    return m_EditorCache[o];

                EditorWrapper w = EditorWrapper.Make(o, m_Requirements);
                return m_EditorCache[o] = w;
            }
        }

        public void CleanupUntouchedEditors()
        {
            if (m_EditorCache != null)
            {
                List<Object> toDelete = new List<Object>();
                foreach (Object key in m_EditorCache.Keys)
                {
                    if (!m_UsedEditors.ContainsKey(key))
                        toDelete.Add(key);
                }

                foreach (Object key in toDelete)
                {
                    EditorWrapper ew = m_EditorCache[key];
                    m_EditorCache.Remove(key);

                    if (ew == null)
                        continue;
                    ew.Dispose();
                }
            }

            m_UsedEditors.Clear();
        }

        public void CleanupAllEditors()
        {
            m_UsedEditors.Clear();
            CleanupUntouchedEditors();
        }

        public void Dispose()
        {
            CleanupAllEditors();
            System.GC.SuppressFinalize(this);
        }

        ~EditorCache()
        {
            Debug.LogError("Failed to dispose EditorCache.");
        }
    }
} // namespace
