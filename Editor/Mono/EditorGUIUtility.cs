// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEditorInternal;
using UnityEngine.Events;
using UnityEngine.Internal;
using UnityEngine.Scripting;
using UnityEngineInternal;
using UnityEditor.StyleSheets;
using UnityEditor.Experimental;

using UnityObject = UnityEngine.Object;

namespace UnityEditor
{
    public sealed partial class EditorGUIUtility : GUIUtility
    {
        public class IconSizeScope : GUI.Scope
        {
            private readonly Vector2 m_OriginalIconSize;

            public IconSizeScope(Vector2 iconSizeWithinScope)
            {
                m_OriginalIconSize = GetIconSize();
                SetIconSize(iconSizeWithinScope);
            }

            protected override void CloseScope()
            {
                SetIconSize(m_OriginalIconSize);
            }
        }

        internal static Material s_GUITextureBlit2SRGBMaterial;
        internal static Material GUITextureBlit2SRGBMaterial
        {
            get
            {
                if (!s_GUITextureBlit2SRGBMaterial)
                {
                    Shader shader = LoadRequired("SceneView/GUITextureBlit2SRGB.shader") as Shader;
                    s_GUITextureBlit2SRGBMaterial = new Material(shader) {hideFlags = HideFlags.HideAndDontSave};
                }
                s_GUITextureBlit2SRGBMaterial.SetFloat("_ManualTex2SRGB", QualitySettings.activeColorSpace == ColorSpace.Linear ? 1.0f : 0.0f);
                return s_GUITextureBlit2SRGBMaterial;
            }
        }

        internal static Material s_GUITextureBlitSceneGUI;
        internal static Material GUITextureBlitSceneGUIMaterial
        {
            get
            {
                if (!s_GUITextureBlitSceneGUI)
                {
                    Shader shader = LoadRequired("SceneView/GUITextureBlitSceneGUI.shader") as Shader;
                    s_GUITextureBlitSceneGUI = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                }
                return s_GUITextureBlitSceneGUI;
            }
        }

        internal static int s_FontIsBold = -1;
        internal static int s_LastControlID = 0;
        private static float s_LabelWidth = 0f;

        private static Texture2D s_InfoIcon;
        private static Texture2D s_WarningIcon;
        private static Texture2D s_ErrorIcon;

        private static GUIStyle s_WhiteTextureStyle;
        private static GUIStyle s_BasicTextureStyle;

        static Hashtable s_TextGUIContents = new Hashtable();
        static Hashtable s_GUIContents = new Hashtable();
        static Hashtable s_IconGUIContents = new Hashtable();

        private static readonly GUIContent s_ObjectContent = new GUIContent();
        private static readonly GUIContent s_Text = new GUIContent();
        private static readonly GUIContent s_Image = new GUIContent();
        private static readonly GUIContent s_TextImage = new GUIContent();

        internal static readonly SVC<Color> kViewBackgroundColor = new SVC<Color>("view", StyleKeyword.backgroundColor, GetDefaultBackgroundColor);

        /// The current UI scaling factor for high-DPI displays. For instance, 2.0 on a retina display

        public new static float pixelsPerPoint => GUIUtility.pixelsPerPoint;

        static EditorGUIUtility()
        {
            GUISkin.m_SkinChanged += SkinChanged;
        }

        internal static void RepaintCurrentWindow()
        {
            CheckOnGUI();
            GUIView.current.Repaint();
        }

        internal static bool HasCurrentWindowKeyFocus()
        {
            CheckOnGUI();
            return GUIView.current.hasFocus;
        }

        public static Rect PointsToPixels(Rect rect)
        {
            var cachedPixelsPerPoint = pixelsPerPoint;
            rect.x *= cachedPixelsPerPoint;
            rect.y *= cachedPixelsPerPoint;
            rect.width *= cachedPixelsPerPoint;
            rect.height *= cachedPixelsPerPoint;
            return rect;
        }

        public static Rect PixelsToPoints(Rect rect)
        {
            var cachedInvPixelsPerPoint = 1f / pixelsPerPoint;
            rect.x *= cachedInvPixelsPerPoint;
            rect.y *= cachedInvPixelsPerPoint;
            rect.width *= cachedInvPixelsPerPoint;
            rect.height *= cachedInvPixelsPerPoint;
            return rect;
        }

        public static Vector2 PointsToPixels(Vector2 position)
        {
            var cachedPixelsPerPoint = pixelsPerPoint;
            position.x *= cachedPixelsPerPoint;
            position.y *= cachedPixelsPerPoint;
            return position;
        }

        public static Vector2 PixelsToPoints(Vector2 position)
        {
            var cachedInvPixelsPerPoint = 1f / pixelsPerPoint;
            position.x *= cachedInvPixelsPerPoint;
            position.y *= cachedInvPixelsPerPoint;
            return position;
        }

        // Given a rectangle, GUI style and a list of items, lay them out sequentially;
        // left to right, top to bottom.
        public static List<Rect> GetFlowLayoutedRects(Rect rect, GUIStyle style, float horizontalSpacing, float verticalSpacing, List<string> items)
        {
            var result = new List<Rect>(items.Count);
            var curPos = rect.position;
            foreach (string item in items)
            {
                var gc = TempContent(item);
                var itemSize = style.CalcSize(gc);
                var itemRect = new Rect(curPos, itemSize);

                // Reached right side, go to next row
                if (curPos.x + itemSize.x + horizontalSpacing >= rect.xMax)
                {
                    curPos.x = rect.x;
                    curPos.y += itemSize.y + verticalSpacing;
                    itemRect.position = curPos;
                }
                result.Add(itemRect);

                // Move next item to the left
                curPos.x += itemSize.x + horizontalSpacing;
            }

            return result;
        }

        internal class SkinnedColor
        {
            Color normalColor;
            Color proColor;

            public SkinnedColor(Color color, Color proColor)
            {
                normalColor = color;
                this.proColor = proColor;
            }

            public SkinnedColor(Color color)
            {
                normalColor = color;
                proColor = color;
            }

            public Color color
            {
                get { return isProSkin ? proColor : normalColor; }

                set
                {
                    if (isProSkin)
                        proColor = value;
                    else
                        normalColor = value;
                }
            }

            public static implicit operator Color(SkinnedColor colorSkin)
            {
                return colorSkin.color;
            }
        }

