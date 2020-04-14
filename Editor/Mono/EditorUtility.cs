// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Experimental;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Internal;
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

    public enum DialogOptOutDecisionType
    {
        ForThisMachine,
        ForThisSession,
    }

    public class SceneAsset : Object
    {
        private SceneAsset() {}
    }

    public partial class EditorUtility
    {
        static class Content
        {
            public static readonly string Cancel = L10n.Tr("Cancel");
            static readonly string k_DialogOptOutForThisMachine = L10n.Tr("Do not show me this message again on this machine.");
            static readonly string k_DialogOptOutForThisSession = L10n.Tr("Do not show me this message again for this session.");
            public static string GetDialogOptOutMessage(DialogOptOutDecisionType dialogOptOutType)
            {
                switch (dialogOptOutType)
                {
                    case DialogOptOutDecisionType.ForThisMachine:
                        return k_DialogOptOutForThisMachine;
                    case DialogOptOutDecisionType.ForThisSession:
                        return k_DialogOptOutForThisSession;
                    default:
                        throw new NotImplementedException(string.Format("The DialogOptOut type named {0} has not been implemented.", dialogOptOutType));
                }
            }
        }

        private static readonly Stack<StringBuilder> _SbPool = new Stack<StringBuilder>();

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

        private static System.Collections.Generic.Dictionary<int, string> s_ActiveIconPathLUT = new System.Collections.Generic.Dictionary<int, string>();
        internal static Texture GetIconInActiveState(Texture icon)
        {
            if (icon == null)
                return null;

            string selectedIconPath;
            var iconInstanceId = icon.GetInstanceID();
            if (!s_ActiveIconPathLUT.TryGetValue(iconInstanceId, out selectedIconPath))
            {
                const string iconText = " icon";
                const string onText = " on";

                string path = EditorResources.GetAssetPath(icon).ToLowerInvariant();
                if (path.Contains(iconText))
                    selectedIconPath = path.Replace(iconText, onText + iconText);
                else if (path.Contains('@'))
                    selectedIconPath = path.Replace("@", onText + "@");
                else if (path.EndsWith(".png"))
                    selectedIconPath = path.Replace(".png", onText + ".png");

                s_ActiveIconPathLUT[iconInstanceId] = selectedIconPath;
            }

            return EditorResources.Load<Texture>(selectedIconPath, false);
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

        public static bool GetDialogOptOutDecision(DialogOptOutDecisionType dialogOptOutDecisionType, string dialogOptOutDecisionStorageKey)
        {
            switch (dialogOptOutDecisionType)
            {
                case DialogOptOutDecisionType.ForThisMachine:
                    return EditorPrefs.GetBool(dialogOptOutDecisionStorageKey, false);
                case DialogOptOutDecisionType.ForThisSession:
                    return SessionState.GetBool(dialogOptOutDecisionStorageKey, false);
                default:
                    throw new NotImplementedException(string.Format("The DialogOptOut type named {0} has not been implemented.", dialogOptOutDecisionType));
            }
        }

        public static void SetDialogOptOutDecision(DialogOptOutDecisionType dialogOptOutDecisionType, string dialogOptOutDecisionStorageKey, bool optOutDecision)
        {
            switch (dialogOptOutDecisionType)
            {
                case DialogOptOutDecisionType.ForThisMachine:
                    EditorPrefs.SetBool(dialogOptOutDecisionStorageKey, optOutDecision);
                    break;
                case DialogOptOutDecisionType.ForThisSession:
                    SessionState.SetBool(dialogOptOutDecisionStorageKey, optOutDecision);
                    break;
                default:
                    throw new NotImplementedException(string.Format("The DialogOptOut type named {0} has not been implemented.", dialogOptOutDecisionType));
            }
        }

        public static bool DisplayDialog(string title, string message, string ok, DialogOptOutDecisionType dialogOptOutDecisionType, string dialogOptOutDecisionStorageKey)
        {
            return DisplayDialog(title, message, ok, string.Empty, dialogOptOutDecisionType, dialogOptOutDecisionStorageKey);
        }

        public static bool DisplayDialog(string title, string message, string ok, [DefaultValue("\"\"")] string cancel, DialogOptOutDecisionType dialogOptOutDecisionType, string dialogOptOutDecisionStorageKey)
        {
            if (GetDialogOptOutDecision(dialogOptOutDecisionType, dialogOptOutDecisionStorageKey))
            {
                return true;
            }
            else
            {
                bool optOutDecision;
                bool dialogDecision = DisplayDialog(title, message, ok, cancel, Content.GetDialogOptOutMessage(dialogOptOutDecisionType), out optOutDecision);
                // Cancel means the user pressed ESC as the Cancel button was grayed out. Don't store the opt-out decision on cancel. Also, only store it if the user opted out since it defaults to opt-in.
                if (dialogDecision && optOutDecision)
                    SetDialogOptOutDecision(dialogOptOutDecisionType, dialogOptOutDecisionStorageKey, optOutDecision);
                return dialogDecision;
            }
        }

        // TODO: This is an MVP solution. The OptOut option should be a check-box in the dialog. To achieve that, this API will need to move to bindings and get platform specific implementations.
        static bool DisplayDialog(string title, string message, string ok, string cancel, string optOutText, out bool optOutDecision)
        {
            if (string.IsNullOrEmpty(cancel))
            {
                // we can't allow empty cancel buttons in this MVP workaround. Only the two button dialog would be possible to use and it can't differentiate between pressing a cancel button (labeled with OptOut text) and pressing X or ESC.
                cancel = Content.Cancel;
            }
            int result = DisplayDialogComplex(title, message, ok, cancel, string.Format("{0} - {1}", ok, optOutText));
            // result 0 -> OK, 1 -> Cancel, 2 -> Ok & opt out
            optOutDecision = result == 2;
            return result != 1;
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
            return Scripting.Compilers.MicrosoftCSharpCompiler.Compile(sources, references, defines, outputFile, PlayerSettings.allowUnsafeCode);
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

        public static void DisplayCustomMenuWithSeparators(Rect position, string[] options, bool[] enabled, bool[] separator, int[] selected, SelectMenuItemFunction callback, object userData)
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
                if (PrefabUtility.GetCorrespondingConnectedObjectFromSource(targetComponent.gameObject) == null) {}
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
                            if (!PrefabUtility.IsPartOfPrefabThatCanBeAppliedTo(rootObject) || EditorUtility.IsPersistent(instanceGo))
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
                    bool hasPrefabOverride = false;
                    using (var so = new SerializedObject(targetObject))
                    {
                        SerializedProperty property = so.GetIterator();
                        while (property.Next(property.hasChildren))
                        {
                            if (property.isInstantiatedPrefab && property.prefabOverride && !property.isDefaultOverride)
                            {
                                hasPrefabOverride = true;
                                break;
                            }
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
                                if (!PrefabUtility.IsPartOfPrefabThatCanBeAppliedTo(rootObject) || EditorUtility.IsPersistent(targetObject))
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

        private static void Internal_DisplayCustomMenu(Rect screenPosition, string[] options, bool[] enabled, bool[] separator, int[] selected, SelectMenuItemFunction callback, object userData, bool showHotkey, bool allowDisplayNames = false)
        {
            DisplayCustomContextPopupMenu(screenPosition, options, enabled, separator, selected, callback, userData, showHotkey, allowDisplayNames);
        }

        internal static string GetTransformPath(Transform tform)
        {
            if (tform.parent == null)
                return "/" + tform.name;
            return GetTransformPath(tform.parent) + "/" + tform.name;
        }

        internal static string GetHierarchyPath(GameObject gameObject, bool includeScene = true)
        {
            if (gameObject == null)
                return String.Empty;

            StringBuilder sb;
            if (_SbPool.Count > 0)
            {
                sb = _SbPool.Pop();
                sb.Clear();
            }
            else
            {
                sb = new StringBuilder(200);
            }

            try
            {
                if (includeScene)
                {
                    var sceneName = gameObject.scene.name;
                    if (sceneName == string.Empty)
                    {
                        var prefabStage = PrefabStageUtility.GetPrefabStage(gameObject);
                        if (prefabStage != null)
                        {
                            sceneName = "Prefab Stage";
                        }
                        else
                        {
                            sceneName = "Unsaved Scene";
                        }
                    }

                    sb.Append(sceneName);
                }

                sb.Append(GetTransformPath(gameObject.transform));

                var path = sb.ToString();
                sb.Clear();
                return path;
            }
            finally
            {
                _SbPool.Push(sb);
            }
        }
    }
}
