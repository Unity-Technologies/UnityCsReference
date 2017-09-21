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

                GUIPopup(text.mode, m_Mode, s_Texts.emissionModes);
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
        SerializedProperty m_RandomPositionAmount;

        SerializedProperty m_Position;
        SerializedProperty m_Scale;
        SerializedProperty m_Rotation;

        // primitive properties
        MultiModeParameter m_Radius;
        SerializedProperty m_RadiusThickness;
        SerializedProperty m_Angle;
        SerializedProperty m_Length;
        SerializedProperty m_BoxThickness;
        MultiModeParameter m_Arc;
        SerializedProperty m_DonutRadius;

        // mesh properties
        SerializedProperty m_PlacementMode;
        SerializedProperty m_Mesh;
        SerializedProperty m_MeshRenderer;
        SerializedProperty m_SkinnedMeshRenderer;
        SerializedProperty m_MeshMaterialIndex;
        SerializedProperty m_UseMeshMaterialIndex;
        SerializedProperty m_UseMeshColors;
        SerializedProperty m_MeshNormalOffset;
        SerializedProperty m_AlignToDirection;

        // internal
        private Material m_Material;
        private static readonly Matrix4x4 s_ArcHandleOffsetMatrix = Matrix4x4.TRS(
                Vector3.zero, Quaternion.AngleAxis(90f, Vector3.right) * Quaternion.AngleAxis(90f, Vector3.up), Vector3.one
                );
        private ArcHandle m_ArcHandle = new ArcHandle();
        private BoxBoundsHandle m_BoxBoundsHandle = new BoxBoundsHandle();
        private SphereBoundsHandle m_SphereBoundsHandle = new SphereBoundsHandle();
        private static Color s_ShapeGizmoColor = new Color(148f / 255f, 229f / 255f, 1f, 0.9f);

        private readonly ParticleSystemShapeType[] m_GuiTypes = new[] { ParticleSystemShapeType.Sphere, ParticleSystemShapeType.Hemisphere, ParticleSystemShapeType.Cone, ParticleSystemShapeType.Donut, ParticleSystemShapeType.Box, ParticleSystemShapeType.Mesh, ParticleSystemShapeType.MeshRenderer, ParticleSystemShapeType.SkinnedMeshRenderer, ParticleSystemShapeType.Circle, ParticleSystemShapeType.SingleSidedEdge };
        private readonly int[] m_TypeToGuiTypeIndex = new[] { 0, 0, 1, 1, 2, 4, 5, 2, 2, 2, 8, 8, 9, 6, 7, 4, 4, 3 };

        private readonly ParticleSystemShapeType[] boxShapes = new ParticleSystemShapeType[] { ParticleSystemShapeType.Box, ParticleSystemShapeType.BoxShell, ParticleSystemShapeType.BoxEdge };
        private readonly ParticleSystemShapeType[] coneShapes = new ParticleSystemShapeType[] { ParticleSystemShapeType.Cone, ParticleSystemShapeType.ConeVolume };

        class Texts
        {
            public GUIContent shape = EditorGUIUtility.TextContent("Shape|Defines the shape of the volume from which particles can be emitted, and the direction of the start velocity.");
            public GUIContent radius = EditorGUIUtility.TextContent("Radius|Radius of the shape.");
            public GUIContent radiusThickness = EditorGUIUtility.TextContent("Radius Thickness|Control the thickness of the spawn volume, from 0 to 1.");
            public GUIContent coneAngle = EditorGUIUtility.TextContent("Angle|Angle of the cone.");
            public GUIContent coneLength = EditorGUIUtility.TextContent("Length|Length of the cone.");
            public GUIContent boxThickness = EditorGUIUtility.TextContent("Box Thickness|When using shell/edge modes, control the thickness of the spawn volume, from 0 to 1.");
            public GUIContent meshType = EditorGUIUtility.TextContent("Type|Generate particles from vertices, edges or triangles.");
            public GUIContent mesh = EditorGUIUtility.TextContent("Mesh|Mesh that the particle system will emit from.");
            public GUIContent meshRenderer = EditorGUIUtility.TextContent("Mesh|MeshRenderer that the particle system will emit from.");
            public GUIContent skinnedMeshRenderer = EditorGUIUtility.TextContent("Mesh|SkinnedMeshRenderer that the particle system will emit from.");
            public GUIContent meshMaterialIndex = EditorGUIUtility.TextContent("Single Material|Only emit from a specific material of the mesh.");
            public GUIContent useMeshColors = EditorGUIUtility.TextContent("Use Mesh Colors|Modulate particle color with mesh vertex colors, or if they don't exist, use the shader color property \"_Color\" or \"_TintColor\" from the material. Does not read texture colors.");
            public GUIContent meshNormalOffset = EditorGUIUtility.TextContent("Normal Offset|Offset particle spawn positions along the mesh normal.");
            public GUIContent alignToDirection = EditorGUIUtility.TextContent("Align To Direction|Automatically align particles based on their initial direction of travel.");
            public GUIContent randomDirectionAmount = EditorGUIUtility.TextContent("Randomize Direction|Randomize the emission direction.");
            public GUIContent sphericalDirectionAmount = EditorGUIUtility.TextContent("Spherize Direction|Spherize the emission direction.");
            public GUIContent randomPositionAmount = EditorGUIUtility.TextContent("Randomize Position|Randomize the starting positions.");
            public GUIContent emitFrom = EditorGUIUtility.TextContent("Emit from:|Specifies from where particles are emitted.");
            public GUIContent donutRadius = EditorGUIUtility.TextContent("Donut Radius|The radius of the donut. Used to control the thickness of the ring.");
            public GUIContent position = EditorGUIUtility.TextContent("Position|Translate the emission shape.");
            public GUIContent rotation = EditorGUIUtility.TextContent("Rotation|Rotate the emission shape.");
            public GUIContent scale = EditorGUIUtility.TextContent("Scale|Scale the emission shape.");

            public GUIContent[] shapeTypes = new GUIContent[]
            {
                EditorGUIUtility.TextContent("Sphere"),
                EditorGUIUtility.TextContent("Hemisphere"),
                EditorGUIUtility.TextContent("Cone"),
                EditorGUIUtility.TextContent("Donut"),
                EditorGUIUtility.TextContent("Box"),
                EditorGUIUtility.TextContent("Mesh"),
                EditorGUIUtility.TextContent("Mesh Renderer"),
                EditorGUIUtility.TextContent("Skinned Mesh Renderer"),
                EditorGUIUtility.TextContent("Circle"),
                EditorGUIUtility.TextContent("Edge")
            };

            public GUIContent[] boxTypes = new GUIContent[]
            {
                EditorGUIUtility.TextContent("Volume"),
                EditorGUIUtility.TextContent("Shell"),
                EditorGUIUtility.TextContent("Edge")
            };

            public GUIContent[] coneTypes = new GUIContent[]
            {
                EditorGUIUtility.TextContent("Base"),
                EditorGUIUtility.TextContent("Volume")
            };

            public GUIContent[] meshTypes = new GUIContent[]
            {
                EditorGUIUtility.TextContent("Vertex"),
                EditorGUIUtility.TextContent("Edge"),
                EditorGUIUtility.TextContent("Triangle")
            };

            public GUIContent[] emissionModes = new GUIContent[]
            {
                EditorGUIUtility.TextContent("Random"),
                EditorGUIUtility.TextContent("Loop"),
                EditorGUIUtility.TextContent("Ping-Pong"),
                EditorGUIUtility.TextContent("Burst Spread")
            };
        }

        static Texts s_Texts;

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
            m_RadiusThickness = GetProperty("radiusThickness");
            m_Angle = GetProperty("angle");
            m_Length = GetProperty("length");
            m_BoxThickness = GetProperty("boxThickness");
            m_Arc = MultiModeParameter.GetProperty(this, "arc", s_ArcTexts.speed);
            m_DonutRadius = GetProperty("donutRadius");

            m_PlacementMode = GetProperty("placementMode");
            m_Mesh = GetProperty("m_Mesh");
            m_MeshRenderer = GetProperty("m_MeshRenderer");
            m_SkinnedMeshRenderer = GetProperty("m_SkinnedMeshRenderer");
            m_MeshMaterialIndex = GetProperty("m_MeshMaterialIndex");
            m_UseMeshMaterialIndex = GetProperty("m_UseMeshMaterialIndex");
            m_UseMeshColors = GetProperty("m_UseMeshColors");
            m_MeshNormalOffset = GetProperty("m_MeshNormalOffset");
            m_RandomDirectionAmount = GetProperty("randomDirectionAmount");
            m_SphericalDirectionAmount = GetProperty("sphericalDirectionAmount");
            m_RandomPositionAmount = GetProperty("randomPositionAmount");
            m_AlignToDirection = GetProperty("alignToDirection");

            m_Position = GetProperty("m_Position");
            m_Scale = GetProperty("m_Scale");
            m_Rotation = GetProperty("m_Rotation");

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

        private ParticleSystemShapeType ConvertBoxEmitFromToBoxType(int emitFrom)
        {
            return boxShapes[emitFrom];
        }

        private int ConvertBoxTypeToBoxEmitFrom(ParticleSystemShapeType shapeType)
        {
            return System.Array.IndexOf(boxShapes, shapeType);
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            EditorGUI.showMixedValue = m_Type.hasMultipleDifferentValues;
            int type = m_Type.intValue;
            int index = m_TypeToGuiTypeIndex[type];

            EditorGUI.BeginChangeCheck();
            int index2 = GUIPopup(s_Texts.shape, index, s_Texts.shapeTypes);
            bool shapeTypeChanged = EditorGUI.EndChangeCheck();

            EditorGUI.showMixedValue = false;

            ParticleSystemShapeType guiType = m_GuiTypes[index2];
            if (index2 != index)
            {
                type = (int)guiType;
            }

            if (!m_Type.hasMultipleDifferentValues)
            {
                switch (guiType)
                {
                    case ParticleSystemShapeType.Box:
                    {
                        int emitFrom = ConvertBoxTypeToBoxEmitFrom((ParticleSystemShapeType)type);
                        emitFrom = GUIPopup(s_Texts.emitFrom, emitFrom, s_Texts.boxTypes);
                        type = (int)ConvertBoxEmitFromToBoxType(emitFrom);

                        if (type == (int)ParticleSystemShapeType.BoxShell || type == (int)ParticleSystemShapeType.BoxEdge)
                            GUIVector3Field(s_Texts.boxThickness, m_BoxThickness);
                    }
                    break;

                    case ParticleSystemShapeType.Cone:
                    {
                        GUIFloat(s_Texts.coneAngle, m_Angle);
                        GUIFloat(s_Texts.radius, m_Radius.m_Value);
                        GUIFloat(s_Texts.radiusThickness, m_RadiusThickness);

                        m_Arc.OnInspectorGUI(s_ArcTexts);

                        bool showLength = (type != (int)ParticleSystemShapeType.ConeVolume);
                        using (new EditorGUI.DisabledScope(showLength))
                        {
                            GUIFloat(s_Texts.coneLength, m_Length);
                        }

                        int emitFrom = ConvertConeTypeToConeEmitFrom((ParticleSystemShapeType)type);
                        emitFrom = GUIPopup(s_Texts.emitFrom, emitFrom, s_Texts.coneTypes);
                        type = (int)ConvertConeEmitFromToConeType(emitFrom);
                    }
                    break;

                    case ParticleSystemShapeType.Donut:
                    {
                        GUIFloat(s_Texts.radius, m_Radius.m_Value);
                        GUIFloat(s_Texts.donutRadius, m_DonutRadius);
                        GUIFloat(s_Texts.radiusThickness, m_RadiusThickness);

                        m_Arc.OnInspectorGUI(s_ArcTexts);
                    }
                    break;

                    case ParticleSystemShapeType.Mesh:
                    case ParticleSystemShapeType.MeshRenderer:
                    case ParticleSystemShapeType.SkinnedMeshRenderer:
                    {
                        GUIPopup(s_Texts.meshType, m_PlacementMode, s_Texts.meshTypes);

                        Material material = null;
                        Mesh srcMesh = null;
                        if (guiType == ParticleSystemShapeType.Mesh)
                        {
                            GUIObject(s_Texts.mesh, m_Mesh);
                            srcMesh = (Mesh)m_Mesh.objectReferenceValue;
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
                    }
                    break;

                    case ParticleSystemShapeType.Sphere:
                    case ParticleSystemShapeType.Hemisphere:
                    {
                        GUIFloat(s_Texts.radius, m_Radius.m_Value);
                        GUIFloat(s_Texts.radiusThickness, m_RadiusThickness);
                    }
                    break;

                    case ParticleSystemShapeType.Circle:
                    {
                        GUIFloat(s_Texts.radius, m_Radius.m_Value);
                        GUIFloat(s_Texts.radiusThickness, m_RadiusThickness);

                        m_Arc.OnInspectorGUI(s_ArcTexts);
                    }
                    break;

                    case ParticleSystemShapeType.SingleSidedEdge:
                    {
                        m_Radius.OnInspectorGUI(s_RadiusTexts);
                    }
                    break;
                }
            }

            if (shapeTypeChanged || !m_Type.hasMultipleDifferentValues)
                m_Type.intValue = type;

            EditorGUILayout.Space();

            GUIVector3Field(s_Texts.position, m_Position);
            GUIVector3Field(s_Texts.rotation, m_Rotation);
            GUIVector3Field(s_Texts.scale, m_Scale);

            EditorGUILayout.Space();

            GUIToggle(s_Texts.alignToDirection, m_AlignToDirection);
            GUIFloat(s_Texts.randomDirectionAmount, m_RandomDirectionAmount);
            GUIFloat(s_Texts.sphericalDirectionAmount, m_SphericalDirectionAmount);
            GUIFloat(s_Texts.randomPositionAmount, m_RandomPositionAmount);
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

                Matrix4x4 transformMatrix = new Matrix4x4();
                if (mainModule.scalingMode == ParticleSystemScalingMode.Local)
                {
                    transformMatrix.SetTRS(ps.transform.position, ps.transform.rotation, ps.transform.localScale);
                }
                else if (mainModule.scalingMode == ParticleSystemScalingMode.Hierarchy)
                {
                    transformMatrix = ps.transform.localToWorldMatrix;
                }
                else
                {
                    transformMatrix.SetTRS(ps.transform.position, ps.transform.rotation, ps.transform.lossyScale);
                }

                bool isBox = (type == ParticleSystemShapeType.Box || type == ParticleSystemShapeType.BoxShell || type == ParticleSystemShapeType.BoxEdge);

                Vector3 emitterScale = isBox ? Vector3.one : shapeModule.scale;
                Matrix4x4 emitterMatrix = Matrix4x4.TRS(shapeModule.position, Quaternion.Euler(shapeModule.rotation), emitterScale);
                transformMatrix *= emitterMatrix;
                Handles.matrix = transformMatrix;

                if (type == ParticleSystemShapeType.Sphere)
                {
                    EditorGUI.BeginChangeCheck();
                    float radius = Handles.DoSimpleRadiusHandle(Quaternion.identity, Vector3.zero, shapeModule.radius, false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, "Sphere Handle Change");
                        shapeModule.radius = radius;
                    }
                }
                else if (type == ParticleSystemShapeType.Circle)
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
                else if (type == ParticleSystemShapeType.Hemisphere)
                {
                    EditorGUI.BeginChangeCheck();
                    float radius = Handles.DoSimpleRadiusHandle(Quaternion.identity, Vector3.zero, shapeModule.radius, true);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, "Hemisphere Handle Change");
                        shapeModule.radius = radius;
                    }
                }
                else if (type == ParticleSystemShapeType.Cone)
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
                else if (type == ParticleSystemShapeType.ConeVolume)
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
                    m_BoxBoundsHandle.size = shapeModule.scale;
                    m_BoxBoundsHandle.DrawHandle();

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, "Box Handle Change");
                        shapeModule.scale = m_BoxBoundsHandle.size;
                    }
                }
                else if (type == ParticleSystemShapeType.Donut)
                {
                    // radius
                    EditorGUI.BeginChangeCheck();

                    m_ArcHandle.radius = shapeModule.radius;
                    m_ArcHandle.angle = shapeModule.arc;
                    m_ArcHandle.SetColorWithRadiusHandle(Color.white, 0f);
                    m_ArcHandle.wireframeColor = Color.clear;

                    using (new Handles.DrawingScope(Handles.matrix * s_ArcHandleOffsetMatrix))
                        m_ArcHandle.DrawHandle();

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, "Donut Handle Change");
                        shapeModule.radius = m_ArcHandle.radius;
                        shapeModule.arc = m_ArcHandle.angle;
                    }

                    // donut extents
                    using (new Handles.DrawingScope(Handles.matrix * s_ArcHandleOffsetMatrix))
                    {
                        float excessAngle = shapeModule.arc % 360f;
                        float angle = Mathf.Abs(shapeModule.arc) >= 360f ? 360f : excessAngle;

                        Handles.DrawWireArc(new Vector3(0.0f, shapeModule.donutRadius, 0.0f), Vector3.up, Vector3.forward, angle, shapeModule.radius);
                        Handles.DrawWireArc(new Vector3(0.0f, -shapeModule.donutRadius, 0.0f), Vector3.up, Vector3.forward, angle, shapeModule.radius);
                        Handles.DrawWireArc(Vector3.zero, Vector3.up, Vector3.forward, angle, shapeModule.radius + shapeModule.donutRadius);
                        Handles.DrawWireArc(Vector3.zero, Vector3.up, Vector3.forward, angle, shapeModule.radius - shapeModule.donutRadius);

                        if (shapeModule.arc != 360.0f)
                        {
                            Quaternion arcRotation = Quaternion.AngleAxis(shapeModule.arc, Vector3.up);
                            Vector3 capCenter = arcRotation * Vector3.forward * shapeModule.radius;
                            Handles.DrawWireDisc(capCenter, arcRotation * Vector3.right, shapeModule.donutRadius);
                        }
                    }

                    // donut radius
                    m_SphereBoundsHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;
                    m_SphereBoundsHandle.radius = shapeModule.donutRadius;
                    m_SphereBoundsHandle.center = Vector3.zero;
                    m_SphereBoundsHandle.SetColor(Color.white);

                    float handleInterval = 90.0f;
                    int numOuterRadii = Mathf.Max(1, (int)Mathf.Ceil(shapeModule.arc / handleInterval));
                    Matrix4x4 donutRadiusStartMatrix = Matrix4x4.TRS(new Vector3(shapeModule.radius, 0.0f, 0.0f), Quaternion.Euler(90.0f, 0.0f, 0.0f), Vector3.one);
                    for (int i = 0; i < numOuterRadii; i++)
                    {
                        EditorGUI.BeginChangeCheck();
                        using (new Handles.DrawingScope(Handles.matrix * (Matrix4x4.Rotate(Quaternion.Euler(0.0f, 0.0f, handleInterval * i)) * donutRadiusStartMatrix)))
                            m_SphereBoundsHandle.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(ps, "Donut Radius Handle Change");
                            shapeModule.donutRadius = m_SphereBoundsHandle.radius;
                        }
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
                        Graphics.DrawMeshNow(mesh, transformMatrix);
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