        private delegate bool HeaderItemDelegate(Rect rectangle, UnityObject[] targets);
        private static List<HeaderItemDelegate> s_EditorHeaderItemsMethods = null;
        internal static Rect DrawEditorHeaderItems(Rect rectangle, UnityObject[] targetObjs)
        {
            if (targetObjs.Length == 0 || (targetObjs.Length == 1 && targetObjs[0].GetType() == typeof(System.Object)))
                return rectangle;

            if (s_EditorHeaderItemsMethods == null)
            {
                List<Type> targetObjTypes = new List<Type>();
                var type = targetObjs[0].GetType();
                while (type.BaseType != null)
                {
                    targetObjTypes.Add(type);
                    type = type.BaseType;
                }

                AttributeHelper.MethodInfoSorter methods = AttributeHelper.GetMethodsWithAttribute<EditorHeaderItemAttribute>(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
                Func<EditorHeaderItemAttribute, bool> filter = (a) => targetObjTypes.Any(c => a.TargetType == c);
                var methodInfos = methods.FilterAndSortOnAttribute(filter, (a) => a.callbackOrder);
                s_EditorHeaderItemsMethods = new List<HeaderItemDelegate>();
                foreach (MethodInfo methodInfo in methodInfos)
                {
                    s_EditorHeaderItemsMethods.Add((HeaderItemDelegate)Delegate.CreateDelegate(typeof(HeaderItemDelegate), methodInfo));
                }
            }

            foreach (HeaderItemDelegate dele in s_EditorHeaderItemsMethods)
            {
                if (dele(rectangle, targetObjs))
                    rectangle.x -= rectangle.width;
            }

            return rectangle;
        }

        /// <summary>
        /// Use this container and helper class when implementing lock behaviour on a window when also using an <see cref="ActiveEditorTracker"/>.
        /// </summary>
        [Serializable]
        internal class EditorLockTrackerWithActiveEditorTracker : EditorLockTracker
        {
            internal override bool isLocked
            {
                get
                {
                    if (m_Tracker != null)
                    {
                        base.isLocked = m_Tracker.isLocked;
                        return m_Tracker.isLocked;
                    }
                    return base.isLocked;
                }
                set
                {
                    if (m_Tracker != null)
                    {
                        m_Tracker.isLocked = value;
                    }
                    base.isLocked = value;
                }
            }

            [SerializeField, HideInInspector]
            ActiveEditorTracker m_Tracker;

            internal ActiveEditorTracker tracker
            {
                get { return m_Tracker; }
                set
                {
                    m_Tracker = value;
                    if (m_Tracker != null)
                    {
                        isLocked = m_Tracker.isLocked;
                    }
                }
            }
        }

        /// <summary>
        /// Use this container and helper class when implementing lock behaviour on a window.
        /// </summary>
        [Serializable]
        internal class EditorLockTracker
        {
            [Serializable] public class LockStateEvent : UnityEvent<bool> {}
            [HideInInspector]
            internal LockStateEvent lockStateChanged = new LockStateEvent();

            const string k_LockMenuText = "Lock";
            static readonly GUIContent k_LockMenuGUIContent =  TextContent(k_LockMenuText);

            /// <summary>
            /// don't set or get this directly unless from within the <see cref="isLocked"/> property,
            /// as that property also keeps track of the potentially existing tracker in <see cref="EditorLockTrackerWithActiveEditorTracker"/>
            /// </summary>
            [SerializeField, HideInInspector]
            bool m_IsLocked;

            internal virtual bool isLocked
            {
                get
                {
                    return m_IsLocked;
                }
                set
                {
                    bool wasLocked = m_IsLocked;
                    m_IsLocked = value;

                    if (wasLocked != m_IsLocked)
                    {
                        lockStateChanged.Invoke(m_IsLocked);
                    }
                }
            }

            internal virtual void AddItemsToMenu(GenericMenu menu, bool disabled = false)
            {
                if (disabled)
                {
                    menu.AddDisabledItem(k_LockMenuGUIContent);
                }
                else
                {
                    menu.AddItem(k_LockMenuGUIContent, isLocked, FlipLocked);
                }
            }

            internal void ShowButton(Rect position, GUIStyle lockButtonStyle, bool disabled = false)
            {
                using (new EditorGUI.DisabledScope(disabled))
                {
                    EditorGUI.BeginChangeCheck();
                    bool newLock = GUI.Toggle(position, isLocked, GUIContent.none, lockButtonStyle);

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (newLock != isLocked)
                            FlipLocked();
                    }
                }
            }

            void FlipLocked()
            {
                isLocked = !isLocked;
            }
        }

        // Get a texture from its source filename
        public static Texture2D FindTexture(string name)
        {
            return FindTextureByName(name);
        }

        // Get texture from managed type
        internal static Texture2D FindTexture(Type type)
        {
            return FindTextureByType(type);
        }

        [ExcludeFromDocs]
        public static GUIContent TrTextContent(string key, string text, string tooltip, Texture icon)
        {
            GUIContent gc = (GUIContent)s_GUIContents[key];
            if (gc == null)
            {
                gc = new GUIContent(L10n.Tr(text));
                if (tooltip != null)
                {
                    gc.tooltip = L10n.Tr(tooltip);
                }
                if (icon != null)
                {
                    gc.image = icon;
                }
                s_GUIContents[key] = gc;
            }
            return gc;
        }

        [ExcludeFromDocs]
        public static GUIContent TrTextContent(string text, string tooltip = null, Texture icon = null)
        {
            string key = string.Format("{0}|{1}", text ?? "", tooltip ?? "");
            return TrTextContent(key, text, tooltip, icon);
        }

        [ExcludeFromDocs]
        public static GUIContent TrTextContent(string text, string tooltip, string iconName)
        {
            string key = string.Format("{0}|{1}|{2}", text ?? "", tooltip ?? "", iconName ?? "");
            return TrTextContent(key, text, tooltip, LoadIconRequired(iconName));
        }

        [ExcludeFromDocs]
        public static GUIContent TrTextContent(string text, Texture icon)
        {
            return TrTextContent(text, null, icon);
        }

        [ExcludeFromDocs]
        public static GUIContent TrTextContentWithIcon(string text, Texture icon)
        {
            return TrTextContent(text, null, icon);
        }

