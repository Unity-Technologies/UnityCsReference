// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor
{
    class ShapeModuleUI : ModuleUI
    {
        struct MultiModeParameter
        {
            public SerializedProperty m_Value;
            public SerializedProperty m_Mode;
            public SerializedProperty m_Spread;
            public SerializedMinMaxCurve m_Speed;

            public enum ValueMode { Random, Loop, PingPong, BurstSpread };

            public static MultiModeParameter GetProperty(ModuleUI ui, string name, GUIContent speed)
            {
                MultiModeParameter result = new MultiModeParameter();
                result.m_Value = ui.GetProperty(name + ".value");
                result.m_Mode = ui.GetProperty(name + ".mode");
                result.m_Spread = ui.GetProperty(name + ".spread");
                result.m_Speed = new SerializedMinMaxCurve(ui, speed, name + ".speed", kUseSignedRange);
                result.m_Speed.m_AllowRandom = false;
                return result;
            }

            public void OnInspectorGUI(MultiModeTexts text)
            {
                GUIFloat(text.value, m_Value);

                EditorGUI.indentLevel++;

                GUIPopup(text.mode, m_Mode, new string[] { "Random", "Loop", "Ping-Pong", "Burst Spread" });
                GUIFloat(text.spread, m_Spread);

                if (!m_Mode.hasMultipleDifferentValues)
                {
                    ValueMode mode = (ValueMode)m_Mode.intValue;
                    if (mode == ValueMode.Loop || mode == ValueMode.PingPong)
                    {
                        GUIMinMaxCurve(text.speed, m_Speed);
                    }
                }

                EditorGUI.indentLevel--;
            }
        };

        SerializedProperty m_Type;
        SerializedProperty m_RandomDirectionAmount;
        SerializedProperty m_SphericalDirectionAmount;

        // primitive properties
        MultiModeParameter m_Radius;
        SerializedProperty m_Angle;
        SerializedProperty m_Length;
        SerializedProperty m_BoxX;
        SerializedProperty m_BoxY;
        SerializedProperty m_BoxZ;
        MultiModeParameter m_Arc;

        // mesh properties
        SerializedProperty m_PlacementMode;
        SerializedProperty m_Mesh;
        SerializedProperty m_MeshRenderer;
        SerializedProperty m_SkinnedMeshRenderer;
        SerializedProperty m_MeshMaterialIndex;
        SerializedProperty m_UseMeshMaterialIndex;
        SerializedProperty m_UseMeshColors;
        SerializedProperty m_MeshNormalOffset;
        SerializedProperty m_MeshScale;
        SerializedProperty m_AlignToDirection;

        // internal
        private Material m_Material;
        private static int s_HandleControlIDHint = typeof(ShapeModuleUI).Name.GetHashCode();
        Matrix4x4 s_ArcHandleOffsetMatrix = Matrix4x4.TRS(
                Vector3.zero, Quaternion.AngleAxis(90f, Vector3.right) * Quaternion.AngleAxis(90f, Vector3.up), Vector3.one
                );
        private ArcHandle m_ArcHandle = new ArcHandle(s_HandleControlIDHint);
        private BoxBoundsHandle m_BoxBoundsHandle = new BoxBoundsHandle(s_HandleControlIDHint);
        private static Color s_ShapeGizmoColor = new Color(148f / 255f, 229f / 255f, 1f, 0.9f);

        private readonly string[] m_GuiNames = new string[] { "Sphere", "Hemisphere", "Cone", "Box", "Mesh", "Mesh Renderer", "Skinned Mesh Renderer", "Circle", "Edge" };
        private readonly ParticleSystemShapeType[] m_GuiTypes = new[] { ParticleSystemShapeType.Sphere, ParticleSystemShapeType.Hemisphere, ParticleSystemShapeType.Cone, ParticleSystemShapeType.Box, ParticleSystemShapeType.Mesh, ParticleSystemShapeType.MeshRenderer, ParticleSystemShapeType.SkinnedMeshRenderer, ParticleSystemShapeType.Circle, ParticleSystemShapeType.SingleSidedEdge };
        private readonly int[] m_TypeToGuiTypeIndex = new[] { 0, 0, 1, 1, 2, 3, 4, 2, 2, 2, 7, 7, 8, 5, 6, 3, 3 };

        private readonly ParticleSystemShapeType[] boxShapes = new ParticleSystemShapeType[] { ParticleSystemShapeType.Box, ParticleSystemShapeType.BoxShell, ParticleSystemShapeType.BoxEdge };
        private readonly ParticleSystemShapeType[] coneShapes = new ParticleSystemShapeType[] { ParticleSystemShapeType.Cone, ParticleSystemShapeType.ConeShell, ParticleSystemShapeType.ConeVolume, ParticleSystemShapeType.ConeVolumeShell };
        private readonly ParticleSystemShapeType[] shellShapes = new ParticleSystemShapeType[] { ParticleSystemShapeType.BoxShell, ParticleSystemShapeType.HemisphereShell, ParticleSystemShapeType.SphereShell, ParticleSystemShapeType.ConeShell, ParticleSystemShapeType.ConeVolumeShell, ParticleSystemShapeType.CircleEdge };

        class Texts
        {
            public GUIContent shape = EditorGUIUtility.TextContent("Shape|Defines the shape of the volume from which particles can be emitted, and the direction of the start velocity.");
            public GUIContent radius = EditorGUIUtility.TextContent("Radius|Radius of the shape.");
            public GUIContent coneAngle = EditorGUIUtility.TextContent("Angle|Angle of the cone.");
            public GUIContent coneLength = EditorGUIUtility.TextContent("Length|Length of the cone.");
            public GUIContent boxX = EditorGUIUtility.TextContent("Box X|Scale of the box in X Axis.");
            public GUIContent boxY = EditorGUIUtility.TextContent("Box Y|Scale of the box in Y Axis.");
            public GUIContent boxZ = EditorGUIUtility.TextContent("Box Z|Scale of the box in Z Axis.");
            public GUIContent mesh = EditorGUIUtility.TextContent("Mesh|Mesh that the particle system will emit from.");
            public GUIContent meshRenderer = EditorGUIUtility.TextContent("Mesh|MeshRenderer that the particle system will emit from.");
            public GUIContent skinnedMeshRenderer = EditorGUIUtility.TextContent("Mesh|SkinnedMeshRenderer that the particle system will emit from.");
            public GUIContent meshMaterialIndex = EditorGUIUtility.TextContent("Single Material|Only emit from a specific material of the mesh.");
            public GUIContent useMeshColors = EditorGUIUtility.TextContent("Use Mesh Colors|Modulate particle color with mesh vertex colors, or if they don't exist, use the shader color property \"_Color\" or \"_TintColor\" from the material. Does not read texture colors.");
            public GUIContent meshNormalOffset = EditorGUIUtility.TextContent("Normal Offset|Offset particle spawn positions along the mesh normal.");
            public GUIContent meshScale = EditorGUIUtility.TextContent("Mesh Scale|Adjust the size of the source mesh.");
            public GUIContent alignToDirection = EditorGUIUtility.TextContent("Align To Direction|Automatically align particles based on their initial direction of travel.");
            public GUIContent randomDirectionAmount = EditorGUIUtility.TextContent("Randomize Direction|Randomize the emission direction.");
            public GUIContent sphericalDirectionAmount = EditorGUIUtility.TextContent("Spherize Direction|Spherize the emission direction.");
            public GUIContent emitFromShell = EditorGUIUtility.TextContent("Emit from Shell|Emit from shell of the sphere. If disabled particles will be emitted from the volume of the shape.");
            public GUIContent emitFromEdge = EditorGUIUtility.TextContent("Emit from Edge|Emit from edge of the shape. If disabled particles will be emitted from the volume of the shape.");
            public GUIContent emitFrom = EditorGUIUtility.TextContent("Emit from:|Specifies from where particles are emitted.");
        }

        static Texts s_Texts = new Texts();

        class MultiModeTexts
        {
            public MultiModeTexts(string _value, string _mode, string _spread, string _speed)
            {
                value = EditorGUIUtility.TextContent(_value);
                mode = EditorGUIUtility.TextContent(_mode);
                spread = EditorGUIUtility.TextContent(_spread);
                speed = EditorGUIUtility.TextContent(_speed);
            }

            public GUIContent value;
            public GUIContent mode;
            public GUIContent spread;
            public GUIContent speed;
        }

        static MultiModeTexts s_RadiusTexts = new MultiModeTexts(
                /*_value:*/ "Radius|New particles are spawned along the radius.",
                /*_mode:*/ "Mode|Control how particles are spawned along the radius.",
                /*_spread:*/ "Spread|Spawn particles only at specific positions along the radius (0 to disable).",
                /*_speed:*/ "Speed|Control the speed that the emission position moves along the radius.");

        static MultiModeTexts s_ArcTexts = new MultiModeTexts(
                /*_value:*/ "Arc|New particles are spawned around the arc.",
                /*_mode:*/ "Mode|Control how particles are spawned around the arc.",
                /*_spread:*/ "Spread|Spawn particles only at specific angles around the arc (0 to disable).",
                /*_speed:*/ "Speed|Control the speed that the emission position moves around the arc.");

        public ShapeModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "ShapeModule", displayName, VisibilityState.VisibleAndFolded)
        {
            m_ToolTip = "Shape of the emitter volume, which controls where particles are emitted and their initial direction.";
        }

        protected override void Init()
        {
            if (m_Type != null)
                return;

            if (s_Texts == null)
                s_Texts = new Texts();

            m_Type = GetProperty("type");
            m_Radius = MultiModeParameter.GetProperty(this, "radius", s_RadiusTexts.speed);
            m_Angle = GetProperty("angle");
            m_Length = GetProperty("length");
            m_BoxX = GetProperty("boxX");
            m_BoxY = GetProperty("boxY");
            m_BoxZ = GetProperty("boxZ");
            m_Arc = MultiModeParameter.GetProperty(this, "arc", s_ArcTexts.speed);

            m_PlacementMode = GetProperty("placementMode");
            m_Mesh = GetProperty("m_Mesh");
            m_MeshRenderer = GetProperty("m_MeshRenderer");
            m_SkinnedMeshRenderer = GetProperty("m_SkinnedMeshRenderer");
            m_MeshMaterialIndex = GetProperty("m_MeshMaterialIndex");
            m_UseMeshMaterialIndex = GetProperty("m_UseMeshMaterialIndex");
            m_UseMeshColors = GetProperty("m_UseMeshColors");
            m_MeshNormalOffset = GetProperty("m_MeshNormalOffset");
            m_MeshScale = GetProperty("m_MeshScale");
            m_RandomDirectionAmount = GetProperty("randomDirectionAmount");
            m_SphericalDirectionAmount = GetProperty("sphericalDirectionAmount");
            m_AlignToDirection = GetProperty("alignToDirection");

            // @TODO: Use something that uses vertex color + alpha and is transparent (Particles/Alpha blended does this, but need builtin material for it)
            m_Material = EditorGUIUtility.GetBuiltinExtraResource(typeof(Material), "Default-Material.mat") as Material;
        }

        public override float GetXAxisScalar()
        {
            return m_ParticleSystemUI.GetEmitterDuration();
        }

        private ParticleSystemShapeType ConvertConeEmitFromToConeType(int emitFrom)
        {
            return coneShapes[emitFrom];
        }

        private int ConvertConeTypeToConeEmitFrom(ParticleSystemShapeType shapeType)
        {
            return System.Array.IndexOf(coneShapes, shapeType);
        }

        private ParticleSystemShapeType ConvertBoxEmitFromToConeType(int emitFrom)
        {
            return boxShapes[emitFrom];
        }

        private int ConvertBoxTypeToConeEmitFrom(ParticleSystemShapeType shapeType)
        {
            return System.Array.IndexOf(boxShapes, shapeType);
        }

        private bool GetUsesShell(ParticleSystemShapeType shapeType)
        {
            return System.Array.IndexOf(shellShapes, shapeType) != -1;
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            if (s_Texts == null)
                s_Texts = new Texts();

            int type = m_Type.intValue;
            int index = m_TypeToGuiTypeIndex[(int)type];

            bool wasUsingShell = GetUsesShell((ParticleSystemShapeType)type);

            EditorGUI.BeginChangeCheck();
            int index2 = GUIPopup(s_Texts.shape, index, m_GuiNames);
            bool shapeTypeChanged = EditorGUI.EndChangeCheck();

            ParticleSystemShapeType guiType = m_GuiTypes[index2];
            if (index2 != index)
            {
                type = (int)guiType;
            }

            switch (guiType)
            {
                case ParticleSystemShapeType.Box:
                {
                    GUIFloat(s_Texts.boxX, m_BoxX);
                    GUIFloat(s_Texts.boxY, m_BoxY);
                    GUIFloat(s_Texts.boxZ, m_BoxZ);

                    string[] types = new string[] { "Volume", "Shell", "Edge" };

                    int emitFrom = ConvertBoxTypeToConeEmitFrom((ParticleSystemShapeType)type);
                    emitFrom = GUIPopup(s_Texts.emitFrom, emitFrom, types);
                    type = (int)ConvertBoxEmitFromToConeType(emitFrom);
                }
                break;

                case ParticleSystemShapeType.Cone:
                {
                    GUIFloat(s_Texts.coneAngle, m_Angle);
                    GUIFloat(s_Texts.radius, m_Radius.m_Value);

                    m_Arc.OnInspectorGUI(s_ArcTexts);

                    bool showLength = !((type == (int)ParticleSystemShapeType.ConeVolume) || (type == (int)ParticleSystemShapeType.ConeVolumeShell));
                    using (new EditorGUI.DisabledScope(showLength))
                    {
                        GUIFloat(s_Texts.coneLength, m_Length);
                    }

                    string[] types = new string[] { "Base", "Base Shell", "Volume", "Volume Shell" };

                    int emitFrom = ConvertConeTypeToConeEmitFrom((ParticleSystemShapeType)type);
                    emitFrom = GUIPopup(s_Texts.emitFrom, emitFrom, types);
                    type = (int)ConvertConeEmitFromToConeType(emitFrom);
                }
                break;

                case ParticleSystemShapeType.Mesh:
                case ParticleSystemShapeType.MeshRenderer:
                case ParticleSystemShapeType.SkinnedMeshRenderer:
                {
                    string[] types = new string[] {"Vertex", "Edge", "Triangle"};
                    GUIPopup("", m_PlacementMode, types);

                    Material material = null;
                    Mesh srcMesh = null;
                    if (guiType == ParticleSystemShapeType.Mesh)
                    {
                        GUIObject(s_Texts.mesh, m_Mesh);
                    }
                    else if (guiType == ParticleSystemShapeType.MeshRenderer)
                    {
                        GUIObject(s_Texts.meshRenderer, m_MeshRenderer);
                        MeshRenderer mesh = (MeshRenderer)m_MeshRenderer.objectReferenceValue;
                        if (mesh)
                        {
                            material = mesh.sharedMaterial;
                            if (mesh.GetComponent<MeshFilter>())
                                srcMesh = mesh.GetComponent<MeshFilter>().sharedMesh;
                        }
                    }
                    else
                    {
                        GUIObject(s_Texts.skinnedMeshRenderer, m_SkinnedMeshRenderer);
                        SkinnedMeshRenderer mesh = (SkinnedMeshRenderer)m_SkinnedMeshRenderer.objectReferenceValue;
                        if (mesh)
                        {
                            material = mesh.sharedMaterial;
                            srcMesh = mesh.sharedMesh;
                        }
                    }

                    GUIToggleWithIntField(s_Texts.meshMaterialIndex, m_UseMeshMaterialIndex, m_MeshMaterialIndex, false);
                    bool useMeshColors = GUIToggle(s_Texts.useMeshColors, m_UseMeshColors);
                    if (useMeshColors)
                    {
                        if (material != null && srcMesh != null)
                        {
                            int colorName = Shader.PropertyToID("_Color");
                            int tintColorName = Shader.PropertyToID("_TintColor");
                            if (!material.HasProperty(colorName) && !material.HasProperty(tintColorName) && !srcMesh.HasChannel(Mesh.InternalShaderChannel.Color))
                            {
                                GUIContent warning = EditorGUIUtility.TextContent("To use mesh colors, your source mesh must either provide vertex colors, or its shader must contain a color property named \"_Color\" or \"_TintColor\".");
                                EditorGUILayout.HelpBox(warning.text, MessageType.Warning, true);
                            }
                        }
                    }

                    GUIFloat(s_Texts.meshNormalOffset, m_MeshNormalOffset);
                    GUIFloat(s_Texts.meshScale, m_MeshScale);
                }
                break;

                case ParticleSystemShapeType.Sphere:
                {
                    // sphere
                    GUIFloat(s_Texts.radius, m_Radius.m_Value);
                    bool useShellEmit = GUIToggle(s_Texts.emitFromShell, wasUsingShell);
                    type = (int)(useShellEmit ? ParticleSystemShapeType.SphereShell : ParticleSystemShapeType.Sphere);
                }
                break;

                case ParticleSystemShapeType.Hemisphere:
                {
                    // sphere
                    GUIFloat(s_Texts.radius, m_Radius.m_Value);
                    bool useShellEmit = GUIToggle(s_Texts.emitFromShell, wasUsingShell);
                    type = (int)(useShellEmit ? ParticleSystemShapeType.HemisphereShell : ParticleSystemShapeType.Hemisphere);
                }
                break;

                case ParticleSystemShapeType.Circle:
                {
                    GUIFloat(s_Texts.radius, m_Radius.m_Value);

                    m_Arc.OnInspectorGUI(s_ArcTexts);

                    bool useShellEmit = GUIToggle(s_Texts.emitFromEdge, wasUsingShell);
                    type = (int)(useShellEmit ? ParticleSystemShapeType.CircleEdge : ParticleSystemShapeType.Circle);
                }
                break;

                case ParticleSystemShapeType.SingleSidedEdge:
                {
                    m_Radius.OnInspectorGUI(s_RadiusTexts);
                }
                break;
            }

            if (shapeTypeChanged || !m_Type.hasMultipleDifferentValues)
                m_Type.intValue = type;

            GUIToggle(s_Texts.alignToDirection, m_AlignToDirection);
            GUIFloat(s_Texts.randomDirectionAmount, m_RandomDirectionAmount);
            GUIFloat(s_Texts.sphericalDirectionAmount, m_SphericalDirectionAmount);
        }

        override public void OnSceneViewGUI()
        {
            Color origCol = Handles.color;
            Handles.color = s_ShapeGizmoColor;

            Matrix4x4 orgMatrix = Handles.matrix;

            EditorGUI.BeginChangeCheck();

            foreach (ParticleSystem ps in m_ParticleSystemUI.m_ParticleSystems)
            {
                var shapeModule = ps.shape;
                var mainModule = ps.main;

                ParticleSystemShapeType type = shapeModule.shapeType;

                Matrix4x4 scaleMatrix = new Matrix4x4();
                float extraScale = (type == ParticleSystemShapeType.Mesh) ? shapeModule.meshScale : 1.0f;
                if (mainModule.scalingMode == ParticleSystemScalingMode.Local)
                {
                    scaleMatrix.SetTRS(ps.transform.position, ps.transform.rotation, ps.transform.localScale * extraScale);
                }
                else if (mainModule.scalingMode == ParticleSystemScalingMode.Hierarchy)
                {
                    scaleMatrix = ps.transform.localToWorldMatrix * Matrix4x4.Scale(new Vector3(extraScale, extraScale, extraScale));
                }
                else
                {
                    scaleMatrix.SetTRS(ps.transform.position, ps.transform.rotation, ps.transform.lossyScale * extraScale);
                }

                Handles.matrix = scaleMatrix;

                if (type == ParticleSystemShapeType.Sphere || type == ParticleSystemShapeType.SphereShell)
                {
                    EditorGUI.BeginChangeCheck();
                    float radius = Handles.DoSimpleRadiusHandle(Quaternion.identity, Vector3.zero, shapeModule.radius, false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, "Sphere Handle Change");
                        shapeModule.radius = radius;
                    }
                }
                else if (type == ParticleSystemShapeType.Circle || type == ParticleSystemShapeType.CircleEdge)
                {
                    EditorGUI.BeginChangeCheck();

                    m_ArcHandle.radius = shapeModule.radius;
                    m_ArcHandle.angle = shapeModule.arc;
                    m_ArcHandle.SetColorWithRadiusHandle(Color.white, 0f);

                    using (new Handles.DrawingScope(Handles.matrix * s_ArcHandleOffsetMatrix))
                        m_ArcHandle.DrawHandle();

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, "Circle Handle Change");
                        shapeModule.radius = m_ArcHandle.radius;
                        shapeModule.arc = m_ArcHandle.angle;
                    }
                }
                else if (type == ParticleSystemShapeType.Hemisphere || type == ParticleSystemShapeType.HemisphereShell)
                {
                    EditorGUI.BeginChangeCheck();
                    float radius = Handles.DoSimpleRadiusHandle(Quaternion.identity, Vector3.zero, shapeModule.radius, true);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, "Hemisphere Handle Change");
                        shapeModule.radius = radius;
                    }
                }
                else if ((type == ParticleSystemShapeType.Cone) || (type == ParticleSystemShapeType.ConeShell))
                {
                    EditorGUI.BeginChangeCheck();

                    Vector3 radiusAngleRange = new Vector3(shapeModule.radius, shapeModule.angle, mainModule.startSpeedMultiplier);
                    radiusAngleRange = Handles.ConeFrustrumHandle(Quaternion.identity, Vector3.zero, radiusAngleRange);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, "Cone Handle Change");
                        shapeModule.radius = radiusAngleRange.x;
                        shapeModule.angle = radiusAngleRange.y;
                        mainModule.startSpeedMultiplier = radiusAngleRange.z;
                    }
                }
                else if ((type == ParticleSystemShapeType.ConeVolume) || (type == ParticleSystemShapeType.ConeVolumeShell))
                {
                    EditorGUI.BeginChangeCheck();

                    Vector3 radiusAngleLength = new Vector3(shapeModule.radius, shapeModule.angle, shapeModule.length);
                    radiusAngleLength = Handles.ConeFrustrumHandle(Quaternion.identity, Vector3.zero, radiusAngleLength);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, "Cone Volume Handle Change");
                        shapeModule.radius = radiusAngleLength.x;
                        shapeModule.angle = radiusAngleLength.y;
                        shapeModule.length = radiusAngleLength.z;
                    }
                }
                else if (type == ParticleSystemShapeType.Box || type == ParticleSystemShapeType.BoxShell || type == ParticleSystemShapeType.BoxEdge)
                {
                    EditorGUI.BeginChangeCheck();

                    m_BoxBoundsHandle.center = Vector3.zero;
                    m_BoxBoundsHandle.size = shapeModule.box;
                    m_BoxBoundsHandle.DrawHandle();

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, "Box Handle Change");
                        shapeModule.box = m_BoxBoundsHandle.size;
                    }
                }
                else if (type == ParticleSystemShapeType.SingleSidedEdge)
                {
                    EditorGUI.BeginChangeCheck();
                    float radius = Handles.DoSimpleEdgeHandle(Quaternion.identity, Vector3.zero, shapeModule.radius);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, "Edge Handle Change");
                        shapeModule.radius = radius;
                    }
                }
                else if (type == ParticleSystemShapeType.Mesh)
                {
                    Mesh mesh = (Mesh)shapeModule.mesh;
                    if (mesh)
                    {
                        bool orgWireframeMode = GL.wireframe;
                        GL.wireframe = true;
                        m_Material.SetPass(0);
                        Graphics.DrawMeshNow(mesh, scaleMatrix);
                        GL.wireframe = orgWireframeMode;
                    }
                }
            }

            if (EditorGUI.EndChangeCheck())
                m_ParticleSystemUI.m_ParticleEffectUI.m_Owner.Repaint();

            Handles.color = origCol;
            Handles.matrix = orgMatrix;
        }

        override public void UpdateCullingSupportedString(ref string text)
        {
            Init();

            if (m_Arc.m_Mode.intValue != (int)MultiModeParameter.ValueMode.Random || m_Radius.m_Mode.intValue != (int)MultiModeParameter.ValueMode.Random)
                text += "\n\tAnimated shape emission is enabled.";
        }
    }
} // namespace UnityEditor
