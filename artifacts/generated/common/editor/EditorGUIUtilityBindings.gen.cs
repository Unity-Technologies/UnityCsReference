// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngineInternal;
using UnityEditorInternal;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

namespace UnityEditor
{


[StructLayout(LayoutKind.Sequential)]
internal sealed partial class BuiltinResource
{
    
            public string m_Name;
            public int m_InstanceID;
}

internal struct SliderLabels
    {
        public void SetLabels(GUIContent leftLabel, GUIContent rightLabel)
        {
            if (Event.current.type == EventType.Repaint)
            {
                this.leftLabel = leftLabel;
                this.rightLabel = rightLabel;
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



public sealed partial class EditorGUIUtility : GUIUtility
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string SerializeMainMenuToString () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetMenuLocalizationTestMode (bool onoff) ;

    static internal int s_FontIsBold = -1;
    
    
    private static Texture2D s_InfoIcon;
    private static Texture2D s_WarningIcon;
    private static Texture2D s_ErrorIcon;
    
    
    static EditorGUIUtility()
        {
            GUISkin.m_SkinChanged += SkinChanged;
        }
    
    
    public static float singleLineHeight { get { return EditorGUI.kSingleLineHeight; } }
    public static float standardVerticalSpacing { get { return EditorGUI.kControlVerticalSpacing; } }
    
    
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
                gc = new GUIContent(strings[1]);

                gc.image = LoadIconRequired(icon);

                if (strings[2] != null)
                {
                    gc.tooltip = strings[2];
                }
                s_TextGUIContents[key] = gc;
            }
            return gc;
        }
    
    
    [uei.ExcludeFromDocs]
internal static GUIContent TrTextContent (string text, string tooltip ) {
    Texture icon = null;
    return TrTextContent ( text, tooltip, icon );
}

[uei.ExcludeFromDocs]
internal static GUIContent TrTextContent (string text) {
    Texture icon = null;
    string tooltip = null;
    return TrTextContent ( text, tooltip, icon );
}

internal static GUIContent TrTextContent(string text, [uei.DefaultValue("null")]  string tooltip , [uei.DefaultValue("null")]  Texture icon )
        {
            string text_k = text != null ? text : "";
            string tooltip_k = tooltip != null ? tooltip : "";
            string key = string.Format("{0}|{1}", text_k, tooltip_k);

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

    
    
    internal static GUIContent TrTextContent(string text, string tooltip, string iconName)
        {
            string text_k = text != null ? text : "";
            string tooltip_k = tooltip != null ? tooltip : "";
            string key = string.Format("{0}|{1}", text_k, tooltip_k);

            GUIContent gc = (GUIContent)s_GUIContents[key];
            if (gc == null)
            {
                gc = new GUIContent(L10n.Tr(text));
                if (tooltip != null)
                {
                    gc.tooltip = L10n.Tr(tooltip);
                }
                if (iconName != null)
                {
                    Texture icon = LoadIconRequired(iconName);
                    gc.image = icon;
                }
                s_GUIContents[key] = gc;
            }
            return gc;
        }
    
    
    internal static GUIContent TrTextContent(string text, Texture icon)
        {
            return TrTextContent(text, null, icon);
        }
    
    
    internal static GUIContent TrTextContentWithIcon(string text, Texture icon)
        {
            return TrTextContent(text, null, icon);
        }
    
    
    internal static GUIContent TrTextContentWithIcon(string text, string iconName)
        {
            return TrTextContent(text, null, iconName);
        }
    
    
    internal static GUIContent TrTextContentWithIcon(string text, string tooltip, string iconName)
        {
            return TrTextContent(text, tooltip, iconName);
        }
    
    
    internal static GUIContent TrTextContentWithIcon(string text, string tooltip, Texture icon)
        {
            return TrTextContent(text, tooltip, icon);
        }
    
    
    [uei.ExcludeFromDocs]
internal static GUIContent TrIconContent (string name) {
    string tooltip = null;
    return TrIconContent ( name, tooltip );
}

internal static GUIContent TrIconContent(string name, [uei.DefaultValue("null")]  string tooltip )
        {
            GUIContent gc = (GUIContent)s_IconGUIContents[name];
            if (gc != null)
            {
                return gc;
            }
            gc = new GUIContent();

            if (tooltip != null)
            {
                gc.tooltip = L10n.Tr(tooltip);
            }
            gc.image = LoadIconRequired(name);
            s_IconGUIContents[name] = gc;
            return gc;
        }

    
    
    [uei.ExcludeFromDocs]
internal static GUIContent TrIconContent (Texture icon) {
    string tooltip = null;
    return TrIconContent ( icon, tooltip );
}

internal static GUIContent TrIconContent(Texture icon, [uei.DefaultValue("null")]  string tooltip )
        {
            GUIContent gc = (GUIContent)s_IconGUIContents[tooltip];
            if (gc != null)
            {
                return gc;
            }
            gc = new GUIContent();

            if (tooltip != null)
            {
                gc.tooltip = L10n.Tr(tooltip);
            }
            gc.image = icon;
            s_IconGUIContents[tooltip] = gc;
            return gc;
        }

    
    
    internal static Color kDarkViewBackground = new Color(0.22f, 0.22f, 0.22f, 0);
    
    
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
                Debug.LogErrorFormat("Unable to load the icon: '{0}'.\nNote that either full project path should be used (with extension) or just the icon name if the icon is located in the following location: '{1}' (without extension, since png is assumed)", name, "Assets/Editor Default Resources/" + EditorResourcesUtility.iconsPath);

            return tex;
        }
    
    
    internal static Texture2D LoadIcon(string name)
        {
            return LoadIconForSkin(name, skinIndex);
        }
    
    
    static Texture2D LoadGeneratedIconOrNormalIcon(string name)
        {
            Texture2D tex = Load(EditorResourcesUtility.generatedIconsPath + name + ".asset") as Texture2D;
            if (!tex)
                tex = Load(EditorResourcesUtility.iconsPath + name + ".png") as Texture2D;
            if (!tex)
                tex = Load(name) as Texture2D; 
            return tex;
        }
    
    
    internal static Texture2D LoadIconForSkin(string name, int skinIndex)
        {
            if (String.IsNullOrEmpty(name))
                return null;

            if (skinIndex == 0)
            {
                return LoadGeneratedIconOrNormalIcon(name);
            }

            var newName = "d_" + Path.GetFileName(name);
            var dirName = Path.GetDirectoryName(name);
            if (!String.IsNullOrEmpty(dirName))
                newName = String.Format("{0}/{1}", dirName, newName);

            Texture2D tex = LoadGeneratedIconOrNormalIcon(newName);
            if (!tex)
                tex = LoadGeneratedIconOrNormalIcon(name);
            return tex;
        }
    
    
    [uei.ExcludeFromDocs]
public static GUIContent IconContent (string name) {
    string text = null;
    return IconContent ( name, text );
}

public static GUIContent IconContent(string name, [uei.DefaultValue("null")]  string text )
        {
            GUIContent gc = (GUIContent)s_IconGUIContents[name];
            if (gc != null)
            {
                return gc;
            }
            gc = new GUIContent();

            if (text != null)
            {
                string[] strings  = GetNameAndTooltipString(text);
                if (strings[2] != null)
                {
                    gc.tooltip = strings[2];
                }
            }
            gc.image = LoadIconRequired(name);
            s_IconGUIContents[name] = gc;
            return gc;
        }

    
    
