// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AnimatedValues;
using UnityEditor.Modules;
using UnityEditorInternal;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEditor.Build;

namespace UnityEditor
{
    internal partial class PlayerSettingsSplashScreenEditor
    {
        PlayerSettingsEditor m_Owner;

        SerializedProperty m_ResolutionDialogBanner;
        SerializedProperty m_ShowUnitySplashLogo;
        SerializedProperty m_ShowUnitySplashScreen;
        SerializedProperty m_SplashScreenAnimation;
        SerializedProperty m_SplashScreenBackgroundAnimationZoom;
        SerializedProperty m_SplashScreenBackgroundColor;
        SerializedProperty m_SplashScreenBackgroundLandscape;
        SerializedProperty m_SplashScreenBackgroundPortrait;
        SerializedProperty m_SplashScreenDrawMode;
        SerializedProperty m_SplashScreenLogoAnimationZoom;
        SerializedProperty m_SplashScreenLogos;
        SerializedProperty m_SplashScreenLogoStyle;
        SerializedProperty m_SplashScreenOverlayOpacity;
        SerializedProperty m_VirtualRealitySplashScreen;

        ReorderableList m_LogoList;

        float m_TotalLogosDuration;

        static readonly float k_MinLogoTime = 2;
        static readonly float k_MaxLogoTime = 10.0f;
        static readonly float k_DefaultLogoTime = 2.0f;

        static readonly float k_LogoListElementHeight = 72;
        static readonly float k_LogoListLogoFieldHeight = 64;
        static readonly float k_LogoListFooterHeight = 20;
        static readonly float k_LogoListUnityLogoMinWidth = 64;
        static readonly float k_LogoListUnityLogoMaxWidth = 220;
        static readonly float k_LogoListPropertyMinWidth = 230;
        static readonly float k_LogoListPropertyLabelWidth = 100;
        static readonly float k_MinPersonalEditionOverlayOpacity = 0.5f;
        static readonly float k_MinProEditionOverlayOpacity = 0.0f;

        static Sprite s_UnityLogo;

        readonly AnimBool m_ShowAnimationControlsAnimator = new AnimBool();
        readonly AnimBool m_ShowBackgroundColorAnimator = new AnimBool();
        readonly AnimBool m_ShowLogoControlsAnimator = new AnimBool();

        class Texts
        {
            public GUIContent animate = EditorGUIUtility.TextContent("Animation");
            public GUIContent backgroundColor = EditorGUIUtility.TextContent("Background Color|Background color when no background image is used.");
            public GUIContent backgroundImage = EditorGUIUtility.TextContent("Background Image|Image to be used in landscape and portrait(when portrait image is not set).");
            public GUIContent backgroundPortraitImage = EditorGUIUtility.TextContent("Alternate Portrait Image*|Optional image to be used in portrait mode.");
            public GUIContent backgroundTitle = EditorGUIUtility.TextContent("Background*");
            public GUIContent backgroundZoom = EditorGUIUtility.TextContent("Background Zoom");
            public GUIContent configDialogBanner = EditorGUIUtility.TextContent("Application Config Dialog Banner");
            public GUIContent drawMode = EditorGUIUtility.TextContent("Draw Mode");
            public GUIContent logoDuration = EditorGUIUtility.TextContent("Logo Duration|The time the logo will be shown for.");
            public GUIContent logosTitle = EditorGUIUtility.TextContent("Logos*");
            public GUIContent logoZoom = EditorGUIUtility.TextContent("Logo Zoom");
            public GUIContent overlayOpacity = EditorGUIUtility.TextContent("Overlay Opacity|Overlay strength applied to improve logo visibility.");
            public GUIContent previewSplash = EditorGUIUtility.TextContent("Preview|Preview the splash screen in the game view.");
            public GUIContent showLogo = EditorGUIUtility.TextContent("Show Unity Logo");
            public GUIContent showSplash = EditorGUIUtility.TextContent("Show Splash Screen");
            public GUIContent splashStyle = EditorGUIUtility.TextContent("Splash Style");
            public GUIContent splashTitle = EditorGUIUtility.TextContent("Splash Screen");
            public GUIContent title = EditorGUIUtility.TextContent("Splash Image");
            public GUIContent vrSplashScreen = EditorGUIUtility.TextContent("Virtual Reality Splash Image");
        }
        static readonly Texts k_Texts = new Texts();

