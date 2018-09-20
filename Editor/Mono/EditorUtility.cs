// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [Flags]
    public enum EditorSelectedRenderState
    {
        Hidden = 0,
        Wireframe = 1,
        Highlight = 2
    }

    public enum InteractionMode
    {
        // Perform action without undo or dialogs.
        AutomatedAction,
        // Record undo and allow potential dialogs to pop up.
        UserAction
    }

    // Compression Quality. Corresponds to the settings in a [[wiki:class-Texture2D|texture inspector]].
    public enum TextureCompressionQuality
    {
        Fast = 0, // Fast compression
        Normal = 50, // Normal compression (default)
        Best = 100 // Best compression
    }

    public class SceneAsset : Object
    {
        private SceneAsset() {}
    }

    public partial class EditorUtility
    {
        public delegate void SelectMenuItemFunction(object userData, string[] options, int selected);

        public static bool LoadWindowLayout(string path)
        {
            return WindowLayout.LoadWindowLayout(path, false);
        }

        public static void CompressTexture(Texture2D texture, TextureFormat format, TextureCompressionQuality quality)
        {
            CompressTexture(texture, format, (int)quality);
        }

        private static void CompressTexture(Texture2D texture, TextureFormat format)
        {
            CompressTexture(texture, format, TextureCompressionQuality.Normal);
        }

        public static void CompressCubemapTexture(Cubemap texture, TextureFormat format, TextureCompressionQuality quality)
        {
            CompressCubemapTexture(texture, format, (int)quality);
        }

        private static void CompressCubemapTexture(Cubemap texture, TextureFormat format)
        {
            CompressCubemapTexture(texture, format, TextureCompressionQuality.Normal);
        }

        public static string SaveFilePanelInProject(string title, string defaultName, string extension, string message)
        {
            return Internal_SaveFilePanelInProject(title, defaultName, extension, message, "Assets");
        }

        public static string SaveFilePanelInProject(string title, string defaultName, string extension, string message, string path)
        {
            return Internal_SaveFilePanelInProject(title, defaultName, extension, message, path);
        }

        public static void CopySerializedIfDifferent(Object source, Object dest)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (dest == null)
                throw new ArgumentNullException(nameof(dest));

            InternalCopySerializedIfDifferent(source, dest);
        }

        [Obsolete("Use AssetDatabase.GetAssetPath", false)]
        public static string GetAssetPath(Object asset)
        {
            return AssetDatabase.GetAssetPath(asset);
        }

        public static void UnloadUnusedAssetsImmediate()
        {
            UnloadUnusedAssets(true);
        }

        public static void UnloadUnusedAssetsImmediate(bool includeMonoReferencesAsRoots)
        {
            UnloadUnusedAssets(includeMonoReferencesAsRoots);
        }

        [Obsolete("Use BuildPipeline.BuildAssetBundle instead")]
        public static bool BuildResourceFile(Object[] selection, string pathName)
        {
            return false;
        }

        public static void DisplayPopupMenu(Rect position, string menuItemPath, MenuCommand command)
        {
            // Validate input. Fixes case 406024: 'Custom context menu in a custom window crashes Unity'
            if (menuItemPath == "CONTEXT" || menuItemPath == "CONTEXT/" || menuItemPath == "CONTEXT\\")
            {
                bool error = command == null || command.context == null;
                if (error)
                {
                    Debug.LogError("DisplayPopupMenu: invalid arguments: using CONTEXT requires a valid MenuCommand object. If you want a custom context menu then try using the GenericMenu.");
                    return;
                }
            }

            Vector2 temp = GUIUtility.GUIToScreenPoint(new Vector2(position.x, position.y));
            position.x = temp.x;
            position.y = temp.y;
            Internal_DisplayPopupMenu(position, menuItemPath, command?.context, command?.userData ?? 0);
            ResetMouseDown();
        }

        public static void DisplayCustomMenu(Rect position, GUIContent[] options, int selected, SelectMenuItemFunction callback, object userData)
        {
            DisplayCustomMenu(position, options, null, selected, callback, userData, false);
        }

        public static void DisplayCustomMenu(Rect position, GUIContent[] options, int selected, SelectMenuItemFunction callback, object userData, bool showHotkey)
        {
            DisplayCustomMenu(position, options, null, selected, callback, userData, showHotkey);
        }

        public static void DisplayCustomMenu(Rect position, GUIContent[] options, Func<int, bool> checkEnabled, int selected, SelectMenuItemFunction callback, object userData, bool showHotkey = false)
        {
            int[] selectedArray = { selected };
            string[] strings = new string[options.Length];
            for (int i = 0; i < options.Length; i++)
                strings[i] = options[i].text;

            bool[] enabled;
            if (checkEnabled != null)
            {
                enabled = new bool[strings.Length];
                for (int i = 0; i < strings.Length; i++)
                {
                    enabled[i] = checkEnabled(i);
                }
            }
            else
            {
                enabled = Enumerable.Repeat(true, options.Length).ToArray();
            }
            DisplayCustomMenu(position, strings, enabled, selectedArray, callback, userData, showHotkey);
        }

        public static string FormatBytes(int bytes)
        {
            return FormatBytes((long)bytes);
        }

        [Obsolete("Use EditorUtility.SetSelectedRenderState", false)]
        public static void SetSelectedWireframeHidden(Renderer renderer, bool enabled)
        {
            SetSelectedRenderState(renderer,
                enabled
                ? EditorSelectedRenderState.Hidden
                : EditorSelectedRenderState.Wireframe | EditorSelectedRenderState.Highlight);
        }

        public static GameObject CreateGameObjectWithHideFlags(string name, HideFlags flags, params Type[] components)
        {
            GameObject go = Internal_CreateGameObjectWithHideFlags(name, flags);
            // always add Transform
            go.AddComponent(typeof(Transform));
            foreach (Type t in components)
                go.AddComponent(t);
            return go;
        }

        public static string[] CompileCSharp(string[] sources, string[] references, string[] defines, string outputFile)
        {
            return Scripting.Compilers.MonoCSharpCompiler.Compile(sources, references, defines, outputFile, PlayerSettings.allowUnsafeCode);
        }

        [Obsolete("Use PrefabUtility.InstantiatePrefab", false)]
        public static Object InstantiatePrefab(Object target)
        {
            return PrefabUtility.InstantiatePrefab(target);
        }

        [Obsolete("Use PrefabUtility.SaveAsPrefabAsset with a path instead.", false)]
        public static GameObject ReplacePrefab(GameObject go, Object targetPrefab, ReplacePrefabOptions options)
        {
            return PrefabUtility.ReplacePrefab(go, targetPrefab, options);
        }

        [Obsolete("Use PrefabUtility.SaveAsPrefabAsset or PrefabUtility.SaveAsPrefabAssetAndConnect with a path instead.", false)]
        public static GameObject ReplacePrefab(GameObject go, Object targetPrefab)
        {
            return PrefabUtility.ReplacePrefab(go, targetPrefab, ReplacePrefabOptions.Default);
        }

        [Obsolete("The concept of creating a completely empty Prefab has been discontinued. You can however use PrefabUtility.SaveAsPrefabAsset with an empty GameObject.", false)]
        public static Object CreateEmptyPrefab(string path)
        {
            return PrefabUtility.CreateEmptyPrefab(path);
        }

        [Obsolete("Use PrefabUtility.RevertPrefabInstance.", false)]
        public static bool ReconnectToLastPrefab(GameObject go)
        {
            return PrefabUtility.ReconnectToLastPrefab(go);
        }

        [Obsolete("Use PrefabUtility.GetPrefabAssetType and PrefabUtility.GetPrefabInstanceStatus to get the full picture about Prefab types.", false)]
        public static PrefabType GetPrefabType(Object target)
        {
            return PrefabUtility.GetPrefabType(target);
        }

        [Obsolete("Use PrefabUtility.GetCorrespondingObjectFromSource.", false)]
        public static Object GetPrefabParent(Object source)
        {
            return PrefabUtility.GetCorrespondingObjectFromSource(source);
        }

        [Obsolete("Use PrefabUtility.GetOutermostPrefabInstanceRoot if source is a Prefab instance or source.transform.root.gameObject if source is a Prefab Asset object.", false)]
        public static GameObject FindPrefabRoot(GameObject source)
        {
            return PrefabUtility.FindPrefabRoot(source);
        }

        [Obsolete("Use PrefabUtility.RevertObjectOverride.", false)]
        public static bool ResetToPrefabState(Object source)
        {
            return PrefabUtility.ResetToPrefabState(source);
        }

        internal static void ResetMouseDown()
        {
            Tools.s_ButtonDown = -1;
            GUIUtility.hotControl = 0;
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

            bool[] enabled = Enumerable.Repeat(true, options.Length).ToArray();
            Internal_DisplayCustomMenu(position, options, enabled, separator, selected, callback, userData, showHotkey);
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
            DisplayCustomMenuWithSeparators(position, options, enabled, separator, selected, callback, userData, showHotkey, false);
        }

        internal static void DisplayCustomMenuWithSeparators(Rect position, string[] options, bool[] enabled, bool[] separator, int[] selected, SelectMenuItemFunction callback, object userData, bool showHotkey, bool allowDisplayNames)
        {
            Vector2 temp = GUIUtility.GUIToScreenPoint(new Vector2(position.x, position.y));
            position.x = temp.x;
            position.y = temp.y;

            Internal_DisplayCustomMenu(position, options, enabled, separator, selected, callback, userData, showHotkey, allowDisplayNames);
            ResetMouseDown();
        }

        internal static void DisplayObjectContextMenu(Rect position, Object context, int contextUserData)
        {
            DisplayObjectContextMenu(position, new[] { context }, contextUserData);
        }

        internal static void DisplayObjectContextMenu(Rect position, Object[] context, int contextUserData)
        {
            // Don't show context menu if we're inside the side-by-side diff comparison.
            if (EditorGUIUtility.comparisonViewMode != EditorGUIUtility.ComparisonViewMode.None)
                return;

            Vector2 temp = GUIUtility.GUIToScreenPoint(new Vector2(position.x, position.y));
            position.x = temp.x;
            position.y = temp.y;

            GenericMenu pm = new GenericMenu();

            if (context != null && context.Length == 1 && context[0] is Component)
            {
                Object targetObject = context[0];
                Component targetComponent = (Component)targetObject;

                // Do nothing if component is not on a prefab instance.
                if (PrefabUtility.GetCorrespondingObjectFromSource(targetComponent.gameObject) == null) {}
                // Handle added component.
                else if (PrefabUtility.GetCorrespondingObjectFromSource(targetObject) == null && targetComponent != null)
                {
                    GameObject instanceGo = targetComponent.gameObject;
                    PrefabUtility.HandleApplyRevertMenuItems(
                        "Added Component",
                        instanceGo,
                        (menuItemContent, sourceGo) =>
                        {
                            TargetChoiceHandler.ObjectInstanceAndSourcePathInfo info = new TargetChoiceHandler.ObjectInstanceAndSourcePathInfo();
                            info.instanceObject = targetComponent;
                            info.assetPath = AssetDatabase.GetAssetPath(sourceGo);
                            GameObject rootObject = PrefabUtility.GetRootGameObject(sourceGo);
                            if (!PrefabUtility.IsPartOfPrefabThatCanBeAppliedTo(rootObject))
                                pm.AddDisabledItem(menuItemContent);
                            else
                                pm.AddItem(menuItemContent, false, TargetChoiceHandler.ApplyPrefabAddedComponent, info);
                        },
                        (menuItemContent) =>
                        {
                            pm.AddItem(menuItemContent, false, TargetChoiceHandler.RevertPrefabAddedComponent, targetComponent);
                        }
                    );
                }
                else
                {
                    SerializedObject so = new SerializedObject(targetObject);
                    SerializedProperty property = so.GetIterator();
                    bool hasPrefabOverride = false;
                    while (property.Next(property.hasChildren))
                    {
                        if (property.isInstantiatedPrefab && property.prefabOverride && !property.isDefaultOverride)
                        {
                            hasPrefabOverride = true;
                            break;
                        }
                    }

                    // Handle modified component.
                    if (hasPrefabOverride)
                    {
                        bool defaultOverrides =
                            PrefabUtility.IsObjectOverrideAllDefaultOverridesComparedToAnySource(targetObject);

                        PrefabUtility.HandleApplyRevertMenuItems(
                            "Modified Component",
                            targetObject,
                            (menuItemContent, sourceObject) =>
                            {
                                TargetChoiceHandler.ObjectInstanceAndSourcePathInfo info = new TargetChoiceHandler.ObjectInstanceAndSourcePathInfo();
                                info.instanceObject = targetObject;
                                info.assetPath = AssetDatabase.GetAssetPath(sourceObject);
                                GameObject rootObject = PrefabUtility.GetRootGameObject(sourceObject);
                                if (!PrefabUtility.IsPartOfPrefabThatCanBeAppliedTo(rootObject))
                                    pm.AddDisabledItem(menuItemContent);
                                else
                                    pm.AddItem(menuItemContent, false, TargetChoiceHandler.ApplyPrefabObjectOverride, info);
                            },
                            (menuItemContent) =>
                            {
                                pm.AddItem(menuItemContent, false, TargetChoiceHandler.RevertPrefabObjectOverride, targetObject);
                            },
                            defaultOverrides
                        );
                    }
                }
            }

            pm.ObjectContextDropDown(position, context, contextUserData);

            ResetMouseDown();
        }

        internal static void Internal_DisplayPopupMenu(Rect position, string menuItemPath, Object context, int contextUserData)
        {
            Private_DisplayPopupMenu(position, menuItemPath, context, contextUserData);
        }

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
                throw new ArgumentException("The Prefab you want to instantiate is null.");

            GameObject go = InstantiateRemoveAllNonAnimationComponents(original, Vector3.zero, Quaternion.identity) as GameObject;
            go.name = go.name + "AnimatorPreview";       //To avoid FindGameObject picking up our dummy object
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

            // We absolutely need an animator on the gameObject
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
                throw new ArgumentException("The Prefab you want to instantiate is null.");

            return Internal_InstantiateRemoveAllNonAnimationComponentsSingle(original, position, rotation);
        }

        internal static bool IsUnityAssembly(Object target)
        {
            if (target == null)
                return false;
            Type type = target.GetType();
            return IsUnityAssembly(type);
        }

        internal static bool IsUnityAssembly(Type type)
        {
            if (type == null)
                return false;
            string assemblyName = type.Assembly.GetName().Name;
            if (assemblyName.StartsWith("UnityEditor"))
                return true;
            if (assemblyName.StartsWith("UnityEngine"))
                return true;
            return false;
        }

        private static void Internal_DisplayCustomMenu(Rect screenPosition, string[] options, bool[] enabled, bool[] separator, int[] selected, SelectMenuItemFunction callback, object userData, bool showHotkey, bool allowDisplayNames = false)
        {
            DisplayCustomContextPopupMenu(screenPosition, options, enabled, separator, selected, callback, userData, showHotkey, allowDisplayNames);
        }
    }
}
