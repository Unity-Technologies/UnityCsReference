// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityEditor
{
    internal class RendererModuleUI : ModuleUI
    {
        // Keep in sync with the one in ParticleSystemRenderer.h
        const int k_MaxNumMeshes = 4;

        // BaseRenderer and Renderer
        SerializedProperty m_CastShadows;
        SerializedProperty m_ReceiveShadows;
        SerializedProperty m_MotionVectors;
        SerializedProperty m_Material;
        SerializedProperty m_TrailMaterial;
        SerializedProperty m_SortingOrder;
        SerializedProperty m_SortingLayerID;

        // From ParticleSystemRenderer
        SerializedProperty m_RenderMode;
        SerializedProperty[] m_Meshes = new SerializedProperty[k_MaxNumMeshes];
        SerializedProperty[] m_ShownMeshes;

        SerializedProperty m_MinParticleSize;   //< How small is a particle allowed to be on screen at least? 1 is entire viewport. 0.5 is half viewport.
        SerializedProperty m_MaxParticleSize;   //< How large is a particle allowed to be on screen at most? 1 is entire viewport. 0.5 is half viewport.
        SerializedProperty m_CameraVelocityScale; //< How much the camera motion is factored in when determining particle stretching.
        SerializedProperty m_VelocityScale;     //< When Stretch Particles is enabled, defines the length of the particle compared to its velocity.
        SerializedProperty m_LengthScale;       //< When Stretch Particles is enabled, defines the length of the particle compared to its width.
        SerializedProperty m_SortMode;          //< What method of particle sorting to use. If none is specified, no sorting will occur.
        SerializedProperty m_SortingFudge;      //< Lower the number, most likely that these particles will appear in front of other transparent objects, including other particles.
        SerializedProperty m_NormalDirection;
        RendererEditorBase.Probes m_Probes;

        SerializedProperty m_RenderAlignment;
        SerializedProperty m_Pivot;
        SerializedProperty m_UseCustomVertexStreams;
        SerializedProperty m_VertexStreams;
        SerializedProperty m_MaskInteraction;

        ReorderableList m_VertexStreamsList;
        int m_NumTexCoords;
        int m_TexCoordChannelIndex;
        bool m_HasTangent;
        bool m_HasColor;

        static bool s_VisualizePivot = false;

        // Keep in sync with ParticleSystemRenderMode in ParticleSystemRenderer.h
        enum RenderMode
        {
            Billboard = 0,
            Stretch3D = 1,
            BillboardFixedHorizontal = 2,
            BillboardFixedVertical = 3,
            Mesh = 4,
            None = 5
        };

        class Texts
        {
            public GUIContent renderMode = EditorGUIUtility.TextContent("Render Mode|Defines the render mode of the particle renderer.");
            public GUIContent material = EditorGUIUtility.TextContent("Material|Defines the material used to render particles.");
            public GUIContent trailMaterial = EditorGUIUtility.TextContent("Trail Material|Defines the material used to render particle trails.");
            public GUIContent mesh = EditorGUIUtility.TextContent("Mesh|Defines the mesh that will be rendered as particle.");
            public GUIContent minParticleSize = EditorGUIUtility.TextContent("Min Particle Size|How small is a particle allowed to be on screen at least? 1 is entire viewport. 0.5 is half viewport.");
            public GUIContent maxParticleSize = EditorGUIUtility.TextContent("Max Particle Size|How large is a particle allowed to be on screen at most? 1 is entire viewport. 0.5 is half viewport.");
            public GUIContent cameraSpeedScale = EditorGUIUtility.TextContent("Camera Scale|How much the camera speed is factored in when determining particle stretching.");
            public GUIContent speedScale = EditorGUIUtility.TextContent("Speed Scale|Defines the length of the particle compared to its speed.");
            public GUIContent lengthScale = EditorGUIUtility.TextContent("Length Scale|Defines the length of the particle compared to its width.");
            public GUIContent sortingFudge = EditorGUIUtility.TextContent("Sorting Fudge|Lower the number and most likely these particles will appear in front of other transparent objects, including other particles.");
            public GUIContent sortMode = EditorGUIUtility.TextContent("Sort Mode|The draw order of particles can be sorted by distance, oldest in front, or youngest in front.");
            public GUIContent rotation = EditorGUIUtility.TextContent("Rotation|Set whether the rotation of the particles is defined in Screen or World space.");
            public GUIContent castShadows = EditorGUIUtility.TextContent("Cast Shadows|Only opaque materials cast shadows");
            public GUIContent receiveShadows = EditorGUIUtility.TextContent("Receive Shadows|Only opaque materials receive shadows");
            public GUIContent motionVectors = EditorGUIUtility.TextContent("Motion Vectors|Specifies whether the Particle System renders 'Per Object Motion', 'Camera Motion', or 'No Motion' vectors to the Camera Motion Vector Texture. Note that there is no built-in support for Per-Particle Motion.");
            public GUIContent normalDirection = EditorGUIUtility.TextContent("Normal Direction|Value between 0.0 and 1.0. If 1.0 is used, normals will point towards camera. If 0.0 is used, normals will point out in the corner direction of the particle.");

            public GUIContent sortingLayer = EditorGUIUtility.TextContent("Sorting Layer|Name of the Renderer's sorting layer.");
            public GUIContent sortingOrder = EditorGUIUtility.TextContent("Order in Layer|Renderer's order within a sorting layer");
            public GUIContent space = EditorGUIUtility.TextContent("Render Alignment|Specifies if the particles will face the camera, align to world axes, or stay local to the system's transform.");
            public GUIContent pivot = EditorGUIUtility.TextContent("Pivot|Applies an offset to the pivot of particles, as a multiplier of its size.");
            public GUIContent visualizePivot = EditorGUIUtility.TextContent("Visualize Pivot|Render the pivot positions of the particles.");
            public GUIContent useCustomVertexStreams = EditorGUIUtility.TextContent("Custom Vertex Streams|Choose whether to send custom particle data to the shader.");

            // Keep in sync with enum in ParticleSystemRenderer.h
            public GUIContent[] particleTypes = new GUIContent[]
            {
                EditorGUIUtility.TextContent("Billboard"),
                EditorGUIUtility.TextContent("Stretched Billboard"),
                EditorGUIUtility.TextContent("Horizontal Billboard"),
                EditorGUIUtility.TextContent("Vertical Billboard"),
                EditorGUIUtility.TextContent("Mesh"),
                EditorGUIUtility.TextContent("None")
            };

            public GUIContent[] sortTypes = new GUIContent[]
            {
                EditorGUIUtility.TextContent("None"),
                EditorGUIUtility.TextContent("By Distance"),
                EditorGUIUtility.TextContent("Oldest in Front"),
                EditorGUIUtility.TextContent("Youngest in Front")
            };

            public GUIContent[] spaces = new GUIContent[]
            {
                EditorGUIUtility.TextContent("View"),
                EditorGUIUtility.TextContent("World"),
                EditorGUIUtility.TextContent("Local"),
                EditorGUIUtility.TextContent("Facing"),
                EditorGUIUtility.TextContent("Velocity")
            };

            public GUIContent[] localSpace = new GUIContent[]
            {
                EditorGUIUtility.TextContent("Local")
            };

            public GUIContent[] motionVectorOptions = new GUIContent[]
            {
                EditorGUIUtility.TextContent("Camera Motion Only"),
                EditorGUIUtility.TextContent("Per Object Motion"),
                EditorGUIUtility.TextContent("Force No Motion")
            };

            public GUIContent maskingMode = EditorGUIUtility.TextContent("Masking|Defines the masking behavior of the particles. See Sprite Masking documentation for more details.");
            public GUIContent[] maskInteractions = new GUIContent[]
            {
                EditorGUIUtility.TextContent("No Masking"),
                EditorGUIUtility.TextContent("Visible Inside Mask"),
                EditorGUIUtility.TextContent("Visible Outside Mask")
            };

            private string[] vertexStreamsMenu = { "Position", "Normal", "Tangent", "Color", "UV/UV1", "UV/UV2", "UV/UV3", "UV/UV4", "UV/AnimBlend", "UV/AnimFrame", "Center", "VertexID", "Size/Size.x", "Size/Size.xy", "Size/Size.xyz", "Rotation/Rotation", "Rotation/Rotation3D", "Rotation/RotationSpeed", "Rotation/RotationSpeed3D", "Velocity", "Speed", "Lifetime/AgePercent", "Lifetime/InverseStartLifetime", "Random/Stable.x", "Random/Stable.xy", "Random/Stable.xyz", "Random/Stable.xyzw", "Random/Varying.x", "Random/Varying.xy", "Random/Varying.xyz", "Random/Varying.xyzw", "Custom/Custom1.x", "Custom/Custom1.xy", "Custom/Custom1.xyz", "Custom/Custom1.xyzw", "Custom/Custom2.x", "Custom/Custom2.xy", "Custom/Custom2.xyz", "Custom/Custom2.xyzw", "Noise/Sum.x", "Noise/Sum.xy", "Noise/Sum.xyz", "Noise/Impulse.x", "Noise/Impulse.xy", "Noise/Impulse.xyz" };
            public string[] vertexStreamsPacked = { "Position", "Normal", "Tangent", "Color", "UV", "UV2", "UV3", "UV4", "AnimBlend", "AnimFrame", "Center", "VertexID", "Size", "Size.xy", "Size.xyz", "Rotation", "Rotation3D", "RotationSpeed", "RotationSpeed3D", "Velocity", "Speed", "AgePercent", "InverseStartLifetime", "StableRandom.x", "StableRandom.xy", "StableRandom.xyz", "StableRandom.xyzw", "VariableRandom.x", "VariableRandom.xy", "VariableRandom.xyz", "VariableRandom.xyzw", "Custom1.x", "Custom1.xy", "Custom1.xyz", "Custom1.xyzw", "Custom2.x", "Custom2.xy", "Custom2.xyz", "Custom2.xyzw", "NoiseSum.x", "NoiseSum.xy", "NoiseSum.xyz", "NoiseImpulse.x", "NoiseImpulse.xy", "NoiseImpulse.xyz" }; // Keep in sync with enums in ParticleSystemRenderer.h and ParticleSystem.bindings
            public string[] vertexStreamPackedTypes = { "POSITION.xyz", "NORMAL.xyz", "TANGENT.xyzw", "COLOR.xyzw" }; // all other types are floats
            public int[] vertexStreamTexCoordChannels = { 0, 0, 0, 0, 2, 2, 2, 2, 1, 1, 3, 1, 1, 2, 3, 1, 3, 1, 3, 3, 1, 1, 1, 1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 4, 1, 2, 3, 1, 2, 3 };
            public string channels = "xyzw|xyz";

            public GUIContent[] vertexStreamsMenuContent;

            public Texts()
            {
                vertexStreamsMenuContent = vertexStreamsMenu.Select(x => new GUIContent(x)).ToArray();
            }
        }
        private static Texts s_Texts;

        public RendererModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "ParticleSystemRenderer", displayName, VisibilityState.VisibleAndFolded)
        {
            m_ToolTip = "Specifies how the particles are rendered.";
        }

        protected override void Init()
        {
            if (m_CastShadows != null)
                return;
            if (s_Texts == null)
                s_Texts = new Texts();

            m_CastShadows = GetProperty0("m_CastShadows");
            m_ReceiveShadows = GetProperty0("m_ReceiveShadows");
            m_MotionVectors = GetProperty0("m_MotionVectors");
            m_Material = GetProperty0("m_Materials.Array.data[0]");
            m_TrailMaterial = GetProperty0("m_Materials.Array.data[1]");
            m_SortingOrder = GetProperty0("m_SortingOrder");
            m_SortingLayerID = GetProperty0("m_SortingLayerID");

            m_RenderMode = GetProperty0("m_RenderMode");
            m_MinParticleSize = GetProperty0("m_MinParticleSize");
            m_MaxParticleSize = GetProperty0("m_MaxParticleSize");
            m_CameraVelocityScale = GetProperty0("m_CameraVelocityScale");
            m_VelocityScale = GetProperty0("m_VelocityScale");
            m_LengthScale = GetProperty0("m_LengthScale");
            m_SortingFudge = GetProperty0("m_SortingFudge");
            m_SortMode = GetProperty0("m_SortMode");
            m_NormalDirection = GetProperty0("m_NormalDirection");

            m_Probes = new RendererEditorBase.Probes();
            m_Probes.Initialize(serializedObject);

            m_RenderAlignment = GetProperty0("m_RenderAlignment");
            m_Pivot = GetProperty0("m_Pivot");

            m_Meshes[0] = GetProperty0("m_Mesh");
            m_Meshes[1] = GetProperty0("m_Mesh1");
            m_Meshes[2] = GetProperty0("m_Mesh2");
            m_Meshes[3] = GetProperty0("m_Mesh3");
            List<SerializedProperty> shownMeshes = new List<SerializedProperty>();
            for (int i = 0; i < m_Meshes.Length; ++i)
            {
                // Always show the first mesh
                if (i == 0 || m_Meshes[i].objectReferenceValue != null)
                    shownMeshes.Add(m_Meshes[i]);
            }
            m_ShownMeshes = shownMeshes.ToArray();

            m_MaskInteraction = GetProperty0("m_MaskInteraction");

            m_UseCustomVertexStreams = GetProperty0("m_UseCustomVertexStreams");
            m_VertexStreams = GetProperty0("m_VertexStreams");
            m_VertexStreamsList = new ReorderableList(serializedObject, m_VertexStreams, true, true, true, true);
            m_VertexStreamsList.elementHeight = kReorderableListElementHeight;
            m_VertexStreamsList.headerHeight = 0;
            m_VertexStreamsList.onAddDropdownCallback = OnVertexStreamListAddDropdownCallback;
            m_VertexStreamsList.onCanRemoveCallback = OnVertexStreamListCanRemoveCallback;
            m_VertexStreamsList.drawElementCallback = DrawVertexStreamListElementCallback;

            s_VisualizePivot = EditorPrefs.GetBool("VisualizePivot", false);
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            EditorGUI.BeginChangeCheck();
            RenderMode renderMode = (RenderMode)GUIPopup(s_Texts.renderMode, m_RenderMode, s_Texts.particleTypes);
            bool renderModeChanged = EditorGUI.EndChangeCheck();

            if (!m_RenderMode.hasMultipleDifferentValues)
            {
                if (renderMode == RenderMode.Mesh)
                {
                    EditorGUI.indentLevel++;
                    DoListOfMeshesGUI();
                    EditorGUI.indentLevel--;

                    if (renderModeChanged && m_Meshes[0].objectReferenceInstanceIDValue == 0 && !m_Meshes[0].hasMultipleDifferentValues)
                        m_Meshes[0].objectReferenceValue = Resources.GetBuiltinResource(typeof(Mesh), "Cube.fbx");
                }
                else if (renderMode == RenderMode.Stretch3D)
                {
                    EditorGUI.indentLevel++;
                    GUIFloat(s_Texts.cameraSpeedScale, m_CameraVelocityScale);
                    GUIFloat(s_Texts.speedScale, m_VelocityScale);
                    GUIFloat(s_Texts.lengthScale, m_LengthScale);
                    EditorGUI.indentLevel--;
                }

                if (renderMode != RenderMode.None)
                {
                    if (renderMode != RenderMode.Mesh)
                        GUIFloat(s_Texts.normalDirection, m_NormalDirection);

                    if (m_Material != null) // The renderer's material list could be empty
                        GUIObject(s_Texts.material, m_Material);
                }
            }

            if (m_TrailMaterial != null) // The renderer's material list could be empty
                GUIObject(s_Texts.trailMaterial, m_TrailMaterial);

            if (renderMode != RenderMode.None)
            {
                if (!m_RenderMode.hasMultipleDifferentValues)
                {
                    GUIPopup(s_Texts.sortMode, m_SortMode, s_Texts.sortTypes);
                    GUIFloat(s_Texts.sortingFudge, m_SortingFudge);

                    if (renderMode != RenderMode.Mesh)
                    {
                        GUIFloat(s_Texts.minParticleSize, m_MinParticleSize);
                        GUIFloat(s_Texts.maxParticleSize, m_MaxParticleSize);
                    }

                    if (renderMode == RenderMode.Billboard || renderMode == RenderMode.Mesh)
                    {
                        bool anyAligntoDirection = m_ParticleSystemUI.m_ParticleSystems.FirstOrDefault(o => o.shape.alignToDirection) != null;
                        if (anyAligntoDirection)
                        {
                            using (new EditorGUI.DisabledScope(true))
                            {
                                GUIPopup(s_Texts.space, 0, s_Texts.localSpace); // force to "Local"
                            }

                            GUIContent info = EditorGUIUtility.TextContent("Using Align to Direction in the Shape Module forces the system to be rendered using Local Render Alignment.");
                            EditorGUILayout.HelpBox(info.text, MessageType.Info, true);
                        }
                        else
                        {
                            GUIPopup(s_Texts.space, m_RenderAlignment, s_Texts.spaces);
                        }
                    }
                }
                GUIVector3Field(s_Texts.pivot, m_Pivot);

                EditorGUI.BeginChangeCheck();
                s_VisualizePivot = GUIToggle(s_Texts.visualizePivot, s_VisualizePivot);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetBool("VisualizePivot", s_VisualizePivot);
                }

                GUIPopup(s_Texts.maskingMode, m_MaskInteraction, s_Texts.maskInteractions);

                if (GUIToggle(s_Texts.useCustomVertexStreams, m_UseCustomVertexStreams))
                    DoVertexStreamsGUI(renderMode);
            }

            EditorGUILayout.Space();

            GUIPopup(s_Texts.castShadows, m_CastShadows, EditorGUIUtility.TempContent(m_CastShadows.enumDisplayNames));

            // Disable ReceiveShadows options for Deferred rendering path
            if (SceneView.IsUsingDeferredRenderingPath())
            {
                using (new EditorGUI.DisabledScope(true)) { GUIToggle(s_Texts.receiveShadows, true); }
            }
            else
            {
                GUIToggle(s_Texts.receiveShadows, m_ReceiveShadows);
            }

            GUIPopup(s_Texts.motionVectors, m_MotionVectors, s_Texts.motionVectorOptions);

            EditorGUILayout.SortingLayerField(s_Texts.sortingLayer, m_SortingLayerID, ParticleSystemStyles.Get().popup, ParticleSystemStyles.Get().label);
            GUIInt(s_Texts.sortingOrder, m_SortingOrder);

            List<ParticleSystemRenderer> renderers = new List<ParticleSystemRenderer>();
            foreach (ParticleSystem ps in m_ParticleSystemUI.m_ParticleSystems)
            {
                renderers.Add(ps.GetComponent<ParticleSystemRenderer>());
            }
            m_Probes.OnGUI(renderers.ToArray(), renderers.FirstOrDefault(), true);
        }

        private void DoListOfMeshesGUI()
        {
            GUIListOfFloatObjectToggleFields(s_Texts.mesh, m_ShownMeshes, null, null, false);

            // Minus button
            Rect rect = GUILayoutUtility.GetRect(0, kSingleLineHeight); //GUILayoutUtility.GetLastRect();
            rect.x = rect.xMax - kPlusAddRemoveButtonWidth * 2 - kPlusAddRemoveButtonSpacing;
            rect.width = kPlusAddRemoveButtonWidth;
            if (m_ShownMeshes.Length > 1)
            {
                if (MinusButton(rect))
                {
                    m_ShownMeshes[m_ShownMeshes.Length - 1].objectReferenceValue = null;

                    List<SerializedProperty> shownMeshes = new List<SerializedProperty>(m_ShownMeshes);
                    shownMeshes.RemoveAt(shownMeshes.Count - 1);
                    m_ShownMeshes = shownMeshes.ToArray();
                }
            }

            // Plus button
            if (m_ShownMeshes.Length < k_MaxNumMeshes && !m_ParticleSystemUI.multiEdit)
            {
                rect.x += kPlusAddRemoveButtonWidth + kPlusAddRemoveButtonSpacing;
                if (PlusButton(rect))
                {
                    List<SerializedProperty> shownMeshes = new List<SerializedProperty>(m_ShownMeshes);
                    shownMeshes.Add(m_Meshes[shownMeshes.Count]);
                    m_ShownMeshes = shownMeshes.ToArray();
                }
            }
        }

        private class StreamCallbackData
        {
            public StreamCallbackData(UnityEditorInternal.ReorderableList l, SerializedProperty prop, int s)
            {
                list = l;
                streamProp = prop;
                stream = s;
            }

            public UnityEditorInternal.ReorderableList list;
            public SerializedProperty streamProp;
            public int stream;
        }

        void SelectVertexStreamCallback(object obj)
        {
            StreamCallbackData data = (StreamCallbackData)obj;

            ReorderableList.defaultBehaviours.DoAddButton(data.list);

            var element = data.streamProp.GetArrayElementAtIndex(data.list.index);
            element.intValue = data.stream;

            m_ParticleSystemUI.m_RendererSerializedObject.ApplyModifiedProperties();
        }

        private void DoVertexStreamsGUI(RenderMode renderMode)
        {
            // render list
            m_NumTexCoords = 0;
            m_TexCoordChannelIndex = 0;
            m_HasTangent = false;
            m_HasColor = false;
            m_VertexStreamsList.DoLayoutList();

            if (!m_ParticleSystemUI.multiEdit)
            {
                // error messages
                string errors = "";

                // check we have the same streams as the assigned shader
                if (m_Material != null)
                {
                    Material material = m_Material.objectReferenceValue as Material;
                    int totalChannelCount = m_NumTexCoords * 4 + m_TexCoordChannelIndex;
                    bool tangentError = false, colorError = false, uvError = false;
                    bool anyErrors = m_ParticleSystemUI.m_ParticleSystems[0].CheckVertexStreamsMatchShader(m_HasTangent, m_HasColor, totalChannelCount, material, ref tangentError, ref colorError, ref uvError);
                    if (anyErrors)
                    {
                        errors += "Vertex streams do not match the shader inputs. Particle systems may not render correctly. Ensure your streams match and are used by the shader.";
                        if (tangentError)
                            errors += "\n- TANGENT stream does not match.";
                        if (colorError)
                            errors += "\n- COLOR stream does not match.";
                        if (uvError)
                            errors += "\n- TEXCOORD streams do not match.";
                    }
                }

                // check we aren't using too many texcoords
                int maxTexCoords = m_ParticleSystemUI.m_ParticleSystems[0].GetMaxTexCoordStreams();
                if (m_NumTexCoords > maxTexCoords || (m_NumTexCoords == maxTexCoords && m_TexCoordChannelIndex > 0))
                {
                    if (errors != "")
                        errors += "\n\n";
                    errors += "Only " + maxTexCoords + " TEXCOORD streams are supported.";
                }

                // check input meshes aren't using too many UV streams
                if (renderMode == RenderMode.Mesh)
                {
                    ParticleSystemRenderer renderer = m_ParticleSystemUI.m_ParticleSystems[0].GetComponent<ParticleSystemRenderer>();
                    Mesh[] meshes = new Mesh[k_MaxNumMeshes];
                    int numMeshes = renderer.GetMeshes(meshes);
                    for (int i = 0; i < numMeshes; i++)
                    {
                        if (meshes[i].HasChannel(Mesh.InternalShaderChannel.TexCoord2))
                        {
                            if (errors != "")
                                errors += "\n\n";
                            errors += "Meshes may only use a maximum of 2 input UV streams.";
                        }
                    }
                }

                if (errors != "")
                {
                    GUIContent warning = EditorGUIUtility.TextContent(errors);
                    EditorGUILayout.HelpBox(warning.text, MessageType.Error, true);
                }
            }
        }

        private void OnVertexStreamListAddDropdownCallback(Rect rect, UnityEditorInternal.ReorderableList list)
        {
            List<int> notEnabled = new List<int>();
            for (int i = 0; i < s_Texts.vertexStreamsPacked.Length; ++i)
            {
                bool exists = false;
                for (int j = 0; j < m_VertexStreams.arraySize; ++j)
                {
                    if (m_VertexStreams.GetArrayElementAtIndex(j).intValue == i)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    notEnabled.Add(i);
            }

            GenericMenu menu = new GenericMenu();
            for (int i = 0; i < notEnabled.Count; ++i)
                menu.AddItem(s_Texts.vertexStreamsMenuContent[notEnabled[i]], false, SelectVertexStreamCallback, new StreamCallbackData(list, m_VertexStreams, notEnabled[i]));
            menu.ShowAsContext();
            Event.current.Use();
        }

        private bool OnVertexStreamListCanRemoveCallback(ReorderableList list)
        {
            // dont allow position stream to be removed
            SerializedProperty vertexStream = m_VertexStreams.GetArrayElementAtIndex(list.index);
            return (s_Texts.vertexStreamsPacked[vertexStream.intValue] != "Position");
        }

        private void DrawVertexStreamListElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty vertexStream = m_VertexStreams.GetArrayElementAtIndex(index);
            int vertexStreamValue = vertexStream.intValue;
            string tcName = isWindowView ? "TEX" : "TEXCOORD";

            int numChannels = s_Texts.vertexStreamTexCoordChannels[vertexStreamValue];
            if (numChannels != 0)
            {
                int swizzleLength = (m_TexCoordChannelIndex + numChannels > 4) ? numChannels + 1 : numChannels;
                string swizzle = s_Texts.channels.Substring(m_TexCoordChannelIndex, swizzleLength);
                GUI.Label(rect, s_Texts.vertexStreamsPacked[vertexStreamValue] + " (" + tcName + m_NumTexCoords + "." + swizzle + ")", ParticleSystemStyles.Get().label);
                m_TexCoordChannelIndex += numChannels;
                if (m_TexCoordChannelIndex >= 4)
                {
                    m_TexCoordChannelIndex -= 4;
                    m_NumTexCoords++;
                }
            }
            else
            {
                GUI.Label(rect, s_Texts.vertexStreamsPacked[vertexStreamValue] + " (" + s_Texts.vertexStreamPackedTypes[vertexStreamValue] + ")", ParticleSystemStyles.Get().label);
                if (s_Texts.vertexStreamsPacked[vertexStreamValue] == "Tangent")
                    m_HasTangent = true;
                if (s_Texts.vertexStreamsPacked[vertexStreamValue] == "Color")
                    m_HasColor = true;
            }
        }

        // render pivots
        override public void OnSceneViewGUI()
        {
            if (s_VisualizePivot == false)
                return;

            Color oldColor = Handles.color;
            Handles.color = Color.green;
            Matrix4x4 oldMatrix = Handles.matrix;

            Vector3[] lineSegments = new Vector3[6];

            foreach (ParticleSystem ps in m_ParticleSystemUI.m_ParticleSystems)
            {
                ParticleSystem.Particle[] particles = new ParticleSystem.Particle[ps.particleCount];
                int count = ps.GetParticles(particles);

                Matrix4x4 transform = Matrix4x4.identity;
                if (ps.main.simulationSpace == ParticleSystemSimulationSpace.Local)
                {
                    transform = ps.GetLocalToWorldMatrix();
                }
                Handles.matrix = transform;

                for (int i = 0; i < count; i++)
                {
                    ParticleSystem.Particle particle = particles[i];
                    Vector3 size = particle.GetCurrentSize3D(ps) * 0.05f;

                    lineSegments[0] = particle.position - (Vector3.right * size.x);
                    lineSegments[1] = particle.position + (Vector3.right * size.x);

                    lineSegments[2] = particle.position - (Vector3.up * size.y);
                    lineSegments[3] = particle.position + (Vector3.up * size.y);

                    lineSegments[4] = particle.position - (Vector3.forward * size.z);
                    lineSegments[5] = particle.position + (Vector3.forward * size.z);

                    Handles.DrawLines(lineSegments);
                }
            }

            Handles.color = oldColor;
            Handles.matrix = oldMatrix;
        }
    }
} // namespace UnityEditor