        public PlayerSettingsSplashScreenEditor(PlayerSettingsEditor owner)
        {
            m_Owner = owner;
        }

        public void OnEnable()
        {
            m_ResolutionDialogBanner = m_Owner.FindPropertyAssert("resolutionDialogBanner");
            m_ShowUnitySplashLogo = m_Owner.FindPropertyAssert("m_ShowUnitySplashLogo");
            m_ShowUnitySplashScreen = m_Owner.FindPropertyAssert("m_ShowUnitySplashScreen");
            m_SplashScreenAnimation = m_Owner.FindPropertyAssert("m_SplashScreenAnimation");
            m_SplashScreenBackgroundAnimationZoom = m_Owner.FindPropertyAssert("m_SplashScreenBackgroundAnimationZoom");
            m_SplashScreenBackgroundColor = m_Owner.FindPropertyAssert("m_SplashScreenBackgroundColor");
            m_SplashScreenBackgroundLandscape = m_Owner.FindPropertyAssert("splashScreenBackgroundSourceLandscape");
            m_SplashScreenBackgroundPortrait = m_Owner.FindPropertyAssert("splashScreenBackgroundSourcePortrait");
            m_SplashScreenDrawMode = m_Owner.FindPropertyAssert("m_SplashScreenDrawMode");
            m_SplashScreenLogoAnimationZoom = m_Owner.FindPropertyAssert("m_SplashScreenLogoAnimationZoom");
            m_SplashScreenLogos = m_Owner.FindPropertyAssert("m_SplashScreenLogos");
            m_SplashScreenLogoStyle = m_Owner.FindPropertyAssert("m_SplashScreenLogoStyle");
            m_SplashScreenOverlayOpacity = m_Owner.FindPropertyAssert("m_SplashScreenOverlayOpacity");
            m_VirtualRealitySplashScreen = m_Owner.FindPropertyAssert("m_VirtualRealitySplashScreen");

            m_LogoList = new ReorderableList(m_Owner.serializedObject, m_SplashScreenLogos, true, true, true, true);
            m_LogoList.elementHeight = k_LogoListElementHeight;
            m_LogoList.footerHeight = k_LogoListFooterHeight;
            m_LogoList.onAddCallback = OnLogoListAddCallback;
            m_LogoList.drawHeaderCallback = DrawLogoListHeaderCallback;
            m_LogoList.onCanRemoveCallback = OnLogoListCanRemoveCallback;
            m_LogoList.drawElementCallback = DrawLogoListElementCallback;
            m_LogoList.drawFooterCallback = DrawLogoListFooterCallback;

            // Set up animations
            m_ShowAnimationControlsAnimator.value = m_SplashScreenAnimation.intValue == (int)PlayerSettings.SplashScreen.AnimationMode.Custom;
            m_ShowAnimationControlsAnimator.valueChanged.AddListener(m_Owner.Repaint);
            m_ShowBackgroundColorAnimator.value = m_SplashScreenBackgroundLandscape.objectReferenceValue == null;
            m_ShowBackgroundColorAnimator.valueChanged.AddListener(m_Owner.Repaint);
            m_ShowLogoControlsAnimator.value = m_ShowUnitySplashLogo.boolValue;
            m_ShowLogoControlsAnimator.valueChanged.AddListener(m_Owner.Repaint);

            if (s_UnityLogo == null)
                s_UnityLogo = Resources.GetBuiltinResource<Sprite>("UnitySplash-cube.png");
        }

        private void DrawLogoListHeaderCallback(Rect rect)
        {
            m_TotalLogosDuration = 0; // Calculated during logo list draw
            EditorGUI.LabelField(rect, "Logos");
        }