    public static bool isProSkin { get { return skinIndex == 1; } }
    
    
    internal extern static int skinIndex
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    internal static void Internal_SwitchSkin()
        {
            skinIndex = 1 - skinIndex;
        }
    
    
    static GUIContent s_ObjectContent = new GUIContent();
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  string GetObjectNameWithInfo (Object obj) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  string GetTypeNameWithInfo (string typeName) ;

    public static GUIContent ObjectContent(Object obj, System.Type type)
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
    
    
    
            static GUIContent s_Text = new GUIContent(), s_Image = new GUIContent(), s_TextImage = new GUIContent();
    internal static GUIContent TempContent(string t)
        {
            s_Text.text = t;
            return s_Text;
        }
    
    internal static GUIContent TempContent(Texture i)
        {
            s_Image.image = i;
            return s_Image;
        }
    
    internal static GUIContent TempContent(string t, Texture i)
        {
            s_TextImage.image = i;
            s_TextImage.text = t;
            return s_TextImage;
        }
    
    internal static GUIContent[] TempContent(string[] texts)
        {
            GUIContent[] retval = new GUIContent[texts.Length];
            for (int i = 0; i < texts.Length; i++)
                retval[i] = new GUIContent(texts[i]);
            return retval;
        }
    
    internal static GUIContent TrTempContent(string t)
        {
            return TempContent(L10n.Tr(t));
        }
    
