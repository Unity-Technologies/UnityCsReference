// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;
using UnityEngine.U2D;
using UnityEditor.U2D;

namespace UnityEditor
{
    class UVModuleUI : ModuleUI
    {
        SerializedProperty m_Mode;
        SerializedProperty m_TimeMode;
        SerializedProperty m_FPS;
        SerializedMinMaxCurve m_FrameOverTime;
        SerializedMinMaxCurve m_StartFrame;
        SerializedProperty m_SpeedRange;
        SerializedProperty m_TilesX;
        SerializedProperty m_TilesY;
        SerializedProperty m_AnimationType;
        SerializedProperty m_RowMode;
        SerializedProperty m_RowIndex;
        SerializedProperty m_Sprites;
        SerializedProperty m_Cycles;
        SerializedProperty m_UVChannelMask;

        class Texts
        {
            public GUIContent mode = EditorGUIUtility.TrTextContent("Mode", "Animation frames can either be specified on a regular grid texture, or as a list of Sprites.");
            public GUIContent timeMode = EditorGUIUtility.TrTextContent("Time Mode", "Play frames either based on the lifetime of the particle, the speed of the particle, or at a constant FPS, regardless of particle lifetime.");
            public GUIContent fps = EditorGUIUtility.TrTextContent("FPS", "Specify the Frames Per Second of the animation.");
            public GUIContent frameOverTime = EditorGUIUtility.TrTextContent("Frame over Time", "Controls the uv animation frame of each particle over its lifetime. On the horizontal axis you will find the lifetime. On the vertical axis you will find the sheet index.");
            public GUIContent startFrame = EditorGUIUtility.TrTextContent("Start Frame", "Phase the animation, so it starts on a frame other than 0.");
            public GUIContent speedRange = EditorGUIUtility.TrTextContent("Speed Range", "Remaps speed in the defined range to a 0-1 value through the animation.");
            public GUIContent tiles = EditorGUIUtility.TrTextContent("Tiles", "Defines the tiling of the texture.");
            public GUIContent tilesX = EditorGUIUtility.TextContent("X");
            public GUIContent tilesY = EditorGUIUtility.TextContent("Y");
            public GUIContent animation = EditorGUIUtility.TrTextContent("Animation", "Specifies the animation type: Whole Sheet or Single Row. Whole Sheet will animate over the whole texture sheet from left to right, top to bottom. Single Row will animate a single row in the sheet from left to right.");
            public GUIContent rowMode = EditorGUIUtility.TrTextContent("Row Mode", "Determine how the row is selected for each particle.");
            public GUIContent row = EditorGUIUtility.TrTextContent("Row", "The row in the sheet which will be played.");
            public GUIContent sprites = EditorGUIUtility.TrTextContent("Sprites", "The list of Sprites to be played.");
            public GUIContent frame = EditorGUIUtility.TrTextContent("Frame", "The frame in the sheet which will be used.");
            public GUIContent cycles = EditorGUIUtility.TrTextContent("Cycles", "Specifies how many times the animation will loop during the lifetime of the particle.");
            public GUIContent uvChannelMask = EditorGUIUtility.TrTextContent("Affected UV Channels", "Specifies which UV channels will be animated.");

            public GUIContent[] modes = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("Grid"),
                EditorGUIUtility.TrTextContent("Sprites")
            };

            public GUIContent[] timeModes = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("Lifetime"),
                EditorGUIUtility.TrTextContent("Speed"),
                EditorGUIUtility.TrTextContent("FPS")
            };

