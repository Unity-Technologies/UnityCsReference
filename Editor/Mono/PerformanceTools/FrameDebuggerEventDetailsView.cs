// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Experimental.Rendering;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.AnimatedValues;

namespace UnityEditorInternal
{
    internal class FrameDebuggerEventDetailsView
    {
        // Render target view options
        [NonSerialized] private int m_RTIndex;
        [NonSerialized] private bool m_ForceRebuildStrings = false;
        [NonSerialized] private int m_RTIndexLastSet = int.MaxValue;
        [NonSerialized] private int m_RTSelectedChannel;
        [NonSerialized] private float m_RTBlackLevel;
        [NonSerialized] private float m_RTWhiteLevel = 1.0f;

        // Private
        private int m_SelectedColorChannel = 0;
        private bool m_ShouldShowMeshListFoldout = false;
        private Vector2 m_ScrollViewVector = Vector2.zero;
        private Vector4 m_SelectedMask = Vector4.one;
        private Material m_TargetTextureMaterial = null;
        private AnimBool[] m_FoldoutAnimators = null;
        private GUIContent[] m_OutputMeshTabsGuiContents = new[] { new GUIContent("Output"), new GUIContent("Mesh Preview") };
        private List<string> m_KeywordsList = new List<String>();
        private StringBuilder m_TempSB1 = new StringBuilder();
        private StringBuilder m_TempSB2 = new StringBuilder();
        private EventDisplayData m_LastEventData;
        private ShowAdditionalInfo m_OutputMeshTabs = ShowAdditionalInfo.ShaderProperties;
        private FrameDebuggerWindow m_FrameDebugger = null;
        private Lazy<FrameDebuggerEventData> m_CurEventData = new Lazy<FrameDebuggerEventData>(() => new FrameDebuggerEventData());

        // Constants
        private const int k_NumberGUISections = 10;
        private const int k_ArraySizeBitMask = 0x3FF;
        private const int k_ShaderTypeBits = (int)ShaderType.Count;

        // Properties
        private FrameDebuggerEvent curEvent { get; set; }
        private FrameDebuggerEventData curEventData => m_CurEventData.Value;

        // Structs

        // Shader Property ID's for the shader used to display the output texture
        private struct ShaderPropertyIDs
        {
            public static int _Levels = Shader.PropertyToID("_Levels");
            public static int _MainTex = Shader.PropertyToID("_MainTex");
            public static int _Channels = Shader.PropertyToID("_Channels");
            public static int _ShouldYFlip = Shader.PropertyToID("_ShouldYFlip");
            public static int _UndoOutputSRGB = Shader.PropertyToID("_UndoOutputSRGB");
        }

        // Cached data built from FrameDebuggerEventData.
        // Only need to rebuild them when event data actually changes.
        private struct EventDisplayData
        {
            public uint hash;
            public int index;
            public bool isValid;
            public bool isClearEvent;
            public bool isResolveEvent;
            public bool isComputeEvent;
            public bool isRayTracingEvent;
            public string title;
            public string detailsLabelsLeftColumn;
            public string detailsValuesLeftColumn;
            public string detailLabelsRightColumn;
            public string detailValuesRightColumn;
            public string keywords;
            public string shaderName;
            public string passAndLightMode;
            public string listOfMeshesString;
            public string firstMeshName;
            public string meshTitle;
            public UnityEngine.Object shader;
            public FrameEventType type;
        }

        // Public functions
        public FrameDebuggerEventDetailsView(FrameDebuggerWindow frameDebugger)
        {
            m_FrameDebugger = frameDebugger;

            m_LastEventData = new EventDisplayData();
            m_LastEventData.keywords = string.Empty;
        }

        public void Reset()
        {
            m_RTSelectedChannel = 0;
            m_SelectedColorChannel = 0;
            m_RTIndex = 0;
            m_RTBlackLevel = 0.0f;
            m_RTWhiteLevel = 1.0f;
        }

        MeshPreview m_Preview;
        public void OnNewFrameEventSelected()
        {
            m_KeywordsList.Clear();

            m_Preview?.Dispose();
            m_Preview = null;
        }

        public void OnDisable()
        {
            m_Preview?.Dispose();
            m_Preview = null;
        }