    internal static bool HasHolddownKeyModifiers(Event evt)
        {
            return evt.shift | evt.control | evt.alt | evt.command;
        }
    
    
    public static bool HasObjectThumbnail(Type objType)
        {
            return objType != null && (objType.IsSubclassOf(typeof(Texture)) || objType == typeof(Texture) || objType == typeof(Sprite));
        }
    
    
    public static void SetIconSize (Vector2 size) {
        INTERNAL_CALL_SetIconSize ( ref size );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetIconSize (ref Vector2 size);
    public static Vector2 GetIconSize()
        {
            Vector2 size;
            Internal_GetIconSize(out size);
            return size;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_GetIconSize (out Vector2 size) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  Object GetScript (string scriptClass) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetIconForObject (Object obj, Texture2D icon) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  Texture2D GetIconForObject (Object obj) ;

    public extern static Texture2D whiteTexture
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    internal static Texture2D infoIcon
        {
            get
            {
                if (s_InfoIcon == null)
                    s_InfoIcon = EditorGUIUtility.LoadIcon("console.infoicon");
                return s_InfoIcon;
            }
        }
    
    
    
    internal static Texture2D warningIcon
        {
            get
            {
                if (s_WarningIcon == null)
                    s_WarningIcon = EditorGUIUtility.LoadIcon("console.warnicon");
                return s_WarningIcon;
            }
        }
    
    
    internal static Texture2D errorIcon
        {
            get
            {
                if (s_ErrorIcon == null)
                    s_ErrorIcon = EditorGUIUtility.LoadIcon("console.erroricon");
                return s_ErrorIcon;
            }
        }
    
    
    internal static Texture2D GetHelpIcon(MessageType type)
        {
            switch (type)
            {
                case MessageType.Info:
                    return EditorGUIUtility.infoIcon;
                case MessageType.Warning:
                    return EditorGUIUtility.warningIcon;
                case MessageType.Error:
                    return EditorGUIUtility.errorIcon;
            }
            return null;
        }
    
    
    static GUIContent s_BlankContent = new GUIContent(" ");
    internal static GUIContent blankContent { get { return s_BlankContent; } }
    
    
    static GUIStyle s_WhiteTextureStyle;
    internal static GUIStyle whiteTextureStyle
        {
            get
            {
                if (s_WhiteTextureStyle == null)
                {
                    s_WhiteTextureStyle = new GUIStyle();
                    s_WhiteTextureStyle.normal.background = whiteTexture;
                }
                return s_WhiteTextureStyle;
            }
        }
    
    
    static GUIStyle s_BasicTextureStyle;
    internal static GUIStyle GetBasicTextureStyle(Texture2D tex)
        {
            if (s_BasicTextureStyle == null)
                s_BasicTextureStyle = new GUIStyle();

            s_BasicTextureStyle.normal.background = tex;

            return s_BasicTextureStyle;
        }
    
    
    
            static Hashtable s_TextGUIContents = new Hashtable();
            static Hashtable s_GUIContents = new Hashtable();
            static Hashtable s_IconGUIContents = new Hashtable();
    
    
    internal static void NotifyLanguageChanged(SystemLanguage newLanguage)
        {
            s_TextGUIContents = new Hashtable();
            s_GUIContents = new Hashtable();
            s_IconGUIContents = new Hashtable();
            EditorUtility.Internal_UpdateMenuTitleForLanguage(newLanguage);
            LocalizationDatabase.SetCurrentEditorLanguage(newLanguage);
            EditorApplication.RequestRepaintAllViews();
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Texture2D FindTexture (string name) ;

    public static GUISkin GetBuiltinSkin(EditorSkin skin)
        {
            return GUIUtility.GetBuiltinSkin((int)skin);
        }
    
    
    public static Object LoadRequired(string path)
        {
            Object o = Load(path, typeof(Object));
            if (!o)
                Debug.LogError("Unable to find required resource at 'Editor Default Resources/" + path + "'");
            return o;
        }
    
    
    public static Object Load(string path)
        {
            return Load(path, typeof(Object));
        }
    
    
    [TypeInferenceRule(TypeInferenceRules.TypeReferencedBySecondArgument)]
    private static Object Load(string filename, Type type)
        {
            Object asset = AssetDatabase.LoadAssetAtPath("Assets/Editor Default Resources/" + filename, type);
            if (asset != null)
                return asset;

            AssetBundle bundle = GetEditorAssetBundle();
            if (bundle == null)
            {
                if (Application.isBatchmode)
                    return null;
                throw new NullReferenceException("Failure to load editor resource asset bundle.");
            }

            asset = bundle.LoadAsset(filename, type);
            if (asset != null)
                return asset;

            return AssetDatabase.LoadAssetAtPath(filename, type);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  Object GetBuiltinExtraResource (Type type, string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  BuiltinResource[] GetBuiltinResourceList (int classID) ;

    public static void PingObject(Object obj)
        {
            if (obj != null)
                PingObject(obj.GetInstanceID());
        }
    
    
    public static void PingObject(int targetInstanceID)
        {
            foreach (SceneHierarchyWindow shw in SceneHierarchyWindow.GetAllSceneHierarchyWindows())
            {
                bool ping = true;
                shw.FrameObject(targetInstanceID, ping);
            }

            foreach (ProjectBrowser pb in ProjectBrowser.GetAllProjectBrowsers())
            {
                bool ping = true;
                pb.FrameObject(targetInstanceID, ping);
            }

        }
    
    
    internal static void MoveFocusAndScroll(bool forward)
        {
            int prev = GUIUtility.keyboardControl;
            Internal_MoveKeyboardFocus(forward);
            if (prev != GUIUtility.keyboardControl)
                RefreshScrollPosition();
        }
    
    
    internal static void RefreshScrollPosition()
        {
            Rect r;

            if (Internal_GetKeyboardRect(GUIUtility.keyboardControl, out r))
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
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool Internal_GetKeyboardRect (int id, out Rect rect) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_MoveKeyboardFocus (bool forward) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int Internal_GetNextKeyboardControlID (bool forward) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  AssetBundle GetEditorAssetBundle () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetRenderTextureNoViewport (RenderTexture rt) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetVisibleLayers (int layers) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetLockedLayers (int layers) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool IsGizmosAllowedForObject (Object obj) ;

    internal static void ResetGUIState()
        {
            GUI.skin = null;
            GUI.backgroundColor = GUI.contentColor = Color.white;
            GUI.color = EditorApplication.isPlayingOrWillChangePlaymode ? HostView.kPlayModeDarken : Color.white;
            GUI.enabled = true;
            GUI.changed = false;
            EditorGUI.indentLevel = 0;
            EditorGUI.ClearStacks();
            EditorGUIUtility.fieldWidth = 0;
            EditorGUIUtility.labelWidth = 0;

            EditorGUIUtility.SetBoldDefaultFont(false);
            EditorGUIUtility.UnlockContextWidth();
            EditorGUIUtility.hierarchyMode = false;
            EditorGUIUtility.wideMode = false;

            ScriptAttributeUtility.propertyHandlerCache = null;
        }
    
    
    internal static void RenderGameViewCamerasInternal (RenderTexture target, int targetDisplay, Rect screenRect, Vector2 mousePosition, bool gizmos, bool sendInput) {
        INTERNAL_CALL_RenderGameViewCamerasInternal ( target, targetDisplay, ref screenRect, ref mousePosition, gizmos, sendInput );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_RenderGameViewCamerasInternal (RenderTexture target, int targetDisplay, ref Rect screenRect, ref Vector2 mousePosition, bool gizmos, bool sendInput);
    [System.Obsolete ("RenderGameViewCameras is no longer supported. Consider rendering cameras manually.", true)]
    public static void RenderGameViewCameras (RenderTexture target, int targetDisplay, Rect screenRect, Vector2 mousePosition, bool gizmos) {
        INTERNAL_CALL_RenderGameViewCameras ( target, targetDisplay, ref screenRect, ref mousePosition, gizmos );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_RenderGameViewCameras (RenderTexture target, int targetDisplay, ref Rect screenRect, ref Vector2 mousePosition, bool gizmos);
    [System.Obsolete ("RenderGameViewCameras is no longer supported. Consider rendering cameras manually.", true)]
public static void RenderGameViewCameras(Rect cameraRect, bool gizmos, bool gui) {}
    
    
    [System.Obsolete ("RenderGameViewCameras is no longer supported. Consider rendering cameras manually.", true)]
public static void RenderGameViewCameras(Rect cameraRect, Rect statsRect, bool gizmos, bool gui) {}
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsDisplayReferencedByCameras (int displayIndex) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void QueueGameViewInputEvent (Event evt) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetDefaultFont (Font font) ;

    
    static GUIStyle GetStyle(string styleName)
        {
            GUIStyle s = GUI.skin.FindStyle(styleName);
            if (s == null)
                s = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle(styleName);
            if (s == null)
            {
                Debug.Log("Missing built-in guistyle " + styleName);
                s = GUISkin.error;
            }
            return s;
        }
    
            internal static int s_LastControlID = 0;
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
    
    
    private static bool s_HierarchyMode = false;
    public static bool hierarchyMode
        {
            get { return s_HierarchyMode; }
            set { s_HierarchyMode = value; }
        }
    
    
    internal static bool s_WideMode = false;
    public static bool wideMode
        {
            get { return s_WideMode; }
            set { s_WideMode = value; }
        }
    
    
    private static float s_ContextWidth = 0f;
    private static float CalcContextWidth()
        {
            float output = GUIClip.GetTopRect().width;
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
    
    
    public static float currentViewWidth { get { return GUIView.current.position.width; } }
    
    
    private static float s_LabelWidth = 0f;
    public static float labelWidth
        {
            get
            {
                if (s_LabelWidth > 0)
                    return s_LabelWidth;

                if (s_HierarchyMode)
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
    
    
    
    [System.Obsolete ("LookLikeControls and LookLikeInspector modes are deprecated. Use EditorGUIUtility.labelWidth and EditorGUIUtility.fieldWidth to control label and field widths.")]
[uei.ExcludeFromDocs]
public static void LookLikeControls (float labelWidth ) {
    float fieldWidth = 0;
    LookLikeControls ( labelWidth, fieldWidth );
}

[System.Obsolete ("LookLikeControls and LookLikeInspector modes are deprecated. Use EditorGUIUtility.labelWidth and EditorGUIUtility.fieldWidth to control label and field widths.")]
[uei.ExcludeFromDocs]
public static void LookLikeControls () {
    float fieldWidth = 0;
    float labelWidth = 0;
    LookLikeControls ( labelWidth, fieldWidth );
}

[System.Obsolete ("LookLikeControls and LookLikeInspector modes are deprecated. Use EditorGUIUtility.labelWidth and EditorGUIUtility.fieldWidth to control label and field widths.")]
public static void LookLikeControls( [uei.DefaultValue("0")] float labelWidth , [uei.DefaultValue("0")]  float fieldWidth )
        {
            EditorGUIUtility.fieldWidth = fieldWidth;
            EditorGUIUtility.labelWidth = labelWidth;
        }

    
    
    [System.Obsolete ("LookLikeControls and LookLikeInspector modes are deprecated.")]
public static void LookLikeInspector()
        {
            EditorGUIUtility.fieldWidth = 0;
            EditorGUIUtility.labelWidth = 0;
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
    
    
    static internal void SetBoldDefaultFont(bool isBold)
        {
            int wantsBold = isBold ? 1 : 0;
            if (wantsBold != s_FontIsBold)
            {
                EditorGUIUtility.SetDefaultFont(isBold ? EditorStyles.boldFont : EditorStyles.standardFont);
                s_FontIsBold = wantsBold;
            }
        }
    
    
    static internal bool GetBoldDefaultFont() { return s_FontIsBold == 1 ? true : false; }
    
    public static Event CommandEvent(string commandName)
        {
            Event e = new Event();
            Internal_SetupEventValues(e);
            e.type = EventType.ExecuteCommand;
            e.commandName = commandName;
            return e;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SetupEventValues (object evt) ;

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
            GUI.backgroundColor = Color.white;

            GUIStyle gs = whiteTextureStyle;
            gs.Draw(position, false, false, false, false);

            float maxColorComponent = GUI.color.maxColorComponent;

            if (hdr && maxColorComponent > 1.0f)
            {
                float gradientWidth = position.width / 3f;
                Rect leftRect = new Rect(position.x, position.y, gradientWidth, position.height);
                Rect rightRect = new Rect(position.xMax - gradientWidth, position.y, gradientWidth, position.height);

                Color normalizedColor = GUI.color.RGBMultiplied(1f / maxColorComponent);

                Color orgColor = GUI.color;
                GUI.color = normalizedColor;
                GUIStyle  basicStyle = EditorGUIUtility.GetBasicTextureStyle(EditorGUIUtility.whiteTexture);
                basicStyle.Draw(leftRect, false, false, false, false);
                basicStyle.Draw(rightRect, false, false, false, false);
                GUI.color = orgColor;

                basicStyle = EditorGUIUtility.GetBasicTextureStyle(ColorPicker.GetGradientTextureWithAlpha0To1());
                basicStyle.Draw(leftRect, false, false, false, false);
                basicStyle = EditorGUIUtility.GetBasicTextureStyle(ColorPicker.GetGradientTextureWithAlpha1To0());
                basicStyle.Draw(rightRect, false, false, false, false);
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

            if (hdr && maxColorComponent > 1.0f)
            {
                GUI.Label(new Rect(position.x, position.y, position.width - 3, position.height), "HDR", EditorStyles.centeredGreyMiniLabel);
            }
        }
    
    
    public static void DrawCurveSwatch(Rect position, AnimationCurve curve, SerializedProperty property, Color color, Color bgColor)
        {
            DrawCurveSwatchInternal(position, curve, null, property, null, color, bgColor, false, new Rect(), Color.clear, Color.clear);
        }
    
    public static void DrawCurveSwatch(Rect position, AnimationCurve curve, SerializedProperty property, Color color, Color bgColor, Color topFillColor, Color bottomFillColor)
        {
            DrawCurveSwatchInternal(position, curve, null, property, null, color, bgColor, false, new Rect(), topFillColor, bottomFillColor);
        }
    
    public static void DrawCurveSwatch(Rect position, AnimationCurve curve, SerializedProperty property, Color color, Color bgColor, Color topFillColor, Color bottomFillColor, Rect curveRanges)
        {
            DrawCurveSwatchInternal(position, curve, null, property, null, color, bgColor, true, curveRanges, topFillColor, bottomFillColor);
        }
    
    public static void DrawCurveSwatch(Rect position, AnimationCurve curve, SerializedProperty property, Color color, Color bgColor, Rect curveRanges)
        {
            DrawCurveSwatchInternal(position, curve, null, property, null, color, bgColor, true, curveRanges, Color.clear, Color.clear);
        }
    
    public static void DrawRegionSwatch(Rect position, SerializedProperty property, SerializedProperty property2, Color color, Color bgColor, Rect curveRanges)
        {
            DrawCurveSwatchInternal(position, null, null, property, property2, color, bgColor, true, curveRanges, Color.clear, Color.clear);
        }
    
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

            Color oldColor = GUI.color;
            GUI.color = bgColor;
            GUIStyle gs = whiteTextureStyle;
            gs.Draw(position, false, false, false, false);
            GUI.color = oldColor;

            if (property != null && property.hasMultipleDifferentValues)
            {
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

                if (!stretchX)
                    position.width = preview.width;
                if (!stretchY)
                    position.height = preview.height;

                gs.Draw(position, false, false, false, false);
            }
        }
    
    
    [System.Obsolete("EditorGUIUtility.RGBToHSV is obsolete. Use Color.RGBToHSV instead (UnityUpgradable) -> [UnityEngine] UnityEngine.Color.RGBToHSV(*)", true)]
    public static void RGBToHSV(Color rgbColor, out float H, out float S, out float V)
        {
            Color.RGBToHSV(rgbColor, out H, out S, out V);
        }
    
    
    [System.Obsolete("EditorGUIUtility.HSVToRGB is obsolete. Use Color.HSVToRGB instead (UnityUpgradable) -> [UnityEngine] UnityEngine.Color.HSVToRGB(*)", true)]
    public static Color HSVToRGB(float H, float S, float V)
        {
            return Color.HSVToRGB(H, S, V);
        }
    
    
    [System.Obsolete("EditorGUIUtility.HSVToRGB is obsolete. Use Color.HSVToRGB instead (UnityUpgradable) -> [UnityEngine] UnityEngine.Color.HSVToRGB(*)", true)]
    public static Color HSVToRGB(float H, float S, float V, bool hdr)
        {
            return Color.HSVToRGB(H, S, V, hdr);
        }
    
    
    public extern new static string systemCopyBuffer
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    internal static void SetPasteboardColor (Color color) {
        INTERNAL_CALL_SetPasteboardColor ( ref color );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetPasteboardColor (ref Color color);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool HasPasteboardColor () ;

    internal static Color GetPasteboardColor () {
        Color result;
        INTERNAL_CALL_GetPasteboardColor ( out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetPasteboardColor (out Color value);
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
    
    
    private static void Internal_AddCursorRect (Rect r, MouseCursor m, int controlID) {
        INTERNAL_CALL_Internal_AddCursorRect ( ref r, m, controlID );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_AddCursorRect (ref Rect r, MouseCursor m, int controlID);
    internal static Rect HandleHorizontalSplitter(Rect dragRect, float width, float minLeftSide, float minRightSide)
        {
            if (Event.current.type == EventType.Repaint)
                EditorGUIUtility.AddCursorRect(dragRect, MouseCursor.SplitResizeLeftRight);

            float newX = 0;

            float deltaX = EditorGUI.MouseDeltaReader(dragRect, true).x;
            if (deltaX != 0f)
            {
                dragRect.x += deltaX;
                newX = Mathf.Clamp(dragRect.x, minLeftSide, width - minRightSide);
            }

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
            Color tintColor = (EditorGUIUtility.isProSkin) ? new Color(0.12f, 0.12f, 0.12f, 1.333f) : new Color(0.6f, 0.6f, 0.6f, 1.333f);
            GUI.color = GUI.color * tintColor;
            Rect splitterRect = new Rect(dragRect.x - 1, dragRect.y, 1, dragRect.height);
            GUI.DrawTexture(splitterRect, EditorGUIUtility.whiteTexture);
            GUI.color = orgColor;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void CleanCache (string text) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetSearchIndexOfControlIDList (int index) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  int GetSearchIndexOfControlIDList () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool CanHaveKeyboardFocus (int id) ;

    
            internal static EventType magnifyGestureEventType { get {return (EventType)1000; } }
            internal static EventType swipeGestureEventType { get {return (EventType)1001; } }
            internal static EventType rotateGestureEventType { get {return (EventType)1002; } }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetWantsMouseJumping (int wantz) ;

    public static void ShowObjectPicker<T>(Object obj, bool allowSceneObjects, string searchFilter, int controlID) where T : Object
        {
            System.Type objType = typeof(T);
            ObjectSelector.get.Show(obj, objType, null, allowSceneObjects);
            ObjectSelector.get.objectSelectorID = controlID;
            ObjectSelector.get.searchFilter = searchFilter;
        }
    
    
    public static Object GetObjectPickerObject()
        {
            return ObjectSelector.GetCurrentObject();
        }
    
    
    public static int GetObjectPickerControlID()
        {
            return ObjectSelector.get.objectSelectorID;
        }
    
    
}

public enum MessageType
{
    
    None = 0,
    
    Info = 1,
    
    Warning = 2,
    
    Error = 3,
}

public enum EditorSkin
{
    
    Game = 0,
    
    Inspector = 1,
    
    Scene = 2,
}

public enum MouseCursor
{
    
    Arrow = 0,
    
    Text = 1,
    
    ResizeVertical = 2,
    
    ResizeHorizontal = 3,
    
    Link = 4,
    
    SlideArrow = 5,
    
    ResizeUpRight = 6,
    
    ResizeUpLeft = 7,
    
    MoveArrow = 8,
    
    RotateArrow = 9,
    
    ScaleArrow = 10,
    
    ArrowPlus = 11,
    
    ArrowMinus = 12,
    
    Pan = 13,
    
    Orbit = 14,
    
    Zoom = 15,
    
    FPS = 16,
    
    CustomCursor = 17,
    
    SplitResizeUpDown = 18,
    
    SplitResizeLeftRight = 19
}

internal enum EditorLook
    {
        Uninitialized = 0,
        LikeControls = 1,
        LikeInspector = 2
    }


internal sealed partial class GUILayoutFadeGroup : GUILayoutGroup
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

internal sealed partial class OptimizedGUIBlock
{
    [System.NonSerialized]
            private IntPtr m_Ptr;
    
            bool m_Valid = false;
    
            bool m_Recording = false;
    
            bool m_WatchForUsed = false;
    
            int m_KeyboardControl;
    
            int m_LastSearchIndex;
    
            int m_ActiveDragControl;
    
            Color m_GUIColor;
    
            Rect m_Rect;
    
            public OptimizedGUIBlock()
        {
            Init();
        }
    
    
    ~OptimizedGUIBlock()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Debug.Log("Failed cleaning up Optimized GUI Block");
            }
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void Init () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Dispose () ;

    public bool Begin(bool hasChanged, Rect position)
        {
            if (hasChanged)
                m_Valid = false;

            if (Event.current.type == EventType.Repaint)
            {
                if (GUIUtility.keyboardControl != m_KeyboardControl)
                {
                    m_Valid = false;
                    m_KeyboardControl = GUIUtility.keyboardControl;
                }

                if (DragAndDrop.activeControlID != m_ActiveDragControl)
                {
                    m_Valid = false;
                    m_ActiveDragControl = DragAndDrop.activeControlID;
                }

                if (GUI.color != m_GUIColor)
                {
                    m_Valid = false;
                    m_GUIColor = GUI.color;
                }

                position = GUIClip.Unclip(position);
                if (m_Valid && position != m_Rect)
                {
                    m_Rect = position;
                    m_Valid = false;
                }

                if (EditorGUI.isCollectingTooltips)
                    return true;

                if (m_Valid)
                    return false;
                else
                {
                    m_Recording = true;
                    BeginRecording();
                    return true;
                }
            }
            if (Event.current.type == EventType.Used)
                return false;

            if (Event.current.type != EventType.Used)
                m_WatchForUsed = true;

            return true;
        }
    
    
    public void End()
        {
            bool wasRecording = m_Recording;
            if (m_Recording)
            {
                EndRecording();
                m_Recording = false;
                m_Valid = true;
                m_LastSearchIndex = EditorGUIUtility.GetSearchIndexOfControlIDList();
            }

            if (Event.current == null)
                Debug.LogError("Event.current is null");

            if (Event.current.type == EventType.Repaint && !EditorGUI.isCollectingTooltips)
            {
                Execute();
                if (!wasRecording)
                    EditorGUIUtility.SetSearchIndexOfControlIDList(m_LastSearchIndex);
            }

            if (m_WatchForUsed && Event.current.type == EventType.Used)
            {
                m_Valid = false;
            }
            m_WatchForUsed = false;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void BeginRecording () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void EndRecording () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public void Execute () ;

    public bool valid { get { return m_Valid; } set { m_Valid = value; } }
}

public static partial class SessionState
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetBool (string key, bool value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool GetBool (string key, bool defaultValue) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void EraseBool (string key) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetFloat (string key, float value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  float GetFloat (string key, float defaultValue) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void EraseFloat (string key) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetInt (string key, int value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetInt (string key, int defaultValue) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void EraseInt (string key) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetString (string key, string value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetString (string key, string defaultValue) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void EraseString (string key) ;

    public static void SetVector3 (string key, Vector3 value) {
        INTERNAL_CALL_SetVector3 ( key, ref value );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetVector3 (string key, ref Vector3 value);
    public static Vector3 GetVector3 (string key, Vector3 defaultValue) {
        Vector3 result;
        INTERNAL_CALL_GetVector3 ( key, ref defaultValue, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetVector3 (string key, ref Vector3 defaultValue, out Vector3 value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void EraseVector3 (string key) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetIntArray (string key, int[] value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int[] GetIntArray (string key, int[] defaultValue) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void EraseIntArray (string key) ;

}


}    
