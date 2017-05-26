// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;


namespace UnityEditor
{
    class ParticleSystemStyles
    {
        private static ParticleSystemStyles s_ParticleSystemStyles;
        public static ParticleSystemStyles Get()
        {
            if (s_ParticleSystemStyles == null)
                s_ParticleSystemStyles = new ParticleSystemStyles();
            return s_ParticleSystemStyles;
        }

        public GUIStyle label = FindStyle("ShurikenLabel"); //new GUIStyle (EditorStyles.miniLabel);
        public GUIStyle editableLabel = FindStyle("ShurikenEditableLabel"); //new GUIStyle (EditorStyles.miniLabel);
        public GUIStyle numberField = FindStyle("ShurikenValue");//new GUIStyle(EditorStyles.miniLabel);//new GUIStyle(EditorStyles.miniTextField);//label; //new GUIStyle (EditorStyles.miniTextField);//
        public GUIStyle objectField = FindStyle("ShurikenObjectField"); //new GUIStyle (EditorStyles.objectField);
        public GUIStyle effectBgStyle = FindStyle("ShurikenEffectBg");//new GUIStyle (GUI.skin.button);
        public GUIStyle emitterHeaderStyle = FindStyle("ShurikenEmitterTitle");//new GUIStyle (GUI.skin.button);
        public GUIStyle moduleHeaderStyle = FindStyle("ShurikenModuleTitle"); //new GUIStyle (GUI.skin.button);
        public GUIStyle moduleBgStyle = FindStyle("ShurikenModuleBg"); //new GUIStyle (GUI.skin.button);
        public GUIStyle plus = FindStyle("ShurikenPlus"); //new GUIStyle (GUI.skin.button);
        public GUIStyle minus = FindStyle("ShurikenMinus"); //new GUIStyle (GUI.skin.button);
        public GUIStyle checkmark = FindStyle("ShurikenCheckMark"); //new GUIStyle (GUI.skin.button);
        public GUIStyle checkmarkMixed = FindStyle("ShurikenCheckMarkMixed");
        public GUIStyle minMaxCurveStateDropDown = FindStyle("ShurikenDropdown");
        public GUIStyle toggle = FindStyle("ShurikenToggle");
        public GUIStyle toggleMixed = FindStyle("ShurikenToggleMixed");
        public GUIStyle popup = FindStyle("ShurikenPopUp");
        public GUIStyle selectionMarker = FindStyle("IN ThumbnailShadow");
        public GUIStyle toolbarButtonLeftAlignText = new GUIStyle(FindStyle("ToolbarButton"));
        public GUIStyle modulePadding = new GUIStyle();
        public Texture2D warningIcon;

        ParticleSystemStyles()
        {
            // Todo: Fix in editor resources
            emitterHeaderStyle.clipping = TextClipping.Clip;
            emitterHeaderStyle.padding.right = 45;

            warningIcon = EditorGUIUtility.LoadIcon("console.infoicon.sml");

            toolbarButtonLeftAlignText.alignment = TextAnchor.MiddleLeft;

            modulePadding.padding = new RectOffset(3, 3, 4, 2);
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