        [ExcludeFromDocs]
        public static GUIContent TrTextContentWithIcon(string text, string iconName)
        {
            return TrTextContent(text, null, iconName);
        }

        [ExcludeFromDocs]
        public static GUIContent TrTextContentWithIcon(string text, string tooltip, string iconName)
        {
            return TrTextContent(text, tooltip, iconName);
        }

        [ExcludeFromDocs]
        public static GUIContent TrTextContentWithIcon(string text, string tooltip, Texture icon)
        {
            return TrTextContent(text, tooltip, icon);
        }

        [ExcludeFromDocs]
        public static GUIContent TrTextContentWithIcon(string text, string tooltip, MessageType messageType)
        {
            return TrTextContent(text, tooltip, GetHelpIcon(messageType));
        }

        [ExcludeFromDocs]
        public static GUIContent TrTextContentWithIcon(string text, MessageType messageType)
        {
            return TrTextContentWithIcon(text, null, messageType);
        }

        [ExcludeFromDocs]
        public static GUIContent TrIconContent(string iconName, string tooltip = null)
        {
            string key = (tooltip == null ? iconName : iconName + tooltip);
            GUIContent gc = (GUIContent)s_IconGUIContents[key];
            if (gc != null)
            {
                return gc;
            }
            gc = new GUIContent();

            if (tooltip != null)
            {
                gc.tooltip = L10n.Tr(tooltip);
            }
            gc.image = LoadIconRequired(iconName);
            s_IconGUIContents[key] = gc;
            return gc;
        }

        [ExcludeFromDocs]
        public static GUIContent TrIconContent(Texture icon, string tooltip = null)
        {
            GUIContent gc = (tooltip != null) ? (GUIContent)s_IconGUIContents[tooltip] : null;
            if (gc != null)
            {
                return gc;
            }
            gc = new GUIContent { image = icon };
            if (tooltip != null)
            {
                gc.tooltip = L10n.Tr(tooltip);
                s_IconGUIContents[tooltip] = gc;
            }

            return gc;
        }

        [ExcludeFromDocs]
        public static GUIContent TrTempContent(string t)
        {
            return TempContent(L10n.Tr(t));
        }

        [ExcludeFromDocs]
        public static GUIContent[] TrTempContent(string[] texts)
        {
            GUIContent[] retval = new GUIContent[texts.Length];
            for (int i = 0; i < texts.Length; i++)
                retval[i] = new GUIContent(L10n.Tr(texts[i]));
            return retval;
        }

        [ExcludeFromDocs]
        public static GUIContent[] TrTempContent(string[] texts, string[] tooltips)
        {
            GUIContent[] retval = new GUIContent[texts.Length];
            for (int i = 0; i < texts.Length; i++)
                retval[i] = new GUIContent(L10n.Tr(texts[i]), L10n.Tr(tooltips[i]));
            return retval;
        }

        internal static GUIContent TrIconContent<T>(string tooltip = null) where T : UnityObject
        {
            return TrIconContent(FindTexture(typeof(T)), tooltip);
        }

        public static float singleLineHeight => EditorGUI.kSingleLineHeight;
        public static float standardVerticalSpacing => EditorGUI.kControlVerticalSpacing;

        internal static SliderLabels sliderLabels = new SliderLabels();

        internal static GUIContent TextContent(string textAndTooltip)
        {
            if (textAndTooltip == null)
                textAndTooltip = "";

            string key = textAndTooltip;

            GUIContent gc = (GUIContent)s_TextGUIContents[key];
            if (gc == null)
            {
                string[] strings = GetNameAndTooltipString(textAndTooltip);
                gc = new GUIContent(strings[1]);

                if (strings[2] != null)
                {
                    gc.tooltip = strings[2];
                }
                s_TextGUIContents[key] = gc;
            }
            return gc;
        }

        internal static GUIContent TextContentWithIcon(string textAndTooltip, string icon)
        {
            if (textAndTooltip == null)
                textAndTooltip = "";

            if (icon == null)
                icon = "";

            string key = string.Format("{0}|{1}", textAndTooltip, icon);

            GUIContent gc = (GUIContent)s_TextGUIContents[key];
            if (gc == null)
            {
                string[] strings = GetNameAndTooltipString(textAndTooltip);
                gc = new GUIContent(strings[1]) { image = LoadIconRequired(icon) };

                // We want to catch missing icons so we can fix them (therefore using LoadIconRequired)

                if (strings[2] != null)
                {
                    gc.tooltip = strings[2];
                }
                s_TextGUIContents[key] = gc;
            }
            return gc;
        }

        private static Color GetDefaultBackgroundColor()
        {
            float kViewBackgroundIntensity = isProSkin ? 0.22f : 0.76f;
            return new Color(kViewBackgroundIntensity, kViewBackgroundIntensity, kViewBackgroundIntensity, 1f);
        }

        // [0] original name, [1] localized name, [2] localized tooltip
        internal static string[] GetNameAndTooltipString(string nameAndTooltip)
        {
            string[] retval = new string[3];

            string[] s1 = nameAndTooltip.Split('|');

            switch (s1.Length)
            {
                case 0:
                    retval[0] = "";
                    retval[1] = "";
                    break;
                case 1:
                    retval[0] = s1[0].Trim();
                    retval[1] = retval[0];
                    break;
                case 2:
                    retval[0] = s1[0].Trim();
                    retval[1] = retval[0];
                    retval[2] = s1[1].Trim();
                    break;
                default:
                    Debug.LogError("Error in Tooltips: Too many strings in line beginning with '" + s1[0] + "'");
                    break;
            }
            return retval;
        }

        internal static Texture2D LoadIconRequired(string name)
        {
            Texture2D tex = LoadIcon(name);

            if (!tex)
                Debug.LogErrorFormat("Unable to load the icon: '{0}'.\nNote that either full project path should be used (with extension) " +
                    "or just the icon name if the icon is located in the following location: '{1}' (without extension, since png is assumed)",
                    name, EditorResources.editorDefaultResourcesPath + EditorResources.iconsPath);

            return tex;
        }

        // Automatically loads version of icon that matches current skin.
        // Equivalent to Texture2DNamed in ObjectImages.cpp
        internal static Texture2D LoadIcon(string name)
        {
            return LoadIconForSkin(name, skinIndex);
        }

