// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;


namespace UnityEditor
{
    class ParticleSystemStyles
    {
        GUIStyle m_Label;
        GUIStyle m_LabelBold;
        GUIStyle m_EditableLabel;
        GUIStyle m_EditableLabelBold;
        GUIStyle m_ObjectField;
        GUIStyle m_ObjectFieldBold;
        GUIStyle m_NumberField;
        GUIStyle m_NumberFieldBold;
        GUIStyle m_ModuleHeaderStyle;
        GUIStyle m_ModuleHeaderStyleBold;
        GUIStyle m_PopupStyle;
        GUIStyle m_PopupStyleBold;
        GUIStyle m_EmitterHeaderStyle;
        GUIStyle m_EffectBgStyle;
        GUIStyle m_ModuleBgStyle;
        GUIStyle m_Plus;
        GUIStyle m_Minus;
        GUIStyle m_Checkmark;
        GUIStyle m_CheckmarkMixed;
        GUIStyle m_MinMaxCurveStateDropDown;
        GUIStyle m_Toggle;
        GUIStyle m_ToggleMixed;
        GUIStyle m_SelectionMarker;
        GUIStyle m_ToolbarButtonLeftAlignText;
        GUIStyle m_ModulePadding;
        Texture2D m_WarningIcon;

        private static ParticleSystemStyles s_ParticleSystemStyles;
        public static ParticleSystemStyles Get()
        {
            if (s_ParticleSystemStyles == null)
                s_ParticleSystemStyles = new ParticleSystemStyles();
            return s_ParticleSystemStyles;
        }

        public GUIStyle label { get { return EditorGUIUtility.GetBoldDefaultFont() ? m_LabelBold : m_Label; } }
        public GUIStyle editableLabel { get { return EditorGUIUtility.GetBoldDefaultFont() ? m_EditableLabelBold : m_EditableLabel; } }
        public GUIStyle objectField { get { return EditorGUIUtility.GetBoldDefaultFont() ? m_ObjectFieldBold : m_ObjectField; } }
        public GUIStyle numberField { get { return EditorGUIUtility.GetBoldDefaultFont() ? m_NumberFieldBold : m_NumberField; } }
        public GUIStyle moduleHeaderStyle { get { return EditorGUIUtility.GetBoldDefaultFont() ? m_ModuleHeaderStyleBold : m_ModuleHeaderStyle; } }
        public GUIStyle popup { get { return EditorGUIUtility.GetBoldDefaultFont() ? m_PopupStyleBold : m_PopupStyle; } }
        public GUIStyle emitterHeaderStyle { get { return m_EmitterHeaderStyle; } }
        public GUIStyle effectBgStyle { get { return m_EffectBgStyle; } }
        public GUIStyle moduleBgStyle { get { return m_ModuleBgStyle; } }
        public GUIStyle plus { get { return m_Plus; } }
        public GUIStyle minus { get { return m_Minus; } }
        public GUIStyle checkmark { get { return m_Checkmark; } }
        public GUIStyle checkmarkMixed { get { return m_CheckmarkMixed; } }
        public GUIStyle minMaxCurveStateDropDown { get { return m_MinMaxCurveStateDropDown; } }
        public GUIStyle toggle { get { return m_Toggle; } }
        public GUIStyle toggleMixed { get { return m_ToggleMixed; } }
        public GUIStyle selectionMarker { get { return m_SelectionMarker; } }
        public GUIStyle toolbarButtonLeftAlignText { get { return m_ToolbarButtonLeftAlignText; } }
        public GUIStyle modulePadding { get { return m_ModulePadding; } }
        public Texture2D warningIcon { get { return m_WarningIcon; } }

        ParticleSystemStyles()
        {
            InitStyle(out m_Label, out m_LabelBold, "ShurikenLabel");
            InitStyle(out m_EditableLabel, out m_EditableLabelBold, "ShurikenEditableLabel");
            InitStyle(out m_ObjectField, out m_ObjectFieldBold, "ShurikenObjectField");
            InitStyle(out m_NumberField, out m_NumberFieldBold, "ShurikenValue");
            InitStyle(out m_ModuleHeaderStyle, out m_ModuleHeaderStyleBold, "ShurikenModuleTitle");
            InitStyle(out m_PopupStyle, out m_PopupStyleBold, "ShurikenPopUp");
            InitStyle(out m_EmitterHeaderStyle, "ShurikenEmitterTitle");
            InitStyle(out m_EmitterHeaderStyle, "ShurikenEmitterTitle");
            InitStyle(out m_EffectBgStyle, "ShurikenEffectBg");
            InitStyle(out m_ModuleBgStyle, "ShurikenModuleBg");
            InitStyle(out m_Plus, "ShurikenPlus");
            InitStyle(out m_Minus, "ShurikenMinus");
            InitStyle(out m_Checkmark, "ShurikenCheckMark");
            InitStyle(out m_CheckmarkMixed, "ShurikenCheckMarkMixed");
            InitStyle(out m_MinMaxCurveStateDropDown, "ShurikenDropdown");
            InitStyle(out m_Toggle, "ShurikenToggle");
            InitStyle(out m_ToggleMixed, "ShurikenToggleMixed");
            InitStyle(out m_SelectionMarker, "IN ThumbnailShadow");
            InitStyle(out m_ToolbarButtonLeftAlignText, "ToolbarButton");

            // Todo: Fix in editor resources
            m_EmitterHeaderStyle.clipping = TextClipping.Clip;
            m_EmitterHeaderStyle.padding.right = 45;

            m_WarningIcon = EditorGUIUtility.LoadIcon("console.infoicon.sml");
            // Don't change the original as it is used in areas other than particles.
            m_ToolbarButtonLeftAlignText = new GUIStyle(m_ToolbarButtonLeftAlignText);
            m_ToolbarButtonLeftAlignText.alignment = TextAnchor.MiddleLeft;

            m_ModulePadding = new GUIStyle();
            m_ModulePadding.padding = new RectOffset(3, 3, 4, 2);
        }

        static void InitStyle(out GUIStyle normal, string name)
        {
            normal = FindStyle(name);
        }

        static void InitStyle(out GUIStyle normal, out GUIStyle bold, string name)
        {
            InitStyle(out normal, name);
            bold = new GUIStyle(normal);
            bold.font = EditorStyles.miniBoldFont;
        }

        static GUIStyle FindStyle(string styleName)
        {
            // Outcomment for testing in EditorResources project
            //GUISkin skin = EditorGUIUtility.LoadRequired("Builtin Skins/DarkSkin/Skins/ShurikenSkin.guiSkin") as GUISkin;
            //return skin.GetStyle(styleName);

            return styleName;
        }
    }
} // namespace UnityEditor
