// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    class InstructionOverlayWindow : EditorWindow
    {
        class Styles
        {
            public GUIStyle solidColor;

            public Styles()
            {
                solidColor = new GUIStyle();
                solidColor.normal.background = EditorGUIUtility.whiteTexture;
            }
        }

        static Styles s_Styles;
        GUIView m_InspectedGUIView;
        Rect m_InstructionRect;
        GUIStyle m_InstructionStyle;
        RenderTexture m_RenderTexture;

        [NonSerialized]
        bool m_RenderTextureNeedsRefresh = false;

        Styles styles
        {
            get
            {
                if (s_Styles == null)
                    s_Styles = new Styles();
                return s_Styles;
            }
        }

        void Start()
        {
            minSize = Vector2.zero;
            m_Parent.window.m_DontSaveToLayout = true;
        }

        public void SetTransparent(float d)
        {
            m_Parent.window.SetAlpha(d);
            m_Parent.window.SetInvisible();
        }

        public void Show(GUIView view, Rect instructionRect, GUIStyle style)
        {
            minSize = Vector2.zero;

            m_InstructionStyle = style;

            m_InspectedGUIView = view;
            m_InstructionRect = instructionRect;
            Rect finalRect = new Rect(instructionRect);


            finalRect.x += m_InspectedGUIView.screenPosition.x;
            finalRect.y += m_InspectedGUIView.screenPosition.y;

            position = finalRect;

            m_RenderTextureNeedsRefresh = true;

            ShowWithMode(ShowMode.NoShadow);
            m_Parent.window.m_DontSaveToLayout = true;

            //m_InstructionOverlayTemp.SetTransparent(0.5f);
            Repaint();
        }

        void DoRefreshRenderTexture()
        {
            if (m_RenderTexture == null)
            {
                int width = Mathf.Max(Mathf.CeilToInt(m_InstructionRect.width), 1);
                int height = Mathf.Max(Mathf.CeilToInt(m_InstructionRect.height), 1);
                m_RenderTexture = new RenderTexture(width, height, 24);
                m_RenderTexture.Create();
            }
            else if (m_RenderTexture.width != m_InstructionRect.width || m_RenderTexture.height != m_InstructionRect.height)
            {
                m_RenderTexture.Release();
                m_RenderTexture.width = Mathf.Max(Mathf.CeilToInt(m_InstructionRect.width), 1);
                m_RenderTexture.height = Mathf.Max(Mathf.CeilToInt(m_InstructionRect.height), 1);
                m_RenderTexture.Create();
            }

            //m_InspectedGUIView.GrabPixels(m_RenderTexture, m_InstructionRect);

            m_RenderTextureNeedsRefresh = false;
            Repaint();
        }

        void Update()
        {
            if (m_RenderTextureNeedsRefresh)
            {
                DoRefreshRenderTexture();
            }
        }

        void OnFocus()
        {
            GetWindow<GUIViewDebuggerWindow>();
        }

        void OnGUI()
        {
            Color paddingBg = new Color(0.76f, 0.87f, 0.71f);
            Color contentBg = new Color(0.62f, 0.77f, 0.90f);

            Rect elementRect = new Rect(0, 0, m_InstructionRect.width, m_InstructionRect.height);

            GUI.backgroundColor = paddingBg;
            GUI.Box(elementRect, GUIContent.none, styles.solidColor);

            RectOffset padding = m_InstructionStyle.padding;

            Rect noPadding = padding.Remove(elementRect);

            GUI.backgroundColor = contentBg;
            GUI.Box(noPadding, GUIContent.none, styles.solidColor);
        }
    }
}