        // Attempts to load a higher resolution icon if needed
        static Texture2D LoadGeneratedIconOrNormalIcon(string name)
        {
            Texture2D icon = null;
            if (GUIUtility.pixelsPerPoint > 1.0f)
            {
                icon = InnerLoadGeneratedIconOrNormalIcon(name + "@2x");
                if (icon != null)
                {
                    icon.pixelsPerPoint = 2.0f;
                }
            }

            if (icon == null)
            {
                icon = InnerLoadGeneratedIconOrNormalIcon(name);
            }

            if (icon != null &&
                !Mathf.Approximately(icon.pixelsPerPoint, GUIUtility.pixelsPerPoint) && //scaling are different
                !Mathf.Approximately(GUIUtility.pixelsPerPoint % 1, 0)) //screen scaling is non-integer
            {
                icon.filterMode = FilterMode.Bilinear;
            }

            return icon;
        }

        // Takes a name that already includes d_ if dark skin version is desired.
        // Equivalent to Texture2DSkinNamed in ObjectImages.cpp
        static Texture2D InnerLoadGeneratedIconOrNormalIcon(string name)
        {
            Texture2D tex = Load(EditorResources.generatedIconsPath + name + ".asset") as Texture2D;

            if (!tex)
            {
                tex = Load(EditorResources.iconsPath + name + ".png") as Texture2D;
            }
            if (!tex)
            {
                tex = Load(name) as Texture2D; // Allow users to specify their own project path to an icon (e.g see EditorWindowTitleAttribute)
            }

            return tex;
        }

        internal static Texture2D LoadIconForSkin(string name, int in_SkinIndex)
        {
            if (String.IsNullOrEmpty(name))
                return null;

            if (in_SkinIndex == 0)
                return LoadGeneratedIconOrNormalIcon(name);

            //Remap file name for dark skin
            var newName = "d_" + Path.GetFileName(name);
            var dirName = Path.GetDirectoryName(name);
            if (!String.IsNullOrEmpty(dirName))
                newName = String.Format("{0}/{1}", dirName, newName);

            Texture2D tex = LoadGeneratedIconOrNormalIcon(newName);
            if (!tex)
                tex = LoadGeneratedIconOrNormalIcon(name);
            return tex;
        }

        internal static GUIContent IconContent<T>(string text = null) where T : UnityObject
        {
            return IconContent(FindTexture(typeof(T)), text);
        }

        [ExcludeFromDocs]
        public static GUIContent IconContent(string name)
        {
            return IconContent(name, null);
        }

        public static GUIContent IconContent(string name, [DefaultValue("null")] string text)
        {
            GUIContent gc = (GUIContent)s_IconGUIContents[name];
            if (gc != null)
            {
                return gc;
            }
            gc = new GUIContent();

            if (text != null)
            {
                string[] strings = GetNameAndTooltipString(text);
                if (strings[2] != null)
                {
                    gc.tooltip = strings[2];
                }
            }
            gc.image = LoadIconRequired(name);
            s_IconGUIContents[name] = gc;
            return gc;
        }

        private static GUIContent IconContent(Texture icon, string text)
        {
            GUIContent gc = text != null ? (GUIContent)s_IconGUIContents[text] : null;
            if (gc != null)
            {
                return gc;
            }
            gc = new GUIContent { image = icon };

            if (text != null)
            {
                string[] strings = GetNameAndTooltipString(text);
                if (strings[2] != null)
                {
                    gc.tooltip = strings[2];
                }
                s_IconGUIContents[text] = gc;
            }
            return gc;
        }

        // Is the user currently using the pro skin? (RO)
        public static bool isProSkin => skinIndex == 1;

        internal static void Internal_SwitchSkin()
        {
            skinIndex = 1 - skinIndex;
        }

        // Return a GUIContent object with the name and icon of an Object.
        public static GUIContent ObjectContent(UnityObject obj, Type type)
        {
            if (obj)
            {
                s_ObjectContent.text = GetObjectNameWithInfo(obj);
                s_ObjectContent.image = AssetPreview.GetMiniThumbnail(obj);
            }
            else if (type != null)
            {
                s_ObjectContent.text = GetTypeNameWithInfo(type.Name);
                s_ObjectContent.image = AssetPreview.GetMiniTypeThumbnail(type);
            }
            else
            {
                s_ObjectContent.text = "<no type>";
                s_ObjectContent.image = null;
            }
            return s_ObjectContent;
        }

        internal static GUIContent TempContent(string t)
        {
            s_Text.image = null;
            s_Text.text = t;
            s_Text.tooltip = null;
            return s_Text;
        }

        internal static GUIContent TempContent(Texture i)
        {
            s_Image.image = i;
            s_Image.text = null;
            s_Image.tooltip = null;
            return s_Image;
        }

        internal static GUIContent TempContent(string t, Texture i)
        {
            s_TextImage.image = i;
            s_TextImage.text = t;
            s_TextImage.tooltip = null;
            return s_TextImage;
        }

        internal static GUIContent[] TempContent(string[] texts)
        {
            GUIContent[] retval = new GUIContent[texts.Length];
            for (int i = 0; i < texts.Length; i++)
                retval[i] = new GUIContent(texts[i]);
            return retval;
        }

        internal static GUIContent[] TempContent(string[] texts, string[] tooltips)
        {
            GUIContent[] retval = new GUIContent[texts.Length];
            for (int i = 0; i < texts.Length; i++)
                retval[i] = new GUIContent(texts[i], tooltips[i]);
            return retval;
        }

        internal static bool HasHolddownKeyModifiers(Event evt)
        {
            return evt.shift | evt.control | evt.alt | evt.command;
        }

        // Does a given class have per-object thumbnails?
        public static bool HasObjectThumbnail(Type objType)
        {
            return objType != null && (objType.IsSubclassOf(typeof(Texture)) || objType == typeof(Texture) || objType == typeof(Sprite));
        }

        // Get the size that has been set using ::ref::SetIconSize.
        public static Vector2 GetIconSize()
        {
            //FIXME: this is how it really should be, but right now it seems to fail badly (unrelated null ref exceptions and then crash)
            return Internal_GetIconSize();
        }

