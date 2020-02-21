// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

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

            public enum ValueMode { Random, Loop, PingPong, BurstSpread }

            public static MultiModeParameter GetProperty(ModuleUI ui, string name, GUIContent speed, bool skipValue = false)
            {
                MultiModeParameter result = new MultiModeParameter();
                if (!skipValue)
                    result.m_Value = ui.GetProperty(name + ".value");
                result.m_Mode = ui.GetProperty(name + ".mode");
                result.m_Spread = ui.GetProperty(name + ".spread");
                result.m_Speed = new SerializedMinMaxCurve(ui, speed, name + ".speed", kUseSignedRange);
                result.m_Speed.m_AllowRandom = false;
                return result;
            }

            public void OnInspectorGUI(MultiModeTexts text, GUIContent[] modesText, bool showSpread = true, bool showSpeed = true)
            {
                if (m_Value != null)
                    GUIFloat(text.value, m_Value);

                EditorGUI.indentLevel++;

                GUIPopup(text.mode, m_Mode, modesText);
                if (showSpread)
                    GUIFloat(text.spread, m_Spread);

                if (showSpeed && !m_Mode.hasMultipleDifferentValues)
                {
                    ValueMode mode = (ValueMode)m_Mode.intValue;
                    if (mode == ValueMode.Loop || mode == ValueMode.PingPong)
                    {
                        GUIMinMaxCurve(text.speed, m_Speed);
                    }
                }

                EditorGUI.indentLevel--;
            }
        }

        SerializedProperty m_Type;
        SerializedProperty m_RandomDirectionAmount;
        SerializedProperty m_SphericalDirectionAmount;
        SerializedProperty m_RandomPositionAmount;
        SerializedProperty m_AlignToDirection;

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
        SerializedProperty m_Sprite;
        SerializedProperty m_SpriteRenderer;
        SerializedProperty m_MeshMaterialIndex;
        SerializedProperty m_UseMeshMaterialIndex;
        SerializedProperty m_UseMeshColors;
        SerializedProperty m_MeshNormalOffset;
        MultiModeParameter m_MeshSpawn;


        // texture properties
        SerializedProperty m_Texture;
        SerializedProperty m_TextureClipChannel;
        SerializedProperty m_TextureClipThreshold;
        SerializedProperty m_TextureColorAffectsParticles;
        SerializedProperty m_TextureAlphaAffectsParticles;
        SerializedProperty m_TextureBilinearFiltering;
        SerializedProperty m_TextureUVChannel;

        // internal
        private static Material s_Material;
        private static Material s_TextureMaterial;
        private static Material s_SphereTextureMaterial;
        private static Mesh s_CircleMesh;
        private static Mesh s_QuadMesh;
        private static Mesh s_SphereMesh;
        private static Mesh s_HemisphereMesh;
        private static readonly Matrix4x4 s_ArcHandleOffsetMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.AngleAxis(90f, Vector3.right) * Quaternion.AngleAxis(90f, Vector3.up), Vector3.one);
        private ArcHandle m_ArcHandle = new ArcHandle();
        private BoxBoundsHandle m_BoxBoundsHandle = new BoxBoundsHandle();
        private SphereBoundsHandle m_SphereBoundsHandle = new SphereBoundsHandle();
        private static PrefColor s_GizmoColor = new PrefColor("Particle System/Shape Module Gizmos", 148f / 255f, 229f / 255f, 1f, 0.9f);
        private static Color s_ShapeGizmoThicknessTint = new Color(0.7f, 0.7f, 0.7f, 1.0f);

        private Mesh m_SpriteMesh;

        private readonly ParticleSystemShapeType[] m_GuiTypes = new[] { ParticleSystemShapeType.Sphere, ParticleSystemShapeType.Hemisphere, ParticleSystemShapeType.Cone, ParticleSystemShapeType.Donut, ParticleSystemShapeType.Box, ParticleSystemShapeType.Mesh, ParticleSystemShapeType.MeshRenderer, ParticleSystemShapeType.SkinnedMeshRenderer, ParticleSystemShapeType.Sprite, ParticleSystemShapeType.SpriteRenderer, ParticleSystemShapeType.Circle, ParticleSystemShapeType.SingleSidedEdge, ParticleSystemShapeType.Rectangle };
        private readonly int[] m_TypeToGuiTypeIndex = new[] { 0, 0, 1, 1, 2, 4, 5, 2, 2, 2, 10, 10, 11, 6, 7, 4, 4, 3, 12, 8, 9 };

        private readonly ParticleSystemShapeType[] boxShapes = new ParticleSystemShapeType[] { ParticleSystemShapeType.Box, ParticleSystemShapeType.BoxShell, ParticleSystemShapeType.BoxEdge };
        private readonly ParticleSystemShapeType[] coneShapes = new ParticleSystemShapeType[] { ParticleSystemShapeType.Cone, ParticleSystemShapeType.ConeVolume };

        static EditMode.SceneViewEditMode[] s_SceneViewEditModes = new[]
        {
            EditMode.SceneViewEditMode.ParticleSystemShapeModuleGizmo,
            EditMode.SceneViewEditMode.ParticleSystemShapeModulePosition,
            EditMode.SceneViewEditMode.ParticleSystemShapeModuleRotation,
            EditMode.SceneViewEditMode.ParticleSystemShapeModuleScale
        };

        class Texts
        {
            public GUIContent shape = EditorGUIUtility.TrTextContent("Shape", "Defines the shape of the volume from which particles can be emitted, and the direction of the start velocity.");
            public GUIContent radius = EditorGUIUtility.TrTextContent("Radius", "Radius of the shape.");
            public GUIContent radiusThickness = EditorGUIUtility.TrTextContent("Radius Thickness", "Control the thickness of the spawn volume, from 0 to 1.");
            public GUIContent coneAngle = EditorGUIUtility.TrTextContent("Angle", "Angle of the cone.");
            public GUIContent coneLength = EditorGUIUtility.TrTextContent("Length", "Length of the cone.");
            public GUIContent boxThickness = EditorGUIUtility.TrTextContent("Box Thickness", "When using shell/edge modes, control the thickness of the spawn volume, from 0 to 1.");
            public GUIContent meshType = EditorGUIUtility.TrTextContent("Type", "Generate particles from vertices, edges or triangles.");
            public GUIContent mesh = EditorGUIUtility.TrTextContent("Mesh", "Mesh that the particle system will emit from.");
            public GUIContent meshRenderer = EditorGUIUtility.TrTextContent("Mesh", "MeshRenderer that the particle system will emit from.");
            public GUIContent skinnedMeshRenderer = EditorGUIUtility.TrTextContent("Mesh", "SkinnedMeshRenderer that the particle system will emit from.");
            public GUIContent sprite = EditorGUIUtility.TrTextContent("Sprite", "Sprite that the particle system will emit from.");
            public GUIContent spriteRenderer = EditorGUIUtility.TrTextContent("Sprite", "SpriteRenderer that the particle system will emit from.");
            public GUIContent meshMaterialIndex = EditorGUIUtility.TrTextContent("Single Material", "Only emit from a specific material of the mesh.");
            public GUIContent useMeshColors = EditorGUIUtility.TrTextContent("Use Mesh Colors", "Modulate particle color with mesh vertex colors, or if they don't exist, use the shader color property \"_Color\" or \"_TintColor\" from the material. Does not read texture colors.");
            public GUIContent meshNormalOffset = EditorGUIUtility.TrTextContent("Normal Offset", "Offset particle spawn positions along the mesh normal.");
            public GUIContent texture = EditorGUIUtility.TrTextContent("Texture", "Texture that the particles will sample their color from.");
            public GUIContent textureClipChannel = EditorGUIUtility.TrTextContent("Clip Channel", "Select a channel to use for discarding particles.");
            public GUIContent textureClipThreshold = EditorGUIUtility.TrTextContent("Clip Threshold", "Only emit from parts of the texture where the Clip Channel is greater than or equal to this value.");
            public GUIContent textureColorAffectsParticles = EditorGUIUtility.TrTextContent("Color affects Particles", "Multiply the particle color by the texture RGB value.");
            public GUIContent textureAlphaAffectsParticles = EditorGUIUtility.TrTextContent("Alpha affects Particles", "Multiply the particle alpha by the texture alpha value.");
            public GUIContent textureBilinearFiltering = EditorGUIUtility.TrTextContent("Bilinear Filtering", "Blend between pixels on the texture.");
            public GUIContent textureUVChannel = EditorGUIUtility.TrTextContent("UV Channel", "Use the selected UV channel from the source mesh, for reading the texture.");
            public GUIContent alignToDirection = EditorGUIUtility.TrTextContent("Align To Direction", "Automatically align particles based on their initial direction of travel.");
            public GUIContent randomDirectionAmount = EditorGUIUtility.TrTextContent("Randomize Direction", "Override the initial direction of travel with a random direction.");
            public GUIContent sphericalDirectionAmount = EditorGUIUtility.TrTextContent("Spherize Direction", "Override the initial direction of travel with a direction that projects particles outwards from the center of the Shape Transform.");
            public GUIContent randomPositionAmount = EditorGUIUtility.TrTextContent("Randomize Position", "Move the starting positions by a random amount, up to this maximum value.");
            public GUIContent emitFrom = EditorGUIUtility.TrTextContent("Emit from:", "Specifies from where particles are emitted.");
            public GUIContent donutRadius = EditorGUIUtility.TrTextContent("Donut Radius", "The radius of the donut. Used to control the thickness of the ring.");
            public GUIContent position = EditorGUIUtility.TrTextContent("Position", "Translate the emission shape.");
            public GUIContent rotation = EditorGUIUtility.TrTextContent("Rotation", "Rotate the emission shape.");
            public GUIContent scale = EditorGUIUtility.TrTextContent("Scale", "Scale the emission shape.");

            public readonly string undoSphereThickness = L10n.Tr("Sphere Thickness Handle Change");
            public readonly string undoSphere = L10n.Tr("Sphere Handle Change");
            public readonly string undoCircleThickness = L10n.Tr("Circle Thickness Handle Change");
            public readonly string undoCircle = L10n.Tr("Circle Handle Change");
            public readonly string undoHemisphereThickness = L10n.Tr("Hemisphere Thickness Handle Change");
            public readonly string undoHemisphere = L10n.Tr("Hemisphere Handle Change");
            public readonly string undoConeThickness = L10n.Tr("Cone Thickness Handle Change");
            public readonly string undoCone = L10n.Tr("Cone Handle Change");
            public readonly string undoConeVolumeThickness = L10n.Tr("Cone Volume Thickness Handle Change");
            public readonly string undoConeVolume = L10n.Tr("Cone Volume Handle Change");
            public readonly string undoBox = L10n.Tr("Box Handle Change");
            public readonly string undoDonut = L10n.Tr("Donut Handle Change");
            public readonly string undoDonutRadiusThickness = L10n.Tr("Donut Radius Thickness Handle Change");
            public readonly string undoDonutRadius = L10n.Tr("Donut Radius Handle Change");
            public readonly string undoEdge = L10n.Tr("Edge Handle Change");
            public readonly string undoRectangle = L10n.Tr("Rectangle Handle Change");
            public readonly string undoTransform = L10n.Tr("Shape Transform Change");

            public GUIContent[] shapeTypes = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("Sphere"),
                EditorGUIUtility.TrTextContent("Hemisphere"),
                EditorGUIUtility.TrTextContent("Cone"),
                EditorGUIUtility.TrTextContent("Donut"),
                EditorGUIUtility.TrTextContent("Box"),
                EditorGUIUtility.TrTextContent("Mesh"),
                EditorGUIUtility.TrTextContent("Mesh Renderer"),
                EditorGUIUtility.TrTextContent("Skinned Mesh Renderer"),
                EditorGUIUtility.TextContent("Sprite"),
                EditorGUIUtility.TextContent("Sprite Renderer"),
                EditorGUIUtility.TrTextContent("Circle"),
                EditorGUIUtility.TrTextContent("Edge"),
                EditorGUIUtility.TrTextContent("Rectangle")
            };

            public GUIContent[] boxTypes = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("Volume"),
                EditorGUIUtility.TrTextContent("Shell"),
                EditorGUIUtility.TrTextContent("Edge")
            };

            public GUIContent[] coneTypes = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("Base"),
                EditorGUIUtility.TrTextContent("Volume")
            };

            public GUIContent[] meshTypes = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("Vertex"),
                EditorGUIUtility.TrTextContent("Edge"),
                EditorGUIUtility.TrTextContent("Triangle")
            };

            public GUIContent[] emissionModes = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("Random"),
                EditorGUIUtility.TrTextContent("Loop"),
                EditorGUIUtility.TrTextContent("Ping-Pong"),
                EditorGUIUtility.TrTextContent("Burst Spread")
            };

            public GUIContent[] emissionModesMesh = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("Random"),
                EditorGUIUtility.TrTextContent("Loop"),
                EditorGUIUtility.TrTextContent("Ping-Pong")
            };

            public GUIContent[] textureClipChannels = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("Red"),
                EditorGUIUtility.TrTextContent("Green"),
                EditorGUIUtility.TrTextContent("Blue"),
                EditorGUIUtility.TrTextContent("Alpha")
            };

            public GUIContent[] toolContents =
            {
                EditorGUIUtility.TrIconContent("ParticleShapeTool", "Shape gizmo editing mode."),
                EditorGUIUtility.TrIconContent("MoveTool", "Shape transform position editing mode."),
                EditorGUIUtility.TrIconContent("RotateTool", "Shape transform rotation editing mode."),
                EditorGUIUtility.TrIconContent("ScaleTool", "Shape transform scale editing mode.")
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

        static MultiModeTexts s_MeshTexts = new MultiModeTexts(
            /*_value:*/ null,
            /*_mode:*/ "Mode|Control how particles are spawned across the mesh.",
            /*_spread:*/ "Spread|Spawn particles only at specific positions across the mesh (0 to disable).",
            /*_speed:*/ "Speed|Control the speed that the emission position moves along the edges of the mesh.");

        public ShapeModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "ShapeModule", displayName, VisibilityState.VisibleAndFolded)
        {
            m_ToolTip = "Shape of the emitter volume, which controls where particles are emitted and their initial direction.";
        }

        private bool editToolActive
        {
            get
            {
                if (EditMode.IsOwner(m_ParticleSystemUI.m_ParticleEffectUI.m_Owner.customEditor))
                {
                    if (EditMode.editMode == EditMode.SceneViewEditMode.ParticleSystemShapeModuleGizmo ||
                        EditMode.editMode == EditMode.SceneViewEditMode.ParticleSystemShapeModulePosition ||
                        EditMode.editMode == EditMode.SceneViewEditMode.ParticleSystemShapeModuleRotation ||
                        EditMode.editMode == EditMode.SceneViewEditMode.ParticleSystemShapeModuleScale)
                        return true;
                }
                return false;
            }
            set
            {
                if (!value && editToolActive)
                    EditMode.QuitEditMode();
                SceneView.RepaintAll();
            }
        }

        protected override void SetVisibilityState(VisibilityState newState)
        {
            base.SetVisibilityState(newState);

            // Show tools again when module is not visible
            if (newState != VisibilityState.VisibleAndFoldedOut)
                editToolActive = false;
        }

        protected override void OnModuleDisable()
        {
            base.OnModuleDisable();
            editToolActive = false;
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
            m_Sprite = GetProperty("m_Sprite");
            m_SpriteRenderer = GetProperty("m_SpriteRenderer");
            m_MeshMaterialIndex = GetProperty("m_MeshMaterialIndex");
            m_UseMeshMaterialIndex = GetProperty("m_UseMeshMaterialIndex");
            m_UseMeshColors = GetProperty("m_UseMeshColors");
            m_MeshNormalOffset = GetProperty("m_MeshNormalOffset");
            m_MeshSpawn = MultiModeParameter.GetProperty(this, "m_MeshSpawn", s_MeshTexts.speed, true);
            m_Texture = GetProperty("m_Texture");
            m_TextureClipChannel = GetProperty("m_TextureClipChannel");
            m_TextureClipThreshold = GetProperty("m_TextureClipThreshold");
            m_TextureColorAffectsParticles = GetProperty("m_TextureColorAffectsParticles");
            m_TextureAlphaAffectsParticles = GetProperty("m_TextureAlphaAffectsParticles");
            m_TextureBilinearFiltering = GetProperty("m_TextureBilinearFiltering");
            m_TextureUVChannel = GetProperty("m_TextureUVChannel");
            m_RandomDirectionAmount = GetProperty("randomDirectionAmount");
            m_SphericalDirectionAmount = GetProperty("sphericalDirectionAmount");
            m_RandomPositionAmount = GetProperty("randomPositionAmount");
            m_AlignToDirection = GetProperty("alignToDirection");

            m_Position = GetProperty("m_Position");
            m_Scale = GetProperty("m_Scale");
            m_Rotation = GetProperty("m_Rotation");

            if (!s_Material)
                s_Material = Material.GetDefaultMaterial();
            if (!s_TextureMaterial)
            {
                s_TextureMaterial = new Material((Shader)EditorGUIUtility.Load("SceneView/ParticleShapeGizmo.shader"));
                s_TextureMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
            if (!s_SphereTextureMaterial)
            {
                s_SphereTextureMaterial = new Material((Shader)EditorGUIUtility.Load("SceneView/ParticleShapeGizmoSphere.shader"));
                s_SphereTextureMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
            if (!s_CircleMesh)
                s_CircleMesh = ((GameObject)EditorGUIUtility.Load("SceneView/Circle.fbx")).transform.GetComponent<MeshFilter>().sharedMesh;
            if (!s_QuadMesh)
                s_QuadMesh = Resources.GetBuiltinResource(typeof(Mesh), "Quad.fbx") as Mesh;
            if (!s_SphereMesh)
                s_SphereMesh = Resources.GetBuiltinResource(typeof(Mesh), "New-Sphere.fbx") as Mesh;
            if (!s_HemisphereMesh)
                s_HemisphereMesh = ((GameObject)EditorGUIUtility.Load("SceneView/Hemisphere.fbx")).transform.GetComponent<MeshFilter>().sharedMesh;
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
            int index2 = GUIPopup(s_Texts.shape, index, s_Texts.shapeTypes, m_Type);
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
                        emitFrom = GUIPopup(s_Texts.emitFrom, emitFrom, s_Texts.boxTypes, m_Type);
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

                        m_Arc.OnInspectorGUI(s_ArcTexts, s_Texts.emissionModes);

                        bool showLength = (type != (int)ParticleSystemShapeType.ConeVolume);
                        using (new EditorGUI.DisabledScope(showLength))
                        {
                            GUIFloat(s_Texts.coneLength, m_Length);
                        }

                        int emitFrom = ConvertConeTypeToConeEmitFrom((ParticleSystemShapeType)type);
                        emitFrom = GUIPopup(s_Texts.emitFrom, emitFrom, s_Texts.coneTypes, m_Type);
                        type = (int)ConvertConeEmitFromToConeType(emitFrom);
                    }
                    break;

                    case ParticleSystemShapeType.Donut:
                    {
                        GUIFloat(s_Texts.radius, m_Radius.m_Value);
                        GUIFloat(s_Texts.donutRadius, m_DonutRadius);
                        GUIFloat(s_Texts.radiusThickness, m_RadiusThickness);

                        m_Arc.OnInspectorGUI(s_ArcTexts, s_Texts.emissionModes);
                    }
                    break;

                    case ParticleSystemShapeType.Mesh:
                    case ParticleSystemShapeType.MeshRenderer:
                    case ParticleSystemShapeType.SkinnedMeshRenderer:
                    {
                        ParticleSystemMeshShapeType placementMode = (ParticleSystemMeshShapeType)GUIPopup(s_Texts.meshType, m_PlacementMode, s_Texts.meshTypes);

                        if (!m_PlacementMode.hasMultipleDifferentValues && placementMode != ParticleSystemMeshShapeType.Triangle)
                        {
                            bool showSpeedAndSpread = (placementMode != ParticleSystemMeshShapeType.Vertex);
                            m_MeshSpawn.OnInspectorGUI(s_MeshTexts, s_Texts.emissionModesMesh, showSpeedAndSpread, showSpeedAndSpread);
                        }

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
                                if (!material.HasProperty(colorName) && !material.HasProperty(tintColorName) && !srcMesh.HasVertexAttribute(VertexAttribute.Color))
                                {
                                    GUIContent warning = EditorGUIUtility.TrTextContent("To use mesh colors, your source mesh must either provide vertex colors, or its shader must contain a color property named \"_Color\" or \"_TintColor\".");
                                    EditorGUILayout.HelpBox(warning.text, MessageType.Warning, true);
                                }
                            }
                        }

                        GUIFloat(s_Texts.meshNormalOffset, m_MeshNormalOffset);
                    }
                    break;

                    case ParticleSystemShapeType.Sprite:
                    case ParticleSystemShapeType.SpriteRenderer:
                    {
                        GUIPopup(s_Texts.meshType, m_PlacementMode, s_Texts.meshTypes);

                        if (guiType == ParticleSystemShapeType.Sprite)
                            GUIObject(s_Texts.sprite, m_Sprite);
                        else if (guiType == ParticleSystemShapeType.SpriteRenderer)
                            GUIObject(s_Texts.spriteRenderer, m_SpriteRenderer);

                        GUIFloat(s_Texts.meshNormalOffset, m_MeshNormalOffset);
                    }
                    break;

                    case ParticleSystemShapeType.Sphere:
                    case ParticleSystemShapeType.Hemisphere:
                    {
                        GUIFloat(s_Texts.radius, m_Radius.m_Value);
                        GUIFloat(s_Texts.radiusThickness, m_RadiusThickness);

                        m_Arc.OnInspectorGUI(s_ArcTexts, s_Texts.emissionModes);
                    }
                    break;

                    case ParticleSystemShapeType.Circle:
                    {
                        GUIFloat(s_Texts.radius, m_Radius.m_Value);
                        GUIFloat(s_Texts.radiusThickness, m_RadiusThickness);

                        m_Arc.OnInspectorGUI(s_ArcTexts, s_Texts.emissionModes);
                    }
                    break;

                    case ParticleSystemShapeType.SingleSidedEdge:
                    {
                        m_Radius.OnInspectorGUI(s_RadiusTexts, s_Texts.emissionModes);
                    }
                    break;

                    case ParticleSystemShapeType.Rectangle:
                        break;
                }
            }

            if (shapeTypeChanged || !m_Type.hasMultipleDifferentValues)
                m_Type.intValue = type;

            OnTextureInspectorGUI();
            OnTransformInspectorGUI();
            OnMiscInspectorGUI();

            GUIButtonGroup(s_SceneViewEditModes, s_Texts.toolContents, m_ParticleSystemUI.GetBounds, m_ParticleSystemUI.m_ParticleEffectUI.m_Owner.customEditor);
        }

        private void OnTextureInspectorGUI()
        {
            if (m_Texture.hasMultipleDifferentValues || m_Texture.objectReferenceValue != null)
            {
                EditorGUILayout.Space();
                GUIObject(s_Texts.texture, m_Texture, typeof(Texture2D));
                GUIPopup(s_Texts.textureClipChannel, m_TextureClipChannel, s_Texts.textureClipChannels);
                GUIFloat(s_Texts.textureClipThreshold, m_TextureClipThreshold);
                GUIToggle(s_Texts.textureColorAffectsParticles, m_TextureColorAffectsParticles);
                GUIToggle(s_Texts.textureAlphaAffectsParticles, m_TextureAlphaAffectsParticles);
                GUIToggle(s_Texts.textureBilinearFiltering, m_TextureBilinearFiltering);

                if (!m_Type.hasMultipleDifferentValues)
                {
                    if ((m_Type.intValue == (int)ParticleSystemShapeType.Mesh) || (m_Type.intValue == (int)ParticleSystemShapeType.MeshRenderer) || (m_Type.intValue == (int)ParticleSystemShapeType.SkinnedMeshRenderer))
                        GUIInt(s_Texts.textureUVChannel, m_TextureUVChannel);
                }
            }
            else
            {
                GUIObject(s_Texts.texture, m_Texture, typeof(Texture2D));
            }
        }

        private void OnTransformInspectorGUI()
        {
            EditorGUILayout.Space();
            GUIVector3Field(s_Texts.position, m_Position);
            GUIVector3Field(s_Texts.rotation, m_Rotation);
            GUIVector3Field(s_Texts.scale, m_Scale);
        }

        private void OnMiscInspectorGUI()
        {
            EditorGUILayout.Space();
            GUIToggle(s_Texts.alignToDirection, m_AlignToDirection);
            GUIFloat(s_Texts.randomDirectionAmount, m_RandomDirectionAmount);
            GUIFloat(s_Texts.sphericalDirectionAmount, m_SphericalDirectionAmount);
            GUIFloat(s_Texts.randomPositionAmount, m_RandomPositionAmount);
        }

        override public void OnSceneViewGUI()
        {
            Color origCol = Handles.color;
            Handles.color = s_GizmoColor;

            Matrix4x4 orgMatrix = Handles.matrix;

            EditorGUI.BeginChangeCheck();

            bool allowGizmoEditing = editToolActive && EditMode.editMode == EditMode.SceneViewEditMode.ParticleSystemShapeModuleGizmo;

            foreach (ParticleSystem ps in m_ParticleSystemUI.m_ParticleSystems)
            {
                var shapeModule = ps.shape;
                var mainModule = ps.main;

                ParticleSystemShapeType type = shapeModule.shapeType;

                Matrix4x4 transformMatrix = new Matrix4x4();
                if (mainModule.scalingMode == ParticleSystemScalingMode.Local)
                    transformMatrix.SetTRS(ps.transform.position, ps.transform.rotation, ps.transform.localScale);
                else
                    transformMatrix = ps.transform.localToWorldMatrix;

                bool isBox = (type == ParticleSystemShapeType.Box || type == ParticleSystemShapeType.BoxShell || type == ParticleSystemShapeType.BoxEdge || type == ParticleSystemShapeType.Rectangle);

                Vector3 emitterScale = isBox ? Vector3.one : shapeModule.scale;
                Matrix4x4 emitterMatrix = Matrix4x4.TRS(shapeModule.position, Quaternion.Euler(shapeModule.rotation), emitterScale);
                Matrix4x4 combinedMatrix = transformMatrix * emitterMatrix;
                Handles.matrix = combinedMatrix;

                Color gizmoEditingColor = new Color(1.0f, 1.0f, 1.0f, allowGizmoEditing && !Event.current.alt ? 1.0f : 0.0f);

                if (type == ParticleSystemShapeType.Sphere)
                {
                    // Thickness
                    Handles.color *= s_ShapeGizmoThicknessTint;
                    EditorGUI.BeginChangeCheck();
                    float radiusThickness = Handles.DoSimpleRadiusHandle(Quaternion.identity, Vector3.zero, shapeModule.radius * (1.0f - shapeModule.radiusThickness), false, shapeModule.arc, allowGizmoEditing);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, s_Texts.undoSphereThickness);
                        shapeModule.radiusThickness = 1.0f - (radiusThickness / shapeModule.radius);
                    }

                    // Sphere
                    Handles.color = s_GizmoColor;
                    EditorGUI.BeginChangeCheck();
                    float radius = Handles.DoSimpleRadiusHandle(Quaternion.identity, Vector3.zero, shapeModule.radius, false, shapeModule.arc, allowGizmoEditing);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, s_Texts.undoSphere);
                        shapeModule.radius = radius;
                    }

                    // Texture
                    Matrix4x4 textureTransform = combinedMatrix * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * shapeModule.radius * 2.0f);
                    OnSceneViewTextureGUI(shapeModule, s_SphereMesh, false, s_SphereTextureMaterial, textureTransform);
                }
                else if (type == ParticleSystemShapeType.Circle)
                {
                    // Thickness
                    EditorGUI.BeginChangeCheck();

                    m_ArcHandle.angle = shapeModule.arc;
                    m_ArcHandle.radius = shapeModule.radius * (1.0f - shapeModule.radiusThickness);
                    m_ArcHandle.SetColorWithRadiusHandle(s_ShapeGizmoThicknessTint, 0f);
                    m_ArcHandle.radiusHandleColor *= gizmoEditingColor;
                    m_ArcHandle.angleHandleColor = Color.clear;

                    using (new Handles.DrawingScope(Handles.matrix * s_ArcHandleOffsetMatrix))
                        m_ArcHandle.DrawHandle();

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, s_Texts.undoCircleThickness);
                        shapeModule.radiusThickness = 1.0f - (m_ArcHandle.radius / shapeModule.radius);
                    }

                    // Circle
                    EditorGUI.BeginChangeCheck();

                    m_ArcHandle.radius = shapeModule.radius;
                    m_ArcHandle.SetColorWithRadiusHandle(Color.white, 0f);
                    m_ArcHandle.radiusHandleColor *= gizmoEditingColor;
                    m_ArcHandle.angleHandleColor *= gizmoEditingColor;

                    using (new Handles.DrawingScope(Handles.matrix * s_ArcHandleOffsetMatrix))
                        m_ArcHandle.DrawHandle();

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, s_Texts.undoCircle);
                        shapeModule.radius = m_ArcHandle.radius;
                        shapeModule.arc = m_ArcHandle.angle;
                    }

                    // Texture
                    Matrix4x4 textureTransform = combinedMatrix * Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(90.0f, 0.0f, 180.0f), Vector3.one * shapeModule.radius * 2.0f);
                    OnSceneViewTextureGUI(shapeModule, s_CircleMesh, true, s_TextureMaterial, textureTransform);
                }
                else if (type == ParticleSystemShapeType.Hemisphere)
                {
                    // Thickness
                    Handles.color *= s_ShapeGizmoThicknessTint;
                    EditorGUI.BeginChangeCheck();
                    float radiusThickness = Handles.DoSimpleRadiusHandle(Quaternion.identity, Vector3.zero, shapeModule.radius * (1.0f - shapeModule.radiusThickness), true, shapeModule.arc, allowGizmoEditing);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, s_Texts.undoHemisphereThickness);
                        shapeModule.radiusThickness = 1.0f - (radiusThickness / shapeModule.radius);
                    }

                    // Hemisphere
                    Handles.color = s_GizmoColor;
                    EditorGUI.BeginChangeCheck();
                    float radius = Handles.DoSimpleRadiusHandle(Quaternion.identity, Vector3.zero, shapeModule.radius, true, shapeModule.arc, allowGizmoEditing);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, s_Texts.undoHemisphere);
                        shapeModule.radius = radius;
                    }

                    // Texture
                    Matrix4x4 textureTransform = combinedMatrix * Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * shapeModule.radius * 2.0f);
                    OnSceneViewTextureGUI(shapeModule, s_HemisphereMesh, false, s_SphereTextureMaterial, textureTransform);
                }
                else if (type == ParticleSystemShapeType.Cone)
                {
                    Handles.ConeHandles coneHandlesMask = allowGizmoEditing ? Handles.ConeHandles.All : 0;

                    // Thickness
                    Handles.color *= s_ShapeGizmoThicknessTint;
                    EditorGUI.BeginChangeCheck();
                    float angleThickness = Mathf.Lerp(shapeModule.angle, 0.0f, shapeModule.radiusThickness);
                    Vector3 radiusThicknessAngleRange = new Vector3(shapeModule.radius * (1.0f - shapeModule.radiusThickness), angleThickness, mainModule.startSpeedMultiplier);
                    radiusThicknessAngleRange = Handles.ConeFrustrumHandle(Quaternion.identity, Vector3.zero, radiusThicknessAngleRange, Handles.ConeHandles.Radius & coneHandlesMask);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, s_Texts.undoConeThickness);
                        shapeModule.radiusThickness = 1.0f - (radiusThicknessAngleRange.x / shapeModule.radius);
                    }

                    // Cone
                    Handles.color = s_GizmoColor;
                    EditorGUI.BeginChangeCheck();
                    Vector3 radiusAngleRange = new Vector3(shapeModule.radius, shapeModule.angle, mainModule.startSpeedMultiplier);
                    radiusAngleRange = Handles.ConeFrustrumHandle(Quaternion.identity, Vector3.zero, radiusAngleRange, coneHandlesMask);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, s_Texts.undoCone);
                        shapeModule.radius = radiusAngleRange.x;
                        shapeModule.angle = radiusAngleRange.y;
                        mainModule.startSpeedMultiplier = radiusAngleRange.z;
                    }

                    // Texture
                    Matrix4x4 textureTransform = combinedMatrix * Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(90.0f, 0.0f, 180.0f), Vector3.one * shapeModule.radius * 2.0f);
                    OnSceneViewTextureGUI(shapeModule, s_CircleMesh, true, s_TextureMaterial, textureTransform);
                }
                else if (type == ParticleSystemShapeType.ConeVolume)
                {
                    Handles.ConeHandles coneHandlesMask = allowGizmoEditing ? Handles.ConeHandles.All : 0;

                    // Thickness
                    Handles.color *= s_ShapeGizmoThicknessTint;
                    EditorGUI.BeginChangeCheck();
                    float angleThickness = Mathf.Lerp(shapeModule.angle, 0.0f, shapeModule.radiusThickness);
                    Vector3 radiusThicknessAngleLength = new Vector3(shapeModule.radius * (1.0f - shapeModule.radiusThickness), angleThickness, shapeModule.length);
                    radiusThicknessAngleLength = Handles.ConeFrustrumHandle(Quaternion.identity, Vector3.zero, radiusThicknessAngleLength, Handles.ConeHandles.Radius & coneHandlesMask);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, s_Texts.undoConeVolumeThickness);
                        shapeModule.radiusThickness = 1.0f - (radiusThicknessAngleLength.x / shapeModule.radius);
                    }

                    // Cone
                    Handles.color = s_GizmoColor;
                    EditorGUI.BeginChangeCheck();
                    Vector3 radiusAngleLength = new Vector3(shapeModule.radius, shapeModule.angle, shapeModule.length);
                    radiusAngleLength = Handles.ConeFrustrumHandle(Quaternion.identity, Vector3.zero, radiusAngleLength, coneHandlesMask);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, s_Texts.undoConeVolume);
                        shapeModule.radius = radiusAngleLength.x;
                        shapeModule.angle = radiusAngleLength.y;
                        shapeModule.length = radiusAngleLength.z;
                    }

                    // Texture
                    Matrix4x4 textureTransform = combinedMatrix * Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(90.0f, 0.0f, 180.0f), Vector3.one * shapeModule.radius * 2.0f);
                    OnSceneViewTextureGUI(shapeModule, s_CircleMesh, true, s_TextureMaterial, textureTransform);
                }
                else if (type == ParticleSystemShapeType.Box || type == ParticleSystemShapeType.BoxShell || type == ParticleSystemShapeType.BoxEdge)
                {
                    EditorGUI.BeginChangeCheck();

                    m_BoxBoundsHandle.center = Vector3.zero;
                    m_BoxBoundsHandle.size = shapeModule.scale;
                    m_BoxBoundsHandle.handleColor = gizmoEditingColor;
                    m_BoxBoundsHandle.DrawHandle();

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, s_Texts.undoBox);
                        shapeModule.scale = m_BoxBoundsHandle.size;
                    }

                    Matrix4x4 textureTransform = combinedMatrix * Matrix4x4.TRS(new Vector3(0.0f, 0.0f, -m_BoxBoundsHandle.size.z * 0.5f), Quaternion.identity, m_BoxBoundsHandle.size);
                    OnSceneViewTextureGUI(shapeModule, s_QuadMesh, true, s_TextureMaterial, textureTransform);
                }
                else if (type == ParticleSystemShapeType.Donut)
                {
                    // Radius
                    EditorGUI.BeginChangeCheck();

                    m_ArcHandle.radius = shapeModule.radius;
                    m_ArcHandle.angle = shapeModule.arc;
                    m_ArcHandle.SetColorWithRadiusHandle(Color.white * gizmoEditingColor, 0f);
                    m_ArcHandle.wireframeColor = Color.clear;

                    using (new Handles.DrawingScope(Handles.matrix * s_ArcHandleOffsetMatrix))
                        m_ArcHandle.DrawHandle();

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, s_Texts.undoDonut);
                        shapeModule.radius = m_ArcHandle.radius;
                        shapeModule.arc = m_ArcHandle.angle;
                    }

                    // Donut extents
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

                    // Donut thickness
                    m_SphereBoundsHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;
                    m_SphereBoundsHandle.radius = shapeModule.donutRadius * (1.0f - shapeModule.radiusThickness);
                    m_SphereBoundsHandle.center = Vector3.zero;
                    m_SphereBoundsHandle.SetColor(s_ShapeGizmoThicknessTint);
                    m_SphereBoundsHandle.handleColor *= gizmoEditingColor;

                    const float handleInterval = 90.0f;
                    int numOuterRadii = Mathf.Max(1, (int)Mathf.Ceil(shapeModule.arc / handleInterval));
                    Matrix4x4 donutRadiusStartMatrix = Matrix4x4.TRS(new Vector3(shapeModule.radius, 0.0f, 0.0f), Quaternion.Euler(90.0f, 0.0f, 0.0f), Vector3.one);

                    for (int i = 0; i < numOuterRadii; i++)
                    {
                        EditorGUI.BeginChangeCheck();
                        using (new Handles.DrawingScope(Handles.matrix * (Matrix4x4.Rotate(Quaternion.Euler(0.0f, 0.0f, handleInterval * i)) * donutRadiusStartMatrix)))
                            m_SphereBoundsHandle.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(ps, s_Texts.undoDonutRadiusThickness);
                            shapeModule.radiusThickness = 1.0f - (m_SphereBoundsHandle.radius / shapeModule.donutRadius);
                        }
                    }

                    // Donut radius
                    m_SphereBoundsHandle.radius = shapeModule.donutRadius;
                    m_SphereBoundsHandle.SetColor(Color.white);
                    m_SphereBoundsHandle.handleColor *= gizmoEditingColor;

                    for (int i = 0; i < numOuterRadii; i++)
                    {
                        EditorGUI.BeginChangeCheck();
                        using (new Handles.DrawingScope(Handles.matrix * (Matrix4x4.Rotate(Quaternion.Euler(0.0f, 0.0f, handleInterval * i)) * donutRadiusStartMatrix)))
                            m_SphereBoundsHandle.DrawHandle();
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(ps, s_Texts.undoDonutRadius);
                            shapeModule.donutRadius = m_SphereBoundsHandle.radius;
                        }
                    }

                    // Texture
                    Matrix4x4 textureTransform = combinedMatrix * Matrix4x4.TRS(new Vector3(shapeModule.radius, 0.0f, 0.0f), Quaternion.Euler(180.0f, 0.0f, 180.0f), Vector3.one * shapeModule.donutRadius * 2.0f);
                    OnSceneViewTextureGUI(shapeModule, s_CircleMesh, true, s_TextureMaterial, textureTransform);
                }
                else if (type == ParticleSystemShapeType.SingleSidedEdge)
                {
                    EditorGUI.BeginChangeCheck();
                    float radius = Handles.DoSimpleEdgeHandle(Quaternion.identity, Vector3.zero, shapeModule.radius, allowGizmoEditing);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, s_Texts.undoEdge);
                        shapeModule.radius = radius;
                    }
                }
                else if (type == ParticleSystemShapeType.Mesh)
                {
                    Mesh mesh = shapeModule.mesh;
                    if (mesh)
                    {
                        bool orgWireframeMode = GL.wireframe;
                        GL.wireframe = true;
                        s_Material.SetPass(0);
                        Graphics.DrawMeshNow(mesh, combinedMatrix);
                        GL.wireframe = orgWireframeMode;

                        OnSceneViewTextureGUI(shapeModule, mesh, false, s_TextureMaterial, combinedMatrix);
                    }
                }
                else if (type == ParticleSystemShapeType.Rectangle)
                {
                    EditorGUI.BeginChangeCheck();

                    m_BoxBoundsHandle.center = Vector3.zero;
                    m_BoxBoundsHandle.size = new Vector3(shapeModule.scale.x, shapeModule.scale.y, 0.0f);
                    m_BoxBoundsHandle.handleColor = gizmoEditingColor;
                    m_BoxBoundsHandle.DrawHandle();

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, s_Texts.undoRectangle);
                        shapeModule.scale = new Vector3(m_BoxBoundsHandle.size.x, m_BoxBoundsHandle.size.y, 0.0f);
                    }

                    OnSceneViewTextureGUI(shapeModule, s_QuadMesh, true, s_TextureMaterial, combinedMatrix * Matrix4x4.Scale(m_BoxBoundsHandle.size));
                }
                else if (type == ParticleSystemShapeType.Sprite)
                {
                    Sprite sprite = shapeModule.sprite;
                    if (sprite)
                    {
                        if (!m_SpriteMesh)
                        {
                            m_SpriteMesh = new Mesh();
                            m_SpriteMesh.name = "ParticleSpritePreview";
                            m_SpriteMesh.hideFlags |= HideFlags.HideAndDontSave;
                        }

                        m_SpriteMesh.vertices = Array.ConvertAll(sprite.vertices, i => (Vector3)i);
                        m_SpriteMesh.uv = sprite.uv;
                        m_SpriteMesh.triangles = Array.ConvertAll(sprite.triangles, i => (int)i);

                        bool orgWireframeMode = GL.wireframe;
                        GL.wireframe = true;
                        s_Material.SetPass(0);
                        Graphics.DrawMeshNow(m_SpriteMesh, combinedMatrix);
                        GL.wireframe = orgWireframeMode;

                        OnSceneViewTextureGUI(shapeModule, m_SpriteMesh, false, s_TextureMaterial, combinedMatrix);
                    }
                }

                if (EditMode.editMode == EditMode.SceneViewEditMode.ParticleSystemShapeModulePosition || EditMode.editMode == EditMode.SceneViewEditMode.ParticleSystemShapeModuleRotation || EditMode.editMode == EditMode.SceneViewEditMode.ParticleSystemShapeModuleScale)
                {
                    Handles.matrix = Matrix4x4.identity;

                    Vector3 position = transformMatrix.MultiplyPoint(shapeModule.position);
                    Quaternion rotation = transformMatrix.rotation * Quaternion.Euler(shapeModule.rotation);
                    Vector3 scale = transformMatrix.MultiplyVector(shapeModule.scale);

                    float handleSize = HandleUtility.GetHandleSize(position);

                    EditorGUI.BeginChangeCheck();

                    if (EditMode.editMode == EditMode.SceneViewEditMode.ParticleSystemShapeModulePosition)
                        position = Handles.PositionHandle(position, rotation);
                    else if (EditMode.editMode == EditMode.SceneViewEditMode.ParticleSystemShapeModuleRotation)
                        rotation = Handles.RotationHandle(rotation, position);
                    else if (EditMode.editMode == EditMode.SceneViewEditMode.ParticleSystemShapeModuleScale)
                        scale = Handles.ScaleHandle(scale, position, rotation, handleSize);

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(ps, s_Texts.undoTransform);

                        Matrix4x4 inverseTransformMatrix = transformMatrix.inverse;

                        shapeModule.position = inverseTransformMatrix.MultiplyPoint(position);
                        shapeModule.rotation = (inverseTransformMatrix.rotation * rotation).eulerAngles;
                        shapeModule.scale = inverseTransformMatrix.MultiplyVector(scale);
                    }
                }
            }

            if (EditorGUI.EndChangeCheck())
                m_ParticleSystemUI.m_ParticleEffectUI.m_Owner.Repaint();

            Handles.color = origCol;
            Handles.matrix = orgMatrix;
        }

        private void OnSceneViewTextureGUI(ParticleSystem.ShapeModule shapeModule, Mesh mesh, bool twoSided, Material mat, Matrix4x4 transform)
        {
            Texture texture = shapeModule.texture;
            if (texture)
            {
                mat.SetPass(0);
                mat.SetTexture("_MainTex", texture);
                mat.SetColor("_Color", new Color(1.0f, 1.0f, 1.0f, 0.4f));
                mat.SetFloat("_ClipChannel", (float)(int)shapeModule.textureClipChannel);
                mat.SetFloat("_ClipThreshold", shapeModule.textureClipThreshold);
                mat.SetFloat("_Cull", twoSided ? (float)UnityEngine.Rendering.CullMode.Off : (float)UnityEngine.Rendering.CullMode.Back);
                mat.SetFloat("_UVChannel", (float)shapeModule.textureUVChannel);
                Graphics.DrawMeshNow(mesh, transform);
            }
        }

        override public void UpdateCullingSupportedString(ref string text)
        {
            bool animatedShape = false;

            foreach (ParticleSystem ps in m_ParticleSystemUI.m_ParticleSystems)
            {
                switch (ps.shape.shapeType)
                {
                    case ParticleSystemShapeType.Cone:
                    case ParticleSystemShapeType.ConeVolume:
                    case ParticleSystemShapeType.Donut:
                    case ParticleSystemShapeType.Circle:
                        animatedShape = (ps.shape.arcMode != ParticleSystemShapeMultiModeValue.Random);
                        break;
                    case ParticleSystemShapeType.SingleSidedEdge:
                        animatedShape = (ps.shape.radiusMode != ParticleSystemShapeMultiModeValue.Random);
                        break;
                    default:
                        break;
                }

                if (animatedShape)
                    break;
            }

            if (animatedShape)
                text += "\n\tAnimated shape emission is enabled.";
        }
    }
} // namespace UnityEditor
