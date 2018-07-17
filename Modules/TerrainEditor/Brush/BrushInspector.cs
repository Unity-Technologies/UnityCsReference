// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor.Experimental.AssetImporters;
using System.Collections;
using System.IO;
using UnityEditorInternal;
using UnityEditor;


namespace UnityEditor
{
    [CustomEditor(typeof(Brush))]
    internal class BrushInspector : Editor
    {
        [SerializeField]
        protected Vector2 m_Pos;

        SerializedProperty m_Mask;
        SerializedProperty m_Falloff;
        SerializedProperty m_RadiusScale;

        bool m_HasChanged = true;
        Texture2D m_PreviewTexture = null;

        static class Styles
        {
            public static GUIContent maskTexture = EditorGUIUtility.TrTextContent("Mask texture");
        }


        internal virtual void OnEnable()
        {
            if (!target)
                return;

            m_Mask = serializedObject.FindProperty("m_Mask");
            m_Falloff = serializedObject.FindProperty("m_Falloff");
            m_RadiusScale = serializedObject.FindProperty("m_RadiusScale");
        }

        private Texture2D Texture2DSelectorButtonAlpha(Texture2D texture, params GUILayoutOption[] options)
        {
            Rect stampPreviewRect = EditorGUILayout.GetControlRect(true, options);

            Event evt = Event.current;
            EventType eventType = evt.type;

            // special case test, so we continue to ping/select objects with the object field disabled
            if (!GUI.enabled && GUIClip.enabled && (Event.current.rawType == EventType.MouseDown))
                eventType = Event.current.rawType;

            int id = "TextureSelectorButton".GetHashCode();

            switch (eventType)
            {
                case EventType.MouseDown:
                {
                    // Ignore right clicks
                    if (Event.current.button != 0)
                        break;
                    if (stampPreviewRect.Contains(Event.current.mousePosition))
                    {
                        if (GUI.enabled)
                        {
                            GUIUtility.keyboardControl = id;
                            ObjectSelector.get.Show(texture, typeof(Texture2D), null, true);
                            ObjectSelector.get.objectSelectorID = id;
                            evt.Use();
                            GUIUtility.ExitGUI();
                        }
                    }
                    break;
                }

                case EventType.ExecuteCommand:
                {
                    string commandName = evt.commandName;
                    if (commandName == "ObjectSelectorUpdated" && ObjectSelector.get.objectSelectorID == id && GUIUtility.keyboardControl == id)
                    {
                        texture = (Texture2D)ObjectSelector.GetCurrentObject();
                    }
                    else if (commandName == "ObjectSelectorClosed" && ObjectSelector.get.objectSelectorID == id && GUIUtility.keyboardControl == id)
                    {
                        if (ObjectSelector.get.GetInstanceID() == 0)
                        {
                            // User canceled object selection; don't apply
                            evt.Use();
                            break;
                        }
                        texture = (Texture2D)ObjectSelector.GetCurrentObject();
                    }

                    if (!texture)
                        texture = EditorGUIUtility.whiteTexture;

                    // TODO is it ok ?
                    //Repaint();
                    break;
                }
            }

            if (!texture)
                texture = EditorGUIUtility.whiteTexture;

            EditorGUI.DrawTextureAlpha(stampPreviewRect, texture);
            return texture;
        }

        public override void OnInspectorGUI()
        {
            Brush b = target as Brush;
            if (b.m_ReadOnly)
                return;

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            Texture2D mask = (Texture2D)EditorGUILayout.ObjectField(Styles.maskTexture, (Texture2D)m_Mask.objectReferenceValue, typeof(Texture2D), false);
            if (mask == null)
            {
                mask = Brush.DefaultMask();
                m_HasChanged = true;
            }
            m_Mask.objectReferenceValue = mask;
            EditorGUILayout.CurveField(m_Falloff, Color.white, new Rect(0, 0, 1, 1));
            EditorGUILayout.Slider(m_RadiusScale, 1.0f, Brush.kMaxRadiusScale);
            m_HasChanged |= EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();

            b.SetDirty(m_HasChanged);
        }

        public override bool HasPreviewGUI()
        {
            return target != null;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type == EventType.Repaint)
                background.Draw(r, false, false, false, false);

            Texture2D mask = (Texture2D)m_Mask.objectReferenceValue;
            if (mask == null)
            {
                mask = Brush.DefaultMask();
                m_HasChanged = true;
            }

            int texWidth = Mathf.Min(mask.width, 256);
            int texHeight = Mathf.Min(mask.height, 256);

            if (m_HasChanged || m_PreviewTexture == null)
            {
                m_PreviewTexture = Brush.GenerateBrushTexture(mask, m_Falloff.animationCurveValue, m_RadiusScale.floatValue, texWidth, texHeight);
                m_HasChanged = false;
            }

            float zoomLevel = Mathf.Min(Mathf.Min(r.width / texWidth, r.height / texHeight), 1);
            Rect wantedRect = new Rect(r.x, r.y, texWidth * zoomLevel, texHeight * zoomLevel);
            PreviewGUI.BeginScrollView(r, m_Pos, wantedRect, "PreHorizontalScrollbar", "PreHorizontalScrollbarThumb");

            if (m_PreviewTexture.alphaIsTransparency)
                EditorGUI.DrawTextureTransparent(wantedRect, m_PreviewTexture);
            else
                EditorGUI.DrawPreviewTexture(wantedRect, m_PreviewTexture);

            m_Pos = PreviewGUI.EndScrollView();
        }

        public sealed override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height)
        {
            Brush brush = AssetDatabase.LoadMainAssetAtPath(assetPath) as Brush;

            if (brush == null)
                return Brush.DefaultMask();

            if (brush.m_Mask == null)
                brush.m_Mask = Brush.DefaultMask();
            PreviewHelpers.AdjustWidthAndHeightForStaticPreview(brush.m_Mask.width, brush.m_Mask.height, ref width, ref height);
            return Brush.GenerateBrushTexture((Texture2D)brush.m_Mask, brush.m_Falloff, brush.m_RadiusScale, width, height);
        }
    }
}