        internal static Texture2D infoIcon => s_InfoIcon ?? (s_InfoIcon = LoadIcon("console.infoicon"));
        internal static Texture2D warningIcon => s_WarningIcon ?? (s_WarningIcon = LoadIcon("console.warnicon"));
        internal static Texture2D errorIcon => s_ErrorIcon ?? (s_ErrorIcon = LoadIcon("console.erroricon"));

        internal static Texture2D GetHelpIcon(MessageType type)
        {
            switch (type)
            {
                case MessageType.Info:
                    return infoIcon;
                case MessageType.Warning:
                    return warningIcon;
                case MessageType.Error:
                    return errorIcon;
            }
            return null;
        }

        // An invisible GUIContent that is not the same as GUIContent.none
        internal static GUIContent blankContent { get; } = new GUIContent(" ");

        internal static GUIStyle whiteTextureStyle => s_WhiteTextureStyle ??
        (s_WhiteTextureStyle = new GUIStyle {normal = {background = whiteTexture}});

        internal static GUIStyle GetBasicTextureStyle(Texture2D tex)
        {
            if (s_BasicTextureStyle == null)
                s_BasicTextureStyle = new GUIStyle();

            s_BasicTextureStyle.normal.background = tex;

            return s_BasicTextureStyle;
        }

        internal static void NotifyLanguageChanged(SystemLanguage newLanguage)
        {
            s_TextGUIContents = new Hashtable();
            s_GUIContents = new Hashtable();
            s_IconGUIContents = new Hashtable();
            EditorUtility.Internal_UpdateMenuTitleForLanguage(newLanguage);
            LocalizationDatabase.currentEditorLanguage = newLanguage;
            EditorApplication.RequestRepaintAllViews();
        }

        // Get one of the built-in GUI skins, which can be the game view, inspector or scene view skin as chosen by the parameter.
        public static GUISkin GetBuiltinSkin(EditorSkin skin)
        {
            return GUIUtility.GetBuiltinSkin((int)skin);
        }

        // Load a built-in resource that has to be there.
        public static UnityObject LoadRequired(string path)
        {
            var o = Load(path, typeof(UnityObject));
            if (!o)
                Debug.LogError("Unable to find required resource at " + path);
            return o;
        }