        private void DrawElementUnityLogo(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = m_SplashScreenLogos.GetArrayElementAtIndex(index);
            var duration = element.FindPropertyRelative("duration");

            // Unity logo
            float logoWidth = Mathf.Clamp(rect.width - k_LogoListPropertyMinWidth, k_LogoListUnityLogoMinWidth, k_LogoListUnityLogoMaxWidth);
            float logoHeight = logoWidth / (s_UnityLogo.texture.width / (float)s_UnityLogo.texture.height);
            var logoRect = new Rect(rect.x, rect.y + (rect.height - logoHeight) / 2.0f, k_LogoListUnityLogoMaxWidth, logoHeight);
            var oldCol = GUI.color;
            GUI.color = (m_SplashScreenLogoStyle.intValue == (int)PlayerSettings.SplashScreen.UnityLogoStyle.DarkOnLight ? Color.black : Color.white);
            GUI.Label(logoRect, s_UnityLogo.texture);
            GUI.color = oldCol;

            // Properties
            var oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = k_LogoListPropertyLabelWidth;
            var propertyRect = new Rect(rect.x + logoWidth, rect.y + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight, rect.width - logoWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginChangeCheck();
            var durationLabel = EditorGUI.BeginProperty(propertyRect, k_Texts.logoDuration, duration);
            var newDurationVal = EditorGUI.Slider(propertyRect, durationLabel, duration.floatValue, k_MinLogoTime, k_MaxLogoTime);
            if (EditorGUI.EndChangeCheck())
                duration.floatValue = newDurationVal;
            EditorGUI.EndProperty();
            EditorGUIUtility.labelWidth = oldLabelWidth;

            m_TotalLogosDuration += duration.floatValue;
        }

        private void DrawLogoListElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.height -= EditorGUIUtility.standardVerticalSpacing;

            var element = m_SplashScreenLogos.GetArrayElementAtIndex(index);
            var logo = element.FindPropertyRelative("logo");

            if ((Sprite)logo.objectReferenceValue == s_UnityLogo)
            {
                DrawElementUnityLogo(rect, index, isActive, isFocused);
                return;
            }

            // Logo field
            float unityLogoWidth = Mathf.Clamp(rect.width - k_LogoListPropertyMinWidth, k_LogoListUnityLogoMinWidth, k_LogoListUnityLogoMaxWidth);
            var logoRect = new Rect(rect.x, rect.y + (rect.height - k_LogoListLogoFieldHeight) / 2.0f, k_LogoListUnityLogoMinWidth, k_LogoListLogoFieldHeight);
            EditorGUI.BeginChangeCheck();
            var value = EditorGUI.ObjectField(logoRect, GUIContent.none, (Sprite)logo.objectReferenceValue, typeof(Sprite), false);
            if (EditorGUI.EndChangeCheck())
                logo.objectReferenceValue = value;

            // Properties
            var oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = k_LogoListPropertyLabelWidth;
            var propertyRect = new Rect(rect.x + unityLogoWidth, rect.y + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight, rect.width - unityLogoWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginChangeCheck();
            var duration = element.FindPropertyRelative("duration");
            var newDurationVal = EditorGUI.Slider(propertyRect, k_Texts.logoDuration, duration.floatValue, k_MinLogoTime, k_MaxLogoTime);
            if (EditorGUI.EndChangeCheck())
                duration.floatValue = newDurationVal;

            EditorGUIUtility.labelWidth = oldLabelWidth;

            m_TotalLogosDuration += duration.floatValue;
        }

        private void DrawLogoListFooterCallback(Rect rect)
        {
            float totalDuration = Mathf.Max(k_MinLogoTime, m_TotalLogosDuration);
            EditorGUI.LabelField(rect, "Splash Screen Duration: " + totalDuration.ToString(), EditorStyles.miniBoldLabel);
            ReorderableList.defaultBehaviours.DrawFooter(rect, m_LogoList);
        }

        private void OnLogoListAddCallback(ReorderableList list)
        {
            int index = m_SplashScreenLogos.arraySize;
            m_SplashScreenLogos.InsertArrayElementAtIndex(m_SplashScreenLogos.arraySize);
            var element = m_SplashScreenLogos.GetArrayElementAtIndex(index);

            // Set up default values.
            var logo = element.FindPropertyRelative("logo");
            var duration = element.FindPropertyRelative("duration");
            logo.objectReferenceValue = null;
            duration.floatValue = k_DefaultLogoTime;
        }

        // Prevent users removing the unity logo.
        private static bool OnLogoListCanRemoveCallback(ReorderableList list)
        {
            var element = list.serializedProperty.GetArrayElementAtIndex(list.index);
            var logo = (Sprite)element.FindPropertyRelative("logo").objectReferenceValue;
            return logo != s_UnityLogo;
        }

        private void AddUnityLogoToLogosList()
        {
            // Only add a logo if one does not already exist.
            for (int i = 0; i < m_SplashScreenLogos.arraySize; ++i)
            {
                var listElement = m_SplashScreenLogos.GetArrayElementAtIndex(i);
                var listLogo = listElement.FindPropertyRelative("logo");
                if ((Sprite)listLogo.objectReferenceValue == s_UnityLogo)
                    return;
            }

            m_SplashScreenLogos.InsertArrayElementAtIndex(0);
            var element = m_SplashScreenLogos.GetArrayElementAtIndex(0);
            var logo = element.FindPropertyRelative("logo");
            var duration = element.FindPropertyRelative("duration");
            logo.objectReferenceValue = s_UnityLogo;
            duration.floatValue = k_DefaultLogoTime;
        }

        private void RemoveUnityLogoFromLogosList()
        {
            for (int i = 0; i < m_SplashScreenLogos.arraySize; ++i)
            {
                var element = m_SplashScreenLogos.GetArrayElementAtIndex(i);
                var logo = element.FindPropertyRelative("logo");
                if ((Sprite)logo.objectReferenceValue == s_UnityLogo)
                {
                    m_SplashScreenLogos.DeleteArrayElementAtIndex(i);
                    --i; // Continue checking in case we have duplicates.
                }
            }
        }

        private static bool TargetSupportsOptionalBuiltinSplashScreen(BuildTargetGroup targetGroup, ISettingEditorExtension settingsExtension)
        {
            if (settingsExtension != null)
                return settingsExtension.CanShowUnitySplashScreen();
            return targetGroup == BuildTargetGroup.Standalone;
        }

        private static void ObjectReferencePropertyField<T>(SerializedProperty property, GUIContent label) where T : UnityEngine.Object
        {
            EditorGUI.BeginChangeCheck();
            Rect r = EditorGUILayout.GetControlRect(true, 64, EditorStyles.objectFieldThumb);
            label = EditorGUI.BeginProperty(r, label, property);
            var value = EditorGUI.ObjectField(r, label, (T)property.objectReferenceValue, typeof(T), false);
            if (EditorGUI.EndChangeCheck())
            {
                property.objectReferenceValue = value;
                GUI.changed = true;
            }
            EditorGUI.EndProperty();
        }

        public void SplashSectionGUI(BuildPlatform platform, BuildTargetGroup targetGroup, ISettingEditorExtension settingsExtension, int sectionIndex = 2)
        {
            GUI.changed = false;
            if (m_Owner.BeginSettingsBox(sectionIndex, k_Texts.title))
            {
                if (targetGroup == BuildTargetGroup.Standalone)
                {
                    ObjectReferencePropertyField<Texture2D>(m_ResolutionDialogBanner, k_Texts.configDialogBanner);
                    EditorGUILayout.Space();
                }

                if (m_Owner.m_VRSettings.TargetGroupSupportsVirtualReality(targetGroup))
                    ObjectReferencePropertyField<Texture2D>(m_VirtualRealitySplashScreen, k_Texts.vrSplashScreen);

                if (TargetSupportsOptionalBuiltinSplashScreen(targetGroup, settingsExtension))
                    BuiltinCustomSplashScreenGUI();

                if (settingsExtension != null)
                    settingsExtension.SplashSectionGUI();

                if (m_ShowUnitySplashScreen.boolValue)
                    m_Owner.ShowSharedNote();
            }
            m_Owner.EndSettingsBox();
        }

        private void BuiltinCustomSplashScreenGUI()
        {
            EditorGUILayout.LabelField(k_Texts.splashTitle, EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!licenseAllowsDisabling))
            {
                EditorGUILayout.PropertyField(m_ShowUnitySplashScreen, k_Texts.showSplash);
                if (!m_ShowUnitySplashScreen.boolValue)
                    return;
            }

            Rect previewButtonRect = GUILayoutUtility.GetRect(k_Texts.previewSplash, "button");
            previewButtonRect = EditorGUI.PrefixLabel(previewButtonRect, new GUIContent(" "));
            if (GUI.Button(previewButtonRect, k_Texts.previewSplash))
            {
                SplashScreen.Begin();

                var gv = GameView.GetMainGameView();
                if (gv)
                    gv.Focus();

                GameView.RepaintAll();
            }

            EditorGUILayout.PropertyField(m_SplashScreenLogoStyle, k_Texts.splashStyle);

            // Animation
            EditorGUILayout.PropertyField(m_SplashScreenAnimation, k_Texts.animate);
            m_ShowAnimationControlsAnimator.target = m_SplashScreenAnimation.intValue == (int)PlayerSettings.SplashScreen.AnimationMode.Custom;

            if (EditorGUILayout.BeginFadeGroup(m_ShowAnimationControlsAnimator.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Slider(m_SplashScreenLogoAnimationZoom, 0.0f, 1.0f, k_Texts.logoZoom);
                EditorGUILayout.Slider(m_SplashScreenBackgroundAnimationZoom, 0.0f, 1.0f, k_Texts.backgroundZoom);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();

            EditorGUILayout.Space();

            // Logos
            EditorGUILayout.LabelField(k_Texts.logosTitle, EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(!Application.HasProLicense()))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_ShowUnitySplashLogo, k_Texts.showLogo);
                if (EditorGUI.EndChangeCheck())
                {
                    if (!m_ShowUnitySplashLogo.boolValue)
                        RemoveUnityLogoFromLogosList();
                    else if (m_SplashScreenDrawMode.intValue == (int)PlayerSettings.SplashScreen.DrawMode.AllSequential)
                        AddUnityLogoToLogosList();
                }

                m_ShowLogoControlsAnimator.target = m_ShowUnitySplashLogo.boolValue;
            }

            if (EditorGUILayout.BeginFadeGroup(m_ShowLogoControlsAnimator.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                var oldDrawmode = m_SplashScreenDrawMode.intValue;
                EditorGUILayout.PropertyField(m_SplashScreenDrawMode, k_Texts.drawMode);
                if (oldDrawmode != m_SplashScreenDrawMode.intValue)
                {
                    if (m_SplashScreenDrawMode.intValue == (int)PlayerSettings.SplashScreen.DrawMode.UnityLogoBelow)
                        RemoveUnityLogoFromLogosList();
                    else
                        AddUnityLogoToLogosList();
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();

            m_LogoList.DoLayoutList();
            EditorGUILayout.Space();

            // Background
            EditorGUILayout.LabelField(k_Texts.backgroundTitle, EditorStyles.boldLabel);
            EditorGUILayout.Slider(m_SplashScreenOverlayOpacity, Application.HasProLicense() ? k_MinProEditionOverlayOpacity : k_MinPersonalEditionOverlayOpacity, 1.0f, k_Texts.overlayOpacity);
            m_ShowBackgroundColorAnimator.target = m_SplashScreenBackgroundLandscape.objectReferenceValue == null;
            if (EditorGUILayout.BeginFadeGroup(m_ShowBackgroundColorAnimator.faded))
                EditorGUILayout.PropertyField(m_SplashScreenBackgroundColor, k_Texts.backgroundColor);
            EditorGUILayout.EndFadeGroup();

            ObjectReferencePropertyField<Sprite>(m_SplashScreenBackgroundLandscape, k_Texts.backgroundImage);
            if (GUI.changed && m_SplashScreenBackgroundLandscape.objectReferenceValue == null)
                m_SplashScreenBackgroundPortrait.objectReferenceValue = null;

            using (new EditorGUI.DisabledScope(m_SplashScreenBackgroundLandscape.objectReferenceValue == null))
            {
                ObjectReferencePropertyField<Sprite>(m_SplashScreenBackgroundPortrait, k_Texts.backgroundPortraitImage);
            }
        }
    }
}
