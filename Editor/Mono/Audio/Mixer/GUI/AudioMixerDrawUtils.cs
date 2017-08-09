// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.Audio;

namespace UnityEditor
{
    internal class AudioMixerDrawUtils
    {
        // OpenGL and D3D<11 use different ways of specifying the rasterization grid, so in order to draw exactly we need to account for this.
        private static float vertexOffset = -1.0f;
        private static void DetectVertexOffset()
        {
            //string driver = SystemInfo.graphicsDeviceVersion;
            //vertexOffset = 0.5f; //(driver.StartsWith("OpenGL") || driver.StartsWith("Direct3D 11")) ? 0.0f : 0.5f;
            //vertexOffset /= EditorGUIUtility.pixelsPerPoint;
            vertexOffset = 0.0f;
            //Debug.Log("Detected vertex offset as " + vertexOffset + " driver=" + driver);
        }

        static readonly Color kAttenuationColor = AudioCurveRendering.kAudioOrange;
        static readonly Color kEffectColor = new Color(103 / 255f, 160 / 255f, 0 / 255f);
        static readonly Color kSendColor = new Color(0 / 255f, 147 / 255f, 170 / 255f);
        static readonly Color kReceiveColor = kSendColor;
        static readonly Color kDuckVolumeColor = kSendColor;

        public static Color GetEffectColor(AudioMixerEffectController effect)
        {
            if (effect.IsSend())
                return (effect.sendTarget != null) ? kSendColor : Color.gray;
            if (effect.IsReceive())
                return kReceiveColor;
            if (effect.IsDuckVolume())
                return kDuckVolumeColor;
            if (effect.IsAttenuation())
                return kAttenuationColor;

            return kEffectColor;
        }

        public static Color kBackgroundHi          = new Color(0.5f, 0.5f, 0.5f);
        public static Color kBackgroundLo          = new Color(0.3f, 0.3f, 0.3f);
        public static Color kBackgroundHiHighlight = new Color(0.6f, 0.6f, 0.6f);
        public static Color kBackgroundLoHighlight = new Color(0.4f, 0.4f, 0.4f);

        public class Styles
        {
            public GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            public GUIStyle reorderableListLabel = new GUIStyle("PR Label");
            public GUIStyle regionBg = GetStyle("RegionBg");
            public GUIStyle channelStripVUMeterBg = GetStyle("ChannelStripVUMeterBg");
            public GUIStyle channelStripAreaBackground = "CurveEditorBackground"; //"flow background";
            public GUIStyle channelStripBg = GetStyle("ChannelStripBg");
            public GUIStyle duckingMarker = GetStyle("ChannelStripDuckingMarker");
            public GUIStyle channelStripAttenuationMarkerSquare = GetStyle("ChannelStripAttenuationMarkerSquare");
            public GUIStyle channelStripHeaderStyle;
            public GUIStyle soloToggle = GetStyle("SoloToggle");
            public GUIStyle muteToggle = GetStyle("MuteToggle");
            public GUIStyle bypassToggle = GetStyle("BypassToggle");
            public GUIStyle circularToggle = GetStyle("CircularToggle");
            public GUIStyle totalVULevel = new GUIStyle(EditorStyles.label);
            public GUIStyle attenuationBar = "ChannelStripAttenuationBar";
            public GUIStyle effectBar = "ChannelStripEffectBar";
            public GUIStyle sendReturnBar = "ChannelStripSendReturnBar";
            public GUIStyle effectName = new GUIStyle(EditorStyles.miniLabel);
            public GUIStyle vuValue = new GUIStyle(EditorStyles.miniLabel);
            public GUIStyle mixerHeader = new GUIStyle(EditorStyles.largeLabel);
            public GUIStyle warningOverlay = GetStyle("WarningOverlay");
            public Texture2D scrollShadowTexture = EditorGUIUtility.FindTexture("ScrollShadow");
            public Texture2D leftToRightShadowTexture = EditorGUIUtility.FindTexture("LeftToRightShadow");
            public GUIContent soloGUIContent = new GUIContent("", "Adds this group to set of soloed groups");
            public GUIContent muteGUIContent = new GUIContent("", "Mutes this group");
            public GUIContent bypassGUIContent = new GUIContent("", "Bypasses the effects on this group");
            public GUIContent effectSlotGUIContent = new GUIContent("", "Drag horizontally to change wet mix levels or vertically to change order of effects. Note: Enable wet mixing in the context menu.");
            public GUIContent attenuationSlotGUIContent = new GUIContent("", "Place the attenuation slot in the effect stack where attenuation should take effect");
            public GUIContent emptySendSlotGUIContent = new GUIContent("", "Connect to a Receive in the context menu or in the inspector");
            public GUIContent returnSlotGUIContent = new GUIContent("", "Connect a Send to this Receive");
            public GUIContent duckVolumeSlotGUIContent = new GUIContent("", "Connect a Send to this Duck Volume");
            public GUIContent duckingFaderGUIContent = new GUIContent("", "Ducking Fader");
            public GUIContent attenuationFader = new GUIContent("", "Attenuation fader");
            public GUIContent vuMeterGUIContent = new GUIContent("", "The VU meter shows the current level of the mix of all sounds and subgroups.");
            public GUIContent referencedGroups = new GUIContent("Referenced groups", "Mixer groups that are hidden but are referenced by the visible mixer groups are shown here for convenience");
            public GUIContent sendString = new GUIContent("s");