        // Here is the major performance bottleneck!
        public void DrawEvent(Rect rect, FrameDebuggerEvent[] descs, bool isDebuggingEditor)
        {
            int curEventIndex = FrameDebuggerUtility.limit - 1;
            if (!FrameDebuggerHelper.IsAValidFrame(curEventIndex, descs.Length))
                return;

            Initialize(curEventIndex, descs, out bool isReceivingFrameEventData, out bool isFrameEventDataValid);

            GUILayout.BeginArea(rect);
            m_ScrollViewVector = GUILayout.BeginScrollView(m_ScrollViewVector);

            // Toolbar
            Profiler.BeginSample("DrawToolbar");
            {
                DrawRenderTargetToolbar();
            }
            Profiler.EndSample();

            // Title
            Profiler.BeginSample("DrawTitle");
            {
                GUILayout.BeginHorizontal(FrameDebuggerStyles.EventDetails.titleHorizontalStyle);
                EditorGUILayout.LabelField(m_LastEventData.title, FrameDebuggerStyles.EventDetails.titleStyle);
                GUILayout.EndHorizontal();
            }
            Profiler.EndSample();

            // Output & Mesh
            // We disable Output and Mesh for Compute and Ray Tracing events
            bool shouldDrawOutputAndMesh = !m_LastEventData.isComputeEvent && !m_LastEventData.isRayTracingEvent;
            Profiler.BeginSample("DrawOutputAndMesh");
            {
                DrawOutputAndMesh(rect, shouldDrawOutputAndMesh, isDebuggingEditor);
            }
            Profiler.EndSample();

            // Event Details
            Profiler.BeginSample("DrawEventDetails");
            {
                DrawEventDetails(rect);
            }
            Profiler.EndSample();

            // We disable and hide keywords and shader properties for clear and resolve events.
            bool shouldDisplayProperties = !m_LastEventData.isClearEvent && !m_LastEventData.isResolveEvent;

            // Keywords...
            Profiler.BeginSample("DrawKeywords");
            {
                DrawKeywords(shouldDisplayProperties);
            }
            Profiler.EndSample();

            // Properties...
            Profiler.BeginSample("DrawProperties");
            {
                DrawProperties(shouldDisplayProperties);
            }
            Profiler.EndSample();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        ///////////////////////////////////////////////
        // PRIVATE
        ///////////////////////////////////////////////

        private void Initialize(int curEventIndex, FrameDebuggerEvent[] descs, out bool isReceivingFrameEventData, out bool isFrameEventDataValid)
        {
            uint eventDataHash = FrameDebuggerUtility.eventDataHash;
            isReceivingFrameEventData = FrameDebugger.IsRemoteEnabled() && FrameDebuggerUtility.receivingRemoteFrameEventData;
            isFrameEventDataValid = curEventIndex == curEventData.frameEventIndex;

            if (!isFrameEventDataValid || (eventDataHash != 0 && (eventDataHash != m_LastEventData.hash || m_ForceRebuildStrings)))
            {
                isFrameEventDataValid = FrameDebuggerUtility.GetFrameEventData(curEventIndex, curEventData);
                m_LastEventData.hash = eventDataHash;
                m_LastEventData.isValid = false;
                m_ForceRebuildStrings = false;
            }

            // event type and draw call info
            curEvent = descs[curEventIndex];
            FrameEventType eventType = curEvent.type;

            // Rebuild strings...
            if (isFrameEventDataValid)
                if (!m_LastEventData.isValid || m_LastEventData.index != curEventIndex || m_LastEventData.type != eventType)
                    BuildCurEventDataStrings(curEvent, curEventData);

            if (m_FoldoutAnimators == null || m_FoldoutAnimators.Length == 0)
            {
                m_FoldoutAnimators = new AnimBool[k_NumberGUISections];
                for (int i = 0; i < m_FoldoutAnimators.Length; i++)
                    m_FoldoutAnimators[i] = new AnimBool(i < 2);
            }

            // TODO: Sort the properties
            // TODO: Make the sorting work for both single parameters as well
            // TODO: as parameters in arrays. Found an issue in SRP0601_RealtimeLights
            // TODO: in Mingwai's CustomSRP project.
            /*ShaderProperties data = curEventData.shaderProperties;
            if (!data.Equals(default(ShaderProperties)))
            {
                Array.Sort(data.buffers);
                Array.Sort(data.cbuffers);
                Array.Sort(data.floats);
                Array.Sort(data.ints);
                Array.Sort(data.matrices);
                Array.Sort(data.textures);
                Array.Sort(data.vectors);
            }*/
        }

        private void DrawRenderTargetToolbar()
        {
            if (m_LastEventData.isRayTracingEvent)
                return;

            bool isBackBuffer = curEventData.rtIsBackBuffer;
            bool isDepthOnlyRT = GraphicsFormatUtility.IsDepthFormat((GraphicsFormat)curEventData.rtFormat);
            bool isClearAction = (int)curEvent.type <= 7;
            bool hasShowableDepth = (curEventData.rtHasDepthTexture != 0);
            int showableRTCount = curEventData.rtCount;

            if (hasShowableDepth)
                showableRTCount++;

            GUILayout.BeginHorizontal(FrameDebuggerStyles.EventToolbar.toolbarHorizontalStyle);

            // MRT to show
            EditorGUI.BeginChangeCheck();
            GUI.enabled = showableRTCount > 1;

            var rtNames = new GUIContent[showableRTCount];
            for (var i = 0; i < curEventData.rtCount; ++i)
                rtNames[i] = FrameDebuggerStyles.EventToolbar.MRTLabels[i];

            if (hasShowableDepth)
                rtNames[curEventData.rtCount] = FrameDebuggerStyles.EventToolbar.depthLabel;

            // If we showed depth before then try to keep showing depth
            // otherwise try to keep showing color
            if (m_RTIndexLastSet == -1)
                m_RTIndex = hasShowableDepth ? showableRTCount - 1 : 0;
            else if (m_RTIndex > curEventData.rtCount)
                m_RTIndex = 0;

            m_RTIndex = EditorGUILayout.Popup(m_RTIndex, rtNames, FrameDebuggerStyles.EventToolbar.popupLeftStyle, GUILayout.Width(70));


            GUI.enabled = !isBackBuffer && !isDepthOnlyRT;

            // color channels
            EditorGUILayout.Space(5f);
            GUILayout.Label(FrameDebuggerStyles.EventToolbar.channelHeader, FrameDebuggerStyles.EventToolbar.channelHeaderStyle);
            EditorGUILayout.Space(5f);

            int channelToDisplay = 0;
            bool forceUpdate = false;
            bool shouldDisableChannelButtons = isDepthOnlyRT || isClearAction || isBackBuffer;
            UInt32 componentCount = GraphicsFormatUtility.GetComponentCount((GraphicsFormat)curEventData.rtFormat);
            GUILayout.BeginHorizontal();
            {
                GUI.enabled = !shouldDisableChannelButtons && m_SelectedColorChannel != 0;
                if (GUILayout.Button(FrameDebuggerStyles.EventToolbar.channelAll, FrameDebuggerStyles.EventToolbar.channelAllStyle)) { m_RTSelectedChannel = 0; }

                GUI.enabled = !shouldDisableChannelButtons && componentCount > 0 && m_SelectedColorChannel != 1;
                if (GUILayout.Button(FrameDebuggerStyles.EventToolbar.channelR, FrameDebuggerStyles.EventToolbar.channelStyle)) { m_RTSelectedChannel = 1; }

                GUI.enabled = !shouldDisableChannelButtons && componentCount > 1 && m_SelectedColorChannel != 2;
                if (GUILayout.Button(FrameDebuggerStyles.EventToolbar.channelG, FrameDebuggerStyles.EventToolbar.channelStyle)) { m_RTSelectedChannel = 2; }

                GUI.enabled = !shouldDisableChannelButtons && componentCount > 2 && m_SelectedColorChannel != 3;
                if (GUILayout.Button(FrameDebuggerStyles.EventToolbar.channelB, FrameDebuggerStyles.EventToolbar.channelStyle)) { m_RTSelectedChannel = 3; }

                GUI.enabled = !shouldDisableChannelButtons && componentCount > 3 && m_SelectedColorChannel != 4;
                if (GUILayout.Button(FrameDebuggerStyles.EventToolbar.channelA, FrameDebuggerStyles.EventToolbar.channelAStyle)) { m_RTSelectedChannel = 4; }

                // Force the channel to be "All" when:
                // * Showing the back buffer
                // * Showing Shadows/Depth/Clear
                // * Channel index is higher then the number available channels
                bool shouldForceAll = isBackBuffer || (m_RTSelectedChannel != 0 && (shouldDisableChannelButtons || m_RTSelectedChannel < 4 && componentCount < m_RTSelectedChannel));
                channelToDisplay = shouldForceAll ? 0 : m_RTSelectedChannel;

                if (channelToDisplay != m_SelectedColorChannel)
                {
                    forceUpdate = true;
                    m_SelectedColorChannel = channelToDisplay;
                }

                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();

            GUI.enabled = !isBackBuffer;

            // levels
            GUILayout.BeginHorizontal(FrameDebuggerStyles.EventToolbar.levelsHorizontalStyle);
            GUILayout.Label(FrameDebuggerStyles.EventToolbar.levelsHeader);

            EditorGUILayout.MinMaxSlider(ref m_RTBlackLevel, ref m_RTWhiteLevel, 0.0f, 1.0f, GUILayout.MaxWidth(200.0f));

            int rtIndexToSet = m_RTIndex;
            if (hasShowableDepth && rtIndexToSet == (showableRTCount - 1))
                rtIndexToSet = -1;

            if (EditorGUI.EndChangeCheck() || rtIndexToSet != m_RTIndexLastSet || forceUpdate)
            {
                m_SelectedMask = Vector4.zero;
                switch (channelToDisplay)
                {
                    case 1: m_SelectedMask.x = 1f; break;
                    case 2: m_SelectedMask.y = 1f; break;
                    case 3: m_SelectedMask.z = 1f; break;
                    case 4: m_SelectedMask.w = 1f; break;
                    case 5: m_SelectedMask = Vector4.zero; break;
                    default: m_SelectedMask = Vector4.one; break;
                }

                FrameDebuggerUtility.SetRenderTargetDisplayOptions(rtIndexToSet, m_SelectedMask, m_RTBlackLevel, m_RTWhiteLevel);
                m_FrameDebugger.RepaintAllNeededThings();
                m_RTIndexLastSet = rtIndexToSet;
                m_ForceRebuildStrings = true;
            }

            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUI.enabled = true;
        }

        private void DrawOutputAndMesh(Rect rect, bool shouldDrawOutputAndMesh, bool isDebuggingEditor)
        {
            if (BeginFoldoutBox(0, shouldDrawOutputAndMesh, FrameDebuggerStyles.EventDetails.foldoutOutputOrMeshText, out float fadePercent))
            {
                if (shouldDrawOutputAndMesh)
                {
                    EditorGUILayout.BeginVertical();
                    {
                        float viewportWidth = Mathf.Max(850, rect.width) - 20f;
                        if (viewportWidth < FrameDebuggerStyles.EventDetails.k_MaxViewportWidth)
                            return;

                        float viewportHeightFaded = FrameDebuggerStyles.EventDetails.k_MaxViewportHeight * fadePercent;
                        float texWidth = viewportWidth;
                        float texHeight = viewportHeightFaded;
                        if (curEventData.rtOutput != null)
                        {
                            texWidth = curEventData.rtOutput.width;
                            texHeight = curEventData.rtOutput.height;

                            if (texWidth > viewportWidth)
                            {
                                float scale = viewportWidth / texWidth;
                                texWidth *= scale;
                                texHeight *= scale;
                            }

                            if (texHeight > FrameDebuggerStyles.EventDetails.k_MaxViewportHeight)
                            {
                                float scale = FrameDebuggerStyles.EventDetails.k_MaxViewportHeight / texHeight;
                                texWidth *= scale;
                                texHeight = FrameDebuggerStyles.EventDetails.k_MaxViewportHeight;
                            }
                        }

                        EditorGUILayout.BeginHorizontal();
                        {
                            m_OutputMeshTabs = (ShowAdditionalInfo)GUILayout.Toolbar((int)m_OutputMeshTabs, m_OutputMeshTabsGuiContents, FrameDebuggerStyles.EventDetails.outputMeshTabStyle);
                        }
                        EditorGUILayout.EndHorizontal();

                        if (m_OutputMeshTabs == 0)
                        {
                            DrawTargetTexture(viewportWidth, viewportHeightFaded, texWidth, texHeight, fadePercent, isDebuggingEditor);
                        }
                        else
                        {
                            DrawEventMesh(viewportWidth, viewportHeightFaded, texWidth);
                        }
                    }
                    GUILayout.EndVertical();
                }
            }
            EndFoldoutBox();
        }

        private void DrawTargetTexture(float viewportWidth, float viewportHeight, float texWidth, float texHeight, float fadePercent, bool isDebuggingEditor)
        {
            if (fadePercent < 1f)
                viewportHeight *= fadePercent;

            EditorGUILayout.BeginHorizontal(FrameDebuggerStyles.EventDetails.outputMeshTextureStyle);
            Rect previewRect = GUILayoutUtility.GetRect(viewportWidth, viewportHeight);
            Rect textureRect = new Rect(previewRect.x, previewRect.y, texWidth, texHeight);

            if (Event.current.type == EventType.Repaint && curEventData.rtOutput != null && previewRect.height > 1.0f)
            {
                if (m_TargetTextureMaterial == null)
                    m_TargetTextureMaterial = Resources.GetBuiltinResource<Material>("PerformanceTools/FrameDebuggerRenderTargetDisplay.mat");

                GraphicsFormat targetTextureFormat = (GraphicsFormat)curEventData.rtFormat;

                uint componentCount = GraphicsFormatUtility.GetComponentCount(targetTextureFormat);
                m_TargetTextureMaterial.SetVector(ShaderPropertyIDs._Channels, (componentCount == 1) ? new Vector4(1, 0, 0, 0) : m_SelectedMask);

                bool linearColorSpace = QualitySettings.activeColorSpace == ColorSpace.Linear;
                bool textureSRGB = GraphicsFormatUtility.IsSRGBFormat(targetTextureFormat);
                float undoOutputSRGB = (isDebuggingEditor && (!linearColorSpace || textureSRGB)) ? 0.0f : 1.0f;
                m_TargetTextureMaterial.SetFloat(ShaderPropertyIDs._UndoOutputSRGB, undoOutputSRGB);

                if (curEventData.rtIsBackBuffer && isDebuggingEditor)
                {
                    m_TargetTextureMaterial.SetVector(ShaderPropertyIDs._Levels, new Vector4(0f, 1f, 0f, 0f));
                    m_TargetTextureMaterial.SetFloat(ShaderPropertyIDs._ShouldYFlip, 1f);
                }
                else
                {
                    m_TargetTextureMaterial.SetVector(ShaderPropertyIDs._Levels, new Vector4(m_RTBlackLevel, m_RTWhiteLevel, 0f, 0f));
                    m_TargetTextureMaterial.SetFloat(ShaderPropertyIDs._ShouldYFlip, 0f);
                }

                m_TargetTextureMaterial.SetTexture(ShaderPropertyIDs._MainTex, curEventData.rtOutput);
                m_TargetTextureMaterial.SetPass(0);

                if (viewportWidth > texWidth)
                    textureRect.x += (viewportWidth - texWidth) * 0.5f;

                if (viewportHeight > texHeight)
                    textureRect.y += (viewportHeight - texHeight) * 0.5f;

                // Remember currently active render texture
                RenderTexture currentActiveRT = RenderTexture.active;

                // Blit to the Render Texture
                RenderTexture targetRenderTexture = RenderTexture.GetTemporary((int)texWidth, (int)previewRect.height);
                Graphics.Blit(null, targetRenderTexture, m_TargetTextureMaterial, 0);

                // Restore previously active render texture
                RenderTexture.active = currentActiveRT;

                // Draw the texture to the screen
                GUI.DrawTexture(textureRect, targetRenderTexture, ScaleMode.ScaleAndCrop, false, (texWidth / texHeight));

                // Release
                RenderTexture.ReleaseTemporary(targetRenderTexture);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEventMesh(float viewportWidth, float viewportHeight, float texWidth)
        {
            Mesh mesh = curEventData.mesh;
            if (viewportHeight - FrameDebuggerStyles.EventDetails.k_MeshBottomToolbarHeight < 1.0f)
                return;

            if (mesh == null)
            {
                // Draw the background
                EditorGUILayout.BeginHorizontal(FrameDebuggerStyles.EventDetails.outputMeshTextureStyle, GUILayout.Width(viewportWidth));
                GUILayoutUtility.GetRect(viewportWidth, viewportHeight);
                EditorGUILayout.EndHorizontal();
                return;
            }

            if (m_Preview == null)
                m_Preview = new MeshPreview(mesh);
            else
                m_Preview.mesh = mesh;

            // We need this rect called here to push the control buttons below the Mesh...
            Rect previewRect = GUILayoutUtility.GetRect(viewportWidth, viewportHeight - FrameDebuggerStyles.EventDetails.k_MeshBottomToolbarHeight, GUILayout.ExpandHeight(false));

            // Rectangle for the buttons...
            Rect rect = EditorGUILayout.BeginHorizontal(GUIContent.none, FrameDebuggerStyles.EventDetails.meshPreToolbarStyle, GUILayout.Height(FrameDebuggerStyles.EventDetails.k_MeshBottomToolbarHeight));
            {
                GUILayout.FlexibleSpace();

                GUIContent meshName = new GUIContent(mesh.name);
                float meshNameWidth = EditorStyles.label.CalcSize(meshName).x + 10f;
                Rect meshNameRect = EditorGUILayout.GetControlRect(GUILayout.Width(meshNameWidth));
                meshNameRect.y -= 1;
                meshNameRect.x = 10;

                GUI.Label(meshNameRect, meshName, FrameDebuggerStyles.EventDetails.meshPreToolbarLabelStyle);

                if (FrameDebuggerHelper.IsCurrentEventMouseDown() && FrameDebuggerHelper.IsClickingRect(meshNameRect))
                {
                    EditorGUIUtility.PingObject(mesh);
                    Event.current.Use();
                }

                m_Preview.OnPreviewSettings();
            }
            EditorGUILayout.EndHorizontal();

            m_Preview?.OnPreviewGUI(previewRect, EditorStyles.helpBox);
        }

        private void DrawEventDetails(Rect rect)
        {
            if (BeginFoldoutBox(1, true, FrameDebuggerStyles.EventDetails.foldoutEventDetailsText, out float fadePercent))
            {
                // Render Target...
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.BeginVertical(FrameDebuggerStyles.EventDetails.verticalLabelStyle);
                    {
                        EditorGUILayout.LabelField("RenderTarget", FrameDebuggerStyles.EventDetails.labelStyle);
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical();
                    {
                        if (m_LastEventData.isComputeEvent || m_LastEventData.isRayTracingEvent)
                            EditorGUILayout.LabelField(FrameDebuggerStyles.EventDetails.k_NotAvailable, FrameDebuggerStyles.EventDetails.labelStyle);
                        else
                            EditorGUILayout.LabelField(curEventData.rtName, FrameDebuggerStyles.EventDetails.labelStyle);
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();

                // Size, Color Actions, Blending, Z, Stencil...
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.BeginVertical(FrameDebuggerStyles.EventDetails.verticalLabelStyle);
                    {
                        EditorGUILayout.LabelField(m_LastEventData.detailsLabelsLeftColumn, FrameDebuggerStyles.EventDetails.labelStyle);
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical(FrameDebuggerStyles.EventDetails.verticalValueStyle);
                    {
                        EditorGUILayout.LabelField(m_LastEventData.detailsValuesLeftColumn, FrameDebuggerStyles.EventDetails.labelStyle);
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical(FrameDebuggerStyles.EventDetails.verticalLabelStyle);
                    {
                        EditorGUILayout.LabelField(m_LastEventData.detailLabelsRightColumn, FrameDebuggerStyles.EventDetails.labelStyle);
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical(FrameDebuggerStyles.EventDetails.verticalValueStyle);
                    {
                        EditorGUILayout.LabelField(m_LastEventData.detailValuesRightColumn, FrameDebuggerStyles.EventDetails.labelStyle);
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();

                // Meshes
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.BeginVertical(FrameDebuggerStyles.EventDetails.verticalLabelStyle);
                    {
                        EditorGUILayout.LabelField(m_LastEventData.meshTitle, FrameDebuggerStyles.EventDetails.labelStyle);
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical(FrameDebuggerStyles.EventDetails.verticalLabelStyle);
                    {
                        if (m_LastEventData.listOfMeshesString == null)
                        {
                            EditorGUILayout.LabelField(m_LastEventData.firstMeshName, FrameDebuggerStyles.EventDetails.labelStyle);
                        }
                        else
                        {
                            m_ShouldShowMeshListFoldout = EditorGUILayout.Foldout(m_ShouldShowMeshListFoldout, m_LastEventData.firstMeshName + ((m_ShouldShowMeshListFoldout) ? string.Empty : "..."));
                            if (m_ShouldShowMeshListFoldout)
                            {
                                EditorGUILayout.LabelField(m_LastEventData.listOfMeshesString, FrameDebuggerStyles.EventDetails.labelStyle);
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();

                // Batch cause
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.BeginVertical(FrameDebuggerStyles.EventDetails.verticalLabelStyle);
                    {
                        EditorGUILayout.LabelField(FrameDebuggerStyles.EventDetails.batchCauseText);
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical();
                    {
                        EditorGUILayout.LabelField(FrameDebuggerStyles.EventDetails.batchBreakCauses[curEventData.batchBreakCause], FrameDebuggerStyles.EventDetails.labelStyle);
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();

                // Shader
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.BeginVertical(FrameDebuggerStyles.EventDetails.verticalLabelStyle);
                    {
                        EditorGUILayout.LabelField(FrameDebuggerStyles.EventDetails.passLightModeText, FrameDebuggerStyles.EventDetails.labelStyle);
                        EditorGUILayout.LabelField(FrameDebuggerStyles.EventDetails.shaderText, FrameDebuggerStyles.EventDetails.labelStyle);
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical();
                    {
                        EditorGUILayout.LabelField($"{m_LastEventData.passAndLightMode}", FrameDebuggerStyles.EventDetails.labelStyle);
                        if (m_LastEventData.shader != null)
                        {
                            GUI.enabled = false;
                            EditorGUILayout.ObjectField(m_LastEventData.shader, typeof(Shader), true);
                            GUI.enabled = true;
                        }
                        else
                        {
                            EditorGUILayout.LabelField(m_LastEventData.shaderName);
                        }

                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }
            EndFoldoutBox();
        }

        private void DrawKeywords(bool shouldDisplayProperties)
        {
            bool hasKeywords = m_LastEventData.keywords.Length != 0;
            if (BeginFoldoutBox(2, shouldDisplayProperties && hasKeywords, FrameDebuggerStyles.EventDetails.foldoutKeywordsText, out float fadePercent))
            {
                GUILayout.BeginHorizontal(FrameDebuggerStyles.EventDetails.propertiesVerticalStyle);
                {
                    if (shouldDisplayProperties && hasKeywords)
                        GUILayout.Label(m_LastEventData.keywords, FrameDebuggerStyles.EventDetails.labelStyle);
                }
                GUILayout.EndHorizontal();
            }
            EndFoldoutBox();
        }

        private void DrawProperties(bool shouldDisplayProperties)
        {
            ShaderProperties props = curEventData.shaderProperties;

            Profiler.BeginSample("DrawTextureProperties");
            {
                DrawTextureProperties(3, shouldDisplayProperties && props.textures.Length != 0, props.textures);
            }
            Profiler.EndSample();

            Profiler.BeginSample("DrawIntProperties");
            {
                DrawIntProperties(4, shouldDisplayProperties && props.ints.Length != 0, props.ints);
            }
            Profiler.EndSample();
            Profiler.BeginSample("DrawFloatProperties");
            {
                DrawFloatProperties(5, shouldDisplayProperties && props.floats.Length != 0, props.floats);
            }
            Profiler.EndSample();
            Profiler.BeginSample("DrawVectorProperties");
            {
                DrawVectorProperties(6, shouldDisplayProperties && props.vectors.Length != 0, props.vectors);
            }
            Profiler.EndSample();
            Profiler.BeginSample("DrawMatrixProperties");
            {
                DrawMatrixProperties(7, shouldDisplayProperties && props.matrices.Length != 0, props.matrices);
            }
            Profiler.EndSample();
            Profiler.BeginSample("DrawBufferProperties");
            {
                DrawBufferProperties(8, shouldDisplayProperties && props.buffers.Length != 0, props.buffers);
            }
            Profiler.EndSample();
            Profiler.BeginSample("DrawConstantBufferProperties");
            {
                DrawConstantBufferProperties(9, shouldDisplayProperties && props.cbuffers.Length != 0, props.cbuffers);
            }
            Profiler.EndSample();
        }

        private void DrawTextureProperties(int num, bool shouldDrawProperties, ShaderTextureInfo[] textures)
        {
            if (BeginFoldoutBox(num, shouldDrawProperties, FrameDebuggerStyles.EventDetails.foldoutTexturesText, out float fadePercent))
            {
                GUILayout.BeginVertical(FrameDebuggerStyles.EventDetails.propertiesVerticalStyle);
                {
                    int numOfTextures = shouldDrawProperties ? textures.Length : 0;
                    for (int i = 0; i < numOfTextures; i++)
                    {
                        ShaderTextureInfo t = textures[i];

                        GUILayout.BeginHorizontal();
                        {
                            Event evt = Event.current;

                            // Parameter name..
                            DrawPropName(t.name);

                            // Vertex/Fragment/Geometry/Hull..
                            DrawShaderPropertyFlags(t.flags);

                            Texture texture = t.value;
                            if (texture != null)
                            {
                                // Texture Preview..
                                // for 2D textures, we want to display them directly as a preview (this will make render textures display their contents) but0
                                // for cube maps and other non-2D types DrawPreview does not do anything useful right now, so get their asset type icon at least
                                bool isTex2D = texture.dimension == TextureDimension.Tex2D;
                                Texture previewTexture = isTex2D ? texture : AssetPreview.GetMiniThumbnail(texture);

                                Rect previewRect = GUILayoutUtility.GetRect(new GUIContent(previewTexture), FrameDebuggerStyles.EventDetails.textureButtonStyle);
                                previewRect.x += 1f;
                                previewRect.y += 4f;
                                GUI.DrawTexture(previewRect, previewTexture, ScaleMode.StretchToFill, false);

                                if (FrameDebuggerHelper.IsCurrentEventMouseDown() && FrameDebuggerHelper.IsClickingRect(previewRect))
                                {
                                    PopupWindowWithoutFocus.Show(
                                        previewRect,
                                        new ObjectPreviewPopup(previewTexture),
                                        new[] { PopupLocation.Left, PopupLocation.Below, PopupLocation.Right }
                                    );
                                }

                                // Dimensions: Tex2D, Tex3D. etc...
                                GUILayout.Label($"{texture.dimension}", FrameDebuggerStyles.EventDetails.textureDimensionsStyle);

                                // Texture Size...
                                GUILayout.Label($"{texture.width}x{texture.height}", FrameDebuggerStyles.EventDetails.textureSizeStyle);

                                // Texture format...
                                GUILayout.Label(GetGUIContent(FrameDebuggerHelper.GetFormat(texture), FrameDebuggerStyles.EventDetails.k_TextureFormatMaxChars), FrameDebuggerStyles.EventDetails.textureFormatStyle);

                                // Texture name...
                                // Disable the GUI to prevent users from assigning textures to the field
                                GUI.enabled = false;
                                EditorGUILayout.ObjectField(texture, typeof(Texture), true);
                                GUI.enabled = true;
                            }
                            else
                            {
                                EditorGUILayout.LabelField(t.textureName, FrameDebuggerStyles.EventDetails.labelStyle);
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndVertical();
            }
            EndFoldoutBox();
        }

        private void DrawIntProperties(int num, bool shouldDrawProperties, ShaderIntInfo[] ints)
        {
            if (BeginFoldoutBox(num, shouldDrawProperties, FrameDebuggerStyles.EventDetails.foldoutIntsText, out float fadePercent))
            {
                GUILayout.BeginVertical(FrameDebuggerStyles.EventDetails.propertiesVerticalStyle);
                {
                    int numOfIntegers = shouldDrawProperties ? ints.Length : 0;
                    for (int i = 0; i < numOfIntegers;)
                    {
                        ShaderIntInfo t = ints[i];
                        int numValues = (ints[i].flags >> k_ShaderTypeBits) & k_ArraySizeBitMask;
                        if (numValues == 0)
                            break;

                        GUILayout.BeginHorizontal();
                        if (numValues == 1)
                        {
                            DrawPropName(t.name);
                            DrawShaderPropertyFlags(t.flags);
                            GUILayout.Label(t.value.ToString(FrameDebuggerStyles.EventDetails.k_IntFormat, CultureInfo.InvariantCulture.NumberFormat), FrameDebuggerStyles.EventDetails.labelStyle);
                            ShaderPropertyCopyValueMenu(GUILayoutUtility.GetLastRect(), t.value);
                        }
                        else
                        {
                            DrawPropName($"{t.name} [{numValues}]");
                            DrawShaderPropertyFlags(t.flags);

                            Rect buttonRect = GUILayoutUtility.GetRect(FrameDebuggerStyles.EventDetails.arrayPopupButtonText, GUI.skin.button);
                            buttonRect.width = FrameDebuggerStyles.EventDetails.k_ArrayValuePopupBtnWidth;
                            if (GUI.Button(buttonRect, FrameDebuggerStyles.EventDetails.arrayPopupButtonText))
                            {
                                FrameDebuggerStyles.ArrayValuePopup.GetValueStringDelegate getValueString =
                                    (int index, bool highPrecision) => ints[index].value.ToString(FrameDebuggerStyles.EventDetails.k_IntFormat, CultureInfo.InvariantCulture.NumberFormat);

                                PopupWindowWithoutFocus.Show(
                                    buttonRect,
                                    new FrameDebuggerStyles.ArrayValuePopup(i, numValues, 1, 100.0f, getValueString),
                                    new[] { PopupLocation.Left, PopupLocation.Below, PopupLocation.Right }
                                );
                            }
                        }

                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        i += numValues;
                    }
                }
                GUILayout.EndVertical();
            }
            EndFoldoutBox();
        }

        private void DrawFloatProperties(int num, bool shouldDrawProperties, ShaderFloatInfo[] floats)
        {
            if (BeginFoldoutBox(num, shouldDrawProperties, FrameDebuggerStyles.EventDetails.foldoutFloatsText, out float fadePercent))
            {
                GUILayout.BeginVertical(FrameDebuggerStyles.EventDetails.propertiesVerticalStyle);
                {
                    int numOfFloats = shouldDrawProperties ? floats.Length : 0;
                    for (int i = 0; i < numOfFloats;)
                    {
                        ShaderFloatInfo t = floats[i];
                        int numValues = (floats[i].flags >> k_ShaderTypeBits) & k_ArraySizeBitMask;
                        if (numValues == 0)
                            break;

                        GUILayout.BeginHorizontal();
                        if (numValues == 1)
                        {
                            DrawPropName(t.name);
                            DrawShaderPropertyFlags(t.flags);
                            GUILayout.Label(t.value.ToString(FrameDebuggerStyles.EventDetails.k_FloatFormat, CultureInfo.InvariantCulture.NumberFormat), FrameDebuggerStyles.EventDetails.labelStyle);
                            ShaderPropertyCopyValueMenu(GUILayoutUtility.GetLastRect(), t.value);
                        }
                        else
                        {
                            string arrayName = $"{t.name} [{numValues}]";
                            DrawPropName(arrayName);
                            DrawShaderPropertyFlags(t.flags);

                            Rect buttonRect = GUILayoutUtility.GetRect(FrameDebuggerStyles.EventDetails.arrayPopupButtonText, GUI.skin.button);
                            buttonRect.width = FrameDebuggerStyles.EventDetails.k_ArrayValuePopupBtnWidth;
                            if (GUI.Button(buttonRect, FrameDebuggerStyles.EventDetails.arrayPopupButtonText))
                            {
                                FrameDebuggerStyles.ArrayValuePopup.GetValueStringDelegate getValueString =
                                    (int index, bool highPrecision) => floats[index].value.ToString(FrameDebuggerStyles.EventDetails.k_FloatFormat, CultureInfo.InvariantCulture.NumberFormat);

                                PopupWindowWithoutFocus.Show(
                                    buttonRect,
                                    new FrameDebuggerStyles.ArrayValuePopup(i, numValues, 1, 100.0f, getValueString),
                                    new[] { PopupLocation.Left, PopupLocation.Below, PopupLocation.Right });
                            }
                        }
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        i += numValues;
                    }
                }
                GUILayout.EndVertical();
            }
            EndFoldoutBox();
        }

        private void DrawVectorProperties(int num, bool shouldDrawProperties, ShaderVectorInfo[] vectors)
        {
            if (BeginFoldoutBox(num, shouldDrawProperties, FrameDebuggerStyles.EventDetails.foldoutVectorsText, out float fadePercent))
            {
                GUILayout.BeginVertical(FrameDebuggerStyles.EventDetails.propertiesVerticalStyle);
                {
                    int numOfVectors = shouldDrawProperties ? vectors.Length : 0;
                    for (int i = 0; i < numOfVectors;)
                    {
                        ShaderVectorInfo t = vectors[i];
                        int numValues = (vectors[i].flags >> k_ShaderTypeBits) & k_ArraySizeBitMask;
                        if (numValues == 0)
                            break;

                        GUILayout.BeginHorizontal();
                        if (numValues == 1)
                        {
                            DrawPropName(t.name);
                            DrawShaderPropertyFlags(t.flags);
                            GUILayout.Label(t.value.ToString(FrameDebuggerStyles.EventDetails.k_FloatFormat), FrameDebuggerStyles.EventDetails.labelStyle);
                            ShaderPropertyCopyValueMenu(GUILayoutUtility.GetLastRect(), t.value);
                        }
                        else
                        {
                            DrawPropName($"{t.name} [{numValues}]");
                            DrawShaderPropertyFlags(t.flags);

                            Rect buttonRect = GUILayoutUtility.GetRect(FrameDebuggerStyles.EventDetails.arrayPopupButtonText, GUI.skin.button);
                            if (GUI.Button(buttonRect, FrameDebuggerStyles.EventDetails.arrayPopupButtonText))
                            {
                                FrameDebuggerStyles.ArrayValuePopup.GetValueStringDelegate getValueString =
                                    (int index, bool highPrecision) => vectors[index].value.ToString(highPrecision ? FrameDebuggerStyles.EventDetails.k_FloatFormat : FrameDebuggerStyles.EventDetails.k_FloatFormat);

                                PopupWindowWithoutFocus.Show(
                                    buttonRect,
                                    new FrameDebuggerStyles.ArrayValuePopup(i, numValues, 1, 200.0f, getValueString),
                                    new[] { PopupLocation.Left, PopupLocation.Below, PopupLocation.Right });
                            }
                        }
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        i += numValues;
                    }
                }
                GUILayout.EndVertical();
            }
            EndFoldoutBox();
        }

        private void DrawMatrixProperties(int num, bool shouldDrawProperties, ShaderMatrixInfo[] matrices)
        {
            if (BeginFoldoutBox(num, shouldDrawProperties, FrameDebuggerStyles.EventDetails.foldoutMatricesText, out float fadePercent))
            {
                GUILayout.BeginVertical(FrameDebuggerStyles.EventDetails.propertiesVerticalStyle);
                {
                    int numOfMatrices = shouldDrawProperties ? matrices.Length : 0;
                    for (int i = 0; i < numOfMatrices;)
                    {
                        ShaderMatrixInfo t = matrices[i];
                        int numValues = (matrices[i].flags >> k_ShaderTypeBits) & k_ArraySizeBitMask;
                        if (numValues == 0)
                            break;

                        GUILayout.BeginHorizontal();
                        if (numValues == 1)
                        {
                            DrawPropName(t.name);
                            DrawShaderPropertyFlags(t.flags);
                            GUILayout.Label(t.value.ToString(FrameDebuggerStyles.EventDetails.k_FloatFormat), FrameDebuggerStyles.EventDetails.labelStyle);
                            ShaderPropertyCopyValueMenu(GUILayoutUtility.GetLastRect(), t.value);
                        }
                        else
                        {
                            DrawPropName($"{t.name} [{numValues}]");
                            DrawShaderPropertyFlags(t.flags);

                            Rect buttonRect = GUILayoutUtility.GetRect(FrameDebuggerStyles.EventDetails.arrayPopupButtonText, GUI.skin.button);
                            if (GUI.Button(buttonRect, FrameDebuggerStyles.EventDetails.arrayPopupButtonText))
                            {
                                FrameDebuggerStyles.ArrayValuePopup.GetValueStringDelegate getValueString =
                                    (int index, bool highPrecision) => '\n' + matrices[index].value.ToString(FrameDebuggerStyles.EventDetails.k_FloatFormat);

                                PopupWindowWithoutFocus.Show(
                                    buttonRect,
                                    new FrameDebuggerStyles.ArrayValuePopup(i, numValues, 5, 200.0f, getValueString),
                                    new[] { PopupLocation.Left, PopupLocation.Below, PopupLocation.Right }
                                );
                            }
                        }
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        i += numValues;
                    }
                }
                GUILayout.EndVertical();
            }
            EndFoldoutBox();
        }

        private void DrawBufferProperties(int num, bool shouldDrawProperties, ShaderBufferInfo[] buffers)
        {
            if (BeginFoldoutBox(num, shouldDrawProperties, FrameDebuggerStyles.EventDetails.foldoutBuffersText, out float fadePercent))
            {
                GUILayout.BeginVertical(FrameDebuggerStyles.EventDetails.propertiesVerticalStyle);
                {
                    if (shouldDrawProperties)
                    {
                        foreach (var t in buffers)
                        {
                            GUILayout.BeginHorizontal();

                            DrawPropName(t.name);
                            DrawShaderPropertyFlags(t.flags);

                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                        }
                    }
                }
                GUILayout.EndVertical();
            }
            EndFoldoutBox();
        }

        private void DrawConstantBufferProperties(int num, bool shouldDrawProperties, ShaderConstantBufferInfo[] cbuffers)
        {
            if (BeginFoldoutBox(num, shouldDrawProperties, FrameDebuggerStyles.EventDetails.foldoutCBufferText, out float fadePercent))
            {
                GUILayout.BeginVertical(FrameDebuggerStyles.EventDetails.propertiesVerticalStyle);
                {
                    if (shouldDrawProperties)
                    {
                        foreach (var t in cbuffers)
                        {
                            GUILayout.BeginHorizontal();

                            DrawPropName(t.name);
                            DrawShaderPropertyFlags(t.flags);

                            GUILayout.FlexibleSpace();
                            GUILayout.EndHorizontal();
                        }
                    }
                }
                GUILayout.EndVertical();
            }
            EndFoldoutBox();
        }

        private void DrawShaderPropertyFlags(int flags)
        {
            m_TempSB1.Clear();
            m_TempSB2.Clear();

            // Lowest bits of flags are set for each shader stage that property is used in; matching ShaderType C++ enum
            const int k_VertexShaderFlag = (1 << 1);
            const int k_FragmentShaderFlag = (1 << 2);
            const int k_GeometryShaderFlag = (1 << 3);
            const int k_HullShaderFlag = (1 << 4);
            const int k_DomainShaderFlag = (1 << 5);

            int shaderCounter = 0;
            if ((flags & k_VertexShaderFlag) != 0) { shaderCounter++; m_TempSB1.Append("v/"); m_TempSB2.Append("vertex & "); }
            if ((flags & k_FragmentShaderFlag) != 0) { shaderCounter++; m_TempSB1.Append("f/"); m_TempSB2.Append("fragment & "); }
            if ((flags & k_GeometryShaderFlag) != 0) { shaderCounter++; m_TempSB1.Append("g/"); m_TempSB2.Append("geometry & "); }
            if ((flags & k_HullShaderFlag) != 0) { shaderCounter++; m_TempSB1.Append("h/"); m_TempSB2.Append("hull & "); }
            if ((flags & k_DomainShaderFlag) != 0) { shaderCounter++; m_TempSB1.Append("d/"); m_TempSB2.Append("domain & "); }

            if (shaderCounter > 0)
            {
                m_TempSB1.Remove(m_TempSB1.Length - 1, 1); // Remove the last /
                m_TempSB2.Remove(m_TempSB2.Length - 3, 2); // Remove the last &
                m_TempSB2.Insert(0, "Used in ");
                m_TempSB2.Append((shaderCounter > 1) ? "shaders" : "shader");
            }

            GUILayout.Label(EditorGUIUtility.TrTextContent(m_TempSB1.ToString(), m_TempSB2.ToString()), FrameDebuggerStyles.EventDetails.propertiesFlagsStyle);
        }

        private void ShaderPropertyCopyValueMenu(Rect valueRect, System.Object value)
        {
            var e = Event.current;
            if (e.type == EventType.ContextClick && valueRect.Contains(e.mousePosition))
            {
                e.Use();
                GenericMenu menu = new GenericMenu();
                menu.AddItem(FrameDebuggerStyles.EventDetails.copyValueText, false, delegate
                {
                    if (value is Vector4)
                    {
                        EditorGUIUtility.systemCopyBuffer = ((Vector4)value).ToString(FrameDebuggerStyles.EventDetails.k_FloatFormat);
                    }
                    else if (value is Matrix4x4)
                    {
                        EditorGUIUtility.systemCopyBuffer = ((Matrix4x4)value).ToString(FrameDebuggerStyles.EventDetails.k_FloatFormat);
                    }
                    else if (value is System.Single)
                    {
                        EditorGUIUtility.systemCopyBuffer = ((System.Single)value).ToString(FrameDebuggerStyles.EventDetails.k_FloatFormat);
                    }
                    else
                    {
                        EditorGUIUtility.systemCopyBuffer = $"{value}";
                    }
                });
                menu.ShowAsContext();
            }
        }

        private GUIContent GetGUIContent(string text, int maxLength)
        {
            string fullName = text;
            const int k_NumberOfDots = 3;

            // If we need to shorten the name, we will add the full name as a tooltip
            if (text.Length > maxLength - k_NumberOfDots)
            {
                string shortName = fullName.Substring(0, maxLength - k_NumberOfDots) + "...";
                return EditorGUIUtility.TrTextContent(shortName, fullName);
            }

            return EditorGUIUtility.TrTextContent(fullName);
        }

        private void DrawPropName(string name)
        {
            GUILayout.Label(GetGUIContent(name, FrameDebuggerStyles.EventDetails.k_PropertyNameMaxChars), FrameDebuggerStyles.EventDetails.propertiesNameStyle);
        }

        private void BuildCurEventDataStrings(FrameDebuggerEvent curEvent, FrameDebuggerEventData curEventData)
        {
            m_LastEventData.index = FrameDebuggerUtility.limit - 1;
            m_LastEventData.type = curEvent.type;
            int eventTypeInt = (int)m_LastEventData.type;

            // Figure out the type of event we have
            m_LastEventData.isClearEvent = FrameDebuggerHelper.IsAClearEvent(m_LastEventData.type);
            m_LastEventData.isResolveEvent = FrameDebuggerHelper.IsAResolveEvent(m_LastEventData.type);
            m_LastEventData.isComputeEvent = FrameDebuggerHelper.IsAComputeEvent(m_LastEventData.type);
            m_LastEventData.isRayTracingEvent = FrameDebuggerHelper.IsARayTracingEvent(m_LastEventData.type);

            // Shader Pass name & LightMode tag
            GetShaderData();
            string pass = $"{(string.IsNullOrEmpty(curEventData.passName) ? FrameDebuggerStyles.EventDetails.k_NotAvailable : curEventData.passName)} ({curEventData.shaderPassIndex})";
            string lightMode = $"{(string.IsNullOrEmpty(curEventData.passLightMode) ? FrameDebuggerStyles.EventDetails.k_NotAvailable : curEventData.passLightMode)}";
            m_LastEventData.passAndLightMode = $"{pass}\n{lightMode}";

            // Event title
            var eventObj = FrameDebuggerUtility.GetFrameEventObject(m_LastEventData.index);
            if (eventObj)
                m_LastEventData.title = $"Event #{m_LastEventData.index + 1} {FrameDebuggerStyles.frameEventTypeNames[eventTypeInt]} {eventObj.name}";
            else
                m_LastEventData.title = $"Event #{m_LastEventData.index + 1} {FrameDebuggerStyles.frameEventTypeNames[eventTypeInt]}";

            m_TempSB1.Clear();
            m_TempSB2.Clear();


            if (m_LastEventData.isComputeEvent || m_LastEventData.isRayTracingEvent)
            {
                m_TempSB1.AppendLine("Size");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine("Format");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine("Color Actions");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine("Depth Actions");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);
            }
            else
            {
                m_TempSB1.AppendLine("Size");
                m_TempSB2.AppendLine($"{curEventData.rtWidth}x{curEventData.rtHeight}");

                m_TempSB1.AppendLine("Format");
                m_TempSB2.AppendLine($"{(GraphicsFormat)curEventData.rtFormat}");

                m_TempSB1.AppendLine("Color Actions");
                m_TempSB2.AppendLine((curEventData.rtLoadAction == -1) ? FrameDebuggerStyles.EventDetails.k_NotAvailable : $"{(RenderBufferLoadAction)curEventData.rtLoadAction} / {(RenderBufferStoreAction)curEventData.rtStoreAction}");

                m_TempSB1.AppendLine("Depth Actions");
                m_TempSB2.AppendLine((curEventData.rtDepthLoadAction == -1) ? FrameDebuggerStyles.EventDetails.k_NotAvailable : $"{(RenderBufferLoadAction)curEventData.rtDepthLoadAction} / {(RenderBufferStoreAction)curEventData.rtDepthStoreAction}");
            }

            m_LastEventData.isValid = true;
            if (m_LastEventData.isComputeEvent)
            {
                m_TempSB1.AppendLine();
                m_TempSB2.AppendLine();

                m_TempSB1.AppendLine("Kernel");
                m_TempSB2.AppendLine(curEventData.csKernel);

                m_TempSB1.AppendLine("Thread Groups");
                if (curEventData.csThreadGroupsX != 0 || curEventData.csThreadGroupsY != 0 || curEventData.csThreadGroupsZ != 0)
                    m_TempSB2.AppendLine($"{curEventData.csThreadGroupsX}x{curEventData.csThreadGroupsY}x{curEventData.csThreadGroupsZ}");
                else
                    m_TempSB2.AppendLine("Indirect dispatch");

                m_TempSB1.AppendLine("Thread Group Size");
                if (curEventData.csGroupSizeX > 0)
                    m_TempSB2.AppendLine($"{curEventData.csGroupSizeX}x{curEventData.csGroupSizeY}x{curEventData.csGroupSizeZ}");
                else
                    m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);
            }
            else if (m_LastEventData.isRayTracingEvent)
            {
                m_TempSB1.AppendLine();
                m_TempSB2.AppendLine();

                m_TempSB1.AppendLine("Ray Generation Shader");
                m_TempSB2.AppendLine(curEventData.rtsRayGenShaderName);

                m_TempSB1.AppendLine("SubShader Pass");
                m_TempSB2.AppendLine(curEventData.rtsShaderPassName);

                m_TempSB1.AppendLine("Acceleration Structure");
                if (curEventData.rtsAccelerationStructureName.Length > 0)
                    m_TempSB2.AppendLine(curEventData.rtsAccelerationStructureName);
                else
                    m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine("Dispatch Size");
                m_TempSB2.AppendLine($"{curEventData.rtsWidth} x {curEventData.rtsHeight} x {curEventData.rtsDepth}");

                m_TempSB1.AppendLine("Max. Recursion Depth");
                m_TempSB2.AppendLine($"{curEventData.rtsMaxRecursionDepth}");

                m_TempSB1.AppendLine("Miss Shader Count");
                m_TempSB2.AppendLine($"{curEventData.rtsMissShaderCount}");

                m_TempSB1.AppendLine("Callable Shader Count");
                m_TempSB2.AppendLine($"{curEventData.rtsCallableShaderCount}");
            }
            else if (m_LastEventData.isClearEvent || m_LastEventData.isResolveEvent)
            {
                m_LastEventData.passAndLightMode = $"{FrameDebuggerStyles.EventDetails.k_NotAvailable}\n{FrameDebuggerStyles.EventDetails.k_NotAvailable}";

                m_TempSB1.AppendLine("Memoryless");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine();
                m_TempSB2.AppendLine();

                // Colormask
                m_TempSB1.AppendLine("ColorMask");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                // Blend state
                m_TempSB1.AppendLine("Blend Color");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine("Blend Alpha ");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine("BlendOp Color");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine("BlendOp Alpha");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine();
                m_TempSB2.AppendLine();

                m_TempSB1.AppendLine("Draw Calls");
                m_TempSB2.AppendLine($"{curEventData.drawCallCount}");

                m_TempSB1.AppendLine("Vertices");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine("Indices");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine();
                m_TempSB2.AppendLine();

                int type = (int)curEvent.type;

                m_TempSB1.AppendLine("Clear Color");
                if ((type & 1) != 0 && !m_LastEventData.isResolveEvent)
                    m_TempSB2.AppendLine($"({curEventData.rtClearColorR:F3}, {curEventData.rtClearColorG:F3}, {curEventData.rtClearColorB:F3}, {curEventData.rtClearColorA:F3})");
                else
                    m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine("Clear Depth");
                if ((type & 2) != 0)
                    m_TempSB2.AppendLine(curEventData.clearDepth.ToString("f3"));
                else
                    m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine("Clear Stencil");
                if ((type & 4) != 0)
                    m_TempSB2.Append(FrameDebuggerHelper.GetStencilString((int)curEventData.clearStencil));
                else
                    m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);
            }
            else
            {
                m_TempSB1.AppendLine("Memoryless");
                m_TempSB2.AppendLine((curEventData.rtMemoryless != 0) ? "Yes" : "No");

                m_TempSB1.AppendLine();
                m_TempSB2.AppendLine();

                FrameDebuggerBlendState blendState = curEventData.blendState;

                // Colormask
                m_TempSB1.AppendLine("ColorMask");
                if (blendState.writeMask == 0)
                    m_TempSB2.AppendLine("0");
                else
                {
                    if ((blendState.writeMask & 8) != 0)
                        m_TempSB2.Append('R');

                    if ((blendState.writeMask & 4) != 0)
                        m_TempSB2.Append('G');

                    if ((blendState.writeMask & 2) != 0)
                        m_TempSB2.Append('B');

                    if ((blendState.writeMask & 1) != 0)
                        m_TempSB2.Append('A');

                    m_TempSB2.Append("\n");
                }

                // Blend state
                m_TempSB1.AppendLine("Blend Color");
                m_TempSB2.AppendLine($"{blendState.srcBlend} {blendState.dstBlend}");

                m_TempSB1.AppendLine("Blend Alpha ");
                m_TempSB2.AppendLine($"{blendState.srcBlendAlpha} {blendState.dstBlendAlpha}");

                m_TempSB1.AppendLine("BlendOp Color");
                m_TempSB2.AppendLine(blendState.blendOp.ToString());

                m_TempSB1.AppendLine("BlendOp Alpha");
                m_TempSB2.AppendLine(blendState.blendOpAlpha.ToString());

                m_TempSB1.AppendLine();
                m_TempSB2.AppendLine();

                if (curEventData.instanceCount > 1)
                {
                    m_TempSB1.AppendLine("DrawInstanced Calls");
                    m_TempSB2.AppendLine($"{curEventData.drawCallCount}");

                    m_TempSB1.AppendLine("Instances");
                    m_TempSB2.AppendLine($"{curEventData.instanceCount}");
                }
                else
                {
                    m_TempSB1.AppendLine("Draw Calls");
                    m_TempSB2.AppendLine($"{curEventData.drawCallCount}");
                }

                m_TempSB1.AppendLine("Vertices");
                m_TempSB2.AppendLine(curEventData.vertexCount.ToString());

                m_TempSB1.AppendLine("Indices");
                m_TempSB2.AppendLine(curEventData.indexCount.ToString());

                m_TempSB1.AppendLine();
                m_TempSB2.AppendLine();

                m_TempSB1.AppendLine("Clear Color");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine("Clear Depth");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine("Clear Stencil");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);
            }

            m_LastEventData.detailsLabelsLeftColumn = m_TempSB1.ToString();
            m_LastEventData.detailsValuesLeftColumn = m_TempSB2.ToString();

            m_TempSB1.Clear();
            m_TempSB2.Clear();

            m_LastEventData.firstMeshName = null;
            m_LastEventData.listOfMeshesString = null;
            if (curEventData.meshInstanceIDs != null && curEventData.meshInstanceIDs.Length > 0)
            {
                int numOfMeshesAdded = 0;
                m_LastEventData.meshTitle = curEventData.meshInstanceIDs.Length < 2 ? "Mesh" : "Meshes";
                for (int i = 0; i < curEventData.meshInstanceIDs.Length; i++)
                {
                    int id = curEventData.meshInstanceIDs[i];
                    Mesh mesh = EditorUtility.InstanceIDToObject(id) as Mesh;
                    if (mesh != null)
                    {
                        if (m_LastEventData.firstMeshName == null)
                            m_LastEventData.firstMeshName = mesh.name;
                        else
                            m_TempSB2.AppendLine("   " + mesh.name);
                        numOfMeshesAdded++;
                    }
                }

                // We keep the meshes string null if it's only one instance
                // and just show the firstMesh instead.
                if (numOfMeshesAdded > 1)
                {
                    m_LastEventData.listOfMeshesString = m_TempSB2.ToString();
                    m_TempSB2.Clear();
                }
            }
            else
            {
                m_LastEventData.meshTitle = "Mesh";
                m_LastEventData.firstMeshName = curEventData.mesh == null ? FrameDebuggerStyles.EventDetails.k_NotAvailable : curEventData.mesh.name;
            }

            if (m_LastEventData.isClearEvent
                || m_LastEventData.isResolveEvent
                || m_LastEventData.isComputeEvent
                || m_LastEventData.isRayTracingEvent)
            {
                // Depth state
                m_TempSB1.AppendLine("ZClip");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine("ZTest");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine("ZWrite");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine("Cull");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine("Conservative");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine("Offset");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine();
                m_TempSB2.AppendLine();

                // Stencil state
                m_TempSB1.AppendLine("Stencil");
                m_TempSB2.AppendLine("Disabled");

                m_TempSB1.AppendLine("Stencil Ref");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine("Stencil ReadMask");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine("Stencil WriteMask");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine("Stencil Comp");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine("Stencil Pass");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine("Stencil Fail");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                m_TempSB1.AppendLine("Stencil ZFail");
                m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);
            }
            else
            {
                FrameDebuggerRasterState rasterState = curEventData.rasterState;
                FrameDebuggerDepthState depthState = curEventData.depthState;

                // Depth state
                m_TempSB1.AppendLine("ZClip");
                m_TempSB2.AppendLine(rasterState.depthClip.ToString());

                m_TempSB1.AppendLine("ZTest");
                m_TempSB2.AppendLine(depthState.depthFunc.ToString());

                m_TempSB1.AppendLine("ZWrite");
                m_TempSB2.AppendLine(depthState.depthWrite == 0 ? "Off" : "On");

                m_TempSB1.AppendLine("Cull");
                m_TempSB2.AppendLine(rasterState.cullMode.ToString());

                m_TempSB1.AppendLine("Conservative");
                m_TempSB2.AppendLine(rasterState.conservative.ToString());

                m_TempSB1.AppendLine("Offset");
                m_TempSB2.AppendLine($"{rasterState.slopeScaledDepthBias}, {rasterState.depthBias}");

                m_TempSB1.AppendLine();
                m_TempSB2.AppendLine();

                // Stencil state
                if (curEventData.stencilState.stencilEnable)
                {
                    m_TempSB1.AppendLine("Stencil");
                    m_TempSB2.AppendLine("Enabled");

                    m_TempSB1.AppendLine("Stencil Ref");
                    m_TempSB2.AppendLine(FrameDebuggerHelper.GetStencilString(curEventData.stencilRef));

                    m_TempSB1.AppendLine("Stencil ReadMask");
                    m_TempSB2.AppendLine(curEventData.stencilState.readMask != 255 ? FrameDebuggerHelper.GetStencilString(curEventData.stencilState.readMask) : FrameDebuggerStyles.EventDetails.k_NotAvailable);

                    m_TempSB1.AppendLine("Stencil WriteMask");
                    m_TempSB2.AppendLine(curEventData.stencilState.writeMask != 255 ? FrameDebuggerHelper.GetStencilString(curEventData.stencilState.writeMask) : FrameDebuggerStyles.EventDetails.k_NotAvailable);

                    // Only show *Front states when CullMode is set to Back.
                    if (curEventData.rasterState.cullMode == CullMode.Back)
                    {
                        m_TempSB1.AppendLine("Stencil Comp");
                        m_TempSB2.AppendLine($"{curEventData.stencilState.stencilFuncFront}");
                        m_TempSB1.AppendLine("Stencil Pass");
                        m_TempSB2.AppendLine($"{curEventData.stencilState.stencilPassOpFront}");
                        m_TempSB1.AppendLine("Stencil Fail");
                        m_TempSB2.AppendLine($"{curEventData.stencilState.stencilFailOpFront}");
                        m_TempSB1.AppendLine("Stencil ZFail");
                        m_TempSB2.AppendLine($"{curEventData.stencilState.stencilZFailOpFront}");
                    }
                    // Only show *Back states when CullMode is set to Front.
                    else if (curEventData.rasterState.cullMode == CullMode.Front)
                    {
                        m_TempSB1.AppendLine("Stencil Comp");
                        m_TempSB2.AppendLine($"{curEventData.stencilState.stencilFuncBack}");
                        m_TempSB1.AppendLine("Stencil Pass");
                        m_TempSB2.AppendLine($"{curEventData.stencilState.stencilPassOpBack}");
                        m_TempSB1.AppendLine("Stencil Fail");
                        m_TempSB2.AppendLine($"{curEventData.stencilState.stencilFailOpBack}");
                        m_TempSB1.AppendLine("Stencil ZFail");
                        m_TempSB2.AppendLine($"{curEventData.stencilState.stencilZFailOpBack}");
                    }
                    // Show both *Front and *Back states for two-sided geometry.
                    else
                    {
                        m_TempSB1.AppendLine("Stencil Comp");
                        m_TempSB2.AppendLine($"{curEventData.stencilState.stencilFuncFront} {curEventData.stencilState.stencilFuncBack}");
                        m_TempSB1.AppendLine("Stencil Pass");
                        m_TempSB2.AppendLine($"{curEventData.stencilState.stencilPassOpFront} {curEventData.stencilState.stencilPassOpBack}");
                        m_TempSB1.AppendLine("Stencil Fail");
                        m_TempSB2.AppendLine($"{curEventData.stencilState.stencilFailOpFront} {curEventData.stencilState.stencilFailOpBack}");
                        m_TempSB1.AppendLine("Stencil ZFail");
                        m_TempSB2.AppendLine($"{curEventData.stencilState.stencilZFailOpFront} {curEventData.stencilState.stencilZFailOpBack}");
                    }
                }
                else
                {
                    m_TempSB1.AppendLine("Stencil");
                    m_TempSB2.AppendLine("Disabled");

                    m_TempSB1.AppendLine("Stencil Ref");
                    m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                    m_TempSB1.AppendLine("Stencil ReadMask");
                    m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                    m_TempSB1.AppendLine("Stencil WriteMask");
                    m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                    m_TempSB1.AppendLine("Stencil Comp");
                    m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                    m_TempSB1.AppendLine("Stencil Pass");
                    m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                    m_TempSB1.AppendLine("Stencil Fail");
                    m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);

                    m_TempSB1.AppendLine("Stencil ZFail");
                    m_TempSB2.AppendLine(FrameDebuggerStyles.EventDetails.k_NotAvailable);
                }
            }

            m_LastEventData.detailLabelsRightColumn = m_TempSB1.ToString();
            m_LastEventData.detailValuesRightColumn = m_TempSB2.ToString();

            // Keywords
            m_TempSB2.Clear();
            if (!string.IsNullOrEmpty(curEventData.shaderKeywords))
            {
                if (m_KeywordsList.Count == 0)
                    m_KeywordsList.AddRange(curEventData.shaderKeywords.Split(' '));

                m_KeywordsList.Sort();
                m_TempSB2.AppendLine(string.Join<string>("\n", m_KeywordsList));
            }

            m_LastEventData.keywords = m_TempSB2.ToString().Trim();
        }

        private void GetShaderData()
        {
            const string k_ComputeShaderFilter = "t:computeshader";
            const string k_RayTracingShaderFilter = "t:raytracingshader";

            // Clear or Resolve events
            if (m_LastEventData.isClearEvent || m_LastEventData.isResolveEvent)
            {
                m_LastEventData.shaderName = FrameDebuggerStyles.EventDetails.k_NotAvailable;
                m_LastEventData.shader = null;
                return;
            }

            // Normal shader events
            if (!m_LastEventData.isComputeEvent && !m_LastEventData.isRayTracingEvent)
            {
                m_LastEventData.shaderName = curEventData.shaderName;
                m_LastEventData.shader = Shader.Find(m_LastEventData.shaderName);
                return;
            }

            // Compute or RayTracing events
            m_LastEventData.shader = null;
            string filter;
            if (m_LastEventData.isComputeEvent)
            {
                m_LastEventData.shaderName = curEventData.csName;
                filter = k_ComputeShaderFilter;
            }
            else if (m_LastEventData.isRayTracingEvent)
            {
                m_LastEventData.shaderName = curEventData.rtsName;
                filter = k_RayTracingShaderFilter;
            }
            else
                return;

            string[] guids = AssetDatabase.FindAssets($"{m_LastEventData.shaderName} {filter}");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                UnityEngine.Object shader = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (shader != null)
                    m_LastEventData.shader = shader;
            }
        }

        private bool BeginFoldoutBox(int nr, bool hasData, GUIContent header, out float fadePercent)
        {
            GUI.enabled = hasData;

            EditorGUILayout.BeginVertical(FrameDebuggerStyles.EventDetails.foldoutCategoryBoxStyle);
            Rect r = GUILayoutUtility.GetRect(2, 21);

            EditorGUI.BeginChangeCheck();
            bool expanded = EditorGUI.FoldoutTitlebar(r, header, m_FoldoutAnimators[nr].target, true, EditorStyles.inspectorTitlebarFlat, EditorStyles.inspectorTitlebarText);
            if (EditorGUI.EndChangeCheck())
            {
                m_FoldoutAnimators[nr].target = !m_FoldoutAnimators[nr].target;
            }

            GUI.enabled = true;
            EditorGUI.indentLevel++;
            fadePercent = m_FoldoutAnimators[nr].faded;

            return EditorGUILayout.BeginFadeGroup(m_FoldoutAnimators[nr].faded);
        }

        private void EndFoldoutBox()
        {
            EditorGUILayout.EndFadeGroup();
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }
    }
}
