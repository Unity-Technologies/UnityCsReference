// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine.Scripting;

namespace UnityEditor
{
[Flags]
public enum EditorSelectedRenderState
{
    Hidden = 0,
    Wireframe = 1,
    Highlight = 2,
}

public sealed partial class EditorUtility
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void RevealInFinder (string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetDirty (Object target) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void LoadPlatformSupportModuleNativeDllInternal (string target) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void LoadPlatformSupportNativeLibrary (string nativeLibrary) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  int GetDirtyIndex (int instanceID) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool IsDirty (int instanceID) ;

    public static bool LoadWindowLayout(string path)
        {
            bool newProjectLayoutWasCreated = false;
            return UnityEditor.WindowLayout.LoadWindowLayout(path, newProjectLayoutWasCreated);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsPersistent (Object target) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool DisplayDialog (string title, string message, string ok, [uei.DefaultValue("\"\"")]  string cancel ) ;

    [uei.ExcludeFromDocs]
    public static bool DisplayDialog (string title, string message, string ok) {
        string cancel = "";
        return DisplayDialog ( title, message, ok, cancel );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int DisplayDialogComplex (string title, string message, string ok, string cancel, string alt) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string OpenFilePanel (string title, string directory, string extension) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string OpenFilePanelWithFilters (string title, string directory, string[] filters) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string SaveFilePanel (string title, string directory, string defaultName, string extension) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string SaveBuildPanel (BuildTarget target, string title, string directory, string defaultName, string extension, out bool updateExistingBuild) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int NaturalCompare (string a, string b) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  int NaturalCompareObjectNames (Object a, Object b) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string OpenFolderPanel (string title, string folder, string defaultName) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string SaveFolderPanel (string title, string folder, string defaultName) ;

    public static string SaveFilePanelInProject(string title, string defaultName, string extension, string message)
        {
            return Internal_SaveFilePanelInProject(title, defaultName, extension, message, "Assets");
        }
    
    
    public static string SaveFilePanelInProject(string title, string defaultName, string extension, string message, string path)
        {
            return Internal_SaveFilePanelInProject(title, defaultName, extension, message, path);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  string Internal_SaveFilePanelInProject (string title, string defaultName, string extension, string message, string path) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool WarnPrefab (Object target, string title, string warning, string okButton) ;

    [System.Obsolete ("use AssetDatabase.LoadAssetAtPath")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Object FindAsset (string path, Type type) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Object InstanceIDToObject (int instanceID) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void CompressTexture (Texture2D texture, TextureFormat format, int quality) ;

    public static void CompressTexture(Texture2D texture, TextureFormat format, TextureCompressionQuality quality)
        {
            if (texture == null)
            {
                throw new ArgumentNullException("texture can not be null");
            }

            CompressTexture(texture, format, (int)quality);
        }
    
    
    static void CompressTexture(Texture2D texture, TextureFormat format)
        {
            if (texture == null)
            {
                throw new ArgumentNullException("texture can not be null");
            }

            CompressTexture(texture, format, TextureCompressionQuality.Normal);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void CompressCubemapTexture (Cubemap texture, TextureFormat format, int quality) ;

    public static void CompressCubemapTexture(Cubemap texture, TextureFormat format, TextureCompressionQuality quality)
        {
            if (texture == null)
            {
                throw new ArgumentNullException("texture can not be null");
            }

            CompressCubemapTexture(texture, format, (int)quality);
        }
    
    
    static void CompressCubemapTexture(Cubemap texture, TextureFormat format)
        {
            if (texture == null)
            {
                throw new ArgumentNullException("texture can not be null");
            }

            CompressCubemapTexture(texture, format, TextureCompressionQuality.Normal);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string InvokeDiffTool (string leftTitle, string leftFile, string rightTitle, string rightFile, string ancestorTitle, string ancestorFile) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void CopySerialized (Object source, Object dest) ;

    public static void CopySerializedIfDifferent(Object source, Object dest)
        {
            if (source == null)
                throw new ArgumentNullException("Argument 'source' is null");
            if (dest == null)
                throw new ArgumentNullException("Argument 'dest' is null");

            InternalCopySerializedIfDifferent(source, dest);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void InternalCopySerializedIfDifferent (Object source, Object dest) ;

    [System.Obsolete ("Use AssetDatabase.GetAssetPath")]
public static string GetAssetPath(Object asset)
        {
            return AssetDatabase.GetAssetPath(asset);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Object[] CollectDependencies (Object[] roots) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Object[] CollectDeepHierarchy (Object[] roots) ;

    internal static void InitInstantiatedPreviewRecursive(GameObject go)
        {
            go.hideFlags = HideFlags.HideAndDontSave;
            go.layer = Camera.PreviewCullingLayer;
            foreach (Transform c in go.transform)
                InitInstantiatedPreviewRecursive(c.gameObject);
        }
    
    
    internal static GameObject InstantiateForAnimatorPreview(Object original)
        {
            if (original == null)
                throw new ArgumentException("The prefab you want to instantiate is null.");

            GameObject go = InstantiateRemoveAllNonAnimationComponents(original, Vector3.zero, Quaternion.identity) as GameObject;
            go.name = go.name + "AnimatorPreview";       
            go.tag = "Untagged";
            InitInstantiatedPreviewRecursive(go);

            Animator[] animators = go.GetComponentsInChildren<Animator>();
            for (int i = 0; i < animators.Length; i++)
            {
                Animator animator = animators[i];

                animator.enabled = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.logWarnings = false;
                animator.fireEvents = false;
            }

            if (animators.Length == 0)
            {
                Animator animator = go.AddComponent<Animator>();
                animator.enabled = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.logWarnings = false;
                animator.fireEvents = false;
            }


            return go;
        }
    
    
    internal static Object InstantiateRemoveAllNonAnimationComponents(Object original, Vector3 position, Quaternion rotation)
        {
            if (original == null)
                throw new ArgumentException("The prefab you want to instantiate is null.");

            return Internal_InstantiateRemoveAllNonAnimationComponentsSingle(original, position, rotation);
        }
    
    
    private static Object Internal_InstantiateRemoveAllNonAnimationComponentsSingle (Object data, Vector3 pos, Quaternion rot) {
        return INTERNAL_CALL_Internal_InstantiateRemoveAllNonAnimationComponentsSingle ( data, ref pos, ref rot );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Object INTERNAL_CALL_Internal_InstantiateRemoveAllNonAnimationComponentsSingle (Object data, ref Vector3 pos, ref Quaternion rot);
    [System.Obsolete ("Use EditorUtility.UnloadUnusedAssetsImmediate instead")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void UnloadUnusedAssets () ;

    [System.Obsolete ("Use EditorUtility.UnloadUnusedAssetsImmediate instead")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void UnloadUnusedAssetsIgnoreManagedReferences () ;

    public static void UnloadUnusedAssetsImmediate()
        {
            UnloadUnusedAssetsImmediateInternal(true);
        }
    
    
    public static void UnloadUnusedAssetsImmediate(bool includeMonoReferencesAsRoots)
        {
            UnloadUnusedAssetsImmediateInternal(includeMonoReferencesAsRoots);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void UnloadUnusedAssetsImmediateInternal (bool includeMonoReferencesAsRoots) ;

    [System.Obsolete ("Use BuildPipeline.BuildAssetBundle instead", true)]
public static bool BuildResourceFile(Object[] selection, string pathName)
        {
            return false;
        }
    
    
    internal static void Internal_DisplayPopupMenu(Rect position, string menuItemPath, Object context, int contextUserData)
        {
            Private_DisplayPopupMenu(position, menuItemPath, context, contextUserData);
        }
    
    
    private static void Private_DisplayPopupMenu (Rect position, string menuItemPath, Object context, int contextUserData) {
        INTERNAL_CALL_Private_DisplayPopupMenu ( ref position, menuItemPath, context, contextUserData );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Private_DisplayPopupMenu (ref Rect position, string menuItemPath, Object context, int contextUserData);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void Internal_UpdateMenuTitleForLanguage (SystemLanguage newloc) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void Internal_UpdateAllMenus () ;

    static public void DisplayPopupMenu(Rect position, string menuItemPath, MenuCommand command)
        {
            if (menuItemPath == "CONTEXT" || menuItemPath == "CONTEXT/" || menuItemPath == "CONTEXT\\")
            {
                bool error = false;
                if (command == null)
                    error = true;

                if (command != null && command.context == null)
                    error = true;

                if (error)
                {
                    Debug.LogError("DisplayPopupMenu: invalid arguments: using CONTEXT requires a valid MenuCommand object. If you want a custom context menu then try using the GenericMenu.");
                    return;
                }
            }

            Vector2 temp = GUIUtility.GUIToScreenPoint(new Vector2(position.x, position.y));
            position.x = temp.x;
            position.y = temp.y;
            Internal_DisplayPopupMenu(position, menuItemPath, command == null ? null : command.context, command == null ? 0 : command.userData);
            ResetMouseDown();
        }
    
    
    internal static void DisplayObjectContextMenu(Rect position, Object context, int contextUserData)
        {
            DisplayObjectContextMenu(position, new Object[] { context }, contextUserData);
        }
    
    
    internal static void DisplayObjectContextMenu(Rect position, Object[] context, int contextUserData)
        {
            Vector2 temp = GUIUtility.GUIToScreenPoint(new Vector2(position.x, position.y));
            position.x = temp.x;
            position.y = temp.y;

            Internal_DisplayObjectContextMenu(position, context, contextUserData);
            ResetMouseDown();
        }
    
    
    internal static void Internal_DisplayObjectContextMenu (Rect position, Object[] context, int contextUserData) {
        INTERNAL_CALL_Internal_DisplayObjectContextMenu ( ref position, context, contextUserData );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_DisplayObjectContextMenu (ref Rect position, Object[] context, int contextUserData);
    public delegate void SelectMenuItemFunction(object userData, string[] options, int selected);
    
    
    
    public static void DisplayCustomMenu(Rect position, GUIContent[] options, int selected, SelectMenuItemFunction callback, object userData)
        {
            DisplayCustomMenu(position, options, selected, callback, userData, false);
        }
    
    
    public static void DisplayCustomMenu(Rect position, GUIContent[] options, int selected, SelectMenuItemFunction callback, object userData, bool showHotkey)
        {
            int[] selectedArray = { selected };
            string[] strings = new string[options.Length];
            for (int i = 0; i < options.Length; i++)
                strings[i] = options[i].text;

            DisplayCustomMenu(position, strings, selectedArray, callback, userData, showHotkey);
        }
    
    
    internal static void DisplayCustomMenu(Rect position, string[] options, int[] selected, SelectMenuItemFunction callback, object userData)
        {
            DisplayCustomMenu(position, options, selected, callback, userData, false);
        }
    
    
    internal static void DisplayCustomMenu(Rect position, string[] options, int[] selected, SelectMenuItemFunction callback, object userData, bool showHotkey)
        {
            bool[] separator = new bool[options.Length];
            DisplayCustomMenuWithSeparators(position, options, separator, selected, callback, userData, showHotkey);
        }
    
    
    internal static void DisplayCustomMenuWithSeparators(Rect position, string[] options, bool[] separator, int[] selected, SelectMenuItemFunction callback, object userData)
        {
            DisplayCustomMenuWithSeparators(position, options, separator, selected, callback, userData, false);
        }
    
    
    internal static void DisplayCustomMenuWithSeparators(Rect position, string[] options, bool[] separator, int[] selected, SelectMenuItemFunction callback, object userData, bool showHotkey)
        {
            Vector2 temp = GUIUtility.GUIToScreenPoint(new Vector2(position.x, position.y));
            position.x = temp.x;
            position.y = temp.y;

            int[] ienabled = new int[options.Length];
            int[] iseparator = new int[options.Length];
            for (int i = 0; i < options.Length; i++)
            {
                ienabled[i] = 1;
                iseparator[i] = 0;
            }

            Internal_DisplayCustomMenu(position, options, ienabled, iseparator, selected, callback, userData, showHotkey);
            ResetMouseDown();
        }
    
    
    internal static void DisplayCustomMenu(Rect position, string[] options, bool[] enabled, int[] selected, SelectMenuItemFunction callback, object userData)
        {
            DisplayCustomMenu(position, options, enabled, selected, callback, userData, false);
        }
    
    
    internal static void DisplayCustomMenu(Rect position, string[] options, bool[] enabled, int[] selected, SelectMenuItemFunction callback, object userData, bool showHotkey)
        {
            bool[] separator = new bool[options.Length];
            DisplayCustomMenuWithSeparators(position, options, enabled, separator, selected, callback, userData, showHotkey);
        }
    
    
    internal static void DisplayCustomMenuWithSeparators(Rect position, string[] options, bool[] enabled, bool[] separator, int[] selected, SelectMenuItemFunction callback, object userData)
        {
            DisplayCustomMenuWithSeparators(position, options, enabled, separator, selected, callback, userData, false);
        }
    
    
    internal static void DisplayCustomMenuWithSeparators(Rect position, string[] options, bool[] enabled, bool[] separator, int[] selected, SelectMenuItemFunction callback, object userData, bool showHotkey)
        {
            Vector2 temp = GUIUtility.GUIToScreenPoint(new Vector2(position.x, position.y));
            position.x = temp.x;
            position.y = temp.y;

            int[] ienabled = new int[options.Length];
            int[] iseparator = new int[options.Length];
            for (int i = 0; i < options.Length; i++)
            {
                ienabled[i] = enabled[i] ? 1 : 0;
                iseparator[i] = separator[i] ? 1 : 0;
            }

            Internal_DisplayCustomMenu(position, options, ienabled, iseparator, selected, callback, userData, showHotkey);
            ResetMouseDown();
        }
    
    
    private static void Internal_DisplayCustomMenu(Rect screenPosition, string[] options, int[] enabled, int[] separator, int[] selected, SelectMenuItemFunction callback, object userData, bool showHotkey)
        {
            Private_DisplayCustomMenu(screenPosition, options, enabled, separator, selected, callback, userData, showHotkey);
        }
    
    
    private static void Private_DisplayCustomMenu (Rect screenPosition, string[] options, int[] enabled, int[] separator, int[] selected, SelectMenuItemFunction callback, object userData, bool showHotkey) {
        INTERNAL_CALL_Private_DisplayCustomMenu ( ref screenPosition, options, enabled, separator, selected, callback, userData, showHotkey );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Private_DisplayCustomMenu (ref Rect screenPosition, string[] options, int[] enabled, int[] separator, int[] selected, SelectMenuItemFunction callback, object userData, bool showHotkey);
    internal static void ResetMouseDown()
        {
            Tools.s_ButtonDown = -1;
            EditorGUIUtility.hotControl = 0;
        }
    
    
    [RequiredByNativeCode] public static void FocusProjectWindow()
        {
            ProjectBrowser prjBrowser = null;
            var focusedView = GUIView.focusedView as HostView;
            if (focusedView != null && focusedView.actualView is ProjectBrowser)
            {
                prjBrowser = focusedView.actualView as ProjectBrowser;
            }

            if (prjBrowser == null)
            {
                UnityEngine.Object[] wins = Resources.FindObjectsOfTypeAll(typeof(ProjectBrowser));
                if (wins.Length > 0)
                {
                    prjBrowser = wins[0] as ProjectBrowser;
                }
            }

            if (prjBrowser != null)
            {
                prjBrowser.Focus(); 
                var commandEvent = EditorGUIUtility.CommandEvent("FocusProjectWindow");
                prjBrowser.SendEvent(commandEvent);
            }
        }
    
    
    public static string FormatBytes(int bytes)
        {
            return FormatBytes((long)bytes);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string FormatBytes (long bytes) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void DisplayProgressBar (string title, string info, float progress) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool DisplayCancelableProgressBar (string title, string info, float progress) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ClearProgressBar () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetObjectEnabled (Object target) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetObjectEnabled (Object target, bool enabled) ;

    [System.Obsolete ("Use EditorUtility.SetSelectedRenderState")]
public static void SetSelectedWireframeHidden(Renderer renderer, bool enabled)
        {
            SetSelectedRenderState(renderer,
                enabled
                ? EditorSelectedRenderState.Hidden
                : EditorSelectedRenderState.Wireframe | EditorSelectedRenderState.Highlight);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetSelectedRenderState (Renderer renderer, EditorSelectedRenderState renderState) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void ForceReloadInspectors () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void ForceRebuildInspectors () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool ExtractOggFile (Object obj, string path) ;

    static public GameObject CreateGameObjectWithHideFlags(string name, HideFlags flags, params Type[] components)
        {
            GameObject go = Internal_CreateGameObjectWithHideFlags(name, flags);
            go.AddComponent(typeof(Transform));
            foreach (Type t in components)
                go.AddComponent(t);
            return go;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  GameObject Internal_CreateGameObjectWithHideFlags (string name, HideFlags flags) ;

    public static string[] CompileCSharp(string[] sources, string[] references, string[] defines, string outputFile)
        {
            return UnityEditor.Scripting.Compilers.MonoCSharpCompiler.Compile(sources, references, defines, outputFile);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void OpenWithDefaultApp (string fileName) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool WSACreateTestCertificate (string path, string publisher, string password, bool overwrite) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool IsWindows10OrGreater () ;

    [System.Obsolete ("Use PrefabUtility.InstantiatePrefab")]
public static Object InstantiatePrefab(Object target)
        {
            return PrefabUtility.InstantiatePrefab(target);
        }
    
    
    [System.Obsolete ("Use PrefabUtility.ReplacePrefab")]
public static GameObject ReplacePrefab(GameObject go, Object targetPrefab, ReplacePrefabOptions options)
        {
            return PrefabUtility.ReplacePrefab(go, targetPrefab, options);
        }
    
    
    [System.Obsolete ("Use PrefabUtility.ReplacePrefab")]
public static GameObject ReplacePrefab(GameObject go, Object targetPrefab)
        {
            return PrefabUtility.ReplacePrefab(go, targetPrefab, ReplacePrefabOptions.Default);
        }
    
    
    [System.Obsolete ("Use PrefabUtility.CreateEmptyPrefab")]
public static Object CreateEmptyPrefab(string path)
        {
            return PrefabUtility.CreateEmptyPrefab(path);
        }
    
    
    [System.Obsolete ("Use PrefabUtility.CreateEmptyPrefab")]
public static bool ReconnectToLastPrefab(GameObject go)
        {
            return PrefabUtility.ReconnectToLastPrefab(go);
        }
    
    
    [System.Obsolete ("Use PrefabUtility.GetPrefabType")]
public static PrefabType GetPrefabType(Object target)
        {
            return PrefabUtility.GetPrefabType(target);
        }
    
    
    [System.Obsolete ("Use PrefabUtility.GetPrefabParent")]
public static Object GetPrefabParent(Object source)
        {
            return PrefabUtility.GetPrefabParent(source);
        }
    
    
    [System.Obsolete ("Use PrefabUtility.FindPrefabRoot")]
static public GameObject FindPrefabRoot(GameObject source)
        {
            return PrefabUtility.FindPrefabRoot(source);
        }
    
    
    [System.Obsolete ("Use PrefabUtility.ResetToPrefabState")]
static public bool ResetToPrefabState(Object source)
        {
            return PrefabUtility.ResetToPrefabState(source);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetCameraAnimateMaterials (Camera camera, bool animate) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string GetInvalidFilenameChars () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool IsAutoRefreshEnabled () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string GetActiveNativePlatformSupportModuleName () ;

    public extern static bool audioMasterMute
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void LaunchBugReporter () ;

    internal extern static bool audioProfilingEnabled
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    public extern static bool scriptCompilationFailed
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool EventHasDragCopyModifierPressed (Event evt) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool EventHasDragMoveModifierPressed (Event evt) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  string GetInternalEditorPath () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SaveProjectAsTemplate (string targetPath, string name, string displayName, string description, string version) ;

}

public sealed partial class SceneAsset : Object
{
}


}