            static GUIStyle GetStyle(string styleName)
            {
                return styleName; // Implicit construction of GUIStyle
            }

            public Styles()
            {
                headerStyle.alignment = TextAnchor.MiddleLeft;

                Texture2D transparent = reorderableListLabel.hover.background;
                reorderableListLabel.normal.background = transparent;
                reorderableListLabel.active.background = transparent;
                reorderableListLabel.focused.background = transparent;
                reorderableListLabel.onNormal.background = transparent;
                reorderableListLabel.onHover.background = transparent;
                reorderableListLabel.onActive.background = transparent;
                reorderableListLabel.onFocused.background = transparent;
                reorderableListLabel.padding.left = reorderableListLabel.padding.right = 0;
                reorderableListLabel.alignment = TextAnchor.MiddleLeft;

                scrollShadowTexture = EditorGUIUtility.FindTexture("ScrollShadow");

                channelStripHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
                channelStripHeaderStyle.alignment = TextAnchor.MiddleLeft;
                channelStripHeaderStyle.fontSize = 11;
                channelStripHeaderStyle.fontStyle = FontStyle.Bold;
                channelStripHeaderStyle.wordWrap = false;
                channelStripHeaderStyle.clipping = TextClipping.Clip;
                channelStripHeaderStyle.padding = new RectOffset(4, 4, 4, 4);

                totalVULevel.alignment = TextAnchor.MiddleRight;
                totalVULevel.padding.right = 20;

                effectName.padding.left = 4;
                effectName.padding.top = 2;

                vuValue.padding.left = 4;
                vuValue.padding.right = 4;
                vuValue.padding.top = 0;
                vuValue.alignment = TextAnchor.MiddleRight;
                vuValue.clipping = TextClipping.Overflow;

                warningOverlay.alignment = TextAnchor.MiddleCenter;

                mixerHeader.fontStyle = FontStyle.Bold;
                mixerHeader.fontSize = 17;
                mixerHeader.margin = new RectOffset();
                mixerHeader.padding = new RectOffset();
                mixerHeader.alignment = TextAnchor.UpperLeft;
                if (EditorGUIUtility.isProSkin)
                    mixerHeader.normal.textColor = new Color(0.7f, 0.7f, 0.7f, 1.0f);
                else
                    mixerHeader.normal.textColor = new Color(0.2f, 0.2f, 0.2f, 1.0f);
            }
        }
        static Styles s_Styles;
        public static Styles styles { get { return s_Styles; }}
        public static void InitStyles()
        {
            if (s_Styles == null)
            {
                s_Styles = new Styles();
                DetectVertexOffset();
            }
        }

        public static float GetAlpha()
        {
            return GUI.enabled ? 1.0f : 0.7f;
        }

        public static void DrawSplitter()
        {
            Rect r = GUILayoutUtility.GetRect(1, 1);
            if (Event.current.type == EventType.Repaint)
            {
                Color color = (EditorGUIUtility.isProSkin ? new Color(0.12f, 0.12f, 0.12f, 1.333f) : new Color(0.6f, 0.6f, 0.6f, 1.333f)); // dark : light
                EditorGUI.DrawRect(r, color);
            }
        }