            public GUIContent[] types = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("Whole Sheet"),
                EditorGUIUtility.TrTextContent("Single Row")
            };

            public GUIContent[] rowModes = new GUIContent[]
            {
                EditorGUIUtility.TrTextContent("Custom"),
                EditorGUIUtility.TrTextContent("Random"),
                EditorGUIUtility.TrTextContent("Mesh Index")
            };
        }
        private static Texts s_Texts;


        public UVModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "UVModule", displayName)
        {
            m_ToolTip = "Particle UV animation. This allows you to specify a texture sheet (a texture with multiple tiles/sub frames) and animate or randomize over it per particle.";
        }

        protected override void Init()
        {
            // Already initialized?
            if (m_TilesX != null)
                return;
            if (s_Texts == null)
                s_Texts = new Texts();

            m_Mode = GetProperty("mode");
            m_TimeMode = GetProperty("timeMode");
            m_FPS = GetProperty("fps");
            m_FrameOverTime = new SerializedMinMaxCurve(this, s_Texts.frameOverTime, "frameOverTime");
            m_StartFrame = new SerializedMinMaxCurve(this, s_Texts.startFrame, "startFrame");
            m_StartFrame.m_AllowCurves = false;
            m_SpeedRange = GetProperty("speedRange");
            m_TilesX = GetProperty("tilesX");
            m_TilesY = GetProperty("tilesY");
            m_AnimationType = GetProperty("animationType");
            m_RowMode = GetProperty("rowMode");
            m_RowIndex = GetProperty("rowIndex");
            m_Sprites = GetProperty("sprites");
            m_Cycles = GetProperty("cycles");
            m_UVChannelMask = GetProperty("uvChannelMask");
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            int mode = GUIPopup(s_Texts.mode, m_Mode, s_Texts.modes);
            if (!m_Mode.hasMultipleDifferentValues)
            {
                if (mode == (int)ParticleSystemAnimationMode.Grid)
                {
                    GUIIntDraggableX2(s_Texts.tiles, s_Texts.tilesX, m_TilesX, s_Texts.tilesY, m_TilesY);

                    int type = GUIPopup(s_Texts.animation, m_AnimationType, s_Texts.types);
                    if (type == (int)ParticleSystemAnimationType.SingleRow)
                    {
                        GUIPopup(s_Texts.rowMode, m_RowMode, s_Texts.rowModes);
                        if (!m_RowMode.hasMultipleDifferentValues && m_RowMode.intValue == (int)ParticleSystemAnimationRowMode.Custom)
                            GUIInt(s_Texts.row, m_RowIndex);

                        m_FrameOverTime.m_RemapValue = (float)(m_TilesX.intValue);
                        m_StartFrame.m_RemapValue = (float)(m_TilesX.intValue);
                    }
                    else
                    {
                        m_FrameOverTime.m_RemapValue = (float)(m_TilesX.intValue * m_TilesY.intValue);
                        m_StartFrame.m_RemapValue = (float)(m_TilesX.intValue * m_TilesY.intValue);
                    }
                }
                else
                {
                    DoListOfSpritesGUI();
                    ValidateSpriteList();

                    m_FrameOverTime.m_RemapValue = (float)(m_Sprites.arraySize);
                    m_StartFrame.m_RemapValue = (float)(m_Sprites.arraySize);
                }
            }

            ParticleSystemAnimationTimeMode timeMode = (ParticleSystemAnimationTimeMode)GUIPopup(s_Texts.timeMode, m_TimeMode, s_Texts.timeModes);
            if (!m_TimeMode.hasMultipleDifferentValues)
            {
                if (timeMode == ParticleSystemAnimationTimeMode.FPS)
                {
                    GUIFloat(s_Texts.fps, m_FPS);
                    foreach (ParticleSystem ps in m_ParticleSystemUI.m_ParticleSystems)
                    {
                        if (ps.main.startLifetimeMultiplier == Mathf.Infinity)
                        {
                            EditorGUILayout.HelpBox("FPS mode does not work when using infinite particle lifetimes.", MessageType.Error, true);
                            break;
                        }
                    }
                }
                else if (timeMode == ParticleSystemAnimationTimeMode.Speed)
                {
                    GUIMinMaxRange(s_Texts.speedRange, m_SpeedRange);
                }
                else
                {
                    GUIMinMaxCurve(s_Texts.frameOverTime, m_FrameOverTime);
                }
            }
            GUIMinMaxCurve(s_Texts.startFrame, m_StartFrame);

            if (!m_TimeMode.hasMultipleDifferentValues && timeMode != ParticleSystemAnimationTimeMode.FPS)
                GUIFloat(s_Texts.cycles, m_Cycles);
            GUIEnumMaskUVChannelFlags(s_Texts.uvChannelMask, m_UVChannelMask);
        }

        private void DoListOfSpritesGUI()
        {
            if (m_Sprites.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("Sprite editing is only available when all selected Particle Systems contain the same number of Sprites.", MessageType.Info, true);
                return;
            }

            // Support multi edit of large arrays (case 1222515)
            if (serializedObject.isEditingMultipleObjects && serializedObject.maxArraySizeForMultiEditing < m_Sprites.minArraySize)
            {
                // Increase maxArraySizeForMultiEditing so that arraySize and GetArrayElementAtIndex will work
                serializedObject.maxArraySizeForMultiEditing = m_Sprites.minArraySize;
            }

            for (int i = 0; i < m_Sprites.arraySize; i++)
            {
                GUILayout.BeginHorizontal();

                SerializedProperty spriteData = m_Sprites.GetArrayElementAtIndex(i);
                SerializedProperty sprite = spriteData.FindPropertyRelative("sprite");
                GUIObject(new GUIContent(" "), sprite, typeof(Sprite));

                // add plus button to first element
                if (i == 0)
                {
                    if (GUILayout.Button(s_AddItem, "OL Plus", GUILayout.Width(16)))
                    {
                        m_Sprites.InsertArrayElementAtIndex(m_Sprites.arraySize);
                        SerializedProperty newSpriteData = m_Sprites.GetArrayElementAtIndex(m_Sprites.arraySize - 1);
                        SerializedProperty newSprite = newSpriteData.FindPropertyRelative("sprite");
                        newSprite.objectReferenceValue = null;
                    }
                }
                // add minus button to all other elements
                else
                {
                    if (GUILayout.Button(s_RemoveItem, "OL Minus", GUILayout.Width(16)))
                        m_Sprites.DeleteArrayElementAtIndex(i);
                }

                GUILayout.EndHorizontal();
            }
        }

        // Ensure sprite mesh is a rect (we don't support non-rectangular sprites)
        internal static bool ValidateSpriteUVs(Vector2[] uvs)
        {
            if (uvs.Length != 4)
                return false;

            Vector2Int secondUnique = new Vector2Int(-1, -1);
            for (int j = 1; j < uvs.Length; j++)
            {
                for (int k = 0; k < 2; k++)
                {
                    if (!Mathf.Approximately(uvs[j][k], uvs[0][k]))
                    {
                        if (secondUnique[k] == -1)
                            secondUnique[k] = j;
                        else if (!Mathf.Approximately(uvs[j][k], uvs[secondUnique[k]][k]))
                            return false;
                    }
                }
            }

            return true;
        }

        private void ValidateSpriteList()
        {
            if (m_Sprites.hasMultipleDifferentValues)
                return;
            if (m_Sprites.arraySize <= 1)
                return;

            Texture texture = null;
            for (int i = 0; i < m_Sprites.arraySize; i++)
            {
                SerializedProperty spriteData = m_Sprites.GetArrayElementAtIndex(i);
                SerializedProperty prop = spriteData.FindPropertyRelative("sprite");
                Sprite sprite = prop.objectReferenceValue as Sprite;
                if (sprite != null)
                {
                    // Find the sprite atlas asset for this texture, if it has one
                    var spriteAtlas = SpriteEditorExtension.GetActiveAtlas(sprite);
                    if (spriteAtlas != null)
                    {
                        if (spriteAtlas.GetPackingSettings().enableTightPacking)
                        {
                            EditorGUILayout.HelpBox("Tightly packed Sprite Atlases are not supported.", MessageType.Error, true);
                            break;
                        }
                    }

                    // Ensure sprite mesh is a rect (we don't support non-rectangular sprites)
                    bool uvsOk = ValidateSpriteUVs(sprite.uv);
                    if (!uvsOk)
                    {
                        EditorGUILayout.HelpBox("Sprites must use rectangles. Change the Atlas Mesh Type to Full Rect instead of Tight, if applicable.", MessageType.Error, true);
                        break;
                    }

                    if (texture == null)
                    {
                        texture = sprite.GetTextureForPlayMode();
                    }
                    else if (texture != sprite.GetTextureForPlayMode())
                    {
                        if (EditorSettings.spritePackerMode == SpritePackerMode.Disabled)
                            EditorGUILayout.HelpBox("To use multiple sprites, enable the Sprite Packer Mode in the Editor Project Settings, and create a Texture Atlas.", MessageType.Error, true);
                        else if (EditorSettings.spritePackerMode == SpritePackerMode.SpriteAtlasV2 || EditorSettings.spritePackerMode == SpritePackerMode.AlwaysOnAtlas || EditorSettings.spritePackerMode == SpritePackerMode.BuildTimeOnlyAtlas)
                            EditorGUILayout.HelpBox("All Sprites must share the same Texture Atlas. Also check that all your sprites fit onto 1 texture of the Sprite Atlas.", MessageType.Error, true);
                        else
                            EditorGUILayout.HelpBox("All Sprites must share the same texture. Either pack all Sprites into one Texture by setting the Packing Tag, or use a Multiple Mode Sprite.", MessageType.Error, true);
                        break;
                    }
                    else if (sprite.border != Vector4.zero)
                    {
                        EditorGUILayout.HelpBox("Sprite borders are not supported. They will be ignored.", MessageType.Warning, true);
                        break;
                    }
                }
            }
        }
    }
} // namespace UnityEditor