        // Load a built-in resource
        public static UnityObject Load(string path)
        {
            return Load(path, typeof(UnityObject));
        }

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedBySecondArgument)]
        private static UnityObject Load(string filename, Type type)
        {
            var asset = EditorResources.Load(filename, type);
            if (asset != null)
                return asset;

            AssetBundle bundle = GetEditorAssetBundle();
            if (bundle == null)
            {
                // If in batch mode, loading any Editor UI items shouldn't be needed
                if (Application.isBatchMode)
                    return null;
                throw new NullReferenceException("Failure to load editor resource asset bundle.");
            }

            asset = bundle.LoadAsset(filename, type);
            if (asset != null)
                return asset;

            return AssetDatabase.LoadAssetAtPath(filename, type);
        }

        public static void PingObject(UnityObject obj)
        {
            if (obj != null)
                PingObject(obj.GetInstanceID());
        }

        // Ping an object in a window like clicking it in an inspector
        public static void PingObject(int targetInstanceID)
        {
            foreach (SceneHierarchyWindow shw in SceneHierarchyWindow.GetAllSceneHierarchyWindows())
            {
                shw.FrameObject(targetInstanceID, true);
            }

            foreach (ProjectBrowser pb in ProjectBrowser.GetAllProjectBrowsers())
            {
                pb.FrameObject(targetInstanceID, true);
            }
        }

        internal static void MoveFocusAndScroll(bool forward)
        {
            int prev = keyboardControl;
            Internal_MoveKeyboardFocus(forward);
            if (prev != keyboardControl)
                RefreshScrollPosition();
        }

        internal static void RefreshScrollPosition()
        {
            Rect r;

            if (Internal_GetKeyboardRect(keyboardControl, out r))
            {
                GUI.ScrollTo(r);
            }
        }

        internal static void ScrollForTabbing(bool forward)
        {
            Rect r;

            if (Internal_GetKeyboardRect(Internal_GetNextKeyboardControlID(forward), out r))
            {
                GUI.ScrollTo(r);
            }
        }

        internal static void ResetGUIState()
        {
            GUI.skin = null;
            GUI.backgroundColor = GUI.contentColor = Color.white;
            GUI.color = EditorApplication.isPlayingOrWillChangePlaymode ? HostView.kPlayModeDarken : Color.white;
            GUI.enabled = true;
            GUI.changed = false;
            EditorGUI.indentLevel = 0;
            EditorGUI.ClearStacks();
            fieldWidth = 0;
            labelWidth = 0;

            SetBoldDefaultFont(false);
            UnlockContextWidth();
            hierarchyMode = false;
            wideMode = false;
            comparisonViewMode = ComparisonViewMode.None;

            //Clear the cache, so it uses the global one
            ScriptAttributeUtility.propertyHandlerCache = null;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("RenderGameViewCameras is no longer supported.Consider rendering cameras manually.", true)]
        public static void RenderGameViewCameras(Rect cameraRect, bool gizmos, bool gui) {}

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("RenderGameViewCameras is no longer supported.Consider rendering cameras manually.", true)]
        public static void RenderGameViewCameras(Rect cameraRect, Rect statsRect, bool gizmos, bool gui) {}

        // Called from C++ GetControlID method when run from the Editor.
        // Editor GUI needs some additional things to happen when calling GetControlID.
        // While this will also be called for runtime code running in Play mode in the Editor,
        // it won't have any effect. EditorGUIUtility.s_LastControlID will be set to the id,
        // but this is only used inside the handling of a single control
        // (see DoPropertyFieldKeyboardHandling).
        // EditorGUI.s_PrefixLabel.text will only be not null when EditorGUI.PrefixLabel
        // has been called without a specified controlID. The control following the PrefixLabel clears this.
        [RequiredByNativeCode]
        internal static void HandleControlID(int id)
        {
            s_LastControlID = id;
            EditorGUI.PrepareCurrentPrefixLabel(s_LastControlID);
        }

        public static bool editingTextField
        {
            get { return EditorGUI.RecycledTextEditor.s_ActuallyEditing; }
            set { EditorGUI.RecycledTextEditor.s_ActuallyEditing = value; }
        }

        public static bool textFieldHasSelection
        {
            get { return EditorGUI.s_RecycledEditor.hasSelection; }
        }

        // hierarchyMode changes how foldouts are drawn so the foldout triangle is drawn to the left,
        // outside the rect of the control, rather than inside the rect.
        // This way the text of the foldout lines up with the labels of other controls.
        // hierarchyMode is primarily enabled for editors in the Inspector.
        public static bool hierarchyMode { get; set; } = false;

        // wideMode is used when the Inspector is wide and uses a more tidy and vertically compact layout for certain controls.
        public static bool wideMode { get; set; } = false;

        internal enum ComparisonViewMode
        {
            None, Original, Modified
        }

        // ComparisonViewMode is used when editors are drawn in the context of showing differences between different objects.
        // Controls that must not be used in this context can be hidden or disabled.
        private static ComparisonViewMode s_ComparisonViewMode = ComparisonViewMode.None;
        internal static ComparisonViewMode comparisonViewMode
        {
            get { return s_ComparisonViewMode; }
            set { s_ComparisonViewMode = value; }
        }

        // Context width is used for calculating the label width for various editor controls.
        // In most cases the top level clip rect is a perfect context width.
        private static float s_ContextWidth = 0f;
        private static float CalcContextWidth()
        {
            float output = GUIClip.GetTopRect().width;
            // If there's no top clip rect, fallback to using screen width.
            if (output < 1f || output >= 40000)
                output = currentViewWidth;

            return output;
        }

        internal static void LockContextWidth()
        {
            s_ContextWidth = CalcContextWidth();
        }

        internal static void UnlockContextWidth()
        {
            s_ContextWidth = 0f;
        }

        internal static float contextWidth
        {
            get
            {
                if (s_ContextWidth > 0f)
                    return s_ContextWidth;

                return CalcContextWidth();
            }
        }

        public static float currentViewWidth => GUIView.current.position.width;

        public static float labelWidth
        {
            get
            {
                if (s_LabelWidth > 0)
                    return s_LabelWidth;

                if (hierarchyMode)
                    return Mathf.Max(contextWidth * 0.45f - 40, 120);
                return 150;
            }
            set { s_LabelWidth = value; }
        }

        private static float s_FieldWidth = 0f;
        public static float fieldWidth
        {
            get
            {
                if (s_FieldWidth > 0)
                    return s_FieldWidth;

                return 50;
            }
            set { s_FieldWidth = value; }
        }

        // Make all ref::EditorGUI look like regular controls.
        private const string k_LookLikeControlsObsoleteMessage = "LookLikeControls and LookLikeInspector modes are deprecated.Use EditorGUIUtility.labelWidth and EditorGUIUtility.fieldWidth to control label and field widths.";
        [Obsolete(k_LookLikeControlsObsoleteMessage, false)]
        public static void LookLikeControls(float _labelWidth, float _fieldWidth)
        {
            fieldWidth = _fieldWidth;
            labelWidth = _labelWidth;
        }

        [ExcludeFromDocs, Obsolete(k_LookLikeControlsObsoleteMessage, false)] public static void LookLikeControls(float _labelWidth) { LookLikeControls(_labelWidth, 0); }
        [ExcludeFromDocs, Obsolete(k_LookLikeControlsObsoleteMessage, false)] public static void LookLikeControls() { LookLikeControls(0, 0); }

        // Make all ::ref::EditorGUI look like simplified outline view controls.
        [Obsolete("LookLikeControls and LookLikeInspector modes are deprecated.", false)]
        public static void LookLikeInspector()
        {
            fieldWidth = 0;
            labelWidth = 0;
        }

        [Obsolete("This field is no longer used by any builtin controls. If passing this field to GetControlID, explicitly use the FocusType enum instead.", false)]
        public static FocusType native = FocusType.Keyboard;

        internal static void SkinChanged()
        {
            EditorStyles.UpdateSkinCache();
        }

        internal static Rect DragZoneRect(Rect position)
        {
            return new Rect(position.x, position.y, labelWidth, position.height);
        }

        internal static void SetBoldDefaultFont(bool isBold)
        {
            int wantsBold = isBold ? 1 : 0;
            if (wantsBold != s_FontIsBold)
            {
                SetDefaultFont(isBold ? EditorStyles.boldFont : EditorStyles.standardFont);
                s_FontIsBold = wantsBold;
            }
        }

        internal static bool GetBoldDefaultFont() { return s_FontIsBold == 1; }

        // Creates an event
        public static Event CommandEvent(string commandName)
        {
            Event e = new Event();
            Internal_SetupEventValues(e);
            e.type = EventType.ExecuteCommand;
            e.commandName = commandName;
            return e;
        }

        // Draw a color swatch.
        public static void DrawColorSwatch(Rect position, Color color)
        {
            DrawColorSwatch(position, color, true);
        }

        internal static void DrawColorSwatch(Rect position, Color color, bool showAlpha)
        {
            DrawColorSwatch(position, color, showAlpha, false);
        }

        internal static void DrawColorSwatch(Rect position, Color color, bool showAlpha, bool hdr)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Color oldColor = GUI.color;
            Color oldBackgroundColor = GUI.backgroundColor;

            float a = GUI.enabled ? 1 : 2;

            GUI.color = EditorGUI.showMixedValue ? new Color(0.82f, 0.82f, 0.82f, a) * oldColor : new Color(color.r, color.g, color.b, a);
            if (hdr)
                GUI.color = GUI.color.gamma;
            GUI.backgroundColor = Color.white;

            GUIStyle gs = whiteTextureStyle;
            gs.Draw(position, false, false, false, false);

            // Render LDR -> HDR gradients on the sides when having HDR values (to let the user see what the normalized color looks like)
            // Note that we use GUIStyle rendering methods to ensure this part works with OptimizedGUIBlock
            if (hdr)
            {
                Color32 baseColor;
                float exposure;
                ColorMutator.DecomposeHdrColor(GUI.color.linear, out baseColor, out exposure);

                if (!Mathf.Approximately(exposure, 0f))
                {
                    float gradientWidth = position.width / 3f;
                    Rect leftRect = new Rect(position.x, position.y, gradientWidth, position.height);
                    Rect rightRect = new Rect(position.xMax - gradientWidth, position.y, gradientWidth,
                        position.height);

                    Color orgColor = GUI.color;
                    GUI.color = ((Color)baseColor).gamma;
                    GUIStyle basicStyle = GetBasicTextureStyle(whiteTexture);
                    basicStyle.Draw(leftRect, false, false, false, false);
                    basicStyle.Draw(rightRect, false, false, false, false);
                    GUI.color = orgColor;

                    basicStyle = GetBasicTextureStyle(ColorPicker.GetGradientTextureWithAlpha0To1());
                    basicStyle.Draw(leftRect, false, false, false, false);
                    basicStyle = GetBasicTextureStyle(ColorPicker.GetGradientTextureWithAlpha1To0());
                    basicStyle.Draw(rightRect, false, false, false, false);
                }
            }

            if (!EditorGUI.showMixedValue)
            {
                if (showAlpha)
                {
                    GUI.color = new Color(0, 0, 0, a);
                    float alphaHeight = Mathf.Clamp(position.height * .2f, 2, 20);
                    Rect alphaBarRect = new Rect(position.x, position.yMax - alphaHeight, position.width, alphaHeight);
                    gs.Draw(alphaBarRect, false, false, false, false);

                    GUI.color = new Color(1, 1, 1, a);
                    alphaBarRect.width *= Mathf.Clamp01(color.a);
                    gs.Draw(alphaBarRect, false, false, false, false);
                }
            }
            else
            {
                EditorGUI.BeginHandleMixedValueContentColor();
                gs.Draw(position, EditorGUI.mixedValueContent, false, false, false, false);
                EditorGUI.EndHandleMixedValueContentColor();
            }

            GUI.color = oldColor;
            GUI.backgroundColor = oldBackgroundColor;

            // HDR label overlay
            if (hdr)
            {
                GUI.Label(new Rect(position.x, position.y, position.width - 3, position.height), "HDR", EditorStyles.centeredGreyMiniLabel);
            }
        }

        internal static void DrawRegionSwatch(Rect position, SerializedProperty property, SerializedProperty property2, Color color, Color bgColor)
        {
            DrawCurveSwatchInternal(position, null, null, property, property2, color, bgColor, false, new Rect(), Color.clear, Color.clear);
        }

        public static void DrawCurveSwatch(Rect position, AnimationCurve curve, SerializedProperty property, Color color, Color bgColor)
        {
            DrawCurveSwatchInternal(position, curve, null, property, null, color, bgColor, false, new Rect(), Color.clear, Color.clear);
        }

        public static void DrawCurveSwatch(Rect position, AnimationCurve curve, SerializedProperty property, Color color, Color bgColor, Color topFillColor, Color bottomFillColor)
        {
            DrawCurveSwatchInternal(position, curve, null, property, null, color, bgColor, false, new Rect(), topFillColor, bottomFillColor);
        }

        // Draw a curve swatch.
        public static void DrawCurveSwatch(Rect position, AnimationCurve curve, SerializedProperty property, Color color, Color bgColor, Color topFillColor, Color bottomFillColor, Rect curveRanges)
        {
            DrawCurveSwatchInternal(position, curve, null, property, null, color, bgColor, true, curveRanges, topFillColor, bottomFillColor);
        }

        public static void DrawCurveSwatch(Rect position, AnimationCurve curve, SerializedProperty property, Color color, Color bgColor, Rect curveRanges)
        {
            DrawCurveSwatchInternal(position, curve, null, property, null, color, bgColor, true, curveRanges, Color.clear, Color.clear);
        }

        // Draw swatch with a filled region between two SerializedProperty curves.
        public static void DrawRegionSwatch(Rect position, SerializedProperty property, SerializedProperty property2, Color color, Color bgColor, Rect curveRanges)
        {
            DrawCurveSwatchInternal(position, null, null, property, property2, color, bgColor, true, curveRanges, Color.clear, Color.clear);
        }

        // Draw swatch with a filled region between two curves.
        public static void DrawRegionSwatch(Rect position, AnimationCurve curve, AnimationCurve curve2, Color color, Color bgColor, Rect curveRanges)
        {
            DrawCurveSwatchInternal(position, curve, curve2, null, null, color, bgColor, true, curveRanges, Color.clear, Color.clear);
        }

        private static void DrawCurveSwatchInternal(Rect position, AnimationCurve curve, AnimationCurve curve2, SerializedProperty property, SerializedProperty property2, Color color, Color bgColor, bool useCurveRanges, Rect curveRanges, Color topFillColor, Color bottomFillColor)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            int previewWidth = (int)position.width;
            int previewHeight = (int)position.height;
            int maxTextureDim = SystemInfo.maxTextureSize;

            bool stretchX = previewWidth > maxTextureDim;
            bool stretchY = previewHeight > maxTextureDim;
            if (stretchX)
                previewWidth = Mathf.Min(previewWidth, maxTextureDim);
            if (stretchY)
                previewHeight = Mathf.Min(previewHeight, maxTextureDim);

            // Draw background color
            Color oldColor = GUI.color;
            GUI.color = bgColor;
            GUIStyle gs = whiteTextureStyle;
            gs.Draw(position, false, false, false, false);
            GUI.color = oldColor;

            if (property != null && property.hasMultipleDifferentValues)
            {
                // No obvious way to show that curve field has mixed values so we just draw
                // the same content as for text fields since the user at least know what that means.
                EditorGUI.BeginHandleMixedValueContentColor();
                GUI.Label(position, EditorGUI.mixedValueContent, "PreOverlayLabel");
                EditorGUI.EndHandleMixedValueContentColor();
            }
            else
            {
                Texture2D preview = null;
                if (property != null)
                {
                    if (property2 == null)
                        preview = useCurveRanges ? AnimationCurvePreviewCache.GetPreview(previewWidth, previewHeight, property, color, topFillColor, bottomFillColor, curveRanges) : AnimationCurvePreviewCache.GetPreview(previewWidth, previewHeight, property, color, topFillColor, bottomFillColor);
                    else
                        preview = useCurveRanges ? AnimationCurvePreviewCache.GetPreview(previewWidth, previewHeight, property, property2, color, topFillColor, bottomFillColor, curveRanges) : AnimationCurvePreviewCache.GetPreview(previewWidth, previewHeight, property, property2, color, topFillColor, bottomFillColor);
                }
                else if (curve != null)
                {
                    if (curve2 == null)
                        preview = useCurveRanges ? AnimationCurvePreviewCache.GetPreview(previewWidth, previewHeight, curve, color, topFillColor, bottomFillColor, curveRanges) : AnimationCurvePreviewCache.GetPreview(previewWidth, previewHeight, curve, color, topFillColor, bottomFillColor);
                    else
                        preview = useCurveRanges ? AnimationCurvePreviewCache.GetPreview(previewWidth, previewHeight, curve, curve2, color, topFillColor, bottomFillColor, curveRanges) : AnimationCurvePreviewCache.GetPreview(previewWidth, previewHeight, curve, curve2, color, topFillColor, bottomFillColor);
                }
                gs = GetBasicTextureStyle(preview);

                if (!stretchX && preview)
                    position.width = preview.width;
                if (!stretchY && preview)
                    position.height = preview.height;

                gs.Draw(position, false, false, false, false);
            }
        }

        // Convert a color from RGB to HSV color space.
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("EditorGUIUtility.RGBToHSV is obsolete. Use Color.RGBToHSV instead (UnityUpgradable) -> [UnityEngine] UnityEngine.Color.RGBToHSV(*)", true)]
        public static void RGBToHSV(Color rgbColor, out float H, out float S, out float V)
        {
            Color.RGBToHSV(rgbColor, out H, out S, out V);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("EditorGUIUtility.HSVToRGB is obsolete. Use Color.HSVToRGB instead (UnityUpgradable) -> [UnityEngine] UnityEngine.Color.HSVToRGB(*)", true)]
        public static Color HSVToRGB(float H, float S, float V)
        {
            return Color.HSVToRGB(H, S, V);
        }

        // Convert a set of HSV values to an RGB Color.
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("EditorGUIUtility.HSVToRGB is obsolete. Use Color.HSVToRGB instead (UnityUpgradable) -> [UnityEngine] UnityEngine.Color.HSVToRGB(*)", true)]
        public static Color HSVToRGB(float H, float S, float V, bool hdr)
        {
            return Color.HSVToRGB(H, S, V, hdr);
        }

        // Add a custom mouse pointer to a control
        public static void AddCursorRect(Rect position, MouseCursor mouse)
        {
            AddCursorRect(position, mouse, 0);
        }

        public static void AddCursorRect(Rect position, MouseCursor mouse, int controlID)
        {
            if (Event.current.type == EventType.Repaint)
            {
                Rect r = GUIClip.Unclip(position);
                Rect clip = GUIClip.topmostRect;
                Rect clipped = Rect.MinMaxRect(Mathf.Max(r.x, clip.x), Mathf.Max(r.y, clip.y), Mathf.Min(r.xMax, clip.xMax), Mathf.Min(r.yMax, clip.yMax));

                if (clipped.width <= 0 || clipped.height <= 0)
                    return;
                Internal_AddCursorRect(clipped, mouse, controlID);
            }
        }

        internal static Rect HandleHorizontalSplitter(Rect dragRect, float width, float minLeftSide, float minRightSide)
        {
            // Add a cursor rect indicating we can drag this area
            if (Event.current.type == EventType.Repaint)
                AddCursorRect(dragRect, MouseCursor.SplitResizeLeftRight);

            float newX = 0;

            // Drag splitter
            float deltaX = EditorGUI.MouseDeltaReader(dragRect, true).x;
            if (deltaX != 0f)
            {
                dragRect.x += deltaX;
                newX = Mathf.Clamp(dragRect.x, minLeftSide, width - minRightSide);
            }

            // We might need to move the splitter position if our area/window size
            // has changed
            if (dragRect.x > width - minRightSide)
                newX = width - minRightSide;

            if (newX > 0)
            {
                dragRect.x = newX;
            }

            return dragRect;
        }

        internal static void DrawHorizontalSplitter(Rect dragRect)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Color orgColor = GUI.color;
            Color tintColor = (isProSkin) ? new Color(0.12f, 0.12f, 0.12f, 1.333f) : new Color(0.6f, 0.6f, 0.6f, 1.333f);
            GUI.color = GUI.color * tintColor;
            Rect splitterRect = new Rect(dragRect.x - 1, dragRect.y, 1, dragRect.height);
            GUI.DrawTexture(splitterRect, whiteTexture);
            GUI.color = orgColor;
        }

        internal static EventType magnifyGestureEventType => (EventType)1000;
        internal static EventType swipeGestureEventType => (EventType)1001;
        internal static EventType rotateGestureEventType => (EventType)1002;

        public static void ShowObjectPicker<T>(UnityObject obj, bool allowSceneObjects, string searchFilter, int controlID) where T : UnityObject
        {
            Type objType = typeof(T);
            ObjectSelector.get.Show(obj, objType, null, allowSceneObjects);
            ObjectSelector.get.objectSelectorID = controlID;
            ObjectSelector.get.searchFilter = searchFilter;
        }

        public static UnityObject GetObjectPickerObject()
        {
            return ObjectSelector.GetCurrentObject();
        }

        public static int GetObjectPickerControlID()
        {
            return ObjectSelector.get.objectSelectorID;
        }

        // Enum for tracking what styles the editor uses
        internal enum EditorLook
        {
            // Hasn't been set
            Uninitialized = 0,
            // Looks like regular controls
            LikeControls = 1,
            // Looks like inspector
            LikeInspector = 2
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class BuiltinResource
    {
        public string m_Name;
        public int m_InstanceID;
    }

    internal struct SliderLabels
    {
        public void SetLabels(GUIContent _leftLabel, GUIContent _rightLabel)
        {
            if (Event.current.type == EventType.Repaint)
            {
                leftLabel = _leftLabel;
                rightLabel = _rightLabel;
            }
        }

        public bool HasLabels()
        {
            if (Event.current.type == EventType.Repaint)
            {
                return leftLabel != null && rightLabel != null;
            }
            return false;
        }

        public GUIContent leftLabel;
        public GUIContent rightLabel;
    }

    internal class GUILayoutFadeGroup : GUILayoutGroup
    {
        public float fadeValue;
        public bool wasGUIEnabled;
        public Color guiColor;

        public override void CalcHeight()
        {
            base.CalcHeight();
            minHeight *= fadeValue;
            maxHeight *= fadeValue;
        }
    }
}