        public static void Vertex(float x, float y)
        {
            GL.Vertex3(x + vertexOffset, y + vertexOffset, 0);
        }

        public static void DrawLine(float x1, float y1, float x2, float y2, Color c)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            HandleUtility.ApplyWireMaterial();
            GL.Begin(GL.LINES);
            GL.Color(new Color(c.r, c.g, c.b, c.a * GetAlpha()));
            Vertex(x1, y1);
            Vertex(x2, y2);
            GL.End();
        }

        //todo: 339 draw calls in our test case. Reduce it to fewer calls into this function.
        public static void DrawGradientRect(Rect r, Color c1, Color c2)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            //Profiler.BeginSample ("DrawGradientRect");
            HandleUtility.ApplyWireMaterial();
            GL.Begin(GL.QUADS);
            GL.Color(new Color(c1.r, c1.g, c1.b, c1.a * GetAlpha()));
            Vertex(r.x, r.y);
            Vertex(r.x + r.width, r.y);
            GL.Color(new Color(c2.r, c2.g, c2.b, c2.a * GetAlpha()));
            Vertex(r.x + r.width, r.y + r.height);
            Vertex(r.x, r.y + r.height);
            GL.End();
            //Profiler.EndSample ();
        }

        public static void DrawGradientRectHorizontal(Rect r, Color c1, Color c2)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            //Profiler.BeginSample ("DrawGradientRectHorizontal");
            HandleUtility.ApplyWireMaterial();
            GL.Begin(GL.QUADS);
            GL.Color(new Color(c1.r, c1.g, c1.b, c1.a * GetAlpha()));
            Vertex(r.x + r.width, r.y);
            Vertex(r.x + r.width, r.y + r.height);
            GL.Color(new Color(c2.r, c2.g, c2.b, c2.a * GetAlpha()));
            Vertex(r.x, r.y + r.height);
            Vertex(r.x, r.y);
            GL.End();
            //Profiler.EndSample ();
        }

        internal const float kSectionHeaderHeight = 22f;
        internal const float kSpaceBetweenSections = 10f;

        public static void DrawRegionBg(Rect rect, out Rect headerRect, out Rect contentRect)
        {
            const float headerIndent = 2f;
            headerRect = new Rect(rect.x + headerIndent, rect.y, rect.width - headerIndent, kSectionHeaderHeight);
            contentRect = new Rect(rect.x, headerRect.yMax, rect.width, rect.height - kSectionHeaderHeight);
            GUI.Label(new RectOffset(1, 1, 1, 1).Add(contentRect), GUIContent.none, EditorStyles.helpBox);
        }

        public static void HeaderLabel(Rect r, GUIContent text, Texture2D icon)
        {
            if (icon != null)
            {
                EditorGUIUtility.SetIconSize(new Vector2(16, 16));
                GUI.Label(r, icon, styles.headerStyle);
                EditorGUIUtility.SetIconSize(Vector2.zero);
                r.xMin += 20f;
            }
            GUI.Label(r, text, styles.headerStyle);
        }

        public static GUIStyle BuildGUIStyleForLabel(Color color, int fontSize, bool wrapText, FontStyle fontstyle, TextAnchor anchor)
        {
            GUIStyle style = new GUIStyle();
            style.focused.background = style.onNormal.background;
            style.focused.textColor = color;
            style.alignment = anchor;
            style.fontSize = fontSize;
            style.fontStyle = fontstyle;
            style.wordWrap = wrapText;
            style.clipping = TextClipping.Clip;
            style.normal.textColor = color;
            style.padding = new RectOffset(4, 4, 4, 4);
            return style;
        }

        public static void ReadOnlyLabel(Rect r, GUIContent content, GUIStyle style)
        {
            GUI.Label(r, content, style);
        }

        public static void ReadOnlyLabel(Rect r, string text, GUIStyle style)
        {
            GUI.Label(r, GUIContent.Temp(text), style);
        }

        public static void ReadOnlyLabel(Rect r, string text, GUIStyle style, string tooltipText)
        {
            GUI.Label(r, GUIContent.Temp(text, tooltipText), style);
        }

        public static void AddTooltipOverlay(Rect r, string tooltip)
        {
            GUI.Label(r, GUIContent.Temp(string.Empty, tooltip), s_Styles.headerStyle);
        }

        public static void DrawConnection(Color col, float mixLevel, float srcX, float srcY, float dstX, float dstY, float width)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            //Profiler.BeginSample ("DrawConnection");
            HandleUtility.ApplyWireMaterial();

            var dirX = dstX - srcX;
            var dirY = dstY - srcY;
            var scale = width / Mathf.Sqrt(dirX * dirX + dirY * dirY);
            dirX *= scale;
            dirY *= scale;

            float a = 2.0f, b = 1.2f;
            var arrowPoints = new Vector3[4];
            arrowPoints[0] = new Vector3(dstX, dstY);
            arrowPoints[1] = new Vector3(dstX - a * dirX + b * dirY, dstY - a * dirY - b * dirX);
            arrowPoints[2] = new Vector3(dstX - a * dirX - b * dirY, dstY - a * dirY + b * dirX);
            arrowPoints[3] = arrowPoints[0];

            Color triColor = new Color(col.r, col.g, col.b, mixLevel * 0.3f + 0.3f);
            Shader.SetGlobalColor("_HandleColor", triColor);
            GL.Begin(GL.TRIANGLES);
            GL.Color(triColor);
            GL.Vertex(arrowPoints[0]);
            GL.Vertex(arrowPoints[1]);
            GL.Vertex(arrowPoints[2]);
            GL.End();

            Handles.DrawAAPolyLine(width, new[] { triColor, triColor, triColor, triColor }, arrowPoints);

            Handles.DrawAAPolyLine(
                width,
                new[] { new Color(col.r, col.g, col.b, mixLevel), new Color(col.r, col.g, col.b, mixLevel) },
                new[] { new Vector3(srcX, srcY, 0.0f), new Vector3(dstX, dstY, 0.0f) }
                );
            //Profiler.EndSample ();
        }

        public static void DrawVerticalShow(Rect rect, bool fadeToTheRight)
        {
            Rect textureCoordRect = fadeToTheRight ? new Rect(0, 0, 1, 1) : new Rect(1, 1, -1, -1);   // flip over X
            GUI.DrawTextureWithTexCoords(rect, styles.leftToRightShadowTexture, textureCoordRect);
        }

        public static void DrawScrollDropShadow(Rect scrollViewRect, float scrollY, float contentHeight)
        {
            if (Event.current.type == EventType.Repaint)
            {
                bool isShowingScrollBar = contentHeight > scrollViewRect.height;
                if (!isShowingScrollBar)
                    return;

                const float shadowHeightTop = 20f;
                const float shadowHeightBottom = 10f;
                const float fadeDist = 10f;

                Color orgColor = GUI.color;

                // Top shadow is faded in over the first 'fadeDist'
                float alpha = scrollY > fadeDist ? 1f : (scrollY / fadeDist);
                if (alpha < 1f)
                    GUI.color = new Color(1, 1, 1, alpha);
                if (alpha > 0f)
                    GUI.DrawTexture(new Rect(scrollViewRect.x, scrollViewRect.y, scrollViewRect.width, shadowHeightTop), styles.scrollShadowTexture);
                if (alpha < 1f)
                    GUI.color = orgColor;

                // Bottom shadow is shown if the scroll bar is visible
                float scrollAvailable = Mathf.Max(contentHeight - scrollViewRect.height, 0f);
                float alphaBottom = (scrollAvailable - scrollY) > fadeDist ? 1f : (scrollAvailable - scrollY) / fadeDist;
                if (alphaBottom < 1f)
                    GUI.color = new Color(1, 1, 1, alphaBottom);
                if (alphaBottom > 0f)
                    GUI.DrawTextureWithTexCoords(new Rect(scrollViewRect.x, scrollViewRect.yMax - shadowHeightBottom, scrollViewRect.width, shadowHeightBottom), styles.scrollShadowTexture, new Rect(1, 1, -1, -1)); // flip over Y
                if (alphaBottom < 1f)
                    GUI.color = orgColor;
            }
        }

        public static void DrawRect(Rect rect, Color color)
        {
            Color orgColor = GUI.color;
            GUI.color = color;
            GUI.Label(rect, GUIContent.Temp(string.Empty), EditorGUIUtility.whiteTextureStyle);   // Using style rendering so that disabled state is reflected in rendering
            GUI.color = orgColor;
        }
    }
}
